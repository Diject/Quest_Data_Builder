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
using System.Text;
using Newtonsoft.Json;
using Quest_Data_Builder.Config;

namespace Quest_Data_Builder
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            CustomLogger.Level = LogLevel.Warn;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            MainConfig.FileEncoding = Encoding.GetEncoding(1252);

            var options = Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (options.InputFile is not null)
                {
                    MainConfig.LoadConfiguration(options.InputFile);
                }

                if (options.Directory is not null)
                {
                    MainConfig.MorrowindDirectory = options.Directory;
                }

                if (options.Encoding is not null)
                {
                    MainConfig.FileEncoding = Encoding.GetEncoding((int)options.Encoding);
                }

                if (options.MaxObjectPositions is not null)
                {
                    MainConfig.MaxObjectPositions = (int)options.MaxObjectPositions;
                }

                if (options.LogLevel is not null)
                {
                    MainConfig.LogLevel = (LogLevel)options.LogLevel;
                }

                if (options.Output is not null)
                {
                    MainConfig.OutputDirectory = options.Output;
                }

                if (options.RemoveUnused is not null)
                {
                    MainConfig.RemoveUnused = options.RemoveUnused ?? false;
                }
            });

            {
                if (MainConfig.GameFiles is null || MainConfig.GameFiles.Count == 0)
                {
                    CustomLogger.WriteLine(LogLevel.Text, "Using \"morrowind.ini\" to generate data");

                    string? morrowindDirectory = MainConfig.MorrowindDirectory ?? DirectoryUtils.GetParentDirectoryPathWithName(Directory.GetCurrentDirectory(), "morrowind");
                    if (morrowindDirectory is null)
                    {
                        CustomLogger.WriteLine(LogLevel.Error, "Error: cannot find morrowind directory.");
                        return;
                    }

                    string morrowindIni = morrowindDirectory + @"\morrowind.ini";
                    if (!File.Exists(morrowindIni))
                    {
                        CustomLogger.WriteLine(LogLevel.Error, "Error: cannot find \"morrowind.ini\"");
                        return;
                    }

                    try
                    {
                        var matches = DataFileRegex().Matches(File.ReadAllText(morrowindIni));
                        var morrowindFiles = new SortedList<uint, string>();
                        foreach (Match match in matches)
                        {
                            string filePath = Path.Combine(morrowindDirectory, "Data Files", match.Groups[2].Value.Replace("\r", ""));
                            CustomLogger.WriteLine(LogLevel.Info, $"file path: \"{filePath}\"");
                            morrowindFiles.TryAdd(uint.Parse(match.Groups[1].Value), filePath);
                        }

                        MainConfig.GameFiles = new List<string>(morrowindFiles.Values);
                    }
                    catch (Exception ex)
                    {
                        CustomLogger.RegisterErrorException(ex);
                        CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                        return;
                    }
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Text, "Using info from configuration file to generate data");

                    if (MainConfig.GameFiles is null)
                    {
                        CustomLogger.WriteLine(LogLevel.Error, "Error: files for data generation are not listed");
                        return;
                    }
                }
            }

            if (MainConfig.GameFiles.Count == 0)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: files for data generation are not listed");
                return;
            }

            var recordData = new List<RecordDataHandler>();

            foreach (var filePath in MainConfig.GameFiles)
            {
                CustomLogger.WriteLine(LogLevel.Text, $"processing \"{filePath}\"");
                try
                {
                    using var reader = new BetterBinaryReader(File.OpenRead(filePath));
                    var tes3 = new TES3DataFile(reader);

                    var dataHandler = new RecordDataHandler(tes3);

                    recordData.Add(dataHandler);
                }
                catch (Exception ex)
                {
                    CustomLogger.RegisterErrorException(ex);
                    CustomLogger.WriteLine(LogLevel.Error, "Error: failed to process the file");
                    CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                }
            }

            if (recordData.Count == 0)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: files for data generation are not found");
                return;
            }

            try
            {
                for (int i = 1; i < recordData.Count; i++)
                {
                    var data = recordData[i];

                    recordData[0].Merge(data);
                }
                recordData[0].RemoveDeletedRecords();
                recordData[0].AddItemsFromLeveledListsToObjects();
            }
            catch (Exception ex)
            {
                CustomLogger.RegisterErrorException(ex);
                CustomLogger.WriteLine(LogLevel.Error, "Error: failed to merge data");
                CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
            }

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
            jsonSer.MaximumObjectPositions = MainConfig.MaxObjectPositions;
            File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "quests.json"]), jsonSer.QuestData(), MainConfig.FileEncoding);
            File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "questByTopicText.json"]), jsonSer.QuestByTopicText(), MainConfig.FileEncoding);
            File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "questObjects.json"]), jsonSer.QuestObjects(), MainConfig.FileEncoding);
            File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "localVariables.json"]), jsonSer.LocalVariableDataByScriptId(), MainConfig.FileEncoding);

            File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "luaAnnotations.lua"]), CustomSerializer.LuaAnnotations, MainConfig.FileEncoding);
            File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "info.lua"]), "return " + (new GeneratedDataInfo(MainConfig.GameFiles).ToString()), MainConfig.FileEncoding);

            CustomLogger.WriteLine(LogLevel.Text, $"Completed with {CustomLogger.Errors.Count} errors");
        }

        public class Options
        {
            [Option('l', "logLevel", Required = false, HelpText = "Log level. From 0 to 3.")]
            public uint? LogLevel { get; set; }

            [Option('d', "directory", Required = false, HelpText = "Path to the game directory. If set and \"-f\" doesn't, the parser will attempt to get info about active mods from \"morrowind.ini\".")]
            public string? Directory { get; set; }

            [Option('o', "output", Required = false, HelpText = "Output directory for result data. If doesn't set, the data will be placed in the parser directory.")]
            public string? Output { get; set; }

            [Option('p', "maxPos", Required = false, HelpText = "Maximum number of positions for an object.")]
            public int? MaxObjectPositions { get; set; }

            [Option('e', "encoding", Required = false, HelpText = "Encoding of the game. (1252, 1251, 1250)")]
            public int? Encoding { get; set; }

            [Option('i', "inputFile", Required = false, HelpText = "Input config file with required data.")]
            public string? InputFile { get; set; }

            [Option('u', "removeUnused", Required = false, HelpText = "Remove unused quest objects from the output data. true by default.")]
            public bool? RemoveUnused { get; set; }
        }

        [GeneratedRegex(@"^ *GameFile(\d+) *= *(.+?)[ ;\\t]*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
        private static partial Regex DataFileRegex();
    }
}