using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace RoboChat.ViewModels
{
    class UserViewModel : INotifyPropertyChanged
    {
        private bool _startup;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Binding

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        private bool _requireEncryption;
        public bool RequireEncryption
        {
            get
            {
                return _requireEncryption;
            }
            set
            {
                _requireEncryption = value;
                NotifyPropertyChanged();
            }
        }

        private string _privateKey;
        public string PrivateKey
        {
            get
            {
                return _privateKey;
            }
            set
            {
                _privateKey = value;
                NotifyPropertyChanged();
            }
        }

        private string _publicKey;
        public string PublicKey
        {
            get
            {
                return _publicKey;
            }
            set
            {
                _publicKey = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand OK { get; private set; }
        public ICommand Generate { get; private set; }
        public ICommand Cancel { get; private set; }

        #endregion

        public UserViewModel(bool startup = false)
        {
            _startup = startup;

            Name = Settings.Instance.Name;
            RequireEncryption = Settings.Instance.RequireEncryption;
            PrivateKey = Settings.Instance.PrivateKey;
            PublicKey = Settings.Instance.PublicKey;

            OK = new DelegateCommand(p => DoOK(p));
            Generate = new DelegateCommand(p => DoGenerate());
            Cancel = new DelegateCommand(p => DoCancel(p));
        }

        private void DoOK(object window)
        {
            if (Name == "" || Name == "newuser")
            {
                MessageBox.Show("Please fill user name",
                    Settings.Title, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (!_startup)
            {
                bool needRestart = false;

                if (Settings.Instance.Name != Name)
                {
                    if (MessageBox.Show("Changing your name will change it in contact list of other people " +
                        "and will start a separate chat history for them. Are you sure?",
                        Settings.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }

                    needRestart = true;
                }

                if (Settings.Instance.PrivateKey != PrivateKey || Settings.Instance.PublicKey != PublicKey)
                {
                    if (MessageBox.Show("Changing your private and public keys will essencially create a new user. Are you sure?",
                        Settings.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }

                    needRestart = true;
                }

                if (needRestart)
                {
                    if (MessageBox.Show("These changes require a restart of the application. Continue?",
                        Settings.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }

                    Settings.Instance.RequireEncryption = RequireEncryption;
                    Settings.Instance.Name = Name;
                    Settings.Instance.PrivateKey = PrivateKey;
                    Settings.Instance.PublicKey = PublicKey;

                    Helpers.Restart();
                }

                Settings.Instance.RequireEncryption = RequireEncryption;
            }
            else
            {
                Settings.Instance.RequireEncryption = RequireEncryption;
                Settings.Instance.Name = Name;
                Settings.Instance.PrivateKey = PrivateKey;
                Settings.Instance.PublicKey = PublicKey;
            }

            ((Window)window).DialogResult = true;
            ((Window)window).Close();
        }

        private void DoCancel(object window)
        {
            ((Window)window).DialogResult = false;
            ((Window)window).Close();
        }

        private void DoGenerate()
        {
            string privateKey, publicKey;
            Helpers.GenerateKeyPair(out privateKey, out publicKey);
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }
    }
}