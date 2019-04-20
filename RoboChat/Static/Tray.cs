using System;
using System.Windows.Forms;

namespace RoboChat
{
    public static class AppTray
    {
        private const int BALLOON_TIMEOUT = 500;

        private static NotifyIcon _notifyIcon;
        private static ContextMenu _contextMenu;

        public static event EventHandler<EventArgs> Show;
        public static event EventHandler<EventArgs> Toggle;
        public static event EventHandler<EventArgs> CheckUpdates;
        public static event EventHandler<EventArgs> About;
        public static event EventHandler<WindowEventArgs> OpenWindow;

        public class WindowEventArgs : EventArgs
        {
            public string WindowTitle { get; set; }
            public WindowEventArgs(string windowTitle)
            {
                WindowTitle = windowTitle;
            }
        }

        public static void Start()
        {
            _contextMenu = new ContextMenu();

            var openMenuItem = new MenuItem("Show");
            openMenuItem.Click += (s, e) => Show?.Invoke(null, EventArgs.Empty);
            _contextMenu.MenuItems.Add(openMenuItem);

            _contextMenu.MenuItems.Add(new MenuItem("-"));

            var checkUpdatesMenuItem = new MenuItem("Check Updates");
            checkUpdatesMenuItem.Click += (s, e) => CheckUpdates?.Invoke(null, EventArgs.Empty);
            _contextMenu.MenuItems.Add(checkUpdatesMenuItem);

            var aboutMenuItem = new MenuItem("About RoboChat...");
            aboutMenuItem.Click += (s, e) => About?.Invoke(null, EventArgs.Empty);
            _contextMenu.MenuItems.Add(aboutMenuItem);

            _contextMenu.MenuItems.Add(new MenuItem("-"));

            var closeMenuItem = new MenuItem("Exit");
            closeMenuItem.Click += (s, e) => App.Current.Shutdown();
            _contextMenu.MenuItems.Add(closeMenuItem);

            _notifyIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.RoboChat32,
                ContextMenu = _contextMenu,
                Text = Settings.Instance.Name,
                Visible = true
            };

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left)
                    return;

                if (Toggle == null)
                    Message(ToolTipIcon.Warning, "Application is closing. Please wait...");

                Toggle?.Invoke(null, EventArgs.Empty);
            };
            _notifyIcon.BalloonTipClicked += _notifyIcon_BalloonTipClicked;
        }

        private static void _notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            // Если это системное сообщение
            if (_notifyIcon.BalloonTipTitle == Settings.TitleUpdate)
            {
                CheckUpdates?.Invoke(null, EventArgs.Empty);
            }

            string windowTitle = _notifyIcon.Tag as string;
            if (windowTitle != null && windowTitle.Length > 0)
            {
                OpenWindow?.Invoke(null, new WindowEventArgs(windowTitle));
            }
        }

        public static void Message(ToolTipIcon icon, string message, string title = null, int timeout = BALLOON_TIMEOUT, object tag = null)
        {
            _notifyIcon.BalloonTipTitle = title ?? Settings.Title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.Tag = tag;
            _notifyIcon.ShowBalloonTip(timeout);
        }
    }
}