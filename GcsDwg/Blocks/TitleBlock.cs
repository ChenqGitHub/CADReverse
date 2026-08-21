using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

/// <summary>
/// 图题（标题）标注块：标题文字 + 两条横线 + 比例文字。
/// 以 1:1 在局部坐标(原点=下横线中点)构造后，整体倍数缩放并平移至插入点。
/// 横线长度随标题文字个数自动伸缩，标题文字沿横线居中，比例文字位于横线右端外侧。
/// </summary>
public static class TitleBlock
{
    private const double TitleHeight = 5;
    private const double ScaleHeight = 3.5;
    private const double LineGap = 1.5;
    private const double TitleGap = 2.0;
    private const double ScaleGap = 0.5;
    private const double CharWidth = 5;
    private const double EndMargin = 2;

    /// <summary>
    /// 绘制图题标注。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="insertPoint">插入点（下横线中点）</param>
    /// <param name="titleText">标题文字，如 "Ⅰ-Ⅰ"</param>
    /// <param name="scaleText">比例文字，如 "1:25"</param>
    /// <param name="scale">整体缩放比例，以插入点为基准放大/缩小</param>
    public static void Add(
        CadDocument doc,
        string titleText,
        string scaleText,
        XYZ insertPoint = default,
        double scale = 1
    )
    {
        var pivot = new XYZ(0, 0, 0);

        foreach (var e in CreateEntities(doc, titleText, scaleText))
        {
            if (scale != 1)
            {
                e.ApplyScaling(new XYZ(scale, scale, 1), pivot);
            }
            e.ApplyTranslation(insertPoint);
            doc.Entities.Add(e);
        }
    }

    /// <summary>
    /// 在局部坐标（原点=下横线中点）创建图题图元。
    /// </summary>
    private static Entity[] CreateEntities(CadDocument doc, string titleText, string scaleText)
    {
        double lineLength = titleText.Length * CharWidth + 2 * EndMargin;
        double half = lineLength / 2;

        var b01 = doc.Layer(CadLayers.B01);
        var b07 = doc.Layer(CadLayers.B07);

        return
        [
            // 下横线（B-07 文字层）
            CadDraw.Line(CadDraw.P(-half, 0), CadDraw.P(half, 0), b07),
            // 上横线（B-01 粗实线层）
            CadDraw.Line(CadDraw.P(-half, LineGap), CadDraw.P(half, LineGap), b01),
            // 标题文字（上横线上方居中，锚点=下中）
            CadDraw.MText(
                titleText,
                CadDraw.P(0, LineGap + TitleGap),
                TitleHeight,
                b07,
                attachment: AttachmentPointType.BottomCenter
            ),
            // 比例文字（下横线下方、右端外侧错开 0.5，左对齐）
            CadDraw.Text(scaleText, CadDraw.P(half + 0.5, -ScaleGap), ScaleHeight, b07),
        ];
    }
}
