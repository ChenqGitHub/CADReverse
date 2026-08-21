using CSMath;

namespace GcsDwg.Models;

public sealed record CadText(
    string Value,
    XYZ InsertPoint,
    double Height,
    double Rotation = 0);
