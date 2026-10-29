# 区域 JSON → DwgSharpKit C#

转换分两个阶段，本文档描述**第二阶段（优化）**的规则；第一阶段用 `--strict` 逐实体 1:1 输出，
只做图层映射、块替身，以及标注分列表/按 rotation 分组的结构整理；尺寸链合并留到第二阶段。

三组优化：**合并 → 块替身 → 阵列**，外加一个结构级的**表格化**。每一步都有"看起来对、其实错"的写法，
下面把它们写成硬规则。

## 输入约定：一张图纸一个类

生成器只认**目录**，把目录里所有区域 JSON 生成到**一个类**里：

| 文件名 | 解析为 |
|---|---|
| `shot-02-region-03.json` | 截图 2、区域 3 |
| `region-03.json` | 截图 1、区域 3（旧格式，仍兼容） |
| `index.json` 等 | 跳过 |

- 排序按 `(截图号, 区域号)`，所以方法在类里的顺序与红框编号一致；
- 方法名：**单截图** → `DrawRegionMM`；**多截图**（目录里出现 ≥2 个截图号）→ `DrawShotNNRegionMM`，
  否则不同截图的"区域 1"会撞名；
- `--append` 是"并进已有类"：同名方法**原地替换**、新方法插到类尾，类名不在文件里才新建一个类。
  所以第二张截图的正确操作是"再抓一张 → 再导出 → 重跑同一条带 `--append` 的命令"。

## 0. 图层映射

源图图层名 → 项目标准图集（`CadLayers`）：

| 源图层 | 目标 |
|---|---|
| 粗实线 | `B01` |
| 细实线 / 轮廓线 / `0` | `B04` |
| 标注 / B-03 标注 / 尺寸线 | `B03` |
| 中心线 | `B06` |
| 文本 | `B07` |
| Defpoints | 丢弃（标注定位点） |

每个方法开头按需声明：`var b03 = doc.Layer(CadLayers.B03);`，只声明该区域真正用到的图层。
本项目已确认：所有原生标注使用 `b03`，所有单行/多行文字使用 `b07`；尺寸文字由原生标注维护。

## 1. 合并：节点图，不是数组相邻

规则：**同图层**内，端点距离 ≤ 0.5 mm 即视为相连；Line / LwPolyline / Arc 统一成带 bulge 的顶点链，串成一条多段线；回到起点则闭合。

### 1.0 共线并段（在走链之前）

源图的尺寸界线常被拆成多段、彼此还重叠：一条长竖线 + 每道尺寸线处一段 50 mm 的出头线，
在同一 x 上写 6 条 `CadDraw.Line`，图上是同一块墨迹。走链救不了它——出头线把节点度数变成 3，
按"度数 ≠ 2 就断"的规则，长线反而被打断。

所以先按 `(图层, 轴向, 固定坐标, 线宽, 标高)` 分组做区间并：

- 只处理无 bulge 的两点直线；固定坐标两位小数聚类，输出仍用原坐标；
- 区间相接或重叠（间隙 ≤ 0.5 mm）取并集，被包含的段直接吸收；
- 并完再走链，顶点更少，也更容易落进阵列。

实测 04 顶帽图：`x=-62161.487` 上 6 段并为 1 条 `(-21628.554 → -19528.554)`；
全图 1147 条 `Line` 并掉 139 条，语句再降 72。代价是线段内部的断点会消失，
验收口径要从"源顶点在 DWG 顶点集合里"改成"源几何顶点落在 DWG 某条线段上"（距离 ≤ 0.05 mm）。

实现要点：

1. 把每条线段规范成 `(起点, 终点, bulge)`；Arc 先转 bulge，LwPolyline 按顶点拆分。
2. 端点按 1.0 mm 网格 + 0.5 mm 容差聚成节点，建"节点 → 线段"的邻接表。
3. 从**度数 ≠ 2 的节点**（端点、岔口）出发沿邻接走链，走到另一个非 2 度节点或回到起点为止；剩下纯环单独走一圈。
4. 走链时方向很关键——见下面的坑 2。

