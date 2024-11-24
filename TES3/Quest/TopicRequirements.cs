using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class TopicRequirements : List<QuestRequirement>
    {
        public TopicRequirements(TopicRecord topic)
        {
            if (topic.Actor is not null)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomActor;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Cell is not null)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomActorCell;
                requirement.ValueStr = topic.Cell;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Class is not null)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomActorClass;
                requirement.ValueStr = topic.Class;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Gender is not null && (int)topic.Gender != -1)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomActorGender;
                requirement.Value = (int)topic.Gender;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Faction is not null)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomActorFaction;
                requirement.ValueStr = topic.Faction;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Rank != -1)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.RankRequirement;
                requirement.Operator = SCVROperator.GreaterOrEqual;
                requirement.Value = topic.Rank;
                requirement.Variable = topic.Faction;
                requirement.Object = topic.Actor;
                this.Add(requirement);
            }

            if (topic.Disposition != 0)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomDisposition;
                requirement.Operator = SCVROperator.GreaterOrEqual;
                requirement.Object = topic.Actor;
                requirement.Value = topic.Disposition;
                this.Add(requirement);
            }

            if (topic.PCFaction is not null)
            {
                var requirement = new QuestRequirement();
                requirement.Type = RequirementType.CustomPCFaction;
                requirement.ValueStr = topic.PCFaction;
                requirement.MakeAsPlayerRequirement();
                this.Add(requirement);
            }

            if (topic.PCRank != -1)
            {
                var requirement = new QuestRequirement();
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
                this.Add(requirement);
            }
        }
    }
}
