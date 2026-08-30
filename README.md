# CADReverse

梁图出图逆向工程工作流：用代码复刻现有 ZWCAD 图纸，为后续参数化出图打基础。分两部分：

| 部分 | 作用 |
| --- | --- |
| `Cadpython/` | Python（pyzwcad）读取 ZWCAD 图元，输出可粘贴的 C# 绘图代码。 |
| `CadCli/` | 控制台应用：运行生成/手写的 C# 代码并写出 DWG。 |

绘图类库 **[DwgSharpKit](https://github.com/ChenqGitHub/DwgSharpKit)** 已独立成仓（NuGet 包 `DwgSharpKit`），本仓库的 `CadCli` 通过包引用使用它。

## DwgSharpKit（独立仓库）

类库源码与发布流程见 [github.com/ChenqGitHub/DwgSharpKit](https://github.com/ChenqGitHub/DwgSharpKit)。

- `Standards/`：标准图层（B-01~B-09）/文字样式/标注样式，单一数据源。
- `Drawing/`：`CadDraw` 绘图原语 + `CadDocumentExtensions`（`doc.Layer(CadLayers.B01)` 等）。
- `Blocks/`：HRB400 钢筋块、钢结构断面、标题栏、剖面、引线标注。
- `Rebar/`：钢筋大样模型（对称/非对称/箍筋顶点生成、逐段单行文本标注、整根引线标注、属性 XData）。
- `Infrastructure/`：`CadInitializer` 一键初始化标准。

引用图层/样式一律用常量：`doc.Layer(CadLayers.B03)`、`doc.TextStyle(CadTextStyles.JstiSimsun)`、`doc.DimStyle(CadDimStyles.FangSong1_50)`，避免散落字符串。

## 本地打包与引用

- 在 DwgSharpKit 仓库执行 `dotnet pack -c Release -o nupkgs`，把产物拷到本仓库 `nupkgs/`（已含 `acadsharp.3.7.1.nupkg` 供离线还原）。
- `CadCli/NuGet.config` 已指向本地源 `../nupkgs`；正式发布到 nuget.org 后可切换官方源。

## Cadpython

在 ZWCAD 里选择图元（或 `--all` 全图），打印生成代码：

- 已知标准层 → `doc.Layer(CadLayers.Bxx)`；
- 未知层 → 回退 `doc.Layers["..."]`；
- 已支持：Line/Arc/Circle/RotatedDimension/2Line及3Point角度标注/LwPolyline(带bulge)/Text；
- 其余实体（BlockReference、MText 等）计数跳过，待补。

## CadCli（应用）

- `Program.cs`：入口——初始化标准 → 逐张调用各绘图方法 → 每张图各写一个独立 DWG（`bearing-reinforcement.dwg` / `frame-bridge-reinforcement.dwg` / `frame-body-rebar-quantity.dwg`）。
- `Generated/`
  - `BearingReinforcementDrawing.cs`：支座加强钢筋图（Cadpython 生成代码的落点，可手动编辑）。
  - `FrameBridgeReinforcementDrawing.cs`：框架桥钢筋图（一张 DWG，含两部分：① 参数化大样 N0~N17，每根钢筋用 `Rebar` + `PlaceDetail` 落地：形状+每段标注+引线标注+XData，H1/H2 双参数族；② 框架身钢筋数量表（每延米）：外包矩形 + 顶/底板 + 侧墙、板内纵筋密排、39.9° 斜筋锯齿、中心线与尺寸/编号标注，放在大样下方）。

## Current Workflow

1. 在 ZWCAD 中通过 Cadpython 选择图元，复制打印出的 C# 代码。
2. 粘贴进 `CadCli/Generated/BearingReinforcementDrawing.cs`（支座加强）或 `FrameBridgeReinforcementDrawing.cs`（框架桥）的方法体。
3. `dotnet run --project CadCli`。
4. 检查 `CadCli/bin/Debug/net10.0/` 下的 `bearing-reinforcement.dwg` 与 `frame-bridge-reinforcement.dwg`。

## 约定

- `CadCli` 只放应用逻辑与生成代码；库代码一律进独立的 [DwgSharpKit](https://github.com/ChenqGitHub/DwgSharpKit) 仓库。
- `Cadpython` 生成的代码要求：新标准层先加进 `CadLayers` 并同步 `main.py` 的 `STANDARD_LAYERS` 映射。
