using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RoboChat
{
    public class FindBehavior : Behavior<FlowDocumentScrollViewer>
    {
        public static readonly DependencyProperty ShouldFindProperty =
            DependencyProperty.Register("ShouldFind",
            typeof(bool),
            typeof(FindBehavior),
            new PropertyMetadata(false, ShouldFindPropertyChanged));

        public bool ShouldFind
        {
            get { return (bool)this.GetValue(ShouldFindProperty); }
            set { this.SetValue(ShouldFindProperty, value); }
        }

        private static void ShouldFindPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as FindBehavior;
            if (behavior == null)
                return;

            var flowDocumentScrollViewer = behavior.AssociatedObject as FlowDocumentScrollViewer;
            if (flowDocumentScrollViewer == null)
                return;

            if (e.NewValue is bool && (bool)e.NewValue)
                flowDocumentScrollViewer.Find();
        }
    }
}