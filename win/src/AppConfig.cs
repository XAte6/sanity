using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Sanity
{
    public class UrlRule
    {
        public string Domain { get; set; }
        public string Regex { get; set; }
    }

    public class AppConfig
    {
        public bool Enabled { get; set; }
        public bool LinkProxyEnabled { get; set; }
        public string TargetBrowser { get; set; }
        public bool LaunchOnStartup { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool UpdatesEnabled { get; set; }
        public bool SetupCompleted { get; set; }
        public DateTime? SleepUntil { get; set; }
        public int RulesVersion { get; set; }
        public DateTime? LastUpdateCheck { get; set; }
        public List<UrlRule> Rules { get; set; }

        public static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "config.json");
            }
        }

        [ScriptIgnore]
        public bool IsActive
        {
            get
            {
                if (!Enabled)
                    return false;
                if (SleepUntil.HasValue && SleepUntil.Value > DateTime.Now)
                    return false;
                return true;
            }
        }

        [ScriptIgnore]
        public bool IsLinkProxyActive
        {
            get { return IsActive; }
        }

        public static AppConfig Load()
        {
            var path = ConfigPath;
            if (!File.Exists(path))
            {
                var defaults = CreateDefault();
                defaults.Save();
                return defaults;
            }

            try
            {
                var raw = File.ReadAllText(path);
                var json = NormalizeJsonForLoad(raw);
                var serializer = new JavaScriptSerializer();
                var config = serializer.Deserialize<AppConfig>(json);
                if (config.Rules == null)
                    config.Rules = new List<UrlRule>();
                if (!raw.Contains("notificationsEnabled"))
                    config.NotificationsEnabled = true;
                if (!raw.Contains("updatesEnabled"))
                    config.UpdatesEnabled = true;
                if (config.TargetBrowser == null)
                    config.TargetBrowser = string.Empty;
                // Existing installs lack this key — treat as already set up so upgrades skip the wizard.
                if (!raw.Contains("setupCompleted"))
                    config.SetupCompleted = true;
                // Existing installs without rulesVersion match the first shipped catalog.
                if (!raw.Contains("rulesVersion"))
                    config.RulesVersion = 1;
                return config;
            }
            catch
            {
                return CreateDefault();
            }
        }

        public void Save()
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var json = FormatJsonForSave(serializer.Serialize(this));
            File.WriteAllText(ConfigPath, json);
        }

        private static string FormatJsonForSave(string json)
        {
            return json
                .Replace("\"Enabled\":", "\"enabled\":")
                .Replace("\"LinkProxyEnabled\":", "\"linkProxyEnabled\":")
                .Replace("\"TargetBrowser\":", "\"targetBrowser\":")
                .Replace("\"LaunchOnStartup\":", "\"launchOnStartup\":")
                .Replace("\"NotificationsEnabled\":", "\"notificationsEnabled\":")
                .Replace("\"UpdatesEnabled\":", "\"updatesEnabled\":")
                .Replace("\"SetupCompleted\":", "\"setupCompleted\":")
                .Replace("\"SleepUntil\":", "\"sleepUntil\":")
                .Replace("\"RulesVersion\":", "\"rulesVersion\":")
                .Replace("\"LastUpdateCheck\":", "\"lastUpdateCheck\":")
                .Replace("\"Rules\":", "\"rules\":")
                .Replace("\"Domain\":", "\"domain\":")
                .Replace("\"Regex\":", "\"regex\":");
        }

        private static string NormalizeJsonForLoad(string json)
        {
            return json
                .Replace("\"enabled\":", "\"Enabled\":")
                .Replace("\"linkProxyEnabled\":", "\"LinkProxyEnabled\":")
                .Replace("\"targetBrowser\":", "\"TargetBrowser\":")
                .Replace("\"launchOnStartup\":", "\"LaunchOnStartup\":")
                .Replace("\"notificationsEnabled\":", "\"NotificationsEnabled\":")
                .Replace("\"updatesEnabled\":", "\"UpdatesEnabled\":")
                .Replace("\"setupCompleted\":", "\"SetupCompleted\":")
                .Replace("\"sleepUntil\":", "\"SleepUntil\":")
                .Replace("\"rulesVersion\":", "\"RulesVersion\":")
                .Replace("\"lastUpdateCheck\":", "\"LastUpdateCheck\":")
                .Replace("\"rules\":", "\"Rules\":")
                .Replace("\"domain\":", "\"Domain\":")
                .Replace("\"regex\":", "\"Regex\":");
        }

        public static AppConfig CreateDefault()
        {
            var catalog = DefaultRules.LoadLocal();
            return new AppConfig
            {
                Enabled = false,
                LinkProxyEnabled = false,
                TargetBrowser = string.Empty,
                LaunchOnStartup = false,
                NotificationsEnabled = true,
                UpdatesEnabled = true,
                SetupCompleted = false,
                SleepUntil = null,
                RulesVersion = catalog.Version,
                LastUpdateCheck = null,
                Rules = catalog.Rules
            };
        }
    }
}
