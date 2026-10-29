using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using DwgSharpKit.Drawing;

namespace DwgSharpKit;

/// <summary>
/// 行列网格阵列。与 <see cref="CadArray"/> 的单方向实体阵列分开，避免混淆两类语义。
/// </summary>
public static class CadArrayGrid
{
    public static List<Entity> Independent(
        CadDocument doc,
        IReadOnlyList<Entity> rowTemplates,
        IReadOnlyList<Entity> colTemplates,
        int cols,
        int rows,
        double spacingCol,
        double spacingRow,
        XYZ origin
    )
    {
        ValidateDimensions(cols, rows);
        var result = new List<Entity>();
        for (var row = 0; row < rows; row++)
        {
            result.AddRange(
                Translated(
                doc,
                new XYZ(origin.X, origin.Y - row * spacingRow, origin.Z),
                rowTemplates
                )
            );
        }

        for (var col = 0; col < cols; col++)
        {
            result.AddRange(
                Translated(
                doc,
                new XYZ(origin.X + col * spacingCol, origin.Y, origin.Z),
                colTemplates
                )
            );
        }
        return result;
    }

    public static List<Entity> RowOuter(
        CadDocument doc,
        IReadOnlyList<Entity> rowTemplates,
        IReadOnlyList<Entity> cellTemplates,
        int cols,
        int rows,
        double spacingCol,
        double spacingRow,
        XYZ origin
    )
    {
        ValidateDimensions(cols, rows);
        var result = new List<Entity>();
        for (var row = 0; row < rows; row++)
        {
            var y = origin.Y - row * spacingRow;
            result.AddRange(Translated(doc, new XYZ(origin.X, y, origin.Z), rowTemplates));
            for (var col = 0; col < cols; col++)
            {
                result.AddRange(Translated(
                    doc,
                    new XYZ(origin.X + col * spacingCol, y, origin.Z),
                    cellTemplates
                ));
            }
        }
        return result;
    }

    public static List<Entity> ColumnOuter(
        CadDocument doc,
        IReadOnlyList<Entity> colTemplates,
        IReadOnlyList<Entity> cellTemplates,
        int cols,
        int rows,
        double spacingCol,
        double spacingRow,
        XYZ origin
    )
    {
        ValidateDimensions(cols, rows);
        var result = new List<Entity>();
        for (var col = 0; col < cols; col++)
        {
            var x = origin.X + col * spacingCol;
            result.AddRange(Translated(doc, new XYZ(x, origin.Y, origin.Z), colTemplates));
            for (var row = 0; row < rows; row++)
            {
                result.AddRange(Translated(
                    doc,
                    new XYZ(x, origin.Y - row * spacingRow, origin.Z),
                    cellTemplates
                ));
            }
        }
        return result;
    }

    private static List<Entity> Translated(
        CadDocument doc,
        XYZ origin,
        IReadOnlyList<Entity> templates
    ) => templates.Count == 0 ? [] : doc.TransformEntities(origin, templates);

    private static void ValidateDimensions(int cols, int rows)
    {
        if (cols < 0)
            throw new ArgumentOutOfRangeException(nameof(cols), "列数不能小于 0。");
        if (rows < 0)
            throw new ArgumentOutOfRangeException(nameof(rows), "行数不能小于 0。");
    }
}
