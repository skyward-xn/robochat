using RoboChat.ViewModels;
using System.Windows;

namespace RoboChat
{
    public partial class UserView : Window
    {
        public UserView(bool startup = false)
        {
            InitializeComponent();
            this.DataContext = new UserViewModel(startup);
        }
    }
}