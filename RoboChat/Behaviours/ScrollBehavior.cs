using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace RoboChat
{
    public class ScrollBehavior : Behavior<FlowDocumentScrollViewer>
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register("AutoScroll",
            typeof(bool),
            typeof(ScrollBehavior),
            new PropertyMetadata(false, AutoScrollPropertyChanged));

        public static readonly DependencyProperty IsScrollTopProperty =
            DependencyProperty.Register("IsScrollTop",
            typeof(bool),
            typeof(ScrollBehavior));

        private bool _eventAdded = false;

        /// <summary>
        /// Автоматически прокрутить в самый конец
        /// </summary>
        public bool AutoScroll
        {
            get { return (bool)this.GetValue(AutoScrollProperty); }
            set { this.SetValue(AutoScrollProperty, value); }
        }

        public bool IsScrollTop
        {
            get { return (bool)this.GetValue(IsScrollTopProperty); }
            set { this.SetValue(IsScrollTopProperty, value); }
        }

        private static void AutoScrollPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                return;

            var behavior = sender as ScrollBehavior;
            if (behavior == null)
                return;

            var flowDocumentScrollViewer = behavior.AssociatedObject as FlowDocumentScrollViewer;
            if (flowDocumentScrollViewer == null)
                return;

            // Найдем внутри данного контрола собственно ScrollViewer
            DependencyObject obj = flowDocumentScrollViewer;
            do
            {
                if (VisualTreeHelper.GetChildrenCount(obj) > 0)
                    obj = VisualTreeHelper.GetChild(obj as Visual, 0);
                else
                    return;
            }
            while (!(obj is ScrollViewer));
            var scrollViewer = obj as ScrollViewer;

            // Нашли ScrollViewer
            if (scrollViewer != null)
            {
                // Привяжем событие на прокрутку, если еще не привязали
                if (!behavior._eventAdded)
                {
                    scrollViewer.ScrollChanged += behavior.scrollViewer_ScrollChanged;
                    behavior._eventAdded = true;
                }

                // Прокрутим вниз
                scrollViewer.ScrollToBottom();

                // Полоса прокрутки не появилась, значит мы в самом верху
                if (scrollViewer.ExtentHeight <= scrollViewer.ViewportHeight)
                    behavior.IsScrollTop = true;
            }
        }

        private void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Если мы в самом верху и увеличился размер области контента, 
            // значит мы добавили новые сообщения вверху.
            // Тогда прокрутим к нашему предыдущему месту.
            if (IsScrollTop && e.ExtentHeightChange > 0)
                ((ScrollViewer)sender).ScrollToVerticalOffset(e.ExtentHeightChange);

            // Если мы в самом верху за счет прокрутки вручную, а не за счат добавленного контента
            // Или если полоса прокрутки не появилась
            IsScrollTop = e.VerticalOffset == 0 && e.ExtentHeightChange == 0 || e.ExtentHeight <= e.ViewportHeight;
        }
    }
}