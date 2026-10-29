using ACadSharp;
using ACadSharp.Entities;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Drawing;
using DwgSharpKit.Rebar;
using DwgSharpKit.Standards;
using DwgSharpKit.Tables;

namespace CadCli.Generated.Pier;

public static class BearingPedestalDrawing
{
    public static void Draw(CadDocument doc)
    {
        var n1 = new Rebar()
        {
            Number = "N1",
            Count = 2,
            Diameter = 16,
            Vertices = [],
        };

        var n2 = new Rebar()
        {
            Number = "N2",
            Count = 2,
            Diameter = 16,
            Vertices = [],
        };
        var n3 = new Rebar()
        {
            Number = "N3",
            Count = 2,
            Diameter = 16,
            Vertices = [],
        };
        var n4 = new Rebar()
        {
            Number = "N4",
            Count = 2,
            Diameter = 16,
            Vertices = [],
        };
        var n5 = new Rebar()
        {
            Number = "N5",
            Count = 2,
            Diameter = 16,
            Vertices = [],
        };

        DrawBearingPedestalReinforcementElevation(doc);
        DrawBearingPedestalReinforcementSide(doc);
        DrawBearingPedestalReinforcementSection(doc);

        // 工程数量表
        var frameRebarData = new List<string[]>
        {
            new[] { "编号", "直径", "根数", "每根长", "总长", "单位重量", "总重量" },
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
            new[] { "每延米框架身HRB400钢筋合计重(kg)", "", "", "", "", "", "14412.24" },
            new[] { "每延米框架身HPB300钢筋合计重(kg)", "", "", "", "", "", "2211.28" },
            new[] { "每延米框架C40混凝土(m³)", "", "", "", "", "", "76.69" },
        };
        doc.AddEntities(
            TitleBlock.Add(doc, "框架身钢筋数量表（每延米）", "", CadDraw.P(-12000, -59500), 20)
        );
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
        doc.Entities.Add(frameRebarTable);
    }

