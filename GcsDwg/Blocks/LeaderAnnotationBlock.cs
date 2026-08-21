using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

/// <summary>
/// 引线标注块：折线引线 + HRB 钢筋文字。
/// 1:1 局部几何：折线 (0,-8)-(0,0)-(11.316302,0)、文字置于 (5.4589,1.9065)。
/// 以基准顶点（第二个顶点，原点）为缩放 pivot，平移使基准顶点落到用户第二点，
/// 并将折线首顶点精确落到用户第一点。不旋转。
/// </summary>
public static class LeaderAnnotationBlock
{
    private const double TipY = -8; // 首顶点（引线尖端）
    private const double TailX = 11.316302; // 水平段末端
    private const double TextX = 5.4589; // 文字局部 x
    private const double TextY = 1.9065; // 文字局部 y
    private const double TextScale = 0.1; // 文字块基础缩放
    private const double TextHeight = 25 * TextScale; // 纯文本字高（2.5）

    /// <summary>
    /// 绘制引线标注。以基准顶点（第二个顶点）为缩放 pivot，按比例缩放，
    /// 平移使基准顶点落到 second，并将首顶点精确落到 first。无旋转。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="first">第一点（折线首顶点落点）</param>
    /// <param name="second">第二点（基准顶点/拐点落点、缩放 pivot 平移目标）</param>
    /// <param name="grade">钢筋编号（HRB 块）</param>
    /// <param name="spec">钢筋直径（HRB 块）</param>
    /// <param name="scale">整体缩放比例，默认 1</param>
    /// <returns>生成的折线多段线实体</returns>
    public static (LwPolyline, Insert) Add(
        CadDocument doc,
        XYZ first,
        XYZ second,
        string grade,
        string spec,
        double scale = 1
    )
    {
        // 1:1 建在原点，基准顶点在 (0,0)
        var pl = CadDraw.Polyline(
            [CadDraw.V(0, TipY), CadDraw.V(0, 0), CadDraw.V(TailX, 0)],
            doc.Layer(CadLayers.B03)
        );
        var insert = HRB400Block.Insert(doc, CadDraw.P(5.4589 * 10, 1.9065 * 10), grade, spec);

        // 缩放（绕原点=基准顶点）+ 平移（基准顶点→ second）
        if (scale != 1)
        {
            pl.ApplyScaling(new(scale, scale, 1));
            insert.ApplyScaling(new(scale / 10, scale / 10, 1));
        }
        pl.ApplyTranslation(second);
        insert.ApplyTranslation(second);

        // 折线首顶点（尖端）精确落到第一点
        pl.Vertices[0].Location = new XY(first.X, first.Y);

        doc.AddEntities<Entity>([pl, insert]);
        return (pl, insert);
    }

    /// <summary>
    /// 绘制引线 + 纯文本（不用 HRB 块，直接画文本）。
    /// 折线引线同完整版：1:1 建在原点、绕基准顶点缩放、平移使基准顶点落到 second、
    /// 首顶点精确落到 first。文字位置同 <see cref="Add(CadDocument, XYZ, XYZ, string, string, double)"/>，
    /// 相对基准顶点按比例缩放，再平移到 second。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="first">第一点（引线尖端落点）</param>
    /// <param name="second">第二点（基准顶点/拐点落点）</param>
    /// <param name="text">文字内容</param>
    /// <param name="scale">整体缩放比例，默认 1</param>
    /// <returns>引线折线多段线实体</returns>
    public static LwPolyline Add(
        CadDocument doc,
        XYZ first,
        XYZ second,
        string text,
        double scale = 1
    )
    {
        // 1:1 建在原点，基准顶点在 (0,0)
        var pl = CadDraw.Polyline(
            [CadDraw.V(0, TipY), CadDraw.V(0, 0), CadDraw.V(TailX, 0)],
            doc.Layer(CadLayers.B03)
        );

        // 缩放（绕原点=基准顶点）+ 平移（基准顶点→ second）
        if (scale != 1)
        {
            pl.ApplyScaling(new(scale, scale, 1));
        }
        pl.ApplyTranslation(second);

        // 引线尖端精确落到第一点
        pl.Vertices[0].Location = new XY(first.X, first.Y);

        // 纯文本：相对基准顶点按比例缩放 + 平移（与完整版文字位置一致）
        var textPos = CadDraw.P(
            TextX * scale / 10 + second.X,
            TextY * scale / 10 + second.Y,
            second.Z
        );
        var entity = CadDraw.Text(text, textPos, TextHeight * scale, doc.Layer(CadLayers.B07));

        doc.Entities.Add(pl);
        doc.Entities.Add(entity);
        return pl;
    }
}
