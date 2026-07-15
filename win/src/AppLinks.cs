using System.Diagnostics;

namespace Sanity
{
    public static class AppLinks
    {
        public const string GitHub = "https://github.com/XAte6/sanity";
        public const string Support = "https://github.com/XAte6/sanity/issues";
        public const string Tip = "https://paypal.me/XAte6";
        public const string RegexRulesRaw =
            "https://raw.githubusercontent.com/XAte6/sanity/main/defaults/regex-rules.json";
        public const string ReleaseAsset =
            "https://github.com/XAte6/sanity/raw/main/releases/Sanity-win-x86-setup.exe";
        public const string ReleaseCommitsApi =
            "https://api.github.com/repos/XAte6/sanity/commits?path=releases/Sanity-win-x86-setup.exe&per_page=1";

        public static void Open(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
