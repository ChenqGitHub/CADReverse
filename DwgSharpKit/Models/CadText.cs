using CSMath;

namespace DwgSharpKit.Models;

public sealed record CadText(
    string Value,
    XYZ InsertPoint,
    double Height,
    double Rotation = 0);
