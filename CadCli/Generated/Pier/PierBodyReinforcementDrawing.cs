using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Models;
using DwgSharpKit.Rebar;
using DwgSharpKit.Standards;

namespace CadCli.Generated.Pier;

public static class PierBodyReinforcementDrawing
{
    /// <summary>
    /// 90°圆弧对应的多段线凸度，计算方式为 tan(90° / 4)
    /// </summary>
    private static readonly double Bulge90Degrees = Math.Tan(Math.PI / 8);

    private sealed record EntityArrayPattern(
        Entity Template,
        double StepX,
        double StepY,
        int Count
    );

    private static void AddSymmetricArray(
        CadDocument doc,
        double x,
        double y,
        EntityArrayPattern pattern,
        double horizontalAxisY,
        bool includeHorizontalSymmetry = true
    )
    {
        IEnumerable<Entity> Templates(double stepX, double stepY) =>
            CadDraw.EntityArray(pattern.Template, CadDraw.P(stepX, stepY, 0), pattern.Count);

        doc.AddTransformed(CadDraw.P(x, y, 0), Templates(pattern.StepX, pattern.StepY));

        if (includeHorizontalSymmetry)
        {
            doc.AddTransformed(
                CadDraw.P(x, 2 * horizontalAxisY - y, 0),
                Templates(pattern.StepX, -pattern.StepY)
            );
        }

        doc.AddTransformed(CadDraw.P(-x, y, 0), Templates(-pattern.StepX, pattern.StepY));

        if (includeHorizontalSymmetry)
        {
            doc.AddTransformed(
                CadDraw.P(-x, 2 * horizontalAxisY - y, 0),
                Templates(-pattern.StepX, -pattern.StepY)
            );
        }
    }

    public static void Draw(CadDocument doc)
    {
        DrawPierBottomReinforcement(doc);
        DrawPierTopReinforcement(doc);
        DrawPierFrontReinforcement(doc);
        DrawPierSideReinforcement(doc);
    }

