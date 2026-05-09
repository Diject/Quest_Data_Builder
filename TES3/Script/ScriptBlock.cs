using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.TES3.Records;
using System.Text.RegularExpressions;

namespace Quest_Data_Builder.TES3.Script
{
    internal enum ScriptBlockType
    {
        Empty = 0,
        Begin = 1,
        If = 2,
        While = 3,
        Dialog = 4,
    }

    internal partial class ScriptBlock
    {
        private static int _id = 0;
        private int id => _id++;

        public readonly int Id;
        public readonly ScriptBlockType Type = ScriptBlockType.Empty;

        public ScriptBlock? Parent;

        public IReadOnlyList<ScriptBlock> InnerBlocks => innerBlocks.AsReadOnly();
        protected readonly List<ScriptBlock> innerBlocks = new();

        public bool IsContainBlocks => innerBlocks.Count > 0;

        protected readonly string text;

        public string? ScriptId { get; private set; }

        public IReadOnlyList<QuestRequirement>? Requirements => _requirements?.AsReadOnly();
        protected QuestRequirementList? _requirements;

        /// <summary>
        /// Reversed requirements from "if (condition) Return endif" patterns.
        /// Stored only at the head (top-level) block.
        /// </summary>
        public IReadOnlyList<QuestRequirement>? ReturnRequirements => _returnRequirements?.AsReadOnly();
        protected List<QuestRequirement>? _returnRequirements;

        /// <summary>
        /// Dictionary of local script variables.
        /// </summary>
        /// <value>Key is a variable name. Value is a variable type.</value>
        public IReadOnlyDictionary<string, string>? LocalVariables => localVariables?.AsReadOnly();
        protected Dictionary<string, string>? localVariables;

        /// <summary>
        /// Info about values of the variables
        /// </summary>
        /// <value>Key is a variable name</value>
        public Dictionary<string, ScriptVariableList>? VariableData;


        private static readonly Dictionary<string, Tuple<ScriptBlockType, string, bool>> startLabels = new(StringComparer.OrdinalIgnoreCase) {
            {"begin", new(ScriptBlockType.Begin, "end", false)},
            {"if", new(ScriptBlockType.If,"endif|elseif|else", false)},
            {"elseif", new(ScriptBlockType.If,"endif|elseif|else", true)},
            {"else", new(ScriptBlockType.If,"endif|elseif|else", true)},
            {"while", new(ScriptBlockType.While,"endwhile", false)}
        };

        private static readonly Dictionary<string, (ScriptBlockType type, string endLabel, HashSet<string> altLabels)> startLabels1 = new(StringComparer.OrdinalIgnoreCase) {
            {"begin", new(ScriptBlockType.Begin, "end", new())},
            {"_dialog_", new(ScriptBlockType.Dialog, "end", new())},
            {"if", new(ScriptBlockType.If,"endif", new(StringComparer.OrdinalIgnoreCase) {"else", "elseif"})},
            {"while", new(ScriptBlockType.While,"endwhile", new())}
        };

        private static readonly HashSet<string> labelsWithCondition = new(StringComparer.OrdinalIgnoreCase) { "if", "elseif", "while" };

        public ScriptBlock()
        {
            text = "";
            Id = id;
        }

        public ScriptBlock(string scriptText)
        {
            text = scriptText;
            Id = id;

            parseScriptText(text);

            findVariables();
        }

        public ScriptBlock(string scriptText, TopicRecord topic)
        {
            text = scriptText;
            Id = id;

            parseScriptText(text);

            var diaRequirement = new QuestRequirement();
            diaRequirement.Type = RequirementType.CustomDialogue;
            if (topic.Parent is not null)
                diaRequirement.Variable = Consts.DialoguePrefix + topic.Parent.Id;
            diaRequirement.ValueStr = topic.Id;
            this.AddRequirement(diaRequirement);

            findVariables();
        }

        public ScriptBlock(ScriptBlock parent, string scriptText)
        {
            text = scriptText;
            ScriptId = parent.ScriptId;
            Parent = parent;
            Id = id;

            parseScriptText(text);
        }

        public ScriptBlock(ScriptBlock parent, string scriptText, ScriptBlockType type) : this(parent, scriptText)
        {
            Type = type;
        }

