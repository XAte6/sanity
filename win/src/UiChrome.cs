using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Sanity
{
    public static class UiChrome
    {
        public static readonly Color SanityGreen = Color.FromArgb(34, 139, 34);
        public static readonly Color PanelBg = Color.FromArgb(248, 250, 248);
        public static readonly Color Border = Color.FromArgb(220, 228, 220);
        public static readonly Color Muted = Color.FromArgb(90, 100, 90);
        public static readonly Color Ink = Color.FromArgb(28, 36, 28);
        public static readonly Color Danger = Color.FromArgb(180, 50, 50);
        public static readonly Color Hover = Color.FromArgb(232, 245, 232);

        public static FlowLayoutPanel CreateLinksPanel()
        {
            var panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            panel.Controls.Add(CreateLinkChip("GitHub", "Repository", AppLinks.GitHub, DrawGitHubIcon));
            panel.Controls.Add(CreateLinkChip("Support", "Issues", AppLinks.Support, DrawSupportIcon));
            panel.Controls.Add(CreateLinkChip("Tip me", "PayPal", AppLinks.Tip, DrawTipIcon));
            return panel;
        }

        public static Control CreateLinkChip(string title, string subtitle, string url, Func<int, Bitmap> iconFactory)
        {
            var chip = new Panel
            {
                Width = 144,
                Height = 44,
                Margin = new Padding(0, 0, 10, 0),
                BackColor = PanelBg,
                Cursor = Cursors.Hand,
                Tag = url
            };
            chip.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, chip.Width - 1, chip.Height - 1);
            };

            var icon = new PictureBox
            {
                Image = iconFactory(22),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(10, 11),
                Size = new Size(22, 22),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Ink,
                AutoSize = true,
                Location = new Point(40, 6),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            var subtitleLabel = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(40, 24),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            EventHandler open = (s, e) => AppLinks.Open(url);
            EventHandler enter = (s, e) => chip.BackColor = Hover;
            EventHandler leave = (s, e) =>
            {
                if (!chip.ClientRectangle.Contains(chip.PointToClient(Cursor.Position)))
                    chip.BackColor = PanelBg;
            };

            foreach (Control child in new Control[] { chip, icon, titleLabel, subtitleLabel })
            {
                child.Click += open;
                child.MouseEnter += enter;
                child.MouseLeave += leave;
            }

            chip.Controls.Add(icon);
            chip.Controls.Add(titleLabel);
            chip.Controls.Add(subtitleLabel);
            return chip;
        }

        public static TextBox CreateFilterBox(string placeholder)
        {
            var box = new TextBox
            {
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 26
            };
            SetPlaceholder(box, placeholder);
            return box;
        }

        public static void SetPlaceholder(TextBox box, string placeholder)
        {
            box.ForeColor = Muted;
            box.Text = placeholder;
            box.GotFocus += (s, e) =>
            {
                if (box.ForeColor == Muted)
                {
                    box.Text = string.Empty;
                    box.ForeColor = Ink;
                }
            };
            box.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(box.Text))
                {
                    box.ForeColor = Muted;
                    box.Text = placeholder;
                }
            };
        }

        public static string FilterText(TextBox box, string placeholder)
        {
            if (box.ForeColor == Muted || box.Text == placeholder)
                return string.Empty;
            return box.Text.Trim();
        }

        public static Button CreateIconButton(Bitmap icon, string tooltip)
        {
            var button = new Button
            {
                Width = 30,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                Image = icon,
                ImageAlign = ContentAlignment.MiddleCenter,
                BackColor = PanelBg,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0),
                TabStop = false
            };
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = Hover;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(210, 235, 210);
            var tip = new ToolTip();
            tip.SetToolTip(button, tooltip);
            return button;
        }

        public static Bitmap DrawGitHubIcon(int size)
        {
            // Official Simple Icons GitHub mark path (viewBox 0 0 24 24).
            const string path =
                "M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12";
            return SvgPath.Render(path, size, Color.FromArgb(24, 23, 23));
        }

        public static Bitmap DrawSupportIcon(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.FromArgb(9, 105, 218)))
                    g.FillEllipse(brush, 1, 1, size - 3, size - 3);
                using (var font = new Font("Segoe UI", size * 0.48f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(Color.White))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    g.DrawString("!", font, brush, new RectangleF(0, 1, size, size), format);
                }
            }
            return bmp;
        }

        public static Bitmap DrawTipIcon(int size)
        {
            // PayPal dual-P mark in brand blues.
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                var dark = Color.FromArgb(0, 48, 135);
                var light = Color.FromArgb(0, 156, 222);
                using (var font = new Font("Arial Black", size * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var darkBrush = new SolidBrush(dark))
                using (var lightBrush = new SolidBrush(light))
                {
                    g.DrawString("P", font, lightBrush, size * 0.28f, size * -0.02f);
                    g.DrawString("P", font, darkBrush, size * 0.08f, size * 0.08f);
                }
            }
            return bmp;
        }

        public static Bitmap DrawPencilIcon(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var pen = new Pen(Ink, 1.6f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, size * 0.22f, size * 0.72f, size * 0.68f, size * 0.26f);
                    g.DrawLine(pen, size * 0.68f, size * 0.26f, size * 0.78f, size * 0.36f);
                    g.DrawLine(pen, size * 0.22f, size * 0.72f, size * 0.18f, size * 0.82f);
                    g.DrawLine(pen, size * 0.18f, size * 0.82f, size * 0.32f, size * 0.78f);
                }
            }
            return bmp;
        }

        public static Bitmap DrawBinIcon(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var pen = new Pen(Danger, 1.6f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, size * 0.28f, size * 0.30f, size * 0.72f, size * 0.30f);
                    g.DrawLine(pen, size * 0.38f, size * 0.22f, size * 0.62f, size * 0.22f);
                    g.DrawLine(pen, size * 0.34f, size * 0.30f, size * 0.38f, size * 0.80f);
                    g.DrawLine(pen, size * 0.66f, size * 0.30f, size * 0.62f, size * 0.80f);
                    g.DrawLine(pen, size * 0.38f, size * 0.80f, size * 0.62f, size * 0.80f);
                    g.DrawLine(pen, size * 0.46f, size * 0.38f, size * 0.46f, size * 0.70f);
                    g.DrawLine(pen, size * 0.54f, size * 0.38f, size * 0.54f, size * 0.70f);
                }
            }
            return bmp;
        }

        public static Bitmap DrawPlusIcon(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(SanityGreen))
                    g.FillEllipse(brush, 1, 1, size - 3, size - 3);
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, size * 0.28f, size * 0.5f, size * 0.72f, size * 0.5f);
                    g.DrawLine(pen, size * 0.5f, size * 0.28f, size * 0.5f, size * 0.72f);
                }
            }
            return bmp;
        }
    }
}
