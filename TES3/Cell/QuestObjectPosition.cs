using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using System.Numerics;

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

        public QuestObjectPosition(string? cellName, string objectId, Vector3 position, Tuple<int, int>? gridPosition = null)
        {
            ObjectId = objectId;
            Position = position;
            CellName = cellName ?? string.Empty;
            if (gridPosition is null)
            {
                GridPosition = String.IsNullOrEmpty(CellName) ? new ((int)Math.Floor(position.X / 8192), (int)Math.Floor(position.Y / 8192)) : null;
            }
            else
            {
                GridPosition = gridPosition;
            }
            
        }

        public QuestObjectPosition(string? cellName, QuestObject questObject, Vector3 position, Tuple<int, int>? gridPosition = null) : this(cellName, questObject.ObjectId, position, gridPosition)
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
                !cell.IsInterior ? new(cell.GridX, cell.GridY) : null);

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


        public void Add(string? cellName, QuestObject questObject, Vector3 position, Tuple<int, int>? gridPosition = null,
            bool updateObjCount = true)
        {
            var cellPos = new QuestObjectPosition(cellName, questObject, position, gridPosition);
            questObject.AddPosition(cellPos, updateObjCount);

            var cellUniqueName = CellRecord.GetCellUniqueName(cellName, position);

            if (base.TryGetValue(cellUniqueName, out var cellDictionary))
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
                base.Add(cellUniqueName, new(StringComparer.OrdinalIgnoreCase) { { questObject.ObjectId, new() { cellPos } } });
            }
        }


        public bool? HasObjectPositionInCell(string? cellName, string objectId, Vector3 position, double distanceThreshold = 100.0)
        {
            var cellUniqueName = CellRecord.GetCellUniqueName(cellName, position);
            if (base.TryGetValue(cellUniqueName, out var objects))
            {
                if (objects.TryGetValue(objectId, out var poss))
                {
                    foreach (var objPos in poss)
                    {
                        if (Vector3.Distance(objPos.Position, position) <= distanceThreshold)
                            return true;
                    }
                    return false;
                }
            }
            return null;
        }
    }
}