        public ScriptBlock(ScriptBlock parent, string scriptText, ScriptBlockType type, QuestRequirement requirement) : this(parent, scriptText, type)
        {
            _requirements ??= new();
            _requirements.Add(requirement);
        }

        public ScriptBlock(ScriptBlock parent, string scriptText, ScriptBlockType type, string conditionString) : this(parent, scriptText, type)
        {

            var requirement = ConditionConverter.ConditionToRequirement(conditionString);
            if (requirement is not null)
            {
                requirement.Script = this.ScriptId;
                AddRequirement(requirement);
            }
        }


        private void parseScriptText(string scriptText)
        {
            var parent = this;
            using var reader = new StringReader(scriptText);

            string blockStr = string.Empty; // contains text for the block that is being processed
            (ScriptBlockType type, string endLabel, HashSet<string> altLabels)? lastLabelInfo = null;
            int startedNumber = 0; // number of blocks of the same type as the detected block. To prevent capturing the ends from wrong blocks
            List<QuestRequirement> lastRequirements = new(); // to track requirements for "else". All requirements from non-end labels will in the list

            for (string? line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                if (BlockLabelRegex(line, out var blockLabelMatch))
                {
                    string label = blockLabelMatch.Groups[1].Value;
                    string labelVariable = blockLabelMatch.Groups[2].Value;

                    if (startLabels1.TryGetValue(label, out var labelInfo)) // the block is started
                    {
                        if (lastLabelInfo is null || labelInfo.type == lastLabelInfo?.type)
                        {
                            startedNumber++;
                        }
                        if (startedNumber > 1 || lastLabelInfo is not null)
                        {
                            blockStr += string.IsNullOrEmpty(blockStr) ? line : Environment.NewLine + line;
                            continue;
                        }

                        if (!string.IsNullOrEmpty(blockStr))
                        {
                            var textBlock = new ScriptBlock(this, blockStr);
                            textBlock.ApplyReturnRequirements();
                            innerBlocks.Add(textBlock);
                            blockStr = string.Empty;
                        }

                        lastLabelInfo = labelInfo;
                        blockStr = string.Empty;

                        // For the first block with a quest name
                        if (string.Equals("begin", label, StringComparison.OrdinalIgnoreCase))
                        {
                            var qReq = new QuestRequirement();
                            qReq.Type = RequirementType.CustomScript;
                            this.ScriptId = labelVariable;
                            qReq.Variable = labelVariable;
                            qReq.Script = labelVariable;
                            AddRequirement(qReq);
                        }

                        if (!string.IsNullOrEmpty(labelVariable) && labelsWithCondition.Contains(label))
                        {
                            var req = ConditionConverter.ConditionToRequirement(labelVariable);
                            if (req is not null)
                            {
                                req.Script = this.ScriptId;
                                lastRequirements.Add(req);
                            }
                        }
                    }
                    else if (lastLabelInfo is not null && string.Equals(label, lastLabelInfo.Value.endLabel, StringComparison.OrdinalIgnoreCase)) // the block is ended
                    {
                        if (--startedNumber > 0)
                        {
                            blockStr += string.IsNullOrEmpty(blockStr) ? line : Environment.NewLine + line;
                            continue;
                        }

                        var block = new ScriptBlock(parent, blockStr, lastLabelInfo.Value.type);

                        block.ApplyReturnRequirements();
                        block.AddRequirements(lastRequirements);

                        // Detect "if (condition) Return endif" pattern
                        if (lastLabelInfo.Value.type == ScriptBlockType.If &&
                            blockStr.Trim().Equals("return", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var req in lastRequirements)
                            {
                                this.AddReturnRequirement(req);
                            }
                        }

                        lastRequirements.Clear();

                        innerBlocks.Add(block);

                        blockStr = string.Empty;

                        lastLabelInfo = null;
                    }
                    else if (lastLabelInfo is not null && lastLabelInfo.Value.altLabels.Contains(label)) // alternative labels for the block are found. Like "else"
                    {
                        if (startedNumber > 1)
                        {
                            blockStr += string.IsNullOrEmpty(blockStr) ? line : Environment.NewLine + line;
                            continue;
                        }

                        var block = new ScriptBlock(parent, blockStr, lastLabelInfo.Value.type);

                        block.ApplyReturnRequirements();
                        block.AddRequirements(lastRequirements);

                        // Reverse operator for the latest requirement. For "else" labels
                        if (lastRequirements.Count > 0)
                        {
                            lastRequirements[lastRequirements.Count - 1].ReverseOperator();
                        }

                        innerBlocks.Add(block);

                        blockStr = string.Empty;

                        if (!string.IsNullOrEmpty(labelVariable) && labelsWithCondition.Contains(label))
                        {
                            var req = ConditionConverter.ConditionToRequirement(labelVariable);
                            if (req is not null)
                            {
                                req.Script = this.ScriptId;
                                lastRequirements.Add(req);
                            }
                        }
                    }
                    else
                    {
                        //CustomLogger.WriteLine(LogLevel.Error, $"Found unknown label of script block, {label}");
                        blockStr += string.IsNullOrEmpty(blockStr) ? line : Environment.NewLine + line;
                    }
                }
                else if (LocalVariableRegex(line, out var localVariableMatch))
                {
                    localVariables ??= new(StringComparer.OrdinalIgnoreCase);
                    localVariables.TryAdd(localVariableMatch.Groups[2].Value, localVariableMatch.Groups[1].Value.ToLower());
                    blockStr += string.IsNullOrEmpty(blockStr) ? line : Environment.NewLine + line;
                }
                else
                {
                    blockStr += string.IsNullOrEmpty(blockStr) ? line : Environment.NewLine + line;
                }
            }

            if (!string.IsNullOrEmpty(blockStr) && innerBlocks.Count > 0)
            {
                var textBlock = new ScriptBlock(this, blockStr);
                textBlock.ApplyReturnRequirements();
                innerBlocks.Add(textBlock);
            }
        }

