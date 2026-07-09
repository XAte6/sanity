using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sanity
{
    public static class AppIcon
    {
        private static Icon _cachedIcon;
        private static readonly Color Background = Color.FromArgb(34, 139, 34);

        public static Icon Get()
        {
            if (_cachedIcon == null)
            {
                var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                _cachedIcon = extracted ?? CreateIcon(32);
            }

            return (Icon)_cachedIcon.Clone();
        }

        public static void SaveToFile(string path)
        {
            using (var icon = CreateIcon(32))
            using (var stream = File.Create(path))
            {
                icon.Save(stream);
            }
        }

        private static Icon CreateIcon(int size)
        {
            using (var bitmap = CreateBitmap(size))
            {
                var handle = bitmap.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(handle).Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        private static Bitmap CreateBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Background);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var fontSize = size * 0.56f;
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(Color.White))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    graphics.DrawString("S", font, brush, new RectangleF(0, 0, size, size), format);
                }
            }

            return bitmap;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
