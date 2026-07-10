using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Sanity
{
    public class StatisticsForm : Form
    {
        public StatisticsForm()
        {
            var metrics = UsageMetrics.Load();

            Text = "Statistics";
            Icon = AppIcon.Get();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(500, 520);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;

            Controls.Add(BuildHeader());
            Controls.Add(BuildStatsPanel(metrics));
            Controls.Add(BuildDomainSection(metrics));

            var links = UiChrome.CreateLinksPanel();
            links.Location = new Point(22, 458);
            Controls.Add(links);
        }

        private Control BuildHeader()
        {
            var panel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 86),
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
                Text = "Statistics",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(88, 16)
            });
            panel.Controls.Add(new Label
            {
                Text = "Tracking parameters removed before you paste or open links.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(88, 50),
                Size = new Size(380, 24)
            });
            return panel;
        }

        private Control BuildStatsPanel(UsageMetrics metrics)
        {
            var panel = new Panel
            {
                Location = new Point(22, 92),
                Size = new Size(456, 118),
                BackColor = UiChrome.PanelBg
            };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(UiChrome.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                using (var pen = new Pen(UiChrome.Border))
                    e.Graphics.DrawLine(pen, panel.Width / 2, 22, panel.Width / 2, panel.Height - 22);
            };

            panel.Controls.Add(new Label
            {
                Text = "PERFORMANCE",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UiChrome.SanityGreen,
                AutoSize = true,
                Location = new Point(16, 10)
            });

            var topShare = GetTopDomainSharePercent(metrics);
            AddStat(
                panel,
                metrics.LinksCleaned.ToString("N0"),
                topShare.HasValue ? topShare.Value + "%" : null,
                "Total cleaned of " + metrics.LinksCleaned.ToString("N0")
                    + (metrics.LinksCleaned == 1 ? " click" : " clicks"),
                16,
                34);

            AddStat(
                panel,
                metrics.DomainCount.ToString("N0"),
                null,
                metrics.DomainCount == 1 ? "Domain protected" : "Domains protected",
                248,
                34);

            return panel;
        }

        private static int? GetTopDomainSharePercent(UsageMetrics metrics)
        {
            if (metrics.LinksCleaned <= 0)
                return null;

            var rows = metrics.GetDomainsByCount();
            if (rows.Count == 0)
                return null;

            return (int)Math.Round(100.0 * rows[0].Value / metrics.LinksCleaned);
        }

        private static void AddStat(Control parent, string value, string percent, string caption, int x, int y)
        {
            const int columnWidth = 200;
            var percentReserve = string.IsNullOrEmpty(percent) ? 0 : 44;
            var valueWidth = columnWidth - percentReserve;
            var fontSize = FitFontSize(value, 26f, 11f, valueWidth);

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = false,
                Location = new Point(x, y),
                Size = new Size(valueWidth, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(valueLabel);

            if (!string.IsNullOrEmpty(percent))
            {
                var textWidth = TextRenderer.MeasureText(
                    value,
                    valueLabel.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding).Width;
                parent.Controls.Add(new Label
                {
                    Text = percent,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = UiChrome.SanityGreen,
                    AutoSize = true,
                    Location = new Point(x + Math.Min(textWidth, valueWidth) + 4, y + 12)
                });
            }

            parent.Controls.Add(new Label
            {
                Text = caption,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiChrome.Muted,
                AutoSize = false,
                Location = new Point(x, y + 44),
                Size = new Size(columnWidth, 28)
            });
        }

        private static float FitFontSize(string text, float maxSize, float minSize, int maxWidth)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                for (var size = maxSize; size >= minSize; size -= 1f)
                {
                    using (var font = new Font("Segoe UI", size, FontStyle.Bold))
                    {
                        if (graphics.MeasureString(text, font).Width <= maxWidth)
                            return size;
                    }
                }
            }

            return minSize;
        }

        private Control BuildDomainSection(UsageMetrics metrics)
        {
            var panel = new Panel
            {
                Location = new Point(22, 226),
                Size = new Size(456, 220),
                BackColor = Color.White
            };

            panel.Controls.Add(new Label
            {
                Text = "Domains",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UiChrome.Ink,
                AutoSize = true,
                Location = new Point(0, 0)
            });
            panel.Controls.Add(new Label
            {
                Text = "Share of total cleaned clicks",
                Font = new Font("Segoe UI", 8f),
                ForeColor = UiChrome.Muted,
                AutoSize = true,
                Location = new Point(0, 20)
            });

            var list = new ListView
            {
                Location = new Point(0, 42),
                Size = new Size(456, 178),
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                GridLines = false,
                MultiSelect = false,
                HideSelection = true,
                OwnerDraw = true
            };
            list.Columns.Add("Domain", 300);
            list.Columns.Add("Cleans", 136);

            var rows = metrics.GetDomainsByCount();
            var total = Math.Max(metrics.LinksCleaned, 1);
            if (rows.Count == 0)
            {
                var empty = new ListViewItem(new[] { "No cleans recorded yet — copy or open a tracked link", "—" });
                empty.ForeColor = UiChrome.Muted;
                list.Items.Add(empty);
            }
            else
            {
                var rank = 1;
                foreach (var row in rows)
                {
                    var percent = (int)Math.Round(100.0 * row.Value / total);
                    var item = new ListViewItem(new[]
                    {
                        row.Key,
                        row.Value.ToString("N0") + "  (" + percent + "%)"
                    });
                    if (rank <= 3)
                        item.Font = new Font(list.Font, FontStyle.Bold);
                    list.Items.Add(item);
                    rank++;
                }
            }

            list.DrawColumnHeader += (s, e) =>
            {
                using (var brush = new SolidBrush(UiChrome.PanelBg))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                using (var pen = new Pen(UiChrome.Border))
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                var flags = e.ColumnIndex == 1
                    ? TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis
                    : TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header.Text.ToUpperInvariant(),
                    new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    Rectangle.Inflate(e.Bounds, -10, 0),
                    UiChrome.Muted,
                    flags);
            };
            list.DrawItem += (s, e) => { };
            list.DrawSubItem += (s, e) =>
            {
                var bg = e.ItemIndex % 2 == 0 ? Color.White : UiChrome.PanelBg;
                using (var brush = new SolidBrush(bg))
                    e.Graphics.FillRectangle(brush, e.Bounds);

                var align = e.ColumnIndex == 1
                    ? TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis
                    : TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
                var color = e.ColumnIndex == 1 ? UiChrome.SanityGreen : UiChrome.Ink;

                TextRenderer.DrawText(
                    e.Graphics,
                    e.SubItem.Text,
                    e.Item.Font,
                    Rectangle.Inflate(e.Bounds, -10, 0),
                    color,
                    align);
            };

            panel.Controls.Add(list);
            return panel;
        }
    }
}
