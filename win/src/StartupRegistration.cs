using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Sanity
{
    public static class StartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupApprovedKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string ValueName = "Sanity";

        // Task Manager / Settings read this binary blob; 0x02 = enabled.
        private static readonly byte[] EnabledApproval =
            { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public static bool IsRegistered()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                if (key == null)
                    return false;

                var value = key.GetValue(ValueName) as string;
                if (string.IsNullOrEmpty(value))
                    return false;

                if (!string.Equals(
                    NormalizePath(value),
                    NormalizePath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return IsStartupApproved();
        }

        public static void Apply(bool launchOnStartup)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key == null)
                    return;

                if (launchOnStartup)
                    key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
                else
                    key.DeleteValue(ValueName, false);
            }

            ApplyStartupApproved(launchOnStartup);
        }

        private static bool IsStartupApproved()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupApprovedKeyPath, false))
            {
                if (key == null)
                    return true;

                var data = key.GetValue(ValueName) as byte[];
                if (data == null || data.Length == 0)
                    return true;

                // Odd low nibble (e.g. 0x03) means disabled in Task Manager.
                return (data[0] & 0x01) == 0;
            }
        }

        private static void ApplyStartupApproved(bool launchOnStartup)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(StartupApprovedKeyPath, true))
            {
                if (key == null)
                    return;

                if (launchOnStartup)
                    key.SetValue(ValueName, EnabledApproval, RegistryValueKind.Binary);
                else
                    key.DeleteValue(ValueName, false);
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Trim().Trim('"');
        }
    }
}
