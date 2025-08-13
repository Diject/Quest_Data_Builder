using Quest_Data_Builder.Logger;
using System.Text;
using System.Text.RegularExpressions;

namespace Quest_Data_Builder.Initializer
{
    internal class OMWDataHandler
    {
        public readonly InitializatorType Type = InitializatorType.OMWHandler;

        private readonly Dictionary<string, (List<string> data, List<string> content)> profilesData = new();
        private readonly string? launcherCfgPath;
        private readonly string? openmwCfgPath;
        private string? encoding;


        public bool IsValid => !string.IsNullOrEmpty(this.launcherCfgPath) && !string.IsNullOrEmpty(this.openmwCfgPath) && this.profilesData.Count > 0;
        public string? CurrentProfile;
        public List<string> ProfileNames => this.profilesData.Keys.ToList();
        public Encoding? Encoding
        {
            get
            {
                if (string.IsNullOrEmpty(this.encoding))
                {
                    return null;
                }
                try
                {
                    return Encoding.GetEncoding(this.encoding);
                }
                catch (ArgumentException)
                {
                    CustomLogger.WriteLine(LogLevel.Error, $"Invalid encoding specified: {this.encoding}.");
                    return null;
                }
            }
        }



        public OMWDataHandler(string? launcherCfgPathParam, string? openmwCfgPathParam)
        {
            if (launcherCfgPathParam != null && this.LoadLauncherCfg(launcherCfgPathParam))
            {
                this.launcherCfgPath = launcherCfgPathParam;
                CustomLogger.WriteLine(LogLevel.Text, $"OpenMW launcher.cfg data loaded successfully from {launcherCfgPathParam}");
            }

            if (openmwCfgPathParam != null && this.LoadOpenMWCfg(openmwCfgPathParam))
            {
                this.openmwCfgPath = openmwCfgPathParam;
                CustomLogger.WriteLine(LogLevel.Text, $"OpenMW openmw.cfg data loaded successfully from {openmwCfgPathParam}");
            }

            if (!string.IsNullOrEmpty(this.launcherCfgPath) && !string.IsNullOrEmpty(this.openmwCfgPath))
            {
                return;
            }

            string? cfgDirectory = this.GetCfgDirectory();
            if (cfgDirectory == null)
            {
                CustomLogger.WriteLine(LogLevel.Error, "Failed to find OpenMW configuration directory.");
                return;
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Text, "OpenMW configuration directory found at: " + cfgDirectory);
            }

            var launcherCfgPath = Path.Combine(cfgDirectory, "launcher.cfg");
            if (string.IsNullOrEmpty(this.launcherCfgPath) && this.LoadLauncherCfg(launcherCfgPath))
            {
                this.launcherCfgPath = launcherCfgPath;
                CustomLogger.WriteLine(LogLevel.Text, "OpenMW launcher.cfg data loaded successfully from: " + this.launcherCfgPath);
            }

            var openmwCfgPath = Path.Combine(cfgDirectory, "openmw.cfg");
            if (string.IsNullOrEmpty(this.openmwCfgPath) && this.LoadOpenMWCfg(openmwCfgPath))
            {
                this.openmwCfgPath = openmwCfgPath;
                CustomLogger.WriteLine(LogLevel.Text, "OpenMW openmw.cfg data loaded successfully from: " + this.openmwCfgPath);
            }
        }


        public OMWDataHandler() : this(null, null)
        {

        }


