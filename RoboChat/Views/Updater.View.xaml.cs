using NAppUpdate.Framework;
using NAppUpdate.Framework.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace RoboChat
{
    public partial class UpdaterView : Window
    {
        private const string UPDATE_MARKER_FILE = "version.txt";
        private bool _isUpdating;

        public UpdaterView()
        {
            InitializeComponent();

            if (!File.Exists(Path.Combine(Settings.Directory, UPDATE_MARKER_FILE)))
                DoUpdate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isUpdating)
                e.Cancel = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DoUpdate();
        }

        private async void DoUpdate()
        {
            _isUpdating = true;
            ButtonsBlock.Visibility = Visibility.Collapsed;
            SeparatorBlock.Visibility = Visibility.Collapsed;
            UpdateProgressBar.Visibility = Visibility.Visible;
            MessageTextBlock.Text = "Updating...";
            UpdateManager.Instance.ReportProgress += Instance_ReportProgress;

            await Task.Factory.StartNew(() =>
           {
               UpdateManager.Instance.PrepareUpdates();
           });

            UpdateManager.Instance.ReportProgress -= Instance_ReportProgress;
            UpdateManager.Instance.ApplyUpdates(true); // перезагрузит приложение
        }

        void Instance_ReportProgress(UpdateProgressInfo currentStatus)
        {
            UpdateProgressBar.Dispatcher.Invoke((Action)(() =>
        {
            UpdateProgressBar.Value = currentStatus.Percentage;
        }));
        }
    }
}