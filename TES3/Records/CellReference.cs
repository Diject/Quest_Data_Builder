using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    internal class CellReference
    {
        public readonly UInt32 Id;
        public string? ObjectId;
        public Vector3? Position { get; set; }
        public Vector3? Rotation { get; set; }
        public bool Deleted { get; set; } = false;

        public CellReference(UInt32 id)
        {
            Id = id;
        }

        public void Merge(CellReference newRecord)
        {
            this.ObjectId = newRecord.ObjectId;
            if (newRecord.Position is not null)
                this.Position = newRecord.Position;
            if (newRecord.Rotation is not null)
                this.Rotation = newRecord.Rotation;
            this.Deleted |= newRecord.Deleted;
        }
    }
}
