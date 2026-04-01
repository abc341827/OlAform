using System.Drawing;
using System.Windows.Forms;

namespace OlAform
{
    internal sealed class WindowHighlightOverlay : Form
    {
        public WindowHighlightOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            TopMost = true;
            Enabled = false;
            ShowIcon = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_TOPMOST = 0x00000008;
                const int WS_EX_TRANSPARENT = 0x00000020;
                const int WS_EX_NOACTIVATE = 0x08000000;

                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.Red, 3);
            e.Graphics.DrawRectangle(pen, 1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        }

        public void ShowBorder(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                HideOverlay();
                return;
            }

            if (Bounds != bounds)
            {
                Bounds = bounds;
            }

            if (!Visible)
            {
                Show();
            }

            Refresh();
        }

        public void HideOverlay()
        {
            if (Visible)
            {
                Hide();
            }
        }
    }
}
