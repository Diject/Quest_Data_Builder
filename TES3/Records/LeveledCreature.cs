using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Variables;
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
        public readonly uint Data;
        public readonly byte ChanceNone;
        public readonly uint? Count;
        public readonly HashSet<string> Creatures = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Required PC's level by item id
        /// </summary>
        public readonly List<LeveledListItem> Items = new();

        public bool IsDeleted { get; private set; } = false;

        public bool CalculateForAllLevelsLessOrEqualPC
        {
            get
            {
                return (this.Data & 0x2) != 0;
            }
        }

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

            CustomLogger.WriteLine(LogLevel.Misc, $"leveled creature record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                LeveledListItem? lastItem = null;

                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();

                    switch (field)
                    {
                        case "NAME":
                            {
                                Id = reader.ReadNullTerminatedString(length);
                                CustomLogger.WriteLine(LogLevel.Misc, $"ID {Id}");
                                break;
                            }
                        case "DATA":
                            {
                                Data = reader.ReadUInt32();
                                break;
                            }
                        case "NNAM":
                            {
                                ChanceNone = reader.ReadByte();
                                break;
                            }
                        case "INDX":
                            {
                                Count = reader.ReadUInt32();
                                break;
                            }
                        case "CNAM":
                            {
                                var creaId = reader.ReadNullTerminatedString(length);
                                this.Creatures.Add(creaId);

                                LeveledListItem item = new(creaId, 0);
                                this.Items.Add(item);
                                lastItem = item;
                                break;
                            }
                        case "INTV":
                            {
                                uint pcLevel = reader.ReadUInt16();
                                if (lastItem is not null)
                                {
                                    lastItem.Level = pcLevel;
                                    lastItem = null;
                                }
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
