using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using CSMath.Extensions;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Drawing;
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
        var orgin = CadDraw.P(0, 0);

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 按轮廓自右下向右上、再向左下的走向连接，故凸度取负值。
        var arcBulge =
            6100.0 - 3500.0 <= 0 ? 0 : CadDraw.BulgeFromTangent((6100.0 - 3500.0) / 2.0 / 4500.0);
        var mainEntity = new List<Entity>
        {
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
        };
        mainEntity.AddRange(TitleBlock.Add(doc, "桥墩立面图", "", CadDraw.P(0, 11500), 10));

        doc.AddTranslated(orgin, mainEntity);

        // 侧面标注
        doc.AddTranslated(
            new XYZ(-6100 / 2, 0, 0).Add(orgin),
            CadDraw
                .ChainDimension(
                    [CadDraw.P(0, 0), CadDraw.P(0, 5000), CadDraw.P(0, 9500), CadDraw.P(0, 10000)],
                    90
                )
                .Append(CadDraw.RotatedDimension(CadDraw.P(0, 0), CadDraw.P(0, 10000), 130))
        );

        // 正面标注-下面标注
        doc.AddTranslated(
            orgin,
            [CadDraw.RotatedDimension(CadDraw.P(-3500 / 2, 0), CadDraw.P(3500 / 2, 0), 90)]
        );
        // 正面标注-上面标注
        doc.AddTranslated(
            new XYZ(0, 10000, 0).Add(orgin),
            [CadDraw.RotatedDimension(CadDraw.P(-6100 / 2, 0), CadDraw.P(6100 / 2, 0), 90)]
        );

    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的桥墩侧面轮廓。
    /// </summary>
    public static void DrawPierSideElevation(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);

        var orgin = CadDraw.P(0, 0);

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 将扫描坐标的微小浮点误差归并到公共端点，形成严格闭合轮廓。
        var arcBulge =
            2600.0 - 2200.0 <= 0 ? 0 : CadDraw.BulgeFromTangent((2600.0 - 2200.0) / 2.0 / 4500.0);
        var mainEntity = new List<Entity>
        {
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
        };
        mainEntity.AddRange(TitleBlock.Add(doc, "桥墩侧面图", "", CadDraw.P(0, 11500), 10));

        doc.AddTranslated(orgin, mainEntity);

        // 侧面标注
        doc.AddTranslated(
            orgin.Add(new XYZ(-2600 / 2, 0, 0)),
            CadDraw
                .ChainDimension(
                    [CadDraw.P(0, 0), CadDraw.P(0, 5000), CadDraw.P(0, 9500), CadDraw.P(0, 10000)],
                    90
                )
                .Append(CadDraw.RotatedDimension(CadDraw.P(0, 0), CadDraw.P(0, 10000), 90 + 30))
        );

        // 正面标注-下面标注
        doc.AddTranslated(
            orgin,
            CadDraw.RotatedDimension(CadDraw.P(-2200 / 2, 0), CadDraw.P(2200 / 2, 0), 90)
        );
        // 正面标注-上面标注
        doc.AddTranslated(
            orgin.Add(new XYZ(0, 10000, 0)),
            CadDraw.RotatedDimension(CadDraw.P(-2600 / 2, 0), CadDraw.P(2600 / 2, 0), 90)
        );

    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的墩顶平面轮廓。
    /// </summary>
    public static void DrawPierTopPlan(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var orgin = CadDraw.P(0, 0);

        var mainEntity = new List<Entity>
        {
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
        };
        mainEntity.AddRange(TitleBlock.Add(doc, "墩顶平面图", "", CadDraw.P(0, 4450), 10));

        doc.AddTranslated(orgin, mainEntity);
        // 标注
        doc.AddTranslated(
            orgin.Add(new XYZ(0, 2600 / 2, 0)),
            CadDraw.RotatedDimension(CadDraw.P(-6100 / 2, 0), CadDraw.P(6100 / 2, 0), 90)
        );
        // 正面标注
        doc.AddTranslated(
            orgin.Add(new XYZ(-6100 / 2, 0, 0)),
            CadDraw.RotatedDimension(CadDraw.P(0, -2600 / 2), CadDraw.P(0, 2600 / 2), 90)
        );

    }

    /// <summary>
    /// 绘制由 cad-region.json 识别得到的墩顶底面轮廓。
    /// </summary>
    public static void DrawPierBottomPlan(CadDocument doc)
    {
        var orgin = CadDraw.P(0, 0);

        var mainEntity = new List<Entity>
        {
            CadDraw.Polyline(
                [
                    CadDraw.V(-3500 / 2, 2200 / 2),
                    CadDraw.V(3500 / 2, 2200 / 2),
                    CadDraw.V(3500 / 2, -2200 / 2),
                    CadDraw.V(-3500 / 2, -2200 / 2),
                ],
                doc.Layer(CadLayers.B01),
                closed: true
            ),
            CadDraw.Line(CadDraw.P(-3500 / 2, 0), CadDraw.P(3500 / 2, 0), doc.Layer(CadLayers.B06)),
            CadDraw.Line(CadDraw.P(0, -2200 / 2), CadDraw.P(0, 2200 / 2), doc.Layer(CadLayers.B06)),
        };
        mainEntity.AddRange(TitleBlock.Add(doc, "墩底平面图", "", CadDraw.P(0, 3150), 10));

        doc.AddTranslated(orgin, mainEntity);

        // 水平标注：保持标注线的 Y 坐标不变。
        doc.AddTranslated(
            orgin,
            CadDraw.RotatedDimension(
                CadDraw.P(-3500 / 2, 2200 / 2),
                CadDraw.P(3500 / 2, 2200 / 2),
                90
            )
        );

        // 竖直标注：保持标注线的 X 坐标不变。
        doc.AddTranslated(
            orgin,
            CadDraw.RotatedDimension(
                CadDraw.P(-3500 / 2, -2200 / 2),
                CadDraw.P(-3500 / 2, 2200 / 2),
                90
            )
        );
    }
}
