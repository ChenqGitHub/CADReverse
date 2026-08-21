using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Entities;
using CSMath;

namespace GcsDwg.Rebar;

/// <summary>
/// 箍筋：矩形轮廓。节点模型，返回 1:1 本地图元（不自动入文档），
/// 供使用者用 <see cref="GcsDwg.CadDocumentExtensions.AddTranslated"/> 复用放置。
/// </summary>
public static class Stirrup
{
    /// <summary>
    /// 生成矩形箍筋图元（1:1 本地坐标，未入文档）。
    /// </summary>
    /// <param name="width">箍筋外包宽（沿 +X）</param>
    /// <param name="height">箍筋外包高（沿 +Y）</param>
    public static List<Entity> Add(
        CadDocument doc,
        double width,
        double height,
        RebarDetailOptions? options = null
    )
    {
        double hook = 60;
        var w = RebarDetail.FormatLength(width);
        var h = RebarDetail.FormatLength(height);

        var verts = new List<RebarVertex>
        {
            new(new XY(0, 0), 0, w),
            new(new XY(width, 0), 0, h),
            new(new XY(width, height), 0, w),
            new(new XY(0, height), 0, h),
            new(new XY(0, hook)),
            new(new XY(-hook, hook)),
        };

        return RebarDetail.Add(doc, verts, options);
    }
}