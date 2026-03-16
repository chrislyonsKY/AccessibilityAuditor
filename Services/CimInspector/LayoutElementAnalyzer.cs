using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Layouts;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Services.CimInspector
{
    /// <summary>
    /// Analyzes CIM layout elements to extract text, metadata, and structural information.
    /// Must be called on the MCT via <c>QueuedTask.Run()</c>.
    /// </summary>
    public sealed class LayoutElementAnalyzer
    {
        /// <summary>
        /// Walks all elements in a layout and populates the audit context.
        /// Must be called on the MCT via <c>QueuedTask.Run()</c>.
        /// </summary>
        /// <param name="layout">The layout to inspect.</param>
        /// <param name="context">The audit context to populate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="layout"/> or <paramref name="context"/> is <c>null</c>.</exception>
        public void WalkLayout(Layout layout, AuditContext context)
        {
            if (layout is null) throw new ArgumentNullException(nameof(layout));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var cimLayout = layout.GetDefinition();
            if (cimLayout is null) return;

            context.LayoutTitle = cimLayout.Name;

            if (cimLayout.Elements is null) return;

            for (int i = 0; i < cimLayout.Elements.Length; i++)
            {
                var element = cimLayout.Elements[i];
                var info = AnalyzeElement(element);
                if (info is not null)
                {
                    info.SortOrder = i;
                    context.LayoutElements.Add(info);
                }
            }
        }

        /// <summary>
        /// Analyzes a single CIM layout element and returns structured information.
        /// </summary>
        /// <param name="element">The CIM element to analyze.</param>
        /// <returns>A <see cref="LayoutElementInfo"/> or <c>null</c> if the element cannot be analyzed.</returns>
        public LayoutElementInfo? AnalyzeElement(CIMElement? element)
        {
            if (element is null) return null;

            var info = element switch
            {
                CIMMapFrame mapFrame => AnalyzeMapFrame(mapFrame),
                CIMGroupElement groupEl => AnalyzeGroupElement(groupEl),
                CIMGraphicElement graphicEl => AnalyzeGraphicElement(graphicEl),
                _ => AnalyzeGenericElement(element)
            };

            // Extract description from CIM CustomProperties for all element types
            if (info is not null)
            {
                info.Description = ExtractDescription(element);
            }

            return info;
        }

        /// <summary>
        /// Extracts a description from the CIM element's CustomProperties.
        /// ArcGIS Pro stores element descriptions as a "Description" key in the
        /// CustomProperties CIMStringMap array when set via Element Properties > General.
        /// </summary>
        private static string? ExtractDescription(CIMElement element)
        {
            if (element.CustomProperties is null) return null;

            foreach (var prop in element.CustomProperties)
            {
                if (string.Equals(prop.Key, "Description", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(prop.Value))
                {
                    return prop.Value;
                }
            }

            return null;
        }

        private static LayoutElementInfo AnalyzeMapFrame(CIMMapFrame mapFrame)
        {
            var info = new LayoutElementInfo
            {
                Name = mapFrame.Name ?? "Unnamed Map Frame",
                ElementType = "MapFrame"
            };
            return info;
        }

        private LayoutElementInfo AnalyzeGroupElement(CIMGroupElement groupElement)
        {
            var info = new LayoutElementInfo
            {
                Name = groupElement.Name ?? "Unnamed Group",
                ElementType = "GroupElement"
            };
            return info;
        }

        private static LayoutElementInfo AnalyzeGraphicElement(CIMGraphicElement graphicElement)
        {
            var info = new LayoutElementInfo
            {
                Name = graphicElement.Name ?? "Unnamed Graphic",
                ElementType = "GraphicElement"
            };

            // Check if this is a picture/image element
            if (graphicElement.Graphic is CIMPictureGraphic)
            {
                info.IsPictureElement = true;
                info.ElementType = "PictureElement";
            }
            else if (graphicElement.Graphic is CIMTextGraphic textGraphic)
            {
                info.TextContent = textGraphic.Text;
                info.ElementType = "TextElement";

                var textSymbol = textGraphic.Symbol?.Symbol as CIMTextSymbol;
                if (textSymbol is not null)
                {
                    info.FontSize = textSymbol.Height;
                    info.IsBold = IsBoldStyle(textSymbol.FontStyleName);

                    var solidFill = textSymbol.Symbol?.SymbolLayers?
                        .OfType<CIMSolidFill>()
                        .FirstOrDefault();

                    if (solidFill is not null)
                    {
                        info.TextColor = CimWalker.ExtractColor(solidFill.Color);
                    }
                }
            }
            else if (graphicElement.Graphic is CIMParagraphTextGraphic paragraphGraphic)
            {
                info.TextContent = paragraphGraphic.Text;
                info.ElementType = "ParagraphTextElement";

                var textSymbol = paragraphGraphic.Symbol?.Symbol as CIMTextSymbol;
                if (textSymbol is not null)
                {
                    info.FontSize = textSymbol.Height;
                    info.IsBold = IsBoldStyle(textSymbol.FontStyleName);

                    var solidFill = textSymbol.Symbol?.SymbolLayers?
                        .OfType<CIMSolidFill>()
                        .FirstOrDefault();

                    if (solidFill is not null)
                    {
                        info.TextColor = CimWalker.ExtractColor(solidFill.Color);
                    }
                }
            }

            return info;
        }

        private static LayoutElementInfo AnalyzeGenericElement(CIMElement element)
        {
            var info = new LayoutElementInfo
            {
                Name = element.Name ?? "Unnamed Element",
                ElementType = element.GetType().Name.Replace("CIM", "")
            };
            return info;
        }

        private static bool IsBoldStyle(string? fontStyleName)
        {
            if (string.IsNullOrEmpty(fontStyleName)) return false;
            return fontStyleName.Contains("Bold", StringComparison.OrdinalIgnoreCase);
        }
    }
}
