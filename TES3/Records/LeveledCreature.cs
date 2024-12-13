using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    class LeveledCreature : Record
    {
        public readonly string Type = RecordType.LeveledCreature;
        public readonly string Id = "";
        public readonly HashSet<string> Creatures = new(StringComparer.OrdinalIgnoreCase);

        public bool IsDeleted { get; private set; } = false;


        public LeveledCreature(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != RecordType.LeveledCreature)
                throw new Exception("not a leveled creature record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            this.IsDeleted = this.RecordInfo.Deleted;

            if (this.RecordInfo.Data is null) throw new Exception("leveled creature record data is null");

            CustomLogger.WriteLine(LogLevel.Info, $"leveled creature record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();

                    CustomLogger.WriteLine(LogLevel.Misc, $"field {field} length {length}");

                    switch (field)
                    {
                        case "NAME":
                            {
                                Id = reader.ReadNullTerminatedString(length);
                                CustomLogger.WriteLine(LogLevel.Info, $"ID {Id}");
                                break;
                            }
                        case "CNAM":
                            {
                                var creaId = reader.ReadNullTerminatedString(length);
                                this.Creatures.Add(creaId);
                                break;
                            }
                        case "DELE":
                            {
                                reader.Position += length;
                                IsDeleted = true;
                                break;
                            }
                        default:
                            {
                                reader.Position += length;
                                break;
                            }
                    }
                }
            }
        }

        public void Merge(LeveledItem newRecord)
        {
            this.IsDeleted |= newRecord.IsDeleted;
            this.Creatures.Clear();
            foreach (var item in newRecord.CarriedItems)
            {
                this.Creatures.Add(item);
            }
        }
    }
}
