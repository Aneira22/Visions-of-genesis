using System.Collections.Generic;

namespace VisionsOfGenesis.Home
{
    public static class HeroLeveling
    {
        public const int MaxLevel = 20;

        public static int ExpToNext(int level)
        {
            return 100 + (level - 1) * 25;
        }

        // Returns how many levels the hero gained.
        public static int AddExp(HeroInfo hero, int amount)
        {
            if (hero == null || amount <= 0 || hero.level >= MaxLevel) return 0;

            int gained = 0;
            hero.exp += amount;
            while (hero.level < MaxLevel && hero.exp >= ExpToNext(hero.level))
            {
                hero.exp -= ExpToNext(hero.level);
                hero.level++;
                gained++;
            }
            if (hero.level >= MaxLevel) hero.exp = 0;
            return gained;
        }
    }

    public static class Wallet
    {
        public static int Gold = 500;      // shop currency
        public static int Crystals = 1200; // gacha currency

        public static void AddGold(int amount)
        {
            if (amount > 0) Gold += amount;
        }

        public static void AddCrystals(int amount)
        {
            if (amount > 0) Crystals += amount;
        }

        public static bool SpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public static bool SpendCrystals(int amount)
        {
            if (amount <= 0 || Crystals < amount) return false;
            Crystals -= amount;
            return true;
        }
    }

    public static class PlayerProgress
    {
        public const int MaxRank = 50;
        public const int RegenSeconds = 300; // 1 energy every 5 minutes

        public static int Rank = 1;
        public static int Exp;

        // Energy can exceed MaxEnergy after a rank-up refill; it does not
        // regenerate until it drops below MaxEnergy again.
        public static int Energy = 20;
        public static int MaxEnergy = 20;

        private static System.DateTime _regenAnchor = System.DateTime.UtcNow;

        private static readonly HashSet<string> _completed = new HashSet<string>();

        // SaveSystem access
        public static long RegenAnchorTicks => _regenAnchor.Ticks;
        public static IEnumerable<string> CompletedEntries => _completed;

        public static void LoadRegenAnchor(long ticks)
        {
            _regenAnchor = ticks > 0
                ? new System.DateTime(ticks, System.DateTimeKind.Utc)
                : System.DateTime.UtcNow;
        }

        public static void LoadCompleted(IEnumerable<string> entries)
        {
            _completed.Clear();
            if (entries != null) foreach (var e in entries) _completed.Add(e);
        }

        public static void TickRegen()
        {
            var now = System.DateTime.UtcNow;
            if (Energy >= MaxEnergy)
            {
                _regenAnchor = now;
                return;
            }

            double elapsed = (now - _regenAnchor).TotalSeconds;
            int ticks = (int)(elapsed / RegenSeconds);
            if (ticks <= 0) return;

            int add = System.Math.Min(ticks, MaxEnergy - Energy);
            Energy += add;
            _regenAnchor = Energy >= MaxEnergy
                ? now
                : _regenAnchor.AddSeconds((double)ticks * RegenSeconds);
        }

        public static bool SpendEnergy(int amount)
        {
            if (amount <= 0) return true;
            TickRegen();
            if (Energy < amount) return false;
            Energy -= amount;
            return true;
        }

        // Seconds until the next regen point, or -1 when not regenerating.
        public static int SecondsToNextRegen()
        {
            if (Energy >= MaxEnergy) return -1;
            double elapsed = (System.DateTime.UtcNow - _regenAnchor).TotalSeconds;
            double remaining = RegenSeconds - (elapsed % RegenSeconds);
            return (int)System.Math.Ceiling(remaining);
        }

        public static int ExpToNext(int rank)
        {
            return 100 + (rank - 1) * 50;
        }

        public static bool IsCompleted(string entryId)
        {
            return !string.IsNullOrEmpty(entryId) && _completed.Contains(entryId);
        }

        // Returns true only the first time the entry is completed.
        public static bool MarkCompleted(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return false;
            return _completed.Add(entryId);
        }

        // Returns how many ranks were gained.
        public static int AddExp(int amount)
        {
            if (amount <= 0 || Rank >= MaxRank) return 0;

            int gained = 0;
            Exp += amount;
            while (Rank < MaxRank && Exp >= ExpToNext(Rank))
            {
                Exp -= ExpToNext(Rank);
                Rank++;
                gained++;

                // Rank-up: +1 max energy, then a full refill of the new max
                // stacked on top of whatever was left (can exceed the max).
                MaxEnergy += 1;
                Energy += MaxEnergy;
            }
            if (Rank >= MaxRank) Exp = 0;
            return gained;
        }
    }
}
