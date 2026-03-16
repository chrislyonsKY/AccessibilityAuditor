using System.Text.Json;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Services.PortalInspector;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ExperienceBuilderChecker"/> covering ExB config parsing.
/// </summary>
public sealed class ExperienceBuilderCheckerTests
{
    private readonly ExperienceBuilderChecker _checker = new();

    [Fact]
    public void ParseExBConfig_NullDoc_Throws()
    {
        var context = new AuditContext();
        Assert.Throws<ArgumentNullException>(() => _checker.ParseExBConfig(null!, context));
    }

    [Fact]
    public void ParseExBConfig_NullContext_Throws()
    {
        using var doc = JsonDocument.Parse("{}");
        Assert.Throws<ArgumentNullException>(() => _checker.ParseExBConfig(doc, null!));
    }

    [Fact]
    public void ParseExBConfig_EmptyJson_SetsExBInfo()
    {
        using var doc = JsonDocument.Parse("{}");
        var context = new AuditContext();

        _checker.ParseExBConfig(doc, context);

        Assert.NotNull(context.ExperienceBuilder);
    }

    [Fact]
    public void ParseExBConfig_ExtractsTitle()
    {
        string json = @"{ ""attributes"": { ""title"": ""My App"", ""description"": ""An accessible app"" } }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseExBConfig(doc, context);

        Assert.Equal("My App", context.ExperienceBuilder!.Title);
        Assert.Equal("An accessible app", context.ExperienceBuilder.Description);
    }

    [Fact]
    public void ParseExBConfig_ExtractsWidgets()
    {
        string json = @"{
            ""widgets"": {
                ""widget_1"": { ""uri"": ""widgets/arcgis/map"", ""label"": ""Main Map"" },
                ""widget_2"": { ""uri"": ""widgets/arcgis/search"" }
            }
        }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseExBConfig(doc, context);

        Assert.Equal(2, context.ExperienceBuilder!.Widgets.Count);

        var mapWidget = context.ExperienceBuilder.Widgets[0];
        Assert.Equal("widget_1", mapWidget.WidgetId);
        Assert.Equal("map", mapWidget.WidgetType);
        Assert.Equal("Main Map", mapWidget.Label);
        Assert.True(mapWidget.HasLabel);

        var searchWidget = context.ExperienceBuilder.Widgets[1];
        Assert.Equal("widget_2", searchWidget.WidgetId);
        Assert.False(searchWidget.HasLabel);
    }

    [Fact]
    public void ParseExBConfig_ExtractsLanguage()
    {
        string json = @"{ ""mainPage"": { ""locale"": ""en-us"" } }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseExBConfig(doc, context);

        Assert.Equal("en-us", context.ExperienceBuilder!.Language);
    }

    [Fact]
    public void ParseExBConfig_NoLanguage_LanguageIsNull()
    {
        string json = @"{ ""mainPage"": {} }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseExBConfig(doc, context);

        Assert.Null(context.ExperienceBuilder!.Language);
    }
}