**坑 1：用圆心判断圆弧归属，会丢掉大半径弧。**
R = 25250 mm 的弧，圆心可能在图形外几十米处。按"圆心是否在窗口内"过滤，这些弧会整条消失。
正确做法：用**几何包围盒**（弧/圆 = 圆心 ± R）与区域框求交，判交才算命中。

**坑 2：走链时起止节点取错，会凭空造出弦。**
正向延伸要从"进入点的另一端"开始，反向延伸要交换端点，并在反向时把 bulge 取负。
写错会得到 `A→B→A` 的回折尖刺（真实案例：一对相距 5033 mm 的点被连成一条假弦），
同时产生大量重复顶点（案例里 96 个）。

**坑 3：bulge 精度不足。**
`bulge = tan(sweep/4)`；R = 25250 的弧用 3 位小数，矢高误差约 1 mm。**保留 6 位**。

**坑 4：闭合环忘了闭合。**
首尾节点相同（距离 ≤ 容差）时必须置 `closed: true`，否则接口处会有一条可见缺口。

## 2. 映射：块与文字

```csharp
// HRB400：帮助器不接图层参数（插入后落在 0 层），必须事后归还源图图层
HRB400Block.Insert(doc, CadDraw.P(x, y), "N3-1", "14", 5, 0),
...
// 同一区域里这些块可能分属多个图层：按图层连续发射，再用 Skip/Take 精确归还
var hrb400 = entities.OfType<Insert>().Where(x => x.Block?.Name == HRB400Block.Name).ToList();
foreach (var insert in hrb400.Take(18)) { insert.Layer = b04; }
foreach (var insert in hrb400.Skip(18).Take(14)) { insert.Layer = b07; }
```

只有一种图层时退化成 `foreach (var insert in hrb400)`，不必写 `Take`。
**不要**一律归到 `B-03 标注`：那是桥墩案例的巧合，别的视图里同一批 HRB400 可能一半在轮廓线、一半在文本层，
一刀切会静默改图层。发射顺序（也就是 `Take/Skip` 的依据）来自 JSON 顺序，转换器负责把同图层的块排在一起。

```csharp
// 钢筋断面：显式建 Insert 才能指定图层；缩放按断面半径换算
new Insert(SteelSection.EnsureDefined(doc))
{
    InsertPoint = CadDraw.P(x, y),
    XScale = 68.0, YScale = 68.0, ZScale = 68.0,   // = x_scale * 17/3.75
    Rotation = 0,
    Layer = b04,
},
```

- `钢筋轮廓（双筋）` 一个块里放两个断面符号：第二个沿**旋转后的 x 方向**偏移 `37.512825587706175 * scale`
  （这个常数是块内两圆心的实际间距，不要用块包围盒推算）。
- `_Oblique` 是"斜线块"：块内几何是 `(-0.5,-0.5)→(0.5,0.5)`，按 `x_scale/y_scale` 与旋转展开成一条普通 Line，
  不要当成 Insert 插入（否则要维护一个只有一条线的块定义）。

```csharp
// 文字
CadDraw.Text("N1-1", CadDraw.P(x, y), 250, b07),            // 第五个参数是字高
CadDraw.Text("A", CadDraw.P(x, y), 250, b07, rotation),     // 第六个是旋转(弧度)
CadDraw.MText("28 L=H+1414", CadDraw.P(x, y), 125, b07),
```

```csharp
// 标注：按 JSON 传三点、源角度及文字属性；sourceDimStyle 须属于目标 doc
CadDraw.RotatedDimension(
    CadDraw.P(x1, y1), CadDraw.P(x2, y2), CadDraw.P(dx, dy),
    rotation: sourceRotation,
    style: sourceDimStyle,
    layer: b03,
    text: sourceText,
    textMiddlePoint: CadDraw.P(tx, ty),
    isTextUserDefinedLocation: sourceTextIsUserPositioned,
    dimensionLineExtension: sourceExtension
),
```

