using System.Threading;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSMath;
using CSMath.Extensions;
using CSMath.Geometry;
using DwgSharpKit.Standards;

namespace DwgSharpKit;

public static partial class CadDraw
{
    private static int _dimensionBlockIndex;

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
    /// 按明确的测量方向和样式创建旋转线性标注，适用于从源图恢复标注。
    /// 测量点不必与尺寸线平行；图形缓存仍属于原生 DIMENSION 实体。
    /// </summary>
    /// <param name="startPoint">第一条尺寸界线的测量原点</param>
    /// <param name="endPoint">第二条尺寸界线的测量原点</param>
    /// <param name="dimensionLinePoint">尺寸线通过的位置点</param>
    /// <param name="rotation">尺寸线方向，逆时针弧度；水平为 0，竖直为 π/2</param>
    /// <param name="style">源标注样式，须属于目标文档</param>
    /// <param name="layer">所在图层，默认 B-03 标注</param>
    /// <param name="text">源文字覆盖内容；空串表示显示测量值</param>
    /// <param name="textMiddlePoint">源文字中心；未提供时按样式自动计算</param>
    /// <param name="isTextUserDefinedLocation">保留源图手动定位文字的标志</param>
    /// <param name="dimensionLineExtension">尺寸线两端额外延长量，使用世界坐标单位</param>
    /// <returns>保留测量点、文字覆盖及标注样式的原生线性标注</returns>
    public static DimensionLinear RotatedDimension(
        XYZ startPoint,
        XYZ endPoint,
        XYZ dimensionLinePoint,
        double rotation,
        DimensionStyle style,
        Layer? layer = null,
        string text = "",
        XYZ? textMiddlePoint = null,
        bool isTextUserDefinedLocation = false,
        double dimensionLineExtension = 0
    )
    {
        ArgumentNullException.ThrowIfNull(style);
        layer ??= (
            CurrentDocument
            ?? throw new InvalidOperationException(
                "CadDraw.CurrentDocument 未设置，无法解析默认图层。"
            )
        ).Layers[CadLayers.B03.Name];

        DimensionLinear dimensionLinear = new()
        {
            FirstPoint = startPoint,
            SecondPoint = endPoint,
            DefinitionPoint = dimensionLinePoint,
            Rotation = rotation,
            Text = text,
            Style = style,
            Layer = layer,
        };

        BuildLinearPicture(dimensionLinear, textMiddlePoint, dimensionLineExtension);
        // 源标注的 0 表示不额外旋转文字；缓存文字已按尺寸线方向排布。
        dimensionLinear.TextRotation = 0;
        dimensionLinear.IsTextUserDefinedLocation = isTextUserDefinedLocation;
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
        for (var i = 0; i + 1 < pts.Count; i++)
        {
            dims.Add(RotatedDimension(pts[i], pts[i + 1], offset, layer));
        }
        return dims;
    }

    /// <summary>
    /// 创建一组共用尺寸线方向、尺寸线位置和文字基线的连续标注。
    /// 相邻测量点之间各生成一个原生线性标注；各段尺寸线位于同一条线上，
    /// 文字中心沿标注方向取该段测量点的投影中点，垂直方向与
    /// <paramref name="textLinePoint"/> 对齐。
    /// </summary>
    /// <param name="points">依次排列的测量点；相邻两点之间各生成一段标注</param>
    /// <param name="dimensionLinePoint">共用尺寸线上的位置点；每段定义点沿尺寸方向投影到对应终点</param>
    /// <param name="rotation">共用的尺寸线方向，逆时针弧度</param>
    /// <param name="style">所有标注使用的尺寸样式</param>
    /// <param name="textLinePoint">文字基线上的参考点；各段文字中心沿其尺寸方向定位</param>
    /// <param name="layer">所在图层，默认 B-03 标注</param>
    /// <param name="dimensionLineExtensions">每段尺寸线两端的延长量；省略时各段均为 0</param>
    /// <returns>连续排列的原生线性标注实体</returns>
    public static List<DimensionLinear> ChainDimension(
        IEnumerable<XYZ> points,
        XYZ dimensionLinePoint,
        double rotation,
        DimensionStyle style,
        XYZ textLinePoint,
        Layer? layer = null,
        IReadOnlyList<double>? dimensionLineExtensions = null
    )
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(style);

