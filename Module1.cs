using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace AccessibilityAuditor
{
    /// <summary>
    /// AccessibilityAuditor module entry point. Provides the singleton module instance.
    /// </summary>
    internal class Module1 : Module
    {
        private static Module1? _this;

        /// <summary>
        /// Retrieves the singleton instance of this module.
        /// </summary>
        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("AccessibilityAuditor_Module");

        /// <summary>
        /// Called by the Framework when ArcGIS Pro is closing.
        /// </summary>
        /// <returns><c>false</c> to prevent Pro from closing; otherwise <c>true</c>.</returns>
        protected override bool CanUnload()
        {
            return true;
        }
    }
}
