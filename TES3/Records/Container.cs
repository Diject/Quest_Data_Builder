﻿using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    internal class ContainerRecord : Record
    {
        public readonly string Type = RecordType.Container;
        public readonly string Id = "";
        public string? Script { get; private set; }
        public readonly Inventory CarriedItems = new();

        public bool IsDeleted { get; private set; } = false;


        public ContainerRecord(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != RecordType.Container)
                throw new Exception("not a container record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            this.IsDeleted = this.RecordInfo.Deleted;

            if (this.RecordInfo.Data is null) throw new Exception("container record data is null");

            CustomLogger.WriteLine(LogLevel.Info, $"container record {this.RecordInfo.Position}");

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
                        case "SCRI":
                            {
                                Script = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "NPCO":
                            {
                                var count = reader.ReadInt32();
                                var itemId = reader.ReadString(32).TrimEnd('\0');
                                this.CarriedItems.TryAdd(itemId, count);
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

        public void Merge(ContainerRecord newRecord)
        {
            this.Script = newRecord.Script;
            this.IsDeleted |= newRecord.IsDeleted;
            this.CarriedItems.Clear();
            foreach (var item in newRecord.CarriedItems)
            {
                this.CarriedItems.TryAdd(item.Key, item.Value);
            }
        }
    }
}
