using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;

namespace GcsDwg;

public static partial class CadDraw
{
    public static Circle Circle(XYZ center, double radius, Layer layer) =>
        new(center, radius) { Layer = layer };

    public static List<Circle> Circles(
        IEnumerable<(XYZ Center, double Radius)> circles,
        Layer layer
    ) => [.. circles.Select(x => Circle(x.Center, x.Radius, layer))];
}
