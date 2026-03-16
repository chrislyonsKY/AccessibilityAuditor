using System;
using System.IO;
using Xunit;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Tests.Core
{
    public sealed class AuditSettingsTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _tempFile;

        public AuditSettingsTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"AA_Test_{Guid.NewGuid():N}");
            _tempFile = Path.Combine(_tempDir, "settings.json");
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
            catch { /* cleanup best effort */ }
        }

        [Fact]
        public void Defaults_AreCorrect()
        {
            var settings = new AuditSettings();

            Assert.True(settings.IncludePassFindings);
            Assert.True(settings.CheckColorBlindSafety);
            Assert.Equal(0.5, settings.ContrastWarningMargin);
            Assert.Equal(0, settings.LastTargetIndex);
        }

        [Fact]
        public void SaveAndLoad_RoundTrips()
        {
            Directory.CreateDirectory(_tempDir);

            var original = new AuditSettings
            {
                IncludePassFindings = false,
                CheckColorBlindSafety = false,
                ContrastWarningMargin = 1.2,
                LastTargetIndex = 2
            };

            // Save
            var json = System.Text.Json.JsonSerializer.Serialize(original, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_tempFile, json);

            // Load
            var loadedJson = File.ReadAllText(_tempFile);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AuditSettings>(loadedJson);

            Assert.NotNull(loaded);
            Assert.False(loaded!.IncludePassFindings);
            Assert.False(loaded.CheckColorBlindSafety);
            Assert.Equal(1.2, loaded.ContrastWarningMargin);
            Assert.Equal(2, loaded.LastTargetIndex);
        }

        [Fact]
        public void Deserialize_CorruptJson_Throws()
        {
            Assert.Throws<System.Text.Json.JsonException>(() =>
                System.Text.Json.JsonSerializer.Deserialize<AuditSettings>("not valid json {{{"));
        }

        [Fact]
        public void Deserialize_EmptyObject_ReturnsDefaults()
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<AuditSettings>("{}");

            Assert.NotNull(result);
            Assert.True(result!.IncludePassFindings);
            Assert.True(result.CheckColorBlindSafety);
            Assert.Equal(0.5, result.ContrastWarningMargin);
            Assert.Equal(0, result.LastTargetIndex);
        }

        [Fact]
        public void Deserialize_PartialJson_PreservesDefaults()
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<AuditSettings>(
                "{\"IncludePassFindings\": false}");

            Assert.NotNull(result);
            Assert.False(result!.IncludePassFindings);
            // Others should be defaults
            Assert.True(result.CheckColorBlindSafety);
            Assert.Equal(0.5, result.ContrastWarningMargin);
        }
    }
}