**文字与标注用项目 helper 生成原生实体。** 单行文字调用 `CadDraw.Text`，多行文字调用
`CadDraw.MText`；不要在生成类里自行 `new TextEntity` / `new MText` 并重写构造逻辑。
保留 JSON 中可用的旋转、对齐、宽度、样式等属性；若现有 helper 参数不够，扩展
`CadDraw` helper 的可选参数，再由生成代码传入源值。

源 JSON 中的 `Dimension` 必须继续生成原生 DIMENSION，调用项目已有的
`CadDraw.RotatedDimension` helper；不要把标注拆成尺寸线、界线和文字，也不要把这些
图形缓存误当成独立源对象重复绘制。按 JSON 保留第一/第二测量点、定义点和文字覆盖值；
若源数据含旋转角、文字中心或手动定位标志，使用支持这些参数的 helper 重载，并保留源值。
水平标注保持源尺寸线的 Y 坐标，竖直标注保持源尺寸线的 X 坐标；使用项目中已确认的
`RotatedDimension` 调用约定，不要因点的排列方向把标注线移到另一侧。

定义点缺失的标注跳过（渲染不出来），但要在汇报里计数。

### 2.1 标注处理：固定顺序

生成或追加替换方法后，对照 JSON 依次完成下面三步。`--strict` 基线只做前两步，
保持一条源标注对应一次 `RotatedDimension`；第三步属于优化。

**第一步：先把标注和其他图元分开添加到 doc。**

- 普通线、弧、圆、多段线、文字、块、表格等放入 `List<Entity> entities`。
- 原生标注放入 `List<Entity> dimensionEntities`，包括独立的 `RotatedDimension` 和后续尺寸链的展开项。
- 方法末尾先 `doc.AddEntities(entities)`，再 `doc.AddEntities(dimensionEntities)`；无标注区域只保留普通列表。
- 按实体类型划分，不能把 `b03` 上的所有对象都当成标注；单行/多行文字仍在 `entities`。

**第二步：在标注列表内按 rotation 分组。**

- 取 JSON 的源旋转角，角度单位以项目 API 为准；当前 API 为弧度。相同角度连续放置并写组注释。
- `0`、`Math.PI / 2`、`Math.PI` 是不同组，不能按“同为水平”把 `0` 和 `Math.PI` 混在一起。
- 仅允许处理读取产生的浮点噪声；不能粗略取整、按模 π 归一化，或修改实际输出角度来凑组。
- 这一阶段只整理顺序，保留每条标注的源句柄、第一/第二测量点、定义点、样式、文字和定位属性。

**第三步：同 rotation 组内，共享文字坐标的标注检查能否改为 ChainDimension。**

文字对齐只决定候选集合：

| 尺寸线方向 | `textMiddlePoint` 候选条件 |
|---|---|
| 水平（如 rotation = 0 或 π，各自组内） | Y 相同 |
| 竖直（如 rotation = π/2，各自组内） | X 相同 |
| 斜向 | 法向投影 `dot(textMiddlePoint, normal)` 相同，`normal = (-sin(rotation), cos(rotation))` |

不能仅凭 X 或 Y 相同就合并。候选还必须满足：

1. 同 rotation、同图层、相同或已确认等价的样式及外观属性；尺寸线的法向位置一致。
2. 按真实连接关系组成连续链，每条源标注对应一对相邻测量点；连接点的完整坐标一致。
   只容许源数据精度内的浮点噪声，不能套用普通线段的 0.5 mm 拼接容差，也不能修改源点。
   测量点可以不共线，尺寸方向由显式 rotation 决定。
3. 按实际 helper 展开后，每段第一/第二测量点、定义点、rotation、文字中心和尺寸线延长量
   都与源值一致。文字的切向位置必须符合 helper 的定位规则；只保留共同 X/Y 还不够。
