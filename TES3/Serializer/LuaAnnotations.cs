using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Serializer
{
    internal partial class CustomSerializer
    {
        public static string LuaAnnotations = @"
---@class questDataGenerator.requirementData
---@field type string
---@field operator integer
---@field value number|string|nil
---@field variable string|nil
---@field object string|nil
---@field skill integer|nil
---@field attribute integer|nil
---@field script string|nil

---@alias questDataGenerator.requirementBlock questDataGenerator.requirementData[]

---@class questDataGenerator.stageData
---@field id string
---@field requirements questDataGenerator.requirementBlock[]
---@field next integer[]
---@field nextIndex integer|nil
---@field finished boolean|nil
---@field restart boolean|nil


---@alias questDataGenerator.questData { name: string, [string]: questDataGenerator.stageData }

---@alias questDataGenerator.quests table<string, questDataGenerator.questData>


---@class questDataGenerator.questTopicInfo
---@field id string
---@field index integer

---@alias questDataGenerator.questByTopicText table<string, questDataGenerator.questTopicInfo[]>

--- 1 - object, 2 - owner, 3 - dialog, 4 - script, 5 - local variable
--- @alias questDataGenerator.questObjectType integer

---@class questDataGenerator.objectPosition
---@field type questDataGenerator.questObjectType|nil
---@field pos number[]
---@field name string|nil
---@field grid integer[]|nil
---@field id string|nil

---@alias questDataGenerator.questObjectPositions table<string, questDataGenerator.objectPosition[]>

---@class questDataGenerator.objectInfo
---@field type questDataGenerator.questObjectType
---@field inWorld integer
---@field total integer
---@field starts string[]|nil
---@field stages questDataGenerator.questTopicInfo[]
---@field positions questDataGenerator.objectPosition[]
---@field links string[]|nil
---@field contains string[]|nil

---@class questDataGenerator.localVariableData
---@field type integer
---@field results table<string, questDataGenerator.requirementBlock[]>

---@alias questDataGenerator.localVariableByQuestId table<string, table<string, questDataGenerator.localVariableData>>
";
    }
}
