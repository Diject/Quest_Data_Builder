using System.Collections.Concurrent;

namespace Quest_Data_Builder.TES3.Variables
{
    internal class Inventory : ConcurrentDictionary<string, ItemCount>
    {

        public Inventory() : base(StringComparer.OrdinalIgnoreCase)
        {

        }


        public void Add(string id, int count)
        {
            lock (this)
                if (!base.TryAdd(id, new(count)))
                {
                    this[id].Count += count;
                    this[id].NormalizedCount += count;
                }
        }

        public bool TryAdd(string id, int count)
        {
            this.Add(id, count);
            return true;
        }

        public void Add(string id, int count, double normalizedCount)
        {
            lock (this)
                if (!base.TryAdd(id, new(count, normalizedCount)))
                {
                    this[id].Count += count;
                    this[id].NormalizedCount += normalizedCount;
                }
        }

        public void Add(string id, double normalizedCount)
        {
            lock (this)
                if (!base.TryAdd(id, new((int)Math.Ceiling(normalizedCount), normalizedCount)))
                {
                    this[id].Count += (int)Math.Ceiling(normalizedCount);
                    this[id].NormalizedCount += normalizedCount;
                }
        }

        public void Add(string id, double normalizedCount, double chance)
        {
            lock (this)
                if (!base.TryAdd(id, new((int)Math.Ceiling(normalizedCount), normalizedCount, chance)))
                {
                    this[id].Count += (int)Math.Ceiling(normalizedCount);
                    this[id].NormalizedCount += normalizedCount;
                    this[id].Chance = Math.Max(chance, this[id].Chance);
                }
        }

        public bool TryAdd(string id, double normalizedCount)
        {
            this.Add(id, normalizedCount);
            return true;
        }
    }
}