        private bool BlockLabelRegex(string str, out Match match)
        {
            match = BlockLabelRegex().Match(str);
            return match.Success;
        }

        [GeneratedRegex("^[ \\t]*(begin|if|elseif|else|while|endif|endwhile|end|_dialog_)[ (\"]*(.*?)[ )\"\\t]*$", RegexOptions.IgnoreCase)]
        private static partial Regex BlockLabelRegex();


        private bool LocalVariableRegex(string str, out Match match)
        {
            match = localVariableRegex().Match(str);
            return match.Success;
        }

        [GeneratedRegex("^[ \\t]*(short|long|float)[ ]+?(\\S+)[ \\t]*$", RegexOptions.IgnoreCase)]
        private static partial Regex localVariableRegex();


        private void findVariables()
        {
            if (this.FindSetFunction(out var varResults))
            {
                foreach (var result in varResults)
                {
                    var varName = result.ReturnValues[1]?.Value;
                    var res = result.ReturnValues[2]?.Value.Trim();
                    if (res == null || varName == null) continue;

                    ScriptVariable variable = new(varName, this.ScriptId ?? "", res, result.Requirements);

                    var head = this.GetHead();
                    head!.VariableData ??= new(StringComparer.OrdinalIgnoreCase);
                    head!.VariableData.TryAdd(varName, new());
                    var variableList = head!.VariableData[varName];
                    bool isLocal = head!.LocalVariables is not null && head!.LocalVariables.ContainsKey(varName);
                    variableList.Type = isLocal ? ScriptVariableType.Local : ScriptVariableType.Global;
                    variableList.Add(variable);
                }
            }
        }


        /// <summary>
        /// Returns top level Block
        /// </summary>
        /// <returns></returns>
        public ScriptBlock? GetHead()
        {
            ScriptBlock? head = this;
            for (int i = 0; i < 99; i++)
            {
                if (head.Parent is null)
                {
                    return head;
                }
                else
                {
                    head = head.Parent;
                }
            }
            return head;
        }

        /// <summary>
        /// Returns local variable data of the script attached to this block.
        /// </summary>
        /// <param name="name">Variable name. Case insensitive.</param>
        /// <returns></returns>
        public List<ScriptVariable>? GetVariableData(string name)
        {
            var head = GetHead();
            if (head is null) return null;

            if (head.VariableData is not null && head.VariableData.TryGetValue(name, out var data))
            {
                return data;
            }

            return null;
        }


