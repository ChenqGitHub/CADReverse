using ACadSharp;
using ACadSharp.Tables;
using GcsDwg.Blocks;
using GcsDwg.Standards;

namespace GcsDwg.Infrastructure;

public static class CadInitializer
{
    /// <summary>
    /// 初始化 CAD 标准：线型、图层、文字样式、标注样式、常用块。
    /// </summary>
    public static void InitCad(CadDocument doc)
    {
        CadDraw.CurrentDocument = doc;

        AddLineType(doc, "DASHED", "Dashed __ __ __ __", [0.5, -0.25]);
        AddLineType(doc, "CENTER", "Center _ _ _ _ _", [1.25, -0.25, 0.25, -0.25]);

        foreach (var def in CadLayers.All)
            AddLayer(doc, def);

        foreach (var def in CadTextStyles.All)
            AddTextStyle(doc, def);

        foreach (var def in CadDimStyles.All)
            AddDimStyle(doc, def);

        HRB400Block.EnsureDefined(doc);
        SteelSection.EnsureDefined(doc);
    }

    private static void AddDimStyle(CadDocument doc, CadDimStyleDef def)
    {
        if (doc.DimensionStyles.Contains(def.Name))
            return;

        var dimStyle = new DimensionStyle(def.Name)
        {
            ScaleFactor = def.ScaleFactor,
            DimensionLineWeight = 0,
            ExtensionLineWeight = 0,
            TextHeight = def.TextHeight,
            ArrowSize = def.ArrowSize,
            TickSize = def.TickSize,
            DimensionLineGap = def.DimensionLineGap,
            ExtensionLineOffset = def.ExtensionLineOffset,
            ExtensionLineExtension = def.ExtensionLineExtension,
            DecimalPlaces = 2,
            Style = doc.TextStyles[def.TextStyleName],
            DimensionLineColor = Color.ByBlock,
            ExtensionLineColor = Color.ByBlock,
            TextColor = Color.ByBlock,
            LineType = doc.LineTypes["ByBlock"],
            LineTypeExt1 = doc.LineTypes["ByBlock"],
            LineTypeExt2 = doc.LineTypes["ByBlock"],
        };

        doc.DimensionStyles.Add(dimStyle);
    }

    private static void AddTextStyle(CadDocument doc, CadTextStyleDef def)
    {
        if (doc.TextStyles.Contains(def.Name))
            return;

        doc.TextStyles.Add(
            new TextStyle(def.Name)
            {
                BigFontFilename = def.BigFontFilename,
                Filename = def.Filename,
                Height = 0,
                Width = def.Width,
            }
        );
    }

    private static void AddLineType(
        CadDocument doc,
        string name,
        string description,
        double[] pattern
    )
    {
        if (doc.LineTypes.Contains(name))
            return;

        var lineType = new LineType(name) { Description = description };
        foreach (var length in pattern)
        {
            lineType.AddSegment(new LineType.Segment { Length = length });
        }

        doc.LineTypes.Add(lineType);
    }

    private static Layer AddLayer(CadDocument doc, CadLayerDef def)
    {
        if (doc.Layers.Contains(def.Name))
            return doc.Layers[def.Name];

        var layer = new Layer(def.Name)
        {
            Color = new Color(def.Color),
            LineType = doc.LineTypes[def.LineType],
            PlotFlag = !def.NoPlot,
        };

        doc.Layers.Add(layer);
        return layer;
    }
}
