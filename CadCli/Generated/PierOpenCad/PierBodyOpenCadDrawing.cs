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
/// 由 OpenCAD Studio 分区导出（region-*.json）逐区还原的图纸。
/// 每个区域一个方法，坐标保留源图绝对坐标，图层按标准图集映射为 B-01/B-03/B-04/B-06/B-07。
/// 同图层内端点距离 ≤ 0.5 mm 的直线、多段线与圆弧已先行合并为一条多段线；
/// 等间距的直线、多段线、钢筋断面族用 CadDraw.EntityArray 展开为阵列，不再逐条抄写。
/// 块参照映射：HRB400 → HRB400Block，钢筋断面/钢筋轮廓（双筋）→ SteelSection。
/// 只还原模型空间实体：尺寸块/图块内的图形缓存（owner≠模型空间）不再重复绘制；
/// Defpoints 定位点与 Hatch 填充不还原。
/// </summary>
public static class PierBodyOpenCadDrawing
{
    /// <summary>
    /// 绘制 OpenCAD Studio 当前视图（03 桥墩墩身钢筋图）的全部 8 个分区：
    /// 侧面/正面钢筋布置图、Ⅰ-Ⅰ/Ⅱ-Ⅱ/Ⅲ-Ⅲ截面、钢筋大样图、墩身钢筋数量表、附注。
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
    }

    /// <summary>截图 01 / 区域 1：源图窗口 [39, 258, 230, 536]，210 个实体，去重 1，8.80×12.80 m。</summary>
    public static void DrawRegion01(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以 ID 指定点 (-111285.3700, 16017.6970) 为分区原点：
        // 图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-111285.3700, 16017.6970);

        List<Entity> entities =

            // 源图有 1 个重复对象（同层同几何，仅句柄不同），只画一份
            [
                .. TitleBlock.Add(doc, "侧面钢筋布置图", "1:50", CadDraw.P(-180.891, 8487.545), 50),
                CadDraw.Polyline(
                    [CadDraw.V(900, 2598.514), CadDraw.V(900, -1400)],
                    b01,
                    closed: true
                ),
                CadDraw.Polyline(
                    [CadDraw.V(1028, 2498.514), CadDraw.V(-1028, 2498.514)],
                    b01,
                    closed: true
                ),
                .. new[]
                {
                    CadDraw.P(400, 7498.514),
                    CadDraw.P(1000, 7498.514),
                    CadDraw.P(1228, 7498.514),
                }.Select(p =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(p.X, p.Y),
                            CadDraw.V(p.X, p.Y - 400, bulge: 0.022211),
                            CadDraw.V(p.X - 200, p.Y - 4900),
                            CadDraw.V(p.X - 200, p.Y - 8898.515),
                        ],
                        b01
                    )
                ),
                .. new[]
                {
                    CadDraw.P(400, -1400),
                    CadDraw.P(600, -1400),
                    CadDraw.P(990.51, -1400),
                }.Select(p =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(p.X, p.Y),
                            CadDraw.V(p.X, p.Y + 3998.515, bulge: -0.022211),
                            CadDraw.V(p.X + 200, p.Y + 8498.515),
                            CadDraw.V(p.X + 200, p.Y + 8898.515),
                        ],
                        b01
                    )
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Polyline(
                        [
                            CadDraw.V(500, 7498.514),
                            CadDraw.V(500, 7098.514, bulge: 0.022211),
                            CadDraw.V(300, 2598.514),
                            CadDraw.V(300, -1400),
                        ],
                        b01
                    ),
                    CadDraw.P(200, 0),
                    3
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1028, -1400),
                        CadDraw.V(-1028, 2598.514, bulge: 0.022211),
                        CadDraw.V(-1228, 7098.514),
                        CadDraw.V(-1228, 7498.514),
                    ],
                    b01
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-990.509, -1400),
                        CadDraw.V(-990.509, 2598.514, bulge: 0.022211),
                        CadDraw.V(-1190.509, 7098.514),
                        CadDraw.V(-1190.509, 7498.514),
                    ],
                    b01
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Polyline(
                        [
                            CadDraw.V(-900, -1400),
                            CadDraw.V(-900, 2598.514, bulge: 0.022211),
                            CadDraw.V(-1100, 7098.514),
                            CadDraw.V(-1100, 7498.514),
                        ],
                        b01
                    ),
                    CadDraw.P(100, 0),
                    3
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-800, 7498.514),
                        CadDraw.V(-800, 7098.514, bulge: -0.022211),
                        CadDraw.V(-600, 2598.514),
                        CadDraw.V(-600, -1400),
                    ],
                    b01
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-700, 7498.514),
                        CadDraw.V(-700, 7098.514, bulge: -0.022211),
                        CadDraw.V(-500, 2598.514),
                        CadDraw.V(-500, -1400),
                    ],
                    b01
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Polyline(
                        [
                            CadDraw.V(-400, -1400),
                            CadDraw.V(-400, 2598.514, bulge: 0.022211),
                            CadDraw.V(-600, 7098.514),
                            CadDraw.V(-600, 7498.514),
                        ],
                        b01
                    ),
                    CadDraw.P(100, 0),
                    3
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-100, 7498.514),
                        CadDraw.V(-100, 2598.514, bulge: 0.022211),
                        CadDraw.V(-300, 7098.514),
                        CadDraw.V(-300, 7498.514),
                    ],
                    b01
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(900, 2598.514, bulge: -0.022211),
                        CadDraw.V(1100, 7098.514),
                        CadDraw.V(1100, 7498.514),
                    ],
                    b01
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(300, 7498.514),
                        CadDraw.V(300, 7098.514, bulge: 0.022211),
                        CadDraw.V(100, 2598.514),
                    ],
                    b01
                ),
                CadDraw.Line(CadDraw.P(-1073.584, 4748.514), CadDraw.P(1073.585, 4748.514), b01),
                CadDraw.Line(CadDraw.P(-1043.404, 3848.514), CadDraw.P(1043.404, 3848.514), b01),
                CadDraw.Line(CadDraw.P(-1061.747, 4448.514), CadDraw.P(1061.747, 4448.514), b01),
                CadDraw.Line(CadDraw.P(-1056.495, 4298.514), CadDraw.P(1056.495, 4298.514), b01),
                CadDraw.Line(CadDraw.P(-1080.17, 4898.514), CadDraw.P(1080.171, 4898.514), b01),
                CadDraw.Line(CadDraw.P(-1029.207, 2948.514), CadDraw.P(1029.208, 2948.514), b01),
                CadDraw.Line(CadDraw.P(-1030.464, 3098.514), CadDraw.P(1030.465, 3098.514), b01),
                CadDraw.Line(CadDraw.P(-1032.164, 3248.514), CadDraw.P(1032.165, 3248.514), b01),
                CadDraw.Line(CadDraw.P(200, 5782.063), CadDraw.P(200, 7498.514), b01),
                CadDraw.Line(CadDraw.P(-1051.687, 4148.514), CadDraw.P(1051.688, 4148.514), b01),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1028, 1148.514), CadDraw.P(1028, 1148.514), b01),
                    CadDraw.P(0, 150),
                    9
                ),
                CadDraw.Line(CadDraw.P(-1102.599, 5348.514), CadDraw.P(1102.599, 5348.514), b01),
                CadDraw.Line(CadDraw.P(-1028.024, 2648.514), CadDraw.P(1028.025, 2648.514), b01),
                CadDraw.Line(CadDraw.P(-1028.394, 2798.514), CadDraw.P(1028.395, 2798.514), b01),
                CadDraw.Line(CadDraw.P(-1039.928, 3698.514), CadDraw.P(1039.929, 3698.514), b01),
                CadDraw.Line(CadDraw.P(-1067.443, 4598.514), CadDraw.P(1067.444, 4598.514), b01),
                CadDraw.Line(CadDraw.P(-200, 5782.063), CadDraw.P(-200, 7498.514), b01),
                CadDraw.Line(CadDraw.P(100, -1400), CadDraw.P(100, 7498.514), b01),
                CadDraw.Line(CadDraw.P(0, -1400), CadDraw.P(0, 7498.514), b01),
                CadDraw.Line(CadDraw.P(-100, -1400), CadDraw.P(-100, 2598.514), b01),
                CadDraw.Line(CadDraw.P(-1094.677, 5198.514), CadDraw.P(1094.678, 5198.514), b01),
                CadDraw.Line(CadDraw.P(-1047.323, 3998.514), CadDraw.P(1047.324, 3998.514), b01),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1028, -1300), CadDraw.P(1028, -1300), b01),
                    CadDraw.P(0, 100),
                    24
                ),
                CadDraw.Line(CadDraw.P(-1036.896, 3548.514), CadDraw.P(1036.897, 3548.514), b01),
                CadDraw.Line(CadDraw.P(-1034.309, 3398.514), CadDraw.P(1034.309, 3398.514), b01),
                CadDraw.Line(CadDraw.P(-1087.201, 5048.514), CadDraw.P(1087.202, 5048.514), b01),
                CadDraw.Polyline(
                    [CadDraw.V(800, 7598.514), CadDraw.V(800, 7598.514)],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-800, 7598.514), CadDraw.V(-800, 7598.514)],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1395.561, -2409.633),
                        CadDraw.V(1395.561, -1156.705),
                        CadDraw.V(1514.054, -1156.705),
                        CadDraw.V(1277.068, -931.396),
                        CadDraw.V(1395.561, -931.396),
                        CadDraw.V(1395.561, 0),
                        CadDraw.V(1100, 0),
                        CadDraw.V(1100, 1050),
                        CadDraw.V(1100, 2598.514, bulge: -0.022211),
                        CadDraw.V(1300, 7098.514),
                        CadDraw.V(1300, 7598.514),
                        CadDraw.V(600, 7598.514),
                        CadDraw.V(600, 7598.514),
                        CadDraw.V(-600, 7598.514),
                        CadDraw.V(-600, 7598.514),
                        CadDraw.V(-1300, 7598.514),
                        CadDraw.V(-1300, 7098.514, bulge: -0.022211),
                        CadDraw.V(-1100, 2598.514),
                        CadDraw.V(-1100, 1050),
                        CadDraw.V(-1100, 0),
                        CadDraw.V(-1511.258, 0),
                        CadDraw.V(-1511.258, -931.396),
                        CadDraw.V(-1392.765, -931.396),
                        CadDraw.V(-1629.751, -1156.705),
                        CadDraw.V(-1511.258, -1156.705),
                        CadDraw.V(-1511.258, -2409.633),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-18.785, 7908.521),
                        CadDraw.V(-67.916, 7896.253),
                        CadDraw.V(94.654, 7896.253),
                        CadDraw.V(94.654, 7655.353),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-228.881, -2247.596),
                        CadDraw.V(-228.881, -2104.97),
                        CadDraw.V(228.882, -2411.314),
                        CadDraw.V(228.882, -2239.272),
                        CadDraw.V(1395.561, -2239.272),
                        CadDraw.V(228.882, -2239.272),
                        CadDraw.V(228.882, -2411.314),
                        CadDraw.V(-228.881, -2104.97),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(94.654, -2482.954),
                        CadDraw.V(94.654, -2723.853),
                        CadDraw.V(-67.916, -2723.853),
                        CadDraw.V(-18.785, -2736.122),
                    ],
                    b04
                ),
                CadDraw.Line(CadDraw.P(-1511.258, -2247.596), CadDraw.P(-228.881, -2247.596), b04),
                CadDraw.Polyline(
                    [CadDraw.V(900, 2124.42), CadDraw.V(800, 2164.42)],
                    b03,
                    closed: true
                ),
                CadDraw.Polyline(
                    [CadDraw.V(584.106, 900), CadDraw.V(624.106, 1000)],
                    b03,
                    closed: true
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(742.296, 2798.514), CadDraw.P(782.296, 2898.514), b03),
                    CadDraw.P(0, 150),
                    17
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(584.106, 1148.514), CadDraw.P(624.106, 1248.514), b03),
                    CadDraw.P(0, 150),
                    9
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(100, 2124.42), CadDraw.P(0, 2164.42), b03),
                    CadDraw.P(100, 0),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(584.106, -1200), CadDraw.P(624.106, -1100), b03),
                    CadDraw.P(0, 100),
                    21
                ),
                .. new[]
                {
                    CadDraw.P(-1128, 2164.42),
                    CadDraw.P(-1000, 2164.42),
                    CadDraw.P(-300, 6929.996),
                    CadDraw.P(-200, 6929.996),
                    CadDraw.P(0, 6929.996),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 100, p.Y - 40), b03)),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-800, 2124.42), CadDraw.P(-900, 2164.42), b03),
                    CadDraw.P(100, 0),
                    8
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1701.816, 2124.42),
                        CadDraw.V(1028, 2124.42),
                        CadDraw.V(928, 2164.42),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(100, 6929.996),
                        CadDraw.V(200, 6889.996),
                        CadDraw.V(-1814.83, 6889.996),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(624.106, 2598.514),
                        CadDraw.V(584.106, 2498.514),
                        CadDraw.V(584.106, -1300),
                        CadDraw.V(624.106, -1200),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(782.296, 2748.514),
                        CadDraw.V(742.296, 2648.514),
                        CadDraw.V(742.296, 5348.514),
                        CadDraw.V(782.296, 5448.514),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1762.401, 6371.724),
                        CadDraw.V(0, 6371.724),
                        CadDraw.V(-100, 6411.724),
                    ],
                    b03
                ),
                CadDraw.Line(CadDraw.P(584.106, 1106.115), CadDraw.P(2312.779, 1106.115), b03),
                CadDraw.Line(CadDraw.P(742.296, 3943.024), CadDraw.P(2495.043, 3943.024), b03),
                CadDraw.Text(
                    "Ⅴ",
                    CadDraw.P(-13.315, -2672.028),
                    150,
                    b07,
                    0,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7
                ),
                CadDraw.Text(
                    "Ⅴ",
                    CadDraw.P(-13.315, 7704.968),
                    150,
                    b07,
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    widthFactor: 0.7
                ),
                HRB400Block.Insert(doc, CadDraw.P(-1407.296, 2190.377), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2186.552, 3812.577), "N4-2", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1491.561, 4049.716), "N3-2", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1491.561, 3812.577), "N5-2", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1509.506, 1212.807), "N3-1", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1509.506, 975.667), "N5-1", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2080.739, 975.667), "N4-1", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2080.739, 1212.807), "N2-1", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1520.31, 6955.953), "N1-6", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1467.881, 6437.681), "N1-5", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2186.552, 4049.716), "N2-2", "14", 5, 0),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        // 首尾严格相接、同一条尺寸线且无文字覆盖的连续段用 CadDraw.ChainDimension（实体数不变）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 7 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1228, 7199.959),
                CadDraw.P(-1100, 7177.959),
                CadDraw.P(-1100, 7968.362),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-300, 7177.959),
                CadDraw.P(300, 7498.514),
                CadDraw.P(300, 7968.362),
                0,
                dimStyle,
                b03,
                "6×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1228, 7199.959),
                CadDraw.P(-1300, 7199.959),
                CadDraw.P(-1300, 7968.362),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(300, 7177.959),
                CadDraw.P(1100, 7177.959),
                CadDraw.P(1100, 7968.362),
                0,
                dimStyle,
                b03,
                "8×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1100, 7177.959),
                CadDraw.P(-300, 7177.959),
                CadDraw.P(-300, 7968.362),
                0,
                dimStyle,
                b03,
                "8×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1100, -141.394),
                CadDraw.P(1100, -235.412),
                CadDraw.P(1100, -1906.752),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1300, 7255.258),
                CadDraw.P(1300, 7255.258),
                CadDraw.P(1300, 8210.516),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 4 段
            CadDraw.RotatedDimension(
                CadDraw.P(1300, 7098.515),
                CadDraw.P(250, 2598.515),
                CadDraw.P(1750.661, 2598.515),
                Math.PI / 2,
                dimStyle,
                b03,
                "4500(圆弧段)"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1566.185, 7598.514),
                CadDraw.P(1566.185, 7098.515),
                CadDraw.P(1750.661, 7098.515),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-414.142, -1300),
                CadDraw.P(-729.262, 0),
                CadDraw.P(-1913.729, 0),
                Math.PI / 2,
                dimStyle,
                b03,
                "13×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-414.142, -1400),
                CadDraw.P(-414.142, -1300),
                CadDraw.P(-1913.729, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 2 段
            CadDraw.RotatedDimension(
                CadDraw.P(1228, 7291.313),
                CadDraw.P(1100, 7177.959),
                CadDraw.P(1100, 7968.362),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1228, 7291.313),
                CadDraw.P(1300, 7199.959),
                CadDraw.P(1300, 7968.362),
                Math.PI,
                dimStyle,
                b03
            ),
            // rotation = 4.71239（竖直反向）· 7 段
            CadDraw.RotatedDimension(
                CadDraw.P(-729.262, 0),
                CadDraw.P(-414.142, -1400),
                CadDraw.P(-2113.729, -1400),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "主筋伸入承台长度1400"
            ),
            // 源 DIMENSION 19A541/19A53B：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1713.729, 7598.514),
                    CadDraw.P(-1713.729, 7498.514),
                    CadDraw.P(-1713.729, 5348.514),
                ],
                dimensionLinePoint: CadDraw.P(-1913.729, 7498.514),
                rotation: 3 * Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1028, 1148.514),
                CadDraw.P(-1028, 1000),
                CadDraw.P(-1913.729, 900),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "a"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1713.729, 7598.514),
                CadDraw.P(-1718.544, 0),
                CadDraw.P(-2113.729, 0),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "墩柱高度H"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1028, 1000),
                CadDraw.P(-1829.262, 0),
                CadDraw.P(-1913.729, 0),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "nx100(箍筋加密区)"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1713.729, 5348.514),
                CadDraw.P(-1028, 1148.514),
                CadDraw.P(-1913.729, 1050),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "mx150"
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 11 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b03;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 2：源图窗口 [238, 258, 440, 536]，239 个实体，9.30×12.80 m。</summary>
    public static void DrawRegion02(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以 ID 指定点 (-101770.4735, 16017.6969) 为分区原点：
        // 图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-101770.4735, 16017.6969);

        List<Entity> entities =
        [
            .. TitleBlock.Add(doc, "正面钢筋布置图", "1:50", CadDraw.P(-83.17, 8486.502), 50),
            CadDraw.Polyline(
                [CadDraw.V(1328, 2498.515), CadDraw.V(-1328, 2498.515)],
                b01,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(980, 7498.515),
                    CadDraw.V(980, 7098.515, bulge: 0.064179),
                    CadDraw.V(400, 2598.515),
                    CadDraw.V(400, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(500, -1400),
                    CadDraw.V(500, 2598.515, bulge: -0.071302),
                    CadDraw.V(1145, 7098.515),
                    CadDraw.V(1145, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1310, 7498.515),
                    CadDraw.V(1310, 7098.515, bulge: 0.078404),
                    CadDraw.V(600, 2598.515),
                    CadDraw.V(600, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1475, 7498.515),
                    CadDraw.V(1475, 7098.515, bulge: 0.085482),
                    CadDraw.V(700, 2598.515),
                    CadDraw.V(700, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1640, 7498.515),
                    CadDraw.V(1640, 7098.515, bulge: 0.092534),
                    CadDraw.V(800, 2598.515),
                    CadDraw.V(800, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1805, 7498.515),
                    CadDraw.V(1805, 7098.515, bulge: 0.099559),
                    CadDraw.V(900, 2598.515),
                    CadDraw.V(900, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1000, -1400),
                    CadDraw.V(1000, 2598.515, bulge: -0.106554),
                    CadDraw.V(1970, 7098.515),
                    CadDraw.V(1970, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(2135, 7498.515),
                    CadDraw.V(2135, 7098.515, bulge: 0.113518),
                    CadDraw.V(1100, 2598.515),
                    CadDraw.V(1100, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1200, -1400),
                    CadDraw.V(1200, 2598.515, bulge: -0.120449),
                    CadDraw.V(2300, 7098.515),
                    CadDraw.V(2300, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1290.51, -1400),
                    CadDraw.V(1290.51, 2598.515, bulge: -0.120449),
                    CadDraw.V(2390.51, 7098.515),
                    CadDraw.V(2390.51, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(2428, 7498.515),
                    CadDraw.V(2428, 7098.515, bulge: 0.120449),
                    CadDraw.V(1328, 2598.515),
                    CadDraw.V(1328, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(254, 7198.515, bulge: 0.022163),
                    CadDraw.V(50, 2598.515),
                    CadDraw.V(50, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(680, 7498.515, bulge: 0.048863),
                    CadDraw.V(199.992, 2598.515),
                    CadDraw.V(199.992, -1400),
                    CadDraw.V(199.992, 742.948),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-50, 542.948),
                    CadDraw.V(-50, -1400),
                    CadDraw.V(-50, 2598.515, bulge: 0.022163),
                    CadDraw.V(-254, 7198.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-680, 7498.515, bulge: -0.048863),
                    CadDraw.V(-199.992, 2598.515),
                    CadDraw.V(-199.992, -1400),
                    CadDraw.V(-199.992, 742.948),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1328, -1400),
                    CadDraw.V(-1328, 2598.515, bulge: 0.120449),
                    CadDraw.V(-2428, 7098.515),
                    CadDraw.V(-2428, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-2390.51, 7498.515),
                    CadDraw.V(-2390.51, 7098.515, bulge: -0.120449),
                    CadDraw.V(-1290.51, 2598.515),
                    CadDraw.V(-1290.51, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1200, -1400),
                    CadDraw.V(-1200, 2598.515, bulge: 0.083306),
                    CadDraw.V(-1734.125, 5782.066, bulge: 0.036774),
                    CadDraw.V(-2300, 7098.515),
                    CadDraw.V(-2300, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1970, 7498.515),
                    CadDraw.V(-1970, 7098.515, bulge: -0.106554),
                    CadDraw.V(-1000, 2598.515),
                    CadDraw.V(-1000, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1805, 7498.515),
                    CadDraw.V(-1805, 7098.515, bulge: -0.099559),
                    CadDraw.V(-900, 2598.515),
                    CadDraw.V(-900, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1640, 7498.515),
                    CadDraw.V(-1640, 7098.515, bulge: -0.092534),
                    CadDraw.V(-800, 2598.515),
                    CadDraw.V(-800, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1475, 7498.515),
                    CadDraw.V(-1475, 7098.515, bulge: -0.085482),
                    CadDraw.V(-700, 2598.515),
                    CadDraw.V(-700, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-600, -1400),
                    CadDraw.V(-600, 2598.515, bulge: 0.078404),
                    CadDraw.V(-1310, 7098.515),
                    CadDraw.V(-1310, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1145, 7498.515),
                    CadDraw.V(-1145, 7098.515, bulge: -0.071302),
                    CadDraw.V(-500, 2598.515),
                    CadDraw.V(-500, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-980, 7498.515),
                    CadDraw.V(-980, 7098.515, bulge: -0.064179),
                    CadDraw.V(-400, 2598.515),
                    CadDraw.V(-400, -1400),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1100, -1400),
                    CadDraw.V(-1100, 2598.515, bulge: 0.113518),
                    CadDraw.V(-2135, 7098.515),
                    CadDraw.V(-2135, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-300, 2598.515, bulge: 0.057036),
                    CadDraw.V(-815, 7098.515),
                    CadDraw.V(-815, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(300, 2598.515, bulge: -0.057036),
                    CadDraw.V(815, 7098.515),
                    CadDraw.V(815, 7498.515),
                ],
                b01
            ),
            CadDraw.Polyline(
                [CadDraw.V(480, 7337.101, bulge: 0.022905), CadDraw.V(150.459, 5048.515)],
                b01
            ),
            CadDraw.Polyline(
                [CadDraw.V(-150.459, 5048.515, bulge: 0.022905), CadDraw.V(-480, 7337.101)],
                b01
            ),
            CadDraw.Line(CadDraw.P(-1535.234, 4598.515), CadDraw.P(1535.234, 4598.515), b01),
            CadDraw.Line(CadDraw.P(-1567.891, 4748.515), CadDraw.P(1567.891, 4748.515), b01),
            CadDraw.Line(CadDraw.P(-1451.935, 4148.515), CadDraw.P(1451.935, 4148.515), b01),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1328, 1148.515), CadDraw.P(1328, 1148.515), b01),
                CadDraw.P(0, 150),
                9
            ),
            CadDraw.Line(CadDraw.P(-1603.033, 4898.515), CadDraw.P(1603.033, 4898.515), b01),
            CadDraw.Line(CadDraw.P(-1505.038, 4448.515), CadDraw.P(1505.038, 4448.515), b01),
            CadDraw.Line(CadDraw.P(-1477.278, 4298.515), CadDraw.P(1477.278, 4298.515), b01),
            CadDraw.Line(CadDraw.P(-1390.221, 3698.515), CadDraw.P(1390.221, 3698.515), b01),
            CadDraw.Line(CadDraw.P(-1680.888, 5198.515), CadDraw.P(1680.888, 5198.515), b01),
            CadDraw.Line(CadDraw.P(-1640.689, 5048.515), CadDraw.P(1640.689, 5048.515), b01),
            CadDraw.Line(CadDraw.P(-1428.989, 3998.515), CadDraw.P(1428.989, 3998.515), b01),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1328, -1300), CadDraw.P(1328, -1300), b01),
                CadDraw.P(0, 100),
                24
            ),
            CadDraw.Line(CadDraw.P(-1408.422, 3848.515), CadDraw.P(1408.422, 3848.515), b01),
            CadDraw.Line(CadDraw.P(0, 5048.515), CadDraw.P(0, 7198.515), b01),
            CadDraw.Line(CadDraw.P(-1374.371, 3548.515), CadDraw.P(1374.371, 3548.515), b01),
            CadDraw.Line(CadDraw.P(-1723.664, 5348.515), CadDraw.P(1723.664, 5348.515), b01),
            CadDraw.Line(CadDraw.P(-1360.861, 3398.515), CadDraw.P(1360.861, 3398.515), b01),
            CadDraw.Line(CadDraw.P(-1349.681, 3248.515), CadDraw.P(1349.681, 3248.515), b01),
            CadDraw.Line(CadDraw.P(-1340.823, 3098.515), CadDraw.P(1340.823, 3098.515), b01),
            CadDraw.Line(CadDraw.P(-1334.281, 2948.515), CadDraw.P(1334.281, 2948.515), b01),
            CadDraw.Line(CadDraw.P(-1330.051, 2798.515), CadDraw.P(1330.051, 2798.515), b01),
            CadDraw.Line(CadDraw.P(-1328.128, 2648.515), CadDraw.P(1328.128, 2648.515), b01),
            .. new[]
            {
                CadDraw.P(-800.001, 7598.514),
                CadDraw.P(-600.001, 7598.514),
                CadDraw.P(599.999, 7598.514),
                CadDraw.P(799.999, 7598.514),
            }.Select(p =>
                CadDraw.Polyline([CadDraw.V(p.X, p.Y), CadDraw.V(p.X, p.Y)], b04, closed: true)
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-600, 7598.515, bulge: -0.049876),
                    CadDraw.V(-100, 2598.515),
                    CadDraw.V(-100, -1400),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(800, 7598.515),
                    CadDraw.V(800, 7598.515, bulge: 0.049876),
                    CadDraw.V(300, 2598.515),
                    CadDraw.V(300, -1400),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-800, 7598.515),
                    CadDraw.V(-800, 7598.515, bulge: -0.049876),
                    CadDraw.V(-300, 2598.515),
                    CadDraw.V(-300, -1400),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(600, 7598.515, bulge: 0.049876),
                    CadDraw.V(100, 2598.515),
                    CadDraw.V(100, -1400),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-228.881, -2247.596),
                    CadDraw.V(-228.881, -2104.969),
                    CadDraw.V(228.881, -2411.313),
                    CadDraw.V(228.881, -2239.272),
                    CadDraw.V(1695.56, -2239.272),
                    CadDraw.V(228.881, -2239.272),
                    CadDraw.V(228.881, -2411.313),
                    CadDraw.V(-228.881, -2104.969),
                ],
                b04,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1695.56, -2409.633),
                    CadDraw.V(1695.56, -1156.705),
                    CadDraw.V(1814.053, -1156.705),
                    CadDraw.V(1577.067, -931.396),
                    CadDraw.V(1695.56, -931.396),
                    CadDraw.V(1695.56, 0),
                    CadDraw.V(1400, 0),
                    CadDraw.V(1400, 1050),
                    CadDraw.V(1400, 2598.515, bulge: -0.120449),
                    CadDraw.V(2500, 7098.515),
                    CadDraw.V(2500, 7598.515),
                    CadDraw.V(600, 7598.515),
                    CadDraw.V(600, 7598.515),
                    CadDraw.V(300, 7298.515),
                    CadDraw.V(-300, 7298.515),
                    CadDraw.V(-600, 7598.515),
                    CadDraw.V(-600, 7598.515),
                    CadDraw.V(-2500, 7598.515),
                    CadDraw.V(-2500, 7098.515, bulge: -0.120449),
                    CadDraw.V(-1400, 2598.515),
                    CadDraw.V(-1400, 1050),
                    CadDraw.V(-1400, 0),
                    CadDraw.V(-1811.258, 0),
                    CadDraw.V(-1811.258, -931.396),
                    CadDraw.V(-1692.765, -931.396),
                    CadDraw.V(-1929.751, -1156.705),
                    CadDraw.V(-1811.258, -1156.705),
                    CadDraw.V(-1811.258, -2409.633),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-716.479, -2352.708),
                    CadDraw.V(-716.479, -2593.608),
                    CadDraw.V(-879.05, -2593.608),
                    CadDraw.V(-829.919, -2605.876),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1571.717, 400),
                    CadDraw.V(1812.616, 400),
                    CadDraw.V(1812.616, 237.43),
                    CadDraw.V(1824.884, 286.56),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-2559.124, 7498.515),
                    CadDraw.V(-2800.024, 7498.515),
                    CadDraw.V(-2800.024, 7335.944),
                    CadDraw.V(-2812.292, 7385.075),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1812.616, 237.43),
                    CadDraw.V(-1812.616, 400),
                    CadDraw.V(-1571.717, 400),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1916.709, 5348.515),
                    CadDraw.V(-2157.608, 5348.515),
                    CadDraw.V(-2157.608, 5185.944),
                    CadDraw.V(-2169.876, 5235.075),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-716.479, 8212.618),
                    CadDraw.V(-716.479, 8453.518),
                    CadDraw.V(-879.05, 8453.518),
                    CadDraw.V(-829.919, 8465.786),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1916.709, 5348.515),
                    CadDraw.V(2157.608, 5348.515),
                    CadDraw.V(2157.608, 5185.944),
                    CadDraw.V(2169.876, 5235.075),
                ],
                b04
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(3001.664, 7498.515),
                    CadDraw.V(3242.564, 7498.515),
                    CadDraw.V(3242.564, 7335.944),
                    CadDraw.V(3254.832, 7385.075),
                ],
                b04
            ),
            CadDraw.Line(CadDraw.P(-1811.258, -2247.596), CadDraw.P(-228.881, -2247.596), b04),
            CadDraw.Polyline(
                [CadDraw.V(-820.846, 900), CadDraw.V(-860.846, 1000)],
                b03,
                closed: true
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(332, 1824.257), CadDraw.P(232, 1864.257), b03),
                CadDraw.P(124, 0),
                8
            ),
            .. new[]
            {
                CadDraw.P(-1428, 1864.257),
                CadDraw.P(-1300, 1864.257),
                CadDraw.P(-507.935, 6934.108),
                CadDraw.P(-300, 2175.994),
                CadDraw.P(-162, 2175.994),
                CadDraw.P(-108.49, 6662.454),
                CadDraw.P(0, 2175.994),
            }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 100, p.Y - 40), b03)),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-1076, 1824.257), CadDraw.P(-1176, 1864.257), b03),
                CadDraw.P(124, 0),
                7
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-860.846, -1200),
                    CadDraw.V(-820.846, -1300),
                    CadDraw.V(-820.846, 2498.515),
                    CadDraw.V(-860.846, 2598.515),
                ],
                b03
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-1019.123, 5448.515),
                    CadDraw.V(-979.123, 5348.515),
                    CadDraw.V(-979.123, 2648.515),
                    CadDraw.V(-1019.123, 2748.515),
                ],
                b03
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(1228, 1864.257),
                    CadDraw.V(1328, 1824.257),
                    CadDraw.V(-2042.295, 1824.257),
                ],
                b03
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(290.954, 6934.108),
                    CadDraw.V(390.954, 6894.108),
                    CadDraw.V(-3038.263, 6894.108),
                ],
                b03
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-2042.295, 2135.994),
                    CadDraw.V(200, 2135.994),
                    CadDraw.V(100, 2175.994),
                ],
                b03
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-979.123, 2798.515), CadDraw.P(-1019.123, 2898.515), b03),
                CadDraw.P(0, 150),
                17
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-820.846, 1148.515), CadDraw.P(-860.846, 1248.515), b03),
                CadDraw.P(0, 150),
                9
            ),
            .. CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(-820.846, -1200), CadDraw.P(-860.846, -1100), b03),
                CadDraw.P(0, 100),
                21
            ),
            CadDraw.Line(CadDraw.P(-2835.5, 6622.454), CadDraw.P(0, 6622.454), b03),
            CadDraw.Line(CadDraw.P(-2560.691, 3943.024), CadDraw.P(-979.123, 3943.024), b03),
            CadDraw.Line(CadDraw.P(-2609.143, 1106.115), CadDraw.P(-820.846, 1106.115), b03),
            CadDraw.Text(
                "Ⅱ",
                CadDraw.P(2264.333, 5182.121),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅲ",
                CadDraw.P(1919.341, 233.607),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅰ",
                CadDraw.P(3349.288, 7332.121),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅱ",
                CadDraw.P(-2264.333, 5182.121),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅰ",
                CadDraw.P(-2905.735, 7332.121),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅲ",
                CadDraw.P(-1918.327, 233.607),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅳ",
                CadDraw.P(-824.449, 8262.233),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "Ⅳ",
                CadDraw.P(-824.449, -2541.783),
                150,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.7
            ),
            HRB400Block.Insert(doc, CadDraw.P(-2540.98, 6688.411), "N1-3", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1747.775, 2201.951), "N1-2", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1747.775, 1890.214), "N1-1", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-2743.743, 6960.065), "N1-4", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1773.277, 975.667), "N5-1", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-2314.623, 975.667), "N4-1", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1773.277, 1201.222), "N3-1", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-2314.623, 1201.222), "N2-1", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1773.277, 3812.577), "N5-2", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-1773.277, 4033.273), "N3-2", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-2297.088, 3812.577), "N4-2", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-2297.088, 4033.273), "N2-2", "14", 5, 0),
        ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        // 首尾严格相接、同一条尺寸线且无文字覆盖的连续段用 CadDraw.ChainDimension（实体数不变）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 11 段
            // 源 DIMENSION 199C0C/199C0F：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [CadDraw.P(2300, 7498.515), CadDraw.P(2428, 7498.515), CadDraw.P(2500, 7598.515)],
                dimensionLinePoint: CadDraw.P(2428, 7923.66),
                rotation: 0,
                style: dimStyle,
                layer: b03
            ),
            // 源 DIMENSION 199C08/199C09：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2500, 7255.258),
                    CadDraw.P(-2428, 7155.258),
                    CadDraw.P(-2300, 7498.515),
                ],
                dimensionLinePoint: CadDraw.P(-2428, 7923.66),
                rotation: 0,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2300, 7498.515),
                CadDraw.P(-815, 7498.515),
                CadDraw.P(-815, 7923.66),
                0,
                dimStyle,
                b03,
                "9×165"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-815, 7498.515),
                CadDraw.P(-680.008, 7498.515),
                CadDraw.P(-680.008, 7923.66),
                0,
                dimStyle,
                b03
            ),
            // 源 DIMENSION 199C25/199C27：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [CadDraw.P(-680, 7498.515), CadDraw.P(-480, 7337.101), CadDraw.P(-254, 7198.515)],
                dimensionLinePoint: CadDraw.P(-480, 7923.66),
                rotation: 0,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-254, 7198.515),
                CadDraw.P(254, 7198.515),
                CadDraw.P(254, 7923.66),
                0,
                dimStyle,
                b03,
                "2×254"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1400, -141.394),
                CadDraw.P(1400, -235.412),
                CadDraw.P(1400, -1906.751),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2500, 7255.258),
                CadDraw.P(2500, 7255.258),
                CadDraw.P(2500, 8210.516),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 3 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1614.143, -1300),
                CadDraw.P(-1929.262, 0),
                CadDraw.P(-3113.73, 0),
                Math.PI / 2,
                dimStyle,
                b03,
                "13×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1614.143, -1400),
                CadDraw.P(-1614.143, -1300),
                CadDraw.P(-3113.73, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2766.185, 7598.515),
                CadDraw.P(2766.185, 7098.515),
                CadDraw.P(2950.661, 7098.515),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 4 段
            // 源 DIMENSION 199C28/199C29：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(680.008, 7551.296),
                    CadDraw.P(480.008, 7389.883),
                    CadDraw.P(254, 7198.515),
                ],
                dimensionLinePoint: CadDraw.P(480.008, 7923.66),
                rotation: Math.PI,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(815, 7498.515),
                CadDraw.P(680.008, 7498.515),
                CadDraw.P(680.008, 7923.66),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2300, 7498.515),
                CadDraw.P(815, 7498.515),
                CadDraw.P(815, 7923.66),
                Math.PI,
                dimStyle,
                b03,
                "9×165"
            ),
            // rotation = 4.71239（竖直反向）· 7 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1929.262, 0),
                CadDraw.P(-1614.143, -1400),
                CadDraw.P(-3313.73, -1400),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "主筋伸入承台长度1400"
            ),
            // 源 DIMENSION 19A636/19A630：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2913.73, 7598.515),
                    CadDraw.P(-2913.73, 7498.515),
                    CadDraw.P(-2913.73, 5348.515),
                ],
                dimensionLinePoint: CadDraw.P(-3113.73, 7498.515),
                rotation: 3 * Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1328, 1148.515),
                CadDraw.P(-1328, 1000),
                CadDraw.P(-3113.73, 900),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "a"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2913.73, 7598.515),
                CadDraw.P(-1718.545, 0),
                CadDraw.P(-3313.73, 0),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "墩柱高度H"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1328, 1000),
                CadDraw.P(-1829.262, 0),
                CadDraw.P(-3113.73, 0),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "nx100(箍筋加密区)"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2913.73, 5348.515),
                CadDraw.P(-1290.51, 1148.515),
                CadDraw.P(-3113.73, 1050),
                3 * Math.PI / 2,
                dimStyle,
                b03,
                "mx150"
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 12 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b03;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 3：源图窗口 [515, 117, 715, 247]，259 个实体，去重 3，9.20×6.00 m。</summary>
    public static void DrawRegion03(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以 ID 指定点 (-89327.2136, 28260.0505) 为分区原点：
        // 图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-89327.2136, 28260.0505);

        List<Entity> entities =

            // 源图有 1 个重复对象（同层同几何，仅句柄不同），只画一份
            // 另有 2 个对象与块替身重合（_Oblique 块 / 断面与直线同位置），只画一份
            // 本区域表格存在未闭合的合并单元格边界 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "Ⅰ-Ⅰ截面", "1:50", CadDraw.P(37.281, 2647.348), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2300, 1300),
                        CadDraw.V(780.03, 1300),
                        CadDraw.V(580.03, 1100),
                        CadDraw.V(100, 1100),
                        CadDraw.V(-100, 1100),
                        CadDraw.V(-580.031, 1100),
                        CadDraw.V(-780.031, 1300),
                        CadDraw.V(-2300, 1300, bulge: 0.414214),
                        CadDraw.V(-2500, 1100),
                        CadDraw.V(-2500, -1100, bulge: 0.414214),
                        CadDraw.V(-2300, -1300),
                        CadDraw.V(-780.031, -1300),
                        CadDraw.V(-580.031, -1100),
                        CadDraw.V(-100, -1100),
                        CadDraw.V(100, -1100),
                        CadDraw.V(580.03, -1100),
                        CadDraw.V(780.03, -1300),
                        CadDraw.V(2300, -1300, bulge: 0.414214),
                        CadDraw.V(2500, -1100),
                        CadDraw.V(2500, 1100, bulge: 0.414214),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(0, -1001.639),
                        CadDraw.V(95.208, -767.597),
                        CadDraw.V(679.649, -767.597),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(0, 1001.639),
                        CadDraw.V(95.208, 767.597),
                        CadDraw.V(679.649, 767.597),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2300, -1360),
                        CadDraw.V(2300, -1360, bulge: 0.414214),
                        CadDraw.V(2560, -1100),
                        CadDraw.V(2560, -300),
                        CadDraw.V(2428, -300),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2300, 1360),
                        CadDraw.V(2300, 1360, bulge: -0.414214),
                        CadDraw.V(2560, 1100),
                        CadDraw.V(2560, 500),
                        CadDraw.V(2560, 300),
                        CadDraw.V(2428, 300),
                    ],
                    b03
                ),
                CadDraw.Line(CadDraw.P(2390.509, -1190.51), CadDraw.P(2483.848, -1283.848), b03),
                CadDraw.Line(CadDraw.P(2390.509, 1190.51), CadDraw.P(2483.848, 1283.848), b03),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2300, -1360),
                        CadDraw.V(-2300, -1360, bulge: -0.414214),
                        CadDraw.V(-2560, -1100),
                        CadDraw.V(-2560, -300),
                        CadDraw.V(-2428, -300),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2300, 1360),
                        CadDraw.V(-2300, 1360, bulge: 0.414214),
                        CadDraw.V(-2560, 1100),
                        CadDraw.V(-2560, 500),
                        CadDraw.V(-2560, 300),
                        CadDraw.V(-2428, 300),
                    ],
                    b03
                ),
                CadDraw.Line(CadDraw.P(-2390.51, -1190.51), CadDraw.P(-2483.848, -1283.848), b03),
                CadDraw.Line(CadDraw.P(-2390.51, 1190.51), CadDraw.P(-2483.848, 1283.848), b03),
                CadDraw.Polyline(
                    [
                        CadDraw.V(480.008, -1001.639),
                        CadDraw.V(480.008, -1133.639),
                        CadDraw.V(-480.008, -1133.639),
                        CadDraw.V(-480.008, -1001.639),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-680.008, -1098.154),
                        CadDraw.V(-680.008, -1280.824),
                        CadDraw.V(-253.927, -1280.824),
                        CadDraw.V(-253.927, -1001.639),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-253.927, 1001.639),
                        CadDraw.V(-253.927, 1280.824),
                        CadDraw.V(-680.008, 1280.824),
                        CadDraw.V(-680.008, 1098.154),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1718.236, 112.669),
                        CadDraw.V(-2315.111, 112.669),
                        CadDraw.V(-2428, 0),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(680.008, -1280.824),
                        CadDraw.V(680.008, -1098.154),
                        CadDraw.V(680.008, -1280.824),
                        CadDraw.V(253.927, -1280.824),
                        CadDraw.V(253.927, -1001.639),
                        CadDraw.V(253.927, -1184.309),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-815, 1228), CadDraw.V(-815, 1360), CadDraw.V(-2300, 1360)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(815, -1228), CadDraw.V(815, -1360), CadDraw.V(2300, -1360)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-815, -1228), CadDraw.V(-815, -1360), CadDraw.V(-2300, -1360)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(815, 1228), CadDraw.V(815, 1360), CadDraw.V(2300, 1360)],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1718.236, 112.669),
                        CadDraw.V(2315.111, 112.669),
                        CadDraw.V(2428, 0),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-480.008, 1001.639),
                        CadDraw.V(-480.008, 1133.639),
                        CadDraw.V(480.008, 1133.639),
                        CadDraw.V(480.008, 1001.639),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(680.008, 1098.154),
                        CadDraw.V(680.008, 1280.824),
                        CadDraw.V(253.927, 1280.824),
                        CadDraw.V(253.927, 1001.639),
                    ],
                    b03
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(980, -1360), CadDraw.P(980, -1228), b03),
                    CadDraw.P(165, 0),
                    9
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(980, 1228), CadDraw.P(980, 1360), b03),
                    CadDraw.P(165, 0),
                    9
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2428, -200),
                        CadDraw.V(2560, -200),
                        CadDraw.V(2560, 200),
                        CadDraw.V(2428, 200),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2428, 200),
                        CadDraw.V(-2560, 200),
                        CadDraw.V(-2560, -200),
                        CadDraw.V(-2428, -200),
                    ],
                    b03
                ),
                .. new[]
                {
                    CadDraw.P(-2560, -100),
                    CadDraw.P(-2560, 100),
                    CadDraw.P(2428, 100),
                    CadDraw.P(2428, -100),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 132, p.Y), b03)),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2560, -1100), CadDraw.P(-2428, -1100), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(2428, -1100), CadDraw.P(2560, -1100), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2560, 400), CadDraw.P(-2428, 400), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(2428, 400), CadDraw.P(2560, 400), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2300, -1360), CadDraw.P(-2300, -1228), b03),
                    CadDraw.P(165, 0),
                    9
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-2300, 1228), CadDraw.P(-2300, 1360), b03),
                    CadDraw.P(165, 0),
                    9
                ),
                CadDraw.Line(CadDraw.P(0, -1150), CadDraw.P(0, 1150), b06),
                CadDraw.Line(CadDraw.P(-2550, 0), CadDraw.P(2550, 0), b06),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(815, -1228),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(165, 0),
                    10
                ),
                .. new[]
                {
                    CadDraw.P(-480, 1001.638),
                    CadDraw.P(-254, 1001.638),
                    CadDraw.P(0, 1001.638),
                    CadDraw.P(253.927, 1001.638),
                    CadDraw.P(480.008, 1001.638),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-680.008, 1098.154),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(680.008, 1098.154),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(815, 1228),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(165, 0),
                    10
                ),
                .. new[]
                {
                    CadDraw.P(-480, -1001.639),
                    CadDraw.P(-254, -1001.639),
                    CadDraw.P(0, -1001.639),
                    CadDraw.P(253.927, -1001.639),
                    CadDraw.P(480.008, -1001.639),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-680.008, -1098.154),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(680.008, -1098.154),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2428, 400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2257.942, 400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2428, 700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2257.942, 700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2428, 1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2257.942, 1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2428, -400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2257.942, -400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2428, -700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2257.942, -700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2428, -1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2257.942, -1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(2428, -200),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(0, 100),
                    6
                ),
                .. new[]
                {
                    CadDraw.P(2428, -1100),
                    CadDraw.P(2428, -900),
                    CadDraw.P(2428, -800),
                    CadDraw.P(2428, -600),
                    CadDraw.P(2428, -500),
                    CadDraw.P(2428, -300),
                    CadDraw.P(2428, 500),
                    CadDraw.P(2428, 600),
                    CadDraw.P(2428, 800),
                    CadDraw.P(2428, 900),
                    CadDraw.P(2428, 1100),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2390.509, -1190.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(2390.509, 1190.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2428, 400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2257.942, 400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2428, 700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2257.942, 700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2428, 1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2257.942, 1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2428, -400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2257.942, -400),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2428, -700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2257.942, -700),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2428, -1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2257.942, -1000),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2428, -200),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(0, 100),
                    6
                ),
                .. new[]
                {
                    CadDraw.P(-2428, -1100),
                    CadDraw.P(-2428, -900),
                    CadDraw.P(-2428, -800),
                    CadDraw.P(-2428, -600),
                    CadDraw.P(-2428, -500),
                    CadDraw.P(-2428, -300),
                    CadDraw.P(-2428, 500),
                    CadDraw.P(-2428, 600),
                    CadDraw.P(-2428, 800),
                    CadDraw.P(-2428, 900),
                    CadDraw.P(-2428, 1100),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2300, -1228),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(165, 0),
                    10
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-2300, 1228),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(165, 0),
                    10
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2390.51, -1190.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-2390.51, 1190.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                HRB400Block.Insert(doc, CadDraw.P(-1883.556, 1488.003), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1836.403, 1488.076), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2701.745, 767.427), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-1991.981, 183.401), "N1-5", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2693.673, 767.427), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(2693.673, -641.4), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(52.896, 1220.185), "N1-4", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-381.281, 1370.789), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(552.654, 1370.789), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-430.232, -1390.4), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(525.119, -1390.4), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2694.903, 45.011), "N1-6", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(417.483, -679.654), "N1-3", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2701.745, -641.4), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(47.987, -1255.484), "N1-4", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2697.356, 45.011), "N1-6", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(2069.947, 183.401), "N1-5", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2076.924, -1550.894), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-2004.363, -1550.967), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(417.483, 848.931), "N1-3", "28", 5, 0),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        // 首尾严格相接、同一条尺寸线且无文字覆盖的连续段用 CadDraw.ChainDimension（实体数不变）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 10 段
            CadDraw.RotatedDimension(
                CadDraw.P(-2428, 1226.742),
                CadDraw.P(-2500, 1226.742),
                CadDraw.P(-2500, 1837.679),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2428, 1226.742),
                CadDraw.P(-2300, 1354.742),
                CadDraw.P(-2300, 1837.679),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2300, 1354.742),
                CadDraw.P(-815, 1228),
                CadDraw.P(-815, 1837.679),
                0,
                dimStyle,
                b03,
                "9×165"
            ),
            // 源 DIMENSION 1997F1/1997F2：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-680.008, 1224.896),
                    CadDraw.P(-480.008, 1128.381),
                    CadDraw.P(-253.927, 1128.381),
                ],
                dimensionLinePoint: CadDraw.P(-480.008, 1837.679),
                rotation: 0,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-253.927, 1128.381),
                CadDraw.P(253.927, 1128.381),
                CadDraw.P(253.927, 1837.679),
                0,
                dimStyle,
                b03,
                "2×254"
            ),
            // 源 DIMENSION 1997AC/1997AD：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [CadDraw.P(2300, 1228), CadDraw.P(2428, 1100), CadDraw.P(2500, 1100)],
                dimensionLinePoint: CadDraw.P(2428, 1837.679),
                rotation: 0,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2500, 0),
                CadDraw.P(2500, 0),
                CadDraw.P(2500, 2081.638),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-815, 1228),
                CadDraw.P(-680.008, 1098.154),
                CadDraw.P(-680.008, 1837.679),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 8 段
            CadDraw.RotatedDimension(
                CadDraw.P(-2534.967, 1228),
                CadDraw.P(-2534.967, 1300),
                CadDraw.P(-3025.482, 1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // 源 DIMENSION 19977C/19977D/1997CC/1997BE：4 段首尾相接，合并为尺寸链（仍是 4 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2534.967, 1228),
                    CadDraw.P(-2396.861, 1100),
                    CadDraw.P(-2428, 300),
                    CadDraw.P(-2428, -300),
                    CadDraw.P(-2428, -1100),
                ],
                dimensionLinePoint: CadDraw.P(-3025.482, 1100),
                rotation: Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            // 源 DIMENSION 19977E/19977F：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2396.861, -1100),
                    CadDraw.P(-2268.861, -1228),
                    CadDraw.P(-2268.861, -1300),
                ],
                dimensionLinePoint: CadDraw.P(-3025.482, -1228),
                rotation: Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1965.015, 1300),
                CadDraw.P(-1965.015, -1300),
                CadDraw.P(-3266.653, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 4 段
            CadDraw.RotatedDimension(
                CadDraw.P(2300, 1354.742),
                CadDraw.P(815, 1228),
                CadDraw.P(815, 1837.679),
                Math.PI,
                dimStyle,
                b03,
                "9×165"
            ),
            // 源 DIMENSION 1997F4/1997F5：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(680.008, 1224.896),
                    CadDraw.P(480.008, 1128.381),
                    CadDraw.P(253.927, 1128.381),
                ],
                dimensionLinePoint: CadDraw.P(480.008, 1837.679),
                rotation: Math.PI,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(815, 1228),
                CadDraw.P(680.008, 1098.154),
                CadDraw.P(680.008, 1837.679),
                Math.PI,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 20 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b03;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 4：源图窗口 [537, 308, 715, 442]，294 个实体，8.20×6.20 m。</summary>
    public static void DrawRegion04(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以 ID 指定点 (-89207.7614, 19453.3827) 为分区原点：
        // 图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-89207.7614, 19453.3827);

        List<Entity> entities =

            // 找到了 21×27 的闭合网格，但格内文字只填了 0/48，不像表格 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "Ⅱ-Ⅱ截面", "1:50", CadDraw.P(37.281, 2434.447), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(250.199, -906.775),
                        CadDraw.V(250.199, 906.775),
                        CadDraw.V(470.123, 1126.699),
                        CadDraw.V(800, 1126.699),
                        CadDraw.V(1595.764, 1126.699, bulge: -0.414214),
                        CadDraw.V(1747.664, 974.799),
                        CadDraw.V(1747.664, -974.799, bulge: -0.414214),
                        CadDraw.V(1595.764, -1126.699),
                        CadDraw.V(780.031, -1126.699),
                        CadDraw.V(470.123, -1126.699),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-250.199, -906.775),
                        CadDraw.V(-250.199, 906.775),
                        CadDraw.V(-470.123, 1126.699),
                        CadDraw.V(-800, 1126.699),
                        CadDraw.V(-1595.764, 1126.699, bulge: 0.414214),
                        CadDraw.V(-1747.664, 974.799),
                        CadDraw.V(-1747.664, -974.799, bulge: 0.414214),
                        CadDraw.V(-1595.764, -1126.699),
                        CadDraw.V(-780.031, -1126.699),
                        CadDraw.V(-470.123, -1126.699),
                    ],
                    b01,
                    closed: true
                ),
                .. new[]
                {
                    CadDraw.P(-1747.564, -622.75),
                    CadDraw.P(-1747.564, -322.75),
                    CadDraw.P(-1747.564, -22.75),
                    CadDraw.P(-1747.564, 322.75),
                    CadDraw.P(-1747.564, 622.75),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 3495.128, p.Y), b01)),
                .. new[]
                {
                    CadDraw.P(-1494.664, -1126.499),
                    CadDraw.P(-1043.974, -1126.499),
                    CadDraw.P(-524.199, -1126.499),
                    CadDraw.P(524.199, -1126.499),
                    CadDraw.P(997.848, -1126.499),
                    CadDraw.P(1494.664, -1126.499),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 2252.998), b01)),
                CadDraw.Line(CadDraw.P(-1747.664, 920.776), CadDraw.P(1747.664, 920.776), b01),
                CadDraw.Line(CadDraw.P(-1747.664, -920.776), CadDraw.P(1747.664, -920.776), b01),
                CadDraw.Polyline(
                    [
                        CadDraw.V(450.199, -1174.799),
                        CadDraw.V(250.199, -974.799),
                        CadDraw.V(100, -974.799),
                        CadDraw.V(-100, -974.799),
                        CadDraw.V(-250.199, -974.799),
                        CadDraw.V(-450.199, -1174.799),
                        CadDraw.V(-780.031, -1174.799),
                        CadDraw.V(-1595.764, -1174.799, bulge: -0.414214),
                        CadDraw.V(-1795.764, -974.799),
                        CadDraw.V(-1795.764, 974.799, bulge: -0.414214),
                        CadDraw.V(-1595.764, 1174.799),
                        CadDraw.V(-780.031, 1174.799),
                        CadDraw.V(-450.199, 1174.799),
                        CadDraw.V(-250.199, 974.799),
                        CadDraw.V(-100, 974.799),
                        CadDraw.V(100, 974.799),
                        CadDraw.V(250.199, 974.799),
                        CadDraw.V(450.199, 1174.799),
                        CadDraw.V(780.031, 1174.799),
                        CadDraw.V(1595.764, 1174.799, bulge: -0.414214),
                        CadDraw.V(1795.764, 974.799),
                        CadDraw.V(1795.764, -974.799, bulge: -0.414214),
                        CadDraw.V(1595.764, -1174.799),
                        CadDraw.V(780.031, -1174.799),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(-250.199, -974.599), CadDraw.P(250.199, -974.599), b04),
                CadDraw.Line(CadDraw.P(-250.199, 974.599), CadDraw.P(250.199, 974.599), b04),
                CadDraw.Polyline(
                    [CadDraw.V(1595.664, -1102.599), CadDraw.V(1595.664, -1234.599)],
                    b03,
                    closed: true
                ),
                .. new[]
                {
                    CadDraw.P(-791.133, -920.576),
                    CadDraw.P(904.612, -622.75),
                    CadDraw.P(904.612, -322.75),
                    CadDraw.P(904.612, -22.75),
                    CadDraw.P(904.612, 322.75),
                    CadDraw.P(904.612, 622.75),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 40, p.Y - 100), b03)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(0, -902.599),
                        CadDraw.V(95.208, -727.793),
                        CadDraw.V(679.649, -727.793),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(0, 902.599),
                        CadDraw.V(95.208, 668.557),
                        CadDraw.V(679.649, 668.557),
                    ],
                    b03
                ),
                CadDraw.Line(CadDraw.P(1686.174, -1065.109), CadDraw.P(1779.512, -1158.447), b03),
                CadDraw.Line(CadDraw.P(-1686.174, -1065.109), CadDraw.P(-1779.512, -1158.447), b03),
                CadDraw.Line(CadDraw.P(-1686.174, 1065.109), CadDraw.P(-1779.512, 1158.447), b03),
                .. new[]
                {
                    CadDraw.P(-1043.974, -341.532),
                    CadDraw.P(-524.199, -341.532),
                    CadDraw.P(-250.199, 523.631),
                    CadDraw.P(250.199, 523.631),
                    CadDraw.P(524.199, -341.532),
                    CadDraw.P(997.848, -341.532),
                    CadDraw.P(1494.664, -341.532),
                    CadDraw.P(1747.564, 523.631),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 100, p.Y - 40), b03)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-350.199, -979.64),
                        CadDraw.V(-350.199, -1234.599),
                        CadDraw.V(-123, -1234.599),
                        CadDraw.V(-123, -902.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(490.533, -1234.599),
                        CadDraw.V(1595.664, -1234.599, bulge: 0.414214),
                        CadDraw.V(1855.664, -974.599),
                        CadDraw.V(1855.664, -174.599),
                        CadDraw.V(1723.664, -174.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-490.533, 1234.599),
                        CadDraw.V(-1595.664, 1234.599, bulge: 0.414214),
                        CadDraw.V(-1855.664, 974.599),
                        CadDraw.V(-1855.664, 174.599),
                        CadDraw.V(-1723.664, 174.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1723.664, 0),
                        CadDraw.V(-1629.297, 115.026),
                        CadDraw.V(-1032.422, 115.026),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1647.564, 483.631),
                        CadDraw.V(-1747.564, 523.631),
                        CadDraw.V(2600.105, 523.631),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1723.664, 174.599),
                        CadDraw.V(1855.664, 174.599),
                        CadDraw.V(1855.664, 974.599, bulge: 0.414214),
                        CadDraw.V(1595.664, 1234.599),
                        CadDraw.V(490.533, 1234.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1629.297, 115.026),
                        CadDraw.V(1723.664, 0),
                        CadDraw.V(1629.297, 115.026),
                        CadDraw.V(1032.422, 115.026),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-490.533, -1234.599),
                        CadDraw.V(-1595.664, -1234.599, bulge: -0.414214),
                        CadDraw.V(-1855.664, -974.599),
                        CadDraw.V(-1855.664, -174.599),
                        CadDraw.V(-1723.664, -174.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-791.133, -1812.601),
                        CadDraw.V(-791.133, 920.576),
                        CadDraw.V(-751.133, 820.576),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-123, 902.599),
                        CadDraw.V(-123, 1234.599),
                        CadDraw.V(-350.199, 1234.599),
                        CadDraw.V(-350.199, 979.64),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(350.199, 979.64),
                        CadDraw.V(350.199, 1234.599),
                        CadDraw.V(123, 1234.599),
                        CadDraw.V(123, 902.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(350.199, -979.64),
                        CadDraw.V(350.199, -1234.599),
                        CadDraw.V(123, -1234.599),
                        CadDraw.V(123, -902.599),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1394.664, -381.532),
                        CadDraw.V(-1494.664, -341.532),
                        CadDraw.V(2551.592, -341.532),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1723.664, -100),
                        CadDraw.V(-1855.664, -100),
                        CadDraw.V(-1855.664, 100),
                        CadDraw.V(-1723.664, 100),
                    ],
                    b03
                ),
                CadDraw.Line(CadDraw.P(1686.174, 1065.109), CadDraw.P(1779.512, 1158.447), b03),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1723.664, -100),
                        CadDraw.V(1855.664, -100),
                        CadDraw.V(1855.664, 100),
                        CadDraw.V(1723.664, 100),
                    ],
                    b03
                ),
                CadDraw.Line(CadDraw.P(904.612, -1833.375), CadDraw.P(904.612, 632.367), b03),
                .. CadDraw.EntityArray(
                    CadDraw.Line(
                        CadDraw.P(-1855.664, -974.599),
                        CadDraw.P(-1723.664, -974.599),
                        b03
                    ),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1723.664, -974.599), CadDraw.P(1855.664, -974.599), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-182, 902.599),
                        CadDraw.V(-182, 1034.599),
                        CadDraw.V(-181.532, 1034.599),
                        CadDraw.V(181.532, 1034.599),
                        CadDraw.V(182, 1034.599),
                        CadDraw.V(182, 902.599),
                    ],
                    b03
                ),
                .. new[]
                {
                    CadDraw.P(-1595.664, 1102.599),
                    CadDraw.P(-1473.894, -1234.599),
                    CadDraw.P(-1473.894, 1102.599),
                    CadDraw.P(-1351.835, -1234.599),
                    CadDraw.P(-1351.835, 1102.599),
                    CadDraw.P(-1229.504, -1234.599),
                    CadDraw.P(-1229.504, 1102.599),
                    CadDraw.P(-1106.917, -1234.599),
                    CadDraw.P(-1106.917, 1102.599),
                    CadDraw.P(-984.091, -1234.599),
                    CadDraw.P(-984.091, 1102.599),
                    CadDraw.P(-861.045, -1234.599),
                    CadDraw.P(-861.045, 1102.599),
                    CadDraw.P(-737.795, -1234.599),
                    CadDraw.P(-737.795, 1102.599),
                    CadDraw.P(-614.36, -1234.599),
                    CadDraw.P(-614.36, 1102.599),
                    CadDraw.P(-490.533, -1232.991),
                    CadDraw.P(-490.533, 1100.991),
                    CadDraw.P(490.533, -1232.991),
                    CadDraw.P(490.533, 1100.991),
                    CadDraw.P(614.36, -1234.599),
                    CadDraw.P(614.36, 1102.599),
                    CadDraw.P(737.795, -1234.599),
                    CadDraw.P(737.795, 1102.599),
                    CadDraw.P(861.045, -1234.599),
                    CadDraw.P(861.045, 1102.599),
                    CadDraw.P(984.091, -1234.599),
                    CadDraw.P(984.091, 1102.599),
                    CadDraw.P(1106.917, -1234.599),
                    CadDraw.P(1106.917, 1102.599),
                    CadDraw.P(1229.504, -1234.599),
                    CadDraw.P(1229.504, 1102.599),
                    CadDraw.P(1351.835, -1234.599),
                    CadDraw.P(1351.835, 1102.599),
                    CadDraw.P(1473.894, -1234.599),
                    CadDraw.P(1473.894, 1102.599),
                    CadDraw.P(1595.664, 1102.599),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 132), b03)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(182, -902.599),
                        CadDraw.V(182, -1034.599),
                        CadDraw.V(181.532, -1034.599),
                        CadDraw.V(-181.532, -1034.599),
                        CadDraw.V(-182, -1034.599),
                        CadDraw.V(-182, -902.599),
                    ],
                    b03
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1855.664, 274.599), CadDraw.P(-1723.664, 274.599), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1723.664, 274.599), CadDraw.P(1855.664, 274.599), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                CadDraw.Line(CadDraw.P(0, -1024.799), CadDraw.P(0, 1024.799), b06),
                CadDraw.Line(CadDraw.P(-1845.764, 0), CadDraw.P(1845.764, 0), b06),
                CadDraw.Circle(CadDraw.P(861.045, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(984.092, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(737.795, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(614.36, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(490.533, -1100.991), 17, b01),
                CadDraw.Circle(CadDraw.P(1106.917, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(1229.504, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(1351.835, -1102.599), 17, b01),
                CadDraw.Circle(CadDraw.P(1473.894, -1102.599), 17, b01),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-490.533, -1100.991),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. new[]
                {
                    CadDraw.P(-1473.894, -1102.599),
                    CadDraw.P(-1351.835, -1102.599),
                    CadDraw.P(-1229.504, -1102.599),
                    CadDraw.P(-1106.917, -1102.599),
                    CadDraw.P(-984.092, -1102.599),
                    CadDraw.P(-861.046, -1102.599),
                    CadDraw.P(-737.796, -1102.599),
                    CadDraw.P(-614.36, -1102.599),
                    CadDraw.P(1595.664, -1102.599),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                .. new[]
                {
                    CadDraw.P(-1723.665, -974.599),
                    CadDraw.P(-1723.665, -774.599),
                    CadDraw.P(-1723.665, -674.599),
                    CadDraw.P(-1723.665, -474.599),
                    CadDraw.P(-1723.665, -374.599),
                    CadDraw.P(-1723.665, -174.599),
                    CadDraw.P(-1723.665, 174.599),
                    CadDraw.P(-1723.665, 374.599),
                    CadDraw.P(-1723.665, 474.599),
                    CadDraw.P(-1723.665, 674.599),
                    CadDraw.P(-1723.665, 774.599),
                    CadDraw.P(-1723.665, 974.599),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1686.174, -1065.109),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. new[]
                {
                    CadDraw.P(1723.664, -974.599),
                    CadDraw.P(1723.664, -774.599),
                    CadDraw.P(1723.664, -674.599),
                    CadDraw.P(1723.664, -474.599),
                    CadDraw.P(1723.664, -374.599),
                    CadDraw.P(1723.664, -174.599),
                    CadDraw.P(1723.664, 174.599),
                    CadDraw.P(1723.664, 374.599),
                    CadDraw.P(1723.664, 474.599),
                    CadDraw.P(1723.664, 674.599),
                    CadDraw.P(1723.664, 774.599),
                    CadDraw.P(1723.664, 974.599),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                .. new[]
                {
                    CadDraw.P(-1595.665, 1102.599),
                    CadDraw.P(-1473.894, 1102.599),
                    CadDraw.P(-1351.835, 1102.599),
                    CadDraw.P(-1229.504, 1102.599),
                    CadDraw.P(-1106.917, 1102.599),
                    CadDraw.P(-984.092, 1102.599),
                    CadDraw.P(-861.046, 1102.599),
                    CadDraw.P(-737.796, 1102.599),
                    CadDraw.P(-614.36, 1102.599),
                    CadDraw.P(614.36, 1102.599),
                    CadDraw.P(737.795, 1102.599),
                    CadDraw.P(861.045, 1102.599),
                    CadDraw.P(984.091, 1102.599),
                    CadDraw.P(1106.916, 1102.599),
                    CadDraw.P(1229.503, 1102.599),
                    CadDraw.P(1351.835, 1102.599),
                    CadDraw.P(1473.894, 1102.599),
                    CadDraw.P(1595.664, 1102.599),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1686.174, 1065.109),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1723.664, -100),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(0, 100),
                    3
                ),
                .. new[]
                {
                    CadDraw.P(-182, 902.599),
                    CadDraw.P(-123, 902.599),
                    CadDraw.P(0, 902.599),
                    CadDraw.P(123, 902.599),
                    CadDraw.P(182, 902.599),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(350.199, 974.463),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. new[]
                {
                    CadDraw.P(-182, -902.599),
                    CadDraw.P(-123, -902.599),
                    CadDraw.P(0, -902.599),
                    CadDraw.P(123, -902.599),
                    CadDraw.P(182, -902.599),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-350.199, -974.463),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(350.199, -974.463),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1686.174, 1065.109),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1723.664, 274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1553.606, 274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1723.664, 574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1553.606, 574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1723.664, 874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1553.606, 874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1723.664, -274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1553.606, -274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1723.664, -574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1553.606, -574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1723.664, -874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1553.606, -874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1723.664, 274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1553.606, 274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1723.664, 574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1553.606, 574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1723.664, 874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1553.606, 874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1723.664, -274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1553.606, -274.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1723.664, -574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1553.606, -574.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1723.664, -874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1553.606, -874.599),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(1723.664, -100),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(0, 100),
                    3
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1686.174, -1065.109),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-350.199, 974.463),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(490.533, 1100.991),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-490.533, 1100.991),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                HRB400Block.Insert(doc, CadDraw.P(-1347.305, -1386.747), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(417.483, 749.891), "N1-3", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1306.166, 185.757), "N1-5", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1971.032, 61.629), "N1-6", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(1979.034, -620.512), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(1979.034, 703.857), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-1347.305, 1396.864), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(417.484, -643.883), "N1-3", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1979.582, 770.105), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(4.459, 1356.831), "N1-4", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(1487.134, 1372.219), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-886.063, -1458.678), "N3-2", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(809.682, -1479.452), "N4-2", "12", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(1366.411, 185.757), "N1-5", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1980.042, 61.629), "N1-6", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(2379.18, -257.286), "N5-2", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(2427.692, 607.877), "N2-2", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1487.134, -1345.053), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(410.175, -1356.332), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-276.882, -1356.332), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(13.039, -1257.18), "N1-4", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-1979.582, -617.878), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(410.175, 1342.289), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-276.882, 1342.289), "N1-2", "28", 5, 0),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        // 首尾严格相接、同一条尺寸线且无文字覆盖的连续段用 CadDraw.ChainDimension（实体数不变）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 8 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1723.664, 1005.865),
                CadDraw.P(-1795.664, 1005.865),
                CadDraw.P(-1795.664, 1616.801),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-123, 902.599),
                CadDraw.P(-182, 902.599),
                CadDraw.P(-182, 1616.801),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-350.199, 1005.729),
                CadDraw.P(-181.532, 933.865),
                CadDraw.P(-181.532, 1616.801),
                0,
                dimStyle,
                b03,
                "168"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-490.533, 1102.599),
                CadDraw.P(-350.199, 974.463),
                CadDraw.P(-350.199, 1616.801),
                0,
                dimStyle,
                b03,
                "139"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1723.664, 1005.865),
                CadDraw.P(-1595.664, 1133.865),
                CadDraw.P(-1595.664, 1616.801),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1595.664, 1133.865),
                CadDraw.P(-490.533, 1102.599),
                CadDraw.P(-490.533, 1616.801),
                0,
                dimStyle,
                b03,
                "9×123"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1795.764, 0),
                CadDraw.P(1795.764, 0),
                CadDraw.P(1795.764, 1956.437),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-123, 902.599),
                CadDraw.P(123, 902.599),
                CadDraw.P(123, 1616.801),
                0,
                dimStyle,
                b03,
                "2×123"
            ),
            // rotation = 1.5708（竖直）· 7 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1733.429, 1102.599),
                CadDraw.P(-1733.429, 1174.599),
                CadDraw.P(-2223.944, 1174.599),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1595.323, -100),
                CadDraw.P(-1595.323, -174.599),
                CadDraw.P(-2223.944, -174.599),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // 源 DIMENSION 19991B/19991C/1999BF：3 段首尾相接，合并为尺寸链（仍是 3 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1733.429, 1102.599),
                    CadDraw.P(-1595.323, 974.599),
                    CadDraw.P(-1595.323, 174.599),
                    CadDraw.P(-1595.324, 100),
                ],
                dimensionLinePoint: CadDraw.P(-2223.944, 974.599),
                rotation: Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1260.78, 1174.799),
                CadDraw.P(-1260.78, -1174.799),
                CadDraw.P(-2562.417, -1174.799),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1595.324, 100),
                CadDraw.P(-1595.323, -100),
                CadDraw.P(-2223.944, -100),
                Math.PI / 2,
                dimStyle,
                b03,
                "2×100"
            ),
            // rotation = 3.14159（水平反向）· 6 段
            CadDraw.RotatedDimension(
                CadDraw.P(1723.664, 1005.865),
                CadDraw.P(1795.664, 1005.865),
                CadDraw.P(1795.664, 1616.801),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1723.664, 1005.865),
                CadDraw.P(1595.664, 1133.865),
                CadDraw.P(1595.664, 1616.801),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1595.664, 1133.865),
                CadDraw.P(490.533, 1100.991),
                CadDraw.P(490.533, 1616.801),
                Math.PI,
                dimStyle,
                b03,
                "9×123"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(350.199, 1005.729),
                CadDraw.P(181.532, 933.865),
                CadDraw.P(181.532, 1616.801),
                Math.PI,
                dimStyle,
                b03,
                "168"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(490.533, 1102.599),
                CadDraw.P(350.199, 974.463),
                CadDraw.P(350.199, 1616.801),
                Math.PI,
                dimStyle,
                b03,
                "139"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(123, 902.599),
                CadDraw.P(182, 902.599),
                CadDraw.P(182, 1616.801),
                Math.PI,
                dimStyle,
                b03
            ),
            // rotation = 4.71239（竖直反向）· 3 段
            // 源 DIMENSION 19991E/19991F：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1733.429, -1102.599),
                    CadDraw.P(-1595.323, -974.599),
                    CadDraw.P(-1595.323, -174.599),
                ],
                dimensionLinePoint: CadDraw.P(-2223.944, -974.599),
                rotation: 3 * Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1733.429, -1102.599),
                CadDraw.P(-1733.429, -1174.599),
                CadDraw.P(-2223.944, -1174.599),
                3 * Math.PI / 2,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 24 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b03;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 5：源图窗口 [537, 505, 715, 626]，263 个实体，去重 6，8.20×5.60 m。</summary>
    public static void DrawRegion05(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以 ID 指定点 (-89460.1599, 10558.2614) 为分区原点：
        // 图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-89460.1599, 10558.2614);

        List<Entity> entities =

            // 源图有 6 个重复对象（同层同几何，仅句柄不同），只画一份
            // 找到了 23×22 的闭合网格，但格内文字只填了 0/42，不像表格 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "Ⅲ-Ⅲ截面", "1:50", CadDraw.P(37.281, 2447.348), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(100, -831.976),
                        CadDraw.V(319.924, -1051.9),
                        CadDraw.V(1200, -1051.9, bulge: 0.414214),
                        CadDraw.V(1351.9, -900),
                        CadDraw.V(1351.9, 900, bulge: 0.414214),
                        CadDraw.V(1200, 1051.9),
                        CadDraw.V(319.924, 1051.9),
                        CadDraw.V(100, 831.976),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-100, -831.976),
                        CadDraw.V(-319.924, -1051.9),
                        CadDraw.V(-1200, -1051.9, bulge: -0.414214),
                        CadDraw.V(-1351.9, -900),
                        CadDraw.V(-1351.9, 900, bulge: -0.414214),
                        CadDraw.V(-1200, 1051.9),
                        CadDraw.V(-319.924, 1051.9),
                        CadDraw.V(-100, 831.976),
                    ],
                    b01,
                    closed: true
                ),
                .. new[]
                {
                    CadDraw.P(-1351.9, -845.977),
                    CadDraw.P(-1351.9, -622.75),
                    CadDraw.P(-1351.9, -322.75),
                    CadDraw.P(-1351.9, -22.75),
                    CadDraw.P(-1351.9, 322.75),
                    CadDraw.P(-1351.9, 622.75),
                    CadDraw.P(-1351.9, 845.977),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 2703.8, p.Y), b01)),
                .. new[]
                {
                    CadDraw.P(-1098, -1051.9),
                    CadDraw.P(-726, -1051.9),
                    CadDraw.P(-354, -1051.9),
                    CadDraw.P(354, -1051.9),
                    CadDraw.P(726, -1051.9),
                    CadDraw.P(1098, -1051.9),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 2103.8), b01)),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-780.031, 1100),
                        CadDraw.V(-1200, 1100, bulge: 0.414214),
                        CadDraw.V(-1400, 900),
                        CadDraw.V(-1400, -900, bulge: 0.414214),
                        CadDraw.V(-1200, -1100),
                        CadDraw.V(-780.031, -1100),
                        CadDraw.V(-450.199, -1100),
                        CadDraw.V(-300, -1100),
                        CadDraw.V(-100, -900),
                        CadDraw.V(100, -900),
                        CadDraw.V(300, -1100),
                        CadDraw.V(450.199, -1100),
                        CadDraw.V(780.03, -1100),
                        CadDraw.V(1200, -1100, bulge: 0.414214),
                        CadDraw.V(1400, -900),
                        CadDraw.V(1400, 900, bulge: 0.414214),
                        CadDraw.V(1200, 1100),
                        CadDraw.V(780.03, 1100),
                        CadDraw.V(450.199, 1100),
                        CadDraw.V(300, 1100),
                        CadDraw.V(100, 900),
                        CadDraw.V(-100, 900),
                        CadDraw.V(-300, 1100),
                        CadDraw.V(-450.199, 1100),
                    ],
                    b04,
                    closed: true
                ),
                .. new[]
                {
                    CadDraw.P(-519.523, -845.977),
                    CadDraw.P(608.209, -622.75),
                    CadDraw.P(608.209, -322.75),
                    CadDraw.P(608.209, -22.75),
                    CadDraw.P(608.209, 322.75),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 40, p.Y - 100), b03)),
                CadDraw.Polyline(
                    [CadDraw.V(659.111, 96.7), CadDraw.V(1255.987, 96.7), CadDraw.V(1328, 0)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-659.111, 96.7), CadDraw.V(-1255.987, 96.7), CadDraw.V(-1328, 0)],
                    b03
                ),
                .. new[]
                {
                    CadDraw.P(-726, -503.501),
                    CadDraw.P(-354, -503.501),
                    CadDraw.P(-100, 554.422),
                    CadDraw.P(100, 554.422),
                    CadDraw.P(354, -503.501),
                    CadDraw.P(726, -503.501),
                    CadDraw.P(1098, -503.501),
                    CadDraw.P(1251.9, 554.422),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X + 100, p.Y - 40), b03)),
                CadDraw.Line(CadDraw.P(1383.848, -1083.848), CadDraw.P(1290.51, -990.51), b03),
                CadDraw.Polyline(
                    [CadDraw.V(1200, -1160, bulge: 0.414214), CadDraw.V(1460, -900)],
                    b03
                ),
                CadDraw.Line(CadDraw.P(1383.848, 1083.848), CadDraw.P(1290.51, 990.51), b03),
                CadDraw.Polyline(
                    [CadDraw.V(1460, 900, bulge: 0.414214), CadDraw.V(1200, 1160)],
                    b03
                ),
                CadDraw.Line(CadDraw.P(-1383.848, -1083.848), CadDraw.P(-1290.51, -990.51), b03),
                CadDraw.Polyline(
                    [CadDraw.V(-1460, -900, bulge: 0.414214), CadDraw.V(-1200, -1160)],
                    b03
                ),
                CadDraw.Line(CadDraw.P(-1383.848, 1083.848), CadDraw.P(-1290.51, 990.51), b03),
                CadDraw.Polyline(
                    [CadDraw.V(-1200, 1160, bulge: 0.414214), CadDraw.V(-1460, 900)],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(200, 877.338),
                        CadDraw.V(200, 1080.036),
                        CadDraw.V(88.694, 969.044),
                        CadDraw.V(-88.695, 969.044),
                        CadDraw.V(-200, 1080.036),
                        CadDraw.V(-200, 877.338),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(300, -977.338), CadDraw.V(300, -1160), CadDraw.V(1200, -1160)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(300, 977.338), CadDraw.V(300, 1160), CadDraw.V(1200, 1160)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-300, -977.338), CadDraw.V(-300, -1160), CadDraw.V(-1200, -1160)],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(200, -877.338),
                        CadDraw.V(200, -1080.036),
                        CadDraw.V(88.694, -969.044),
                        CadDraw.V(-88.695, -969.044),
                        CadDraw.V(-200, -1080.036),
                        CadDraw.V(-200, -877.338),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(608.209, -1757.042),
                        CadDraw.V(608.209, 622.75),
                        CadDraw.V(648.209, 522.75),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-479.523, 745.977),
                        CadDraw.V(-519.523, 845.977),
                        CadDraw.V(-519.523, -1757.042),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1251.9, 514.422),
                        CadDraw.V(-1351.9, 554.422),
                        CadDraw.V(2147.163, 554.422),
                    ],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-300, 977.338), CadDraw.V(-300, 1160), CadDraw.V(-1200, 1160)],
                    b03
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-998, -543.501),
                        CadDraw.V(-1098, -503.501),
                        CadDraw.V(2149.325, -503.501),
                    ],
                    b03
                ),
                .. new[]
                {
                    CadDraw.P(-50, -969.044),
                    CadDraw.P(-50, 801.639),
                    CadDraw.P(50, -969.044),
                    CadDraw.P(50, 801.639),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 167.405), b03)),
                CadDraw.Polyline(
                    [CadDraw.V(1460, -900), CadDraw.V(1460, -100), CadDraw.V(1328, -100)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(1328, 100), CadDraw.V(1460, 100), CadDraw.V(1460, 900)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1460, -900), CadDraw.V(-1460, -100), CadDraw.V(-1328, -100)],
                    b03
                ),
                CadDraw.Polyline(
                    [CadDraw.V(-1460, 900), CadDraw.V(-1460, 100), CadDraw.V(-1328, 100)],
                    b03
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1460, 200), CadDraw.P(-1328, 200), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1328, 200), CadDraw.P(1460, 200), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1460, -900), CadDraw.P(-1328, -900), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(1328, -900), CadDraw.P(1460, -900), b03),
                    CadDraw.P(0, 100),
                    8
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1200, -1160), CadDraw.P(-1200, -1028), b03),
                    CadDraw.P(100, 0),
                    9
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(-1200, 1028), CadDraw.P(-1200, 1160), b03),
                    CadDraw.P(100, 0),
                    9
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(400, -1160), CadDraw.P(400, -1028), b03),
                    CadDraw.P(100, 0),
                    9
                ),
                .. CadDraw.EntityArray(
                    CadDraw.Line(CadDraw.P(400, 1028), CadDraw.P(400, 1160), b03),
                    CadDraw.P(100, 0),
                    9
                ),
                CadDraw.Line(CadDraw.P(0, -950), CadDraw.P(0, 950), b06),
                CadDraw.Line(CadDraw.P(-1450, 0), CadDraw.P(1450, 0), b06),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-300, 977.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-200, -877.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-50, 801.639),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-200, 877.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-50, -801.639),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(200, -877.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(50, -801.639),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(200, 877.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1328, 200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1157.942, 200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1328, 500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1157.942, 500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1328, 800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1157.942, 800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1328, -200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1157.942, -200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1328, -500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1157.942, -500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1328, -800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1157.942, -800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1290.51, -990.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. new[]
                {
                    CadDraw.P(1328, -900),
                    CadDraw.P(1328, -700),
                    CadDraw.P(1328, -600),
                    CadDraw.P(1328, -400),
                    CadDraw.P(1328, -300),
                    CadDraw.P(1328, -100),
                    CadDraw.P(1328, 0),
                    CadDraw.P(1328, 100),
                    CadDraw.P(1328, 300),
                    CadDraw.P(1328, 400),
                    CadDraw.P(1328, 600),
                    CadDraw.P(1328, 700),
                    CadDraw.P(1328, 900),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(1290.51, 990.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1328, 200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1157.942, 200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1328, 500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1157.942, 500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1328, 800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1157.942, 800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1328, -200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1157.942, -200),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1328, -500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1157.942, -500),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1328, -800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1157.942, -800),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1200, -1028),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(100, 0),
                    9
                ),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1200, 1028),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(100, 0),
                    9
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1290.51, -990.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. new[]
                {
                    CadDraw.P(-1328, -900),
                    CadDraw.P(-1328, -700),
                    CadDraw.P(-1328, -600),
                    CadDraw.P(-1328, -400),
                    CadDraw.P(-1328, -300),
                    CadDraw.P(-1328, 300),
                    CadDraw.P(-1328, 400),
                    CadDraw.P(-1328, 600),
                    CadDraw.P(-1328, 700),
                    CadDraw.P(-1328, 900),
                }.Select(p => new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = p,
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                }),
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(-1328, -100),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(0, 100),
                    3
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-1290.51, 990.51),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(300, -977.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(50, 801.639),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(400, -1028),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(100, 0),
                    9
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(300, 977.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                .. CadDraw.EntityArray(
                    new Insert(SteelSection.EnsureDefined(doc))
                    {
                        InsertPoint = CadDraw.P(400, 1028),
                        XScale = 4.533,
                        YScale = 4.533,
                        ZScale = 4.533,
                        Layer = b01,
                    },
                    CadDraw.P(100, 0),
                    9
                ),
                new Insert(SteelSection.EnsureDefined(doc))
                {
                    InsertPoint = CadDraw.P(-300, -977.338),
                    XScale = 4.533,
                    YScale = 4.533,
                    ZScale = 4.533,
                    Layer = b01,
                },
                HRB400Block.Insert(doc, CadDraw.P(867.31, 1282.34), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-607.222, -1391.333), "N3-1", "14", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(66.614, 1174.451), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1974.75, 638.668), "N2-1", "14", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1976.913, -419.255), "N5-1", "12", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-1551.597, 567.427), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(1572.813, 567.427), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(982.561, 167.431), "N1-5", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1572.813, -370.162), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-1551.597, -370.162), "N1-1", "28", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-993.611, -1272.811), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(66.614, -1174.451), "N1-2", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(1125.684, -1272.811), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(520.509, -1391.333), "N4-1", "12", 5, 1.571),
                HRB400Block.Insert(doc, CadDraw.P(-767.148, 1282.34), "N1-1", "28", 5, 0),
                HRB400Block.Insert(doc, CadDraw.P(-932.856, 167.431), "N1-5", "28", 5, 0),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        // 首尾严格相接、同一条尺寸线且无文字覆盖的连续段用 CadDraw.ChainDimension（实体数不变）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 6 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1328, 899.643),
                CadDraw.P(-1400, 899.643),
                CadDraw.P(-1400, 1510.579),
                0,
                dimStyle,
                b03
            ),
            // 源 DIMENSION 1998CE/1998CF：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [CadDraw.P(-200, 877.338), CadDraw.P(-50, 801.639), CadDraw.P(50, 801.639)],
                dimensionLinePoint: CadDraw.P(-50, 1510.936),
                rotation: 0,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1328, 899.643),
                CadDraw.P(-1200, 1027.643),
                CadDraw.P(-1200, 1510.579),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1200, 1027.643),
                CadDraw.P(-200, 877.338),
                CadDraw.P(-200, 1510.579),
                0,
                dimStyle,
                b03,
                "10×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1400, 0),
                CadDraw.P(1400, 0),
                CadDraw.P(1400, 1881.638),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 6 段
            // 源 DIMENSION 199854/199855/199856/199857：4 段首尾相接，合并为尺寸链（仍是 4 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1327.169, 1028),
                    CadDraw.P(-1189.063, 900),
                    CadDraw.P(-1189.063, -900),
                    CadDraw.P(-1061.063, -1028),
                    CadDraw.P(-1061.063, -1100),
                ],
                dimensionLinePoint: CadDraw.P(-1817.684, 900),
                rotation: Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1327.169, 1028),
                CadDraw.P(-1327.169, 1100),
                CadDraw.P(-1817.684, 1100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-865.015, 1100),
                CadDraw.P(-865.015, -1100),
                CadDraw.P(-2166.653, -1100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 4 段
            CadDraw.RotatedDimension(
                CadDraw.P(1328, 899.643),
                CadDraw.P(1200, 1027.643),
                CadDraw.P(1200, 1510.579),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1200, 1027.643),
                CadDraw.P(200, 877.338),
                CadDraw.P(200, 1510.579),
                Math.PI,
                dimStyle,
                b03,
                "10×100"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1328, 899.643),
                CadDraw.P(1400, 899.643),
                CadDraw.P(1400, 1510.579),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(200, 877.338),
                CadDraw.P(50, 801.639),
                CadDraw.P(50, 1510.936),
                Math.PI,
                dimStyle,
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 16 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b03;
        }

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 6：源图窗口 [775, 217, 1247, 507]，185 个实体，21.80×13.40 m。</summary>
    public static void DrawRegion06(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        List<Entity> entities =
        [
            .. TitleBlock.Add(doc, "箍筋关系示意图", "", CadDraw.P(-62334.959, 18984.948), 50),
            .. TitleBlock.Add(doc, "钢筋大样图", "", CadDraw.P(-70869.722, 26483.07), 50),
            CadDraw.Polyline(
                [
                    CadDraw.V(-63754.838, 18346.885),
                    CadDraw.V(-61044.08, 18346.885),
                    CadDraw.V(-61044.08, 16701.962),
                    CadDraw.V(-63754.838, 16701.962),
                ],
                b01,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-62650.924, 18361.713),
                    CadDraw.V(-62650.924, 16687.134),
                    CadDraw.V(-62863.635, 16474.424),
                    CadDraw.V(-63642.924, 16474.424, bulge: -0.414214),
                    CadDraw.V(-63792.924, 16624.424),
                    CadDraw.V(-63792.924, 18424.424, bulge: -0.414214),
                    CadDraw.V(-63642.924, 18574.424),
                    CadDraw.V(-62863.635, 18574.424),
                ],
                b01,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-61155.993, 18574.424),
                    CadDraw.V(-61935.283, 18574.424),
                    CadDraw.V(-62147.993, 18361.713),
                    CadDraw.V(-62147.993, 16687.134),
                    CadDraw.V(-61935.283, 16474.424),
                    CadDraw.V(-61155.993, 16474.424, bulge: 0.414214),
                    CadDraw.V(-61005.993, 16624.424),
                    CadDraw.V(-61005.993, 18424.424, bulge: 0.414214),
                ],
                b01,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-68611.249, 17978.555, bulge: 0.074834),
                    CadDraw.V(-69552.414, 20725.415),
                    CadDraw.V(-69552.572, 20725.671),
                    CadDraw.V(-69552.572, 21125.671),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-65512.538, 23029.41, bulge: 0.022905),
                    CadDraw.V(-65842.079, 25317.996),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-79356.718, 17665.182),
                    CadDraw.V(-79356.718, 20717.214),
                    CadDraw.V(-79356.718, 20716.958, bulge: 0.120449),
                    CadDraw.V(-80456.718, 25216.958),
                    CadDraw.V(-80456.876, 25217.209),
                    CadDraw.V(-80456.876, 25617.209),
                ],
                b01
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(-74664.905, 17665.182),
                    CadDraw.V(-74664.905, 20717.218, bulge: 0.022211),
                    CadDraw.V(-74864.905, 25217.214),
                    CadDraw.V(-74864.905, 25617.214),
                ],
                b01
            ),
            CadDraw.Line(CadDraw.P(-67729.895, 17978.555), CadDraw.P(-67729.895, 21125.671), b01),
            CadDraw.Line(CadDraw.P(-71117.411, 17665.182), CadDraw.P(-71117.411, 25617.214), b01),
            CadDraw.Line(CadDraw.P(-69183.958, 22954.415), CadDraw.P(-69183.958, 25104.415), b01),
            CadDraw.Line(CadDraw.P(-72601.449, 22082.975), CadDraw.P(-72048.358, 22187.367), b03),
            CadDraw.Line(CadDraw.P(-66111.343, 23839.646), CadDraw.P(-65690.804, 23919.02), b03),
            CadDraw.Line(CadDraw.P(-64704.928, 23751.881), CadDraw.P(-64282.688, 23831.576), b03),
            CadDraw.Line(CadDraw.P(-68332.403, 23630.613), CadDraw.P(-67919.391, 23708.566), b03),
            CadDraw.Line(CadDraw.P(-76908.615, 21681.344), CadDraw.P(-75979.751, 21856.662), b03),
            CadDraw.Line(CadDraw.P(-69159.751, 18558.756), CadDraw.P(-68828.457, 18621.286), b03),
            CadDraw.Line(CadDraw.P(-80377.497, 22006.153), CadDraw.P(-79540.463, 22164.138), b03),
            CadDraw.Line(CadDraw.P(-78719.327, 23211.235), CadDraw.P(-78320.291, 23286.55), b03),
            CadDraw.Line(CadDraw.P(-75200.351, 23010.71), CadDraw.P(-74794.656, 23087.282), b03),
            CadDraw.Text(
                "400",
                CadDraw.P(-80434.772, 25525.375),
                125,
                b07,
                4.712,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "曲线长2318（投影长2313）",
                CadDraw.P(-65614.89, 24832.991),
                125,
                b07,
                4.835,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "R25250",
                CadDraw.P(-66091.04, 23862.335),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "曲线长1434～4677",
                CadDraw.P(-68982.604, 19642.703),
                125,
                b07,
                4.951,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "R50725",
                CadDraw.P(-64682.836, 23765.098),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "R50725",
                CadDraw.P(-68311.323, 23649.188),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text("（正面）", CadDraw.P(-65845.72, 25582.815), 125, b07, widthFactor: 0.7),
            CadDraw.Text("（侧面）", CadDraw.P(-64744.308, 25595.024), 125, b07, widthFactor: 0.7),
            CadDraw.Text("（正面）", CadDraw.P(-73123.879, 25816.901), 125, b07, widthFactor: 0.7),
            CadDraw.Text("（侧面）", CadDraw.P(-68060.558, 25375.96), 125, b07, widthFactor: 0.7),
            CadDraw.Text("（侧面）", CadDraw.P(-74981.953, 25836.659), 125, b07, widthFactor: 0.7),
            CadDraw.Text(
                "R25250～R51965",
                CadDraw.P(-76887.123, 21697.737),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "R50725",
                CadDraw.P(-78696.655, 23223.377),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "曲线长4512～4530（投影长4506）",
                CadDraw.P(-74717.378, 24207.094),
                125,
                b07,
                4.765,
                widthFactor: 0.7
            ),
            CadDraw.Text("（正面）", CadDraw.P(-76522.479, 25827.587), 125, b07, widthFactor: 0.7),
            CadDraw.Text(
                "R9755",
                CadDraw.P(-69136.462, 18567.632),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "400",
                CadDraw.P(-69530.468, 21033.837),
                125,
                b07,
                4.712,
                widthFactor: 0.7
            ),
            CadDraw.Text("（侧面）", CadDraw.P(-78497.819, 25851.743), 125, b07, widthFactor: 0.7),
            CadDraw.Text("（正面）", CadDraw.P(-80619.65, 25842.667), 125, b07, widthFactor: 0.7),
            CadDraw.Text(
                "曲线长4545～4683（投影长4539～4677）",
                CadDraw.P(-79871.245, 24159.56),
                125,
                b07,
                4.946,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "H-3600",
                CadDraw.P(-71835.041, 19536.793),
                125,
                b07,
                4.712,
                widthFactor: 0.7
            ),
            CadDraw.Text(
                "R9755",
                CadDraw.P(-72470.174, 22120.33),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text("（正面）", CadDraw.P(-69346.732, 25366.888), 125, b07, widthFactor: 0.7),
            CadDraw.Text("示意", CadDraw.P(-70128.072, 26488.252), 175, b07, widthFactor: 0.7),
            CadDraw.Text("（侧面）", CadDraw.P(-71252.695, 25778.775), 125, b07, widthFactor: 0.7),
            CadDraw.Text("（正面）", CadDraw.P(-69428.38, 21389.992), 125, b07, widthFactor: 0.7),
            CadDraw.Text(
                "R9755～R19918",
                CadDraw.P(-80355.499, 22021.869),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            CadDraw.Text("（侧面）", CadDraw.P(-68055.055, 21393.795), 125, b07, widthFactor: 0.7),
            CadDraw.Text(
                "R50725",
                CadDraw.P(-75178.703, 23026.279),
                125,
                b07,
                0.187,
                widthFactor: 0.7
            ),
            HRB400Block.Insert(
                doc,
                CadDraw.P(-68944.534, 21785.116),
                "N1-6",
                "28 L=3455(平均)",
                5,
                0
            ),
            HRB400Block.Insert(doc, CadDraw.P(-63160.595, 18709.522), "N2-1～2", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-79361.689, 26132.668), "N1-1", "28 L=11914", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-79696.831, 16635.631), "N3-1", "14 L=9076", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-65343.691, 25923.741), "N1-4", "28 L=2318", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-68675.253, 25698.777), "N1-3", "28 L=2155", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-61390.189, 18709.522), "N2-1～2", "14", 5, 0),
            HRB400Block.Insert(
                doc,
                CadDraw.P(-69240.506, 14915.182),
                "N5-2",
                "12 L=2461(平均)",
                5,
                0
            ),
            HRB400Block.Insert(
                doc,
                CadDraw.P(-69260.422, 16230.67),
                "N4-2",
                "12 L=3382(平均)",
                5,
                0
            ),
            HRB400Block.Insert(doc, CadDraw.P(-65798.391, 22361.789), "N2-1", "14 L=6584", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-65921.942, 19035.646), "N2-2", "14 L=6979", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-72765.161, 16249.656), "N4-1", "12 L=2986", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-72317.731, 26085.584), "N1-5", "28 L=11977", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-76395.868, 16574.548), "N3-2", "14 L=10018", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-75833.355, 26165.46), "N1-2", "28 L=11675", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-63003.987, 16834.81), "N3-1～2", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-72745.628, 14940.581), "N5-1", "12 L=2386", 5, 0),
        ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        // 首尾严格相接、同一条尺寸线且无文字覆盖的连续段用 CadDraw.ChainDimension（实体数不变）。
        List<Entity> dimensionEntities =
        [
            // rotation = 1.5708（竖直）· 19 段
            CadDraw.RotatedDimension(
                CadDraw.P(-67943.51, 25004.415),
                CadDraw.P(-67802.712, 22954.415),
                CadDraw.P(-68469.828, 22954.415),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-67943.51, 25004.415),
                CadDraw.P(-67943.51, 25104.415),
                CadDraw.P(-68470.084, 25104.415),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-64303.732, 25079.41),
                CadDraw.P(-64162.934, 23029.41),
                CadDraw.P(-64830.05, 23029.41),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-65842.079, 25317.996),
                CadDraw.P(-65512.538, 23029.41),
                CadDraw.P(-66164.341, 23029.41),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-71117.411, 25617.214),
                CadDraw.P(-71117.411, 17665.182),
                CadDraw.P(-71412.366, 17665.182),
                Math.PI / 2,
                dimStyle,
                b03,
                "H+1300"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-74618.504, 25217.214),
                CadDraw.P(-74618.504, 25617.214),
                CadDraw.P(-75391.479, 25617.214),
                Math.PI / 2,
                dimStyle,
                b03,
                "100～400"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-74618.222, 25216.958),
                CadDraw.P(-74368.248, 20717.218),
                CadDraw.P(-75391.223, 20717.218),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-76359.705, 25617.214),
                CadDraw.P(-75879.697, 20717.214),
                CadDraw.P(-76663.007, 20717.214),
                Math.PI / 2,
                dimStyle,
                b03,
                "4600～<>"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-69552.572, 21125.671),
                CadDraw.P(-69552.572, 20725.671),
                CadDraw.P(-69812.278, 20725.671),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-69183.958, 25104.415),
                CadDraw.P(-69183.958, 22954.415),
                CadDraw.P(-69442.713, 22954.415),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // 源 DIMENSION 95574/95575：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-72961.105, 25617.214),
                    CadDraw.P(-72961.105, 25217.214),
                    CadDraw.P(-72011.105, 20717.214),
                ],
                dimensionLinePoint: CadDraw.P(-73220.811, 25217.214),
                rotation: Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-64303.732, 25079.41),
                CadDraw.P(-64303.732, 25317.996),
                CadDraw.P(-64830.306, 25317.996),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-78134.37, 25217.214),
                CadDraw.P(-78134.37, 25617.214),
                CadDraw.P(-78907.345, 25617.214),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-67729.895, 21125.671),
                CadDraw.P(-67729.895, 17978.555),
                CadDraw.P(-68017.211, 17978.555),
                Math.PI / 2,
                dimStyle,
                b03,
                "1716～4900"
            ),
            // 源 DIMENSION 95484/95485：2 段首尾相接，合并为尺寸链（仍是 2 个原生 DIMENSION）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-80456.876, 25617.209),
                    CadDraw.P(-80456.876, 25217.209),
                    CadDraw.P(-79506.876, 20717.209),
                ],
                dimensionLinePoint: CadDraw.P(-80716.582, 25217.209),
                rotation: Math.PI / 2,
                style: dimStyle,
                layer: b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-69552.572, 20725.671),
                CadDraw.P(-68436.64, 17978.555),
                CadDraw.P(-69812.278, 17978.555),
                Math.PI / 2,
                dimStyle,
                b03,
                "1316～4500"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-78134.088, 25216.958),
                CadDraw.P(-77884.114, 20717.218),
                CadDraw.P(-78907.089, 20717.218),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // 角度标注（源 Angular2Ln，4 条）：helper 由两边方向生成尺寸弧、箭头与 135° 文字
            CadDraw.Angular2Line(
                CadDraw.P(-65138.075, 22034.051),
                CadDraw.P(-64925.365, 21821.340),
                CadDraw.P(-66249.308, 22034.051),
                CadDraw.P(-65302.488, 21869.638),
                b03
            ),
            CadDraw.Angular2Line(
                CadDraw.P(-65038.791, 18773.000),
                CadDraw.P(-64826.080, 18560.290),
                CadDraw.P(-66535.098, 18773.000),
                CadDraw.P(-65203.204, 18608.587),
                b03
            ),
            CadDraw.Angular2Line(
                CadDraw.P(-65038.791, 16673.000),
                CadDraw.P(-64826.080, 16885.711),
                CadDraw.P(-66535.098, 16673.000),
                CadDraw.P(-65038.791, 16905.515),
                b03
            ),
            CadDraw.Angular2Line(
                CadDraw.P(-65138.075, 19934.051),
                CadDraw.P(-64925.365, 20146.762),
                CadDraw.P(-66249.308, 19934.051),
                CadDraw.P(-65138.075, 20166.566),
                b03
            ),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 17 个）
        var hrb400 = entities
            .OfType<Insert>()
            .Where(x => x.Block?.Name == HRB400Block.Name)
            .ToList();
        foreach (var insert in hrb400)
        {
            insert.Layer = b04;
        }

        doc.AddEntities(entities);
        var rebarOptions = new RebarDetailOptions { TextHeight = 125, TextOffset = 120 };
        // N2-1 箍筋：8 段，总长 6584（1800 + 239 + 880 + 311 + 1664 + 311 + 880 + 239 + 2×130 弯钩）
        var nn21 = new Stirrup
        {
            Number = "N2-1",
            Diameter = 14,
            TrueLength = 6584,
            Vertices =
            [
                RebarDetail.V(-66399.308, 20084.051, "1800"),
                RebarDetail.V(-66399.308, 21884.051, "239"),
                RebarDetail.V(-66249.308, 22034.051, "880"),
                RebarDetail.V(-65138.075, 22034.051, "311"),
                RebarDetail.V(-64925.365, 21821.34, "1664"),
                RebarDetail.V(-64925.365, 20146.762, "311"),
                RebarDetail.V(-65138.075, 19934.051, "880"),
                RebarDetail.V(-66249.308, 19934.051, "239"),
            ],
            Hooks =
            [
                new(new XY(-66322.932, 22014.739), new XY(-66200.114, 21901.74), "130"),
                new(new XY(-66386.305, 21945.14), new XY(-66262.035, 21830.805)),
            ],
        };
        doc.AddEntities(nn21.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N2-2 箍筋：8 段，总长 6979（311 + 880～1125 + 239 + 1800～1950 + 239 + 880～1125 + 311 + 1664～1814 + 2×130 弯钩）
        var nn22 = new Stirrup
        {
            Number = "N2-2",
            Diameter = 14,
            TrueLength = 6979,
            Vertices =
            [
                RebarDetail.V(-64826.08, 16885.711, "311"),
                RebarDetail.V(-65038.791, 16673, "880～1125"),
                RebarDetail.V(-66535.098, 16673, "239"),
                RebarDetail.V(-66685.098, 16823, "1800～1950"),
                RebarDetail.V(-66685.098, 18623, "239"),
                RebarDetail.V(-66535.098, 18773, "880～1125"),
                RebarDetail.V(-65038.791, 18773, "311"),
                RebarDetail.V(-64826.08, 18560.29, "1664～1814"),
            ],
            Hooks =
            [
                new(new XY(-66608.723, 18753.688), new XY(-66485.905, 18640.689), "130"),
                new(new XY(-66672.095, 18684.089), new XY(-66547.825, 18569.754)),
            ],
        };
        doc.AddEntities(nn22.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N3-1 箍筋：4 段，总长 9076（2704 + 1704 + 2704 + 1704 + 2×130 弯钩）
        var nn31 = new Stirrup
        {
            Number = "N3-1",
            Diameter = 14,
            TrueLength = 9076,
            Vertices =
            [
                RebarDetail.V(-80333.12, 16256.163, "2704"),
                RebarDetail.V(-78680.14, 16256.163, "1704"),
                RebarDetail.V(-78680.14, 14556.163, "2704"),
                RebarDetail.V(-80333.12, 14556.163, "1704"),
            ],
            Hooks =
            [
                new(new XY(-78746.051, 16256.163), new XY(-78896.927, 16092.177), "130"),
                new(new XY(-78680.14, 16201.818), new XY(-78831.016, 16037.831)),
            ],
        };
        doc.AddEntities(nn31.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N3-2 箍筋：4 段，总长 10018（2704～3496 + 1704～1854 + 2704～3496 + 1704～1854 + 2×130 弯钩）
        var nn32 = new Stirrup
        {
            Number = "N3-2",
            Diameter = 14,
            TrueLength = 10018,
            Vertices =
            [
                RebarDetail.V(-75182.498, 14556.163, "2704～3496"),
                RebarDetail.V(-77125.193, 14556.163, "1704～1854"),
                RebarDetail.V(-77125.193, 16256.163, "2704～3496"),
                RebarDetail.V(-75182.498, 16256.163, "1704～1854"),
            ],
            Hooks =
            [
                new(new XY(-75182.498, 16201.818), new XY(-75333.374, 16037.831), "130"),
                new(new XY(-75248.408, 16256.163), new XY(-75399.284, 16092.177)),
            ],
        };
        doc.AddEntities(nn32.Detail(doc, CadDraw.P(0, 0), rebarOptions));

        // N4-1 线筋：3 段，总长 2986（141 + 2704 + 141）
        var nn41 = new Rebar
        {
            Number = "N4-1",
            Diameter = 12,
            TrueLength = 2986,
            Vertices =
            [
                RebarDetail.V(-73814.827, 15767.289, "141"),
                RebarDetail.V(-73814.827, 15916.158, "2704"),
                RebarDetail.V(-71319.827, 15916.158, "141"),
                RebarDetail.V(-71319.827, 15767.289),
            ],
        };
        doc.AddEntities(nn41.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N4-2 线筋：3 段，总长 3382（141 + 2704～3496 + 141）
        var nn42 = new Rebar
        {
            Number = "N4-2",
            Diameter = 12,
            TrueLength = 3382,
            Vertices =
            [
                RebarDetail.V(-70153.622, 15767.289, "141"),
                RebarDetail.V(-70153.622, 15916.158, "2704～3496"),
                RebarDetail.V(-67658.622, 15916.158, "141"),
                RebarDetail.V(-67658.622, 15767.289),
            ],
        };
        doc.AddEntities(nn42.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N5-1 线筋：3 段，总长 2386（141 + 2104 + 141）
        var nn51 = new Rebar
        {
            Number = "N5-1",
            Diameter = 12,
            TrueLength = 2386,
            Vertices =
            [
                RebarDetail.V(-71363.057, 14476.481, "141"),
                RebarDetail.V(-71363.057, 14625.35, "2104"),
                RebarDetail.V(-73858.057, 14625.35, "141"),
                RebarDetail.V(-73858.057, 14476.481),
            ],
        };
        doc.AddEntities(nn51.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N5-2 线筋：3 段，总长 2461（141 + 2104～2254 + 141）
        var nn52 = new Rebar
        {
            Number = "N5-2",
            Diameter = 12,
            TrueLength = 2461,
            Vertices =
            [
                RebarDetail.V(-67701.851, 14476.481, "141"),
                RebarDetail.V(-67701.851, 14625.35, "2104～2254"),
                RebarDetail.V(-70196.851, 14625.35, "141"),
                RebarDetail.V(-70196.851, 14476.481),
            ],
        };
        doc.AddEntities(nn52.Detail(doc, CadDraw.P(0, 0), rebarOptions));

        // N1-1 线筋：3 段，总长 11914（400 + 4614 + 6900；H=10500）
        var nn11 = new Rebar
        {
            Number = "N1-1",
            Diameter = 28,
            TrueLength = 11914,
            Vertices =
            [
                RebarDetail.V(-78380.771, 25617.214, "400"),
                RebarDetail.V(-78380.771, 25217.214, "4614", bulge: -0.022211),
                RebarDetail.V(-78180.771, 20717.218, "6900"),
                RebarDetail.V(-78180.771, 17665.182),
            ],
        };
        doc.AddEntities(nn11.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N1-2 线筋：2 段，总长 11675（6900 + 4775；H=10500）
        var nn12 = new Rebar
        {
            Number = "N1-2",
            Diameter = 28,
            TrueLength = 11675,
            Vertices =
            [
                RebarDetail.V(-75879.697, 17665.178, "6900"),
                RebarDetail.V(-75879.697, 20717.214, "4775", bulge: 0.048863),
                RebarDetail.V(-76359.705, 25617.214),
            ],
        };
        doc.AddEntities(nn12.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N1-3 线筋：2 段，总长 2155（100 + 2055；H=10500）
        var nn13 = new Rebar
        {
            Number = "N1-3",
            Diameter = 28,
            TrueLength = 2155,
            Vertices =
            [
                RebarDetail.V(-67943.51, 25104.415, "100"),
                RebarDetail.V(-67943.51, 25004.415, "2055", bulge: -0.010128),
                RebarDetail.V(-67802.712, 22954.415),
            ],
        };
        doc.AddEntities(nn13.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N1-4 线筋：2 段，总长 2318（243 + 2075；H=10500）
        var nn14 = new Rebar
        {
            Number = "N1-4",
            Diameter = 28,
            TrueLength = 2318,
            Vertices =
            [
                RebarDetail.V(-64303.732, 25317.996, "243"),
                RebarDetail.V(-64303.732, 25079.41, "2075", bulge: -0.010128),
                RebarDetail.V(-64162.934, 23029.41),
            ],
        };
        doc.AddEntities(nn14.Detail(doc, CadDraw.P(0, 0), rebarOptions));
        // N1-5 线筋：3 段，总长 11977（6900 + 4677 + 400；H=10500）
        var nn15 = new Rebar
        {
            Number = "N1-5",
            Diameter = 28,
            TrueLength = 11977,
            Vertices =
            [
                RebarDetail.V(-71861.105, 17665.432, "6900"),
                RebarDetail.V(-71861.105, 20717.214, "4677", bulge: 0.120449),
                RebarDetail.V(-72961.105, 25217.214, "400"),
                RebarDetail.V(-72961.105, 25617.214),
            ],
        };
        doc.AddEntities(nn15.Detail(doc, CadDraw.P(0, 0), rebarOptions));

        doc.AddEntities(dimensionEntities);
    }

    /// <summary>截图 01 / 区域 7：源图窗口 [1306, 232, 1522, 406]，151 个实体，10.00×8.00 m。</summary>
    public static void DrawRegion07(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);

        var table01 = TableHelper.CreateTable(
            doc,
            CadDraw.P(-55859.745, 25262.698),
            new string[,]
            {
                { "墩高H（mm）", "10500", "m", "31", "a(mm)", "0", "n", "36" },
                {
                    "墩号",
                    "编号",
                    "直径\\P(mm)",
                    "单根长\\P(mm)",
                    "根数",
                    "总长\\P(m)",
                    "单位重\\P(kg/m)",
                    "总重\\P(kg)",
                },
                { "12、13、14、15", "N1-1", "", "11914", "92", "1096.1", "4.830", "5294.1" },
                { "", "N1-2", "", "11675", "8", "93.4", "4.830", "451.1" },
                { "", "N1-3", "", "2155", "2", "4.3", "4.830", "20.8" },
                { "", "N1-4", "", "2318", "4", "9.3", "4.830", "44.8" },
                { "", "N1-5", "", "11977", "2", "24.0", "4.830", "115.7" },
                { "", "N1-6", "", "3455", "4", "13.8", "4.830", "66.8" },
                { "", "N2-1", "", "6584", "126", "829.6", "1.210", "1003.8" },
                { "", "N2-2", "", "6979", "38", "265.2", "1.210", "320.9" },
                { "", "N3-1", "", "9076", "63", "571.8", "1.210", "691.9" },
                { "", "N3-2", "", "10018", "19", "190.3", "1.210", "230.3" },
                { "", "N4-1", "", "2986", "315", "940.6", "0.888", "835.2" },
                { "", "N4-2", "", "3382", "95", "321.3", "0.888", "285.3" },
                { "", "N5-1", "", "2386", "378", "901.9", "0.888", "800.9" },
                { "", "N5-2", "", "2461", "114", "280.6", "0.888", "249.1" },
                { "", "合计", "HRB400E钢筋（kg）", "", "", "", "", "10410.7" },
                { "", "", "墩身C40混凝土（m3）", "", "", "", "", "80.3" },
            },
            [812.5, 812.5, 812.5, 812.5, 812.5, 812.5, 812.5, 812.5],
            [
                541.741,
                571.156,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                316.667,
                336.22,
                317.568,
            ],
            125
        );
        TableHelper.MergeCells(table01, 2, 0, 17, 0);
        TableHelper.MergeCells(table01, 16, 1, 17, 1);
        TableHelper.MergeCells(table01, 16, 2, 16, 6);
        TableHelper.MergeCells(table01, 17, 2, 17, 6);

        List<Entity> entities =
        [
            table01,
            .. TitleBlock.Add(doc, "墩身钢筋数量表", "", CadDraw.P(-52667.801, 25545.843), 50),
            HRB400Block.Insert(doc, CadDraw.P(-53905.303, 20508.135), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53913.636, 23991.468), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53913.636, 23041.468), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53913.636, 23358.135), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53907.386, 22091.468), "", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53913.636, 22408.135), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53913.636, 22724.802), "", "28", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53905.303, 20824.802), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53907.386, 21458.135), "", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53907.386, 21774.802), "", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53907.386, 21141.468), "", "14", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53905.303, 19874.802), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53905.303, 20191.468), "", "12", 5, 0),
            HRB400Block.Insert(doc, CadDraw.P(-53913.636, 23674.802), "", "28", 5, 0),
        ];

        // TableHelper 创建的 TABLE 默认落在 0 层，统一归到「B-03 标注」
        foreach (var table in entities.OfType<TableEntity>())
        {
            table.Layer = b03;
        }

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 14 个）
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

    /// <summary>截图 01 / 区域 8：源图窗口 [1328, 492, 1479, 557]，2 个实体，7.00×3.00 m。</summary>
    public static void DrawRegion08(CadDocument doc)
    {
        var b04 = doc.Layer(CadLayers.B04);
        var b07 = doc.Layer(CadLayers.B07);

        List<Entity> entities =
        [
            CadDraw.MText(
                "附注{\\H1.25x;：\\P}     1、本图尺寸以毫米计。\\P     2、“   ”表示HRB400E钢筋,\\P     3、钢筋弯钩为标准弯钩。\\P     4、{\\C4;本图适用于12#～15#桥墩。}",
                CadDraw.P(-54344.782, 13713.004),
                175,
                b07,
                attachment: AttachmentPointType.TopLeft
            ),
            HRB400Block.Insert(doc, CadDraw.P(-53358.034, 12901.674), "", "", 7, 0),
        ];

        // HRB400 帮助器把块建在 0 层：按源图图层分组归还（共 1 个）
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
}
