using System.Collections.Generic;
using UnityEngine;

namespace VisionsOfGenesis.Data
{
    [CreateAssetMenu(fileName = "GameCatalog", menuName = "Visions of Genesis/Game Catalog", order = 10)]
    public class GameCatalog : ScriptableObject
    {
        public ItemData[] items;
        public MaterialData[] materials;
        [Tooltip("UnitData for every summonable hero, so battles can resolve them by name.")]
        public UnitData[] heroes;

        private Dictionary<string, ItemData> _itemDict;
        private Dictionary<string, MaterialData> _matDict;
        private Dictionary<string, UnitData> _heroDict;

        private void BuildDicts()
        {
            if (_itemDict != null) return;
            _itemDict = new Dictionary<string, ItemData>(System.StringComparer.OrdinalIgnoreCase);
            _matDict  = new Dictionary<string, MaterialData>(System.StringComparer.OrdinalIgnoreCase);
            _heroDict = new Dictionary<string, UnitData>(System.StringComparer.OrdinalIgnoreCase);
            if (items != null)
                foreach (var i in items) if (i != null) _itemDict[i.itemName] = i;
            if (materials != null)
                foreach (var m in materials) if (m != null) _matDict[m.materialName] = m;
            if (heroes != null)
                foreach (var h in heroes) if (h != null) _heroDict[h.unitName] = h;
        }

        public ItemData FindItem(string name)
        {
            BuildDicts();
            return _itemDict.TryGetValue(name, out var v) ? v : null;
        }

        public MaterialData FindMaterial(string name)
        {
            BuildDicts();
            return _matDict.TryGetValue(name, out var v) ? v : null;
        }

        public UnitData FindHero(string name)
        {
            BuildDicts();
            return _heroDict.TryGetValue(name, out var v) ? v : null;
        }

        private static GameCatalog _instance;
        public static GameCatalog Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameCatalog>("GameCatalog");
                return _instance;
            }
        }
    }
}
