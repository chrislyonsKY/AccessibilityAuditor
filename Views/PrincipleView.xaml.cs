using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.ViewModels;

namespace AccessibilityAuditor.Views
{
    /// <summary>
    /// Reusable view for a WCAG principle tab showing findings.
    /// </summary>
    public partial class PrincipleView : UserControl
    {
        public PrincipleView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Routes ListBoxItem double-click to the ViewModel's OpenFindingDetailCommand.
        /// </summary>
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is PrincipleViewModel vm
                && sender is ListBoxItem item
                && item.DataContext is Finding finding)
            {
                if (vm.OpenFindingDetailCommand.CanExecute(finding))
                {
                    vm.OpenFindingDetailCommand.Execute(finding);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Routes ListBoxItem Enter/Space key to the ViewModel's OpenFindingDetailCommand.
        /// </summary>
        private void ListBoxItem_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Space)
                && DataContext is PrincipleViewModel vm
                && sender is ListBoxItem item
                && item.DataContext is Finding finding)
            {
                if (vm.OpenFindingDetailCommand.CanExecute(finding))
                {
                    vm.OpenFindingDetailCommand.Execute(finding);
                    e.Handled = true;
                }
            }
        }
    }
}
