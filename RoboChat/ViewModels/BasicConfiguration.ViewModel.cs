using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace RoboChat.ViewModels
{
    class BasicConfigurationViewModel : INotifyPropertyChanged
    {
        private bool _startup;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Binding

        public bool BasicSettingsOk
        {
            get
            {
                return Settings.Instance.IgnoreBasicSettings ||
                    !string.IsNullOrEmpty(CacheName) &&
                    !string.IsNullOrEmpty(CachePass) &&
                    !string.IsNullOrEmpty(UpdateSource);
            }
        }

        private string _updateSource;
        public string UpdateSource
        {
            get
            {
                return _updateSource;
            }
            set
            {
                _updateSource = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        private string _cacheName;
        public string CacheName
        {
            get
            {
                return _cacheName;
            }
            set
            {
                _cacheName = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        private string _cachePass;
        public string CachePass
        {
            get
            {
                return _cachePass;
            }
            set
            {
                _cachePass = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        private long _fileSizeLimit;
        public long FileSizeLimit
        {
            get
            {
                return _fileSizeLimit;
            }
            set
            {
                _fileSizeLimit = value;
                NotifyPropertyChanged();
            }
        }

        private int _fileBlockSize;
        public int FileBlockSize
        {
            get
            {
                return _fileBlockSize;
            }
            set
            {
                _fileBlockSize = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand OK { get; private set; }
        public ICommand Cancel { get; private set; }

        #endregion

        public BasicConfigurationViewModel(bool startup = false)
        {
            _startup = startup;

            UpdateSource = Settings.Instance.UpdateSource;
            CacheName = Settings.Instance.CacheName;
            CachePass = Settings.Instance.CachePass;
            FileSizeLimit = Settings.Instance.FileSizeLimit;
            FileBlockSize = Settings.Instance.FileBlockSize;

            OK = new DelegateCommand(p => DoOK(p));
            Cancel = new DelegateCommand(p => DoCancel(p));
        }

        private void DoOK(object window)
        {
            if (!BasicSettingsOk)
            {
                if (System.Windows.MessageBox.Show("Some of the fields are empty. Continue anyway?",
                    Settings.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                Settings.Instance.IgnoreBasicSettings = true;
            }

            bool needRestart = false;
            if (!_startup &&
                (Settings.Instance.UpdateSource != UpdateSource ||
                Settings.Instance.CacheName != CacheName ||
                Settings.Instance.CachePass != CachePass))
            {
                if (MessageBox.Show("Some of the changes require a restart of the application. Continue?",
                    Settings.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                needRestart = true;
            }

            Settings.Instance.UpdateSource = UpdateSource;
            Settings.Instance.CacheName = CacheName;
            Settings.Instance.CachePass = CachePass;
            Settings.Instance.FileSizeLimit = FileSizeLimit;
            Settings.Instance.FileBlockSize = FileBlockSize;

            if (needRestart)
            {
                Helpers.Restart();
            }

            ((Window)window).DialogResult = true;
            ((Window)window).Close();
        }

        private void DoCancel(object window)
        {
            ((Window)window).DialogResult = false;
            ((Window)window).Close();
        }
    }
}