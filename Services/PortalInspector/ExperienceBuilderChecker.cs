using System;
using System.Collections.Generic;
using System.Text.Json;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.PortalInspector
{
    /// <summary>
    /// Parses Experience Builder application configuration for accessibility analysis.
    /// Checks app title, widget labels, language settings, and navigation consistency.
    /// Runs on a background thread — no QueuedTask required.
    /// </summary>
    public sealed class ExperienceBuilderChecker
    {
        /// <summary>
        /// Parses an Experience Builder config JSON document and populates the
        /// <see cref="ExperienceBuilderInfo"/> on the audit context.
        /// </summary>
        /// <param name="configDoc">The ExB config JSON (from item data endpoint).</param>
        /// <param name="context">The audit context to populate.</param>
        public void ParseExBConfig(JsonDocument configDoc, AuditContext context)
        {
            if (configDoc is null) throw new ArgumentNullException(nameof(configDoc));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var root = configDoc.RootElement;
            var info = new ExperienceBuilderInfo();

            // App-level properties
            if (root.TryGetProperty("attributes", out var attrs))
            {
                info.Title = GetString(attrs, "title");
                info.Description = GetString(attrs, "description");
            }

            // Language from locale settings
            if (root.TryGetProperty("mainPage", out var mainPage))
            {
                info.Language = GetString(mainPage, "locale");
            }

            // Widgets
            if (root.TryGetProperty("widgets", out var widgets) && widgets.ValueKind == JsonValueKind.Object)
            {
                foreach (var widget in widgets.EnumerateObject())
                {
                    ParseWidget(widget.Name, widget.Value, info);
                }
            }

            // Also check for pages/views layout
            if (root.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Object)
            {
                foreach (var page in pages.EnumerateObject())
                {
                    if (page.Value.TryGetProperty("label", out var pageLabel) &&
                        string.IsNullOrWhiteSpace(pageLabel.GetString()))
                    {
                        info.Widgets.Add(new ExBWidgetInfo
                        {
                            WidgetId = page.Name,
                            WidgetType = "page",
                            Label = null,
                            HasLabel = false
                        });
                    }
                }
            }

            context.ExperienceBuilder = info;
        }

        private static void ParseWidget(string widgetId, JsonElement widget, ExperienceBuilderInfo info)
        {
            string? widgetType = GetString(widget, "uri") ?? GetString(widget, "type");
            string? label = GetString(widget, "label");

            // Normalize the widget type from URIs like "widgets/arcgis/map"
            if (widgetType is not null && widgetType.Contains('/'))
            {
                var parts = widgetType.Split('/');
                widgetType = parts[^1]; // Take the last segment
            }

            info.Widgets.Add(new ExBWidgetInfo
            {
                WidgetId = widgetId,
                WidgetType = widgetType,
                Label = label,
                HasLabel = !string.IsNullOrWhiteSpace(label)
            });
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }
    }
}
