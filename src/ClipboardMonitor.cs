using System;
using System.Windows.Forms;

namespace Sanity
{
    public class ClipboardMonitor : Form
    {
        private readonly AppConfig _config;
        private bool _updatingClipboard;

        public event EventHandler ClipboardCleaned;

        public ClipboardMonitor(AppConfig config)
        {
            _config = config;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            Opacity = 0;
            Width = 0;
            Height = 0;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.AddClipboardFormatListener(Handle);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            NativeMethods.RemoveClipboardFormatListener(Handle);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                ProcessClipboard();
            }

            base.WndProc(ref m);
        }

        public void ProcessClipboard()
        {
            if (_updatingClipboard || !_config.IsActive)
                return;

            if (!Clipboard.ContainsText())
                return;

            string text;
            try
            {
                text = Clipboard.GetText();
            }
            catch
            {
                return;
            }

            string cleaned;
            if (!UrlCleaner.TryClean(text, _config.Rules, out cleaned))
                return;

            try
            {
                _updatingClipboard = true;
                Clipboard.SetText(cleaned);
                if (ClipboardCleaned != null)
                    ClipboardCleaned(this, EventArgs.Empty);
            }
            catch
            {
                // Clipboard may be locked by another application.
            }
            finally
            {
                _updatingClipboard = false;
            }
        }
    }
}
