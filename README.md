# CADReverse

梁图出图逆向工程工作流：用代码复刻现有 ZWCAD 图纸，为后续参数化出图打基础。分三部分：

| 部分 | 作用 |
| --- | --- |
| `GcsDwg/` | ACadSharp 封装类库：CAD 标准（图层/文字样式/标注样式）+ 绘图原语 + 常用块。 |
| `Cadpython/` | Python（pyzwcad）读取 ZWCAD 图元，输出可粘贴的 C# 绘图代码。 |
| `CadCli/` | 控制台应用：运行生成/手写的 C# 代码并写出 DWG。 |

## GcsDwg（类库）

- `Standards/`
  - `CadLayers.cs`：标准图层目录（B-01~B-09，依据制图标准化 表2-2）。单一数据源，初始化与绘图都从这里取。
  - `CadTextStyles.cs`：标准文字样式（仿宋 TTF / 非仿宋 SHX）。
  - `CadDimStyles.cs`：标准标注样式（仿宋/非仿宋 × 1:1/1:50）。
- `Infrastructure/`
  - `CadInitializer.cs`：遍历标准目录建线型/图层/文字样式/标注样式，注册常用块。
- `Drawing/`
  - `CadDraw.*.cs`：绘图原语（Line/Arc/Circle/Polyline/Text/Dimension），实体统一挂到 `doc.Entities`。
  - `CadDocumentExtensions.cs`：`doc.Layer(CadLayers.B01)` 等常规扩展。
- `Models/`
  - `CadPolyline.cs`、`CadText.cs`：绘图参数 record。
- `Blocks/`
  - `HRB400Block.cs`、`SteelSection.cs`：常用钢筋块（含属性）。

引用图层/样式一律用常量：`doc.Layer(CadLayers.B03)`、`doc.TextStyle(CadTextStyles.JstiSimsun)`、`doc.DimStyle(CadDimStyles.FangSong1_50)`，避免散落字符串。

## Cadpython

在 ZWCAD 里选择图元（或 `--all` 全图），打印生成代码：

- 已知标准层 → `doc.Layer(CadLayers.Bxx)`；
- 未知层 → 回退 `doc.Layers["..."]`；
- 已支持：Line/Arc/Circle/RotatedDimension/2Line及3Point角度标注/LwPolyline(带bulge)/Text；
- 其余实体（BlockReference、MText 等）计数跳过，待补。

## CadCli（应用）

- `Program.cs`：入口——初始化标准 → 调用 `GeneratedDraw.DrawFromPythonGeneratedCode` → 写出 `reverse-output.dwg`。
- `Generated/`
  - `DrawFromPythonGeneratedCode.cs`：Cadpython 生成代码的落点（可手动编辑）。

## Current Workflow

1. 在 ZWCAD 中通过 Cadpython 选择图元，复制打印出的 C# 代码。
2. 粘贴进 `CadCli/Generated/DrawFromPythonGeneratedCode.cs` 的方法体。
3. `dotnet run --project CadCli`。
4. 检查 `CadCli/bin/Debug/net10.0/reverse-output.dwg`。

## 约定

- `GcsDwg` 是类库，所有标准/原语先加在这里；`CadCli` 只放应用逻辑与生成代码。
- `Cadpython` 生成的代码要求：新标准层先加进 `CadLayers` 并同步 `main.py` 的 `STANDARD_LAYERS` 映射。
