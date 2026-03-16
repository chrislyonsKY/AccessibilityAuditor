using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.Fixes;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for a WCAG principle tab showing its findings.
    /// Shared across Perceivable, Operable, Understandable, and Robust tabs.
    /// </summary>
    internal sealed class PrincipleViewModel : ObservableObject
    {
        private readonly Action<Finding>? _openDetailAction;
        private readonly Func<Finding, Task>? _applyFixAction;

        /// <summary>
        /// Initializes a new <see cref="PrincipleViewModel"/> for the specified principle.
        /// </summary>
        /// <param name="principle">The WCAG principle this tab represents.</param>
        /// <param name="openDetailAction">Callback to open a finding detail window.</param>
        /// <param name="applyFixAction">Callback to apply a fix for a finding.</param>
        public PrincipleViewModel(
            WcagPrinciple principle,
            Action<Finding>? openDetailAction = null,
            Func<Finding, Task>? applyFixAction = null)
        {
            Principle = principle;
            PrincipleLabel = principle.ToString();
            _openDetailAction = openDetailAction;
            _applyFixAction = applyFixAction;
            OpenFindingDetailCommand = new RelayCommand<Finding>(
                f => _openDetailAction?.Invoke(f!),
                f => f is not null);
            ApplyFixCommand = new RelayCommand<Finding>(
                async f => await ApplyFixAsync(f!),
                f => f is not null && CanFix(f));
            DismissFixMessageCommand = new RelayCommand(DismissFixMessage);
        }

        /// <summary>Gets the WCAG principle.</summary>
        public WcagPrinciple Principle { get; }

        /// <summary>Gets the principle display label.</summary>
        public string PrincipleLabel { get; }

        /// <summary>Gets the command to open a finding detail window.</summary>
        public RelayCommand<Finding> OpenFindingDetailCommand { get; }

        /// <summary>Gets the command to apply a fix for a finding.</summary>
        public RelayCommand<Finding> ApplyFixCommand { get; }

        /// <summary>Gets the command to dismiss the fix result panel.</summary>
        public RelayCommand DismissFixMessageCommand { get; }

        private FixEngine? _fixEngine;
        /// <summary>Sets the fix engine used for resolving and applying fixes.</summary>
        public FixEngine? FixEngine
        {
            get => _fixEngine;
            set => SetProperty(ref _fixEngine, value);
        }

        private string? _lastFixMessage;
        /// <summary>Status message from the last fix attempt.</summary>
        public string? LastFixMessage
        {
            get => _lastFixMessage;
            set => SetProperty(ref _lastFixMessage, value);
        }

        private FixResult? _lastFixResult;
        /// <summary>Full result from the last fix attempt.</summary>
        public FixResult? LastFixResult
        {
            get => _lastFixResult;
            set
            {
                if (SetProperty(ref _lastFixResult, value))
                {
                    OnPropertyChanged(nameof(HasSuggestion));
                    OnPropertyChanged(nameof(FixStatusLabel));
                    OnPropertyChanged(nameof(SuggestedColorHex));
                    OnPropertyChanged(nameof(OriginalColorHex));
                    OnPropertyChanged(nameof(HasColorSwatch));
                }
            }
        }

        private Finding? _lastFixFinding;

        /// <summary>True when the last fix returned a suggestion for review.</summary>
        public bool HasSuggestion => _lastFixResult?.Status == FixStatus.Suggested;

        /// <summary>Status label for the fix result badge.</summary>
        public string FixStatusLabel => _lastFixResult?.Status switch
        {
            FixStatus.Applied => "Applied",
            FixStatus.Suggested => "Review Suggestion",
            FixStatus.Failed => "Failed",
            _ => ""
        };

        /// <summary>Suggested hex color from a contrast fix, if any.</summary>
        public string? SuggestedColorHex =>
            _lastFixResult?.SuggestedContent is { } s && s.StartsWith("#") && s.Length == 7 ? s : null;

        /// <summary>Original foreground hex from the finding that was fixed.</summary>
        public string? OriginalColorHex => _lastFixFinding?.ForegroundColor?.Hex;

        /// <summary>True when before/after color swatches should be shown.</summary>
        public bool HasColorSwatch => SuggestedColorHex is not null && OriginalColorHex is not null;

        private ObservableCollection<Finding> _findings = new();
        /// <summary>Gets or sets the findings for this principle.</summary>
        public ObservableCollection<Finding> Findings
        {
            get => _findings;
            set => SetProperty(ref _findings, value);
        }

        private Finding? _selectedFinding;
        /// <summary>Gets or sets the currently selected finding.</summary>
        public Finding? SelectedFinding
        {
            get => _selectedFinding;
            set => SetProperty(ref _selectedFinding, value);
        }

        private int _failCount;
        /// <summary>Gets or sets the Fail count for this principle.</summary>
        public int FailCount
        {
            get => _failCount;
            set => SetProperty(ref _failCount, value);
        }

        private int _warningCount;
        /// <summary>Gets or sets the Warning count for this principle.</summary>
        public int WarningCount
        {
            get => _warningCount;
            set => SetProperty(ref _warningCount, value);
        }

        private int _passCount;
        /// <summary>Gets or sets the Pass count for this principle.</summary>
        public int PassCount
        {
            get => _passCount;
            set => SetProperty(ref _passCount, value);
        }

        private int _manualReviewCount;
        /// <summary>Gets or sets the ManualReview count for this principle.</summary>
        public int ManualReviewCount
        {
            get => _manualReviewCount;
            set => SetProperty(ref _manualReviewCount, value);
        }

        /// <summary>
        /// Updates findings from the latest audit result.
        /// </summary>
        /// <param name="findings">Findings for this principle.</param>
        public void UpdateFindings(IReadOnlyList<Finding> findings)
        {
            Findings = new ObservableCollection<Finding>(findings);
            FailCount = findings.Count(f => f.Severity == FindingSeverity.Fail);
            WarningCount = findings.Count(f => f.Severity == FindingSeverity.Warning);
            PassCount = findings.Count(f => f.Severity == FindingSeverity.Pass);
            ManualReviewCount = findings.Count(f => f.Severity == FindingSeverity.ManualReview);
            SelectedFinding = null;
            LastFixMessage = null;
        }

        /// <summary>Returns true if the fix engine has a strategy for this finding.</summary>
        public bool CanFix(Finding? finding) =>
            finding is not null && _fixEngine?.ResolveStrategy(finding) is not null;

        private async Task ApplyFixAsync(Finding finding)
        {
            if (_fixEngine is null) return;

            var strategy = _fixEngine.ResolveStrategy(finding);
            if (strategy is null) return;

            try
            {
                _lastFixFinding = finding;
                LastFixMessage = "Applying fix...";
                LastFixResult = null;

                var result = await strategy.ApplyFixAsync(finding, CancellationToken.None);
                LastFixResult = result;
                LastFixMessage = result.Summary;

                // Stamp the result onto the finding so the row updates visually
                finding.IsFixed = result.Status == FixStatus.Applied;
                finding.FixStatusText = result.Status switch
                {
                    FixStatus.Applied => "Fixed",
                    FixStatus.Suggested when result.SuggestedContent is not null =>
                        $"Suggested: {result.SuggestedContent}",
                    FixStatus.Suggested => "Suggestion available",
                    _ => $"Failed: {result.Summary}"
                };
            }
            catch (Exception ex)
            {
                LastFixResult = new FixResult(FixStatus.Failed, $"Fix error: {ex.Message}");
                LastFixMessage = $"Fix error: {ex.Message}";
                Debug.WriteLine($"Fix error: {ex}");
            }
        }

        private void DismissFixMessage()
        {
            LastFixMessage = null;
            LastFixResult = null;
            _lastFixFinding = null;
        }
    }
}
