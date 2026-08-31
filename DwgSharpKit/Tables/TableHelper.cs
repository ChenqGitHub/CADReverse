using System;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSMath;
using DwgSharpKit.Blocks;

namespace DwgSharpKit.Tables;

/// <summary>
/// ACadSharp 原生 TABLE 创建与编辑工具。
/// </summary>
public static class TableHelper
{
    private static int _tableIndex = 1;

    /// <summary>
    /// 获取并修改 Standard 表格样式。
    /// 标题、表头和数据单元格统一水平垂直居中且不显示填充。
    /// </summary>
    public static TableStyle ConfigureStandardTableStyle(CadDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        doc.TableStyles.CreateDefaults();

        var style =
            doc.TableStyles["Standard"]
            ?? throw new InvalidOperationException("无法获取 ACadSharp 的 Standard TableStyle。");
        // ConfigureCellStyle(style.TableCellStyle);
        // ConfigureCellStyle(style.TitleCellStyle);
        // ConfigureCellStyle(style.HeaderCellStyle);
        ConfigureCellStyle(style.DataCellStyle);

        return style;
    }

    private static void ConfigureCellStyle(TableStyle.CellStyle cellStyle)
    {
        cellStyle.HasData = true;
        cellStyle.IsFillColorOn = false;
        cellStyle.CellAlignment = TableStyle.CellAlignmentType.MiddleCenter;
        cellStyle.Alignment = (int)TableStyle.CellAlignmentType.MiddleCenter;
        cellStyle.PropertyFlags |= (int)TableStyle.CellStylePropertyFlags.Alignment;
        cellStyle.PropertyOverrideFlags |= TableStyle.CellStylePropertyFlags.Alignment;
        cellStyle.TableCellStylePropertyFlags |= TableStyle.CellStylePropertyFlags.Alignment;
    }

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
        double textHeight = 3.5
    )
    {
        ArgumentNullException.ThrowIfNull(doc);

        ArgumentNullException.ThrowIfNull(data);

        int rowCount = data.GetLength(0);
        int columnCount = data.GetLength(1);

        if (rowCount == 0 || columnCount == 0)
            throw new ArgumentException("表格不能没有行或列。", nameof(data));

        return CreateTable(
            doc,
            insertPoint,
            data,
            [.. Enumerable.Repeat(columnWidth, columnCount)],
            [.. Enumerable.Repeat(rowHeight, rowCount)],
            textHeight
        );
    }

    /// <summary>
    /// 创建一个 ACadSharp 原生 TABLE，并为每一列、每一行指定尺寸。
    /// </summary>
    public static TableEntity CreateTable(
        CadDocument doc,
        XYZ insertPoint,
        string[,] data,
        double[] columnWidths,
        double[] rowHeights,
        double textHeight = 3.5
    )
    {
        ArgumentNullException.ThrowIfNull(doc);

        ArgumentNullException.ThrowIfNull(data);

        ArgumentNullException.ThrowIfNull(columnWidths);

        ArgumentNullException.ThrowIfNull(rowHeights);

        int rowCount = data.GetLength(0);
        int columnCount = data.GetLength(1);

        if (rowCount == 0 || columnCount == 0)
            throw new ArgumentException("表格不能没有行或列。", nameof(data));

        if (columnWidths.Length != columnCount)
        {
            throw new ArgumentException("列宽数量必须与表格列数一致。", nameof(columnWidths));
        }

        if (rowHeights.Length != rowCount)
        {
            throw new ArgumentException("行高数量必须与表格行数一致。", nameof(rowHeights));
        }

        var tableStyle = ConfigureStandardTableStyle(doc);

        string blockName = $"*T{_tableIndex++}";
        var block = new BlockRecord(blockName) { IsAnonymous = true };
        doc.BlockRecords.Add(block);

        var table = new TableEntity(block)
        {
            InsertPoint = insertPoint,
            HorizontalDirection = XYZ.AxisX,
            Style = tableStyle,
        };

        table.CellStyleOverride.HasData = true;
        table.CellStyleOverride.IsFillColorOn = false;
        table.CellStyleOverride.CellAlignment = TableStyle.CellAlignmentType.MiddleCenter;
        ConfigureCellStyle(table.CellStyleOverride);

        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            table.Columns.Add(
                new TableEntity.Column
                {
                    Name = $"Column{columnIndex + 1}",
                    Width = columnWidths[columnIndex],
                }
            );
        }

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var row = new TableEntity.Row { Height = rowHeights[rowIndex] };

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var cell = new TableEntity.Cell
                {
                    Type = TableEntity.CellType.Text,
                    Style = tableStyle.DataCellStyle,
                };

                SetTextContent(cell, data[rowIndex, columnIndex], textHeight);
                row.Cells.Add(cell);
            }

            table.Rows.Add(row);
        }

        if (
            table.Rows.Count != rowCount
            || table.Columns.Count != columnCount
            || table.Rows.Any(row => row.Cells.Count != columnCount)
        )
        {
            throw new InvalidOperationException("TABLE 内容缓存未正确建立。\n");
        }

        return table;
    }

    /// <summary>
    /// 创建一个 ACadSharp 原生 TABLE，数据以行列表形式传入，可随时 <c>Add</c> 增加行。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="insertPoint">表格插入点</param>
    /// <param name="data">表格数据，每项为一行；不传 <paramref name="rowHeights"/> 时所有行使用默认行高 8.0</param>
    /// <param name="columnWidths">列宽</param>
    /// <param name="rowHeights">行高，数量必须与行数一致；为空时自动补齐</param>
    /// <param name="textHeight">文字高度</param>
    public static TableEntity CreateTable(
        CadDocument doc,
        XYZ insertPoint,
        IReadOnlyList<string[]> data,
        double[] columnWidths,
        double[]? rowHeights = null,
        double textHeight = 3.5
    )
    {
        ArgumentNullException.ThrowIfNull(doc);

        ArgumentNullException.ThrowIfNull(data);

        ArgumentNullException.ThrowIfNull(columnWidths);

        int rowCount = data.Count;
        int columnCount = rowCount > 0 ? data[0].Length : 0;

        if (rowCount == 0 || columnCount == 0)
            throw new ArgumentException("表格不能没有行或列。", nameof(data));

        if (columnWidths.Length != columnCount)
        {
            throw new ArgumentException("列宽数量必须与表格列数一致。", nameof(columnWidths));
        }

        rowHeights ??= [.. Enumerable.Repeat(8.0, rowCount)];

        if (rowHeights.Length != rowCount)
        {
            throw new ArgumentException("行高数量必须与表格行数一致。", nameof(rowHeights));
        }

        var rect = new string[rowCount, columnCount];

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var row =
                data[rowIndex]
                ?? throw new ArgumentException($"第 {rowIndex} 行数据不能为空。", nameof(data));

            if (row.Length != columnCount)
            {
                throw new ArgumentException(
                    $"第 {rowIndex} 行有 {row.Length} 列，与表头 {columnCount} 列不一致。",
                    nameof(data)
                );
            }

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                rect[rowIndex, columnIndex] = row[columnIndex];
        }

        return CreateTable(doc, insertPoint, rect, columnWidths, rowHeights, textHeight);
    }

    /// <summary>
    /// 设置指定单元格的文字内容。
    /// </summary>
    public static TableEntity.Cell SetTextCell(
        TableEntity table,
        int rowIndex,
        int columnIndex,
        string? value,
        double textHeight = 3.5
    )
    {
        var cell = GetCell(table, rowIndex, columnIndex);
        cell.Type = TableEntity.CellType.Text;
        cell.Contents.Clear();
        SetTextContent(cell, value, textHeight);
        return cell;
    }

    private static void SetTextContent(TableEntity.Cell cell, string? value, double textHeight)
    {
        var content = new TableEntity.CellContent
        {
            ContentType = TableEntity.TableCellContentType.Value,
        };

        content.CadValue.SetValue(value ?? string.Empty, CadValueType.String);
        content.Format.HasData = true;
        content.Format.TextHeight = textHeight;
        content.Format.Alignment = (int)TableStyle.CellAlignmentType.MiddleCenter;
        content.Format.PropertyFlags |= (int)TableStyle.CellStylePropertyFlags.Alignment;
        content.Format.PropertyOverrideFlags |= TableStyle.CellStylePropertyFlags.Alignment;
        cell.Contents.Add(content);
    }

    /// <summary>
    /// 合并一个矩形单元格区域，索引均为从零开始且包含边界。
    /// 合并后仅保留左上角单元格的内容。
    /// </summary>
    public static TableEntity.CellRange MergeCells(
        TableEntity table,
        int topRowIndex,
        int leftColumnIndex,
        int bottomRowIndex,
        int rightColumnIndex
    )
    {
        ArgumentNullException.ThrowIfNull(table);

        if (topRowIndex < 0 || leftColumnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topRowIndex), "合并区域索引不能小于零。");
        }

        if (bottomRowIndex < topRowIndex || rightColumnIndex < leftColumnIndex)
        {
            throw new ArgumentException("合并区域的结束行列必须大于或等于起始行列。");
        }

        ValidateCellCoordinates(table, bottomRowIndex, rightColumnIndex);

        var existing = table.MergedCellRanges.FirstOrDefault(range =>
            range.TopRowIndex == topRowIndex
            && range.LeftColumnIndex == leftColumnIndex
            && range.BottomRowIndex == bottomRowIndex
            && range.RightColumnIndex == rightColumnIndex
        );

        if (existing != null)
            return existing;

        if (
            table.MergedCellRanges.Any(range =>
                topRowIndex <= range.BottomRowIndex
                && bottomRowIndex >= range.TopRowIndex
                && leftColumnIndex <= range.RightColumnIndex
                && rightColumnIndex >= range.LeftColumnIndex
            )
        )
        {
            throw new InvalidOperationException("合并区域与已有合并区域重叠。");
        }

        for (int rowIndex = topRowIndex; rowIndex <= bottomRowIndex; rowIndex++)
        {
            for (int columnIndex = leftColumnIndex; columnIndex <= rightColumnIndex; columnIndex++)
            {
                if (rowIndex == topRowIndex && columnIndex == leftColumnIndex)
                    continue;

                table.GetCell(rowIndex, columnIndex).Contents.Clear();
            }
        }

        var mergedRange = new TableEntity.CellRange
        {
            TopRowIndex = topRowIndex,
            LeftColumnIndex = leftColumnIndex,
            BottomRowIndex = bottomRowIndex,
            RightColumnIndex = rightColumnIndex,
        };

        table.MergedCellRanges.Add(mergedRange);
        return mergedRange;
    }

    /// <summary>
    /// 设置单元格的块内容状态。
    /// </summary>
    public static TableEntity.Cell SetBlockCell(
        TableEntity table,
        int rowIndex,
        int columnIndex,
        BlockRecord block,
        double blockScale = 1.0,
        double rotation = 0.0
    )
    {
        ArgumentNullException.ThrowIfNull(table);

        ArgumentNullException.ThrowIfNull(block);

        if (blockScale == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockScale), "块比例不能为零。");
        }

        var cell = GetCell(table, rowIndex, columnIndex);
        cell.Type = TableEntity.CellType.Block;
        cell.BlockScale = blockScale;
        cell.Rotation = rotation;
        cell.Contents.Clear();

        var content = new TableEntity.CellContent
        {
            ContentType = TableEntity.TableCellContentType.Block,
        };

        // ACadSharp 3.7.1 的 CellContent 没有公开 BlockRecord 属性，
        // 这里保留块名供内存模型及后续写入器使用。
        content.CadValue.SetValue(block.Name, CadValueType.String);
        cell.Contents.Add(content);

        return cell;
    }

    /// <summary>
    /// 将块放置到指定单元格中心，并返回实际创建的 INSERT。
    /// ACadSharp 3.7.1 的 DWG 写入器会把 TABLE 块内容句柄写成空值，
    /// 因此同时将 INSERT 写入 TABLE 的匿名缓存块以保证图形可见。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="table">目标表格</param>
    /// <param name="rowIndex">行索引（从 0 开始）</param>
    /// <param name="columnIndex">列索引（从 0 开始）</param>
    /// <param name="blockName">块名</param>
    /// <param name="attributes">块属性字典，键为属性标签（Tag），值为要写入的属性值；未提供的标签保留块定义的默认值</param>
    /// <param name="blockScale">块比例</param>
    /// <param name="rotation">旋转角</param>
    public static Insert AddBlockToCell(
        CadDocument doc,
        TableEntity table,
        int rowIndex,
        int columnIndex,
        string blockName,
        IReadOnlyDictionary<string, string>? attributes = null,
        double blockScale = 1.0,
        double rotation = 0.0
    )
    {
        ArgumentNullException.ThrowIfNull(doc);

        if (string.IsNullOrWhiteSpace(blockName))
            throw new ArgumentException("块名称不能为空。", nameof(blockName));

        if (!doc.BlockRecords.Contains(blockName))
        {
            if (string.Equals(blockName, HRB400Block.Name, StringComparison.OrdinalIgnoreCase))
            {
                HRB400Block.EnsureDefined(doc);
            }
            else
            {
                throw new InvalidOperationException($"文档中不存在名为“{blockName}”的块定义。");
            }
        }

        var block = doc.BlockRecords[blockName];
        SetBlockCell(table, rowIndex, columnIndex, block, blockScale, rotation);

        var center = GetCellLocalCenter(table, rowIndex, columnIndex);
        var insert = new Insert(block)
        {
            InsertPoint = center,
            XScale = blockScale,
            YScale = blockScale,
            ZScale = blockScale,
            Rotation = rotation,
        };

        ApplyAttributes(insert, attributes);

        table.Block.Entities.Add(insert);
        return insert;
    }

    /// <summary>
    /// 按属性标签（Tag）字典为 INSERT 填充块属性值，未提供的标签保留块定义的默认值。
    /// </summary>
    private static void ApplyAttributes(
        Insert insert,
        IReadOnlyDictionary<string, string>? attributes
    )
    {
        insert.UpdateAttributes();

        foreach (var definition in insert.Block.AttributeDefinitions)
        {
            var value =
                attributes is not null && attributes.TryGetValue(definition.Tag, out var v)
                    ? v
                    : definition.Value;

            var attribute = insert.Attributes.FirstOrDefault(x => x.Tag == definition.Tag);
            if (attribute is null)
            {
                attribute = new AttributeEntity(definition);
                insert.Attributes.Add(attribute);
            }

            BlockAttributeHelper.Sync(attribute, definition, insert);
            attribute.Value = value;
        }
    }

    /// <summary>
    /// 计算单元格中心点。表格行按当前表格平面向下排列。
    /// </summary>
    public static XYZ GetCellCenter(TableEntity table, int rowIndex, int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(table);

        ValidateCellCoordinates(table, rowIndex, columnIndex);

        var horizontal = Normalize(table.HorizontalDirection, "HorizontalDirection");
        var normal = Normalize(table.Normal, "Normal");

        // horizontal x normal 指向表格的行方向；标准 XY 平面中为 -Y。
        var rowDirection = Normalize(
            new XYZ(
                horizontal.Y * normal.Z - horizontal.Z * normal.Y,
                horizontal.Z * normal.X - horizontal.X * normal.Z,
                horizontal.X * normal.Y - horizontal.Y * normal.X
            ),
            "table row direction"
        );

        double columnOffset = 0;
        for (int column = 0; column < columnIndex; column++)
            columnOffset += table.Columns[column].Width;
        columnOffset += table.Columns[columnIndex].Width / 2.0;

        double rowOffset = 0;
        for (int row = 0; row < rowIndex; row++)
            rowOffset += table.Rows[row].Height;
        rowOffset += table.Rows[rowIndex].Height / 2.0;

        return new XYZ(
            table.InsertPoint.X + horizontal.X * columnOffset + rowDirection.X * rowOffset,
            table.InsertPoint.Y + horizontal.Y * columnOffset + rowDirection.Y * rowOffset,
            table.InsertPoint.Z + horizontal.Z * columnOffset + rowDirection.Z * rowOffset
        );
    }

    private static XYZ GetCellLocalCenter(TableEntity table, int rowIndex, int columnIndex)
    {
        ValidateCellCoordinates(table, rowIndex, columnIndex);

        double x = 0;
        for (int column = 0; column < columnIndex; column++)
            x += table.Columns[column].Width;
        x += table.Columns[columnIndex].Width / 2.0;

        double y = 0;
        for (int row = 0; row < rowIndex; row++)
            y -= table.Rows[row].Height;
        y -= table.Rows[rowIndex].Height / 2.0;

        return new XYZ(x, y, 0);
    }

    private static TableEntity.Cell GetCell(TableEntity table, int rowIndex, int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(table);

        ValidateCellCoordinates(table, rowIndex, columnIndex);
        return table.GetCell(rowIndex, columnIndex);
    }

    private static void ValidateCellCoordinates(TableEntity table, int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= table.Rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        if (columnIndex < 0 || columnIndex >= table.Columns.Count)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));

        if (table.Rows[rowIndex].Cells.Count <= columnIndex)
        {
            throw new InvalidOperationException(
                $"表格第 {rowIndex} 行没有第 {columnIndex} 列单元格。"
            );
        }
    }

    private static XYZ Normalize(XYZ value, string name)
    {
        double length = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);

        if (length < 1e-12)
            throw new InvalidOperationException($"{name} 不能是零向量。");

        return new XYZ(value.X / length, value.Y / length, value.Z / length);
    }
}
