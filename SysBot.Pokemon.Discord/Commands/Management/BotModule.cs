﻿using Discord;
using Discord.Commands;
using SysBot.Base;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class BotModule : ModuleBase<SocketCommandContext>
    {
        [Command("botStatus")]
        [Summary("Gets the status of the bots.")]
        [RequireSudo]
        public async Task GetStatusAsync()
        {
            var me = SysCordInstance.Runner;
            var bots = me.Bots.Select(z => z.Bot).OfType<PokeRoutineExecutor>().ToArray();
            if (bots.Length == 0)
            {
                await ReplyAsync("No bots configured.").ConfigureAwait(false);
                return;
            }

            var summaries = bots.Select(GetDetailedSummary);
            var lines = string.Join(Environment.NewLine, summaries);
            await ReplyAsync(Format.Code(lines)).ConfigureAwait(false);
        }

        private static string GetDetailedSummary(PokeRoutineExecutor z)
        {
            return $"- {(z.Config.ConnectionType == ConnectionType.USB ? "USB" + z.Config.UsbPortIndex : z.Connection.IP)} | {z.Connection.Name} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
        }

        [Command("botStart")]
        [Summary("Starts a bot by IP address.")]
        [RequireSudo]
        public async Task StartBotAsync(string ip)
        {
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"{(bot?.Bot.Config.ConnectionType == ConnectionType.WiFi ? $"No bot has that IP address ({ip})." : $"No bot has that USB port index ({ip}).")}").ConfigureAwait(false);
                return;
            }

            bot.Start();
            await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Name}) has been commanded to start.").ConfigureAwait(false);
        }

        [Command("botStop")]
        [Summary("Stops a bot by IP address.")]
        [RequireSudo]
        public async Task StopBotAsync(string ip)
        {
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"{(bot?.Bot.Config.ConnectionType == ConnectionType.WiFi ? $"No bot has that IP address ({ip})." : $"No bot has that USB port index ({ip}).")}").ConfigureAwait(false);
                return;
            }

            bot.Stop();
            await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Name}) has been commanded to stop.").ConfigureAwait(false);
        }

        [Command("botIdle")]
        [Alias("botPause")]
        [Summary("Commands a bot to Idle by IP address.")]
        [RequireSudo]
        public async Task IdleBotAsync(string ip)
        {
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"{(bot?.Bot.Config.ConnectionType == ConnectionType.WiFi ? $"No bot has that IP address ({ip})." : $"No bot has that USB port index ({ip}).")}").ConfigureAwait(false);
                return;
            }

            bot.Pause();
            await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Name}) has been commanded to idle.").ConfigureAwait(false);
        }

        [Command("botChange")]
        [Summary("Changes the routine of a bot (trades).")]
        [RequireSudo]
        public async Task ChangeTaskAsync(string ip, [Summary("Routine enum name")] PokeRoutineType task)
        {
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"{(bot?.Bot.Config.ConnectionType == ConnectionType.WiFi ? $"No bot has that IP address ({ip})." : $"No bot has that USB port index ({ip}).")}").ConfigureAwait(false);
                return;
            }

            bot.Bot.Config.Initialize(task, bot.Bot.Config.ConnectionType);
            await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Name}) has been commanded to do {task} as its next task.").ConfigureAwait(false);
        }

        [Command("botRestart")]
        [Summary("Restarts the bot(s) by IP address(es), separated by commas.")]
        [RequireSudo]
        public async Task RestartBotAsync(string ipAddressesCommaSeparated)
        {
            var ips = ipAddressesCommaSeparated.Split(',');
            foreach (var ip in ips)
            {
                var bot = SysCordInstance.Runner.GetBot(ip);
                if (bot?.Bot.Config.ConnectionType == ConnectionType.USB)
                {
                    await ReplyAsync($"This bot could not be restarted because it's running in USB mode.").ConfigureAwait(false);
                    return;
                }

                if (bot == null)
                {
                    await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                    return;
                }

                bot.Bot.Connection.Reset(ip);
                bot.Start();
                await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Name}) has been commanded to start.").ConfigureAwait(false);
            }
        }
    }
}
