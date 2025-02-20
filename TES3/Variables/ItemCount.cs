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


        public static ItemCount operator +(ItemCount a, ItemCount b)
        {
            a.Count += b.Count;
            a.NormalizedCount += b.NormalizedCount;
            return a;
        }

        public static ItemCount operator -(ItemCount a, ItemCount b)
        {
            a.Count -= b.Count;
            a.NormalizedCount -= b.NormalizedCount;
            return a;
        }
    }
}
