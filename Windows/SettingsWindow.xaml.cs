using ArcGIS.Desktop.Framework.Controls;

namespace AccessibilityAuditor.Windows
{
    /// <summary>
    /// Modal ProWindow for rule configuration and settings.
    /// </summary>
    public partial class SettingsWindow : ProWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
