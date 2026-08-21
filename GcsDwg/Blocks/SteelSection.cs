using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

public class SteelSection
{
    public const string Name = "SteelSection";

    /// <summary>
    /// 创建块对象
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static BlockRecord EnsureDefined(CadDocument doc)
    {
        if (doc.BlockRecords.Contains(Name))
            return doc.BlockRecords[Name];

        var block = new BlockRecord(Name);
        doc.BlockRecords.Add(block);

        var layer = doc.Layer(CadLayers.B01);
        block.Entities.Add(CadDraw.Circle(CadDraw.P(0, 0), 3.75, layer));

        // 2: AcDbPolyline
        block.Entities.Add(
            CadDraw.Polyline(
                [CadDraw.V(-3.75 / 2, 0, bulge: 1), CadDraw.V(3.75 / 2, 0, bulge: 1)],
                layer,
                closed: true,
                constantWidth: 3.75
            )
        );

        return block;
    }

    /// <summary>
    /// 插入块对象
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="insertPoint"></param>
    /// <param name="scale"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public static Insert Insert(
        CadDocument doc,
        XYZ insertPoint = default,
        double scale = 1,
        double rotation = 0
    )
    {
        var block = EnsureDefined(doc);
        var insert = new Insert(block)
        {
            InsertPoint = XYZ.Zero,
            XScale = scale,
            YScale = scale,
            ZScale = scale,
            Rotation = rotation,
            Layer = doc.Layer(CadLayers.B01),
        };
        insert.ApplyTranslation(insertPoint);

        return insert;
    }
}
