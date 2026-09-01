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
        // AI 识别复刻：第一批 cad-region.json 中的两个线性阵列。
        // 源图 gj 映射为 B-01；源图 B-03 标注_指示标注映射为 B-03。
        // 两组均沿 X 方向每 30 个单位平移，共 24 个元素。
        doc.AddTranslated(
            CadDraw.P(606.03342472644, 490, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(-574.0735597332762, -480, 0),
                    doc.Layer(CadLayers.B01)
                ),
                CadDraw.P(30, 0, 0),
                24
            )
        );

        doc.AddTranslated(
            CadDraw.P(433.2989248270278, 359.1042417199187, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(11.27631144942643, -4.1042417199169, 0),
                    doc.Layer(CadLayers.B03)
                ),
                CadDraw.P(30, 0, 0),
                24
            )
        );

        // AI 识别复刻：第二批 cad-region.json 中的两个线性阵列。
        // 两组均沿 (35.8795974333334, 30, 0) 平移，共 17 个元素。
        doc.AddTranslated(
            CadDraw.P(31.95986579316377, 10, 0),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(690, 0, 0), doc.Layer(CadLayers.B01)),
                CadDraw.P(35.8795974333334, 30, 0),
                17
            )
        );

        doc.AddTranslated(
            CadDraw.P(165.70492492870835, 9.999999999996362, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(-11.824894782, -2.042513989996223, 0),
                    doc.Layer(CadLayers.B03)
                ),
                CadDraw.P(35.8795974333334, 30, 0),
                17
            )
        );

        // AI 识别复刻：cad-region.json 中的标题块“主筋骨架示意图”。
        TitleBlock.Add(
            doc,
            "主筋骨架示意图",
            "",
            CadDraw.P(14955.19111009196, 2061.446111203868, 0),
            9
        );

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

        // var sections = new List<Entity>();
        // for (int i = 0; i < 53; i++)
        // {
        //     var x = 1239.25 + i * 12.5;
        //     sections.Add(SteelSection.Insert(doc, new XYZ(x, -8, 0)));
        //     sections.Add(SteelSection.Insert(doc, new XYZ(x, -870 + 8, 0)));
        // }

        // sections.AddRange(
        //     CadDraw.EntityArrayAlongPolyline(
        //         SteelSection.Insert(doc),
        //         CadDraw.Polyline(
        //             [new XY(-1083, -725.3561), new(-1044.5810, -750.0000), new(0, -750)],
        //             doc.Layer(CadLayers.B01)
        //         ),
        //         12.5,
        //         followPath: true
        //     )
        // );
        // sections.AddRange(
        //     CadDraw.EntityArrayAlongPolyline(
        //         SteelSection.Insert(doc),
        //         CadDraw.Polyline(
        //             [new XY(-1083, -157.6), new(-857.505, -100), new(0, -100)],
        //             doc.Layer(CadLayers.B01)
        //         ),
        //         12.5,
        //         followPath: true
        //     )
        // );

        // doc.AddEntities(sections);
        // doc.AddMirroredAcrossVertical(sections);

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

        DrawAiMainRebarSkeletonRegion(doc);
        DrawAiNotesRegion(doc);
        DrawAiObtuseReinforcementRegion(doc);
        DrawAiFrameBodyRebarQuantityRegion(doc);
        DrawAiFrameBodyRebarSectionRegion(doc);
    }

    private static void DrawAiObtuseReinforcementRegion(CadDocument doc)
    {
        // Generated from cad-region.json: obtuse-corner reinforcement layout.
        var rebarLayer = doc.Layer(CadLayers.B01);
        var annotationLayer = doc.Layer(CadLayers.B03);

        // Four detected arrays from the source grid.
        doc.AddTranslated(
            CadDraw.P(165.70492492870835, 9.999999999996362, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(-11.824894782, -2.042513989996223, 0),
                    annotationLayer
                ),
                CadDraw.P(35.8795974333334, 30, 0),
                17
            )
        );
        doc.AddTranslated(
            CadDraw.P(433.2989248270278, 359.1042417199187, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(11.27631144942643, -4.1042417199169, 0),
                    annotationLayer
                ),
                CadDraw.P(30, 0, 0),
                24
            )
        );
        doc.AddTranslated(
            CadDraw.P(606.03342472644, 490, 0),
            CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(0, 0, 0),
                    CadDraw.P(-574.0735597332762, -480, 0),
                    rebarLayer
                ),
                CadDraw.P(30, 0, 0),
                24
            )
        );

        doc.AddTranslated(
            CadDraw.P(31.95986579316377, 10, 0),
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(0, 0, 0), CadDraw.P(690, 0, 0), rebarLayer),
                CadDraw.P(35.8795974333334, 30, 0),
                17
            )
        );

        // Outer obtuse-corner boundary and leader geometry.
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(105.99153499659587, -39.928143739434745, 0),
                CadDraw.P(49.53380766288319, -39.928143739434745, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(-98.04013417093665, 50, 0),
                CadDraw.P(71.73598924546968, 50, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(739.7784838620137, 490, 0),
                CadDraw.P(106.32612287750817, -39.64838484732536, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(351.31399388602586, 355, 0),
                CadDraw.P(1134.5752362764615, 355, 0),
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Polyline(
                [
                    CadDraw.V(597.9932905554888, 500),
                    CadDraw.V(0, 0),
                    CadDraw.V(236.95986582906335, 0),
                    CadDraw.V(236.95986582906335, 30),
                    CadDraw.V(266.95986582906335, -30),
                    CadDraw.V(266.95986582906335, 0),
                    CadDraw.V(729.999999964114, 0),
                    CadDraw.V(1327.9932905196026, 499.99999999999994),
                ],
                annotationLayer,
                closed: true
            )
        );

        // N27/N28 leader labels.
        doc.Entities.Add(
            CadDraw.Text(
                "N27",
                CadDraw.P(355.72723430710903, 359.93762364465874, 0),
                27,
                annotationLayer
            )
        );
        doc.Entities.Add(
            CadDraw.Text(
                "N28",
                CadDraw.P(52.07538770890096, -34.774013947098865, 0),
                27,
                annotationLayer
            )
        );

        // Dimension annotations reconstructed from measured geometry and text position.
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(0, -39.928143739434745, 0),
                CadDraw.P(0, 440.07185626056525, 0),
                CadDraw.P(-41.441410468320086, 250, 0),
                annotationLayer,
                "20×12.5cm"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(606.03342472644, 490, 0),
                CadDraw.P(1296.03342472644, 490, 0),
                CadDraw.P(951.03342472644, 542.2628455574231, 0),
                annotationLayer,
                "30×12.5cm"
            )
        );
    }

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

    private static void DrawAiMainRebarSkeletonRegion(CadDocument doc)
    {
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
            CadDraw.RotatedDimension(
                CadDraw.P(997.035100365321, -686.6559752986, 0),
                CadDraw.P(1043.80410036532, -686.6559752986, 0),
                CadDraw.P(1020.41960036532, -686.6559752986, 0),
                annotationLayer,
                "30/sin39.9\u00b0"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(980.824634413832, -735, 0),
                CadDraw.P(1010.82463441383, -735, 0),
                CadDraw.P(995.824634413832, -735, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(857.505000000001, -185.052703659805, 0),
                CadDraw.P(1091.35, -185.052703659805, 0),
                CadDraw.P(974.4275, -185.052703659805, 0),
                annotationLayer,
                "150/sin39.9\u00b0"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(735.075230516268, -130, 0),
                CadDraw.P(795.075230516268, -130, 0),
                CadDraw.P(765.075230516268, -130, 0),
                annotationLayer,
                "50"
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
            CadDraw.RotatedDimension(
                CadDraw.P(939.005743930287, -476.651029627981, 0),
                CadDraw.P(1094.90574393029, -476.651029627981, 0),
                CadDraw.P(1016.95574393029, -476.651029627981, 0),
                annotationLayer,
                "100/sin39.9\u00b0"
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
            CadDraw.RotatedDimension(
                CadDraw.P(209.799190833651, -810, 0),
                CadDraw.P(329.799190833652, -810, 0),
                CadDraw.P(269.799190833651, -810, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(219.79919083361, -49.9999999999983, 0),
                CadDraw.P(319.799190833606, -49.9999999999983, 0),
                CadDraw.P(269.799190833608, -49.9999999999983, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-55.2008091664358, -425, 0),
                CadDraw.P(594.799190833564, -425, 0),
                CadDraw.P(269.799190833564, -425, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(848.824014289694, -435, 0),
                CadDraw.P(1718.82401428969, -435, 0),
                CadDraw.P(1283.82401428969, -435, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1091.35, -470.259137736089, 0),
                CadDraw.P(1091.34999999999, -470.259137736089, 0),
                CadDraw.P(-1.81780199262912e-12, -470.259137736089, 0),
                annotationLayer,
                "1400/sin39.9\u00b0=<>"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1094.90574393029, -476.651029627981, 0),
                CadDraw.P(-939.005743930287, -476.651029627981, 0),
                CadDraw.P(-1016.95574393029, -476.651029627981, 0),
                annotationLayer,
                "100/sin39.9\u00b0"
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
            CadDraw.RotatedDimension(
                CadDraw.P(-1043.80410036532, -686.6559752986, 0),
                CadDraw.P(-997.035100365321, -686.6559752986, 0),
                CadDraw.P(-1020.41960036532, -686.6559752986, 0),
                annotationLayer,
                "30/sin39.9\u00b0"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1045.82463441378, -735, 0),
                CadDraw.P(-1015.82463441378, -735, 0),
                CadDraw.P(-1030.82463441378, -735, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1091.35, -181.771371587007, 0),
                CadDraw.P(-857.505000000001, -181.771371587007, 0),
                CadDraw.P(-974.4275, -181.771371587007, 0),
                annotationLayer,
                "150/sin39.9\u00b0"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-871.831483995178, -130, 0),
                CadDraw.P(-811.831483995178, -130, 0),
                CadDraw.P(-841.831483995178, -130, 0),
                annotationLayer,
                "50"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(8.00000000000728, 95.5803508671233, 0),
                CadDraw.P(1083, 95.5803508671233, 0),
                CadDraw.P(545.500000000004, 95.5803508671233, 0),
                annotationLayer,
                "86\u00d712.5"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(1083, 95.5803508671233, 0),
                CadDraw.P(1247.25, 95.5803508671233, 0),
                CadDraw.P(1165.125, 95.5803508671233, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-8.00000000000727, 95.5803508671197, 0),
                CadDraw.P(8.00000000000728, 95.5803508671197, 0),
                CadDraw.P(6.40063646745469e-15, 95.5803508671197, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1083, 95.5803508671233, 0),
                CadDraw.P(-8.00000000000728, 95.5803508671233, 0),
                CadDraw.P(-545.500000000004, 95.5803508671233, 0),
                annotationLayer,
                "86\u00d712.5"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1247.25, 95.5803508671233, 0),
                CadDraw.P(-1083, 95.5803508671233, 0),
                CadDraw.P(-1165.125, 95.5803508671233, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(8.00000000000728, -929.580350867123, 0),
                CadDraw.P(1083, -929.580350867123, 0),
                CadDraw.P(545.500000000004, -929.580350867123, 0),
                annotationLayer,
                "86\u00d712.5"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(1083, -929.580350867123, 0),
                CadDraw.P(1247.25, -929.580350867123, 0),
                CadDraw.P(1165.125, -929.580350867123, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1364.39943884095, -813.999999999998, 0),
                CadDraw.P(-1252.39943884094, -813.999999999998, 0),
                CadDraw.P(-1308.39943884095, -813.999999999998, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1316.39943884099, -749.999999999994, 0),
                CadDraw.P(-1300.39943884099, -749.999999999994, 0),
                CadDraw.P(-1308.39943884099, -749.999999999994, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1633.39943884096, -416.999999999996, 0),
                CadDraw.P(-983.399438840956, -416.999999999996, 0),
                CadDraw.P(-1308.39943884096, -416.999999999996, 0),
                annotationLayer,
                "52\u00d712.5"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1354.39943884106, -45.9999999999997, 0),
                CadDraw.P(-1262.39943884106, -45.9999999999997, 0),
                CadDraw.P(-1308.39943884106, -45.9999999999997, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-8.00000000000728, -929.580350867123, 0),
                CadDraw.P(8.00000000000727, -929.580350867123, 0),
                CadDraw.P(-4.25750456894682e-15, -929.580350867123, 0),
                annotationLayer,
                ""
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1083, -929.580350867123, 0),
                CadDraw.P(-8.00000000000728, -929.580350867123, 0),
                CadDraw.P(-545.500000000004, -929.580350867123, 0),
                annotationLayer,
                "86\u00d712.5"
            )
        );
        doc.Entities.Add(
            CadDraw.RotatedDimension(
                CadDraw.P(-1247.25, -929.580350867123, 0),
                CadDraw.P(-1083, -929.580350867123, 0),
                CadDraw.P(-1165.125, -929.580350867123, 0),
                annotationLayer,
                ""
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
        doc.Entities.Add(
            CadDraw.Text(
                "24115",
                CadDraw.P(-47.9811011413067, -1027.19488081657, 0),
                25,
                annotationLayer,
                rotation: 0
            )
        );
    }

    private static void DrawAiFrameBodyRebarSectionRegion(CadDocument doc)
    {
        // // 最新 cad-region.json：1/4 框架桥断面钢筋。圆形钢筋断面统一替换为 SteelSection 块。
        // var quarter = new List<Entity>();

        // void AddArray(double x, double y, double dx, double dy, int count)
        // {
        //     foreach (
        //         var entity in CadDraw.EntityArray(
        //             SteelSection.Insert(doc),
        //             CadDraw.P(dx, dy, 0),
        //             count
        //         )
        //     )
        //     {
        //         entity.ApplyTranslation(CadDraw.P(x, y, 0));
        //         quarter.Add(entity);
        //     }
        // }

        // // 1/4 断面中的四条主要钢筋排：钢筋净距按 12.5 布置。
        // AddArray(-1239.25, -417, 0, 12.5, 27);
        // AddArray(-1099.35, -417, 0, 12.5, 27);
        // AddArray(-11.85, -8, -12.5, 0, 99);
        // AddArray(-11.85, -92, -12.5, 0, 88);

        void AddSymmetricArray(
            double x,
            double y,
            double stepX,
            double stepY,
            int count,
            double horizontalAxisY,
            string comment
        )
        {
            IEnumerable<Entity> Templates(double sx, double sy) =>
                CadDraw.EntityArray(SteelSection.Insert(doc), CadDraw.P(sx, sy, 0), count);

            // 原始 1/4 断面。
            doc.AddTransformed(
                new XYZ(x, y, 0),
                Templates(stepX, stepY)
            ); // {comment}

            // 关于水平中心线镜像：直接反向 Y 步进，避免块插入实体负缩放失效。
            doc.AddTransformed(
                new XYZ(x, 2 * horizontalAxisY - y, 0),
                Templates(stepX, -stepY)
            );

            // 关于竖直中心线 x = 0 镜像：直接反向 X 步进。
            doc.AddTransformed(
                new XYZ(-x, y, 0),
                Templates(-stepX, stepY)
            );

            // 同时关于水平、竖直中心线镜像。
            doc.AddTransformed(
                new XYZ(-x, 2 * horizontalAxisY - y, 0),
                Templates(-stepX, -stepY)
            );
        }

        AddSymmetricArray(-1239.25, -8, 12.5, 0, 11, -435, "左侧壁厚"); // 外框
        AddSymmetricArray(-1099.35, -8, 12.5, 0, 88, -435, "中间部分"); // 外框
        AddSymmetricArray(-1239.25, -8, 0, -12.5, 7, -435, "上部壁厚"); // 外框
        AddSymmetricArray(-1239.25, -92, 0, -12.5, 27, -435, "中间部分"); // 外框
        AddSymmetricArray(-1099.35, -92, 12.5, 0, 88, -425, "中间部分"); // 内框
        AddSymmetricArray(-1099.35, -92, 0, -12.5, 27, -425, "中间部分"); // 内框

        // // 1/4 断面中不完整的端部排，按 JSON 中的实际端点补齐。
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -83, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -70.5, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -58, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -45.5, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -33, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -20.5, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -8, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1099.35, -92, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1099.35, -8, 0)));
        // quarter.Add(SteelSection.Insert(doc, new XYZ(-1239.25, -92, 0)));

        // doc.AddEntities(quarter);

        // // 断面关于竖直 x=0、水平 y=-417 对称，补齐其余 3/4。
        // var horizontalMirror = new List<Entity>();
        // foreach (var entity in quarter)
        // {
        //     var copy = (Entity)entity.Clone();
        //     copy.ApplyScaling(new XYZ(1, -1, 1), new XYZ(0, -417, 0));
        //     horizontalMirror.Add(copy);
        // }

        // doc.AddMirroredAcrossVertical(quarter, axisX: 0);
        // doc.AddEntities(horizontalMirror);
        // doc.AddMirroredAcrossVertical(horizontalMirror, axisX: 0);

        // // JSON 中唯一的边界线（点线实体无可见长度，保留其语义位置）。
        // doc.Entities.Add(
        //     CadDraw.Line(
        //         CadDraw.P(-1091.35, -167, 0),
        //         CadDraw.P(-1091.35, -167, 0),
        //         doc.Layer(CadLayers.B03)
        //     )
        // );
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
