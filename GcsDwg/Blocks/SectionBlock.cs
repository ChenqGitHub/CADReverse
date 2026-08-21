using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

/// <summary>
/// 剖面标注块：左右两条剖面线成对出现（L 形折线 + 箭头）+ 两端剖面文字。
/// 以 1:1 尺寸在局部坐标构造（锚点为各多段线的第一个顶点），
/// 左右两部分各自以本侧第一个顶点为基准点缩放，再按 direction 旋转并平移到插入点。
/// distance 为两多段线第一个顶点之间的距离（不随缩放变化）。
/// </summary>
public static class SectionBlock
{
    private const double CutLen = 5; // 剖面线段长度（1:1）
    private const double StubLen = 5; // 箭头杆长度（1:1）
    private const double ArrowLen = 2; // 箭头长度（1:1）
    private const double ArrowLean = 15 * Math.PI / 180; // 箭头偏离竖直方向角度
    private const double TextHeight = 2.5; // 剖面文字高度（1:1）
    private const double TextOffsetX = 3.015126; // 文字距本侧第一个顶点的横向偏移（1:1）
    private const double TextOffsetY = 2.2830; // 文字相对剖面线的纵向偏移（1:1）

    /// <summary>
    /// 绘制剖面标注。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="insertPoint">插入点（左侧多段线第一个顶点，即左臂缩放基准点）</param>
    /// <param name="distance">两多段线第一个顶点之间的距离（不随缩放变化）</param>
    /// <param name="text">剖面文字，如 "Ⅲ"</param>
    /// <param name="direction">剖面方向（弧度）：0 = 水平剖切且箭头向上，-PI/2 = 竖直剖切</param>
    /// <param name="scale">整体缩放比例（左右两部分各自以本侧第一个顶点为基准缩放）</param>
    public static void Add(
        CadDocument doc,
        double distance,
        string text,
        XYZ insertPoint = default,
        double direction = 0,
        double scale = 1
    )
    {
        var s = Math.Sin(direction);
        var c = Math.Cos(direction);

        // 右侧多段线第一个顶点 = 左侧第一个顶点沿 direction 平移 distance（distance 不乘 scale）
        var rightAnchor = CadDraw.P(
            insertPoint.X + distance * c,
            insertPoint.Y + distance * s,
            insertPoint.Z
        );

        PlaceGroup(doc, insertPoint, -1, text, direction, scale);
        PlaceGroup(doc, rightAnchor, +1, text, direction, scale);
    }

    /// <summary>
    /// 将一侧剖面的一组图元以整组 ApplyScaling 缩放（pivot=首顶点），再平移到落点。
    /// </summary>
    private static void PlaceGroup(
        CadDocument doc,
        XYZ placePoint,
        int sign,
        string text,
        double direction,
        double scale
    )
    {
        var pivot = new XYZ(0, 0, 0);

        foreach (var e in CreateGroup(doc, sign, text, direction))
        {
            if (scale != 1)
            {
                e.ApplyScaling(new XYZ(scale, scale, 1), pivot);
            }
            e.ApplyTranslation(placePoint);
            doc.Entities.Add(e);
        }
    }

    /// <summary>
    /// 在局部坐标创建一侧剖面的一组图元：剖面线多段线 + 剖面文字。
    /// 首顶点（多段线第一个顶点）落在局部原点 (0,0)，方向已烘焙进坐标；不含缩放。
    /// </summary>
    /// <param name="sign">-1=左侧（剖线向左、文字偏左），+1=右侧（剖线向右、文字偏右）</param>
    private static Entity[] CreateGroup(
        CadDocument doc,
        int sign,
        string text,
        double direction
    )
    {
        double arrowDx = ArrowLen * Math.Sin(ArrowLean);
        double arrowDy = ArrowLen * Math.Cos(ArrowLean);

        var b01 = doc.Layer(CadLayers.B01);
        var b07 = doc.Layer(CadLayers.B07);

        return
        [
            // 剖面线多段线（第一个顶点即局部原点 = 缩放 pivot）
            CadDraw.Polyline(
                [
                    Local(0, 0, direction),
                    Local(sign * CutLen, 0, direction),
                    Local(sign * CutLen, StubLen, direction),
                    Local(sign * (CutLen + arrowDx), StubLen - arrowDy, direction),
                ],
                b01
            ),
            // 剖面文字（本侧偏移点正中对齐，字号随整体缩放）
            CadDraw.MText(
                text,
                LocalP(sign * TextOffsetX, TextOffsetY, direction),
                TextHeight,
                b07
            ),
        ];
    }

    /// <summary>局部多段线顶点（相对原点，已按 direction 旋转）</summary>
    private static XY Local(double x, double y, double direction)
    {
        var (rx, ry) = Rotate(x, y, direction);
        return new XY(rx, ry);
    }

    /// <summary>局部点（已按 direction 旋转）</summary>
    private static XYZ LocalP(double x, double y, double direction)
    {
        var (rx, ry) = Rotate(x, y, direction);
        return new XYZ(rx, ry, 0);
    }

    private static (double X, double Y) Rotate(double x, double y, double direction)
    {
        var s = Math.Sin(direction);
        var c = Math.Cos(direction);
        return (x * c - y * s, x * s + y * c);
    }
}
