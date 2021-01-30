using System;
using PKHeX.Core;
using SysBot.Base;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SysBot.Pokemon
{
    public class GiveawayPool<T> : List<T> where T : PKM, new()
    {
        public readonly int ExpectedSize = new T().Data.Length;

        public readonly PokeTradeHubConfig Settings;

        public GiveawayPool(PokeTradeHubConfig settings)
        {
            Settings = settings;
        }

        public bool Randomized => Settings.Distribution.Shuffled;

        private int Counter;

        public T GetRandomPoke()
        {
            var choice = this[Counter];
            Counter = (Counter + 1) % Count;
            if (Counter == 0 && Randomized)
                Util.Shuffle(this);
            return choice;
        }

        public T GetRandomSurprise()
        {
            int ctr = 0;
            while (true)
            {
                var rand = GetRandomPoke();
                if (rand is PK8 pk8)
                    continue;

                ctr++; // if the pool has no valid matches, yield out eventually
                if (ctr > Count * 2)
                    return rand;
            }
        }

        public bool Reload()
        {
            return LoadFolder(Path.Combine(Settings.Giveaway.GiveawayFolder));
        }

        public Dictionary<string, GiveawayRequest<T>> Files = new();

        public bool LoadFolder(string path)
        {
            Clear();
            Files.Clear();
            if (!Directory.Exists(path))
                return false;

            var loadedAny = false;
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            var matchFiles = LoadUtil.GetFilesOfSize(files, ExpectedSize);

            foreach (var file in matchFiles)
            {
                var data = File.ReadAllBytes(file);
                var pkm = PKMConverter.GetPKMfromBytes(data);
                if (pkm is null)
                    continue;
                if (pkm is not T)
                    pkm = PKMConverter.ConvertToType(pkm, typeof(T), out _);
                if (pkm is not T dest)
                    continue;

                if (dest.Species == 0 || dest is not PK8 pk8)
                {
                    LogUtil.LogInfo("SKIPPED: Provided pk8 is not valid: " + dest.FileName, nameof(GiveawayPool<T>));
                    continue;
                }

                if (!dest.CanBeTraded())
                {
                    LogUtil.LogInfo("SKIPPED: Provided pk8 cannot be traded: " + dest.FileName, nameof(GiveawayPool<T>));
                    continue;
                }

                var la = new LegalityAnalysis(pk8);
                if (!la.Valid)
                {
                    var reason = la.Report();
                    LogUtil.LogInfo($"SKIPPED: Provided pk8 is not legal: {dest.FileName} -- {reason}", nameof(GiveawayPool<T>));
                    continue;
                }

                if (Settings.Legality.ResetHOMETracker)
                    pk8.Tracker = 0;

                var fn = Path.GetFileNameWithoutExtension(file);
                fn = StringsUtil.Sanitize(fn);

                // Since file names can be sanitized to the same string, only add one of them.
                if (!Files.ContainsKey(fn))
                {
                    Add(dest);
                    Files.Add(fn, new GiveawayRequest<T>(dest, fn));
                }
                else
                {
                    LogUtil.LogInfo("Provided pk8 was not added due to duplicate name: " + dest.FileName, nameof(GiveawayPool<T>));
                }
                loadedAny = true;
            }

            return loadedAny;
        }
    }
}