using RoboChat.ViewModels;
using System.Windows;

namespace RoboChat
{
    public partial class FontsView : Window
    {
        public FontsView()
        {
            InitializeComponent();
            this.DataContext = new FontsViewModel();
        }
    }
}
