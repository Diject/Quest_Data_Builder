using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Handlers;
using Quest_Data_Builder.TES3.Variables;
using System.Collections.ObjectModel;

namespace Quest_Data_Builder.TES3.Records
{
    internal class LeveledItem : Record
    {
        public LeveledItemHandler? Handler {  get; private set; }

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

        public bool CalculateForEach => (this.Data & 0x1) != 0;

        public bool CalculateForAllLevelsLessOrEqualPC => (this.Data & 0x2) != 0;

        public readonly double ChanceForItem;

        private Dictionary<string, double>? cachedChances;


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

            CustomLogger.WriteLine(LogLevel.Misc, $"leveled item record {this.RecordInfo.Position}");

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

            if (this.Items.Count > 0)
            {
                double chanceForOneRecord = 1 - this.ChanceNone * 0.01;
                chanceForOneRecord /= this.Items.Count;

                this.ChanceForItem = chanceForOneRecord;
            }
            else
            {
                this.ChanceForItem = 0;
            }
        }


        public void SetHandler(LeveledItemHandler handler)
        {
            this.Handler = handler;
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


        public ReadOnlyDictionary<string, double>? GetChances(int depth)
        {
            if (depth == 0) return null;

            if (this.cachedChances is not null) return this.cachedChances.AsReadOnly();

            Dictionary<string, double> chances = new(StringComparer.OrdinalIgnoreCase);

            foreach (var item in this.Items)
            {
                item.IdentifyType(this.Handler);

                if (item.Type == LeveledListItemType.LeveledList)
                {
                    var levListObj = (LeveledItem?)item.Object;
                    var nestedChances = levListObj?.GetChances(depth - 1);

                    if (nestedChances is null) continue;

                    foreach (var nestedChance in nestedChances)
                    {
                        double chance = nestedChance.Value * this.ChanceForItem;
                        if (!chances.TryAdd(nestedChance.Key, chance))
                        {
                            chances[nestedChance.Key] += chance;
                        }
                    }

                }
                else
                {
                    if (!chances.TryAdd(item.Id, this.ChanceForItem))
                    {
                        chances[item.Id] += this.ChanceForItem;
                    }
                }
            }

            this.cachedChances = chances;

            return chances.AsReadOnly();
        }
    }
}
