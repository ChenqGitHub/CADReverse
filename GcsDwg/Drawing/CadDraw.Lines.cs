using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;

namespace GcsDwg;

public static partial class CadDraw
{
    /// <summary>
    /// 创建直线实体。
    /// </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <param name="layer">所在图层</param>
    /// <returns>直线实体</returns>
    public static Line Line(XYZ start, XYZ end, Layer layer) =>
        new()
        {
            StartPoint = start,
            EndPoint = end,
            Layer = layer,
        };

    /// <summary>
    /// 批量创建直线实体。
    /// </summary>
    /// <param name="lines">直线参数列表（起点/终点）</param>
    /// <param name="layer">所在图层</param>
    /// <returns>直线实体列表</returns>
    public static List<Line> Lines(IEnumerable<(XYZ Start, XYZ End)> lines, Layer layer) =>
        [.. lines.Select(x => Line(x.Start, x.End, layer))];
}
