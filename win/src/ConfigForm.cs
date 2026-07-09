using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Sanity
{
    public class ConfigForm : Form
    {
        private readonly DataGridView _grid;
        private readonly AppConfig _config;

        public ConfigForm(AppConfig config)
        {
            _config = config;

            Text = "Sanity - URL Tracker Rules";
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            Width = 720;
            Height = 480;
            MinimumSize = new Size(600, 360);
            Font = SystemFonts.MessageBoxFont;

            var instructions = new Label
            {
                Text = "Domain: host name to match (* = all). Regex: pattern removed from matching URLs.",
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(8, 8, 8, 0)
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Domain",
                HeaderText = "Domain",
                DataPropertyName = "Domain",
                Width = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Regex",
                HeaderText = "Regex to remove",
                DataPropertyName = "Regex",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };

            var saveButton = new Button { Text = "Save", Width = 90, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            var removeButton = new Button { Text = "Remove Selected", Width = 120 };
            var addButton = new Button { Text = "Add Row", Width = 90 };

            saveButton.Click += (s, e) => SaveGridToConfig();
            removeButton.Click += (s, e) => RemoveSelectedRows();
            addButton.Click += (s, e) => _grid.Rows.Add("*", string.Empty);

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(removeButton);
            buttonPanel.Controls.Add(addButton);

            Controls.Add(_grid);
            Controls.Add(buttonPanel);
            Controls.Add(instructions);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            LoadRulesIntoGrid();
        }

        private void LoadRulesIntoGrid()
        {
            _grid.Rows.Clear();
            foreach (var rule in _config.Rules)
            {
                _grid.Rows.Add(rule.Domain, rule.Regex);
            }
        }

        private void RemoveSelectedRows()
        {
            foreach (DataGridViewRow row in _grid.SelectedRows)
            {
                if (!row.IsNewRow)
                    _grid.Rows.Remove(row);
            }
        }

        private void SaveGridToConfig()
        {
            var rules = new List<UrlRule>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                var domain = Convert.ToString(row.Cells[0].Value) ?? string.Empty;
                var regex = Convert.ToString(row.Cells[1].Value) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(domain) && string.IsNullOrWhiteSpace(regex))
                    continue;

                rules.Add(new UrlRule
                {
                    Domain = domain.Trim(),
                    Regex = regex.Trim()
                });
            }

            _config.Rules = rules;
            _config.Save();
        }
    }
}
