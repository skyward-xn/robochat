using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RoboChat
{
    public class SelectionBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty BindableSelectionStartProperty =
            DependencyProperty.Register(
            "BindableSelectionStart",
            typeof(int),
            typeof(SelectionBehavior),
            new PropertyMetadata(TextBoxSelectionStartChanged));

        public static readonly DependencyProperty BindableSelectionLengthProperty =
            DependencyProperty.Register(
            "BindableSelectionLength",
            typeof(int),
            typeof(SelectionBehavior),
            new PropertyMetadata(TextBoxSelectionLengthChanged));

        private bool changeFromUI;

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.SelectionChanged += this.OnSelectionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.AssociatedObject.SelectionChanged -= this.OnSelectionChanged;
        }

        public int BindableSelectionStart
        {
            get { return (int)this.GetValue(BindableSelectionStartProperty); }
            set { this.SetValue(BindableSelectionStartProperty, value); }
        }

        public int BindableSelectionLength
        {
            get { return (int)this.GetValue(BindableSelectionLengthProperty); }
            set { this.SetValue(BindableSelectionLengthProperty, value); }
        }

        private static void TextBoxSelectionStartChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var behavior = dependencyObject as SelectionBehavior;

            if (!behavior.changeFromUI)
                behavior.AssociatedObject.SelectionStart = (int)args.NewValue;
            else
                behavior.changeFromUI = false;
        }

        private static void TextBoxSelectionLengthChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var behavior = dependencyObject as SelectionBehavior;

            if (!behavior.changeFromUI)
                behavior.AssociatedObject.SelectionLength = (int)args.NewValue;
            else
                behavior.changeFromUI = false;
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (this.BindableSelectionStart != this.AssociatedObject.SelectionStart)
            {
                this.changeFromUI = true;
                this.BindableSelectionStart = this.AssociatedObject.SelectionStart;
            }

            if (this.BindableSelectionLength != this.AssociatedObject.SelectionLength)
            {
                this.changeFromUI = true;
                this.BindableSelectionLength = this.AssociatedObject.SelectionLength;
            }
        }
    }
}