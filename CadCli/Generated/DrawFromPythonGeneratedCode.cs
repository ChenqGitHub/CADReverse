using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Extensions;
using CSMath;
using DwgSharpKit;
using DwgSharpKit.Blocks;
using DwgSharpKit.Rebar;
using DwgSharpKit.Standards;

namespace CadCli.Generated;

public static partial class GeneratedDraw
{
    public static void DrawFromPythonGeneratedCode(CadDocument doc)
    {
        // doc.Entities.Add(HRB400Block.Insert(doc, CadDraw.P(0, 0), grade: "N11", spec: "18"));
        // doc.Entities.Add(HRB400Block.Insert(doc, CadDraw.P(100, 100), grade: "N13", spec: "12"));

        #region Ⅰ-Ⅰ截面

        // 图题
        TitleBlock.Add(doc, "Ⅰ-Ⅰ", "1:25", CadDraw.P(234308.7207205, 15752.635522), 50);

        // 剖面
        /// 3-3剖面
        SectionBlock.Add(
            doc,
            1832.037973,
            "Ⅲ",
            insertPoint: CadDraw.P(233185.969986, 13877.341965),
            scale: 50
        );
        /// Ⅱ剖面
        SectionBlock.Add(
            doc,
            2348.379371,
            "Ⅱ",
            insertPoint: CadDraw.P(233448.478839, 15355.934557),
            direction: -Math.PI / 2,
            scale: 50
        );

        // 方向
        doc.AddEntities<Entity>([
            CadDraw.Polyline(
                [
                    CadDraw.V(234523.821508, 12797.463156),
                    CadDraw.V(234603.283246, 12757.254591),
                    CadDraw.V(234031.113629, 12757.254591),
                ],
                doc.Layer(CadLayers.B03)
            ),
            CadDraw.Text(
                "横桥向",
                CadDraw.P(234111.156214, 12795.116859),
                125,
                doc.Layer(CadLayers.B07)
            ),
            CadDraw.Polyline(
                [
                    CadDraw.V(235653.613753, 14233.297805),
                    CadDraw.V(235693.822319, 14312.759543),
                    CadDraw.V(235693.822319, 13751.506135),
                ],
                doc.Layer(CadLayers.B03)
            ),
            CadDraw.Text(
                "顺桥向",
                CadDraw.P(235648.557444, 13837.314232, 0),
                125,
                doc.Layer(CadLayers.B07),
                rotation: 1.570796
            ),
        ]);

        DrawRebarMesh(doc, (double)233645.379631, (double)14969.982256, cols: 7, rows: 9);
        #endregion


        #region Ⅲ-Ⅲ截面
        // 剖面
        SectionBlock.Add(doc, 2840, "Ⅰ", CadDraw.P(232348.3737, 17556.2223), scale: 50);
        TitleBlock.Add(doc, "Ⅲ-Ⅲ", "1:25", CadDraw.P(233552.6821, 20048.4196), 50);

        // 构造
        doc.AddEntities<Entity>([
            CadDraw.Polyline(
                [
                    CadDraw.V(232043.896665, 19757.021596, bulge: -0.049579),
                    CadDraw.V(232110.626171, 19570.783812),
                    CadDraw.V(232595.379631, 17631.734954),
                    CadDraw.V(234945.39137, 17631.734954),
                ],
                doc.Layer(CadLayers.B04)
            ),
        ]);
        // 中心线
        doc.AddEntities<Entity>([
            CadDraw.Line(
                CadDraw.P(234945.39137, 17402.494146),
                CadDraw.P(234945.39137, 19869.472137),
                doc.Layer(CadLayers.B06)
            ),
            CadDraw.MText(
                "箱\n梁\n中\n心\n线",
                CadDraw.P(235085.4642, 19253.9306),
                150,
                doc.Layer(CadLayers.B07)
            ),
        ]);
        // 折断线
        BreakLineBlock.Insert(
            doc,
            CadDraw.P(232043.8967, 19757.0216),
            CadDraw.P(234945.3914, 19757.0216),
            doc.Layer(CadLayers.B04),
            scale: 50 * 3,
            extendL: 3 * 50
        );

        // 中心线
        doc.AddEntities<Entity>([
            CadDraw.Line(
                CadDraw.P(233770.3855, 16828.959815, 0),
                CadDraw.P(233770.385501, 18360.29711, 0),
                doc.Layer(CadLayers.B06)
            ),
        ]);
        LeaderAnnotationBlock.Add(
            doc,
            new(233770.3855, 16990.586066, 0),
            new(233963.722931, 16786.384474, 0),
            "支座中心线",
            50
        );

        DrawMeshⅢ(doc, originX: 233045.39137, originY: 18204.358129, rows: 3, cols: 7);

        // 底标注
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [
                    CadDraw.P(232595.379631, 17631.734954),
                    CadDraw.P(233645.3914, 17631.734954),
                    CadDraw.P(234945.39137, 17631.734954),
                ],
                -300
            )
        );
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [CadDraw.P(232595.379631, 17631.734954), CadDraw.P(234945.39137, 17631.734954)],
                -520
            )
        );

        // 侧标注
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [CadDraw.P(233045.3914, 18231.3581), CadDraw.P(233045.3914, 18231.3581 - 599.6231)],
                -200
            )
        );

        #endregion


        #region Ⅱ-Ⅱ截面
        // 图题
        TitleBlock.Add(doc, "Ⅱ-Ⅱ", "1:25", CadDraw.P(242734.0736, 19907.7665), 50);

        // 构造
        doc.Entities.Add(
            CadDraw.Line(
                CadDraw.P(240288.253959, 18118.180775),
                CadDraw.P(244984.85595, 18118.180775),
                doc.Layer(CadLayers.B04)
            )
        );
        // 折线
        BreakLineBlock.Insert(
            doc,
            CadDraw.P(240288.253959, 18118.180775),
            CadDraw.P(240288.253959, 19612.746788),
            doc.Layer(CadLayers.B04),
            scale: 50
        );
        BreakLineBlock.Insert(
            doc,
            CadDraw.P(240288.253959, 19612.746788),
            CadDraw.P(244984.85595, 19612.746788),
            doc.Layer(CadLayers.B04),
            scale: 50
        );
        BreakLineBlock.Insert(
            doc,
            CadDraw.P(244984.85595, 19612.746788),
            CadDraw.P(244984.85595, 18118.180775),
            doc.Layer(CadLayers.B04),
            scale: 50
        );
        // 中心线
        doc.AddEntities<Entity>([
            CadDraw.Line(
                CadDraw.P(242501.578428, 18907.740101, 0),
                CadDraw.P(242501.578428, 17862.233805, 0),
                doc.Layer(CadLayers.B06)
            ),
        ]);
        LeaderAnnotationBlock.Add(
            doc,
            new(242501.578428, 18007.898618, 0),
            new(242694.915858, 17840.312615, 0),
            "支座中心线",
            50
        );

        // 钢筋网
        DrawMeshⅡ(doc, originX: 241701.7772, originY: 18712.6799, rows: 3, cols: 9);

        #endregion


        #region 钢筋大样
        // 图题
        TitleBlock.Add(doc, "钢筋大样图", "", CadDraw.P(242549.0624, 16408.2951), 50);

        // N1 对称线筋：一根钢筋 = 形状 + 每段单行文本标注 + 整根引线标注（编号 N1 / 直径 12 / 长度 L=900），
        // PlaceDetail 落地时自动把 编号/等级/直径/根数/长度 写入多段线 XData
        var n1 = new Rebar
        {
            Number = "N1",
            Diameter = 12,
            SubstituteLength = "900",
            Vertices = SymmetricRebar.BuildVertices(
                [RebarDetail.V(-450, 0), RebarDetail.V(0, 0)]
            ),
        };
        n1.PlaceDetail(
            doc,
            CadDraw.P(242625.296067, 15882.500674),
            CadDraw.P(242814.314896, 16090.206334),
            scale: 50
        );

        // N11 箍筋（竖直）：左钩132 → 左边3170 → 顶332 → 右边3170 → 右钩132，整根 L=6936。
        // 每段长度自动标注（几何算），引线标注 编号 N11 + 规格 Φ12 L=6936；XData 记录 编号/等级/直径/根数/长度。
        var n11 = new Rebar
        {
            Number = "N11",
            Diameter = 12,
            Count = 2,
            Vertices =
            [
                RebarDetail.V(-132, 0), RebarDetail.V(0, 0),
                RebarDetail.V(0, 3170), RebarDetail.V(332, 3170),
                RebarDetail.V(332, 0), RebarDetail.V(464, 0),
            ],
        };
        n11.PlaceDetail(
            doc,
            CadDraw.P(241700.0, 14100.0),        // 图形位置（左钩外端；位置可调）
            CadDraw.P(242300.0, 15000.0),        // 引线拐点/文字基准点
            scale: 50
        );

        // N8 水平线筋（带两端立钩）：左钩132 → 直段8699 → 右钩132，整根 L=8963。
        // 直段文本覆盖为范围 "9403~7995"（替代长度），引线标注 编号 N8 + 规格 Φ12 L=8963。
        var n8 = new Rebar
        {
            Number = "N8",
            Diameter = 12,
            Count = 3,
            Vertices =
            [
                RebarDetail.V(0, 132),                  // [0] 左钩顶端
                RebarDetail.V(0, 0, "9403~7995"),       // [1] 左钩底（直段起点，Text 覆盖直段）
                RebarDetail.V(8699, 0),                 // [2] 直段终点 / 右钩起点
                RebarDetail.V(8699, 132),               // [3] 右钩顶端
            ],
        };
        n8.PlaceDetail(
            doc,
            CadDraw.P(236000.0, 11800.0),        // 图形位置（左钩顶端；位置可调）
            CadDraw.P(239000.0, 12800.0),        // 引线拐点/文字基准点
            scale: 50
        );

        #endregion

        // 引线标注
        // doc.AddEntities<Entity>([
        //     CadDraw.Polyline(
        //         [CadDraw.V(0, -8), CadDraw.V(0, 0), CadDraw.V(11.316302, 0)],
        //         doc.Layer(CadLayers.B03)
        //     ),
        //     HRB400Block.Insert(doc, CadDraw.P(5.4589, 1.9065), "N1", "12", 0.1),
        // ]);

        // 折断线
        // var pl = CadDraw.Polyline(
        //     [
        //         CadDraw.V(-5, 0),
        //         CadDraw.V(-0.5, 0),
        //         CadDraw.V(-0.25, -0.686869),
        //         CadDraw.V(0.25, 0.686869),
        //         CadDraw.V(0.5, 0),
        //         CadDraw.V(5, 0),
        //     ],
        //     doc.Layer(CadLayers.B03)
        // );
        // doc.Entities.Add(pl);
        // pl.Vertices[0].Location = new XY(-20, 0);

        // var l = CadDraw.Line(CadDraw.P(0, 0), CadDraw.P(10, 0), doc.Layer(CadLayers.B03));
        // l.ApplyScaling(new XYZ(5, 1, 1));
        // l.ApplyTranslation(new XYZ(10, 0, 0));
        // l.ApplyRotation(XYZ.AxisZ, Math.PI / 2);
        // doc.Entities.Add(l);

        // // 图题
        // TitleBlock.Add(doc, "这是一个很长这是一个很长这是一个很长这是一个很长", "1:25", CadDraw.P(0, 0));
    }

    private static void DrawMeshⅡ(
        CadDocument doc,
        double originX,
        double originY,
        int cols,
        int rows,
        double spacing_row = 200,
        double spacing_col = 200,
        double overhang_row = 100
    )
    {
        double width = (cols - 1) * spacing_col;

        // 单元格模板：钢断面 + 引线（每行每列重复），偏移在模板内平移
        var cell = new List<Entity>
        {
            SteelSection.Insert(doc, new XYZ(0, -22.5, 0), scale: 5),
            LeaderLine.Insert(doc, new XYZ(0, -22.5, 0), scale: 50),
        };

        // 横向钢筋 + 横向引线 模板（局部坐标，每行重复）
        var row = new List<Entity>
        {
            CadDraw.Line(
                CadDraw.P(-overhang_row, 0),
                CadDraw.P(width + overhang_row, 0),
                doc.Layer(CadLayers.B01)
            ),
            CadDraw.Line(
                CadDraw.P(75, 95 - 49.5),
                CadDraw.P(75, 95 - 49.5).Add(CadDraw.P(width + overhang_row * 2, 0)),
                doc.Layer(CadLayers.B03)
            ),
            LeaderLine.Insert(doc, new XYZ(700, 0, 0), scale: 40, direction: Math.PI / 2),
        };

        // 阵列方式 2：行是大循环，列是内层小循环
        CadMesh.RowOuter(
            doc,
            rowTemplates: row,
            cellTemplates: cell,
            cols: cols,
            rows: rows,
            spacingCol: spacing_col,
            spacingRow: spacing_row,
            origin: new XYZ(originX, originY, 0)
        );

        // 连续标注
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [CadDraw.P(originX, originY), CadDraw.P(originX + width, originY)],
                500
            )
        );

        LeaderAnnotationBlock.Add(
            doc,
            CadDraw.P(243401.5784, 18358.1867),
            CadDraw.P(243401.5784, 18758.1867),
            grade: "N1",
            spec: "12",
            scale: 50
        );
        LeaderAnnotationBlock.Add(
            doc,
            CadDraw.P(242401.7772, 17804.3581),
            CadDraw.P(242401.7772, 18467.3576),
            grade: "N2",
            spec: "12",
            scale: 50
        );
    }

    private static void DrawMeshⅢ(
        CadDocument doc,
        double originX,
        double originY,
        int cols,
        int rows,
        double spacing_row = 200,
        double spacing_col = 200,
        double overhang_row = 100
    )
    {
        double width = (cols - 1) * spacing_col;

        // 单元格模板：钢断面 + 引线（每行每列重复），偏移在模板内平移
        var cell = new List<Entity>
        {
            SteelSection.Insert(doc, new XYZ(0, 27, 0), scale: 5),
            LeaderLine.Insert(doc, new XYZ(0, 27, 0), scale: 50),
        };

        // 横向钢筋 + 横向引线 模板（局部坐标，每行重复）
        var row = new List<Entity>
        {
            CadDraw.Line(
                CadDraw.P(-overhang_row, 0),
                CadDraw.P(-overhang_row, 0).Add(CadDraw.P(width + overhang_row * 2, 0)),
                doc.Layer(CadLayers.B01)
            ),
            CadDraw.Line(
                CadDraw.P(75, 95),
                CadDraw.P(75, 95).Add(CadDraw.P(width + overhang_row * 2, 0)),
                doc.Layer(CadLayers.B03)
            ),
            LeaderLine.Insert(doc, new XYZ(700, 0, 0), scale: 40, direction: Math.PI / 2),
        };

        // 阵列方式 2：行是大循环，列是内层小循环
        CadMesh.RowOuter(
            doc,
            rowTemplates: row,
            cellTemplates: cell,
            cols: cols,
            rows: rows,
            spacingCol: spacing_col,
            spacingRow: spacing_row,
            origin: new XYZ(originX, originY, 0)
        );

        // 连续标注
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [CadDraw.P(originX, originY), CadDraw.P(originX + width, originY)],
                500
            )
        );

        LeaderAnnotationBlock.Add(
            doc,
            CadDraw.P(234345.3914, 17899.3581),
            CadDraw.P(234345.3914, 18299.3581),
            grade: "N1",
            spec: "12",
            scale: 50
        );
        LeaderAnnotationBlock.Add(
            doc,
            CadDraw.P(233745.3914, 17804.3581),
            CadDraw.P(233745.3914, 18467.3576),
            grade: "N2",
            spec: "12",
            scale: 50
        );
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="originX"></param>
    /// <param name="originY"></param>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <param name="spacing"></param>
    /// <param name="overhang"></param>
    public static void DrawRebarMesh(
        CadDocument doc,
        double originX,
        double originY,
        int cols,
        int rows,
        double spacing = 200,
        double overhang = 100,
        double colTickY = 14657.768549,
        double rowTickX = 234333.825574,
        double tickDx = 85,
        double tickDy = 35
    )
    {
        double width = (cols - 1) * spacing;
        double height = (rows - 1) * spacing;

        // 竖筋 + 竖刻纹 模板（局部坐标，每列仅 X 变化）
        var vRebar = CadDraw.Line(
            CadDraw.P(0, overhang, 0),
            CadDraw.P(0, -height - overhang, 0),
            doc.Layer(CadLayers.B01)
        );
        var vTick = CadDraw.Line(
            CadDraw.P(0, colTickY - originY, 0),
            CadDraw.P(tickDx, colTickY + tickDy - originY, 0),
            doc.Layer(CadLayers.B03)
        );

        // 横筋 + 横刻纹 模板（局部坐标，每行仅 Y 变化）
        var hRebar = CadDraw.Line(
            CadDraw.P(-overhang, 0, 0),
            CadDraw.P(width + overhang, 0, 0),
            doc.Layer(CadLayers.B01)
        );

        var hTick = CadDraw.Line(
            CadDraw.P(rowTickX - originX, 0, 0),
            CadDraw.P(rowTickX + tickDy - originX, -tickDx, 0),
            doc.Layer(CadLayers.B03)
        );

        // 阵列方式 1：行与列互不干扰
        CadMesh.Independent(
            doc,
            rowTemplates: [hRebar, hTick],
            colTemplates: [vRebar, vTick],
            cols: cols,
            rows: rows,
            spacingCol: spacing,
            spacingRow: spacing,
            origin: new XYZ(originX, originY, 0)
        );

        // 连续标注
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [
                    CadDraw.P(originX - overhang, originY),
                    CadDraw.P(originX, originY),
                    CadDraw.P(originX + width, originY),
                    CadDraw.P(originX + width + overhang, originY),
                ],
                500
            )
        );
        doc.Entities.AddRange(
            CadDraw.ChainDimension(
                [
                    CadDraw.P(originX, originY + overhang),
                    CadDraw.P(originX, originY),
                    CadDraw.P(originX, originY - height),
                    CadDraw.P(originX, originY - height - overhang),
                ],
                -500
            )
        );

        // 引线标注

        LeaderAnnotationBlock.Add(
            doc,
            CadDraw.P(originX, colTickY),
            CadDraw.P(235526.8066, colTickY),
            grade: "N1",
            spec: "12",
            scale: 50
        );

        LeaderAnnotationBlock.Add(
            doc,
            CadDraw.P(rowTickX, originY),
            CadDraw.P(rowTickX, 12660.1395),
            grade: "N2",
            spec: "12",
            scale: 50
        );
    }
}
