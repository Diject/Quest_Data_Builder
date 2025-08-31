using Quest_Data_Builder.Core;
using Quest_Data_Builder.Extentions;
using Quest_Data_Builder.Logger;

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

            CustomLogger.WriteLine(LogLevel.Misc, $"topic record {RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(RecordInfo.Data!)))
            {
                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();

                    switch (field)
                    {
                        case "INAM":
                            {
                                Id = reader.ReadNullTerminatedString(length);
                                CustomLogger.WriteLine(LogLevel.Misc, $"ID {Id}");
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
                                Variables.Last().INTV = reader.ReadInt32();
                                break;
                            }
                        case "FLTV":
                            {
                                Variables.Last().FLTV = reader.ReadSingle();
                                break;
                            }
                        case "BNAM":
                            {
                                Result = reader.ReadNullTerminatedString(length).RemoveMWScriptComments();
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

        /// <summary>
        /// Compares two TopicRecords by their main condition values
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Compare(TopicRecord other)
        {
            if (this.Type != other.Type) return false;
            if (this.Actor != other.Actor) return false;
            if (this.Race != other.Race) return false;
            if (this.Gender != other.Gender) return false;
            if (this.Class != other.Class) return false;
            if (this.Faction != other.Faction) return false;
            if (this.Cell != other.Cell) return false;
            if (this.Faction != other.Faction) return false;
            if (this.Rank != other.Rank) return false;
            if (this.PCFaction != other.PCFaction) return false;
            if (this.PCRank != other.PCRank) return false;
            return true;
        }

        /// <summary>
        /// Compares two TopicRecords by their SCVR values. Returns true if less or equal than count variables of *other* are unmatched
        /// </summary>
        /// <param name="other"></param>
        /// <param name="count">Maximum number of unmatched variables to return true</param>
        /// <param name="unmatched">Unmatched variables</param>
        /// <returns></returns>
        public bool CompareSCVR(TopicRecord other, int count, out List<SCVRVariable> unmatched)
        {
            unmatched = new List<SCVRVariable>();
            for (int i = 0; i < other.Variables.Count; i++)
            {
                var variable = other.Variables[i];

                bool found = false;
                for (int j = 0; j < this.Variables.Count; j++)
                {
                    if (variable.Compare(this.Variables[j]))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    count--;
                    if (count < 0) return false;
                    unmatched.Add(variable);
                }
            }
            return true;
        }

        public void SetParent(DialogRecord parent)
        {
            this.Parent = parent;
        }

        public void Merge(TopicRecord newRecord)
        {
            // sometimes merges are attempted with different types, ignore those because they are invalid (I guess)
            if (newRecord.Type != this.Type)
                return;

            if (newRecord.Parent is not null)
            {
                if (this.Parent is null || this.Parent.Id != newRecord.Parent.Id)
                {
                    this.Parent = newRecord.Parent;
                }
            }
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
