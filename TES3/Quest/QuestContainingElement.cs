using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class QuestContainingElement
    {
        public object? Record { get; private set; }
        public string Id { get; private set; } = "";
        public string Type { get; private set; }

        public TopicRecord Topic { get; private set; }
        public uint? Index { get { return Topic.Index; } }
        public string? QuestId { get { return Topic.Parent?.Id; } }

        public TopicRequirements? Requirements { get; private set; }

        public QuestContainingElement(TopicRecord itemWithQuestInfo, TopicRecord questTopic)
        {
            if (itemWithQuestInfo.Parent?.Type == DialogType.Journal)
                throw new Exception("Journal records cannot contain a quest info");

            Record = itemWithQuestInfo;
            Type = RecordType.Topic;
            Topic = questTopic;
            Requirements = new(itemWithQuestInfo);
            Id = itemWithQuestInfo.Id;
        }

        public QuestContainingElement(ScriptRecord itemWithQuestInfo, TopicRecord questTopic)
        {
            Record = itemWithQuestInfo;
            Type = RecordType.Script;
            Topic = questTopic;
            Id = itemWithQuestInfo.Id;
        }
    }
}
