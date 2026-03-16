using System.Collections.Generic;
using System.Text.Json;
using AccessibilityAuditor.Core.Models;
using ArcGIS.Core.CIM;

namespace AccessibilityAuditor.Orchestration
{
    /// <summary>
    /// Runtime state container for an in-progress audit. Populated by the scan pipeline
    /// and consumed by rules during evaluation.
    /// </summary>
    public sealed class AuditContext
    {
        /// <summary>Gets or sets the audit target being scanned.</summary>
        public AuditTarget Target { get; set; } = new();

        /// <summary>Gets the label class information extracted from CIM inspection.</summary>
        public List<LabelClassInfo> LabelClasses { get; } = new();

        /// <summary>Gets the renderer information extracted from CIM inspection.</summary>
        public List<RendererInfo> Renderers { get; } = new();

        /// <summary>Gets the layout element information extracted from CIM inspection.</summary>
        public List<LayoutElementInfo> LayoutElements { get; } = new();

        /// <summary>Gets or sets the map title, if available.</summary>
        public string? MapTitle { get; set; }

        /// <summary>Gets or sets the layout title, if available.</summary>
        public string? LayoutTitle { get; set; }

        /// <summary>Gets or sets the map description, if available.</summary>
        public string? MapDescription { get; set; }

        /// <summary>Gets or sets the estimated default background color of the map.</summary>
        public ColorInfo DefaultBackgroundColor { get; set; } = new(255, 255, 255);

        /// <summary>
        /// Gets or sets whether the map background is heterogeneous (e.g., imagery basemap),
        /// meaning per-label contrast checking may be unreliable.
        /// When true, label contrast findings should consider manual review.
        /// </summary>
        public bool IsHeterogeneousBackground { get; set; }

        /// <summary>Gets or sets the active audit settings for this scan.</summary>
        public AuditSettings? Settings { get; set; }

        // ?? Portal-specific data (Phase 3) ??

        /// <summary>Gets or sets the portal item metadata, if scanning a portal item.</summary>
        public PortalItemInfo? PortalItem { get; set; }

        /// <summary>Gets the pop-up configurations extracted from a web map.</summary>
        public List<PopupInfo> Popups { get; } = new();

        /// <summary>Gets the web map layer metadata extracted from web map JSON.</summary>
        public List<WebMapLayerInfo> WebMapLayers { get; } = new();

        /// <summary>Gets or sets the Experience Builder app configuration info, if applicable.</summary>
        public ExperienceBuilderInfo? ExperienceBuilder { get; set; }
    }

    /// <summary>
    /// Information about a label class extracted from the CIM.
    /// </summary>
    public sealed class LabelClassInfo
    {
        /// <summary>Gets or sets the label class name.</summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>Gets or sets the owning layer name.</summary>
        public string LayerName { get; set; } = string.Empty;

        /// <summary>Gets or sets the foreground text color.</summary>
        public ColorInfo ForegroundColor { get; set; } = new(0, 0, 0);

        /// <summary>Gets or sets the estimated background color behind labels.</summary>
        public ColorInfo EstimatedBackgroundColor { get; set; } = new(255, 255, 255);

        /// <summary>Gets or sets the font size in points.</summary>
        public double FontSize { get; set; }

