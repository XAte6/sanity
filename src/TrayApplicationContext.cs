using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sanity
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly AppConfig _config;
        private readonly ClipboardMonitor _clipboardMonitor;
        private readonly Timer _refreshTimer;
        private readonly ContextMenuStrip _menu;

        private ToolStripMenuItem _enabledItem;
        private ToolStripMenuItem _notificationsItem;
        private ToolStripMenuItem _launchOnStartupItem;
        private ToolStripMenuItem _sleepMenuItem;
        private ToolStripMenuItem _sleep1hItem;
        private ToolStripMenuItem _sleep2hItem;
        private ToolStripMenuItem _sleep4hItem;
        private ToolStripMenuItem _sleep8hItem;

        public TrayApplicationContext()
        {
            _config = AppConfig.Load();
            StartupRegistration.Apply(_config.LaunchOnStartup);

            _clipboardMonitor = new ClipboardMonitor(_config);
            _clipboardMonitor.Show();
            _clipboardMonitor.ClipboardCleaned += (s, e) => ShowBalloon("Tracking removed from copied URL.");

            _menu = BuildMenu();

            _notifyIcon = new NotifyIcon
            {
                Icon = AppIcon.Get(),
                Text = "Sanity - URL tracker remover",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _notifyIcon.DoubleClick += (s, e) => OpenConfiguration();

            _refreshTimer = new Timer { Interval = 30000 };
            _refreshTimer.Tick += (s, e) => RefreshMenuState();
            _refreshTimer.Start();

            RefreshMenuState();
            Application.ApplicationExit += (s, e) => Cleanup();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var configItem = new ToolStripMenuItem("Configuration");
            configItem.Click += (s, e) => OpenConfiguration();

            _enabledItem = new ToolStripMenuItem("Enabled");
            _enabledItem.Click += (s, e) => ToggleEnabled();

            _launchOnStartupItem = new ToolStripMenuItem("Launch on startup");
            _launchOnStartupItem.Click += (s, e) => ToggleLaunchOnStartup();

            _notificationsItem = new ToolStripMenuItem("Notifications");
            _notificationsItem.Click += (s, e) => ToggleNotifications();

            _sleepMenuItem = new ToolStripMenuItem("Sleep");

            _sleep1hItem = new ToolStripMenuItem("1 hour");
            _sleep1hItem.Click += (s, e) => SetSleep(1);

            _sleep2hItem = new ToolStripMenuItem("2 hours");
            _sleep2hItem.Click += (s, e) => SetSleep(2);

            _sleep4hItem = new ToolStripMenuItem("4 hours");
            _sleep4hItem.Click += (s, e) => SetSleep(4);

            _sleep8hItem = new ToolStripMenuItem("8 hours");
            _sleep8hItem.Click += (s, e) => SetSleep(8);

            _sleepMenuItem.DropDownItems.Add(_sleep1hItem);
            _sleepMenuItem.DropDownItems.Add(_sleep2hItem);
            _sleepMenuItem.DropDownItems.Add(_sleep4hItem);
            _sleepMenuItem.DropDownItems.Add(_sleep8hItem);

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitThread();

            menu.Items.Add(configItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_enabledItem);
            menu.Items.Add(_notificationsItem);
            menu.Items.Add(_launchOnStartupItem);
            menu.Items.Add(_sleepMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            return menu;
        }

        private void OpenConfiguration()
        {
            using (var form = new ConfigForm(_config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _config.Save();
                    RefreshMenuState();
                }
            }
        }

        private void ToggleEnabled()
        {
            _config.Enabled = !_config.Enabled;
            if (_config.Enabled)
                _config.SleepUntil = null;

            _config.Save();
            RefreshMenuState();
        }

        private void ToggleLaunchOnStartup()
        {
            _config.LaunchOnStartup = !_config.LaunchOnStartup;
            StartupRegistration.Apply(_config.LaunchOnStartup);
            _config.Save();
            RefreshMenuState();
        }

        private void ToggleNotifications()
        {
            _config.NotificationsEnabled = !_config.NotificationsEnabled;
            _config.Save();
            RefreshMenuState();
        }

        private void SetSleep(int hours)
        {
            if (_config.SleepUntil.HasValue
                && _config.SleepUntil.Value > DateTime.Now
                && _config.SleepUntil.Value <= DateTime.Now.AddHours(hours + 0.1))
            {
                _config.SleepUntil = null;
            }
            else
            {
                _config.SleepUntil = DateTime.Now.AddHours(hours);
            }

            _config.Save();
            RefreshMenuState();
        }

        private void RefreshMenuState()
        {
            var sleeping = _config.SleepUntil.HasValue && _config.SleepUntil.Value > DateTime.Now;
            var active = _config.IsActive;

            _enabledItem.Checked = _config.Enabled && !sleeping;
            _enabledItem.Text = sleeping
                ? "Enabled (sleeping)"
                : "Enabled";

            _launchOnStartupItem.Checked = _config.LaunchOnStartup;
            _notificationsItem.Checked = _config.NotificationsEnabled;

            _sleepMenuItem.Text = sleeping
                ? "Sleep (until " + _config.SleepUntil.Value.ToString("HH:mm") + ")"
                : "Sleep";

            UpdateSleepItem(_sleep1hItem, 1, sleeping);
            UpdateSleepItem(_sleep2hItem, 2, sleeping);
            UpdateSleepItem(_sleep4hItem, 4, sleeping);
            UpdateSleepItem(_sleep8hItem, 8, sleeping);

            _notifyIcon.Text = active
                ? "Sanity - active"
                : sleeping
                    ? "Sanity - sleeping until " + _config.SleepUntil.Value.ToString("HH:mm")
                    : "Sanity - disabled";
        }

        private void UpdateSleepItem(ToolStripMenuItem item, int hours, bool sleeping)
        {
            var until = DateTime.Now.AddHours(hours);
            var isSelected = sleeping
                && _config.SleepUntil.HasValue
                && Math.Abs((_config.SleepUntil.Value - until).TotalMinutes) < 5;

            item.Checked = isSelected;
        }

        private void ShowBalloon(string message)
        {
            if (!_config.NotificationsEnabled)
                return;

            _notifyIcon.BalloonTipTitle = "Sanity";
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(2000);
        }

        private void Cleanup()
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _clipboardMonitor.Close();
        }
    }
}
