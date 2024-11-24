using System;
using System.Collections;
using System.IO;
using Quest_Data_Builder.Core;
using Quest_Data_Builder.Extentions;
using Quest_Data_Builder.Logger;

namespace Quest_Data_Builder.TES3.Records
{
    internal class ScriptRecord : Record
    {

        public string Id { get; private set; } = "";
        public UInt32 NumShorts { get; private set; }
        public UInt32 NumLongs { get; private set; }
        public UInt32 NumFloats { get; private set; }
        public UInt32 ScriptDataSize { get; private set; }
        public UInt32 LocalVarSize { get; private set; }

        public bool IsDeleted { get; private set; } = false;

        private string[]? _SCVR;
        private byte[]? _SCDT;

        public string? Text { get; private set; }

        public ScriptRecord(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != "SCPT") throw new Exception("not a script record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            IsDeleted = this.RecordInfo.Deleted;

            if (this.RecordInfo.Data is null) throw new Exception("record data is null");

            CustomLogger.WriteLine(LogLevel.Info, $"script record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();
                    long end = reader.Position + length;

                    CustomLogger.WriteLine(LogLevel.Misc, $"field {field} length {length}");

                    switch (field)
                    {
                        case "SCHD":
                            {
                                Id = reader.ReadString(32).TrimEnd('\0');
                                CustomLogger.WriteLine(LogLevel.Info, $"Name {Id}");
                                NumShorts = reader.ReadUInt32();
                                NumLongs = reader.ReadUInt32();
                                NumFloats = reader.ReadUInt32();
                                ScriptDataSize = reader.ReadUInt32();
                                LocalVarSize = reader.ReadUInt32();
                                break;
                            }
                        case "SCVR":
                            {
                                // some mods have ids that are longer than 32 characters, which causes overriding of the next data
                                if (NumShorts > 47)
                                {
                                    reader.Position = end;
                                    break;
                                }

                                var varCount = NumShorts + NumLongs + NumFloats;
                                string[] arr = new string[varCount];
                                for (int i = 0; i < varCount; i++)
                                {
                                    arr[i] = reader.ReadNullTerminatedString();
                                }
                                _SCVR = arr;
                                break;
                            }
                        case "SCDT":
                            {
                                _SCDT = reader.ReadBytes(length);
                                break;
                            }
                        case "SCTX":
                            {
                                Text = reader.ReadString(length).RemoveMWScriptComments();
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

        public void Merge(ScriptRecord newRecord)
        {
            this.NumShorts = newRecord.NumShorts;
            this.NumLongs = newRecord.NumLongs;
            this.NumFloats = newRecord.NumFloats;
            this.ScriptDataSize = newRecord.ScriptDataSize;
            this.LocalVarSize = newRecord.LocalVarSize;
            this.IsDeleted |= newRecord.IsDeleted;
            if (newRecord._SCDT is not null)
                this._SCDT = newRecord._SCDT;
            if (newRecord._SCVR is not null)
                this._SCVR = newRecord._SCVR;
            if (newRecord.Text is not null)
                this.Text = newRecord.Text;
        }

    }
}
