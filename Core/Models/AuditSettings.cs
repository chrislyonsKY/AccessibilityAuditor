using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Persistent audit settings saved across sessions.
    /// </summary>
    public sealed class AuditSettings
    {
        /// <summary>Gets or sets whether pass findings are included in results.</summary>
        public bool IncludePassFindings { get; set; } = true;

        /// <summary>Gets or sets whether colorblind safety is evaluated.</summary>
        public bool CheckColorBlindSafety { get; set; } = true;

        /// <summary>Gets or sets the contrast ratio warning margin above the threshold.</summary>
        public double ContrastWarningMargin { get; set; } = 0.5;

        /// <summary>Gets or sets the last selected target index.</summary>
        public int LastTargetIndex { get; set; }

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AccessibilityAuditor");

        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Loads settings from disk. Returns defaults if the file doesn't exist or is corrupt.
        /// </summary>
        public static AuditSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AuditSettings>(json, JsonOptions) ?? new AuditSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            return new AuditSettings();
        }

        /// <summary>
        /// Saves settings to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
