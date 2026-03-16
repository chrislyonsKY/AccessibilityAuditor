# ArcGIS Pro SDK Skill — v2 Additions

> Extends the existing arcgis-pro-sdk-skill.md.
> Read the original first. Only v2-specific patterns documented here.

---

## CIM Color Access for Deterministic Fixes

Reading and writing symbol colors via CIM requires `QueuedTask.Run()`:

```csharp
await QueuedTask.Run(() =>
{
    var element = layout.FindElement(elementName) as GraphicElement;
    if (element is null) return;

    var cimGraphic = element.GetGraphic() as CIMTextGraphic;
    if (cimGraphic?.Symbol?.Symbol is CIMTextSymbol textSymbol)
    {
        // Read current color
        var current = textSymbol.Color as CIMRGBColor;

        // Apply corrected color
        textSymbol.Color = new CIMRGBColor { R = r, G = g, B = b, Alpha = 255 };
        cimGraphic.Symbol = new CIMSymbolReference { Symbol = textSymbol };
        element.SetGraphic(cimGraphic);
    }
});
```

## CIM Font Size Access

```csharp
await QueuedTask.Run(() =>
{
    var element = layout.FindElement(elementName) as GraphicElement;
    var cimGraphic = element?.GetGraphic() as CIMTextGraphic;
    if (cimGraphic?.Symbol?.Symbol is CIMTextSymbol textSymbol)
    {
        textSymbol.Height = minimumSizePoints;
        element.SetGraphic(cimGraphic);
    }
});
```

## CIM Element Description (Alt Text)

```csharp
await QueuedTask.Run(() =>
{
    var element = layout.FindElement(elementName);
    var cimElement = element?.GetDefinition();
    if (cimElement is not null)
    {
        cimElement.CustomProperties ??= Array.Empty<CIMStringMap>();
        // ArcGIS Pro uses "description" as the accessible description field
        // TODO: confirm correct CIM property for Pro 3.4 SDK
        element.SetDefinition(cimElement);
    }
});
```

## WCAG Contrast Ratio Calculation

Pure math — no Pro SDK dependency:

```csharp
/// <summary>Calculates relative luminance per WCAG 2.1 spec.</summary>
private static double RelativeLuminance(byte r, byte g, byte b)
{
    double Linearise(byte c)
    {
        double s = c / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
    return 0.2126 * Linearise(r) + 0.7152 * Linearise(g) + 0.0722 * Linearise(b);
}

/// <summary>Returns WCAG contrast ratio between two colors. Min 4.5:1 for AA normal text.</summary>
private static double ContrastRatio(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
{
    var l1 = RelativeLuminance(r1, g1, b1);
    var l2 = RelativeLuminance(r2, g2, b2);
    var lighter = Math.Max(l1, l2);
    var darker = Math.Min(l1, l2);
    return (lighter + 0.05) / (darker + 0.05);
}
```

## Windows Credential Manager via CredentialManagement NuGet

```csharp
// Store
using var cred = new Credential
{
    Target = targetName,
    Password = key,
    Type = CredentialType.Generic,
    PersistanceType = PersistanceType.LocalComputer
};
cred.Save();

// Retrieve
using var cred = new Credential { Target = targetName };
cred.Load();
return string.IsNullOrEmpty(cred.Password) ? null : cred.Password;

// Delete
using var cred = new Credential { Target = targetName };
cred.Delete();
```

## .csproj Upgrade (v1 → v2)

```xml
<!-- Single line change -->
<TargetFramework>net8.0-windows</TargetFramework>

<!-- SDK NuGet references — bump from 3.0.x to 3.4.x -->
<PackageReference Include="ArcGIS.Core" Version="3.4.*" />
<PackageReference Include="ArcGIS.Desktop.Framework" Version="3.4.*" />
<PackageReference Include="ArcGIS.Desktop.Layouts" Version="3.4.*" />
<!-- ... other Pro SDK references -->

<!-- New v2 dependencies -->
<PackageReference Include="CredentialManagement" Version="1.0.2" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
```
