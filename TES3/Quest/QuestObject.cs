using Quest_Data_Builder.TES3.Cell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Quest
{
    internal enum QuestObjectType
    {
        Object = 1,
        Owner = 2,
        Dialog = 3,
        Script = 4,
        Local = 5,
    }

    internal class QuestObject
    {
        public readonly string ObjectId;

        /// <summary>
        /// Name of the quest item that contains this container
        /// </summary>
        public readonly string? OriginId;

        public QuestObjectType Type = QuestObjectType.Object;

        /// <summary>
        /// Quests in which this item is involved. (quest id, quest index)
        /// </summary>
        public readonly List<(string, uint)> InvolvedQuestStages = new();

        /// <summary>
        /// Positions of the object in the world. Should be set manually
        /// </summary>
        public readonly List<QuestObjectPosition> Positions = new();

        /// <summary>
        /// Quests that this object starts (probably)
        /// </summary>
        public readonly HashSet<QuestHandler> Starts = new();

        /// <summary>
        /// Ids of objects that this container owns
        /// </summary>
        public readonly HashSet<string> Contains = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Ids of objects that owns this object. Not for script types
        /// </summary>
        public readonly HashSet<string> Links = new(StringComparer.OrdinalIgnoreCase);

        public QuestObject(string objectId)
        {
            ObjectId = objectId;
        }

        public QuestObject(string objectId, QuestObjectType? type) : this(objectId)
        {
            if (type is not null)
            {
                Type = (QuestObjectType)type;
            }
        }

        public QuestObject(string objectId, QuestObjectType? type, string originId) : this(objectId, type)
        {
            OriginId = originId;
        }

        public void AddStage(string questId, uint stage)
        {
            if (!InvolvedQuestStages.Any(a => string.Equals(a.Item1, questId, StringComparison.OrdinalIgnoreCase) && a.Item2 == stage))
            {
                InvolvedQuestStages.Add(new(questId, stage));
            }
        }

        public void AddPosition(QuestObjectPosition position)
        {
            Positions.Add(position);
        }

        public void AddContainedObjectId(QuestObject qObject)
        {
            Contains.Add(qObject.ObjectId);
            qObject.Links.Add(this.ObjectId);
        }

        public void AddContainedObjectId(string id)
        {
            Contains.Add(id);
        }
    }

    internal class QuestObjectById : Dictionary<string, QuestObject>
    {
        public QuestObjectById(StringComparer comparer) : base(comparer)
        {

        }

        public QuestObject? Add(string? objectId, QuestHandler quest, uint stageNum, QuestObjectType? type)
        {
            if (objectId is null) return null;

            if (base.TryGetValue(objectId, out var qObject))
            {
                qObject.AddStage(quest.Id, stageNum);
                if (quest.Givers.Contains(objectId))
                {
                    qObject.Starts.Add(quest);
                }
                return qObject;
            }
            else
            {
                var newObj = new QuestObject(objectId, type);
                newObj.AddStage(quest.Id, stageNum);
                if (quest.Givers.Contains(objectId))
                {
                    newObj.Starts.Add(quest);
                }

                base.Add(objectId, newObj);
                return newObj;
            }
        }

        /// <summary>
        /// For objects that owns a quest object. For "Owner" and "ScriptData" types
        /// </summary>
        public QuestObject? Add(string? ownerId, string? objectId, QuestObject questObject, QuestObjectType objType)
        {
            if (ownerId is null || objectId is null) return null;

            if (base.TryGetValue(ownerId, out var qObject))
            {
                qObject.AddContainedObjectId(questObject);
                foreach (var qStage in questObject.InvolvedQuestStages)
                {
                    qObject.AddStage(qStage.Item1, qStage.Item2);
                }
                return qObject;
            }
            else
            {
                var newObj = new QuestObject(ownerId, objType, questObject.ObjectId);
                newObj.InvolvedQuestStages.AddRange(questObject.InvolvedQuestStages);
                newObj.AddContainedObjectId(questObject);
                base.Add(ownerId, newObj);
                return newObj;
            }
        }
    }
}
