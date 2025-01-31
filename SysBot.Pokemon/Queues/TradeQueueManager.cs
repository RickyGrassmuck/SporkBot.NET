﻿using PKHeX.Core;
using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub;

        private readonly PokeTradeQueue<T> Trade = new(PokeTradeType.Specific);
        private readonly PokeTradeQueue<T> Giveaway = new(PokeTradeType.Giveaway);
        private readonly PokeTradeQueue<T> GiveawayUpload = new(PokeTradeType.GiveawayUpload);
        private readonly PokeTradeQueue<T> Seed = new(PokeTradeType.Seed);
        private readonly PokeTradeQueue<T> Clone = new(PokeTradeType.Clone);
        private readonly PokeTradeQueue<T> FixOT = new(PokeTradeType.FixOT);
        private readonly PokeTradeQueue<T> Dump = new(PokeTradeType.Dump);
        public readonly TradeQueueInfo<T> Info;
        public readonly PokeTradeQueue<T>[] AllQueues;

        public TradeQueueManager(PokeTradeHub<T> hub)
        {
            Hub = hub;
            Info = new TradeQueueInfo<T>(hub);
            AllQueues = new[] { Seed, Dump, Clone, FixOT, Trade, Giveaway, GiveawayUpload };

            foreach (var q in AllQueues)
                q.Queue.Settings = hub.Config.Favoritism;
        }

        public PokeTradeQueue<T> GetQueue(PokeRoutineType type)
        {
            return type switch
            {
                PokeRoutineType.Clone => Clone,
                PokeRoutineType.FixOT => FixOT,
                PokeRoutineType.Dump => Dump,
                _ => Trade,
            };
        }

        public void ClearAll()
        {
            foreach (var q in AllQueues)
                q.Clear();
        }

        public bool TryDequeue(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
        {
            if (type == PokeRoutineType.FlexTrade)
                return GetFlexDequeue(out detail, out priority);

            return TryDequeueInternal(type, out detail, out priority);
        }

        private bool TryDequeueInternal(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
        {
            var queue = GetQueue(type);
            return queue.TryDequeue(out detail, out priority);
        }

        private bool GetFlexDequeue(out PokeTradeDetail<T> detail, out uint priority)
        {
            var cfg = Hub.Config.Queues;
            if (cfg.FlexMode == FlexYieldMode.LessCheatyFirst)
                return GetFlexDequeueOld(out detail, out priority);
            return GetFlexDequeueWeighted(cfg, out detail, out priority);
        }

        private bool GetFlexDequeueWeighted(QueueSettings cfg, out PokeTradeDetail<T> detail, out uint priority)
        {
            PokeTradeQueue<T>? preferredQueue = null;
            long bestWeight = 0; // prefer higher weights
            uint bestPriority = uint.MaxValue; // prefer smaller
            foreach (var q in AllQueues)
            {
                var peek = q.TryPeek(out detail, out priority);
                if (!peek)
                    continue;

                // priority queue is a min-queue, so prefer smaller priorities
                if (priority > bestPriority)
                    continue;

                var count = q.Count;
                var time = detail.Time;
                var weight = cfg.GetWeight(count, time, q.Type);

                if (priority >= bestPriority && weight <= bestWeight)
                    continue; // not good enough to be preferred over the other.

                // this queue has the most preferable priority/weight so far!
                bestWeight = weight;
                bestPriority = priority;
                preferredQueue = q;
            }

            if (preferredQueue == null)
            {
                detail = default!;
                priority = default;
                return false;
            }

            return preferredQueue.TryDequeue(out detail, out priority);
        }

        private bool GetFlexDequeueOld(out PokeTradeDetail<T> detail, out uint priority)
        {
            if (TryDequeueInternal(PokeRoutineType.SeedCheck, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.Clone, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.FixOT, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.Dump, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.LinkTrade, out detail, out priority))
                return true;
            return false;
        }

        public void Enqueue(PokeRoutineType type, PokeTradeDetail<T> detail, uint priority)
        {
            var queue = GetQueue(type);
            queue.Enqueue(detail, priority);
        }

        // hook in here if you want to forward the message elsewhere???
        public readonly List<Action<PokeTradeBot, PokeTradeDetail<T>>> Forwarders = new();

        public void StartTrade(PokeTradeBot b, PokeTradeDetail<T> detail)
        {
            foreach (var f in Forwarders)
                f.Invoke(b, detail);
        }
    }
}
