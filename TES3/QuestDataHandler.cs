using Quest_Data_Builder.Extentions;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Cell;
using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Quest_Data_Builder.TES3
{
    internal partial class QuestDataHandler
    {
        private readonly RecordDataHandler dataHandler;

        /// <summary>
        /// Data about quest by its id
        /// </summary>
        public Quests QuestData = new();

        /// <summary>
        /// Objects involved to quests by id
        /// </summary>
        public QuestObjectById QuestObjects = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// List of elements that contains quest information. Low level. From dialogs and scripts. By quest id; by index
        /// </summary>
        public Dictionary<string, SortedDictionary<uint, List<QuestContainingElement>>> QuestContainigElements = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Object positions in the world. By cell id; by object id
        /// </summary>
        public ObjectPositionsInCell QuestObjectPositionsByCell = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// List of object ids that have attached script. By script id
        /// </summary>
        public Dictionary<string, List<string>> QuestObjectIDsWithScript = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Data about script
        /// </summary>
        public Dictionary<string, ScriptData> ScriptDataById = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Contains data about local variables in scripts or on objects. By script or object id; by variable name
        /// </summary>
        public ScriptVariables VariablesByScriptId = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Global variables. By variable name
        /// </summary>
        public Dictionary<string, ScriptVariableList> GlobalVariables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Number of steps in a quest to be considered a quest that an object can start(give)
        /// </summary>
        private const int numberOfStagesToBecomeQuest = 2;



        public QuestDataHandler(RecordDataHandler handler)
        {
            dataHandler = handler;

            this.findQuestContainingElements();

            // the order should stay the same
            this.FindQuestData();
            this.FindQuestRecord();
            this.FindQuestObjectPositions();
            this.FixQuestObjectData();
            this.FindVariables();
        }

        private void findQuestContainingElements()
        {
            foreach (var dialogItem in dataHandler.Dialogs)
            {
                if (this.tryAddToElementsWithAttachedQuest(dialogItem.Value))
                {
                    CustomLogger.WriteLine(LogLevel.Info, $"Found info about quest in the dialog record, {dialogItem.Value.Id}");
                }

                // find items in dialogs that are added to the player by AddItem
                foreach (var topic in dialogItem.Value.Topics)
                {
                    if (String.IsNullOrEmpty(topic.Result)) continue;
                    if (String.IsNullOrEmpty(topic.Actor)) continue;

                    var matches = AddItemRegex().Matches(topic.Result!);
                    foreach (Match match in matches)
                    {
                        var itemId = match.Groups[1].Value;
                        var qObject = this.QuestObjects.Add(itemId, QuestObjectType.Object);

                        if (qObject is null) continue;

                        var diaActorObject = this.QuestObjects.Add(topic.Actor, itemId, qObject, QuestObjectType.Owner);
                    }
                }
            }

            foreach (var script in dataHandler.Scripts)
            {
                if (this.tryAddToElementsWithAttachedQuest(script.Value))
                {
                    CustomLogger.WriteLine(LogLevel.Info, $"Found info about quest in the script record, {script.Value.Id}");
                }

                // find items in scripts that are added to the player by AddItem
                if (String.IsNullOrEmpty(script.Value.Text)) continue;
                var matches = AddItemRegex().Matches(script.Value.Text!);
                foreach (Match match in matches)
                {
                    var itemId = match.Groups[1].Value;
                    var qObject = this.QuestObjects.Add(itemId, QuestObjectType.Object);

                    if (qObject is null) continue;

                    var scrObject = this.QuestObjects.Add(script.Value.Id, itemId, qObject, QuestObjectType.Script);
                }
            }
        }

        [GeneratedRegex("Player[\" ]*->[ ]*AddItem[\", ]+([^\", ]+)[\", ]*([^\", ]*)", RegexOptions.IgnoreCase)]
        private static partial Regex AddItemRegex();


        private bool tryAddToElementsWithAttachedQuest(DialogRecord record)
        {
            bool ret = false;
            foreach (var topic in record.Topics)
            {
                ret = findQuestData(topic.Result!, (s, rec) => addQuestContainingElement(new QuestContainingElement(topic, rec))) || ret;
            }
            return ret;
        }

        private bool tryAddToElementsWithAttachedQuest(ScriptRecord record)
        {
            return findQuestData(record.Text, (s, rec) => addQuestContainingElement(new QuestContainingElement(record, rec)));
        }


        private bool findQuestData(string? text, Action<string, TopicRecord> function)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            bool ret = false;

            var matches = JournalRegex().Matches(text);

            foreach (Match match in matches)
            {

                var questName = match.Groups[1].Value;
                var questIndex = Convert.ToUInt32(match.Groups[2].Value);

                if (dataHandler.Dialogs.TryGetValue(questName, out var dialog))
                {
                    TopicRecord? questTopic = dialog.Topics.FirstOrDefault(x => x.Index == questIndex);

                    if (questTopic is null)
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, $"cannot find \"{questIndex}\" index for \"{questName}\" dialog");
                        continue;
                    }

                    function(match.Groups[0].Value, questTopic);

                    ret = true;
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"cannot find \"{questName}\" dialog");
                    continue;
                }
            }
            return ret;
        }

        [GeneratedRegex("(?:Journal|SetJournalIndex)[ ,]+\\\"*([^<>!=]+?)\\\"*[ ,]+(\\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex JournalRegex();

        private void addQuestContainingElement(QuestContainingElement questContainingElement)
        {
            string questId = questContainingElement.QuestId ?? "";

            QuestContainigElements.TryAdd(questId, new());

            QuestContainigElements[questId].TryAdd((uint)questContainingElement.Index!, new());

            var elementList = QuestContainigElements[questId][(uint)questContainingElement.Index!];
            if (!elementList.Any(a => a.Record == questContainingElement.Record))
            {
                elementList.Add(questContainingElement);
            }
        }

        public void FindQuestData()
        {
            foreach (var dialogItem in dataHandler.Dialogs)
            {
                var dialog = dialogItem.Value;
                if (dialog.Type != DialogType.Journal)
                    continue;

                var quest = new QuestHandler(dialogItem.Value);

                if (!this.QuestContainigElements.ContainsKey(dialog.Id))
                    continue;

                var dialogData = this.QuestContainigElements[dialog.Id];

                foreach (var stageItem in quest.Stages)
                {
                    var stage = stageItem.Value;

                    if (!dialogData.ContainsKey(stage.Index))
                        continue;

                    foreach (var element in dialogData[stage.Index])
                    {
                        ScriptBlock scriptBlock;

                        if (element.Type == RecordType.Topic)
                        {
                            stage.AddRequirements(element.Requirements);

                            scriptBlock = new ScriptBlock(((TopicRecord?)element.Record!).Result ?? "");
                        }
                        else if (element.Type == RecordType.Script)
                        {
                            var scriptText = ((ScriptRecord?)element.Record!).Text!;
                            var scriptData = new ScriptData((ScriptRecord?)element.Record!, scriptText);

                            scriptData.BlockData.UpdateToIncludeStartScriptData(dataHandler, this);
                            this.ScriptDataById.TryAdd(scriptData.Id, scriptData);

                            scriptBlock = scriptData.BlockData;
                        }
                        else
                        {
                            continue;
                        }

                        // find journal functions in the script block that represent the script
                        if (scriptBlock.FindJournalFunction(element.QuestId, stage.Index.ToString(), out var journalResults))
                        {
                            foreach (var result in journalResults)
                            {
                                var requirements = result.Requirements ?? new();

                                HashSet<string> processedVars = new(StringComparer.OrdinalIgnoreCase);

                                for (int i = requirements.Count - 1; i >= 0; i--)
                                {
                                    var req = requirements[i];

                                    if (req.Type != RequirementType.CustomLocal || processedVars.Contains(req.Variable ?? "") || req.Object is not null) continue;

                                    var variableData = scriptBlock.GetVariableData(req.Variable!);
                                    if (variableData is null) continue;

                                    // to remove local vars like doOnce
                                    if (variableData.Count == 1 &&
                                        variableData[0].BlockId == requirements.ScriptBlock?.Id &&
                                        !ConditionConverter.CheckCondition((double?)variableData[0].Value, (double?)req.Value, req.Operator))
                                    {
                                        processedVars.Add(req.Variable ?? "");
                                        requirements.RemoveAt(i);
                                        continue;
                                    }

                                    // to replace requirements of vars that to be present once, but not in the same block
                                    if (variableData.Count == 1 &&
                                        variableData[0].BlockId != requirements.ScriptBlock?.Id &&
                                        ConditionConverter.CheckCondition((double?)variableData[0].Value, (double?)req.Value, req.Operator))
                                    {
                                        processedVars.Add(req.Variable ?? "");
                                        requirements.RemoveAt(i);
                                        if (variableData[0].Requirements is not null)
                                        {
                                            foreach (var r in variableData[0].Requirements!)
                                            {
                                                if (!(r.Type == RequirementType.CustomLocal && (processedVars.Contains(req.Variable ?? "") || r.Variable?.ToLower() == req.Variable?.ToLower())))
                                                {
                                                    requirements.Add(r);
                                                }
                                            }
                                            i = requirements.Count; // restart the cycle
                                        }
                                    }
                                }

                                stage.Requirements.Add(requirements);
                            }
                        }
                    }
                }

                for (int i = 0; i < quest.Stages.Count; i++)
                {
                    var stage = quest.Stages.Values[i];
                    
                    if (stage.Requirements.IsContainsRequirementType(RequirementType.PreviousDialogChoice))
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            var stageToVerify = quest.Stages.Values[j];
                            if (!stageToVerify.Requirements.IsContainsRequirementType(RequirementType.PreviousDialogChoice))
                            {
                                stageToVerify.AddNextStage(stage);
                                break;
                            }
                        }
                    }
                    for (int j = i + 1; j < quest.Stages.Count; j++)
                    {
                        var stageToCompare = quest.Stages.Values[j];

                        if (stageToCompare.Requirements.IsContainsJornalIndexRequirement(quest.Id, stage.Index))
                        {
                            stage.AddNextStage(stageToCompare);
                        }
                    }
                }

                if (quest.Stages.Count >= numberOfStagesToBecomeQuest)
                {
                    var stage = quest.Stages.Values[0];
                    var involvedIds = stage.Requirements.GetInvolvedObjectIds();
                    if (involvedIds.Count > 0)
                    {
                        foreach (var involvedId in involvedIds)
                        {
                            quest.Givers.Add(involvedId);
                        }
                    }
                }

                QuestData.TryAdd(dialog.Id, quest);
            }
        }

        public void FindQuestRecord()
        {
            foreach (var rec in dataHandler.RecordsWithScript.Values)
            {
                if (rec.Script is null) continue;

                if (this.QuestObjectIDsWithScript.TryGetValue(rec.Script, out var list))
                {
                    list.Add(rec.Id);
                }
                else
                {
                    this.QuestObjectIDsWithScript.Add(rec.Script, new() { rec.Id });
                }
            }

            foreach (var questData in QuestData.Values)
                foreach(var stage in questData.Stages.Values)
                    foreach(var req in stage.Requirements.SelectMany(a => a))
                    {
                        this.QuestObjects.Add(req.Object, questData, stage.Index, null);

                        var scriptObj = this.QuestObjects.Add(req.Script, questData, stage.Index, QuestObjectType.Script);

                        if (req.Script is not null && this.QuestObjectIDsWithScript.TryGetValue(req.Script!, out var ids))
                        {
                            foreach (var id in ids)
                            {
                                scriptObj?.AddLink(id);
                            }
                        }

                        if (req.ValueStr is not null && !String.Equals(req.ValueStr, "player", StringComparison.OrdinalIgnoreCase))
                        {
                            var valObj = this.QuestObjects.Add(req.ValueStr, questData, stage.Index, req.Type == RequirementType.CustomLocal ? QuestObjectType.Local : null);
                            if (req.Script is not null)
                                valObj?.AddLink(req.Script!);
                            scriptObj?.AddContainedObjectId(req.ValueStr);
                        }

                        if (req.Variable is not null && !String.Equals(req.ValueStr, "player", StringComparison.OrdinalIgnoreCase))
                        {
                            var varObj = this.QuestObjects.Add(req.Variable, questData, stage.Index, req.Type == RequirementType.CustomLocal ? QuestObjectType.Local : null);
                            if (req.Script is not null)
                                varObj?.AddLink(req.Script!);
                            scriptObj?.AddContainedObjectId(req.Variable);
                        }

                    }
            this.QuestObjects.Remove("player");
            this.QuestObjects.Remove("");
            this.QuestObjects.Remove("gold_001");

            foreach (var item in this.QuestObjectIDsWithScript)
            {
                if (this.QuestObjects.TryGetValue(item.Key, out var qObject))
                {
                    foreach (var objId in item.Value)
                    {
                        var qObj = this.QuestObjects.Add(objId, item.Key, qObject, QuestObjectType.Owner);
                        qObj!.AddContainedObjectId(item.Key);
                    }
                }
            }

            dataHandler.IterateItemsInActors(this.QuestObjects, itemInActorsAction);
            dataHandler.IterateItemsInContainers(this.QuestObjects, itemInContainersAction);
        }

        private void itemInActorsAction(string itemId, ActorRecord actorRecord, QuestObject questObject, int itemCount)
        {
            this.QuestObjects.Add(actorRecord.Id, itemId, questObject, QuestObjectType.Owner);
        }

        private void itemInContainersAction(string itemId, ContainerRecord containerRecord, QuestObject questObject, int itemCount)
        {
            this.QuestObjects.Add(containerRecord.Id, itemId, questObject, QuestObjectType.Owner);
        }

        private void cellRefAction(CellRecord cell, CellReference reference, QuestObject questObject)
        {
            QuestObjectPositionsByCell.Add(cell, reference, questObject);
        }

        public void FindQuestObjectPositions()
        {
            dataHandler.IterateObjectPositionsFromCells(this.QuestObjects, cellRefAction);
        }

        public void FixQuestObjectData()
        {
            foreach (var questKey in this.QuestData.Keys)
            {
                if (this.QuestObjects.TryGetValue(questKey, out var dialogObject))
                {
                    dialogObject.Type = QuestObjectType.Dialog;
                }
            }

            foreach (var scriptItem in this.QuestObjectIDsWithScript)
            {
                if (this.QuestObjects.TryGetValue(scriptItem.Key, out var qObj))
                {
                    qObj.Type = QuestObjectType.Script;
                }
            }

            // calculating the total number of object locations
            foreach (var obj in this.QuestObjects.Values)
            {
                foreach (var linkedObjId in obj.Links)
                {
                    if (!this.QuestObjects.TryGetValue(linkedObjId, out var linkedObj)) continue;

                    if (linkedObj.Type != QuestObjectType.Object && linkedObj.Type != QuestObjectType.Owner) continue;

                    obj.TotalCount += linkedObj.Positions.Count;
                }
            }
        }

        public void FindVariables()
        {
            foreach (var scriptItem in this.ScriptDataById)
            {
                var scriptId = scriptItem.Key;
                var script = scriptItem.Value;

                var variables = script.BlockData.VariableData;
                if (variables is null) continue;

                foreach (var variableItem in variables)
                {
                    var variableName = variableItem.Key;
                    var variableList = variableItem.Value;

                    if (variableList.Type != ScriptVariableType.Global)
                    {
                        if (!this.QuestObjects.ContainsKey(scriptId)) continue;

                        this.VariablesByScriptId.TryAdd(scriptId, new(StringComparer.OrdinalIgnoreCase));
                        this.VariablesByScriptId[scriptId].Add(variableName, variableList);
                    }
                    else
                    {
                        // check if it is a local variable of an object
                        var match = LocVariableRegex().Match(variableName);
                        if (match.Success)
                        {
                            var ownerId = match.Groups[1].Value;
                            var varId = match.Groups[2].Value;

                            if (!this.QuestObjects.TryGetValue(ownerId, out var ownerObject))
                            {
                                var type = QuestObjectType.Owner;
                                if (this.dataHandler.Scripts.ContainsKey(ownerId))
                                {
                                    type = QuestObjectType.Script;
                                }

                                ownerObject = this.QuestObjects.Add(ownerId, type);
                            }

                            if (!this.QuestObjects.TryGetValue(varId, out var varObject))
                            {
                                varObject = this.QuestObjects.Add(varId, QuestObjectType.Local);
                            }

                            if (ownerObject is null || varObject is null) continue;

                            ownerObject.AddContainedObjectId(varId);
                            varObject.AddLink(ownerId);

                            this.VariablesByScriptId.TryAdd(ownerId, new(StringComparer.OrdinalIgnoreCase));
                            if (!this.VariablesByScriptId[ownerId].TryAdd(varId, variableList))
                            {
                                this.VariablesByScriptId[ownerId][varId].AddRange(variableList);
                            }
                        }
                        else
                        {
                            this.GlobalVariables.TryAdd(variableName, new());
                            this.GlobalVariables[variableName].AddRange(variableList);
                        }
                    }
                }
            }
        }

        [GeneratedRegex("[\\\"]*([^\\\"]+)[\\\"]*[.](\\S+)")]
        private static partial Regex LocVariableRegex();
    }

}
