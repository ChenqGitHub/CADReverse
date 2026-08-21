using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;

namespace GcsDwg;

public static partial class CadDraw
{
    /// <summary>
    /// 创建圆弧实体。
    /// </summary>
    /// <param name="center">圆心坐标</param>
    /// <param name="radius">半径</param>
    /// <param name="startAngle">起始角（弧度）</param>
    /// <param name="endAngle">终止角（弧度）</param>
    /// <param name="layer">所在图层</param>
    /// <returns>圆弧实体</returns>
    public static Arc Arc(
        XYZ center,
        double radius,
        double startAngle,
        double endAngle,
        Layer layer
    ) => new(center, radius, startAngle, endAngle) { Layer = layer };

    /// <summary>
    /// 批量创建圆弧实体。
    /// </summary>
    /// <param name="arcs">圆弧参数列表（圆心/半径/起止角）</param>
    /// <param name="layer">所在图层</param>
    /// <returns>圆弧实体列表</returns>
    public static List<Arc> Arcs(
        IEnumerable<(XYZ Center, double Radius, double StartAngle, double EndAngle)> arcs,
        Layer layer
    ) => arcs.Select(x => Arc(x.Center, x.Radius, x.StartAngle, x.EndAngle, layer)).ToList();
}
