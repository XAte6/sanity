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

            CollectStartMenuBrowsers(Registry.LocalMachine, @"SOFTWARE\Clients\StartMenuInternet", browsers, seen);
            CollectStartMenuBrowsers(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet", browsers, seen);
            CollectStartMenuBrowsers(Registry.CurrentUser, @"SOFTWARE\Clients\StartMenuInternet", browsers, seen);

            var defaultProgId = GetDefaultBrowserProgId();
            if (string.IsNullOrEmpty(defaultProgId))
                return browsers;

            var defaultPath = ResolveProgIdToPath(defaultProgId);
            if (string.IsNullOrEmpty(defaultPath))
                return browsers;

            var friendlyName = ResolveFriendlyName(defaultProgId, defaultPath, browsers);
            var existing = FindByPath(browsers, defaultPath);
            if (existing != null)
            {
                browsers.Remove(existing);
                browsers.Insert(0, new BrowserInfo
                {
                    Name = "System default (" + existing.Name + ")",
                    ProgId = defaultProgId,
                    ExecutablePath = existing.ExecutablePath
                });
                return browsers;
            }

            if (seen.Add(NormalizePathKey(defaultPath)))
            {
                browsers.Insert(0, new BrowserInfo
                {
                    Name = "System default (" + friendlyName + ")",
                    ProgId = defaultProgId,
                    ExecutablePath = defaultPath
                });
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

        private static void CollectStartMenuBrowsers(
            RegistryKey hive,
            string clientsPath,
            List<BrowserInfo> browsers,
            HashSet<string> seen)
        {
            using (var clientsKey = hive.OpenSubKey(clientsPath, false))
            {
                if (clientsKey == null)
                    return;

                foreach (var subKeyName in clientsKey.GetSubKeyNames())
                {
                    using (var browserKey = clientsKey.OpenSubKey(subKeyName, false))
                    {
                        if (browserKey == null)
                            continue;

                        var name = browserKey.GetValue(null) as string ?? subKeyName;
                        string command = null;
                        using (var commandKey = browserKey.OpenSubKey(@"shell\open\command", false))
                        {
                            if (commandKey != null)
                                command = commandKey.GetValue(null) as string;
                        }

                        if (string.IsNullOrEmpty(command))
                            command = GetOpenCommandForProgId(subKeyName);

                        var path = ExtractExecutablePath(command);
                        if (string.IsNullOrEmpty(path) || !File.Exists(path))
                            continue;

                        if (!seen.Add(NormalizePathKey(path)))
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

        private static string ResolveFriendlyName(string progId, string executablePath, List<BrowserInfo> known)
        {
            var match = FindByPath(known, executablePath);
            if (match != null && !string.IsNullOrWhiteSpace(match.Name))
                return match.Name;

            using (var appKey = Registry.ClassesRoot.OpenSubKey(progId + @"\Application", false))
            {
                if (appKey != null)
                {
                    var appName = appKey.GetValue("ApplicationName") as string;
                    if (!string.IsNullOrWhiteSpace(appName))
                        return appName;
                }
            }

            using (var progKey = Registry.ClassesRoot.OpenSubKey(progId, false))
            {
                if (progKey != null)
                {
                    var description = progKey.GetValue(null) as string;
                    if (!string.IsNullOrWhiteSpace(description)
                        && !string.Equals(description, progId, StringComparison.OrdinalIgnoreCase))
                        return description;
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                {
                    var info = FileVersionInfo.GetVersionInfo(executablePath);
                    if (!string.IsNullOrWhiteSpace(info.FileDescription))
                        return info.FileDescription;
                    if (!string.IsNullOrWhiteSpace(info.ProductName))
                        return info.ProductName;
                }
            }
            catch
            {
            }

            return Path.GetFileNameWithoutExtension(executablePath) ?? progId;
        }

        private static BrowserInfo FindByPath(List<BrowserInfo> browsers, string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var key = NormalizePathKey(path);
            foreach (var browser in browsers)
            {
                if (string.Equals(NormalizePathKey(browser.ExecutablePath), key, StringComparison.OrdinalIgnoreCase))
                    return browser;
            }
            return null;
        }

        private static string NormalizePathKey(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                NormalizePathKey(left),
                NormalizePathKey(right),
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
