using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;

namespace DwgSharpKit;

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

    /// <summary>
    /// 将任意实体按等间距克隆成阵列。原实体不修改。
    /// </summary>
    /// <param name="template">阵列模板实体</param>
    /// <param name="step">相邻实体的位移向量</param>
    /// <param name="count">阵列数量</param>
    /// <returns>克隆后的实体列表</returns>
    public static List<Entity> EntityArray(Entity template, XYZ step, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "阵列数量不能小于 0。");
        }

        var result = new List<Entity>(count);
        for (int i = 0; i < count; i++)
        {
            var copy = (Entity)template.Clone();
            copy.ApplyTranslation(new XYZ(step.X * i, step.Y * i, step.Z * i));
            result.Add(copy);
        }

        return result;
    }

    /// <summary>
    /// 沿多段线按固定间距克隆实体。多段线应由直线段组成。
    /// </summary>
    /// <param name="template">阵列模板实体，其局部基准点应位于原点</param>
    /// <param name="path">阵列路径</param>
    /// <param name="spacing">沿路径的间距</param>
    /// <param name="followPath">是否使实体旋转到当前路径切线方向</param>
    /// <param name="includeEnd">是否包含路径终点</param>
    public static List<Entity> EntityArrayAlongPolyline(
        Entity template,
        LwPolyline path,
        double spacing,
        bool followPath = false,
        bool includeEnd = true
    )
    {
        if (spacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing), "间距必须大于 0。");
        }

        var vertices = path.Vertices.ToList();
        if (vertices.Count < 2)
        {
            return [];
        }

        var segments = new List<(XYZ Start, XYZ End, double Length, double Angle)>();
        var segmentCount = path.IsClosed ? vertices.Count : vertices.Count - 1;
        var totalLength = 0.0;

        for (var i = 0; i < segmentCount; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertices.Count];
            if (Math.Abs(a.Bulge) > 1e-9)
            {
                throw new NotSupportedException(
                    "沿多段线阵列暂只支持直线段，不支持带 bulge 的圆弧段。"
                );
            }

            var start = new XYZ(a.Location.X, a.Location.Y, path.Elevation);
            var end = new XYZ(b.Location.X, b.Location.Y, path.Elevation);
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 1e-9)
            {
                continue;
            }

            segments.Add((start, end, length, Math.Atan2(dy, dx)));
            totalLength += length;
        }

        var result = new List<Entity>();
        for (var distance = 0.0; distance <= totalLength + 1e-9; distance += spacing)
        {
            AddAtPathDistance(result, template, segments, distance, followPath);
        }

        if (includeEnd && totalLength > 1e-9)
        {
            var remainder = totalLength % spacing;
            if (remainder > 1e-9 && spacing - remainder > 1e-9)
            {
                AddAtPathDistance(result, template, segments, totalLength, followPath);
            }
        }

        return result;
    }

    private static void AddAtPathDistance(
        List<Entity> result,
        Entity template,
        IReadOnlyList<(XYZ Start, XYZ End, double Length, double Angle)> segments,
        double distance,
        bool followPath
    )
    {
        var remaining = distance;
        var segment = segments[^1];
        foreach (var candidate in segments)
        {
            if (remaining <= candidate.Length + 1e-9)
            {
                segment = candidate;
                break;
            }

            remaining -= candidate.Length;
        }

        var t = Math.Clamp(remaining / segment.Length, 0, 1);
        var point = new XYZ(
            segment.Start.X + (segment.End.X - segment.Start.X) * t,
            segment.Start.Y + (segment.End.Y - segment.Start.Y) * t,
            segment.Start.Z + (segment.End.Z - segment.Start.Z) * t
        );
        var copy = (Entity)template.Clone();
        if (followPath && Math.Abs(segment.Angle) > 1e-9)
        {
            copy.ApplyRotation(XYZ.AxisZ, segment.Angle);
        }

        copy.ApplyTranslation(point);
        result.Add(copy);
    }

    /// <summary>
    /// 在上下边界之间生成竖向线阵列。每根线的上下端点由边界与当前 X 坐标的交点决定。
    /// </summary>
    /// <param name="topBoundary">上边界线段集合</param>
    /// <param name="bottomBoundary">下边界线段集合</param>
    /// <param name="startX">阵列起始 X</param>
    /// <param name="endX">阵列结束 X</param>
    /// <param name="spacing">X 方向间距</param>
    /// <param name="layer">生成线所在图层</param>
    public static List<Line> LinesBetweenBoundaries(
        IEnumerable<Line> topBoundary,
        IEnumerable<Line> bottomBoundary,
        double startX,
        double endX,
        double spacing,
        Layer layer
    )
    {
        if (spacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing), "间距必须大于 0。");
        }

        var top = topBoundary.ToList();
        var bottom = bottomBoundary.ToList();
        var result = new List<Line>();
        var minX = Math.Min(startX, endX);
        var maxX = Math.Max(startX, endX);

        for (var x = minX; x <= maxX + spacing * 1e-9; x += spacing)
        {
            var topY = BoundaryYAtX(top, x);
            var bottomY = BoundaryYAtX(bottom, x);
            if (topY is null || bottomY is null)
            {
                continue;
            }

            result.Add(Line(new XYZ(x, topY.Value, 0), new XYZ(x, bottomY.Value, 0), layer));
        }

        return result;
    }

    public static List<Line> LinesBetweenBoundaries(
        Line topBoundary,
        Line bottomBoundary,
        double startX,
        double endX,
        double spacing,
        Layer layer
    ) => LinesBetweenBoundaries([topBoundary], [bottomBoundary], startX, endX, spacing, layer);

    /// <summary>
    /// 在多段线形式的上下边界之间生成竖向线阵列。
    /// </summary>
    public static List<Line> LinesBetweenBoundaries(
        LwPolyline topBoundary,
        LwPolyline bottomBoundary,
        double startX,
        double endX,
        double spacing,
        Layer layer
    ) => LinesBetweenBoundaries(
        PolylineSegments(topBoundary),
        PolylineSegments(bottomBoundary),
        startX,
        endX,
        spacing,
        layer
    );

    /// <summary>
    /// 在左右边界之间生成横向线阵列。每根线的左右端点由边界与当前 Y 坐标的交点决定。
    /// </summary>
    public static List<Line> LinesBetweenSideBoundaries(
        IEnumerable<Line> leftBoundary,
        IEnumerable<Line> rightBoundary,
        double startY,
        double endY,
        double spacing,
        Layer layer
    )
    {
        if (spacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing), "间距必须大于 0。");
        }

        var left = leftBoundary.ToList();
        var right = rightBoundary.ToList();
        var result = new List<Line>();
        var minY = Math.Min(startY, endY);
        var maxY = Math.Max(startY, endY);

        for (var y = minY; y <= maxY + spacing * 1e-9; y += spacing)
        {
            var leftX = BoundaryXAtY(left, y);
            var rightX = BoundaryXAtY(right, y);
            if (leftX is null || rightX is null)
            {
                continue;
            }

            result.Add(Line(new XYZ(leftX.Value, y, 0), new XYZ(rightX.Value, y, 0), layer));
        }

        return result;
    }

    public static List<Line> LinesBetweenSideBoundaries(
        Line leftBoundary,
        Line rightBoundary,
        double startY,
        double endY,
        double spacing,
        Layer layer
    ) => LinesBetweenSideBoundaries([leftBoundary], [rightBoundary], startY, endY, spacing, layer);

    /// <summary>
    /// 在多段线形式的左右边界之间生成横向线阵列。
    /// </summary>
    public static List<Line> LinesBetweenSideBoundaries(
        LwPolyline leftBoundary,
        LwPolyline rightBoundary,
        double startY,
        double endY,
        double spacing,
        Layer layer
    ) => LinesBetweenSideBoundaries(
        PolylineSegments(leftBoundary),
        PolylineSegments(rightBoundary),
        startY,
        endY,
        spacing,
        layer
    );

    private static double? BoundaryYAtX(IEnumerable<Line> segments, double x)
    {
        const double epsilon = 1e-9;
        foreach (var segment in segments)
        {
            var a = segment.StartPoint;
            var b = segment.EndPoint;
            var minX = Math.Min(a.X, b.X) - epsilon;
            var maxX = Math.Max(a.X, b.X) + epsilon;
            if (x < minX || x > maxX)
            {
                continue;
            }

            var dx = b.X - a.X;
            if (Math.Abs(dx) < epsilon)
            {
                if (Math.Abs(x - a.X) < epsilon)
                {
                    return Math.Min(a.Y, b.Y);
                }

                continue;
            }

            var t = (x - a.X) / dx;
            return a.Y + t * (b.Y - a.Y);
        }

        return null;
    }

    private static double? BoundaryXAtY(IEnumerable<Line> segments, double y)
    {
        const double epsilon = 1e-9;
        foreach (var segment in segments)
        {
            var a = segment.StartPoint;
            var b = segment.EndPoint;
            var minY = Math.Min(a.Y, b.Y) - epsilon;
            var maxY = Math.Max(a.Y, b.Y) + epsilon;
            if (y < minY || y > maxY)
            {
                continue;
            }

            var dy = b.Y - a.Y;
            if (Math.Abs(dy) < epsilon)
            {
                if (Math.Abs(y - a.Y) < epsilon)
                {
                    return Math.Min(a.X, b.X);
                }

                continue;
            }

            var t = (y - a.Y) / dy;
            return a.X + t * (b.X - a.X);
        }

        return null;
    }

    private static List<Line> PolylineSegments(LwPolyline polyline)
    {
        var vertices = polyline.Vertices.ToList();
        var count = vertices.Count;
        var segments = new List<Line>();
        var segmentCount = polyline.IsClosed ? count : count - 1;

        for (var i = 0; i < segmentCount; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % count];
            if (Math.Abs(a.Bulge) > 1e-9)
            {
                throw new NotSupportedException(
                    "边界多段线暂只支持直线段，不支持带 bulge 的圆弧段。"
                );
            }

            segments.Add(
                Line(
                    new XYZ(a.Location.X, a.Location.Y, polyline.Elevation),
                    new XYZ(b.Location.X, b.Location.Y, polyline.Elevation),
                    polyline.Layer
                )
            );
        }

        return segments;
    }

}
