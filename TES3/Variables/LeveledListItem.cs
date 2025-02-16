using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Variables
{
    internal class LeveledListItem
    {
        public string Id { get; set; }
        public uint Level { get; set; }

        public LeveledListItem(string Id, uint Level)
        {
            this.Id = Id;
            this.Level = Level;
        }
    }
}
