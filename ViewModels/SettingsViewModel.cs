using CommunityToolkit.Mvvm.ComponentModel;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings ProWindow (modal).
    /// Manages rule enable/disable and threshold overrides.
    /// </summary>
    internal sealed class SettingsViewModel : ObservableObject
    {
        private readonly AuditSettings _settings;

        /// <summary>
        /// Initializes a new <see cref="SettingsViewModel"/> backed by the given settings.
        /// </summary>
        public SettingsViewModel(AuditSettings settings)
        {
            _settings = settings;
            _includePassFindings = settings.IncludePassFindings;
            _checkColorBlindSafety = settings.CheckColorBlindSafety;
            _contrastWarningMargin = settings.ContrastWarningMargin;
        }

        /// <summary>Parameterless constructor that loads from disk.</summary>
        public SettingsViewModel() : this(AuditSettings.Load()) { }

        private bool _includePassFindings;
        /// <summary>Gets or sets whether pass findings are included in results.</summary>
        public bool IncludePassFindings
        {
            get => _includePassFindings;
            set => SetProperty(ref _includePassFindings, value);
        }

        private bool _checkColorBlindSafety;
        /// <summary>Gets or sets whether colorblind safety is evaluated.</summary>
        public bool CheckColorBlindSafety
        {
            get => _checkColorBlindSafety;
            set => SetProperty(ref _checkColorBlindSafety, value);
        }

        private double _contrastWarningMargin;
        /// <summary>Gets or sets the contrast ratio warning margin above the threshold.</summary>
        public double ContrastWarningMargin
        {
            get => _contrastWarningMargin;
            set => SetProperty(ref _contrastWarningMargin, value);
        }

        /// <summary>
        /// Applies changes back to the settings model and saves to disk.
        /// </summary>
        public void ApplyAndSave()
        {
            _settings.IncludePassFindings = IncludePassFindings;
            _settings.CheckColorBlindSafety = CheckColorBlindSafety;
            _settings.ContrastWarningMargin = ContrastWarningMargin;
            _settings.Save();
        }
    }
}
