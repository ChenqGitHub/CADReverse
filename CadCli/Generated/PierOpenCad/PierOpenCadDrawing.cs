using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Drawing;
using DwgSharpKit.Standards;
using DwgSharpKit.Tables;

namespace CadCli.Generated.PierOpenCad;

/// <summary>
/// 由 OpenCAD Studio 分区导出（region-*.json）逐区还原的图纸。
/// 每个区域一个方法，坐标保留源图绝对坐标，图层按标准图集映射为 B-01/B-03/B-04/B-06/B-07。
/// 同图层内端点距离 ≤ 0.5 mm 的直线、多段线与圆弧已先行合并为一条多段线；
/// 等间距的直线、多段线、钢筋断面族用 CadDraw.EntityArray 展开为阵列，不再逐条抄写。
/// 块参照映射：HRB400 → HRB400Block，钢筋断面/钢筋轮廓（双筋）→ SteelSection，
/// _Oblique → 单条 45° 斜线；Defpoints 定位点与 Hatch 填充不还原。
/// </summary>
public static class PierOpenCadDrawing
{
    /// <summary>
    /// 绘制 OpenCAD Studio 当前视图（02 桥墩构造.dwg）的全部 6 个分区：
    /// 墩顶平面图、桥墩立面图、桥墩侧面图、墩高度参数表、Ⅰ-Ⅰ截面、附注。
    /// </summary>
    public static void Draw(CadDocument doc)
    {
        DrawRegion01(doc);
        DrawRegion02(doc);
        DrawRegion03(doc);
        DrawRegion04(doc);
        DrawRegion05(doc);
        DrawRegion06(doc);
    }

    /// <summary>截图 01 / 区域 1：源图窗口 [860, 94, 1096, 269]，78 个实体，去重 1，6.77×5.05 m。</summary>
    public static void DrawRegion01(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-70151.435, -15567.883);

        List<Entity> entities =

