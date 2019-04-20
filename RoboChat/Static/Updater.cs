using NAppUpdate.Framework;
using NAppUpdate.Framework.Sources;
using RoboChat.DataStructures;
using RoboChat.Enums;
using System;
using System.IO;
using System.Threading;

namespace RoboChat
{
    public static class AppUpdater
    {
        const int CHECK_UPDATE_INTERVAL = 3600000;
        const int UPDATE_BALLOON_TIMEOUT = 10000;

        private static Timer _timer;
        private static object _locker = new object();

        public static void Start()
        {
            UpdateManager.Instance.UpdateSource = new SimpleWebSource(Settings.Instance.UpdateSource);
            UpdateManager.Instance.ReinstateIfRestarted();

            // Проверка обновлений при старте
            UpdateCheck(new UpdaterState()
            {
                IsDialog = true
            });

            // Проверка обновлений каждый час
            _timer = new Timer(new TimerCallback(UpdateCheck), new UpdaterState()
            {
                IsDialog = false
            }, CHECK_UPDATE_INTERVAL, CHECK_UPDATE_INTERVAL);
        }

        public static void Stop()
        {
            if (_timer != null)
                _timer.Dispose();
        }

        public static void UpdateCheck(object updaterStateParam)
        {
            lock (_locker)
            {
                try
                {
                    // Костыль для RobotNet, который меняет текущую папку на себя
                    string oldDir = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(Settings.Directory);

                    UpdateManager.Instance.CleanUp();
                    UpdateManager.Instance.CheckForUpdates();
                    if (UpdateManager.Instance.UpdatesAvailable > 0)
                    {
                        // Показать ли диалоговое окно сразу?
                        var updaterState = (UpdaterState)updaterStateParam;
                        if (!updaterState.IsDialog)
                        {
                            AppTray.Message(System.Windows.Forms.ToolTipIcon.Info,
                                "A new update is available!" + Environment.NewLine +
                                "Click to update.", Settings.TitleUpdate, UPDATE_BALLOON_TIMEOUT);
                        }
                        else
                        {
                            new UpdaterView().ShowDialog();
                        }
                    }

                    Directory.SetCurrentDirectory(oldDir);
                }
                catch (Exception ex)
                {
                    ex.Process(ErrorHandlingLevels.Silent);
                }
            }
        }
    }
}