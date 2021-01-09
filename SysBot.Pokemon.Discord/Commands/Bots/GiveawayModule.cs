using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{

    [Summary("Queues new Link Code trades")]
    public class GiveawayModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;
        private readonly PokeTradeHub<PK8> Hub;

        public GiveawayModule(PokeTradeHub<PK8> hub)
        {
            Hub = hub;
        }

        [Command("giveawaylist")]
        [Alias("gal")]
        [Summary("Prints the users in the giveway queues.")]
        [RequireSudo]
        public async Task GetTradeListAsync()
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
        [Command("giveaway")]
        [Alias("ga")]
        [Summary("Makes the bot trade you the specified pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task TradeAsync([Summary("Pool Pokemon")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            var pk = new PK8();
            content = ReusableActions.StripCodeBlock(content);
            pk.Nickname = content;
            var trade = Hub.Ledy.GetLedyTrade(pk, Hub.Config.Distribution.LedySpecies);

            if (trade != null && trade.Receive != null)
            {
                pk = trade.Receive;
            } else
            {
                await ReplyAsync("Unable to find the selected pokemon!").ConfigureAwait(false);
                return;
            }
            var sig = Context.User.GetFavor();
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User).ConfigureAwait(false);
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, PK8 pk8, RequestSignificance sig, SocketUser usr)
        {
            if (!pk8.CanBeTraded() || !new TradeExtensions(Info.Hub).IsItemMule(pk8))
            {
                var msg = "Provided Pokémon content is blocked from trading!";
                await ReplyAsync($"{(!Info.Hub.Config.Trade.ItemMuleCustomMessage.Equals(string.Empty) && !Info.Hub.Config.Trade.ItemMuleSpecies.Equals(Species.None) ? Info.Hub.Config.Trade.ItemMuleCustomMessage : msg)}").ConfigureAwait(false);
                return;
            }

            var la = new LegalityAnalysis(pk8);
            if (!la.Valid && SysCordInstance.Self.Hub.Config.Legality.VerifyLegality)
            {
                await ReplyAsync("PK8 attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }

            await Context.AddToQueueAsync(code, trainerName, sig, pk8, PokeRoutineType.LinkTrade, PokeTradeType.Giveaway, usr).ConfigureAwait(false);
        }
    }
}
