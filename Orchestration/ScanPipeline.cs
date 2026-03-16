using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Layouts;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Services.CimInspector;
using AccessibilityAuditor.Services.PortalInspector;

namespace AccessibilityAuditor.Orchestration
{
    /// <summary>
    /// Ordered execution pipeline: CIM walk ? rule evaluation ? scoring.
    /// Coordinates the full scan lifecycle.
    /// </summary>
    public sealed class ScanPipeline
    {
        private readonly AuditOrchestrator _orchestrator;
        private readonly CimWalker _cimWalker;
        private readonly LayoutElementAnalyzer _layoutAnalyzer;
        private readonly BackgroundColorEstimator _backgroundEstimator;
        private readonly PortalItemChecker _portalItemChecker;
        private readonly WebMapChecker _webMapChecker;
        private readonly ExperienceBuilderChecker _exbChecker;

        /// <summary>
        /// Initializes a new <see cref="ScanPipeline"/>.
        /// </summary>
        /// <param name="ruleRegistry">The registered compliance rules.</param>
        public ScanPipeline(RuleRegistry ruleRegistry)
        {
            if (ruleRegistry is null) throw new ArgumentNullException(nameof(ruleRegistry));
            _orchestrator = new AuditOrchestrator(ruleRegistry);
            _cimWalker = new CimWalker();
            _layoutAnalyzer = new LayoutElementAnalyzer();
            _backgroundEstimator = new BackgroundColorEstimator();
            _portalItemChecker = new PortalItemChecker();
            _webMapChecker = new WebMapChecker();
            _exbChecker = new ExperienceBuilderChecker();
        }

        /// <summary>
        /// Occurs when a rule begins execution. Forwarded from the orchestrator.
        /// </summary>
        public event Action<string>? RuleStarted
        {
            add => _orchestrator.RuleStarted += value;
            remove => _orchestrator.RuleStarted -= value;
        }

        /// <summary>
        /// Occurs when a rule completes execution. Forwarded from the orchestrator.
        /// </summary>
        public event Action<Core.Rules.RuleResult>? RuleCompleted
        {
            add => _orchestrator.RuleCompleted += value;
            remove => _orchestrator.RuleCompleted -= value;
        }

        /// <summary>
        /// Occurs when a pipeline phase starts (e.g., "CIM Inspection", "Rule Evaluation").
        /// </summary>
        public event Action<string>? PhaseStarted;

        /// <summary>
        /// Gets the map palette collected during the last scan.
        /// Available after <see cref="ScanActiveMapAsync"/> or <see cref="ScanActiveLayoutAsync"/> completes.
        /// </summary>
        public IReadOnlyList<ColorInfo>? LastPalette { get; private set; }

        /// <summary>
        /// Gets the labeled palette collected during the last scan, with layer/category context.
        /// Available after <see cref="ScanActiveMapAsync"/> or <see cref="ScanActiveLayoutAsync"/> completes.
        /// </summary>
        public IReadOnlyList<LabeledColor>? LastLabeledPalette { get; private set; }

        /// <summary>
        /// Gets the default background color from the last scan.
        /// </summary>
        public ColorInfo? LastBackgroundColor { get; private set; }

        /// <summary>
        /// Gets or sets the active audit settings. When set, settings are passed
        /// to the <see cref="AuditContext"/> and used to filter results.
        /// </summary>
        public AuditSettings? Settings { get; set; }

        /// <summary>
        /// Runs the full scan pipeline against the active map.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The audit result.</returns>
        public async Task<AuditResult> ScanActiveMapAsync(CancellationToken cancellationToken = default)
        {
            var context = new AuditContext
            {
                Target = new AuditTarget
                {
                    TargetType = AuditTargetType.Map,
                    Name = "Active Map"
                },
                Settings = Settings
            };

            IReadOnlyList<ColorInfo>? palette = null;
            IReadOnlyList<LabeledColor>? labeledPalette = null;

            // Phase 1: CIM Inspection (must run on MCT)
            PhaseStarted?.Invoke("CIM Inspection");
            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView?.Map is null) return;

