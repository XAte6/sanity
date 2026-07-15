using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Script.Serialization;

namespace Sanity
{
    public class RegexRulesCatalog
    {
        public int Version { get; set; }
        public List<UrlRule> Rules { get; set; }
    }

    public static class DefaultRules
    {
        public const string FileName = "regex-rules.json";

        public static string LocalPath
        {
            get
            {
                return Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    FileName);
            }
        }

        public static RegexRulesCatalog LoadLocal()
        {
            var path = LocalPath;
            if (!File.Exists(path))
            {
                var repoDefault = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(path) ?? ".",
                    "..", "..", "defaults", FileName));
                if (File.Exists(repoDefault))
                    path = repoDefault;
            }

            if (!File.Exists(path))
                throw new FileNotFoundException("Default regex rules file not found.", path);

            return Parse(File.ReadAllText(path));
        }

        public static RegexRulesCatalog FetchRemote()
        {
            EnsureTls();
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "Sanity";
                client.Encoding = System.Text.Encoding.UTF8;
                return Parse(client.DownloadString(AppLinks.RegexRulesRaw));
            }
        }

        public static RegexRulesCatalog LoadForReset()
        {
            try
            {
                return FetchRemote();
            }
            catch
            {
                return LoadLocal();
            }
        }

        public static RegexRulesCatalog Parse(string json)
        {
            var normalized = json
                .Replace("\"version\":", "\"Version\":")
                .Replace("\"rules\":", "\"Rules\":")
                .Replace("\"domain\":", "\"Domain\":")
                .Replace("\"regex\":", "\"Regex\":");

            var catalog = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                .Deserialize<RegexRulesCatalog>(normalized);

            if (catalog == null || catalog.Rules == null || catalog.Rules.Count == 0)
                throw new InvalidOperationException("Regex rules catalog is empty.");

            if (catalog.Version < 1)
                catalog.Version = 1;

            return catalog;
        }

        private static void EnsureTls()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            }
            catch
            {
            }
        }
    }
}