        /// <summary>
        /// Returns all script blocks in the script, including nested blocks.
        /// </summary>
        public List<ScriptBlock> GetScriptBlocks()
        {
            List<ScriptBlock> ret = new();

            ret.Add(this);

            foreach (var block in this.innerBlocks)
            {
                ret.AddRange(block.GetScriptBlocks());
            }

            return ret;
        }


        /// <summary>
        /// Returns local variable data of the script (it checks nested records too)
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, string>? GetLocalVariables()
        {
            if (this.LocalVariables is not null)
            {
                return this.LocalVariables;
            }

            var recordWithVars = this.innerBlocks.Find(a => a.LocalVariables is not null);
            if (recordWithVars is not null)
            {
                return recordWithVars.LocalVariables;
            }

            if (this.Parent is null)
            {
                return null;
            }
            return Parent.GetLocalVariables();
        }

        private void findBlocakWithLocalVariables(ScriptBlock parent, List<ScriptBlock> ret)
        {
            foreach (var block in parent.InnerBlocks)
            {
                if (block.LocalVariables is not null)
                {
                    ret.Add(block);
                }
                else
                {
                    findBlocakWithLocalVariables(block, ret);
                }
            }
        }

        /// <summary>
        /// Returns true if this block contains nested blocks with local variables
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool ContainsBlocksWithLocalVariables(out List<ScriptBlock> ret)
        {
            ret = new();

            findBlocakWithLocalVariables(this, ret);

            if (ret.Count > 0)
                return true;

            return false;
        }


        public void AddRequirement(QuestRequirement requirement)
        {
            _requirements ??= new();
            _requirements.Add(requirement);
        }

        public void AddRequirements(IEnumerable<QuestRequirement> requirements)
        {
            _requirements ??= new();
            _requirements.AddRange(requirements);
        }

        /// <summary>
        /// Adds a reversed copy of the requirement to the head block's return requirements.
        /// Used when a "if (condition) Return endif" pattern is detected.
        /// </summary>
        public void AddReturnRequirement(QuestRequirement requirement)
        {
            var head = GetHead();
            if (head is null) return;
            head._returnRequirements ??= new();
            var reversed = requirement.Duplicate();
            reversed.ReverseOperator();
            if (!head._returnRequirements.Exists(a => a.Equals(reversed)))
                head._returnRequirements.Add(reversed);
        }

        /// <summary>
        /// Copies return requirements from the head block to this block's requirements.
        /// </summary>
        public void ApplyReturnRequirements()
        {
            var head = GetHead();
            if (head?._returnRequirements is null || head._returnRequirements.Count == 0) return;
            AddRequirements(head._returnRequirements);
        }

        /// <summary>
        /// Add block to inner block
        /// </summary>
        /// <param name="block"></param>
        /// <param name="clone">If true, insert a duplicate of the block</param>
        public void AddBlock(ScriptBlock block, bool clone)
        {
            if (clone)
            {
                ScriptBlock newBlock = new ScriptBlock(block.text);
                this.innerBlocks.Add(newBlock);
            }
            else
            {
                this.innerBlocks.Add(block);
            }
        }

        /// <summary>
        /// Finds a function in the scriptBlock. Params takes a regex string. varStr->functionStr, argumentStr valueStr
        /// </summary>
        /// <param name="functionStr"></param>
        /// <param name="objectStr"></param>
        /// <param name="argumentStr"></param>
        /// <param name="valueStr"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public bool FindFunction(string? functionStr, string? objectStr, string? argumentStr, string? valueStr, 
            out List<(GroupCollection ReturnValues, QuestRequirementList? Requirements, ScriptBlock ScriptBlock)> results)
        {
            results = new();
            return findFunctionInside(functionStr, objectStr, argumentStr, valueStr, results, null);
        }

        private bool findFunctionInside(string? functionStr, string? objectStr, string? argumentStr, string? valueStr,
            List<(GroupCollection ReturnValues, QuestRequirementList? Requirements, ScriptBlock ScriptBlock)> results, QuestRequirementList? pathRequirements)
        {
            pathRequirements ??= new();
            if (this._requirements is not null)
            {
                pathRequirements.AddRange(this._requirements);
            }
            bool ret = false;
            foreach (var block in this.innerBlocks)
            {
                if (block.IsContainBlocks)
                {
                    var pathReq = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        pathReq.AddRange(block._requirements);
                    ret = block.findFunctionInside(functionStr, objectStr, argumentStr, valueStr, results!, pathReq) || ret;
                    continue;
                }

                var matches = Regex.Matches(block.text, $"[( \"]*(?:({objectStr ?? ""})[\" ]*->)*[( ]*?({functionStr ?? ""})[(), \"]+({argumentStr ?? ""})[ ,\"]+(?:to)?[ ,\"]*({valueStr ?? ""})[ )]*",
                    RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    results ??= new();
                    var reqs = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        reqs.AddRange(block._requirements);
                    reqs.ScriptBlock = block;
                    results?.Add((match.Groups, reqs, block));
                    ret = true;
                }
            }

            return ret;
        }

