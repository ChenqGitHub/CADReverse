using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

public static class BlockAttributeHelper
{
    public static AttributeDefinition Define(
        string tag,
        string prompt,
        string value,
        XYZ insertPoint,
        double height,
        Layer layer
    ) =>
        new()
        {
            Tag = tag,
            Prompt = prompt,
            Value = value,
            InsertPoint = insertPoint,
            AlignmentPoint = insertPoint,
            Height = height,
            Layer = layer,
            IsLocked = true,
            Style = layer.Document.TextStyles[CadTextStyles.JstiSimsun.Name],
            WidthFactor = layer
                .Document.TextStyles[CadTextStyles.JstiSimsun.Name]
                .Width,
        };

    public static void Sync(AttributeEntity attribute, AttributeDefinition definition, Insert insert)
    {
        attribute.InsertPoint = Transform(definition.InsertPoint, insert);
        attribute.AlignmentPoint = Transform(definition.AlignmentPoint, insert);
        attribute.Height = definition.Height * insert.YScale;
        attribute.Rotation = definition.Rotation + insert.Rotation;
        attribute.HorizontalAlignment = definition.HorizontalAlignment;
        attribute.VerticalAlignment = definition.VerticalAlignment;
        attribute.WidthFactor = definition.WidthFactor;
        attribute.ObliqueAngle = definition.ObliqueAngle;
        attribute.Style = definition.Style;
        attribute.Layer = definition.Layer;
    }

    private static XYZ Transform(XYZ point, Insert insert)
    {
        var x = point.X * insert.XScale;
        var y = point.Y * insert.YScale;
        var cos = Math.Cos(insert.Rotation);
        var sin = Math.Sin(insert.Rotation);

        return new XYZ(
            insert.InsertPoint.X + x * cos - y * sin,
            insert.InsertPoint.Y + x * sin + y * cos,
            insert.InsertPoint.Z + point.Z * insert.ZScale
        );
    }
}
