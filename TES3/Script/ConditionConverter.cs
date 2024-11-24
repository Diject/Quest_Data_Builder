using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Script
{
    internal static partial class ConditionConverter
    {
        public static HashSet<string> UnfoundCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool CheckCondition(int? a, int? b, SCVROperator oper)
        {
            return (oper == SCVROperator.Equal && a == b) ||
                (oper == SCVROperator.NotEqual && a != b) ||
                (oper == SCVROperator.Greater && a > b) ||
                (oper == SCVROperator.GreaterOrEqual && a >= b) ||
                (oper == SCVROperator.Less && a < b) ||
                (oper == SCVROperator.LessOrEqual && a <= b);
        }

        public static bool CheckCondition(double? a, double? b, SCVROperator oper)
        {
            return (oper == SCVROperator.Equal && a == b) ||
                (oper == SCVROperator.NotEqual && a != b) ||
                (oper == SCVROperator.Greater && a > b) ||
                (oper == SCVROperator.GreaterOrEqual && a >= b) ||
                (oper == SCVROperator.Less && a < b) ||
                (oper == SCVROperator.LessOrEqual && a <= b);
        }

        public static SCVROperator? StringToSCVROperator(string str)
        {
            switch (str)
            {
                case "==":
                    return SCVROperator.Equal;
                case "!=":
                    return SCVROperator.NotEqual;
                case ">":
                    return SCVROperator.Greater;
                case "<":
                    return SCVROperator.Less;
                case "<=":
                    return SCVROperator.LessOrEqual;
                case ">=":
                    return SCVROperator.GreaterOrEqual;
                default:
                    return null;
            }
        }

        public static QuestRequirement? ConditionToRequirement(string conditionStr)
        {
            var match = conditionRegex().Match(conditionStr.Replace("\t", String.Empty));

            if (!match.Success)
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"Undetected condition: \"{conditionStr}\"");
                return null;
            }

            var objectStr = match.Groups[1].Value;
            var commandStr = match.Groups[2].Value.Trim();
            var variableStr = match.Groups[3].Value.Trim();
            var operatorStr = match.Groups[4].Value;
            var valueStr = match.Groups[5].Value;

            bool isObjectPlayer = string.Equals(objectStr, "player", StringComparison.OrdinalIgnoreCase);

            var requirement = new QuestRequirement();

            if (!string.IsNullOrEmpty(objectStr))
            {
                requirement.Object = objectStr.ToLower();
            }

            if (!string.IsNullOrEmpty(commandStr) && conditionData.TryGetValue(commandStr, out var data))
            {
                if (data.TypeForPlayer is not null && isObjectPlayer)
                {
                    requirement.Type = data.TypeForPlayer;
                }
                else if (data.TypeForActor is not null && !string.IsNullOrEmpty(objectStr))
                {
                    requirement.Type = data.TypeForActor;
                }
                else
                {
                    requirement.Type = data.RequirementType;
                }

                if (data.Attribute is not null)
                {
                    requirement.Attribute = data.Attribute;
                }

                if (data.Skill is not null)
                {
                    requirement.Skill = data.Skill;
                }
            }
            else
            {
                if (commandStr is not null)
                {
                    ConditionConverter.UnfoundCommands.Add(commandStr);
                    requirement.Type = RequirementType.CustomLocal;
                    requirement.Variable = commandStr;
                }
                else
                {
                    requirement.Text = conditionStr;
                }
            }

            if (!string.IsNullOrEmpty(variableStr))
            {
                requirement.Variable = variableStr;
            }

            if (string.IsNullOrEmpty(operatorStr))
            {
                requirement.Operator = SCVROperator.Equal;
            }
            else
            {
                var oper = StringToSCVROperator(operatorStr);
                if (oper is null) return null;
                requirement.Operator = (SCVROperator)oper;
            }

            if (string.IsNullOrEmpty(valueStr))
            {
                requirement.Value = 0;
                requirement.Operator = SCVROperator.Greater;
            }
            else
            {
                if (double.TryParse(valueStr, out var value))
                {
                    requirement.Value = value;
                }
                else
                {
                    requirement.ValueStr = valueStr;
                }
            }

            if (requirement.Text is not null)
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"Unfound condition: \"{conditionStr}\"");
            }

            return requirement;
        }

        [GeneratedRegex("(?:[\" ]*([^<>!=]+?)[\" ]*(?:->|\\.))*[( ]*([^,\"!=<> ]+)[(), $]*(?:[ \"]*([^\"!=<>$]+)[\" )]*)*[() $]*(?:([!=><]+))?[ (]*(?:([^)( ]+))*[ )]*", RegexOptions.IgnoreCase)]
        private static partial Regex conditionRegex();
    }
}
