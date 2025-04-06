using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Quest_Data_Builder.TES3.Quest
{
    internal partial class TopicRequirements : List<QuestRequirement>
    {
        public TopicRequirements(TopicRecord topic)
        {
            if (topic.Actor is not null)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomActor;
                requirement.Object = topic.Actor;
                if (topic.Parent is not null)
                {
                    requirement.Variable = Consts.DialoguePrefix + topic.Parent.Id;
                }
                this.Add(requirement);
            }

            if (topic.Cell is not null)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomActorCell;
                requirement.ValueStr = topic.Cell;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Class is not null)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomActorClass;
                requirement.ValueStr = topic.Class;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Gender is not null && (int)topic.Gender != -1)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomActorGender;
                requirement.Value = (int)topic.Gender;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Faction is not null)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomActorFaction;
                requirement.ValueStr = topic.Faction;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Rank != -1)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.RankRequirement;
                requirement.Operator = SCVROperator.GreaterOrEqual;
                requirement.Value = topic.Rank;
                requirement.Variable = topic.Faction;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Disposition != 0)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomDisposition;
                requirement.Operator = SCVROperator.GreaterOrEqual;
                requirement.Object = topic.Actor;
                requirement.Value = topic.Disposition;
                this.Add(requirement);
            }

            if (topic.PCFaction is not null)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomPCFaction;
                requirement.ValueStr = topic.PCFaction;
                requirement.MakeAsPlayerRequirement();
                this.Add(requirement);
            }

            if (topic.PCRank != -1)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomPCRank;
                requirement.Operator = SCVROperator.GreaterOrEqual;
                requirement.Value = topic.PCRank;
                requirement.Variable = topic.PCFaction;
                requirement.MakeAsPlayerRequirement();
                this.Add(requirement);
            }

            foreach (var variable in topic.Variables)
            {
                var requirement = new QuestRequirement(variable);
                if (!requirement.IsPlayerRequirement && requirement.Object is null)
                {
                    requirement.Object = topic.Actor;
                }
                if (topic.Parent is not null)
                    requirement.Dialogue = (topic.Parent.Id, topic.Id);
                this.Add(requirement);
            }

            if (topic.Parent is not null)
            {
                // search for different topics that have higher priority and almost the same requirements to detect additional requirements
                for (int i = topic.Parent.Topics.IndexOf(topic) - 1; i >= 0; i--)
                {
                    var previous = topic.Parent.Topics[i];

                    if (!previous.Compare(topic)) break;

                    if (!topic.CompareSCVR(previous, 1, out var unmatched)) continue;

                    foreach (var req in unmatched)
                    {
                        if (req.Type == SCVRType.Item || req.Type == SCVRType.Dead)
                        {
                            var requirement = new QuestRequirement(req);
                            requirement.ReverseOperator();
                            this.Add(requirement);
                        }
                    }
                }

                // search for requirements from "Choice" command owner if this topic have "PreviousDialogChoice" requirement
                if (topic.Variables.Exists(a => a.DetailsValue == RequirementType.PreviousDialogChoice))
                {
                    for (int i = topic.Parent.Topics.IndexOf(topic) + 1; i < topic.Parent.Topics.Count; i++)
                    {
                        var next = topic.Parent.Topics[i];

                        if (!next.Variables.Exists(a => a.DetailsValue == RequirementType.PreviousDialogChoice) &&
                            next.Result is not null &&
                            ChoiceRegex().Match(next.Result) is not null)
                        {
                            foreach (var req in next.Variables)
                            {
                                var requirement = new QuestRequirement(req);
                                this.Add(requirement);
                            }
                            break;
                        }
                    }
                }
            }

        }

        [GeneratedRegex(@"choice .+?\d+", RegexOptions.IgnoreCase)]
        private static partial Regex ChoiceRegex();
    }
}
