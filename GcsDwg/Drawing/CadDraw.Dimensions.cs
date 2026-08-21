using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSMath;
using CSMath.Geometry;
using GcsDwg.Standards;

namespace GcsDwg;

public static partial class CadDraw
{
    /// <summary>
    /// 当前 CAD 文档，用于解析默认图层、样式等。
    /// 由 CadInitializer.InitCad 初始化时设置。
    /// </summary>
    public static CadDocument? CurrentDocument { get; set; }

    /// <summary>
    /// 创建旋转线性标注。由标注起点、终点定位，尺寸线通过指定位置点。
    /// </summary>
    /// <param name="startPoint">标注起点（第一条尺寸界线原点）</param>
    /// <param name="endPoint">标注终点（第二条尺寸界线原点）</param>
    /// <param name="dimensionLinePoint">尺寸线所在位置</param>
    /// <param name="layer">所在图层，默认 B-03 标注</param>
    /// <param name="text">文字覆盖内容，非空时直接显示该内容</param>
    /// <returns>线性标注实体</returns>
    public static DimensionLinear RotatedDimension(
        XYZ startPoint,
        XYZ endPoint,
        XYZ dimensionLinePoint,
        Layer? layer = null,
        string text = ""
    )
    {
        layer ??= (
            CurrentDocument
            ?? throw new InvalidOperationException(
                "CadDraw.CurrentDocument 未设置，无法解析默认图层。"
            )
        ).Layers[CadLayers.B03.Name];

        var delta = endPoint - startPoint;
        var direction = delta.GetLength() > 0 ? delta.Normalize() : XYZ.AxisX;

        DimensionLinear dimensionLinear = new()
        {
            FirstPoint = startPoint,
            SecondPoint = endPoint,
            DefinitionPoint = dimensionLinePoint,
            Rotation = Math.Atan2(direction.Y, direction.X),
            Text = text,
            Style = layer.Document.DimensionStyles[CadDimStyles.FangSong1_50.Name],
            Layer = layer,
        };

        BuildLinearPicture(dimensionLinear);
        return dimensionLinear;
    }

    /// <summary>
    /// 创建旋转线性标注。由标注起点、终点定位，尺寸线位于测量线段中点沿法向偏移 offset 处。
    /// </summary>
    /// <param name="startPoint">标注起点（第一条尺寸界线原点）</param>
    /// <param name="endPoint">标注终点（第二条尺寸界线原点）</param>
    /// <param name="offset">尺寸线相对测量线中点的垂直偏移（沿测量线法向，正值为左/上侧）</param>
    /// <param name="layer">所在图层，默认 B-03 标注</param>
    /// <param name="text">文字覆盖内容，非空时直接显示该内容</param>
    /// <returns>线性标注实体</returns>
    public static DimensionLinear RotatedDimension(
        XYZ startPoint,
        XYZ endPoint,
        double offset,
        Layer? layer = null,
        string text = ""
    )
    {
        var delta = endPoint - startPoint;
        var dir = delta.GetLength() > 0 ? delta.Normalize() : XYZ.AxisX;
        var normal = new XYZ(-dir.Y, dir.X, 0);
        var mid = (startPoint + endPoint) / 2;
        return RotatedDimension(startPoint, endPoint, mid + normal * offset, layer, text);
    }

