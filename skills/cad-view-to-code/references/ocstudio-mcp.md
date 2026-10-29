# OpenCAD Studio MCP 取图与取数

## 连接方式

OpenCAD Studio（`D:\OpenCad\OpenCADStudio.exe`）以 `--mcp` 启动后是一个 **stdio JSON-RPC** 服务，
它附着到**已经在跑的 GUI 进程**上：读写都发生在 GUI 里，桥进程退出后文档状态保留。

协议版本 `2025-11-25`；握手：

```json
{"jsonrpc":"2.0","id":1,"method":"initialize",
 "params":{"protocolVersion":"2025-11-25","capabilities":{},
           "clientInfo":{"name":"cad-view-to-code","version":"1"}}}
```

随后发 `notifications/initialized`，再 `tools/call`。

环境变量 `OCS_SKIP_SCHEMA_SYNC=1` 可以跳过启动时的 schema 同步，明显加快每次调用（桥是**一次调用一个进程**）。

## 四个工具

| 工具 | 用途 |
|---|---|
| `ocs_sessions` | 列出 GUI 会话与文档、camera、图层、能力列表 |
| `ocs_read` | 只读：`query` / `records` / `properties` / `layers` / `capabilities` … |
| `ocs_execute` | 写入：建实体、改属性、批量、命令 |
| `ocs_capture` | 视口/窗口截图（PNG + 元数据） |

会话 id 来自 `ocs_sessions` 返回的 `session_id`（示例 `c883cd77fe4e70b2f6824bf3cbfb7a68`），
**每次调用都要带** `ocs_session_id`，否则报 `Missing ocs_session_id`。

## 关键坑：参数放在哪一层

- `ocs_capture` 的参数在**顶层**：`{"ocs_session_id": …, "scope": "viewport", "max_dimension": 1600}`
- `ocs_read` / `ocs_execute` 的过滤器在 **`parameters` 里**：

```json
{"ocs_session_id": "…", "op": "query",
 "parameters": {"bounds": [minx,miny,maxx,maxy], "detail": "full", "limit": 2000, "offset": 0}}
```

放错层的后果很隐蔽：不会报错，而是**忽略过滤条件**返回整张图纸的全部实体
（`count: 26241` 就是全图数量）。自查办法：拿一个明显空白的世界框（如 `[900000,900000,901000,901000]`）查询，
必须返回 0；如果仍返回全量，就是参数层级写错了。

`bounds` 是**扁平四元组** `[min_x, min_y, max_x, max_y]`，不是对象。

`detail` 三档：`summary`（够做统计）/ `geometry`（默认）/ `full`（含 `properties`、块 `attributes`、bulge，
转换必须用 full）。

`limit` 上限 10000，超出按页取：`offset += len(batch)`，直到 `offset >= count` 或返回不足一页。

## 抓图

全景（默认，不移动相机）：

```json
{"ocs_session_id":"…","scope":"viewport","max_dimension":1600}
```

**按区域放大**（会把相机推过去，抓完记得复原）：

```json
{"ocs_session_id":"…","view":"region","bounds":[-31055,-29813,1669,-16681],"max_dimension":2048}
```

`view` 还有 `extents`（全图）与 `selection`（当前选择）；`bounds` 只在 `view:"region"` 时生效。
`max_dimension` 上限 4096。另可传 `focus_handles`（按实体句柄取景）、`annotate`（编号标记）、
`diff` / `diff_mode`（和上一次截图做差分）、`tile`（DeepZoom 金字塔分块）。

返回两个内容块：`image`（PNG base64）与 `text`（元数据 JSON）。
元数据关键字段：

```json
{"width":1600,"height":686,
 "viewport_world_bounds":{"min":[-30734.6,-33156.7],"max":[16187.3,-13043.9]},
 "pixel_to_world_matrix":[29.326,0.0,0.0,-29.319,-30734.6,-13043.86],
 "camera_revision":774,"hash":"b1bf8b…"}
```

`pixel_to_world_matrix` 是 **6 元扁平数组**（不是 3×3）：`[a,b,c,d,e,f]`，
`wx = a*px + c*py + e`，`wy = b*px + d*py + f`。

**矩阵只对本次返回的这张图有效**：`view:"region"` 会改动相机，矩阵也随之改变。
拿 A 图的矩阵去解 B 图，或者图纸被平移后再用旧矩阵，投影会整体偏移——表现就是"投影处全是背景"。
自查办法：把 JSON 里的实体坐标投影回图，看有没有墨迹（`ocs_regions.py check`），命中率应为 ~100%。

`max_dimension` 控制长边像素；分区精度约等于"世界单位/像素 = 矩阵的 a"（本例 29.3 mm/px）。
放大到区域级时这个值会降到 3~7 mm/px，字才看得清。

图像里可能带界面叠加（视图立方、命令行条），它们也在"viewport 截图"里，所以按墨迹分区会
在多出来的边缘产生"有墨迹但 0 实体"的幻影区域——那是界面，不是图。

## 实体 JSON 形状

常用字段（`type` 与 `display_type` 同时存在，值相同）：

```text
Line        start, end                      # 都是 [x,y,z]
Polyline    vertices / bulges               # 全局宽度、闭合标志在 properties
Arc         center, radius, start_angle, end_angle   # 角度制
Circle      center, radius
Text        value, position, height         # position 是 [x,y,z]；旋转在 properties.rotation
MText       value, position, height
Block Ref.  block, position, attributes{}, properties.{x_scale,y_scale,rotation}
Dimension   properties.Linear/Aligned.{first_point,second_point,definition_point,base}
```

`attributes` 的键是**本地化**的（`编号`、`直径`），取值也是字符串，不要假设英文键。

## 中文与编码

Windows 控制台按 GBK 输出会把中文渲染成乱码，但 **JSON 本身是 UTF-8 且内容正确**。
凡是人眼要看的结论，写进 UTF-8 文件再读，不要在终端里比对中文。

PowerShell 给原生程序传 JSON 参数时要小心引号：用单引号包住含双引号的 JSON，
**不要**写字面 `\"`（会被当成反斜杠原样传入，`json.loads` 直接失败）。
参数一长就改用脚本内的 `json.dumps`，不要走命令行拼接。

## 会话自愈

- 编辑器重启后 `session_id` 会变：每次流程开头重新 `ocs_sessions` 取一次；
- `mode` 必须是 `gui`（还有 `headless` 之类的模式不会渲染视口，抓不到图）；
- 文档 `dirty: true` 不影响读取，但生成的 DWG 应写到独立文件，别覆盖源图。
