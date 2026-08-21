using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

public static class HRB400Block
{
    public const string Name = "HRB400";
    public const string GradeTag = "编号";
    public const string SpecTag = "直径";

    public static BlockRecord EnsureDefined(CadDocument doc)
    {
        if (doc.BlockRecords.Contains(Name))
        {
            return doc.BlockRecords[Name];
        }

        var block = new BlockRecord(Name);
        doc.BlockRecords.Add(block);

        var layer = doc.Layer(CadLayers.B07);

        block.Entities.Add(CadDraw.Line(CadDraw.P(-7.4, -11.5), CadDraw.P(5.4, -11.5), layer));
        block.Entities.Add(CadDraw.Line(CadDraw.P(2.8, 11.5), CadDraw.P(0.8, -11.5), layer));
        block.Entities.Add(CadDraw.Line(CadDraw.P(-0.8, 11.5), CadDraw.P(-2.8, -11.5), layer));
        block.Entities.Add(CadDraw.Circle(CadDraw.P(0, 0, 0), 7.2, layer));
        var grade = BlockAttributeHelper.Define(
            GradeTag,
            "钢筋编号",
            Name,
            CadDraw.P(-11.2347, -0.0201),
            25,
            layer
        );
        
        grade.HorizontalAlignment = TextHorizontalAlignment.Right;
        grade.VerticalAlignment = TextVerticalAlignmentType.Middle;

        block.Entities.Add(grade);

        var spec = BlockAttributeHelper.Define(
            SpecTag,
            "钢筋直径",
            "12",
            CadDraw.P(13.1230, 0),
            25,
            layer
        );
        spec.HorizontalAlignment = TextHorizontalAlignment.Left;
        spec.VerticalAlignment = TextVerticalAlignmentType.Middle;
        block.Entities.Add(spec);

        return block;
    }

    public static Insert Insert(
        CadDocument doc,
        XYZ insertPoint,
        string grade = Name,
        string spec = "",
        double scale = 1,
        double rotation = 0
    )
    {
        var block = EnsureDefined(doc);
        var insert = new Insert(block)
        {
            InsertPoint = insertPoint,
            XScale = scale,
            YScale = scale,
            ZScale = scale,
            Rotation = rotation,
        };

        insert.UpdateAttributes();

        foreach (var definition in insert.Block.AttributeDefinitions)
        {
            var value = definition.Tag switch
            {
                GradeTag => grade,
                SpecTag => spec,
                _ => definition.Value,
            };

            var attribute = insert.Attributes.FirstOrDefault(x => x.Tag == definition.Tag);
            if (attribute is null)
            {
                attribute = new AttributeEntity(definition);
                insert.Attributes.Add(attribute);
            }

            BlockAttributeHelper.Sync(attribute, definition, insert);
            attribute.Value = value;
        }

        return insert;
    }
}
