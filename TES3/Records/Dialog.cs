using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;

namespace Quest_Data_Builder.TES3.Records
{
    enum DialogType
    {
        RegularTopic = 0,
        Voice = 1,
        Greeting = 2,
        Persuasion = 3,
        Journal = 4,
    }

    internal class DialogRecord : Record
    {
        public DialogType Type { get; private set; }
        public readonly string Id = string.Empty;
        public readonly List<TopicRecord> Topics = new();
        public bool IsDeleted { get; private set; } = false;

        public DialogRecord(RecordData recordData) : base(recordData)
        {
            if (this.RecordInfo.Type != "DIAL") throw new Exception("not a dialog record");
            if (this.RecordInfo.Data is null)
            {
                this.RecordInfo.DeserializeSubRecords();
            }

            if (this.RecordInfo.Data is null) throw new Exception("dialog record data is null");

            IsDeleted = this.RecordInfo.Deleted;

            CustomLogger.WriteLine(LogLevel.Misc, $"dialog record {this.RecordInfo.Position}");

            using (var reader = new BetterBinaryReader(new MemoryStream(this.RecordInfo.Data)))
            {
                while (reader.Position < reader.Length)
                {
                    string field = reader.ReadString(4);
                    int length = reader.ReadInt32();

                    switch (field)
                    {
                        case "NAME":
                            {
                                Id = reader.ReadNullTerminatedString(length);
                                CustomLogger.WriteLine(LogLevel.Misc, $"ID {Id}");
                                break;
                            }
                        case "DATA":
                            {
                                Type = (DialogType)reader.ReadByte();
                                break;
                            }
                        case "DELE":
                            {
                                reader.Position += length;
                                IsDeleted = true;
                                break;
                            }
                        default:
                            {
                                reader.Position += length;
                                break;
                            }
                    }
                }
            }

            foreach (var childRecordData in RecordInfo.ChildRecords)
            {
                Topics.Add(new TopicRecord(childRecordData, this));
            }
        }


        private void SortTopics()
        {
            if (Topics.Count <= 1) return;

            var topics = Topics.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

            TopicRecord? head = Topics.FirstOrDefault(t => 
                string.IsNullOrEmpty(t.PreviousId) || !topics.ContainsKey(t.PreviousId));

            if (head == null) return;

            var sorted = new List<TopicRecord>();
            var current = head;
            var hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (current != null && !hash.Contains(current.Id))
            {
                sorted.Add(current);
                hash.Add(current.Id);

                if (string.IsNullOrEmpty(current.NextId) || !topics.TryGetValue(current.NextId, out var next))
                    break;

                current = next;
            }

            if (sorted.Count < Topics.Count)
            {
                sorted.AddRange(Topics.Where(t => !hash.Contains(t.Id)));
            }

            Topics.Clear();
            Topics.AddRange(sorted);
        }


        public void Merge(DialogRecord newRecord)
        {
            this.Type = newRecord.Type;
            this.IsDeleted |= newRecord.IsDeleted;
            foreach (var newTopic in newRecord.Topics)
            {
                var topic = this.Topics.FirstOrDefault(a => string.Equals(a.Id, newTopic.Id, StringComparison.OrdinalIgnoreCase));
                if (topic is not null)
                {
                    topic.Merge(newTopic);
                }
                else
                {
                    newTopic.SetParent(this);
                    this.Topics.Add(newTopic);
                }
            }
            SortTopics();
        }
    }
}
