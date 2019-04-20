using System.Windows;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Interactivity;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;

namespace RoboChat
{
    public class MessagesBehavior : Behavior<TableRowGroup>
    {
        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.Register("Messages",
            typeof(ObservableCollection<TextElement>),
            typeof(MessagesBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, MessagesChanged));

        public ObservableCollection<TextElement> Messages
        {
            get { return (ObservableCollection<TextElement>)GetValue(MessagesProperty); }
            set { SetValue(MessagesProperty, value); }
        }

        private static void MessagesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as MessagesBehavior;
            if (behavior == null)
                return;

            if (e.OldValue != null)
            {
                ((INotifyCollectionChanged)e.OldValue).CollectionChanged -= behavior.CollectionChanged;
                behavior.AssociatedObject.Rows.Clear();
            }

            if (e.NewValue != null)
            {
                ((INotifyCollectionChanged)e.NewValue).CollectionChanged += behavior.CollectionChanged;
                foreach (var row in ((IEnumerable)e.NewValue).OfType<TableRow>())
                    behavior.AssociatedObject.Rows.Add(row);
            }
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                    AssociatedObject.Rows.Insert(e.NewStartingIndex + i, (TableRow)e.NewItems[i]);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var row in e.OldItems.OfType<TableRow>())
                    AssociatedObject.Rows.Remove(row);
            }
        }
    }
}