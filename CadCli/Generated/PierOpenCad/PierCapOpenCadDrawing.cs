using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Drawing;
using DwgSharpKit.Rebar;
using DwgSharpKit.Standards;
using DwgSharpKit.Tables;

namespace CadCli.Generated.PierOpenCad;

/// <summary>
/// 由 OpenCAD Studio 分区导出（shot-01-region-*.json）逐区还原的 04 桥墩顶帽钢筋图。
/// 一个区域一个方法，坐标保留源图绝对坐标，图层按标准图集映射为 B-01/B-03/B-04/B-06/B-07。
/// 同图层内端点距离 ≤ 0.5 mm 的直线、多段线与圆弧已先行合并为多段线；
/// 等间距的直线、多段线、钢筋断面族用 CadDraw.EntityArray 展开为阵列，不再逐条抄写。
/// 钢筋断面是圆：旋转可忽略、± 缩放等价，统一写成 0 旋转 / 正缩放 / B-01 图层，同类断面才能并族。
/// 图题整段交给 TitleBlock.Add，钢筋数量表（23×7，末行合并 2~5 列）整段交给 TableHelper；
/// 区域 04 的 20 根钢筋按 Rebar/Stirrup 建模（编号/直径/段标注/长度闭合），见该方法内注释。
/// 块参照映射：HRB400 → HRB400Block，钢筋断面 → SteelSection；_Oblique → 单条 45° 斜线；
/// Defpoints 定位点与 Hatch 填充不还原。
/// </summary>
public static class PierCapOpenCadDrawing
{
    /// <summary>
    /// 绘制 OpenCAD Studio 当前视图（04 桥墩顶帽钢筋图.dwg）的全部 9 个分区：
    /// 侧面钢筋布置图、半Ⅲ-Ⅲ截面、半Ⅳ-Ⅳ截面、钢筋大样图（含箍筋关系示意图）、
    /// 墩帽钢筋数量表、Ⅴ-Ⅴ截面、正面钢筋布置图、两条附注。
    /// </summary>
    public static void Draw(CadDocument doc)
    {
        DrawRegion01(doc);
        DrawRegion02(doc);
        DrawRegion03(doc);
        DrawRegion04(doc);
        DrawRegion05(doc);
        DrawRegion06(doc);
        DrawRegion07(doc);
        DrawRegion08(doc);
        DrawRegion09(doc);
    }

    /// <summary>截图 01 / 区域 1：源图窗口 [55, 100, 258, 288]，387 个实体，6.68×6.20 m。</summary>
    public static void DrawRegion01(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-68523.277, -9701.297);