    private static void DrawBearingPedestalReinforcementElevation(CadDocument doc)
    {
        var fineLine = doc.Layer(CadLayers.B04);
        var rebar = doc.Layer(CadLayers.B01);
        var origin = CadDraw.P(0, 0);

        // 断面钢筋
        foreach (var y in new[] { -856d, -616, -376, -136 })
        {
            doc.AddTranslated(
                origin,
                CadDraw
                    .EntityArray(
                        SteelSection.Insert(doc, CadDraw.P(300, y)),
                        new CSMath.XYZ(200, 0, 0),
                        20
                    )
                    .Concat(
                        [
                            SteelSection.Insert(doc, CadDraw.P(136, y)),
                            SteelSection.Insert(doc, CadDraw.P(4264, y)),
                        ]
                    )
            );
        }

        // 横筋
        doc.AddTranslated(
            origin,
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(548, -70), CadDraw.P(548, -856), rebar),
                new CSMath.XYZ(0, -200, 0),
                3
            )
        );

        // 竖筋
        doc.AddTranslated(
            origin,
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(1148, -70), CadDraw.P(1148, -856), rebar),
                new CSMath.XYZ(200, 0, 0),
                16
            )
        );

        // 横筋
        doc.AddTranslated(
            origin,
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(70, -328), CadDraw.P(4330, -328), rebar),
                new CSMath.XYZ(0, -240, 0),
                3
            )
        );

        // 框筋
        CadDraw.Polyline(
            [
                CadDraw.P2(88.00000000046566, -1288.0000000009313),
                CadDraw.P2(88, -88),
                CadDraw.P2(4312, -88),
                CadDraw.P2(4312, -1288.0000000009313),
            ],
            rebar
        );

        // 构造
        doc.AddTranslated(
            origin,
            [
                CadDraw.Polyline(
                    [
                        CadDraw.P2(-950, -583.4210470784456),
                        CadDraw.P2(0, 0),
                        CadDraw.P2(0, 915.357),
                        CadDraw.P2(4400, 0),
                        CadDraw.P2(4400, -583.4210470784456),
                        CadDraw.P2(5350, -583.4210470784456),
                    ],
                    fineLine
                ),
            ]
        );
    }

    private static void DrawBearingPedestalReinforcementSide(CadDocument doc)
    {
        var rebar = doc.Layer(CadLayers.B01);
        var fineLine = doc.Layer(CadLayers.B04);
        var origin = CadDraw.P(0, 0);
        var entities = new List<Entity>();

        // 断面钢筋
        foreach (
            var y in new[]
            {
                -785.2751735961065d,
                -545.2751735961065,
                -305.27517359610647,
                -73.66008213255554,
            }
        )
        {
            doc.AddTranslated(
                origin,
                CadDraw
                    .EntityArray(
                        SteelSection.Insert(doc, CadDraw.P(300, y)),
                        new CSMath.XYZ(200, 0, 0),
                        8
                    )
                    .Concat(
                        [
                            SteelSection.Insert(doc, CadDraw.P(136, y)),
                            SteelSection.Insert(doc, CadDraw.P(1864, y)),
                        ]
                    )
            );
        }

        // 竖筋
        doc.AddTranslated(
            origin,
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(348, -70), CadDraw.P(348, -865.962938753888), rebar),
                new CSMath.XYZ(200, 0, 0),
                8
            )
        );

        // 横筋
        doc.AddTranslated(
            origin,
            CadDraw.EntityArray(
                CadDraw.Line(CadDraw.P(70, -328), CadDraw.P(1930, -328), rebar),
                new CSMath.XYZ(0, -240, 0),
                3
            )
        );

        // 框筋
        CadDraw.Polyline(
            [
                CadDraw.P2(88, -1288.0000000009313),
                CadDraw.P2(88, -121.66008213255554),
                CadDraw.P2(1912, -121.66008213255554),
                CadDraw.P2(1912, -1288.0000000009313),
            ],
            fineLine
        );

        // 构造
        doc.AddTranslated(
            origin,
            [
                CadDraw.Polyline(
                    [
                        CadDraw.P2(-950, -1327.9297719020396),
                        CadDraw.P2(-950, -583.4210470784456),
                        CadDraw.P2(0, -583.4210470784456),
                        CadDraw.P2(0, 0),
                        CadDraw.P2(2000, 0),
                        CadDraw.P2(2000, -583.4210470784456),
                        CadDraw.P2(2950, -583.4210470784456),
                        CadDraw.P2(2950, -1327.9297719020396),
                    ],
                    fineLine
                ),
            ]
        );
    }

    private static void DrawBearingPedestalReinforcementSection(CadDocument doc)
    {
        var rebar = doc.Layer(CadLayers.B01);
        var fineLine = doc.Layer(CadLayers.B04);
        var origin = CadDraw.P(0, 0);

        // 纵筋
        doc.AddTranslated(
            origin,
            CadDraw
                .EntityArray(
                    CadDraw.Line(CadDraw.P(300, -1930), CadDraw.P(300, -70), rebar),
                    new CSMath.XYZ(200, 0, 0),
                    20
                )
                .Concat(
                    [
                        CadDraw.Line(CadDraw.P(136, -1930), CadDraw.P(136, -70), rebar),
                        CadDraw.Line(CadDraw.P(4264, -1930), CadDraw.P(4264, -70), rebar),
                    ]
                )
        );

        // 横筋
        doc.AddTranslated(
            origin,
            CadDraw
                .EntityArray(
                    CadDraw.Line(CadDraw.P(4330, -300), CadDraw.P(70, -300), rebar),
                    new CSMath.XYZ(0, -200, 0),
                    8
                )
                .Concat(
                    [
                        CadDraw.Line(CadDraw.P(4330, -136), CadDraw.P(70, -136), rebar),
                        CadDraw.Line(CadDraw.P(4330, -1864), CadDraw.P(70, -1864), rebar),
                    ]
                )
        );

        // 构造
        doc.AddTranslated(
            origin,
            [
                CadDraw.Polyline(
                    [
                        CadDraw.P2(0, -2000),
                        CadDraw.P2(4400, -2000),
                        CadDraw.P2(4400, 0),
                        CadDraw.P2(0, 0),
                    ],
                    fineLine,
                    closed: true
                ),
            ]
        );
    }
}
