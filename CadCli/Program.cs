using ACadSharp;
using ACadSharp.IO;
using CadCli.Generated;
using DwgSharpKit.Infrastructure;

var doc = new CadDocument();


CadInitializer.InitCad(doc);
GeneratedDraw.DrawFromPythonGeneratedCode(doc);

var output = WriteDwg(
    doc,
    AppContext.BaseDirectory,
    args.Length > 0 ? args[0] : "reverse-output.dwg"
);

Console.WriteLine($"CAD file created: {output}");

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
