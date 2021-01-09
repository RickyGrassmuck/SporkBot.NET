using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{

    [Summary("Queues new Giveway trade")]
    public class GiveawayModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("giveawayqueue")]
        [Alias("gaq")]
        [Summary("Prints the users in the giveway queues.")]
        [RequireSudo]
        public async Task GetGiveawayListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Giveaways";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }
        [Command("giveawaypool")]
        [Alias("gap")]
        [Summary("Show a list of pokemon available for giveaway")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task DisplayGiveawayPoolCountAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;
            var pool = hub.Ledy.Pool;
            var count = pool.Count;
            if (count > 0 && count < 20)
            {
                var lines = pool.Files.Select((z, i) => $"{i + 1:00}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");
                var msg = string.Join("\n", lines);

                var embed = new EmbedBuilder();
                embed.AddField(x =>
                {
                    x.Name = $"Count: {count}";
                    x.Value = msg;
                    x.IsInline = false;
                });
                await ReplyAsync("Giveaway Pool Details", embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"Giveaway Pool Count: {count}").ConfigureAwait(false);
            }
        }

        [Command("giveaway")]
        [Alias("ga", "giveme", "gimme")]
        [Summary("Makes trade you the specified giveaway pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task GiveawayAsync([Summary("Start a Giveaway trade")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            var pk = new PK8();
            content = ReusableActions.StripCodeBlock(content);
            pk.Nickname = content;

            if (pk.Nickname.ToLower() == "random")
            {
                // Request a random giveaway prize
                pk = Info.Hub.Ledy.Pool.GetRandomSurprise();
            } 
            else
            {
                var trade = Info.Hub.Ledy.GetLedyTrade(pk, Info.Hub.Config.Distribution.LedySpecies);
                if (trade != null && trade.Receive != null)
                {
                    pk = trade.Receive;
                }
                else
                {
                    await ReplyAsync("Pokemon requests not available, us $giveaway pool for full list of available giveaways!").ConfigureAwait(false);
                    return;
                }
            }

            var sig = Context.User.GetFavor();
            await Context.AddToQueueAsync(code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Giveaway, Context.User).ConfigureAwait(false);

        }
    }
}
