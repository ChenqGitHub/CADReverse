using System.Reflection;
using ACadSharp.Entities;
using CSMath;

Console.WriteLine($"DimensionLinear base={typeof(DimensionLinear).BaseType}");
foreach (var p in typeof(DimensionLinear).GetProperties(BindingFlags.Public|BindingFlags.Instance))
    if (p.PropertyType == typeof(XYZ)) Console.WriteLine($"prop {p.Name}={p.GetValue(new DimensionLinear())}");
var d = new DimensionLinear { FirstPoint = new XYZ(1,2,0), SecondPoint = new XYZ(3,4,0), DefinitionPoint = new XYZ(2,5,0), TextMiddlePoint = new XYZ(2,5,0) };
d.ApplyTranslation(new XYZ(10,20,0));
Console.WriteLine($"dim after: first={d.FirstPoint} second={d.SecondPoint} def={d.DefinitionPoint} text={d.TextMiddlePoint}");

static void DumpPoly(string title, LwPolyline p)
{
    Console.WriteLine($"{title}: n={p.Vertices.Count}");
    for (var i = 0; i < p.Vertices.Count; i++)
    {
        var v = p.Vertices[i];
        Console.WriteLine($"  [{i}] ({v.Location.X}, {v.Location.Y}) bulge={v.Bulge}");
    }
}

// ApplyScaling 是实例方法还是扩展方法？谁实现的？
foreach (var t in new[] { typeof(LwPolyline), typeof(Line), typeof(Arc), typeof(TextEntity) })
{
    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        .Where(m => m.Name == "ApplyScaling")
        .Select(m => $"{m.DeclaringType!.Name}.{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
    Console.WriteLine($"{t.Name} -> {string.Join(" | ", methods)}");
}

static LwPolyline MakePoly() =>
    new(
        [
            new LwPolyline.Vertex(new XY(0, 0)),
            new LwPolyline.Vertex(new XY(0, 6400)) { Bulge = 0.1343104044982665 },
            new LwPolyline.Vertex(new XY(-1231, 10900)),
            new LwPolyline.Vertex(new XY(-1231, 11300)),
        ]
    );

var p1 = MakePoly();
DumpPoly("LwPolyline raw      ", p1);
p1.ApplyScaling(new XYZ(2, 2, 1), new XYZ(0, 0, 0));
DumpPoly("LwPolyline scale 2  ", p1);

var p2 = MakePoly();
p2.ApplyScaling(new XYZ(-1, 1, 1), new XYZ(0, 0, 0));
DumpPoly("LwPolyline scale -1 ", p2);

var line = new Line { StartPoint = new XYZ(-500, 100, 0), EndPoint = new XYZ(-900, 700, 0) };
line.ApplyScaling(new XYZ(-1, 1, 1), new XYZ(0, 0, 0));
Console.WriteLine($"Line scale -1: {line.StartPoint} -> {line.EndPoint}");

var circle = new Circle { Center = new XYZ(-500, 100, 0), Radius = 50 };
circle.ApplyScaling(new XYZ(-1, 1, 1), new XYZ(0, 0, 0));
Console.WriteLine($"Circle scale -1: center={circle.Center} r={circle.Radius}");
