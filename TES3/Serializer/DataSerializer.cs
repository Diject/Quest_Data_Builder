using Luaon.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quest_Data_Builder.Config;
using Quest_Data_Builder.Extentions;
using Quest_Data_Builder.TES3.Cell;
using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using Quest_Data_Builder.TES3.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Quest_Data_Builder.TES3.Serializer
{
    public enum SerializerType
    {
        Lua,
        Json,
        Yaml
    }

    internal partial class DataSerializer
    {
        private readonly SerializerType _type;
        private readonly QuestDataHandler dataHandler;
        private readonly CustomSerializer _serializer;

        public DataSerializer(SerializerType type, QuestDataHandler dataHandler)
        {
            _type = type;
            this.dataHandler = dataHandler;
            _serializer = new CustomSerializer(type);
        }

        private dynamic newTable()
        {
            return _serializer.NewTable();
        }

        private dynamic newArray()
        {
            return _serializer.NewArray();
        }

        private string getResult(dynamic obj)
        {
            return _serializer.GetResult(obj);
        }


        public string QuestObjectPositionsInCell()
        {
            var questPoss = dataHandler.QuestObjectPositionsByCell;

            var table = newTable();

            foreach (var cellDataItem in questPoss)
            {
                var cellTable = newTable();
                foreach (var objectDataItem in cellDataItem.Value)
                {
                    var objectDataTable = newArray();

                    foreach (var objPos in objectDataItem.Value)
                    {
                        var objPosTable = newTable();
                        objPosTable.Add("type", (int)objPos.ObjectType);

                        if (objPos.OriginId is not null)
                            objPosTable.Add("id", objPos.ObjectId.ToLower());

                        var positionTable = newArray();
                        positionTable.Add(objPos.Position.X);
                        positionTable.Add(objPos.Position.Y);
                        positionTable.Add(objPos.Position.Z);
                        objPosTable.Add("position", positionTable);
                        if (objPos.GridPosition is not null)
                        {
                            var gridTable = newArray();
                            gridTable.Add(objPos.GridPosition.Item1);
                            gridTable.Add(objPos.GridPosition.Item2);
                            objPosTable.Add("grid", gridTable);
                        }
                        objectDataTable.Add(objPosTable);
                    }

                    cellTable.Add(objectDataItem.Key.ToLower(), objectDataTable);
                }

                table.Add(cellDataItem.Key.ToLower(), cellTable);
            }

            return getResult(table);
        }

        public string TopicByName()
        {
            var data = dataHandler.QuestData;
            var table = newTable();

            foreach (var questItem in data)
            {
                foreach (var stageItem in questItem.Value.Stages)
                {
                    var subTable = newTable();
                    subTable.Add("quest", questItem.Key.ToLower());
                    subTable.Add("id", stageItem.Value.Id.ToLower());
                    subTable.Add("index", stageItem.Value.Index);
                    table.Add(stageItem.Value.Text, subTable);
                }
            }

            return getResult(table);
        }


        private dynamic serializeRequirement(QuestRequirementList requirements, bool? ignoreUnnecessary)
        {
            var requirementTable = newArray();
            foreach (var requirement in requirements)
            {
                if (ignoreUnnecessary == true && requirement.Type == RequirementType.CustomScript)
                {
                    continue;
                }

                var subTable = newTable();
                subTable.Add("type", requirement.Type);
                subTable.Add("operator", (int)requirement.Operator);

                if (requirement.Type == RequirementType.CustomActor && requirement.Dialogue is not null)
                {
                    subTable.Add("value", requirement.Dialogue.Value.TopicId.ToLower());
                    subTable.Add("variable", Consts.DialoguePrefix + requirement.Dialogue.Value.Id.ToLower());
                }
                else
                {
                    if (requirement.Value is not null)
                        subTable.Add("value", requirement.Value);
                    else if (requirement.ValueStr is not null)
                        subTable.Add("value", requirement.ValueStr.ToLower());

                    if (requirement.Variable is not null)
                        subTable.Add("variable", requirement.Variable.ToLower());
                }

                if (requirement.Object is not null)
                    subTable.Add("object", requirement.Object.ToLower());

                if (requirement.Attribute is not null)
                    subTable.Add("attribute", requirement.Attribute);

                if (requirement.Skill is not null)
                    subTable.Add("skill", requirement.Skill);

                if (ignoreUnnecessary != true && requirement.Script is not null)
                    subTable.Add("script", requirement.Script.ToLower());

                if (requirement.Text is not null)
                    subTable.Add("text", requirement.Text);

                requirementTable.Add(subTable);
            }

            return requirementTable;
        }


        public string QuestData()
        {
            var data = dataHandler.QuestData;

            Dictionary<string, List<QuestHandler>> questByName = new(StringComparer.OrdinalIgnoreCase);
            foreach (var q in data.Values)
            {
                if (q.Name is null) continue;
                if (!questByName.TryGetValue(q.Name, out var list))
                {
                    list = new List<QuestHandler>();
                    questByName[q.Name] = list;
                }

                list.Add(q);
            }

            var table = newTable();

            foreach (var questItem in data)
            {
                var questTable = newTable();

                if (questItem.Value.Name is not null)
                {
                    questTable.Add("name", questItem.Value.Name);
                }

                if (questItem.Value.Name is not null && questByName.TryGetValue(questItem.Value.Name, out var linked) &&
                    linked.Count > 1)
                {
                    var linkedArr = newArray();
                    foreach (var q in linked)
                    {
                        if (q.Id != questItem.Value.Id)
                        {
                            linkedArr.Add(q.Id.ToLower());
                        }
                    }
                    questTable.Add("links", linkedArr);
                }

                if (questItem.Value.Stages.Any(a => a.Value.IsFinished == true))
                {
                    questTable.Add("hasFinished", true);
                }

                var stages = questItem.Value.Stages;
                foreach (var stageItem in stages)
                {
                    var stageTable = newTable();

                    stageTable.Add("id", stageItem.Value.Id);

                    if (stageItem.Value.IsFinished)
                        stageTable.Add("finished", true);

                    if (stageItem.Value.IsRestart)
                        stageTable.Add("restart", true);

                    var requirementsTable = newArray();
                    foreach (var requirements in stageItem.Value.Requirements)
                    {
                        requirementsTable.Add(serializeRequirement(requirements, false));
                    }
                    stageTable.Add("requirements", requirementsTable);

                    var nextIdsTable = newArray();
                    foreach (var nextStage in stageItem.Value.NextStages)
                    {
                        nextIdsTable.Add(nextStage.Index);
                    }
                    stageTable.Add("next", nextIdsTable);

                    var nextKeyIndex = stages.IndexOfKey(stageItem.Key) + 1;
                    if (nextKeyIndex >= 0 && nextKeyIndex < stages.Values.Count)
                    {
                        stageTable.Add("nextIndex", stages.Values[nextKeyIndex].Index);
                    }

                    questTable.Add(stageItem.Value.Index.ToString(), stageTable);
                }
                table.Add(questItem.Value.Id.ToLower(), questTable);
            }
            return getResult(table);
        }

        public string TopicById()
        {
            var data = dataHandler.QuestData;

            var table = newTable();

            foreach (var questItem in data)
            {
                foreach (var stageItem in questItem.Value.Stages)
                {
                    var subTable = newTable();
                    subTable.Add("id", questItem.Key.ToLower());
                    subTable.Add("index", stageItem.Value.Index);
                    table.Add(stageItem.Value.Id, subTable);
                }
            }

            return getResult(table);
        }

        public string QuestByTopicText()
        {
            var data = dataHandler.QuestData;

            var table = newTable();

            var questsByTopic = new Dictionary<string, List<(string questId, QuestStage stageObj)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var questItem in data)
            {
                foreach (var stage in questItem.Value.Stages.Values)
                {
                    if (questsByTopic.TryGetValue(stage.Text, out var stages))
                    {
                        stages.Add(new (questItem.Key, stage));
                    }
                    else
                    {
                        questsByTopic.Add(stage.Text, new() { new (questItem.Key, stage) });
                    }
                }
            }

            foreach (var topicItem in questsByTopic)
            {
                var subBlock = newArray();
                foreach (var stageData in topicItem.Value)
                {
                    var subTable = newTable();
                    subTable.Add("id", stageData.questId.ToLower());
                    subTable.Add("index", stageData.stageObj.Index);
                    subBlock.Add(subTable);
                }
                table.Add(topicItem.Key, subBlock);
            }

            return getResult(table);
        }

        public string QuestObjects()
        {
            var questObjects = dataHandler.QuestObjects;
            var questPoss = dataHandler.QuestObjectPositionsByCell;

            var objectPositionById = new Dictionary<string, List<QuestObjectPosition>>(StringComparer.OrdinalIgnoreCase);

            foreach (var cellDataItem in questPoss)
            {
                foreach (var objectDataItem in cellDataItem.Value)
                {
                    objectPositionById.TryAdd(objectDataItem.Key, new());
                    foreach (var objPos in objectDataItem.Value)
                    {
                        objectPositionById[objectDataItem.Key].Add(objPos);
                    }
                }
            }

            var table = newTable();

            foreach (var objectItem in questObjects)
            {
                var objectTable = newTable();
                objectTable.Add("type", (int)objectItem.Value.Type);

                if (objectItem.Value.Starts.Count > 0)
                {
                    var starts = newArray();
                    foreach (var quest in objectItem.Value.Starts)
                    {
                        starts.Add(quest.Id.ToLower());
                    }
                    objectTable.Add("starts",  starts);
                }

                var stagesTable = newArray();
                foreach (var questInfo in objectItem.Value.InvolvedQuestStages)
                {
                    var subTable = newTable();
                    subTable.Add("id", questInfo.Item1.ToLower());
                    subTable.Add("index", questInfo.Item2);
                    stagesTable.Add(subTable);
                }
                objectTable.Add("stages", stagesTable);

                if (objectPositionById.TryGetValue(objectItem.Key, out var objPos))
                {
                    var objPosArray = newArray();

                    objPos.Shuffle();
                    for (int i = 0; i < Math.Min(MainConfig.MaxObjectPositions, objPos.Count); i++)
                    {
                        var pos = objPos[i];

                        var objPosTable = newTable();

                        var positionTable = newArray();
                        positionTable.Add(pos.Position.X);
                        positionTable.Add(pos.Position.Y);
                        positionTable.Add(pos.Position.Z);
                        objPosTable.Add("pos", positionTable);
                        if (pos.GridPosition is not null)
                        {
                            var gridTable = newArray();
                            gridTable.Add(pos.GridPosition.Item1);
                            gridTable.Add(pos.GridPosition.Item2);
                            objPosTable.Add("grid", gridTable);
                        }
                        else
                        {
                            objPosTable.Add("name", pos.CellName);
                        }
                        objPosArray.Add(objPosTable);
                    }

                    objectTable.Add("inWorld", objPos.Count);

                    double normalized = 0;
                    foreach (var link in objectItem.Value.Links)
                    {
                        if (questObjects.TryGetValue(link.Key, out var obj))
                        {
                            normalized += link.Value.Chance * obj.TotalCount;
                        }
                    }

                    objectTable.Add("total", objectItem.Value.TotalCount);
                    if (_type == SerializerType.Lua)
                    {
                        objectTable.Add("norm", Math.Round((objPos.Count + normalized), 2));
                    }
                    else
                    {
                        objectTable.Add("norm", Math.Round((decimal)(objPos.Count + normalized), 2));
                    }
                    

                    objectTable.Add("positions", objPosArray);
                }
                else
                {
                    objectTable.Add("inWorld", 0);

                    double normalized = 0;
                    foreach (var itemCount in objectItem.Value.Links.Values)
                        normalized += itemCount.Count * itemCount.Chance;

                    objectTable.Add("total", (int)Math.Round(objectItem.Value.TotalCount + normalized));
                    if (_type == SerializerType.Lua)
                    {
                        objectTable.Add("norm", Math.Round(normalized, 2));
                    }
                    else
                    {
                        objectTable.Add("norm", Math.Round((decimal)normalized, 2));
                    }
                }

                if (objectItem.Value.Contains.Count > 0)
                {
                    var containedArray = newArray();

                    var list = objectItem.Value.Contains.Select(a =>
                        new Tuple<string, decimal, double>(a.Key, Math.Round((decimal)a.Value.Chance, MainConfig.RoundFractionalDigits), a.Value.Chance)).ToList();
                    foreach (var tuple in list.OrderByDescending(a => a.Item3 == 0 ? uint.MaxValue : a.Item2))
                    {
                        var arr = newArray();
                        arr.Add(tuple.Item1.ToLower());
                        if (_type == SerializerType.Lua)
                        {
                            arr.Add((double)tuple.Item2);
                        }
                        else
                        {
                            arr.Add(tuple.Item2);
                        }
                        containedArray.Add(arr);
                    }

                    objectTable.Add("contains", containedArray);
                }

                if (objectItem.Value.Links.Count > 0)
                {
                    var containedArray = newArray();

                    var list = objectItem.Value.Links.Select(a =>
                        new Tuple<string, decimal, double>(a.Key, Math.Round((decimal)a.Value.Chance, MainConfig.RoundFractionalDigits), a.Value.Chance)).ToList();
                    foreach (var tuple in list.OrderByDescending(a => a.Item3 == 0 ? uint.MaxValue : a.Item2))
                    {
                        var arr = newArray();
                        arr.Add(tuple.Item1.ToLower());
                        if (_type == SerializerType.Lua)
                        {
                            arr.Add((double)tuple.Item2);
                        }
                        else
                        {
                            arr.Add(tuple.Item2);
                        }
                        containedArray.Add(arr);
                    }

                    objectTable.Add("links", containedArray);
                }

                table.Add(objectItem.Key.ToLower(), objectTable);
            }

            return getResult(table);
        }

        public string QuestObjectPositions()
        {
            var questPoss = dataHandler.QuestObjectPositionsByCell;

            var questObjectsById = new Dictionary<string, List<QuestObjectPosition>>(StringComparer.OrdinalIgnoreCase);

            foreach (var cellDataItem in questPoss)
            {
                foreach (var objectDataItem in cellDataItem.Value)
                {
                    questObjectsById.TryAdd(objectDataItem.Key, new());
                    foreach (var objPos in objectDataItem.Value)
                    {
                        questObjectsById[objectDataItem.Key].Add(objPos);
                    }
                }
            }

            var table = newTable();

            foreach (var positionsItem in questObjectsById)
            {
                var subTable = newArray();
                foreach (var pos in positionsItem.Value)
                {
                    var objPosTable = newTable();
                    objPosTable.Add("type", (int)pos.ObjectType);

                    if (pos.OriginId is not null)
                        objPosTable.Add("id", pos.ObjectId.ToLower());

                    var positionTable = newArray();
                    positionTable.Add(pos.Position.X);
                    positionTable.Add(pos.Position.Y);
                    positionTable.Add(pos.Position.Z);
                    objPosTable.Add("pos", positionTable);
                    if (pos.GridPosition is not null)
                    {
                        var gridTable = newArray();
                        gridTable.Add(pos.GridPosition.Item1);
                        gridTable.Add(pos.GridPosition.Item2);
                        objPosTable.Add("grid", gridTable);
                    }
                    else
                    {
                        objPosTable.Add("name", pos.CellName);
                    }
                    subTable.Add(objPosTable);
                }
                table.Add(positionsItem.Key.ToLower(), subTable);
            }

            return getResult(table);
        }


        public string ObjectIdsByScript()
        {
            var data = dataHandler.QuestObjectIDsWithScript;

            var table = newTable();

            foreach (var dataItem in data)
            {
                var subTable = newArray();
                foreach (var objId in dataItem.Value)
                {
                    subTable.Add(objId.ToLower());

                }
                table.Add(dataItem.Key.ToLower(), subTable);
            }

            return getResult(table);
        }


        public string LocalVariableDataByScriptId()
        {
            var data = dataHandler.VariablesByScriptId;

            var table = newTable();

            foreach (var scriptItem in data)
            {
                var varsTable = newTable();

                foreach (var varsItem in scriptItem.Value)
                {
                    var varTable = newTable();

                    varTable.Add("type", (int)varsItem.Value.Type);

                    Dictionary<string, List<QuestRequirementList>> results = new();
                    foreach (var varData in varsItem.Value)
                    {
                        if (varData.Requirements is null) continue;

                        results.TryAdd(varData.ValueStr.ToLower(), new());
                        results[varData.ValueStr.ToLower()].Add(varData.Requirements);
                    }


                    var valsTable = newTable();
                    foreach (var valItem in results)
                    {
                        var reqsArray = newArray();
                        foreach (var requirements in valItem.Value)
                        {
                            reqsArray.Add(serializeRequirement(requirements, true));
                        }
                        valsTable.Add(valItem.Key, reqsArray);
                    }

                    varTable.Add("results", valsTable);

                    varsTable.Add(varsItem.Key.ToLower(), varTable);
                }
                table.Add(scriptItem.Key.ToLower(), varsTable);
            }

            return getResult(table);
        }

    }
}
