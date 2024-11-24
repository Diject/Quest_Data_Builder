using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    internal class CellRecord : Record
    {
        public readonly string Name = string.Empty;
        public readonly UInt32 Flags;
        public readonly int GridX;
        public readonly int GridY;

        public readonly List<CellReference> References = new();

        public bool IsDeleted { get; private set; } = false;

        public bool IsInterior { get { return (Flags & 0x1) != 0; } }

        public string UniqueName
        {
            get
            {
                if (this.IsInterior)
                {
                    return this.Name;
                }
                else
                {
                    return $"{this.GridX}, {this.GridY}";
                }
            }
        }

        public CellRecord(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != RecordType.Cell)
                throw new Exception("not a cell record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            this.IsDeleted = this.RecordInfo.Deleted;

            if (this.RecordInfo.Data is null) throw new Exception("cell record data is null");

            CustomLogger.WriteLine(LogLevel.Info, $"cell record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                bool foundDataLabel = false;

                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();

                    CustomLogger.WriteLine(LogLevel.Misc, $"field {field} length {length}");

                    switch (field)
                    {
                        case "NAME":
                            {
                                if (!foundDataLabel)
                                {
                                    Name = reader.ReadNullTerminatedString(length);
                                    CustomLogger.WriteLine(LogLevel.Info, $"ID {Name}");
                                    foundDataLabel = false;
                                }
                                else
                                {
                                    var lastRef = this.References.Last();
                                    lastRef.ObjectId = reader.ReadNullTerminatedString(length);
                                }
                                break;
                            }
                        case "DATA":
                            {
                                if (!foundDataLabel)
                                {
                                    this.Flags = reader.ReadUInt32();
                                    this.GridX = reader.ReadInt32();
                                    this.GridY = reader.ReadInt32();
                                    foundDataLabel = true;
                                }
                                else
                                {
                                    float x = reader.ReadSingle();
                                    float y = reader.ReadSingle();
                                    float z = reader.ReadSingle();
                                    float rotX = reader.ReadSingle();
                                    float rotY = reader.ReadSingle();
                                    float rotZ = reader.ReadSingle();
                                    var lastRef = this.References.Last();
                                    lastRef.Position = new Vector3(x, y, z);
                                    lastRef.Rotation = new Vector3(rotX, rotY, rotZ);
                                }
                                break;
                            }
                        case "FRMR":
                            {
                                var reference = new CellReference(reader.ReadUInt32());
                                this.References.Add(reference);
                                break;
                            }
                        case "DELE":
                            {
                                if (!foundDataLabel)
                                {
                                    this.IsDeleted = true;
                                }
                                else
                                {
                                    var lastRef = this.References.Last();
                                    lastRef.Deleted = true;
                                }
                                reader.Position += length;
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

        public void Merge(CellRecord newRecord)
        {
            this.IsDeleted |= newRecord.IsDeleted;
            foreach (var newRef in newRecord.References)
            {
                var reference = this.References.FirstOrDefault(a => a.Id == newRef.Id);
                if (reference is not null)
                {
                    reference.Merge(newRef);
                }
                else
                {
                    this.References.Add(newRef);
                }
            }
        }
    }
}
