using ACadSharp;
using ACadSharp.Tables;
using CSMath;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Standards;

namespace CadCli.Generated.Pier;

public static class PierStructureDrawing
{
    public static void Draw(CadDocument doc)
    {
        DrawPierElevation(doc);
        DrawPierBottomPlan(doc);
        DrawPierTopPlan(doc);
        DrawPierSideElevation(doc);
    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的桥墩立面轮廓。
    /// </summary>
    public static void DrawPierElevation(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var dim = doc.Layer(CadLayers.B03);

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 按轮廓自右下向右上、再向左下的走向连接，故凸度取负值。
        double arcBulge =
            6100.0 - 3500.0 <= 0 ? 0 : CadDraw.BulgeFromTangent((6100.0 - 3500.0) / 2.0 / 4500.0);
        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3500 / 2, 0),
                        CadDraw.V(-3500 / 2, 5000, bulge: arcBulge),
                        CadDraw.V(-6100 / 2, 9500),
                        CadDraw.V(-6100 / 2, 10000),
                        CadDraw.V(0, 10000),
                        CadDraw.V(6100 / 2, 10000),
                        CadDraw.V(6100 / 2, 9500, bulge: arcBulge),
                        CadDraw.V(3500 / 2, 5000),
                        CadDraw.V(3500 / 2, 0),
                    ],
                    outlineLayer,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(0, -500), CadDraw.P(0, 10500), center),
            ]
        );

        // 侧面标注
        doc.AddTranslated(
            new XYZ(-6100 / 2, 0, 0),
            CadDraw
                .ChainDimension(
                    [CadDraw.P(0, 0), CadDraw.P(0, 5000), CadDraw.P(0, 9500), CadDraw.P(0, 10000)],
                    90,
                    dim
                )
                .Append(CadDraw.RotatedDimension(CadDraw.P(0, 0), CadDraw.P(0, 10000), 130, dim))
        );

        // 正面标注-下面标注
        doc.AddTranslated(
            new XYZ(0, 0, 0),
            [CadDraw.RotatedDimension(CadDraw.P(-3500 / 2, 0), CadDraw.P(3500 / 2, 0), 90, dim)]
        );
        // 正面标注-上面标注
        doc.AddTranslated(
            new XYZ(0, 10000, 0),
            [CadDraw.RotatedDimension(CadDraw.P(-6100 / 2, 0), CadDraw.P(6100 / 2, 0), 90, dim)]
        );

        TitleBlock.Add(doc, "桥墩立面图", "", CadDraw.P(0, 11500), 10);
    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的桥墩侧面轮廓。
    /// </summary>
    public static void DrawPierSideElevation(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var dim = doc.Layer(CadLayers.B03);

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 将扫描坐标的微小浮点误差归并到公共端点，形成严格闭合轮廓。
        var arcBulge =
            2600.0 - 2200.0 <= 0 ? 0 : CadDraw.BulgeFromTangent((2600.0 - 2200.0) / 2.0 / 4500.0);
        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2200 / 2, 0),
                        CadDraw.V(-2200 / 2, 5000, bulge: arcBulge),
                        CadDraw.V(-2600 / 2, 9500),
                        CadDraw.V(-2600 / 2, 10000),
                        CadDraw.V(0, 10000),
                        CadDraw.V(2600 / 2, 10000),
                        CadDraw.V(2600 / 2, 9500, bulge: arcBulge),
                        CadDraw.V(2200 / 2, 5000),
                        CadDraw.V(2200 / 2, 0),
                    ],
                    outlineLayer,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(0, -500), CadDraw.P(0, 10500), center),
            ]
        );

        // 侧面标注
        doc.AddTranslated(
            new XYZ(0, 0, 0),
            CadDraw
                .ChainDimension(
                    [CadDraw.P(0, 0), CadDraw.P(0, 5000), CadDraw.P(0, 9500), CadDraw.P(0, 10000)],
                    90,
                    dim
                )
                .Append(CadDraw.RotatedDimension(CadDraw.P(0, 0), CadDraw.P(0, 10000), 90, dim))
        );

        // 正面标注-下面标注
        doc.AddTranslated(
            new XYZ(0, 0, 0),
            [CadDraw.RotatedDimension(CadDraw.P(-2200 / 2, 0), CadDraw.P(2200 / 2, 0), 90, dim)]
        );
        // 正面标注-上面标注
        doc.AddTranslated(
            new XYZ(0, 0, 0),
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(-2600 / 2, 10000),
                    CadDraw.P(2600 / 2, 10000),
                    90,
                    dim
                ),
            ]
        );

        TitleBlock.Add(doc, "桥墩侧面图", "", CadDraw.P(0, 11500), 10);
    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的墩顶平面轮廓。
    /// </summary>
    public static void DrawPierTopPlan(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var dim = doc.Layer(CadLayers.B03);

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-6100 / 2, 2600 / 2),
                        CadDraw.V(6100 / 2, 2600 / 2),
                        CadDraw.V(6100 / 2, -2600 / 2),
                        CadDraw.V(-6100 / 2, -2600 / 2),
                    ],
                    outlineLayer,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(-6100 / 2, 0), CadDraw.P(6100 / 2, 0), center),
                CadDraw.Line(CadDraw.P(0, -2600 / 2), CadDraw.P(0, 2600 / 2), center),
            ]
        );
        // 正面标注
        doc.AddTranslated(
            new XYZ(0, 0, 0),
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(-6100 / 2, 2600 / 2),
                    CadDraw.P(6100 / 2, 2600 / 2),
                    90,
                    dim
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(-6100 / 2, -2600 / 2),
                    CadDraw.P(-6100 / 2, 2600 / 2),
                    90,
                    dim
                ),
            ]
        );

        TitleBlock.Add(doc, "墩顶平面图", "", CadDraw.P(0, 4450), 10);
    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的墩顶底面轮廓。
    /// </summary>
    public static void DrawPierBottomPlan(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var dim = doc.Layer(CadLayers.B03);

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3500 / 2, 2200 / 2),
                        CadDraw.V(3500 / 2, 2200 / 2),
                        CadDraw.V(3500 / 2, -2200 / 2),
                        CadDraw.V(-3500 / 2, -2200 / 2),
                    ],
                    outlineLayer,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(-3500 / 2, 0), CadDraw.P(3500 / 2, 0), center),
                CadDraw.Line(CadDraw.P(0, -2200 / 2), CadDraw.P(0, 2200 / 2), center),
            ]
        );

        // 正面标注
        doc.AddTranslated(
            new XYZ(0, 0, 0),
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(-3500 / 2, 2200 / 2),
                    CadDraw.P(3500 / 2, 2200 / 2),
                    90,
                    dim
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(-3500 / 2, -2200 / 2),
                    CadDraw.P(-3500 / 2, 2200 / 2),
                    90,
                    dim
                ),
            ]
        );

        TitleBlock.Add(doc, "墩底平面图", "", CadDraw.P(0, 3150), 10);
    }
}