            // 源图有 1 个重复对象（同层同几何，仅句柄不同），只画一份
            // 本区域有 15 条横线 / 11 条竖线，但找不到四边闭合的外框，无法确定表格边界 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "墩顶平面图", "1:50", CadDraw.P(-73.652, 2165.328), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(
                            -2500,
                            -1100,
                            bulge: CadDraw.BulgeFromTangent(
                                (-2500.0 - (-2300.0)) / (-1300.0 - (-1100.0))
                            )
                        ),
                        CadDraw.V(-2300, -1300),
                        CadDraw.V(-800, -1300),
                        CadDraw.V(-600, -1100),
                        CadDraw.V(600, -1100),
                        CadDraw.V(800, -1300),
                        CadDraw.V(
                            2300,
                            -1300,
                            bulge: -CadDraw.BulgeFromTangent(
                                (2300.0 - 2500.0) / (-1100.0 - (-1300.0))
                            )
                        ),
                        CadDraw.V(2500, -1100),
                        CadDraw.V(
                            2500,
                            1100,
                            bulge: CadDraw.BulgeFromTangent((2500.0 - 2300.0) / (1300.0 - 1100.0))
                        ),
                        CadDraw.V(2300, 1300),
                        CadDraw.V(800, 1300),
                        CadDraw.V(600, 1100),
                        CadDraw.V(-600, 1100),
                        CadDraw.V(-800, 1300),
                        CadDraw.V(
                            -2300,
                            1300,
                            bulge: -CadDraw.BulgeFromTangent(
                                (-2300.0 - (-2500.0)) / (1100.0 - 1300.0)
                            )
                        ),
                        CadDraw.V(-2500, 1100),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2200, -1100),
                        CadDraw.V(-1200, -1100),
                        CadDraw.V(-1200, 1100),
                        CadDraw.V(-2200, 1100),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2200, -1100),
                        CadDraw.V(1200, -1100),
                        CadDraw.V(1200, 1100),
                        CadDraw.V(2200, 1100),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-750, 1250),
                        CadDraw.V(
                            -2300,
                            1250,
                            bulge: -CadDraw.BulgeFromTangent(
                                (-2300.0 - (-2450.0)) / (1100.0 - 1250.0)
                            )
                        ),
                        CadDraw.V(-2450, 1100),
                        CadDraw.V(
                            -2450,
                            -1100,
                            bulge: CadDraw.BulgeFromTangent(
                                (-2450.0 - (-2300.0)) / (-1250.0 - (-1100.0))
                            )
                        ),
                        CadDraw.V(-2300, -1250),
                        CadDraw.V(-750, -1250),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(750, -1250),
                        CadDraw.V(
                            2300,
                            -1250,
                            bulge: -CadDraw.BulgeFromTangent(
                                (2300.0 - 2450.0) / (-1100.0 - (-1250.0))
                            )
                        ),
                        CadDraw.V(2450, -1100),
                        CadDraw.V(
                            2450,
                            1100,
                            bulge: CadDraw.BulgeFromTangent((2450.0 - 2300.0) / (1250.0 - 1100.0))
                        ),
                        CadDraw.V(2300, 1250),
                        CadDraw.V(750, 1250),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2406.066, 1206.066),
                        CadDraw.V(-2300, 0),
                        CadDraw.V(-2406.066, -1206.066),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2406.066, 1206.066),
                        CadDraw.V(2300, 0),
                        CadDraw.V(2406.066, -1206.066),
                    ],
                    b04
                ),
                .. new[]
                {
                    CadDraw.P(-600, -1100),
                    CadDraw.P(-300, -1100),
                    CadDraw.P(300, -1100),
                    CadDraw.P(600, -1100),
                }.Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 2200), b04)),
                CadDraw.Line(CadDraw.P(-2550, 0), CadDraw.P(2550, 0), b06),
                CadDraw.Line(CadDraw.P(0, -1150), CadDraw.P(0, 1150), b06),
                CadDraw.Line(CadDraw.P(-1700, 0), CadDraw.P(-1700, 2360), b06),
                CadDraw.Line(CadDraw.P(1700, 0), CadDraw.P(1700, 2360), b06),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3587.556, -448.496),
                        CadDraw.V(-3587.556, 538.944),
                        CadDraw.V(-3746.364, 352.776),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-553.331, -2338.712),
                        CadDraw.V(278.938, -2338.712),
                        CadDraw.V(92.769, -2179.904),
                    ],
                    b07
                ),
                CadDraw.MText(
                    "支\\P座\\P中\\P心\\P线",
                    CadDraw.P(-1823.551, 459.27),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
                CadDraw.MText(
                    "支\\P座\\P中\\P心\\P线",
                    CadDraw.P(1823.551, 459.27),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
                CadDraw.MText(
                    "桥墩中心线",
                    CadDraw.P(1008.355, 257.884),
                    150,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
                CadDraw.MText(
                    "顺\\P桥\\P向",
                    CadDraw.P(-3687.39, 275.487),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
                CadDraw.MText(
                    "横桥向",
                    CadDraw.P(-202.822, -2140.353),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 11 段
            CadDraw.RotatedDimension(
                CadDraw.P(-800, -1300),
                CadDraw.P(800, -1300),
                CadDraw.P(800, -1704.696),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-800, -1300),
                    CadDraw.P(-600, -1100),
                    CadDraw.P(-300, -1100),
                    CadDraw.P(300, -1100),
                ],
                CadDraw.P(-600, -1497.581),
                0,
                dimStyle,
                b04
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2500, 0),
                CadDraw.P(2500, 0),
                CadDraw.P(2500, 1830),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [CadDraw.P(-2200, 1100), CadDraw.P(-1200, 1150), CadDraw.P(1200, 1100)],
                CadDraw.P(-1200, 1564.458),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2500, 650),
                CadDraw.P(-2200, 1100),
                CadDraw.P(-2200, 1564.458),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2400, -1100),
                    CadDraw.P(-1700, -1100),
                    CadDraw.P(1700, -1230),
                    CadDraw.P(2400, -904.646),
                ],
                CadDraw.P(-1700, -1934.168),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 9 段
            CadDraw.RotatedDimension(
                CadDraw.P(-2100, 1300),
                CadDraw.P(-2100, -1300),
                CadDraw.P(-3163.283, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-800, 1300),
                CadDraw.P(-300, 1100),
                CadDraw.P(-134.784, 1100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-300, 1100),
                CadDraw.P(-600, -1100),
                CadDraw.P(-134.784, -1100),
                Math.PI / 2,
                dimStyle,
                b04
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-400, -1100),
                CadDraw.P(-300, -1300),
                CadDraw.P(-134.784, -1300),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-2352.003, 1300),
                    CadDraw.P(-2352.003, 1100),
                    CadDraw.P(-2352.003, -1100),
                    CadDraw.P(-2352.003, -1300),
                ],
                CadDraw.P(-2896.723, 1100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [CadDraw.P(2300, 1300), CadDraw.P(2300, 0), CadDraw.P(2300, -1300)],
                CadDraw.P(2847.634, 0),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            // rotation = 3.14159（水平反向）· 4 段
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [CadDraw.P(800, -1300), CadDraw.P(600, -1100), CadDraw.P(300, -1100)],
                CadDraw.P(600, -1497.581),
                Math.PI,
                dimStyle,
                b04
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2200, 1100),
                CadDraw.P(1200, 1150),
                CadDraw.P(1200, 1564.458),
                Math.PI,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(2500, 650),
                CadDraw.P(2200, 1100),
                CadDraw.P(2200, 1564.458),
                Math.PI,
                dimStyle,
                b03
            ),
        ];

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 2：源图窗口 [124, 186, 355, 554]，138 个实体，去重 3，6.70×10.55 m。</summary>
    public static void DrawRegion02(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以立面图底边中点为基点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-91025.929, -24773.125);

        List<Entity> entities =

            // 源图有 3 个重复对象（同层同几何，仅句柄不同），只画一份
            [
                .. TitleBlock.Add(doc, "桥墩立面图", "1:50", CadDraw.P(-215.97, 8783.549), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1400, 0),
                        CadDraw.V(-1400, 0),
                        CadDraw.V(
                            -1400,
                            2584.358,
                            bulge: CadDraw.BulgeFromTangent(
                                (-1400.0 - (-2500.0)) / (7084.358 - 2584.358)
                            )
                        ),
                        CadDraw.V(-2500, 7084.358),
                        CadDraw.V(
                            -2500,
                            7534.358,
                            bulge: CadDraw.BulgeFromTangent(
                                (-2500.0 - (-2450.0)) / (7584.358 - 7534.358)
                            )
                        ),
                        CadDraw.V(-2450, 7584.358),
                        CadDraw.V(-600, 7584.358),
                        CadDraw.V(-300, 7284.358),
                        CadDraw.V(300, 7284.358),
                        CadDraw.V(600, 7584.358),
                        CadDraw.V(
                            2450,
                            7584.358,
                            bulge: CadDraw.BulgeFromTangent(
                                (2500.0 - 2450.0) / (7534.358 - 7584.358)
                            )
                        ),
                        CadDraw.V(2500, 7534.358),
                        CadDraw.V(
                            2500,
                            7084.358,
                            bulge: CadDraw.BulgeFromTangent(
                                (1400.0 - 2500.0) / (2584.358 - 7084.358)
                            )
                        ),
                        CadDraw.V(1400, 2584.358),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2200, 7584.358),
                        CadDraw.V(-1200, 7584.358),
                        CadDraw.V(-1200, 7884.358),
                        CadDraw.V(-2200, 7884.358),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2200, 7584.358),
                        CadDraw.V(1200, 7584.358),
                        CadDraw.V(1200, 7884.358),
                        CadDraw.V(2200, 7884.358),
                    ],
                    b04,
                    closed: true
                ),
                // 墩身左侧轮廓：d 为 y=0 处的 x（间距 80/60/40 不等，EntityArray 用不了）
                .. new[] { -1200.0, -1280.0, -1340.0, -1380.0 }.Select(d =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(
                                d - 1050,
                                7584.358,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d - 1100.0) - (d - 1050.0)) / (7534.358 - 7584.358)
                                )
                            ),
                            CadDraw.V(d - 1100, 7534.358),
                            CadDraw.V(
                                d - 1100,
                                7084.358,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d) - (d - 1100.0)) / (2584.358 - 7084.358)
                                )
                            ),
                            CadDraw.V(d, 2584.358),
                            CadDraw.V(d, 0),
                        ],
                        b04
                    )
                ),
                // 墩身右侧轮廓：d 为 y=0 处的 x
                .. new[] { 1200.0, 1280.0, 1340.0, 1380.0 }.Select(d =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(
                                d + 1050,
                                7584.358,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d + 1100.0) - (d + 1050.0)) / (7534.358 - 7584.358)
                                )
                            ),
                            CadDraw.V(d + 1100, 7534.358),
                            CadDraw.V(
                                d + 1100,
                                7084.358,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d) - (d + 1100.0)) / (2584.358 - 7084.358)
                                )
                            ),
                            CadDraw.V(d, 2584.358),
                            CadDraw.V(d, 0),
                        ],
                        b04
                    )
                ),
                // 墩身内侧轮廓：左、右各两条，d 为 y=0 处的 x
                .. new[] { -100.0, -300.0 }.Select(d =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(d, 0),
                            CadDraw.V(
                                d,
                                2584.358,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d) - (d - 500.0)) / (7584.358 - 2584.358)
                                )
                            ),
                            CadDraw.V(d - 500, 7584.358),
                        ],
                        b04
                    )
                ),
                .. new[] { 100.0, 300.0 }.Select(d =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(
                                d + 500,
                                7584.358,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d) - (d + 500.0)) / (2584.358 - 7584.358)
                                )
                            ),
                            CadDraw.V(d, 2584.358),
                            CadDraw.V(d, 0),
                        ],
                        b04
                    )
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2450, 7584.358),
                        CadDraw.V(-2500, 7584.358),
                        CadDraw.V(-2500, 7534.358),
                        CadDraw.V(-789.952, 7534.358),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2450, 7584.358),
                        CadDraw.V(2500, 7584.358),
                        CadDraw.V(2500, 7534.358),
                        CadDraw.V(789.951, 7534.358),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-228.881, -830.628),
                        CadDraw.V(228.881, -1136.972),
                        CadDraw.V(228.881, -964.93),
                        CadDraw.V(2655.757, -964.93),
                        CadDraw.V(2655.757, 0),
                        CadDraw.V(-2641.408, 0),
                        CadDraw.V(-2641.408, -973.254),
                        CadDraw.V(-228.881, -973.254),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2240.445, 899.667),
                        CadDraw.V(-2716.53, 899.667),
                        CadDraw.V(-2716.53, 715.506),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2716.53, 598.357),
                        CadDraw.V(-2716.53, 723.357),
                        CadDraw.V(-2716.53, 899.667),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2240.445, 899.667),
                        CadDraw.V(2716.53, 899.667),
                        CadDraw.V(2716.53, 715.506),
                    ],
                    b04
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(2716.53, 598.357),
                        CadDraw.V(2716.53, 723.357),
                        CadDraw.V(2716.53, 899.667),
                    ],
                    b04
                ),
                CadDraw.Line(CadDraw.P(-1105.523, 5465.07), CadDraw.P(-552.432, 5569.462), b03),
                CadDraw.Line(CadDraw.P(1105.523, 5465.07), CadDraw.P(552.432, 5569.462), b03),
                CadDraw.Line(CadDraw.P(-2741.346, 7817.726), CadDraw.P(-2539.221, 7621.9), b03),
                CadDraw.Line(CadDraw.P(-2445.016, 4745.329), CadDraw.P(-1750.196, 4876.472), b03),
                CadDraw.Line(CadDraw.P(2445.016, 4745.329), CadDraw.P(1750.195, 4876.472), b03),
                CadDraw.Line(CadDraw.P(0, 7794.708), CadDraw.P(0, -137.631), b06),
                CadDraw.Line(CadDraw.P(-1700, 7734.358), CadDraw.P(-1700, 8514.358), b06),
                CadDraw.Line(CadDraw.P(1700, 7734.358), CadDraw.P(1700, 8514.358), b06),
                CadDraw.Text(
                    "R25250",
                    CadDraw.P(-1065.407, 5491.706),
                    125,
                    b07,
                    0.187,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "R25250",
                    CadDraw.P(664.012, 5567.467),
                    125,
                    b07,
                    6.097,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "R50",
                    CadDraw.P(-2679.699, 7784.084),
                    125,
                    b07,
                    5.514,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "基础仅为示意",
                    CadDraw.P(-397.912, -574.457),
                    150,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "R9754.55",
                    CadDraw.P(-2417.238, 4766.67),
                    125,
                    b07,
                    0.187,
                    widthFactor: 0.75
                ),
                CadDraw.Text("Ⅰ", CadDraw.P(-2981.545, 711.624), 150, b07, widthFactor: 0.75),
                CadDraw.Text("Ⅰ", CadDraw.P(2883.107, 711.624), 150, b07, widthFactor: 0.75),
                CadDraw.Text(
                    "支座中心线",
                    CadDraw.P(-1695.435, 7188.914),
                    125,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "支座中心线",
                    CadDraw.P(1209.679, 7171.8),
                    125,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "R9754.55",
                    CadDraw.P(1913.796, 4861.692),
                    125,
                    b07,
                    6.097,
                    widthFactor: 0.75
                ),
                CadDraw.MText(
                    "（圆弧段）",
                    CadDraw.P(-2977.58, 4762.029),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter,
                    alignmentPoint: CadDraw.P(0, 1)
                ),
                CadDraw.MText(
                    "桥\\P墩\\P中\\P心\\P线",
                    CadDraw.P(139.983, 7048.889),
                    150,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 21 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1400, 145.404),
                CadDraw.P(1400, 187.966),
                CadDraw.P(1400, 734.449),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1400, 174.133),
                CadDraw.P(-1200, 174.133),
                CadDraw.P(-1200, 440.317),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1200, 176.772),
                CadDraw.P(1400, 176.772),
                CadDraw.P(1400, 440.317),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-300, 1274.646),
                    CadDraw.P(-100, 1274.646),
                    CadDraw.P(100, 1315.42),
                    CadDraw.P(300, 1315.42),
                ],
                CadDraw.P(-100, 1535.584),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2500, 7534.358),
                CadDraw.P(2500, 7534.358),
                CadDraw.P(2500, 8483.077),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [CadDraw.P(-1200, 7884.358), CadDraw.P(-600, 7584.358), CadDraw.P(0, 7765.207)],
                CadDraw.P(-600, 8034.069),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（5 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-800, 7584.358),
                    CadDraw.P(-600, 7584.358),
                    CadDraw.P(-300, 7284.358),
                    CadDraw.P(300, 7284.358),
                    CadDraw.P(600, 7584.358),
                    CadDraw.P(800, 7584.358),
                ],
                CadDraw.P(-600, 7773.653),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1700, 7884.358),
                    CadDraw.P(0, 8084.069),
                    CadDraw.P(1700, 7964.358),
                    CadDraw.P(2500, 7584.358),
                ],
                CadDraw.P(0, 8269.446),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1700, 7884.358),
                CadDraw.P(-2500, 7534.358),
                CadDraw.P(-2500, 8269.446),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1200, 7884.358),
                    CadDraw.P(-2200, 7884.358),
                    CadDraw.P(-2500, 7534.358),
                ],
                CadDraw.P(-2200, 8034.069),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1200, 176.772),
                CadDraw.P(1200, 176.772),
                CadDraw.P(1200, 440.317),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 7 段
            CadDraw.RotatedDimension(
                CadDraw.P(600, 7584.358),
                CadDraw.P(316.854, 7284.358),
                CadDraw.P(173.49, 7284.358),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1242.481, 2584.358),
                CadDraw.P(-1932.149, 0),
                CadDraw.P(-3056.834, 0),
                Math.PI / 2,
                dimStyle,
                b03,
                "H-5000"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2700, 7084.358),
                CadDraw.P(-662.498, 2584.358),
                CadDraw.P(-3056.834, 2584.358),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2700, 7084.358),
                CadDraw.P(-2033.63, 7584.358),
                CadDraw.P(-3056.834, 7584.358),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1362.498, 7584.358),
                CadDraw.P(-1930.453, 7534.358),
                CadDraw.P(-2856.834, 7534.358),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2700, 7084.358),
                CadDraw.P(-2033.63, 7584.358),
                CadDraw.P(-3056.834, 7584.358),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-2641.408, -3.767),
                CadDraw.P(-2200, 7584.358),
                CadDraw.P(-3426.19, 7584.358),
                Math.PI / 2,
                dimStyle,
                b03,
                "墩高H"
            ),
            // rotation = 3.14159（水平反向）· 4 段
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [CadDraw.P(1200, 7884.358), CadDraw.P(600, 7584.358), CadDraw.P(0, 7765.207)],
                CadDraw.P(600, 8034.069),
                Math.PI,
                dimStyle,
                b03
            ),
            // 尺寸链（2 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [CadDraw.P(1200, 7884.358), CadDraw.P(2200, 7884.358), CadDraw.P(2500, 7534.358)],
                CadDraw.P(2200, 8034.069),
                Math.PI,
                dimStyle,
                b03
            ),
        ];

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 3：源图窗口 [511, 185, 688, 545]，98 个实体，去重 3，5.18×10.32 m。</summary>
    public static void DrawRegion03(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-81230.9738, -24539.549);

        List<Entity> entities =

            // 源图有 3 个重复对象（同层同几何，仅句柄不同），只画一份
            // 本区域表格存在未闭合的合并单元格边界 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "桥墩侧面图", "1:50", CadDraw.P(-263.684, 8574.303), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(
                            1300.767,
                            7084.357,
                            bulge: CadDraw.BulgeFromTangent(
                                (1100.767 - 1300.767) / (2584.357 - 7084.357)
                            )
                        ),
                        CadDraw.V(1100.767, 2584.357),
                        CadDraw.V(1100.767, 0),
                        CadDraw.V(-1099.233, 0),
                        CadDraw.V(
                            -1099.233,
                            2584.357,
                            bulge: CadDraw.BulgeFromTangent(
                                (-1099.233 - (-1299.233)) / (7084.357 - 2584.357)
                            )
                        ),
                        CadDraw.V(-1299.233, 7084.357),
                    ],
                    b01
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1300.767, 7084.357),
                        CadDraw.V(
                            1300.767,
                            7534.357,
                            bulge: CadDraw.BulgeFromTangent(
                                (1300.767 - 1250.767) / (7584.357 - 7534.357)
                            )
                        ),
                        CadDraw.V(1250.767, 7584.357),
                        CadDraw.V(600.767, 7584.357),
                        CadDraw.V(-599.233, 7584.357),
                        CadDraw.V(
                            -1249.233,
                            7584.357,
                            bulge: CadDraw.BulgeFromTangent(
                                (-1299.233 - (-1249.233)) / (7534.357 - 7584.357)
                            )
                        ),
                        CadDraw.V(-1299.233, 7534.357),
                        CadDraw.V(-1299.233, 7084.357),
                    ],
                    b01
                ),
                CadDraw.Line(CadDraw.P(-1299.233, 7084.357), CadDraw.P(1300.767, 7084.357), b01),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1099.233, 7584.357),
                        CadDraw.V(1100.767, 7584.357),
                        CadDraw.V(1100.767, 7884.357),
                        CadDraw.V(-1099.233, 7884.357),
                    ],
                    b04,
                    closed: true
                ),
                // 墩身左侧轮廓：d 为 y=0 处的 x（间距 80/60/40 不等，EntityArray 用不了）
                .. new[] { -899.233, -979.233, -1039.233, -1079.233 }.Select(d =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(
                                d - 150,
                                7584.357,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d - 200.0) - (d - 150.0)) / (7534.357 - 7584.357)
                                )
                            ),
                            CadDraw.V(d - 200, 7534.357),
                            CadDraw.V(
                                d - 200,
                                7084.357,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d) - (d - 200.0)) / (2584.357 - 7084.357)
                                )
                            ),
                            CadDraw.V(d, 2584.357),
                            CadDraw.V(d, 0),
                        ],
                        b04
                    )
                ),
                // 墩身右侧轮廓：d 为 y=0 处的 x
                .. new[] { 900.767, 980.767, 1040.767, 1080.767 }.Select(d =>
                    CadDraw.Polyline(
                        [
                            CadDraw.V(
                                d + 150,
                                7584.357,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d + 200.0) - (d + 150.0)) / (7534.357 - 7584.357)
                                )
                            ),
                            CadDraw.V(d + 200, 7534.357),
                            CadDraw.V(
                                d + 200,
                                7084.357,
                                bulge: CadDraw.BulgeFromTangent(
                                    ((d) - (d + 200.0)) / (2584.357 - 7084.357)
                                )
                            ),
                            CadDraw.V(d, 2584.357),
                            CadDraw.V(d, 0),
                        ],
                        b04
                    )
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1582.641, -1.032),
                        CadDraw.V(-1581.107, 1.031),
                        CadDraw.V(-1581.107, -973.255),
                        CadDraw.V(-228.115, -973.255),
                        CadDraw.V(-228.115, -830.628),
                        CadDraw.V(229.648, -1136.972),
                        CadDraw.V(229.648, -964.931),
                        CadDraw.V(1582.641, -964.931),
                    ],
                    b04,
                    closed: true
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1249.233, 7584.357),
                        CadDraw.V(-1299.233, 7584.357),
                        CadDraw.V(-1299.233, 7534.357),
                        CadDraw.V(1300.767, 7534.357),
                        CadDraw.V(1300.767, 7584.357),
                        CadDraw.V(1250.767, 7584.357),
                    ],
                    b04
                ),
                CadDraw.Line(CadDraw.P(-1841.149, 5881.646), CadDraw.P(-1288.058, 5986.038), b03),
                CadDraw.Line(CadDraw.P(-1613.269, 7707.592), CadDraw.P(-1353.75, 7598.725), b03),
                CadDraw.Line(CadDraw.P(1757.229, 5881.646), CadDraw.P(1204.138, 5986.038), b03),
                CadDraw.Line(CadDraw.P(0.767, 7794.707), CadDraw.P(0.767, -137.632), b06),
                CadDraw.Line(CadDraw.P(0.767, 7734.357), CadDraw.P(0.767, 8194.357), b06),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1536.349, 8727.154),
                        CadDraw.V(-2480.363, 8727.154),
                        CadDraw.V(-2327.138, 8880.378),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(1578.726, 8727.154),
                        CadDraw.V(2481.896, 8727.154),
                        CadDraw.V(2328.672, 8880.378),
                    ],
                    b07
                ),
                CadDraw.Text(
                    "R50725",
                    CadDraw.P(-1655.637, 5936.8),
                    125,
                    b07,
                    0.187,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "R50",
                    CadDraw.P(-1543.607, 7698.684),
                    125,
                    b07,
                    5.886,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "基础仅为示意",
                    CadDraw.P(-473.135, -576.066),
                    150,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "R50725",
                    CadDraw.P(1341.245, 5975.823),
                    125,
                    b07,
                    6.097,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "小里程侧",
                    CadDraw.P(-2302.323, 8801.442),
                    175,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "大里程侧",
                    CadDraw.P(1605.162, 8794.607),
                    175,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.Text(
                    "支座中心线",
                    CadDraw.P(-381.979, 7281.15),
                    150,
                    b07,
                    widthFactor: 0.75
                ),
                CadDraw.MText(
                    "（圆弧段）",
                    CadDraw.P(-1666.309, 4412.04),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter,
                    alignmentPoint: CadDraw.P(0, 1)
                ),
                CadDraw.MText(
                    "桥\\P墩\\P中\\P心\\P线",
                    CadDraw.P(118.22, 6985.114),
                    150,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 8 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1099.233, 1452.149),
                CadDraw.P(1100.767, 1630.364),
                CadDraw.P(1100.767, 1630.364),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1099.233, 930.687),
                    CadDraw.P(-899.233, 930.687),
                    CadDraw.P(900.767, 930.687),
                    CadDraw.P(1100.767, 930.687),
                ],
                CadDraw.P(-899.233, 1179.382),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1299.233, 7584.357),
                CadDraw.P(1300.767, 7584.357),
                CadDraw.P(1300.767, 8296.211),
                0,
                dimStyle,
                b03
            ),
            // 尺寸链（3 段，与逐条标注等价）
            .. CadDraw.ChainDimension(
                [
                    CadDraw.P(-1299.233, 7584.357),
                    CadDraw.P(-1099.233, 7884.357),
                    CadDraw.P(1100.767, 7884.357),
                    CadDraw.P(1300.767, 7584.357),
                ],
                CadDraw.P(-1099.233, 8064.288),
                0,
                dimStyle,
                b03
            ),
            // rotation = 1.5708（竖直）· 5 段
            CadDraw.RotatedDimension(
                CadDraw.P(-1299.233, 7084.353),
                CadDraw.P(-738.776, 2584.362),
                CadDraw.P(-1681.107, 2584.362),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-838.776, 2584.362),
                CadDraw.P(-779.233, 1.031),
                CadDraw.P(-1681.107, 1.031),
                Math.PI / 2,
                dimStyle,
                b03,
                "H-5000"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-779.233, 1.031),
                CadDraw.P(-1238.776, 7584.357),
                CadDraw.P(-1838.228, 7584.357),
                Math.PI / 2,
                dimStyle,
                b03,
                "墩高H"
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1299.233, 7584.357),
                CadDraw.P(-1299.233, 7084.353),
                CadDraw.P(-1681.107, 7084.353),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(1100.767, 7885.965),
                CadDraw.P(1100.767, 7585.965),
                CadDraw.P(1631.469, 7585.965),
                Math.PI / 2,
                dimStyle,
                b03,
                "330"
            ),
        ];

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 4：源图窗口 [1292, 228, 1515, 268]，13 个实体，6.40×1.25 m。</summary>
    public static void DrawRegion04(CadDocument doc)
    {
        var b03 = doc.Layer(CadLayers.B03);
        var b07 = doc.Layer(CadLayers.B07);

        var table01 = TableHelper.CreateTable(
            doc,
            CadDraw.P(0, 0),
            new string[,]
            {
                { "墩台号", "墩高H（cm）" },
                { "12、13、14、15", "{\\fSimSun|b0|i0|c134|p2;1050" },
            },
            [1380.271, 1230.271],
            [300, 300],
            125
        );

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-60102.678, -17283.21);

        List<Entity> entities =
        [
            table01,
            CadDraw.Polyline(
                [CadDraw.V(522.069, 219.438), CadDraw.V(2274, 216.182)],
                b07,
                constantWidth: 35.714
            ),
            CadDraw.Line(CadDraw.P(521.511, 148.009), CadDraw.P(2273.19, 144.754), b07),
            CadDraw.Text(
                "墩高度参数表",
                CadDraw.P(951.823, 325.849),
                250,
                b07,
                horizontalAlignment: TextHorizontalAlignment.Center,
                widthFactor: 0.75,
                alignmentPoint: CadDraw.P(1402.716, 325.849)
            ),
        ];

        // TableHelper 创建的 TABLE 默认落在 0 层，统一归到「B-03 标注」
        foreach (var table in entities.OfType<TableEntity>())
        {
            table.Layer = b03;
        }

        doc.AddTranslated(orgin, entities);
    }

    /// <summary>截图 01 / 区域 5：源图窗口 [860, 405, 1096, 548]，41 个实体，去重 1，6.77×4.15 m。</summary>
    public static void DrawRegion05(CadDocument doc)
    {
        var b01 = doc.Layer(CadLayers.B01);
        var b04 = doc.Layer(CadLayers.B04);
        var b03 = doc.Layer(CadLayers.B03);
        var b06 = doc.Layer(CadLayers.B06);
        var b07 = doc.Layer(CadLayers.B07);
        var dimStyle = doc.DimensionStyles[CadDimStyles.FangSong1_50.Name];

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-69793.674, -23913.117);

        List<Entity> entities =

            // 源图有 1 个重复对象（同层同几何，仅句柄不同），只画一份
            // 本区域有 11 条横线 / 3 条竖线，但找不到四边闭合的外框，无法确定表格边界 → 保持线段+文字还原
            [
                .. TitleBlock.Add(doc, "Ⅰ-Ⅰ截面", "1:50", CadDraw.P(-91.114, 1738.293), 50),
                CadDraw.Polyline(
                    [
                        CadDraw.V(
                            -1400,
                            -900,
                            bulge: CadDraw.BulgeFromTangent(
                                (-1400.0 - (-1200.0)) / (-1100.0 - (-900.0))
                            )
                        ),
                        CadDraw.V(-1200, -1100),
                        CadDraw.V(-300, -1100),
                        CadDraw.V(-100, -900),
                        CadDraw.V(100, -900),
                        CadDraw.V(300, -1100),
                        CadDraw.V(
                            1200,
                            -1100,
                            bulge: CadDraw.BulgeFromTangent(
                                (1400.0 - 1200.0) / (-900.0 - (-1100.0))
                            )
                        ),
                        CadDraw.V(1400, -900),
                        CadDraw.V(
                            1400,
                            900,
                            bulge: CadDraw.BulgeFromTangent((1400.0 - 1200.0) / (1100.0 - 900.0))
                        ),
                        CadDraw.V(1200, 1100),
                        CadDraw.V(300, 1100),
                        CadDraw.V(100, 900),
                        CadDraw.V(-100, 900),
                        CadDraw.V(-300, 1100),
                        CadDraw.V(
                            -1200,
                            1100,
                            bulge: CadDraw.BulgeFromTangent(
                                (900.0 - 1100.0) / (-1400.0 - (-1200.0))
                            )
                        ),
                        CadDraw.V(-1400, 900),
                    ],
                    b01,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(1697.712, -1252.583), CadDraw.P(1405.941, -1079.66), b03),
                CadDraw.Line(CadDraw.P(-1450, 0), CadDraw.P(1450, 0), b06),
                CadDraw.Line(CadDraw.P(0, -950), CadDraw.P(0, 950), b06),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2481.184, -422.832),
                        CadDraw.V(-2481.184, 564.608),
                        CadDraw.V(-2639.992, 378.44),
                    ],
                    b07
                ),
                CadDraw.Polyline(
                    [
                        CadDraw.V(-395.922, -1838.451),
                        CadDraw.V(436.346, -1838.451),
                        CadDraw.V(250.177, -1679.643),
                    ],
                    b07
                ),
                CadDraw.Text(
                    "R200",
                    CadDraw.P(1460.327, -1094.195),
                    125,
                    b07,
                    5.699,
                    widthFactor: 0.75
                ),
                CadDraw.MText(
                    "顺\\P桥\\P向",
                    CadDraw.P(-2581.018, 301.152),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
                CadDraw.MText(
                    "横桥向",
                    CadDraw.P(-45.414, -1640.091),
                    125,
                    b07,
                    attachment: AttachmentPointType.TopCenter
                ),
            ];

        // 原生标注（skill §5.1）：与普通图元分列表；按源 rotation 升序分组，组内保持源顺序。
        // rotation 取源 DIMENSION 的角度，不是测量点连线方向（斜向标注常是水平/竖直投影）。
        List<Entity> dimensionEntities =
        [
            // rotation = 0（水平）· 5 段
            CadDraw.RotatedDimension(
                CadDraw.P(-300.087, -1100),
                CadDraw.P(299.913, -1100),
                CadDraw.P(299.913, -1404.556),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-100.087, -900),
                CadDraw.P(99.913, -900),
                CadDraw.P(99.913, -702.131),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-100.087, -900),
                CadDraw.P(-300.087, -1100),
                CadDraw.P(-300.087, -1227.442),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(99.913, -900),
                CadDraw.P(299.913, -1100),
                CadDraw.P(299.913, -1229.295),
                0,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1400, 683.78),
                CadDraw.P(1400, 549.54),
                CadDraw.P(1400, 1324.469),
                0,
                dimStyle,
                b04
            ),
            // rotation = 1.5708（竖直）· 2 段
            CadDraw.RotatedDimension(
                CadDraw.P(299.913, -1100),
                CadDraw.P(99.913, -900),
                CadDraw.P(534.748, -900),
                Math.PI / 2,
                dimStyle,
                b03
            ),
            CadDraw.RotatedDimension(
                CadDraw.P(-1298.302, 1100),
                CadDraw.P(-1298.302, -1100),
                CadDraw.P(-2017.569, -1100),
                Math.PI / 2,
                dimStyle,
                b03
            ),
        ];

        doc.AddTranslated(orgin, entities);
        doc.AddTranslated(orgin, dimensionEntities);
    }

    /// <summary>截图 01 / 区域 6：源图窗口 [1292, 454, 1515, 525]，1 个实体，6.40×2.12 m。</summary>
    public static void DrawRegion06(CadDocument doc)
    {
        var b07 = doc.Layer(CadLayers.B07);

        // 以该分区基准点为原点：图元按相对坐标书写，末尾统一平移到位
        var orgin = CadDraw.P(-61751.943, -23091.764);

        List<Entity> entities =
        [
            CadDraw.MText(
                "{\\fSimSun|b0|i0|c134|p2;\\H1.1429x;附注：\\P}1{\\fSimSun|b0|i0|c134|p2;、本图尺寸均以毫米计。\\P}2{\\fSimSun|b0|i0|c134|p2;、本图适用于}12#{\\fSimSun|b0|i0|c134|p2;～}15#{\\fSimSun|b0|i0|c134|p2;墩。\\P}3{\\fSimSun|b0|i0|c134|p2;、支承垫石采用}C50{\\fSimSun|b0|i0|c134|p2;混凝土，墩身采用}C40{\\fSimSun|b0|i0|c134|p2;混凝土，钢筋保护层厚度不小于}40mm{\\fSimSun|b0|i0|c134|p2;。\\P}4{\\fSimSun|b0|i0|c134|p2;、墩顶圆弧段与下部墩身直线段圆顺连接（相切）。\\P}5{\\fSimSun|b0|i0|c134|p2;、墩顶设不小于}3%{\\fSimSun|b0|i0|c134|p2;的排水坡。\\P}6{\\fSimSun|b0|i0|c134|p2;、基础仅为示意，承台尺寸，桩基根数详见《全桥总布置设计图》。}",
                CadDraw.P(0, 0),
                175,
                b07,
                attachment: AttachmentPointType.TopLeft
            ),
        ];

        doc.AddTranslated(orgin, entities);
    }
}
