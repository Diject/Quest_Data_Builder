using Newtonsoft.Json;
using Quest_Data_Builder.Core;
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
    static class MainConfig
    {
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

        public static string MorrowindDirectory = "";

        public static string? MorrowindIni;

        public static List<string>? GameFiles;

        public static string OutputDirectory = "";

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



        public static bool LoadConfiguration(string filename)
        {
            CustomLogger.WriteLine(LogLevel.Info, "Loading configuration file...");

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
