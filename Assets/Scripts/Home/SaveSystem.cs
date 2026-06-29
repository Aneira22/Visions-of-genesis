using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Home
{
    public static class SaveSystem
    {
        static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public static void Save()
        {
            try
            {
                var d = new SaveData();

                d.gold     = Wallet.Gold;
                d.crystals = Wallet.Crystals;

                d.playerRank       = PlayerProgress.Rank;
                d.playerExp        = PlayerProgress.Exp;
                d.energy           = PlayerProgress.Energy;
                d.maxEnergy        = PlayerProgress.MaxEnergy;
                d.regenAnchorTicks = PlayerProgress.RegenAnchorTicks;
                d.completedEntries = new List<string>(PlayerProgress.CompletedEntries);

                if (TeamState.Owned != null)
                    foreach (var h in TeamState.Owned)
                    {
                        if (h == null) continue;
                        d.heroes.Add(new HeroSaveEntry { id = h.id, level = h.level, exp = h.exp, stars = h.stars });
                    }

                if (TeamState.Parties != null)
                    for (int p = 0; p < TeamState.PartyCount; p++)
                        for (int s = 0; s < TeamState.PartySize; s++)
                        {
                            var hero = TeamState.Parties[p][s];
                            d.parties.Add(hero != null ? hero.id : "");
                        }
                d.currentParty = TeamState.CurrentParty;

                foreach (var kvp in Inventory.Items)
                {
                    if (kvp.Key == null || kvp.Value <= 0) continue;
                    d.items.Add(new NamedCount { name = kvp.Key.itemName, count = kvp.Value });
                }
                foreach (var kvp in Inventory.Materials)
                {
                    if (kvp.Key == null || kvp.Value <= 0) continue;
                    d.materials.Add(new NamedCount { name = kvp.Key.materialName, count = kvp.Value });
                }

                for (int i = 0; i < BattleBag.MaxSlots; i++)
                {
                    var item = BattleBag.GetSlot(i);
                    d.bagSlots.Add(item != null ? item.itemName : "");
                }

                File.WriteAllText(SavePath, JsonUtility.ToJson(d, prettyPrint: false));
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] Save failed: " + e.Message);
            }
        }

        // Loads from disk only if static state has not been initialized yet.
        public static void LoadIfNeeded()
        {
            if (TeamState.Initialized) return;
            Load();
        }

        // ------------------------------------------------------------------
        // Internal load
        // ------------------------------------------------------------------

        static void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) { LoadDefaults(); return; }
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (d == null) { LoadDefaults(); return; }
                ApplyData(d);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Load failed (" + e.Message + "), using defaults.");
                LoadDefaults();
            }
        }

        static void ApplyData(SaveData d)
        {
            var catalog = GameCatalog.Instance;

            Wallet.Gold     = d.gold;
            Wallet.Crystals = d.crystals;

            PlayerProgress.Rank       = Mathf.Max(1, d.playerRank);
            PlayerProgress.Exp        = Mathf.Max(0, d.playerExp);
            PlayerProgress.Energy     = Mathf.Max(0, d.energy);
            PlayerProgress.MaxEnergy  = Mathf.Max(1, d.maxEnergy);
            PlayerProgress.LoadRegenAnchor(d.regenAnchorTicks);
            PlayerProgress.LoadCompleted(d.completedEntries);

            // Rebuild the owned-hero list from saved data. Each saved id is resolved
            // against the full HeroCatalog, so summoned (non-starter) heroes survive.
            var heroMap = new Dictionary<string, HeroInfo>(StringComparer.OrdinalIgnoreCase);
            TeamState.Owned = new List<HeroInfo>();

            foreach (var entry in d.heroes)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || heroMap.ContainsKey(entry.id)) continue;
                var h = HeroCatalog.MakeInfo(entry.id);
                if (h == null) continue;
                h.level = Mathf.Max(1, entry.level);
                h.exp   = Mathf.Max(0, entry.exp);
                h.stars = Mathf.Clamp(entry.stars, 1, 5);
                TeamState.Owned.Add(h);
                heroMap[h.id] = h;
            }

            // Defensive: a save with no recognizable heroes falls back to the starters.
            if (TeamState.Owned.Count == 0)
            {
                TeamState.Owned = HeroRoster.BuildOwned();
                foreach (var h in TeamState.Owned) heroMap[h.id] = h;
            }

            TeamState.Parties = new HeroInfo[TeamState.PartyCount][];
            for (int p = 0; p < TeamState.PartyCount; p++)
            {
                TeamState.Parties[p] = new HeroInfo[TeamState.PartySize];
                for (int s = 0; s < TeamState.PartySize; s++)
                {
                    int idx = p * TeamState.PartySize + s;
                    if (idx >= d.parties.Count) break;
                    string id = d.parties[idx];
                    if (!string.IsNullOrEmpty(id) && heroMap.TryGetValue(id, out var h))
                        TeamState.Parties[p][s] = h;
                }
            }
            TeamState.CurrentParty  = Mathf.Clamp(d.currentParty, 0, TeamState.PartyCount - 1);
            TeamState.Initialized   = true;

            Inventory.Items.Clear();
            Inventory.Materials.Clear();
            if (catalog != null)
            {
                foreach (var nc in d.items)
                {
                    var item = catalog.FindItem(nc.name);
                    if (item != null && nc.count > 0) Inventory.Items[item] = nc.count;
                }
                foreach (var nc in d.materials)
                {
                    var mat = catalog.FindMaterial(nc.name);
                    if (mat != null && nc.count > 0) Inventory.Materials[mat] = nc.count;
                }
            }
            Inventory.Initialized = true;

            for (int i = 0; i < Mathf.Min(d.bagSlots.Count, BattleBag.MaxSlots); i++)
            {
                var name = d.bagSlots[i];
                BattleBag.SetSlot(i, (!string.IsNullOrEmpty(name) && catalog != null)
                    ? catalog.FindItem(name) : null);
            }
        }

        static void LoadDefaults()
        {
            var catalog = GameCatalog.Instance;

            Wallet.Gold     = 500;
            Wallet.Crystals = 1200;

            PlayerProgress.Rank      = 1;
            PlayerProgress.Exp       = 0;
            PlayerProgress.Energy    = 20;
            PlayerProgress.MaxEnergy = 20;
            PlayerProgress.LoadRegenAnchor(DateTime.UtcNow.Ticks);
            PlayerProgress.LoadCompleted(new List<string>());

            var owned = HeroRoster.BuildOwned();
            TeamState.Owned = owned;

            TeamState.Parties = new HeroInfo[TeamState.PartyCount][];
            for (int p = 0; p < TeamState.PartyCount; p++)
                TeamState.Parties[p] = new HeroInfo[TeamState.PartySize];

            for (int i = 0; i < TeamState.PartySize && i < owned.Count; i++)
                TeamState.Parties[0][i] = owned[i];

            TeamState.CurrentParty = 0;
            TeamState.Initialized  = true;

            Inventory.Items.Clear();
            Inventory.Materials.Clear();
            if (catalog != null)
            {
                var pocion = catalog.FindItem("Poción");
                var eter   = catalog.FindItem("Éter");
                if (pocion != null) { Inventory.Items[pocion] = 5; BattleBag.SetSlot(0, pocion); }
                if (eter   != null) { Inventory.Items[eter]   = 5; BattleBag.SetSlot(1, eter);   }
            }
            Inventory.Initialized = true;
        }
    }

    // ------------------------------------------------------------------
    // Serialisable data classes (plain C#, no MonoBehaviour)
    // ------------------------------------------------------------------

    [Serializable]
    public class SaveData
    {
        public int schemaVersion = 1;
        public int gold;
        public int crystals;
        public int playerRank;
        public int playerExp;
        public int energy;
        public int maxEnergy;
        public long regenAnchorTicks;
        public List<string>       completedEntries = new List<string>();
        public List<HeroSaveEntry> heroes          = new List<HeroSaveEntry>();
        public List<string>        parties         = new List<string>();
        public int currentParty;
        public List<NamedCount> items     = new List<NamedCount>();
        public List<NamedCount> materials = new List<NamedCount>();
        public List<string>     bagSlots  = new List<string>();
    }

    [Serializable]
    public class HeroSaveEntry
    {
        public string id;
        public int level;
        public int exp;
        public int stars = 1;
    }

    [Serializable]
    public class NamedCount
    {
        public string name;
        public int    count;
    }
}
