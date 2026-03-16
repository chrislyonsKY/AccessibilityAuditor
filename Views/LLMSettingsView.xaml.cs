using System.Windows;
using System.Windows.Controls;
using AccessibilityAuditor.ViewModels;

namespace AccessibilityAuditor.Views
{
    /// <summary>
    /// Code-behind for LLMSettingsView.
    /// Only responsibility: pass PasswordBox value to ViewModel on Save
    /// (PasswordBox does not support data binding by WPF design).
    /// </summary>
    public partial class LLMSettingsView : UserControl
    {
        public LLMSettingsView()
        {
            InitializeComponent();
        }

        private void SaveKey_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LLMSettingsViewModel vm)
            {
                vm.SaveKey(ApiKeyBox.Password);
                ApiKeyBox.Clear();
            }
        }
    }
}
