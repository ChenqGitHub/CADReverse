using CSMath;

namespace DwgSharpKit.Models;

public sealed record CadPolylineVertex(
    XY Point,
    double Bulge = 0,
    double StartWidth = 0,
    double EndWidth = 0
);
