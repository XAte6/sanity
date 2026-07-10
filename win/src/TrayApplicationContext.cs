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
        private ToolStripMenuItem _linkProxyItem;
        private ToolStripMenuItem _targetBrowserMenuItem;
        private ToolStripMenuItem _notificationsItem;
        private ToolStripMenuItem _launchOnStartupItem;
        private ToolStripMenuItem _sleepMenuItem;
        private ToolStripMenuItem _sleep1hItem;
        private ToolStripMenuItem _sleep2hItem;
        private ToolStripMenuItem _sleep4hItem;
        private ToolStripMenuItem _sleep8hItem;

        private Form _statisticsForm;
        private Form _configForm;

        public TrayApplicationContext()
        {
            _config = AppConfig.Load();
            StartupRegistration.Apply(_config.LaunchOnStartup);
            if (_config.LinkProxyEnabled)
                ProtocolRegistration.Apply(true, _config);

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
            _notifyIcon.DoubleClick += (s, e) => OpenStatistics();

            _refreshTimer = new Timer { Interval = 30000 };
            _refreshTimer.Tick += (s, e) => RefreshMenuState();
            _refreshTimer.Start();

            RefreshMenuState();
            Application.ApplicationExit += (s, e) => Cleanup();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var statisticsItem = new ToolStripMenuItem("Statistics");
            statisticsItem.Click += (s, e) => OpenStatistics();

            var configItem = new ToolStripMenuItem("Regex Rules");
            configItem.Click += (s, e) => OpenConfiguration();

            _enabledItem = new ToolStripMenuItem("Enabled");
            _enabledItem.Click += (s, e) => ToggleEnabled();

            _linkProxyItem = new ToolStripMenuItem("Clean clicked links");
            _linkProxyItem.Click += (s, e) => ToggleLinkProxy();

            _targetBrowserMenuItem = new ToolStripMenuItem("Target browser");
            RebuildTargetBrowserMenu();

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

            menu.Items.Add(statisticsItem);
            menu.Items.Add(configItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_enabledItem);
            menu.Items.Add(_linkProxyItem);
            menu.Items.Add(_targetBrowserMenuItem);
            menu.Items.Add(_notificationsItem);
            menu.Items.Add(_launchOnStartupItem);
            menu.Items.Add(_sleepMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            return menu;
        }

        private void OpenConfiguration()
        {
            if (FocusExisting(_configForm))
                return;

            var form = new ConfigForm(_config);
            _configForm = form;
            form.FormClosed += (s, e) =>
            {
                _configForm = null;
                RefreshMenuState();
            };
            form.Show();
        }

        private void OpenStatistics()
        {
            if (FocusExisting(_statisticsForm))
                return;

            var form = new StatisticsForm();
            _statisticsForm = form;
            form.FormClosed += (s, e) => _statisticsForm = null;
            form.Show();
        }

        private static bool FocusExisting(Form form)
        {
            if (form == null || form.IsDisposed)
                return false;

            if (form.WindowState == FormWindowState.Minimized)
                form.WindowState = FormWindowState.Normal;

            form.BringToFront();
            form.Activate();
            return true;
        }

        private void ToggleEnabled()
        {
            _config.Enabled = !_config.Enabled;
            if (_config.Enabled)
                _config.SleepUntil = null;

            _config.Save();
            RefreshMenuState();
        }

        private void ToggleLinkProxy()
        {
            _config.LinkProxyEnabled = !_config.LinkProxyEnabled;
            if (_config.LinkProxyEnabled && string.IsNullOrWhiteSpace(_config.TargetBrowser))
            {
                var defaultPath = BrowserHelper.GetDefaultBrowserPath();
                _config.TargetBrowser = defaultPath ?? BrowserHelper.GetDefaultBrowserProgId() ?? string.Empty;
            }

            ProtocolRegistration.Apply(_config.LinkProxyEnabled, _config);
            _config.Save();

            if (_config.LinkProxyEnabled)
            {
                ProtocolRegistration.OpenDefaultAppsSettings();
                ShowBalloon("Set Sanity as the default app for HTTP and HTTPS links.");
            }
            else
            {
                ProtocolRegistration.OpenDefaultAppsSettings();
                ShowBalloon("Choose your browser as the default for HTTP and HTTPS links.");
            }

            RebuildTargetBrowserMenu();
            RefreshMenuState();
        }

        private void RebuildTargetBrowserMenu()
        {
            _targetBrowserMenuItem.DropDownItems.Clear();
            foreach (var browser in BrowserHelper.GetInstalledBrowsers())
            {
                var item = new ToolStripMenuItem(browser.Name);
                var path = browser.ExecutablePath;
                var progId = browser.ProgId;
                item.Click += (s, e) =>
                {
                    _config.TargetBrowser = !string.IsNullOrEmpty(path) ? path : progId;
                    _config.Save();
                    RefreshMenuState();
                };
                _targetBrowserMenuItem.DropDownItems.Add(item);
            }

            if (_targetBrowserMenuItem.DropDownItems.Count == 0)
            {
                _targetBrowserMenuItem.DropDownItems.Add(new ToolStripMenuItem("(no browsers found)")
                {
                    Enabled = false
                });
            }
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

            _linkProxyItem.Checked = _config.LinkProxyEnabled;
            _linkProxyItem.Text = _config.LinkProxyEnabled && !ProtocolRegistration.IsRegistered()
                ? "Clean clicked links (set as default)"
                : "Clean clicked links";
            UpdateTargetBrowserChecks();

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
                ? (_config.LinkProxyEnabled ? "Sanity - active (clipboard + links)" : "Sanity - active")
                : sleeping
                    ? "Sanity - sleeping until " + _config.SleepUntil.Value.ToString("HH:mm")
                    : "Sanity - disabled";
        }

        private void UpdateTargetBrowserChecks()
        {
            foreach (ToolStripItem dropDownItem in _targetBrowserMenuItem.DropDownItems)
            {
                var item = dropDownItem as ToolStripMenuItem;
                if (item == null || !item.Enabled)
                    continue;

                var browser = BrowserHelper.GetInstalledBrowsers().Find(b => b.Name == item.Text);
                if (browser == null)
                {
                    item.Checked = false;
                    continue;
                }

                var target = _config.TargetBrowser ?? string.Empty;
                item.Checked = string.Equals(target, browser.ExecutablePath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(target, browser.ProgId, StringComparison.OrdinalIgnoreCase);
            }
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
