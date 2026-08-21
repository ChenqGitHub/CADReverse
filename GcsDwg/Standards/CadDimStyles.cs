namespace GcsDwg.Standards;

/// <summary>
/// 标准标注样式定义（依据制图标准化 2.6 标注）。
/// 仿宋字高 2.5，非仿宋字高 3.0；1:1 与 1:50 两种全局比例。
/// 箭头统一采用倾斜箭头（TickSize>0），尺寸界线原点偏移 1.5、超出尺寸线 1。
/// 文字与尺寸线间距 0.5（与 JSTI_QL 模板一致）。
/// </summary>
public sealed record CadDimStyleDef(
    string Name,
    double ScaleFactor,
    double TextHeight,
    double ArrowSize,
    string TextStyleName,
    double TickSize,
    double ExtensionLineOffset,
    double ExtensionLineExtension,
    double DimensionLineGap = 0.5
);

/// <summary>
/// 标准标注样式目录：单一数据源。
/// </summary>
public static class CadDimStyles
{
    public static readonly CadDimStyleDef FangSong1_1 = new(
        "仿宋 1：1",
        1,
        2.5,
        1,
        CadTextStyles.JstiSimsun.Name,
        TickSize: 1,
        ExtensionLineOffset: 1.5,
        ExtensionLineExtension: 1
    );
    public static readonly CadDimStyleDef FangSong1_50 = new(
        "仿宋 1：50",
        50,
        2.5,
        1,
        CadTextStyles.JstiSimsun.Name,
        TickSize: 1,
        ExtensionLineOffset: 1.5,
        ExtensionLineExtension: 1
    );
    public static readonly CadDimStyleDef NonFangSong1_1 = new(
        "非仿宋 1：1",
        1,
        3.0,
        1,
        CadTextStyles.JstiNonSimsun.Name,
        TickSize: 1,
        ExtensionLineOffset: 1.5,
        ExtensionLineExtension: 1
    );
    public static readonly CadDimStyleDef NonFangSong1_50 = new(
        "非仿宋 1：50",
        50,
        3.0,
        1,
        CadTextStyles.JstiNonSimsun.Name,
        TickSize: 1,
        ExtensionLineOffset: 1.5,
        ExtensionLineExtension: 1
    );

    public static readonly IReadOnlyList<CadDimStyleDef> All = [FangSong1_1, FangSong1_50, NonFangSong1_1, NonFangSong1_50];
}
