using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Models;
using GcsDwg.Standards;

namespace GcsDwg;

public static partial class CadDraw
{
    /// <summary>
    /// 创建单行文字实体。
    /// </summary>
    /// <param name="value">文字内容</param>
    /// <param name="insertPoint">插入点（水平/垂直对齐后即对齐点）</param>
    /// <param name="height">字高</param>
    /// <param name="layer">所在图层</param>
    /// <param name="rotation">旋转角（弧度）</param>
    /// <param name="textStyle">文字样式名（默认标准仿宋）</param>
    /// <param name="horizontalAlignment">水平对齐方式，默认左对齐</param>
    /// <param name="verticalAlignment">垂直对齐方式，默认基线对齐</param>
    /// <returns>文字实体</returns>
    public static TextEntity Text(
        string value,
        XYZ insertPoint,
        double height,
        Layer layer,
        double rotation = 0,
        string? textStyle = null,
        TextHorizontalAlignment horizontalAlignment = TextHorizontalAlignment.Left,
        TextVerticalAlignmentType verticalAlignment = TextVerticalAlignmentType.Baseline
    ) =>
        new()
        {
            Value = value,
            InsertPoint = insertPoint,
            AlignmentPoint = insertPoint,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
            Height = height,
            Rotation = rotation,
            Layer = layer,
            Style = layer.Document.TextStyles[textStyle ?? CadTextStyles.JstiSimsun.Name],
            WidthFactor = layer
                .Document.TextStyles[textStyle ?? CadTextStyles.JstiSimsun.Name]
                .Width,
        };

    /// <summary>
    /// 批量创建单行文字实体。
    /// </summary>
    /// <param name="texts">文字参数列表</param>
    /// <param name="layer">所在图层</param>
    /// <returns>文字实体列表</returns>
    public static List<TextEntity> Texts(IEnumerable<CadText> texts, Layer layer) =>
        [.. texts.Select(x => Text(x.Value, x.InsertPoint, x.Height, layer, x.Rotation))];

    /// <summary>
    /// 创建多行文字实体（MTEXT）。使用锚点对齐（AttachmentPoint），打开即按锚点渲染，
    /// 不存在 TEXT 对齐需刷新（REGEN）才生效的问题。
    /// </summary>
    /// <param name="value">文字内容</param>
    /// <param name="insertPoint">锚点位置</param>
    /// <param name="height">字高</param>
    /// <param name="layer">所在图层</param>
    /// <param name="attachment">锚点对齐方式，默认正中</param>
    /// <param name="textStyle">文字样式名（默认标准仿宋）</param>
    /// <returns>多行文字实体</returns>
    public static MText MText(
        string value,
        XYZ insertPoint,
        double height,
        Layer layer,
        AttachmentPointType attachment = AttachmentPointType.MiddleCenter,
        string? textStyle = null
    ) =>
        new()
        {
            Value = value,
            InsertPoint = insertPoint,
            AttachmentPoint = attachment,
            Height = height,
            AlignmentPoint = new XYZ(1, 0, 0),
            Style = layer.Document.TextStyles[textStyle ?? CadTextStyles.JstiSimsun.Name],
            Layer = layer,
        };
}
