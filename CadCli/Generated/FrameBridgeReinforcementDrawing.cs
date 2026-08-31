using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
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
    //     rebar.PlaceDetail(doc, 摆放点, 引线点, scale: 50, options);
    // PlaceDetail 自动完成：形状多段线 + 每段标注 + 引线标注(编号/直径/L=公式) + XData。
    // 端部 R350 圆角直接写在顶点 Bulge 上（RebarDetail.Add 会渲染凸度），
    // 不再用单独的 CadDraw.Arc 补画。
    // 双参数族：H1 族（断面 Ⅲ-d+300，斜段投影 304）与 H2 族（断面 H2-d+100，斜段投影 320）。
    // 图中所有英文字母(A / α / H1 / H2 / d / c)均为参数，先给定假定值。
    // ────────────────────────────────────────────────────────────────

    private const double A = 9143; // 斜长基数（公式用）
    private const double Alpha = Math.PI / 4; // α = 45°
    private const double H1 = 360; // H1（使 H1-2d = 304）
    private const double H2 = 376; // H2（使 H2-2d = 320）
    private const double d = 28; // 钢筋直径
    private const double c = 50; // 保护层

    private const double Hook = 900; // 端部竖向弯钩高
    private const double Foot = 328; // 端部水平段
    private const double FilletR = 350; // 端部圆角 R350
    private const double FilletBulge = -Math.PI / 8; // tan(22.5°)，90° 圆角凸度
    private const double TopStub = 275; // 斜段两侧短平段
    private const double Center = 4000; // 中部下平段

    private const double TextH = 250; // 标注字高
    private const double DimOffset = 420; // 尺寸文字相对线偏移

    private const string SectionLabel1 = "Ⅲ-d+300"; // H1 族端部断面
    private const string SectionLabel2 = "H2-d+100"; // H2 族端部断面

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
            n_系筋,
            n_系筋_s;

        // ── 公共几何 ──
        double diag1 = H1 - 2 * d; // 304：H1 族斜段水平/竖直投影
        double diag2 = H2 - 2 * d; // 320：H2 族斜段水平/竖直投影
        double ts = TopStub; // 275
        double m = Center; // 4000：中部下平段
        double hook = Hook; // 900

        double lowEndL = -m / 2;
        double lowEndR = m / 2;

        double tsxStartL1 = -(m / 2 + 2 * ts + diag1);
        double tsxEndL1 = -(m / 2 + ts + diag1);
        double diagBotL1 = -(m / 2 + ts);
        double diagBotR1 = +(m / 2 + ts);
        double tsxEndR1 = +(m / 2 + ts + diag1);
        double tsxStartR1 = +(m / 2 + 2 * ts + diag1);

        double tsxStartL2 = -(m / 2 + 2 * ts + diag2);
        double tsxEndL2 = -(m / 2 + ts + diag2);
        double diagBotL2 = -(m / 2 + ts);
        double diagBotR2 = +(m / 2 + ts);
        double tsxEndR2 = +(m / 2 + ts + diag2);
        double tsxStartR2 = +(m / 2 + 2 * ts + diag2);

        // N8~N11 直线筋宽度 = H2 族最宽下折筋（N12，每侧净长 6045）对齐
        double halfStraight = 6045 + m / 2 + 2 * ts + diag2;

        // N0：直线筋（顶板上层），8N0
        {
            var n0 = new Rebar
            {
                Number = "8N0",
                Count = 8,
                Diameter = d,
                SubstituteLength = "A/sinα-2c+3556",
                Vertices =
                [
                    RebarDetail.V(-1214.15, -160),
                    RebarDetail.V(-1242.25, -160),
                    RebarDetail.V(-1242.25, -35, bulge: -0.414214),
                    RebarDetail.V(-1207.25, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(1207.25, 0, bulge: -0.414214),
                    RebarDetail.V(1242.25, -35),
                    RebarDetail.V(1242.25, -160),
                    RebarDetail.V(1214.15, -160),
                ],
            };
            n0.PlaceDetail(doc, new XYZ(0, 43000, 0), new XYZ(0, 41900, 0), scale: 50, options);
        }

        // N1：下折筋，每侧净长 8143
        {
            double half = 8143 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n1 = new Rebar
            {
                Number = "2N1",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H1-2d)sin45° +19134",
                Vertices =
                [
                    RebarDetail.V(-1242.25, -70),
                    RebarDetail.V(-1214.15, -70),
                    RebarDetail.V(-1242.25, -70),
                    RebarDetail.V(-1242.25, 55, "", -0.414214),
                    RebarDetail.V(-1207.25, 90),
                    RebarDetail.V(-392.920324, 90, "", -0.174651),
                    RebarDetail.V(-365.183773, 79.236551),
                    RebarDetail.V(-297.223611, 11.276389, "", 0.198912),
                    RebarDetail.V(-270, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(270, 0, "", 0.198912),
                    RebarDetail.V(297.223611, 11.276389),
                    RebarDetail.V(365.183773, 79.236551, "", -0.174651),
                    RebarDetail.V(392.920324, 90),
                    RebarDetail.V(1207.25, 90, "", -0.414214),
                    RebarDetail.V(1242.25, 55),
                    RebarDetail.V(1242.25, -70),
                    RebarDetail.V(1214.15, -70),
                ],
            };
            n1.PlaceDetail(doc, new XYZ(0, 38000, 0), new XYZ(0, 36900, 0), scale: 50, options);
        }

        // N2：下折筋，每侧净长 7643
        {
            double half = 7643 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n2 = new Rebar
            {
                Number = "2N2",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H1-2d)sin45° +19134",
                Vertices =
                [
                    RebarDetail.V(-1214.15, -70),
                    RebarDetail.V(-1242.25, -70),
                    RebarDetail.V(-1242.25, 55, "", -0.414214),
                    RebarDetail.V(-1207.25, 90),
                    RebarDetail.V(-442.920324, 90, "", -0.174651),
                    RebarDetail.V(-415.183773, 79.236551),
                    RebarDetail.V(-347.223611, 11.276389, "", 0.198912),
                    RebarDetail.V(-320, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(320, 0, "", 0.198912),
                    RebarDetail.V(347.223611, 11.276389),
                    RebarDetail.V(415.183773, 79.236551, "", -0.174651),
                    RebarDetail.V(442.920324, 90),
                    RebarDetail.V(1207.25, 90, "", -0.414214),
                    RebarDetail.V(1242.25, 55),
                    RebarDetail.V(1242.25, -70),
                    RebarDetail.V(1214.15, -70),
                ],
            };
            n2.PlaceDetail(doc, new XYZ(0, 33000, 0), new XYZ(0, 31900, 0), scale: 50, options);
        }

        // N3：下折筋，每侧净长 6243
        {
            double half = 6243 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n3 = new Rebar
            {
                Number = "2N3",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H1-2d)sin45° +19134",
                Vertices =
                [
                    RebarDetail.V(-1214.15, -70),
                    RebarDetail.V(-1242.25, -70),
                    RebarDetail.V(-1242.25, 55, "", -0.414214),
                    RebarDetail.V(-1207.25, 90),
                    RebarDetail.V(-582.920324, 90, "", -0.174651),
                    RebarDetail.V(-555.183773, 79.236551),
                    RebarDetail.V(-487.223611, 11.276389, "", 0.198912),
                    RebarDetail.V(-460, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(460, 0, "", 0.198912),
                    RebarDetail.V(487.223611, 11.276389),
                    RebarDetail.V(555.183773, 79.236551, "", -0.174651),
                    RebarDetail.V(582.920324, 90),
                    RebarDetail.V(1207.25, 90, "", -0.414214),
                    RebarDetail.V(1242.25, 55),
                    RebarDetail.V(1242.25, -70),
                    RebarDetail.V(1214.15, -70),
                ],
            };
            n3.PlaceDetail(doc, new XYZ(0, 28000, 0), new XYZ(0, 26900, 0), scale: 50, options);
        }

        // N4：下折筋，每侧净长 5443
        {
            double half = 5443 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n4 = new Rebar
            {
                Number = "2N4",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H1-2d)sin45° +19134",
                Vertices =
                [
                    RebarDetail.V(-1214.15, -70),
                    RebarDetail.V(-1242.25, -70),
                    RebarDetail.V(-1242.25, 55, "", -0.414214),
                    RebarDetail.V(-1207.25, 90),
                    RebarDetail.V(-662.920324, 90, "", -0.174651),
                    RebarDetail.V(-635.183773, 79.236551),
                    RebarDetail.V(-567.223611, 11.276389, "", 0.198912),
                    RebarDetail.V(-540, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(540, 0, "", 0.198912),
                    RebarDetail.V(567.223611, 11.276389),
                    RebarDetail.V(635.183773, 79.236551, "", -0.174651),
                    RebarDetail.V(662.920324, 90),
                    RebarDetail.V(1207.25, 90, "", -0.414214),
                    RebarDetail.V(1242.25, 55),
                    RebarDetail.V(1242.25, -70),
                    RebarDetail.V(1214.15, -70),
                ],
            };
            n4.PlaceDetail(doc, new XYZ(0, 23000, 0), new XYZ(0, 21900, 0), scale: 50, options);
        }

        // N5：下折筋，每侧净长 4843
        {
            double half = 4843 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n5 = new Rebar
            {
                Number = "2N5",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H1-2d)sin45° +19134",
                Vertices =
                [
                    RebarDetail.V(-1214.15, -70),
                    RebarDetail.V(-1242.25, -70),
                    RebarDetail.V(-1242.25, 55, "", -0.414214),
                    RebarDetail.V(-1207.25, 90),
                    RebarDetail.V(-722.920324, 90, "", -0.174651),
                    RebarDetail.V(-695.183773, 79.236551),
                    RebarDetail.V(-627.223611, 11.276389, "", 0.198912),
                    RebarDetail.V(-600, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(600, 0, "", 0.198912),
                    RebarDetail.V(627.223611, 11.276389),
                    RebarDetail.V(695.183773, 79.236551, "", -0.174651),
                    RebarDetail.V(722.920324, 90),
                    RebarDetail.V(1207.25, 90, "", -0.414214),
                    RebarDetail.V(1242.25, 55),
                    RebarDetail.V(1242.25, -70),
                    RebarDetail.V(1214.15, -70),
                ],
            };
            n5.PlaceDetail(doc, new XYZ(0, 18000, 0), new XYZ(0, 16900, 0), scale: 50, options);
        }

        // N6：下折筋，每侧净长 4693
        {
            double half = 4693 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n6 = new Rebar
            {
                Number = "2N6",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2(H1-2d)/sin45° -2(H1-2d)+468",
                Vertices =
                [
                    RebarDetail.V(1242.25, 61.9),
                    RebarDetail.V(1242.25, 90),
                    RebarDetail.V(772.920324, 90, "", 0.174651),
                    RebarDetail.V(745.183773, 79.236551),
                    RebarDetail.V(677.223611, 11.276389, "", -0.198912),
                    RebarDetail.V(650, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(-650, 0, "", -0.198912),
                    RebarDetail.V(-677.223611, 11.276389),
                    RebarDetail.V(-745.183773, 79.236551, "", 0.174651),
                    RebarDetail.V(-772.920324, 90),
                    RebarDetail.V(-1242.25, 90),
                    RebarDetail.V(-1242.25, 61.9),
                ],
            };
            n6.PlaceDetail(doc, new XYZ(0, 13000, 0), new XYZ(0, 11900, 0), scale: 50, options);
        }

        // N7：下折筋，每侧净长 4193
        {
            double half = 4193 + m / 2 + 2 * ts + diag1;
            double lx = -half,
                rx = half,
                fy = -hook;
            n7 = new Rebar
            {
                Number = "2N7",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2(H1-2d)/sin45° -2(H1-2d)+1468",
                Vertices =
                [
                    RebarDetail.V(1242.25, 61.9),
                    RebarDetail.V(1242.25, 90),
                    RebarDetail.V(822.920324, 90, "", 0.174651),
                    RebarDetail.V(795.183773, 79.236551),
                    RebarDetail.V(727.223611, 11.276389, "", -0.198912),
                    RebarDetail.V(700, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(-700, 0, "", -0.198912),
                    RebarDetail.V(-727.223611, 11.276389),
                    RebarDetail.V(-795.183773, 79.236551, "", 0.174651),
                    RebarDetail.V(-822.920324, 90),
                    RebarDetail.V(-1242.25, 90),
                    RebarDetail.V(-1242.25, 61.9),
                ],
            };
            n7.PlaceDetail(doc, new XYZ(0, 8000, 0), new XYZ(0, 6900, 0), scale: 50, options);
        }

        // N8：直线筋（2N8）
        {
            double half = halfStraight;
            double lx = -half,
                rx = half;
            n8 = new Rebar
            {
                Number = "2N8",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+656",
                Vertices =
                [
                    RebarDetail.V(-1242.25, -28.1),
                    RebarDetail.V(-1242.25, 0),
                    RebarDetail.V(1242.25, 0),
                    RebarDetail.V(1242.25, -28.1),
                ],
            };
            n8.PlaceDetail(doc, new XYZ(0, 3000, 0), new XYZ(0, 1900, 0), scale: 50, options);
        }

        // N9：直线筋（2N9）
        {
            double half = halfStraight;
            double lx = -half,
                rx = half,
                fy = -hook;
            n9 = new Rebar
            {
                Number = "2N9",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+656",
                Vertices =
                [
                    RebarDetail.V(-1242.25, -28.1),
                    RebarDetail.V(-1242.25, 0),
                    RebarDetail.V(1242.25, 0),
                    RebarDetail.V(1242.25, -28.1),
                ],
            };
            n9.PlaceDetail(doc, new XYZ(0, -2000, 0), new XYZ(0, -3100, 0), scale: 50, options);
        }

        // N10：直线筋（16N10）
        {
            double half = halfStraight;
            double lx = -half,
                rx = half,
                fy = -hook;
            n10 = new Rebar
            {
                Number = "16N10",
                Count = 16,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+656",
                Vertices =
                [
                    RebarDetail.V(-1242.25, 28.1),
                    RebarDetail.V(-1242.25, 0),
                    RebarDetail.V(1242.25, 0),
                    RebarDetail.V(1242.25, 28.1),
                ],
            };
            n10.PlaceDetail(doc, new XYZ(0, -7000, 0), new XYZ(0, -8100, 0), scale: 50, options);
        }

        // N11：直线筋（8N11）
        {
            double half = halfStraight;
            double lx = -half,
                rx = half,
                fy = -hook;
            n11 = new Rebar
            {
                Number = "8N11",
                Count = 8,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2(H2-d)+1256",
                Vertices =
                [
                    RebarDetail.V(-1214.15, 160),
                    RebarDetail.V(-1242.25, 160),
                    RebarDetail.V(-1242.25, 35, "", 0.414214),
                    RebarDetail.V(-1207.25, 0),
                    RebarDetail.V(1207.25, 0, "", 0.414214),
                    RebarDetail.V(1242.25, 35),
                    RebarDetail.V(1242.25, 160),
                    RebarDetail.V(1214.15, 160),
                ],
            };
            n11.PlaceDetail(doc, new XYZ(0, -12000, 0), new XYZ(0, -13100, 0), scale: 50, options);
        }

        // N12：下折筋，每侧净长 6045
        {
            double half = 6045 + m / 2 + 2 * ts + diag2;
            double lx = -half,
                rx = half,
                fy = -hook;
            n12 = new Rebar
            {
                Number = "2N12",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H2-2d)/sin45° +1076",
                Vertices =
                [
                    RebarDetail.V(-1214.15, 50),
                    RebarDetail.V(-1242.25, 50),
                    RebarDetail.V(-1242.25, -75, "", 0.414214),
                    RebarDetail.V(-1207.25, -110),
                    RebarDetail.V(-602.777045, -110, "", 0.177199),
                    RebarDetail.V(-575.183773, -99.236551),
                    RebarDetail.V(-487.223611, -11.276389, "", -0.198912),
                    RebarDetail.V(-460, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(460, 0, "", -0.198912),
                    RebarDetail.V(487.223611, -11.276389),
                    RebarDetail.V(575.183773, -99.236551, "", 0.177199),
                    RebarDetail.V(602.777045, -110),
                    RebarDetail.V(1207.25, -110, "", 0.414214),
                    RebarDetail.V(1242.25, -75),
                    RebarDetail.V(1242.25, 50),
                    RebarDetail.V(1214.15, 50),
                ],
            };
            n12.PlaceDetail(doc, new XYZ(0, -17000, 0), new XYZ(0, -18100, 0), scale: 50, options);
        }

        // N13：下折筋，每侧净长 5245
        {
            double half = 5245 + m / 2 + 2 * ts + diag2;
            double lx = -half,
                rx = half,
                fy = -hook;
            n13 = new Rebar
            {
                Number = "2N13",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H2-2d)/sin45° +1076",
                Vertices =
                [
                    RebarDetail.V(-1214.15, 50),
                    RebarDetail.V(-1242.25, 50),
                    RebarDetail.V(-1242.25, -75, "", 0.414214),
                    RebarDetail.V(-1207.25, -110),
                    RebarDetail.V(-682.777045, -110, "", 0.177199),
                    RebarDetail.V(-655.183773, -99.236551),
                    RebarDetail.V(-567.223611, -11.276389, "", -0.198912),
                    RebarDetail.V(-540, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(540, 0, "", -0.198912),
                    RebarDetail.V(567.223611, -11.276389),
                    RebarDetail.V(655.183773, -99.236551, "", 0.177199),
                    RebarDetail.V(682.777045, -110),
                    RebarDetail.V(1207.25, -110.000017, "", 0.414214),
                    RebarDetail.V(1242.25, -75.000017),
                    RebarDetail.V(1242.25, 49.999983),
                    RebarDetail.V(1214.15, 49.999983),
                ],
            };
            n13.PlaceDetail(doc, new XYZ(0, -22000, 0), new XYZ(0, -23100, 0), scale: 50, options);
        }

        // N14：下折筋，每侧净长 4545
        {
            double half = 4545 + m / 2 + 2 * ts + diag2;
            double lx = -half,
                rx = half,
                fy = -hook;
            n14 = new Rebar
            {
                Number = "2N14",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H2-2d)/sin45° +1076",
                Vertices =
                [
                    RebarDetail.V(-1214.15, 50),
                    RebarDetail.V(-1242.25, 50),
                    RebarDetail.V(-1242.25, -75, "", 0.414214),
                    RebarDetail.V(-1207.25, -110),
                    RebarDetail.V(-752.777045, -110, "", 0.177199),
                    RebarDetail.V(-725.183773, -99.236551),
                    RebarDetail.V(-637.223611, -11.276389, "", -0.198912),
                    RebarDetail.V(-610, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(610, 0, "", -0.198912),
                    RebarDetail.V(637.223611, -11.276389),
                    RebarDetail.V(725.183773, -99.236551, "", 0.177199),
                    RebarDetail.V(752.777045, -110),
                    RebarDetail.V(1207.25, -110, "", 0.414214),
                    RebarDetail.V(1242.25, -75),
                    RebarDetail.V(1242.25, 50),
                    RebarDetail.V(1214.15, 50),
                ],
            };
            n14.PlaceDetail(doc, new XYZ(0, -27000, 0), new XYZ(0, -28100, 0), scale: 50, options);
        }

        // N15：下折筋，每侧净长 3945
        {
            double half = 3945 + m / 2 + 2 * ts + diag2;
            double lx = -half,
                rx = half,
                fy = -hook;
            n15 = new Rebar
            {
                Number = "2N15",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H2-2d)/sin45° +1076",
                Vertices =
                [
                    RebarDetail.V(-1214.15, 50),
                    RebarDetail.V(-1242.25, 50),
                    RebarDetail.V(-1242.25, -75, "", 0.414214),
                    RebarDetail.V(-1207.25, -110),
                    RebarDetail.V(-812.777045, -110, "", 0.177199),
                    RebarDetail.V(-785.183773, -99.236551),
                    RebarDetail.V(-697.223611, -11.276389, "", -0.198912),
                    RebarDetail.V(-670, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(670, 0, "", -0.198912),
                    RebarDetail.V(697.223611, -11.276389),
                    RebarDetail.V(785.183773, -99.236551, "", 0.177199),
                    RebarDetail.V(812.777045, -110),
                    RebarDetail.V(1207.25, -110, "", 0.414214),
                    RebarDetail.V(1242.25, -75),
                    RebarDetail.V(1242.25, 50),
                    RebarDetail.V(1214.15, 50),
                ],
            };
            n15.PlaceDetail(doc, new XYZ(0, -32000, 0), new XYZ(0, -33100, 0), scale: 50, options);
        }

        // N16：下折筋，每侧净长 3445
        {
            double half = 3445 + m / 2 + 2 * ts + diag2;
            double lx = -half,
                rx = half,
                fy = -hook;
            n16 = new Rebar
            {
                Number = "2N16",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H2-2d)/sin45° +1076",
                Vertices =
                [
                    RebarDetail.V(-1214.15, 50),
                    RebarDetail.V(-1242.25, 50),
                    RebarDetail.V(-1242.25, -75, "", 0.414214),
                    RebarDetail.V(-1207.25, -110),
                    RebarDetail.V(-862.777045, -110, "", 0.177199),
                    RebarDetail.V(-835.183773, -99.236551),
                    RebarDetail.V(-747.223611, -11.276389, "", -0.198912),
                    RebarDetail.V(-720, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(720, 0, "", -0.198912),
                    RebarDetail.V(747.223611, -11.276389),
                    RebarDetail.V(835.183773, -99.236551, "", 0.177199),
                    RebarDetail.V(862.777045, -110),
                    RebarDetail.V(1207.25, -110, "", 0.414214),
                    RebarDetail.V(1242.25, -75),
                    RebarDetail.V(1242.25, 50),
                    RebarDetail.V(1214.15, 50),
                ],
            };
            n16.PlaceDetail(doc, new XYZ(0, -37000, 0), new XYZ(0, -38100, 0), scale: 50, options);
        }

        // N17：下折筋，每侧净长 2945
        {
            double half = 2945 + m / 2 + 2 * ts + diag2;
            double lx = -half,
                rx = half,
                fy = -hook;
            n17 = new Rebar
            {
                Number = "2N17",
                Count = 2,
                Diameter = d,
                SubstituteLength = "A/sin α -2c+2d+2(H2-2d)/sin45° +1076",
                Vertices =
                [
                    RebarDetail.V(-1214.168155, 50),
                    RebarDetail.V(-1242.268155, 50),
                    RebarDetail.V(-1242.268155, -75, "", 0.414214),
                    RebarDetail.V(-1207.268155, -110),
                    RebarDetail.V(-912.777045, -110, "", 0.177199),
                    RebarDetail.V(-885.183773, -99.236551),
                    RebarDetail.V(-797.223611, -11.276389, "", -0.198912),
                    RebarDetail.V(-770, 0),
                    RebarDetail.V(0, 0),
                    RebarDetail.V(770, 0, "", -0.198912),
                    RebarDetail.V(797.223611, -11.276389),
                    RebarDetail.V(885.183773, -99.236551, "", 0.177199),
                    RebarDetail.V(912.777045, -110),
                    RebarDetail.V(1207.268155, -110, "", 0.414214),
                    RebarDetail.V(1242.268155, -75),
                    RebarDetail.V(1242.268155, 50),
                    RebarDetail.V(1214.168155, 50),
                ],
            };
            n17.PlaceDetail(doc, new XYZ(0, -42000, 0), new XYZ(0, -43100, 0), scale: 50, options);
        }

        // N18：竖向钢筋
        n18 = new Rebar
        {
            Number = "16N18",
            Count = 16,
            Diameter = d,
            Vertices =
            [
                RebarDetail.V(159.979871, -401.9),
                RebarDetail.V(159.979871, -430),
                RebarDetail.V(34.979871, -430, "", -0.414214),
                RebarDetail.V(-0.020129, -395),
                RebarDetail.V(0.020129, 395, "", -0.414214),
                RebarDetail.V(35.020129, 430),
                RebarDetail.V(160.020129, 430),
                RebarDetail.V(160.020129, 401.9),
            ],
        };

        // N19：竖向直筋
        n19 = new Rebar
        {
            Number = "24N19",
            Count = 24,
            Diameter = d,
            Vertices =
            [
                RebarDetail.V(28.1, 430),
                RebarDetail.V(0, 430),
                RebarDetail.V(0, -430),
                RebarDetail.V(28.1, -430),
            ],
        };

        // N19：竖向直筋
        n20 = new Rebar
        {
            Number = "24N20",
            Count = 24,
            Diameter = d,
            Vertices =
            [
                RebarDetail.V(28.1, 430),
                RebarDetail.V(0, 430),
                RebarDetail.V(0, -430),
                RebarDetail.V(28.1, -430),
            ],
        };

        DrawObtuseReinforcementSketch(doc);

        #region  框架桥结构
        doc.AddEntities<Entity>(
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1247.25, -870),
                        CadDraw.V(1247.25, -870),
                        CadDraw.V(1247.25, 0),
                        CadDraw.V(-1247.25, 0),
                        CadDraw.V(-1247.25, -870),
                    ],
                    doc.Layer(CadLayers.B04)
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1091.35, -160),
                        CadDraw.V(-1091.35, -720),
                        CadDraw.V(-1044.581, -750),
                        CadDraw.V(1044.581, -750),
                        CadDraw.V(1091.35, -720),
                        CadDraw.V(1091.35, -160),
                        CadDraw.V(857.505, -100),
                        CadDraw.V(-857.505, -100),
                        CadDraw.V(-1091.35, -160),
                    ],
                    doc.Layer(CadLayers.B04)
                ),
            ]
        );

        // 将 N1~N10 无标注钢筋统一插入框架桥结构图指定位置。
        n1.Place(doc, new XYZ(0, -95, 0));
        n2.Place(doc, new XYZ(0, -95, 0));
        n3.Place(doc, new XYZ(0, -95, 0));
        n4.Place(doc, new XYZ(0, -95, 0));
        n5.Place(doc, new XYZ(0, -95, 0));
        n6.Place(doc, new XYZ(0, -95, 0));
        n7.Place(doc, new XYZ(0, -95, 0));
        n8.Place(doc, new XYZ(0, -95, 0));
        n9.Place(doc, new XYZ(0, -95, 0));
        n10.Place(doc, new XYZ(0, -95, 0));
        n11.Place(doc, new XYZ(0, -755, 0));
        n12.Place(doc, new XYZ(0, -755, 0));
        n13.Place(doc, new XYZ(0, -755, 0));
        n14.Place(doc, new XYZ(0, -755, 0));
        n15.Place(doc, new XYZ(0, -755, 0));
        n16.Place(doc, new XYZ(0, -755, 0));
        n17.Place(doc, new XYZ(0, -755, 0));

        n18.Place(doc, new XYZ(-1242.25, -435, 0));
        n19.Place(doc, new XYZ(-1242.25, -435, 0));
        n20.Place(doc, new XYZ(-1096.35, -435, 0));
        n18.Place(
            doc,
            new XYZ(1242.25, -435, 0),
            transform: new PlaceTransform() { Rotation = Math.PI }
        );
        n19.Place(
            doc,
            new XYZ(1242.25, -435, 0),
            transform: new PlaceTransform() { Rotation = Math.PI }
        );
        n20.Place(
            doc,
            new XYZ(1096.35, -435, 0),
            transform: new PlaceTransform() { Rotation = Math.PI }
        );

        // 顶底系筋
        var lines = new List<Entity>();
        lines.AddRange(
            CadDraw.LinesBetweenBoundaries(
                CadDraw.Polyline([new XY(-1083, 0), new XY(0, 0)], doc.Layer(CadLayers.B01)),
                CadDraw.Polyline(
                    [new XY(-1083, -157.6), new(-857.505, -100), new(0, -100)],
                    doc.Layer(CadLayers.B01)
                ),
                -1083,
                0,
                12.5,
                doc.Layer(CadLayers.B01)
            )
        );
        lines.AddRange(
            CadDraw.LinesBetweenBoundaries(
                CadDraw.Polyline(
                    [new XY(-1083, 0 - 870), new XY(0, 0 - 870)],
                    doc.Layer(CadLayers.B01)
                ),
                CadDraw.Polyline(
                    [new XY(-1083, -725.3561), new(-1044.5810, -750.0000), new(0, -750)],
                    doc.Layer(CadLayers.B01)
                ),
                -1083,
                0,
                12.5,
                doc.Layer(CadLayers.B01)
            )
        );

        doc.AddEntities(lines);
        doc.AddMirroredAcrossVertical(lines);

        // 钢筋断面

        var sections = new List<Entity>();
        for (int i = 0; i < 53; i++)
        {
            var x = 1239.25 + i * 12.5;
            sections.Add(SteelSection.Insert(doc, new XYZ(x, -8, 0)));
            sections.Add(SteelSection.Insert(doc, new XYZ(x, -870 + 8, 0)));
        }

        sections.AddRange(
            CadDraw.EntityArrayAlongPolyline(
                SteelSection.Insert(doc),
                CadDraw.Polyline(
                    [new XY(-1083, -725.3561), new(-1044.5810, -750.0000), new(0, -750)],
                    doc.Layer(CadLayers.B01)
                ),
                12.5,
                followPath: true
            )
        );
        sections.AddRange(
            CadDraw.EntityArrayAlongPolyline(
                SteelSection.Insert(doc),
                CadDraw.Polyline(
                    [new XY(-1083, -157.6), new(-857.505, -100), new(0, -100)],
                    doc.Layer(CadLayers.B01)
                ),
                12.5,
                followPath: true
            )
        );

        doc.AddEntities(sections);
        doc.AddMirroredAcrossVertical(sections);

        // 左右系筋
        var lines2 = new List<Entity>();
        lines2.AddRange(
            CadDraw.EntityArray(
                CadDraw.Line(
                    new XYZ(-1242.2531, -100.0000, 0),
                    new XYZ(-1242.2531 + 145.9, -100.0000, 0),
                    doc.Layer(CadLayers.B01)
                ),
                new XYZ(0, -12.5, 0),
                53
            )
        );
        doc.AddEntities(lines2);
        doc.AddMirroredAcrossVertical(lines2);

        #endregion

        var tieData = new List<string[]>
        {
            new[] { "位置", "编号", "D", "L1", "H", "L" },
            new[] { "顶板", "N23", "28", "102", "H1-2d", "H1-2d+160" },
            new[]
            {
                "顶板",
                "N23-1~18",
                "28",
                "102",
                "(H1-2d)~(H1+y1-2d)",
                "(H1-2d+160)~(H1+y1-2d+160)",
            },
            new[] { "底板", "N24", "28", "102", "H2-2d", "H2-2d+160" },
            new[]
            {
                "底板",
                "N24-1~3",
                "28",
                "102",
                "(H2-2d)~(H2+y2-2d)",
                "(H2-2d+160)~(H2+y2-2d+160)",
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
            new[] { "N1", "28", "2", "28934", "57.868", "4.83", "279.50" },
            new[] { "N2", "28", "2", "28934", "57.868", "4.83", "279.50" },
            new[] { "N3", "28", "2", "28934", "57.868", "4.83", "279.50" },
            new[] { "N4", "28", "2", "28934", "57.868", "4.83", "279.50" },
            new[] { "N5", "28", "2", "28934", "57.868", "4.83", "279.50" },
            new[] { "N6", "28", "2", "26034", "52.068", "4.83", "251.49" },
            new[] { "N7", "28", "2", "26034", "52.068", "4.83", "251.49" },
            new[] { "N8", "28", "2", "25471", "50.942", "4.83", "246.05" },
            new[] { "N9", "28", "2", "25471", "50.942", "4.83", "246.05" },
            new[] { "N10", "28", "16", "25471", "407.536", "4.83", "1968.40" },
            new[] { "N11", "28", "8", "28371", "226.968", "4.83", "1096.26" },
            new[] { "N12", "28", "2", "29104", "58.208", "4.83", "281.14" },
            new[] { "N13", "28", "2", "29104", "58.208", "4.83", "281.14" },
            new[] { "N14", "28", "2", "29104", "58.208", "4.83", "281.14" },
            new[] { "N15", "28", "2", "29104", "58.208", "4.83", "281.14" },
            new[] { "N16", "28", "2", "29104", "58.208", "4.83", "281.14" },
            new[] { "N17", "28", "2", "25504", "51.008", "4.83", "246.37" },
            new[] { "N18", "28", "16", "12126", "194.016", "4.83", "937.10" },
            new[] { "N19", "28", "24", "9226", "221.424", "4.83", "1069.48" },
            new[] { "N20", "16", "16", "8930", "142.88", "1.58", "225.75" },
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
    }

    private static void DrawObtuseReinforcementSketch(CadDocument doc)
    {
        var steel = doc.Layer(CadLayers.B01);
        var dim = doc.Layer(CadLayers.B03);
        var text = doc.Layer(CadLayers.B07);

        double ox = 30000,
            oy = 12000;
        double top = 17000,
            bottom = 6500,
            right = 18000;

        TitleBlock.Add(
            doc,
            "框架顶板上部和底板下部边墙钝角加强钢筋布置示意图",
            "",
            CadDraw.P(ox + 9000, oy + 12500),
            18
        );

        // 绿色外轮廓与边墙折线。
        doc.AddEntities<Entity>(
            [
                CadDraw.Line(CadDraw.P(ox - 1000, oy + bottom), CadDraw.P(ox + 900, oy + top), dim),
                CadDraw.Line(CadDraw.P(ox + 900, oy + top), CadDraw.P(ox + right, oy + top), dim),
                CadDraw.Line(
                    CadDraw.P(ox - 1000, oy + bottom),
                    CadDraw.P(ox + right - 1200, oy + bottom),
                    dim
                ),
                CadDraw.Line(
                    CadDraw.P(ox + right - 1200, oy + bottom),
                    CadDraw.P(ox + right, oy + top),
                    dim
                ),
            ]
        );

        // 红色板内斜向钢筋网格。
        for (double x = ox - 700; x < ox + right; x += 650)
        {
            double y0 = oy + bottom;
            double y1 = oy + top;
            doc.Entities.Add(
                CadDraw.Line(CadDraw.P(x, y0), CadDraw.P(x + (top - bottom) * 1.15, y1), steel)
            );
        }
        for (double y = oy + bottom + 500; y < oy + top; y += 650)
        {
            double offset = (y - (oy + bottom)) * 1.15;
            doc.Entities.Add(
                CadDraw.Line(
                    CadDraw.P(ox - 600 + offset, y),
                    CadDraw.P(ox + right - 600 + offset, y),
                    steel
                )
            );
        }

        // 绿色尺寸与说明。
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(ox + 7000, oy + top),
                CadDraw.P(ox + 16000, oy + top),
                CadDraw.P(ox + 7000, oy + top + 900),
                dim,
                "30×12.5cm"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(ox - 1000, oy + bottom),
                CadDraw.P(ox - 1000, oy + top),
                CadDraw.P(ox - 2200, oy + bottom),
                dim,
                "20×12.5cm"
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(ox + 4200, oy + top - 2500),
                CadDraw.P(ox + 5600, oy + top - 2500),
                dim
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(ox + 5600, oy + top - 2500),
                CadDraw.P(ox + 6700, oy + top - 1900),
                dim
            )
        );
        doc.Entities.Add(CadDraw.Text("N27", CadDraw.P(ox + 3600, oy + top - 2500), TextH, text));
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(ox + 2200, oy + bottom),
                CadDraw.P(ox + 1200, oy + bottom - 900),
                dim
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(ox + 1200, oy + bottom - 900),
                CadDraw.P(ox + 500, oy + bottom - 900),
                dim
            )
        );
        doc.Entities.Add(CadDraw.Text("N28", CadDraw.P(ox + 600, oy + bottom - 900), TextH, text));
        doc.Entities.Add(
            CadDraw.Text("边墙", CadDraw.P(ox - 1800, oy + bottom + 500), TextH, text)
        );
    }
}
