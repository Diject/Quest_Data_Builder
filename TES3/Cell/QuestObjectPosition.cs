using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Cell
{
    internal class QuestObjectPosition
    {
        public readonly string ObjectId;
        /// <summary>
        /// Name of the quest item that contains this container
        /// </summary>
        public readonly string? OriginId;
        public readonly QuestObjectType ObjectType = QuestObjectType.Object;
        public readonly Vector3 Position;
        public readonly string CellName;
        public readonly Tuple<int, int>? GridPosition;

        public QuestObjectPosition(string cellName, string objectId, Vector3 position, Tuple<int, int>? gridPosition)
        {
            ObjectId = objectId;
            CellName = cellName;
            Position = position;
            GridPosition = gridPosition;
        }

        public QuestObjectPosition(string cellName, QuestObject questObject, Vector3 position, Tuple<int, int>? gridPosition) : this(cellName, questObject.ObjectId, position, gridPosition)
        {
            this.ObjectType = questObject.Type;

            if (questObject.OriginId is not null && !string.Equals(questObject.OriginId, questObject.ObjectId, StringComparison.OrdinalIgnoreCase))
                OriginId = questObject.OriginId;
        }
    }

    internal class ObjectPositionsInCell : Dictionary<string, Dictionary<string, List<QuestObjectPosition>>>
    {
        public ObjectPositionsInCell(StringComparer comparer) : base(comparer) { }

        public void Add(CellRecord cell, CellReference reference, QuestObject questObject)
        {
            if (reference.Position is null) return;

            var cellPos = new QuestObjectPosition(cell.UniqueName, questObject, (Vector3)reference.Position!,
                !cell.IsInterior ? new (cell.GridX, cell.GridY) : null);

            questObject.AddPosition(cellPos);

            if (base.TryGetValue(cell.UniqueName, out var cellDictionary))
            {
                if (cellDictionary.TryGetValue(questObject.ObjectId, out var cellObjDictionary))
                {
                    cellObjDictionary.Add(cellPos);
                }
                else
                {
                    cellDictionary.Add(questObject.ObjectId, new() { cellPos });
                }

            }
            else
            {
                base.Add(cell.UniqueName, new(StringComparer.OrdinalIgnoreCase) { { questObject.ObjectId, new() { cellPos } } });
            }
        }
    }
}
