using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.PortalInspector
{
    /// <summary>
    /// Checks portal item metadata for accessibility compliance (title, description, tags, culture).
    /// All HTTP calls run on background threads — never inside QueuedTask.
    /// </summary>
    public sealed class PortalItemChecker
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Fetches and parses portal item metadata into a <see cref="PortalItemInfo"/>.
        /// </summary>
        /// <param name="portalUrl">The portal base URL (e.g., "https://www.arcgis.com").</param>
        /// <param name="itemId">The portal item ID.</param>
        /// <param name="token">An authentication token, or <c>null</c> for public items.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="PortalItemInfo"/> populated from the REST response.</returns>
        public async Task<PortalItemInfo> GetItemInfoAsync(
            string portalUrl, string itemId, string? token = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(portalUrl)) throw new ArgumentException("Portal URL is required.", nameof(portalUrl));
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID is required.", nameof(itemId));

            string url = $"{portalUrl.TrimEnd('/')}/sharing/rest/content/items/{itemId}?f=json";
            if (!string.IsNullOrEmpty(token))
                url += $"&token={token}";

            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check for portal error response
            if (root.TryGetProperty("error", out var error))
            {
                string msg = error.TryGetProperty("message", out var m) ? m.GetString() ?? "Unknown error" : "Unknown error";
                throw new InvalidOperationException($"Portal API error: {msg}");
            }

            var info = new PortalItemInfo
            {
                ItemId = itemId,
                PortalUrl = portalUrl,
                Title = GetString(root, "title"),
                Description = GetString(root, "description"),
                Snippet = GetString(root, "snippet"),
                Culture = GetString(root, "culture"),
                ItemType = GetString(root, "type"),
                AccessInformation = GetString(root, "accessInformation")
            };

            if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tags.EnumerateArray())
                {
                    var val = tag.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        info.Tags.Add(val);
                }
            }

            return info;
        }

        /// <summary>
        /// Fetches the raw item data (web map JSON or ExB config) as a <see cref="JsonDocument"/>.
        /// Caller is responsible for disposing the returned document.
        /// </summary>
        /// <param name="portalUrl">The portal base URL.</param>
        /// <param name="itemId">The portal item ID.</param>
        /// <param name="token">An authentication token, or <c>null</c> for public items.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A parsed <see cref="JsonDocument"/> of the item data.</returns>
        public async Task<JsonDocument> GetItemDataAsync(
            string portalUrl, string itemId, string? token = null,
            CancellationToken cancellationToken = default)
        {
            string url = $"{portalUrl.TrimEnd('/')}/sharing/rest/content/items/{itemId}/data?f=json";
            if (!string.IsNullOrEmpty(token))
                url += $"&token={token}";

            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }

        /// <summary>
        /// Searches the portal for web map and Experience Builder items accessible to the current user.
        /// </summary>
        /// <param name="portalUrl">The portal base URL.</param>
        /// <param name="token">An authentication token, or <c>null</c> for public search.</param>
        /// <param name="searchText">Optional search text to filter results.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of matching portal item summaries.</returns>
        public async Task<IReadOnlyList<PortalItemSummary>> SearchItemsAsync(
            string portalUrl, string? token = null, string? searchText = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(portalUrl))
                throw new ArgumentException("Portal URL is required.", nameof(portalUrl));

            // Build query: web maps and ExB apps
            string typeFilter = "(type:\"Web Map\" OR type:\"Web Mapping Application\")";
            string query = string.IsNullOrWhiteSpace(searchText)
                ? typeFilter
                : $"{searchText} AND {typeFilter}";

            string url = $"{portalUrl.TrimEnd('/')}/sharing/rest/search" +
                         $"?f=json&q={Uri.EscapeDataString(query)}&num=50&sortField=modified&sortOrder=desc";

            if (!string.IsNullOrEmpty(token))
                url += $"&token={token}";

            var items = new List<PortalItemSummary>();

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in results.EnumerateArray())
                    {
                        var summary = new PortalItemSummary
                        {
                            ItemId = GetString(item, "id") ?? string.Empty,
                            Title = GetString(item, "title") ?? "Untitled",
                            ItemType = GetString(item, "type") ?? "Unknown",
                            Owner = GetString(item, "owner") ?? string.Empty
                        };

                        if (!string.IsNullOrEmpty(summary.ItemId))
                            items.Add(summary);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portal search failed: {ex.Message}");
            }

            return items;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }
    }

    /// <summary>
    /// Represents a minimal portal item summary for browse/selection UI.
    /// </summary>
    public sealed class PortalItemSummary
    {
        /// <summary>Gets or sets the portal item ID.</summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>Gets or sets the item title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the item type (e.g., "Web Map").</summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>Gets or sets the item owner.</summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>Gets a display string for the ComboBox.</summary>
        public string DisplayText => $"{Title}  ({Owner})";

        /// <inheritdoc />
        public override string ToString() => DisplayText;
    }
}
