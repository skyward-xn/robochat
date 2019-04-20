using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RoboChat.Contracts
{
    [Serializable]
    public class Contact : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Contact()
        {
            Settings.Instance.PropertyChanged += Instance_PropertyChanged;
        }

        public void Dispose()
        {
            Settings.Instance.PropertyChanged -= Instance_PropertyChanged;
        }

        #region Properties

        private string _id;
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                if (value != _id)
                {
                    _id = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _version;
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (value != _version)
                {
                    _version = value;
                    NotifyPropertyChanged();
                }
                NotifyPropertyChanged("Tooltip");
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged();
                }
                NotifyPropertyChanged("Avatar");
                NotifyPropertyChanged("ShortName");
                NotifyPropertyChanged("Group");
            }
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }
            set
            {
                if (value != _isOnline)
                {
                    _isOnline = value;
                    NotifyPropertyChanged();
                }
                NotifyPropertyChanged("IsOnlineImage");
                NotifyPropertyChanged("Tooltip");
                NotifyPropertyChanged("Visibility");
                NotifyPropertyChanged("EnableSend");
            }
        }

        private bool _isConnectionLost;
        public bool IsConnectionLost
        {
            get
            {
                return _isConnectionLost;
            }
            set
            {
                if (value != _isConnectionLost)
                {
                    _isConnectionLost = value;
                    NotifyPropertyChanged();
                }
                NotifyPropertyChanged("EnableSend");
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
                if (value != _requireEncryption)
                {
                    _requireEncryption = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("EncryptionImage");
                }
            }
        }

        private bool _sendEncrypted;
        public bool SendEncrypted
        {
            get
            {
                return _sendEncrypted;
            }
            set
            {
                if (value != _sendEncrypted)
                {
                    _sendEncrypted = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("EncryptionImage");
                }
            }
        }

        public string EncryptionImage
        {
            get
            {
                if (RequireEncryption)
                    return "/Resources;component/images/lock_disabled.png";

                if (SendEncrypted)
                    return "/Resources;component/images/lock.png";

                return "/Resources;component/images/unlock.png";
            }
        }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get
            {
                return _isFavorite;
            }
            set
            {
                if (value != _isFavorite)
                {
                    _isFavorite = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private DateTime _lastOnline;
        public DateTime LastOnline
        {
            get
            {
                return _lastOnline;
            }
            set
            {
                if (value != _lastOnline)
                {
                    _lastOnline = value;
                    NotifyPropertyChanged();
                }
                NotifyPropertyChanged("Tooltip");
            }
        }

        #endregion

        #region Computed

        public string ShortName
        {
            get
            {
                return Name.Split('@').First();
            }
        }

        public string Group
        {
            get
            {
                return Name.Split('@').Last();
            }
        }

        public string IsOnlineImage
        {
            get
            {
                return "/Resources;component/images/" + (IsOnline ? "green.png" : "red.png");
            }
        }

        public bool EnableSend
        {
            get
            {
                return !IsConnectionLost && (IsOnline || Settings.Instance.SendOfflineMessages);
            }
        }

        public string Tooltip
        {
            get
            {
                string tooltip = "";

                if (IsOnline)
                {
                    tooltip += "Status: Online";
                }
                else
                {
                    tooltip += "Status: Offline";

                    if (LastOnline != DateTime.MinValue)
                        tooltip += Environment.NewLine + string.Format("Last online: {0}", LastOnline.ToLocalTime());
                }

                tooltip += Environment.NewLine + string.Format("Version: {0}", Version);

                return tooltip;
            }
        }

        public string Avatar
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                    return "";

                return string.Format("/Resources;component/letters/{0}.png", Name[0]);
            }
        }

        public Visibility Visibility
        {
            get
            {
                return (Settings.Instance.ShowOffline || IsOnline) && ID != Settings.Instance.PublicKey ?
                    Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Handlers

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShowOffline":
                    NotifyPropertyChanged("Visibility");
                    break;

                case "SendOfflineMessages":
                    NotifyPropertyChanged("EnableSend");
                    break;
            }
        }

        #endregion
    }
}