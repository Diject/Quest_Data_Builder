using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    internal class LandRecord : Record
    {
        public readonly int GridX;
        public readonly int GridY;
        public uint DATA { get; private set; }

        public float[,]? Heights { get; private set; }

        public string Name
        {
            get
            {
                return $"{this.GridX}, {this.GridY}";
            }
        }

        public bool IsDeleted { get; private set; } = false;


        public LandRecord(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != RecordType.Land)
                throw new Exception("not a land record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }
            if (this.RecordInfo.Data is null) throw new Exception("land record data is null");

            this.IsDeleted = this.RecordInfo.Deleted;

            CustomLogger.WriteLine(LogLevel.Misc, $"land record {this.RecordInfo.Position}");

            using var reader = new Core.BetterBinaryReader(new MemoryStream(this.RecordInfo.Data));

            while (reader.Position < reader.Length)
            {
                string field = reader.ReadString(4);
                int length = reader.ReadInt32();

                switch (field)
                {
                    case "INTV":
                        GridX = reader.ReadInt32();
                        GridY = reader.ReadInt32();
                        break;

                    case "DATA":
                        DATA = reader.ReadUInt32();
                        break;

                    case "VHGT":
                        float offset = reader.ReadSingle();

                        int[] hDataInt = new int[65 * 65];
                        for (int i = 0; i < 65 * 65; i++)
                        {
                            hDataInt[i] = (sbyte)reader.ReadByte();
                        }

                        float[,] heightmap = new float[65, 65];

                        float rowOffset = offset * 8;
                        for (int yi = 0; yi < 65; yi++)
                        {
                            rowOffset += hDataInt[yi * 65] * 8;
                            heightmap[0, yi] = rowOffset;

                            float colOffset = rowOffset;
                            for (int xi = 1; xi < 65; xi++)
                            {
                                colOffset += hDataInt[yi * 65 + xi] * 8;
                                heightmap[xi, yi] = colOffset;
                            }
                        }

                        Heights = heightmap;

                        reader.Position += 3;
                        break;

                    default:
                        reader.Position += length;
                        break;
                }
            }
        }


        public void Merge(LandRecord newRecord)
        {
            if (this.GridX != newRecord.GridX || this.GridY != newRecord.GridY)
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"cannot merge land records with different grid coordinates: ({this.GridX},{this.GridY}) != ({newRecord.GridX},{newRecord.GridY})");
                return;
            }

            this.DATA |= newRecord.DATA;

            if (newRecord.Heights is null) return;

            this.Heights = newRecord.Heights;

        }
    }
}
