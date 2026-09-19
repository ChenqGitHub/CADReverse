using ACadSharp;

namespace CadCli.Generated;

public static partial class GeneratedDraw
{
    public static void DrawPierStructure(CadDocument doc) => Pier.PierStructureDrawing.Draw(doc);

    public static void DrawPierBodyReinforcement(CadDocument doc) =>
        Pier.PierBodyReinforcementDrawing.Draw(doc);

    public static void PierCapReinforcementDrawing(CadDocument doc) =>
        Pier.PierCapReinforcementDrawing.Draw(doc);

    public static void DrawBearingPedestal(CadDocument doc) =>
        Pier.BearingPedestalDrawing.Draw(doc);
}