4. 分支、断点和重合标注分开处理；不按文字坐标排序后强行串联，不反转测量点顺序来凑链。
   至少两段才改成尺寸链，其余保留 `RotatedDimension`。

先查看当前项目 `CadDraw.ChainDimension` 签名。`ChainDimension(points, offset, layer)`
会按每段测量向量推导角度和偏移，不能默认用它保存显式 rotation 与源文字位置。
当前项目支持下面的重载（目标项目缺少时保留独立标注，或在任务范围内扩展 helper）：

```csharp
ChainDimension(
    IEnumerable<XYZ> points,
    XYZ dimensionLinePoint,
    double rotation,
    DimensionStyle style,
    XYZ textLinePoint,
    Layer? layer = null,
    IReadOnlyList<double>? dimensionLineExtensions = null
)
```

该重载为相邻测量点各生成一个原生 `DimensionLinear`。尺寸线与文字的法向位置分别取
`dimensionLinePoint`、`textLinePoint`；每段定义点的切向位置取该段终点投影，文字中心的
切向位置取该段测量点的投影中点。`dimensionLineExtensions` 按段给出世界单位延长量，
数量须等于 `points.Count - 1`，省略时各段为 0。
它当前只支持空文字覆盖、无手动文字定位；源文字覆盖、手动位置或其他属性无法保持时，
继续使用 `RotatedDimension`，不能丢弃属性来套用尺寸链。

```csharp
// rotation: 0；源 DIMENSION C5DA6–C5DAA，五段标注保留为五个原生 DIMENSION。
..CadDraw.ChainDimension(
    [
        CadDraw.P(-91825.929, -17188.767),
        CadDraw.P(-91625.929, -17188.767),
        CadDraw.P(-91325.929, -17488.767),
        CadDraw.P(-90725.929, -17488.767),
        CadDraw.P(-90425.929, -17188.767),
        CadDraw.P(-90225.929, -17188.767),
    ],
    dimensionLinePoint: CadDraw.P(-91625.929, -16999.472),
    rotation: 0,
    style: sourceDimStyle,
    layer: b03,
    textLinePoint: CadDraw.P(-91725.929, -16911.972),
    dimensionLineExtensions: [50, 0, 0, 0, 50]
),
```

用 `..` 展开到 `dimensionEntities`，留在原 rotation 组内；注释列出源句柄。
尺寸链只减少调用语句，**不减少原生标注实体数**，不附带去重。记录合并前后标注数量、
尺寸链调用数/覆盖段数和未合并原因，并逐段对照 JSON。

已遇到的边界：C5CD4 与 C5CD5 的文字 Y 相同，但连接点 Y 相差 2.639 mm，应断开；
C5CE3 与 C5CE7 是重合标注，应各自保留，不能把重复对象当成连续两段。

### 2.2 图题：识别出整段，交给 TitleBlock.Add

图题 = 标题文字 + 上下两条平行横线 +（可选）比例文字。它不是一个块，而是几条线加几个字，
所以只能**按布局认**，认出来就整段替换：

```csharp
..TitleBlock.Add(doc, "正面钢筋布置图", "1:50", CadDraw.P(-58794.903, -18271.186), 50),
```

