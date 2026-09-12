using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using DwgSharpKit.Standards;

namespace DwgSharpKit.Rebar;

/// <summary>
/// 钢筋大样的一个多段线顶点（仿 <see cref="DwgSharpKit.CadDraw"/> 的顶点参数）。
/// Point 为该顶点在 1:1 局部坐标下的位置；相邻两顶点即构成一段线。
/// Text 为该段（本顶点 → 下一顶点）的标注文本覆盖；null 时自动采用该段 1:1 长度；
/// 末顶点的 Text 忽略。
/// </summary>
public readonly record struct RebarVertex(
    XY Point,
    double Bulge = 0,
    string? Text = null,
    double Scale = 1
);

/// <summary>
/// 钢筋大样绘制参数。
/// </summary>
public sealed class RebarDetailOptions
{
    /// <summary>是否显示每段文本标注，默认 true</summary>
    public bool ShowText = true;

    /// <summary>文本相对各段中点的垂直偏移（沿段法向）</summary>
    public double TextOffset = 120;

    /// <summary>标注字高</summary>
    public double TextHeight = 2.5;

    /// <summary>钢筋图层，默认 B-01</summary>
    public Layer? RebarLayer;

    /// <summary>文本图层，默认 B-07</summary>
    public Layer? TextLayer;
}

/// <summary>
/// 钢筋大样公共底层。管线：
/// 1. 全部图元（折线 + 每段文本标注）以 1:1 局部坐标在原点构建（本类只负责这一步）；
/// 2. 落地时对同一组图元做 缩放(绕 pivot)→旋转(绕 Z 轴)→平移（<see cref="Rebar.Place"/> /
///    <see cref="Rebar.PlaceDetail"/> 经 <see cref="DwgSharpKit.CadDocumentExtensions.AddTransformed"/> 完成）；
/// 3. 整组写入文档。
/// 对称线筋/非对称线筋/箍筋 均基于此。
/// </summary>
public static class RebarDetail
{
    /// <summary>
    /// 便捷构造一个不带标注覆盖文本的顶点（仿 <see cref="CadDraw.V"/>）。
    /// 自动标注使用实际距离乘以 <paramref name="scale"/>；默认比例为 1。
    /// </summary>
    public static RebarVertex V(double x, double y, double scale = 1, double bulge = 0) =>
        new(new XY(x, y), bulge, null, scale);

    /// <summary>
    /// 便捷构造一个带显式标注文本的顶点。使用此重载时不设置距离比例。
    /// </summary>
    public static RebarVertex V(double x, double y, string text, double bulge = 0) =>
        new(new XY(x, y), bulge, text, 1);

    /// <summary>
    /// 按顶点序列构建钢筋大样图元，**返回但不写入文档**，供使用者自行添加或
    /// 用 <see cref="DwgSharpKit.CadDocumentExtensions.AddTranslated"/> 复用。
    /// 图元以 1:1 局部坐标在原点构建（含折线 + 每段文本标注）。
    /// 每段文本 = 起点顶点的 Text，为空用该段 1:1 长度。
    /// </summary>
    /// <param name="doc">CAD 文档（用于解析图层）</param>
    /// <param name="vertices">1:1 局部坐标顶点序列</param>
    /// <param name="options">绘制选项</param>
    /// <returns>大样图元（1:1 本地坐标，未入文档）</returns>
    public static List<Entity> Add(
        CadDocument doc,
        IReadOnlyList<RebarVertex> vertices,
        RebarDetailOptions? options = null
    )
    {
        options ??= new RebarDetailOptions();
        var rebarLayer = options.RebarLayer ?? doc.Layer(CadLayers.B01);
        var textLayer = options.TextLayer ?? doc.Layer(CadLayers.B07);

        var pl = CadDraw.Polyline(
            vertices.Select(v => CadDraw.V(v.Point.X, v.Point.Y, v.Bulge)),
            rebarLayer
        );
        var entities = new List<Entity> { pl };

        if (options.ShowText)
        {
            for (int i = 0; i + 1 < vertices.Count; i++)
            {
                var a = new XYZ(vertices[i].Point.X, vertices[i].Point.Y, 0);
                var b = new XYZ(vertices[i + 1].Point.X, vertices[i + 1].Point.Y, 0);

                // 两点距离只是弦长；有 Bulge 时，标注应使用该段圆弧的真实长度。
                double len = SegmentLength(a, b, vertices[i].Bulge);
                string text = vertices[i].Text ?? FormatLength(len * vertices[i].Scale);

                var dir = (b - a).Normalize();
                var normal = new XYZ(-dir.Y, dir.X, 0);
                var mid = (a + b) / 2;

                entities.Add(
                    CadDraw.Text(
                        text,
                        mid + normal * options.TextOffset,
                        options.TextHeight,
                        textLayer,
                        rotation:dir.GetAngle( XYZ.AxisX),
                        horizontalAlignment: TextHorizontalAlignment.Center,
                        verticalAlignment: TextVerticalAlignmentType.Middle
                    )
                );
            }
        }

        return entities;
    }



    private static double SegmentLength(XYZ a, XYZ b, double bulge)
    {
        return SegmentLength(
            new XY(a.X, a.Y),
            new XY(b.X, b.Y),
            bulge
        );
    }

    internal static double PolylineLength(IReadOnlyList<RebarVertex> vertices)
    {
        double sum = 0;
        for (int i = 0; i + 1 < vertices.Count; i++)
        {
            sum += SegmentLength(vertices[i].Point, vertices[i + 1].Point, vertices[i].Bulge);
        }

        return sum;
    }

    private static double SegmentLength(XY a, XY b, double bulge)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double chord = Math.Sqrt(dx * dx + dy * dy);
        double absoluteBulge = Math.Abs(bulge);

        if (chord == 0 || absoluteBulge < 1e-12)
        {
            return chord;
        }

        // bulge = tan(includedAngle / 4), and arcLength = radius * includedAngle.
        return chord
            * (1 + bulge * bulge)
            * Math.Abs(Math.Atan(bulge))
            / absoluteBulge;
    }

    internal static string FormatLength(double len) =>
        Math.Round(len, 0).ToString(CultureInfo.InvariantCulture);
}
