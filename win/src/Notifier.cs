using System.Threading;
using System.Windows.Forms;

namespace Sanity
{
    public static class Notifier
    {
        public static void Show(string message)
        {
            var config = AppConfig.Load();
            if (!config.NotificationsEnabled)
                return;

            using (var icon = new NotifyIcon())
            {
                icon.Icon = AppIcon.Get();
                icon.Visible = true;
                icon.BalloonTipTitle = "Sanity";
                icon.BalloonTipText = message;
                icon.ShowBalloonTip(2000);
                Thread.Sleep(2200);
                icon.Visible = false;
            }
        }
    }
}
