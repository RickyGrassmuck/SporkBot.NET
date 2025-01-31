﻿using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Clone trades")]
    public class CloneModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync(int code)
        {
            var sig = Context.User.GetFavor();
            await Context.AddToQueueAsync(code, Context.User.Username, sig, new PK8(), PokeRoutineType.Clone, PokeTradeType.Clone).ConfigureAwait(false);
        }

        [Command("multiclone")]
        [Alias("mc")]
        [Summary("Clones the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync([Summary("Clone Count")][Remainder] string cloneCount)
        {
            int count = Util.ToInt32(cloneCount);
            var entry = new GiveawayPoolEntry
            {
                Count = count
            };
            var sig = Context.User.GetFavor();
            var code = 88891234;
            await Context.AddToQueueAsync(code, Context.User.Username, sig, new PK8(), PokeRoutineType.Clone, PokeTradeType.Clone, Context.User, entry).ConfigureAwait(false);
        }

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync()
        {
            var code = Info.GetRandomTradeCode();
            await CloneAsync(code).ConfigureAwait(false);
        }

        [Command("cloneList")]
        [Alias("cl", "cq")]
        [Summary("Prints the users in the Clone queue.")]
        [RequireSudo]
        public async Task GetListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.Clone);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }
    }
}
