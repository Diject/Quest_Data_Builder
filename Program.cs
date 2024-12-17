using Quest_Data_Builder.Core;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3;
using Quest_Data_Builder.TES3.Quest;
using Quest_Data_Builder.Logger;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;
using System.Text.RegularExpressions;
using Quest_Data_Builder.TES3.Script;
using System.Collections.Generic;
using System.Collections;
using Microsoft.Win32;
using Quest_Data_Builder.TES3.Serializer;
using CommandLine;
using System.IO;
using Quest_Data_Builder.Extentions;

namespace Quest_Data_Builder
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            CustomLogger.Level = LogLevel.Warn;
            int maximumNumberOfObjectPositions = 50;


            var outputDirPath = "";
            var morrowindFiles = new SortedList<uint, string>();

            var options = Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (options.MaximumNumberOfObjectPositions is not null)
                {
                    maximumNumberOfObjectPositions = (int)options.MaximumNumberOfObjectPositions;
                }

                if (options.LogLevel is not null)
                {
                    CustomLogger.Level = (LogLevel)options.LogLevel;
                }

                if (options.Output is not null)
                {
                    outputDirPath = options.Output;
                }

                if (options.GameFiles.Any())
                {
                    if (options.Directory is null)
                    {
                        CustomLogger.WriteLine(LogLevel.Error, "Error: path to the morrowind directory wasn't set.");
                        return;
                    }
                    uint i = 0;
                    foreach (var fileName in options.GameFiles)
                    {
                        morrowindFiles.Add(i++, Path.Combine([options.Directory, "Data Files", fileName]));
                    }
                }
                else
                {
                    try
                    {
                        string? morrowindDirectory = options.Directory ?? DirectoryUtils.GetParentDirectoryPathWithName(Directory.GetCurrentDirectory(), "morrowind");
                        if (morrowindDirectory is null)
                        {
                            CustomLogger.WriteLine(LogLevel.Error, "Error: cannot find morrowind directory.");
                            return;
                        }
                        var matches = DataFileRegex().Matches(File.ReadAllText(morrowindDirectory + @"\morrowind.ini"));
                        foreach (Match match in matches)
                        {
                            morrowindFiles.TryAdd(uint.Parse(match.Groups[1].Value), morrowindDirectory + @"\Data Files\" + match.Groups[2].Value.Replace("\r", ""));
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
            });

            if (!morrowindFiles.Any()) return;

            var recordData = new SortedList<uint, RecordDataHandler>();

            foreach (var fileItem in morrowindFiles)
            {
                var fileName = fileItem.Value;
                CustomLogger.WriteLine(LogLevel.Error, $"processing \"{fileName}\"");
                try
                {
                    using var reader = new BetterBinaryReader(File.OpenRead(fileName));
                    var tes3 = new TES3DataFile(reader);

                    var dataHandler = new RecordDataHandler(tes3);

                    recordData.Add(fileItem.Key, dataHandler);
                }
                catch (Exception ex)
                {
                    CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                }
            }

            for (int i = 1; i < recordData.Count; i++)
            {
                var data = recordData.Values[i];

                recordData[0].Merge(data);
            }
            recordData[0].RemoveDeletedRecords();
            recordData[0].AddItemsFromLeveledListsToObjects();

            var dataProcessor = new QuestDataHandler(recordData[0]);

            if (CustomLogger.Level >= LogLevel.Info)
            {
                foreach (var dialog in recordData[0]!.Dialogs.ToList().FindAll((a) => a.Value.Type == DialogType.Journal && !dataProcessor.QuestContainigElements.ContainsKey(a.Value.Id)))
                {
                    CustomLogger.WriteLine(LogLevel.Info, $"found unused quest: \"{dialog.Value.Id}\"");
                }
            }
            if (CustomLogger.Level >= LogLevel.Info)
            {
                foreach (var unfound in ConditionConverter.UnfoundCommands)
                {
                    CustomLogger.WriteLine(LogLevel.Info, $"found strange condition command: \"{unfound}\"");
                }
            }


            var jsonSer = new CustomSerializer(SerializerType.Json, dataProcessor);
            jsonSer.MaximumObjectPositions = maximumNumberOfObjectPositions;
            File.WriteAllText(Path.Combine([outputDirPath, "quests.json"]), jsonSer.QuestData());
            //File.WriteAllText(Path.Combine([outputDirPath, "questByTopicId.json"]), jsonSer.TopicById());
            File.WriteAllText(Path.Combine([outputDirPath, "questByTopicText.json"]), jsonSer.QuestByTopicText());
            File.WriteAllText(Path.Combine([outputDirPath, "questObjects.json"]), jsonSer.QuestObjects());
            //File.WriteAllText(Path.Combine([outputDirPath, "questObjectInCell.json"]), jsonSer.QuestObjectPositionsInCell());
            //File.WriteAllText(Path.Combine([outputDirPath, "questObjectPositions.json"]), jsonSer.QuestObjectPositions());
            //File.WriteAllText(Path.Combine([outputDirPath, "objectIdsByScript.json"]), jsonSer.ObjectIdsByScript());
            //File.WriteAllText(Path.Combine([outputDirPath, "questObjectInCell.json"]), jsonSer.QuestObjectPositionsInCell());
            File.WriteAllText(Path.Combine([outputDirPath, "localVariables.json"]), jsonSer.LocalVariableDataByScriptId());

            File.WriteAllText(Path.Combine([outputDirPath, "luaAnnotations.lua"]), CustomSerializer.LuaAnnotations);
            File.WriteAllText(Path.Combine([outputDirPath, "info.lua"]), "return " + (new GeneratedDataInfo(morrowindFiles).ToString()));

            CustomLogger.WriteLine(LogLevel.Error, "Done");
        }

        public class Options
        {
            [Option('f', "gameFiles", Required = false, HelpText = "List of mod file names, separated by a space character. Required \"-d\" to be set.")]
            public required IEnumerable<string> GameFiles { get; set; }

            [Option('l', "logLevel", Required = false, HelpText = "Log level. From 0 to 3.")]
            public uint? LogLevel { get; set; }

            [Option('d', "directory", Required = false, HelpText = "Path to the game directory. If set and \"-f\" doesn't, the parser will attempt to get info about active mods from \"morrowind.ini\".")]
            public string? Directory { get; set; }

            [Option('o', "output", Required = false, HelpText = "Output directory for result data. If doesn't set, the data will be placed in the parser directory.")]
            public string? Output { get; set; }

            [Option('p', "maxPos", Required = false, HelpText = "Maximum number of positions for an object.")]
            public int? MaximumNumberOfObjectPositions { get; set; }
        }

        [GeneratedRegex(@"^ *GameFile(\d+) *= *(.+?) *$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex DataFileRegex();
    }
}