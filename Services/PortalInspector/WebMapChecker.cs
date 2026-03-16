using System;
using System.Collections.Generic;
using System.Text.Json;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.PortalInspector
{
    /// <summary>
    /// Parses web map JSON to extract operational layers, renderer colors, and pop-up configurations
    /// for accessibility analysis. Runs on a background thread — no QueuedTask required.
    /// </summary>
    public sealed class WebMapChecker
    {
        /// <summary>
        /// Parses a web map JSON document and populates the audit context with
        /// layer, renderer, and popup data.
        /// </summary>
        /// <param name="webMapDoc">The web map JSON document (from item data endpoint).</param>
        /// <param name="context">The audit context to populate.</param>
        public void ParseWebMap(JsonDocument webMapDoc, AuditContext context)
        {
            if (webMapDoc is null) throw new ArgumentNullException(nameof(webMapDoc));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var root = webMapDoc.RootElement;

            // Extract operational layers
            if (root.TryGetProperty("operationalLayers", out var layers) && layers.ValueKind == JsonValueKind.Array)
            {
                foreach (var layer in layers.EnumerateArray())
                {
                    ParseLayer(layer, context);
                }
            }
        }

        private static void ParseLayer(JsonElement layer, AuditContext context)
        {
            var layerInfo = new WebMapLayerInfo
            {
                LayerId = GetString(layer, "id") ?? string.Empty,
                Title = GetString(layer, "title")
            };

            // Parse renderer for color extraction
            if (layer.TryGetProperty("layerDefinition", out var layerDef) &&
                layerDef.TryGetProperty("drawingInfo", out var drawingInfo) &&
                drawingInfo.TryGetProperty("renderer", out var renderer))
            {
                layerInfo.RendererType = GetString(renderer, "type");
                ExtractRendererColors(renderer, layerInfo);

                // Build a RendererInfo for rule consumption
                var rendererInfo = BuildRendererInfo(renderer, layerInfo.Title ?? layerInfo.LayerId);
                if (rendererInfo is not null)
                    context.Renderers.Add(rendererInfo);
            }

            // Parse pop-up info
            if (layer.TryGetProperty("popupInfo", out var popup))
            {
                var popupInfo = ParsePopup(popup, layerInfo.Title ?? layerInfo.LayerId);
                context.Popups.Add(popupInfo);
                layerInfo.HasPopup = true;
            }

            context.WebMapLayers.Add(layerInfo);
        }

        private static PopupInfo ParsePopup(JsonElement popup, string layerName)
        {
            var info = new PopupInfo
            {
                LayerName = layerName,
                TitleTemplate = GetString(popup, "title")
            };

            // Check for custom description HTML
            if (popup.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
            {
                info.DescriptionHtml = desc.GetString();
                info.HasCustomHtml = !string.IsNullOrWhiteSpace(info.DescriptionHtml);
            }

            // Extract field infos for label checking
            if (popup.TryGetProperty("fieldInfos", out var fieldInfos) && fieldInfos.ValueKind == JsonValueKind.Array)
            {
                foreach (var field in fieldInfos.EnumerateArray())
                {
                    bool visible = true;
                    if (field.TryGetProperty("visible", out var vis))
                        visible = vis.GetBoolean();
                    if (!visible) continue;

                    var fieldName = GetString(field, "fieldName");
                    var label = GetString(field, "label");

                    if (fieldName is not null) info.FieldNames.Add(fieldName);
                    if (label is not null) info.FieldLabels.Add(label);
                }
            }

            return info;
        }

        private static void ExtractRendererColors(JsonElement renderer, WebMapLayerInfo layerInfo)
        {
            string? type = GetString(renderer, "type");

            switch (type)
            {
                case "simple":
                    if (renderer.TryGetProperty("symbol", out var simpleSymbol))
                    {
                        var color = ExtractSymbolColor(simpleSymbol);
                        if (color is not null) layerInfo.RendererColors.Add(color);
                    }
                    break;

                case "uniqueValue":
                    if (renderer.TryGetProperty("uniqueValueInfos", out var uvis) && uvis.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var uvi in uvis.EnumerateArray())
                        {
                            if (uvi.TryGetProperty("symbol", out var uvSymbol))
                            {
                                var color = ExtractSymbolColor(uvSymbol);
                                if (color is not null) layerInfo.RendererColors.Add(color);
                            }
                        }
                    }
                    break;

                case "classBreaks":
                    if (renderer.TryGetProperty("classBreakInfos", out var cbis) && cbis.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var cbi in cbis.EnumerateArray())
                        {
                            if (cbi.TryGetProperty("symbol", out var cbSymbol))
                            {
                                var color = ExtractSymbolColor(cbSymbol);
                                if (color is not null) layerInfo.RendererColors.Add(color);
                            }
                        }
                    }
                    break;
            }
        }

        private static RendererInfo? BuildRendererInfo(JsonElement renderer, string layerName)
        {
            string? type = GetString(renderer, "type");
            if (type is null) return null;

            var info = new RendererInfo
            {
                LayerName = layerName,
                RendererType = type
            };

            switch (type)
            {
                case "uniqueValue":
                    if (renderer.TryGetProperty("uniqueValueInfos", out var uvis) && uvis.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var uvi in uvis.EnumerateArray())
                        {
                            var sci = new SymbolClassInfo
                            {
                                Label = GetString(uvi, "label") ?? GetString(uvi, "value") ?? string.Empty,
                                FillColor = uvi.TryGetProperty("symbol", out var s) ? ExtractSymbolColor(s) : null
                            };
                            info.SymbolClasses.Add(sci);
                        }
                    }
                    break;

                case "classBreaks":
                    if (renderer.TryGetProperty("classBreakInfos", out var cbis) && cbis.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var cbi in cbis.EnumerateArray())
                        {
                            var sci = new SymbolClassInfo
                            {
                                Label = GetString(cbi, "label") ?? string.Empty,
                                FillColor = cbi.TryGetProperty("symbol", out var s) ? ExtractSymbolColor(s) : null
                            };
                            info.SymbolClasses.Add(sci);
                        }
                    }
                    break;

                case "simple":
                    if (renderer.TryGetProperty("symbol", out var sym))
                    {
                        info.SymbolClasses.Add(new SymbolClassInfo
                        {
                            Label = "Default",
                            FillColor = ExtractSymbolColor(sym)
                        });
                    }
                    break;
            }

            return info;
        }

        /// <summary>
        /// Extracts a <see cref="ColorInfo"/> from a web map symbol JSON element.
        /// Portal renderer colors are stored as [R, G, B, A] arrays with values 0-255.
        /// </summary>
        internal static ColorInfo? ExtractSymbolColor(JsonElement symbol)
        {
            if (symbol.TryGetProperty("color", out var colorArr) && colorArr.ValueKind == JsonValueKind.Array)
            {
                var enumerator = colorArr.EnumerateArray();
                var components = new List<byte>(4);
                foreach (var c in enumerator)
                {
                    if (c.TryGetInt32(out int val))
                        components.Add((byte)Math.Clamp(val, 0, 255));
                }

                if (components.Count >= 3)
                {
                    byte a = components.Count >= 4 ? components[3] : (byte)255;
                    return new ColorInfo(components[0], components[1], components[2], a);
                }
            }

            return null;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }
    }
}