        var pts = points.ToList();
        var segmentCount = Math.Max(pts.Count - 1, 0);
        if (dimensionLineExtensions is not null && dimensionLineExtensions.Count != segmentCount)
        {
            throw new ArgumentException(
                "尺寸线延长量数量必须与连续标注段数一致。",
                nameof(dimensionLineExtensions)
            );
        }

        var axis = new XYZ(Math.Cos(rotation), Math.Sin(rotation), 0);
        var normal = new XYZ(-axis.Y, axis.X, 0);
        var dimensionLineOffset = dimensionLinePoint.Dot(normal);
        var textLineOffset = textLinePoint.Dot(normal);
        var dimensions = new List<DimensionLinear>(segmentCount);

        for (var i = 0; i < segmentCount; i++)
        {
            var startPoint = pts[i];
            var endPoint = pts[i + 1];
            var midpoint = (startPoint + endPoint) / 2;
            var segmentDimensionLinePoint =
                axis * endPoint.Dot(axis) + normal * dimensionLineOffset;
            var segmentTextMiddlePoint = midpoint + normal * (textLineOffset - midpoint.Dot(normal));
            var extension = dimensionLineExtensions is null ? 0 : dimensionLineExtensions[i];

            dimensions.Add(
                RotatedDimension(
                    startPoint,
                    endPoint,
                    segmentDimensionLinePoint,
                    rotation,
                    style,
                    layer,
                    text: "",
                    textMiddlePoint: segmentTextMiddlePoint,
                    dimensionLineExtension: extension
                )
            );
        }