        public bool FindSetFunction(out List<(GroupCollection ReturnValues, QuestRequirementList? Requirements, ScriptBlock ScriptBlock)> results)
        {
            results = new();
            return FindSetFunctionInside(results, null);
        }

        private bool FindSetFunctionInside(List<(GroupCollection ReturnValues, QuestRequirementList? Requirements, ScriptBlock ScriptBlock)> results,
            QuestRequirementList? pathRequirements)
        {
            pathRequirements ??= new();
            if (this._requirements is not null)
            {
                pathRequirements.AddRange(this._requirements);
            }
            bool ret = false;
            foreach (var block in this.innerBlocks)
            {
                if (block.IsContainBlocks)
                {
                    var pathReq = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        pathReq.AddRange(block._requirements);
                    ret = block.FindSetFunctionInside(results!, pathReq) || ret;
                    continue;
                }

                var matches = SetFunctionRegex().Matches(block.text);
                foreach (Match match in matches)
                {
                    results ??= new();
                    var reqs = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        reqs.AddRange(block._requirements);
                    reqs.ScriptBlock = block;
                    results?.Add((match.Groups, reqs, block));
                    ret = true;
                }
            }

            return ret;
        }

        [GeneratedRegex(@"set[ ]+(.+?)[ ]+to[ ]+(.+)", RegexOptions.IgnoreCase)]
        private static partial Regex SetFunctionRegex();


        public bool FindJournalFunction(string? varStr, string? valueStr,
            out List<(GroupCollection ReturnValues, QuestRequirementList? Requirements, ScriptBlock ScriptBlock)> results)
        {
            results = new();
            return FindJournalFunctionInside(varStr, valueStr, results, null);
        }

        private bool FindJournalFunctionInside(string? varStr, string? valueStr,
            List<(GroupCollection ReturnValues, QuestRequirementList? Requirements, ScriptBlock ScriptBlock)> results, QuestRequirementList? pathRequirements)
        {
            pathRequirements ??= new();
            if (this._requirements is not null)
            {
                pathRequirements.AddRange(this._requirements);
            }
            bool ret = false;
            foreach (var block in this.innerBlocks)
            {
                if (block.IsContainBlocks)
                {
                    var pathReq = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        pathReq.AddRange(block._requirements);
                    ret = block.FindJournalFunctionInside(varStr, valueStr, results!, pathReq) || ret;
                    continue;
                }

                var matches = Regex.Matches(block.text, $"(?:SetJournalIndex|Journal)[, (\"]+(?:{varStr})[, )\"]+(?:{valueStr})",
                    RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    results ??= new();
                    var reqs = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        reqs.AddRange(block._requirements);
                    reqs.ScriptBlock = block;
                    results?.Add((match.Groups, reqs, block));
                    ret = true;
                }
            }

            return ret;
        }

        public bool FindChoiceFunction(string? choiceId, out List<(QuestRequirementList? Requirements, ScriptBlock ScriptBlock, string Text)> results)
        {
            results = new();
            return FindChoiceFunctionInside(choiceId, results, null);
        }

        private bool FindChoiceFunctionInside(string? choiceId, List<(QuestRequirementList? Requirements, ScriptBlock ScriptBlock, string Text)> results,
            QuestRequirementList? pathRequirements)
        {
            pathRequirements ??= new();
            if (this._requirements is not null)
            {
                pathRequirements.AddRange(this._requirements);
            }
            bool ret = false;

            if (!this.IsContainBlocks)
            {
                ret = SearchChoiceInText(this.text, choiceId, results, pathRequirements, this) || ret;
            }

            foreach (var block in this.innerBlocks)
            {
                if (block.IsContainBlocks)
                {
                    var pathReq = new QuestRequirementList(pathRequirements);
                    if (block._requirements is not null)
                        pathReq.AddRange(block._requirements);
                    ret = block.FindChoiceFunctionInside(choiceId, results!, pathReq) || ret;
                    continue;
                }
                ret = SearchChoiceInText(block.text, choiceId, results, pathRequirements, block) || ret;
            }
            return ret;
        }

