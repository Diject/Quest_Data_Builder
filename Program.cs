using CommandLine;
using Quest_Data_Builder.Config;
using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3;
using Quest_Data_Builder.TES3.Records;
using Quest_Data_Builder.TES3.Script;
using Quest_Data_Builder.TES3.Serializer;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Quest_Data_Builder
{
    internal partial class Program
    {
        private static void ExitProgram(int exitCode = 0)
        {
            if (MainConfig.InitializerType != InitializerType.ConfigFile)
            {
                CustomLogger.WriteLine(LogLevel.Text, "Press enter to exit.");
                Console.ReadLine();
            }
            CustomLogger.Shutdown();
            Environment.Exit(exitCode);
        }


        static void Main(string[] args)
        {
            string? execLocation = Assembly.GetExecutingAssembly().Location;
            string? exeDir = String.IsNullOrEmpty(execLocation) ? AppContext.BaseDirectory
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(exeDir))
            {
                Directory.SetCurrentDirectory(exeDir!);
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: failed to set current directory.");
                ExitProgram(2);
            }

            if (CustomLogger.LogToFile)
                CustomLogger.ClearLogFile();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            MainConfig.FileEncoding = Encoding.GetEncoding(1252);

            bool isConfigFile = false;
            var options = Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (options.LogToFile is not null)
                {
                    CustomLogger.LogToFile = (bool)options.LogToFile;
                    if (CustomLogger.LogToFile)
                        CustomLogger.ClearLogFile();
                }

                if (options.ConfigFile is not null)
                {
                    isConfigFile = MainConfig.LoadConfiguration(options.ConfigFile);
                }

                if (options.LogLevel is not null)
                {
                    MainConfig.LogLevel = (LogLevel)options.LogLevel;
                }
            });

            try
            {
                if (!MainConfig.Initialize(!isConfigFile))
                {
                    CustomLogger.WriteLine(LogLevel.Error, "Error: failed to initialize configuration");
                    ExitProgram(3);
                }
            }
            catch (Exception ex)
            {
                CustomLogger.RegisterErrorException(ex);
                CustomLogger.WriteLine(LogLevel.Error, "Error: failed to initialize configuration");
                CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                ExitProgram(4);
            }

            if (MainConfig.Files is null)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: no files specified for processing");
                ExitProgram(5);
            }

            var recordData = new List<RecordDataHandler>();

            foreach (var filePath in MainConfig.Files!)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    string exceptionMessage = $"Error: file \"{filePath}\" does not exist";
                    CustomLogger.RegisterErrorException(new Exception(exceptionMessage));
                    CustomLogger.WriteLine(LogLevel.Error, exceptionMessage);
                    continue;
                }

                bool ignored = false;
                try
                {
                    if (MainConfig.IgnoredDataFilePatterns is not null)
                    {
                        foreach (var ignoredPattern in MainConfig.IgnoredDataFilePatterns)
                        {
                            if (Regex.Match(filePath, ignoredPattern, RegexOptions.IgnoreCase).Success)
                            {
                                CustomLogger.WriteLine(LogLevel.Text, $"skipping file \"{filePath}\", ignored by pattern \"{ignoredPattern}\"");
                                ignored = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomLogger.RegisterErrorException(ex);
                    CustomLogger.WriteLine(LogLevel.Error, "Error: failed to process ignored files");
                    CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                }

                if (ignored)
                {
                    continue;
                }

                if (Path.GetExtension(Path.GetFileName(filePath))?.ToLower() != ".esp" &&
                    Path.GetExtension(Path.GetFileName(filePath)).ToLower() != ".esm" &&
                    Path.GetExtension(Path.GetFileName(filePath)).ToLower() != ".omwaddon")
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"skipping file \"{filePath}\", not a valid data file");
                    continue;
                }

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
                ExitProgram(6);
            }

            try
            {
                CustomLogger.WriteLine(LogLevel.Text, $"Merging {recordData.Count} data files");
                for (int i = 1; i < recordData.Count; i++)
                {
                    var data = recordData[i];

                    recordData[0].Merge(data);
                }

                CustomLogger.WriteLine(LogLevel.Text, "Removing deleted records");
                recordData[0].RemoveDeletedRecords();

                CustomLogger.WriteLine(LogLevel.Text, "Adding leveled lists items to object records");
                recordData[0].AddItemsFromLeveledListsToObjects();
            }
            catch (Exception ex)
            {
                CustomLogger.RegisterErrorException(ex);
                CustomLogger.WriteLine(LogLevel.Error, "Error: failed to merge data");
                CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
            }

            if (MainConfig.GenerateQuestData)
            {

                CustomLogger.WriteLine(LogLevel.Text, "Processing quest data");
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


                var jsonSer = new DataSerializer(MainConfig.OutputFormatType, dataProcessor);

                try
                {
                    Directory.CreateDirectory(MainConfig.OutputDirectory);

                    CustomLogger.WriteLine(LogLevel.Text, $"Writing data to \"{Path.GetFullPath(MainConfig.OutputDirectory)}\"");

                    File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "quests." + MainConfig.OutputFileFormat]), jsonSer.QuestData(),
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding);
                    File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "questByTopicText." + MainConfig.OutputFileFormat]), jsonSer.QuestByTopicText(),
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding);
                    File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "questObjects." + MainConfig.OutputFileFormat]), jsonSer.QuestObjects(),
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding);
                    File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "localVariables." + MainConfig.OutputFileFormat]), jsonSer.LocalVariableDataByScriptId(),
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding);

                    if (MainConfig.GenerateDialogueTopicRequirements)
                    {
                        File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "dialogueTopics." + MainConfig.OutputFileFormat]), jsonSer.DialogueTopics(),
                            MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding);
                    }

                    File.WriteAllText(Path.Combine([MainConfig.OutputDirectory, "luaAnnotations.lua"]), DataSerializer.LuaAnnotations,
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding);
                    File.WriteAllText(
                        Path.Combine([MainConfig.OutputDirectory, "info." + MainConfig.OutputFileFormat]),
                        new GeneratedDataInfo(MainConfig.Files, MainConfig.OutputFormatType).ToString(),
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding
                    );
                }
                catch (Exception ex)
                {
                    CustomLogger.RegisterErrorException(ex);
                    CustomLogger.WriteLine(LogLevel.Error, "Error: failed to write data");
                    CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                }
            }

            if (MainConfig.EnableHeightMapImageGeneration)
            {
                try
                {
                    var mapImageBuilder = new MapImageBuilder(recordData[0]);
                    mapImageBuilder.BuildImage(Path.Combine(Path.GetFullPath(MainConfig.OutputDirectory), "map.png"));

                    File.WriteAllText(
                        Path.Combine([MainConfig.OutputDirectory, "mapInfo." + MainConfig.OutputFileFormat]),
                        mapImageBuilder.GenerateInfo(MainConfig.OutputFormatType),
                        MainConfig.OutputFormatType == SerializerType.Yaml ? Encoding.UTF8 : MainConfig.FileEncoding
                    );
                }
                catch (Exception ex)
                {
                    CustomLogger.RegisterErrorException(ex);
                    CustomLogger.WriteLine(LogLevel.Error, "Error: failed to build map image");
                    CustomLogger.WriteLine(LogLevel.Error, ex.ToString());
                }
            }

            CustomLogger.WriteLine(LogLevel.Text, $"\nCompleted with {CustomLogger.Errors.Count} errors");
            if (MainConfig.InitializerType != InitializerType.ConfigFile)
            {
                CustomLogger.WriteLine(LogLevel.Text, $"You can find the output files in \"{Path.GetFullPath(MainConfig.OutputDirectory)}\"");
            }
            ExitProgram();
        }

        public class Options
        {
            [Option('l', "logLevel", Required = false, HelpText = "Log level. From 0 to 3.")]
            public uint? LogLevel { get; set; }

            [Option('L', "logToFile", Required = false, HelpText = "Log to file")]
            public bool? LogToFile { get; set; }

            [Option('c', "configFile", Required = false, HelpText = "Input config file with required data.")]
            public string? ConfigFile { get; set; }
        }
    }
}