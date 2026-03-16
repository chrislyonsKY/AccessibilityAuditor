using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the About ProWindow (modal).
    /// </summary>
    internal sealed class AboutViewModel : ObservableObject
    {
        /// <summary>Gets the add-in display name.</summary>
        public string ProductName => "Accessibility Auditor";

        /// <summary>Gets the version string.</summary>
        public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        /// <summary>Gets the description text.</summary>
        public string Description =>
            "Semi-automated WCAG 2.1 Level AA compliance auditing for ArcGIS Pro map products. " +
            "Scans maps, layouts, web maps, and Experience Builder apps for accessibility issues " +
            "and provides actionable remediation guidance.";

        /// <summary>Gets the author text.</summary>
        public string Author => "Chris Lyons";

        /// <summary>Gets the organization text.</summary>
        public string Organization => "Open Source — github.com/arcgis-pro-accessibility";

        /// <summary>Gets the copyright text.</summary>
        public string Copyright => $"Copyright \u00A9 2026 Chris Lyons. Apache 2.0 License.";

        /// <summary>Gets the framework info.</summary>
        public string Framework => "ArcGIS Pro SDK 3.6 | .NET 8";

        /// <summary>Gets the WCAG standard reference.</summary>
        public string Standard => "WCAG 2.1 Level AA (W3C Recommendation)";

        /// <summary>Gets the rule count.</summary>
        public string RuleCount => "13 rules covering 12 WCAG criteria across all 4 principles";
    }
}