        private static bool SearchChoiceInText(string text, string? choiceId, 
            List<(QuestRequirementList? Requirements, ScriptBlock ScriptBlock, string Text)> results,
            QuestRequirementList pathRequirements, ScriptBlock block)
        {
            bool ret = false;
            var matches = ChoiceRegex().Matches(text);
            foreach (Match match in matches)
            {
                var choiceArgumentsMatches = ChoiceArgumentsRegex().Matches(match.Groups[1].Value);
                foreach (Match choiceArgMatch in choiceArgumentsMatches)
                {
                    if (choiceId is null || choiceArgMatch.Groups[2].Value.Equals(choiceId))
                    {
                        results ??= new();
                        var reqs = new QuestRequirementList(pathRequirements);
                        if (block._requirements is not null)
                            reqs.AddRange(block._requirements);
                        reqs.ScriptBlock = block;
                        results?.Add((reqs, block, choiceArgMatch.Groups[1].Value));
                        ret = true;
                    }
                }
            }
            return ret;
        }

        [GeneratedRegex(@"choice[ ,](.+?)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex ChoiceRegex();

        [GeneratedRegex("\\\"(.+?)\\\"[, ]+(\\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex ChoiceArgumentsRegex();


        public bool UpdateToIncludeStartScriptData(RecordDataHandler recordHandler, QuestDataHandler questHandler)
        {
            HashSet<ScriptBlock> processedBlocks = new();

            return this.UpdateToIncludeStartScriptData(recordHandler, questHandler, processedBlocks, 3);
        }

        protected bool UpdateToIncludeStartScriptData(RecordDataHandler recordHandler, QuestDataHandler questHandler,
            HashSet<ScriptBlock> processedBlocks, int depth)
        {
            bool ret = false;
            if (depth <= 0) return ret;
            depth--;

            if (this.FindFunction("StartScript", null, "\\w+", null, out var ress))
            {
                foreach (var res in ress)
                {
                    var block = res.ScriptBlock;
                    if (processedBlocks.Contains(block)) continue;

                    var scriptId = res.ReturnValues[3].Value;
                    ScriptData? scrData = questHandler.ScriptDataById.GetValue(scriptId);
                    if (scrData is null)
                    {
                        ScriptRecord? scrRecord = recordHandler.Scripts.GetValue(scriptId);
                        if (scrRecord is not null)
                        {
                            scrData = new ScriptData(scrRecord);
                            ret = scrData.BlockData.UpdateToIncludeStartScriptData(recordHandler, questHandler, processedBlocks, depth) || ret;
                            questHandler.ScriptDataById.TryAdd(scriptId, scrData);
                        }
                    }
                    if (scrData is null) continue;

                    this.AddBlock(scrData.BlockData, false);

                    processedBlocks.Add(scrData.BlockData);

                    ret = true;
                }
            }
            return ret;
        }


        /// <summary>
        /// Returns all requirements from this block and all parent blocks.
        /// </summary>
        public QuestRequirementList GetRequirements()
        {
            var ret = new QuestRequirementList();
            if (this._requirements is not null)
            {
                ret.AddRange(this._requirements);
            }
            if (this.Parent is not null)
            {
                ret.AddRange(this.Parent.GetRequirements());
            }
            return ret;
        }
    }


    internal class QuestRequirementList : List<QuestRequirement>
    {
        /// <summary>
        /// ScriptBlock from which these requirements
        /// </summary>
        public ScriptBlock? ScriptBlock { get; set; }

        private protected bool? _hasGroupedRequirements;
        public bool HasGroppedRequirements
        {
            get
            {
                if (_hasGroupedRequirements is not null) return (bool)_hasGroupedRequirements;

                _hasGroupedRequirements = this.Exists(a => a.GroupId is not null);
                return (bool)_hasGroupedRequirements;
            }
        }

        public QuestRequirementList() { }

        public QuestRequirementList(IEnumerable<QuestRequirement> list) : base(list) { }

        /// <summary>
        /// the argument will be duplicated
        /// </summary>
        /// <param name="requirement">will be duplicated</param>
        public new void Add(QuestRequirement requirement)
        {
            if (this.Find(a => a.Equals(requirement)) is null)
                base.Add(requirement.Duplicate());
        }

        /// <summary>
        /// elements from the list will be duplicated
        /// </summary>
        /// <param name="requirement">elements from the list will be duplicated</param>
        public new void AddRange(IEnumerable<QuestRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                this.Add(requirement);
            }
        }

        public bool IsContainsJornalIndexRequirement(string questId, uint index)
        {
            bool? ret = null;
            foreach (var requirement in this)
            {
                if (requirement.Type != RequirementType.Journal || requirement.Variable != questId)
                    continue;

                var res = ConditionConverter.CheckCondition((int)index, Convert.ToInt32(requirement.Value), requirement.Operator);

                ret = (ret ?? true) && res;
            }

            return ret ?? false;
        }

        public bool IsContainsRequirementType(string type)
        {
            return this.Exists(a => a.Type == type);
        }

        public bool HasDialogueRequirementWithTopic(string topicId)
        {
            return this.Exists(a => a.Type == RequirementType.CustomDialogue && a.ValueStr == topicId);
        }

        public List<string>? GetContainedScriptIds()
        {
            List<string> ret = this.Where(r => r.Type == RequirementType.CustomScript).Select(r => r.Script!).ToList();
            return ret.Count > 0 ? ret : null;
        }

        public bool IsDispositionOnlyRequirement()
        {
            return !this.Any(r => r.Type != RequirementType.CustomDialogue &&
                r.Type != RequirementType.CustomDisposition &&
                r.Type != RequirementType.CustomActor);
        }

        public bool IsDeadOnlyRequirement()
        {
            return !this.Any(r => r.Type != RequirementType.CustomDialogue &&
                r.Type != RequirementType.Dead &&
                r.Type != RequirementType.CustomActor);
        }

        public bool IsNextStage(string questId, uint index, ScriptVariables scrVars)
        {
            if (this.IsContainsJornalIndexRequirement(questId, index)) return true;

            var copy = new QuestRequirementList(this);
            for (var i = copy.Count - 1; i >= 0; i--)
            {
                var req = copy[i];
                if (req.Type == RequirementType.CustomOnActivate || req.Type == RequirementType.CustomScript ||
                    req.Type == RequirementType.CustomDisposition)
                {
                    copy.RemoveAt(i);
                    continue;
                }
                else if (req.Type == RequirementType.CustomLocal && req.Variable is not null && req.Script is not null)
                {
                    if (req.Variable.Equals("doonce", StringComparison.OrdinalIgnoreCase))
                    {
                        copy.RemoveAt(i);
                        continue;
                    }

                    var scrDt = scrVars.GetValue(req.Script);
                    var vals = scrDt?.GetValue(req.Variable);
                    if (vals is null || vals.Count == 0)
                    {
                        copy.RemoveAt(i);
                        continue;
                    }

                    if (vals.Count == 1 || vals[0].Name.Equals(req.Variable, StringComparison.OrdinalIgnoreCase))
                    {
                        copy.RemoveAt(i);
                        continue;
                    }
                }
            }
            if (copy.Count == 0) return true;

            return copy.IsDeadOnlyRequirement() || copy.IsDeadOnlyRequirement();
        }


        public bool IsJournalOnlyRequirement(string questId)
        {
            return !this.Any(r => r.Type != RequirementType.Journal && r.Type != RequirementType.CustomDialogue &&
                r.Type != RequirementType.CustomActor || (r.Type == RequirementType.Journal && !String.Equals(r.Variable, questId)));
        }


        public QuestRequirementList GetTypeRequirements(string type)
        {
            QuestRequirementList ret = new(this.Where(r => r.Type == type));
            ret.ScriptBlock = this.ScriptBlock;
            return ret;
        }


        public bool Equals(QuestRequirementList other)
        {
            if (other is null || this.Count != other.Count) return false;
            foreach (var req in this)
            {
                if (!other.Exists(a => a.Equals(req)))
                    return false;
            }
            return true;
        }
    }
}
