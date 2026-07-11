using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Sanity
{
    public class SetupWizardForm : Form
    {
        private const string DefaultAppsGifResource = "Sanity.default-apps-setup.gif";
        private const string DefaultAppsGifFileName = "default-apps-setup.gif";
        private const string TestUrl =
            "https://www.github.com/XAte6/sanity?fbclid=IwAR0SANITYWASHERE";

        private readonly AppConfig _config;
        private readonly Panel _stepOptions;
        private readonly Panel _stepDefaultApps;
        private readonly Panel _stepDone;
        private ComboBox _browserCombo;
        private CheckBox _enableCheck;
        private CheckBox _startupCheck;
        private CheckBox _notificationsCheck;
        private readonly Button _primaryButton;
        private readonly Button _backButton;
        private readonly Label _footerHint;
        private Label _doneBodyLabel;
        private LinkLabel _testLink;
        private PictureBox _demoPicture;
        private Image _demoImage;
        private MemoryStream _demoImageStream;

        public bool Completed { get; private set; }

        public SetupWizardForm(AppConfig config)
            : this(config, false)
        {
        }

        public SetupWizardForm(AppConfig config, bool startAtDefaultApps)
        {
            _config = config;

            Text = "Welcome to Sanity";
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(520, 560);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            Controls.Add(BuildHeader());

            _stepOptions = BuildOptionsStep();
            _stepDefaultApps = BuildDefaultAppsStep();
            _stepDone = BuildDoneStep();
            Controls.Add(_stepOptions);
            Controls.Add(_stepDefaultApps);
            Controls.Add(_stepDone);

            _backButton = new Button
            {
                Text = "Back",
                Location = new Point(22, 510),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiChrome.PanelBg,
                ForeColor = UiChrome.Ink,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _backButton.FlatAppearance.BorderColor = UiChrome.Border;
            _backButton.Click += (s, e) => OnBackClick();
            Controls.Add(_backButton);

            _primaryButton = new Button
            {
                Text = "Next",
                Location = new Point(382, 510),
                Size = new Size(116, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiChrome.SanityGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _primaryButton.FlatAppearance.BorderSize = 0;
            _primaryButton.Click += (s, e) => OnPrimaryClick();
            Controls.Add(_primaryButton);

            _footerHint = new Label
            {
                Text = "Closing without finishing leaves Sanity unset up; this wizard will show again next launch.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(120, 508),
                Size = new Size(250, 36)
            };
            Controls.Add(_footerHint);

            AcceptButton = _primaryButton;
            if (startAtDefaultApps || _config.SetupCompleted)
                SyncOptionsFromConfig();
            if (startAtDefaultApps)
                ShowDefaultAppsStep();
            else
                ShowOptionsStep();
            FormClosed += (s, e) => DisposeDemoImage();
        }

        private void SyncOptionsFromConfig()
        {
            _enableCheck.Checked = _config.Enabled;
            _startupCheck.Checked = _config.LaunchOnStartup;
            _notificationsCheck.Checked = _config.NotificationsEnabled;
        }

        private Control BuildHeader()
        {
            var panel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(520, 78),
                BackColor = Color.White
            };

            panel.Controls.Add(new PictureBox
            {
                Image = AppIcon.Get().ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(22, 14),
                Size = new Size(48, 48)
            });
            panel.Controls.Add(new Label
            {
                Text = "Welcome to Sanity",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(82, 16)
            });
            panel.Controls.Add(new Label
            {
                Text = "Strip tracking parameters before you paste or open links.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(82, 46)
            });
            return panel;
        }

        private Panel BuildOptionsStep()
        {
            var panel = new Panel
            {
                Location = new Point(0, 78),
                Size = new Size(520, 420),
                BackColor = Color.White
            };

            panel.Controls.Add(new Label
            {
                Text = "Choose a few options to get started — you can change them any time from the tray icon.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(22, 14),
                Size = new Size(476, 40)
            });

            panel.Controls.Add(new Label
            {
                Text = "Target browser",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(22, 68)
            });

            _browserCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(22, 92),
                Width = 476,
                Font = new Font("Segoe UI", 9f)
            };
            PopulateBrowsers();
            panel.Controls.Add(_browserCombo);

            var optionsPanel = new Panel
            {
                Location = new Point(22, 140),
                Size = new Size(476, 110),
                BackColor = UiChrome.PanelBg
            };
            optionsPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiChrome.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, optionsPanel.Width - 1, optionsPanel.Height - 1);
            };

            _enableCheck = new CheckBox
            {
                Text = "Enable clipboard and link cleaning now",
                Location = new Point(14, 14),
                AutoSize = true,
                Checked = true,
                ForeColor = UiChrome.Ink,
                BackColor = UiChrome.PanelBg
            };
            _startupCheck = new CheckBox
            {
                Text = "Launch on startup",
                Location = new Point(14, 42),
                AutoSize = true,
                Checked = true,
                ForeColor = UiChrome.Ink,
                BackColor = UiChrome.PanelBg
            };
            _notificationsCheck = new CheckBox
            {
                Text = "Show notifications when a URL is cleaned",
                Location = new Point(14, 70),
                AutoSize = true,
                Checked = true,
                ForeColor = UiChrome.Ink,
                BackColor = UiChrome.PanelBg
            };

            optionsPanel.Controls.Add(_enableCheck);
            optionsPanel.Controls.Add(_startupCheck);
            optionsPanel.Controls.Add(_notificationsCheck);
            panel.Controls.Add(optionsPanel);
            return panel;
        }

        private Panel BuildDefaultAppsStep()
        {
            var panel = new Panel
            {
                Location = new Point(0, 78),
                Size = new Size(520, 420),
                BackColor = Color.White,
                Visible = false
            };

            panel.Controls.Add(new Label
            {
                Text = "Set Sanity as the default for HTTP and HTTPS",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(22, 14)
            });

            panel.Controls.Add(new Label
            {
                Text = "This guide stays on top. Use the Settings window behind it and follow the animation to set Sanity as the default for HTTP and HTTPS links.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(22, 42),
                Size = new Size(476, 40)
            });

            _demoPicture = new PictureBox
            {
                Location = new Point(22, 92),
                Size = new Size(476, 300),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = UiChrome.PanelBg
            };
            panel.Controls.Add(_demoPicture);
            return panel;
        }

        private Panel BuildDoneStep()
        {
            var panel = new Panel
            {
                Location = new Point(0, 78),
                Size = new Size(520, 420),
                BackColor = Color.White,
                Visible = false
            };

            panel.Controls.Add(new Label
            {
                Text = "Setup complete",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = UiChrome.SanityGreen,
                AutoSize = true,
                Location = new Point(22, 14)
            });

            _doneBodyLabel = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UiChrome.Ink,
                AutoSize = false,
                Location = new Point(22, 52),
                Size = new Size(476, 120)
            };
            panel.Controls.Add(_doneBodyLabel);

            panel.Controls.Add(new Label
            {
                Text = "Try a tracked link",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(22, 190)
            });

            var linkPanel = new Panel
            {
                Location = new Point(22, 218),
                Size = new Size(476, 72),
                BackColor = UiChrome.PanelBg
            };
            linkPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiChrome.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, linkPanel.Width - 1, linkPanel.Height - 1);
            };

            _testLink = new LinkLabel
            {
                Text = TestUrl,
                Font = new Font("Segoe UI", 9f),
                LinkColor = UiChrome.SanityGreen,
                ActiveLinkColor = UiChrome.Ink,
                Location = new Point(14, 14),
                Size = new Size(448, 44),
                AutoSize = false
            };
            _testLink.Links.Clear();
            _testLink.Links.Add(0, TestUrl.Length, TestUrl);
            _testLink.LinkClicked += (s, e) => OpenTestLinkAndClose();
            linkPanel.Controls.Add(_testLink);
            panel.Controls.Add(linkPanel);

            panel.Controls.Add(new Label
            {
                Text = "Click the link above, or Close when you are done.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(22, 306)
            });

            return panel;
        }

        private void ShowOptionsStep()
        {
            TopMost = false;
            _stepOptions.Visible = true;
            _stepDefaultApps.Visible = false;
            _stepDone.Visible = false;
            _backButton.Visible = false;
            _primaryButton.Text = _enableCheck.Checked ? "Next" : "Close";
            _footerHint.Visible = true;
            _enableCheck.CheckedChanged -= EnableCheckChanged;
            _enableCheck.CheckedChanged += EnableCheckChanged;
        }

        private void EnableCheckChanged(object sender, EventArgs e)
        {
            if (_stepOptions.Visible)
                _primaryButton.Text = _enableCheck.Checked ? "Next" : "Close";
        }

        private void ShowDefaultAppsStep()
        {
            EnsureDemoImage();
            _stepOptions.Visible = false;
            _stepDefaultApps.Visible = true;
            _stepDone.Visible = false;
            _backButton.Visible = true;
            _primaryButton.Text = "Next";
            _footerHint.Visible = false;

            TopMost = true;
            ScheduleDefaultAppsSettingsOpen();
        }

        private void ScheduleDefaultAppsSettingsOpen()
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(OpenDefaultAppsAndFocus));
                return;
            }

            EventHandler onShown = null;
            onShown = (s, e) =>
            {
                Shown -= onShown;
                BeginInvoke(new Action(OpenDefaultAppsAndFocus));
            };
            Shown += onShown;
        }

        private void OpenDefaultAppsAndFocus()
        {
            ProtocolRegistration.OpenDefaultAppsSettings();
            BringToFront();
            Activate();
        }

        private void ShowDoneStep()
        {
            TopMost = false;
            UpdateDoneCopy();
            _stepOptions.Visible = false;
            _stepDefaultApps.Visible = false;
            _stepDone.Visible = true;
            _backButton.Visible = true;
            _primaryButton.Text = "Close";
            _footerHint.Visible = false;
            BringToFront();
            Activate();
        }

        private void UpdateDoneCopy()
        {
            var browserName = GetSelectedBrowserDisplayName();
            _doneBodyLabel.Text =
                "You're set up. Sanity will strip tracking from URLs when you copy and paste them, " +
                "and when you open links through Sanity.\n\n" +
                "Try the GitHub link below — it includes a Facebook tracking tag (?fbclid=…). " +
                "After it opens in " + browserName + ", that tag should be gone from the address bar.";
        }

        private string GetSelectedBrowserDisplayName()
        {
            var browser = _browserCombo.SelectedItem as BrowserInfo;
            if (browser == null || string.IsNullOrWhiteSpace(browser.Name))
                return "your browser";

            var name = browser.Name;
            const string prefix = "System default (";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(")"))
                name = name.Substring(prefix.Length, name.Length - prefix.Length - 1);

            return name;
        }

        private void OnBackClick()
        {
            if (_stepDone.Visible)
            {
                if (_config.Enabled)
                    ShowDefaultAppsStep();
                else
                    ShowOptionsStep();
                return;
            }

            if (_stepDefaultApps.Visible)
                ShowOptionsStep();
        }

        private void OnPrimaryClick()
        {
            if (_stepDone.Visible)
            {
                CompleteSetup();
                return;
            }

            if (_stepDefaultApps.Visible)
            {
                ShowDoneStep();
                return;
            }

            ApplyOptionsToConfig();
            if (_enableCheck.Checked)
            {
                ShowDefaultAppsStep();
                return;
            }

            CompleteSetup();
        }

        private void OpenTestLinkAndClose()
        {
            ApplyOptionsToConfig();
            LinkOpener.Open(TestUrl);
            CompleteSetup();
        }

        private void ApplyOptionsToConfig()
        {
            var browser = _browserCombo.SelectedItem as BrowserInfo;
            if (browser != null)
            {
                _config.TargetBrowser = !string.IsNullOrEmpty(browser.ExecutablePath)
                    ? browser.ExecutablePath
                    : (browser.ProgId ?? string.Empty);
            }

            _config.Enabled = _enableCheck.Checked;
            _config.LinkProxyEnabled = _enableCheck.Checked;
            _config.LaunchOnStartup = _startupCheck.Checked;
            _config.NotificationsEnabled = _notificationsCheck.Checked;
            _config.SetupCompleted = true;
            _config.Save();

            StartupRegistration.Apply(_config.LaunchOnStartup);
            ProtocolRegistration.Apply(_config.Enabled, _config);
        }

        private void CompleteSetup()
        {
            TopMost = false;
            ApplyOptionsToConfig();
            Completed = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void PopulateBrowsers()
        {
            var browsers = BrowserHelper.GetInstalledBrowsers();
            _browserCombo.Items.Clear();
            foreach (var browser in browsers)
                _browserCombo.Items.Add(browser);

            if (_browserCombo.Items.Count == 0)
            {
                _browserCombo.Enabled = false;
                return;
            }

            var selected = 0;
            var target = _config.TargetBrowser ?? string.Empty;
            var defaultPath = BrowserHelper.GetDefaultBrowserPath();
            var defaultProgId = BrowserHelper.GetDefaultBrowserProgId();

            for (var i = 0; i < browsers.Count; i++)
            {
                var b = browsers[i];
                if (!string.IsNullOrEmpty(target)
                    && (string.Equals(b.ExecutablePath, target, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(b.ProgId, target, StringComparison.OrdinalIgnoreCase)))
                {
                    selected = i;
                    break;
                }

                if ((!string.IsNullOrEmpty(defaultPath)
                        && string.Equals(b.ExecutablePath, defaultPath, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(defaultProgId)
                        && string.Equals(b.ProgId, defaultProgId, StringComparison.OrdinalIgnoreCase)))
                {
                    selected = i;
                }
            }

            _browserCombo.SelectedIndex = selected;
        }

        private void EnsureDemoImage()
        {
            if (_demoImage != null)
            {
                _demoPicture.Image = _demoImage;
                return;
            }

            _demoImage = LoadDemoGif();
            if (_demoImage != null)
            {
                _demoPicture.Image = _demoImage;
                return;
            }

            _demoPicture.Image = null;
            _demoPicture.Controls.Clear();
            _demoPicture.Controls.Add(new Label
            {
                Text = "Demo animation missing.\nAdd win\\assets\\default-apps-setup.gif and rebuild.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = UiChrome.Muted,
                Font = new Font("Segoe UI", 9f)
            });
        }

        private Image LoadDemoGif()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(DefaultAppsGifResource);
            if (stream != null)
            {
                _demoImageStream = CopyToMemory(stream);
                stream.Dispose();
                return Image.FromStream(_demoImageStream);
            }

            var besideExe = Path.Combine(
                Path.GetDirectoryName(assembly.Location) ?? string.Empty,
                "assets",
                DefaultAppsGifFileName);
            if (File.Exists(besideExe))
            {
                _demoImageStream = new MemoryStream(File.ReadAllBytes(besideExe));
                return Image.FromStream(_demoImageStream);
            }

            try
            {
                var devPath = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..",
                    "assets",
                    DefaultAppsGifFileName));
                if (File.Exists(devPath))
                {
                    _demoImageStream = new MemoryStream(File.ReadAllBytes(devPath));
                    return Image.FromStream(_demoImageStream);
                }
            }
            catch
            {
            }

            return null;
        }

        private static MemoryStream CopyToMemory(Stream source)
        {
            var copy = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                copy.Write(buffer, 0, read);
            copy.Position = 0;
            return copy;
        }

        private void DisposeDemoImage()
        {
            if (_demoPicture != null)
                _demoPicture.Image = null;
            if (_demoImage != null)
            {
                _demoImage.Dispose();
                _demoImage = null;
            }
            if (_demoImageStream != null)
            {
                _demoImageStream.Dispose();
                _demoImageStream = null;
            }
        }
    }
}
