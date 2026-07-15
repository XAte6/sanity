using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace Sanity
{
    public static class UpdateChecker
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(7);
        private static int _running;

        public static void RunAsync(AppConfig config, SynchronizationContext ui)
        {
            if (config == null || ui == null)
                return;
            if (!ShouldStart(config))
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                RegexRulesCatalog remoteRules = null;
                DateTime? remoteReleaseDate = null;

                try
                {
                    remoteRules = DefaultRules.FetchRemote();
                }
                catch
                {
                }

                try
                {
                    remoteReleaseDate = FetchReleaseFileDate();
                }
                catch
                {
                }

                ui.Post(__ =>
                {
                    try
                    {
                        Prompt(config, remoteRules, remoteReleaseDate);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _running, 0);
                    }
                }, null);
            });
        }

        /// <summary>
        /// Blocks while fetching and prompting — for short-lived --open processes.
        /// </summary>
        public static void RunSync(AppConfig config)
        {
            if (config == null || !ShouldStart(config))
                return;

            try
            {
                RegexRulesCatalog remoteRules = null;
                DateTime? remoteReleaseDate = null;

                try
                {
                    remoteRules = DefaultRules.FetchRemote();
                }
                catch
                {
                }

                try
                {
                    remoteReleaseDate = FetchReleaseFileDate();
                }
                catch
                {
                }

                Prompt(config, remoteRules, remoteReleaseDate);
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);
            }
        }

        private static bool ShouldStart(AppConfig config)
        {
            if (!config.UpdatesEnabled)
                return false;
            if (!IsDue(config))
                return false;
            return Interlocked.CompareExchange(ref _running, 1, 0) == 0;
        }

        private static bool IsDue(AppConfig config)
        {
            if (!config.LastUpdateCheck.HasValue)
                return true;

            return DateTime.UtcNow - config.LastUpdateCheck.Value.ToUniversalTime() >= CheckInterval;
        }

        private static void Prompt(
            AppConfig config,
            RegexRulesCatalog remoteRules,
            DateTime? remoteReleaseDate)
        {
            try
            {
                if (remoteRules != null && remoteRules.Version > config.RulesVersion)
                {
                    var answer = MessageBox.Show(
                        "A newer regex list is available (v" + remoteRules.Version
                        + "). Replace your current rules with the updated defaults?",
                        "Sanity — regex update",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (answer == DialogResult.Yes)
                    {
                        config.Rules = remoteRules.Rules;
                        config.RulesVersion = remoteRules.Version;
                    }
                }

                var localDate = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
                if (remoteReleaseDate.HasValue && remoteReleaseDate.Value > localDate.AddMinutes(1))
                {
                    var answer = MessageBox.Show(
                        "A newer Sanity build is available on GitHub"
                        + " (release file dated "
                        + remoteReleaseDate.Value.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)
                        + "). Open the download page?",
                        "Sanity — app update",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (answer == DialogResult.Yes)
                        AppLinks.Open(AppLinks.ReleaseAsset);
                }
            }
            finally
            {
                config.LastUpdateCheck = DateTime.UtcNow;
                config.Save();
            }
        }

        private static DateTime? FetchReleaseFileDate()
        {
            EnsureTls();
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "Sanity";
                client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                var json = client.DownloadString(AppLinks.ReleaseCommitsApi);
                var commits = new JavaScriptSerializer().DeserializeObject(json) as object[];
                if (commits == null || commits.Length == 0)
                    return null;

                var first = commits[0] as System.Collections.Generic.Dictionary<string, object>;
                if (first == null || !first.ContainsKey("commit"))
                    return null;

                var commit = first["commit"] as System.Collections.Generic.Dictionary<string, object>;
                if (commit == null || !commit.ContainsKey("committer"))
                    return null;

                var committer = commit["committer"] as System.Collections.Generic.Dictionary<string, object>;
                if (committer == null || !committer.ContainsKey("date"))
                    return null;

                var dateText = committer["date"] as string;
                if (string.IsNullOrEmpty(dateText))
                    return null;

                DateTime parsed;
                if (!DateTime.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsed))
                    return null;

                return parsed.ToUniversalTime();
            }
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
