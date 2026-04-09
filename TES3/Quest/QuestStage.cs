using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class QuestStage
    {
        public readonly QuestHandler Parent;
        public readonly string Id;
        public readonly uint Index;
        public readonly string Text;
        public readonly bool IsFinished;
        public readonly bool IsRestart;

        public readonly List<QuestStage> NextStages = new();
        public readonly List<QuestStage> LinkedStages = new();
        public readonly QuestStageRequirements Requirements = new();

        public QuestStage(QuestHandler quest, TopicRecord topic)
        {
            if (topic.Type != DialogType.Journal)
                throw new ArgumentException($"Not a journal type dialog, {topic.Id}");

            Parent = quest;
            Id = topic.Id;
            Index = topic.Index ?? 0;
            Text = topic.Response ?? string.Empty;
            IsFinished = topic.QuestFinished ?? false;
            IsRestart = topic.QuestRestart ?? false;
        }

        public QuestStage(QuestHandler quest, string? id, uint index, string text)
        {
            Parent = quest;
            Id = id ?? "";
            Index = index;
            Text = text;
            IsFinished = false;
            IsRestart = false;
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
                    newRequirement.Variable = Consts.DialoguePrefix + requirement.Dialogue.Value.Id;
                    newRequirement.ValueStr = requirement.Dialogue.Value.TopicId;

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

        public bool AddLinkedStage(QuestStage linkedStage)
        {
            if (!LinkedStages.Exists(a => a.Index == linkedStage.Index && a.Parent.Id == linkedStage.Parent.Id))
            {
                LinkedStages.Add(linkedStage);
                return true;
            }
            return false;
        }
    }


    internal class QuestStageRequirements : List<QuestRequirementList>
    {
        public new void Add(QuestRequirementList item)
        {
            if (item.HasGroppedRequirements)
            {
                HashSet<int> groupIndexes = new();
                while (true)
                {
                    QuestRequirementList list = new();
                    int? grIndex = null;
                    foreach (var req in item)
                    {
                        if (req.GroupId is null)
                        {
                            list.Add(req);
                        }
                        else if (!groupIndexes.Contains((int)req.GroupId))
                        {
                            grIndex ??= req.GroupId;
                            if (grIndex == req.GroupId && req.Type != RequirementType.Custom)
                                list.Add(req);
                        }
                    }

                    if (grIndex is not null)
                    {
                        groupIndexes.Add((int)grIndex);
                        base.Add(list);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                base.Add(item);
            }
        }

        public bool Exists(QuestRequirementList requirementList)
        {
            return this.Exists(a => a.Equals(requirementList));
        }

        public void RemoveDuplicates()
        {
            for (int i = 0; i < this.Count; i++)
            {
                for (int j = this.Count - 1; j > i; j--)
                {
                    if (this[i].Equals(this[j]))
                    {
                        this.RemoveAt(j);
                    }
                }
            }
        }

        public List<string> GetInvolvedObjectIds()
        {
            var ret = new HashSet<string>();
            foreach (var requirementBlock in this)
            {
                foreach (var requirement in requirementBlock)
                {
                    if (requirement.Object is not null)
                    {
                        ret.Add(requirement.Object);
                    }
                    else if (requirement.Script is not null)
                    {
                        ret.Add(requirement.Script);
                    }
                }
            }
            return ret.ToList();
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

        public bool IsContainsDispositionOnlyRequirement()
        {
            foreach (var requirement in this)
            {
                if (requirement.IsDispositionOnlyRequirement())
                    return true;
            }
            return false;
        }

        public bool IsContainsDeadOnlyRequirement()
        {
            foreach (var requirement in this)
            {
                if (requirement.IsDeadOnlyRequirement())
                    return true;
            }
            return false;
        }

        public bool HasDialogueTopicRequirement(string topicId)
        {
            foreach (var requirement in this)
            {
                if (requirement.HasDialogueRequirementWithTopic(topicId))
                    return true;
            }
            return false;
        }

        public bool IsContainsNextStage(string questId, uint index, ScriptVariables scrVars)
        {
            foreach (var requirement in this)
            {
                if (requirement.IsNextStage(questId, index, scrVars))
                    return true;
            }
            return false;
        }
    }
}
