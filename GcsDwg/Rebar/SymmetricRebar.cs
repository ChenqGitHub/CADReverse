using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using CSMath;

namespace GcsDwg.Rebar;

/// <summary>
/// 对称线筋：左右两半关于竖直对称轴镜像。节点模型，返回 1:1 本地图元（不自动入文档），
/// 供使用者用 <see cref="GcsDwg.CadDocumentExtensions.AddTranslated"/> 复用放置。
/// 拼接镜像后会自动合并对称轴上的共线点，使恰好穿过对称轴的中间段成为一整段，
/// 标注取整段长度、文本居中于对称轴。
/// </summary>
public static class SymmetricRebar
{
    private const double Eps = 1e-9;

    /// <summary>
    /// 生成对称线筋图元（1:1 本地坐标，未入文档）。
    /// </summary>
    /// <param name="half">一侧（自最外侧端到对称轴）的顶点序列；Text 挂在各段起点上</param>
    public static List<Entity> Add(
        CadDocument doc,
        IReadOnlyList<RebarVertex> half,
        RebarDetailOptions? options = null
    )
    {
        double axis = half[^1].Point.X;

        // 右半镜像：去掉与轴重合点，绕 X
        var mirror = half
            .SkipLast(1)
            .Reverse()
            .Select(v => new RebarVertex(new XY(2 * axis - v.Point.X, v.Point.Y), v.Bulge, v.Text));

        // 拼接后合并共线点：对称轴穿过中间某段时，轴上的中间点应移除，
        // 使该段成为一整段（文本随之整段标注，不拆半）。
        var merged = MergeCollinear(half.Concat(mirror).ToList());

        return RebarDetail.Add(doc, merged, options);
    }

    /// <summary>
    /// 删除三点共线时的中间点。被合并段起点文本置空，标注自动取整段长度。
    /// </summary>
    private static List<RebarVertex> MergeCollinear(List<RebarVertex> verts)
    {
        if (verts.Count < 3)
        {
            return verts;
        }

        var result = new List<RebarVertex>(verts.Count);
        for (int i = 0; i < verts.Count; i++)
        {
            var p = verts[i];
            if (result.Count >= 2 && IsCollinear(result[^2], result[^1], p))
            {
                // 中点在轴上共线：移除，并把前段文本置空使整段自动取长度
                result[^1] = result[^1] with { Text = null };
                continue;
            }
            result.Add(p);
        }
        return result;
    }

    private static bool IsCollinear(RebarVertex a, RebarVertex b, RebarVertex c)
    {
        var ab = new XY(b.Point.X - a.Point.X, b.Point.Y - a.Point.Y);
        var ac = new XY(c.Point.X - a.Point.X, c.Point.Y - a.Point.Y);
        return Math.Abs(ab.X * ac.Y - ab.Y * ac.X) < Eps;
    }
}