namespace GcsDwg.Standards;

/// <summary>
/// 桥梁专业标准图层定义（依据制图标准化 表2-2）。
/// B 为桥梁专业代号。
/// </summary>
public sealed record CadLayerDef(
    string Name,
    short Color,
    string LineType,
    string Usage,
    bool NoPlot = false
);

/// <summary>
/// 标准图层目录：单一数据源。
/// 初始化时由 <see cref="GcsDwg.Infrastructure.CadInitializer"/> 遍历 <see cref="All"/> 建图层；
/// 绘图代码通过 <c>doc.Layer(CadLayers.B01)</c> 引用，避免散落字符串。
/// </summary>
public static class CadLayers
{
    public static readonly CadLayerDef B01 = new(
        "B-01 粗实线",
        1,
        "CONTINUOUS",
        "结构主要线、钢筋线、表格外框"
    );
    public static readonly CadLayerDef B02 = new("B-02 粗虚线", 2, "DASHED", "不可视结构线");
    public static readonly CadLayerDef B03 = new(
        "B-03 标注",
        3,
        "CONTINUOUS",
        "标注、指示、截断符号、高程符号"
    );
    public static readonly CadLayerDef B04 = new(
        "B-04 细实线",
        4,
        "CONTINUOUS",
        "结构次要线、钢筋图中的结构轮廓线"
    );
    public static readonly CadLayerDef B05 = new("B-05 细虚线", 2, "DASHED", "不可视次要线");
    public static readonly CadLayerDef B06 = new("B-06 中心线", 6, "CENTER", "结构中心线、对称线");
    public static readonly CadLayerDef B07 = new("B-07 文字", 7, "CONTINUOUS", "图中文字");
    public static readonly CadLayerDef B08 = new(
        "B-08 地层地质",
        8,
        "CONTINUOUS",
        "地质、地层、既有构筑物等"
    );
    public static readonly CadLayerDef B09 = new(
        "B-09 不打印",
        5,
        "CONTINUOUS",
        "不打印，视口参考线",
        NoPlot: true
    );

    public static readonly IReadOnlyList<CadLayerDef> All =
    [
        B01,
        B02,
        B03,
        B04,
        B05,
        B06,
        B07,
        B08,
        B09,
    ];
}
