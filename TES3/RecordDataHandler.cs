using Quest_Data_Builder.TES3.Handlers;
using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Variables;
using System.Collections.Concurrent;

namespace Quest_Data_Builder.TES3
{
    internal partial class RecordDataHandler
    {
        private readonly TES3DataFile master;
        public Dictionary<string, ScriptRecord> Scripts = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, DialogRecord> Dialogs = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, ActorRecord> Actors = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, ContainerRecord> Containers = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, CellRecord> Cells = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, RecordWithScript> RecordsWithScript = new(StringComparer.OrdinalIgnoreCase);
        public LeveledItemHandler LeveledItems = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, LeveledCreature> LeveledCreatures = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, GlobalVariableRecord> GlobalVariables = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, LandRecord> Lands = new(StringComparer.OrdinalIgnoreCase);

        public RecordDataHandler(TES3DataFile master)
        {
            this.master = master;

            foreach (var recordData in master.Records.Values.SelectMany(a => a))
            {
                var record = new RecordWithScript(recordData);
                if (record.Script is not null)
                {
                    RecordsWithScript.TryAdd(record.Id, record);
                }
            }

            if (master.Records.ContainsKey(RecordType.Script))
                foreach (var recordData in master.Records[RecordType.Script])
                {
                    var record = new ScriptRecord(recordData);
                    this.Scripts.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.Dialog))
                foreach (var recordData in master.Records[RecordType.Dialog])
                {
                    var record = new DialogRecord(recordData);
                    this.Dialogs.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.NPC))
                foreach (var recordData in master.Records[RecordType.NPC])
                {
                    var record = new ActorRecord(recordData);
                    this.Actors.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.Creature))
                foreach (var recordData in master.Records[RecordType.Creature])
                {
                    var record = new ActorRecord(recordData);
                    this.Actors.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.Container))
                foreach (var recordData in master.Records[RecordType.Container])
                {
                    var record = new ContainerRecord(recordData);
                    this.Containers.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.Cell))
                foreach (var recordData in master.Records[RecordType.Cell])
                {
                    var record = new CellRecord(recordData);
                    this.Cells.TryAdd(record.UniqueName, record);
                }

            if (master.Records.ContainsKey(RecordType.LeveledItem))
                foreach (var recordData in master.Records[RecordType.LeveledItem])
                {
                    var record = new LeveledItem(recordData);
                    this.LeveledItems.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.LeveledCreature))
                foreach (var recordData in master.Records[RecordType.LeveledCreature])
                {
                    var record = new LeveledCreature(recordData);
                    this.LeveledCreatures.TryAdd(record.Id, record);
                }

            if (master.Records.ContainsKey(RecordType.GlobalVariable))
                foreach (var recordData in master.Records[RecordType.GlobalVariable])
                {
                    var record = new GlobalVariableRecord(recordData);
                    this.GlobalVariables.TryAdd(record.Name, record);
                }

            if (master.Records.TryGetValue(RecordType.Land, out var landRecords))
            {
                foreach (var recordData in landRecords)
                {
                    var record = new LandRecord(recordData);
                    this.Lands.TryAdd(record.Name, record);
                }
            }
        }

        public void IterateObjectPositionsFromCells(QuestObjectById objectsToFind, Action<CellRecord, CellReference, QuestObject> action)
        {
            foreach (var cell in this.Cells.Values)
            {
                foreach (var cellObject in cell.References)
                {
                    if (this.LeveledCreatures.TryGetValue(cellObject.ObjectId!, out var leveledCreature))
                    {
                        foreach (var creaId in leveledCreature.Creatures)
                        {
                            if (objectsToFind.TryGetValue(creaId, out var questObject))
                            {
                                action(cell, cellObject, questObject);
                            }
                        }
                    }
                    else if (objectsToFind.TryGetValue(cellObject.ObjectId!, out var questObject))
                    {
                        action(cell, cellObject, questObject);
                    }
                }
            }
        }

        public void IterateItemsInActors(QuestObjectById objectsToFind, Action<string, ActorRecord, QuestObject, ItemCount> action)
        {
            Parallel.ForEach(this.Actors.Values, (actor, state) =>
            //foreach (var actor in this.Actors.Values)
            {
                lock (actor)
                    foreach (var itemInfo in actor.CarriedItems)
                    {
                        if (objectsToFind.TryGetValue(itemInfo.Key, out var questObject))
                        {
                            action(itemInfo.Key, actor, questObject, itemInfo.Value);
                        }
                    }
            });
        }

        public void IterateItemsInContainers(QuestObjectById objectsToFind, Action<string, ContainerRecord, QuestObject, ItemCount> action)
        {
            Parallel.ForEach(this.Containers.Values, (container, state) =>
            //foreach (var container in this.Containers.Values)
            {
                lock (container)
                    foreach (var itemInfo in container.CarriedItems)
                    {
                        if (objectsToFind.TryGetValue(itemInfo.Key, out var questObject))
                        {
                            action(itemInfo.Key, container, questObject, itemInfo.Value);
                        }
                    }
            });
        }

        public void AddItemsFromLeveledListsToObjects()
        {
            // TODO unite with the next
            foreach (var container in this.Containers.Values)
            {
                foreach (var carriedItem in container.CarriedItems.ToDictionary())
                {
                    if (!this.LeveledItems.TryGetValue(carriedItem.Key, out var levItem)) continue;

                    var itemChances = levItem.GetChances(20);

                    if (itemChances is null) continue;

                    foreach (var itemChance in itemChances)
                    {
                        if (levItem.CalculateForEach)
                        {
                            // probability of at least one success multiplied by number of items
                            double chance = (1 - Math.Pow(1 - itemChance.Value, carriedItem.Value.Count));
                            container.CarriedItems.Add(itemChance.Key, chance * carriedItem.Value.Count, chance);
                        }
                        else
                        {
                            container.CarriedItems.Add(itemChance.Key, itemChance.Value * carriedItem.Value.Count, itemChance.Value);
                        }
                    }
                }
            }

            foreach (var container in this.Actors.Values)
            {
                foreach (var carriedItem in container.CarriedItems.ToDictionary())
                {
                    if (!this.LeveledItems.TryGetValue(carriedItem.Key, out var levItem)) continue;

                    var itemChances = levItem.GetChances(20);

                    if (itemChances is null) continue;

                    foreach (var itemChance in itemChances)
                    {
                        if (levItem.CalculateForEach)
                        {
                            // probability of at least one success multiplied by number of items
                            double chance = (1 - Math.Pow(1 - itemChance.Value, carriedItem.Value.Count));
                            container.CarriedItems.Add(itemChance.Key, chance * carriedItem.Value.Count, chance);
                        }
                        else
                        {
                            container.CarriedItems.Add(itemChance.Key, itemChance.Value * carriedItem.Value.Count, itemChance.Value);
                        }
                    }
                }
            }
        }

        public void Merge(RecordDataHandler newHandler)
        {
            foreach (var newCellItem in newHandler.Cells)
            {
                if (this.Cells.TryGetValue(newCellItem.Key, out var cell))
                {
                    cell.Merge(newCellItem.Value);
                }
                else
                {
                    this.Cells.Add(newCellItem.Key, newCellItem.Value);
                }
            }

            foreach (var newActorItem in newHandler.Actors)
            {
                if (this.Actors.TryGetValue(newActorItem.Key, out var actor))
                {
                    actor.Merge(newActorItem.Value);
                }
                else
                {
                    this.Actors.TryAdd(newActorItem.Key, newActorItem.Value);
                }
            }

            foreach (var newContainerItem in newHandler.Containers)
            {
                if (this.Containers.TryGetValue(newContainerItem.Key, out var container))
                {
                    container.Merge(newContainerItem.Value);
                }
                else
                {
                    this.Containers.TryAdd(newContainerItem.Key, newContainerItem.Value);
                }
            }

            foreach (var newDialogItem in newHandler.Dialogs)
            {
                if (this.Dialogs.TryGetValue(newDialogItem.Key, out var dia))
                {
                    dia.Merge(newDialogItem.Value);
                }
                else
                {
                    this.Dialogs.TryAdd(newDialogItem.Key, newDialogItem.Value);
                }
            }

            foreach (var newScriptItem in newHandler.Scripts)
            {
                if (this.Scripts.TryGetValue(newScriptItem.Key, out var script))
                {
                    script.Merge(newScriptItem.Value);
                }
                else
                {
                    this.Scripts.Add(newScriptItem.Key, newScriptItem.Value);
                }
            }

            foreach (var newItem in newHandler.RecordsWithScript)
            {
                if (this.RecordsWithScript.TryGetValue(newItem.Key, out var record))
                {
                    record.Merge(newItem.Value);
                }
                else
                {
                    this.RecordsWithScript.Add(newItem.Key, newItem.Value);
                }
            }

            foreach (var newItem in newHandler.LeveledItems)
            {
                if (this.LeveledItems.TryGetValue(newItem.Key, out var record))
                {
                    record.Merge(newItem.Value);
                }
                else
                {
                    this.LeveledItems.Add(newItem.Key, newItem.Value);
                }
            }

            foreach (var newItem in newHandler.LeveledCreatures)
            {
                if (this.LeveledCreatures.TryGetValue(newItem.Key, out var record))
                {
                    record.Merge(newItem.Value);
                }
                else
                {
                    this.LeveledCreatures.Add(newItem.Key, newItem.Value);
                }
            }

            foreach (var newItem in newHandler.GlobalVariables)
            {
                if (this.GlobalVariables.TryGetValue(newItem.Key, out var record))
                {
                    record.Merge(newItem.Value);
                }
                else
                {
                    this.GlobalVariables.TryAdd(newItem.Key, newItem.Value);
                }
            }

            foreach (var newItem in newHandler.Lands)
            {
                if (this.Lands.TryGetValue(newItem.Key, out var record))
                {
                    record.Merge(newItem.Value);
                }
                else
                {
                    this.Lands.TryAdd(newItem.Key, newItem.Value);
                }
            }
        }

        public void RemoveDeletedRecords()
        {
            {
                HashSet<string> deleted = new();
                foreach (var cellItem in this.Cells)
                {
                    if (cellItem.Value.IsDeleted)
                    {
                        deleted.Add(cellItem.Key);
                    }
                    else
                    {
                        for (int i = cellItem.Value.References.Count - 1; i >= 0; i--)
                        {
                            var reference = cellItem.Value.References[i];
                            if (reference.Deleted)
                            {
                                cellItem.Value.References.RemoveAt(i);
                            }
                        }
                    }
                }

                foreach (var cellKey in deleted)
                {
                    this.Cells.Remove(cellKey);
                }
            }

            {
                HashSet<string> deleted = new();
                foreach (var actorItem in this.Actors)
                {
                    if (actorItem.Value.IsDeleted)
                        deleted.Add(actorItem.Key);
                }

                foreach (var key in deleted)
                {
                    this.Actors.Remove(key, out var _);
                }
            }

            {
                HashSet<string> deleted = new();
                foreach (var scriptItem in this.Scripts)
                {
                    if (scriptItem.Value.IsDeleted)
                        deleted.Add(scriptItem.Key);
                }

                foreach (var key in deleted)
                {
                    this.Scripts.Remove(key);
                }
            }

            {
                HashSet<string> deleted = new();
                foreach (var dialogItem in this.Dialogs)
                {
                    if (dialogItem.Value.IsDeleted)
                    {
                        deleted.Add(dialogItem.Key);
                    }
                    else
                    {
                        for (int i = dialogItem.Value.Topics.Count - 1; i >= 0; i--)
                        {
                            var topic = dialogItem.Value.Topics[i];
                            if (topic.IsDeleted)
                            {
                                dialogItem.Value.Topics.RemoveAt(i);
                            }
                        }
                    }
                }

                foreach (var key in deleted)
                {
                    this.Dialogs.Remove(key, out var _);
                }
            }

            {
                HashSet<string> deleted = new();
                foreach (var recordItem in this.RecordsWithScript)
                {
                    if (recordItem.Value.IsDeleted)
                        deleted.Add(recordItem.Key);
                }

                foreach (var key in deleted)
                {
                    this.RecordsWithScript.Remove(key);
                }
            }


            {
                HashSet<string> deleted = new();
                foreach (var levItem in this.LeveledItems)
                {
                    if (levItem.Value.IsDeleted)
                        deleted.Add(levItem.Key);
                }

                foreach (var key in deleted)
                {
                    this.LeveledItems.Remove(key);
                }
            }


            {
                HashSet<string> deleted = new();
                foreach (var land in this.Lands)
                {
                    if (land.Value.IsDeleted)
                        deleted.Add(land.Key);
                }

                foreach (var key in deleted)
                {
                    this.Lands.Remove(key, out _);
                }
            }
        }

    }
}
