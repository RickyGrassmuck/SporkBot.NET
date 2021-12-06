using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor
    {
        public static ISeedSearchHandler<PK8> SeedChecker = new NoSeedSearchHandler<PK8>();
        private readonly PokeTradeHub<PK8> Hub;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        private readonly IDumper DumpSetting;

        private readonly IGiveaway GiveawaySetting;

        /// <summary>
        /// Synchronized start for multiple bots.
        /// </summary>
        public bool ShouldWaitAtBarrier { get; private set; }

        /// <summary>
        /// Tracks failed synchronized starts to attempt to re-sync.
        /// </summary>
        public int FailedBarrier { get; private set; }

        public PokeTradeBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Hub = hub;
            DumpSetting = hub.Config.Folder;
            GiveawaySetting = hub.Config.Giveaway;
        }

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        protected override async Task MainLoop(CancellationToken token)
        {
            LogUtil.LogInfo("Identifying trainer data of the host console.", "PokeTradeBot");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);

            LogUtil.LogInfo("Starting main TradeBot loop.", "PokeTradeBot");
            while (!token.IsCancellationRequested)
            {
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    PokeRoutineType.Idle => DoNothing(token),
                    _ => DoTrades(sav, token),
                };
                await task.ConfigureAwait(false);
            }
            Hub.Bots.Remove(this);
        }

        private async Task DoNothing(CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (waitCounter == 0)
                    LogUtil.LogInfo("No task assigned. Waiting for new task assignment.", "PokeTradeBot");
                waitCounter++;
                if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                    await Click(B, 1_000, token).ConfigureAwait(false);
                else
                    await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task DoTrades(SAV8SWSH sav, CancellationToken token)
        {
            var type = Config.CurrentRoutineType;
            int waitCounter = 0;
            await SetCurrentBox(0, token).ConfigureAwait(false);
            while (!token.IsCancellationRequested && Config.NextRoutineType == type)
            {
                if (!Hub.Queues.TryDequeue(type, out var detail, out var priority))
                {
                    if (waitCounter == 0)
                    {
                        // Updates the assets.
                        Hub.Config.Stream.IdleAssets(this);
                        LogUtil.LogInfo("Nothing to check, waiting for new users...", "PokeTradeBot");
                    }
                    waitCounter++;
                    if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                        await Click(B, 1_000, token).ConfigureAwait(false);
                    else
                        await Task.Delay(1_000, token).ConfigureAwait(false);
                    continue;
                }
                waitCounter = 0;

                string tradetype = $" ({detail.Type})";
                Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
                Hub.Config.Stream.StartTrade(this, detail, Hub);
                Hub.Queues.StartTrade(this, detail);

                await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);
                var result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);
                if (result != PokeTradeResult.Success) // requeue
                {
                    if (result.AttemptRetry() && !detail.IsRetry)
                    {
                        detail.IsRetry = true;
                        detail.SendNotification(this, "Oops! Something happened. I'll requeue you for another attempt.");
                        Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradeQueue<PK8>.Tier2));
                    }
                    else
                    {
                        detail.SendNotification(this, $"Oops! Something happened. Canceling the trade: {result}.");
                        detail.TradeCanceled(this, result);
                    }
                }
            }

        }

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8SWSH sav, PokeTradeDetail<PK8> poke, CancellationToken token)
        {
            // Update Barrier Settings
            poke.TradeInitialize(this);
            Hub.Config.Stream.EndEnterCode(this);

            if (await CheckIfSoftBanned(token).ConfigureAwait(false))
                await Unban(token).ConfigureAwait(false);

            var pkm = poke.TradeData;

            if (pkm.Species != 0)
                await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            if (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverStart;
            }

            while (await CheckIfSearchingForLinkTradePartner(token).ConfigureAwait(false))
            {
                Log("Still searching, reset bot position.");
                await ResetTradePosition(Hub.Config, token).ConfigureAwait(false);
            }

            Log("Opening Y-Comm Menu");
            await Click(Y, 2_000, token).ConfigureAwait(false);

            Log("Selecting Link Trade");
            await Click(A, 1_500, token).ConfigureAwait(false);

            Log("Selecting Link Trade Code");
            await Click(DDOWN, 500, token).ConfigureAwait(false);

            for (int i = 0; i < 2; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);

            // All other languages require an extra A press at this menu.
            if (GameLang != LanguageID.English && GameLang != LanguageID.Spanish)
                await Click(A, 1_500, token).ConfigureAwait(false);
                Log(LanguageID.English.ToString());

            // Loading Screen
            await Task.Delay(1_000, token).ConfigureAwait(false);

            // Create Code Assets
            Hub.Config.Stream.StartEnterCode(this);
            await Task.Delay(1_000, token).ConfigureAwait(false);

            // Enter Code
            var code = poke.Code;
            Log($"Entering Link Trade Code: {code:0000 0000}...");
            await EnterTradeCode(code, Hub.Config, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);


            await Click(PLUS, 1_000, token).ConfigureAwait(false);

            Hub.Config.Stream.EndEnterCode(this);

            // Confirming and return to overworld.
            var delay_count = 0;
            while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
            {
                if (delay_count >= 5)
                {
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.RecoverPostLinkCode;
                }

                for (int i = 0; i < 5; i++)
                    await Click(A, 0_800, token).ConfigureAwait(false);
                delay_count++;
            }

            poke.TradeSearching(this);
            await Task.Delay(0_500, token).ConfigureAwait(false);

            // Wait for a Trainer...
            Log("Waiting for trainer...");
            bool partnerFound = await WaitForPokemonChanged(LinkTradePartnerPokemonOffset, Hub.Config.Trade.TradeWaitTime * 1_000, 0_200, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;
            if (!partnerFound)
            {
                await ResetTradePosition(Hub.Config, token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Select Pokemon
            // pkm already injected to b1s1
            await Task.Delay(5_500, token).ConfigureAwait(false); // necessary delay to get to the box properly

            var TrainerName = await GetTradePartnerName(TradeMethod.LinkTrade, token).ConfigureAwait(false);
            Log($"Found Trading Partner: {TrainerName}...");

            // Make sure we're in the box screen
            if (!await IsInBox(token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverOpenBox;
            }

            LogUtil.LogInfo($"PokeTradeType: {poke.Type}", "PokeTradeBot");
            // Confirm Box 1 Slot 1
            if (poke.Type == PokeTradeType.Specific || poke.Type == PokeTradeType.Giveaway)
            {
                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }

            poke.SendNotification(this, $"Found Trading Partner: {TrainerName}. Waiting for a Pokémon...");

            if (poke.Type == PokeTradeType.Dump)
                return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

            if (poke.Type == PokeTradeType.GiveawayUpload)
                return await ProcessGiveawayUploadAsync(poke, token).ConfigureAwait(false);

            if (poke.Type == PokeTradeType.Clone)
                return await ProcessCloneTradeAsync(poke, token, sav);

            // Wait for User Input...
            var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, token).ConfigureAwait(false);
            var oldEC = await Connection.ReadBytesAsync(LinkTradePartnerPokemonOffset, 4, Config.ConnectionType, token).ConfigureAwait(false);
            if (pk == null)
            {
                if (poke.Type == PokeTradeType.Seed)
                    await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);
                else
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);

                return PokeTradeResult.TrainerTooSlow;
            }

            if (poke.Type == PokeTradeType.Seed)
            {
                // Immediately exit, we aren't trading anything.
                return await EndSeedCheckTradeAsync(poke, pk, token).ConfigureAwait(false);
            }
            else if (poke.Type == PokeTradeType.FixOT)
            {
                var clone = (PK8)pk.Clone();
                var adOT = System.Text.RegularExpressions.Regex.Match(clone.OT_Name, @"(YT$)|(YT\w*$)|(Lab$)|(\.\w*)|(TV$)|(PKHeX)|(FB:)|(SysBot)|(AuSLove)|(ShinyMart)").Value != ""
                    || System.Text.RegularExpressions.Regex.Match(clone.Nickname, @"(YT$)|(YT\w*$)|(Lab$)|(\.\w*)|(TV$)|(PKHeX)|(FB:)|(SysBot)|(AuSLove)|(ShinyMart)").Value != "";

                var extraInfo = $"\nBall: {(Ball)clone.Ball}\nShiny: {(clone.ShinyXor == 0 ? "Square" : clone.ShinyXor <= 16 ? "Star" : "No")}{(clone.FatefulEncounter ? "" : $"\nOT: {TrainerName}")}";
                var laInit = new LegalityAnalysis(clone);
                if (laInit.Valid && adOT)
                {
                    clone.OT_Name = clone.FatefulEncounter ? clone.OT_Name : $"{TrainerName}";
                    clone.PKRS_Infected = false;
                    clone.PKRS_Cured = false;
                    clone.PKRS_Days = 0;
                    clone.PKRS_Strain = 0;
                }
                else if (!laInit.Valid)
                {
                    Log($"FixOT request has detected an invalid Pokémon: {(Species)clone.Species}");
                    if (DumpSetting.Dump)
                        DumpPokemon(DumpSetting.DumpFolder, "hacked", clone);

                    poke.SendNotification(this, $"```fix\nShown Pokémon is invalid. Attempting to regenerate... \n{laInit.Report()}```");
                    clone = (PK8)AutoLegalityWrapper.GetTrainerInfo(8).GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(ShowdownSet.GetShowdownText(clone) + extraInfo)), out _);
                    var laRegen = new LegalityAnalysis(clone);
                    if (laRegen.Valid)
                        poke.SendNotification(this, $"```fix\nRegenerated and legalized your {(Species)clone.Species}!```");
                }
                else if (!adOT && laInit.Valid)
                {
                    poke.SendNotification(this, "```fix\nNo ad detected in Nickname or OT, and the Pokémon is legal. Exiting trade...```");
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.Aborted;
                }

                clone = (PK8)TradeExtensions.TrashBytes(clone);
                var la = new LegalityAnalysis(clone);
                if (!la.Valid && Hub.Config.Legality.VerifyLegality)
                {
                    var report = la.Report();
                    Log(report);
                    poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from fixing this. Exiting trade.");
                    poke.SendNotification(this, report);

                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.IllegalTrade;
                }

                if (Hub.Config.Legality.ResetHOMETracker)
                    clone.Tracker = 0;

                poke.SendNotification(this, $"```fix\nNow confirm the trade!```");
                Log($"Fixed Nickname/OT for {(Species)clone.Species}!");

                await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, token).ConfigureAwait(false);
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(clone, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);
                pkm = clone;

                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }
            else if (poke.Type == PokeTradeType.Clone)
            {
                // Inject the shown Pokémon.
                var clone = (PK8)pk.Clone();

                if (Hub.Config.Discord.ReturnPK8s)
                    poke.SendNotification(this, clone, "Here's what you showed me!");

                var la = new LegalityAnalysis(clone);
                if (!la.Valid && Hub.Config.Legality.VerifyLegality)
                {
                    Log($"Clone request has detected an invalid Pokémon: {(Species)clone.Species}");
                    if (DumpSetting.Dump)
                        DumpPokemon(DumpSetting.DumpFolder, "hacked", clone);

                    var report = la.Report();
                    Log(report);
                    poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                    poke.SendNotification(this, report);

                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.IllegalTrade;
                }

                if (Hub.Config.Legality.ResetHOMETracker)
                    clone.Tracker = 0;

                poke.SendNotification(this, $"**Cloned your {(Species)clone.Species}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
                Log($"Cloned a {(Species)clone.Species}. Waiting for user to change their Pokémon...");

                // Separate this out from WaitForPokemonChanged since we compare to old EC from original read.
                partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, oldEC, 15_000, 0_200, false, token).ConfigureAwait(false);

                if (!partnerFound)
                {
                    poke.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");
                    // They get one more chance.
                    partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, oldEC, 15_000, 0_200, false, token).ConfigureAwait(false);
                }

                var pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, token).ConfigureAwait(false);
                if (!partnerFound || pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(pk))
                {
                    Log("Trading partner did not change their Pokémon.");
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.TrainerTooSlow;
                }

                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(clone, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);
                pkm = clone;

                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);

            delay_count = 0;
            while (!await IsInBox(token).ConfigureAwait(false))
            {
                await Click(A, 3_000, token).ConfigureAwait(false);
                delay_count++;
                if (delay_count >= 50)
                    break;
                if (await IsOnOverworld(Hub.Config, token).ConfigureAwait(false)) // In case we are in a Trade Evolution/PokeDex Entry and the Trade Partner quits we land on the Overworld
                    break;
            }

            await Task.Delay(1_000 + Util.Rand.Next(0_700, 1_000), token).ConfigureAwait(false);

            await ExitTrade(Hub.Config, false, token).ConfigureAwait(false);
            Log("Exited Trade!");

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            // Trade was Successful!
            var traded = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (poke.Type != PokeTradeType.FixOT && SearchUtil.HashByDetails(traded) == SearchUtil.HashByDetails(pkm))
            {
                Log("User did not complete the trade.");
                return PokeTradeResult.TrainerTooSlow;
            }
            else
            {
                // As long as we got rid of our inject in b1s1, assume the trade went through.
                Log("User completed the trade.");
                poke.TradeFinished(this, traded);

                // Only log if we completed the trade.
                var counts = Hub.Counts;
                if (poke.Type == PokeTradeType.Clone)
                    counts.AddCompletedClones();
                else if (poke.Type == PokeTradeType.FixOT)
                    counts.AddCompletedFixOTs();
                else if (poke.Type == PokeTradeType.Giveaway)
                    counts.AddCompletedGiveaways();
                else
                    Hub.Counts.AddCompletedTrade();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                {
                    var subfolder = poke.Type.ToString().ToLower();
                    DumpPokemon(DumpSetting.DumpFolder, subfolder, traded); // received
                    if (poke.Type == PokeTradeType.Specific || poke.Type == PokeTradeType.Clone || poke.Type == PokeTradeType.FixOT || poke.Type == PokeTradeType.Giveaway)
                        DumpPokemon(DumpSetting.DumpFolder, "traded", pkm); // sent to partner
                }
            }

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> ProcessCloneTradeAsync(PokeTradeDetail<PK8> detail, CancellationToken token, SAV8SWSH sav)
        {

            var startingPk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, token).ConfigureAwait(false);

            if (startingPk == null || startingPk.Species < 1 || !startingPk.ChecksumValid)
            {
                Log("Trading partner did not Show their Pokémon.");
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            // Legality Check
            if (!IsLegal(startingPk, detail))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.IllegalTrade;
            }

            // Verified we receieved a valid pokemon from the trainer so we'll make a copy of it now
            var pk = (PK8)startingPk.Clone();

            if (Hub.Config.Legality.ResetHOMETracker)
                pk.Tracker = 0;

            if (Hub.Config.Discord.ReturnPK8s)
                detail.SendNotification(this, pk, "Here's what you showed me!");

            // Don't know what EC is but we use it later to make sure the trainer doesn't try trading us the thing it's cloning.
            var startingEC = await Connection.ReadBytesAsync(LinkTradePartnerPokemonOffset, 4, Config.ConnectionType, token).ConfigureAwait(false);

            detail.SendNotification(this, $"**Cloned your {(Species)pk.Species}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
            LogUtil.LogInfo($"Cloned a {(Species)pk.Species}. Waiting for user to change their Pokémon...", nameof(PokeTradeBot));

            // Start trading loop 
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;
            while (ctr < detail.PoolEntry.Count && DateTime.Now - start < time)
            {
                // Routine that waits for the trainer to switch to a different pokemon from the one they are cloning
                var trainerWaitCtr = 0;
                var maxTries = 2;
                bool partnerFound = false;
                while (!partnerFound && trainerWaitCtr < maxTries)
                {
                    partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, startingEC, 15_000, 0_200, false, token).ConfigureAwait(false);
                    // Wait for our trade partner to be ready
                    if (!partnerFound)
                    {
                        if (trainerWaitCtr == 0)
                        {
                            detail.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");
                        }
                        else if (trainerWaitCtr == 2)
                        {
                            await ExitTrade(Hub.Config, false, token).ConfigureAwait(false);
                            return PokeTradeResult.TrainerTooSlow;
                        }
                        trainerWaitCtr++;
                    }
                }

                // Get the PK8 of the pokemon the trainer switched to after showing what they want cloned
                PK8? pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, token).ConfigureAwait(false);

                // Verify we got something and it's different from the one we're cloning
                if (pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(pk))
                {
                    Log("Trading partner did not change their Pokémon.");
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.TrainerTooSlow;
                }

                // Set our box slot to the pokemon they showed and ship it
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(pk, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);

                // Check if we're back in the box, if not, click A and wait again.
                var delay = 0;
                while (!await IsInBox(token).ConfigureAwait(false))
                {
                    await Click(A, 3_000, token).ConfigureAwait(false);
                    delay++;
                    if (delay >= 50)
                        break;
                    if (await IsOnOverworld(Hub.Config, token).ConfigureAwait(false)) // If we're in the overworld, the trainer prolly ditched us
                        Log("Trade partner left unexpectedly, resetting");
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.RecoverReturnOverworld;
                }
                ctr++;
            }

            // Wait for the final trade to complete
            var delay_count = 0;
            while (!await IsInBox(token).ConfigureAwait(false))
            {
                await Click(A, 3_000, token).ConfigureAwait(false);
                delay_count++;
                if (delay_count >= 50)
                    break;
                if (await IsOnOverworld(Hub.Config, token).ConfigureAwait(false)) // In case we are in a Trade Evolution/PokeDex Entry and the Trade Partner quits we land on the Overworld
                    break;
            }

            // Final Trade completed, exit the trade
            await Task.Delay(1_000 + Util.Rand.Next(0_700, 1_000), token).ConfigureAwait(false);
            Log($"Pokémon cloned {ctr} times");
            await ExitTrade(Hub.Config, false, token).ConfigureAwait(false);

            // Notify and return
            Hub.Counts.AddCompletedClones();
            detail.Notifier.SendNotification(this, detail, $"Sent {ctr} clones of your Pokémon.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PK8> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;
            var pkprev = new PK8();
            while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
            {
                var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, token).ConfigureAwait(false);
                if (pk == null || pk.Species < 1 || !pk.ChecksumValid || SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
                    continue;

                // Save the new Pokémon for comparison next round.
                pkprev = pk;

                // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
                if (DumpSetting.Dump)
                {
                    var subfolder = detail.Type.ToString().ToLower();
                    DumpPokemon(DumpSetting.DumpFolder, subfolder, pk); // received
                }

                var la = new LegalityAnalysis(pk);
                var verbose = la.Report(true);
                Log($"Shown Pokémon is {(la.Valid ? "Valid" : "Invalid")}.");

                detail.SendNotification(this, pk, verbose);
                ctr++;
            }

            Log($"Ended Dump loop after processing {ctr} Pokémon");
            await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);
            if (ctr == 0)
                return PokeTradeResult.TrainerTooSlow;

            Hub.Counts.AddCompletedDumps();
            detail.Notifier.SendNotification(this, detail, $"Dumped {ctr} Pokémon.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> ProcessGiveawayUploadAsync(PokeTradeDetail<PK8> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;
            var pkprev = new PK8();
            var poolEntry = detail.PoolEntry;

            while (ctr < 1 && DateTime.Now - start < time)
            {
                var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, token).ConfigureAwait(false);
                if (pk == null || pk.Species < 1 || !pk.ChecksumValid || SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
                    continue;

                // Save the new Pokémon for comparison next round.
                pkprev = pk;
                poolEntry.PK8 = pk;
                poolEntry.Pokemon = SpeciesName.GetSpeciesName( pk.Species, 2);

                if (Hub.Config.Legality.VerifyLegality)
                {
                    LogUtil.LogInfo($"Performing legality check on {poolEntry.Pokemon}", "PokeTradeBot.GiveawayUpload");
                    var la = new LegalityAnalysis(poolEntry.PK8);
                    var verbose = la.Report(true);
                    LogUtil.LogInfo($"Shown Pokémon is {(la.Valid ? "Valid" : "Invalid")}.", "PokeTradeBot.GiveawayUpload");
                    detail.SendNotification(this, pk, $"Pokémon sent is {(la.Valid ? "Valid" : "Invalid")}.");
                    detail.SendNotification(this, pk, verbose);
                    if (!la.Valid)
                    {
                        detail.SendNotification(this, pk, $"Show a different pokemon to continue or exit the trade to end.");
                        continue;
                    }
                }
                LogUtil.LogInfo("Creating new database entry", "PokeTradeBot.GiveawayUpload");
                Hub.GiveawayPoolDatabase.NewEntry(poolEntry);
                if (Hub.Config.Discord.ReturnPK8s)
                    detail.SendNotification(this, pk, "Here's what you showed me!");

                ctr++;
            }

            LogUtil.LogInfo($"Ended Giveaway pool upload", "PokeTradeBot.GiveawayUpload");
            await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);
            if (ctr == 0)
                return PokeTradeResult.TrainerTooSlow;

            detail.Notifier.SendNotification(this, detail, $"Finished uploading Pokémon to the Giveaway Pool.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> EndSeedCheckTradeAsync(PokeTradeDetail<PK8> detail, PK8 pk, CancellationToken token)
        {
            await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);

            detail.TradeFinished(this, pk);

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "seed", pk);

            // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
