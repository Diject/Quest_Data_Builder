using Luaon.Linq;
using Quest_Data_Builder.TES3.Cell;
using Quest_Data_Builder.TES3.Quest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Serializer
{
    internal static class LuaSerializer
    {
        public static string TopicByName(Quests dataHandler)
        {
            var table = new LTable();

            foreach (var questItem in dataHandler)
            {
                foreach (var stageItem in questItem.Value.Stages)
                {
                    var subTable = new LTable();
                    subTable.Add("quest", questItem.Key.ToLower());
                    subTable.Add("id", stageItem.Value.Id);
                    subTable.Add("index", stageItem.Value.Index);
                    table.Add(stageItem.Value.Text, subTable);
                }
            }

            return "return " + table.ToString();
        }

        public static string QuestData(Quests dataHandler)
        {
            var table = new LTable();

            foreach (var questItem in dataHandler)
            {
                var questTable = new LTable();

                if (questItem.Value.Name is not null)
                {
                    questTable.Add("name", questItem.Value.Name);
                }

                var stages = questItem.Value.Stages;
                foreach (var stageItem in stages)
                {
                    var stageTable = new LTable();

                    stageTable.Add("id", stageItem.Value.Id);

                    if (stageItem.Value.IsFinished)
                        stageTable.Add("finished", true);

                    if (stageItem.Value.IsRestart)
                        stageTable.Add("restart", true);

                    var requirementsTable = new LTable();
                    foreach (var requirements in stageItem.Value.Requirements)
                    {
                        var requirementTable = new LTable();
                        foreach (var requirement in requirements)
                        {
                            var subTable = new LTable();
                            subTable.Add("type", requirement.Type);
                            subTable.Add("operator", (int)requirement.Operator);

                            if (requirement.Value is not null)
                                subTable.Add("value", requirement.Value);
                            else if (requirement.ValueStr is not null)
                                subTable.Add("value", requirement.ValueStr.ToLower());

                            if (requirement.Variable is not null)
                                subTable.Add("variable", requirement.Variable.ToLower());

                            if (requirement.Object is not null)
                                subTable.Add("object", requirement.Object.ToLower());

                            if (requirement.Attribute is not null)
                                subTable.Add("attribute", requirement.Attribute);

                            if (requirement.Skill is not null)
                                subTable.Add("skill", requirement.Skill);

                            if (requirement.Script is not null)
                                subTable.Add("script", requirement.Script);

                            requirementTable.Add(subTable);
                        }

                        requirementsTable.Add(requirementTable);
                    }
                    stageTable.Add("requirements", requirementsTable);

                    var nextIdsTable = new LTable();
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

                    questTable.Add(stageItem.Value.Index, stageTable);
                }
                table.Add(questItem.Value.Id.ToLower(), questTable);
            }
            return "return " + table.ToString();
        }

        public static string TopicById(Quests dataHandler)
        {
            var table = new LTable();

            foreach (var questItem in dataHandler)
            {
                foreach (var stageItem in questItem.Value.Stages)
                {
                    var subTable = new LTable();
                    subTable.Add("quest", questItem.Key.ToLower());
                    subTable.Add("index", stageItem.Value.Index);
                    table.Add(stageItem.Value.Id, subTable);
                }
            }

            return "return " + table.ToString();
        }

        public static string QuestByTopicText(Quests dataHandler)
        {
            var table = new LTable();

            foreach (var questItem in dataHandler)
            {
                foreach (var stage in questItem.Value.Stages.Values)
                {
                    var subTable = new LTable();
                    subTable.Add("quest", questItem.Key.ToLower());
                    subTable.Add("index", stage.Index);
                    table.Add(stage.Text, subTable);
                }
            }

            return "return " + table.ToString();
        }

        public static string QuestObjects(QuestDataHandler dataHandler)
        {
            var questObjects = dataHandler.QuestObjects;

            var table = new LTable();

            foreach (var objectItem in questObjects)
            {
                var objectTable = new LTable();
                objectTable.Add("type", (int)objectItem.Value.Type);

                var stagesTable = new LTable();
                foreach (var questInfo in objectItem.Value.InvolvedQuestStages)
                {
                    var subTable = new LTable();
                    subTable.Add("id", questInfo.Item1.ToLower());
                    subTable.Add("index", questInfo.Item2);
                    stagesTable.Add(subTable);
                }
                objectTable.Add("stages", stagesTable);

                table.Add(objectItem.Key.ToLower(), objectTable);
            }

            return "return " + table.ToString();
        }

        public static string QuestObjectPositionsInCell(QuestDataHandler dataHandler)
        {
            var questPoss = dataHandler.QuestObjectPositionsByCell;

            var table = new LTable();

            foreach (var cellDataItem in questPoss)
            {
                var cellTable = new LTable();
                foreach (var objectDataItem in cellDataItem.Value)
                {
                    var objectDataTable = new LTable();

                    foreach (var objPos in objectDataItem.Value)
                    {
                        var objPosTable = new LTable();
                        objPosTable.Add("type", (int)objPos.ObjectType);

                        if (objPos.OriginId is not null)
                            objPosTable.Add("id", objPos.ObjectId);

                        var positionTable = new LTable();
                        positionTable.Add(objPos.Position.X);
                        positionTable.Add(objPos.Position.Y);
                        positionTable.Add(objPos.Position.Z);
                        objPosTable.Add("position", positionTable);
                        if (objPos.GridPosition is not null)
                        {
                            var gridTable = new LTable();
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

            return "return " + table.ToString();
        }

        public static string QuestObjectPositions(QuestDataHandler dataHandler)
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

            var table = new LTable();

            foreach (var positionsItem in questObjectsById)
            {
                if (positionsItem.Value.Count > 10) continue;

                var subTable = new LTable();
                foreach (var pos in positionsItem.Value)
                {
                    var objPosTable = new LTable();
                    objPosTable.Add("type", (int)pos.ObjectType);

                    if (pos.OriginId is not null)
                        objPosTable.Add("id", pos.ObjectId);

                    var positionTable = new LTable();
                    positionTable.Add(pos.Position.X);
                    positionTable.Add(pos.Position.Y);
                    positionTable.Add(pos.Position.Z);
                    objPosTable.Add("position", positionTable);
                    if (pos.GridPosition is not null)
                    {
                        var gridTable = new LTable();
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
                table.Add(positionsItem.Key, subTable);
            }

            return "return " + table.ToString();
        }


        public static string ObjectIdsByScript(QuestDataHandler dataHandler)
        {
            var data = dataHandler.QuestObjectIDsWithScript;
            var table = new LTable();

            foreach (var dataItem in data)
            {
                var subTable = new LTable();
                foreach (var objId in dataItem.Value)
                {
                    subTable.Add(objId.ToLower());

                }
                table.Add(dataItem.Key.ToLower(), subTable);
            }

            return "return " + table.ToString();
        }

    }
}
