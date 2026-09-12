using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Rebar;
using DwgSharpKit.Standards;
using DwgSharpKit.Tables;

namespace CadCli.Generated;

public static partial class GeneratedDraw
{
    // ────────────────────────────────────────────────────────────────
    // 框架桥钢筋图 —— 参数化大样复现（N0~N17）
    //
    // 每根钢筋用 DwgSharpKit.Rebar + PlaceDetail 落地：
    //     Vertices = [RebarDetail.V(...), ...]
    //     rebar.PlaceDetail(doc, 摆放点, 引线点, scale: 10, options);
    // PlaceDetail 自动完成：形状多段线 + 每段标注 + 引线标注(编号/直径/L=公式) + XData。
    // 端部 R350 圆角直接写在顶点 Bulge 上（RebarDetail.Add 会渲染凸度），
    // 不再用单独的 CadDraw.Arc 补画。
    // 双参数族：FrameTopSlabThickness 族（断面 Ⅲ-d+300，斜段投影 304）与 FrameBottomSlabThickness 族（断面 FrameBottomSlabThickness-d+100，斜段投影 320）。
    // 图中所有英文字母(α / FrameTopSlabThickness / FrameBottomSlabThickness / d / SideCoverThickness)均为参数，先给定假定值。
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 90°圆弧对应的多段线凸度，计算方式为 tan(90° / 4)
    /// </summary>
    private static readonly double Bulge90Degrees = Math.Tan(Math.PI / 8);

    /// <summary>
    /// 45°圆弧对应的多段线凸度，计算方式为 tan(45° / 4)
    /// </summary>
    private static readonly double Bulge45Degrees = Math.Tan(Math.PI / 16);

    private static readonly double Sin45 = Math.Sin(Math.PI / 4);

    private const double d = 28; // 钢筋直径

    /// <summary>
    /// 左右两侧保护层厚（cm）
    /// </summary>
    private const double SideCoverThickness = 5; // 左右两侧保护层厚（cm）

    /// <summary>
    /// 上下顶底板保护层厚（cm）
    /// </summary>
    private const double TopBottomCoverThickness = 5; // 上下顶底板保护层厚（cm）

    /// <summary>
    /// 顶板钢筋下弯段竖向投影长度 = FrameTopSlabThickness - 2 * TopBottomCoverThickness
    /// </summary>
    private static readonly double TopSlabRebarDrop =
        FrameTopSlabThickness - 2 * TopBottomCoverThickness;

    /// <summary>
    /// 底板钢筋下弯段竖向投影长度 = FrameBottomSlabThickness - 2 * TopBottomCoverThickness
    /// </summary>
    private static readonly double BottomSlabRebarDrop =
        FrameBottomSlabThickness - 2 * TopBottomCoverThickness;

    /// <summary>
    /// 顶板钢筋立腿总长 = FrameTopSlabThickness - TopBottomCoverThickness + 300
    /// </summary>
    private static readonly double TopSlabRebarLegTotal =
        FrameTopSlabThickness - TopBottomCoverThickness + 30;

    /// <summary>
    /// 底板钢筋立腿总长 = FrameBottomSlabThickness - TopBottomCoverThickness + 100
    /// </summary>
    private static readonly double BottomSlabRebarLegTotal =
        FrameBottomSlabThickness - TopBottomCoverThickness + 10;

    private const double EndHorizontalRun = 32.8; // 端部水平段
    private const double FilletR = 35; // 端部圆角 R350
    private static readonly double Fillet45Dx = Math.Sqrt(FilletR * FilletR / 2);
    private static readonly double Fillet45Dy = FilletR - Fillet45Dx;

    private const double EndStraightLeg = 32.8;

    private const double TextH = 25; // 标注字高
    private const double DimOffset = 20; // 尺寸文字相对线偏移

    // 框架桥结构图边框参数。
    /// <summary>
    /// 钢筋与边框之间的附加间距
    /// </summary>
    private const double FrameRebarSpacing = 3;

    /// <summary>
    /// 框架侧壁厚度
    /// </summary>
    private const double FrameSideWallThickness = 100;

    /// <summary>
    /// 框架顶板厚度
    /// </summary>
    private const double FrameTopSlabThickness = 100;

    /// <summary>
    /// 框架底板厚度
    /// </summary>
    private const double FrameBottomSlabThickness = 120;

    /// <summary>
    /// 框架结构图横向宽度
    /// </summary>
    private const double FrameWidth = 1600;

    /// <summary>
    /// 框架结构图竖向高度
    /// </summary>
    private const double FrameHeight = 870;

    /// <summary>
    /// 侧壁与水平线的夹角，单位为度
    /// </summary>
    private const double FrameAngle = 39.9;

    /// <summary>
    /// 框架结构图的绘制比例，1:10
    /// </summary>
    private const double DetailUnit = 10;

    /// <summary>
    /// 夹角的正弦值
    /// </summary>
    private static readonly double FrameAngleSin = Math.Sin(FrameAngle * Math.PI / 180);

    /// <summary>
    /// 框架结构图在斜墙方向的半宽度
    /// </summary>
    private static readonly double FrameHalfWidthOnSlope = FrameWidth / FrameAngleSin / 2;

    /// <summary>
    /// 顶板倒角竖向高度
    /// </summary>
    private const double FrameTopChamferHeight = 60;

    /// <summary>
    /// 底板倒角竖向高度
    /// </summary>
    private const double FrameBottomChamferHeight = 30;

    /// <summary>
    /// 顶板倒角水平长度
    /// </summary>
    private const double FrameTopChamferLength = 150;

    /// <summary>
    /// 底板倒角水平长度
    /// </summary>
    private const double FrameBottomChamferLength = 30;

    /// <summary>
    /// 外侧钢筋在斜墙方向的横向位置
    /// </summary>
    private static readonly double FrameOuterRebarX =
        FrameHalfWidthOnSlope - SideCoverThickness - FrameRebarSpacing;