#pragma warning disable 4014
            Task.Run(() =>
            {
                try
                {
                    ReplyWithSeedCheckResults(detail, pk);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    detail.SendNotification(this, $"Unable to calculate seeds: {ex.Message}\r\n{ex.StackTrace}");
                }
            }, token);
#pragma warning restore 4014

            Hub.Counts.AddCompletedSeedCheck();

            return PokeTradeResult.Success;
        }

        private void ReplyWithSeedCheckResults(PokeTradeDetail<PK8> detail, PK8 result)
        {
            detail.SendNotification(this, "Calculating your seed(s)...");

            if (result.IsShiny)
            {
                LogUtil.LogInfo("The Pokémon is already shiny!", "PokeTradeBot"); // Do not bother checking for next shiny frame
                detail.SendNotification(this, "This Pokémon is already shiny! Raid seed calculation was not done.");

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "seed", result);

                detail.TradeFinished(this, result);
                return;
            }

            SeedChecker.CalculateAndNotify(result, detail, Hub.Config.SeedCheck, this);
            LogUtil.LogInfo("Seed calculation completed.", "PokeTradeBot");
        }

        private void WaitAtBarrierIfApplicable(CancellationToken token)
        {
            if (!ShouldWaitAtBarrier)
                return;
            var opt = Hub.Config.Distribution.SynchronizeBots;
            if (opt == BotSyncOption.NoSync)
                return;

            var timeoutAfter = Hub.Config.Distribution.SynchronizeTimeout;
            if (FailedBarrier == 1) // failed last iteration
                timeoutAfter *= 2; // try to re-sync in the event things are too slow.

            var result = Hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token);

            if (result)
            {
                FailedBarrier = 0;
                return;
            }

            FailedBarrier++;
            Log($"Barrier sync timed out after {timeoutAfter} seconds. Continuing.");
        }

        private bool IsLegal(PK8 pk, PokeTradeDetail<PK8> poke)
        {

            var la = new LegalityAnalysis(pk);
            if (!la.Valid && Hub.Config.Legality.VerifyLegality)
            {
                Log($"Clone request has detected an invalid Pokémon: {(Species)pk.Species}");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "hacked", pk);

                var report = la.Report();
                Log(report);
                poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                poke.SendNotification(this, report);
                return false;
            }
            else
            {
                return true;
            }

        }

        private async Task<bool> WaitForPokemonChanged(uint offset, int waitms, int waitInterval, CancellationToken token)
        {
            var oldEC = await Connection.ReadBytesAsync(offset, 4, Config.ConnectionType, token).ConfigureAwait(false);
            return Hub.Config.Trade.SpinTrade && Config.ConnectionType == ConnectionType.USB ? await SpinTrade(offset, oldEC, waitms, waitInterval, false, token).ConfigureAwait(false) : await ReadUntilChanged(offset, oldEC, waitms, waitInterval, false, token).ConfigureAwait(false);
        }
    }
}
