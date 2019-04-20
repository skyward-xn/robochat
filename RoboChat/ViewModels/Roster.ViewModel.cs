using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using RoboChat.Win32;
using System.Windows;
using System.Windows.Forms;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RoboChat.Model;
using RoboChat.Contracts;
using RoboChat.DataStructures;
using RoboChat.CustomEventArgs;

namespace RoboChat.ViewModels
{
    class RosterViewModel : INotifyPropertyChanged, IDisposable
    {
        private IRosterModel _model;

        public event EventHandler<EventArgs> Show;
        public event EventHandler<EventArgs> Toggle;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Binding

        public Settings SettingsInstance
        {
            get { return Settings.Instance; }
        }

        public ObservableCollection<Contact> Contacts
        {
            get
            {
                return _model.Contacts;
            }
        }

        private Contact _selectedContact;
        public Contact SelectedContact
        {
            get
            {
                return _selectedContact;
            }
            set
            {
                if (value != _selectedContact)
                {
                    _selectedContact = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isOnline = true;
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
            }
        }

        public string LagImage
        {
            get
            {
                if (Lag == 0)
                    return "/Resources;component/images/signal0.png";
                else if (Lag < 500)
                    return "/Resources;component/images/signal4.png";
                else if (Lag < 1000)
                    return "/Resources;component/images/signal3.png";
                else if (Lag < 2000)
                    return "/Resources;component/images/signal2.png";

                return "/Resources;component/images/signal1.png";
            }
        }

        public string LagText
        {
            get
            {
                if (Lag == 0)
                    return "No connection";

                return Lag.ToString() + " ms";
            }
        }

        private int _lag = 0;
        public int Lag
        {
            get
            {
                return _lag;
            }
            set
            {
                if (value != _lag)
                {
                    _lag = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("LagImage");
                    NotifyPropertyChanged("LagText");
                }
            }
        }

        public ICommand DblClick { get; private set; }
        public ICommand OpenFonts { get; private set; }
        public ICommand OpenBasicConfiguration { get; private set; }
        public ICommand OpenUser { get; private set; }
        public ICommand Exit { get; private set; }
        public ICommand CheckUpdates { get; private set; }
        public ICommand OpenAbout { get; private set; }
        public ICommand Closed { get; private set; }
        public ICommand SourceInitialized { get; private set; }
        public ICommand ContactFavorite { get; private set; }
        public ICommand ContactUnFavorite { get; private set; }
        public ICommand ContactRemove { get; private set; }
        public ICommand ContactRemoveAndClear { get; private set; }
        public ICommand Hide { get; private set; }

        #endregion

        public RosterViewModel()
        {
            _model = new RosterModel();
            _model.MessageReceived += _model_MessageReceived;
            _model.StatusChanged += _model_StatusChanged;
            _model.StatisticsUpdated += _model_StatisticsUpdated;

            DblClick = new DelegateCommand(p => DoDblClick());
            OpenFonts = new DelegateCommand(p => DoOpenFonts());
            OpenBasicConfiguration = new DelegateCommand(p => DoOpenBasicConfiguration());
            OpenUser = new DelegateCommand(p => DoOpenUser());
            Exit = new DelegateCommand(p => App.Current.Shutdown());
            CheckUpdates = new DelegateCommand(p => DoCheckUpdates());
            OpenAbout = new DelegateCommand(p => DoOpenAbout());
            Closed = new DelegateCommand(p => DoClosed(p));
            SourceInitialized = new DelegateCommand(p => DoSourceInitialized(p));
            ContactFavorite = new DelegateCommand(p => DoContactFavorite());
            ContactUnFavorite = new DelegateCommand(p => DoContactUnFavorite());
            ContactRemove = new DelegateCommand(p => DoContactRemove());
            ContactRemoveAndClear = new DelegateCommand(p => DoContactRemoveAndClear());
            Hide = new DelegateCommand(p => DoHide(p));

            // Запускаем потоки            
            _model.Start();

            AppTray.Show += AppTray_Show;
            AppTray.Toggle += AppTray_Toggle;
            AppTray.About += AppTray_About;
            AppTray.CheckUpdates += AppTray_CheckUpdates;
            AppTray.OpenWindow += AppTray_OpenWindow;
        }

        public void Dispose()
        {
            AppTray.Show -= AppTray_Show;
            AppTray.Toggle -= AppTray_Toggle;
            AppTray.About -= AppTray_About;
            AppTray.CheckUpdates -= AppTray_CheckUpdates;
            AppTray.OpenWindow -= AppTray_OpenWindow;

            if (_model != null)
                _model.Dispose();
        }

        private void AppTray_OpenWindow(object sender, AppTray.WindowEventArgs e)
        {
            GetWindow(e.WindowTitle);
        }

        private void AppTray_CheckUpdates(object sender, EventArgs e)
        {
            DoCheckUpdates();
        }

        private void AppTray_About(object sender, EventArgs e)
        {
            DoOpenAbout();
        }

        private void AppTray_Toggle(object sender, EventArgs e)
        {
            DoToggle();
        }

        private void AppTray_Show(object sender, EventArgs e)
        {
            DoShow();
        }

        private void _model_MessageReceived(object sender, MessageEventArgs e)
        {
            App.Current.Dispatcher.Invoke(new Action<MessageEventArgs>(ProcessMessage), e);
        }

        private void _model_StatusChanged(object sender, StatusEventArgs e)
        {
            IsOnline = e.IsOnline;

            if (!e.IsOnline)
                Lag = 0;
        }

        private void _model_StatisticsUpdated(object sender, StatisticsEventArgs e)
        {
            Lag = (int)e.Lag;
        }

        private void ProcessMessage(MessageEventArgs e)
        {
            if (!e.Message.IsNotification)
            {
                var window = GetWindow(e.Message.Address, true, true, false);
                if (window != null)
                {
                    // Отобразим сообщение в окне чата
                    ((ChatViewModel)window.DataContext).DisplayMessage(e.Message);

                    if (!window.IsActive && e.Activate)
                    {
                        // Мигание окна
                        window.FlashWindow(false);

                        // Сообщение в трее
                        AppTray.Message(ToolTipIcon.None, e.Message.Text, window.Title, tag: e.Message.Address);
                    }
                }
            }
            else
            {
                var window = GetWindow(e.Message.To, false, false, false);
                if (window != null)
                {
                    // Отобразим подтверждение о получении в окне чата
                    ((ChatViewModel)window.DataContext).DisplayMessage(e.Message);
                }
            }
        }

        private void DoDblClick()
        {
            GetWindow(SelectedContact.ID);
        }

        private void DoClosed(object window)
        {
            _model.MessageReceived -= _model_MessageReceived;
            _model.StatusChanged -= _model_StatusChanged;
            _model.StatisticsUpdated -= _model_StatisticsUpdated;

            Settings.SaveWindowSizeAndPosition((Window)window);
        }

        private void DoSourceInitialized(object window)
        {
            Settings.LoadWindowSizeAndPosition((Window)window);
        }

        private void DoOpenFonts()
        {
            new FontsView().ShowDialog();
        }

        private void DoOpenBasicConfiguration()
        {
            new BasicConfigurationView().ShowDialog();
        }

        private void DoOpenUser()
        {
            new UserView().ShowDialog();
        }

        private void DoContactFavorite()
        {
            SelectedContact.IsFavorite = true;
        }

        private void DoContactUnFavorite()
        {
            SelectedContact.IsFavorite = false;
        }

        private void DoContactRemove()
        {
            if (System.Windows.MessageBox.Show("\"" + SelectedContact.Name + "\" will be removed from contacts",
                Settings.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            {
                return;
            }

            Contacts.Remove(SelectedContact);
        }

        private void DoContactRemoveAndClear()
        {
            if (System.Windows.MessageBox.Show("\"" + SelectedContact.Name + "\" will be removed from contacts. " + Environment.NewLine +
                "All chat history with this contact will be deleted.", Settings.Title,
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            {
                return;
            }

            _model.ClearHistory(SelectedContact.Name);
            Contacts.Remove(SelectedContact);
        }

        private void DoHide(object window)
        {
            ((Window)window).Hide();
        }

        public void DoShow()
        {
            Show?.Invoke(this, EventArgs.Empty);
        }

        public void DoCheckUpdates()
        {
            DoShow();

            AppUpdater.UpdateCheck(new UpdaterState()
            {
                IsDialog = true
            });
        }

        public void DoToggle()
        {
            Toggle?.Invoke(this, EventArgs.Empty);
        }

        private void DoOpenAbout()
        {
            new AboutView().ShowDialog();
        }

        public ChatView GetWindow(string address, bool create = true, bool createMinimized = false, bool activate = true)
        {
            var window = App.Current.Windows
                .OfType<ChatView>()
                .FirstOrDefault(p => p.Tag is string && (string)p.Tag == address);

            if (create && window == null)
            {
                var contact = Contacts.FirstOrDefault(p => p.ID == address);
                if (contact == null)
                    return null;

                window = new ChatView(_model, contact);

                if (createMinimized)
                    window.WindowState = WindowState.Minimized;

                window.Show();
            }

            if (activate && window != null)
            {
                window.WindowState = WindowState.Normal;
                window.Activate();
            }

            return window;
        }
    }
}