using System;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using GcsDwg.Standards;

namespace GcsDwg.Blocks;

/// <summary>
/// 钢筋引线：单段斜线，独立图元助手。
/// 1:1 局部定义 (0,0)-(1.5,1.36)（scale=50 放大为 (0,0)-(75,68)），
/// 插入点即引线起点（锚点）；按 缩放→旋转→平移 管线落地。
/// </summary>
public static class LeaderLine
{
    /// <summary>1:1 水平分量（×50 → 75）</summary>
    public const double Dx = 1.5;

    /// <summary>1:1 竖直分量（×50 → 68）</summary>
    public const double Dy = 68.0 / 50.0;

    /// <param name="scale">比例，默认 50</param>
    /// <param name="direction">方向（弧度），逆时针为正，0=原斜向</param>
    /// <returns>生成的直线实体</returns>
    public static Line Insert(
        CadDocument doc,
        XYZ origin = default,
        double scale = 1,
        double direction = 0
    )
    {
        var l = doc.Layer(CadLayers.B03);

        // 1:1 先建，起点在原点（锚点）
        var line = CadDraw.Line(CadDraw.P(0, 0), CadDraw.P(Dx, Dy), l);

        line.ApplyScaling(new XYZ(scale, scale, 1));
        line.ApplyRotation(XYZ.AxisZ, direction);
        line.ApplyTranslation(origin);

        return line;
    }
}