- 标题字高 H，`scale = H / 5`；插入点 = 下横线中点；
- 两条横线：长度差 ≤ 15%、间距 0.10H~0.50H、中心对齐 ≤ 0.5H；
- 比例文字在横线右端外侧，值原样传（没有就传 `""`，帮助器不画空 Text）；
- 标题文字里允许 `-`、罗马数字、汉字，但不能以 `\` 开头（那是尺寸/格式码）。

**它不是逐像素还原**：标准块横线长 = 5×字数 + 4 单位，手绘图常是"标题宽 + 小余量"，
所以横线会变长（04 图最多 +20%）。图题区在覆盖校验里单独归类。

**剖面符号别硬套**：`SectionBlock` 的模板是固定的 5/5/2 单位 + 文字 2.5 单位，
手绘的 L 形符号（两臂从角点出发、比例不同）对不上时保持原样——
换上去会得到一个"看起来对、比例全错"的符号，比不换更糟。

## 3. 阵列：同键 + 常量步长

数组展开写在集合表达式里，用展开运算符：

```csharp
List<Entity> entities =
[
    ..CadDraw.EntityArray(
        CadDraw.Line(CadDraw.P(-66391.1, -421.5), CadDraw.P(-64335.1, -421.5), b01),
        CadDraw.P(0, 100), 24
    ),
    CadDraw.Circle(CadDraw.P(...), 60, b04),
];
```

`CadDraw.EntityArray(Entity template, XYZ step, int count)` 内部就是 `Clone()` + 沿 `step` 平移 `i` 次，
所以模板必须是"平移可复制"的：几何完全同形、图层相同、块旋转/缩放相同。

"同 x 就竖向循环、同 y 就横向循环"已经包含在这套里——步长向量长什么样就怎么循环：

| 源对象的差别 | step | 方向 |
|---|---|---|
| 只有 y 变（x 相同） | `CadDraw.P(0, dy)` | 纵向 |
| 只有 x 变（y 相同） | `CadDraw.P(dx, 0)` | 横向 |
| x、y 一起变 | `CadDraw.P(dx, dy)` | 斜向 |

判定流程：

1. 给每个待发射实体算一个**键**：直线 `(layer, dx, dy)`；多段线 `(layer, closed, 顶点相对坐标+bulge)`；
   圆 `(layer, r)`；文字 `(layer, 内容, 字高, 旋转)`；断面 `(layer, scale, rotation)`。
   键必须**平移无关**（线用增量、多段线用相对首顶点的坐标），锚点另存。
2. 同键实体按**行**（同 y、按 x 排）、**列**（同 x、按 y 排）、**斜向**（按 x 排、允许 dy ≠ 0）各扫一遍常量步长区间。
3. 候选族按成员数从大到小贪心取，已入选的成员不再复用。
4. 成员数阈值：直线/圆 ≥ 4，文字/断面/多段线 ≥ 3。2 个成员写成阵列并不更短，别降门槛。

### 3.1 不等距的同线形：点列表循环

`EntityArray` 只认常量步长。同一条线在**不等距**的位置上重复（8 条尺寸界线、4 条尺寸线），
用点列表 + 固定增量，模板只写一次：

```csharp
..new[] { CadDraw.P(-61006.044, -19376.508), CadDraw.P(-60872.044, -19376.508) /* … */ }
    .Select(p => CadDraw.Line(p, CadDraw.P(p.X, p.Y + 262.5), b03)),
