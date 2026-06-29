using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Home
{
    public static class Gacha
    {
        public const int SingleCost = 250;

        // Duplicate compensation (FFBE-style: a dupe is not wasted, it feeds upgrades).
        public const int DupeCrystalRefund = 25;
        public const string DupeMaterialName = "Esencia Corrupta";
        public const int DupeMaterialAmount = 2;

        public class SummonResult
        {
            public string error;          // non-null => nothing happened
            public HeroInfo hero;
            public bool isNew;
            public int crystalRefund;
            public MaterialData dupeMaterial;
            public int dupeAmount;
        }

        public static SummonResult SummonSingle()
        {
            if (Wallet.Crystals < SingleCost)
                return new SummonResult { error = "Cristales insuficientes" };

            var def = HeroCatalog.RollSummon();
            if (def == null)
                return new SummonResult { error = "No hay Visiones disponibles" };

            // Only spend once we know the roll is valid.
            Wallet.SpendCrystals(SingleCost);

            var result = new SummonResult();
            HeroInfo existing = TeamState.Owned != null ? TeamState.Owned.Find(h => h != null && h.id == def.id) : null;

            if (existing != null)
            {
                result.isNew = false;
                result.hero = existing;

                Wallet.AddCrystals(DupeCrystalRefund);
                result.crystalRefund = DupeCrystalRefund;

                var mat = GameCatalog.Instance != null ? GameCatalog.Instance.FindMaterial(DupeMaterialName) : null;
                if (mat != null)
                {
                    Inventory.AddMaterial(mat, DupeMaterialAmount);
                    result.dupeMaterial = mat;
                    result.dupeAmount = DupeMaterialAmount;
                }
            }
            else
            {
                var info = HeroCatalog.MakeInfo(def.id);
                TeamState.Owned.Add(info);
                result.isNew = true;
                result.hero = info;
            }

            SaveSystem.Save();
            return result;
        }
    }
}
