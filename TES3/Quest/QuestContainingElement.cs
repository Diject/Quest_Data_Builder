using Quest_Data_Builder.TES3.Records;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class QuestContainingElement
    {
        public object? Record { get; private set; }
        public string Id { get; private set; } = "";
        public string Type { get; private set; }

        public readonly uint? Index;
        public readonly string? QuestId;

        public QuestContainingElement(TopicRecord itemWithQuestInfo, string? questId, uint? questIndex)
        {
            if (itemWithQuestInfo.Parent?.Type == DialogType.Journal)
                throw new Exception("Journal records cannot contain a quest info");

            Record = itemWithQuestInfo;
            Type = RecordType.Topic;
            Id = itemWithQuestInfo.Id;
            QuestId = questId;
            Index = questIndex;
        }

        public QuestContainingElement(ScriptRecord itemWithQuestInfo, string? questId, uint? questIndex)
        {
            Record = itemWithQuestInfo;
            Type = RecordType.Script;
            Id = itemWithQuestInfo.Id;
            QuestId = questId;
            Index = questIndex;
        }
    }
}
