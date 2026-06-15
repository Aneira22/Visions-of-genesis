using UnityEngine;

namespace VisionsOfGenesis.Data
{
    public enum Element
    {
        Neutral,
        Fire,
        Water,
        Wind,
        Earth,
        Light,
        Dark
    }

    [CreateAssetMenu(fileName = "NewUnit", menuName = "Visions of Genesis/Unit Data", order = 0)]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string unitName = "Unnamed";
        public Sprite portrait;
        [Tooltip("World sprite used when this unit appears in battle (mainly enemies). Falls back to portrait if empty.")]
        public Sprite battleSprite;
        public Element element = Element.Neutral;

        [Header("Base Stats")]
        public int maxHP = 100;
        public int maxMP = 30;
        public int attack = 20;
        public int defense = 10;

        [Header("Loadout (only used by player units)")]
        public SkillData[] skills;
        public ItemData[] items;
    }
}
