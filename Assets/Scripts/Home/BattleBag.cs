using System.Collections.Generic;
using VisionsOfGenesis.Data;

namespace VisionsOfGenesis.Home
{
    public static class BattleBag
    {
        public const int MaxSlots = 6;
        private static readonly ItemData[] _slots = new ItemData[MaxSlots];

        public static ItemData GetSlot(int i) => (i >= 0 && i < MaxSlots) ? _slots[i] : null;

        public static void SetSlot(int i, ItemData item)
        {
            if (i >= 0 && i < MaxSlots) _slots[i] = item;
        }

        public static void ClearSlot(int i)
        {
            if (i >= 0 && i < MaxSlots) _slots[i] = null;
        }

        // Unique items from bag slots that have inventory stock, in slot order.
        public static List<ItemData> GetUsable()
        {
            var result = new List<ItemData>();
            for (int i = 0; i < MaxSlots; i++)
            {
                var item = _slots[i];
                if (item == null) continue;
                if (Inventory.GetItemCount(item) <= 0) continue;
                if (!result.Contains(item)) result.Add(item);
            }
            return result;
        }
    }
}
