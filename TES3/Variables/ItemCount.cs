using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Variables
{
    internal class ItemCount
    {
        public int Count = 0;
        public double NormalizedCount = 0;
        public double Chance = 1;

        public ItemCount()
        {
        }

        public ItemCount(int count) : this()
        {
            this.Count = Math.Abs(count);
            this.NormalizedCount = Math.Abs(count);
        }

        public ItemCount( int count, double normalizedCount) : this()
        {
            this.Count = Math.Abs(count);
            this.NormalizedCount = Math.Abs(normalizedCount);
        }

        public ItemCount(int count, double normalizedCount, double chance) : this()
        {
            this.Count = Math.Abs(count);
            this.NormalizedCount = Math.Abs(normalizedCount);
            this.Chance = chance;
        }


        public static ItemCount operator +(ItemCount a, ItemCount b)
        {
            return new(a.Count + b.Count, a.NormalizedCount + b.NormalizedCount, Math.Max(a.Chance, b.Chance));
        }

        public static ItemCount operator -(ItemCount a, ItemCount b)
        {
            return new(a.Count - b.Count, a.NormalizedCount - b.NormalizedCount, a.Chance);
        }
    }
}
