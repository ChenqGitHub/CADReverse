using System;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSMath;

namespace CadCli.Generated
{
    /// <summary>
    /// ACadSharp 原生 TABLE 创建工具。
    /// </summary>
    public static class TableHelper
    {
        private static int _tableIndex = 1;

        /// <summary>
        /// 创建一个 ACadSharp 原生 TABLE。
        /// </summary>
        /// <param name="doc">CAD 文档</param>
        /// <param name="insertPoint">表格插入点</param>
        /// <param name="data">表格数据，[行,列]</param>
        /// <param name="rowHeight">行高</param>
        /// <param name="columnWidth">列宽</param>
        /// <param name="textHeight">文字高度</param>
        public static TableEntity CreateTable(
            CadDocument doc,
            XYZ insertPoint,
            string[,] data,
            double rowHeight = 8.0,
            double columnWidth = 35.0,
            double textHeight = 3.5)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            int rowCount = data.GetLength(0);
            int columnCount = data.GetLength(1);

            if (rowCount == 0 || columnCount == 0)
                throw new ArgumentException("表格不能没有行或列。", nameof(data));

            // ============================================================
            // 1. 确保 TableStyle 存在
            // ============================================================

            doc.TableStyles.CreateDefaults();

            var tableStyle = doc.TableStyles["Standard"];

            if (tableStyle == null)
            {
                throw new InvalidOperationException(
                    "无法获取 ACadSharp 的 Standard TableStyle。");
            }

            // ============================================================
            // 2. 创建 TABLE 专用匿名 BlockRecord
            // ============================================================

            string blockName = $"*T{_tableIndex++}";

            var block = new BlockRecord(blockName)
            {
                IsAnonymous = true
            };

            doc.BlockRecords.Add(block);

            // ============================================================
            // 3. 创建 TableEntity
            // ============================================================

            var table = new TableEntity(block)
            {
                InsertPoint = insertPoint,
                HorizontalDirection = XYZ.AxisX,

                // 指定 TableStyle
                Style = tableStyle
            };

            // ============================================================
            // 4. 设置整个 TABLE 的默认单元格样式
            //
            // 无背景
            // 水平 + 垂直居中
            // ============================================================

            table.CellStyleOverride.HasData = true;

            // 不显示背景填充
            table.CellStyleOverride.IsFillColorOn = false;

            // 水平 + 垂直居中
            table.CellStyleOverride.CellAlignment =
                TableStyle.CellAlignmentType.MiddleCenter;

            // ============================================================
            // 5. 创建 Columns
            // ============================================================

            for (int c = 0; c < columnCount; c++)
            {
                var column = new TableEntity.Column
                {
                    Name = $"Column{c + 1}",
                    Width = columnWidth
                };

                table.Columns.Add(column);
            }

            // ============================================================
            // 6. 创建 Rows
            // ============================================================

            for (int r = 0; r < rowCount; r++)
            {
                var row = new TableEntity.Row
                {
                    Height = rowHeight
                };

                // ========================================================
                // 7. 创建 Cells
                // ========================================================

                for (int c = 0; c < columnCount; c++)
                {
                    var cell = new TableEntity.Cell
                    {
                        Type = TableEntity.CellType.Text
                    };

                    // ====================================================
                    // 8. 设置 Cell 样式
                    // ====================================================

                    var cellStyle =
                        TableStyle.CellStyle.DefaultDataCellStyle;

                    cellStyle.HasData = true;

                    // 无背景
                    cellStyle.IsFillColorOn = false;

                    // 居中
                    cellStyle.CellAlignment =
                        TableStyle.CellAlignmentType.MiddleCenter;

                    cell.Style = cellStyle;

                    // ====================================================
                    // 9. 创建 CellContent
                    // ====================================================

                    var content = new TableEntity.CellContent
                    {
                        ContentType =
                            TableEntity.TableCellContentType.Value
                    };

                    // ====================================================
                    // 10. 设置文字
                    // ====================================================

                    string value = data[r, c] ?? string.Empty;

                    content.CadValue.SetValue(
                        value,
                        CadValueType.String);

                    // ====================================================
                    // 11. 设置文字格式
                    // ====================================================

                    content.Format.HasData = true;

                    content.Format.TextHeight = textHeight;

                    // ====================================================
                    // 12. 加入 Cell
                    // ====================================================

                    cell.Contents.Add(content);

                    row.Cells.Add(cell);
                }

                // ========================================================
                // 13. 加入 Row
                // ========================================================

                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// 创建一个简单的钢筋数量表。
        /// </summary>
        public static TableEntity CreateRebarQuantityTable(
            CadDocument doc,
            XYZ insertPoint)
        {
            string[,] data =
            {
                { "编号", "钢筋规格", "数量" },
                { "1", "HRB400 Φ16", "20" },
                { "2", "HRB400 Φ20", "35" },
                { "3", "HRB400 Φ22", "48" }
            };

            return CreateTable(
                doc,
                insertPoint,
                data,
                rowHeight: 8.0,
                columnWidth: 35.0,
                textHeight: 3.5);
        }
    }
}