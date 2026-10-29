using ACadSharp;

namespace CadCli.Generated.Pier;

public static class GeneratedDraw
{
    public static void DrawPierStructure(CadDocument doc) => PierStructureDrawing.Draw(doc);

    public static void DrawPierBodyReinforcement(CadDocument doc) =>
        PierBodyReinforcementDrawing.Draw(doc);

    public static void PierCapReinforcementDrawing(CadDocument doc) =>
        Pier.PierCapReinforcementDrawing.Draw(doc);
}
