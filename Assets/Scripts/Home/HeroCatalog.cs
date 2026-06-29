using System.Collections.Generic;
using UnityEngine;

namespace VisionsOfGenesis.Home
{
    // Master list of every hero that can exist in the game (starters + summonable).
    // HeroInfo (the per-account, mutable record) is built from these immutable defs.
    // A def's `name` must match a UnitData.unitName so battles can resolve it.
    public static class HeroCatalog
    {
        // Standard gacha rarity rates, by base-star tier. Within a tier the odds are
        // split evenly among that tier's heroes. They sum to 100 (3* takes the
        // remainder); tweak here to rebalance the banner.
        const float Weight5Star = 5f;
        const float Weight4Star = 15f;
        const float Weight3Star = 80f;

        public class HeroDef
        {
            public string id;
            public string name;
            public string role;
            public Color accent;
            public int portraitIndex;
            public int baseStars;   // also the summon rarity tier (3, 4 or 5)
            public bool starter;    // owned from the start
            public bool summonable; // appears in the gacha pool
        }

        static List<HeroDef> _all;

        static List<HeroDef> All()
        {
            if (_all != null) return _all;
            _all = new List<HeroDef>
            {
                new HeroDef { id = "khael",   name = "Khael",   role = "Espada",      accent = new Color(0.9098039f, 0.2901961f, 0.3529412f, 1f), portraitIndex = 0,  baseStars = 1, starter = true,  summonable = false },
                new HeroDef { id = "aria",    name = "Aria",    role = "Sanadora",    accent = new Color(0.21176471f, 0.7882353f, 0.9411765f, 1f), portraitIndex = 1,  baseStars = 1, starter = true,  summonable = false },
                new HeroDef { id = "theron",  name = "Theron",  role = "Guerrero",    accent = new Color(0.62f, 0.50f, 0.26f, 1f), portraitIndex = -1, baseStars = 3, starter = false, summonable = true },
                new HeroDef { id = "garrick", name = "Garrick", role = "Caballero",   accent = new Color(0.85f, 0.45f, 0.22f, 1f), portraitIndex = -1, baseStars = 3, starter = false, summonable = true },
                new HeroDef { id = "lyra",    name = "Lyra",    role = "Maga",        accent = new Color(0.32f, 0.52f, 0.92f, 1f), portraitIndex = -1, baseStars = 4, starter = false, summonable = true },
                new HeroDef { id = "sylphie", name = "Sylphie", role = "Sacerdotisa", accent = new Color(0.95f, 0.90f, 0.62f, 1f), portraitIndex = -1, baseStars = 5, starter = false, summonable = true },
            };
            return _all;
        }

        static float TierWeight(int stars)
        {
            switch (stars)
            {
                case 5: return Weight5Star;
                case 4: return Weight4Star;
                case 3: return Weight3Star;
                default: return 0f;
            }
        }

        public static HeroDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var d in All())
                if (string.Equals(d.id, id, System.StringComparison.OrdinalIgnoreCase)) return d;
            return null;
        }

        // A fresh, owned-state HeroInfo for the given hero id (base level/stars).
        public static HeroInfo MakeInfo(string id)
        {
            var d = Find(id);
            if (d == null) return null;
            return new HeroInfo
            {
                id = d.id,
                name = d.name,
                role = d.role,
                level = 1,
                exp = 0,
                stars = d.baseStars,
                accent = d.accent,
                portraitIndex = d.portraitIndex
            };
        }

        public static List<HeroInfo> Starters()
        {
            var list = new List<HeroInfo>();
            foreach (var d in All())
                if (d.starter) list.Add(MakeInfo(d.id));
            return list;
        }

        public static List<HeroDef> SummonPool()
        {
            var list = new List<HeroDef>();
            foreach (var d in All())
                if (d.summonable) list.Add(d);
            return list;
        }

        // Standard banner roll: weighted pick of a rarity tier, then a uniform pick
        // among the heroes of that tier.
        public static HeroDef RollSummon()
        {
            var pool = SummonPool();
            if (pool.Count == 0) return null;

            var tiers = TiersInPool(pool);

            float total = 0f;
            foreach (var t in tiers) total += TierWeight(t);
            if (total <= 0f) return pool[Random.Range(0, pool.Count)];

            float r = Random.Range(0f, total);
            int chosen = tiers[tiers.Count - 1];
            foreach (var t in tiers)
            {
                r -= TierWeight(t);
                if (r <= 0f) { chosen = t; break; }
            }

            var inTier = new List<HeroDef>();
            foreach (var d in pool) if (d.baseStars == chosen) inTier.Add(d);
            if (inTier.Count == 0) return pool[Random.Range(0, pool.Count)];
            return inTier[Random.Range(0, inTier.Count)];
        }

        // Distinct tiers present in the pool, sorted high to low.
        static List<int> TiersInPool(List<HeroDef> pool)
        {
            var tiers = new List<int>();
            foreach (var d in pool) if (!tiers.Contains(d.baseStars)) tiers.Add(d.baseStars);
            tiers.Sort((a, b) => b.CompareTo(a));
            return tiers;
        }

        public static string RatesText()
        {
            var pool = SummonPool();
            var tiers = TiersInPool(pool);

            float total = 0f;
            foreach (var t in tiers) total += TierWeight(t);

            var sb = new System.Text.StringBuilder("Probabilidades:\n");
            foreach (var t in tiers)
            {
                int count = 0;
                foreach (var d in pool) if (d.baseStars == t) count++;
                if (count == 0) continue;

                float tierPct = total > 0f ? TierWeight(t) / total * 100f : 0f;
                float perHero = tierPct / count;

                string stars = "";
                for (int i = 0; i < t; i++) stars += "★";
                sb.Append(stars).Append("  ").Append(tierPct.ToString("0.#")).Append("%\n");

                foreach (var d in pool)
                    if (d.baseStars == t)
                        sb.Append("   ").Append(d.name).Append("  ").Append(perHero.ToString("0.#")).Append("%\n");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