        private string? GetCfgDirectory()
        {
            if (OperatingSystem.IsWindows())
            {
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string myGamesPath = Path.Combine(myDocumentsPath, "My Games");
                string omwPath = Path.Combine(myGamesPath, "OpenMW");
                if (!Directory.Exists(myGamesPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "My Games directory not found in: " + myDocumentsPath);
                    return null;
                }
                if (!Directory.Exists(omwPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW directory not found in: " + myGamesPath);
                    return null;
                }
                return omwPath;
            }
            else if (OperatingSystem.IsLinux())
            {
                string? configHomePath = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? "$HOME/.config";
                if (string.IsNullOrEmpty(configHomePath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "XDG_CONFIG_HOME environment variable not found.");
                    return null;
                }
                configHomePath = FileLocator.ExpandPath(configHomePath);
                string configDirPath = Path.Combine(configHomePath, "openmw");
                if (!Directory.Exists(configDirPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW directory not found in: " + configHomePath);
                    return null;
                }
                return configDirPath;
            }
            else if (OperatingSystem.IsMacOS())
            {
                string? homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(homePath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "User profile path not found.");
                    return null;
                }
                string configDirPath = Path.Combine(homePath, "Library", "Preferences", "openmw");
                if (!Directory.Exists(configDirPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW directory not found in: " + Path.Combine(homePath, "Library", "Preferences"));
                    return null;
                }
                return configDirPath;
            }
            else
            {
                CustomLogger.WriteLine(LogLevel.Warn, "Unsupported operating system for OpenMW data initialization.");
            }
            return null;
        }


        private bool LoadOpenMWCfg(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                CustomLogger.WriteLine(LogLevel.Warn, "OpenMW openmw.cfg file does not exist at: " + filePath);
                return false;
            }

            // Custom parser because OpenMW's openmw.cfg is not a standard INI file
            var text = File.ReadAllText(Environment.ExpandEnvironmentVariables(filePath), Encoding.UTF8);

            var entries = Regex.Matches(text, @"(\w+)\s*=\s*(.+)", RegexOptions.IgnoreCase);
            if (entries.Count == 0)
            {
                CustomLogger.WriteLine(LogLevel.Warn, "No data entries found in OpenMW openmw.cfg.");
                return false;
            }

            bool hadDefaultProfile = this.profilesData.ContainsKey("Default");
            if (this.CurrentProfile == null)
            {
                this.CurrentProfile = "Default";
                if (!this.profilesData.TryGetValue(this.CurrentProfile, out var profileData))
                {
                    profileData = (new(), new());
                    this.profilesData.Add(this.CurrentProfile, profileData);
                }
            }

            foreach (Match entry in entries)
            {
                var key = entry.Groups[1].Value.Trim().ToLower();
                var value = entry.Groups[2].Value.Trim();

                if (key.Equals("encoding", StringComparison.OrdinalIgnoreCase))
                {
                    this.encoding = value;
                    CustomLogger.WriteLine(LogLevel.Info, $"OpenMW encoding is {this.encoding}");
                    if (hadDefaultProfile) break; else continue;
                }

                if (hadDefaultProfile) continue;

                if (this.profilesData.TryGetValue(this.CurrentProfile, out var profileData))
                {
                    switch (key)
                    {
                        case "data":
                            profileData.data.Add(value);
                            break;

                        case "content":
                            profileData.content.Add(value);
                            break;

                        default:
                            continue;
                    }
                }
            }

            return true;
        }


        private bool LoadLauncherCfg(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                CustomLogger.WriteLine(LogLevel.Warn, "OpenMW launcher.cfg file does not exist at: " + filePath);
                return false;
            }
            // Custom parser because OpenMW's launcher.cfg is not a standard INI file
            var text = File.ReadAllText(Environment.ExpandEnvironmentVariables(filePath), Encoding.UTF8);

            var profilesSection = Regex.Match(text, @"\[Profiles\](.*?)\[|$", RegexOptions.Singleline);
            if (!profilesSection.Success)
            {
                CustomLogger.WriteLine(LogLevel.Warn, "Profiles section not found in OpenMW launcher.cfg.");
                return false;
            }
            var profileSectionText = profilesSection.Groups[1].Value.Trim();

            var currentProfileMatch = Regex.Match(profileSectionText, @"currentprofile\s*=\s*(\w+)", RegexOptions.IgnoreCase);
            if (!currentProfileMatch.Success)
            {
                CustomLogger.WriteLine(LogLevel.Warn, "Current profile not found in OpenMW launcher.cfg.");
                return false;
            }
            this.CurrentProfile = currentProfileMatch.Groups[1].Value.Trim();

            CustomLogger.WriteLine(LogLevel.Info, $"OpenMW current profile: {CurrentProfile}");

            var profileEntries = Regex.Matches(profileSectionText, @"(\w+)/(\w+)\s*=\s*(.+)", RegexOptions.IgnoreCase);
            if (profileEntries.Count == 0)
            {
                CustomLogger.WriteLine(LogLevel.Warn, "No profile data found in OpenMW launcher.cfg.");
                return false;
            }

            foreach (Match profileEntry in profileEntries)
            {
                var profileName = profileEntry.Groups[1].Value.Trim();
                var dataType = profileEntry.Groups[2].Value.Trim();
                var dataValue = profileEntry.Groups[3].Value.Trim();

                if (!this.profilesData.TryGetValue(profileName, out var profileData))
                {
                    profileData = (new(), new());
                    this.profilesData.Add(profileName, profileData);
                }

                switch (dataType)
                {
                    case "data":
                        profileData.data.Add(dataValue);
                        break;

                    case "content":
                        profileData.content.Add(dataValue);
                        break;

                    default:
                        continue;
                }
            }

            foreach (var profileName in this.profilesData.Keys)
            {
                CustomLogger.WriteLine(LogLevel.Text, $"OpenMW profile \"{profileName}\" data loaded successfully.");
            }

            return true;
        }


        public List<string>? GetFullGameFilePaths(string profileName)
        {
            if (string.IsNullOrEmpty(profileName) || !this.profilesData.TryGetValue(profileName, out var profileData))
            {
                CustomLogger.WriteLine(LogLevel.Warn, $"Profile \"{profileName}\" not found in OpenMW data.");
                return null;
            }

            return FileLocator.ResolveFullFilePaths(profileData.content, profileData.data);
        }
    }
}
