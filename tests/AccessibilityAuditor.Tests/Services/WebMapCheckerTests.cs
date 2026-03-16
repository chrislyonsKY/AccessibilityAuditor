using System.Text.Json;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;
using AccessibilityAuditor.Services.PortalInspector;

namespace AccessibilityAuditor.Tests.Services;

/// <summary>
/// Unit tests for <see cref="WebMapChecker"/> covering web map JSON parsing,
/// renderer color extraction, and popup configuration parsing.
/// </summary>
public sealed class WebMapCheckerTests
{
    private readonly WebMapChecker _checker = new();

    [Fact]
    public void ParseWebMap_NullDoc_Throws()
    {
        var context = new AuditContext();
        Assert.Throws<ArgumentNullException>(() => _checker.ParseWebMap(null!, context));
    }

    [Fact]
    public void ParseWebMap_NullContext_Throws()
    {
        using var doc = JsonDocument.Parse("{}");
        Assert.Throws<ArgumentNullException>(() => _checker.ParseWebMap(doc, null!));
    }

    [Fact]
    public void ParseWebMap_EmptyJson_NoLayers()
    {
        using var doc = JsonDocument.Parse("{}");
        var context = new AuditContext();

        _checker.ParseWebMap(doc, context);

        Assert.Empty(context.WebMapLayers);
        Assert.Empty(context.Popups);
    }

    [Fact]
    public void ParseWebMap_ExtractsLayerTitle()
    {
        string json = @"{
            ""operationalLayers"": [
                { ""id"": ""layer1"", ""title"": ""Permit Boundaries"" }
            ]
        }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseWebMap(doc, context);

        Assert.Single(context.WebMapLayers);
        Assert.Equal("layer1", context.WebMapLayers[0].LayerId);
        Assert.Equal("Permit Boundaries", context.WebMapLayers[0].Title);
    }

    [Fact]
    public void ParseWebMap_ExtractsSimpleRendererColor()
    {
        string json = @"{
            ""operationalLayers"": [{
                ""id"": ""l1"",
                ""title"": ""Test"",
                ""layerDefinition"": {
                    ""drawingInfo"": {
                        ""renderer"": {
                            ""type"": ""simple"",
                            ""symbol"": { ""color"": [255, 0, 0, 255] }
                        }
                    }
                }
            }]
        }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseWebMap(doc, context);

        Assert.Single(context.WebMapLayers);
        Assert.Single(context.WebMapLayers[0].RendererColors);
        Assert.Equal(255, context.WebMapLayers[0].RendererColors[0].R);
        Assert.Equal(0, context.WebMapLayers[0].RendererColors[0].G);
        Assert.Equal(0, context.WebMapLayers[0].RendererColors[0].B);
    }

    [Fact]
    public void ParseWebMap_ExtractsUniqueValueRendererColors()
    {
        string json = @"{
            ""operationalLayers"": [{
                ""id"": ""l1"",
                ""title"": ""Status"",
                ""layerDefinition"": {
                    ""drawingInfo"": {
                        ""renderer"": {
                            ""type"": ""uniqueValue"",
                            ""field1"": ""STATUS"",
                            ""uniqueValueInfos"": [
                                { ""value"": ""Active"", ""symbol"": { ""color"": [255, 0, 0, 255] } },
                                { ""value"": ""Inactive"", ""symbol"": { ""color"": [0, 255, 0, 255] } }
                            ]
                        }
                    }
                }
            }]
        }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseWebMap(doc, context);

        Assert.Equal(2, context.WebMapLayers[0].RendererColors.Count);
        Assert.Equal("uniqueValue", context.WebMapLayers[0].RendererType);
    }

    [Fact]
    public void ParseWebMap_ExtractsPopupInfo()
    {
        string json = @"{
            ""operationalLayers"": [{
                ""id"": ""l1"",
                ""title"": ""Permits"",
                ""popupInfo"": {
                    ""title"": ""{PERMIT_NO}"",
                    ""description"": ""<p>Details</p>"",
                    ""fieldInfos"": [
                        { ""fieldName"": ""PERMIT_NO"", ""label"": ""Permit Number"", ""visible"": true },
                        { ""fieldName"": ""STATUS"", ""label"": ""Status"", ""visible"": true },
                        { ""fieldName"": ""OBJECTID"", ""label"": ""ID"", ""visible"": false }
                    ]
                }
            }]
        }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseWebMap(doc, context);

        Assert.Single(context.Popups);
        Assert.Equal("{PERMIT_NO}", context.Popups[0].TitleTemplate);
        Assert.True(context.Popups[0].HasCustomHtml);
        Assert.Equal(2, context.Popups[0].FieldNames.Count); // OBJECTID excluded (visible=false)
    }

    [Fact]
    public void ParseWebMap_BuildsRendererInfoForRules()
    {
        string json = @"{
            ""operationalLayers"": [{
                ""id"": ""l1"",
                ""title"": ""Test"",
                ""layerDefinition"": {
                    ""drawingInfo"": {
                        ""renderer"": {
                            ""type"": ""uniqueValue"",
                            ""uniqueValueInfos"": [
                                { ""value"": ""A"", ""label"": ""Alpha"", ""symbol"": { ""color"": [100, 200, 50, 255] } }
                            ]
                        }
                    }
                }
            }]
        }";
        using var doc = JsonDocument.Parse(json);
        var context = new AuditContext();

        _checker.ParseWebMap(doc, context);

        Assert.Single(context.Renderers);
        Assert.Equal("uniqueValue", context.Renderers[0].RendererType);
        Assert.Single(context.Renderers[0].SymbolClasses);
        Assert.Equal("Alpha", context.Renderers[0].SymbolClasses[0].Label);
    }

    [Fact]
    public void ExtractSymbolColor_ValidArray_ReturnsColorInfo()
    {
        using var doc = JsonDocument.Parse(@"{ ""color"": [128, 64, 32, 200] }");
        var color = WebMapChecker.ExtractSymbolColor(doc.RootElement);

        Assert.NotNull(color);
        Assert.Equal(128, color!.R);
        Assert.Equal(64, color.G);
        Assert.Equal(32, color.B);
        Assert.Equal(200, color.A);
    }

    [Fact]
    public void ExtractSymbolColor_NoColorProperty_ReturnsNull()
    {
        using var doc = JsonDocument.Parse(@"{ ""size"": 10 }");
        var color = WebMapChecker.ExtractSymbolColor(doc.RootElement);

        Assert.Null(color);
    }

    [Fact]
    public void ExtractSymbolColor_ThreeComponents_DefaultAlpha255()
    {
        using var doc = JsonDocument.Parse(@"{ ""color"": [100, 150, 200] }");
        var color = WebMapChecker.ExtractSymbolColor(doc.RootElement);

        Assert.NotNull(color);
        Assert.Equal(255, color!.A);
    }
}