        /// <summary>Gets or sets the font family name.</summary>
        public string FontFamily { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether the font is bold.</summary>
        public bool IsBold { get; set; }

        /// <summary>Gets or sets the halo color, if a halo is present.</summary>
        public ColorInfo? HaloColor { get; set; }

        /// <summary>Gets or sets the halo size in points.</summary>
        public double? HaloSize { get; set; }

        /// <summary>Gets or sets a value indicating whether labeling is enabled.</summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// Information about a renderer extracted from the CIM.
    /// </summary>
    public sealed class RendererInfo
    {
        /// <summary>Gets or sets the owning layer name.</summary>
        public string LayerName { get; set; } = string.Empty;

        /// <summary>Gets or sets the renderer type name.</summary>
        public string RendererType { get; set; } = string.Empty;

        /// <summary>Gets the symbol class information for this renderer.</summary>
        public List<SymbolClassInfo> SymbolClasses { get; } = new();

        /// <summary>Gets or sets a value indicating whether the renderer uses shape or size variation (not just color).</summary>
        public bool UsesShapeOrSizeVariation { get; set; }
    }

    /// <summary>
    /// Information about a single symbol class in a renderer.
    /// </summary>
    public sealed class SymbolClassInfo
    {
        /// <summary>Gets or sets the class label/value.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Gets or sets the primary fill color.</summary>
        public ColorInfo? FillColor { get; set; }

        /// <summary>Gets or sets the primary stroke/outline color.</summary>
        public ColorInfo? StrokeColor { get; set; }

        /// <summary>Gets or sets the stroke width in points.</summary>
        public double StrokeWidth { get; set; }

        /// <summary>Gets or sets the symbol size in points (for marker symbols).</summary>
        public double SymbolSize { get; set; }

        /// <summary>Gets or sets the symbol shape name, if applicable.</summary>
        public string? ShapeName { get; set; }
    }

    /// <summary>
    /// Information about a layout element extracted from the CIM.
    /// </summary>
    public sealed class LayoutElementInfo
    {
        /// <summary>Gets or sets the element name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the element type (e.g., "TextElement", "MapFrame", "Legend").</summary>
        public string ElementType { get; set; } = string.Empty;

        /// <summary>Gets or sets the element description/alt text.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets the text content, for text elements.</summary>
        public string? TextContent { get; set; }

        /// <summary>Gets or sets the font size, for text elements.</summary>
        public double? FontSize { get; set; }

        /// <summary>Gets or sets the text color, for text elements.</summary>
        public ColorInfo? TextColor { get; set; }

        /// <summary>Gets or sets the background color behind this element.</summary>
        public ColorInfo? BackgroundColor { get; set; }

        /// <summary>Gets or sets a value indicating whether the text is bold.</summary>
        public bool IsBold { get; set; }

        /// <summary>Gets or sets the X position of the element anchor in layout page units.</summary>
        public double X { get; set; }

        /// <summary>Gets or sets the Y position of the element anchor in layout page units.</summary>
        public double Y { get; set; }

        /// <summary>Gets or sets the element width in layout page units.</summary>
        public double Width { get; set; }

        /// <summary>Gets or sets the element height in layout page units.</summary>
        public double Height { get; set; }

        /// <summary>Gets or sets the zero-based index of this element in the CIM element array (z-order).</summary>
        public int SortOrder { get; set; }

        /// <summary>Gets or sets a value indicating whether this is a picture/image element.</summary>
        public bool IsPictureElement { get; set; }
    }

    /// <summary>
    /// Metadata about a portal item (web map, web app, ExB app).
    /// </summary>
    public sealed class PortalItemInfo
    {
        /// <summary>Gets or sets the portal item ID.</summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>Gets or sets the item title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the item description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets the item snippet (short description).</summary>
        public string? Snippet { get; set; }

        /// <summary>Gets or sets the item tags.</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Gets or sets the item culture/language code (e.g., "en-us").</summary>
        public string? Culture { get; set; }

        /// <summary>Gets or sets the item type (e.g., "Web Map", "Web Mapping Application").</summary>
        public string? ItemType { get; set; }

        /// <summary>Gets or sets the access information (credits/attribution).</summary>
        public string? AccessInformation { get; set; }

        /// <summary>Gets or sets the portal URL this item came from.</summary>
        public string? PortalUrl { get; set; }
    }

    /// <summary>
    /// Information about a pop-up configuration in a web map layer.
    /// </summary>
    public sealed class PopupInfo
    {
        /// <summary>Gets or sets the layer name this pop-up belongs to.</summary>
        public string LayerName { get; set; } = string.Empty;

        /// <summary>Gets or sets the pop-up title template (may contain field placeholders).</summary>
        public string? TitleTemplate { get; set; }

        /// <summary>Gets or sets the pop-up description HTML.</summary>
        public string? DescriptionHtml { get; set; }

        /// <summary>Gets or sets whether the pop-up uses a custom HTML template.</summary>
        public bool HasCustomHtml { get; set; }

        /// <summary>Gets or sets the field names configured for display.</summary>
        public List<string> FieldNames { get; set; } = new();

        /// <summary>Gets or sets the field labels (may differ from field names).</summary>
        public List<string> FieldLabels { get; set; } = new();
    }

    /// <summary>
    /// Information about an operational layer in a web map.
    /// </summary>
    public sealed class WebMapLayerInfo
    {
        /// <summary>Gets or sets the layer ID.</summary>
        public string LayerId { get; set; } = string.Empty;

        /// <summary>Gets or sets the layer title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets whether the layer has pop-ups enabled.</summary>
        public bool HasPopup { get; set; }

        /// <summary>Gets the symbol colors extracted from the web map renderer.</summary>
        public List<ColorInfo> RendererColors { get; } = new();

        /// <summary>Gets or sets the renderer type (e.g., "simple", "uniqueValue", "classBreaks").</summary>
        public string? RendererType { get; set; }
    }

    /// <summary>
    /// Information about an Experience Builder application configuration.
    /// </summary>
    public sealed class ExperienceBuilderInfo
    {
        /// <summary>Gets or sets the app title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the app description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets the app's configured language.</summary>
        public string? Language { get; set; }

        /// <summary>Gets the widget configurations.</summary>
        public List<ExBWidgetInfo> Widgets { get; } = new();
    }

    /// <summary>
    /// Information about a single widget in an Experience Builder app.
    /// </summary>
    public sealed class ExBWidgetInfo
    {
        /// <summary>Gets or sets the widget ID.</summary>
        public string WidgetId { get; set; } = string.Empty;

        /// <summary>Gets or sets the widget type (e.g., "map", "search", "legend").</summary>
        public string? WidgetType { get; set; }

        /// <summary>Gets or sets the widget label/title.</summary>
        public string? Label { get; set; }

        /// <summary>Gets or sets whether the widget has an accessible label.</summary>
        public bool HasLabel { get; set; }
    }
}
