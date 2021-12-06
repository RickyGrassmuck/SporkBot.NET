using System;
using System.IO;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;
using System.Collections.Generic;

namespace SysBot.Pokemon.ConsoleApp
{
    public class Program
    {
        private static readonly string WorkingDirectory = AppContext.BaseDirectory;
        private const string ConfigPath = "config.json";

        private static void CreateNewConfig(PokeTradeHubConfig hub)
        {
            List<PokeBotConfig> botList = new();
            var type = PokeRoutineType.FlexTrade;
            var ip = "192.168.1.1";
            var usbPortIndex = "1";
            var port = 6000;
            var connectionType = ConnectionType.USB;

            var bot = SwitchBotConfig.GetConfig<PokeBotConfig>(ip, port, connectionType, usbPortIndex);

            bot.Initialize(type, connectionType);
            botList.Add(bot);
           
            ProgramConfig cfg = new()
            {
                Bots = botList.ToArray(),
                Hub = hub,
            };
            var lines = JsonConvert.SerializeObject(cfg);
            File.WriteAllText(ConfigPath, lines);
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Starting up...");
            PokeTradeBot.SeedChecker = new Z3SeedSearchHandler<PK8>();

            if (args.Length > 1)
                Console.WriteLine("This program does not support command line arguments.");

            if (!File.Exists(ConfigPath)) {
                var hub = new PokeTradeHubConfig();
                CreateNewConfig(hub);
                hub.Folder.CreateDefaults(WorkingDirectory);
                Console.WriteLine("New Config Generated. Edit the configuration and restart the bot.");
                return;
            }


            var lines = File.ReadAllText(ConfigPath);
            var prog = JsonConvert.DeserializeObject<ProgramConfig>(lines);
            var env = new PokeBotRunnerImpl(prog.Hub);
            foreach (var bot in prog.Bots)
            {
                bot.Initialize();
                if (!AddBot(env, bot))
                    Console.WriteLine($"Failed to add bot: {bot.IP}");
            }

            LogUtil.Forwarders.Add((msg, ident) => Console.WriteLine($"{ident}: {msg}"));
            env.StartAll();
            Console.WriteLine("Started all bots.");
            Console.WriteLine("Press any key to stop execution and quit.");
            Console.CancelKeyPress += delegate {
                env.StopAll();
            };

            while (true) { }

        }

        private static bool AddBot(PokeBotRunner env, PokeBotConfig cfg)
        {
            if (!cfg.IsValidIP() && cfg.ConnectionType == ConnectionType.WiFi)
            {
                Console.WriteLine($"{cfg.IP}'s config is not valid.");
                return false;
            }
            else if (!cfg.IsValidUSBIndex() && cfg.ConnectionType == ConnectionType.USB)
            {
                Console.WriteLine($"{cfg.UsbPortIndex}'s config is not valid.");
                return false;
            }

            var newbot = env.CreateBotFromConfig(cfg);
            try
            {
                env.Add(newbot);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            Console.WriteLine($"Added: {cfg.IP}: {cfg.InitialRoutine}");
            return true;
        }
  
    }

}
