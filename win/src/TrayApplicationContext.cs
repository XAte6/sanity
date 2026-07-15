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
        private ToolStripMenuItem _targetBrowserMenuItem;
        private ToolStripMenuItem _notificationsItem;
        private ToolStripMenuItem _launchOnStartupItem;
        private ToolStripMenuItem _updatesItem;

        private Form _statisticsForm;
        private Form _configForm;
        private Form _setupWizardForm;

        public TrayApplicationContext()
        {
            _config = AppConfig.Load();

            if (!_config.SetupCompleted)
            {
                using (var wizard = new SetupWizardForm(_config))
                    wizard.ShowDialog();
            }

            StartupRegistration.Apply(_config.LaunchOnStartup);
            ApplyLinkHandling(_config.Enabled);

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
            _clipboardMonitor.BeginInvoke(new MethodInvoker(() =>
                UpdateChecker.RunAsync(_config, System.Threading.SynchronizationContext.Current)));
            Application.ApplicationExit += (s, e) => Cleanup();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var statisticsItem = new ToolStripMenuItem("Statistics");
            statisticsItem.Click += (s, e) => OpenStatistics();

            var configItem = new ToolStripMenuItem("Regex Rules");
            configItem.Click += (s, e) => OpenConfiguration();

            var runSetupItem = new ToolStripMenuItem("Run Setup");
            runSetupItem.Click += (s, e) => OpenSetupWizard();

            _enabledItem = new ToolStripMenuItem("Enabled");
            _enabledItem.Click += (s, e) => ToggleEnabled();

            _targetBrowserMenuItem = new ToolStripMenuItem("Target browser");
            RebuildTargetBrowserMenu();

            _launchOnStartupItem = new ToolStripMenuItem("Launch on startup");
            _launchOnStartupItem.Click += (s, e) => ToggleLaunchOnStartup();

            _notificationsItem = new ToolStripMenuItem("Notifications");
            _notificationsItem.Click += (s, e) => ToggleNotifications();

            _updatesItem = new ToolStripMenuItem("Check for updates");
            _updatesItem.Click += (s, e) => ToggleUpdates();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitThread();

            menu.Items.Add(statisticsItem);
            menu.Items.Add(configItem);
            menu.Items.Add(runSetupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_enabledItem);
            menu.Items.Add(_updatesItem);
            menu.Items.Add(_targetBrowserMenuItem);
            menu.Items.Add(_notificationsItem);
            menu.Items.Add(_launchOnStartupItem);
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

        private void OpenSetupWizard(bool startAtDefaultApps = false)
        {
            if (FocusExisting(_setupWizardForm))
                return;

            var form = new SetupWizardForm(_config, startAtDefaultApps);
            _setupWizardForm = form;
            form.FormClosed += (s, e) =>
            {
                _setupWizardForm = null;
                ApplyLinkHandling(_config.Enabled);
                RebuildTargetBrowserMenu();
                RefreshMenuState();
            };
            form.ShowDialog();
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

            ApplyLinkHandling(_config.Enabled);
            _config.Save();

            if (_config.Enabled)
            {
                if (!ProtocolRegistration.IsRegistered())
                    OpenSetupWizard(startAtDefaultApps: true);
            }
            else
            {
                ProtocolRegistration.OpenDefaultAppsSettings();
                ShowBalloon("Choose your browser as the default for HTTP and HTTPS links.");
            }

            RebuildTargetBrowserMenu();
            RefreshMenuState();
        }

        private void ApplyLinkHandling(bool enabled)
        {
            _config.LinkProxyEnabled = enabled;
            if (enabled && string.IsNullOrWhiteSpace(_config.TargetBrowser))
            {
                var defaultPath = BrowserHelper.GetDefaultBrowserPath();
                _config.TargetBrowser = defaultPath ?? BrowserHelper.GetDefaultBrowserProgId() ?? string.Empty;
            }

            ProtocolRegistration.Apply(enabled, _config);
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

        private void ToggleUpdates()
        {
            _config.UpdatesEnabled = !_config.UpdatesEnabled;
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

            UpdateTargetBrowserChecks();

            _launchOnStartupItem.Checked = _config.LaunchOnStartup;
            _notificationsItem.Checked = _config.NotificationsEnabled;
            _updatesItem.Checked = _config.UpdatesEnabled;

            _notifyIcon.Text = active
                ? "Sanity - active"
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
