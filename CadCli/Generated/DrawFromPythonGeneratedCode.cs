using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using ACadSharp.Tables.Collections;
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
        doc.AddEntities<Entity>(
            [
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
            ]
        );

        DrawRebarMesh(doc, (double)233645.379631, (double)14969.982256, cols: 7, rows: 9);
        #endregion


        #region Ⅲ-Ⅲ截面
        // 剖面
        SectionBlock.Add(doc, 2840, "Ⅰ", CadDraw.P(232348.3737, 17556.2223), scale: 50);
        TitleBlock.Add(doc, "Ⅲ-Ⅲ", "1:25", CadDraw.P(233552.6821, 20048.4196), 50);

        // 构造
        doc.AddEntities<Entity>(
            [
                CadDraw.Polyline(
                    [
                        CadDraw.V(232043.896665, 19757.021596, bulge: -0.049579),
                        CadDraw.V(232110.626171, 19570.783812),
                        CadDraw.V(232595.379631, 17631.734954),
                        CadDraw.V(234945.39137, 17631.734954),
                    ],
                    doc.Layer(CadLayers.B04)
                ),
            ]
        );
        // 中心线
        doc.AddEntities<Entity>(
            [
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
            ]
        );
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
        doc.AddEntities<Entity>(
            [
                CadDraw.Line(
                    CadDraw.P(233770.3855, 16828.959815, 0),
                    CadDraw.P(233770.385501, 18360.29711, 0),
                    doc.Layer(CadLayers.B06)
                ),
            ]
        );
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
        doc.AddEntities<Entity>(
            [
                CadDraw.Line(
                    CadDraw.P(242501.578428, 18907.740101, 0),
                    CadDraw.P(242501.578428, 17862.233805, 0),
                    doc.Layer(CadLayers.B06)
                ),
            ]
        );
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
            Vertices = SymmetricRebar.BuildVertices([RebarDetail.V(-450, 0), RebarDetail.V(0, 0)]),
        };
        n1.PlaceDetail(
            doc,
            CadDraw.P(242625.296067, 15882.500674),
            CadDraw.P(242814.314896, 16090.206334),
            scale: 50
        );

        var n2 = new Rebar
        {
            Number = "N2",
            Diameter = 12,
            SubstituteLength = "1100",
            Vertices = SymmetricRebar.BuildVertices([RebarDetail.V(-550, 0), RebarDetail.V(0, 0)]),
        };
        n2.PlaceDetail(
            doc,
            CadDraw.P(242625.296067, 15882.500674),
            CadDraw.P(242814.314896, 16090.206334),
            scale: 50
        );

        #endregion


        #region 数量表

        // 图题
        TitleBlock.Add(doc, "支座顶部加强数量钢筋表", "", CadDraw.P(0, 0), 1);

        // 数量表：先生成 3×4 表格，再一次循环填入数字 1、2、3…
        // 注意：ACadSharp 3.7.1 写 DWG/DXF 时不会写出 TABLE 实体（写入端丢弃），对象在内存中完整存在。
        // const int tableRows = 3, tableCols = 4;

        // var table = new TableEntity
        // {
        //     // 表格位置
        //     InsertPoint = new XYZ(0, 0, 0),
        //     // 使用专门创建的表格样式（仿宋、表头/数据/标题、全边框）
        //     Style = CreateRebarTableStyle(doc),
        // };

        // // 生成 3×4 网格
        // for (int i = 0; i < tableRows; i++)
        // {
        //     var row = new TableEntity.Row { Height = 10 };
        //     for (int j = 0; j < tableCols; j++)
        //         row.Cells.Add(new TableEntity.Cell());
        //     table.Rows.Add(row);
        // }
        // for (int j = 0; j < tableCols; j++)
        //     table.Columns.Add(new TableEntity.Column { Width = 20 });

        // // 填入数字 1..12
        // void SetCell(int r, int c, string text)
        // {
        //     var cell = table.GetCell(r, c);
        //     var content = new TableEntity.CellContent { ContentType = TableEntity.TableCellContentType.Value };
        //     content.CadValue?.SetValue(text, CadValueType.String);
        //     cell.Contents.Add(content);
        // }

        // int n = 1;
        // for (int i = 0; i < tableRows; i++)
        //     for (int j = 0; j < tableCols; j++)
        //         SetCell(i, j, (n++).ToString());
        string[,] data =
        {
            { "编号", "钢筋规格", "数量" },
            { "1", "HRB400 Φ16", "20" },
            { "2", "HRB400 Φ20", "35" },
            { "3", "HRB400 Φ22", "48" },
        };

        // 数量表：手工绘制（ACadSharp 写 TABLE 实体会损坏 DWG，改用 Line + Text，保证文件正常、表格可见）
        DrawQuantityTable(
            doc,
            originX: -120,
            originY: -20,
            data,
            colWidth: [60, 120, 60],
            rowHeight: [16, 16, 16, 16],
            textHeight: 5
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

    private static int _tableIndex = 1;

    /// <summary>
    /// 创建一个 ACadSharp 原生 TABLE。
    /// 采用与 TableHelper.CreateTable 相同的可行写法：Standard 表格样式 + TABLE 专用匿名 BlockRecord，
    /// 逐格写入文字（CadValue + Format），单元格样式统一无背景、水平+垂直居中。
    /// </summary>
    public static TableEntity CreateTable(
        CadDocument doc,
        XYZ insertPoint,
        string[,] data,
        double rowHeight = 8.0,
        double columnWidth = 30.0,
        double textHeight = 3.5
    )
    {
        int rowCount = data.GetLength(0);
        int columnCount = data.GetLength(1);

        // 1) 新建"数量表"表格样式（基于 Standard 设计、不改 Standard；数据样式、填充空、外框红/内框绿）
        var tableStyle = CreateRebarTableStyle(doc);

        // 2) TABLE 专用匿名 BlockRecord
        var block = new BlockRecord($"*T{_tableIndex++}")
        {
            IsAnonymous = true,
        };
        doc.BlockRecords.Add(block);

        // 3) 创建 TableEntity（必须挂到匿名 BlockRecord 上）
        var table = new TableEntity(block)
        {
            InsertPoint = insertPoint,
            HorizontalDirection = XYZ.AxisX,
            Style = tableStyle,
        };

        // 4) 整表默认单元格样式：无背景、水平+垂直居中
        table.CellStyleOverride.HasData = true;
        table.CellStyleOverride.IsFillColorOn = false;
        table.CellStyleOverride.CellAlignment = TableStyle.CellAlignmentType.MiddleCenter;

        // 5) 列
        for (int c = 0; c < columnCount; c++)
        {
            table.Columns.Add(
                new TableEntity.Column
                {
                    Name = $"Column{c + 1}",
                    Width = columnWidth,
                }
            );
        }

        // 6) 行 + 单元格
        for (int r = 0; r < rowCount; r++)
        {
            var row = new TableEntity.Row { Height = rowHeight };

            for (int c = 0; c < columnCount; c++)
            {
                var cell = new TableEntity.Cell { Type = TableEntity.CellType.Text };

                // 单元格样式：用自定义样式的数据单元格样式（无背景、居中、带边框）
                var cellStyle = tableStyle.DataCellStyle;
                cellStyle.HasData = true;
                cellStyle.IsFillColorOn = false;
                cellStyle.CellAlignment = TableStyle.CellAlignmentType.MiddleCenter;
                cell.Style = cellStyle;

                // 内容：文字值 + 格式
                var content = new TableEntity.CellContent
                {
                    ContentType = TableEntity.TableCellContentType.Value,
                };
                content.CadValue.SetValue(data[r, c] ?? string.Empty, CadValueType.String);
                content.Format.HasData = true;
                content.Format.TextHeight = textHeight;

                cell.Contents.Add(content);
                row.Cells.Add(cell);
            }

            table.Rows.Add(row);
        }

        return table;
    }

    /// <summary>
    /// 新建"数量表"表格样式（完全独立定义，不依赖、不修改 Standard）：
    /// 单元格统一为数据样式且填充全部为空；文字 JSTI_仿宋、水平垂直居中；
    /// 边框外框（上下左右）红色、内框（水平/垂直内部线）绿色。
    /// 已存在同名样式时直接复用（幂等）。
    /// </summary>
    private static TableStyle CreateRebarTableStyle(CadDocument doc)
    {
        const string styleName = "数量表";

        // 已注册过则直接复用
        if (doc.TableStyles is not null && doc.TableStyles.TryGet(styleName, out var existing))
            return existing;

        var textStyle = doc.TextStyle(CadTextStyles.JstiSimsun);

        var style = new TableStyle(styleName)
        {
            Description = "支座顶部加强数量钢筋表",
            HorizontalCellMargin = 1.5,
            VerticalCellMargin = 1.5,
        };

        TableStyle.CellStyle CreateDataCellStyle(string name, double textHeight)
        {
            var cs = new TableStyle.CellStyle
            {
                Name = name,
                Type = TableStyle.CellStyleType.Cell,
                CellAlignment = TableStyle.CellAlignmentType.MiddleCenter,
                TextStyle = textStyle,
                TextHeight = textHeight,
                IsFillColorOn = false, // 填充为空
            };

            // 边框：外框红、内框绿
            void SetBorder(TableStyle.CellBorder border, Color color)
            {
                border.ApplyBorder = true;
                border.Type = TableStyle.BorderType.Single;
                border.IsInvisible = false;
                border.Color = color;
            }

            SetBorder(cs.TopBorder, Color.Red);
            SetBorder(cs.BottomBorder, Color.Red);
            SetBorder(cs.LeftBorder, Color.Red);
            SetBorder(cs.RightBorder, Color.Red);
            SetBorder(cs.HorizontalInsideBorder, Color.Green);
            SetBorder(cs.VerticalInsideBorder, Color.Green);

            return cs;
        }

        // 表格内容都是数据样式，单元格都是数据
        var data = CreateDataCellStyle("数量表_数据", 3.5);

        style.CellStyles.Add(data);
        style.DataCellStyle = data;
        style.HeaderCellStyle = data;
        style.TitleCellStyle = data;

        // 注册到文档（文档未初始化表格样式集合时跳过）
        doc.TableStyles?.TryAdd(style);

        return style;
    }

    /// <summary>
    /// 手工绘制数量表：网格线用 CadDraw.Line、单元格文字用 CadDraw.Text（JSTI_仿宋、居中）。
    /// 外框红色、内框绿色。避免 ACadSharp 写 TABLE 实体导致 DWG 损坏。
    /// </summary>
    private static void DrawQuantityTable(
        CadDocument doc,
        double originX,
        double originY,
        string[,] data,
        double[] colWidth,
        double[] rowHeight,
        double textHeight = 5.0
    )
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        // 列边界
        var xs = new double[cols + 1];
        xs[0] = originX;
        for (int c = 0; c < cols; c++)
            xs[c + 1] = xs[c] + colWidth[c];

        // 行边界（向下为负）
        var ys = new double[rows + 1];
        ys[0] = originY;
        for (int r = 0; r < rows; r++)
            ys[r + 1] = ys[r] - rowHeight[r];

        var gridLayer = doc.Layer(CadLayers.B04);
        var textLayer = doc.Layer(CadLayers.B07);

        void HLine(double x0, double x1, double y, Color color)
        {
            var line = CadDraw.Line(CadDraw.P(x0, y, 0), CadDraw.P(x1, y, 0), gridLayer);
            line.Color = color;
            doc.Entities.Add(line);
        }

        void VLine(double x, double y0, double y1, Color color)
        {
            var line = CadDraw.Line(CadDraw.P(x, y0, 0), CadDraw.P(x, y1, 0), gridLayer);
            line.Color = color;
            doc.Entities.Add(line);
        }

        // 外框：红
        HLine(xs[0], xs[cols], ys[0], Color.Red);
        HLine(xs[0], xs[cols], ys[rows], Color.Red);
        VLine(xs[0], ys[0], ys[rows], Color.Red);
        VLine(xs[cols], ys[0], ys[rows], Color.Red);

        // 内框：绿
        for (int c = 1; c < cols; c++)
            VLine(xs[c], ys[0], ys[rows], Color.Green);
        for (int r = 1; r < rows; r++)
            HLine(xs[0], xs[cols], ys[r], Color.Green);

        // 单元格文字：JSTI_仿宋、水平垂直居中
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                double cx = (xs[c] + xs[c + 1]) / 2;
                double cy = (ys[r] + ys[r + 1]) / 2;
                doc.Entities.Add(
                    CadDraw.Text(
                        data[r, c],
                        CadDraw.P(cx, cy, 0),
                        textHeight,
                        textLayer,
                        textStyle: CadTextStyles.JstiSimsun.Name,
                        horizontalAlignment: TextHorizontalAlignment.Center,
                        verticalAlignment: TextVerticalAlignmentType.Middle
                    )
                );
            }
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
