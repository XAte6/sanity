using System.Diagnostics;

namespace Sanity
{
    public static class AppLinks
    {
        public const string GitHub = "https://github.com/XAte6/sanity";
        public const string Support = "https://github.com/XAte6/sanity/issues";
        public const string Tip = "https://paypal.me/XAte6";

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
