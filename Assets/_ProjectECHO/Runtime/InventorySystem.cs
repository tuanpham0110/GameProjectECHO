using System.Collections.Generic;

namespace ProjectEcho
{
    public sealed class InventorySystem
    {
        private readonly Dictionary<SupportItem, int> items = new();
        public int Count(SupportItem item) => items.TryGetValue(item, out int amount) ? amount : 0;
        public void Add(SupportItem item) => items[item] = Count(item) + 1;
        public bool Use(SupportItem item)
        {
            int count = Count(item);
            if (count == 0) return false;
            items[item] = count - 1;
            return true;
        }
    }
}
