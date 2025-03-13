using Newtonsoft.Json;
using Quest_Data_Builder.Core;
using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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



        public static bool LoadConfiguration(string filename)
        {
            CustomLogger.WriteLine(LogLevel.Info, "Loading configuration file...");

            string text;
            try
            {
                text = File.ReadAllText(filename);
            }
            catch (Exception e)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: Failed to load configuration file.\n" + e.Message);
                return false;
            }

            dynamic? jsonData = JsonConvert.DeserializeObject(text);

            if (jsonData is null)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Error: Failed to load configuration file.");
                return false;
            }

            if ((object)jsonData.logLevel is not null)
            {
                LogLevel = (LogLevel)jsonData.logLevel;
            }

            if ((object)jsonData.directory is not null)
            {
                MorrowindDirectory = (string)jsonData.directory;
            }

            if ((object)jsonData.output is not null)
            {
                OutputDirectory = (string)jsonData.output;
            }

            if ((object)jsonData.encoding is not null)
            {
                FileEncoding = Encoding.GetEncoding((int)jsonData.encoding);
            }

            if ((object)jsonData.maxPos is not null)
            {
                MaxObjectPositions = (int)jsonData.maxPos;
            }

            if ((object)jsonData.removeUnused is not null)
            {
                RemoveUnused = (bool)jsonData.removeUnused;
            }

            if ((object)jsonData.findDialogueLinks is not null)
            {
                FindLinksBetweenDialogues = (bool)jsonData.findDialogueLinks;
            }

            if ((object)jsonData.dialogueSearchDepth is not null)
            {
                DialogueSearchDepth = (int)jsonData.dialogueSearchDepth;
            }

            if ((object)jsonData.roundFractionalDigits is not null)
            {
                RoundFractionalDigits = (int)jsonData.roundFractionalDigits;
            }

            if ((object)jsonData.optimizeData is not null)
            {
                OptimizeData = (bool)jsonData.optimizeData;
            }

            if ((object)jsonData.gameFiles is not null)
            {
                var outGameFileList = new List<string>();
                Newtonsoft.Json.Linq.JArray gameFiles = (Newtonsoft.Json.Linq.JArray)jsonData.gameFiles;
                for (int i = 0; i < gameFiles.Count; i++)
                {
                    outGameFileList.Add(gameFiles.ElementAt(i).ToString());
                }
                GameFiles = outGameFileList;
            }

            CustomLogger.WriteLine(LogLevel.Info, "The configuration file has been loaded");

            return true;
        }
    }
}
