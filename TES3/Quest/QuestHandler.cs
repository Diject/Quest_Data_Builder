using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Quest
{
    internal class QuestHandler
    {
        public readonly string Id;
        public readonly string? Name;

        /// <summary>
        /// by stage index
        /// </summary>
        public readonly SortedList<uint, QuestStage> Stages = new();

        /// <summary>
        /// Objects from first stage. Should be set manually
        /// </summary>
        public readonly HashSet<string> Givers = new(StringComparer.OrdinalIgnoreCase);

        public QuestHandler(DialogRecord dialog)
        {
            if (dialog.Type != DialogType.Journal) throw new Exception($"Not a journal dialog, {dialog.Id}");

            Id = dialog.Id;

            foreach (var topic in dialog.Topics) 
            {
                if (topic.QuestName == true)
                {
                    this.Name = topic.Response;
                    continue;
                }
                if (!Stages.TryAdd((uint)topic.Index!, new QuestStage(topic)))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"dialogue \"{dialog.Id}\" has several \"{topic.Index}\" indexes");
                    Stages[(uint)topic.Index!] = new QuestStage(topic);
                }
            }

            CustomLogger.WriteLine(LogLevel.Info, $"new quest handler {Id}");
        }
    }

    internal class Quests : Dictionary<string, QuestHandler>
    {

    }
}
