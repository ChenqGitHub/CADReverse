using CSMath;

namespace GcsDwg;

public static partial class CadDraw
{
    public static XYZ P(double x, double y, double z = 0) => new(x, y, z);

    public static XY P2(double x, double y) => new(x, y);
}
