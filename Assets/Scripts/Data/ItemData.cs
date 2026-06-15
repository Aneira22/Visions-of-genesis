using UnityEngine;

namespace VisionsOfGenesis.Data
{
    public enum ItemEffect
    {
        HealHP,
        HealMP
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Visions of Genesis/Item Data", order = 2)]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemName = "New Item";
        [TextArea] public string description;

        [Header("Mechanics")]
        public ItemEffect effect = ItemEffect.HealHP;
        public int amount = 50;
    }
}
