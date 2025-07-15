using Newtonsoft.Json;
using Quest_Data_Builder.Core;
using Quest_Data_Builder.Initializer;
using Quest_Data_Builder.Logger;
using Quest_Data_Builder.TES3.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Quest_Data_Builder.Program;

namespace Quest_Data_Builder.Config
{
    enum InitializerType
    {
        Auto,
        ConfigFile,
        Manual
    }

    static class MainConfig
    {
        public static InitializerType InitializerType = InitializerType.Manual;

        public static int MaxObjectPositions = 50;

        public static Encoding FileEncoding
        {
            get { return _fileEncoding; }
            set
            {
                _fileEncoding = value;
                BetterBinaryReader.Encoding = value;
            }
        }
        private static Encoding _fileEncoding = Encoding.ASCII;

        public static LogLevel LogLevel
        {
            get { return CustomLogger.Level; }
            set { CustomLogger.Level = value; }
        }

        public static string? MorrowindDirectory;

        public static List<string>? Files; // game files with full paths

        public static List<string>? GameFiles;

        public static string OutputDirectory = "questData";

        public static bool RemoveUnused = true;

        public static bool OptimizeData = true;

        public static bool FindLinksBetweenDialogues = true;

        public static int DialogueSearchDepth = 2;

        public static int RoundFractionalDigits = 3;

        /// <summary>
        /// Number of steps in a quest to be considered a quest that an object can start(give)
        /// </summary>
        public static int StagesNumToAddQuestInfo = 1;

        public static SerializerType OutputFormatType = SerializerType.Json;
        public static string OutputFileFormat = "json";


        public static bool Initialize(bool loadInitialDataFromRootConfig = true)
        {
            if (loadInitialDataFromRootConfig)
            {
                if (File.Exists("config.json"))
                {
                    LoadConfiguration("config.json");
                }
                else if (File.Exists("config.yaml"))
                {
                    LoadConfiguration("config.yaml");
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Info, "Root configuration file not found. Using default settings.");
                }

                if (InitializerType == InitializerType.ConfigFile)
                {
                    CustomLogger.WriteLine(LogLevel.Text, "Using configuration from config file.");
                    return true;
                }
            }


            if (InitializerType == InitializerType.Auto)
            {
                CustomLogger.WriteLine(LogLevel.Text, "Initializing automatically...");

                var mwBaseHandler = new MorrowindBaseDataHandler(MorrowindDirectory);
                if (mwBaseHandler.IsValid)
                {
                    MorrowindDirectory = mwBaseHandler.MorrowindDirectory!;
                    Files = new List<string>();

                    foreach (var file in mwBaseHandler.dataFiles)
                    {
                        var filePath = Path.Combine(mwBaseHandler.MorrowindDataDirectory!, file);
                        if (File.Exists(filePath))
                        {
                            Files.Add(filePath);
                        }
                        else
                        {
                            CustomLogger.WriteLine(LogLevel.Warn, $"File not found: {filePath}");
                        }
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "Morrowind directory and data files have been initialized from the base handler.");
                    return true;
                }


                var omwHandler = new OMWDataHandler();
                if (omwHandler.IsValid)
                {
                    var omwProfileName = omwHandler.CurrentProfile ?? omwHandler.ProfileNames.FirstOrDefault();
                    if (omwProfileName is null)
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, "No OMW profile found.");
                        return false;
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "Using OMW profile: " + omwProfileName);

