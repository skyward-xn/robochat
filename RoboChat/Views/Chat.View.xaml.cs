using RoboChat.Contracts;
using RoboChat.ViewModels;
using System.Windows;

namespace RoboChat
{
    public partial class ChatView : Window
    {
        public ChatView(IRosterModel model, Contact contact)
        {
            InitializeComponent();
            DataContext = new ChatViewModel(model, contact);
        }

        private void ChatViewElement_Drop(object sender, DragEventArgs e)
        {
            ((ChatViewModel)DataContext).Drop(e);
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void FlowDocumentScrollViewerElement_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
