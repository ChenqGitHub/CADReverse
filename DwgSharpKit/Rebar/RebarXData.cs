using System;
using System.Collections.Generic;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.Tables;
using ACadSharp.XData;

namespace DwgSharpKit.Rebar;

/// <summary>
/// 钢筋简单属性的 XData（扩展数据）读写助手。
/// 把 编号/等级/直径/根数/真正长度/替代长度 写进钢筋多段线的扩展数据（注册应用 GCS_REBAR）。
/// 图面上不可见，但属性随 DWG 存档——数量表统计、后续二次开发识别钢筋时可直接从实体读取，无需再解析。
/// </summary>
public static class RebarXData
{
    /// <summary>注册应用名（AppId），也是 XData 分组名</summary>
    public const string AppName = "GCS_REBAR";

    /// <summary>记录顺序：编号、等级、直径、根数、真正长度、替代长度</summary>
    private const int FieldCount = 6;

    /// <summary>
    /// 确保 AppId 已注册到文档（幂等）。写入 XData 前必须调用。
    /// </summary>
    public static void EnsureAppId(CadDocument doc)
    {
        if (!doc.AppIds.Contains(AppName))
        {
            doc.AppIds.Add(new AppId(AppName));
        }
    }

    /// <summary>
    /// 把钢筋简单属性写入实体的 XData（通常写在其多段线上）。
    /// </summary>
    /// <param name="doc">CAD 文档（用于注册 AppId）</param>
    /// <param name="entity">目标实体（钢筋多段线 LwPolyline）</param>
    /// <param name="rebar">钢筋模型</param>
    public static void Write(CadDocument doc, Entity entity, Rebar rebar)
    {
        EnsureAppId(doc);
        var appId = doc.AppIds[AppName];

        var xdata = new ExtendedData(
            [
                ExtendedDataRecord.Create(GroupCodeValueType.ExtendedDataString, rebar.Number),
                ExtendedDataRecord.Create(GroupCodeValueType.ExtendedDataString, rebar.Grade),
                ExtendedDataRecord.Create(GroupCodeValueType.ExtendedDataDouble, rebar.Diameter),
                ExtendedDataRecord.Create(GroupCodeValueType.ExtendedDataInt16, (short)rebar.Count),
                ExtendedDataRecord.Create(GroupCodeValueType.ExtendedDataDouble, rebar.Length),
                ExtendedDataRecord.Create(
                    GroupCodeValueType.ExtendedDataString,
                    rebar.SubstituteLength ?? string.Empty
                ),
            ]
        );
        xdata.AddControlStrings();
        entity.ExtendedData.Add(appId, xdata);
    }

    /// <summary>
    /// 从实体 XData 读回钢筋简单属性；实体没有该应用的数据时返回 null。
    /// 返回的 Rebar 只有属性（编号/等级/直径/根数/长度），<see cref="Rebar.Vertices"/> 需调用方另行设置。
    /// </summary>
    public static Rebar? Read(Entity entity)
    {
        if (!entity.ExtendedData.TryGet(AppName, out var xdata))
        {
            return null;
        }

        var rebar = new Rebar();
        int i = 0;
        foreach (var record in xdata.Records)
        {
            if (record is ExtendedDataControlString)
            {
                continue; // 跳过 { } 控制串
            }

            switch (i)
            {
                case 0: rebar.Number = (string)record.RawValue; break;
                case 1: rebar.Grade = (string)record.RawValue; break;
                case 2: rebar.Diameter = (double)record.RawValue; break;
                case 3: rebar.Count = (short)record.RawValue; break;
                case 4: rebar.TrueLength = (double)record.RawValue; break;
                case 5:
                    var s = (string)record.RawValue;
                    rebar.SubstituteLength = string.IsNullOrEmpty(s) ? null : s;
                    break;
            }

            if (++i >= FieldCount)
            {
                break;
            }
        }

        return rebar;
    }
}
