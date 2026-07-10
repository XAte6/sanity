using System;
using System.Threading;
using System.Windows.Forms;

namespace Sanity
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--write-default-config")
            {
                AppConfig.CreateDefault().Save();
                return;
            }

            if (args.Length >= 2 && args[0] == "--open")
            {
                if (LinkOpener.Open(args[1]))
                    Notifier.Show("Tracking removed from clicked URL.");
                return;
            }

            bool createdNew;
            using (new Mutex(true, "Sanity.UrlTrackerRemover", out createdNew))
            {
                if (!createdNew)
                    return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext());
            }
        }
    }
}
