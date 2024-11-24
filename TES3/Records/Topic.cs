using Quest_Data_Builder.Core;
using Quest_Data_Builder.Extentions;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    enum Gender
    {
        None = -1,
        Male = 0,
        Female = 1,
    }

    internal partial class TopicRecord : Record
    {
        public DialogRecord? Parent { get; private set; }

        public readonly string Id = string.Empty;
        public string PreviousId { get; private set; } = string.Empty;
        public string NextId { get; private set; } = string.Empty;
        public DialogType? Type {  get; private set; }
        public UInt32? Index { get; private set; }
        public UInt32? Disposition { get { return Index; } }
        public sbyte? Rank { get; private set; }
        public sbyte? PCRank { get; private set; }
        public Gender? Gender { get; private set; }
        public string? Actor { get; private set; }
        public string? Race { get; private set; }
        public string? Class { get; private set; }
        public string? Faction { get; private set; }
        public string? Cell { get; private set; }
        public string? PCFaction { get; private set; }
        public string? Sound { get; private set; }
        public string? Response { get; private set; }
        public string? SCVR { get; private set; }
        public string? Result { get; private set; }

        public readonly List<SCVRVariable> Variables = new();

        public bool? QuestName { get; private set; }
        public bool? QuestFinished { get; private set; }
        public bool? QuestRestart { get; private set; }

        public bool IsDeleted { get; private set; } = false;

        public TopicRecord(RecordData recordData, DialogRecord? parent = null) : base(recordData)
        {
            if (recordData.Type != "INFO") throw new Exception("not a topic record");
            if (recordData.Data is null)
            {
                recordData.DeserializeSubRecords();
            }

            if (recordData.Data is null) throw new Exception("topic record data is null");

            Parent = parent;
            IsDeleted = this.RecordInfo.Deleted;

            CustomLogger.WriteLine(LogLevel.Info, $"topic record {RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(RecordInfo.Data!)))
            {
                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();

                    CustomLogger.WriteLine(LogLevel.Misc, $"field {field} length {length}");

                    switch (field)
                    {
                        case "INAM":
                            {
                                Id = reader.ReadNullTerminatedString(length);
                                CustomLogger.WriteLine(LogLevel.Info, $"ID {Id}");
                                break;
                            }
                        case "PNAM":
                            {
                                PreviousId = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "NNAM":
                            {
                                NextId = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "DATA":
                            {
                                Type = (DialogType)reader.ReadByte();
                                reader.Position += 3;
                                Index = reader.ReadUInt32();
                                Rank = reader.ReadSByte();
                                Gender = (Gender)reader.ReadSByte();
                                PCRank = reader.ReadSByte();
                                reader.Position++;
                                break;
                            }
                        case "ONAM":
                            {
                                Actor = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "RNAM":
                            {
                                Race = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "CNAM":
                            {
                                Class = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "FNAM":
                            {
                                Faction = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "ANAM":
                            {
                                Cell = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "DNAM":
                            {
                                PCFaction = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "SNAM":
                            {
                                Sound = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "NAME":
                            {
                                Response = reader.ReadNullTerminatedString(length);
                                break;
                            }
                        case "SCVR":
                            {
                                SCVR = reader.ReadString(length);
                                Variables.Add(new SCVRVariable(SCVR));
                                break;
                            }
                        case "INTV":
                            {
                                Variables.Last().INTV = reader.ReadUInt32();
                                break;
                            }
                        case "FLTV":
                            {
                                Variables.Last().FLTV = reader.ReadUInt32();
                                break;
                            }
                        case "BNAM":
                            {
                                Result = reader.ReadString(length).RemoveMWScriptComments();
                                break;
                            }
                        case "QSTN":
                            {
                                QuestName = Convert.ToBoolean(reader.ReadByte());
                                break;
                            }
                        case "QSTF":
                            {
                                QuestFinished = Convert.ToBoolean(reader.ReadByte());
                                break;
                            }
                        case "QSTR":
                            {
                                QuestRestart = Convert.ToBoolean(reader.ReadByte());
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
            Parent = parent;
        }

        public void Merge(TopicRecord newRecord)
        {
            if (newRecord.Parent is not null)
                this.Parent = newRecord.Parent;
            this.PreviousId = newRecord.PreviousId;
            this.NextId = newRecord.NextId;
            if (newRecord.Type is not null)
                this.Type = newRecord.Type;
            if (newRecord.Index is not null)
                this.Index = newRecord.Index;
            if (newRecord.Rank is not null)
                this.Rank = newRecord.Rank;
            if (newRecord.PCRank is not null)
                this.PCRank = newRecord.PCRank;
            if (newRecord.Gender is not null)
                this.Gender = newRecord.Gender;
            if (newRecord.Actor is not null)
                this.Actor = newRecord.Actor;
            if (newRecord.Race is not null)
                this.Race = newRecord.Race;
            if (newRecord.Class is not null)
                this.Class = newRecord.Class;
            if (newRecord.Faction is not null)
                this.Faction = newRecord.Faction;
            if (newRecord.Cell is not null)
                this.Cell = newRecord.Cell;
            if (newRecord.PCFaction is not null)
                this.PCFaction = newRecord.PCFaction;
            if (newRecord.Sound is not null)
                this.Sound = newRecord.Sound;
            if (newRecord.Response is not null)
                this.Response = newRecord.Response;
            if (newRecord.SCVR is not null)
                this.SCVR = newRecord.SCVR;
            if (newRecord.Result is not null)
                this.Result = newRecord.Result;

            this.Variables.Clear();
            this.Variables.AddRange(newRecord.Variables);

            if (newRecord.QuestName is not null)
                this.QuestName = newRecord.QuestName;
            if (newRecord.QuestFinished is not null)
                this.QuestFinished = newRecord.QuestFinished;
            if (newRecord.QuestRestart is not null)
                this.QuestRestart = newRecord.QuestRestart;

            this.IsDeleted |= newRecord.IsDeleted;
        }
    }
}