    /// <summary>
    /// 创建连续（链条）标注：沿一系列连续点，在相邻点之间各生成一段旋转线性标注。
    /// 每段标注的尺寸线位于该段中点沿测量线法向偏移 offset 处。
    /// </summary>
    /// <param name="points">依次排列的测量点（相邻两点之间各生成一段标注）</param>
    /// <param name="offset">尺寸线相对测量线的垂直偏移（沿测量线法向）</param>
    /// <param name="layer">所在图层，默认 B-03 标注</param>
    /// <returns>线性标注实体列表</returns>
    public static List<DimensionLinear> ChainDimension(
        IEnumerable<XYZ> points,
        double offset,
        Layer? layer = null
    )
    {
        var pts = points.ToList();
        var dims = new List<DimensionLinear>(Math.Max(pts.Count - 1, 0));
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            dims.Add(RotatedDimension(pts[i], pts[i + 1], offset, layer));
        }
        return dims;
    }

    /// <summary>
    /// 创建两条线角度标注。
    /// </summary>
    /// <param name="firstPoint">第一条线端点</param>
    /// <param name="secondPoint">第二条线端点</param>
    /// <param name="angleVertex">角度顶点</param>
    /// <param name="dimensionArc">标注弧位置点</param>
    /// <param name="layer">所在图层</param>
    /// <param name="text">文字覆盖内容</param>
    /// <returns>角度标注实体</returns>
    public static DimensionAngular2Line Angular2Line(
        XYZ firstPoint,
        XYZ secondPoint,
        XYZ angleVertex,
        XYZ dimensionArc,
        Layer layer,
        string text = ""
    )
    {
        DimensionAngular2Line dimensionAngular = new()
        {
            FirstPoint = firstPoint,
            SecondPoint = secondPoint,
            AngleVertex = angleVertex,
            DimensionArc = dimensionArc,
            DefinitionPoint = dimensionArc,
            Text = text,
            Style = layer.Document.DimensionStyles[CadDimStyles.FangSong1_50.Name],
            Layer = layer,
        };

        BuildAngular2LinePicture(dimensionAngular);
        return dimensionAngular;
    }

    /// <summary>
    /// 创建三点角度标注。
    /// </summary>
    /// <param name="firstPoint">第一条线端点</param>
    /// <param name="secondPoint">第二条线端点</param>
    /// <param name="angleVertex">角度顶点</param>
    /// <param name="dimensionArc">标注弧位置点</param>
    /// <param name="layer">所在图层</param>
    /// <param name="text">文字覆盖内容</param>
    /// <returns>角度标注实体</returns>
    public static DimensionAngular3Pt Angular3Point(
        XYZ firstPoint,
        XYZ secondPoint,
        XYZ angleVertex,
        XYZ dimensionArc,
        Layer layer,
        string text = ""
    )
    {
        DimensionAngular3Pt dimensionAngular = new()
        {
            FirstPoint = firstPoint,
            SecondPoint = secondPoint,
            AngleVertex = angleVertex,
            DefinitionPoint = dimensionArc,
            Text = text,
            Style = layer.Document.DimensionStyles[CadDimStyles.FangSong1_50.Name],
            Layer = layer,
        };

        BuildAngular3PointPicture(dimensionAngular);
        return dimensionAngular;
    }

    /// <summary>
    /// 生成线性标注图形块：尺寸线、倾斜箭头、尺寸界线、文字。
    /// 图形块随实体写入 DWG，CAD 打开即显示，无需重生成。
    /// </summary>
    /// <param name="dim">线性标注实体</param>
    private static void BuildLinearPicture(DimensionLinear dim)
    {
        var style = dim.Style;
        var sf = style.ScaleFactor;

        var block = new BlockRecord($"*D{dim.Handle}") { IsAnonymous = true };
        dim.Block = block;

        var transform = Transform.CreateRotation(dim.Normal, dim.Rotation);
        var yVec = transform.ApplyTransform(XYZ.AxisY).Normalize();
        var xVec = transform.ApplyTransform(XYZ.AxisX).Normalize();

        var dimLine = new Line3D(dim.DefinitionPoint, xVec);
        var dimRef1 = new Line3D(dim.FirstPoint, yVec).FindIntersection(dimLine);
        var dimRef2 = new Line3D(dim.SecondPoint, yVec).FindIntersection(dimLine);
        var dimVec = dimRef2 - dimRef1;
        // 零长度/坐标非法的标注跳过图形绘制，避免 NaN 实体。
        var valid = !dimVec.IsNaN() && dimVec.GetLengthSquared() > 1e-18;
        var dir = valid ? dimVec.Normalize() : xVec;

        if (valid && !style.SuppressFirstDimensionLine && !style.SuppressSecondDimensionLine)
        {
            block.Entities.Add(
                new Line(dimRef1, dimRef2)
                {
                    Color = style.DimensionLineColor,
                    LineType = style.LineType,
                    LineWeight = style.DimensionLineWeight,
                    Layer = dim.Layer,
                }
            );

            block.Entities.Add(ArrowTick(dimRef1, dir, style, dim.Layer));
            block.Entities.Add(ArrowTick(dimRef2, -dir, style, dim.Layer));
        }

        if (valid)
        {
            var dirRef1 = (dimRef1 - dim.FirstPoint).Normalize();
            var dirRef2 = (dimRef2 - dim.SecondPoint).Normalize();
            double dimexo = style.ExtensionLineOffset * sf;
            double dimexe = style.ExtensionLineExtension * sf;
            if (!style.SuppressFirstExtensionLine)
            {
                block.Entities.Add(
                    ExtensionLine(
                        dim.FirstPoint + dimexo * dirRef1,
                        dimRef1 + dimexe * dirRef1,
                        style,
                        style.LineTypeExt1,
                        dim.Layer
                    )
                );
            }

            if (!style.SuppressSecondExtensionLine)
            {
                block.Entities.Add(
                    ExtensionLine(
                        dim.SecondPoint + dimexo * dirRef2,
                        dimRef2 + dimexe * dirRef2,
                        style,
                        style.LineTypeExt2,
                        dim.Layer
                    )
                );
            }
        }

        var textRef = (dimRef1 + dimRef2) / 2;
        // 文字偏移方向按尺寸线相对测量线的侧向决定（兼容水平/竖直标注）：
        // 偏移量沿 yVec（测量线法向）为正，文字放在尺寸线远离测量线一侧。
        // 文字中心距尺寸线 = 间距 + 半文字高，与 CAD 重生成公式（Gap + TextH/2）一致。
        var side = valid && (dimRef1 - dim.FirstPoint).Dot(yVec) >= 0 ? 1 : -1;
        double gap = (style.DimensionLineGap + style.TextHeight / 2) * sf * side;
        var textPos = textRef + gap * yVec;
        dim.TextMiddlePoint = textPos;

        // 文字沿尺寸线方向旋转、字头向上：方向角在 (-90°, 90°] 内直接沿尺寸线，
        // 否则翻转 180°，保证竖排读数自下而上。
        double textRotation = dim.Rotation;
        if (textRotation <= -Math.PI / 2 || textRotation > Math.PI / 2)
        {
            textRotation += textRotation > 0 ? -Math.PI : Math.PI;
        }

        dim.TextRotation = textRotation;

        var textValue = GetDimensionText(dim);
        if (double.IsNaN(dim.Measurement) && string.IsNullOrEmpty(dim.Text))
        {
            textValue = "";
        }

        block.Entities.Add(
            new MText
            {
                Value = textValue,
                AttachmentPoint = AttachmentPointType.MiddleCenter,
                InsertPoint = textPos,
                Height = style.TextHeight * sf,
                AlignmentPoint = new XYZ(Math.Cos(textRotation), Math.Sin(textRotation), 0),
                Style = style.Style,
                Color = style.TextColor,
                Layer = dim.Layer,
            }
        );
    }

    /// <summary>
    /// 生成两条线角度标注图形块：尺寸弧、倾斜箭头、尺寸界线、文字。
    /// 尺寸界线为两条边线从顶点沿径向延伸，弧线半径为顶点到标注弧位置点的距离。
    /// </summary>
    /// <param name="dim">两条线角度标注实体</param>
    private static void BuildAngular2LinePicture(DimensionAngular2Line dim)
    {
        var center = dim.AngleVertex;
        var radius = (dim.DimensionArc - center).GetLength();
        var a1 = Math.Atan2(dim.FirstPoint.Y - center.Y, dim.FirstPoint.X - center.X);
        var a2 = Math.Atan2(dim.SecondPoint.Y - center.Y, dim.SecondPoint.X - center.X);
        BuildAngularPicture(dim, center, radius, a1, a2);
    }

    /// <summary>
    /// 生成三点角度标注图形块：尺寸弧、倾斜箭头、尺寸界线、文字。
    /// 尺寸界线为两条边线从顶点沿径向延伸，弧线半径为顶点到标注弧位置点的距离。
    /// </summary>
    /// <param name="dim">三点角度标注实体</param>
    private static void BuildAngular3PointPicture(DimensionAngular3Pt dim)
    {
        var center = dim.AngleVertex;
        var radius = (dim.DefinitionPoint - center).GetLength();
        var a1 = Math.Atan2(dim.FirstPoint.Y - center.Y, dim.FirstPoint.X - center.X);
        var a2 = Math.Atan2(dim.SecondPoint.Y - center.Y, dim.SecondPoint.X - center.X);
        BuildAngularPicture(dim, center, radius, a1, a2);
    }

    /// <summary>
    /// 角度标注公共图形块：按短夹角绘制尺寸弧、两端倾斜箭头与尺寸界线，文字置于弧中点外侧。
    /// </summary>
    /// <param name="dim">角度标注实体</param>
    /// <param name="center">角度顶点</param>
    /// <param name="radius">标注弧半径</param>
    /// <param name="startAngle">第一条边方向角（弧度）</param>
    /// <param name="endAngle">第二条边方向角（弧度）</param>
    private static void BuildAngularPicture(
        Dimension dim,
        XYZ center,
        double radius,
        double startAngle,
        double endAngle
    )
    {
        var style = dim.Style;
        var sf = style.ScaleFactor;

        var block = new BlockRecord($"*D{dim.Handle}") { IsAnonymous = true };
        dim.Block = block;

        double twoPi = Math.PI * 2;
        double ccw = ((endAngle - startAngle) % twoPi + twoPi) % twoPi;
        if (ccw > Math.PI)
        {
            (startAngle, endAngle) = (endAngle, startAngle);
            ccw = twoPi - ccw;
        }

        var valid = !center.IsNaN()
            && !double.IsNaN(radius)
            && radius > 1e-9
            && !double.IsNaN(startAngle)
            && !double.IsNaN(endAngle)
            && ccw > 1e-9
            && Math.Abs(twoPi - ccw) > 1e-9;

        if (valid)
        {
            block.Entities.Add(
                new Arc(center, radius, startAngle, endAngle)
                {
                    Color = style.DimensionLineColor,
                    LineType = style.LineType,
                    LineWeight = style.DimensionLineWeight,
                    Layer = dim.Layer,
                }
            );
        }

        double tickHalf = style.TickSize * sf / 2;
        double dimexo = style.ExtensionLineOffset * sf;
        double dimexe = style.ExtensionLineExtension * sf;

        if (valid)
        {
            foreach (var (angle, lineType) in new[]
            {
                (startAngle, style.LineTypeExt1),
                (endAngle, style.LineTypeExt2),
            })
            {
                var radial = new XYZ(Math.Cos(angle), Math.Sin(angle), 0);
                var tangent = new XYZ(-radial.Y, radial.X, 0);
                var tickDir = (tangent + radial).Normalize();
                var p = center + radial * radius;

                block.Entities.Add(
                    new Line(p - tickDir * tickHalf, p + tickDir * tickHalf)
                    {
                        Color = style.DimensionLineColor,
                        LineType = style.LineType,
                        LineWeight = style.DimensionLineWeight,
                        Layer = dim.Layer,
                    }
                );

                block.Entities.Add(
                    ExtensionLine(p, p + radial * (dimexo + dimexe), style, lineType, dim.Layer)
                );
            }
        }

        double midAngle = valid ? startAngle + ccw / 2 : startAngle;
        // 文字中心距弧线 = 间距 + 半文字高，避免文字压弧。
        double gap = (style.DimensionLineGap + style.TextHeight / 2) * sf;
        var midDir = new XYZ(Math.Cos(midAngle), Math.Sin(midAngle), 0);
        var textPos = valid ? center + midDir * (radius + gap) : center;
        dim.TextMiddlePoint = textPos;

        var textValue = GetDimensionText(dim);
        if (double.IsNaN(dim.Measurement) && string.IsNullOrEmpty(dim.Text))
        {
            textValue = "";
        }

        block.Entities.Add(
            new MText
            {
                Value = textValue,
                AttachmentPoint = AttachmentPointType.MiddleCenter,
                InsertPoint = textPos,
                Height = style.TextHeight * sf,
                Style = style.Style,
                Color = style.TextColor,
                Layer = dim.Layer,
            }
        );
    }

    /// <summary>
    /// 标注文字内容：有文字覆盖则直接显示，否则取测量值。
    /// </summary>
    /// <param name="dim">标注实体</param>
    /// <returns>文字内容</returns>
    private static string GetDimensionText(Dimension dim) =>
        string.IsNullOrEmpty(dim.Text) ? dim.GetMeasurementText() : dim.Text;

    /// <summary>
    /// 倾斜箭头：与尺寸线成 45° 的短线。
    /// </summary>
    /// <param name="point">箭头中心（尺寸线端点）</param>
    /// <param name="dimDir">尺寸线方向</param>
    /// <param name="style">标注样式</param>
    /// <param name="layer">所在图层</param>
    /// <returns>倾斜箭头线实体</returns>
    private static Line ArrowTick(XYZ point, XYZ dimDir, DimensionStyle style, Layer layer)
    {
        var perp = new XYZ(-dimDir.Y, dimDir.X, 0);
        var tickDir = (dimDir + perp).Normalize();
        double half = style.TickSize * style.ScaleFactor / 2;
        return new Line(point - tickDir * half, point + tickDir * half)
        {
            Color = style.DimensionLineColor,
            LineType = style.LineType,
            LineWeight = style.DimensionLineWeight,
            Layer = layer,
        };
    }

    /// <summary>
    /// 尺寸界线。
    /// </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <param name="style">标注样式</param>
    /// <param name="linetype">线型</param>
    /// <param name="layer">所在图层</param>
    /// <returns>尺寸界线线实体</returns>
    private static Line ExtensionLine(
        XYZ start,
        XYZ end,
        DimensionStyle style,
        LineType? linetype,
        Layer layer
    )
    {
        return new Line(start, end)
        {
            Color = style.ExtensionLineColor,
            LineType = linetype ?? LineType.ByLayer,
            LineWeight = style.ExtensionLineWeight,
            Layer = layer,
        };
    }
}
