﻿using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class Z3SeedSearchHandler<T> : ISeedSearchHandler<T> where T : PKM, new()
    {
        private static int[] GetBlankIVTemplate() => new[] { -1, -1, -1, -1, -1, -1 };

        public void CalculateAndNotify(T pkm, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeTradeBot bot)
        {
            var ec = pkm.EncryptionConstant;
            var pid = pkm.PID;
            PKX.ReorderSpeedLast((int[])pkm.IVs);
            var IVs = pkm.IVs.Length == 0 ? GetBlankIVTemplate() : (int[])pkm.IVs;
            if (settings.ShowAllZ3Results)
            {
                var matches = Z3Search.GetAllSeeds(ec, pid, IVs, settings.ResultDisplayMode);
                foreach (var match in matches)
                {
                    var lump = new PokeTradeSummary("Calculated Seed:", match);
                    detail.SendNotification(bot, lump);
                }
            }
            else
            {
                var match = Z3Search.GetFirstSeed(ec, pid, IVs, settings.ResultDisplayMode);
                var lump = new PokeTradeSummary("Calculated Seed:", match);
                detail.SendNotification(bot, lump);
            }
        }
    }
}