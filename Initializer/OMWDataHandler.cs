using Quest_Data_Builder.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quest_Data_Builder.Initializer
{
    internal class OMWDataHandler
    {

        private readonly Dictionary<string, (List<string> data, List<string> content)> profiles = new();
        private string? currentProfile;
        private readonly string? launcherCfgPath;

        public OMWDataHandler()
        {

            if (OperatingSystem.IsWindows())
            {
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string myGamesPath = Path.Combine(myDocumentsPath, "My Games");
                string omwPath = Path.Combine(myGamesPath, "OpenMW");

                if (!Directory.Exists(myGamesPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "My Games directory not found in: " + myDocumentsPath);
                    return;
                }

                if (!Directory.Exists(omwPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW directory not found in: " + myGamesPath);
                    return;
                }

                string launcherCfgPath = Path.Combine(omwPath, "launcher.cfg");
                if (!File.Exists(launcherCfgPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW launcher.cfg not found in: " + omwPath);
                    return;
                }

                this.launcherCfgPath = launcherCfgPath;

                if (this.LoadLauncherCfg(launcherCfgPath))
                {
                    CustomLogger.WriteLine(LogLevel.Text, "OpenMW launcher.cfg loaded successfully.");
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "Failed to load data from OpenMW launcher.cfg.");
                }
            }

            else if (OperatingSystem.IsLinux())
            {
                string? configHomePath = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(configHomePath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "XDG_CONFIG_HOME environment variable not found.");
                    return;
                }

                string configDirPath = Path.Combine(configHomePath, "openmw");
                if (!Directory.Exists(configDirPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW directory not found in: " + configHomePath);
                    return;
                }

                string launcherCfgPath = Path.Combine(configDirPath, "launcher.cfg");
                if (!File.Exists(launcherCfgPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW launcher.cfg not found in: " + configDirPath);
                    return;
                }

                this.launcherCfgPath = launcherCfgPath;
                if (this.LoadLauncherCfg(launcherCfgPath))
                {
                    CustomLogger.WriteLine(LogLevel.Text, "OpenMW launcher.cfg loaded successfully.");
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "Failed to load data from OpenMW launcher.cfg.");
                }
            }

            else if (OperatingSystem.IsMacOS())
            {
                string? homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(homePath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "User profile path not found.");
                    return;
                }

                string configDirPath = Path.Combine(homePath, "Library", "Preferences", "openmw");
                if (!Directory.Exists(configDirPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW directory not found in: " + Path.Combine(homePath, "Library", "Preferences"));
                    return;
                }

                string launcherCfgPath = Path.Combine(configDirPath, "launcher.cfg");
                if (!File.Exists(launcherCfgPath))
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "OpenMW launcher.cfg not found in: " + configDirPath);
                    return;
                }

                this.launcherCfgPath = launcherCfgPath;
                if (this.LoadLauncherCfg(launcherCfgPath))
                {
                    CustomLogger.WriteLine(LogLevel.Text, "OpenMW launcher.cfg loaded successfully.");
                }
                else
                {
                    CustomLogger.WriteLine(LogLevel.Warn, "Failed to load data from OpenMW launcher.cfg.");
                }
            }

            else
            {
                CustomLogger.WriteLine(LogLevel.Warn, "Unsupported operating system for OpenMW data initialization.");
            }

        }


        private bool LoadLauncherCfg(string filePath)
        {
            // Custom parser because OpenMW's launcher.cfg is not a standard INI file
            var text = File.ReadAllText(filePath, Encoding.UTF8);

            var profilesSection = Regex.Match(text, @"\[Profiles\](.*?)\[", RegexOptions.Singleline);
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
            this.currentProfile = currentProfileMatch.Groups[1].Value.Trim();

            CustomLogger.WriteLine(LogLevel.Info, $"OpenMW current profile: {currentProfile}");

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

                if (!this.profiles.TryGetValue(profileName, out var profileData))
                {
                    profileData = (new(), new());
                    this.profiles.Add(profileName, profileData);
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

            return true;
        }
    }
}
