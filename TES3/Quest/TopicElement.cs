using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class TopicElement
    {
        public string Id => Record.Id;
        public string? Name => Record.Parent?.Id;

        public TopicRecord Record;
        public ScriptBlock? ScriptBlock;
        public TopicRequirements Requirements;

        public TopicElement(TopicRecord topic)
        {
            this.Record = topic;

            this.Requirements = new TopicRequirements(topic);

            if (topic.Result is not null)
            {
                this.ScriptBlock = new ScriptBlock("_dialog_\n" + topic.Result + "\nend", topic);
                this.ScriptBlock.AddRequirements(this.Requirements);
            }
        }
    }
}
