using System.Text.Json;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using CadCli.Generated;
using DwgSharpKit.Infrastructure;
using DwgSharpKit.Rebar;
using GeneratedDraw = CadCli.Generated.Pier.GeneratedDraw;

// --stats <dwg> [--json]：只读取并统计已生成的 DWG，便于和源图/手写版对比。
if (args.Length >= 2 && args[0] == "--stats")
{
    DumpStats(args[1], args.Length >= 3 && args[2] == "--json");
    return;
}

// --dump <dwg>：逐实体回读（多段线顶点 / 文字 / 块属性），用于和源图逐项对照。
if (args.Length >= 2 && args[0] == "--dump")
{
    DumpEntities(args[1]);
    return;
}

// --pier-cap [out]：只生成 04 桥墩顶帽钢筋图（OpenCAD 当前视图还原）的 DWG。
if (args.Length >= 1 && args[0] == "--pier-cap")
{
    var fileName = args.Length >= 2 ? args[1] : "pier-cap-opencad.dwg";
    var output = DrawAndSave(
        fileName,
        CadCli.Generated.PierOpenCad.PierCapOpenCadDrawing.Draw
    );
    Console.WriteLine($"桥墩顶帽钢筋图 CAD file created: {output}");
    return;
}

// --pier-body [out]：只生成 03 桥墩墩身钢筋图（OpenCAD 视图还原）的 DWG。
if (args.Length >= 1 && args[0] == "--pier-body")
{
    var fileName = args.Length >= 2 ? args[1] : "pier-body-opencad.dwg";
    var output = DrawAndSave(
        fileName,
        CadCli.Generated.PierOpenCad.PierBodyOpenCadDrawing.Draw
    );
    Console.WriteLine($"桥墩墩身钢筋图 CAD file created: {output}");
    return;
}

// --probe-text：TEXT 字高/旋转往返校验（临时）。
if (args.Length >= 1 && args[0] == "--probe-text")
{
    var path = Path.Combine(AppContext.BaseDirectory, "probe-text.dwg");
    {
        var doc = new CadDocument();
        DwgSharpKit.Infrastructure.CadInitializer.InitCad(doc);
        var layer = DwgSharpKit.Drawing.CadDocumentExtensions.Layer(
            doc,
            DwgSharpKit.Standards.CadLayers.B07
        );
        var rotations = new[] { 0.0, Math.PI / 2, 1.6152, 1.8105, 0.7854 };
        for (var i = 0; i < rotations.Length; i++)
        {
            doc.Entities.Add(
                DwgSharpKit.CadDraw.Text(
                    $"T{i}",
                    DwgSharpKit.CadDraw.P(i * 500, 0),
                    125,
                    layer,
                    rotation: rotations[i],
                    horizontalAlignment: TextHorizontalAlignment.Center,
                    verticalAlignment: TextVerticalAlignmentType.Middle
                )
            );
        }
        new DwgWriter(path, doc).Write();
    }
    using (var reader = new DwgReader(path))
    {
        foreach (var e in reader.Read().Entities.OfType<TextEntity>())
        {
            Console.WriteLine($"{e.Value} rot={e.Rotation:0.#####} h={e.Height:0.####}");
        }
    }
    return;
}

// 单独输出钢筋大样 N1 系列（N1-1 起）。
if (args.Length >= 1 && args[0] == "--rebar-n1")
{
    var fileName = args.Length >= 2 ? args[1] : "rebar-n1.dwg";
    return;
}

var drawings = new (string Title, string FileName, Action<CadDocument> Draw)[]
{
    (
        "支座加强钢筋图",
        "bearing-reinforcement.dwg",
        CadCli.Generated.GeneratedDraw.DrawBearingReinforcement
    ),
    (
        "框架桥钢筋图",
        "frame-bridge-reinforcement.dwg",
        CadCli.Generated.GeneratedDraw.DrawFrameBridgeReinforcement
    ),
    ("桥墩构造图", "pier-structure.dwg", GeneratedDraw.DrawPierStructure),
    (
        "桥墩构造图（OpenCAD 还原）",
        "pier-opencad.dwg",
        CadCli.Generated.PierOpenCad.PierOpenCadDrawing.Draw
    ),
};

for (var i = 0; i < drawings.Length; i++)
{
    var (title, fileName, draw) = drawings[i];
    if (args.Length > i && !string.IsNullOrWhiteSpace(args[i]))
        fileName = args[i];

    var output = DrawAndSave(fileName, draw);
    Console.WriteLine($"{title} CAD file created: {output}");
}

return;

static string DrawAndSave(string fileName, Action<CadDocument> draw)
{
    var doc = new CadDocument();
    CadInitializer.InitCad(doc);
    draw(doc);
    return WriteDwg(doc, AppContext.BaseDirectory, fileName);
}

static string WriteDwg(CadDocument doc, string outputDirectory, string fileName)
{
    var output = Path.Combine(outputDirectory, fileName);
    if (File.Exists(output))
    {
        File.Delete(output);
    }

    using var writer = new DwgWriter(output, doc);
    writer.Write();

    return output;
}

