using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AccessibilityAuditor.Views
{
    public partial class AIChatView : UserControl
    {
        public AIChatView()
        {
            InitializeComponent();
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var vm = DataContext as ViewModels.AIChatViewModel;
                if (vm?.SendCommand.CanExecute(null) == true)
                {
                    vm.SendCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
