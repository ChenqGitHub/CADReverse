using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using CSMath;

namespace DwgSharpKit.Rebar;

/// <summary>
/// 箍筋：矩形轮廓。节点模型，返回 1:1 本地图元（不自动入文档），
/// 供使用者用 <see cref="DwgSharpKit.CadDocumentExtensions.AddTranslated"/> 复用放置。
/// </summary>
public static class Stirrup
{
    /// <summary>
    /// 生成矩形箍筋图元（1:1 本地坐标，未入文档）。
    /// </summary>
    /// <param name="doc">CAD 文档（用于解析图层）</param>
    /// <param name="width">箍筋外包宽（沿 +X）</param>
    /// <param name="height">箍筋外包高（沿 +Y）</param>
    /// <param name="options">绘制选项</param>
    public static List<Entity> Add(
        CadDocument doc,
        double width,
        double height,
        RebarDetailOptions? options = null
    ) => RebarDetail.Add(doc, BuildVertices(width, height), options);

    /// <summary>
    /// 计算矩形箍筋的完整顶点序列（1:1 局部坐标，含弯钩段）。
    /// 供 <see cref="Rebar"/> 模型复用；<see cref="Add"/> 内部也走此入口。
    /// </summary>
    /// <param name="width">箍筋外包宽（沿 +X）</param>
    /// <param name="height">箍筋外包高（沿 +Y）</param>
    public static List<RebarVertex> BuildVertices(double width, double height)
    {
        double hook = 60;
        var w = RebarDetail.FormatLength(width);
        var h = RebarDetail.FormatLength(height);

        return
        [
            RebarDetail.V(0, 0, w),
            RebarDetail.V(width, 0, h),
            RebarDetail.V(width, height, w),
            RebarDetail.V(0, height, h),
            RebarDetail.V(0, hook),
            RebarDetail.V(-hook, hook),
        ];
    }
}