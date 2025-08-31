using ConcurrentCollections;
using Quest_Data_Builder.Config;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Cell;
using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using Quest_Data_Builder.TES3.Variables;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Quest_Data_Builder.TES3
{
    internal partial class QuestDataHandler
    {
        private readonly RecordDataHandler dataHandler;

        /// <summary>
        /// Data about quest by its id
        /// </summary>
        public Quests QuestData = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Objects involved to quests by id
        /// </summary>
        public QuestObjectById QuestObjects = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// List of elements that contains quest information. Low level. From dialogs and scripts. By quest id; by index
        /// </summary>
        public ConcurrentDictionary<string, ConcurrentDictionary<uint, ConcurrentBag<QuestContainingElement>>> QuestContainigElements = new(StringComparer.OrdinalIgnoreCase);

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
        /// List of topics. By topic id
        /// </summary>
        public ConcurrentDictionary<string, TopicElement> Topics = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Contains data about local variables in scripts or on objects. By script or object id; by variableName name
        /// </summary>
        public ScriptVariables VariablesByScriptId = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Global variables. By variableName name
        /// </summary>
        public ConcurrentDictionary<string, ScriptVariableList> GlobalVariables = new(StringComparer.OrdinalIgnoreCase);




        public QuestDataHandler(RecordDataHandler handler)
        {
            dataHandler = handler;

            try
            {
                this.fillData();
                this.findQuestContainingElements();

                // the order should stay the same
                this.FindQuestData();
                this.FindRewardItems();
                this.FindNextStages();
                this.FindVariables(); // to find global variables
                this.ExpandGlobalVariableRequirements();
                this.FindQuestRecord();
                this.FindVariables(); // to find local variables in scripts and topics
                this.FixRequirementVarialesType();
                this.FindQuestObjectPositions();
                this.FixQuestObjectData();
            }
            catch (Exception ex)
            {
                CustomLogger.RegisterErrorException(ex);
                CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
            }

            try
            {
                if (MainConfig.FindLinksBetweenDialogues)
                {
                    this.findLinksToDialogs();
                }
            }
            catch (Exception ex)
            {
                CustomLogger.RegisterErrorException(ex);
                CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
            }

            try
            {
                if (MainConfig.RemoveUnused)
                {
                    this.RemoveUnused();
                }
            }
            catch (Exception ex)
            {
                CustomLogger.RegisterErrorException(ex);
                CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
            }
        }

        private void findQuestContainingElements()
        {
            Parallel.ForEach(dataHandler.Dialogs.Values, (dialog, state) =>
            {
                lock (dialog)
                {
                    if (this.tryAddToElementsWithAttachedQuest(dialog))
                    {
                        CustomLogger.WriteLine(LogLevel.Info, $"Found info about quest in the dialog record, {dialog.Id}");
                    }
                }
            });

            Parallel.ForEach(dataHandler.Scripts.Values, (script, state) =>
            {
                if (this.tryAddToElementsWithAttachedQuest(script))
                {
                    CustomLogger.WriteLine(LogLevel.Info, $"Found info about quest in the script record, {script.Id}");
                }
            });
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
                        CustomLogger.WriteLine(LogLevel.Info, $"cannot find \"{questIndex}\" index for \"{questName}\" dialog");
                        continue;
                    }

                    function(match.Groups[0].Value, questTopic);

                    ret = true;
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Info, $"cannot find \"{questName}\" dialog");
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
                        ScriptBlock? scriptBlock = null;

                        if (element.Type == RecordType.Topic)
                        {
                            if (this.Topics.TryGetValue(((TopicRecord?)element.Record!).Id, out var topic))
                            {
                                scriptBlock = topic.ScriptBlock;
                            }

                        }
                        else if (element.Type == RecordType.Script)
                        {
                            if (this.ScriptDataById.TryGetValue(((ScriptRecord?)element.Record)!.Id, out var scriptData))
                            {
                                scriptBlock = scriptData.BlockData;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        if (scriptBlock is null) continue;

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


                QuestData.TryAdd(dialog.Id, quest);
            }
        }


        public void FindNextStages()
        {
            foreach (var quest in this.QuestData.Values)
            {
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

                    if (stage.NextStages.Count == 0 && i + 1 < quest.Stages.Count &&
                        quest.Stages.Values[i + 1].Requirements.Count == 0)
                    {
                        for (int j = i + 1; j < quest.Stages.Count; j++)
                        {
                            var stageToCompare = quest.Stages.Values[j];

                            stage.AddNextStage(stageToCompare);

                            if (stageToCompare.Requirements.Count != 0)
                                break;
                        }
                    }
                }

                if (quest.Stages.Count >= MainConfig.StagesNumToAddQuestInfo)
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

            // add objects from requirements
            Parallel.ForEach(QuestData.Values, (questData, state) =>
            {
                lock (questData)
                {
                    foreach (var stage in questData.Stages.Values)
                        foreach (var req in stage.Requirements.SelectMany(a => a))
                        {
                            if (req.Type == RequirementType.CustomDialogue)
                            {
                                this.QuestObjects.Add(req.Variable, questData, stage.Index, QuestObjectType.Dialog);
                                continue;
                            }

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

                            if (req.Variable is not null && !String.Equals(req.Variable, "player", StringComparison.OrdinalIgnoreCase))
                            {
                                var varObj = this.QuestObjects.Add(req.Variable, questData, stage.Index, req.Type == RequirementType.CustomLocal ? QuestObjectType.Local : null);
                                if (req.Script is not null)
                                    varObj?.AddLink(req.Script!);
                                scriptObj?.AddContainedObjectId(req.Variable);
                            }

                        }
                }
            });
            this.QuestObjects.Remove("player", out var plObj);
            this.QuestObjects.Remove("", out var emptyObj);
            this.QuestObjects.Remove("gold_001", out var goldObj);

            foreach (var item in this.QuestObjectIDsWithScript)
            {
                if (this.QuestObjects.TryGetValue(item.Key, out var qObject))
                {
                    foreach (var objId in item.Value)
                    {
                        var qObj = this.QuestObjects.Add(objId, item.Key, qObject, QuestObjectType.Owner, null);
                        qObj!.AddContainedObjectId(item.Key);
                    }
                }
            }

            dataHandler.IterateItemsInActors(this.QuestObjects, itemInActorsAction);
            dataHandler.IterateItemsInContainers(this.QuestObjects, itemInContainersAction);
        }

        private void itemInActorsAction(string itemId, ActorRecord actorRecord, QuestObject questObject, ItemCount carriedItem)
        {
            this.QuestObjects.Add(actorRecord.Id, itemId, questObject, QuestObjectType.Owner, carriedItem);
        }

        private void itemInContainersAction(string itemId, ContainerRecord containerRecord, QuestObject questObject, ItemCount carriedItem)
        {
            this.QuestObjects.Add(containerRecord.Id, itemId, questObject, QuestObjectType.Owner, carriedItem);
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
            foreach (var scriptId in this.dataHandler.Scripts.Keys)
            {
                if (this.QuestObjects.TryGetValue(scriptId, out var qObj))
                {
                    qObj.Type = QuestObjectType.Script;
                }
            }

            // calculating the total number of object locations
            foreach (var obj in this.QuestObjects.Values)
            {
                obj.TotalCount = obj.Positions.Count;
                foreach (var linkedObjId in obj.Links.Keys)
                {
                    if (!this.QuestObjects.TryGetValue(linkedObjId, out var linkedObj)) continue;

                    if (linkedObj.Type != QuestObjectType.Object && linkedObj.Type != QuestObjectType.Owner) continue;

                    obj.TotalCount += linkedObj.Positions.Count;
                }
            }
        }

        public void FindVariables()
        {
            bool? processGlobal(string variableName, ScriptVariableList variableList, string? scriptId = null)
            {
                bool result = false;
                // check if it is a local variableName of an object
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

                    if (ownerObject is null || varObject is null) return null;

                    ownerObject.AddContainedObjectId(varId);
                    varObject.AddLink(ownerId);

                    if (scriptId is not null)
                    {
                        if (!this.QuestObjects.TryGetValue(scriptId, out var scrObject))
                        {
                            scrObject = this.QuestObjects.Add(scriptId, QuestObjectType.Script);
                        }
                        if (scrObject is not null)
                        {
                            varObject.AddLink(scriptId);
                            scrObject.AddContainedObjectId(varId);
                        }
                    }

                    this.VariablesByScriptId.TryAdd(ownerId, new(StringComparer.OrdinalIgnoreCase));


                    if (!this.VariablesByScriptId[ownerId].TryAdd(varId, variableList))
                    {
                        this.VariablesByScriptId[ownerId][varId].AddRange(variableList);
                    }

                    result = true;
                }
                else
                {
                    if (this.GlobalVariables.TryGetValue(variableName, out var varList))
                    {
                        this.GlobalVariables[variableName].AddRange(variableList);
                    }
                }

                return result;
            }


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

                    //variableList.AddRequirements(script.BlockData.GetRequirements());

                    if (variableList.Type != ScriptVariableType.Global)
                    {
                        if (!this.QuestObjects.ContainsKey(scriptId)) continue;

                        this.VariablesByScriptId.TryAdd(scriptId, new(StringComparer.OrdinalIgnoreCase));
                        this.VariablesByScriptId[scriptId].TryAdd(variableName, variableList);
                    }
                    else
                    {
                        processGlobal(variableName, variableList, scriptId);
                    }
                }
            }

            foreach (var topic in this.Topics.Values)
            {
                if (topic.ScriptBlock is null) continue;

                var variables = topic.ScriptBlock.VariableData;
                if (variables is null) continue;

                foreach (var variableItem in variables)
                {
                    var variableName = variableItem.Key;
                    var variableList = variableItem.Value;

                    variableList.AddRequirements(topic.ScriptBlock.GetRequirements());

                    if (processGlobal(variableName, variableList) == false)
                    {
                        if (topic.Record.Actor is not null && topic.Name is not null)
                        {
                            var actorHandler = this.QuestObjects.Add(topic.Record.Actor, QuestObjectType.Object);
                            var qObjectHandler = this.QuestObjects.Add(variableName, QuestObjectType.Local);
                            var dialogueHandler = this.QuestObjects.Add(Consts.DialoguePrefix + topic.Name, QuestObjectType.Dialog);
                            var topicHandler = this.QuestObjects.Add(topic.Id, QuestObjectType.Topic);

                            dialogueHandler!.AddContainedObject(topicHandler!);
                            topicHandler!.AddLink(qObjectHandler!);

                            qObjectHandler!.AddLink(actorHandler!);
                            qObjectHandler!.AddContainedObject(dialogueHandler!);

                            this.VariablesByScriptId.TryAdd(topic.Record.Actor, new(StringComparer.OrdinalIgnoreCase));
                            this.VariablesByScriptId[topic.Record.Actor].TryAdd(variableName, variableList);
                        }
                    }
                }
            }
        }

        [GeneratedRegex("[\\\"]*([^\\\"]+)[\\\"]*[.](\\S+)")]
        private static partial Regex LocVariableRegex();


        private void findLinksToDialogs()
        {
            ConcurrentDictionary<string, ConcurrentHashSet<string>> actorsDialogues = new(StringComparer.OrdinalIgnoreCase);

            ConcurrentDictionary<string, DialogRecord> questDialogueObjects = new(StringComparer.OrdinalIgnoreCase);

            foreach (var dialog in this.QuestObjects)
            {
                if (dialog.Value.Type == QuestObjectType.Dialog &&
                    dataHandler.Dialogs.TryGetValue(dialog.Key.Substring(Consts.DialoguePrefix.Length), out var diaRecord))
                {
                    questDialogueObjects.TryAdd(diaRecord.Id, diaRecord);
                }
            }

            ConcurrentDictionary<string, ConcurrentBag<TopicRecord>> dialogueData = new(StringComparer.OrdinalIgnoreCase);
            foreach (var diaRecord in dataHandler.Dialogs.Values)
            {
                if (diaRecord.Type != DialogType.RegularTopic && diaRecord.Type != DialogType.Greeting) continue;

                dialogueData.TryAdd(diaRecord.Id, new());

                foreach (var topicRecord in diaRecord.Topics)
                {
                    if (topicRecord.Actor is null) continue;
                    dialogueData[diaRecord.Id].Add(topicRecord);

                    if (!actorsDialogues.TryGetValue(topicRecord.Actor, out var actDialogues))
                    {
                        ConcurrentHashSet<string> diaSet = new(StringComparer.OrdinalIgnoreCase);
                        actorsDialogues.TryAdd(topicRecord.Actor, diaSet);
                        actDialogues = diaSet;
                    }
                    actDialogues.Add(diaRecord.Id);
                }
            }

            QuestObjectById dialogueObjects = new(StringComparer.OrdinalIgnoreCase);

            Parallel.ForEach(dialogueData, (diaDataItem, state) =>
            {
                var diaId = diaDataItem.Key;

                Parallel.ForEach(diaDataItem.Value, (topic, state) =>
                {
                    string? topicResult;
                    string? topicResponse;
                    string? topicActor;
                    string topicId = topic.Id;
                    lock (topic)
                    {
                        topicResult = topic.Result;
                        topicResponse = topic.Response;
                        topicActor = topic.Actor;
                    }

                    if (!String.IsNullOrEmpty(topicResult))
                    {
                        var matches = AddTopicRegex().Matches(topicResult);
                        foreach (Match match in matches)
                        {
                            var dialogId = match.Groups[1].Value.Trim();

                            var parentDia = dialogueObjects.Add(Consts.DialoguePrefix + diaId, QuestObjectType.Dialog);
                            if (parentDia is null) break;

                            var parentTopic = dialogueObjects.Add(topicId, QuestObjectType.Topic);
                            if (parentTopic is null) break;

                            var childDia = dialogueObjects.Add(Consts.DialoguePrefix + dialogId, QuestObjectType.Dialog);
                            if (childDia is null) continue;

                            var owner = dialogueObjects.Add(topicActor, QuestObjectType.Object);
                            if (owner is null) continue;

                            parentDia.AddContainedObject(parentTopic);
                            parentTopic.AddLink(owner);
                            parentTopic.AddLink(parentDia);
                            parentTopic.AddContainedObject(childDia);
                            childDia.AddLink(parentTopic);
                        }
                    }

                    if (topicResponse is null || topicActor is null) state.Break();

                    actorsDialogues.TryGetValue(topicActor!, out var actorDialogueIds);
                    if (actorDialogueIds is null) state.Break();

                    foreach (var actorDiaId in actorDialogueIds!)
                    {
                        if (topicResponse!.Contains(actorDiaId, StringComparison.OrdinalIgnoreCase))
                        {
                            var parentDia = dialogueObjects.Add(Consts.DialoguePrefix + diaId, QuestObjectType.Dialog);
                            if (parentDia is null) break;

                            var parentTopic = dialogueObjects.Add(topicId, QuestObjectType.Topic);
                            if (parentTopic is null) break;

                            var childDia = dialogueObjects.Add(Consts.DialoguePrefix + actorDiaId, QuestObjectType.Dialog);
                            if (childDia is null) continue;

                            var owner = dialogueObjects.Add(topicActor, QuestObjectType.Object);
                            if (owner is null) continue;

                            parentDia.AddContainedObject(parentTopic);
                            parentTopic.AddLink(owner);
                            parentTopic.AddLink(parentDia);
                            parentTopic.AddContainedObject(childDia);
                            childDia.AddLink(parentTopic);
                        }
                    }
                });
            });

            void addDialogueToQuestObjectData(QuestObject obj, QuestHandler? quest, uint stage, int depth)
            {
                if (depth == 0) return;

                var qObj = quest is null ? this.QuestObjects.Add(obj.ObjectId, obj.Type) :
                    this.QuestObjects.Add(obj.ObjectId, quest, stage, obj.Type);
                if (qObj is null) return;
                if (MainConfig.OptimizeData && (obj.Links.Count + obj.Contains.Count > 40)) return;

                foreach (var linkId in obj.Links.Keys)
                {
                    if (dialogueObjects.TryGetValue(linkId, out var dialogueObject) && qObj.AddLink(linkId))
                    {
                        addDialogueToQuestObjectData(dialogueObject, null, 0, depth - 1);
                    }
                }
                foreach (var linkId in obj.Contains.Keys)
                {
                    if (dialogueObjects.TryGetValue(linkId, out var dialogueObject) && qObj.AddContainedObjectId(linkId))
                    {
                        addDialogueToQuestObjectData(dialogueObject, null, 0, depth - 1);
                    }
                }
            }

            Parallel.ForEach(dialogueObjects, (diaQuestObjectItem, state) =>
            {
                if (this.QuestObjects.TryGetValue(diaQuestObjectItem.Key, out var qObj))
                {
                    addDialogueToQuestObjectData(diaQuestObjectItem.Value, null, 0, MainConfig.DialogueSearchDepth);
                }
            });
        }

        [GeneratedRegex("addtopic \"?([^\"\\n\\r]+)\"?$", RegexOptions.IgnoreCase)]
        private static partial Regex AddTopicRegex();


        private void FindRewardItems()
        {
            void addObject(string itemId, string itemCountStr, string ownerId, QuestObjectType type)
            {
                if (!this.QuestObjects.TryGetValue(itemId, out var qObject)) return;

                int.TryParse(itemCountStr, out var itemCount);

                var diaActorObject = this.QuestObjects.Add(ownerId, itemId, qObject, type, new(itemCount, itemCount));
            }

            // find items in dialogs that are added to the player by AddItem
            Parallel.ForEach(dataHandler.Dialogs.Values, (dialog, state) =>
            {
                lock (dialog)
                {
                    foreach (var topic in dialog.Topics)
                    {
                        if (String.IsNullOrEmpty(topic.Result)) continue;

                        var matches = AddItemRegex().Matches(topic.Result!);
                        foreach (Match match in matches)
                        {
                            var itemId = match.Groups[1].Value;
                            var itemCountStr = match.Groups[2].Value;

                            addObject(itemId, itemCountStr, Consts.DialoguePrefix + dialog.Id, QuestObjectType.Dialog);
                        }
                    }
                }
            });

            // find items in scripts that are added to the player by AddItem
            Parallel.ForEach(dataHandler.Scripts.Values, (script, state) =>
            {
                lock (script)
                {
                    if (String.IsNullOrEmpty(script.Text)) return;
                    var matches = AddItemRegex().Matches(script.Text!);

                    foreach (Match match in matches)
                    {
                        var itemId = match.Groups[1].Value;
                        var itemCountStr = match.Groups[2].Value;

                        addObject(itemId, itemCountStr, script.Id, QuestObjectType.Script);
                    }
                }
            });
        }


        private void RemoveUnused()
        {
            bool checkQuestObject(QuestObject qObj, int depth)
            {
                if (depth == 0) return false;

                if (qObj.InvolvedQuestStages.Count != 0) return true;
                else
                {
                    bool ret = false;
                    foreach (var objId in qObj.Contains.Keys)
                    {
                        if (this.QuestObjects.TryGetValue(objId, out var childObj))
                        {
                            ret |= checkQuestObject(childObj, depth - 1);
                            if (ret) return ret;
                        }
                    }
                    return ret;
                }
            }

            ConcurrentHashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);

            Parallel.ForEach(this.QuestObjects, (qObjIt, state) =>
            {
                var qObj = qObjIt.Value;
                var qObjId = qObjIt.Key;

                if (qObj.InvolvedQuestStages.Count != 0 || checkQuestObject(qObj, 3))
                {
                    usedIds.Add(qObjId);
                }
            });

            foreach (var qObjId in this.QuestObjects.Keys)
            {
                if (!usedIds.Contains(qObjId))
                {
                    this.QuestObjects.Remove(qObjId, out var _);
                    CustomLogger.WriteLine(LogLevel.Info, $"removed unused id: {qObjId}");
                }
            }
        }


        private void fillData()
        {
            foreach (var variableName in this.dataHandler.GlobalVariables.Keys)
            {
                this.GlobalVariables.TryAdd(variableName, new());
            }

            foreach (var script in this.dataHandler.Scripts.Values)
            {
                if (script.Text is null) continue;

                var scriptData = new ScriptData(script, script.Text);

                scriptData.BlockData.UpdateToIncludeStartScriptData(dataHandler, this);
                this.ScriptDataById.TryAdd(scriptData.Id, scriptData);
            }

            foreach (var dialog in this.dataHandler.Dialogs.Values)
            {
                foreach (var topic in dialog.Topics)
                {
                    this.Topics.TryAdd(topic.Id, new(topic));
                }
            }
        }


        public void FixRequirementVarialesType()
        {
            Parallel.ForEach(QuestData.Values, (quest, state) =>
            {
                lock (quest)
                {
                    foreach (var stage in quest.Stages.Values)
                    {
                        foreach (var req in stage.Requirements.SelectMany(a => a))
                        {
                            if ((req.Type == RequirementType.CustomLocal || req.Type == RequirementType.CustomGlobal)
                                && req.Variable is not null)
                            {
                                if (this.GlobalVariables.ContainsKey(req.Variable))
                                    req.Type = RequirementType.CustomGlobal;
                                else
                                    req.Type = RequirementType.CustomLocal;
                            }

                        }
                    }
                }
            });
        }


        public void ExpandGlobalVariableRequirements()
        {
            Parallel.ForEach(QuestData.Values, (quest, state) =>
            {
                lock (quest)
                {
                    foreach (var stage in quest.Stages.Values)
                    {
                        foreach (var reqBlock in stage.Requirements)
                        {
                            QuestRequirementList newReqs = new();
                            foreach (var req in reqBlock)
                            {
                                if (req.Type != RequirementType.CustomGlobal) continue;
                                if (req.Value is null || req.Variable is null) continue;
                                if (!this.GlobalVariables.TryGetValue(req.Variable, out var valList)) continue;

                                foreach (var val in valList)
                                {
                                    if (val.Value is null || val.Requirements is null) continue;
                                    if (Math.Abs((double)val.Value - (double)req.Value) > 0.00001) continue;

                                    newReqs.AddRange(val.Requirements);
                                    // TODO: make it possible to add multiple requirements for the value
                                    break; // add only one requirement for the value, because I'm too lazy to implement this new feature for the Morrowind part
                                }
                            }
                            reqBlock.AddRange(newReqs);
                        }
                    }
                }
            });
        }

    }
}
