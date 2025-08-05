using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Quest_Data_Builder.TES3.Records
{
    internal class GlobalVariableRecord : Record
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public float FLTV { get; private set; }

        public int? IntValue = null;
        public double? FloatValue = null;

        public bool IsDeleted { get; private set; } = false;

        public GlobalVariableRecord(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != "GLOB") throw new Exception("not a global variable record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            IsDeleted = this.RecordInfo.Deleted;

            if (this.RecordInfo.Data is null) throw new Exception("record data is null");

            CustomLogger.WriteLine(LogLevel.Misc, $"global variable record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();
                    long end = reader.Position + length;

                    switch (field)
                    {
                        case "NAME":
                            {
                                Name = reader.ReadNullTerminatedString();
                                break;
                            }
                        case "FNAM":
                            {
                                Type = reader.ReadString(1);
                                break;
                            }
                        case "FLTV":
                            {
                                FLTV = reader.ReadSingle();
                                if (!String.Equals(Type, "f", StringComparison.OrdinalIgnoreCase))
                                {
                                    IntValue = (int)FLTV;
                                }
                                else
                                {
                                    FloatValue = FLTV;
                                }
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


        public void Merge(GlobalVariableRecord newRecord)
        {
            this.FLTV = newRecord.FLTV;
            this.Type = newRecord.Type;

            this.IntValue = null;
            this.FloatValue = null;

            if (String.Equals(newRecord.Type, "f", StringComparison.OrdinalIgnoreCase))
            {
                this.FloatValue = newRecord.FloatValue;
            }
            else
            {
                this.IntValue = newRecord.IntValue;
            }

            this.IsDeleted |= newRecord.IsDeleted;
        }
    }
}