        List<Entity> entities =
        [
            .. TitleBlock.Add(doc, "侧面钢筋布置图", "1:50", CadDraw.P(-10.698, 1214.791), 50),
            CadDraw.Polyline(
                [
                    CadDraw.V(-986.994, -2156.167, bulge: 0.016404),
                    CadDraw.V(-1228, -500),
                    CadDraw.V(-1228, -100),
                    CadDraw.V(1228, -100),
                    CadDraw.V(1228, -500, bulge: 0.016404),
                    CadDraw.V(986.994, -2156.167),
                ],
                b01
            ),
            CadDraw.Line(CadDraw.P(-1096, -1311.792), CadDraw.P(-1096, -100), b01),
            .. new[]
            {
                CadDraw.P(-1228, -478),
                CadDraw.P(-1228, -356),
                CadDraw.P(-1228, -228),
            }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 2456, p.Y), b01)),
            CadDraw.Line(CadDraw.P(-1131.264, -1078), CadDraw.P(1131.264, -1078), b01),
            CadDraw.Line(CadDraw.P(-995.884, -2078), CadDraw.P(995.884, -2078), b01),
            CadDraw.Line(CadDraw.P(-1019.742, -1878), CadDraw.P(1019.742, -1878), b01),
            CadDraw.Line(CadDraw.P(-1045.205, -1678), CadDraw.P(1045.205, -1678), b01),
            CadDraw.Line(CadDraw.P(-1072.275, -1478), CadDraw.P(1072.275, -1478), b01),
            CadDraw.Line(CadDraw.P(-1100.96, -1278), CadDraw.P(1100.96, -1278), b01),
            CadDraw.Line(CadDraw.P(-480, -2172), CadDraw.P(-480, -100), b01),
            CadDraw.Line(CadDraw.P(-1196.754, -678), CadDraw.P(1196.754, -678), b01),
            CadDraw.Line(CadDraw.P(-1163.193, -878), CadDraw.P(1163.193, -878), b01),
            CadDraw.Line(CadDraw.P(480, -2172), CadDraw.P(480, -100), b01),
            CadDraw.Line(CadDraw.P(1096, -1311.792), CadDraw.P(1096, -100), b01),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1133.747, -3149.996),
                    CadDraw.V(-192.104, -3150),
                    CadDraw.V(-192.104, -3032.607),
                    CadDraw.V(192.104, -3267.393),
                    CadDraw.V(192.104, -3150),
                    CadDraw.V(1134.451, -3130.817),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(93.11, -3271.04),
                    CadDraw.V(93.11, -3415.58),
                    CadDraw.V(-4.432, -3415.58),
                    CadDraw.V(25.046, -3422.941),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(96.195, 399.009),
                    CadDraw.V(96.195, 543.549),
                    CadDraw.V(-1.348, 543.549),
                    CadDraw.V(28.131, 550.91),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-967.164, -3149.996, bulge: 0.02619),
                    CadDraw.V(-1300, -500),
                    CadDraw.V(-1300, 0),
                    CadDraw.V(1300, 0),
                    CadDraw.V(1300, -500, bulge: 0.026034),
                    CadDraw.V(968.317, -3134.199),
                ],
                b04
            ),
            CadDraw.Polyline(
                [CadDraw.V(-1078, -122), CadDraw.V(-930.785, 86.613), CadDraw.V(1655.357, 85.112)],
                b07
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-622.785, 86.613), CadDraw.P(-770, -122), b07),
                CadDraw.P(308, 0),
                7
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(2188.469, -171.229),
                    CadDraw.V(-1022.412, -171.229),
                    CadDraw.V(-1078, -250),
                ],
                b07
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-714.412, -171.229), CadDraw.P(-770, -250), b07),
                CadDraw.P(308, 0),
                7
            ),
            CadDraw.Line(CadDraw.P(-868.412, -299.229), CadDraw.P(-924, -378), b07),
            .. CadDraw.EntityArray(
                CadDraw.Polyline(
                    [CadDraw.V(-770, -500), CadDraw.V(-714.412, -299.229), CadDraw.V(-770, -378)],
                    b07
                ),
                CadDraw.P(154, 0),
                12
            ),
            CadDraw.Line(CadDraw.P(-1126.576, -308.218), CadDraw.P(-1078, -378), b07),
            CadDraw.Line(CadDraw.P(-1958.668, -568.416), CadDraw.P(979.588, -568.416), b07),
            CadDraw.Line(CadDraw.P(-1021.53, -726.973), CadDraw.P(-1078, -900), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1919.595, -1164.394),
                    CadDraw.V(-1010.573, -1164.394),
                    CadDraw.V(-1078, -1100),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(-1021.53, -726.973), CadDraw.P(-1930.552, -726.973), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(-770, -2100),
                    CadDraw.V(-690.979, -1988.022),
                    CadDraw.V(2188.469, -1988.022),
                ],
                b07
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-382.979, -1988.022), CadDraw.P(-462, -2100), b07),
                CadDraw.P(308, 0),
                5
            ),
            CadDraw.Line(CadDraw.P(1078, -378), CadDraw.P(1129.185, -442.921), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(-686.285, -2747.089),
                    CadDraw.V(-686.285, -478),
                    CadDraw.V(-646.285, -578),
                ],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(920.887, -2736.594),
                    CadDraw.V(920.887, -1807.22),
                    CadDraw.V(-480, -1807.22),
                    CadDraw.V(-416.337, -1769.85),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(480, -1807.22), CadDraw.P(543.663, -1769.85), b07),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-686.285, -2078), CadDraw.P(-646.285, -2178), b07),
                CadDraw.P(0, 200),
                8
            ),
            CadDraw.Line(CadDraw.P(-1021.53, -726.973), CadDraw.P(-1078, -700), b07),
            CadDraw.Line(CadDraw.P(-1126.576, -308.218), CadDraw.P(-1078, -500), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1017.357, -2100),
                    CadDraw.V(-1165.234, -2100, bulge: 0.01585),
                    CadDraw.V(-1399.877, -500),
                    CadDraw.V(-1399.877, -350),
                    CadDraw.V(-1399.773, -350),
                    CadDraw.V(-1244, -350),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(999.863, -637.932), CadDraw.P(1078, -900), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(1850.193, -1226.182),
                    CadDraw.V(941.171, -1226.182),
                    CadDraw.V(1078, -1100),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(999.863, -637.932), CadDraw.P(1936.43, -637.932), b07),
            CadDraw.Line(CadDraw.P(999.863, -637.932), CadDraw.P(1078, -700), b07),
            CadDraw.Line(CadDraw.P(1129.185, -442.921), CadDraw.P(1078, -500), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(1244, -350),
                    CadDraw.V(1399.773, -350),
                    CadDraw.V(1399.877, -350),
                    CadDraw.V(1399.877, -500, bulge: 0.01585),
                    CadDraw.V(1165.234, -2100),
                    CadDraw.V(1017.357, -2100),
                ],
                b07
            ),
            CadDraw.Polyline(
                [CadDraw.V(725.049, 646.092), CadDraw.V(725.049, -356), CadDraw.V(670.049, -276)],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1235.251, -789.68),
                    CadDraw.V(1171.588, -827.051),
                    CadDraw.V(1918.103, -827.051),
                ],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1216.441, -904.759),
                    CadDraw.V(-1152.778, -942.13),
                    CadDraw.V(-2016.435, -942.13),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(-924, -500), CadDraw.P(-868.412, -299.229), b07),
            .. CadDraw.EntityArray(
                CadDraw.Polyline(
                    [CadDraw.V(-924, -700), CadDraw.V(-868.412, -568.416), CadDraw.V(-924, -900)],
                    b07
                ),
                CadDraw.P(154, 0),
                12
            ),
            CadDraw.Line(CadDraw.P(924, -700), CadDraw.P(979.588, -568.416), b07),
            CadDraw.Line(CadDraw.P(979.588, -568.416), CadDraw.P(924, -900), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(1886.678, -1021.229),
                    CadDraw.V(-714.412, -1021.229),
                    CadDraw.V(-770, -1100),
                ],
                b07
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-406.412, -1021.229), CadDraw.P(-462, -1100), b07),
                CadDraw.P(308, 0),
                5
            ),
            CadDraw.Line(CadDraw.P(670.049, -148), CadDraw.P(725.049, -228), b07),
            CadDraw.Line(CadDraw.P(670.049, -20), CadDraw.P(725.049, -100), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1244, -225),
                    CadDraw.V(-1399.773, -225),
                    CadDraw.V(-1399.877, -225),
                    CadDraw.V(-1399.877, -100),
                    CadDraw.V(-1399.773, -100),
                    CadDraw.V(-1244, -100),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(-1399.773, -450), CadDraw.P(-1244, -450), b07),
            CadDraw.Line(CadDraw.P(1244, -450), CadDraw.P(1399.773, -450), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(1244, -225),
                    CadDraw.V(1399.773, -225),
                    CadDraw.V(1399.877, -225),
                    CadDraw.V(1399.877, -100),
                    CadDraw.V(1399.773, -100),
                    CadDraw.V(1244, -100),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(-868.412, -299.229), CadDraw.P(2188.469, -299.229), b07),
            CadDraw.Line(CadDraw.P(-2093.644, -308.218), CadDraw.P(-1126.576, -308.218), b07),
            CadDraw.Line(CadDraw.P(1129.185, -442.921), CadDraw.P(2188.469, -442.921), b07),
            .. new[]
            {
                CadDraw.P(-1356.361, -750),
                CadDraw.P(-1331.478, -900),
                CadDraw.P(-1307.513, -1050),
                CadDraw.P(-1284.463, -1200),
                CadDraw.P(-1262.324, -1350),
                CadDraw.P(-1241.096, -1500),
                CadDraw.P(-1220.774, -1650),
                CadDraw.P(-1201.358, -1800),
                CadDraw.P(-1182.846, -1950),
                CadDraw.P(1034.969, -1950),
                CadDraw.P(1053.482, -1800),
                CadDraw.P(1072.898, -1650),
                CadDraw.P(1093.219, -1500),
                CadDraw.P(1114.447, -1350),
                CadDraw.P(1136.586, -1200),
                CadDraw.P(1159.636, -1050),
                CadDraw.P(1183.601, -900),
                CadDraw.P(1208.484, -750),
            }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 147.877, p.Y), b07)),
            CadDraw.Line(CadDraw.P(-1382.163, -600), CadDraw.P(-1235.193, -600), b07),
            CadDraw.Line(CadDraw.P(1235.193, -600), CadDraw.P(1382.163, -600), b07),
            CadDraw.Text("墩身主筋", CadDraw.P(1387.28, -806.167), 125, b07, widthFactor: 0.75),
            CadDraw.Text(
                "Ⅰ",
                CadDraw.P(-1.136, -3382.539),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.75,
                alignmentPoint: CadDraw.P(28.328, -3382.539)
            ),
            CadDraw.Text(
                "Ⅰ",
                CadDraw.P(25.742, 387.396),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.75,
                alignmentPoint: CadDraw.P(55.206, 387.396)
            ),
            CadDraw.Text("墩身主筋", CadDraw.P(-1880.295, -921.247), 125, b07, widthFactor: 0.75),
            CadDraw.MText(
                "{\\W0.75;箍筋间距}",
                CadDraw.P(2891.315, -438.979),
                150,
                b07,
                attachment: AttachmentPointType.TopCenter,
                rectangleWidth: 156.454
            ),
            CadDraw.MText(
                "{\\W0.75;点筋间距}",
                CadDraw.P(-2547.559, -386.119),
                150,
                b07,
                attachment: AttachmentPointType.TopCenter,
                rectangleWidth: 156.454
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1017.357, -2100),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1034.969, -1950),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1053.482, -1800),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1072.898, -1650),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1093.219, -1500),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1114.447, -1350),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1136.586, -1200),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1159.636, -1050),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1183.601, -900),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1208.484, -750),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1235.193, -600),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1017.357, -2100),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1034.969, -1950),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1053.482, -1800),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1072.898, -1650),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1093.219, -1500),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1114.447, -1350),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1136.586, -1200),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1159.636, -1050),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1183.601, -900),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1208.484, -750),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1235.193, -600),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1252, -450),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1252, -350),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(0, 125),
                3
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(154, -378),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(308, -378),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(154, 0),
                6
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1252, -450),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1252, -350),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(0, 125),
                3
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-462, -2100),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                5
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-770, -2100),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-770, -1900),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                6
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-770, -1700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                6
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-462, -1500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                5
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-770, -1500),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-770, -1300),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                6
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1078, -1100),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                8
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(-1078, -900),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1078, -700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(154, 0),
                15
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1078, -500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(154, 0),
                15
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1078, -378),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(154, 0),
                7
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1078, -250),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                8
            ),
            new Insert(SteelSection.EnsureDefined(doc))
            {
                InsertPoint = CadDraw.P(1, -378),
                XScale = 4.533,
                YScale = 4.533,
                ZScale = 4.533,
                Rotation = 0,
                Layer = b01,
            },
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1078, -122),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(308, 0),
                8
            ),
            .. CadDraw.EntityArray(
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-924, -900),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                CadDraw.P(154, 0),
                14
            ),
            HRB400Block.Insert(doc, CadDraw.P(831.278, -2426.635), "N9", "12", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(-775.894, -2437.13), "N6-1", "16", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(-1792.168, -201.582), "N4-2", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1498.039, -48.864), "N13", "14", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(-1605.934, -646.129), "N1-2", "20", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1331.948, -1620.451), "N12-1", "14", 5, 4.8),
            HRB400Block.Insert(doc, CadDraw.P(1725.306, -1911.979), "N7-2", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1332.723, -1524.193), "N12-1", "14", 5, 1.484),
            HRB400Block.Insert(doc, CadDraw.P(-1602.887, -505.902), "N1-1", "20", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1919.613, -380.407), "N4-2", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1915.955, -236.715), "N4-1", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1911.454, -61.767), "N7-1", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1338.787, 180.237), "N2", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1497.85, -120.324), "N13", "14", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(1723.109, -572.681), "N1-2", "20", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(627.747, 392.93), "N3-1", "16", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(1626.559, -1160.931), "N7-3", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1516.282, -1083.55), "N7-3", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(1664.417, -940.668), "N7-2", "16", 5, 0),
        ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 4 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1300, 445.917),
                CadDraw.P(1300, 445.917),
                CadDraw.P(1300, 898.451),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1252, -100),
                CadDraw.P(-1300, -112.105),
                CadDraw.P(-1300, 678.297),
                0,
                dimStyle,
                b03
            ),
            .. CadDraw.ChainDimension(
                [CadDraw.P(-1252, -100), CadDraw.P(-1078, -112.105), CadDraw.P(1078, -112.105)],
                CadDraw.P(-1078, 678.297),
                0,
                dimStyle,
                b03,
                ["", "14×154"]
            ),
            // rotation = 1.5708（竖直）· 9 段
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1906.803, 0),
                    CadDraw.P(-1834.803, -122),
                    CadDraw.P(-1834.803, -378),
                    CadDraw.P(-1834.803, -500),
                    CadDraw.P(-1207.132, -2100),
                ],
                CadDraw.P(-2191.734, -122),
                Math.PI / 2,
                dimStyle,
                b03,
                ["", "2×128", "", "8×200"]
            ),
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(2252.096, 0),
                    CadDraw.P(2180.096, -100),
                    CadDraw.P(2180.096, -350),
                    CadDraw.P(2180.096, -450),
                    CadDraw.P(1552.425, -2100),
                ],
                CadDraw.P(2484.629, -100),
                Math.PI / 2,
                dimStyle,
                b03,
                ["", "2×125", "", "11×150"]
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2433.807, 0),
                CadDraw.P(1997.095, -2100),
                CadDraw.P(2737.364, -2100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 2 段
            CadDraw.RotatedDimension(
                CadDraw.P(1252, -100),
                CadDraw.P(1300, -112.105),
                CadDraw.P(1300, 678.297),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1252, -100),
                CadDraw.P(1078, -112.105),
                CadDraw.P(1078, 678.297),
                Math.PI,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 19 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 2：源图窗口 [322, 78, 452, 258]，119 个实体，4.32×5.92 m。</summary>
    public static void DrawRegion02(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-58524.279, -10531.991);

        List<Entity> entities =

            // 本区域表格存在未闭合的合并单元格边界 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "半Ⅲ-Ⅲ截面", "1:50", CadDraw.P(-1488.468, 2290.698), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2300.022, 1251.9, bulge: 0.414214),
                        CadDraw.V(-2451.922, 1100),
                        CadDraw.V(-2451.922, -1100, bulge: 0.414214),
                        CadDraw.V(-2300.022, -1251.9),
                        CadDraw.V(-799.954, -1251.9),
                        CadDraw.V(-580.03, -1031.977),
                        CadDraw.V(-580.03, 1031.976),
                        CadDraw.V(-799.954, 1251.9),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(-2451.922, 1078), CadDraw.P(-626.054, 1078), b01),
                CadDraw.Line(CadDraw.P(-2451.922, -1078), CadDraw.P(-626.054, -1078), b01),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2451.922, -770), CadDraw.P(-576.952, -770), b01),
                    CadDraw.P(0, 308),
                    6
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2144.022, -1251.9), CadDraw.P(-2144.022, 1251.9), b01),
                    CadDraw.P(300, 0),
                    5
                ),
                CadDraw.Line(CadDraw.P(-644.022, -1095.969), CadDraw.P(-644.022, 1095.968), b01),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2300.022, 1300, bulge: 0.414214),
                        CadDraw.V(-2500.022, 1100),
                        CadDraw.V(-2500.022, -1100, bulge: 0.414214),
                        CadDraw.V(-2300.022, -1300),
                        CadDraw.V(-780.03, -1300),
                        CadDraw.V(-580.03, -1100),
                        CadDraw.V(-500.03, -1100),
                        CadDraw.V(-500.03, 1100),
                        CadDraw.V(-580.03, 1100),
                        CadDraw.V(-780.03, 1300),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(-2869.835, 0), CadDraw.P(2631.51, 0), b06),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2235.537, 576.408), CadDraw.P(-2144.022, 524.066), b07),
                    CadDraw.P(300, 0),
                    5
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-735.537, 576.408),
                        CadDraw.V(-644.022, 524.066),
                        CadDraw.V(-3092.525, 524.066),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1810.451, 986.485),
                        CadDraw.V(-1758.108, 1078),
                        CadDraw.V(-1758.108, -1811.72),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1810.451, -1169.515), CadDraw.P(-1758.108, -1078), b07),
                    CadDraw.P(0, 308),
                    7
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1223.931, 1160.385),
                        CadDraw.V(-1171.588, 1251.9),
                        CadDraw.V(-1171.588, -1811.72),
                    ],
                    b07
                ),
                CadDraw.Line(CadDraw.P(-1223.931, -1343.415), CadDraw.P(-1171.588, -1251.9), b07),
                .. CadDraw.EntityArray(
                    CadDraw.Circle(CadDraw.P(-1862.337, -1060), 13, b01),
                    CadDraw.P(0, 308),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Circle(CadDraw.P(-1296.652, -1060), 13, b01),
                    CadDraw.P(0, 308),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Circle(CadDraw.P(-730.966, -1060), 13, b01),
                    CadDraw.P(0, 308),
                    8
                ),
                CadDraw.Text(
                    "（未示意墩身主筋）",
                    CadDraw.P(-2164.99, 2073.722),
                    150,
                    b07,
                    widthFactor: 0.7
                ),
                HRB400Block.Insert(doc, CadDraw.P(-1262.248, -1559.74), "N13", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-1848.768, -1559.74), "N2", "16", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-2754.544, 645.433), "N3-1", "16", 5, 0),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 3 段
            CadDraw.RotatedDimension(
                CadDraw.P(-2500.022, 1048.13),
                CadDraw.P(-2451.922, 1000),
                CadDraw.P(-2451.922, 1716.533),
                0,
                dimStyle,
                b03
            ),
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2451.922, 1048.13),
                    CadDraw.P(-2294.022, 1000),
                    CadDraw.P(-644.022, 1027.617),
                ],
                CadDraw.P(-2294.022, 1716.533),
                0,
                dimStyle,
                b03,
                ["", "11×150"]
            ),
            // rotation = 1.5708（竖直）· 5 段
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2274.8, -1251.9),
                    CadDraw.P(-2274.8, -1078),
                    CadDraw.P(-2274.8, 1078),
                    CadDraw.P(-2274.8, 1251.9),
                ],
                CadDraw.P(-3166.746, -1078),
                Math.PI / 2,
                dimStyle,
                b03,
                ["", "14×154", ""]
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2274.8, -1251.9),
                CadDraw.P(-2233.751, -1300),
                CadDraw.P(-3166.746, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2300.022, 1300),
                CadDraw.P(-2300.022, -1300),
                CadDraw.P(-3434.728, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 4.71239（竖直反向）· 1 段
            CadDraw.RotatedDimension(
                CadDraw.P(-2274.8, 1251.9),
                CadDraw.P(-2233.751, 1300),
                CadDraw.P(-3166.746, 1300),
                3 * Math.PI / 2,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 3 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 3：源图窗口 [452, 78, 582, 262]，173 个实体，去重 2，4.33×6.05 m。</summary>
    public static void DrawRegion03(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-58524.279, -10531.991);

        List<Entity> entities =

            // 源图有 2 个重复对象（同层同几何，仅句柄不同），只画一份
            // 找到了 16×21 的闭合网格，但格内文字只填了 0/326，不像表格 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "半Ⅳ-Ⅳ截面", "1:50", CadDraw.P(1103.323, 2262.127), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(531.839, 1031.976),
                        CadDraw.V(751.763, 1251.9),
                        CadDraw.V(2299.978, 1251.9, bulge: -0.414214),
                        CadDraw.V(2451.878, 1100),
                        CadDraw.V(2451.878, -1100, bulge: -0.414214),
                        CadDraw.V(2299.978, -1251.9),
                        CadDraw.V(751.763, -1251.9),
                        CadDraw.V(531.839, -1031.977),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(644, -1044), CadDraw.P(644, 1044), b01),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(793.978, -1251.9), CadDraw.P(793.978, 1251.9), b01),
                    CadDraw.P(150, 0),
                    11
                ),
                CadDraw.Line(CadDraw.P(643.978, -1144.115), CadDraw.P(643.978, 1144.114), b01),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(0, -1051.9), CadDraw.P(0, 1051.9), b01),
                    CadDraw.P(150, 0),
                    4
                ),
                CadDraw.Line(CadDraw.P(577.863, 1078), CadDraw.P(2451.878, 1078), b01),
                CadDraw.Line(CadDraw.P(577.863, -1078), CadDraw.P(2451.878, -1078), b01),
                .. new[]
                {
                    CadDraw.P(0, -924),
                    CadDraw.P(0, -1045.977),
                    CadDraw.P(0, 1045.976),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 2451.878, p.Y), b01)),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(0, -770), CadDraw.P(2451.878, -770), b01),
                    CadDraw.P(0, 154),
                    12
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-0.022, 1100),
                        CadDraw.V(531.839, 1100),
                        CadDraw.V(731.839, 1300),
                        CadDraw.V(2299.978, 1300, bulge: -0.414214),
                        CadDraw.V(2499.978, 1100),
                        CadDraw.V(2499.978, -1100, bulge: -0.414214),
                        CadDraw.V(2299.978, -1300),
                        CadDraw.V(731.839, -1300),
                        CadDraw.V(531.839, -1100),
                        CadDraw.V(-0.022, -1100),
                    ],
                    b04
                ),
                CadDraw.Line(CadDraw.P(-0.022, -1257.226), CadDraw.P(-0.022, 1206.715), b06),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1981.215, 986.485),
                        CadDraw.V(1928.873, 1078),
                        CadDraw.V(1928.873, -1948.106),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1363.94, 832.485),
                        CadDraw.V(1311.597, 924),
                        CadDraw.V(1311.597, -1948.106),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1363.94, -1015.515), CadDraw.P(1311.597, -924), b07),
                    CadDraw.P(0, 154),
                    12
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(885.492, 435.102),
                        CadDraw.V(793.978, 382.76),
                        CadDraw.V(3146.976, 382.76),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1035.492, 435.102), CadDraw.P(943.978, 382.76), b07),
                    CadDraw.P(150, 0),
                    10
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(91.515, -636.845),
                        CadDraw.V(0, -689.187),
                        CadDraw.V(3122.986, -689.187),
                    ],
                    b07
                ),
                .. new[]
                {
                    CadDraw.P(150, -689.187),
                    CadDraw.P(300, -689.187),
                    CadDraw.P(450, -689.187),
                    CadDraw.P(644, -689.187),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 91.515, p.Y + 52.343), b07)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1076.593, 1160.385),
                        CadDraw.V(1024.25, 1251.9),
                        CadDraw.V(1024.25, -2122.203),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(421.985, 954.461),
                        CadDraw.V(369.643, 1045.976),
                        CadDraw.V(369.643, -1785.411),
                    ],
                    b07
                ),
                .. new[]
                {
                    CadDraw.P(369.643, -1045.977),
                    CadDraw.P(1024.25, -1251.9),
                    CadDraw.P(1928.873, -1078),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 52.343, p.Y - 91.515), b07)),
                CadDraw.Line(CadDraw.P(1296.629, 1101.248), CadDraw.P(2802.973, 1587.718), b07),
                CadDraw.Line(CadDraw.P(1862.315, 793.248), CadDraw.P(2802.973, 1587.718), b07),
                CadDraw.Line(CadDraw.P(730.944, 1101.248), CadDraw.P(2802.973, 1587.718), b07),
                CadDraw.Line(CadDraw.P(2802.973, 1587.718), CadDraw.P(3216.127, 1587.718), b07),
                .. CadDraw.EntityArray(
                    CadDraw.Circle(CadDraw.P(1862.315, -1054.752), 13, b01),
                    CadDraw.P(0, 308),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Circle(CadDraw.P(1296.629, -1054.752), 13, b01),
                    CadDraw.P(0, 308),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Circle(CadDraw.P(730.944, -1054.752), 13, b01),
                    CadDraw.P(0, 308),
                    8
                ),
                CadDraw.Text(
                    "（未示意墩身主筋）",
                    CadDraw.P(441.335, 2042.066),
                    150,
                    b07,
                    widthFactor: 0.7
                ),
                HRB400Block.Insert(doc, CadDraw.P(938.198, -1544.405), "N12-1", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(2016.142, -1594.018), "N4-2", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(1388.463, -1594.018), "N4-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(278.908, -1328.857), "N12-2", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(2861.488, 504.127), "N3-1", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2861.488, -584.747), "N3-2", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2990.063, 1676.422), "N5", "20", 5, 0),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 1.5708（竖直）· 4 段
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(2274.8, -1251.9),
                    CadDraw.P(2274.8, -1078),
                    CadDraw.P(2274.8, 1078),
                    CadDraw.P(2274.8, 1251.9),
                ],
                CadDraw.P(3166.747, -1078),
                Math.PI / 2,
                dimStyle,
                b03,
                ["", "14×154", ""]
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2274.8, -1251.9),
                CadDraw.P(2233.751, -1300),
                CadDraw.P(3166.747, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 5 段
            CadDraw.RotatedDimension(
                CadDraw.P(2500.023, 1048.13),
                CadDraw.P(2451.878, 1000),
                CadDraw.P(2451.878, 1716.533),
                Math.PI,
                dimStyle,
                b03
            ),
            .. CadDraw.ChainDimension(
                [CadDraw.P(644, 865.217), CadDraw.P(450, 865.217), CadDraw.P(0, 907.052)],
                CadDraw.P(450, 1716.533),
                Math.PI,
                dimStyle,
                b03,
                ["", "3×150"]
            ),
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(2451.878, 900),
                    CadDraw.P(2293.978, 865.217),
                    CadDraw.P(643.978, 1002.017),
                ],
                CadDraw.P(2293.978, 1716.533),
                Math.PI,
                dimStyle,
                b03,
                ["", "11×150"]
            ),
            // rotation = 4.71239（竖直反向）· 1 段
            CadDraw.RotatedDimension(
                CadDraw.P(2274.8, 1251.9),
                CadDraw.P(2233.751, 1300),
                CadDraw.P(3166.747, 1300),
                3 * Math.PI / 2,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 7 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 4：源图窗口 [695, 60, 1180, 480]，307 个实体，15.95×13.87 m。</summary>
    public static void DrawRegion04(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 钢筋大样：一根一段。本体顶点/段标注取源图几何与文字，真长取引线块的 L；
        // 区间标注（如 4381～4611）按 L 反解并校核是否落在区间内；箍筋=闭合环+双钩。
        var options = new RebarDetailOptions
        {
            TextHeight = 125,
            TextOffset = 158,
            KeepTextHorizontal = true,
            RebarLayer = b01,
            TextLayer = b07,
        };

        // N1-1：Φ20 线筋，L=4964，4381～4611（反解 4496）；Σ4849～5079
        var n1_1 = new Rebar
        {
            Number = "N1-1",
            Grade = "HRB400",
            Diameter = 20,
            Count = 26,
            TrueLength = 4964,
            Vertices =
            [
                RebarDetail.V(-49275.46, -8144.858, "234"),
                RebarDetail.V(-49275.46, -8024.65, "4381～4611"),
                RebarDetail.V(-46828.45, -8024.65, "234"),
                RebarDetail.V(-46828.45, -8144.858),
            ],
        };
        var n1_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48325.204, -7839.657),
            "N1-1",
            "20 L=4964",
            5
        );

        // N1-2：Φ20 线筋，L=2218，1709～1790（反解 1750）；Σ2177～2258
        var n1_2 = new Rebar
        {
            Number = "N1-2",
            Grade = "HRB400",
            Diameter = 20,
            Count = 8,
            TrueLength = 2218,
            Vertices =
            [
                RebarDetail.V(-49275.46, -9162.255, "234"),
                RebarDetail.V(-49275.46, -9042.047, "1709～1790"),
                RebarDetail.V(-46828.45, -9042.047, "234"),
                RebarDetail.V(-46828.45, -9162.255),
            ],
        };
        var n1_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48204.277, -8859.212),
            "N1-2",
            "20 L=2218",
            5
        );

        // N2：Φ16 线筋，L=3134；Σ3134～3134
        var n2 = new Rebar
        {
            Number = "N2",
            Grade = "HRB400",
            Diameter = 16,
            Count = 16,
            TrueLength = 3134,
            Vertices =
            [
                RebarDetail.V(-46312.262, -10813.818, "187"),
                RebarDetail.V(-46192.054, -10693.61, "900"),
                RebarDetail.V(-46828.45, -10057.214, "1860"),
                RebarDetail.V(-49275.46, -10057.214, "187"),
                RebarDetail.V(-49275.46, -10177.422),
            ],
        };
        var n2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48237.776, -9876.469),
            "N2",
            "16 L=3134",
            5
        );

        // N3-1：Φ16 线筋，L=2830；Σ2830～2830
        var n3_1 = new Rebar
        {
            Number = "N3-1",
            Grade = "HRB400",
            Diameter = 16,
            Count = 42,
            TrueLength = 2830,
            Vertices =
            [
                RebarDetail.V(-49275.46, -11739.787, "187"),
                RebarDetail.V(-49275.46, -11619.579, "2456"),
                RebarDetail.V(-46828.45, -11619.579, "187"),
                RebarDetail.V(-46828.45, -11739.787),
            ],
        };
        var n3_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48207.97, -11448.581),
            "N3-1",
            "16 L=2830",
            5
        );

        // N3-2：Φ16 线筋，L=2471，2056～2138（反解 2097）；Σ2430～2512
        var n3_2 = new Rebar
        {
            Number = "N3-2",
            Grade = "HRB400",
            Diameter = 16,
            Count = 13,
            TrueLength = 2471,
            Vertices =
            [
                RebarDetail.V(-49275.46, -13171.427, "187"),
                RebarDetail.V(-49275.46, -13001.427, "2056～2138"),
                RebarDetail.V(-46828.45, -13001.427, "187"),
                RebarDetail.V(-46828.45, -13171.427),
            ],
        };
        var n3_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48275.95, -12862.489),
            "N3-2",
            "16 L=2471",
            5
        );

        // N4-1：Φ28 线筋，L=5512；Σ5512～5512
        var n4_1 = new Rebar
        {
            Number = "N4-1",
            Grade = "HRB400",
            Diameter = 28,
            Count = 26,
            TrueLength = 5512,
            Vertices =
            [
                RebarDetail.V(-49275.46, -14754.702, "328"),
                RebarDetail.V(-49275.46, -14584.702, "4856"),
                RebarDetail.V(-46828.45, -14584.702, "328"),
                RebarDetail.V(-46828.45, -14754.702),
            ],
        };
        var n4_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48193.782, -14470.391),
            "N4-1",
            "28 L=5512",
            5
        );

        // N4-2：Φ28 线筋，L=2522，1855～1877（反解 1866）；Σ2511～2533
        var n4_2 = new Rebar
        {
            Number = "N4-2",
            Grade = "HRB400",
            Diameter = 28,
            Count = 8,
            TrueLength = 2522,
            Vertices =
            [
                RebarDetail.V(-49722.46, -16115.912, "328"),
                RebarDetail.V(-49722.46, -15945.912, "1855～1877"),
                RebarDetail.V(-47275.45, -15945.912, "328"),
                RebarDetail.V(-47275.45, -16115.912),
            ],
        };
        var n4_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-48807.558, -15818.018),
            "N4-2",
            "28 L=2522",
            5
        );

        // N6-1：Φ16 线筋，L=2598，1991～2456（反解 2224）；Σ2365～2830
        var n6_1 = new Rebar
        {
            Number = "N6-1",
            Grade = "HRB400",
            Diameter = 16,
            Count = 120,
            TrueLength = 2598,
            Vertices =
            [
                RebarDetail.V(-45379.796, -8657.805, "187"),
                RebarDetail.V(-45379.796, -8487.805, "1991～2456"),
                RebarDetail.V(-42932.785, -8487.805, "187"),
                RebarDetail.V(-42932.785, -8657.805),
            ],
        };
        var n6_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-44351.508, -8346.498),
            "N6-1",
            "16 L=2598",
            5
        );

        // N6-2：Φ16 线筋，L=2239，1591～2138（反解 1865）；Σ1965～2512
        var n6_2 = new Rebar
        {
            Number = "N6-2",
            Grade = "HRB400",
            Diameter = 16,
            Count = 43,
            TrueLength = 2239,
            Vertices =
            [
                RebarDetail.V(-45379.796, -10169.785, "187"),
                RebarDetail.V(-45379.796, -9999.785, "1591～2138"),
                RebarDetail.V(-42932.785, -9999.785, "187"),
                RebarDetail.V(-42932.785, -10169.785),
            ],
        };
        var n6_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-44529.755, -9883.437),
            "N6-2",
            "16 L=2239",
            5
        );

        // N7-1：Φ16 线筋，L=2302；Σ2302～2302
        var n7_1 = new Rebar
        {
            Number = "N7-1",
            Grade = "HRB400",
            Diameter = 16,
            Count = 16,
            TrueLength = 2302,
            Vertices =
            [
                RebarDetail.V(-45379.796, -11712.788, "187"),
                RebarDetail.V(-45379.796, -11592.58, "1928"),
                RebarDetail.V(-42932.785, -11592.58, "187"),
                RebarDetail.V(-42932.785, -11712.788),
            ],
        };
        var n7_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-44304.993, -11447.404),
            "N7-1",
            "16 L=2302",
            5
        );

        // N7-2：Φ16 线筋，L=4081，3248～4166（反解 3707）；Σ3622～4540
        var n7_2 = new Rebar
        {
            Number = "N7-2",
            Grade = "HRB400",
            Diameter = 16,
            Count = 36,
            TrueLength = 4081,
            Vertices =
            [
                RebarDetail.V(-45379.796, -13228.823, "187"),
                RebarDetail.V(-45379.796, -13058.823, "3248～4166"),
                RebarDetail.V(-42932.785, -13058.823, "187"),
                RebarDetail.V(-42932.785, -13228.823),
            ],
        };
        var n7_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-44442.16, -12888.359),
            "N7-2",
            "16 L=4081",
            5
        );

        // N7-3：Φ16 线筋，L=2008；Σ2008～2008
        var n7_3 = new Rebar
        {
            Number = "N7-3",
            Grade = "HRB400",
            Diameter = 16,
            Count = 4,
            TrueLength = 2008,
            Vertices =
            [
                RebarDetail.V(-45379.796, -14800.603, "187"),
                RebarDetail.V(-45379.796, -14630.603, "1634"),
                RebarDetail.V(-42932.785, -14630.603, "187"),
                RebarDetail.V(-42932.785, -14800.603),
            ],
        };
        var n7_3Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-44483.09, -14477.371),
            "N7-3",
            "16 L=2008",
            5
        );

        // N8-1：Φ12 线筋，L=4256；Σ4256～4256
        var n8_1 = new Rebar
        {
            Number = "N8-1",
            Grade = "HRB400",
            Diameter = 12,
            Count = 4,
            TrueLength = 4256,
            Vertices =
            [
                RebarDetail.V(-46054.06, -16714.865, "141"),
                RebarDetail.V(-46139.06, -16714.865, "923"),
                RebarDetail.V(-46139.06, -15846.874, "1268"),
                RebarDetail.V(-45406.401, -15846.874, "1783"),
                RebarDetail.V(-45406.401, -17590.874, "141"),
                RebarDetail.V(-45491.401, -17590.874),
            ],
        };
        var n8_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-45899.125, -15471.251),
            "N8-1",
            "12 L=4256",
            5
        );

        // N8-2：Φ12 线筋，L=4455；Σ4455～4455
        var n8_2 = new Rebar
        {
            Number = "N8-2",
            Grade = "HRB400",
            Diameter = 12,
            Count = 4,
            TrueLength = 4455,
            Vertices =
            [
                RebarDetail.V(-44463.228, -16714.865, "141"),
                RebarDetail.V(-44548.228, -16714.865, "923"),
                RebarDetail.V(-44548.228, -15846.874, "1467"),
                RebarDetail.V(-43671.978, -15846.874, "1783"),
                RebarDetail.V(-43671.978, -17590.874, "141"),
                RebarDetail.V(-43756.978, -17590.874),
            ],
        };
        var n8_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-44287.938, -15483.884),
            "N8-2",
            "12 L=4455",
            5
        );

        // N9：Φ12 线筋，L=4991；Σ4991～4991
        var n9 = new Rebar
        {
            Number = "N9",
            Grade = "HRB400",
            Diameter = 12,
            Count = 16,
            TrueLength = 4991,
            Vertices =
            [
                RebarDetail.V(-42880.307, -16917.153, "141"),
                RebarDetail.V(-42965.307, -16917.153, "1179"),
                RebarDetail.V(-42965.307, -15846.874, "1491"),
                RebarDetail.V(-41888.028, -15846.874, "2039"),
                RebarDetail.V(-41888.028, -17824.874, "141"),
                RebarDetail.V(-41973.028, -17824.874),
            ],
        };
        var n9Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-42600.984, -15494.019),
            "N9",
            "12 L=4991",
            5
        );

        // N10：Φ12 线筋，L=4399；Σ4399～4399
        var n10 = new Rebar
        {
            Number = "N10",
            Grade = "HRB400",
            Diameter = 12,
            Count = 4,
            TrueLength = 4399,
            Vertices =
            [
                RebarDetail.V(-41126.444, -16917.153, "141"),
                RebarDetail.V(-41211.444, -16917.153, "1179"),
                RebarDetail.V(-41211.444, -15846.874, "1491"),
                RebarDetail.V(-40292.398, -15846.874, "1447"),
                RebarDetail.V(-40292.398, -17081.095, "141"),
                RebarDetail.V(-40377.398, -17081.095),
            ],
        };
        var n10Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-40914.444, -15506.721),
            "N10",
            "12 L=4399",
            5
        );

        // N11：Φ12 线筋，L=3457；Σ3457～3457
        var n11 = new Rebar
        {
            Number = "N11",
            Grade = "HRB400",
            Diameter = 12,
            Count = 4,
            TrueLength = 3457,
            Vertices =
            [
                RebarDetail.V(-39485.304, -16625.356, "141"),
                RebarDetail.V(-39570.304, -16625.356, "842"),
                RebarDetail.V(-39570.304, -15846.874, "1491"),
                RebarDetail.V(-38816.462, -15846.874, "842"),
                RebarDetail.V(-38816.462, -16625.356, "141"),
                RebarDetail.V(-38901.462, -16625.356),
            ],
        };
        var n11Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-39367.87, -15503.308),
            "N11",
            "12 L=3457",
            5
        );

        // N12-1：Φ14 箍筋，L=7630，1495～2064 等区间标注；Σ6540～8720
        var n12_1 = new Stirrup
        {
            Number = "N12-1",
            Grade = "HRB400",
            Diameter = 14,
            Count = 26,
            TrueLength = 7630,
            Vertices =
            [
                RebarDetail.V(-40371.447, -8863.163, "1495～2064"),
                RebarDetail.V(-40371.447, -10493.053, "311"),
                RebarDetail.V(-40591.37, -10712.977, "1027～1548"),
                RebarDetail.V(-41657.042, -10712.977, "239", bulge: -0.414),
                RebarDetail.V(-41808.942, -10561.077, "1631～2200"),
                RebarDetail.V(-41808.942, -8795.139, "239", bulge: -0.414),
                RebarDetail.V(-41657.042, -8643.239, "1027～1548"),
                RebarDetail.V(-40591.37, -8643.239, "311"),
            ],
            Hooks =
            [
                new(new XY(-41608.841, -8775.258), new XY(-41731.659, -8662.259), "130"),
                new(new XY(-41670.761, -8846.194), new XY(-41795.031, -8731.859)),
            ],
        };
        var n12_1Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-41176.107, -8360.958),
            "N12-1",
            "14 L=7630",
            5
        );

        // N13：Φ14 箍筋，L=8649，1500～1525 等区间标注；Σ8624～8674
        var n13 = new Stirrup
        {
            Number = "N13",
            Grade = "HRB400",
            Diameter = 14,
            Count = 4,
            TrueLength = 8649,
            Vertices =
            [
                RebarDetail.V(-41566.237, -12003.525, "1500～1525"),
                RebarDetail.V(-40384.876, -12003.525, "311"),
                RebarDetail.V(-40149.521, -12238.881, "2064"),
                RebarDetail.V(-40149.521, -14078.17, "311"),
                RebarDetail.V(-40384.876, -14313.525, "1500～1525"),
                RebarDetail.V(-41566.237, -14313.525, "239", bulge: -0.414),
                RebarDetail.V(-41721.237, -14158.525, "2200"),
                RebarDetail.V(-41721.237, -12158.525, "239"),
            ],
            Hooks =
            [
                new(new XY(-41521.135, -12138.644), new XY(-41643.954, -12025.645), "130"),
                new(new XY(-41583.056, -12209.58), new XY(-41707.326, -12095.245)),
            ],
        };
        var n13Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-41101.531, -11628.596),
            "N13",
            "14 L=8649",
            5
        );

        // N12-2：Φ14 箍筋，L=12100，3332～4904 等区间标注；Σ10110～14382
        var n12_2 = new Stirrup
        {
            Number = "N12-2",
            Grade = "HRB400",
            Diameter = 14,
            Count = 13,
            TrueLength = 12100,
            Vertices =
            [
                RebarDetail.V(-39524.527, -10734.742, "3332～4904"),
                RebarDetail.V(-37072.65, -10734.742, "1500～2064"),
                RebarDetail.V(-37072.65, -8642.789, "3332～4904"),
                RebarDetail.V(-39524.527, -8642.789, "1500～2064"),
            ],
            Hooks =
            [
                new(new XY(-37138.56, -8642.789), new XY(-37289.436, -8806.776)),
                new(new XY(-37072.65, -8697.135), new XY(-37223.526, -8861.121)),
            ],
        };
        var n12_2Leader = HRB400Block.Insert(
            doc,
            CadDraw.P(-38381.906, -8356.644),
            "N12-2",
            "14 L=12100",
            5
        );

        List<Entity> entities =
        [
            .. n1_1.Detail(doc, CadDraw.P(0, 0), options),
            n1_1Leader,
            .. n1_2.Detail(doc, CadDraw.P(0, 0), options),
            n1_2Leader,
            .. n2.Detail(doc, CadDraw.P(0, 0), options),
            n2Leader,
            .. n3_1.Detail(doc, CadDraw.P(0, 0), options),
            n3_1Leader,
            .. n3_2.Detail(doc, CadDraw.P(0, 0), options),
            n3_2Leader,
            .. n4_1.Detail(doc, CadDraw.P(0, 0), options),
            n4_1Leader,
            .. n4_2.Detail(doc, CadDraw.P(0, 0), options),
            n4_2Leader,
            .. n6_1.Detail(doc, CadDraw.P(0, 0), options),
            n6_1Leader,
            .. n6_2.Detail(doc, CadDraw.P(0, 0), options),
            n6_2Leader,
            .. n7_1.Detail(doc, CadDraw.P(0, 0), options),
            n7_1Leader,
            .. n7_2.Detail(doc, CadDraw.P(0, 0), options),
            n7_2Leader,
            .. n7_3.Detail(doc, CadDraw.P(0, 0), options),
            n7_3Leader,
            .. n8_1.Detail(doc, CadDraw.P(0, 0), options),
            n8_1Leader,
            .. n8_2.Detail(doc, CadDraw.P(0, 0), options),
            n8_2Leader,
            .. n9.Detail(doc, CadDraw.P(0, 0), options),
            n9Leader,
            .. n10.Detail(doc, CadDraw.P(0, 0), options),
            n10Leader,
            .. n11.Detail(doc, CadDraw.P(0, 0), options),
            n11Leader,
            .. n12_1.Detail(doc, CadDraw.P(0, 0), options),
            n12_1Leader,
            .. n13.Detail(doc, CadDraw.P(0, 0), options),
            n13Leader,
            .. n12_2.Detail(doc, CadDraw.P(0, 0), options),
            n12_2Leader,
            // 以下是不属于单根钢筋的图元：N5 拐角构造大样、箍筋关系示意图、图题
            .. TitleBlock.Add(doc, "钢筋大样图", "", CadDraw.P(-42455.901, -7957.799), 50),
            .. TitleBlock.Add(doc, "箍筋关系示意图", "", CadDraw.P(-38089.651, -11924.597), 50),
            CadDraw.Polyline(
                [
                    CadDraw.V(-47780.28, -19647.439, bulge: -0.045945),
                    CadDraw.V(-47104.027, -17991.268),
                    CadDraw.V(-47104.027, -17613.268),
                    CadDraw.V(-49532.027, -17613.268),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-48405.398, -17643.268),
                    CadDraw.V(-48205.398, -17643.268),
                    CadDraw.V(-47372.012, -18476.653),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-47839.712, -17643.268),
                    CadDraw.V(-47639.712, -17643.268),
                    CadDraw.V(-47189.727, -18093.253),
                    CadDraw.V(-47189.527, -18092.861, bulge: 0.004479),
                    CadDraw.V(-47278.659, -18271.901),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-48971.083, -17643.268),
                    CadDraw.V(-48771.083, -17643.268),
                    CadDraw.V(-47540.593, -18873.758, bulge: 0.004479),
                    CadDraw.V(-47614.803, -19059.48),
                ],
                b01
            ),
            CadDraw.Line(CadDraw.P(-47234.494, -18182.181), CadDraw.P(-47213.129, -18206.596), b01),
            CadDraw.Line(CadDraw.P(-47246.517, -18206.405), CadDraw.P(-47225.13, -18230.843), b01),
            CadDraw.Line(CadDraw.P(-47258.493, -18230.682), CadDraw.P(-47237.084, -18255.145), b01),
            CadDraw.Line(CadDraw.P(-47222.425, -18158.011), CadDraw.P(-47201.081, -18182.401), b01),
            CadDraw.Line(CadDraw.P(-47210.309, -18133.894), CadDraw.P(-47188.986, -18158.26), b01),
            CadDraw.Line(CadDraw.P(-47198.147, -18109.83), CadDraw.P(-47176.845, -18134.172), b01),
            CadDraw.Polyline(
                [
                    CadDraw.V(-47371.407, -18475.36, bulge: 0.004479),
                    CadDraw.V(-47454.518, -18657.273),
                ],
                b01
            ),
            CadDraw.Line(CadDraw.P(-47413.37, -18566.13), CadDraw.P(-47391.202, -18589.818), b01),
            CadDraw.Line(CadDraw.P(-47424.578, -18590.741), CadDraw.P(-47402.388, -18614.453), b01),
            CadDraw.Line(CadDraw.P(-47435.737, -18615.404), CadDraw.P(-47413.525, -18639.14), b01),
            CadDraw.Line(CadDraw.P(-47446.848, -18640.119), CadDraw.P(-47424.613, -18663.878), b01),
            CadDraw.Line(CadDraw.P(-47402.113, -18541.571), CadDraw.P(-47379.968, -18565.235), b01),
            CadDraw.Line(CadDraw.P(-47390.809, -18517.063), CadDraw.P(-47368.685, -18540.704), b01),
            CadDraw.Line(CadDraw.P(-47379.456, -18492.607), CadDraw.P(-47357.354, -18516.224), b01),
            CadDraw.Line(CadDraw.P(-47578.114, -18966.453), CadDraw.P(-47554.826, -18989.04), b01),
            CadDraw.Line(CadDraw.P(-47588.118, -18991.577), CadDraw.P(-47564.806, -19014.187), b01),
            CadDraw.Line(CadDraw.P(-47598.07, -19016.751), CadDraw.P(-47574.735, -19039.384), b01),
            CadDraw.Line(CadDraw.P(-47607.972, -19041.975), CadDraw.P(-47584.613, -19064.631), b01),
            CadDraw.Line(CadDraw.P(-47568.059, -18941.377), CadDraw.P(-47544.794, -18963.942), b01),
            CadDraw.Line(CadDraw.P(-47557.954, -18916.351), CadDraw.P(-47534.712, -18938.894), b01),
            CadDraw.Line(CadDraw.P(-47547.798, -18891.374), CadDraw.P(-47524.579, -18913.894), b01),
            CadDraw.Polyline(
                [
                    CadDraw.V(-39562.178, -14254.647, bulge: 0.414214),
                    CadDraw.V(-39412.178, -14404.647),
                    CadDraw.V(-38632.889, -14404.647),
                    CadDraw.V(-38420.178, -14191.937),
                    CadDraw.V(-38420.178, -12517.358),
                    CadDraw.V(-38632.889, -12304.647),
                    CadDraw.V(-39412.178, -12304.647, bulge: 0.414214),
                    CadDraw.V(-39562.178, -12454.647),
                ],
                b01,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-36925.247, -14404.647, bulge: 0.414214),
                    CadDraw.V(-36775.247, -14254.647),
                    CadDraw.V(-36775.247, -12454.647, bulge: 0.414214),
                    CadDraw.V(-36925.247, -12304.647),
                    CadDraw.V(-37704.537, -12304.647),
                    CadDraw.V(-37917.247, -12517.358),
                    CadDraw.V(-37917.247, -14191.937),
                    CadDraw.V(-37704.537, -14404.647),
                ],
                b01,
                closed: true
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(-47827.212, -17643.268),
                    CadDraw.P(-47827.212, -17613.268),
                    b01
                ),
                CadDraw.P(25, 0),
                8
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(-48392.898, -17643.268),
                    CadDraw.P(-48392.898, -17613.268),
                    b01
                ),
                CadDraw.P(25, 0),
                8
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(
                    CadDraw.P(-48958.583, -17643.268),
                    CadDraw.P(-48958.583, -17613.268),
                    b01
                ),
                CadDraw.P(25, 0),
                8
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-39524.092, -12532.186),
                    CadDraw.V(-36813.334, -12532.186),
                    CadDraw.V(-36813.334, -14177.109),
                    CadDraw.V(-39524.092, -14177.109),
                ],
                b01,
                closed: true
            ),
            CadDraw.Line(CadDraw.P(-49532.027, -19641.268), CadDraw.P(-49532.027, -17504.884), b06),
            CadDraw.Text(
                "示意",
                CadDraw.P(-41619.391, -7973.463),
                175,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Middle,
                verticalAlignment: TextVerticalAlignmentType.Middle,
                widthFactor: 0.75,
                alignmentPoint: CadDraw.P(-41528.766, -7885.963)
            ),
            CadDraw.Text("墩", CadDraw.P(-49493.112, -18176.696), 125, b07, widthFactor: 0.75),
            CadDraw.Text("中", CadDraw.P(-49493.112, -18382.406), 125, b07, widthFactor: 0.75),
            CadDraw.Text("心", CadDraw.P(-49493.112, -18593.532), 125, b07, widthFactor: 0.75),
            CadDraw.Text("线", CadDraw.P(-49493.112, -18804.658), 125, b07, widthFactor: 0.75),
            CadDraw.Text("N2", CadDraw.P(-49179.998, -17599.355), 125, b07, widthFactor: 0.75),
            CadDraw.Text("200", CadDraw.P(-48405.844, -17584.655), 125, b07, widthFactor: 0.75),
            CadDraw.Text("200", CadDraw.P(-47840.159, -17584.655), 125, b07, widthFactor: 0.75),
            CadDraw.Text(
                "200",
                CadDraw.P(-47282.02, -18689.339),
                125,
                b07,
                1.084,
                widthFactor: 0.75
            ),
            CadDraw.Text(
                "200",
                CadDraw.P(-47150.973, -18331.056),
                125,
                b07,
                1.047,
                widthFactor: 0.75
            ),
            CadDraw.Text(
                "636",
                CadDraw.P(-47581.459, -17901.185),
                125,
                b07,
                5.471,
                widthFactor: 0.75
            ),
            CadDraw.Text(
                "1179",
                CadDraw.P(-47952.563, -18106.813),
                125,
                b07,
                5.471,
                widthFactor: 0.75
            ),
            CadDraw.Text(
                "墩身主筋",
                CadDraw.P(-47633.388, -19704.65),
                125,
                b07,
                1.274,
                widthFactor: 0.75
            ),
            CadDraw.Text(
                "1740",
                CadDraw.P(-48271.347, -18350.16),
                125,
                b07,
                5.471,
                widthFactor: 0.75
            ),
            CadDraw.Text("200", CadDraw.P(-48968.575, -17584.655), 125, b07, widthFactor: 0.75),
            CadDraw.Text(
                "200",
                CadDraw.P(-47441.836, -19087.05),
                125,
                b07,
                1.234,
                widthFactor: 0.75
            ),
            CadDraw.Text(
                "130",
                CadDraw.P(-37568.574, -8899.282),
                125,
                b07,
                5.498,
                widthFactor: 0.75
            ),
            HRB400Block.Insert(doc, CadDraw.P(-48629.912, -17000.924), "N5", "20 L=1585", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-37206.524, -12186.643), "N12-1", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-39000.26, -12619.402), "N12-2", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-38976.929, -12186.643), "N12-1", "14", 5, 0),
        ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 分组，组内保持源顺序。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 4 段
            CadDraw.RotatedDimension(
                CadDraw.P(-48771.083, -17643.268),
                CadDraw.P(-48205.398, -17643.268),
                CadDraw.P(-48205.398, -17385.869),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-48205.398, -17643.268),
                CadDraw.P(-47639.712, -17643.268),
                CadDraw.P(-47639.712, -17385.869),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-47639.712, -17643.268),
                CadDraw.P(-47104.027, -17613.268),
                CadDraw.P(-47104.027, -17385.869),
                0,
                dimStyle,
                b03,
                "566"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-49532.027, -17643.268),
                CadDraw.P(-48771.083, -17643.268),
                CadDraw.P(-48771.083, -17385.869),
                0,
                dimStyle,
                b03,
                "731"
            ),
            // rotation = 1.5708（竖直）· 3 段
            CadDraw.RotatedDimension(
                CadDraw.P(-47104.027, -17613.268),
                CadDraw.P(-47189.527, -18092.861),
                CadDraw.P(-46623.301, -18092.861),
                Math.PI / 2,
                dimStyle,
                b03,
                "502"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-47189.527, -18092.861),
                CadDraw.P(-47372.012, -18476.653),
                CadDraw.P(-46623.301, -18476.653),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-47372.012, -18476.653),
                CadDraw.P(-47540.593, -18873.758),
                CadDraw.P(-46623.301, -18873.758),
                Math.PI / 2,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层归还（20 根引线 + N5/示意图 4 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b04;
        }

        doc.AddEntities(entities);
        doc.AddEntities(dimensionEntities);
    }

    /// <summary>截图 01 / 区域 5：源图窗口 [1282, 143, 1468, 372]，193 个实体，6.16×7.53 m。</summary>
    public static void DrawRegion05(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);

        var table01 = TableHelper.CreateTable(
            doc,
            CadDraw.P(-31830.575, -10297.287),
            new string[,]
            {
                {
                    "编号",
                    "直径\\P(mm)",
                    "单根长\\P(mm)",
                    "根数",
                    "总长\\P(m)",
                    "单位重\\P(kg/m)",
                    "总重\\P（kg）",
                },
                { "N1-1", "", "4964", "26", "129.1", "2.470", "318.8" },
                { "N1-2", "", "2218", "8", "17.7", "2.470", "43.8" },
                { "N2", "", "3134", "16", "50.1", "1.580", "79.2" },
                { "N3-1", "", "2830", "42", "118.9", "1.580", "187.8" },
                { "N3-2", "", "2471", "13", "32.1", "1.580", "50.8" },
                { "N4-1", "", "5512", "26", "143.3", "4.830", "692.2" },
                { "N4-2", "", "2522", "8", "20.2", "4.830", "97.5" },
                { "N5", "", "1585", "48", "76.1", "2.470", "187.9" },
                { "N6-1", "", "2598", "120", "311.8", "1.580", "492.6" },
                { "N6-2", "", "2239", "43", "96.3", "1.580", "152.1" },
                { "N7-1", "", "2302", "16", "36.8", "1.580", "58.2" },
                { "N7-2", "", "4081", "36", "146.9", "1.580", "232.1" },
                { "N7-3", "", "2008", "4", "8.0", "1.580", "12.7" },
                { "N8-1", "", "4256", "4", "17.0", "0.888", "15.1" },
                { "N8-2", "", "4455", "4", "17.8", "0.888", "15.8" },
                { "N9", "", "4991", "16", "79.9", "0.888", "70.9" },
                { "N10", "", "4399", "4", "17.6", "0.888", "15.6" },
                { "N11", "", "3457", "4", "13.8", "0.888", "12.3" },
                { "N12-1", "", "7630", "26", "198.4", "1.210", "240.0" },
                { "N12-2", "", "12100", "13", "157.3", "1.210", "190.3" },
                { "N13", "", "8649", "4", "34.6", "1.210", "41.9" },
                { "合并", "HRB400E钢筋总计（kg）", "", "", "", "", "3207.7" },
            },
            [500, 750, 750, 750, 750, 750, 750],
            [
                500,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
                250,
            ],
            125
        );
        TableHelper.MergeCells(table01, 22, 1, 22, 5);

        List<Entity> entities =
        [
            table01,
            .. TitleBlock.Add(doc, "墩帽钢筋数量表", "", CadDraw.P(-29202.303, -10047.425), 50),
            CadDraw.Line(CadDraw.P(-31723.293, -14967.892), CadDraw.P(-31723.293, -14967.892), b04),
            HRB400Block.Insert(doc, CadDraw.P(-31040.716, -10922.287), "", "20", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31040.716, -11172.287), "", "20", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -11422.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -11672.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -11922.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31040.716, -12172.287), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31040.716, -12422.287), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31040.716, -12672.287), "", "20", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -12922.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -13172.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -13422.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -13672.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31030.299, -13922.287), "", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31032.383, -14172.287), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31032.383, -14422.287), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31032.383, -14672.287), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31032.383, -14922.287), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31032.383, -15172.287), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31034.466, -15422.287), "", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31034.466, -15672.287), "", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-31034.466, -15922.287), "", "14", 5, 0),
        ];

        // TableHelper 创建的 TABLE 默认落在 0 层，统一归到「B-03 标注」
        foreach (var table in entities.OfType<TableEntity>())
        {
            table.Layer = b03;
        }

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 21 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b04;
        }

        doc.AddEntities(entities);
    }

    /// <summary>截图 01 / 区域 6：源图窗口 [12, 418, 208, 568]，145 个实体，6.45×4.98 m。</summary>
    public static void DrawRegion06(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-68747.72, -20757.012);

        List<Entity> entities =
        [
            .. TitleBlock.Add(doc, "Ⅴ-Ⅴ截面", "1:50", CadDraw.P(0.666, 1495.35), 50),
            CadDraw.Polyline(
                [
                    CadDraw.V(-504.808, 1034.853),
                    CadDraw.V(-1570.48, 1034.853, bulge: 0.414214),
                    CadDraw.V(-1722.38, 882.953),
                    CadDraw.V(-1722.38, -882.984, bulge: 0.414214),
                    CadDraw.V(-1570.48, -1034.884),
                    CadDraw.V(-504.808, -1034.884),
                    CadDraw.V(-284.885, -814.961),
                    CadDraw.V(-284.885, 814.929),
                ],
                b01,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(504.809, 1034.853),
                    CadDraw.V(1570.48, 1034.853, bulge: -0.414214),
                    CadDraw.V(1722.38, 882.953),
                    CadDraw.V(1722.38, -882.984, bulge: -0.414214),
                    CadDraw.V(1570.48, -1034.884),
                    CadDraw.V(504.809, -1034.884),
                    CadDraw.V(284.885, -814.961),
                    CadDraw.V(284.885, 814.929),
                ],
                b01,
                closed: true
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(644, -1052.495), CadDraw.P(644, 1052.464), b01),
                CadDraw.P(300, 0),
                4
            ),
            CadDraw.Line(CadDraw.P(450, -997.687), CadDraw.P(450, 997.655), b01),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1722.38, -770.016), CadDraw.P(1722.38, -770.016), b01),
                CadDraw.P(0, 308),
                6
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1544, -1052.495), CadDraw.P(-1544, 1052.464), b01),
                CadDraw.P(300, 0),
                4
            ),
            CadDraw.Line(CadDraw.P(-450, -997.687), CadDraw.P(-450, 997.655), b01),
            CadDraw.Line(CadDraw.P(-150, -844.595), CadDraw.P(-150, 844.564), b01),
            CadDraw.Line(CadDraw.P(-1722.38, 828.93), CadDraw.P(1722.38, 828.93), b01),
            CadDraw.Line(CadDraw.P(-1722.38, -828.961), CadDraw.P(1722.38, -828.961), b01),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1562, -1010.984), CadDraw.P(-1562, 479.984), b01),
                CadDraw.P(300, 0),
                4
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1526, -480.016), CadDraw.P(-1526, 1010.954), b01),
                CadDraw.P(300, 0),
                4
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(662, -1010.984), CadDraw.P(662, 479.984), b01),
                CadDraw.P(300, 0),
                4
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(626, -480.016), CadDraw.P(626, 1010.954), b01),
                CadDraw.P(300, 0),
                4
            ),
            .. new[]
            {
                CadDraw.P(-168, -480.016),
                CadDraw.P(-132, -788.016),
                CadDraw.P(132, -788.016),
                CadDraw.P(168, -480.016),
            }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 1268), b01)),
            CadDraw.Line(CadDraw.P(150, -844.595), CadDraw.P(150, 844.564), b01),
            CadDraw.Line(CadDraw.P(-468, -986.904), CadDraw.P(-468, 479.984), b01),
            CadDraw.Line(CadDraw.P(-432, -480.016), CadDraw.P(-432, 950.872), b01),
            CadDraw.Line(CadDraw.P(468, -986.904), CadDraw.P(468, 479.984), b01),
            CadDraw.Line(CadDraw.P(432, -480.016), CadDraw.P(432, 950.872), b01),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1570.48, 1082.953, bulge: 0.414214),
                    CadDraw.V(-1770.48, 882.953),
                    CadDraw.V(-1770.48, -882.984, bulge: 0.414214),
                    CadDraw.V(-1570.48, -1082.984),
                    CadDraw.V(-484.885, -1082.984),
                    CadDraw.V(-284.885, -882.984),
                    CadDraw.V(284.885, -882.984),
                    CadDraw.V(484.885, -1082.984),
                    CadDraw.V(1570.48, -1082.984, bulge: 0.414214),
                    CadDraw.V(1770.48, -882.984),
                    CadDraw.V(1770.48, 882.953, bulge: 0.414214),
                    CadDraw.V(1570.48, 1082.953),
                    CadDraw.V(484.885, 1082.953),
                    CadDraw.V(284.885, 882.953),
                    CadDraw.V(-284.885, 882.953),
                    CadDraw.V(-484.885, 1082.953),
                ],
                b04,
                closed: true
            ),
            CadDraw.Line(CadDraw.P(-1895.794, 0), CadDraw.P(1842.121, 0), b06),
            CadDraw.Line(CadDraw.P(0, -942.6), CadDraw.P(0, 950.246), b06),
            CadDraw.Polyline(
                [
                    CadDraw.V(-358.485, -498.007),
                    CadDraw.V(-450, -550.35),
                    CadDraw.V(2345.897, -550.35),
                ],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1452.485, -284.084),
                    CadDraw.V(-1544, -336.427),
                    CadDraw.V(2345.897, -336.427),
                ],
                b07
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(735.515, -284.084), CadDraw.P(644, -336.427), b07),
                CadDraw.P(300, 0),
                4
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-925.396, -861.53), CadDraw.P(-873.054, -770.016), b07),
                CadDraw.P(0, 308),
                5
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-925.396, 678.47),
                    CadDraw.V(-873.054, 769.984),
                    CadDraw.V(-873.054, -1685.795),
                ],
                b07
            ),
            .. new[]
            {
                CadDraw.P(-1244, -336.427),
                CadDraw.P(-944, -336.427),
                CadDraw.P(-644, -336.427),
                CadDraw.P(-150, -550.35),
                CadDraw.P(-132, 291.321),
                CadDraw.P(132, 291.321),
                CadDraw.P(150, -550.35),
                CadDraw.P(168, 291.321),
                CadDraw.P(450, -550.35),
                CadDraw.P(662, 47.133),
                CadDraw.P(926, 47.133),
                CadDraw.P(962, 47.133),
                CadDraw.P(1226, 47.133),
                CadDraw.P(1262, 47.133),
                CadDraw.P(1526, 47.133),
                CadDraw.P(1562, 47.133),
            }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 91.515, p.Y + 52.343), b07)),
            CadDraw.Polyline(
                [
                    CadDraw.V(-363.008, -888.885),
                    CadDraw.V(-468, -852.728),
                    CadDraw.V(-294.745, -852.728),
                    CadDraw.V(-260.418, -1051.143),
                    CadDraw.V(260.418, -1051.143),
                    CadDraw.V(294.746, -852.728),
                    CadDraw.V(468, -852.728),
                    CadDraw.V(363.008, -888.885),
                ],
                b07
            ),
            CadDraw.Polyline(
                [CadDraw.V(717.515, 99.476), CadDraw.V(626, 47.133), CadDraw.V(2325.084, 47.133)],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1153.75, 943.338),
                    CadDraw.V(1101.408, 1034.853),
                    CadDraw.V(1101.408, -1674.488),
                ],
                b07
            ),
            CadDraw.Line(CadDraw.P(1153.75, -1126.399), CadDraw.P(1101.408, -1034.884), b07),
            CadDraw.Line(CadDraw.P(601.872, -920.476), CadDraw.P(549.529, -828.961), b07),
            CadDraw.Polyline(
                [
                    CadDraw.V(601.872, 737.415),
                    CadDraw.V(549.529, 828.93),
                    CadDraw.V(549.529, -1710.365),
                ],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-76.485, 343.664),
                    CadDraw.V(-168, 291.321),
                    CadDraw.V(2332.415, 291.321),
                ],
                b07
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-717.515, 292.354),
                    CadDraw.V(-626, 240.011),
                    CadDraw.V(-2311.089, 240.011),
                ],
                b07
            ),
            .. new[]
            {
                CadDraw.P(-1653.515, 292.354),
                CadDraw.P(-1617.515, 292.354),
                CadDraw.P(-1353.515, 292.354),
                CadDraw.P(-1317.515, 292.354),
                CadDraw.P(-1052.988, 292.354),
                CadDraw.P(-1016.988, 292.354),
                CadDraw.P(-753.515, 292.354),
            }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 91.515, p.Y - 52.343), b07)),
            CadDraw.Polyline(
                [
                    CadDraw.V(-327.008, 888.886),
                    CadDraw.V(-432, 852.728),
                    CadDraw.V(-294.745, 852.728),
                    CadDraw.V(-268.877, 1002.247),
                    CadDraw.V(268.878, 1002.247),
                    CadDraw.V(294.746, 852.728),
                    CadDraw.V(432, 852.728),
                    CadDraw.V(327.008, 888.886),
                ],
                b07
            ),
            CadDraw.Text(
                "（未示意墩身主筋）",
                CadDraw.P(-625.686, 1274.925),
                150,
                b07,
                widthFactor: 0.7
            ),
            HRB400Block.Insert(doc, CadDraw.P(53.051, -1146.387), "N8-2", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-959.895, -1326.272), "N7-2", "16", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(448.602, -1314.43), "N12-2", "14", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(1000.481, -1304.479), "N12-1", "14", 5, 1.571),
            HRB400Block.Insert(doc, CadDraw.P(2128.538, 402.523), "N8-1", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(2013.066, 150.125), "N9", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(2142.599, -447.358), "N6-2", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(2142.599, -233.435), "N6-1", "16", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1984.423, 332.278), "N9", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(53.051, 1088.695), "N8-2", "12", 5, 0),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 10 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddTranslated(orgin, entities);
    }

    /// <summary>截图 01 / 区域 7：源图窗口 [288, 382, 624, 590]，595 个实体，去重 9，11.05×6.96 m。</summary>
    public static void DrawRegion07(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-58578.044, -19528.554);

        List<Entity> entities =

            // 源图有 8 个重复对象（同层同几何，仅句柄不同），只画一份
            // 另有 1 个对象与块替身重合（_Oblique 块 / 断面与直线同位置），只画一份
            [
                .. TitleBlock.Add(doc, "正面钢筋布置图", "1:50", CadDraw.P(-216.859, 1257.368), 50),
                CadDraw.Polyline(
                    [CadDraw.V(-2428, -500), CadDraw.V(0, -500), CadDraw.V(2428, -500)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-2305.77, -700), CadDraw.V(0, -700), CadDraw.V(2305.771, -700)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-2190.986, -900), CadDraw.V(0, -900), CadDraw.V(2190.987, -900)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-2083.305, -1100), CadDraw.V(0, -1100), CadDraw.V(2083.305, -1100)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1982.423, -1300), CadDraw.V(0, -1300), CadDraw.V(1982.424, -1300)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1888.074, -1500), CadDraw.V(0, -1500), CadDraw.V(1888.075, -1500)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1800.018, -1700), CadDraw.V(0, -1700), CadDraw.V(1800.019, -1700)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1718.045, -1900), CadDraw.V(0, -1900), CadDraw.V(1718.046, -1900)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1641.965, -2100), CadDraw.V(0, -2100), CadDraw.V(1641.966, -2100)],
                    b01,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2428, -122),
                        CadDraw.V(-627.906, -122),
                        CadDraw.V(-97.479, -652.234),
                        CadDraw.V(-137.077, -691.832),
                    ],
                    b01
                ),
                CadDraw.Line(CadDraw.P(-730.943, -122), CadDraw.P(-1932.091, -1405), b01),
                CadDraw.Line(CadDraw.P(-1296.629, -122), CadDraw.P(-2131.976, -1008), b01),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1862, -1557.776),
                        CadDraw.V(-1862, -122),
                        CadDraw.V(-1862.314, -122),
                        CadDraw.V(-2351.32, -624),
                    ],
                    b01
                ),
                CadDraw.Polyline([CadDraw.V(2428, -500), CadDraw.V(2428, -100)], b01, closed: true),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2428, -122),
                        CadDraw.V(627.907, -122),
                        CadDraw.V(97.48, -652.234),
                        CadDraw.V(137.078, -691.832),
                    ],
                    b01
                ),
                CadDraw.Line(CadDraw.P(2428, -250), CadDraw.P(499.86, -250), b01),
                CadDraw.Polyline(
                    [CadDraw.V(2428, -378), CadDraw.V(-2428, -378)],
                    b01,
                    closed: true
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(662, -122), CadDraw.P(662, -2150), b01),
                    CadDraw.P(300, 0),
                    4
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1623.846, -2150, bulge: 0.054542), CadDraw.V(-2428, -500)],
                    b01
                ),
                CadDraw.Polyline(
                    [CadDraw.V(2428, -500, bulge: 0.054542), CadDraw.V(1623.847, -2150)],
                    b01
                ),
                CadDraw.Line(CadDraw.P(730.944, -122), CadDraw.P(1932.092, -1405), b01),
                CadDraw.Line(CadDraw.P(1296.63, -122), CadDraw.P(2131.977, -1008), b01),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1862, -1557.776),
                        CadDraw.V(1862, -122),
                        CadDraw.V(1862.315, -122),
                        CadDraw.V(2351.321, -624),
                    ],
                    b01
                ),
                CadDraw.Line(CadDraw.P(-2428, -500), CadDraw.P(-2428, -100), b01),
                CadDraw.Line(CadDraw.P(-2428, -250), CadDraw.P(-499.86, -250), b01),
                .. new[]
                {
                    CadDraw.P(-468.022, -2150),
                    CadDraw.P(-168.022, -2150),
                    CadDraw.P(168.023, -2150),
                    CadDraw.P(468.023, -2150),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 1772), b01)),
                CadDraw.Line(CadDraw.P(-2162, -952.584), CadDraw.P(-2162, -122), b01),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1562, -2150), CadDraw.P(-1562, -122), b01),
                    CadDraw.P(300, 0),
                    4
                ),
                CadDraw.Line(CadDraw.P(2162, -952.584), CadDraw.P(2162, -122), b01),
                // 4 处点位标记（零长度 Line），位置不等距，用点列表表达
                .. new[]
                {
                    CadDraw.P(-600, 0),
                    CadDraw.P(600, 0),
                    CadDraw.P(-800, 0),
                    CadDraw.P(800, 0),
                }.Select(p => CadDraw.Line(p, p, b04)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(3895.512, -1950),
                        CadDraw.V(4089.265, -1950),
                        CadDraw.V(4089.265, -1754.916),
                        CadDraw.V(4103.986, -1813.873),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(3895.512, -100),
                        CadDraw.V(4089.265, -100),
                        CadDraw.V(4089.265, -295.085),
                        CadDraw.V(4103.986, -236.128),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(3895.512, -350),
                        CadDraw.V(4089.265, -350),
                        CadDraw.V(4089.265, -545.085),
                        CadDraw.V(4103.986, -486.128),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3825.644, -1950),
                        CadDraw.V(-4019.397, -1950),
                        CadDraw.V(-4019.397, -1754.916),
                        CadDraw.V(-4034.118, -1813.873),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3825.644, -100),
                        CadDraw.V(-4019.397, -100),
                        CadDraw.V(-4019.397, -295.085),
                        CadDraw.V(-4034.118, -236.128),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3825.644, -350),
                        CadDraw.V(-4019.397, -350),
                        CadDraw.V(-4019.397, -545.085),
                        CadDraw.V(-4034.118, -486.128),
                    ],
                    b04
                ),
                CadDraw.Line(CadDraw.P(600, 0), CadDraw.P(2500, 0), b04),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-600, 0),
                        CadDraw.V(-300, -300),
                        CadDraw.V(300, -300),
                        CadDraw.V(600, 0),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1405.288, -3150, bulge: 0.085567),
                        CadDraw.V(-2500, -500),
                        CadDraw.V(-2500, 0),
                        CadDraw.V(-600, 0),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2500, 0),
                        CadDraw.V(2500, -500, bulge: 0.085567),
                        CadDraw.V(1405.289, -3150),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2167.448, -3150),
                        CadDraw.V(-1577.037, -3150),
                        CadDraw.V(-142.605, -3150),
                        CadDraw.V(-142.605, -3032.607),
                        CadDraw.V(241.603, -3267.393),
                        CadDraw.V(241.603, -3150),
                        CadDraw.V(1676.035, -3150),
                        CadDraw.V(2266.446, -3150),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-167.863, -3150, bulge: 0.031511), CadDraw.V(-600, 0)],
                    b04
                ),
                CadDraw.Polyline(
                    [CadDraw.V(600, 0, bulge: 0.0322), CadDraw.V(162.87, -3219.281)],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-800, 0),
                        CadDraw.V(-800, 0, bulge: -0.031511),
                        CadDraw.V(-367.863, -3150, bulge: -0.021061),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(800, 0),
                        CadDraw.V(800, 0, bulge: 0.031511),
                        CadDraw.V(367.864, -3150, bulge: 0.021061),
                    ],
                    b04
                ),
                .. new[]
                {
                    CadDraw.P(-644, -356),
                    CadDraw.P(-450.022, -356),
                    CadDraw.P(-300.022, -356),
                    CadDraw.P(-150.022, -356),
                    CadDraw.P(90.199, -559.727),
                    CadDraw.P(240.199, -559.727),
                    CadDraw.P(390.199, -559.727),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 59.824, p.Y + 81.727), b07)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1049.692, -158.486),
                        CadDraw.V(-997.35, -250),
                        CadDraw.V(-997.35, 568.157),
                    ],
                    b07
                ),
                CadDraw.Line(CadDraw.P(995.302, -286.486), CadDraw.P(1047.645, -378), b07),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1967.955, -376.671),
                        CadDraw.V(-2022.521, -286.464),
                        CadDraw.V(-757.919, -989.242),
                        CadDraw.V(-757.919, -2819.498),
                    ],
                    b07
                ),
                CadDraw.Line(CadDraw.P(-1593.384, -584.831), CadDraw.P(-1647.95, -494.625), b07),
                CadDraw.Line(CadDraw.P(-1219.665, -792.518), CadDraw.P(-1274.232, -702.311), b07),
                CadDraw.Line(CadDraw.P(-150.022, -2078), CadDraw.P(-100.282, -2229.013), b07),
                CadDraw.Line(CadDraw.P(150.023, -2078), CadDraw.P(100.283, -2229.013), b07),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-450.022, -2078),
                        CadDraw.V(-400.282, -2229.013),
                        CadDraw.V(400.283, -2229.013),
                        CadDraw.V(450.023, -2078),
                    ],
                    b07
                ),
                .. new[]
                {
                    CadDraw.P(-450.022, -478),
                    CadDraw.P(-300.022, -478),
                    CadDraw.P(-150.022, -478),
                    CadDraw.P(90.199, -274.273),
                    CadDraw.P(240.199, -274.273),
                    CadDraw.P(390.199, -274.273),
                    CadDraw.P(584.176, -274.273),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 59.824, p.Y - 81.727), b07)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-644, -478),
                        CadDraw.V(-584.175, -559.727),
                        CadDraw.V(584.176, -559.727),
                        CadDraw.V(644, -478),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2253.515, -695.251),
                        CadDraw.V(2162, -747.593),
                        CadDraw.V(3036.925, -740.124),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1953.515, -898.73),
                        CadDraw.V(1862, -951.072),
                        CadDraw.V(2944.338, -951.072),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(794, -478),
                        CadDraw.V(833.544, -556.054),
                        CadDraw.V(3152.635, -556.054),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(944, -478), CadDraw.P(983.544, -556.054), b07),
                    CadDraw.P(150, 0),
                    10
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2444, -225),
                        CadDraw.V(2600.812, -225),
                        CadDraw.V(2600.812, -100),
                        CadDraw.V(2444, -100),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1665.966, -2100),
                        CadDraw.V(1814.778, -2100, bulge: -0.052961),
                        CadDraw.V(2600.812, -500),
                        CadDraw.V(2600.812, -350),
                        CadDraw.V(2444, -350),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-753.514, -1340.962),
                        CadDraw.V(-662, -1393.305),
                        CadDraw.V(-1662.389, -1393.305),
                        CadDraw.V(-1662.389, -2789.139),
                    ],
                    b07
                ),
                .. new[]
                {
                    CadDraw.P(-1653.514, -1340.962),
                    CadDraw.P(-1353.514, -1340.962),
                    CadDraw.P(-1053.514, -1340.962),
                    CadDraw.P(-168.022, -1176.255),
                    CadDraw.P(168.023, -1176.255),
                    CadDraw.P(468.023, -1176.255),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 91.515, p.Y - 52.343), b07)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-376.507, -1228.598),
                        CadDraw.V(-468.022, -1176.255),
                        CadDraw.V(818.114, -1176.255),
                        CadDraw.V(818.114, -3006.967),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-794, -356),
                        CadDraw.V(-825.264, -294.288),
                        CadDraw.V(-2829.143, -294.288),
                        CadDraw.V(-2829.143, -166.288),
                        CadDraw.V(-912.735, -166.288),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2294, -356), CadDraw.P(-2325.264, -294.288), b07),
                    CadDraw.P(150, 0),
                    10
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-794, -478),
                        CadDraw.V(-833.543, -556.054),
                        CadDraw.V(-3181.006, -556.054),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2294, -478), CadDraw.P(-2333.543, -556.054), b07),
                    CadDraw.P(150, 0),
                    10
                ),
                CadDraw.Line(CadDraw.P(-584.175, -274.273), CadDraw.P(-644, -228), b07),
                CadDraw.Line(CadDraw.P(-912.735, -166.288), CadDraw.P(-944, -228), b07),
                .. CadDraw.EntityArray(
                    CadDraw.Polyline(
                        [
                            CadDraw.V(-2144, -100),
                            CadDraw.V(-2112.735, -166.288),
                            CadDraw.V(-2144, -228),
                        ],
                        b07
                    ),
                    CadDraw.P(300, 0),
                    4
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2443.999, -225),
                        CadDraw.V(-2600.811, -225),
                        CadDraw.V(-2600.811, -100),
                        CadDraw.V(-2443.999, -100),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1665.966, -2100),
                        CadDraw.V(-1814.777, -2100, bulge: 0.052961),
                        CadDraw.V(-2600.811, -500),
                        CadDraw.V(-2600.811, -350),
                        CadDraw.V(-2443.999, -350),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2253.514, -669.3),
                        CadDraw.V(-2162, -721.643),
                        CadDraw.V(-3017.85, -721.643),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1329.778, -2191.515), CadDraw.P(1382.121, -2100), b07),
                    CadDraw.P(0, 200),
                    5
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1953.514, -898.73),
                        CadDraw.V(-1862, -951.072),
                        CadDraw.V(-3021.658, -951.072),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-422.68, -192.867),
                        CadDraw.V(-525.927, -214.194),
                        CadDraw.V(-186.562, 85.406),
                        CadDraw.V(186.562, 85.406),
                        CadDraw.V(525.928, -214.194),
                        CadDraw.V(422.681, -192.867),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1329.778, -1191.515),
                        CadDraw.V(1382.121, -1100),
                        CadDraw.V(1382.121, -2819.498),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(644, -2078),
                        CadDraw.V(772.026, -2244.533),
                        CadDraw.V(2331.019, -2244.533),
                    ],
                    b07
                ),
                .. new[]
                {
                    CadDraw.P(944, -2078),
                    CadDraw.P(1244, -2078),
                    CadDraw.P(1544, -2078),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 128.026, p.Y - 166.532), b07)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2152.095, -1091.574),
                        CadDraw.V(-2060.58, -1143.917),
                        CadDraw.V(-2880.231, -1144.978),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2152.095, -1091.574),
                        CadDraw.V(2060.581, -1143.917),
                        CadDraw.V(2880.232, -1144.978),
                    ],
                    b07
                ),
                CadDraw.Line(CadDraw.P(-912.735, -166.288), CadDraw.P(-944, -100), b07),
                CadDraw.Line(CadDraw.P(-584.175, -274.273), CadDraw.P(-644, -100), b07),
                CadDraw.Polyline(
                    [
                        CadDraw.V(912.736, -166.288),
                        CadDraw.V(2829.144, -166.288),
                        CadDraw.V(2829.144, -294.288),
                        CadDraw.V(825.265, -294.288),
                        CadDraw.V(794, -356),
                    ],
                    b07
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(944, -356), CadDraw.P(975.265, -294.288), b07),
                    CadDraw.P(150, 0),
                    10
                ),
                CadDraw.Line(CadDraw.P(912.736, -166.288), CadDraw.P(944, -228), b07),
                .. CadDraw.EntityArray(
                    CadDraw.Polyline(
                        [
                            CadDraw.V(1244, -100),
                            CadDraw.V(1212.736, -166.288),
                            CadDraw.V(1244, -228),
                        ],
                        b07
                    ),
                    CadDraw.P(300, 0),
                    4
                ),
                CadDraw.Line(CadDraw.P(912.736, -166.288), CadDraw.P(944, -100), b07),
                CadDraw.Line(CadDraw.P(2829.144, -253.655), CadDraw.P(3339.617, -253.655), b07),
                CadDraw.Polyline(
                    [
                        CadDraw.V(995.302, -408.486),
                        CadDraw.V(1047.645, -500),
                        CadDraw.V(1047.645, 729.965),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(993.369, -791.515),
                        CadDraw.V(1045.711, -700),
                        CadDraw.V(1045.711, -2819.498),
                    ],
                    b07
                ),
                CadDraw.Line(CadDraw.P(993.369, -991.515), CadDraw.P(1045.711, -900), b07),
                CadDraw.Line(CadDraw.P(584.176, -274.273), CadDraw.P(644, -228), b07),
                CadDraw.Line(CadDraw.P(584.176, -274.273), CadDraw.P(644, -100), b07),
                CadDraw.Line(CadDraw.P(-584.175, -274.273), CadDraw.P(584.176, -274.273), b07),
                CadDraw.Line(CadDraw.P(0, -559.727), CadDraw.P(0, -478), b07),
                CadDraw.Line(CadDraw.P(0, -356), CadDraw.P(0, -274.273), b07),
                CadDraw.Line(CadDraw.P(-2600.811, -450), CadDraw.P(-2443.999, -450), b07),
                CadDraw.Line(CadDraw.P(2444, -450), CadDraw.P(2600.812, -450), b07),
                .. new[]
                {
                    CadDraw.P(-2538.743, -600),
                    CadDraw.P(-2449.202, -750),
                    CadDraw.P(-2363.798, -900),
                    CadDraw.P(-2282.388, -1050),
                    CadDraw.P(-2204.843, -1200),
                    CadDraw.P(-2131.045, -1350),
                    CadDraw.P(-2060.885, -1500),
                    CadDraw.P(-1994.266, -1650),
                    CadDraw.P(-1931.096, -1800),
                    CadDraw.P(-1871.291, -1950),
                    CadDraw.P(1722.481, -1950),
                    CadDraw.P(1782.285, -1800),
                    CadDraw.P(1845.455, -1650),
                    CadDraw.P(1912.075, -1500),
                    CadDraw.P(1982.234, -1350),
                    CadDraw.P(2056.032, -1200),
                    CadDraw.P(2133.577, -1050),
                    CadDraw.P(2214.987, -900),
                    CadDraw.P(2300.391, -750),
                    CadDraw.P(2389.932, -600),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 148.812, p.Y), b07)),
                CadDraw.Line(CadDraw.P(-3339.616, -253.655), CadDraw.P(-2829.143, -253.655), b07),
                CadDraw.Text(
                    "Ⅴ",
                    CadDraw.P(3941.073, -1946.19),
                    175,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7,
                    alignmentPoint: CadDraw.P(3973.156, -1946.19)
                ),
                CadDraw.Text(
                    "Ⅲ",
                    CadDraw.P(3941.073, -278.391),
                    175,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7,
                    alignmentPoint: CadDraw.P(3973.156, -278.391)
                ),
                CadDraw.Text(
                    "Ⅳ",
                    CadDraw.P(3941.073, -533.532),
                    175,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7,
                    alignmentPoint: CadDraw.P(3973.156, -533.532)
                ),
                CadDraw.Text(
                    "Ⅴ",
                    CadDraw.P(-3958.978, -1946.19),
                    175,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7,
                    alignmentPoint: CadDraw.P(-3926.895, -1946.19)
                ),
                CadDraw.Text(
                    "Ⅲ",
                    CadDraw.P(-3958.978, -278.391),
                    175,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7,
                    alignmentPoint: CadDraw.P(-3926.895, -278.391)
                ),
                CadDraw.Text(
                    "Ⅳ",
                    CadDraw.P(-3965.667, -533.532),
                    175,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7,
                    alignmentPoint: CadDraw.P(-3933.584, -533.532)
                ),
                CadDraw.Text(
                    "墩身主筋",
                    CadDraw.P(-2874.326, -1114.627),
                    125,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "墩身主筋",
                    CadDraw.P(2362.502, -1114.627),
                    125,
                    b07,
                    widthFactor: 0.75
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1665.966, -2100),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1722.48, -1950),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1782.284, -1800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1845.454, -1650),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1912.074, -1500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1982.233, -1350),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2056.032, -1200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2133.576, -1050),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2214.986, -900),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2300.391, -750),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2389.931, -600),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(450.023, -2078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(0, 200),
                    9
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(150.023, -2078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(0, 200),
                    9
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(300.023, -478),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(450.023, -356),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(300.023, -356),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(150.023, -356),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2452, -450),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2452, -350),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(0, 125),
                    3
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-450.022, -2078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(0, 200),
                    9
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-150.022, -2078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(0, 200),
                    9
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2294, -478),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    12
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-300.022, -478),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(0, -478),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1544, -2078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    4
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1544, -1878),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    4
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1544, -1678),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    4
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1844, -1478),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1844, -1278),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1844, -1078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2144, -878),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    11
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2294, -356),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    12
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2144, -228),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    6
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1844, -100),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2144, -100),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-450.022, -356),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    3
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(0, -356),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2294, -678),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    12
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1665.966, -2100),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1722.481, -1950),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1782.285, -1800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1845.455, -1650),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1912.075, -1500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1982.234, -1350),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2056.032, -1200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2133.577, -1050),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2214.987, -900),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2300.391, -750),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2389.932, -600),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2452, -450),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Rotation = 0,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -2078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    4
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -1878),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    4
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -1678),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    4
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -1478),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -1278),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -1078),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    5
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -878),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    11
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -678),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    12
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -478),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    12
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -356),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(150, 0),
                    12
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -228),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    6
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(644, -100),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(300, 0),
                    6
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(2452, -100),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Rotation = 0,
                        Layer = b01,
                    },
                    CadDraw.P(0, -125),
                    3
                ),
                HRB400Block.Insert(doc, CadDraw.P(-24.383, 175.976), "N2", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(80.824, -192.138), "N3-2", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(80.824, -633.405), "N6-2", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-3024.57, -182.614), "N3-1", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2821.956, -487.528), "N6-1", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2754.34, -649.172), "N11", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2644.926, -870.776), "N10", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2248.343, -1573.225), "N12-1", "14", 5, 5.009),
                HRB400Block.Insert(doc, CadDraw.P(2131.607, -2174.641), "N6-1", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1741.165, -2497.554), "N9", "12", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-1073.19, 409.68), "N7-1", "16", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-835.936, -2567.508), "N5", "20", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(969.567, 308.378), "N4-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(122.745, -2308.611), "N6-2", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1304.104, -2467.462), "N7-2", "16", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(740.098, -2430.912), "N8-1～2", "12", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(2280.256, -1378.383), "N12-1", "14", 5, 1.274),
                HRB400Block.Insert(doc, CadDraw.P(2694.406, -885.446), "N10", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2795.65, -677.837), "N11", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2931.785, -487.528), "N6-1", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(3153.255, -171.633), "N3-1", "16", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2683.231, 16.918), "N13", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-2716.976, -7.797), "N13", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(967.695, -2467.462), "N1-1", "20", 5, 1.571),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 6 段
            CadDraw.RotatedDimension(
                CadDraw.P(-644, -412.501),
                CadDraw.P(-450.022, -568.643),
                CadDraw.P(-450.022, 352.046),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-450.022, -412.501),
                CadDraw.P(450.023, -568.643),
                CadDraw.P(450.023, 352.046),
                0,
                dimStyle,
                b03,
                "6×150"
            ),
            .. CadDraw.ChainDimension(
                [CadDraw.P(-2294, 0), CadDraw.P(-2428, 0), CadDraw.P(-2500, 0)],
                CadDraw.P(-2428, 352.046),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2294, 152.046),
                CadDraw.P(-644, -100),
                CadDraw.P(-644, 352.046),
                0,
                dimStyle,
                b03,
                "11×150"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2500, -343.257),
                CadDraw.P(2500, -343.257),
                CadDraw.P(2500, 612.001),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 9 段
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-3298.512, 0),
                    CadDraw.P(-3165.779, -122),
                    CadDraw.P(-3029.329, -378),
                    CadDraw.P(-3029.329, -500),
                    CadDraw.P(-2363.473, -2100),
                ],
                CadDraw.P(-3583.443, -122),
                Math.PI / 2,
                dimStyle,
                b03,
                ["", "2×128", "", "8×200"]
            ),
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(3374.065, 0),
                    CadDraw.P(3302.065, -100),
                    CadDraw.P(3302.065, -350),
                    CadDraw.P(3042.483, -450),
                    CadDraw.P(2674.394, -2100),
                ],
                CadDraw.P(3606.598, -100),
                Math.PI / 2,
                dimStyle,
                b03,
                ["", "2×125", "", "11×150"]
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(3374.065, 0),
                CadDraw.P(2674.394, -2100),
                CadDraw.P(3825.518, -2100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 4 段
            CadDraw.RotatedDimension(
                CadDraw.P(644, -412.501),
                CadDraw.P(450.023, -568.643),
                CadDraw.P(450.023, 352.046),
                Math.PI,
                dimStyle,
                b03
            ),
            .. CadDraw.ChainDimension(
                [CadDraw.P(2294, 0), CadDraw.P(2428, 0), CadDraw.P(2500, 0)],
                CadDraw.P(2428, 352.046),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2294, 152.09),
                CadDraw.P(644, -100),
                CadDraw.P(644, 352.046),
                Math.PI,
                dimStyle,
                b03,
                "11×150"
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 24 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 8：源图窗口 [1230, 438, 1408, 548]，2 个实体，5.86×3.68 m。</summary>
    public static void DrawRegion08(CadDocument doc)
    {
        var b07 = doc.Layer(CadLayers.B07);

        List<Entity> entities =
        [
            CadDraw.MText(
                "附注：\\P1、本图尺寸以毫米计。\\P2、“  ”表示HRB400E钢筋,\\P3、安装支座螺栓如与顶帽钢筋有抵触时可适当调整钢筋间距。\\P4、钢筋弯钩为标准弯钩。\\P5、墩顶采用C40混凝土，钢筋保护层厚度不小于40mm。\\P6、钢筋接头按照《铁路混凝土工程施工技术规程》（Q/CR9207-2017）办理，采用套筒连接。\\P{\\C4;7、本图适用于12#～15#桥墩。}\\P8、墩身主筋详细布置见相应墩身钢筋布置图。\\P9、N5钢筋在弯折处与N2钢筋及墩身主筋采用单面焊接，焊缝长度不小于10d。",
                CadDraw.P(-33170.314, -19007.207),
                175,
                b07,
                attachment: AttachmentPointType.TopLeft,
                rectangleWidth: 6276.477,
                rectangleHeight: 4922.788
            ),
            HRB400Block.Insert(doc, CadDraw.P(-32683.547, -19774.669), "", "", 7, 0),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 1 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddEntities(entities);
    }

    /// <summary>截图 01 / 区域 9：源图窗口 [1112, 578, 1232, 642]，2 个实体，3.95×2.16 m。</summary>
    public static void DrawRegion09(CadDocument doc)
    {
        var b07 = doc.Layer(CadLayers.B07);

        List<Entity> entities =
        [
            CadDraw.MText(
                "附注：\\P1、本图尺寸以毫米计。\\P2、“  ”表示HRB400E钢筋。\\P{\\C4;3、本图适用于12#～15#桥墩。}\\P4、墩身主筋详细布置见相应墩身钢筋布置图。",
                CadDraw.P(-37144.136, -23829.815),
                140,
                b07,
                attachment: AttachmentPointType.TopLeft,
                rectangleWidth: 3410.557,
                rectangleHeight: 1733.073
            ),
            HRB400Block.Insert(doc, CadDraw.P(-36746.862, -24447.835), "", "", 7, 0),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 1 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b07;
        }

        doc.AddEntities(entities);
    }
}
