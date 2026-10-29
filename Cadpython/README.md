# Cadpython

ZWCAD 侧的两段式处理：

1. **导出**（`region_export.py`）：把选中区域或整张图导出成 AI 友好的 JSON；
2. **分区**（`classify_regions.py`）：按图面空白切出视图块，再归为「钢筋大样区 / 综合图区 / 数量表区」三类区域。

分区结果交给 `skills/cad-rebar-skill` 的三区重建流程（大样 → 组合图 → 数量表，按钢筋编号对账）。

## 导出

整张图一次导出（推荐，供自动分区使用）：

```powershell
uv run python region_export.py --selection all --output .\artifacts\sheet.json
```

手动选原点 + 框选/点选单个区域（沿用原有行为）：

```powershell
uv run python region_export.py --output .\artifacts\cad-region.json
uv run python region_export.py --selection rectangle --mode crossing --output .\artifacts\cad-region.json
```

- `--selection all` 遍历当前布局全部实体，默认 `--origin zero`，保留绝对坐标；
- `--selection screen`（默认）用 `pyautocad` 屏幕选择，先点局部坐标原点，导出坐标相对该原点；
- `--selection rectangle` 用矩形窗口；`--mode window` 只选完全包含的实体，`crossing` 还包含相交实体；
- `--origin auto|pick|zero` 控制原点：`auto` 为整图取绝对坐标、框选取交互点，`pick` 强制交互，`zero` 用 `(0,0,0)`。

`source.selection_origin` 始终记录原始原点坐标，便于回查，不要二次平移。

## 分区

```powershell
uv run python classify_regions.py .\artifacts\sheet.json --report -o .\artifacts\regions.json
```

- 输入可以是 region 导出（`entities`），也可以是历史 compact 导出（`objects`）；多个文件默认合并处理；
- 输出包含两级：`regions`（区域，同类型相邻视图合并）与 `views`（视图块，重建逻辑的实际落点）；
- `--no-merge` 关闭第二级合并，每个视图块单独成区域；
- `--padding` / `--merge-padding` 覆盖自动阈值（默认按中位字高 4 倍 / 12 倍）；
- `--write-split DIR` 把每个区域的原始实体写成独立 JSON，直接作为三区重建的输入；
- `--per-file` 每个文件单独分类（批量回归时用）。

用仓库现有的三个手工切分文件做回归（应得到 综合图区 / 钢筋大样区 / 数量表区）：

```powershell
cd Cadpython
.\.venv\Scripts\python.exe -X utf8 .\classify_regions.py `
  ..\pier_shaft_general_compact.json `
  ..\pier_shaft_rebar_detail_compact.json `
  ..\pier_shaft_rebar_table_compact.json --report
```

> 坐标单位与导出文件一致，分区只依赖相对间距，不依赖 compact（绝对坐标）与 region（相对原点）的差异。

## 判据与输出约定

规则、阈值、输出 schema 与已知限制见 `skills/cad-rebar-skill/references/region-classification.md`。

## 三区联动校验

分区之后、生成 C# 之前，先校验三个区域是否真的在讲同一批钢筋：

```powershell
uv run python linkage_report.py `
  ..\pier_shaft_general_compact.json `
  ..\pier_shaft_rebar_detail_compact.json `
  ..\pier_shaft_rebar_table_compact.json --report --strict -o ..\artifacts\linkage.json
```

输出按钢筋编号串联的三区视图：大样规格与 `L=H+δ` 公式、公式计算值、数量表单根长、根数、总长、综合图放置数，以及编号集合差异和校验结论。

检查项：

- 每个编号在三个区域都存在（区间编号如 `N2-1～2` 会展开）；
- 大样直径 = 数量表直径；
- `L=H+δ` 公式用数量表表头的墩高 `H` 计算后等于单根长；
- 根数 × 单根长 = 总长，总长 × 单位重 = 总重；
- `平均`/`（均）` 长度记为提示，不算错误。

这份输出是 JSON → C# 的取数表，规则见 [json-to-csharp.md](../skills/cad-rebar-skill/references/json-to-csharp.md)。

## 语义提取与 C# 生成（原型）

联动校验通过后，把三个区域抽成语义模型，再生成手写风格的 C#：

```powershell
uv run python extract_pier_model.py `
  ..\pier_shaft_general_compact.json `
  ..\pier_shaft_rebar_detail_compact.json `
  ..\pier_shaft_rebar_table_compact.json `
  -o ..\pier_body_model.json --report

uv run python emit_pier_csharp.py ..\pier_body_model.json `
  -o ..\CadCli\Generated\Pier\PierBodyReinforcementDrawingSemantic.cs
```

`extract_pier_model.py` 输出（schema 0.1）：

- `parameters.table`：表框原点/宽高、列中心、行距、首行 Y、表头与汇总文字；
- `detail_templates`：编号、直径、`L=H+δ` 公式、TrueLength、局部顶点/bulge、关联置信度；
- `placements`：编号 → 综合图放置（位置/旋转/比例/所在视图）；
- `components`：参数化构件（圆角矩形 + 上下对称凹槽：宽/高/圆角/槽宽/原点/拟合误差），源多段线用 `component` 索引回指；
- `composite_labels/sections/texts/geometry`：综合图的编号块、断面符号、文字与几何；`composite_excluded_noise` 记录被排除的孤立噪声实体数；
- `schedule_rows`：数量表 14 行；`warnings`：关联距离、缺模板、修订差异等提示。

`emit_pier_csharp.py` 生成三个区域方法（`DrawCompositeRegion` / `DrawDetailRegion` / `DrawScheduleRegion`）：大样用 `Rebar` + `CreatePlacedDetail`，数量表用结构化行 + 网格参数，综合图的 3 个截面构件由 `ComponentOutlines` 参数 + `DrawChannelSection` 重建（不再逐顶点照抄）。其余综合图钢筋曲线仍按源几何记录，因为 compact 导出的尺寸标注 `text=None, measurement=0`，参数无法反推。

生成 DWG 后回读统计，并跑可复现校验：

```powershell
dotnet run --project ..\CadCli -- --stats ..\CadCli\bin\Debug\net10.0\pier-body-reinforcement-semantic.dwg --json
uv run python validate_reconstruction.py `
  --model ..\pier_body_model.json `
  --dwg ..\CadCli\bin\Debug\net10.0\pier-body-reinforcement-semantic.dwg `
  --json-out ..\artifacts\semantic-validation.json
```

`validate_reconstruction.py` 做三层检查（数据 14 行数量表 ↔ 13 模板、构件顶点回验、DWG 实体计数与几何包围盒），输出 `artifacts/semantic-validation.json`，全部通过时退出码为 0。

## 测试

标准库 `unittest`，无第三方依赖：

```powershell
cd Cadpython
.\.venv\Scripts\python.exe -m unittest discover -s tests -v
```

测试覆盖：圆弧/多段线 bulge 包围盒、表框识别、关键字优先级、空白聚类、同类型合并、三区回归（实体数、编号、不重复归类、实体守恒）、联动校验（编号集合、H 公式、数量与重量一致性），以及语义提取/生成/校验（模板 13 个、放置 83 处、3 个参数化构件、表结构、生成文件与模型同步、校验器的几何辅助函数与实体计数）。
