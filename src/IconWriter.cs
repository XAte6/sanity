using System;

namespace Sanity
{
    internal static class IconWriter
    {
        public static void Main(string[] args)
        {
            var path = args.Length > 0 ? args[0] : "app.ico";
            AppIcon.SaveToFile(path);
        }
    }
}
