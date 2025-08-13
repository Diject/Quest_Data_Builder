using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Records;

namespace Quest_Data_Builder.TES3.Script
{
    internal class ScriptData
    {
        public string Id => Record.Id;

        public readonly ScriptRecord Record;
        public readonly ScriptBlock BlockData;

        public ScriptData(ScriptRecord record)
        {
            this.Record = record;
            this.BlockData = new ScriptBlock(record.Text ?? "");

            CustomLogger.WriteLine(LogLevel.Info, $"new script data {this.Record.Id}");
        }

        public ScriptData(ScriptRecord record, string text)
        {
            this.Record = record;
            this.BlockData = new ScriptBlock(text);

            CustomLogger.WriteLine(LogLevel.Info, $"new script data {this.Record.Id}");
        }
    }
}
