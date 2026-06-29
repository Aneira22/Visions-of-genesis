using System.Collections.Generic;
using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Home
{
    // Shared, account-wide stock of consumable items and upgrade materials.
    // Items are granted by adventure rewards (see BattleBootstrap) and, in
    // the future, by the shop. Materials have no source yet - the dictionary
    // exists so the "Items" panel and a future hero-upgrade system have
    // somewhere to read/write from.
    public static class Inventory
    {
        public static readonly Dictionary<ItemData, int> Items = new Dictionary<ItemData, int>();
        public static readonly Dictionary<MaterialData, int> Materials = new Dictionary<MaterialData, int>();

        public static bool Initialized;

        public static void EnsureInit()
        {
            if (Initialized) return;
            Initialized = true;
            // Starts empty on purpose: items are meant to be earned, not handed out.
        }

        public static int GetItemCount(ItemData item)
        {
            if (item == null) return 0;
            return Items.TryGetValue(item, out int n) ? n : 0;
        }

        public static int GetMaterialCount(MaterialData material)
        {
            if (material == null) return 0;
            return Materials.TryGetValue(material, out int n) ? n : 0;
        }

        public static void AddItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return;
            Items[item] = GetItemCount(item) + amount;
        }

        public static void AddMaterial(MaterialData material, int amount)
        {
            if (material == null || amount <= 0) return;
            Materials[material] = GetMaterialCount(material) + amount;
        }

        // Consumes `amount` units of the item if enough are owned. Returns false (no change) otherwise.
        public static bool SpendItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            int have = GetItemCount(item);
            if (have < amount) return false;
            Items[item] = have - amount;
            return true;
        }

        // Consumes `amount` units of the material if enough are owned. Returns false (no change) otherwise.
        public static bool SpendMaterial(MaterialData material, int amount = 1)
        {
            if (material == null || amount <= 0) return false;
            int have = GetMaterialCount(material);
            if (have < amount) return false;
            Materials[material] = have - amount;
            return true;
        }
    }
}
