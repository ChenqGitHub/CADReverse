using ACadSharp;
using ACadSharp.IO;
using CadCli.Generated;
using DwgSharpKit.Infrastructure;

// 每张图各自生成一个独立 DWG 文件；命令行参数可按顺序覆盖各图的输出文件名。
var drawings = new (string Title, string FileName, Action<CadDocument> Draw)[]
{
    ("支座加强钢筋图", "bearing-reinforcement.dwg", GeneratedDraw.DrawBearingReinforcement),
    ("框架桥钢筋图", "frame-bridge-reinforcement.dwg", GeneratedDraw.DrawFrameBridgeReinforcement),
};

for (var i = 0; i < drawings.Length; i++)
{
    var (title, fileName, draw) = drawings[i];
    if (args.Length > i && !string.IsNullOrWhiteSpace(args[i]))
        fileName = args[i];

    var output = DrawAndSave(fileName, draw);
    Console.WriteLine($"{title} CAD file created: {output}");
}

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
