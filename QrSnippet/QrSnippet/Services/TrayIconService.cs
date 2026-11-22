using System.Drawing;
using System.Windows.Forms;

namespace QrSnippet.Services
{
    /// <summary>
    /// Notification area icon with a small menu. WPF has no tray icon, so this wraps the WinForms one.
    /// </summary>
    public sealed class TrayIconService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;

        public TrayIconService(string tooltip, Action open, Action capture, Action exit)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => open()).Font = new Font(menu.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add("Capture screen\tWin+Shift+Q", null, (_, _) => capture());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => exit());

            _notifyIcon = new NotifyIcon
            {
                // Reuse the icon embedded in the exe (ApplicationIcon) instead of shipping another copy.
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
                Text = tooltip,
                ContextMenuStrip = menu,
                Visible = true,
            };
            _notifyIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    open();
                }
            };
        }

        public void ShowNotification(string title, string message)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
        }
    }
}