    /// <summary>
    /// 内侧钢筋在斜墙方向的横向位置
    /// </summary>
    private static readonly double FrameInnerRebarX =
        (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin
        + SideCoverThickness
        + FrameRebarSpacing;

    /// <summary>
    /// N20 钢筋在斜墙方向的插入位置
    /// </summary>
    private static readonly double FrameInnerRebarPlacementX =
        (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin + SideCoverThickness;

    /// <summary>
    /// 框架主体中部相对结构图原点的竖向中心位置
    /// </summary>
    private static readonly double FrameBodyCenterY =
        (FrameHeight - FrameTopSlabThickness - FrameBottomSlabThickness) / 2
        + FrameTopSlabThickness;

    /// <summary>
    /// N10、N12~N17 钢筋在底板方向的插入位置
    /// </summary>
    private static readonly double FrameBottomRebarPlacementY =
        -FrameHeight + FrameBottomSlabThickness - SideCoverThickness;

    /// <summary>
    /// 一个实体的等间距阵列规格。
    /// </summary>
    private sealed record EntityArrayPattern(
        Entity Template,
        double StepX,
        double StepY,
        int Count
    );

    /// <summary>
    /// 框架桥钢筋图（参数化大样，N0~N17）。
    /// </summary>
    public static void DrawFrameBridgeReinforcement(CadDocument doc)
    {
        // 图题
        TitleBlock.Add(doc, "框架桥钢筋大样图", "", CadDraw.P(0, 48000), 20);

        var options = new RebarDetailOptions
        {
            TextHeight = TextH,
            TextOffset = DimOffset,
            RebarLayer = doc.Layer(CadLayers.B01),
            TextLayer = doc.Layer(CadLayers.B07),
        };

        Rebar n1,
            n2,
            n3,
            n4,
            n5,
            n6,
            n7,
            n8,
            n9,
            n10,
            n11,
            n12,
            n13,
            n14,
            n15,
            n16,
            n17,
            n18,
            n19,
            n20,
            n0;

        // N0：直线筋（顶板上层），8N0
        {
            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarLegTotal + FilletR;
            n0 = new Rebar
            {
                Number = "8N0",
                Count = 8,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, -y, DetailUnit),
                    RebarDetail.V(-x, -y, DetailUnit),
                    RebarDetail.V(-x, -FilletR, DetailUnit, -Bulge90Degrees),
                    RebarDetail.V(-x + FilletR, 0, DetailUnit),
                    RebarDetail.V(x - FilletR, 0, DetailUnit, -Bulge90Degrees),
                    RebarDetail.V(x, -FilletR, DetailUnit),
                    RebarDetail.V(x, -y, DetailUnit),
                    RebarDetail.V(x - EndHorizontalRun, -y, DetailUnit),
                ],
            };
            n0.SubstituteLength = $"{n0.Length * DetailUnit:0.0}";
            n0.PlaceDetail(doc, new XYZ(0, 43000, 0), new XYZ(0, 42900, 0), scale: 10, options);
        }

        // N1：下折筋（顶板外层下折至内层，两端下弯）。图中每段文本 = 该段 1:1 实际线长，
        // 大样按 1:10 建模，故顶点用 DetailV 换算坐标、标注文本仍写实际长度：
        //   端部水平段 328、立腿 FrameTopSlabThickness-TopBottomCoverThickness+300（其中直线段 = 立腿总长 - R350）、顶部平段 8143、
        //   斜段两侧短平段 275、斜段 (FrameTopSlabThickness-2p)/sin45°-304、中部下平段 FrameWidth/FrameAngleSin/sinα-2c-2(FrameTopSlabThickness-2p)-17666。
        // 顶点坐标不再逐个建变量，而是在 Vertices 里按尺寸链内联：x 自端部立腿起向右累加、
        // y 自端部水平段起向上累加；斜段两端各一个 R350 的 45° 过渡圆角（水平投影 R·sinα、
        // 竖向投影 R(1-cosα)），中部下平段跨对称轴、左端即 -mid/2。
        {
            // 小弧线
            var leftmid = 814.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop - TopSlabRebarLegTotal - FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (TopSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n1 = new Rebar
            {
                Number = "2N1",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y + TopSlabRebarLegTotal, DetailUnit, -Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, TopSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        TopSlabRebarDrop,
                        DetailUnit,
                        -Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, TopSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, TopSlabRebarDrop, DetailUnit, -Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y + TopSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n1.SubstituteLength = $"{n1.Length * DetailUnit:0.0}";
            n1.PlaceDetail(doc, new XYZ(0, 42000, 0), new XYZ(0, 41900, 0), scale: 10, options);
        }

        // N2：下折筋，形状与 N1 相同（顶板外层下折至内层，两端下弯），
        // 只是顶部平段 7643、中部下平段 FrameWidth/FrameAngleSin/sinα-2c-2(FrameTopSlabThickness-2p)-16666。
        // 图中每段文本 = 该段 1:1 实际线长，顶点坐标同样按尺寸链直接内联。
        {
            // 小弧线
            var leftmid = 764.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop - TopSlabRebarLegTotal - FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (TopSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n2 = new Rebar
            {
                Number = "2N2",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y + TopSlabRebarLegTotal, DetailUnit, -Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, TopSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        TopSlabRebarDrop,
                        DetailUnit,
                        -Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, TopSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, TopSlabRebarDrop, DetailUnit, -Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y + TopSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n2.SubstituteLength = $"{n2.Length * DetailUnit:0.0}";
            n2.PlaceDetail(doc, new XYZ(0, 41000, 0), new XYZ(0, 40900, 0), scale: 10, options);
        }

        // N3：下折筋。各段长度和坐标由 FrameTopSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            // 小弧线
            var leftmid = 624.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop - TopSlabRebarLegTotal - FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (TopSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n3 = new Rebar
            {
                Number = "2N3",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y + TopSlabRebarLegTotal, DetailUnit, -Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, TopSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        TopSlabRebarDrop,
                        DetailUnit,
                        -Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, TopSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, TopSlabRebarDrop, DetailUnit, -Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y + TopSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n3.SubstituteLength = $"{n3.Length * DetailUnit:0.0}";
            n3.PlaceDetail(doc, new XYZ(0, 40000, 0), new XYZ(0, 39900, 0), scale: 10, options);
        }

        // N4：下折筋。各段长度和坐标由 FrameTopSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            // 小弧线
            var leftmid = 544.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop - TopSlabRebarLegTotal - FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (TopSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n4 = new Rebar
            {
                Number = "2N4",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y + TopSlabRebarLegTotal, DetailUnit, -Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, TopSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        TopSlabRebarDrop,
                        DetailUnit,
                        -Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, TopSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, TopSlabRebarDrop, DetailUnit, -Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y + TopSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n4.SubstituteLength = $"{n4.Length * DetailUnit:0.0}";
            n4.PlaceDetail(doc, new XYZ(0, 39000, 0), new XYZ(0, 38900, 0), scale: 10, options);
        }

        // N5：下折筋。各段长度和坐标由 FrameTopSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            // 小弧线
            var leftmid = 484.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop - TopSlabRebarLegTotal - FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (TopSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n5 = new Rebar
            {
                Number = "2N5",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y + TopSlabRebarLegTotal, DetailUnit, -Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, TopSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        TopSlabRebarDrop,
                        DetailUnit,
                        -Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, TopSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, TopSlabRebarDrop, DetailUnit, -Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y + TopSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n5.SubstituteLength = $"{n5.Length * DetailUnit:0.0}";
            n5.PlaceDetail(doc, new XYZ(0, 38000, 0), new XYZ(0, 37900, 0), scale: 10, options);
        }

        // N6：下折筋。两端竖段为 328，顶部平段为 4693；其余长度和坐标由
        // FrameTopSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            // 小弧线
            var leftmid = 814.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop;
            var x1 = x - leftmid - Fillet45Dx - (TopSlabRebarDrop - 2 * Fillet45Dy) - Fillet45Dx;

            n6 = new Rebar
            {
                Number = "2N6",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x, y - EndStraightLeg, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x + leftmid, TopSlabRebarDrop, DetailUnit, -Bulge45Degrees), // 顶部平段
                    RebarDetail.V(
                        -x + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - leftmid, TopSlabRebarDrop, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit), // 端部水平段
                    RebarDetail.V(x, y - EndStraightLeg, DetailUnit), // 端部水平段
                ],
            };
            n6.SubstituteLength = $"{n6.Length * DetailUnit:0.0}";
            n6.PlaceDetail(doc, new XYZ(0, 37000, 0), new XYZ(0, 36900, 0), scale: 10, options);
        }

        // N7：下折筋。两端竖段为 328，顶部平段为 4193；其余长度和坐标参数化计算。
        {
            // 小弧线
            var leftmid = 814.3;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = TopSlabRebarDrop;
            var x1 = x - leftmid - Fillet45Dx - (TopSlabRebarDrop - 2 * Fillet45Dy) - Fillet45Dx;
            n7 = new Rebar
            {
                Number = "2N7",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x, y - EndStraightLeg, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x + leftmid, TopSlabRebarDrop, DetailUnit, -Bulge45Degrees), // 顶部平段
                    RebarDetail.V(
                        -x + leftmid + Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, Fillet45Dy, DetailUnit, Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - leftmid - Fillet45Dx,
                        TopSlabRebarDrop - Fillet45Dy,
                        DetailUnit,
                        -Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - leftmid, TopSlabRebarDrop, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit), // 端部水平段
                    RebarDetail.V(x, y - EndStraightLeg, DetailUnit), // 端部水平段
                ],
            };
            n7.SubstituteLength = $"{n7.Length * DetailUnit:0.0}";
            n7.PlaceDetail(doc, new XYZ(0, 36000, 0), new XYZ(0, 35900, 0), scale: 10, options);
        }

        // N8：直线筋（2N8），水平净跨 = FrameWidth/FrameAngleSin/sinα-2c，两端竖段各 328。
        {
            var x = FrameHalfWidthOnSlope - SideCoverThickness;

            n8 = new Rebar
            {
                Number = "2N8",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x, -EndStraightLeg, DetailUnit),
                    RebarDetail.V(-x, 0, DetailUnit),
                    RebarDetail.V(x, 0, DetailUnit),
                    RebarDetail.V(x, -EndStraightLeg, DetailUnit),
                ],
            };
            n8.SubstituteLength = $"{n8.Length * DetailUnit:0.0}";
            n8.PlaceDetail(doc, new XYZ(0, 35000, 0), new XYZ(0, 34900, 0), scale: 10, options);
        }

        // N9：直线筋（2N9），水平净跨 = FrameWidth/FrameAngleSin/sinα-2c，两端竖段各 328。
        {
            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            n9 = new Rebar
            {
                Number = "2N9",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x, -EndStraightLeg, DetailUnit),
                    RebarDetail.V(-x, 0, DetailUnit),
                    RebarDetail.V(x, 0, DetailUnit),
                    RebarDetail.V(x, -EndStraightLeg, DetailUnit),
                ],
            };
            n9.SubstituteLength = $"{n9.Length * DetailUnit:0.0}";
            n9.PlaceDetail(doc, new XYZ(0, 34000, 0), new XYZ(0, 33900, 0), scale: 10, options);
        }

        // N10：直线筋（16N10），水平净跨 = FrameWidth/FrameAngleSin/sinα-2c，两端竖段各 328。
        {
            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            n10 = new Rebar
            {
                Number = "16N10",
                Count = 16,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x, EndStraightLeg, DetailUnit),
                    RebarDetail.V(-x, 0, DetailUnit),
                    RebarDetail.V(x, 0, DetailUnit),
                    RebarDetail.V(x, EndStraightLeg, DetailUnit),
                ],
            };
            n10.SubstituteLength = $"{n10.Length * DetailUnit:0.0}";
            n10.PlaceDetail(doc, new XYZ(0, 33000, 0), new XYZ(0, 32900, 0), scale: 10, options);
        }

        // N11：FrameBottomSlabThickness 底板族 U 形筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = BottomSlabRebarLegTotal + FilletR;

            n11 = new Rebar
            {
                Number = "8N11",
                Count = 8,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit),
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, FilletR, DetailUnit, Bulge90Degrees),
                    RebarDetail.V(-x + FilletR, 0, DetailUnit),
                    RebarDetail.V(x - FilletR, 0, DetailUnit, Bulge90Degrees),
                    RebarDetail.V(x, FilletR, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    RebarDetail.V(x - EndHorizontalRun, y, DetailUnit),
                ],
            };
            n11.SubstituteLength = $"{n11.Length * DetailUnit:0.0}";
            n11.PlaceDetail(doc, new XYZ(0, 32000, 0), new XYZ(0, 31900, 0), scale: 10, options);
        }

        // N12：底板族下折筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var leftmid = 604.5;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = -BottomSlabRebarDrop + BottomSlabRebarLegTotal + FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (BottomSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;
            n12 = new Rebar
            {
                Number = "2N12",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y - BottomSlabRebarLegTotal, DetailUnit, Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, -BottomSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        -BottomSlabRebarDrop,
                        DetailUnit,
                        Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, -Fillet45Dy, DetailUnit, -Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, -Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, -Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit,
                        Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, -BottomSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, -BottomSlabRebarDrop, DetailUnit, Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y - BottomSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n12.SubstituteLength = $"{n12.Length * DetailUnit:0.0}";
            n12.PlaceDetail(doc, new XYZ(0, 31000, 0), new XYZ(0, 30900, 0), scale: 10, options);
        }

        // N13：底板族下折筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var leftmid = 524.5;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = -BottomSlabRebarDrop + BottomSlabRebarLegTotal + FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (BottomSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n13 = new Rebar
            {
                Number = "2N13",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y - BottomSlabRebarLegTotal, DetailUnit, Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, -BottomSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        -BottomSlabRebarDrop,
                        DetailUnit,
                        Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, -Fillet45Dy, DetailUnit, -Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, -Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, -Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit,
                        Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, -BottomSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, -BottomSlabRebarDrop, DetailUnit, Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y - BottomSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n13.SubstituteLength = $"{n13.Length * DetailUnit:0.0}";
            n13.PlaceDetail(doc, new XYZ(0, 30000, 0), new XYZ(0, 29900, 0), scale: 10, options);
        }

        // N14：底板族下折筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var leftmid = 454.5;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = -BottomSlabRebarDrop + BottomSlabRebarLegTotal + FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (BottomSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n14 = new Rebar
            {
                Number = "2N14",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y - BottomSlabRebarLegTotal, DetailUnit, Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, -BottomSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        -BottomSlabRebarDrop,
                        DetailUnit,
                        Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, -Fillet45Dy, DetailUnit, -Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, -Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, -Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit,
                        Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, -BottomSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, -BottomSlabRebarDrop, DetailUnit, Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y - BottomSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n14.SubstituteLength = $"{n14.Length * DetailUnit:0.0}";
            n14.PlaceDetail(doc, new XYZ(0, 29000, 0), new XYZ(0, 28900, 0), scale: 10, options);
        }

        // N15：底板族下折筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var leftmid = 394.5;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = -BottomSlabRebarDrop + BottomSlabRebarLegTotal + FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (BottomSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n15 = new Rebar
            {
                Number = "2N15",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y - BottomSlabRebarLegTotal, DetailUnit, Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, -BottomSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        -BottomSlabRebarDrop,
                        DetailUnit,
                        Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, -Fillet45Dy, DetailUnit, -Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, -Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, -Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit,
                        Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, -BottomSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, -BottomSlabRebarDrop, DetailUnit, Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y - BottomSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n15.SubstituteLength = $"{n15.Length * DetailUnit:0.0}";
            n15.PlaceDetail(doc, new XYZ(0, 28000, 0), new XYZ(0, 27900, 0), scale: 10, options);
        }

        // N16：底板族下折筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var leftmid = 344.5;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = -BottomSlabRebarDrop + BottomSlabRebarLegTotal + FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (BottomSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n16 = new Rebar
            {
                Number = "2N16",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y - BottomSlabRebarLegTotal, DetailUnit, Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, -BottomSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        -BottomSlabRebarDrop,
                        DetailUnit,
                        Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, -Fillet45Dy, DetailUnit, -Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, -Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, -Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit,
                        Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, -BottomSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, -BottomSlabRebarDrop, DetailUnit, Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y - BottomSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n16.SubstituteLength = $"{n16.Length * DetailUnit:0.0}";
            n16.PlaceDetail(doc, new XYZ(0, 27000, 0), new XYZ(0, 26900, 0), scale: 10, options);
        }

        // N17：底板族下折筋。长度和坐标由 FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、FrameWidth / FrameAngleSin、α 及图示构造尺寸计算。
        {
            var leftmid = 294.5;

            var x = FrameHalfWidthOnSlope - SideCoverThickness;
            var y = -BottomSlabRebarDrop + BottomSlabRebarLegTotal + FilletR;
            var x1 =
                x
                - FilletR
                - leftmid
                - Fillet45Dx
                - (BottomSlabRebarDrop - 2 * Fillet45Dy)
                - Fillet45Dx;

            n17 = new Rebar
            {
                Number = "2N17",
                Count = 2,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(-x + EndHorizontalRun, y, DetailUnit), // 端部水平段
                    RebarDetail.V(-x, y, DetailUnit),
                    RebarDetail.V(-x, y - BottomSlabRebarLegTotal, DetailUnit, Bulge90Degrees), // 立腿直线段
                    RebarDetail.V(-x + FilletR, -BottomSlabRebarDrop, DetailUnit), // 端部 R350 圆角
                    RebarDetail.V(
                        -x + FilletR + leftmid,
                        -BottomSlabRebarDrop,
                        DetailUnit,
                        Bulge45Degrees
                    ), // 顶部平段
                    RebarDetail.V(
                        -x + FilletR + leftmid + Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit
                    ), // 顶部短平段
                    // 斜段下圆角（45°）
                    RebarDetail.V(-x1 - Fillet45Dx, -Fillet45Dy, DetailUnit, -Bulge45Degrees),
                    // 中部短平段
                    RebarDetail.V(-x1, 0, DetailUnit),
                    // ── 以下为右半侧镜像 ──
                    // 中部短平段
                    RebarDetail.V(x1, 0, DetailUnit, -Bulge45Degrees),
                    // 斜段下圆角（45°）
                    RebarDetail.V(x1 + Fillet45Dx, -Fillet45Dy, DetailUnit),
                    // 顶部短平段
                    RebarDetail.V(
                        x - FilletR - leftmid - Fillet45Dx,
                        -BottomSlabRebarDrop + Fillet45Dy,
                        DetailUnit,
                        Bulge45Degrees
                    ),
                    // 顶部平段
                    RebarDetail.V(x - FilletR - leftmid, -BottomSlabRebarDrop, DetailUnit),
                    // 端部 R350 圆角
                    RebarDetail.V(x - FilletR, -BottomSlabRebarDrop, DetailUnit, Bulge90Degrees),
                    // 立腿直线段
                    RebarDetail.V(x, y - BottomSlabRebarLegTotal, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    // 端部水平段
                    RebarDetail.V(x - EndHorizontalRun, y),
                ],
            };
            n17.SubstituteLength = $"{n17.Length * DetailUnit:0.0}";
            n17.PlaceDetail(doc, new XYZ(0, 26000, 0), new XYZ(0, 25900, 0), scale: 10, options);
        }

        // N18：侧墙竖向钢筋。长度和坐标由 a、H、FrameBottomSlabThickness、TopBottomCoverThickness、SideCoverThickness、α 及图示尺寸计算。
        {
            double x = FrameSideWallThickness / Sin45 - SideCoverThickness - 25 + FilletR;
            double y = FrameHeight / 2 - TopBottomCoverThickness;

            n18 = new Rebar
            {
                Number = "16N18",
                Count = 16,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(x, y - EndStraightLeg, DetailUnit),
                    RebarDetail.V(x, y, DetailUnit),
                    RebarDetail.V(FilletR, y, DetailUnit, bulge: Bulge90Degrees),
                    RebarDetail.V(0, y - FilletR, DetailUnit),
                    RebarDetail.V(0, -y + FilletR, DetailUnit, bulge: Bulge90Degrees),
                    RebarDetail.V(FilletR, -y, DetailUnit),
                    RebarDetail.V(x, -y, DetailUnit),
                    RebarDetail.V(x, -y + EndStraightLeg, DetailUnit),
                ],
            };

            n18.SubstituteLength = $"{n18.Length * DetailUnit:0.0}";
            n18.PlaceDetail(
                doc,
                new XYZ(-1000, 25000, 0),
                new XYZ(-980, 24900, 0),
                scale: 10,
                options
            );
        }

        // N19：竖向直筋。图形坐标按框架主视图 1:10，长度按实际毫米计算。
        {
            double y = FrameHeight / 2 - TopBottomCoverThickness;

            n19 = new Rebar
            {
                Number = "24N19",
                Count = 24,
                Diameter = d,
                Vertices =
                [
                    RebarDetail.V(EndStraightLeg, y, DetailUnit),
                    RebarDetail.V(0, y, DetailUnit),
                    RebarDetail.V(0, -y, DetailUnit),
                    RebarDetail.V(EndStraightLeg, -y, DetailUnit),
                ],
            };
            n19.SubstituteLength = $"{n19.Length * DetailUnit:0.0}";
            n19.PlaceDetail(doc, new XYZ(0, 25000, 0), new XYZ(20, 24900, 0), scale: 10, options);
        }

        // N20：竖向直筋。图形坐标按框架主视图 1:10，长度按实际毫米计算。
        {
            double y = FrameHeight / 2 - TopBottomCoverThickness;

            n20 = new Rebar
            {
                Number = "16N20",
                Count = 16,
                Diameter = 16,
                Vertices =
                [
                    RebarDetail.V(EndStraightLeg, y, DetailUnit),
                    RebarDetail.V(0, y, DetailUnit),
                    RebarDetail.V(0, -y, DetailUnit),
                    RebarDetail.V(EndStraightLeg, -y, DetailUnit),
                ],
            };
            n20.SubstituteLength = $"{n20.Length * DetailUnit:0.0}";
            n20.PlaceDetail(
                doc,
                new XYZ(1000, 25000, 0),
                new XYZ(1020, 24900, 0),
                scale: 10,
                options
            );
        }

        #region  框架桥结构
        CreatFrame(doc);

        // 将 N1~N10 无标注钢筋统一插入框架桥结构图指定位置。
        n0.Place(doc, new XYZ(0, -SideCoverThickness, 0));
        n1.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n2.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n3.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n4.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n5.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n6.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n7.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n8.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n9.Place(doc, new XYZ(0, -(FrameTopSlabThickness - SideCoverThickness), 0));
        n10.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));
        n11.Place(doc, new XYZ(0, -FrameHeight + SideCoverThickness, 0));
        n12.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));
        n13.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));
        n14.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));
        n15.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));
        n16.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));
        n17.Place(doc, new XYZ(0, FrameBottomRebarPlacementY, 0));

        n18.Place(
            doc,
            new XYZ(-(double)(FrameHalfWidthOnSlope - SideCoverThickness), -FrameHeight / 2, 0)
        );
        n19.Place(
            doc,
            new XYZ(-(double)(FrameHalfWidthOnSlope - SideCoverThickness), -FrameHeight / 2, 0)
        );
        n20.Place(doc, new XYZ(-FrameInnerRebarPlacementX, -FrameHeight / 2, 0));
        n18.Place(
            doc,
            new XYZ((double)(FrameHalfWidthOnSlope - SideCoverThickness), -FrameHeight / 2, 0),
            transform: new PlaceTransform() { Rotation = Math.PI }
        );
        n19.Place(
            doc,
            new XYZ((double)(FrameHalfWidthOnSlope - SideCoverThickness), -FrameHeight / 2, 0),
            transform: new PlaceTransform() { Rotation = Math.PI }
        );
        n20.Place(
            doc,
            new XYZ(FrameInnerRebarPlacementX, -FrameHeight / 2, 0),
            transform: new PlaceTransform() { Rotation = Math.PI }
        );

        #endregion

        var tieData = new List<string[]>
        {
            new[] { "位置", "编号", "D", "L1", "H", "L" },
            new[]
            {
                "顶板",
                "N23",
                "28",
                "102",
                "FrameTopSlabThickness-2d",
                "FrameTopSlabThickness-2d+160",
            },
            new[]
            {
                "顶板",
                "N23-1~18",
                "28",
                "102",
                "(FrameTopSlabThickness-2d)~(FrameTopSlabThickness+y1-2d)",
                "(FrameTopSlabThickness-2d+160)~(FrameTopSlabThickness+y1-2d+160)",
            },
            new[]
            {
                "底板",
                "N24",
                "28",
                "102",
                "FrameBottomSlabThickness-2d",
                "FrameBottomSlabThickness-2d+160",
            },
            new[]
            {
                "底板",
                "N24-1~3",
                "28",
                "102",
                "(FrameBottomSlabThickness-2d)~(FrameBottomSlabThickness+y2-2d)",
                "(FrameBottomSlabThickness-2d+160)~(FrameBottomSlabThickness+y2-2d+160)",
            },
            new[] { "侧墙", "N25", "28", "102", "a/sinα-2c", "a/sinα-2c+160" },
        };
        TitleBlock.Add(doc, "系筋尺寸表", "", CadDraw.P(-12000, -51000), 20);
        var tieTable = TableHelper.CreateTable(
            doc,
            CadDraw.P(-12000, -52000),
            tieData,
            columnWidths: [2200, 3000, 1200, 1800, 6000, 10000],
            rowHeights: [.. Enumerable.Repeat(700.0, tieData.Count)],
            textHeight: 250
        );
        doc.Entities.Add(tieTable);

        var obtuseRebarData = new List<string[]>
        {
            new[]
            {
                "编号",
                "直径(mm)",
                "张数",
                "每根长(mm)",
                "总长(m)",
                "合计长(m)",
                "单位重(kg/m)",
                "总重(kg)",
            },
            new[] { "N27", "25", "31×4", "2500", "310.0", "625.0", "3.853", "2408.12" },
            new[] { "N28", "25", "21×4", "3750", "315.0", "", "", "" },
        };
        TitleBlock.Add(doc, "全框架顶底板钝角加强钢筋数量表", "", CadDraw.P(-12000, -56500), 20);
        var obtuseRebarTable = TableHelper.CreateTable(
            doc,
            CadDraw.P(-12000, -57500),
            obtuseRebarData,
            columnWidths: [1800, 3000, 2200, 3300, 2400, 3000, 3500, 3000],
            rowHeights: [.. Enumerable.Repeat(700.0, obtuseRebarData.Count)],
            textHeight: 220
        );
        TableHelper.MergeCells(obtuseRebarTable, 1, 5, 2, 5);
        TableHelper.MergeCells(obtuseRebarTable, 1, 6, 2, 6);
        TableHelper.MergeCells(obtuseRebarTable, 1, 7, 2, 7);
        doc.Entities.Add(obtuseRebarTable);

        var frameRebarData = new List<string[]>
        {
            new[] { "编号", "直径", "根数", "每根长", "总长", "单位重量", "总重量" },
            new[] { "N0", "28", "8", "28371", "226.968", "4.83", "1096.26" },
            new[]
            {
                "N1",
                $"{n1.Diameter:0}",
                $"{n1.Count}",
                $"{n1.Length:0}",
                $"{n1.Length * n1.Count / 1000:0.###}",
                "4.83",
                $"{n1.Length * n1.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N2",
                $"{n2.Diameter:0}",
                $"{n2.Count}",
                $"{n2.Length:0}",
                $"{n2.Length * n2.Count / 1000:0.###}",
                "4.83",
                $"{n2.Length * n2.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N3",
                $"{n3.Diameter:0}",
                $"{n3.Count}",
                $"{n3.Length:0}",
                $"{n3.Length * n3.Count / 1000:0.###}",
                "4.83",
                $"{n3.Length * n3.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N4",
                $"{n4.Diameter:0}",
                $"{n4.Count}",
                $"{n4.Length:0}",
                $"{n4.Length * n4.Count / 1000:0.###}",
                "4.83",
                $"{n4.Length * n4.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N5",
                $"{n5.Diameter:0}",
                $"{n5.Count}",
                $"{n5.Length:0}",
                $"{n5.Length * n5.Count / 1000:0.###}",
                "4.83",
                $"{n5.Length * n5.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N6",
                $"{n6.Diameter:0}",
                $"{n6.Count}",
                $"{n6.Length:0}",
                $"{n6.Length * n6.Count / 1000:0.###}",
                "4.83",
                $"{n6.Length * n6.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N7",
                $"{n7.Diameter:0}",
                $"{n7.Count}",
                $"{n7.Length:0}",
                $"{n7.Length * n7.Count / 1000:0.###}",
                "4.83",
                $"{n7.Length * n7.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N8",
                $"{n8.Diameter:0}",
                $"{n8.Count}",
                $"{n8.Length:0}",
                $"{n8.Length * n8.Count / 1000:0.###}",
                "4.83",
                $"{n8.Length * n8.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N9",
                $"{n9.Diameter:0}",
                $"{n9.Count}",
                $"{n9.Length:0}",
                $"{n9.Length * n9.Count / 1000:0.###}",
                "4.83",
                $"{n9.Length * n9.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N10",
                $"{n10.Diameter:0}",
                $"{n10.Count}",
                $"{n10.Length:0}",
                $"{n10.Length * n10.Count / 1000:0.###}",
                "4.83",
                $"{n10.Length * n10.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N11",
                $"{n11.Diameter:0}",
                $"{n11.Count}",
                $"{n11.Length:0}",
                $"{n11.Length * n11.Count / 1000:0.###}",
                "4.83",
                $"{n11.Length * n11.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N12",
                $"{n12.Diameter:0}",
                $"{n12.Count}",
                $"{n12.Length:0}",
                $"{n12.Length * n12.Count / 1000:0.###}",
                "4.83",
                $"{n12.Length * n12.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N13",
                $"{n13.Diameter:0}",
                $"{n13.Count}",
                $"{n13.Length:0}",
                $"{n13.Length * n13.Count / 1000:0.###}",
                "4.83",
                $"{n13.Length * n13.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N14",
                $"{n14.Diameter:0}",
                $"{n14.Count}",
                $"{n14.Length:0}",
                $"{n14.Length * n14.Count / 1000:0.###}",
                "4.83",
                $"{n14.Length * n14.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N15",
                $"{n15.Diameter:0}",
                $"{n15.Count}",
                $"{n15.Length:0}",
                $"{n15.Length * n15.Count / 1000:0.###}",
                "4.83",
                $"{n15.Length * n15.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N16",
                $"{n16.Diameter:0}",
                $"{n16.Count}",
                $"{n16.Length:0}",
                $"{n16.Length * n16.Count / 1000:0.###}",
                "4.83",
                $"{n16.Length * n16.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N17",
                $"{n17.Diameter:0}",
                $"{n17.Count}",
                $"{n17.Length:0}",
                $"{n17.Length * n17.Count / 1000:0.###}",
                "4.83",
                $"{n17.Length * n17.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N18",
                $"{n18.Diameter:0}",
                $"{n18.Count}",
                $"{n18.Length:0}",
                $"{n18.Length * n18.Count / 1000:0.###}",
                "4.83",
                $"{n18.Length * n18.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N19",
                $"{n19.Diameter:0}",
                $"{n19.Count}",
                $"{n19.Length:0}",
                $"{n19.Length * n19.Count / 1000:0.###}",
                "4.83",
                $"{n19.Length * n19.Count * 4.83 / 1000:0.##}",
            },
            new[]
            {
                "N20",
                $"{n20.Diameter:0}",
                $"{n20.Count}",
                $"{n20.Length:0}",
                $"{n20.Length * n20.Count / 1000:0.###}",
                "1.58",
                $"{n20.Length * n20.Count * 1.58 / 1000:0.##}",
            },
            new[] { "N21", "16", "16", "7920", "126.72", "1.58", "200.22" },
            new[] { "N22", "16", "16", "4700", "75.2", "1.58", "118.82" },
            new[] { "N23", "Φ12", "552", "1030", "568.56", "0.888", "504.88" },
            new[] { "N23-1~18", "Φ12", "144", "1054~1600", "191.088", "0.888", "169.69" },
            new[] { "N24", "Φ12", "672", "1230", "909.888", "0.888", "807.98" },
            new[] { "N24-1~3", "Φ12", "24", "1288~1448", "32.496", "0.888", "28.86" },
            new[] { "N25", "Φ12", "496", "1689", "788.144", "0.888", "699.87" },
            new[] { "N26", "16", "985", "4179", "4116.315", "0.888", "3655.29" },
            new[] { "每延米框架身HRB400钢筋合计重(kg)", "", "", "", "", "", "14412.24" },
            new[] { "每延米框架身HPB300钢筋合计重(kg)", "", "", "", "", "", "2211.28" },
            new[] { "每延米框架C40混凝土(m³)", "", "", "", "", "", "76.69" },
        };
        TitleBlock.Add(doc, "框架身钢筋数量表（每延米）", "", CadDraw.P(-12000, -59500), 20);
        var frameRebarTable = TableHelper.CreateTable(
            doc,
            CadDraw.P(-12000, -61000),
            frameRebarData,
            columnWidths: [2500, 2600, 2600, 3200, 2800, 3000, 3200],
            rowHeights: [.. Enumerable.Repeat(650.0, frameRebarData.Count)],
            textHeight: 210
        );
        TableHelper.MergeCells(
            frameRebarTable,
            frameRebarData.Count - 3,
            0,
            frameRebarData.Count - 3,
            5
        );
        TableHelper.MergeCells(
            frameRebarTable,
            frameRebarData.Count - 2,
            0,
            frameRebarData.Count - 2,
            5
        );
        TableHelper.MergeCells(
            frameRebarTable,
            frameRebarData.Count - 1,
            0,
            frameRebarData.Count - 1,
            5
        );
        doc.Entities.Add(frameRebarTable);

        DrawAiMainRebarSkeletonRegion(doc);
        DrawAiNotesRegion(doc);
        DrawObtuseCornerReinforcementLayout(doc);
        DrawAiFrameBodyRebarQuantityRegion(doc);
        DrawAiFrameBodyRebarSectionRegion(doc);
    }

    /// <summary>
    /// 绘制框架桥结构图的边框。
    /// </summary>
    /// <param name="doc"></param>
    private static void CreatFrame(CadDocument doc)
    {
        doc.AddEntities<Entity>(
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-FrameHalfWidthOnSlope, -FrameHeight),
                        CadDraw.V(FrameHalfWidthOnSlope, -FrameHeight),
                        CadDraw.V(FrameHalfWidthOnSlope, 0),
                        CadDraw.V(-FrameHalfWidthOnSlope, 0),
                        CadDraw.V(-FrameHalfWidthOnSlope, -FrameHeight),
                    ],
                    doc.Layer(CadLayers.B04)
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(
                            -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                            -FrameTopSlabThickness - FrameTopChamferHeight
                        ),
                        CadDraw.V(
                            -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                            -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight
                        ),
                        CadDraw.V(
                            -(FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                                / FrameAngleSin,
                            -FrameHeight + FrameBottomSlabThickness
                        ),
                        CadDraw.V(
                            (FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                                / FrameAngleSin,
                            -FrameHeight + FrameBottomSlabThickness
                        ),
                        CadDraw.V(
                            (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                            -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight
                        ),
                        CadDraw.V(
                            (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                            -FrameTopSlabThickness - FrameTopChamferHeight
                        ),
                        CadDraw.V(
                            (FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                                / FrameAngleSin,
                            -FrameTopSlabThickness
                        ),
                        CadDraw.V(
                            -(FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                                / FrameAngleSin,
                            -FrameTopSlabThickness
                        ),
                        CadDraw.V(
                            -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                            -FrameTopSlabThickness - FrameTopChamferHeight
                        ),
                    ],
                    doc.Layer(CadLayers.B04)
                ),
            ]
        );

        // dim
        // 横向标注
        doc.AddEntities(
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(-FrameHalfWidthOnSlope, -FrameHeight, 0),
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameHeight,
                        0
                    ),
                    -90,
                    text: $"{FrameSideWallThickness}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameHeight,
                        0
                    ),
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameHeight,
                        0
                    ),
                    -90,
                    text: $"{FrameWidth - 2 * FrameSideWallThickness}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameHeight,
                        0
                    ),
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameHeight, 0),
                    -90,
                    text: $"{FrameSideWallThickness}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(-FrameHalfWidthOnSlope, -FrameHeight, 0),
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameHeight, 0),
                    -150,
                    text: $"{FrameWidth}/sin{FrameAngle}\u00b0"
                ),
            ]
        );

        // 侧向标注
        doc.AddEntities(
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(FrameHalfWidthOnSlope, 0, 0),
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameTopSlabThickness, 0),
                    90
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameTopSlabThickness, 0),
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameHeight + FrameBottomSlabThickness, 0),
                    90
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameHeight + FrameBottomSlabThickness, 0),
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameHeight, 0),
                    90
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(FrameHalfWidthOnSlope, 0, 0),
                    CadDraw.P(FrameHalfWidthOnSlope, -FrameHeight, 0),
                    150
                ),
            ]
        );
        // 上倒角标注
        doc.AddEntities(
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameTopSlabThickness,
                        0
                    ),
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                            / FrameAngleSin,
                        -FrameTopSlabThickness,
                        0
                    ),
                    -90,
                    text: $"{FrameTopChamferLength}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                            / FrameAngleSin,
                        -FrameTopSlabThickness,
                        0
                    ),
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                            / FrameAngleSin,
                        -FrameTopSlabThickness - FrameTopChamferHeight,
                        0
                    ),
                    30
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameTopSlabThickness,
                        0
                    ),
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                            / FrameAngleSin,
                        -FrameTopSlabThickness,
                        0
                    ),
                    90,
                    text: $"{FrameTopChamferLength}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                            / FrameAngleSin,
                        -FrameTopSlabThickness,
                        0
                    ),
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                            / FrameAngleSin,
                        -FrameTopSlabThickness - FrameTopChamferHeight,
                        0
                    ),
                    -30
                ),
            ]
        );

        // 下倒角标注
        doc.AddEntities(
            [
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight,
                        0
                    ),
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                            / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight,
                        0
                    ),
                    90,
                    text: $"{FrameBottomChamferLength}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                            / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight,
                        0
                    ),
                    CadDraw.P(
                        -(FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                            / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness,
                        0
                    ),
                    30
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness) / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight,
                        0
                    ),
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                            / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight,
                        0
                    ),
                    -90,
                    text: $"{FrameBottomChamferLength}/sin{FrameAngle}\u00b0"
                ),
                CadDraw.RotatedDimension(
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                            / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness + FrameBottomChamferHeight,
                        0
                    ),
                    CadDraw.P(
                        (FrameWidth / 2 - FrameSideWallThickness - FrameBottomChamferLength)
                            / FrameAngleSin,
                        -FrameHeight + FrameBottomSlabThickness,
                        0
                    ),
                    -30
                ),
            ]
        );
    }

    /// <summary>
    /// 框架顶板上部和底板下部边墙处钝角加强钢筋布置示意图。
    /// </summary>
    private static void DrawObtuseCornerReinforcementLayout(CadDocument doc)
    {
        // Generated from cad-region.json: obtuse-corner reinforcement layout.
        var rebarLayer = doc.Layer(CadLayers.B01);
        var annotationLayer = doc.Layer(CadLayers.B03);

        // The extracted sketch is local and much smaller than the quantity
        // table. Scale and place both parts here as one compact region.
        const double sketchScale = 1;
        const double sketchOriginX = -30000;
        const double sketchOriginY = -57000;
        XYZ P(double x, double y) =>
            CadDraw.P(sketchOriginX + x * sketchScale, sketchOriginY + y * sketchScale, 0);
        XY V(double x, double y) =>
            new(sketchOriginX + x * sketchScale, sketchOriginY + y * sketchScale);

        TitleBlock.Add(
            doc,
            "框架顶板上部和底板下部边墙钝角加强钢筋布置示意图",
            "",
            CadDraw.P(-21500, -50000),
            18
        );

        // AI 识别复刻：第一批 cad-region.json 中的两个线性阵列。
        // 源图 gj 映射为 B-01；源图 B-03 标注_指示标注映射为 B-03。
        // 两组均沿 X 方向每 30 个单位平移，共 24 个元素。
        doc.AddTranslated(
            P(433.2989248270278, 359.1042417199187),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(11.27631144942643 * sketchScale, -4.1042417199169 * sketchScale, 0),
                    annotationLayer
                ),
                CadDraw.P(30 * sketchScale, 0, 0),
                24
            )
        );

        doc.AddTranslated(
            P(606.03342472644, 490),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(-574.0735597332762 * sketchScale, -480 * sketchScale, 0),
                    rebarLayer
                ),
                CadDraw.P(30 * sketchScale, 0, 0),
                24
            )
        );

        // AI 识别复刻：第二批 cad-region.json 中的两个线性阵列。
        // 两组均沿 (35.8795974333334, 30, 0) 平移，共 17 个元素。
        doc.AddTranslated(
            P(31.95986579316377, 10),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(690 * sketchScale, 0, 0), rebarLayer),
                CadDraw.P(35.8795974333334 * sketchScale, 30 * sketchScale, 0),
                17
            )
        );
        doc.AddTranslated(
            P(165.70492492870835, 9.999999999996362),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(-11.824894782 * sketchScale, -2.042513989996223 * sketchScale, 0),
                    annotationLayer
                ),
                CadDraw.P(35.8795974333334 * sketchScale, 30 * sketchScale, 0),
                17
            )
        );

        // Outer obtuse-corner boundary and leader geometry.
        doc.Entities.Add(
            CadDraw.Line(
                P(105.99153499659587, -39.928143739434745),
                P(49.53380766288319, -39.928143739434745),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(P(-98.04013417093665, 50), P(71.73598924546968, 50), annotationLayer)
        );
        doc.Entities.Add(
            CadDraw.Line(
                P(739.7784838620137, 490),
                P(106.32612287750817, -39.64838484732536),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(P(351.31399388602586, 355), P(1134.5752362764615, 355), annotationLayer)
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    V(597.9932905554888, 500),
                    V(0, 0),
                    V(236.95986582906335, 0),
                    V(236.95986582906335, 30),
                    V(266.95986582906335, -30),
                    V(266.95986582906335, 0),
                    V(729.999999964114, 0),
                    V(1327.9932905196026, 499.99999999999994),
                ],
                annotationLayer,
                closed: true
            )
        );

        // N27/N28 leader labels.
        doc.Entities.Add(
            CadDraw.Text("N27", P(355.72723430710903, 359.93762364465874), 27, annotationLayer)
        );
        doc.Entities.Add(
            CadDraw.Text("N28", P(52.07538770890096, -34.774013947098865), 27, annotationLayer)
        );

        // Dimension annotations reconstructed from measured geometry and text position.
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                P(0, -39.928143739434745),
                P(0, 440.07185626056525),
                90,
                text: "20×12.5cm"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                P(606.03342472644, 490),
                P(1296.03342472644, 490),
                90,
                text: "30×12.5cm"
            )
        );
    }

    /// <summary>
    /// 附注文本区域
    /// </summary>
    /// <param name="doc"></param>
    private static void DrawAiNotesRegion(CadDocument doc)
    {
        // Generated from cad-region.json: the selected "附注" MTEXT region.
        doc.Entities.Add(
            CadDraw.MText(
                "附注：\\P1、本图尺寸除钢筋直径与长度以毫米计外，余均以厘米计。\\P2、框架身按正向每米配置8排骨架钢筋，其排列顺序为① ② ③ ④ ① ② ③ ④，每排正向间距12.5cm，\\P   框架始末端需用①或③骨架。\\P3、钢筋保护层厚度为4.5cm，可适当调整始末排钢筋间距，使其净保护层为4cm。\\P4、钢筋接头采用闪光对焊，钢筋接头应避开受拉区位置,钢筋焊接长度单面焊不小于10倍钢筋直径双面焊不小于5倍钢筋直径。\\P5、钢筋弯钩采用标准弯钩，主筋弯折时，弯曲半径均为14d(d为钢筋直径)。\\P6、框架顶板顶面和底板底面钝角处均应布置钝角加强钢筋，全框架共4处。\\P7、表列钢筋数量未计搭接及损耗。\\P8、主筋骨架中，当顶底板主筋三根成束布置时，置于钢筋束内侧的弯筋，其斜段高度在相应侧减少0.87d(d为钢筋直径)。\\P9、布置钢筋时应注意各钢筋骨架的中心线，墙身轴线，顶底板轴线的相对位置。",
                CadDraw.P(12280.10704856756, 701.6720995849337, 0),
                30,
                doc.Layer(CadLayers.B03),
                attachment: AttachmentPointType.TopLeft
            )
        );
    }

    /// <summary>
    /// 框架身主筋骨架示意图区域
    /// </summary>
    /// <param name="doc"></param>
    private static void DrawAiMainRebarSkeletonRegion(CadDocument doc)
    {
        // AI 识别复刻：cad-region.json 中的标题块“主筋骨架示意图”。
        TitleBlock.Add(
            doc,
            "主筋骨架示意图",
            "",
            CadDraw.P(14955.19111009196, 2061.446111203868, 0),
            9
        );

        // Generated from cad-region.json: main rebar skeleton region.
        var rebarLayer = doc.Layer(CadLayers.B01);
        var annotationLayer = doc.Layer(CadLayers.B03);

        // Array from entity 43: 4 items.
        doc.AddTranslated(
            CadDraw.P(15284.6352538, 288.031190978, 0),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(-84.8, -60, 0), rebarLayer),
                CadDraw.P(0, 470, 0),
                4
            )
        );

        // Array from entity 44: 4 items.
        doc.AddTranslated(
            CadDraw.P(14574.6352538, 288.031190978, 0),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(84.8, -60, 0), rebarLayer),
                CadDraw.P(0, 470, 0),
                4
            )
        );

        // Array from entity 47: 4 items.
        doc.AddTranslated(
            CadDraw.P(15284.6352538, 538.031190978, 0),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(-169.7333334, 40, 0), rebarLayer),
                CadDraw.P(0, 470, 0),
                4
            )
        );

        // Array from entity 48: 4 items.
        doc.AddTranslated(
            CadDraw.P(14574.6352538, 538.031190978, 0),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(169.7333333, 40, 0), rebarLayer),
                CadDraw.P(0, 470, 0),
                4
            )
        );

        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 1998.03119098),
                    new XY(14542.6352538, 2013.03119098),
                    new XY(14929.6352538, 2013.03119098),
                    new XY(15316.6352538, 2013.03119098),
                    new XY(15316.6352538, 1998.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1991.03119098),
                    new XY(14549.6352538, 2006.03119098),
                    new XY(14738.437741, 2006.03119098),
                    new XY(14774.437741, 1970.03119098),
                    new XY(14929.6352538, 1970.03119098),
                    new XY(15084.8327666, 1970.03119098),
                    new XY(15120.8327666, 2006.03119098),
                    new XY(15309.6352538, 2006.03119098),
                    new XY(15309.6352538, 1991.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14641.9005549, 1963.03119098),
                    new XY(14929.6352538, 1963.03119098),
                    new XY(15217.3699526, 1963.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1956.03119098),
                    new XY(14929.6352538, 1956.03119098),
                    new XY(15309.6352538, 1956.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 1628.03119098),
                    new XY(14542.6352538, 1613.03119098),
                    new XY(14929.6352538, 1613.03119098),
                    new XY(15316.6352538, 1613.03119098),
                    new XY(15316.6352538, 1628.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1635.03119098),
                    new XY(14549.6352538, 1620.03119098),
                    new XY(14714.0534444, 1620.03119098),
                    new XY(14750.0534444, 1656.03119098),
                    new XY(14929.6352538, 1656.03119098),
                    new XY(15109.2170631, 1656.03119098),
                    new XY(15145.2170631, 1620.03119098),
                    new XY(15309.6352538, 1620.03119098),
                    new XY(15309.6352538, 1635.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1663.03119098),
                    new XY(14929.6352538, 1663.03119098),
                    new XY(15309.6352538, 1663.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14550.6352538, 1606.03119098),
                    new XY(14535.6352538, 1606.03119098),
                    new XY(14535.6352538, 2020.03119098),
                    new XY(14550.6352538, 2020.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(15308.6352538, 1606.03119098),
                    new XY(15323.6352538, 1606.03119098),
                    new XY(15323.6352538, 2020.03119098),
                    new XY(15308.6352538, 2020.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 1528.03119098),
                    new XY(14542.6352538, 1543.03119098),
                    new XY(14929.6352538, 1543.03119098),
                    new XY(15316.6352538, 1543.03119098),
                    new XY(15316.6352538, 1528.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1521.03119098),
                    new XY(14549.6352538, 1536.03119098),
                    new XY(14840.8517863, 1536.03119098),
                    new XY(14876.8517863, 1500.03119098),
                    new XY(14929.6352538, 1500.03119098),
                    new XY(14982.4187212, 1500.03119098),
                    new XY(15018.4187212, 1536.03119098),
                    new XY(15309.6352538, 1536.03119098),
                    new XY(15309.6352538, 1521.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1529.03119098),
                    new XY(14713.4228665, 1529.03119098),
                    new XY(14735.4228665, 1507.03119098),
                    new XY(14929.6352538, 1507.03119098),
                    new XY(15123.847641, 1507.03119098),
                    new XY(15145.847641, 1529.03119098),
                    new XY(15309.6352538, 1529.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1493.03119098),
                    new XY(14929.6352538, 1493.03119098),
                    new XY(15309.6352538, 1493.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 1158.03119098),
                    new XY(14542.6352538, 1143.03119098),
                    new XY(14929.6352538, 1143.03119098),
                    new XY(15316.6352538, 1143.03119098),
                    new XY(15316.6352538, 1158.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1238.2664851),
                    new XY(14549.6352538, 1150.03119098),
                    new XY(14777.4526154, 1150.03119098),
                    new XY(14813.4526154, 1186.03119098),
                    new XY(14929.6352538, 1186.03119098),
                    new XY(15045.8178921, 1186.03119098),
                    new XY(15081.8178921, 1150.03119098),
                    new XY(15309.6352538, 1150.03119098),
                    new XY(15309.6352538, 1238.2664851),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14556.6352538, 1172.03119098),
                    new XY(14556.6352538, 1157.03119098),
                    new XY(14684.1617107, 1157.03119098),
                    new XY(14706.1617107, 1179.03119098),
                    new XY(14929.6352538, 1179.03119098),
                    new XY(15153.1087968, 1179.03119098),
                    new XY(15175.1087968, 1157.03119098),
                    new XY(15302.6352538, 1157.03119098),
                    new XY(15302.6352538, 1172.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1193.03119098),
                    new XY(14929.6352538, 1193.03119098),
                    new XY(15309.6352538, 1193.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14550.6352538, 1136.03119098),
                    new XY(14535.6352538, 1136.03119098),
                    new XY(14535.6352538, 1550.03119098),
                    new XY(14550.6352538, 1550.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(15308.6352538, 1136.03119098),
                    new XY(15323.6352538, 1136.03119098),
                    new XY(15323.6352538, 1550.03119098),
                    new XY(15308.6352538, 1550.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 1058.03119098),
                    new XY(14542.6352538, 1073.03119098),
                    new XY(14929.6352538, 1073.03119098),
                    new XY(15316.6352538, 1073.03119098),
                    new XY(15316.6352538, 1058.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1051.03119098),
                    new XY(14549.6352538, 1066.03119098),
                    new XY(14718.9303037, 1066.03119098),
                    new XY(14754.9303037, 1030.03119098),
                    new XY(14929.6352538, 1030.03119098),
                    new XY(15104.3402038, 1030.03119098),
                    new XY(15140.3402038, 1066.03119098),
                    new XY(15309.6352538, 1066.03119098),
                    new XY(15309.6352538, 1051.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14641.9005549, 1023.03119098),
                    new XY(14929.6352538, 1023.03119098),
                    new XY(15217.3699526, 1023.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 1016.03119098),
                    new XY(14929.6352538, 1016.03119098),
                    new XY(15309.6352538, 1016.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 688.031190978),
                    new XY(14542.6352538, 673.031190978),
                    new XY(14929.6352538, 673.031190978),
                    new XY(15316.6352538, 673.031190978),
                    new XY(15316.6352538, 688.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 695.031190978),
                    new XY(14549.6352538, 680.031190978),
                    new XY(14689.6691479, 680.031190978),
                    new XY(14725.6691479, 716.031190978),
                    new XY(14929.6352538, 716.031190978),
                    new XY(15133.6013596, 716.031190978),
                    new XY(15169.6013596, 680.031190978),
                    new XY(15309.6352538, 680.031190978),
                    new XY(15309.6352538, 695.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 723.031190978),
                    new XY(14929.6352538, 723.031190978),
                    new XY(15309.6352538, 723.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14550.6352538, 666.031190978),
                    new XY(14535.6352538, 666.031190978),
                    new XY(14535.6352538, 1080.03119098),
                    new XY(14550.6352538, 1080.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(15308.6352538, 666.031190978),
                    new XY(15323.6352538, 666.031190978),
                    new XY(15323.6352538, 1080.03119098),
                    new XY(15308.6352538, 1080.03119098),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 588.031190978),
                    new XY(14542.6352538, 603.031190978),
                    new XY(14929.6352538, 603.031190978),
                    new XY(15316.6352538, 603.031190978),
                    new XY(15316.6352538, 588.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 581.031190978),
                    new XY(14549.6352538, 596.031190978),
                    new XY(14777.4526154, 596.031190978),
                    new XY(14813.4526154, 560.031190978),
                    new XY(14929.6352538, 560.031190978),
                    new XY(15045.8178921, 560.031190978),
                    new XY(15081.8178921, 596.031190978),
                    new XY(15309.6352538, 596.031190978),
                    new XY(15309.6352538, 581.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 589.031190978),
                    new XY(14698.7922886, 589.031190978),
                    new XY(14720.7922886, 567.031190978),
                    new XY(14929.6352538, 567.031190978),
                    new XY(15138.4782189, 567.031190978),
                    new XY(15160.4782189, 589.031190978),
                    new XY(15309.6352538, 589.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 553.031190978),
                    new XY(14929.6352538, 553.031190978),
                    new XY(15309.6352538, 553.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14542.6352538, 218.031190978),
                    new XY(14542.6352538, 203.031190978),
                    new XY(14929.6352538, 203.031190978),
                    new XY(15316.6352538, 203.031190978),
                    new XY(15316.6352538, 218.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 225.031190978),
                    new XY(14549.6352538, 210.031190978),
                    new XY(14748.1914596, 210.031190978),
                    new XY(14784.1914596, 246.031190978),
                    new XY(14929.6352538, 246.031190978),
                    new XY(15075.079048, 246.031190978),
                    new XY(15111.079048, 210.031190978),
                    new XY(15309.6352538, 210.031190978),
                    new XY(15309.6352538, 225.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 217.031190978),
                    new XY(14664.6542735, 217.031190978),
                    new XY(14686.6542735, 239.031190978),
                    new XY(14929.6352538, 239.031190978),
                    new XY(15172.616234, 239.031190978),
                    new XY(15194.616234, 217.031190978),
                    new XY(15309.6352538, 217.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14549.6352538, 253.031190978),
                    new XY(14929.6352538, 253.031190978),
                    new XY(15309.6352538, 253.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(14550.6352538, 196.031190978),
                    new XY(14535.6352538, 196.031190978),
                    new XY(14535.6352538, 610.031190978),
                    new XY(14550.6352538, 610.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    new XY(15308.6352538, 196.031190978),
                    new XY(15323.6352538, 196.031190978),
                    new XY(15323.6352538, 610.031190978),
                    new XY(15308.6352538, 610.031190978),
                ],
                rebarLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N1",
                CadDraw.P(15059.3129757, 1973.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(15210.1608506, 248.280631815, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(14617.0352538, 268.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(15187.3240242, 530.677199526, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(14639.5019204, 523.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(15236.6352537, 439.710981004, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(14622.6352538, 398.684698297, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15259.6352538, 603.031190978, 0),
                CadDraw.P(15259.6352538, 203.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14599.6352538, 603.031190978, 0),
                CadDraw.P(14599.6352538, 203.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(15272.6352537, 437.557748961, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(14586.6352538, 403.031190978, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15295.6352538, 603.031190978, 0),
                CadDraw.P(15295.6352538, 203.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14563.6352538, 603.031190978, 0),
                CadDraw.P(14563.6352538, 203.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(15304.1695628, 437.557748961, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(14555.5169798, 403.031190978, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15302.6352538, 603.031190978, 0),
                CadDraw.P(15302.6352538, 203.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14556.6352538, 603.031190978, 0),
                CadDraw.P(14556.6352538, 203.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(15330.6352537, 436.364524311, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(14528.6352538, 403.031190978, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14818.5385251, 256.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N17",
                CadDraw.P(14889.6352538, 210.130324037, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N13",
                CadDraw.P(14949.6352538, 239.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N11",
                CadDraw.P(14929.6352538, 173.555824336, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14904.6352538, 530.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N7",
                CadDraw.P(14989.6352538, 570.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N3",
                CadDraw.P(14889.6352538, 563.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N0",
                CadDraw.P(14954.6352538, 606.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(15210.1608506, 718.280631815, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(14617.0352538, 738.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(15187.3240242, 1000.67719953, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(14639.5019204, 993.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(15236.6352537, 909.710981004, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(14622.6352538, 873.031190978, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15259.6352538, 1073.03119098, 0),
                CadDraw.P(15259.6352538, 673.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14599.6352538, 1073.03119098, 0),
                CadDraw.P(14599.6352538, 673.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(15279.6352537, 907.557748961, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(14579.6352538, 873.031190978, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15302.6352538, 1073.03119098, 0),
                CadDraw.P(15302.6352538, 673.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14556.6352538, 1073.03119098, 0),
                CadDraw.P(14556.6352538, 673.031190978, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(15330.6352537, 906.364524311, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(14528.6352538, 873.031190978, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14979.576586, 726.604554937, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N15",
                CadDraw.P(14984.6104552, 687.584233371, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N11",
                CadDraw.P(14929.6352538, 676.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14954.6352538, 993.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N9",
                CadDraw.P(14904.6352538, 1000.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N5",
                CadDraw.P(14889.6352538, 1033.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N0",
                CadDraw.P(14954.6352538, 1076.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(15210.1608506, 1188.28063181, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(14617.0352538, 1208.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(15187.3240242, 1470.67719953, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(14639.5019204, 1463.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(15236.6352537, 1379.710981, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(14622.6352538, 1343.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15259.6352538, 1543.03119098, 0),
                CadDraw.P(15259.6352538, 1143.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14599.6352538, 1543.03119098, 0),
                CadDraw.P(14599.6352538, 1143.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(15265.6352537, 1377.55774896, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(14593.6352538, 1343.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15288.6352538, 1543.03119098, 0),
                CadDraw.P(15288.6352538, 1143.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14570.6352538, 1543.03119098, 0),
                CadDraw.P(14570.6352538, 1143.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(15299.7359254, 1376.89646285, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(14560.6324571, 1343.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15295.6352538, 1543.03119098, 0),
                CadDraw.P(15295.6352538, 1143.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14563.6352538, 1543.03119098, 0),
                CadDraw.P(14563.6352538, 1143.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(15330.6352537, 1376.36452431, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(14528.6352538, 1343.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14954.6352538, 1196.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N16",
                CadDraw.P(14889.6352538, 1156.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N12",
                CadDraw.P(14949.6352538, 1163.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N11",
                CadDraw.P(14929.6352538, 1146.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14904.6352538, 1470.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N6",
                CadDraw.P(14989.6352538, 1510.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N2",
                CadDraw.P(14889.6352538, 1503.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N0",
                CadDraw.P(14954.6352538, 1546.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(15210.1608506, 1658.28063181, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(14617.0352538, 1678.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(15187.3240242, 1940.67719953, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(14639.5019204, 1933.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(15236.6352537, 1849.710981, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(14622.6352538, 1813.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15259.6352538, 2013.03119098, 0),
                CadDraw.P(15259.6352538, 1613.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14599.6352538, 2013.03119098, 0),
                CadDraw.P(14599.6352538, 1613.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(15279.6352537, 1847.55774896, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N19",
                CadDraw.P(14579.6352538, 1813.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(15302.6352538, 2013.03119098, 0),
                CadDraw.P(15302.6352538, 1613.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(14556.6352538, 2013.03119098, 0),
                CadDraw.P(14556.6352538, 1613.03119098, 0),
                rebarLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(15330.6352537, 1846.36452431, 0),
                27,
                annotationLayer,
                rotation: 4.71238898038
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N18",
                CadDraw.P(14528.6352538, 1813.03119098, 0),
                27,
                annotationLayer,
                rotation: 1.57079632679
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14954.6352538, 1666.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N14",
                CadDraw.P(14949.6352538, 1633.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N11",
                CadDraw.P(14929.6352538, 1616.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(14954.6352538, 1933.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N8",
                CadDraw.P(14904.6352538, 1940.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N4",
                CadDraw.P(14746.4014929, 1973.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N0",
                CadDraw.P(14954.6352538, 2016.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "4",
                CadDraw.P(14923.6352538, 391.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            SteelSection.Insert(doc, new XYZ(14929.6352538, 403.031190978, 0), scale: 5)
        );
        doc.Entities.Add(
            CadDraw.Text(
                "3",
                CadDraw.P(14923.6352538, 861.031190978, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            SteelSection.Insert(doc, new XYZ(14929.6352538, 873.031190978, 0), scale: 5)
        );
        doc.Entities.Add(
            CadDraw.Text(
                "2",
                CadDraw.P(14923.6352538, 1331.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            SteelSection.Insert(doc, new XYZ(14929.6352538, 1343.03119098, 0), scale: 5)
        );
        doc.Entities.Add(
            CadDraw.Text(
                "1",
                CadDraw.P(14923.6352538, 1801.03119098, 0),
                27,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            SteelSection.Insert(doc, new XYZ(14929.6352538, 1813.03119098, 0), scale: 5)
        );
    }

    /// <summary>
    /// 框架身钢筋断面 标注
    /// </summary>
    /// <param name="doc"></param>
    private static void DrawAiFrameBodyRebarQuantityRegion(CadDocument doc)
    {
        // Generated from cad-region.json: frame-body rebar quantity table per meter.
        var rebarLayer = doc.Layer(CadLayers.B01);
        var annotationLayer = doc.Layer(CadLayers.B03);

        // Detected array from entity 109: 17 items.
        doc.AddTranslated(
            CadDraw.P(859.362023186495, -58.9139088247794, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(8.13797681350843, -2.96198132727295, 0),
                    annotationLayer
                ),
                CadDraw.P(12.5, 0, 0),
                17
            )
        );

        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1080, -803.717107313627),
                    CadDraw.V(-1037.54420726656, -803.717107313627),
                    CadDraw.V(-978.781893072945, -903.905461827388),
                    CadDraw.V(-880.362778460585, -903.905461827388),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1071.86202318649, -58.9139088247794),
                    CadDraw.V(-1080, -61.8758901520523),
                    CadDraw.V(-867.5, -61.8758901520523),
                    CadDraw.V(-841.833825447226, 15.4072156368238),
                    CadDraw.V(-719.313314372157, 15.4072156368238),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-712.090550798923, -158.608615878973),
                    CadDraw.V(-749.319072837021, -158.608615878973),
                    CadDraw.V(-749.319072837021, -61.628149314347),
                    CadDraw.V(-752.281054164294, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-662.090550798919, -158.608615878973),
                    CadDraw.V(-699.319072837017, -158.608615878973),
                    CadDraw.V(-699.319072837017, -61.628149314347),
                    CadDraw.V(-702.28105416429, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-612.090550798923, -158.608615878973),
                    CadDraw.V(-649.319072837021, -158.608615878973),
                    CadDraw.V(-649.319072837021, -61.628149314347),
                    CadDraw.V(-652.281054164294, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-552.090550798923, -158.608615878973),
                    CadDraw.V(-589.319072837021, -158.608615878973),
                    CadDraw.V(-589.319072837021, -61.628149314347),
                    CadDraw.V(-592.281054164294, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-472.090550798923, -158.608615878973),
                    CadDraw.V(-509.319072837021, -158.608615878973),
                    CadDraw.V(-509.319072837021, -61.628149314347),
                    CadDraw.V(-512.281054164294, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-332.090550798923, -158.608615878973),
                    CadDraw.V(-369.319072837021, -158.608615878973),
                    CadDraw.V(-369.319072837021, -61.628149314347),
                    CadDraw.V(-372.281054164294, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-798.735977125547, -708.036810448059),
                    CadDraw.V(-835.964499163645, -708.036810448059),
                    CadDraw.V(-835.964499163645, -805.017277012688),
                    CadDraw.V(-838.926480490918, -796.87930019918),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-748.735977125514, -708.036810448059),
                    CadDraw.V(-785.964499163612, -708.036810448059),
                    CadDraw.V(-785.964499163612, -805.017277012681),
                    CadDraw.V(-788.926480490885, -796.879300199173),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-698.765204626387, -708.036810448059),
                    CadDraw.V(-735.993726664485, -708.036810448059),
                    CadDraw.V(-735.993726664485, -805.017277012681),
                    CadDraw.V(-738.955707991758, -796.879300199173),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-638.735977125518, -708.036810448059),
                    CadDraw.V(-675.964499163616, -708.036810448059),
                    CadDraw.V(-675.964499163616, -805.017277012681),
                    CadDraw.V(-678.926480490889, -796.879300199173),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-568.706749624733, -708.036810448059),
                    CadDraw.V(-605.935271662831, -708.036810448059),
                    CadDraw.V(-605.935271662831, -805.017277012681),
                    CadDraw.V(-608.897252990104, -796.879300199173),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-488.735977125565, -708.036810448059),
                    CadDraw.V(-525.964499163663, -708.036810448059),
                    CadDraw.V(-525.964499163663, -805.017277012681),
                    CadDraw.V(-528.926480490936, -796.879300199173),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-464.801323373507, -920.060974607928),
                    CadDraw.V(-464.801323373507, -865.000000000437),
                    CadDraw.V(-467.76330470078, -873.137976813945),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-366.636025465243, -746.862023186892),
                    CadDraw.V(-363.67404413797, -755.0000000004),
                    CadDraw.V(-363.67404413797, -708.036810448059),
                    CadDraw.V(-316.624037843729, -708.036810448059),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(150.640336841621, -834.591479718205),
                    CadDraw.V(150.640336841607, -708.036810448059),
                    CadDraw.V(197.690343135848, -708.036810448059),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1175.87389931931, -745.000000000004),
                    CadDraw.V(-1175.87389931931, -107.500000000004),
                    CadDraw.V(-1178.83588064658, -115.637976813516),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1178.83588064658, -753.137976813516),
                    CadDraw.V(-1175.87389931931, -745.000000000004),
                    CadDraw.V(-1172.91191799204, -736.862023186495),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(150.640336841621, -35.4085202817951),
                    CadDraw.V(150.640336841607, -161.963189551941),
                    CadDraw.V(197.690343135848, -161.963189551941),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(844.585903758794, -726.150151750211),
                    CadDraw.V(943.981691967201, -726.150151750211),
                    CadDraw.V(1009.17705229668, -803.717107313627),
                    CadDraw.V(1080, -803.717107313627),
                    CadDraw.V(1071.86202318649, -806.6790886409),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-8.00000000000728, -758),
                    CadDraw.V(-29.4361368967948, -726.489451761721),
                    CadDraw.V(-65.2280544488021, -726.489451761721),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(8, -92),
                    CadDraw.V(29.4361368967948, -139.718498523944),
                    CadDraw.V(65.2280544488021, -139.718498523944),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-886.316031751445, -708.036810448059),
                    CadDraw.V(-923.544553789543, -708.036810448059),
                    CadDraw.V(-923.544553789543, -833.526560300001),
                    CadDraw.V(-926.506535116816, -825.388583486492),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-732.076647771613, -217.394746497841),
                    CadDraw.V(-774.670894519895, -217.394746497841),
                    CadDraw.V(-774.670894519895, -73.5844471423079),
                    CadDraw.V(-777.632875847168, -81.7224239558163),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-123.443076084601, -158.608615878973),
                    CadDraw.V(-249.3673908483, -158.608615878973),
                    CadDraw.V(-249.367390848303, -95.0000000000036),
                    CadDraw.V(-252.329372175576, -103.137976813512),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1088.21202318649, -274.89410501477),
                    CadDraw.V(-1096.34999999999, -277.856086342043),
                    CadDraw.V(-1041.36617924311, -277.856086342043),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1250.38797681351, -274.89410501477),
                    CadDraw.V(-1242.25, -277.856086342043),
                    CadDraw.V(-1385.26578236847, -277.856086342043),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(1025.37675768764, -237.452014620321),
                    CadDraw.V(1066.14802337674, -237.452014620321),
                    CadDraw.V(1127.28140247348, -164.057326516304),
                    CadDraw.V(1119.93553858145, -167.514632763319),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(1120.70870226398, -699.229987272218),
                    CadDraw.V(1124.63726244203, -704.620977830433),
                    CadDraw.V(1086.95968004019, -665.524932576656),
                    CadDraw.V(1045.38835877532, -665.524932576656),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-846.862218521514, -38.3704865801083),
                    CadDraw.V(-855.000195335022, -35.4085052528353),
                    CadDraw.V(805.000000000004, -35.4085202817951),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(1071.8620231865, -58.9139088247794),
                    CadDraw.V(1080, -61.8758901520523),
                    CadDraw.V(848.662348998208, -61.8758901520523),
                    CadDraw.V(831.005012505957, 15.4072156368238),
                    CadDraw.V(712.779899716377, 15.4072156368238),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1042.5, -834.591423630787, 0),
                CadDraw.P(1034.36202318649, -837.55340495806, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1042.50003765005, -834.591423630718, 0),
                CadDraw.P(1042.5, -834.591423630787, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1034.36206083654, -837.553404957991, 0),
                CadDraw.P(-1042.50003765005, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1071.8620231865, -800.755125986354, 0),
                CadDraw.P(-1080, -803.717107313627, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(1162.72610068069, -745.000000000004),
                    CadDraw.V(1162.72610068068, -107.500000000004),
                    CadDraw.V(1159.76411935342, -115.637976813516),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(1159.76411935342, -753.137976813516),
                    CadDraw.V(1162.72610068069, -745.000000000004),
                    CadDraw.V(1165.68808200796, -736.862023186495),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-788.980844322523, -191.163263478898),
                    CadDraw.V(-829.752110011621, -191.163263478898),
                    CadDraw.V(-901.897824571952, -106.228360003915),
                    CadDraw.V(-900.265982336601, -111.898716173313),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-1120.70870226398, -699.229987272218),
                    CadDraw.V(-1124.63726244203, -704.620977830433),
                    CadDraw.V(-1076.65333858488, -662.876183285553),
                    CadDraw.V(-1035.08201732002, -662.876183285553),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-326.382325157334, 20.8344021750418),
                    CadDraw.V(-363.610847195432, 20.8344021750418),
                    CadDraw.V(-363.610847195432, -5),
                    CadDraw.V(-366.572828522705, 3.13797681350843),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(-275.572115493069, -158.608615878973),
                    CadDraw.V(-312.800637531167, -158.608615878973),
                    CadDraw.V(-312.800637531167, -61.628149314347),
                    CadDraw.V(-315.76261885844, -69.7661261278554),
                ],
                annotationLayer,
                closed: false,
                constantWidth: 0,
                elevation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N1",
                CadDraw.P(-299.310688570962, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );

        doc.Entities.Add(
            CadDraw.Text(
                "N0",
                CadDraw.P(-356.134781751765, 22.6709745751449, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(-1075.16640164305, -658.591899766727, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(-828.26517306979, -186.878979960073, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N25",
                CadDraw.P(1256.35021996742, -349.95914450427, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -732.500000000004, 0),
                CadDraw.P(1159.76411935342, -740.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -720.000000000004, 0),
                CadDraw.P(1159.76411935342, -728.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -707.500000000004, 0),
                CadDraw.P(1159.76411935342, -715.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -695.000000000004, 0),
                CadDraw.P(1159.76411935342, -703.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -682.500000000004, 0),
                CadDraw.P(1159.76411935342, -690.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -670.000000000004, 0),
                CadDraw.P(1159.76411935342, -678.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -657.500000000004, 0),
                CadDraw.P(1159.76411935342, -665.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -645.000000000004, 0),
                CadDraw.P(1159.76411935342, -653.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -632.500000000004, 0),
                CadDraw.P(1159.76411935342, -640.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -620.000000000004, 0),
                CadDraw.P(1159.76411935342, -628.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -607.500000000004, 0),
                CadDraw.P(1159.76411935342, -615.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -595.000000000004, 0),
                CadDraw.P(1159.76411935342, -603.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -582.500000000004, 0),
                CadDraw.P(1159.76411935342, -590.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -570.000000000004, 0),
                CadDraw.P(1159.76411935342, -578.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -557.500000000004, 0),
                CadDraw.P(1159.76411935342, -565.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -545.000000000004, 0),
                CadDraw.P(1159.76411935342, -553.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -532.500000000004, 0),
                CadDraw.P(1159.76411935342, -540.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -520.000000000004, 0),
                CadDraw.P(1159.76411935342, -528.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -507.500000000004, 0),
                CadDraw.P(1159.76411935342, -515.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -495.000000000004, 0),
                CadDraw.P(1159.76411935342, -503.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -482.500000000004, 0),
                CadDraw.P(1159.76411935342, -490.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -470.000000000004, 0),
                CadDraw.P(1159.76411935342, -478.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -457.500000000004, 0),
                CadDraw.P(1159.76411935342, -465.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -445.000000000004, 0),
                CadDraw.P(1159.76411935342, -453.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -432.500000000004, 0),
                CadDraw.P(1159.76411935342, -440.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -420.000000000004, 0),
                CadDraw.P(1159.76411935342, -428.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -407.500000000004, 0),
                CadDraw.P(1159.76411935342, -415.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -395.000000000004, 0),
                CadDraw.P(1159.76411935342, -403.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -382.500000000004, 0),
                CadDraw.P(1159.76411935342, -390.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -370.000000000004, 0),
                CadDraw.P(1159.76411935342, -378.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -357.500000000004, 0),
                CadDraw.P(1159.76411935342, -365.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -345.000000000004, 0),
                CadDraw.P(1159.76411935342, -353.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -332.500000000004, 0),
                CadDraw.P(1159.76411935342, -340.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -320.000000000004, 0),
                CadDraw.P(1159.76411935342, -328.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -307.500000000004, 0),
                CadDraw.P(1159.76411935342, -315.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -295.000000000004, 0),
                CadDraw.P(1159.76411935342, -303.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -282.500000000004, 0),
                CadDraw.P(1159.76411935342, -290.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -270.000000000004, 0),
                CadDraw.P(1159.76411935342, -278.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -257.500000000004, 0),
                CadDraw.P(1159.76411935342, -265.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -245.000000000004, 0),
                CadDraw.P(1159.76411935342, -253.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -232.500000000004, 0),
                CadDraw.P(1159.76411935342, -240.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -220.000000000004, 0),
                CadDraw.P(1159.76411935342, -228.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -207.500000000004, 0),
                CadDraw.P(1159.76411935342, -215.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -195.000000000004, 0),
                CadDraw.P(1159.76411935342, -203.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -182.500000000004, 0),
                CadDraw.P(1159.76411935342, -190.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -170.000000000004, 0),
                CadDraw.P(1159.76411935342, -178.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -157.500000000004, 0),
                CadDraw.P(1159.76411935342, -165.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -145.000000000004, 0),
                CadDraw.P(1159.76411935342, -153.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -132.500000000004, 0),
                CadDraw.P(1159.76411935342, -140.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068068, -120.000000000004, 0),
                CadDraw.P(1159.76411935342, -128.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1162.72610068069, -352.202208439754, 0),
                CadDraw.P(1297.23382075689, -352.202208439754, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1055, -803.717107313627, 0),
                CadDraw.P(-1046.8620231865, -800.755125986354, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1067.5, -803.717107313627, 0),
                CadDraw.P(-1059.3620231865, -800.755125986354, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N24-1\uff5e3",
                CadDraw.P(-977.592266408802, -900.505414637877, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1021.86206083654, -837.553404957991, 0),
                CadDraw.P(-1030.00003765005, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1009.36206083654, -837.553404957991, 0),
                CadDraw.P(-1017.50003765005, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N23-1\uff5e18",
                CadDraw.P(715.927708886731, 20.2034209846114, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-834.36221852151, -38.3704865801046, 0),
                CadDraw.P(-842.500195335018, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-821.86221852151, -38.3704865801046, 0),
                CadDraw.P(-830.000195335018, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-809.36221852151, -38.3704865801046, 0),
                CadDraw.P(-817.500195335018, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1017.5, -834.591423630787, 0),
                CadDraw.P(1009.36202318649, -837.55340495806, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1030, -834.591423630787, 0),
                CadDraw.P(1021.86202318649, -837.55340495806, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(1049.14841877403, -661.240649057831, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(1028.33676211058, -236.260468371716, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );

        doc.Entities.Add(
            CadDraw.Text(
                "N18/N19",
                CadDraw.P(-1384.29591316246, -274.146835848826, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N20",
                CadDraw.P(-1079.6472988926, -275.063202447409, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N8/N9/N10",
                CadDraw.P(-243.757894081936, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );

        doc.Entities.Add(
            CadDraw.Text(
                "N21",
                CadDraw.P(-767.234999613744, -213.790160156246, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N22",
                CadDraw.P(-921.742187615541, -706.681957027302, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N26",
                CadDraw.P(29.4069807810751, -134.466612074766, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N26",
                CadDraw.P(-64.5015753756657, -721.237565312544, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1055, -803.717107313627, 0),
                CadDraw.P(1046.86202318649, -806.6790886409, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(1067.5, -803.717107313627, 0),
                CadDraw.P(1059.36202318649, -806.6790886409, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N24-1\uff5e3",
                CadDraw.P(845.001612446141, -720.970199568581, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-796.862060836538, -38.3704865801046, 0),
                CadDraw.P(-805.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-784.362060836538, -38.3704865801046, 0),
                CadDraw.P(-792.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-771.862060836538, -38.3704865801046, 0),
                CadDraw.P(-780.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-759.362060836538, -38.3704865801046, 0),
                CadDraw.P(-767.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-746.862060836538, -38.3704865801046, 0),
                CadDraw.P(-755.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-734.362060836538, -38.3704865801046, 0),
                CadDraw.P(-742.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-721.862060836538, -38.3704865801046, 0),
                CadDraw.P(-730.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-709.362060836538, -38.3704865801046, 0),
                CadDraw.P(-717.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-696.862060836538, -38.3704865801046, 0),
                CadDraw.P(-705.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-684.362060836538, -38.3704865801046, 0),
                CadDraw.P(-692.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-671.862060836538, -38.3704865801046, 0),
                CadDraw.P(-680.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-659.362060836538, -38.3704865801046, 0),
                CadDraw.P(-667.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-646.862060836538, -38.3704865801046, 0),
                CadDraw.P(-655.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-634.362060836538, -38.3704865801046, 0),
                CadDraw.P(-642.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-621.862060836538, -38.3704865801046, 0),
                CadDraw.P(-630.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-609.362060836538, -38.3704865801046, 0),
                CadDraw.P(-617.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-596.862060836538, -38.3704865801046, 0),
                CadDraw.P(-605.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-584.362060836538, -38.3704865801046, 0),
                CadDraw.P(-592.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-571.862060836538, -38.3704865801046, 0),
                CadDraw.P(-580.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-559.362060836538, -38.3704865801046, 0),
                CadDraw.P(-567.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-546.862060836538, -38.3704865801046, 0),
                CadDraw.P(-555.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-534.362060836538, -38.3704865801046, 0),
                CadDraw.P(-542.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-521.862060836538, -38.3704865801046, 0),
                CadDraw.P(-530.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-509.362060836538, -38.3704865801046, 0),
                CadDraw.P(-517.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-496.862060836538, -38.3704865801046, 0),
                CadDraw.P(-505.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-484.362060836538, -38.3704865801046, 0),
                CadDraw.P(-492.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-471.862060836538, -38.3704865801046, 0),
                CadDraw.P(-480.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-459.362060836538, -38.3704865801046, 0),
                CadDraw.P(-467.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-446.862060836538, -38.3704865801046, 0),
                CadDraw.P(-455.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-434.362060836538, -38.3704865801046, 0),
                CadDraw.P(-442.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-421.862060836538, -38.3704865801046, 0),
                CadDraw.P(-430.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-409.362060836538, -38.3704865801046, 0),
                CadDraw.P(-417.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-396.862060836538, -38.3704865801046, 0),
                CadDraw.P(-405.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-384.362060836538, -38.3704865801046, 0),
                CadDraw.P(-392.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-371.862060836538, -38.3704865801046, 0),
                CadDraw.P(-380.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-359.362060836538, -38.3704865801046, 0),
                CadDraw.P(-367.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-346.862060836538, -38.3704865801046, 0),
                CadDraw.P(-355.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-334.362060836538, -38.3704865801046, 0),
                CadDraw.P(-342.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-321.862060836538, -38.3704865801046, 0),
                CadDraw.P(-330.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-309.362060836538, -38.3704865801046, 0),
                CadDraw.P(-317.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-296.862060836538, -38.3704865801046, 0),
                CadDraw.P(-305.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-284.362060836538, -38.3704865801046, 0),
                CadDraw.P(-292.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-271.862060836538, -38.3704865801046, 0),
                CadDraw.P(-280.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-259.362060836538, -38.3704865801046, 0),
                CadDraw.P(-267.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-246.862060836538, -38.3704865801046, 0),
                CadDraw.P(-255.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-234.362060836538, -38.3704865801046, 0),
                CadDraw.P(-242.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-221.862060836538, -38.3704865801046, 0),
                CadDraw.P(-230.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-209.362060836538, -38.3704865801046, 0),
                CadDraw.P(-217.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-196.862060836538, -38.3704865801046, 0),
                CadDraw.P(-205.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-184.362060836538, -38.3704865801046, 0),
                CadDraw.P(-192.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-171.862060836538, -38.3704865801046, 0),
                CadDraw.P(-180.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-159.362060836538, -38.3704865801046, 0),
                CadDraw.P(-167.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-146.862060836538, -38.3704865801046, 0),
                CadDraw.P(-155.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-134.362060836538, -38.3704865801046, 0),
                CadDraw.P(-142.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-121.862060836538, -38.3704865801046, 0),
                CadDraw.P(-130.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-109.362060836538, -38.3704865801046, 0),
                CadDraw.P(-117.500037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-96.8620608365381, -38.3704865801046, 0),
                CadDraw.P(-105.000037650047, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-84.3620608365381, -38.3704865801046, 0),
                CadDraw.P(-92.5000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-71.8620608365381, -38.3704865801046, 0),
                CadDraw.P(-80.0000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-59.3620608365381, -38.3704865801046, 0),
                CadDraw.P(-67.5000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-46.8620608365381, -38.3704865801046, 0),
                CadDraw.P(-55.0000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-34.3620608365381, -38.3704865801046, 0),
                CadDraw.P(-42.5000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-21.8620608365381, -38.3704865801046, 0),
                CadDraw.P(-30.0000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-9.36206083653815, -38.3704865801046, 0),
                CadDraw.P(-17.5000376500466, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(3.13793916346185, -38.3704865801046, 0),
                CadDraw.P(-5.00003765004658, -35.4085052528317, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N23",
                CadDraw.P(161.313007445116, -158.365987168805, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N25",
                CadDraw.P(-1079.49334930404, -349.900497041958, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -732.500000000004, 0),
                CadDraw.P(-1178.83588064658, -740.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -720.000000000004, 0),
                CadDraw.P(-1178.83588064658, -728.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -707.500000000004, 0),
                CadDraw.P(-1178.83588064658, -715.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -695.000000000004, 0),
                CadDraw.P(-1178.83588064658, -703.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -682.500000000004, 0),
                CadDraw.P(-1178.83588064658, -690.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -670.000000000004, 0),
                CadDraw.P(-1178.83588064658, -678.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -657.500000000004, 0),
                CadDraw.P(-1178.83588064658, -665.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -645.000000000004, 0),
                CadDraw.P(-1178.83588064658, -653.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -632.500000000004, 0),
                CadDraw.P(-1178.83588064658, -640.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -620.000000000004, 0),
                CadDraw.P(-1178.83588064658, -628.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -607.500000000004, 0),
                CadDraw.P(-1178.83588064658, -615.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -595.000000000004, 0),
                CadDraw.P(-1178.83588064658, -603.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -582.500000000004, 0),
                CadDraw.P(-1178.83588064658, -590.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -570.000000000004, 0),
                CadDraw.P(-1178.83588064658, -578.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -557.500000000004, 0),
                CadDraw.P(-1178.83588064658, -565.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -545.000000000004, 0),
                CadDraw.P(-1178.83588064658, -553.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -532.500000000004, 0),
                CadDraw.P(-1178.83588064658, -540.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -520.000000000004, 0),
                CadDraw.P(-1178.83588064658, -528.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -507.500000000004, 0),
                CadDraw.P(-1178.83588064658, -515.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -495.000000000004, 0),
                CadDraw.P(-1178.83588064658, -503.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -482.500000000004, 0),
                CadDraw.P(-1178.83588064658, -490.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -470.000000000004, 0),
                CadDraw.P(-1178.83588064658, -478.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -457.500000000004, 0),
                CadDraw.P(-1178.83588064658, -465.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -445.000000000004, 0),
                CadDraw.P(-1178.83588064658, -453.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -432.500000000004, 0),
                CadDraw.P(-1178.83588064658, -440.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -420.000000000004, 0),
                CadDraw.P(-1178.83588064658, -428.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -407.500000000004, 0),
                CadDraw.P(-1178.83588064658, -415.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -395.000000000004, 0),
                CadDraw.P(-1178.83588064658, -403.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -382.500000000004, 0),
                CadDraw.P(-1178.83588064658, -390.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -370.000000000004, 0),
                CadDraw.P(-1178.83588064658, -378.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -357.500000000004, 0),
                CadDraw.P(-1178.83588064658, -365.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -345.000000000004, 0),
                CadDraw.P(-1178.83588064658, -353.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -332.500000000004, 0),
                CadDraw.P(-1178.83588064658, -340.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -320.000000000004, 0),
                CadDraw.P(-1178.83588064658, -328.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -307.500000000004, 0),
                CadDraw.P(-1178.83588064658, -315.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -295.000000000004, 0),
                CadDraw.P(-1178.83588064658, -303.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -282.500000000004, 0),
                CadDraw.P(-1178.83588064658, -290.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -270.000000000004, 0),
                CadDraw.P(-1178.83588064658, -278.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -257.500000000004, 0),
                CadDraw.P(-1178.83588064658, -265.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -245.000000000004, 0),
                CadDraw.P(-1178.83588064658, -253.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -232.500000000004, 0),
                CadDraw.P(-1178.83588064658, -240.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -220.000000000004, 0),
                CadDraw.P(-1178.83588064658, -228.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -207.500000000004, 0),
                CadDraw.P(-1178.83588064658, -215.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -195.000000000004, 0),
                CadDraw.P(-1178.83588064658, -203.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -182.500000000004, 0),
                CadDraw.P(-1178.83588064658, -190.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -170.000000000004, 0),
                CadDraw.P(-1178.83588064658, -178.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -157.500000000004, 0),
                CadDraw.P(-1178.83588064658, -165.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -145.000000000004, 0),
                CadDraw.P(-1178.83588064658, -153.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -132.500000000004, 0),
                CadDraw.P(-1178.83588064658, -140.637976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -120.000000000004, 0),
                CadDraw.P(-1178.83588064658, -128.137976813516, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1175.87389931931, -352.202208439754, 0),
                CadDraw.P(-1041.36617924311, -352.202208439754, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-996.862060836538, -837.553404957991, 0),
                CadDraw.P(-1005.00003765005, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-984.362060836538, -837.553404957991, 0),
                CadDraw.P(-992.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-971.862060836538, -837.553404957991, 0),
                CadDraw.P(-980.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-959.362060836538, -837.553404957991, 0),
                CadDraw.P(-967.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-946.862060836538, -837.553404957991, 0),
                CadDraw.P(-955.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-934.362060836538, -837.553404957991, 0),
                CadDraw.P(-942.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-921.862060836538, -837.553404957991, 0),
                CadDraw.P(-930.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-909.362060836538, -837.553404957991, 0),
                CadDraw.P(-917.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-896.862060836538, -837.553404957991, 0),
                CadDraw.P(-905.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-884.362060836538, -837.553404957991, 0),
                CadDraw.P(-892.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-871.862060836538, -837.553404957991, 0),
                CadDraw.P(-880.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-859.362060836538, -837.553404957991, 0),
                CadDraw.P(-867.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-846.862060836538, -837.553404957991, 0),
                CadDraw.P(-855.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-834.362060836538, -837.553404957991, 0),
                CadDraw.P(-842.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-821.862060836538, -837.553404957991, 0),
                CadDraw.P(-830.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-809.362060836538, -837.553404957991, 0),
                CadDraw.P(-817.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-796.862060836538, -837.553404957991, 0),
                CadDraw.P(-805.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-784.362060836538, -837.553404957991, 0),
                CadDraw.P(-792.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-771.862060836538, -837.553404957991, 0),
                CadDraw.P(-780.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-759.362060836538, -837.553404957991, 0),
                CadDraw.P(-767.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-746.862060836538, -837.553404957991, 0),
                CadDraw.P(-755.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-734.362060836538, -837.553404957991, 0),
                CadDraw.P(-742.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-721.862060836538, -837.553404957991, 0),
                CadDraw.P(-730.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-709.362060836538, -837.553404957991, 0),
                CadDraw.P(-717.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-696.862060836538, -837.553404957991, 0),
                CadDraw.P(-705.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-684.362060836538, -837.553404957991, 0),
                CadDraw.P(-692.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-671.862060836538, -837.553404957991, 0),
                CadDraw.P(-680.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-659.362060836538, -837.553404957991, 0),
                CadDraw.P(-667.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-646.862060836538, -837.553404957991, 0),
                CadDraw.P(-655.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-634.362060836538, -837.553404957991, 0),
                CadDraw.P(-642.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-621.862060836538, -837.553404957991, 0),
                CadDraw.P(-630.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-609.362060836538, -837.553404957991, 0),
                CadDraw.P(-617.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-596.862060836538, -837.553404957991, 0),
                CadDraw.P(-605.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-584.362060836538, -837.553404957991, 0),
                CadDraw.P(-592.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-571.862060836538, -837.553404957991, 0),
                CadDraw.P(-580.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-559.362060836538, -837.553404957991, 0),
                CadDraw.P(-567.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-546.862060836538, -837.553404957991, 0),
                CadDraw.P(-555.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-534.362060836538, -837.553404957991, 0),
                CadDraw.P(-542.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-521.862060836538, -837.553404957991, 0),
                CadDraw.P(-530.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-509.362060836538, -837.553404957991, 0),
                CadDraw.P(-517.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-496.862060836538, -837.553404957991, 0),
                CadDraw.P(-505.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-484.362060836538, -837.553404957991, 0),
                CadDraw.P(-492.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-471.862060836538, -837.553404957991, 0),
                CadDraw.P(-480.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-459.362060836538, -837.553404957991, 0),
                CadDraw.P(-467.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-446.862060836538, -837.553404957991, 0),
                CadDraw.P(-455.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-434.362060836538, -837.553404957991, 0),
                CadDraw.P(-442.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-421.862060836538, -837.553404957991, 0),
                CadDraw.P(-430.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-409.362060836538, -837.553404957991, 0),
                CadDraw.P(-417.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-396.862060836538, -837.553404957991, 0),
                CadDraw.P(-405.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-384.362060836538, -837.553404957991, 0),
                CadDraw.P(-392.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-371.862060836538, -837.553404957991, 0),
                CadDraw.P(-380.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-359.362060836538, -837.553404957991, 0),
                CadDraw.P(-367.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-346.862060836538, -837.553404957991, 0),
                CadDraw.P(-355.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-334.362060836538, -837.553404957991, 0),
                CadDraw.P(-342.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-321.862060836538, -837.553404957991, 0),
                CadDraw.P(-330.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-309.362060836538, -837.553404957991, 0),
                CadDraw.P(-317.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-296.862060836538, -837.553404957991, 0),
                CadDraw.P(-305.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-284.362060836538, -837.553404957991, 0),
                CadDraw.P(-292.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-271.862060836538, -837.553404957991, 0),
                CadDraw.P(-280.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-259.362060836538, -837.553404957991, 0),
                CadDraw.P(-267.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-246.862060836538, -837.553404957991, 0),
                CadDraw.P(-255.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-234.362060836538, -837.553404957991, 0),
                CadDraw.P(-242.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-221.862060836538, -837.553404957991, 0),
                CadDraw.P(-230.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-209.362060836538, -837.553404957991, 0),
                CadDraw.P(-217.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-196.862060836538, -837.553404957991, 0),
                CadDraw.P(-205.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-184.362060836538, -837.553404957991, 0),
                CadDraw.P(-192.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-171.862060836538, -837.553404957991, 0),
                CadDraw.P(-180.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-159.362060836538, -837.553404957991, 0),
                CadDraw.P(-167.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-146.862060836538, -837.553404957991, 0),
                CadDraw.P(-155.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-134.362060836538, -837.553404957991, 0),
                CadDraw.P(-142.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-121.862060836538, -837.553404957991, 0),
                CadDraw.P(-130.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-109.362060836538, -837.553404957991, 0),
                CadDraw.P(-117.500037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-96.8620608365381, -837.553404957991, 0),
                CadDraw.P(-105.000037650047, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-84.3620608365381, -837.553404957991, 0),
                CadDraw.P(-92.5000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-71.8620608365381, -837.553404957991, 0),
                CadDraw.P(-80.0000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-59.3620608365381, -837.553404957991, 0),
                CadDraw.P(-67.5000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-46.8620608365381, -837.553404957991, 0),
                CadDraw.P(-55.0000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-34.3620608365381, -837.553404957991, 0),
                CadDraw.P(-42.5000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-21.8620608365381, -837.553404957991, 0),
                CadDraw.P(-30.0000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-9.36206083653815, -837.553404957991, 0),
                CadDraw.P(-17.5000376500466, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(3.13793916346185, -837.553404957991, 0),
                CadDraw.P(-5.00003765004658, -834.591423630718, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N24",
                CadDraw.P(161.313007445116, -703.820462642145, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N10",
                CadDraw.P(-353.001373534461, -703.820462642145, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );

        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-417.751317079266, -920.397975034226, 0),
                CadDraw.P(-464.746626839802, -920.397975034226, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N11",
                CadDraw.P(-454.128652769998, -916.181627228312, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N12",
                CadDraw.P(-523.581389713111, -705.692420465073, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N13",
                CadDraw.P(-603.552162212278, -705.702160876697, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N14",
                CadDraw.P(-673.581389713063, -705.692420465073, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N15",
                CadDraw.P(-731.892860883489, -705.692420465073, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N16",
                CadDraw.P(-781.863633382617, -706.232961005613, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N17",
                CadDraw.P(-828.528604257494, -706.681957027302, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N2",
                CadDraw.P(-355.829123876816, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N3",
                CadDraw.P(-495.829123876816, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N4",
                CadDraw.P(-575.829123876816, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N5",
                CadDraw.P(-635.829123876816, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N6",
                CadDraw.P(-685.829123876812, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N23-1\uff5e18",
                CadDraw.P(-835.729494800973, 20.2034209846114, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N7",
                CadDraw.P(-735.829123876816, -155.004029537378, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-871.862023186492, -58.9139088247794, 0),
                CadDraw.P(-880, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-884.362023186492, -58.9139088247794, 0),
                CadDraw.P(-892.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-896.862023186492, -58.9139088247794, 0),
                CadDraw.P(-905, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-909.362023186492, -58.9139088247794, 0),
                CadDraw.P(-917.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-921.862023186492, -58.9139088247794, 0),
                CadDraw.P(-930, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-934.362023186492, -58.9139088247794, 0),
                CadDraw.P(-942.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-946.862023186492, -58.9139088247794, 0),
                CadDraw.P(-955, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-959.362023186492, -58.9139088247794, 0),
                CadDraw.P(-967.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-971.862023186492, -58.9139088247794, 0),
                CadDraw.P(-980, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-984.362023186492, -58.9139088247794, 0),
                CadDraw.P(-992.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-996.862023186492, -58.9139088247794, 0),
                CadDraw.P(-1005, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1009.36202318649, -58.9139088247794, 0),
                CadDraw.P(-1017.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1021.86202318649, -58.9139088247794, 0),
                CadDraw.P(-1030, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1034.36202318649, -58.9139088247794, 0),
                CadDraw.P(-1042.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1046.86202318649, -58.9139088247794, 0),
                CadDraw.P(-1055, -61.8758901520523, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-1059.36202318649, -58.9139088247794, 0),
                CadDraw.P(-1067.5, -61.8758901520523, 0),
                annotationLayer
            )
        );
    }

    /// <summary>
    /// 框架身钢筋断面 钢筋断面
    /// </summary>
    /// <param name="doc"></param>
    private static void DrawAiFrameBodyRebarSectionRegion(CadDocument doc)
    {
        // 顶底系筋
        var lines = new List<Entity>();
        lines.AddRange(
            CadDraw.LinesBetweenBoundaries(
                CadDraw.Polyline(
                    [
                        new XY(-FrameInnerRebarX, -SideCoverThickness),
                        new XY(0, -SideCoverThickness),
                    ],
                    doc.Layer(CadLayers.B01)
                ),
                CadDraw.Polyline(
                    [
                        new XY(-FrameInnerRebarX, -(FrameTopSlabThickness - SideCoverThickness)),
                        new(
                            -(FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                                / FrameAngleSin,
                            -(FrameTopSlabThickness - SideCoverThickness)
                        ),
                        new(0, -(FrameTopSlabThickness - SideCoverThickness)),
                    ],
                    doc.Layer(CadLayers.B01)
                ),
                -FrameInnerRebarX + FrameRebarSpacing + 12.5,
                0,
                12.5,
                doc.Layer(CadLayers.B01)
            )
        );
        lines.AddRange(
            CadDraw.LinesBetweenBoundaries(
                CadDraw.Polyline(
                    [
                        new XY(-FrameInnerRebarX, SideCoverThickness - FrameHeight),
                        new XY(0, SideCoverThickness - FrameHeight),
                    ],
                    doc.Layer(CadLayers.B01)
                ),
                CadDraw.Polyline(
                    [
                        new XY(
                            -FrameInnerRebarX,
                            FrameBottomSlabThickness - SideCoverThickness - FrameHeight
                        ),
                        new(
                            -(FrameWidth / 2 - FrameSideWallThickness - FrameTopChamferLength)
                                / FrameAngleSin,
                            FrameBottomSlabThickness - SideCoverThickness - FrameHeight
                        ),
                        new(0, FrameBottomSlabThickness - SideCoverThickness - FrameHeight),
                    ],
                    doc.Layer(CadLayers.B01)
                ),
                -FrameInnerRebarX + FrameRebarSpacing + 12.5,
                0,
                12.5,
                doc.Layer(CadLayers.B01)
            )
        );

        doc.AddEntities(lines.Concat(lines.Select(e => e.MirrorAcrossVertical())));

        // 左右系筋
        var lines2 = new List<Entity>();
        lines2.AddRange(
            CadDraw.EntityArray(
                CadDraw.Line(
                    new XYZ(
                        -FrameOuterRebarX - FrameRebarSpacing,
                        -(FrameTopSlabThickness - SideCoverThickness - FrameRebarSpacing)
                            - 12.5
                            - FrameRebarSpacing,
                        0
                    ),
                    new XYZ(
                        -FrameInnerRebarX + FrameRebarSpacing,
                        -(FrameTopSlabThickness - SideCoverThickness - FrameRebarSpacing)
                            - 12.5
                            - FrameRebarSpacing,
                        0
                    ),
                    doc.Layer(CadLayers.B01)
                ),
                new XYZ(0, -12.5, 0),
                (int)
                    Math.Floor(
                        (
                            FrameBodyCenterY
                            - FrameTopSlabThickness
                            + SideCoverThickness
                            + FrameRebarSpacing
                        ) / 12.5
                    )
            )
        );

        doc.AddEntities(
            lines2
                .Concat(lines2.Select(e => e.MirrorAcrossVertical()))
                .Concat(lines2.Select(e => e.MirrorAcrossHorizontal(-FrameBodyCenterY)))
                .Concat(
                    lines2.Select(e =>
                        e.MirrorAcrossVertical().MirrorAcrossHorizontal(-FrameBodyCenterY)
                    )
                )
        );

        AddSymmetricArray(
            doc,
            -FrameOuterRebarX,
            -(SideCoverThickness + FrameRebarSpacing),
            new(
                SteelSection.Insert(doc),
                12.5,
                0,
                (int)Math.Floor((FrameOuterRebarX - FrameInnerRebarX) / 12.5)
            ),
            -FrameHeight / 2
        ); // 外框：左侧壁厚
        AddSymmetricArray(
            doc,
            -FrameInnerRebarX,
            -(SideCoverThickness + FrameRebarSpacing),
            new(SteelSection.Insert(doc), 12.5, 0, (int)Math.Floor(FrameInnerRebarX / 12.5) + 1),
            -FrameHeight / 2
        ); // 外框：中间部分

        AddSymmetricArray(
            doc,
            -FrameOuterRebarX,
            -(SideCoverThickness + FrameRebarSpacing),
            new(
                SteelSection.Insert(doc),
                0,
                -12.5,
                (int)
                    Math.Floor(
                        (FrameTopSlabThickness - 2 * SideCoverThickness - 2 * FrameRebarSpacing)
                            / 12.5
                    ) + 1
            ),
            -FrameHeight / 2
        ); // 外框：上部壁厚
        AddSymmetricArray(
            doc,
            -FrameOuterRebarX,
            -(FrameTopSlabThickness - SideCoverThickness - FrameRebarSpacing),
            new(
                SteelSection.Insert(doc),
                0,
                -12.5,
                (int)
                    Math.Floor(
                        (
                            FrameBodyCenterY
                            - FrameTopSlabThickness
                            + SideCoverThickness
                            + FrameRebarSpacing
                        ) / 12.5
                    ) + 1
            ),
            -FrameBodyCenterY
        ); // 外框：中间部分

        AddSymmetricArray(
            doc,
            -FrameInnerRebarX,
            -(FrameTopSlabThickness - SideCoverThickness - FrameRebarSpacing),
            new(SteelSection.Insert(doc), 12.5, 0, (int)Math.Floor(FrameInnerRebarX / 12.5) + 1),
            -FrameBodyCenterY
        ); // 内框：中间部分
        AddSymmetricArray(
            doc,
            -FrameInnerRebarX,
            -(FrameTopSlabThickness - SideCoverThickness - FrameRebarSpacing),
            new(
                SteelSection.Insert(doc),
                0,
                -12.5,
                (int)
                    Math.Floor(
                        (
                            FrameBodyCenterY
                            - FrameTopSlabThickness
                            + SideCoverThickness
                            + FrameRebarSpacing
                        ) / 12.5
                    ) + 1
            ),
            -FrameBodyCenterY
        ); // 内框：中间部分
    }

    /// <summary>
    /// 按水平中心线和竖直中心线生成四象限对称的实体阵列。
    /// </summary>
    private static void AddSymmetricArray(
        CadDocument doc,
        double x,
        double y,
        EntityArrayPattern pattern,
        double horizontalAxisY
    )
    {
        IEnumerable<Entity> Templates(double stepX, double stepY) =>
            CadDraw.EntityArray(pattern.Template, CadDraw.P(stepX, stepY, 0), pattern.Count);

        // 原始 1/4 断面。
        doc.AddTransformed(new XYZ(x, y, 0), Templates(pattern.StepX, pattern.StepY));

        // 关于水平中心线镜像：反向 Y 步进，避免块插入实体负缩放失效。
        doc.AddTransformed(
            new XYZ(x, 2 * horizontalAxisY - y, 0),
            Templates(pattern.StepX, -pattern.StepY)
        );

        // 关于竖直中心线 x = 0 镜像：反向 X 步进。
        doc.AddTransformed(new XYZ(-x, y, 0), Templates(-pattern.StepX, pattern.StepY));

        // 同时关于水平、竖直中心线镜像。
        doc.AddTransformed(
            new XYZ(-x, 2 * horizontalAxisY - y, 0),
            Templates(-pattern.StepX, -pattern.StepY)
        );
    }
}
