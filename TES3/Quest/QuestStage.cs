using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class QuestStage
    {
        public string Id => topic.Id;
        public uint Index => topic.Index ?? 0;
        public string Text => topic.Response ?? string.Empty;
        public bool IsFinished => topic.QuestFinished ?? false;
        public bool IsRestart => topic.QuestRestart ?? false;

        public readonly List<QuestStage> NextStages = new();
        public readonly QuestStageRequirements Requirements = new();

        private readonly TopicRecord topic;

        public QuestStage(TopicRecord topic)
        {
            if (topic.Type != DialogType.Journal)
                throw new ArgumentException($"Not a journal type dialog, {topic.Id}");

            this.topic = topic;
        }

        public void AddRequirements(IEnumerable<QuestRequirement>? requirements)
        {
            if (requirements is null)
                return;

            var block = new QuestRequirementList();

            bool hasAddedDialRequirement = false;

            foreach (var requirement in requirements)
            {
                if (!hasAddedDialRequirement && requirement.Dialogue is not null)
                {
                    var newRequirement = new QuestRequirement();
                    newRequirement.Type = RequirementType.CustomDialogue;
                    newRequirement.Variable = requirement.Dialogue;

                    block.Add(newRequirement);
                    hasAddedDialRequirement = true;
                }
                block.Add(requirement);
            }

            Requirements.Add(block);
        }

        public bool AddNextStage(QuestStage nextStage)
        {
            if (!NextStages.Exists(a => a.Index == nextStage.Index))
            {
                NextStages.Add(nextStage);
                return true;
            }
            return false;
        }
    }


    internal class QuestStageRequirements : List<QuestRequirementList>
    {
        public List<string> GetInvolvedObjectIds()
        {
            var ret = new List<string>();
            foreach (var requirementBlock in this)
            {
                foreach (var requirement in requirementBlock)
                {
                    if (requirement.Object is not null)
                    {
                        ret.Add(requirement.Object);
                    }
                }
            }
            return ret;
        }

        public bool IsContainsJornalIndexRequirement(string questId, uint index)
        {
            foreach (var requirement in this)
            {
                if (requirement.IsContainsJornalIndexRequirement(questId, index))
                    return true;
            }
            return false;
        }

        public bool IsContainsRequirementType(string type)
        {
            foreach (var requirement in this)
            {
                if (requirement.IsContainsRequirementType(type))
                    return true;
            }
            return false;
        }
    }
}