        return dimensions;
    }

    /// <summary>
    /// 创建连续（链条）标注，逐段可带文字覆盖（如 "2×128" 这类"根数×间距"注记）。
    /// 文字在构建图形缓存**之前**写入，不会出现缓存与属性不一致的"重影"；
    /// <paramref name="texts"/> 中为 null/空串的段显示测量值。
    /// 每段仍是独立原生 DIMENSION，实体数量不变。
    /// </summary>
    /// <param name="points">依次排列的测量点；相邻两点之间各生成一段标注</param>
    /// <param name="dimensionLinePoint">共用尺寸线上的位置点；每段定义点沿尺寸方向投影到对应终点</param>
    /// <param name="rotation">共用的尺寸线方向，逆时针弧度</param>
    /// <param name="style">所有标注使用的尺寸样式</param>
    /// <param name="layer">所在图层</param>
    /// <param name="texts">逐段文字覆盖，数量必须与段数一致</param>
    /// <returns>连续排列的原生线性标注实体</returns>
    public static List<DimensionLinear> ChainDimension(
        IEnumerable<XYZ> points,
        XYZ dimensionLinePoint,
        double rotation,
        DimensionStyle style,
        Layer? layer,
        IReadOnlyList<string?> texts
    )
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(texts);

        var pts = points.ToList();
        var segmentCount = Math.Max(pts.Count - 1, 0);
        if (texts.Count != segmentCount)
        {
            throw new ArgumentException(
                "逐段文字数量必须与连续标注段数一致。",
                nameof(texts)
            );
        }

        var axis = new XYZ(Math.Cos(rotation), Math.Sin(rotation), 0);
        var normal = new XYZ(-axis.Y, axis.X, 0);
        var dimensionLineOffset = dimensionLinePoint.Dot(normal);
        var dimensions = new List<DimensionLinear>(segmentCount);

        for (var i = 0; i < segmentCount; i++)
        {
            var segmentDimensionLinePoint =
                axis * pts[i + 1].Dot(axis) + normal * dimensionLineOffset;
            dimensions.Add(
                RotatedDimension(
                    pts[i],
                    pts[i + 1],
                    segmentDimensionLinePoint,
                    rotation,
                    style,
                    layer,
                    text: texts[i] ?? ""
                )
            );
        }

        return dimensions;
    }

    /// <summary>
    /// 创建连续（链条）标注，文字按与 <see cref="RotatedDimension(XYZ, XYZ, XYZ, double, DimensionStyle, Layer?, string, XYZ?, bool, double)"/>
    /// 相同的规则自动定位（尺寸线外侧、间距 + 半字高），用于把逐条写的连续标注折叠成一次调用。
    /// 每段仍是独立原生 DIMENSION，实体数量不变。
    /// </summary>
    /// <param name="points">依次排列的测量点；相邻两点之间各生成一段标注</param>
    /// <param name="dimensionLinePoint">共用尺寸线上的位置点；每段定义点沿尺寸方向投影到对应终点</param>
    /// <param name="rotation">共用的尺寸线方向，逆时针弧度</param>
    /// <param name="style">所有标注使用的尺寸样式</param>
    /// <param name="layer">所在图层，默认 B-03 标注</param>
    /// <param name="dimensionLineExtensions">每段尺寸线两端的延长量；省略时各段均为 0</param>
    /// <returns>连续排列的原生线性标注实体</returns>
    public static List<DimensionLinear> ChainDimension(
        IEnumerable<XYZ> points,
        XYZ dimensionLinePoint,
        double rotation,
        DimensionStyle style,
        Layer? layer = null,
        IReadOnlyList<double>? dimensionLineExtensions = null
    )
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(style);

        var pts = points.ToList();
        var segmentCount = Math.Max(pts.Count - 1, 0);
        if (dimensionLineExtensions is not null && dimensionLineExtensions.Count != segmentCount)
        {
            throw new ArgumentException(
                "尺寸线延长量数量必须与连续标注段数一致。",
                nameof(dimensionLineExtensions)
            );
        }

        var axis = new XYZ(Math.Cos(rotation), Math.Sin(rotation), 0);
        var normal = new XYZ(-axis.Y, axis.X, 0);
        var dimensionLineOffset = dimensionLinePoint.Dot(normal);
        var dimensions = new List<DimensionLinear>(segmentCount);

        for (var i = 0; i < segmentCount; i++)
        {
            var segmentDimensionLinePoint =
                axis * pts[i + 1].Dot(axis) + normal * dimensionLineOffset;
            dimensions.Add(
                RotatedDimension(
                    pts[i],
                    pts[i + 1],
                    segmentDimensionLinePoint,
                    rotation,
                    style,
                    layer,
                    dimensionLineExtension: dimensionLineExtensions is null
                        ? 0
                        : dimensionLineExtensions[i]
                )
            );
        }

        return dimensions;
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
    /// <param name="textMiddlePoint">可选的源文字中心</param>
    /// <param name="dimensionLineExtension">尺寸线两端额外延长量，使用世界坐标单位</param>
    private static void BuildLinearPicture(
        DimensionLinear dim,
        XYZ? textMiddlePoint = null,
        double dimensionLineExtension = 0
    )
    {
        var style = dim.Style;
        var sf = style.ScaleFactor;

        var block = new BlockRecord($"*D{Interlocked.Increment(ref _dimensionBlockIndex)}")
        {
            IsAnonymous = true,
        };
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
                new Line(
                    dimRef1 - dir * dimensionLineExtension,
                    dimRef2 + dir * dimensionLineExtension
                )
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
            var deltaRef1 = dimRef1 - dim.FirstPoint;
            var deltaRef2 = dimRef2 - dim.SecondPoint;
            var lengthRef1 = deltaRef1.GetLength();
            var lengthRef2 = deltaRef2.GetLength();
            // 测量点恰好位于尺寸线上时，使用标注法向，避免对零向量归一化。
            var dirRef1 = lengthRef1 > 1e-9 ? deltaRef1 / lengthRef1 : yVec;
            var dirRef2 = lengthRef2 > 1e-9 ? deltaRef2 / lengthRef2 : yVec;
            var dimexo = style.ExtensionLineOffset * sf;
            var dimexe = style.ExtensionLineExtension * sf;
            var fixedLength = style.FixedExtensionLineLength * sf;
            // 固定长度从尺寸线向测量点回退，仍遵守原点与界线间的最小间距。
            var startOffset1 = lengthRef1 <= 1e-9 ? 0
                : style.IsExtensionLineLengthFixed
                    ? Math.Max(dimexo, lengthRef1 - fixedLength)
                    : dimexo;
            var startOffset2 = lengthRef2 <= 1e-9 ? 0
                : style.IsExtensionLineLengthFixed
                    ? Math.Max(dimexo, lengthRef2 - fixedLength)
                    : dimexo;
            if (!style.SuppressFirstExtensionLine)
            {
                block.Entities.Add(
                    ExtensionLine(
                        dim.FirstPoint + startOffset1 * dirRef1,
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
                        dim.SecondPoint + startOffset2 * dirRef2,
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
        var gap = (style.DimensionLineGap + style.TextHeight / 2) * sf * side;
        var textPos = textMiddlePoint ?? textRef + gap * yVec;
        dim.TextMiddlePoint = textPos;

        // 文字沿尺寸线方向旋转、字头向上：方向角在 (-90°, 90°] 内直接沿尺寸线，
        // 否则翻转 180°，保证竖排读数自下而上。
        var textRotation = dim.Rotation;
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

        var block = new BlockRecord($"*D{Interlocked.Increment(ref _dimensionBlockIndex)}")
        {
            IsAnonymous = true,
        };
        dim.Block = block;

        var twoPi = Math.PI * 2;
        var ccw = ((endAngle - startAngle) % twoPi + twoPi) % twoPi;
        if (ccw > Math.PI)
        {
            (startAngle, endAngle) = (endAngle, startAngle);
            ccw = twoPi - ccw;
        }

        var valid =
            !center.IsNaN()
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

        var tickHalf = style.TickSize * sf / 2;
        var dimexo = style.ExtensionLineOffset * sf;
        var dimexe = style.ExtensionLineExtension * sf;

        if (valid)
        {
            foreach (
                var (angle, lineType) in new[]
                {
                    (startAngle, style.LineTypeExt1),
                    (endAngle, style.LineTypeExt2),
                }
            )
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

        var midAngle = valid ? startAngle + ccw / 2 : startAngle;
        // 文字中心距弧线 = 间距 + 半文字高，避免文字压弧。
        var gap = (style.DimensionLineGap + style.TextHeight / 2) * sf;
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
    /// 创建标注箭头：指定箭头块且 TickSize 为 0 时插入该块，否则绘制 45° 短线。
    /// </summary>
    /// <param name="point">箭头中心（尺寸线端点）</param>
    /// <param name="dimDir">尺寸线方向</param>
    /// <param name="style">标注样式</param>
    /// <param name="layer">所在图层</param>
    /// <returns>箭头块引用或倾斜箭头线实体</returns>
    private static Entity ArrowTick(XYZ point, XYZ dimDir, DimensionStyle style, Layer layer)
    {
        if (style.TickSize == 0 && style.ArrowBlock is not null)
        {
            var scale = style.ArrowSize * style.ScaleFactor;
            return new Insert(style.ArrowBlock)
            {
                InsertPoint = point,
                Rotation = Math.Atan2(dimDir.Y, dimDir.X),
                XScale = scale,
                YScale = scale,
                ZScale = scale,
                Color = style.DimensionLineColor,
                LineType = style.LineType,
                LineWeight = style.DimensionLineWeight,
                Layer = layer,
            };
        }

        var perp = new XYZ(-dimDir.Y, dimDir.X, 0);
        var tickDir = (dimDir + perp).Normalize();
        var half = style.TickSize * style.ScaleFactor / 2;
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
