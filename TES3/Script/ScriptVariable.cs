using Quest_Data_Builder.TES3.Quest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Script
{
    enum ScriptVariableType
    {
        Unknown = 0,
        Local = 1,
        Global = 2,
    }

    class ScriptVariable
    {
        public string Name;
        public string ScriptId;

        public string ValueStr;
        public double? Value;

        public int BlockId;

        public QuestRequirementList? Requirements;

        public ScriptVariable(string name, string scriptId, string value, QuestRequirementList? requirements)
        {
            Name = name;
            ScriptId = scriptId;
            ValueStr = value;
            Requirements = requirements;
            BlockId = requirements?.ScriptBlock?.Id ?? -1;

            if (double.TryParse(value, out var numberVal))
            {
                Value = numberVal;
            }
        }
    }


    class ScriptVariableList : List<ScriptVariable>
    {
        public ScriptVariableType Type = ScriptVariableType.Unknown;

        public new void Add(ScriptVariable variable)
        {
            if (!this.Contains(variable))
            {
                base.Add(variable);
            }
        }

        public void AddRange(ScriptVariableList list)
        {
            foreach (ScriptVariable variable in list)
            {
                this.Add(variable);
            }
        }
    }

    /// <summary>
    /// by scriptId
    /// </summary>
    class ScriptVariables : Dictionary<string, Dictionary<string, ScriptVariableList>>
    {
        public ScriptVariables(StringComparer comparer) : base(comparer) { }

        public void Add(string scriptId, string variableName, ScriptVariable data)
        {
            if (!this.TryGetValue(scriptId, out var questBlock))
            {
                questBlock = new(StringComparer.OrdinalIgnoreCase);
                this.Add(scriptId, questBlock);
            }
            if (!questBlock.TryGetValue(variableName, out var variableBlock))
            {
                variableBlock = new();
                questBlock.Add(variableName, variableBlock);
            }

            variableBlock.Add(data);
        }

        public void Add(ScriptVariable variable)
        {
            this.Add(variable.ScriptId, variable.Name, variable);
        }
    }
}
