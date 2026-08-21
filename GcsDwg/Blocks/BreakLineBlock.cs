using System;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

/// <summary>
/// 折断线块：1:1 建在原点（端点 ±5），按比例缩放、按用户两点距离对称移动两端点、
/// 延长两端、旋转到用户方向、平移到两端点中点。
/// </summary>
public static class BreakLineBlock
{
    private const double HalfSpan = 5; // 1:1 断点端距原点
    private const double ZigStart = 0.5; // 1:1 折线起始（距中心）
    private const double ZigEnd = 0.25; // 1:1 折线峰谷 x
    private const double ZigDepth = 0.686869; // 1:1 折线峰谷 y

    /// <summary>
    /// 绘制折断线。1:1 建在原点（端点 ±5），按比例缩放（绕原点），
    /// 再按用户两端点距离将两个端点对称移动到 ±d/2，延长两端，
    /// 按用户两端点方向旋转并平移到中点（端点无需贴合）。
    /// </summary>
    /// <param name="doc">CAD 文档</param>
    /// <param name="left">左端点（决定距离、方向、中点）</param>
    /// <param name="right">右端点（决定距离、方向、中点）</param>
    /// <param name="layer">图层</param>
    /// <param name="scale">整体缩放比例（绕原点），默认 1</param>
    /// <param name="extendL">左端延长距离</param>
    /// <param name="extendR">右端延长距离</param>
    /// <returns>折断线多段线实体</returns>
    public static LwPolyline Insert(
        CadDocument doc,
        XYZ left,
        XYZ right,
        Layer layer,
        double scale = 1,
        double extendL = 0,
        double extendR = 0
    )
    {
        // 1. 1:1 建在原点，端点 ±5
        var pl = CadDraw.Polyline(
            [
                CadDraw.V(-HalfSpan, 0),
                CadDraw.V(-ZigStart, 0),
                CadDraw.V(-ZigEnd, -ZigDepth),
                CadDraw.V(ZigEnd, ZigDepth),
                CadDraw.V(ZigStart, 0),
                CadDraw.V(HalfSpan, 0),
            ],
            layer
        );

        var origin = new XYZ(0, 0, 0);

        // 2. 按比例绕原点缩放
        pl.ApplyScaling(new XYZ(scale, scale, 1), origin);

        // 3. 按用户两端点距离对称移动两个端点：-d/2、+d/2
        double d = Math.Sqrt(
            (right.X - left.X) * (right.X - left.X) + (right.Y - left.Y) * (right.Y - left.Y)
        );
        double half = d / 2;
        pl.Vertices[0].Location = new XY(-half, 0);
        pl.Vertices[^1].Location = new XY(half, 0);

        // 4. 两侧延长
        if (extendL != 0)
        {
            pl.Vertices.Insert(0, new LwPolyline.Vertex(new XY(-half - extendL, 0)));
        }
        if (extendR != 0)
        {
            pl.Vertices.Add(new LwPolyline.Vertex(new XY(half + extendR, 0)));
        }

        // 5. 按两端点方向旋转
        double dir = Math.Atan2(right.Y - left.Y, right.X - left.X);
        pl.ApplyRotation(XYZ.AxisZ, dir);

        // 6. 平移到两端点中点
        pl.ApplyTranslation(new XYZ((left.X + right.X) / 2, (left.Y + right.Y) / 2, 0));

        doc.Entities.Add(pl);
        return pl;
    }
}