```

- 只对直线做；多段线/圆/文字没有单一可变坐标，收益小、可读性差；
- 方向规范化后再分族，正反两个画向能进同一族；
- 增量按三位小数分族、按原值输出（`round(dx,1)` 会让端点漂 0.02 mm）；
- 成员数 ≥ 3；与 `EntityArray` 互斥——先跑等距族，被吸收的下标不再参与；
- 用 `.Select` 而不是 `foreach`：后者要把列表拆成"声明 + Add"，方法体结构全变。

实测 04 顶帽图 32 组、268 条线，语句 1091 → 855，DWG 实体数与逐点覆盖校验不变。

**坑 5：只按 (x, y) 排序扫一遍会漏掉网格。**
一个 8 行 × 9 列的断面网格，按 (x,y) 排序后步长在 `(165,0)` 和 `(-1485,100)` 之间跳变，常量步长不成立，整个网格退化成 72 条字面量。
必须分方向扫。

**坑 6：不该阵列的别硬凑。**
- `HRB400` 的属性（编号/直径）逐块不同，阵列会把所有编号变成同一个 → 显式排除；
- `钢筋轮廓（双筋）` 一次产生两个实体，锚点逻辑不同 → 排除。

排除方式是在"键"里带上足以区分它们的字段，或直接把最小成员数设为不可能达到的值，
让它们自然落回字面量发射。

**坑 7：写完要自检。** 用 `scripts/scan_unlooped.py <Drawing.cs>` 扫一遍：它跳过 `EntityArray`，
把剩下的字面量按同一套键重新分族，报出每个漏网族（方法、图层、方向、步长、成员数、行号）——
**必须是 0**。实测一份 8 分区、2542 实体的图：843 条字面量语句、0 个漏网族；
同一判据在 `--strict` 基线里能报出 58 个族，正好说明"循环是做在优化阶段"的。

## 3.5 去重：同层同几何只画一份

源图里同一根线被画两遍是常态（`CadDraw.Line(...)` 连着两行一模一样，句柄不同、坐标差 1e-7 mm）。
两轮去重，缺一不可：

这里仅处理普通图元；原生 `Dimension` 从两轮通用去重中排除，保留源标注时重合对象也各自保留。
若用户明确要求标注去重，单独比较源属性、记录被删句柄和数量，不与尺寸链合并混做。

**① 实体级**（合并之前做，否则重复线进合并器会造出退化闭环）：

```python
DEDUP_DROP = {"handle", "owner_handle", "extended_data", "bounds", "display_type", "vertex_id"}
# 键 = 类型 + 映射后图层 + 几何(round 3) + 外观属性
```

- 图层用**映射后**的 `Bxx`：源图「标注」与「B-03 标注」映射后同为 `b03`，
  同一根线画在这两个层上，输出就是两句完全相同的 `CadDraw.Line(..., b03)`；
- 直线的 `start/end` 先按坐标排序再进键：同一根线正着画、反着画几何完全重合，
  不排序就永远判不成重复（实测在 04 顶帽图里漏了 2 根）；
- `Polyline` 的 `properties.is_closed == true` 时，顶点序列按闭环比较：轮换起点或反转遍历方向
  不改变几何，应该得到同一个键；反转时要同步反转 bulge 符号，并交换起止宽度。忽略
  `vertex_id` 这类仅标识顶点的字段。仍须比较整个顶点环及其属性，不能凭两条边等长、平行
  就判重。
- `closed: true` 会自动连接末点与首点，顶点列表只列各角点，不要再把首点重复追加到末尾。
  例如四个角点围成的 1000×300 矩形由一个闭合多段线实体表达，末点到首点的边由闭合标志生成。
- 句柄/属主/扩展数据不参与比较（每个对象的 ETag 都不同，比了就永远去不掉）；
- 坐标 round 到 0.001 mm，浮点噪声不算差别。

**② 语句级**（生成语句之后做）：不同来源可能落到同一句——`_Oblique` 块展开的 45° 斜线
与同位置的 Line 实体、钢筋断面与轮廓线，最终都是同一句 `CadDraw.Line(...)`。
按**最终语句文本**收口才彻底，实体级看不出"块替身"和"直线"是同一根。

实测收益：旧 5 分区 2274 → 去 196；当前视图 2831 → 去 208（实体级 203 + 语句级 5）。
去重后普通图元**没有两句完整语句相同**；验收方法就是拿普通图元按语句文本分组，
只剩 `CadDraw.Polyline(` 这种"一句的开头"会重复。

**别把去重和合并混为一谈**：去重只删"完全重合"（0.001 mm 级），
合并处理"差 ≤ 0.5 mm 该连起来"的两根线。判定容差差三个数量级，各管各的。

## 4. 表格：整段转 TABLE（含合并单元格）

表格是唯一会**改变实体类型**的优化：一堆 Line + Text 变成 1 个 TableEntity（+ 若干 MergeCells）。

四步：

1. **周界闭合的最大子矩形**：横线按 y、竖线按 x 聚类（容差 1 mm），搜"四边都被实线覆盖"的最大矩形。
   表外的引线、尺寸线、画长了的边框被排除。
2. **缺分隔线 = 合并**：子矩形内部相邻两格之间没有那条线 → 并查集合并，连通块即单元格。
3. **块必须是矩形**：L 形/台阶形无法用 `MergeCells` 表达 → 整表放弃，退回线段+文字。
4. **线头还回去**：被表格吃掉的线若伸出表外，超出部分按原图层补画成 Line。

```csharp
var table01 = TableHelper.CreateTable(
    doc, CadDraw.P(4113.343, -16825.014), new string[,]
    {
        { "编号", "直径", "单根长", "根数", "总长", "单位重" },
        { "N1-1", "", "11914", "92", "1096.1", "4.830" },
    },
    [812.5, 812.5, 812.5, 812.5, 812.5, 812.5],   // 列宽，从左到右
    [571.156, 316.667, 316.667],                   // 行高，自上而下
    125),                                          // 字高
TableHelper.MergeCells(table01, 0, 0, 0, 5);
TableHelper.MergeCells(table01, 1, 1, 2, 1);
```

硬规则：

- `insertPoint` 是**左上角**，`data[0]` 是最上面一行；行高/列宽数量必须与行列数一致；
- `MergeCells(table, topRow, leftCol, bottomRow, rightCol)` **0 基、含边界**，只保留左上角内容；
  **合并区间不能重叠**（抛 `InvalidOperationException`）；
- 表格变量要在 `entities` 列表**之前**声明，再放进列表（合并需要引用）；
- 单元格文字按插入点中心归属；一格多行按 y 降序、x 升序拼空格；
- **空单元格不能写空串**：`TableHelper.SetTextContent` 已改为空值直接返回，
  否则 DwgWriter 会把整张表丢掉（实测写出的 DWG 里 TABLE 数量为 0）；
- `TableHelper` 建出的 TABLE 在 **0 层**，末尾 `foreach (var table in entities.OfType<TableEntity>()) table.Layer = b03;`；
- 追加模式下没有 `using DwgSharpKit.Tables;` 时用全限定名 `DwgSharpKit.Tables.TableHelper`。

仍要降级（保持线段+文字 + 注释）：**找不到闭合外框**、**合并块不是矩形**。

## 5. 跳过项

`Defpoints` 的 `Point`、`Hatch`、`Viewport`、`Solid` 默认不还原
（`CadDraw` 没有 Solid 帮助器；需要时只能手写 `new Solid { ... }` 并自行处理四个角点）。

跳过的**类型与数量**必须出现在交付说明里。静默丢弃是最常见的"看起来成功、其实缺东西"。

## 6. 方法骨架

```csharp
/// <summary>区域 1：源图窗口 [0, 135, 1094, 561]，418 个实体，32.72×13.13 m。</summary>
public static void DrawRegion01(CadDocument doc)
{
    var b01 = doc.Layer(CadLayers.B01);
    var b03 = doc.Layer(CadLayers.B03);
    var b04 = doc.Layer(CadLayers.B04);
    var b07 = doc.Layer(CadLayers.B07);

    List<Entity> entities =
    [
        // 普通图元：多段线、直线、文字、块、表格等
    ];

    List<Entity> dimensionEntities =
    [
        // rotation: 0
        // CadDraw.RotatedDimension(...) 或 ..CadDraw.ChainDimension(...)

        // rotation: Math.PI / 2
        // 同角度标注放在一起

        // rotation: Math.PI
        // 与 rotation: 0 分组保存
    ];

    doc.AddEntities(entities);
    doc.AddEntities(dimensionEntities);
}
```

- 先分普通图元与原生标注两个本地列表，最后分别 `AddEntities`；无标注区域无需空标注列表；
- 标注列表先按 rotation 分组，再在各组内检查文字坐标与连续性并合并尺寸链，详见 §2.1；
- 一个方法只服务一个区域 JSON，不抽公共函数；
- 注释里保留 `pixel_box` 与实体数，方便回溯分区。


