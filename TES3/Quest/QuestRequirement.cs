using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class QuestRequirement : ICloneable
    {
        public string Type { get; set; } = RequirementType.Custom;

        public double? Value { get; set; }
        public string? ValueStr { get; set; }
        public string? Variable { get; set; }
        public string? Object { get; set; }
        public uint? Attribute { get; set; }
        public uint? Skill { get; set; }
        public string? Script { get; set; }
        /// <summary>
        /// The text from which the record was created.
        /// Presents only in records that have the default (RequirementType.Custom) type set.
        /// </summary>
        public string? Text { get; set; }

        public SCVROperator Operator { get; set; } = SCVROperator.Equal;

        public bool IsPlayerRequirement => Object == playerId;


        const string playerId = "player";

        /// <summary>
        /// For manual init
        /// </summary>
        public QuestRequirement()
        {

        }

        public QuestRequirement(SCVRVariable topicVariable)
        {
            if (checkPlayerRequirement(topicVariable))
            {
                MakeAsPlayerRequirement();
            }

            if (topicVariable.Type == SCVRType.Function)
            {
                uint.TryParse(topicVariable.DetailsValue, out uint functionNumberId);

                if (functionNumberId >= 11 && functionNumberId <= 37)
                {
                    Skill = functionNumberId - 11;
                    Type = RequirementType.CustomSkill;
                }
                else if (functionNumberId == 10)
                {
                    Attribute = 0;
                    Type = RequirementType.CustomAttribute;
                }
                else if (functionNumberId >= 51 && functionNumberId <= 57)
                {
                    Attribute = functionNumberId - 50;
                    Type = RequirementType.CustomAttribute;
                }
                else if (topicVariable.DetailsValue == RequirementType.ValueFLTV ||
                    topicVariable.DetailsValue == RequirementType.ValueINTVLong ||
                    topicVariable.DetailsValue == RequirementType.ValueINTVShort)
                {
                    Type = RequirementType.CustomValue;
                }
                else
                {
                    Type = topicVariable.DetailsValue;
                }
            }
            else
            {
                switch (topicVariable.Type)
                {
                    case SCVRType.Journal:
                        Type = RequirementType.Journal;
                        break;

                    case SCVRType.Item:
                        Type = RequirementType.Item;
                        break;

                    case SCVRType.Dead:
                        Type = RequirementType.Dead;
                        break;

                    case SCVRType.Local:
                        Type = RequirementType.CustomLocal;
                        break;

                    case SCVRType.Global:
                        Type = RequirementType.CustomGlobal;
                        break;

                    case SCVRType.NotID:
                        Type = RequirementType.NotActorID;
                        break;

                    case SCVRType.NotFaction:
                        Type = RequirementType.NotActorFaction;
                        break;

                    case SCVRType.NotClass:
                        Type = RequirementType.NotActorClass;
                        break;

                    case SCVRType.NotRace:
                        Type = RequirementType.NotActorRace;
                        break;

                    case SCVRType.NotCell:
                        Type = RequirementType.NotActorCell;
                        break;

                    case SCVRType.NotLocal:
                        Type = RequirementType.CustomNotLocal;
                        break;

                }
                if (topicVariable.Type == SCVRType.Journal)
                {
                    Type = RequirementType.Journal;
                }
                else if (topicVariable.Type == SCVRType.Item)
                {
                    Type = RequirementType.Item;
                }
            }
            Value = topicVariable.Value;
            Operator = topicVariable.Operator;
            Variable = topicVariable.Name;
        }

        private bool checkPlayerRequirement(SCVRVariable variable)
        {
            if (variable.Type == SCVRType.Function)
            {
                int functionNumberId;
                int.TryParse(variable.DetailsValue, out functionNumberId);

                if ((functionNumberId >= 5 && functionNumberId <= 43) ||
                    (functionNumberId >= 47 && functionNumberId <= 48) ||
                    (functionNumberId >= 51 && functionNumberId <= 58) ||
                    functionNumberId == 60 ||
                    functionNumberId == 64 ||
                    functionNumberId == 73)
                {
                    return true;
                }

                if (variable.DetailsValue == RequirementType.Journal) return true;
                if (variable.DetailsValue == RequirementType.Item) return true;
                if (variable.DetailsValue == RequirementType.Dead) return true;
            }
            else
            {
                if (variable.Type == SCVRType.Journal) return true;
                if (variable.Type == SCVRType.Item) return true;
                if (variable.Type == SCVRType.Dead) return true;
            }
            return false;
        }

        public object Clone()
        {
            return (QuestRequirement)this.MemberwiseClone();
        }

        public QuestRequirement Duplicate()
        {
            return (QuestRequirement)this.Clone();
        }

        public void MakeAsPlayerRequirement()
        {
            this.Object = playerId;
        }

        public void ReverseOperator()
        {
            switch (Operator)
            {
                case SCVROperator.Equal:
                    {
                        Operator = SCVROperator.NotEqual;
                        break;
                    }
                case SCVROperator.NotEqual:
                    {
                        Operator = SCVROperator.Equal;
                        break;
                    }
                case SCVROperator.Less:
                    {
                        Operator = SCVROperator.GreaterOrEqual;
                        break;
                    }
                case SCVROperator.LessOrEqual:
                    {
                        Operator = SCVROperator.Greater;
                        break;
                    }
                case SCVROperator.Greater:
                    {
                        Operator = SCVROperator.LessOrEqual;
                        break;
                    }
                case SCVROperator.GreaterOrEqual:
                    {
                        Operator = SCVROperator.Less;
                        break;
                    }
            }
        }

        public bool Equals(QuestRequirement other)
        {
            if (this.Type == other.Type &&
                this.Value == other.Value &&
                this.ValueStr == other.ValueStr &&
                this.Variable == other.Variable &&
                this.Object == other.Object &&
                this.Attribute == other.Attribute &&
                this.Skill == other.Skill &&
                this.Script == other.Script &&
                this.Operator == other.Operator)
            {
                return true;
            }
            return false;
        }
    }
}
