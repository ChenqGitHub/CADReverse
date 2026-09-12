using System;
using System.Collections.Generic;
using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using DwgSharpKit.Blocks;

namespace DwgSharpKit.Rebar;

/// <summary>
/// 钢筋落地变换（<see cref="Rebar.Place"/> / <see cref="Rebar.PlaceDetail"/> 的可选几何变换）：
/// 先绕 Pivot 缩放，再绕经过 Pivot 的 Z 轴旋转，最后平移（摆放点由调用方传入）。
/// Pivot 默认局部原点 (0,0,0)——钢筋以 1:1 局部坐标建在原点，默认即"绕自身缩放/旋转后整体移到摆放点"。
/// 仅作用于钢筋图形与段长标注；不改变标注文本值与 XData（长度保持 1:1 真值）；引线标注不受其影响。
/// </summary>
public sealed class PlaceTransform
{
    /// <summary>缩放比例，默认 1（不缩放）</summary>
    public double Scale = 1;

    /// <summary>旋转角（弧度，绕 Z 轴），默认 0（不旋转）</summary>
    public double Rotation = 0;

    /// <summary>缩放/旋转基准点，默认局部原点</summary>
    public XYZ? Pivot;
}

/// <summary>
/// 一根钢筋的完整描述：图形 + 标注。
/// <list type="bullet">
/// <item>钢筋图形：<see cref="Vertices"/>——一根钢筋只有一种形状，即 1:1 局部坐标多段线顶点。
/// 对称/箍筋/自定义非对称 只是生成顶点的不同方式：
/// 对称用 <see cref="SymmetricRebar.BuildVertices"/>，箍筋用 <see cref="Stirrup.BuildVertices"/>，
/// 自定义非对称直接给顶点</item>
/// <item>钢筋标注：多段线每段用单行文本标注长度（<see cref="Detail"/> 委托 <see cref="RebarDetail.Add"/>；
/// 默认按几何自动算"真正长度"，顶点 Text 覆盖即"替代长度"）</item>
/// <item>整根引线标注（编号 + "直径 L=长度"）：<see cref="PlaceDetail"/> 落地时直接调用
/// 现有 <see cref="LeaderAnnotationBlock"/>，不另造轮子</item>
/// <item>简单属性落库：<see cref="Place"/> / <see cref="PlaceDetail"/> 落地时把
/// 编号/等级/直径/根数/长度 写进多段线 XData（<see cref="RebarXData"/>，可用 <see cref="WriteXData"/> 关闭）</item>
/// </list>
/// 平时画图只要图形：<see cref="Shape"/> / <see cref="Place"/>；大样（图形 + 每段标注 + 整根引线）：
/// <see cref="Detail"/> / <see cref="PlaceDetail"/>。
/// 图元以 1:1 局部坐标在原点构建、不自动入文档，供 <see cref="DwgSharpKit.CadDocumentExtensions.AddTranslated"/>
/// 复用放置（与 SymmetricRebar/AsymmetricRebar/Stirrup 的模板风格一致）。
/// </summary>
public sealed class Rebar
{
    // ── 编号 / 数量表信息 ──

    /// <summary>钢筋编号（引线标注"编号"属性），默认 "N1"</summary>
    public string Number { get; set; } = "N1";

    /// <summary>钢筋等级（HRB 块名称），默认 "HRB400"</summary>
    public string Grade { get; set; } = "HRB400";

    /// <summary>钢筋直径（引线标注"类型"属性），默认 12</summary>
    public double Diameter { get; set; } = 12;

    /// <summary>根数（数量表信息，不参与绘图）</summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// 摆放（<see cref="Place"/> / <see cref="PlaceDetail"/>）时是否把简单属性
    /// （编号/等级/直径/根数/长度）写入钢筋多段线的 XData，默认 true。
    /// 见 <see cref="RebarXData"/>。
    /// </summary>
    public bool WriteXData { get; set; } = true;

    // ── 钢筋图形（一根钢筋只有一种形状）──

    /// <summary>1:1 局部坐标多段线顶点。对称钢筋用 <see cref="SymmetricRebar.BuildVertices"/> 生成，
    /// 箍筋用 <see cref="Stirrup.BuildVertices"/>，自定义非对称直接给顶点</summary>
    public IReadOnlyList<RebarVertex> Vertices { get; set; } = [];

    // ── 钢筋标注（真正长度 / 替代长度）──

    /// <summary>整根真正长度；null 时按 <see cref="Vertices"/> 几何自动计算</summary>
    public double? TrueLength { get; set; }

