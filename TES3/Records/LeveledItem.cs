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
    internal class LeveledItem : Record
    {
        public readonly string Type = RecordType.LeveledItem;
        public readonly string Id = "";
        public readonly uint Data;
        public readonly byte ChanceNone;
        public readonly uint? Count;
        public readonly HashSet<string> CarriedItems = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Required PC's level by item id
        /// </summary>
        public readonly List<LeveledListItem> Items = new();

        public bool IsDeleted { get; private set; } = false;

        public bool CalculateForEach
        {
            get
            {
                return (this.Data & 0x1) != 0;
            }
        }

        public bool CalculateForAllLevelsLessOrEqualPC
        {
            get
            {
                return (this.Data & 0x2) != 0;
            }
        }


        public LeveledItem(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != RecordType.LeveledItem)
                throw new Exception("not a leveled item record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            this.IsDeleted = this.RecordInfo.Deleted;

            if (this.RecordInfo.Data is null) throw new Exception("leveled item record data is null");

            CustomLogger.WriteLine(LogLevel.Info, $"leveled item record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                LeveledListItem? lastItem = null;

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
                        case "INAM":
                            {
                                var itemId = reader.ReadNullTerminatedString(length);
                                this.CarriedItems.Add(itemId);


                                LeveledListItem item = new(itemId, 0);
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
            this.CarriedItems.Clear();
            foreach (var item in newRecord.CarriedItems)
            {
                this.CarriedItems.Add(item);
            }
        }
    }
}
