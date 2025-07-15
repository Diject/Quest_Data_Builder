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
        public readonly InitializatorType Type = InitializatorType.MO2Handler;

        private readonly Dictionary<string, ProfileData> profiles = new();
        private readonly string? baseDirectory;

        public readonly string? MorrowindDirectory;
        public readonly string? MorrowindDataDirectory;
        public string? CurrentProfile;
        public List<string> ProfileNames => profiles.Keys.ToList();


        public bool IsValid => !string.IsNullOrEmpty(this.MorrowindDirectory) && this.profiles.Count > 0;


        public MO2DataHandler(string? baseDirectory)
        {
            if (!OperatingSystem.IsWindows()) return;

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
                string gamePath = gamePathMathch.Groups[1].Value.Trim().Replace(@"\\", @"\");
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

            this.MorrowindDataDirectory = Path.Combine(this.MorrowindDirectory, "Data Files");
            if (!Directory.Exists(this.MorrowindDataDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"Data Files\" directory does not exist in Morrowind directory.");
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
                profileData.Data.Add(this.MorrowindDataDirectory);

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
                    var gameFiles = MorrowindBaseDataHandler.GetMorrowindIniGameFiles(morrowindIniPath);
                    if (gameFiles is not null)
                    {
                        profileData.Content.AddRange(gameFiles);
                    }
                    else
                    {
                        CustomLogger.WriteLine(LogLevel.Warn, $"No game files found in \"Morrowind.ini\" for profile: {profileName}");
                    }
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"\"Morrowind.ini\" file does not exist in profile: {profileName}");
                    continue;
                }


                this.profiles.Add(profileName, profileData);
                CustomLogger.WriteLine(LogLevel.Text, $"MO2 data for profile \"{profileName}\" loaded successfully.");
            }


            var currentProfileMatch = Regex.Match(modOrganizerIniContent, @"^selected_profile\s*=\s*@ByteArray\((.+)\)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (currentProfileMatch.Success)
            {
                var currentProfileName = currentProfileMatch.Groups[1].Value;
                if (this.profiles.ContainsKey(currentProfileName))
                {
                    this.CurrentProfile = currentProfileName;
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, $"Current profile \"{currentProfileName}\" not found in loaded profiles.");
                }
            }
        }


        public MO2DataHandler() : this(null)
        {

        }


        public List<string>? GetFullGameFilePaths(string profileName)
        {
            if (this.profiles.TryGetValue(profileName, out ProfileData? profileData))
            {
                return FileLocator.ResolveFullFilePaths(profileData.Content, profileData.Data);
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Error, $"Profile \"{profileName}\" not found.");
                return null;
            }
        }
    }
}
