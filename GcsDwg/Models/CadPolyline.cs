using CSMath;

namespace GcsDwg.Models;

public sealed record CadPolyline(
    List<CadPolylineVertex> Vertices,
    bool Closed = false,
    double ConstantWidth = 0,
    double Elevation = 0
);

public sealed record CadPolylineVertex(
    XY Point,
    double Bulge = 0,
    double StartWidth = 0,
    double EndWidth = 0
);
