using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Sanity
{
    public class BrowserInfo
    {
        public string Name { get; set; }
        public string ProgId { get; set; }
        public string ExecutablePath { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class BrowserHelper
    {
        private const string UserChoiceHttp =
            @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";

        public static string GetDefaultBrowserProgId()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(UserChoiceHttp, false))
            {
                var progId = key != null ? key.GetValue("ProgId") as string : null;
                return string.IsNullOrEmpty(progId) ? null : progId;
            }
        }

        public static string GetDefaultBrowserPath()
        {
            var progId = GetDefaultBrowserProgId();
            if (string.IsNullOrEmpty(progId))
                return null;

            return ResolveProgIdToPath(progId);
        }

        public static string ResolveProgIdToPath(string progId)
        {
            if (string.IsNullOrEmpty(progId))
                return null;

            var command = GetOpenCommandForProgId(progId);
            if (string.IsNullOrEmpty(command))
                return null;

            return ExtractExecutablePath(command);
        }

        public static string GetOpenCommandForProgId(string progId)
        {
            using (var key = Registry.ClassesRoot.OpenSubKey(progId + @"\shell\open\command", false))
            {
                return key != null ? key.GetValue(null) as string : null;
            }
        }

        public static List<BrowserInfo> GetInstalledBrowsers()
        {
            var browsers = new List<BrowserInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var clientsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet", false))
            {
                if (clientsKey != null)
                {
                    foreach (var subKeyName in clientsKey.GetSubKeyNames())
                    {
                        using (var browserKey = clientsKey.OpenSubKey(subKeyName, false))
                        {
                            if (browserKey == null)
                                continue;

                            var name = browserKey.GetValue(null) as string ?? subKeyName;
                            var command = GetOpenCommandForProgId(subKeyName);
                            var path = ExtractExecutablePath(command);
                            if (string.IsNullOrEmpty(path) || !seen.Add(path))
                                continue;

                            browsers.Add(new BrowserInfo
                            {
                                Name = name,
                                ProgId = subKeyName,
                                ExecutablePath = path
                            });
                        }
                    }
                }
            }

            var defaultProgId = GetDefaultBrowserProgId();
            if (!string.IsNullOrEmpty(defaultProgId))
            {
                var defaultPath = ResolveProgIdToPath(defaultProgId);
                if (!string.IsNullOrEmpty(defaultPath) && seen.Add(defaultPath))
                {
                    browsers.Insert(0, new BrowserInfo
                    {
                        Name = "System default (" + defaultProgId + ")",
                        ProgId = defaultProgId,
                        ExecutablePath = defaultPath
                    });
                }
            }

            return browsers;
        }

        public static bool TryLaunchBrowser(string targetBrowser, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var sanityPath = System.Windows.Forms.Application.ExecutablePath;

            if (!string.IsNullOrWhiteSpace(targetBrowser))
            {
                if (File.Exists(targetBrowser) &&
                    !PathsEqual(targetBrowser, sanityPath))
                {
                    Process.Start(targetBrowser, QuoteUrl(url));
                    return true;
                }

                var path = ResolveProgIdToPath(targetBrowser);
                if (!string.IsNullOrEmpty(path) && !PathsEqual(path, sanityPath))
                {
                    Process.Start(path, QuoteUrl(url));
                    return true;
                }
            }

            var fallback = GetDefaultBrowserPath();
            if (!string.IsNullOrEmpty(fallback) && !PathsEqual(fallback, sanityPath))
            {
                Process.Start(fallback, QuoteUrl(url));
                return true;
            }

            Process.Start(url);
            return true;
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string QuoteUrl(string url)
        {
            if (url.Contains(" ") && !url.StartsWith("\"", StringComparison.Ordinal))
                return "\"" + url + "\"";
            return url;
        }

        private static string ExtractExecutablePath(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return null;

            command = command.Trim();
            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                var end = command.IndexOf('"', 1);
                if (end > 1)
                    return command.Substring(1, end - 1);
            }

            var space = command.IndexOf(' ');
            return space > 0 ? command.Substring(0, space) : command;
        }
    }
}