    public static void DrawPierTopReinforcement(CadDocument doc)
    {
        var outline = doc.Layer(CadLayers.B04);
        var center = doc.Layer(CadLayers.B06);

        const double steelSectionScale = 17.0 / 3.75;

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3050, 1100),
                        CadDraw.V(-3050, -1100, Bulge90Degrees),
                        CadDraw.V(-2850, -1300),
                        CadDraw.V(2850, -1300, Bulge90Degrees),
                        CadDraw.V(3050, -1100),
                        CadDraw.V(3050, 1100, Bulge90Degrees),
                        CadDraw.V(2850, 1300),
                        CadDraw.V(-2850, 1300, Bulge90Degrees),
                    ],
                    outline,
                    closed: true
                ),
                CadDraw.Line(
                    CadDraw.P(0, -1561.2686413871124),
                    CadDraw.P(0, 1561.2686413871124),
                    center
                ),
                CadDraw.Line(
                    CadDraw.P(-3311.2686413871124, 0),
                    CadDraw.P(3311.2686413871124, 0),
                    center
                ),
            ]
        );

        AddSymmetricArray(
            doc,
            -2978,
            100,
            new(SteelSection.Insert(doc, scale: steelSectionScale), 0, 100, 11),
            0
        );

        AddSymmetricArray(
            doc,
            -2940.5094778626226,
            1190.5096693702217,
            new(SteelSection.Insert(doc, scale: steelSectionScale), 0, 0, 1),
            0
        );

        foreach (
            var x in new[]
            {
                -2850,
                -2681,
                -2512,
                -2343,
                -2174,
                -2005,
                -1836,
                -1667,
                -1498,
                -1329,
                -1160,
                -991,
                -822,
                -680.008,
                -253.927,
            }
        )
        {
            AddSymmetricArray(
                doc,
                x,
                1228,
                new(SteelSection.Insert(doc, scale: steelSectionScale), 0, 0, 1),
                0
            );
        }
    }

    public static void DrawPierBottomReinforcement(CadDocument doc)
    {
        var outline = doc.Layer(CadLayers.B04);
        var center = doc.Layer(CadLayers.B06);

        const double steelSectionScale = 17.0 / 3.75;

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-1750, 900),
                        CadDraw.V(-1750, -900, Bulge90Degrees),
                        CadDraw.V(-1550, -1100),
                        CadDraw.V(1550, -1100, Bulge90Degrees),
                        CadDraw.V(1750, -900),
                        CadDraw.V(1750, 900, Bulge90Degrees),
                        CadDraw.V(1550, 1100),
                        CadDraw.V(-1550, 1100, Bulge90Degrees),
                    ],
                    outline,
                    closed: true
                ),
                CadDraw.Line(
                    CadDraw.P(-2014.057555647567, 0),
                    CadDraw.P(2014.057555647567, 0),
                    center
                ),
                CadDraw.Line(
                    CadDraw.P(0, -1276.6332331476733),
                    CadDraw.P(0, 1276.6332331476733),
                    center
                ),
            ]
        );

        AddSymmetricArray(
            doc,
            -1678,
            100,
            new(SteelSection.Insert(doc, scale: steelSectionScale), 0, 100, 9),
            0
        );

        AddSymmetricArray(
            doc,
            -1640.509668,
            990.509668,
            new(SteelSection.Insert(doc, scale: steelSectionScale), 0, 0, 1),
            0
        );

        foreach (
            var x in new[]
            {
                -1550,
                -1450.0,
                -1350.0,
                -1250.0,
                -1150.0,
                -1050.0,
                -950.0,
                -850.0,
                -750.0,
                -650.0,
                -550.0,
                -450.0,
                -350.0,
                -197.0,
                -62.0,
            }
        )
        {
            AddSymmetricArray(
                doc,
                x,
                1028,
                new(SteelSection.Insert(doc, scale: steelSectionScale), 0, 0, 1),
                0
            );
        }
    }

    public static void DrawPierSideReinforcement(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var centerLayer = doc.Layer(CadLayers.B06);

        var arcBulge = CadDraw.BulgeFromTangent((2600.0 - 2200.0) / 2.0 / 4500.0);
        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-2200 / 2, 0),
                        CadDraw.V(-2200 / 2, 5000, bulge: arcBulge),
                        CadDraw.V(-2600 / 2, 9500),
                        CadDraw.V(-2600 / 2, 10000),
                        CadDraw.V(0, 10000),
                        CadDraw.V(2600 / 2, 10000),
                        CadDraw.V(2600 / 2, 9500, bulge: arcBulge),
                        CadDraw.V(2200 / 2, 5000),
                        CadDraw.V(2200 / 2, 0),
                    ],
                    outlineLayer
                ),
                CadDraw.Line(CadDraw.P(0, -500), CadDraw.P(0, 10500), centerLayer),
            ]
        );

        TitleBlock.Add(doc, "侧面钢筋布置图", "", CadDraw.P(0, 11500), 10);

        var rebarLayer = doc.Layer(CadLayers.B01);
        var options = new RebarDetailOptions
        {
            ShowText = false,
            RebarLayer = rebarLayer,
            TextLayer = doc.Layer(CadLayers.B07),
        };

        var number = 1;
        var mirrorX = new Transform();
        mirrorX.ApplyScale(new XYZ(-1, 1, 1));

        var snBottomX = new[]
        {
            -1028.0,
            -990.5,
            -900.0,
            -800.0,
            -700.0,
            -600.0,
            -500.0,
            -400.0,
            -300.0,
            -200.0,
            -100.0,
        };
        var snTopX = new[]
        {
            -1228.0,
            -1190.5,
            -1100.0,
            -1000.0,
            -900.0,
            -800.0,
            -700.0,
            -600.0,
            -500.0,
            -400.0,
            -300.0,
        };

        // N1~N2 + N4~N11: 标准弧形 / 直段钢筋
        var sn = new List<Rebar>();

        for (var i = 0; i < snBottomX.Length; i++)
        {
            var r = new Rebar
            {
                Number = $"N1_{number++}",
                Grade = "HRB400",
                Diameter = 12,
                Count = 1,
                Vertices =
                [
                    RebarDetail.V(0, -1400),
                    RebarDetail.V(
                        0,
                        5000,
                        bulge: CadDraw.BulgeFromTangent(
                            (Math.Abs(snTopX[i]) - Math.Abs(snBottomX[i])) / 4500.0
                        )
                    ),
                    RebarDetail.V(snTopX[i], 9500),
                    RebarDetail.V(snTopX[i], 9900),
                ],
            };
            r.Place(doc, CadDraw.P(snBottomX[i], 0), options);
            r.Place(doc, CadDraw.P(-snBottomX[i], 0), options, mirrorX);
            sn.Add(r);
        }

        // N3: -900 处特殊弯折钢筋
        var sn3 = new Rebar
        {
            Number = $"N{number++}",
            Grade = "HRB400",
            Diameter = 12,
            Count = 1,
            Vertices =
            [
                RebarDetail.V(0, -1400),
                RebarDetail.V(0, 5000, bulge: -CadDraw.BulgeFromTangent(200.0 / 4500.0)),
                RebarDetail.V(-200, 9500),
                RebarDetail.V(-200, 9900),
            ],
        };
        sn3.Place(doc, CadDraw.P(-900, 0), options);
        sn3.Place(doc, CadDraw.P(900, 0), options, mirrorX);

        // N13: 中心竖向钢筋
        var snCenter = new Rebar
        {
            Number = $"N{number++}",
            Grade = "HRB400",
            Diameter = 12,
            Count = 1,
            Vertices = [RebarDetail.V(0, -1400), RebarDetail.V(0, 9900)],
        };
        snCenter.Place(doc, CadDraw.P(0, 0), options);
    }

    public static void DrawPierFrontReinforcement(CadDocument doc)
    {
        var outlineLayer = doc.Layer(CadLayers.B01);
        var center = doc.Layer(CadLayers.B06);
        var mirrorX = new Transform();
        mirrorX.ApplyScale(new XYZ(-1, 1, 1));

        // cad-region.json 中两段圆弧的圆心角均为 0.562464 弧度。
        // 按轮廓自右下向右上、再向左下的走向连接，故凸度取负值。
        var arcBulge = CadDraw.BulgeFromTangent((6100.0 - 3500.0) / 2.0 / 4500.0);

        doc.AddTranslated(
            CadDraw.P(0, 0, 0),
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(-3500 / 2, 0),
                        CadDraw.V(-3500 / 2, 5000, bulge: arcBulge),
                        CadDraw.V(-6100 / 2, 9500),
                        CadDraw.V(-6100 / 2, 10000),
                        CadDraw.V(0, 10000),
                        CadDraw.V(6100 / 2, 10000),
                        CadDraw.V(6100 / 2, 9500, bulge: arcBulge),
                        CadDraw.V(3500 / 2, 5000),
                        CadDraw.V(3500 / 2, 0),
                    ],
                    outlineLayer
                ),
                CadDraw.Line(CadDraw.P(0, -500), CadDraw.P(0, 10500), center),
            ]
        );

        TitleBlock.Add(doc, "正面钢筋布置图", "", CadDraw.P(0, 11500), 10);

        var options = new RebarDetailOptions
        {
            RebarLayer = doc.Layer(CadLayers.B01),
            TextLayer = doc.Layer(CadLayers.B07),
        };
        var origin = CadDraw.P(0, 0, 0);
        var leaderAnchor = CadDraw.P(0, -100, 0);

        // N1~N13: 左侧壁钢筋
        var nTopX = new[]
        {
            -2978.0,
            -2940.5,
            -2850.0,
            -2681.0,
            -2512.0,
            -2343.0,
            -2174.0,
            -2005.0,
            -1836.0,
            -1667.0,
            -1498.0,
            -1329.0,
            -1160.0,
            -991.0,
            -822.0,
        };
        var nBottomX = new[]
        {
            -1678.0,
            -1640.5,
            -1550.0,
            -1450.0,
            -1350.0,
            -1250.0,
            -1150.0,
            -1050.0,
            -950.0,
            -850.0,
            -750.0,
            -650.0,
            -550.0,
            -450.0,
            -350.0,
        };

        var left = new List<Rebar>();
        for (var i = 0; i < nTopX.Length; i++)
        {
            var r = new Rebar
            {
                Number = $"N{i + 1}",
                Diameter = 12,
                Count = 1,
                Vertices =
                [
                    RebarDetail.V(0, -1400),
                    RebarDetail.V(
                        0,
                        5000,
                        bulge: CadDraw.BulgeFromTangent(
                            (Math.Abs(nTopX[i]) - Math.Abs(nBottomX[i])) / 4500
                        )
                    ),
                    RebarDetail.V(nTopX[i] - nBottomX[i], 9500),
                    RebarDetail.V(nTopX[i] - nBottomX[i], 9900),
                ],
            };
            r.Place(doc, CadDraw.P(nBottomX[i], 0));
            r.Place(doc, CadDraw.P(-nBottomX[i], 0), transform: mirrorX);
            left.Add(r);
        }
    }
}
