using Quest_Data_Builder.Logger;
using System.Text.RegularExpressions;

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
                baseDirectory = FileLocator.ExpandPath(baseDirectory);
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

            string? modOrganizerIniPath = Path.Combine(this.baseDirectory, "ModOrganizer.ini");
            if (!File.Exists(modOrganizerIniPath))
            {
                modOrganizerIniPath = null;
                string? mo2dir = null;
                string? localDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localDir))
                {
                    mo2dir = Path.Combine(localDir, "ModOrganizer", "Morrowind");
                }

                if (Directory.Exists(mo2dir))
                {
                    modOrganizerIniPath = Path.Combine(mo2dir, "ModOrganizer.ini");
                }

                if (String.IsNullOrEmpty(modOrganizerIniPath) || !File.Exists(modOrganizerIniPath))
                {
                    CustomLogger.WriteLine(LogLevel.Error, "\"ModOrganizer.ini\" file does not exist in Mod Organizer 2 base directory.");
                    return;
                }
            }


            string modOrganizerIniContent = File.ReadAllText(modOrganizerIniPath);

            var baseDirMatch = Regex.Match(modOrganizerIniContent, @"^base_directory\s*=\s*(.+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (baseDirMatch.Success)
            {
                this.baseDirectory = FileLocator.ExpandPath(baseDirMatch.Groups[1].Value.Trim().Replace(@"\\", @"\"));
            }


            var profilesDirMatch = Regex.Match(modOrganizerIniContent, @"^profiles_directory\s*=\s*(.+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string profilesDirectory = profilesDirMatch.Success
                ? profilesDirMatch.Groups[1].Value.Trim().Replace(@"%BASE_DIR%", this.baseDirectory)
                : Path.Combine(this.baseDirectory, "profiles");


            var modsDirMatch = Regex.Match(modOrganizerIniContent, @"^mod_directory\s*=\s*(.+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string modsDirectory = modsDirMatch.Success
                ? modsDirMatch.Groups[1].Value.Trim().Replace(@"%BASE_DIR%", this.baseDirectory)
                : Path.Combine(this.baseDirectory, "mods");


            var overwriteDirMatch = Regex.Match(modOrganizerIniContent, @"^overwrite_directory\s*=\s*(.+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string overwriteDirectory = overwriteDirMatch.Success
                ? overwriteDirMatch.Groups[1].Value.Trim().Replace(@"%BASE_DIR%", this.baseDirectory)
                : Path.Combine(this.baseDirectory, "overwrite");


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


            if (!Directory.Exists(profilesDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"profiles\" directory does not exist in Mod Organizer 2 base directory.");
                return;
            }
            if (!Directory.Exists(modsDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Error, "\"mods\" directory does not exist in Mod Organizer 2 base directory.");
                return;
            }
            if (!Directory.Exists(overwriteDirectory))
            {
                CustomLogger.WriteLine(LogLevel.Warn, "\"overwrite\" directory does not exist in Mod Organizer 2 base directory.");
            }


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
                    var modPath = Path.Combine(modsDirectory, modName);
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

                profileData.Data.Add(this.MorrowindDataDirectory);

                this.profiles.Add(profileName, profileData);
                CustomLogger.WriteLine(LogLevel.Text, $"MO2 profile \"{profileName}\" data loaded successfully.");
            }


            var currentProfileMatch = Regex.Match(modOrganizerIniContent, @"^selected_profile\s*=\s*@ByteArray\((.+)\)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (currentProfileMatch.Success)
            {
                var currentProfileName = currentProfileMatch.Groups[1].Value;
                CustomLogger.WriteLine(LogLevel.Text, $"MO2 current profile: \"{currentProfileName}\"");

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
