using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using CSMath.Extensions;
using DwgSharpKit.Blocks;
using DwgSharpKit.Drawing;
using DwgSharpKit.Standards;

namespace DwgSharpKit.Rebar;

/// <summary>
/// 箍筋的一个端头弯钩：从本体上的起钩点 <see cref="Root"/> 到钩尖 <see cref="Tip"/> 的一条直线。
/// <para><see cref="Text"/> 为该弯钩的标注文本：<c>null</c> 表示不出标注（两个钩通常只标一次），
/// 非空则按该文本标注。弯钩长度与本体环长分开算，二者相加才是整根钢筋的真实长度。</para>
/// </summary>
public readonly record struct StirrupHook(XY Root, XY Tip, string? Text = null)
{
    /// <summary>弯钩实际画出的长度（钩尖到起钩点的距离）</summary>
    public double Length
    {
        get
        {
            var dx = Tip.X - Root.X;
            var dy = Tip.Y - Root.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>弯钩方向角（弧度）</summary>
    public double Angle => Math.Atan2(Tip.Y - Root.Y, Tip.X - Root.X);
}

/// <summary>
/// 箍筋：<b>一条闭合多段线（本体）+ 两条直线（两端弯钩）</b>。
/// <para>与 <see cref="Rebar"/> 的区别：<see cref="Rebar"/> 描述一根首尾相连的开放折线，
/// 每一段都属于这根钢筋的走向；箍筋的本体是一个闭环，两个端头弯钩画在环上、但不参与环的走向，
/// 因此必须拆成「闭合多段线 + 独立直线」两类图元，不能塞进一条开放折线里。</para>
/// <para>本体的圆角用顶点 bulge 表达（90° 圆角 = <c>tan(90°/4) = 0.41421356</c>）；
/// 弯钩用 <see cref="StirrupHook"/> 直接给起钩点和钩尖，所以既能照源图 1:1 还原，
/// 也能用 <see cref="Rectangular"/> 从外包尺寸生成。</para>
/// <para>图元以 1:1 局部坐标在原点构建、不自动入文档，供 <see cref="CadDocumentExtensions.AddTranslated"/>
/// 复用放置（与 <see cref="Rebar"/>/<see cref="SymmetricRebar"/>/<see cref="AsymmetricRebar"/> 风格一致）。</para>
/// </summary>
public sealed class Stirrup
{
    // ── 编号 / 数量表信息（与 Rebar 一致）──

    /// <summary>钢筋编号（引线标注"编号"属性），默认 "N1"</summary>
    public string Number { get; set; } = "N1";

    /// <summary>钢筋等级（HRB 块名称），默认 "HRB400"</summary>
    public string Grade { get; set; } = "HRB400";

    /// <summary>钢筋直径（引线标注"类型"属性），默认 12</summary>
    public double Diameter { get; set; } = 12;

    /// <summary>根数（数量表信息，不参与绘图）</summary>
    public int Count { get; set; } = 1;

    /// <summary>摆放时是否把简单属性写入本体多段线的 XData，默认 true。见 <see cref="RebarXData"/>。</summary>
    public bool WriteXData { get; set; } = true;

    // ── 长度（真实长度 = 本体环长 + 各弯钩长）──

    /// <summary>整根真正长度；null 时按本体周长 + 弯钩长度自动计算</summary>
    public double? TrueLength { get; set; }

    /// <summary>整根替代长度（引线标注显示值）；null 时取 <see cref="Length"/> 的取整文本</summary>
    public string? SubstituteLength { get; set; }

    /// <summary>有效真正长度：<see cref="TrueLength"/> 或按几何求和</summary>
    public double Length => TrueLength ?? GeometryLength();

    /// <summary>有效替代长度文本：<see cref="SubstituteLength"/> 或真正长度取整</summary>
    public string Label => SubstituteLength ?? RebarDetail.FormatLength(Length);

    /// <summary>引线标注规格串（类型 + 长度），如 "14 L=9076"</summary>
    public string SpecLabel => $"{Diameter:0.###} L={Label}";

    // ── 几何 ──

    /// <summary>
    /// 闭合本体顶点（局部坐标，按顺序绕行，<b>不需要</b>重复首点）。
    /// 第 i 个顶点的 <see cref="RebarVertex.Text"/> 标注「i → i+1」这一段；
    /// <b>末顶点的 Text 标注闭合段</b>（i = n-1 → 0）——这一点与 <see cref="Rebar.Detail(CadDocument, RebarDetailOptions?)"/>
    /// 不同，那边的开放折线没有闭合段，末顶点 Text 被忽略。
    /// </summary>
    public IReadOnlyList<RebarVertex> Vertices { get; set; } = [];

    /// <summary>两端弯钩（两条独立直线，局部坐标）。N3-1 / N2-2 这类是同一角上的两个钩，方向通常相同。</summary>
    public IReadOnlyList<StirrupHook> Hooks { get; set; } = [];

    // ── 长度计算 ──

    /// <summary>本体周长（含闭合段，含圆角弧长，不含弯钩）</summary>
    public double Perimeter()
    {
        var n = Vertices.Count;
        if (n < 2)
        {
            return 0;
        }

        var sum = 0.0;
        for (var i = 0; i < n; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % n];
            sum += RebarDetail.SegmentLength(a.Point, b.Point, a.Bulge);
        }

        return sum;
    }

    /// <summary>几何总长 = 本体周长 + 各弯钩长度</summary>
    private double GeometryLength() => Perimeter() + Hooks.Sum(h => h.Length);

    // ── 输出 ──

    /// <summary>只画图形（本体闭合环 + 弯钩直线，不含任何标注）：1:1 局部图元，未入文档</summary>
    public List<Entity> Shape(CadDocument doc, RebarDetailOptions? options = null) =>
        BuildEntities(doc, options, showText: false);

    /// <summary>图形 + 每段标注 + 弯钩标注：1:1 局部图元，未入文档</summary>
    public List<Entity> Detail(CadDocument doc, RebarDetailOptions? options = null) =>
        BuildEntities(doc, options, showText: true);

    /// <summary>只画图形并落地：按 <paramref name="transform"/> 变换后再平移 <paramref name="origin"/>，写入文档</summary>
    public List<Entity> Shape(
        CadDocument doc,
        XYZ origin,
        RebarDetailOptions? options = null,
        Transform? transform = null
    ) => PlaceEntities(doc, origin, Shape(doc, options), transform);

    /// <summary>图形 + 标注并落地：按 <paramref name="transform"/> 变换后再平移 <paramref name="origin"/>，写入文档</summary>
    public List<Entity> Detail(
        CadDocument doc,
        XYZ origin,
        RebarDetailOptions? options = null,
        Transform? transform = null
    ) => PlaceEntities(doc, origin, Detail(doc, options), transform);

    /// <summary>
    /// 箍筋大样落地：图形 + 每段标注按 <paramref name="transform"/> 变换（默认仅平移 <paramref name="origin"/>）
    /// 写入文档，再调用现有 <see cref="LeaderAnnotationBlock"/> 画整根引线标注。
    /// 引线标注不受 transform 影响。
    /// </summary>
    public List<Entity> CreatePlacedDetail(
        CadDocument doc,
        XYZ origin,
        XYZ leaderAnchor,
        double scale = 50,
        RebarDetailOptions? options = null,
        Transform? transform = null
    )
    {
        var result = PlaceEntities(doc, origin, Detail(doc, options), transform);
        result.AddRange(LeaderAnnotationBlock.Add(doc, origin, leaderAnchor, Number, SpecLabel, scale));
        return result;
    }

    /// <summary>
    /// 兼容旧调用：按外包尺寸画矩形箍筋并加入文档。
    /// 行为已改为「闭合 4 顶点环 + 同一角上的两个 135° 弯钩」，与旧版的一条开放折线不同。
    /// </summary>
    public static List<Entity> Add(
        CadDocument doc,
        double width,
        double height,
        RebarDetailOptions? options = null
    ) => Rectangular(width, height).Detail(doc, options);

    // ── 工厂 ──

    /// <summary>
    /// 矩形箍筋的 4 个直角顶点（闭合环，左下角在原点）。
    /// 标注依次为：底边 width、右边 height、顶边 width、左边 height（左边即闭合段）。
    /// </summary>
    /// <param name="width">外包宽（沿 +X）</param>
    /// <param name="height">外包高（沿 +Y）</param>
    public static List<RebarVertex> BuildVertices(double width, double height)
    {
        var w = RebarDetail.FormatLength(width);
        var h = RebarDetail.FormatLength(height);

        return
        [
            RebarDetail.V(0, 0, w),          // 0→1 底边
            RebarDetail.V(width, 0, h),      // 1→2 右边
            RebarDetail.V(width, height, w), // 2→3 顶边
            RebarDetail.V(0, height, h),     // 3→0 左边（闭合段）
        ];
    }

    /// <summary>
    /// 矩形箍筋：闭合 4 顶点环 + 右上角两个 135° 弯钩。
    /// 两条钩的起钩点分别沿顶边、右边从角点内缩 <paramref name="hookInset"/>，再以 45° 朝内折 <paramref name="hookLength"/>；
    /// 弯钩只标第一条（与图面惯例一致：对称件标一次）。
    /// <para>画图几何与标注尺寸可以不同（源图常把宽度压扁表示意），此时传
    /// <paramref name="labelWidth"/>/<paramref name="labelHeight"/> 显式给出标注值。</para>
    /// </summary>
    /// <param name="width">画出来的外包宽</param>
    /// <param name="height">画出来的外包高</param>
    /// <param name="hookLength">弯钩长度（画出的长度，也是标注值）</param>
    /// <param name="hookInset">起钩点距角点的内缩量，0 表示从角点起钩</param>
    /// <param name="labelWidth">宽度标注值，null 取 <paramref name="width"/></param>
    /// <param name="labelHeight">高度标注值，null 取 <paramref name="height"/></param>
    public static Stirrup Rectangular(
        double width,
        double height,
        double hookLength = 60,
        double hookInset = 0,
        double? labelWidth = null,
        double? labelHeight = null
    )
    {
        var lw = RebarDetail.FormatLength(labelWidth ?? width);
        var lh = RebarDetail.FormatLength(labelHeight ?? height);

        List<RebarVertex> vertices =
        [
            RebarDetail.V(0, 0, lw),          // 0→1 底边
            RebarDetail.V(width, 0, lh),      // 1→2 右边
            RebarDetail.V(width, height, lw), // 2→3 顶边
            RebarDetail.V(0, height, lh),     // 3→0 左边（闭合段）
        ];

        // 45° 内折：两个方向各走 hookLength/√2
        var d = hookLength / Math.Sqrt(2.0);
        XY Fold(XY root) => new(root.X - d, root.Y - d);
        var topRoot = new XY(width - hookInset, height);
        var sideRoot = new XY(width, height - hookInset);

        List<StirrupHook> hooks =
        [
            new(topRoot, Fold(topRoot), RebarDetail.FormatLength(hookLength)),
            new(sideRoot, Fold(sideRoot)),     // 第二个钩不重复标注
        ];

        return new Stirrup { Vertices = vertices, Hooks = hooks };
    }

    // ── 内部实现 ──

    private List<Entity> BuildEntities(
        CadDocument doc,
        RebarDetailOptions? options,
        bool showText
    )
    {
        options ??= new RebarDetailOptions();
        var rebarLayer = options.RebarLayer ?? doc.Layer(CadLayers.B01);
        var textLayer = options.TextLayer ?? doc.Layer(CadLayers.B07);

        if (Vertices.Count < 3)
        {
            throw new InvalidOperationException("箍筋本体至少需要 3 个顶点。");
        }

        // 本体：闭合多段线（bulge 表达圆角）
        var polyline = CadDraw.Polyline(
            Vertices.Select(v => CadDraw.V(v.Point.X, v.Point.Y, v.Bulge)),
            rebarLayer,
            closed: true
        );

        var entities = new List<Entity> { polyline };

        // 弯钩：两条独立直线，始终画出（与是否标注无关）
        foreach (var hook in Hooks)
        {
            entities.Add(
                CadDraw.Line(
                    new XYZ(hook.Root.X, hook.Root.Y, 0),
                    new XYZ(hook.Tip.X, hook.Tip.Y, 0),
                    rebarLayer
                )
            );
        }

        if (!showText)
        {
            return entities;
        }

        // 本体：n 个顶点 → n 段（末段是闭合段），逐段一个标注
        for (var i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % Vertices.Count];
            var length = RebarDetail.SegmentLength(a.Point, b.Point, a.Bulge);
            var text = a.Text ?? RebarDetail.FormatLength(length * a.Scale);
            AddSegmentText(entities, text, a.Point, b.Point, options, textLayer);
        }

        // 弯钩：只有显式给了 Text 的才标
        foreach (var hook in Hooks)
        {
            if (hook.Text is null)
            {
                continue;
            }

            AddSegmentText(entities, hook.Text, hook.Root, hook.Tip, options, textLayer);
        }

        return entities;
    }

    /// <summary>在一段的中点法向偏移处放一个居中文字，方向沿该段（与 <see cref="RebarDetail.Add"/> 一致）</summary>
    private static void AddSegmentText(
        List<Entity> entities,
        string? text,
        XY a,
        XY b,
        RebarDetailOptions options,
        Layer textLayer
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var pa = new XYZ(a.X, a.Y, 0);
        var pb = new XYZ(b.X, b.Y, 0);
        var dir = (pb - pa).Normalize();
        var normal = new XYZ(-dir.Y, dir.X, 0);
        var mid = (pa + pb) / 2;

        entities.Add(
            CadDraw.Text(
                text,
                mid + normal * options.TextOffset,
                options.TextHeight,
                textLayer,
                rotation: options.KeepTextHorizontal ? 0 : dir.GetAngle(XYZ.AxisX),
                horizontalAlignment: TextHorizontalAlignment.Center,
                verticalAlignment: TextVerticalAlignmentType.Middle
            )
        );
    }

    private List<Entity> PlaceEntities(
        CadDocument doc,
        XYZ origin,
        IEnumerable<Entity> entities,
        Transform? transform
    )
    {
        var result = new List<Entity>();
        var t = transform ?? new Transform();

        foreach (var entity in entities)
        {
            entity.ApplyTransform(t);
            entity.ApplyTranslation(origin);

            if (entity is TextEntity text)
            {
                // ApplyTransform may not update AlignmentPoint for non-baseline aligned text.
                text.AlignmentPoint = text.InsertPoint;
            }

            if (WriteXData && entity is LwPolyline)
            {
                RebarXData.Write(doc, entity, ToRebar());
            }

            result.Add(entity);
        }

        return result;
    }

    /// <summary>把箍筋的属性投影成 <see cref="Rebar"/>，复用 <see cref="RebarXData"/> 的写入口径</summary>
    private Rebar ToRebar() =>
        new()
        {
            Number = Number,
            Grade = Grade,
            Diameter = Diameter,
            Count = Count,
            TrueLength = Length,
            SubstituteLength = SubstituteLength,
            Vertices = Vertices,
        };
}
