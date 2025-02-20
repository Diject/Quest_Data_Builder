using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Handlers
{
    internal class LeveledItemHandler : Dictionary<string, LeveledItem>
    {


        public LeveledItemHandler(IEqualityComparer<string>? comparer) : base(comparer)
        {
        }

        public new bool TryAdd(string id, LeveledItem record)
        {
            if (base.TryAdd(id, record))
            {
                record.SetHandler(this);
                return true;
            }
            return false;
        }

        public new void Add(string id, LeveledItem record)
        {
            base.Add(id, record);
            record.SetHandler(this);
        }
    }
}