                    var gameFiles = omwHandler.GetFullGameFilePaths(omwProfileName);
                    if (gameFiles is null || gameFiles.Count == 0)
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, "No game files found in OMW profile.");
                        return false;
                    }

                    Files = gameFiles;

                    if (omwHandler.Encoding is not null)
                    {
                        FileEncoding = omwHandler.Encoding;
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "Morrowind data files have been initialized from OMW handler.");
                    return true;
                }


                var mo2Handler = new MO2DataHandler();
                if (mo2Handler.IsValid)
                {
                    var mo2ProfileName = mo2Handler.CurrentProfile ?? mo2Handler.ProfileNames.FirstOrDefault();
                    if (mo2ProfileName is null)
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, "No MO2 profile found.");
                        return false;
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "Using MO2 profile: " + mo2ProfileName);

                    var gameFiles = mo2Handler.GetFullGameFilePaths(mo2ProfileName);
                    if (gameFiles is null || gameFiles.Count == 0)
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, "No game files found in MO2 profile.");
                        return false;
                    }

                    MorrowindDirectory = mo2Handler.MorrowindDirectory;
                    Files = gameFiles;

                    CustomLogger.WriteLine(LogLevel.Text, "Morrowind data files have been initialized from MO2 handler.");
                    return true;
                }


                return false;
            }


            if (InitializerType == InitializerType.Manual)
            {
                var mwBaseHandler = new MorrowindBaseDataHandler(MorrowindDirectory);
                var omwHandler = new OMWDataHandler();
                var mo2Handler = new MO2DataHandler();

                List<(dynamic handler, string mesage)> handlers = new();

                if (mwBaseHandler.IsValid)
                {
                    string message = $"Morrowind.ini from directory: {mwBaseHandler.MorrowindDirectory}";
                    handlers.Add((mwBaseHandler, message));
                }
                if (omwHandler.IsValid)
                {
                    string message = $"OpenMW profile";
                    handlers.Add((omwHandler, message));
                }
                if (mo2Handler.IsValid)
                {
                    string message = $"Mod Organizer 2 profile";
                    handlers.Add((mo2Handler, message));
                }

                if (handlers.Count == 0)
                {
                    CustomLogger.WriteLine(LogLevel.Error, "No valid data sources found for Morrowind files.");
                    return false;
                }

                void WriteAvailableHandlers()
                {
                    CustomLogger.WriteLine(LogLevel.Text, "\nSelect data source for Morrowind files:");
                    for (int i = 0; i < handlers.Count; i++)
                    {
                        var handlerData = handlers[i];
                        CustomLogger.WriteLine(LogLevel.Text, $"{i + 1}. {handlerData.mesage}");
                    }
                }
                
                dynamic? selectedHandler = null;
                for (; ;)
                {
                    WriteAvailableHandlers();
                    CustomLogger.WriteLine(LogLevel.Text, "\nEnter the number of the data source to use, or 'q' to quit:");
                    string? input = Console.ReadLine();
                    if (input is null || input.Trim().ToLower() == "q")
                    {
                        CustomLogger.WriteLine(LogLevel.Text, "\nExiting initialization.");
                        return false;
                    }
                    if (int.TryParse(input, out int choice) && choice > 0 && choice <= handlers.Count)
                    {
                        choice--;
                        selectedHandler = handlers[choice].handler;
                        CustomLogger.WriteLine(LogLevel.Text, $"\nSelected data source: {handlers[choice].mesage}");
                        break;
                    }
                    else
                    {
                        CustomLogger.WriteLine(LogLevel.Error, "\nInvalid choice. Please try again.");
                    }
                }

                if (selectedHandler is null) return false;

                if (selectedHandler.Type == InitializatorType.MorrowindHandler)
                {
                    MorrowindDirectory = mwBaseHandler.MorrowindDirectory!;
                    Files = new List<string>();

                    foreach (var file in mwBaseHandler.dataFiles)
                    {
                        var filePath = Path.Combine(mwBaseHandler.MorrowindDataDirectory!, file);
                        if (File.Exists(filePath))
                        {
                            Files.Add(filePath);
                        }
                        else
                        {
                            CustomLogger.WriteLine(LogLevel.Warn, $"File not found: {filePath}");
                        }
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "\nMorrowind directory and data files have been initialized from the base handler.");
                    return true;
                }
                else
                {
                    List<string> profileNames = selectedHandler.ProfileNames;
                    if (profileNames.Count == 0)
                    {
                        CustomLogger.WriteLine(LogLevel.Error, "\nNo profiles found in the selected handler.");
                        return false;
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "\nSelect a profile from the available profiles:");
                    for (int i = 0; i < profileNames.Count; i++)
                    {
                        CustomLogger.WriteLine(LogLevel.Text, $"{i + 1}. {profileNames[i]}");
                    }

                    CustomLogger.WriteLine(LogLevel.Text, "\nEnter the number of the profile to use, or 'q' to quit:");
                    for (; ;)
                    {
                        string? profileInput = Console.ReadLine();
                        if (profileInput is null || profileInput.Trim().ToLower() == "q")
                        {
                            CustomLogger.WriteLine(LogLevel.Text, "\nExiting initialization.");
                            return false;
                        }

                        if (int.TryParse(profileInput, out int profileChoice) && profileChoice > 0 && profileChoice <= profileNames.Count)
                        {
                            string selectedProfile = profileNames[profileChoice - 1];
                            CustomLogger.WriteLine(LogLevel.Text, $"\nSelected profile: {selectedProfile}");

                            var gameFiles = selectedHandler.GetFullGameFilePaths(selectedProfile);
                            if (gameFiles is null || gameFiles.Count == 0)
                            {
                                CustomLogger.WriteLine(LogLevel.Warn, "\nNo game files found in the selected profile.");
                                return false;
                            }

                            Files = gameFiles;

                            CustomLogger.WriteLine(LogLevel.Text, "\nMorrowind data files have been initialized from the selected handler.");
                            return true;
                        }
                        else
                        {
                            CustomLogger.WriteLine(LogLevel.Error, "\nInvalid choice. Please try again.");
                        }
                    }
                }
            }


            

            return false;
        }



        public static bool LoadConfiguration(string filename)
        {
            CustomLogger.WriteLine(LogLevel.Info, "Loading configuration file: " + filename);

            if (!File.Exists(filename))
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: Configuration file does not exist.");
                return false;
            }

            string text;
            string? extension;
            try
            {
                text = File.ReadAllText(filename);
                extension = Path.GetExtension(filename)?.ToLower();
            }
            catch (Exception e)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: Failed to load configuration file.\n" + e.Message);
                return false;
            }

            var yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();

            dynamic? configData = extension switch
            {
                "json" => JsonConvert.DeserializeObject(text),
                "yaml" => yamlDeserializer.Deserialize(text),
                _ => null,
            };

            if (configData is null)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: Failed to load configuration file.");
                return false;
            }

            if ((object)configData.logLevel is not null)
            {
                LogLevel = (LogLevel)configData.logLevel;
            }

            if ((object)configData.directory is not null)
            {
                MorrowindDirectory = (string)configData.directory;
            }

            if ((object)configData.output is not null)
            {
                OutputDirectory = (string)configData.output;
            }

            if ((object)configData.encoding is not null)
            {
                FileEncoding = Encoding.GetEncoding((int)configData.encoding);
            }

            if ((object)configData.maxPos is not null)
            {
                MaxObjectPositions = (int)configData.maxPos;
            }

            if ((object)configData.removeUnused is not null)
            {
                RemoveUnused = (bool)configData.removeUnused;
            }

            if ((object)configData.findDialogueLinks is not null)
            {
                FindLinksBetweenDialogues = (bool)configData.findDialogueLinks;
            }

            if ((object)configData.dialogueSearchDepth is not null)
            {
                DialogueSearchDepth = (int)configData.dialogueSearchDepth;
            }

            if ((object)configData.roundFractionalDigits is not null)
            {
                RoundFractionalDigits = (int)configData.roundFractionalDigits;
            }

            if ((object)configData.optimizeData is not null)
            {
                OptimizeData = (bool)configData.optimizeData;
            }

            if ((object)configData.stagesNumToAddQuestInfo is not null)
            {
                StagesNumToAddQuestInfo = (int)configData.stagesNumToAddQuestInfo;
            }

            if ((object)configData.gameFiles is not null)
            {
                var outGameFileList = new List<string>();
                Newtonsoft.Json.Linq.JArray gameFiles = (Newtonsoft.Json.Linq.JArray)configData.gameFiles;
                for (int i = 0; i < gameFiles.Count; i++)
                {
                    outGameFileList.Add(gameFiles.ElementAt(i).ToString());
                }
                GameFiles = outGameFileList;
            }

            if (configData.outputFormat is not null)
            {
                OutputFormatType = (string)configData.outputFormat switch
                {
                    "yaml" => SerializerType.Yaml,
                    "lua" => SerializerType.Lua,
                    _ => SerializerType.Json,
                };
                OutputFileFormat = (string)configData.outputFormat switch
                {
                    "yaml" => "yaml",
                    "lua" => "lua",
                    _ => "json",
                };
            }

            CustomLogger.WriteLine(LogLevel.Info, "The configuration file has been loaded");

            return true;
        }
    }
}