                context.Target.Name = mapView.Map.Name;

                // Estimate background color before walking layers
                context.DefaultBackgroundColor = _backgroundEstimator.EstimateMapBackground(mapView.Map);
                context.IsHeterogeneousBackground = _backgroundEstimator.IsHeterogeneousBackground(mapView.Map);

                _cimWalker.WalkMap(mapView.Map, context);

                // Collect palettes for simulation window
                palette = _backgroundEstimator.CollectMapPalette(mapView.Map);
                labeledPalette = _backgroundEstimator.CollectLabeledPalette(mapView.Map);
            }).ConfigureAwait(false);

            LastPalette = palette;
            LastLabeledPalette = labeledPalette;
            LastBackgroundColor = context.DefaultBackgroundColor;
            cancellationToken.ThrowIfCancellationRequested();

            // Phase 2: Rule Evaluation
            PhaseStarted?.Invoke("Rule Evaluation");
            return await _orchestrator.RunAuditAsync(context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs the full scan pipeline against the active layout.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The audit result.</returns>
        public async Task<AuditResult> ScanActiveLayoutAsync(CancellationToken cancellationToken = default)
        {
            var context = new AuditContext
            {
                Target = new AuditTarget
                {
                    TargetType = AuditTargetType.Layout,
                    Name = "Active Layout"
                },
                Settings = Settings
            };

            IReadOnlyList<ColorInfo>? palette = null;
            IReadOnlyList<LabeledColor>? labeledPalette = null;

            // Phase 1: CIM Inspection (must run on MCT)
            PhaseStarted?.Invoke("CIM Inspection");
            await QueuedTask.Run(() =>
            {
                var layoutView = LayoutView.Active;
                if (layoutView?.Layout is null) return;

                context.Target.Name = layoutView.Layout.Name;
                _layoutAnalyzer.WalkLayout(layoutView.Layout, context);

                // Populate element positions from the managed API (Element.GetBounds())
                PopulateElementPositions(layoutView.Layout, context);

                // Also walk map frames' maps for label and renderer data
                var elements = layoutView.Layout.GetElements();
                foreach (var element in elements)
                {
                    if (element is MapFrame mapFrame && mapFrame.Map is not null)
                    {
                        context.DefaultBackgroundColor = _backgroundEstimator.EstimateMapBackground(mapFrame.Map);
                        context.IsHeterogeneousBackground = _backgroundEstimator.IsHeterogeneousBackground(mapFrame.Map);
                        _cimWalker.WalkMap(mapFrame.Map, context);

                        // Collect palettes from the first map frame
                        if (palette is null)
                        {
                            palette = _backgroundEstimator.CollectMapPalette(mapFrame.Map);
                            labeledPalette = _backgroundEstimator.CollectLabeledPalette(mapFrame.Map);
                        }
                    }
                }
            }).ConfigureAwait(false);

            LastPalette = palette;
            LastLabeledPalette = labeledPalette;
            LastBackgroundColor = context.DefaultBackgroundColor;
            cancellationToken.ThrowIfCancellationRequested();

            // Phase 2: Rule Evaluation
            PhaseStarted?.Invoke("Rule Evaluation");
            return await _orchestrator.RunAuditAsync(context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Populates X, Y, Width, and Height on each <see cref="LayoutElementInfo"/>
        /// by matching against managed <see cref="Element"/> objects via name.
        /// Must be called on the MCT.
        /// </summary>
        private static void PopulateElementPositions(Layout layout, AuditContext context)
        {
            try
            {
                var managedElements = layout.GetElements();
                // Build lookups by element name for matching
                var boundsLookup = new Dictionary<string, ArcGIS.Core.Geometry.Envelope>();
                var descLookup = new Dictionary<string, string>();

                foreach (var el in managedElements)
                {
                    if (string.IsNullOrEmpty(el.Name)) continue;

                    try
                    {
                        var bounds = el.GetBounds();
                        if (bounds is not null)
                        {
                            boundsLookup[el.Name] = bounds;
                        }
                    }
                    catch
                    {
                        // Skip elements that can't provide bounds
                    }

                    // Extract description via managed API as a fallback/supplement
                    // to CIM CustomProperties extraction in LayoutElementAnalyzer
                    try
                    {
                        var desc = el.GetCustomProperty("Description");
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            descLookup[el.Name] = desc;
                        }
                    }
                    catch
                    {
                        // GetCustomProperty may not be available on all element types
                    }
                }

                foreach (var info in context.LayoutElements)
                {
                    if (boundsLookup.TryGetValue(info.Name, out var envelope))
                    {
                        info.X = envelope.XMin;
                        info.Y = envelope.YMax; // YMax = top of element in page coords
                        info.Width = envelope.Width;
                        info.Height = envelope.Height;
                    }

                    // Fill description from managed API if CIM extraction didn't find one
                    if (string.IsNullOrWhiteSpace(info.Description)
                        && descLookup.TryGetValue(info.Name, out var desc))
                    {
                        info.Description = desc;
                    }
                }
            }
            catch
            {
                // Position and description extraction is best-effort
            }
        }

        /// <summary>
        /// Runs the full scan pipeline against a portal web map item.
        /// HTTP calls run on background threads — not inside QueuedTask.
        /// </summary>
        /// <param name="portalUrl">The portal base URL.</param>
        /// <param name="itemId">The portal item ID.</param>
        /// <param name="token">An optional authentication token.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The audit result.</returns>
        public async Task<AuditResult> ScanPortalItemAsync(
            string portalUrl, string itemId, string? token = null,
            CancellationToken cancellationToken = default)
        {
            var context = new AuditContext
            {
                Target = new AuditTarget
                {
                    TargetType = AuditTargetType.WebMap,
                    Name = "Portal Item"
                },
                Settings = Settings
            };

            // Phase 1: Fetch item metadata (HTTP — background thread)
            PhaseStarted?.Invoke("Fetching Item Metadata");
            try
            {
                context.PortalItem = await _portalItemChecker.GetItemInfoAsync(
                    portalUrl, itemId, token, cancellationToken).ConfigureAwait(false);

                context.Target.Name = context.PortalItem.Title ?? itemId;
                context.MapTitle = context.PortalItem.Title;
                context.MapDescription = context.PortalItem.Description;

                // Determine target type from item type
                if (context.PortalItem.ItemType?.Contains("Experience") == true)
                    context.Target.TargetType = AuditTargetType.ExperienceBuilder;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Portal item fetch failed: {ex}");
                context.Target.Name = itemId;
                // Continue — rules will get what data we have
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Phase 2: Fetch and parse item data (HTTP — background thread)
            PhaseStarted?.Invoke("Parsing Item Data");
            try
            {
                using var dataDoc = await _portalItemChecker.GetItemDataAsync(
                    portalUrl, itemId, token, cancellationToken).ConfigureAwait(false);

                if (context.Target.TargetType == AuditTargetType.ExperienceBuilder)
                {
                    _exbChecker.ParseExBConfig(dataDoc, context);
                }
                else
                {
                    _webMapChecker.ParseWebMap(dataDoc, context);

                    // Collect palette from web map renderer colors
                    var palette = new List<ColorInfo>();
                    foreach (var layer in context.WebMapLayers)
                    {
                        palette.AddRange(layer.RendererColors);
                    }
                    LastPalette = palette.Count > 0 ? palette : null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Portal item data parse failed: {ex}");
                // Continue — rule evaluation will work with whatever data we have
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Phase 3: Rule Evaluation
            PhaseStarted?.Invoke("Rule Evaluation");
            return await _orchestrator.RunAuditAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
