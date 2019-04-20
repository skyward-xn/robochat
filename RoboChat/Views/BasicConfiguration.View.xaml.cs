using RoboChat.ViewModels;
using System.Windows;

namespace RoboChat
{
    public partial class BasicConfigurationView : Window
    {
        public BasicConfigurationView(bool startup = false)
        {
            InitializeComponent();
            this.DataContext = new BasicConfigurationViewModel(startup);
        }
    }
}