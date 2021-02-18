using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SysBot.Pokemon
{
    public class BotCompleteCounts
    {
        private readonly CountSettings Config;

        private int CompletedTrades;
        private int CompletedGiveaways;
        private int CompletedSeedChecks;
        private int CompletedDistribution;
        private int CompletedClones;
        private int CompletedFixOTs;
        private int CompletedDumps;

        public BotCompleteCounts(CountSettings config)
        {
            Config = config;
            LoadCountsFromConfig();
        }

        public void LoadCountsFromConfig()
        {
            CompletedTrades = Config.CompletedTrades;
            CompletedGiveaways = Config.CompletedGiveaways;
           
            CompletedSeedChecks = Config.CompletedSeedChecks;
            CompletedDistribution = Config.CompletedDistribution;
            CompletedClones = Config.CompletedClones;
            CompletedFixOTs = Config.CompletedFixOTs;
            CompletedDumps = Config.CompletedDumps;
        }

        public void AddCompletedTrade()
        {
            Interlocked.Increment(ref CompletedTrades);
            Config.CompletedTrades = CompletedTrades;
        }

        public void AddCompletedGiveaways()
        {
            Interlocked.Increment(ref CompletedGiveaways);
            Config.CompletedGiveaways = CompletedGiveaways;
        }

        public void AddCompletedSeedCheck()
        {
            Interlocked.Increment(ref CompletedSeedChecks);
            Config.CompletedSeedChecks = CompletedSeedChecks;
        }

        public void AddCompletedClones()
        {
            Interlocked.Increment(ref CompletedClones);
            Config.CompletedClones = CompletedClones;
        }

        public void AddCompletedFixOTs()
        {
            Interlocked.Increment(ref CompletedFixOTs);
            Config.CompletedFixOTs = CompletedFixOTs;
        }

        public void AddCompletedDumps()
        {
            Interlocked.Increment(ref CompletedDumps);
            Config.CompletedDumps = CompletedDumps;
        }

        public IEnumerable<string> Summary()
        {
            if (CompletedSeedChecks != 0)
                yield return $"Seed Check Trades: {CompletedSeedChecks}";
            if (CompletedClones != 0)
                yield return $"Clone Trades: {CompletedClones}";
            if (CompletedFixOTs != 0)
                yield return $"FixOT Trades: {CompletedFixOTs}";
            if (CompletedDumps != 0)
                yield return $"Dump Trades: {CompletedDumps}";
            if (CompletedTrades != 0)
                yield return $"Link Trades: {CompletedTrades}";
            if (CompletedGiveaways != 0)
                yield return $"Giveaways: {CompletedGiveaways}";
            if (CompletedDistribution != 0)
                yield return $"Distribution Trades: {CompletedDistribution}";
        }
    }
}