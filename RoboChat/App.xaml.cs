using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RoboChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        public App()
        {
            InitializeComponent();

            // Применим единые стили ко всем окнам
            FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
            {
                DefaultValue = FindResource(typeof(Window))
            });
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            e.Exception.Process(ErrorHandlingLevels.Modal);
            Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            AppUpdater.Stop();

            if (_mutex != null)
                _mutex.Dispose();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // =============================================================================
            // Первый этап: базовые проверки и настройки
            // =============================================================================

            // Вначале обязательно проверить, что из той же папки не запущен еще один экземпляр 
            CheckSingleInstance(e.Args);

            // Проверим, что все базовые настройки в наличии
            bool? resultCheckSettings = CheckSettings();
            if (!resultCheckSettings.HasValue || !resultCheckSettings.Value)
            {
                Shutdown();

                // Программа завершается асинхронно, поэтому надо еще и выйти из метода
                return;
            }

            // Проверим, что все базовые настройки в наличии
            bool? resultCheckUser = CheckUser();
            if (!resultCheckUser.HasValue || !resultCheckUser.Value)
            {
                Shutdown();

                // Программа завершается асинхронно, поэтому надо еще и выйти из метода
                return;
            }

            // Сгенерируем новую пару публичного и приватного ключа, если требуется
            CheckKeys();

            // Поправки для перехода к новой версии
            CheckOldVersion();

            // =============================================================================
            // Второй этап: статические классы уровня приложения
            // =============================================================================

            // Инициализируем иконку в трее
            AppTray.Start();

            // Запускаем регулярную проверку обновлений
            AppUpdater.Start();

            // =============================================================================
            // Третий этап: главное окно приложения
            // =============================================================================

            // Главное окно
            new RosterView().Show();
        }

        private void CheckSingleInstance(string[] args)
        {
            string guid = Helpers.ComputeMD5(Settings.Directory);
            _mutex = new Mutex(false, guid);
            if (!args.Contains("--nocheck") && !_mutex.WaitOne(0, false))
                throw new Exception("RoboChat is already running! You must close the previous one first.");
        }

        private bool? CheckSettings()
        {
            if (Settings.Instance.BasicSettingsOk)
                return true;

            return new BasicConfigurationView(true).ShowDialog();
        }

        private bool? CheckUser()
        {
            if (Settings.Instance.Name != "newuser")
                return true;

            return new UserView(true).ShowDialog();
        }

        private void CheckKeys()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.PrivateKey) && !string.IsNullOrEmpty(Settings.Instance.PublicKey))
                return;

            string privateKey, publicKey;
            Helpers.GenerateKeyPair(out privateKey, out publicKey);
            Settings.Instance.PrivateKey = privateKey;
            Settings.Instance.PublicKey = publicKey;
        }

        private void CheckOldVersion()
        {
            string contactsPathOld = Path.Combine(Settings.Directory, "config\\contacts_cache.cfg");
            string contactsPathNew = Path.Combine(Settings.Directory, "config\\contacts.cfg");
            if (File.Exists(contactsPathOld))
            {
                File.Delete(contactsPathNew);
                File.Move(contactsPathOld, contactsPathNew);
            }

            string profilePathOld = Path.Combine(Settings.Directory, "config\\--profile.cfg");
            if (File.Exists(profilePathOld))
                File.Delete(profilePathOld);
        }
    }
}