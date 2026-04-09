using Quest_Data_Builder.Config;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Quest_Data_Builder.TES3.Quest
{
    internal partial class TopicRequirements : ConcurrentBag<QuestRequirement>
    {
        public TopicRequirements(TopicRecord topic)
        {
            if (topic.Actor is not null)
            {
                var requirement = new QuestRequirement(topic);
                requirement.Type = RequirementType.CustomActor;
                requirement.Object = topic.Actor;
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
                var topicVar = topic.Variables.Find(a => a.DetailsValue == RequirementType.PreviousDialogChoice);
                if (topicVar is not null)
                {
                    int groupId = 0;
                    var choiceChain = new List<(string CurrentTopicId, string ParentTopicId)>();

                    FindChoiceChain(topic, topicVar, ref groupId, choiceChain);

                    if (choiceChain.Count > 1)
                    {
                        if (MainConfig.OptimizeData)
                        {
                            var link = choiceChain[0];
                            var linkReq = new QuestRequirement();
                            linkReq.Type = RequirementType.CustomDialogueChoiceLink;
                            linkReq.ValueStr = link.CurrentTopicId;
                            linkReq.Variable = link.ParentTopicId;
                            this.Add(linkReq);
                        }
                        else
                        {
                            foreach (var link in choiceChain)
                            {
                                var linkReq = new QuestRequirement();
                                linkReq.Type = RequirementType.CustomDialogueChoiceLink;
                                linkReq.ValueStr = link.CurrentTopicId;
                                linkReq.Variable = link.ParentTopicId;
                                this.Add(linkReq);
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Recursively finds the chain of dialogue choices starting from a topic with PreviousDialogChoice requirement.
        /// Also adds requirements from parent topics in the chain to the initial topic.
        /// </summary>
        private void FindChoiceChain(TopicRecord topic, SCVRVariable topicVar, ref int groupId, 
            List<(string CurrentTopicId, string ParentTopicId)> chain, HashSet<string>? visitedTopics = null,
            bool isInitialTopic = true)
        {
            visitedTopics ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (topic.Parent is null) return;
            if (visitedTopics.Contains(topic.Id)) return;
            visitedTopics.Add(topic.Id);

            for (int i = topic.Parent.Topics.IndexOf(topic) + 1; i < topic.Parent.Topics.Count; i++)
            {
                var next = topic.Parent.Topics[i];

                var hasPrevDiaChoiceReq = next.Variables.Exists(a => a.DetailsValue == RequirementType.PreviousDialogChoice);

                if (groupId > 0 && hasPrevDiaChoiceReq)
                    break;

                if (next.Result is not null && ChoiceRegex().Match(next.Result).Success)
                {
                    ScriptBlock scriptBlock = new(next.Result);
                    if (scriptBlock.FindChoiceFunction(topicVar.IntValue.ToString(), out var results))
                    {
                        foreach (var res in results)
                        {
                            if (next.Variables.Count > 0)
                            {
                                foreach (var req in next.Variables)
                                {
                                    var requirement = new QuestRequirement(req);
                                    if (requirement.Type == RequirementType.PreviousDialogChoice) continue;
                                    requirement.GroupId = groupId;
                                    this.Add(requirement);
                                }
                            }
                            else
                            {
                                var requirement = new QuestRequirement();
                                requirement.GroupId = groupId;
                                this.Add(requirement);
                            }

                            var targetReq = this.FirstOrDefault(a => a.Type == RequirementType.PreviousDialogChoice && a.Value == topicVar.IntValue);
                            if (targetReq is not null)
                            {
                                targetReq.Variable = res.Text;
                            }

                            if (res.Requirements is not null)
                                foreach (var req in res.Requirements)
                                {
                                    var r = (QuestRequirement)req.Clone();
                                    r.GroupId = groupId;
                                    this.Add(r);
                                }

                            chain.Add((topic.Id, next.Id));

                            var nextTopicVar = next.Variables.Find(a => a.DetailsValue == RequirementType.PreviousDialogChoice);
                            if (nextTopicVar is not null)
                            {
                                // Add requirements from the parent topic to the initial topic
                                AddParentTopicRequirements(next, groupId);

                                FindChoiceChain(next, nextTopicVar, ref groupId, chain, visitedTopics, isInitialTopic: false);
                            }

                            groupId++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds requirements from a parent topic in the chain to the initial topic.
        /// </summary>
        private void AddParentTopicRequirements(TopicRecord parentTopic, int groupId)
        {
            foreach (var variable in parentTopic.Variables)
            {
                var requirement = new QuestRequirement(variable);
                if (requirement.Type == RequirementType.PreviousDialogChoice) continue;

                if (!requirement.IsPlayerRequirement && requirement.Object is null)
                {
                    requirement.Object = parentTopic.Actor;
                }
                if (parentTopic.Parent is not null)
                    requirement.Dialogue = (parentTopic.Parent.Id, parentTopic.Id);

                requirement.GroupId = groupId;
                this.Add(requirement);
            }
        }

        [GeneratedRegex(@"choice .+?\d+", RegexOptions.IgnoreCase)]
        private static partial Regex ChoiceRegex();
    }
}
