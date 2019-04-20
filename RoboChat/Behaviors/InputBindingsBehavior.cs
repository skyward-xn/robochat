using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace RoboChat.Behaviors
{
    public class InputBindingsBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewKeyDown += InputBindingsBehavior_PreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PreviewKeyDown -= InputBindingsBehavior_PreviewKeyDown;
        }

        private void InputBindingsBehavior_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var uielement = sender as UIElement;
            if (uielement == null)
                return;

            var foundBinding = uielement.InputBindings
                .OfType<KeyBinding>()
                .FirstOrDefault(kb => kb.Key == e.Key && kb.Modifiers == e.KeyboardDevice.Modifiers);

            if (foundBinding != null)
            {
                e.Handled = true;
                if (foundBinding.Command.CanExecute(foundBinding.CommandParameter))
                {
                    foundBinding.Command.Execute(foundBinding.CommandParameter);
                }
            }
        }
    }
}
