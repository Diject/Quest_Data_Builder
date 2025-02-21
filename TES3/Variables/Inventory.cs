﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Variables
{
    internal class Inventory : Dictionary<string, ItemCount>
    {

        public Inventory() : base(StringComparer.OrdinalIgnoreCase)
        {

        }


        public void Add(string id, int count)
        {
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
            if (!base.TryAdd(id, new(count, normalizedCount)))
            {
                this[id].Count += count;
                this[id].NormalizedCount += normalizedCount;
            }
        }

        public void Add(string id, double normalizedCount)
        {
            if (!base.TryAdd(id, new((int)Math.Ceiling(normalizedCount), normalizedCount)))
            {
                this[id].Count += (int)Math.Ceiling(normalizedCount);
                this[id].NormalizedCount += normalizedCount;
            }
        }

        public bool TryAdd(string id, double normalizedCount)
        {
            this.Add(id, normalizedCount);
            return true;
        }
    }
}
