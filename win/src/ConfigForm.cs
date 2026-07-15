using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Sanity
{
    public class ConfigForm : Form
    {
        private readonly AppConfig _config;
        private readonly List<UrlRule> _rules;
        private readonly ListView _list;
        private readonly TextBox _domainFilter;
        private readonly TextBox _regexFilter;
        private readonly Label _countLabel;
        private readonly Bitmap _pencilIcon;
        private readonly Bitmap _binIcon;

        private const int DomainColWidth = 180;
        private const int ActionsColWidth = 78;
        private const string DomainPlaceholder = "Filter domain…";
        private const string RegexPlaceholder = "Filter regex…";

        public ConfigForm(AppConfig config)
        {
            _config = config;
            _rules = CloneRules(config.Rules);
            _pencilIcon = UiChrome.DrawPencilIcon(16);
            _binIcon = UiChrome.DrawBinIcon(16);

            Text = "Regex Rules";
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(720, 520);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            Controls.Add(BuildHeader());

            var tablePanel = new Panel
            {
                Location = new Point(22, 96),
                Size = new Size(676, 360),
                BackColor = Color.White
            };

            var domainHeader = MakeColumnHeader("DOMAIN", 0);
            var regexHeader = MakeColumnHeader("REGEX TO REMOVE", DomainColWidth + 8);
            var actionsHeader = MakeColumnHeader("ACTIONS", 676 - ActionsColWidth);
            tablePanel.Controls.Add(domainHeader);
            tablePanel.Controls.Add(regexHeader);
            tablePanel.Controls.Add(actionsHeader);

            _domainFilter = UiChrome.CreateFilterBox(DomainPlaceholder);
            _domainFilter.Location = new Point(0, 22);
            _domainFilter.Width = DomainColWidth;

            _regexFilter = UiChrome.CreateFilterBox(RegexPlaceholder);
            _regexFilter.Location = new Point(DomainColWidth + 8, 22);
            _regexFilter.Width = 676 - DomainColWidth - ActionsColWidth - 16 - 34;

            var addButton = UiChrome.CreateIconButton(UiChrome.DrawPlusIcon(18), "Add rule");
            addButton.Location = new Point(676 - 30, 20);
            addButton.Click += (s, e) => AddRule();

            tablePanel.Controls.Add(_domainFilter);
            tablePanel.Controls.Add(_regexFilter);
            tablePanel.Controls.Add(addButton);

            _list = new ListView
            {
                Location = new Point(0, 54),
                Size = new Size(676, 280),
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                GridLines = false,
                MultiSelect = false,
                HideSelection = true,
                OwnerDraw = true,
                Scrollable = true
            };
            _list.Columns.Add("Domain", DomainColWidth);
            _list.Columns.Add("Regex", 100);
            _list.Columns.Add("Actions", ActionsColWidth);
            _list.HandleCreated += (s, e) => FitColumns();
            _list.SizeChanged += (s, e) => FitColumns();
            _list.DrawItem += (s, e) => { };
            _list.DrawSubItem += DrawSubItem;
            _list.MouseClick += ListMouseClick;
            _list.MouseDoubleClick += (s, e) =>
            {
                var info = _list.HitTest(e.Location);
                if (info.Item != null)
                    EditRule(info.Item.Index);
            };

            _countLabel = new Label
            {
                Font = new Font("Segoe UI", 8f),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(0, 340)
            };

            tablePanel.Controls.Add(_list);
            tablePanel.Controls.Add(_countLabel);
            Controls.Add(tablePanel);

            _domainFilter.TextChanged += (s, e) => RefreshList();
            _regexFilter.TextChanged += (s, e) => RefreshList();

            var resetButton = new Button
            {
                Text = "Reset to defaults",
                Width = 130,
                Height = 28,
                Location = new Point(22, 470),
                FlatStyle = FlatStyle.System
            };
            resetButton.Click += (s, e) => ResetToDefaults();
            Controls.Add(resetButton);

            var links = UiChrome.CreateLinksPanel();
            links.Location = new Point(170, 470);
            Controls.Add(links);

            FormClosing += (s, e) => Persist();
            RefreshList();
        }

        private void ResetToDefaults()
        {
            var answer = MessageBox.Show(
                this,
                "Replace all current rules with the default list from GitHub?",
                "Reset regex rules",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            try
            {
                var catalog = DefaultRules.LoadForReset();
                _rules.Clear();
                _rules.AddRange(CloneRules(catalog.Rules));
                _config.RulesVersion = catalog.Version;
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Could not load default rules:\n" + ex.Message,
                    "Reset regex rules",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private Control BuildHeader()
        {
            var panel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(720, 86),
                BackColor = Color.White
            };

            panel.Controls.Add(new PictureBox
            {
                Image = AppIcon.Get().ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(22, 16),
                Size = new Size(52, 52)
            });
            panel.Controls.Add(new Label
            {
                Text = "Regex Rules",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(88, 16)
            });
            panel.Controls.Add(new Label
            {
                Text = "Domain host to match (* = all). Regex pattern removed from matching URLs.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(88, 50),
                Size = new Size(600, 24)
            });
            return panel;
        }

        private static Label MakeColumnHeader(string text, int x)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(x, 4)
            };
        }

        private void RefreshList()
        {
            var domainFilter = UiChrome.FilterText(_domainFilter, DomainPlaceholder);
            var regexFilter = UiChrome.FilterText(_regexFilter, RegexPlaceholder);

            _list.BeginUpdate();
            _list.Items.Clear();

            var shown = 0;
            for (var i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                var domain = rule.Domain ?? string.Empty;
                var regex = rule.Regex ?? string.Empty;

                if (!string.IsNullOrEmpty(domainFilter)
                    && domain.IndexOf(domainFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (!string.IsNullOrEmpty(regexFilter)
                    && regex.IndexOf(regexFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var item = new ListViewItem(new[] { domain, regex, string.Empty });
                item.Tag = i;
                _list.Items.Add(item);
                shown++;
            }

            _list.EndUpdate();
            FitColumns();
            _countLabel.Text = shown == _rules.Count
                ? _rules.Count + " rule" + (_rules.Count == 1 ? "" : "s")
                : "Showing " + shown + " of " + _rules.Count + " rules";
        }

        private void FitColumns()
        {
            if (_list == null || _list.Columns.Count < 3 || !_list.IsHandleCreated)
                return;

            // ClientSize already excludes a visible vertical scrollbar; keep a 2px
            // slack so column totals never trigger a horizontal scrollbar.
            var width = _list.ClientSize.Width - 2;
            if (width <= 0)
                return;

            _list.Columns[0].Width = DomainColWidth;
            _list.Columns[2].Width = ActionsColWidth;
            _list.Columns[1].Width = Math.Max(80, width - DomainColWidth - ActionsColWidth);
        }

        private void DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var bg = e.ItemIndex % 2 == 0 ? Color.White : UiChrome.PanelBg;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);

            if (e.ColumnIndex == 2)
            {
                var pencilRect = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top + (e.Bounds.Height - 16) / 2, 16, 16);
                var binRect = new Rectangle(e.Bounds.Left + 40, e.Bounds.Top + (e.Bounds.Height - 16) / 2, 16, 16);
                e.Graphics.DrawImage(_pencilIcon, pencilRect);
                e.Graphics.DrawImage(_binIcon, binRect);
                return;
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem.Text,
                e.Item.Font,
                Rectangle.Inflate(e.Bounds, -8, 0),
                UiChrome.Ink,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private void ListMouseClick(object sender, MouseEventArgs e)
        {
            var info = _list.HitTest(e.Location);
            if (info.Item == null || info.SubItem == null)
                return;
            if (info.Item.SubItems.IndexOf(info.SubItem) != 2)
                return;

            var bounds = info.SubItem.Bounds;
            var pencilRect = new Rectangle(bounds.Left + 6, bounds.Top, 28, bounds.Height);
            var binRect = new Rectangle(bounds.Left + 34, bounds.Top, 28, bounds.Height);
            var rowIndex = info.Item.Index;

            if (pencilRect.Contains(e.Location))
                EditRule(rowIndex);
            else if (binRect.Contains(e.Location))
                DeleteRule(rowIndex);
        }

        private void AddRule()
        {
            using (var dialog = new RuleEditDialog("*", string.Empty, "Add rule"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                _rules.Add(new UrlRule { Domain = dialog.DomainValue, Regex = dialog.RegexValue });
                RefreshList();
            }
        }

        private void EditRule(int visibleIndex)
        {
            if (visibleIndex < 0 || visibleIndex >= _list.Items.Count)
                return;

            var ruleIndex = (int)_list.Items[visibleIndex].Tag;
            var rule = _rules[ruleIndex];
            using (var dialog = new RuleEditDialog(rule.Domain, rule.Regex, "Edit rule"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                rule.Domain = dialog.DomainValue;
                rule.Regex = dialog.RegexValue;
                RefreshList();
            }
        }

        private void DeleteRule(int visibleIndex)
        {
            if (visibleIndex < 0 || visibleIndex >= _list.Items.Count)
                return;

            var ruleIndex = (int)_list.Items[visibleIndex].Tag;
            var rule = _rules[ruleIndex];
            var label = string.IsNullOrWhiteSpace(rule.Domain) ? "(blank domain)" : rule.Domain;
            var result = MessageBox.Show(
                this,
                "Delete rule for " + label + "?",
                "Delete rule",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            _rules.RemoveAt(ruleIndex);
            RefreshList();
        }

        private void Persist()
        {
            var rules = new List<UrlRule>();
            foreach (var rule in _rules)
            {
                var domain = (rule.Domain ?? string.Empty).Trim();
                var regex = (rule.Regex ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(domain) && string.IsNullOrWhiteSpace(regex))
                    continue;
                rules.Add(new UrlRule { Domain = domain, Regex = regex });
            }

            _config.Rules = rules;
            _config.Save();
        }


        private static List<UrlRule> CloneRules(IList<UrlRule> source)
        {
            var list = new List<UrlRule>();
            if (source == null)
                return list;

            foreach (var rule in source)
            {
                list.Add(new UrlRule
                {
                    Domain = rule.Domain ?? string.Empty,
                    Regex = rule.Regex ?? string.Empty
                });
            }
            return list;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pencilIcon != null)
                    _pencilIcon.Dispose();
                if (_binIcon != null)
                    _binIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal class RuleEditDialog : Form
    {
        private readonly TextBox _domainBox;
        private readonly TextBox _regexBox;
        private readonly TextBox _testUrlBox;
        private readonly Label _resultLabel;

        public string DomainValue
        {
            get { return _domainBox.Text.Trim(); }
        }

        public string RegexValue
        {
            get { return _regexBox.Text.Trim(); }
        }

        public RuleEditDialog(string domain, string regex, string title)
        {
            Text = title;
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 340);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            Controls.Add(new Label
            {
                Text = "Domain",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(20, 18)
            });

            _domainBox = new TextBox
            {
                Text = domain ?? string.Empty,
                Location = new Point(20, 38),
                Width = 480,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_domainBox);

            Controls.Add(new Label
            {
                Text = "Regex to remove",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(20, 78)
            });

            _regexBox = new TextBox
            {
                Text = regex ?? string.Empty,
                Location = new Point(20, 98),
                Width = 480,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_regexBox);

            Controls.Add(new Label
            {
                Text = "Paste a URL to test",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(20, 138)
            });

            _testUrlBox = new TextBox
            {
                Location = new Point(20, 158),
                Width = 480,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_testUrlBox);

            var resultPanel = new Panel
            {
                Location = new Point(20, 194),
                Size = new Size(480, 72),
                BackColor = UiChrome.PanelBg
            };
            resultPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiChrome.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, resultPanel.Width - 1, resultPanel.Height - 1);
            };

            resultPanel.Controls.Add(new Label
            {
                Text = "RESULT",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = UiChrome.SanityGreen,
                AutoSize = true,
                Location = new Point(10, 8)
            });

            _resultLabel = new Label
            {
                Text = "Paste a URL above to preview this rule.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(10, 28),
                Size = new Size(460, 36)
            };
            resultPanel.Controls.Add(_resultLabel);
            Controls.Add(resultPanel);

            var save = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Width = 90,
                Height = 30,
                Location = new Point(310, 290),
                FlatStyle = FlatStyle.System
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 90,
                Height = 30,
                Location = new Point(410, 290),
                FlatStyle = FlatStyle.System
            };
            AcceptButton = save;
            CancelButton = cancel;
            Controls.Add(save);
            Controls.Add(cancel);

            EventHandler refresh = (s, e) => UpdateTestPreview();
            _domainBox.TextChanged += refresh;
            _regexBox.TextChanged += refresh;
            _testUrlBox.TextChanged += refresh;
        }

        private void UpdateTestPreview()
        {
            var url = _testUrlBox.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                _resultLabel.ForeColor = UiChrome.Muted;
                _resultLabel.Text = "Paste a URL above to preview this rule.";
                return;
            }

            var rules = new List<UrlRule>
            {
                new UrlRule
                {
                    Domain = DomainValue,
                    Regex = RegexValue
                }
            };

            string cleaned;
            if (UrlCleaner.TryClean(url, rules, out cleaned))
            {
                _resultLabel.ForeColor = UiChrome.SanityGreen;
                _resultLabel.Text = cleaned;
            }
            else if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _resultLabel.ForeColor = UiChrome.Danger;
                _resultLabel.Text = "Enter a full http:// or https:// URL.";
            }
            else
            {
                _resultLabel.ForeColor = UiChrome.Muted;
                _resultLabel.Text = "No change — domain or regex did not match.";
            }
        }
    }
}
