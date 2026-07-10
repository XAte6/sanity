using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;

namespace Sanity
{
    public class UsageMetrics
    {
        public int LinksCleaned { get; set; }
        public Dictionary<string, int> Domains { get; set; }

        [ScriptIgnore]
        public int DomainCount
        {
            get { return Domains == null ? 0 : Domains.Count; }
        }

        public static string MetricsPath
        {
            get
            {
                return Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "metrics.json");
            }
        }

        public static UsageMetrics Load()
        {
            var path = MetricsPath;
            if (!File.Exists(path))
                return Empty();

            try
            {
                var raw = File.ReadAllText(path);
                var json = NormalizeJsonForLoad(raw);
                var serializer = new JavaScriptSerializer();
                var metrics = serializer.Deserialize<UsageMetrics>(json);
                if (metrics == null)
                    return Empty();
                if (metrics.Domains == null)
                    metrics.Domains = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                return metrics;
            }
            catch
            {
                return Empty();
            }
        }

        public static void RecordClean(string url)
        {
            var host = ExtractHost(url);
            if (string.IsNullOrEmpty(host))
                return;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    var metrics = Load();
                    metrics.LinksCleaned++;
                    int count;
                    if (!metrics.Domains.TryGetValue(host, out count))
                        count = 0;
                    metrics.Domains[host] = count + 1;
                    metrics.Save();
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(40);
                }
            }
        }

        public void Save()
        {
            if (Domains == null)
                Domains = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var json = FormatJsonForSave(serializer.Serialize(this));
            var path = MetricsPath;
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);
        }

        public string SummaryText()
        {
            return LinksCleaned + " link" + (LinksCleaned == 1 ? "" : "s")
                + " cleaned across "
                + DomainCount + " domain" + (DomainCount == 1 ? "" : "s");
        }

        public List<KeyValuePair<string, int>> GetDomainsByCount()
        {
            if (Domains == null || Domains.Count == 0)
                return new List<KeyValuePair<string, int>>();

            return Domains
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static UsageMetrics Empty()
        {
            return new UsageMetrics
            {
                LinksCleaned = 0,
                Domains = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private static string ExtractHost(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            Uri uri;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri))
                return null;

            if (string.IsNullOrEmpty(uri.Host))
                return null;

            return uri.Host.ToLowerInvariant();
        }

        private static string FormatJsonForSave(string json)
        {
            return json
                .Replace("\"LinksCleaned\":", "\"linksCleaned\":")
                .Replace("\"Domains\":", "\"domains\":");
        }

        private static string NormalizeJsonForLoad(string json)
        {
            return json
                .Replace("\"linksCleaned\":", "\"LinksCleaned\":")
                .Replace("\"domains\":", "\"Domains\":");
        }
    }
}