    /// <summary>整根替代长度（引线标注显示值）；null 时取 <see cref="Length"/> 的取整文本</summary>
    public string? SubstituteLength { get; set; }

    /// <summary>有效真正长度：<see cref="TrueLength"/> 或按顶点几何求和</summary>
    public double Length => TrueLength ?? GeometryLength();

    /// <summary>有效替代长度文本：<see cref="SubstituteLength"/> 或真正长度取整</summary>
    public string Label => SubstituteLength ?? RebarDetail.FormatLength(Length);

    /// <summary>引线标注规格串（类型 + 长度），如 "12 L=6936"</summary>
    public string SpecLabel => $"{Diameter:0.###} L={Label}";

    // ── 输出 ──

    /// <summary>只画钢筋图形（平时画图用）：1:1 局部图元，未入文档</summary>
    public List<Entity> Shape(CadDocument doc, RebarDetailOptions? options = null) =>
        RebarDetail.Add(doc, Vertices, WithoutText(options));

    /// <summary>钢筋图形 + 每段单行文本长度标注（大样用）：1:1 局部图元，未入文档</summary>
    public List<Entity> Detail(CadDocument doc, RebarDetailOptions? options = null) =>
        RebarDetail.Add(doc, Vertices, options);

    /// <summary>只画图形并落地：按 <paramref name="transform"/> 变换（默认仅平移 origin）后写入文档，
    /// 并把简单属性写进多段线 XData（<see cref="WriteXData"/> 可关闭）</summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="origin">摆放位置（平移向量）</param>
    /// <param name="options">绘制选项</param>
    /// <param name="transform">几何变换（缩放/旋转/平移）；默认仅平移 origin</param>
    public void Place(
        CadDocument doc,
        XYZ origin,
        RebarDetailOptions? options = null,
        PlaceTransform? transform = null
    )
    {
        var t = transform ?? new PlaceTransform();
        doc.AddTransformed(
            origin,
            Shape(doc, options),
            t.Scale,
            t.Rotation,
            t.Pivot,
            copy => WriteXDataOnCopy(doc, copy)
        );
    }

    /// <summary>
    /// 大样落地：图形 + 每段单行文本标注按 <paramref name="transform"/> 变换
    /// （默认仅平移 origin）写入文档（并把简单属性写进多段线 XData），
    /// 再调用现有 <see cref="LeaderAnnotationBlock"/> 画整根引线标注
    /// （引线尖端指向 origin，文字基准点落在 leaderAnchor，文字为 编号 + "直径 L=替代长度"）。
    /// 引线标注不受 transform 影响。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="origin">图形摆放位置（引线尖端落点）</param>
    /// <param name="leaderAnchor">引线拐点/文字基准点</param>
    /// <param name="scale">引线标注整体比例，默认 50（与图纸比例一致）</param>
    /// <param name="options">绘制选项</param>
    /// <param name="transform">钢筋几何变换（缩放/旋转/平移）；默认仅平移 origin</param>
    public void PlaceDetail(
        CadDocument doc,
        XYZ origin,
        XYZ leaderAnchor,
        double scale = 50,
        RebarDetailOptions? options = null,
        PlaceTransform? transform = null
    )
    {
        var t = transform ?? new PlaceTransform();
        doc.AddTransformed(
            origin,
            Detail(doc, options),
            t.Scale,
            t.Rotation,
            t.Pivot,
            copy => WriteXDataOnCopy(doc, copy)
        );
        LeaderAnnotationBlock.Add(doc, origin, leaderAnchor, Number, SpecLabel, scale);
    }

    /// <summary>把钢筋简单属性写入落地的多段线 XData（WriteXData 关闭或非多段线时跳过）</summary>
    private void WriteXDataOnCopy(CadDocument doc, Entity copy)
    {
        if (WriteXData && copy is LwPolyline)
        {
            RebarXData.Write(doc, copy, this);
        }
    }

    private static RebarDetailOptions WithoutText(RebarDetailOptions? options)
    {
        var o = options ?? new RebarDetailOptions();
        return new RebarDetailOptions
        {
            ShowText = false,
            TextOffset = o.TextOffset,
            TextHeight = o.TextHeight,
            RebarLayer = o.RebarLayer,
            TextLayer = o.TextLayer,
        };
    }

    /// <summary>按 Shape 多段线的各段长度求和，包含 Bulge 圆弧长度</summary>
    private double GeometryLength() => RebarDetail.PolylineLength(Vertices);
}
