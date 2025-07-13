using CommandLine.Text;
using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quest_Data_Builder.Initializer
{
    internal record ProfileData(
        List<string> Data,
        List<string> Content,
        List<string> Managed
    );

    internal class MO2DataHandler
    {
        private readonly Dictionary<string, ProfileData> profiles = new();
        private readonly string? baseDirectory;
        public readonly string? MorrowindDirectory;

        public MO2DataHandler(string? baseDirectory)
        {
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                this.baseDirectory = baseDirectory;
            }

            if (string.IsNullOrEmpty(this.baseDirectory))
            {
                string? localDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localDir))
                {
                    this.baseDirectory = Path.Combine(localDir, "ModOrganizer", "Morrowind");
                }
            }

            if (string.IsNullOrEmpty(this.baseDirectory) || !Directory.Exists(this.baseDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Warn, "Mod Organizer 2 base directory is not set or does not exist.");
                return;
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Text, $"Mod Organizer 2 base directory found at: {this.baseDirectory}");
            }

            if (!File.Exists(Path.Combine(this.baseDirectory, "ModOrganizer.ini")))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"ModOrganizer.ini\" file does not exist in Mod Organizer 2 base directory.");
                return;
            }

            string modOrganizerIniContent = File.ReadAllText(Path.Combine(this.baseDirectory, "ModOrganizer.ini"));
            var gamePathMathch = Regex.Match(modOrganizerIniContent, @"^gamePath\s*=\s*@ByteArray\((.+)\)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (gamePathMathch.Success)
            {
                string gamePath = gamePathMathch.Groups[1].Value.Trim();
                if (Directory.Exists(gamePath))
                {
                    this.MorrowindDirectory = gamePath;
                    CustomLogger.WriteLine(LogLevel.Text, $"Morrowind directory from ModOrganizer.ini: {this.MorrowindDirectory}");
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Error, "Morrowind directory specified in ModOrganizer.ini does not exist.");
                    return;
                }
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Error, "Failed to find Morrowind directory in ModOrganizer.ini.");
                return;
            }

            if (!Directory.Exists(Path.Combine(this.baseDirectory, "profiles")))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"profiles\" directory does not exist in Mod Organizer 2 base directory.");
                return;
            }
            if (!Directory.Exists(Path.Combine(this.baseDirectory, "mods")))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"mods\" directory does not exist in Mod Organizer 2 base directory.");
                return;
            }
            if (!Directory.Exists(Path.Combine(this.baseDirectory, "overwrite")))
            {
                CustomLogger.WriteLine(LogLevel.Warn, "\"overwrite\" directory does not exist in Mod Organizer 2 base directory.");
            }


            string profilesDirectory = Path.Combine(this.baseDirectory, "profiles");
            foreach (string profilePath in Directory.GetDirectories(profilesDirectory))
            {
                ProfileData profileData = new(new(), new(), new());

                string profileName = Path.GetFileName(profilePath);

                string modlistTextPath = Path.Combine(profilePath, "modlist.txt");
                if (!File.Exists(modlistTextPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"\"modlist.txt\" file does not exist in profile: {profileName}");
                    continue;
                }
                string modlistText = File.ReadAllText(modlistTextPath);

                var activeModsRegex = Regex.Matches(modlistText, @"^\+(.*)$", RegexOptions.Multiline);
                foreach (Match match in activeModsRegex)
                {
                    string modName = match.Groups[1].Value.Trim();
                    profileData.Managed.Add(modName);
                    var modPath = Path.Combine(this.baseDirectory, "mods", modName);
                    if (!string.IsNullOrEmpty(modName) && Directory.Exists(modPath))
                    {
                        profileData.Data.Add(modPath);
                    }
                    else
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, $"Mod directory does not exist for: {modName} in profile: {profileName}");
                    }
                }

                string morrowindIniPath = Path.Combine(profilePath, "Morrowind.ini");
                if (File.Exists(morrowindIniPath))
                {
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

                    profileData.Content.AddRange(gameFiles.Values);
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"\"Morrowind.ini\" file does not exist in profile: {profileName}");
                    continue;
                }


                this.profiles.Add(profilePath, profileData);
                CustomLogger.WriteLine(LogLevel.Text, $"MO2 data for profile \"{profileName}\" loaded successfully.");
            }
        }


        public MO2DataHandler() : this(null)
        {

        }
    }
}
