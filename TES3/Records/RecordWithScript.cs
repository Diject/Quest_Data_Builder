using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    internal class RecordWithScript : Record
    {
        public string Type { get; private set; }
        public string Id { get; private set; } = string.Empty;
        public string? Script { get; private set; }

        public bool IsDeleted { get; private set; } = false;

        public RecordWithScript(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }
            if (this.RecordInfo.Data is null) throw new Exception("record data is null");

            IsDeleted = this.RecordInfo.Deleted;

            CustomLogger.WriteLine(LogLevel.Info, $"record {this.RecordInfo.Position}");

            Type = this.RecordInfo.Type;

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
                                break;
                            }
                        case "SCRI":
                            {
                                Script = reader.ReadNullTerminatedString(length);
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

        public void Merge(RecordWithScript newRecord)
        {
            this.Script = newRecord.Script;
            this.IsDeleted |= newRecord.IsDeleted;
        }
    }
}
