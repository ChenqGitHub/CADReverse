using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using CSMath.Geometry;

namespace DwgSharpKit.Drawing
{
    public enum CadProjectionAxis
    {
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// 一个实体的线性阵列规格。Template 使用 1:1 局部坐标，Step 为相邻实体的位移。
    /// </summary>
    public sealed record CadArrayPattern(Entity Template, XYZ Step, int Count)
    {
        public CadArrayPattern(Entity template, double stepX, double stepY, int count)
            : this(template, new XYZ(stepX, stepY, 0), count) { }

        public void Validate()
        {
            ArgumentNullException.ThrowIfNull(Template);
            if (Count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Count), "阵列数量不能小于 0。");
            }
        }
    }

    /// <summary>
    /// CAD 实体阵列的统一入口。
    /// <para>
    /// 这里负责“实体如何重复和落位”；边界求交等几何构造仍由 <see cref="CadDraw"/> 负责。
    /// </para>
    /// </summary>
    public static class CadArray
    {
        /// <summary>
        /// 创建实体的等间距线性阵列，不修改模板。
        /// </summary>
        public static List<Entity> Linear(Entity template, XYZ step, int count)
        {
            var pattern = new CadArrayPattern(template, step, count);
            pattern.Validate();

            var result = new List<Entity>(count);
            for (var index = 0; index < count; index++)
            {
                var copy = (Entity)template.Clone();
                copy.ApplyTranslation(new XYZ(step.X * index, step.Y * index, step.Z * index));
                result.Add(copy);
            }

            return result;
        }

        /// <summary>
        /// 将模板布置到边界与一组水平线或竖直线的交点处。
        /// 模板应使用局部原点作为插入基准；本方法只创建并返回克隆体，不写入文档。
        /// </summary>
        /// <param name="boundary">用于求交的闭合多段线边界。</param>
        /// <param name="template">局部坐标模板。</param>
        /// <param name="coordinates">投影线的 Y 或 X 坐标。</param>
        /// <param name="axis">Horizontal 表示固定 Y，Vertical 表示固定 X。</param>
        /// <param name="tolerance">交点去重容差。</param>
        public static List<Entity> BoundaryIntersections(
            Hatch.BoundaryPath.Polyline boundary,
            Entity template,
            IEnumerable<double> coordinates,
            CadProjectionAxis axis,
            double tolerance = 1e-9
        )
        {
            ArgumentNullException.ThrowIfNull(boundary);
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(coordinates);
            if (tolerance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tolerance));
            }

            var points = new List<XY>();
            foreach (var coordinate in coordinates)
            {
                var projection = axis switch
                {
                    CadProjectionAxis.Horizontal => Line2D.FromPoints(
                        new XY(0, coordinate),
                        new XY(1, coordinate)
                    ),
                    CadProjectionAxis.Vertical => Line2D.FromPoints(
                        new XY(coordinate, 0),
                        new XY(coordinate, 1)
                    ),
                    _ => throw new ArgumentOutOfRangeException(nameof(axis)),
                };

                foreach (var point in boundary.FindIntersections(projection))
                {
                    if (!points.Any(existing => IsSamePoint(existing, point, tolerance)))
                    {
                        points.Add(point);
                    }
                }
            }

            return points
                .Select(point =>
                {
                    var copy = (Entity)template.Clone();
                    copy.ApplyTranslation(new XYZ(point.X, point.Y, 0));
                    return copy;
                })
                .ToList();
        }

        /// <summary>
        /// 将实体阵列按四象限对称规则加入文档。
        /// </summary>
        /// <param name="doc">目标 CAD 文档。</param>
        /// <param name="originX">第一象限阵列起点的 X 坐标。</param>
        /// <param name="originY">第一象限阵列起点的 Y 坐标。</param>
        /// <param name="pattern">阵列模板、步长和数量。</param>
        /// <param name="horizontalAxisY">水平对称轴的 Y 坐标。</param>
        /// <param name="includeHorizontalSymmetry">是否同时生成水平轴对称副本。</param>
        public static List<Entity> FourWaySymmetric(
            CadDocument doc,
            double originX,
            double originY,
            CadArrayPattern pattern,
            double horizontalAxisY,
            bool includeHorizontalSymmetry = true
        )
        {
            ArgumentNullException.ThrowIfNull(doc);
            pattern.Validate();

            var result = new List<Entity>();
            result.AddRange(Quadrant(doc, originX, originY, pattern));
            if (includeHorizontalSymmetry)
            {
                result.AddRange(
                    Quadrant(
                        doc,
                        originX,
                        2 * horizontalAxisY - originY,
                        pattern with
                        {
                            Step = new XYZ(pattern.Step.X, -pattern.Step.Y, pattern.Step.Z),
                        }
                    )
                );
            }

            result.AddRange(
                Quadrant(
                    doc,
                    -originX,
                    originY,
                    pattern with
                    {
                        Step = new XYZ(-pattern.Step.X, pattern.Step.Y, pattern.Step.Z),
                    }
                )
            );

            if (includeHorizontalSymmetry)
            {
                result.AddRange(
                    Quadrant(
                        doc,
                        -originX,
                        2 * horizontalAxisY - originY,
                        pattern with
                        {
                            Step = new XYZ(-pattern.Step.X, -pattern.Step.Y, pattern.Step.Z),
                        }
                    )
                );
            }
            return result;
        }

        public static List<Entity> FourWaySymmetric(
            CadDocument doc,
            IReadOnlyList<double> originX,
            double originY,
            CadArrayPattern pattern,
            double horizontalAxisY,
            bool includeHorizontalSymmetry = true
        )
        {
            ArgumentNullException.ThrowIfNull(doc);
            ArgumentNullException.ThrowIfNull(originX);
            ArgumentNullException.ThrowIfNull(pattern);
            pattern.Validate();

            var entities = new List<Entity>();
            foreach (var item in originX)
            {
                entities.AddRange(
                    FourWaySymmetric(
                        doc,
                        item,
                        originY,
                        pattern,
                        horizontalAxisY,
                        includeHorizontalSymmetry
                    )
                );
            }
            return entities;
        }

        public static List<Entity> FourWaySymmetric(
            CadDocument doc,
            double originX,
            IReadOnlyList<double> originY,
            CadArrayPattern pattern,
            double horizontalAxisY,
            bool includeHorizontalSymmetry = true
        )
        {
            ArgumentNullException.ThrowIfNull(doc);
            ArgumentNullException.ThrowIfNull(originY);
            ArgumentNullException.ThrowIfNull(pattern);
            pattern.Validate();

            var entities = new List<Entity>();
            foreach (var item in originY)
            {
                entities.AddRange(
                    FourWaySymmetric(
                        doc,
                        originX,
                        item,
                        pattern,
                        horizontalAxisY,
                        includeHorizontalSymmetry
                    )
                );
            }
            return entities;
        }

        private static List<Entity> Quadrant(
            CadDocument doc,
            double originX,
            double originY,
            CadArrayPattern pattern
        ) =>
            doc.TransformEntities(
                new XYZ(originX, originY, 0),
                Linear(pattern.Template, pattern.Step, pattern.Count)
            );

        private static bool IsSamePoint(XY left, XY right, double tolerance) =>
            Math.Abs(left.X - right.X) <= tolerance && Math.Abs(left.Y - right.Y) <= tolerance;
    }
}
