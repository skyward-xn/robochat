using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RoboChat.Behaviors
{
    public class PrintBehavior : Behavior<FlowDocumentScrollViewer>
    {
        public static readonly DependencyProperty ShouldPrintProperty =
            DependencyProperty.Register("ShouldPrint",
            typeof(bool),
            typeof(PrintBehavior),
            new PropertyMetadata(false, ShouldPrintPropertyChanged));

        public bool ShouldPrint
        {
            get { return (bool)this.GetValue(ShouldPrintProperty); }
            set { this.SetValue(ShouldPrintProperty, value); }
        }

        private static void ShouldPrintPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as PrintBehavior;
            if (behavior == null)
                return;

            var flowDocumentScrollViewer = behavior.AssociatedObject as FlowDocumentScrollViewer;
            if (flowDocumentScrollViewer == null)
                return;

            if (e.NewValue is bool && (bool)e.NewValue)
                flowDocumentScrollViewer.Print();
        }
    }
}