using PKHeX.Core;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class GiveawayDistributor<T> where T : PKM, new()
    {
        public readonly Dictionary<string, GiveawayRequest<T>> UserRequests = new();
        public readonly Dictionary<string, GiveawayRequest<T>> GiveawayItems;
        public readonly GiveawayPool<T> Pool;

        public GiveawayDistributor(GiveawayPool<T> pool)
        {
            Pool = pool;
            GiveawayItems = Pool.Files;
        }

    }
}