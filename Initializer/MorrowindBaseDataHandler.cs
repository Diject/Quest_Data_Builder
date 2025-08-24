using Microsoft.Win32;
using Quest_Data_Builder.Logger;
using System.Text.RegularExpressions;

namespace Quest_Data_Builder.Initializer
{
    internal class MorrowindBaseDataHandler
    {
        public readonly InitializatorType Type = InitializatorType.MorrowindHandler;

        public readonly string? MorrowindDirectory;
        public readonly string? MorrowindDataDirectory;
        public readonly List<string> dataFiles = new();
        public bool IsValid => MorrowindDirectory is not null && MorrowindDataDirectory is not null && dataFiles.Count > 0;

        public MorrowindBaseDataHandler(string? morrowindDirectory)
        {
            if (!string.IsNullOrEmpty(morrowindDirectory))
            {
                morrowindDirectory = FileLocator.ExpandPath(morrowindDirectory);
                if (Directory.Exists(morrowindDirectory))
                {
                    MorrowindDirectory = morrowindDirectory;
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"Morrowind directory does not exist: {morrowindDirectory}");
                    MorrowindDirectory = null;
                }
            }

            if (string.IsNullOrEmpty(MorrowindDirectory) || !Directory.Exists(MorrowindDirectory))
            {
                MorrowindDirectory = GetMorrowindDirectoryFromRegistry();
            }

            if (string.IsNullOrEmpty(MorrowindDirectory) || !Directory.Exists(MorrowindDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Warn, "Morrowind directory is not set or does not exist.");
                MorrowindDirectory = null;
                return;
            }

            MorrowindDataDirectory = Path.Combine(MorrowindDirectory, "Data Files");
            if (!Directory.Exists(MorrowindDataDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"Data Files\" directory does not exist in Morrowind directory.");
                MorrowindDataDirectory = null;
                MorrowindDirectory = null;
                return;
            }

            string morrowindIniPath = Path.Combine(MorrowindDirectory, "Morrowind.ini");
            if (!File.Exists(morrowindIniPath))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"Morrowind.ini\" file does not exist in Morrowind directory.");
                MorrowindDirectory = null;
                MorrowindDataDirectory = null;
                return;
            }

            var gameFiles = GetMorrowindIniGameFiles(morrowindIniPath);
            if (gameFiles is not null)
            {
                dataFiles.AddRange(gameFiles);
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Error, "Failed to retrieve game files from \"Morrowind.ini\".");
                return;
            }


            CustomLogger.WriteLine(LogLevel.Text, $"Morrowind data loaded successfully from directory: {MorrowindDirectory}");
        }


        private string? GetMorrowindDirectoryFromRegistry()
        {
            if (!OperatingSystem.IsWindows()) return null;

            string? morrowindDirectory = null;

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Bethesda Softworks\Morrowind");
                if (key != null)
                {
                    morrowindDirectory = key.GetValue("Installed Path") as string;
                }
            }
            catch (Exception ex)
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"Error accessing registry to find Morrowind directory: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(morrowindDirectory) && Directory.Exists(morrowindDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Text, $"Morrowind directory found in registry: {morrowindDirectory}");
                return morrowindDirectory;
            }


            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games\1440164303"))
                {
                    morrowindDirectory = key?.GetValue("PATH") as string;
                }
                if (morrowindDirectory == null)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\Games\1440164303");
                    morrowindDirectory = key?.GetValue("PATH") as string;
                }
            }
            catch (Exception ex)
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"Error accessing registry for GOG Morrowind: {ex.Message}");
            }
            if (!string.IsNullOrEmpty(morrowindDirectory) && Directory.Exists(morrowindDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Text, $"Morrowind directory found in GOG registry: {morrowindDirectory}");
                return morrowindDirectory;
            }


            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    var steamPath = key?.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        morrowindDirectory = Path.Combine(steamPath, @"steamapps\common\Morrowind");
                    }
                }
            }
            catch (Exception ex)
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"Error accessing registry for Steam Morrowind: {ex.Message}");
            }
            if (!string.IsNullOrEmpty(morrowindDirectory) && Directory.Exists(morrowindDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Text, $"Morrowind directory found in Steam directory: {morrowindDirectory}");
                return morrowindDirectory;
            }


            CustomLogger.WriteLine(LogLevel.Warn, "Morrowind directory not found in registry.");

            return null;
        }


        public static List<string>? GetMorrowindIniGameFiles(string morrowindIniPath)
        {
            if (string.IsNullOrEmpty(morrowindIniPath) || !File.Exists(morrowindIniPath))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"Morrowind.ini\" file does not exist at the specified path.");
                return null;
            }

            string morrowindIniText = File.ReadAllText(morrowindIniPath);
            var gameFilesRegex = Regex.Matches(morrowindIniText, @"GameFile(\d+)=(.*)", RegexOptions.IgnoreCase);
            SortedList<int, string> gameFiles = new();
            foreach (Match match in gameFilesRegex)
            {
                string gameFile = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(gameFile))
                {
                    if (gameFiles.ContainsKey(int.Parse(match.Groups[1].Value)))
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, $"Duplicate GameFile entry found in Morrowind.ini: {gameFile}");
                    }
                    else
                    {
                        gameFiles.Add(int.Parse(match.Groups[1].Value), gameFile);
                    }
                }
            }

            return gameFiles.Values.ToList();
        }
    }
}
