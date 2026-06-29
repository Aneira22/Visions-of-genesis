using UnityEngine;

namespace VisionsOfGenesis.Data
{
    // Crafting/upgrade material. Hero upgrades are not implemented yet -
    // this only defines identity so materials can be owned and displayed
    // in the inventory ahead of that system.
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "Visions of Genesis/Material Data", order = 3)]
    public class MaterialData : ScriptableObject
    {
        [Header("Identity")]
        public string materialName = "New Material";
        [TextArea] public string description;
        public Sprite icon;

        [Header("Future use")]
        [Tooltip("Not consumed by any system yet. Intended for a future hero-upgrade feature (e.g. 'Awakening', 'Limit Break').")]
        public string category = "General";
    }
}
