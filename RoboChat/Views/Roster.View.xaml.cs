using RoboChat.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;

namespace RoboChat
{
    public partial class RosterView : Window
    {
        public RosterView()
        {
            InitializeComponent();
            Left = SystemParameters.WorkArea.Right - Width;
            Top = SystemParameters.WorkArea.Bottom - Height;

            var viewModel = new RosterViewModel();
            DataContext = viewModel;
            viewModel.Show += ViewModel_Show;
            viewModel.Toggle += ViewModel_Toggle;

            Closed += RosterView_Closed;
        }

        private void RosterView_Closed(object sender, EventArgs e)
        {
            ((RosterViewModel)DataContext).Dispose();
        }

        private void ViewModel_Show(object sender, EventArgs e)
        {
            DoShow();
        }

        private void ViewModel_Toggle(object sender, EventArgs e)
        {
            if (Visibility == Visibility.Visible)
                Hide();
            else
                DoShow();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        // По неясной причине Binding к событию SourceInitialized не работает,
        // поэтому пойдем другим путем
        private void ContactsViewElement_SourceInitialized(object sender, EventArgs e)
        {
            ((RosterViewModel)DataContext).SourceInitialized.Execute(this);
        }

        private void DoShow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}