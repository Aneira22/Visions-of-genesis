using System.Collections.Generic;
using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Home
{
    // Star (awakening) upgrades: spend materials + gold to raise a hero's stars.
    // Stars boost battle stats via UnitComponent (see ApplyLevelScaling).
    public static class StarUpgrade
    {
        public const int MaxStars = 5;

        public class Req
        {
            public MaterialData mat;
            public string matName;
            public int need;
            public int have;
            public bool ok;
        }

        // Material name + amount required to go FROM `stars` to `stars + 1`.
        static List<KeyValuePair<string, int>> MatCost(int stars)
        {
            var list = new List<KeyValuePair<string, int>>();
            switch (stars)
            {
                case 1:
                    list.Add(new KeyValuePair<string, int>("Fragmento Etereo", 5));
                    break;
                case 2:
                    list.Add(new KeyValuePair<string, int>("Fragmento Etereo", 10));
                    list.Add(new KeyValuePair<string, int>("Nucleo Sombrio", 3));
                    break;
                case 3:
                    list.Add(new KeyValuePair<string, int>("Nucleo Sombrio", 5));
                    list.Add(new KeyValuePair<string, int>("Esencia Corrupta", 2));
                    break;
                case 4:
                    list.Add(new KeyValuePair<string, int>("Esencia Corrupta", 5));
                    list.Add(new KeyValuePair<string, int>("Nucleo Sombrio", 3));
                    break;
            }
            return list;
        }

        public static int GoldCost(int stars)
        {
            switch (stars)
            {
                case 1: return 1000;
                case 2: return 3000;
                case 3: return 8000;
                case 4: return 20000;
                default: return 0;
            }
        }

        public static List<Req> Requirements(int stars)
        {
            var list = new List<Req>();
            var catalog = GameCatalog.Instance;
            foreach (var kvp in MatCost(stars))
            {
                var mat = catalog != null ? catalog.FindMaterial(kvp.Key) : null;
                int have = mat != null ? Inventory.GetMaterialCount(mat) : 0;
                list.Add(new Req
                {
                    mat = mat,
                    matName = kvp.Key,
                    need = kvp.Value,
                    have = have,
                    ok = have >= kvp.Value
                });
            }
            return list;
        }

        public static bool CanUpgrade(HeroInfo hero)
        {
            if (hero == null || hero.stars >= MaxStars) return false;
            if (Wallet.Gold < GoldCost(hero.stars)) return false;
            foreach (var r in Requirements(hero.stars))
                if (r.mat == null || !r.ok) return false;
            return true;
        }

        public static bool DoUpgrade(HeroInfo hero)
        {
            if (!CanUpgrade(hero)) return false;

            foreach (var r in Requirements(hero.stars))
                Inventory.SpendMaterial(r.mat, r.need);
            Wallet.SpendGold(GoldCost(hero.stars));

            hero.stars++;
            SaveSystem.Save();
            return true;
        }
    }
}
