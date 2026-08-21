namespace GcsDwg.Standards;

/// <summary>
/// 标准文字样式定义（依据制图标准化 2.4 字体）。
/// 仿宋用 TTF 字体；非仿宋用 SHX 大字体组合。
/// </summary>
public sealed record CadTextStyleDef(
    string Name,
    string Filename,
    string BigFontFilename,
    double Width = 0.7
);

/// <summary>
/// 标准文字样式目录：单一数据源。
/// </summary>
public static class CadTextStyles
{
    public static readonly CadTextStyleDef JstiSimsun = new("JSTI_仿宋", "仿宋_GB2312.ttf", "");
    public static readonly CadTextStyleDef JstiNonSimsun = new(
        "JSTI_非仿宋",
        "tessdeng.shx",
        "hztxt.shx"
    );
    public static readonly CadTextStyleDef JstiNonSimsunDim = new(
        "JSTI_非仿宋_标注",
        "tessdeng.shx",
        "hztxt.shx",
        Width: 0.6
    );

    public static readonly IReadOnlyList<CadTextStyleDef> All =
    [
        JstiSimsun,
        JstiNonSimsun,
        JstiNonSimsunDim,
    ];
}
