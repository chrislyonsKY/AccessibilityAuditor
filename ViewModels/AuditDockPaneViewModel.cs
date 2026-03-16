using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Rules;
using AccessibilityAuditor.Services.Fixes;
using AccessibilityAuditor.Services.LLM;
using AccessibilityAuditor.Services.PortalInspector;
using AccessibilityAuditor.Windows;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the main Accessibility Auditor dockpane shell.
    /// Manages target selection, scan execution, and child ViewModels.
    /// </summary>
    internal sealed partial class AuditDockPaneViewModel : DockPane
    {
        private const string DockPaneId = "AccessibilityAuditor_AuditDockPane";

        private readonly RuleRegistry _ruleRegistry;
        private readonly ScanPipeline _pipeline;
        private readonly AuditSettings _settings;
        private CancellationTokenSource? _cts;

        // v2: Fix engine and LLM services
        private static readonly HttpClient _sharedHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        private readonly CredentialProvider _credentialProvider;
        private readonly FixEngine _fixEngine;
        private readonly ILLMProvider[] _llmProviders;

        // Track modeless ProWindow instances to prevent duplicates
        private readonly Dictionary<string, FindingDetailWindow> _openDetailWindows = new();
        private ColorSimulationWindow? _colorSimWindow;

        /// <summary>
        /// Show the dockpane.
        /// </summary>
        internal static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }

        public AuditDockPaneViewModel()
        {
            _settings = AuditSettings.Load();
            _ruleRegistry = CreateRuleRegistry();
            _pipeline = new ScanPipeline(_ruleRegistry);

            // v2: Initialize fix engine and LLM services
            _credentialProvider = new CredentialProvider();
            _llmProviders = new ILLMProvider[]
            {
                new AnthropicProvider(_sharedHttpClient, _credentialProvider),
                new OpenAIProvider(_sharedHttpClient, _credentialProvider)
            };

            var deterministicStrategy = new DeterministicFixStrategy();
            var activeProvider = _llmProviders.FirstOrDefault(p =>
                _credentialProvider.IsConfigured(p.ProviderType));
            var llmStrategy = activeProvider is not null
                ? new LLMFixStrategy(activeProvider, _credentialProvider)
                : null;
            _fixEngine = new FixEngine(deterministicStrategy, llmStrategy);

            DashboardVM = new DashboardViewModel();
            PerceivableVM = new PrincipleViewModel(WcagPrinciple.Perceivable, OpenFindingDetail, ApplyFixAsync) { FixEngine = _fixEngine };
            OperableVM = new PrincipleViewModel(WcagPrinciple.Operable, OpenFindingDetail, ApplyFixAsync) { FixEngine = _fixEngine };
            UnderstandableVM = new PrincipleViewModel(WcagPrinciple.Understandable, OpenFindingDetail, ApplyFixAsync) { FixEngine = _fixEngine };
            RobustVM = new PrincipleViewModel(WcagPrinciple.Robust, OpenFindingDetail, ApplyFixAsync) { FixEngine = _fixEngine };

            // v2: AI Settings ViewModel
            LLMSettingsVM = new LLMSettingsViewModel(_credentialProvider, _llmProviders);

            // Restore last target selection
            _selectedTargetIndex = _settings.LastTargetIndex;

            _pipeline.PhaseStarted += phase => StatusText = phase + "...";
        }

        /// <summary>Gets the AI Settings tab ViewModel.</summary>
        public LLMSettingsViewModel LLMSettingsVM { get; }

        /// <summary>Applies a fix for a single finding via the fix engine.</summary>
        private async Task ApplyFixAsync(Finding finding)
        {
            var strategy = _fixEngine.ResolveStrategy(finding);
            if (strategy is null)
            {
                StatusText = "No fix available for this finding.";
                return;
            }

            StatusText = "Applying fix...";
            var result = await strategy.ApplyFixAsync(finding, CancellationToken.None);
            StatusText = result.Summary;
        }

        #region Properties

        /// <summary>Gets the Dashboard tab ViewModel.</summary>
        public DashboardViewModel DashboardVM { get; }

        /// <summary>Gets the Perceivable tab ViewModel.</summary>
        public PrincipleViewModel PerceivableVM { get; }

        /// <summary>Gets the Operable tab ViewModel.</summary>
        public PrincipleViewModel OperableVM { get; }

        /// <summary>Gets the Understandable tab ViewModel.</summary>
        public PrincipleViewModel UnderstandableVM { get; }

        /// <summary>Gets the Robust tab ViewModel.</summary>
        public PrincipleViewModel RobustVM { get; }

        private bool _isScanning;
        /// <summary>Gets or sets whether a scan is in progress.</summary>
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                SetProperty(ref _isScanning, value);
                RunAuditCommand.NotifyCanExecuteChanged();
            }
        }

        private string _statusText = "Ready";
        /// <summary>Gets or sets the current status message.</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private int _selectedTargetIndex;
        /// <summary>Gets or sets the selected target type index (0=Active Map, 1=Active Layout).</summary>
        public int SelectedTargetIndex
        {
            get => _selectedTargetIndex;
            set
            {
                if (SetProperty(ref _selectedTargetIndex, value))
                {
                    _settings.LastTargetIndex = value;
                    _settings.Save();
                }
            }
        }

        private int _selectedTabIndex;
        /// <summary>Gets or sets the selected tab index.</summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        private string _portalItemId = string.Empty;
        /// <summary>Gets or sets the portal item ID for web map/ExB scanning.</summary>
        public string PortalItemId
        {
            get => _portalItemId;
            set => SetProperty(ref _portalItemId, value);
        }

        private bool _hasPalette;
        /// <summary>Gets or sets whether palette data is available for color simulation.</summary>
        public bool HasPalette
        {
            get => _hasPalette;
            set => SetProperty(ref _hasPalette, value);
        }

        private ObservableCollection<PortalItemSummary> _portalItems = new();
        /// <summary>Gets the list of portal items available for selection.</summary>
        public ObservableCollection<PortalItemSummary> PortalItems
        {
            get => _portalItems;
            set => SetProperty(ref _portalItems, value);
        }

        private PortalItemSummary? _selectedPortalItem;
        /// <summary>Gets or sets the currently selected portal item from the browse list.</summary>
        public PortalItemSummary? SelectedPortalItem
        {
            get => _selectedPortalItem;
            set
            {
                if (SetProperty(ref _selectedPortalItem, value) && value is not null)
                {
                    PortalItemId = value.ItemId;
                }
            }
        }

        private bool _isLoadingPortalItems;
        /// <summary>Gets or sets whether portal items are currently being loaded.</summary>
        public bool IsLoadingPortalItems
        {
            get => _isLoadingPortalItems;
            set
            {
                SetProperty(ref _isLoadingPortalItems, value);
                BrowsePortalCommand.NotifyCanExecuteChanged();
            }
        }

        #endregion

        #region Commands

        private CommunityToolkit.Mvvm.Input.RelayCommand? _runAuditCommand;
        /// <summary>Gets the Run Audit command.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand RunAuditCommand => _runAuditCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            async () => await RunAuditAsync(),
            () => !IsScanning);

        private CommunityToolkit.Mvvm.Input.RelayCommand? _cancelCommand;
        /// <summary>Gets the Cancel command.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand CancelCommand => _cancelCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            () => _cts?.Cancel(),
            () => IsScanning);

        private CommunityToolkit.Mvvm.Input.RelayCommand? _openColorSimulationCommand;
        /// <summary>Gets the Open Color Simulation command.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand OpenColorSimulationCommand => _openColorSimulationCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            OpenColorSimulation,
            () => HasPalette);

        private CommunityToolkit.Mvvm.Input.RelayCommand? _openSettingsCommand;
        /// <summary>Gets the Open Settings command.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand OpenSettingsCommand => _openSettingsCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            OpenSettings);

        private CommunityToolkit.Mvvm.Input.RelayCommand? _openAboutCommand;
        /// <summary>Gets the Open About command.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand OpenAboutCommand => _openAboutCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            OpenAbout);

        private CommunityToolkit.Mvvm.Input.RelayCommand<Finding>? _openFindingDetailCommand;
        /// <summary>Gets the Open Finding Detail command.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand<Finding> OpenFindingDetailCommand => _openFindingDetailCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand<Finding>(
            OpenFindingDetail,
            f => f is not null);

        private CommunityToolkit.Mvvm.Input.RelayCommand? _browsePortalCommand;
        /// <summary>Gets the Browse Portal command that fetches web maps from the active portal.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand BrowsePortalCommand => _browsePortalCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            async () => await BrowsePortalAsync(),
            () => !IsLoadingPortalItems && !IsScanning);

        private CommunityToolkit.Mvvm.Input.RelayCommand? _fixAllAutoCommand;
        /// <summary>Gets the Fix All Auto command that applies all deterministic fixes.</summary>
        public CommunityToolkit.Mvvm.Input.RelayCommand FixAllAutoCommand => _fixAllAutoCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(
            async () => await FixAllAutoAsync(),
            () => !IsScanning);

        #endregion

        private async Task FixAllAutoAsync()
        {
            StatusText = "Applying deterministic fixes...";
            var allFindings = DashboardVM.HasResults
                ? PerceivableVM.Findings
                    .Concat(OperableVM.Findings)
                    .Concat(UnderstandableVM.Findings)
                    .Concat(RobustVM.Findings)
                    .ToList()
                : new List<Finding>();

            if (allFindings.Count == 0)
            {
                StatusText = "No findings to fix. Run an audit first.";
                return;
            }

            var results = await _fixEngine.ApplyAllDeterministicAsync(allFindings, CancellationToken.None);

            // Stamp results onto individual findings so rows update visually
            foreach (var (finding, result) in results)
            {
                finding.IsFixed = result.Status == FixStatus.Applied;
                finding.FixStatusText = result.Status switch
                {
                    FixStatus.Applied => "Fixed",
                    FixStatus.Suggested when result.SuggestedContent is not null =>
                        $"Suggested: {result.SuggestedContent}",
                    FixStatus.Suggested => "Suggestion available",
                    _ => null // Don't show anything for failures in bulk mode
                };
            }

            int applied = results.Count(r => r.Result.Status is FixStatus.Applied or FixStatus.Suggested);
            int failed = results.Count(r => r.Result.Status == FixStatus.Failed);
            StatusText = $"Fix All: {applied} fixes applied, {failed} failed, " +
                         $"{allFindings.Count - applied - failed} skipped.";
        }

        private async Task RunAuditAsync()
        {
            IsScanning = true;
            StatusText = "Starting audit...";
            _cts = new CancellationTokenSource();

            // Pass current settings to the pipeline for this scan
            _pipeline.Settings = _settings;

            try
            {
                AuditResult result;

                switch (SelectedTargetIndex)
                {
                    case 1:
                        result = await _pipeline.ScanActiveLayoutAsync(_cts.Token);
                        break;
                    case 2:
                        result = await RunPortalScanAsync(_cts.Token);
                        break;
                    default:
                        result = await _pipeline.ScanActiveMapAsync(_cts.Token);
                        break;
                }

                // Update palette availability
                HasPalette = _pipeline.LastPalette is not null && _pipeline.LastPalette.Count > 0;
                OpenColorSimulationCommand.NotifyCanExecuteChanged();

                // Distribute findings to ViewModels
                DashboardVM.UpdateFromResult(result);
                PerceivableVM.UpdateFindings(result.Findings
                    .Where(f => f.Criterion?.Principle == WcagPrinciple.Perceivable).ToList());
                OperableVM.UpdateFindings(result.Findings
                    .Where(f => f.Criterion?.Principle == WcagPrinciple.Operable).ToList());
                UnderstandableVM.UpdateFindings(result.Findings
                    .Where(f => f.Criterion?.Principle == WcagPrinciple.Understandable).ToList());
                RobustVM.UpdateFindings(result.Findings
                    .Where(f => f.Criterion?.Principle == WcagPrinciple.Robust).ToList());

                StatusText = $"Audit complete � {result.Findings.Count} findings in {result.Elapsed.TotalSeconds:F1}s";
            }
            catch (OperationCanceledException)
            {
                StatusText = "Audit cancelled.";
            }
            catch (Exception ex)
            {
                StatusText = $"Audit failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Audit error: {ex}");
            }
            finally
            {
                IsScanning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task<AuditResult> RunPortalScanAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(PortalItemId))
            {
                StatusText = "Enter a portal item ID to scan.";
                throw new OperationCanceledException();
            }

            // Get portal URL and token from Pro's active portal connection
            string portalUrl = "https://www.arcgis.com";
            string? token = null;

            try
            {
                var portal = ArcGIS.Desktop.Core.ArcGISPortalManager.Current.GetActivePortal();
                if (portal is not null)
                {
                    portalUrl = portal.PortalUri.ToString().TrimEnd('/');
                    token = portal.GetToken();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not get portal token: {ex.Message}");
            }

            return await _pipeline.ScanPortalItemAsync(portalUrl, PortalItemId.Trim(), token, cancellationToken);
        }

        private async Task BrowsePortalAsync()
        {
            IsLoadingPortalItems = true;
            StatusText = "Loading portal items...";

            try
            {
                string portalUrl = "https://www.arcgis.com";
                string? token = null;

                try
                {
                    var portal = ArcGIS.Desktop.Core.ArcGISPortalManager.Current.GetActivePortal();
                    if (portal is not null)
                    {
                        portalUrl = portal.PortalUri.ToString().TrimEnd('/');
                        token = portal.GetToken();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not get portal token: {ex.Message}");
                }

                var checker = new PortalItemChecker();
                var items = await checker.SearchItemsAsync(portalUrl, token);

                PortalItems.Clear();
                foreach (var item in items)
                {
                    PortalItems.Add(item);
                }

                StatusText = items.Count > 0
                    ? $"Found {items.Count} web maps in portal"
                    : "No web maps found in portal";
            }
            catch (Exception ex)
            {
                StatusText = $"Browse failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Portal browse error: {ex}");
            }
            finally
            {
                IsLoadingPortalItems = false;
            }
        }

        private void OpenColorSimulation()
        {
            var palette = _pipeline.LastPalette;
            if (palette is null || palette.Count == 0) return;

            // Bring existing window to front if already open
            if (_colorSimWindow is not null && _colorSimWindow.IsVisible)
            {
                _colorSimWindow.Activate();
                return;
            }

            // Prefer labeled palette (symbology-aware) over flat palette
            ColorSimulationViewModel vm;
            var labeledPalette = _pipeline.LastLabeledPalette;
            if (labeledPalette is not null && labeledPalette.Count > 0)
            {
                var bg = _pipeline.LastBackgroundColor ?? new Core.Models.ColorInfo(255, 255, 255);
                vm = new ColorSimulationViewModel(labeledPalette, bg, DashboardVM.TargetName);
            }
            else
            {
                vm = new ColorSimulationViewModel(palette, DashboardVM.TargetName);
            }

            _colorSimWindow = new ColorSimulationWindow
            {
                DataContext = vm,
                Owner = FrameworkApplication.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            _colorSimWindow.Closed += (s, e) => _colorSimWindow = null;
            _colorSimWindow.Show();
        }

        private void OpenSettings()
        {
            var vm = new SettingsViewModel(_settings);
            var window = new SettingsWindow
            {
                DataContext = vm,
                Owner = FrameworkApplication.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (window.ShowDialog() == true)
            {
                vm.ApplyAndSave();
            }
        }

        private void OpenAbout()
        {
            var window = new AboutWindow
            {
                DataContext = new AboutViewModel(),
                Owner = FrameworkApplication.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
        }

        private void OpenFindingDetail(Finding? finding)
        {
            if (finding is null) return;

            string key = $"{finding.RuleId}_{finding.Element}";

            // Bring existing window to front if already open for this finding
            if (_openDetailWindows.TryGetValue(key, out var existing) && existing.IsVisible)
            {
                existing.Activate();
                return;
            }

            var vm = new FindingDetailViewModel(finding);
            var window = new FindingDetailWindow
            {
                DataContext = vm,
                Owner = FrameworkApplication.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.Closed += (s, e) => _openDetailWindows.Remove(key);
            _openDetailWindows[key] = window;
            window.Show();
        }

        private static RuleRegistry CreateRuleRegistry()
        {
            var registry = new RuleRegistry();
            // Phase 1 rules (CIM-based)
            registry.Register(new TextContrastRule());
            registry.Register(new AltTextRule());
            registry.Register(new PageTitleRule());
            registry.Register(new NonTextContrastRule());
            registry.Register(new UseOfColorRule());
            // Phase 3 rules (Portal-based)
            registry.Register(new LanguageOfPageRule());
            registry.Register(new PopupAccessibilityRule());
            registry.Register(new PortalItemDescriptionRule());
            // Phase 4 rules (expanded coverage)
            registry.Register(new ReadingOrderRule());
            registry.Register(new ImagesOfTextRule());
            registry.Register(new HeadingsAndLabelsRule());
            registry.Register(new NameRoleValueRule());
            return registry;
        }
    }

    /// <summary>
    /// Button that toggles the Accessibility Auditor dockpane.
    /// </summary>
    internal sealed class AuditDockPane_ShowButton : ArcGIS.Desktop.Framework.Contracts.Button
    {
        protected override void OnClick()
        {
            AuditDockPaneViewModel.Show();
        }
    }
}
