using System;
using System.Collections.Generic;
using ACadSharp;
using ACadSharp.Entities;
using CSMath;

namespace GcsDwg.Rebar;

/// <summary>
/// 非对称线筋：各段可直接用节点模型给定，返回 1:1 本地图元（不自动入文档），
/// 供使用者用 <see cref="GcsDwg.CadDocumentExtensions.AddTranslated"/> 复用放置。
/// </summary>
public static class AsymmetricRebar
{
    /// <summary>
    /// 生成非对称线筋图元（1:1 本地坐标，未入文档）。
    /// </summary>
    /// <param name="vertices">顶点序列；Text 挂在各段起点上</param>
    public static List<Entity> Add(
        CadDocument doc,
        IReadOnlyList<RebarVertex> vertices,
        RebarDetailOptions? options = null
    ) => RebarDetail.Add(doc, vertices, options);
}