using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using DwgSharpKit.Drawing;

namespace DwgSharpKit;

/// <summary>
/// 网格阵列助手：把局部模板按行/列重复摆放。共三种阵列方式：
/// 1. 行列独立（互不干扰，如 DrawRebarMesh）；
/// 2. 行是大循环、列是内层小循环（如 DrawMeshⅡ/Ⅲ）；
/// 3. 列是大循环、行是内层小循环。
/// 所有模板均为 1:1 局部坐标；单元格相对行基线的偏移应在模板内先行平移，
/// Grid 不再负责单元格偏移。
/// </summary>
/// <remarks>
/// 新代码请使用 <see cref="CadArrayGrid"/>。本类型保留用于兼容旧图纸代码。
/// </remarks>
[Obsolete("请使用 CadArrayGrid。")]
public static class CadMesh
{
    /// <summary>
    /// 阵列方式 1：行与列互不干扰。行模板逐行沿 -Y 下移，列模板逐列沿 +X 右移，
    /// 二者无嵌套关系。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="rowTemplates">每行重复的模板（横向对象），逐行下移，可为空</param>
    /// <param name="colTemplates">每列重复的模板（纵向对象），逐列右移，可为空</param>
    /// <param name="cols">列数</param>
    /// <param name="rows">行数</param>
    /// <param name="spacingCol">横向间距</param>
    /// <param name="spacingRow">纵向间距</param>
    /// <param name="origin">原点（第一行第一列位置）</param>
    public static void Independent(
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
        doc.AddEntities(CadArrayGrid.Independent(doc, rowTemplates, colTemplates, cols, rows, spacingCol, spacingRow, origin));
    }

    /// <summary>
    /// 阵列方式 2：行是大循环，列是内层小循环（如 DrawMeshⅡ/Ⅲ）。
    /// 每行先摆行模板，再在该行内逐列摆单元格模板。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="rowTemplates">每行重复的模板（横向对象），逐行下移</param>
    /// <param name="cellTemplates">每个单元格重复的模板，可为空</param>
    /// <param name="cols">列数</param>
    /// <param name="rows">行数</param>
    /// <param name="spacingCol">横向间距</param>
    /// <param name="spacingRow">纵向间距</param>
    /// <param name="origin">原点（第一行第一列位置）</param>
    public static void RowOuter(
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
        doc.AddEntities(CadArrayGrid.RowOuter(doc, rowTemplates, cellTemplates, cols, rows, spacingCol, spacingRow, origin));
    }

    /// <summary>
    /// 阵列方式 3：列是大循环，行是内层小循环。
    /// 每列先摆列模板，再在该列内逐行摆单元格模板。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="colTemplates">每列重复的模板（纵向对象），逐列右移</param>
    /// <param name="cellTemplates">每个单元格重复的模板，可为空</param>
    /// <param name="cols">列数</param>
    /// <param name="rows">行数</param>
    /// <param name="spacingCol">横向间距</param>
    /// <param name="spacingRow">纵向间距</param>
    /// <param name="origin">原点（第一行第一列位置）</param>
    public static void ColOuter(
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
        doc.AddEntities(CadArrayGrid.ColumnOuter(doc, colTemplates, cellTemplates, cols, rows, spacingCol, spacingRow, origin));
    }
}
