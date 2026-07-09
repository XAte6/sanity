using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Sanity
{
    public static class StartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Sanity";

        public static bool IsRegistered()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                if (key == null)
                    return false;

                var value = key.GetValue(ValueName) as string;
                if (string.IsNullOrEmpty(value))
                    return false;

                return string.Equals(
                    NormalizePath(value),
                    NormalizePath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase);
            }
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
        }

        private static string NormalizePath(string path)
        {
            return path.Trim().Trim('"');
        }
    }
}