static void DumpStats(string path, bool asJson)
{
    using var reader = new DwgReader(path);
    var doc = reader.Read();
    var entities = doc.Entities.ToList();

    var byType = entities
        .GroupBy(entity => entity.GetType().Name)
        .OrderByDescending(group => group.Count())
        .ToDictionary(group => group.Key, group => group.Count());
    var byLayer = entities
        .GroupBy(entity => entity.Layer?.Name ?? "")
        .OrderByDescending(group => group.Count())
        .ToDictionary(group => group.Key, group => group.Count());

    var geometryEntities = entities
        .Where(entity => entity is Line or LwPolyline or Arc or Circle)
        .ToList();
    var geometryExtents = Extents(geometryEntities);
    var allExtents = Extents(entities);

    if (asJson)
        if (asJson)
        {
            var payload = new
            {
                file = Path.GetFileName(path),
                entities = entities.Count,
                byType,
                byLayer,
                bbox = allExtents is null
                    ? null
                    : new
                    {
                        minX = allExtents.Value.MinX,
                        minY = allExtents.Value.MinY,
                        maxX = allExtents.Value.MaxX,
                        maxY = allExtents.Value.MaxY,
                    },
                geometryBbox = geometryExtents is null
                    ? null
                    : new
                    {
                        minX = geometryExtents.Value.MinX,
                        minY = geometryExtents.Value.MinY,
                        maxX = geometryExtents.Value.MaxX,
                        maxY = geometryExtents.Value.MaxY,
                    },
            };
            Console.WriteLine(JsonSerializer.Serialize(payload));
            return;
        }

    Console.WriteLine($"file: {path}");
    Console.WriteLine($"entities: {entities.Count}");
    foreach (var group in byType)
    {
        Console.WriteLine($"  {group.Key, -20}{group.Value}");
    }

    Console.WriteLine("layers:");
    foreach (var group in byLayer)
    {
        Console.WriteLine($"  {group.Key, -20}{group.Value}");
    }

    if (allExtents is not null)
    {
        Console.WriteLine(
            $"bbox: ({allExtents.Value.MinX:0.###}, {allExtents.Value.MinY:0.###}) - ({allExtents.Value.MaxX:0.###}, {allExtents.Value.MaxY:0.###})"
        );
    }

    if (geometryExtents is not null)
    {
        Console.WriteLine(
            $"geometry bbox: ({geometryExtents.Value.MinX:0.###}, {geometryExtents.Value.MinY:0.###}) - ({geometryExtents.Value.MaxX:0.###}, {geometryExtents.Value.MaxY:0.###})"
        );
    }
}

static void DumpEntities(string path)
{
    using var reader = new DwgReader(path);
    var doc = reader.Read();
    var entities = doc.Entities.ToList();

    Console.WriteLine($"file: {path}");
    Console.WriteLine($"entities: {entities.Count}");

    foreach (var entity in entities)
    {
        var layer = entity.Layer?.Name ?? "";
        switch (entity)
        {
            case LwPolyline polyline:
                var points = polyline.Vertices.Select(vertex => vertex.Location);
                var xdata = RebarXData.Read(polyline);
                var rebarNote = xdata is null
                    ? ""
                    : $"  [rebar {xdata.Number} {xdata.Grade} D{xdata.Diameter:0.###} x{xdata.Count} "
                        + $"L={xdata.Length:0.###} alt={xdata.SubstituteLength}]";
                Console.WriteLine(
                    $"{entity.Handle, -8} LwPolyline  {layer, -6} closed={polyline.IsClosed} "
                        + string.Join(
                            " -> ",
                            points.Select(point => $"({point.X:0.###}, {point.Y:0.###})")
                        )
                        + rebarNote
                );
                break;
            case TextEntity text:
                Console.WriteLine(
                    $"{entity.Handle, -8} Text        {layer, -6} h={text.Height:0.###} "
                        + $"rot={text.Rotation:0.####} @({text.InsertPoint.X:0.###}, {text.InsertPoint.Y:0.###}) "
                        + $"{text.Value}"
                );
                break;
            case Insert insert:
                var attributes = string.Join(
                    ", ",
                    insert.Attributes.Select(attribute => $"{attribute.Tag}={attribute.Value}")
                );
                Console.WriteLine(
                    $"{entity.Handle, -8} Insert      {layer, -6} block={insert.Block?.Name} "
                        + $"scale={insert.XScale:0.###} @({insert.InsertPoint.X:0.###}, {insert.InsertPoint.Y:0.###}) "
                        + attributes
                );
                break;
            default:
                Console.WriteLine($"{entity.Handle, -8} {entity.GetType().Name, -11} {layer}");
                break;
        }
    }
}

static (double MinX, double MinY, double MaxX, double MaxY)? Extents(IEnumerable<Entity> entities)
{
    var hasExtents = false;
    var minX = 0.0;
    var minY = 0.0;
    var maxX = 0.0;
    var maxY = 0.0;
    foreach (var entity in entities)
    {
        try
        {
            var box = entity.GetBoundingBox();
            if (
                double.IsNaN(box.Min.X)
                || double.IsNaN(box.Max.X)
                || double.IsInfinity(box.Min.X)
                || double.IsInfinity(box.Max.X)
            )
            {
                continue;
            }

            if (!hasExtents)
            {
                minX = box.Min.X;
                minY = box.Min.Y;
                maxX = box.Max.X;
                maxY = box.Max.Y;
                hasExtents = true;
            }
            else
            {
                minX = Math.Min(minX, box.Min.X);
                minY = Math.Min(minY, box.Min.Y);
                maxX = Math.Max(maxX, box.Max.X);
                maxY = Math.Max(maxY, box.Max.Y);
            }
        }
        catch (Exception)
        {
            // 个别实体（如无几何的块参照）取不到包围盒，跳过即可。
        }
    }

    return hasExtents ? (minX, minY, maxX, maxY) : null;
}
