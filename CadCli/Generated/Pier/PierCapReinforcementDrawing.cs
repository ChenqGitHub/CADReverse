using ACadSharp;
using ACadSharp.Entities;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Standards;

namespace CadCli.Generated.Pier;

public static class PierCapReinforcementDrawing
{
    /// <summary>
    /// 90°圆弧对应的多段线凸度，计算方式为 tan(90° / 4)
    /// </summary>
    private static readonly double Bulge90Degrees = Math.Tan(Math.PI / 8);

    public static void Draw(CadDocument doc)
    {
        DrawCapFrontReinforcement(doc);
        DrawCapSideReinforcement(doc);
        DrawCapTopReinforcement(doc);
        DrawCapBottomReinforcement(doc);
    }

    public static void DrawCapFrontReinforcement(CadDocument doc)
    {
        var centerLayer = doc.Layer(CadLayers.B06);
        var rebarLayer = doc.Layer(CadLayers.B01);
        const double steelSectionScale = 17.0 / 3.75;
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 按轮廓自右下向右上、再向左下的走向连接，故凸度取负值。
        var arcBulge = CadDraw.BulgeFromTangent((6100.0 - 3500.0) / 2.0 / 4500.0);

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3500 / 2, -5000, bulge: arcBulge),
                        CadDraw.V(-6100 / 2, -500),
                        CadDraw.V(-6100 / 2, 0),
                        CadDraw.V(0, 0),
                        CadDraw.V(6100 / 2, 0),
                        CadDraw.V(6100 / 2, -500, bulge: arcBulge),
                        CadDraw.V(3500 / 2, -5000),
                    ],
                    outlineLayer,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(0, -500), CadDraw.P(0, 10500), center),
            ]
        );

        void AddSteelSection(double x, double y) =>
            doc.Entities.Add(SteelSection.Insert(doc, CadDraw.P(x, y), scale: steelSectionScale));

        var rows12 = new[]
        {
            -2078d,
            -1878,
            -1678,
            -1478,
            -1278,
            -1078,
            -878,
            -678,
            -478,
            -356,
            -228,
            -100,
        };
        var columns12 = new[] { -150d, -450, -644, -944, -1244, -1544, -1844, -2144 };
        foreach (var x in columns12)
        {
            foreach (var y in rows12)
            {
                AddSteelSection(x, y);
            }
        }

        var rows4 = new[] { -878d, -678, -478, -356 };
        var columns4 = new[] { -300d, -794, -1094, -1394, -1694, -1994, -2294, -2594 };
        foreach (var x in columns4)
        {
            foreach (var y in rows4)
            {
                AddSteelSection(x, y);
            }
        }

        foreach (var y in new[] { -1278d, -1078, -878, -678, -478, -356, -228, -100 })
        {
            AddSteelSection(-2444, y);
        }

        foreach (var y in rows4)
        {
            AddSteelSection(-2744, y);
        }

        var diagonalX = new[]
        {
            (-2852.390686, -750d),
            (-2766.986433, -900),
            (-2685.576596, -1050),
            (-2608.031696, -1200),
            (-2534.233555, -1350),
            (-2464.074139, -1500),
            (-2397.454563, -1650),
            (-2334.284219, -1800),
            (-2274.480027, -1950),
            (-2217.965777, -2100),
        };
        foreach (var (x, y) in diagonalX)
        {
            AddSteelSection(x, y);
        }

        foreach (var y in new[] { -478d, -356 })
        {
            AddSteelSection(-2861, y);
        }

        foreach (var y in new[] { -450d, -350, -225, -100 })
        {
            AddSteelSection(-3004, y);
        }

        var polylines = new List<Line>
        {
            CadDraw.Line(CadDraw.P(2978, -378), CadDraw.P(-2978, -378), rebarLayer),
            CadDraw.Line(CadDraw.P(2978, -250), CadDraw.P(-499.86, -250), rebarLayer),
            CadDraw.Line(CadDraw.P(-2978, -250), CadDraw.P(-499.86, -250), rebarLayer),
            CadDraw.Line(CadDraw.P(2978, -500), CadDraw.P(2978, -100), rebarLayer),
            CadDraw.Line(CadDraw.P(-2978, -500), CadDraw.P(-2978, -100), rebarLayer),
        };

        foreach (var x in new[] { -2462d, -2762, -2162, -1862, -1562, -1262, -962 })
        {
            polylines.Add(CadDraw.Line(CadDraw.P(x, -122), CadDraw.P(x, -2150), rebarLayer));
        }
        foreach (var x in new[] { 2462d, 2762, 2162, 1862, 1562, 1262, 962 })
        {
            polylines.Add(CadDraw.Line(CadDraw.P(x, -122), CadDraw.P(x, -2150), rebarLayer));
        }

        foreach (var x in new[] { -662d, -468.022, -168.022, 662, 468.022, 168.022 })
        {
            polylines.Add(CadDraw.Line(CadDraw.P(x, -122), CadDraw.P(x, -2150), rebarLayer));
        }

        foreach (
            var (start, end) in new[]
            {
                (CadDraw.P(-627.907, -122), CadDraw.P(2978, -122)),
                (CadDraw.P(-627.907, -122), CadDraw.P(-2978, -122)),
                (CadDraw.P(-0.0, -500), CadDraw.P(-2978, -500)),
                (CadDraw.P(2978, -500), CadDraw.P(0, -500)),
                (CadDraw.P(-2191.966, -2100), CadDraw.P(2191.966, -2100)),
                (CadDraw.P(-2740.986, -900), CadDraw.P(2740.986, -900)),
                (CadDraw.P(-2855.771, -700), CadDraw.P(2855.771, -700)),
                (CadDraw.P(-2268.046, -1900), CadDraw.P(2268.046, -1900)),
                (CadDraw.P(-2350.019, -1700), CadDraw.P(2350.019, -1700)),
                (CadDraw.P(-2438.074, -1500), CadDraw.P(2438.074, -1500)),
                (CadDraw.P(-2532.423, -1300), CadDraw.P(2532.423, -1300)),
                (CadDraw.P(-2633.305, -1100), CadDraw.P(2633.305, -1100)),
            }
        )
        {
            polylines.Add(CadDraw.Line(start, end, rebarLayer));
        }

        doc.Entities.AddRange(polylines);
        doc.Entities.AddRange(
            [
                CadDraw.Arc(
                    CadDraw.P(10116.461538, -5000),
                    8438.461538,
                    2.579128249757386,
                    2.797078638416907,
                    rebarLayer
                ),
                CadDraw.Arc(
                    CadDraw.P(-10116.461538, -5000),
                    8438.461538,
                    0.3445140151728855,
                    0.5624644038370399,
                    rebarLayer
                ),
            ]
        );

        TitleBlock.Add(doc, "顶帽正面钢筋图", "", CadDraw.P(0, 11500), 10);
    }

    public static void DrawCapSideReinforcement(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 将扫描坐标的微小浮点误差归并到公共端点，形成严格闭合轮廓。
        var arcBulge = CadDraw.BulgeFromTangent((2600.0 - 2200.0) / 2.0 / 4500.0);

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2200 / 2, -5000, bulge: arcBulge),
                        CadDraw.V(-2600 / 2, -500),
                        CadDraw.V(-2600 / 2, 0),
                        CadDraw.V(0, 0),
                        CadDraw.V(2600 / 2, 0),
                        CadDraw.V(2600 / 2, -500, bulge: arcBulge),
                        CadDraw.V(2200 / 2, -5000),
                    ],
                    outlineLayer,
                    closed: true
                ),
                CadDraw.Line(CadDraw.P(0, -500), CadDraw.P(0, 10500), center),
            ]
        );
        var rebarLayer = doc.Layer(CadLayers.B01);
        const double steelSectionScale = 17.0 / 3.75;

        void AddSteelSection(double x, double y) =>
            doc.Entities.Add(SteelSection.Insert(doc, CadDraw.P(x, y), scale: steelSectionScale));

        var rows12 = new[]
        {
            -2100d,
            -1900,
            -1700,
            -1500,
            -1300,
            -1100,
            -900,
            -700,
            -500,
            -378,
            -250,
            -122,
        };
        foreach (var x in new[] { -154d, -462, -770 })
        {
            foreach (var y in rows12)
            {
                AddSteelSection(x, y);
            }
        }

        foreach (var x in new[] { -924d, -1252, -616, -308 })
        {
            foreach (var y in new[] { -900d, -700, -500, -378 })
            {
                AddSteelSection(x, y);
            }
        }

        foreach (var y in new[] { -1100d, -900, -700, -500, -378, -250, -122 })
        {
            AddSteelSection(-1078, y);
        }

        var diagonal = new[]
        {
            (-1235d, -600d),
            (-1208, -750),
            (-1184, -900),
            (-1160, -1050),
            (-1137, -1200),
            (-1114, -1350),
            (-1093, -1500),
            (-1073, -1650),
            (-1053, -1800),
            (-1035, -1950),
            (-1017, -2100),
        };
        foreach (var (x, y) in diagonal)
        {
            AddSteelSection(x, y);
        }

        var paths = new List<LwPolyline>
        {
            CadDraw.Polyline([CadDraw.P2(-1228, -100), CadDraw.P2(1228, -100)], rebarLayer),
            CadDraw.Polyline([CadDraw.P2(-1228, -478), CadDraw.P2(1228, -478)], rebarLayer),
            CadDraw.Polyline([CadDraw.P2(-1163.193, -878), CadDraw.P2(1163.193, -878)], rebarLayer),
            CadDraw.Polyline([CadDraw.P2(-1196.754, -678), CadDraw.P2(1196.754, -678)], rebarLayer),
            CadDraw.Polyline([CadDraw.P2(-995.884, -2078), CadDraw.P2(995.884, -2078)], rebarLayer),
        };

        foreach (
            var (y, halfWidth) in new[]
            {
                (-1278d, 1100.96),
                (-1478, 1072.275),
                (-1678, 1045.205),
                (-1878, 1019.742),
                (-1078, 1131.264),
                (-356, 1228),
                (-228, 1228),
            }
        )
        {
            paths.Add(
                CadDraw.Polyline(
                    [CadDraw.P2(-halfWidth, y), CadDraw.P2(0, y), CadDraw.P2(halfWidth, y)],
                    rebarLayer
                )
            );
        }

        paths.Add(
            CadDraw.Polyline([CadDraw.P2(1096, -100), CadDraw.P2(1096, -1311.792)], rebarLayer)
        );
        paths.Add(
            CadDraw.Polyline([CadDraw.P2(-1096, -100), CadDraw.P2(-1096, -1311.792)], rebarLayer)
        );
        paths.Add(CadDraw.Polyline([CadDraw.P2(480, -100), CadDraw.P2(480, -2172)], rebarLayer));
        paths.Add(CadDraw.Polyline([CadDraw.P2(-480, -100), CadDraw.P2(-480, -2172)], rebarLayer));
        paths.Add(CadDraw.Polyline([CadDraw.P2(1228, -100), CadDraw.P2(1228, -500)], rebarLayer));
        paths.Add(CadDraw.Polyline([CadDraw.P2(-1228, -100), CadDraw.P2(-1228, -500)], rebarLayer));

        doc.Entities.AddRange(paths);
        doc.Entities.AddRange(
            [
                CadDraw.Arc(
                    CadDraw.P(26340.5, -5000),
                    25512.5,
                    2.9642808898550026,
                    3.0298922931279426,
                    rebarLayer
                ),
                CadDraw.Arc(
                    CadDraw.P(-26340.5, -5000),
                    25512.5,
                    0.11170036046185071,
                    0.17731176373479046,
                    rebarLayer
                ),
            ]
        );

        TitleBlock.Add(doc, "顶帽侧面钢筋图", "", CadDraw.P(0, 11500), 10);
    }

    public static void DrawCapTopReinforcement(CadDocument doc)
    {
        var outline = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var arcBulge = CadDraw.BulgeFromTangent((2600.0 - 2200.0) / 2.0 / 4500.0);

        doc.AddTranslated(
            new CSMath.XYZ(),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2200 / 2, 5000, bulge: arcBulge),
                        CadDraw.V(-2600 / 2, 9500),
                        CadDraw.V(-2600 / 2, 10000),
                        CadDraw.V(0, 10000),
                        CadDraw.V(2600 / 2, 10000),
                        CadDraw.V(2600 / 2, 9500, bulge: arcBulge),
                    ],
                    outline
                ),
            ]
        );

        var paths = new List<Line> { };

        var horizontalY = new[]
        {
            -1045.977d,
            -924,
            -770,
            -616,
            -462,
            -308,
            -154,
            154,
            308,
            462,
            616,
            770,
            924,
        };
        foreach (var y in horizontalY)
        {
            paths.Add(CadDraw.Line(CadDraw.P(-2451.9, y), CadDraw.P(2451.9, y), outline));
        }

        foreach (
            var x in new[]
            {
                -2144.0,
                -2294,
                -1994,
                -1844,
                -1694,
                -1544,
                -1394,
                -1244,
                -1094,
                -794,
                -944,
                -644.022,
                -450.022,
                -300.022d,
                -150.022,
                0.022,
            }
        )
        {
            paths.Add(CadDraw.Line(CadDraw.P(x, -1251.9), CadDraw.P(x, 1251.9), outline));
            paths.Add(CadDraw.Line(CadDraw.P(-x, -1251.9), CadDraw.P(-x, 1251.9), outline));
        }

        paths.Add(CadDraw.Line(CadDraw.P(0, -1540.226), CadDraw.P(0, 1544.215), center));
        paths.Add(CadDraw.Line(CadDraw.P(-2869.813, 0), CadDraw.P(2631.533, 0), center));

        doc.Entities.AddRange(paths);

        TitleBlock.Add(doc, "顶帽顶面钢筋图", "", CadDraw.P(0, 11500), 10);
    }

    public static void DrawCapBottomReinforcement(CadDocument doc)
    {
        var outline = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var paths = new List<Entity>
        {
            CadDraw.Polyline(
                [
                    CadDraw.V(2215.866, 865.357),
                    CadDraw.V(2215.866, 0),
                    CadDraw.V(2215.866, -865.357, -0.41421356237309503),
                    CadDraw.V(2063.966, -1017.257),
                    CadDraw.V(-504.83934727595204, -1017.2878833804934),
                    CadDraw.V(-2063.966, -1017.257, -0.41421356237309503),
                    CadDraw.V(-2215.866, -865.357),
                    CadDraw.V(-2215.866, 0),
                    CadDraw.V(-2215.866, 865.357, -0.41421356237309503),
                    CadDraw.V(-2063.966, 1017.257),
                    CadDraw.V(-504.83934727595204, 1017.2878833804934),
                    CadDraw.V(2063.966, 1017.257, -0.41421356237309503),
                ],
                outline,
                closed: true
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(780.0305166954931, -1065.357),
                    CadDraw.V(-780.0305141633144, -1065.357),
                    CadDraw.V(-2063.966, -1065.357, -0.41421356237309503),
                    CadDraw.V(-2263.966, -865.357),
                    CadDraw.V(-2263.966, 865.357, -0.41421356237309503),
                    CadDraw.V(-2063.966, 1065.357),
                    CadDraw.V(-780.0305141633144, 1065.357),
                    CadDraw.V(800, 1065.357),
                    CadDraw.V(2063.966, 1065.357, -0.41421356237309503),
                    CadDraw.V(2263.966, 865.357),
                    CadDraw.V(2263.966, -865.357, -0.41421356237309503),
                    CadDraw.V(2063.966, -1065.357),
                    CadDraw.V(731.8614719069737, -1065.357),
                ],
                outline
            ),
        };

        foreach (
            var line in new[]
            {
                (468.0224725631779, 998.9966439036361, -479.99999687826494),
                (132.00749085457937, -998.9966439036361, 480.00000312170596),
                (661.9999999999491, 998.9959308110119, -479.9999968782795),
                (625.9999999998981, -998.9966439036361, 480.00000312170596),
                (961.9999999999491, 998.9959308110119, -479.9999968782795),
                (925.9999999998981, -998.9966439036361, 480.00000312170596),
                (1261.999999999949, 998.9959308110119, -479.9999968782795),
                (1225.9999999998981, -998.9966439036361, 480.00000312170596),
                (1561.999999999949, 998.9959308110119, -479.9999968782795),
                (1525.9999999998981, -998.9966439036361, 480.00000312170596),
                (1861.999999999949, 998.9959308110119, -479.9999968782795),
                (1825.9999999998981, -998.9966439036361, 480.00000312170596),
                (2161.999999999949, 998.9959308110119, -479.9999968782795),
                (2125.999999999898, -1021.6216628631228, 480.00000312170596),
                (168.00749085457937, -479.99999687826494, 1034.89634263661),
                (432.0224725637929, -998.9966439036361, 480.00000312170596),
                (-468.0224725631567, 1034.89634263661, -479.99999687826494),
                (-132.00749085457937, -998.9966439036361, 480.00000312170596),
                (-661.9999999999491, 998.9959308110119, -479.9999968782795),
                (-625.9999999998981, -998.9966439036361, 480.00000312170596),
                (-961.9999999999491, 998.9959308110119, -479.9999968782795),
                (-925.9999999998981, -998.9966439036361, 480.00000312170596),
                (-1261.999999999949, 998.9959308110119, -479.9999968782795),
                (-1225.9999999998981, -998.9966439036361, 480.00000312170596),
                (-1561.999999999949, 998.9959308110119, -479.9999968782795),
                (-1525.9999999998981, -998.9966439036361, 480.00000312170596),
                (-1861.999999999949, 998.9959308110119, -479.9999968782795),
                (-1825.9999999998981, -998.9966439036361, 480.00000312170596),
                (2144.000009732037, 1012.0737566172756, -1012.0737566172756),
                (-2161.999999999949, 998.9959308110119, -479.9999968782795),
                (-2125.999999999898, -1021.6216628631228, 480.00000312170596),
                (-2143.999999999949, 1012.0712626503082, -1012.0712626503082),
                (-168.00749085457937, -479.99999687826494, 1034.89634263661),
                (-432.0224725637929, -998.9966439036361, 480.00000312170596),
                (1844.0000097320371, 1034.8701219152717, -1034.8701219152717),
                (1544.0000097320371, 1034.8766770955554, -1034.8766770955554),
                (1244.0000097320371, 1034.8832322758244, -1034.8832322758244),
                (944.0000097320371, 1034.889787456108, -1034.889787456108),
                (644.0000000000509, 1034.89634263661, -1034.89634263661),
                (450.0224725639491, 1034.89634263661, -1034.89634263661),
                (148.99253381847666, 828.9426235091669, -1034.89634263661),
                (-1843.999999999949, 1034.8701219154755, -1034.8701219154755),
                (-1543.999999999949, 1034.876677095759, -1034.876677095759),
                (-1243.999999999949, 1034.8832322760427, -1034.8832322760427),
                (-943.9999999999491, 1034.8897874563263, -1034.8897874563263),
                (-643.9999999999491, 1034.89634263661, -1034.89634263661),
                (-450.02247256384726, 980.082508668449, -1034.89634263661),
                (-150.00749085458665, 828.9426235091669, -1034.89634263661),
            }
        )
        {
            paths.Add(
                CadDraw.Line(
                    CadDraw.P(line.Item1, line.Item2),
                    CadDraw.P(line.Item1, line.Item3),
                    outline
                )
            );
        }

        foreach (
            var y in new[]
            {
                -769.9999968782795d,
                -461.9999968782795,
                -153.99999687829404,
                154.00000312170596,
                462.00000312170596,
                770.000003121706,
            }
        )
        {
            paths.Add(CadDraw.Line(CadDraw.P(-2215.866, y), CadDraw.P(2215.866, y), outline));
        }

        paths.Add(CadDraw.Line(CadDraw.P(0, -915.357), CadDraw.P(0, 915.357), center));
        doc.Entities.AddRange(paths);

        TitleBlock.Add(doc, "顶帽底面钢筋图", "", CadDraw.P(0, 11500), 10);
    }
}
