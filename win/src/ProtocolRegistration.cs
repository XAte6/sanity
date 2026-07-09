using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Sanity
{
    public static class ProtocolRegistration
    {
        private const string AppKeyPath = @"Software\Sanity";
        private const string CapabilitiesPath = AppKeyPath + @"\Capabilities";
        private const string UrlAssociationsPath = CapabilitiesPath + @"\URLAssociations";
        private const string RegisteredApplicationsPath = @"Software\RegisteredApplications";
        private const string RegisteredAppName = "Sanity";
        private const string ProgId = "Sanity.Url";
        private const string BackupProgIdValue = "PreviousHttpProgId";
        private const string BackupHttpsProgIdValue = "PreviousHttpsProgId";

        public static bool IsRegistered()
        {
            var httpProgId = GetUrlAssociationProgId("http");
            var httpsProgId = GetUrlAssociationProgId("https");
            return IsSanityProgId(httpProgId) && IsSanityProgId(httpsProgId);
        }

        public static void OpenDefaultAppsSettings()
        {
            var uri = "ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(RegisteredAppName);
            Process.Start(uri);
        }

        public static void Apply(bool linkProxyEnabled, AppConfig config)
        {
            if (linkProxyEnabled)
            {
                if (string.IsNullOrWhiteSpace(config.TargetBrowser))
                {
                    var defaultPath = BrowserHelper.GetDefaultBrowserPath();
                    var defaultProgId = BrowserHelper.GetDefaultBrowserProgId();
                    config.TargetBrowser = !string.IsNullOrEmpty(defaultPath)
                        ? defaultPath
                        : (defaultProgId ?? string.Empty);
                }

                BackupCurrentHandlers();
                RegisterSanityHandler();
            }
            else
            {
                RestorePreviousHandlers();
                UnregisterSanityHandler();
            }
        }

        public static string GetBackedUpBrowser()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AppKeyPath, false))
            {
                if (key == null)
                    return null;

                var httpBackup = key.GetValue(BackupProgIdValue) as string;
                if (!string.IsNullOrEmpty(httpBackup))
                    return httpBackup;

                return key.GetValue(BackupHttpsProgIdValue) as string;
            }
        }

        private static void BackupCurrentHandlers()
        {
            var httpProgId = GetUrlAssociationProgId("http");
            var httpsProgId = GetUrlAssociationProgId("https");

            using (var key = Registry.CurrentUser.CreateSubKey(AppKeyPath))
            {
                if (!string.IsNullOrEmpty(httpProgId) && !IsSanityProgId(httpProgId))
                    key.SetValue(BackupProgIdValue, httpProgId);
                if (!string.IsNullOrEmpty(httpsProgId) && !IsSanityProgId(httpsProgId))
                    key.SetValue(BackupHttpsProgIdValue, httpsProgId);
            }
        }

        private static void RestorePreviousHandlers()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AppKeyPath, true))
            {
                if (key == null)
                    return;

                key.DeleteValue(BackupProgIdValue, false);
                key.DeleteValue(BackupHttpsProgIdValue, false);
            }
        }

        private static void RegisterSanityHandler()
        {
            var exePath = Application.ExecutablePath;
            var command = "\"" + exePath + "\" --open \"%1\"";

            using (var progIdKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ProgId))
            {
                progIdKey.SetValue(null, "URL:Sanity Link Proxy");
                progIdKey.SetValue("URL Protocol", string.Empty);
                using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
                    iconKey.SetValue(null, exePath + ",0");
                using (var commandKey = progIdKey.CreateSubKey(@"shell\open\command"))
                    commandKey.SetValue(null, command);
            }

            using (var capabilitiesKey = Registry.CurrentUser.CreateSubKey(CapabilitiesPath))
            {
                capabilitiesKey.SetValue("ApplicationName", "Sanity");
                capabilitiesKey.SetValue("ApplicationDescription", "Sanity URL tracker remover");
            }

            using (var urlKey = Registry.CurrentUser.CreateSubKey(UrlAssociationsPath))
            {
                urlKey.SetValue("http", ProgId);
                urlKey.SetValue("https", ProgId);
            }

            using (var registeredKey = Registry.CurrentUser.CreateSubKey(RegisteredApplicationsPath))
                registeredKey.SetValue(RegisteredAppName, CapabilitiesPath);
        }

        private static void UnregisterSanityHandler()
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + ProgId, false);
            Registry.CurrentUser.DeleteSubKeyTree(CapabilitiesPath, false);
            Registry.CurrentUser.DeleteSubKeyTree(UrlAssociationsPath, false);

            using (var registeredKey = Registry.CurrentUser.OpenSubKey(RegisteredApplicationsPath, true))
            {
                if (registeredKey != null)
                    registeredKey.DeleteValue(RegisteredAppName, false);
            }
        }

        private static string GetUrlAssociationProgId(string scheme)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\" + scheme + @"\UserChoice",
                false))
            {
                return key != null ? key.GetValue("ProgId") as string : null;
            }
        }

        private static bool IsSanityProgId(string progId)
        {
            return string.Equals(progId, ProgId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
