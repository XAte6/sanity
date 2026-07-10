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
        public DateTime? SleepUntil { get; set; }
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
                if (config.TargetBrowser == null)
                    config.TargetBrowser = string.Empty;
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
                .Replace("\"SleepUntil\":", "\"sleepUntil\":")
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
                .Replace("\"sleepUntil\":", "\"SleepUntil\":")
                .Replace("\"rules\":", "\"Rules\":")
                .Replace("\"domain\":", "\"Domain\":")
                .Replace("\"regex\":", "\"Regex\":");
        }

        public static AppConfig CreateDefault()
        {
            return new AppConfig
            {
                Enabled = true,
                LinkProxyEnabled = true,
                TargetBrowser = string.Empty,
                LaunchOnStartup = false,
                NotificationsEnabled = true,
                SleepUntil = null,
                Rules = CreateDefaultRules()
            };
        }

        private static List<UrlRule> CreateDefaultRules()
        {
            var rules = new List<UrlRule>();

            // Universal tracking parameters
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](utm_[a-zA-Z0-9_]+=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](fbclid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](gclid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](msclkid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](twclid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](dclid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](gbraid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](wbraid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](srsltid=[^&]*)" });
            rules.Add(new UrlRule { Domain = "*", Regex = "[?&](mc_[a-z]+=[^&]*)" });

            AddPlatformRules(rules, new[] { "youtube.com", "youtu.be" },
                "si=[^&]*", "is=[^&]*", "feature=[^&]*", "pp=[^&]*", "embeds_referring_euri=[^&]*");

            AddPlatformRules(rules, new[] { "amazon.com", "amazon.co.uk", "amazon.de", "amazon.fr", "amazon.ca", "amazon.es", "amazon.it", "amazon.co.jp", "amzn.to", "a.co" },
                "tag=[^&]*", "linkCode=[^&]*", "ref_=[^&]*", "ascsubtag=[^&]*", "creative=[^&]*",
                "creativeASIN=[^&]*", "linkId=[^&]*", "pd_rd_w=[^&]*", "pd_rd_wg=[^&]*", "pd_rd_r=[^&]*",
                "pf_rd_p=[^&]*", "pf_rd_r=[^&]*");

            AddPlatformRules(rules, new[] { "google.com", "google.co.uk", "google.de", "google.fr", "google.ca", "google.com.au" },
                "ved=[^&]*", "usg=[^&]*", "sa=[^&]*", "source=[^&]*", "gs_lcp=[^&]*", "ei=[^&]*",
                "sclient=[^&]*", "oq=[^&]*", "gs_l=[^&]*", "cad=[^&]*");

            AddPlatformRules(rules, new[] { "facebook.com", "fb.com", "fb.watch", "m.facebook.com" },
                "ref=[^&]*", "refid=[^&]*", "__tn__=[^&]*", "__cft__=[^&]*", "mibextid=[^&]*");

            AddPlatformRules(rules, new[] { "instagram.com" },
                "igsh=[^&]*", "ig_rid=[^&]*");

            AddPlatformRules(rules, new[] { "tiktok.com", "vm.tiktok.com", "www.tiktok.com" },
                "_t=[^&]*", "_r=[^&]*", "share_app_id=[^&]*", "share_link_id=[^&]*",
                "tt_medium=[^&]*", "tt_source=[^&]*", "is_from_webapp=[^&]*");

            AddPlatformRules(rules, new[] { "x.com", "twitter.com", "t.co", "mobile.twitter.com" },
                "s=[^&]*", "ref_src=[^&]*", "ref_url=[^&]*", "t=[^&]*");

            AddPlatformRules(rules, new[] { "reddit.com", "old.reddit.com", "www.reddit.com", "redd.it", "new.reddit.com" },
                "share_id=[^&]*", "ref_source=[^&]*", "ref_campaign=[^&]*", "embed=[^&]*");

            return rules;
        }

        private static void AddPlatformRules(List<UrlRule> rules, string[] domains, params string[] paramPatterns)
        {
            foreach (var domain in domains)
            {
                foreach (var param in paramPatterns)
                {
                    rules.Add(new UrlRule
                    {
                        Domain = domain,
                        Regex = "[?&](" + param + ")"
                    });
                }
            }
        }
    }
}
