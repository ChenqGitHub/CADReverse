using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Models;

namespace GcsDwg;

public static partial class CadDraw
{
    /// <summary>
    /// 由坐标点创建多段线实体。
    /// </summary>
    /// <param name="points">顶点坐标序列</param>
    /// <param name="layer">所在图层</param>
    /// <param name="closed">是否闭合</param>
    /// <param name="constantWidth">固定线宽</param>
    /// <param name="elevation">标高</param>
    /// <returns>多段线实体</returns>
    public static LwPolyline Polyline(
        IEnumerable<XY> points,
        Layer layer,
        bool closed = false,
        double constantWidth = 0,
        double elevation = 0
    ) =>
        new(points)
        {
            Layer = layer,
            IsClosed = closed,
            ConstantWidth = constantWidth,
            Elevation = elevation,
        };

    /// <summary>
    /// 由带 bulge 的顶点创建多段线实体。
    /// </summary>
    /// <param name="vertices">顶点列表（含 bulge/线宽）</param>
    /// <param name="layer">所在图层</param>
    /// <param name="closed">是否闭合</param>
    /// <param name="constantWidth">固定线宽</param>
    /// <param name="elevation">标高</param>
    /// <returns>多段线实体</returns>
    public static LwPolyline Polyline(
        IEnumerable<CadPolylineVertex> vertices,
        Layer layer,
        bool closed = false,
        double constantWidth = 0,
        double elevation = 0
    ) =>
        new(
            vertices.Select(x => new LwPolyline.Vertex(x.Point)
            {
                Bulge = x.Bulge,
                StartWidth = x.StartWidth,
                EndWidth = x.EndWidth,
            })
        )
        {
            Layer = layer,
            IsClosed = closed,
            ConstantWidth = constantWidth,
            Elevation = elevation,
        };

    /// <summary>
    /// 由坐标点创建多段线参数模型。
    /// </summary>
    /// <param name="points">顶点坐标序列</param>
    /// <param name="closed">是否闭合</param>
    /// <param name="constantWidth">固定线宽</param>
    /// <param name="elevation">标高</param>
    /// <returns>多段线参数模型</returns>
    public static CadPolyline Polyline(
        IEnumerable<XY> points,
        bool closed = false,
        double constantWidth = 0,
        double elevation = 0
    ) =>
        new(
            points.Select(x => new CadPolylineVertex(x)).ToList(),
            closed,
            constantWidth,
            elevation
        );

    /// <summary>
    /// 由带 bulge 的顶点创建多段线参数模型。
    /// </summary>
    /// <param name="vertices">顶点列表（含 bulge/线宽）</param>
    /// <param name="closed">是否闭合</param>
    /// <param name="constantWidth">固定线宽</param>
    /// <param name="elevation">标高</param>
    /// <returns>多段线参数模型</returns>
    public static CadPolyline BulgePolyline(
        IEnumerable<CadPolylineVertex> vertices,
        bool closed = false,
        double constantWidth = 0,
        double elevation = 0
    ) => new(vertices.ToList(), closed, constantWidth, elevation);

    /// <summary>
    /// 创建多段线顶点参数。
    /// </summary>
    /// <param name="x">顶点 X 坐标</param>
    /// <param name="y">顶点 Y 坐标</param>
    /// <param name="bulge">凸度</param>
    /// <param name="startWidth">起始线宽</param>
    /// <param name="endWidth">终止线宽</param>
    /// <returns>多段线顶点参数</returns>
    public static CadPolylineVertex V(
        double x,
        double y,
        double bulge = 0,
        double startWidth = 0,
        double endWidth = 0
    ) => new(P2(x, y), bulge, startWidth, endWidth);

    /// <summary>
    /// 批量创建多段线实体。
    /// </summary>
    /// <param name="polylines">多段线参数模型列表</param>
    /// <param name="layer">所在图层</param>
    /// <returns>多段线实体列表</returns>
    public static List<LwPolyline> LwPolylines(IEnumerable<CadPolyline> polylines, Layer layer) =>
        polylines
            .Select(x => Polyline(x.Vertices, layer, x.Closed, x.ConstantWidth, x.Elevation))
            .ToList();
}
