import argparse
import math

from pyautocad import Autocad
# from comtypes.gen. import IZcadDimRotated

# 标准图层目录：名称 -> CadLayers 常量名（与 DwgSharpKit/Standards/CadLayers.cs 保持一致）。
# 已知标准层生成 doc.Layer(CadLayers.Bxx) 引用，未知层回退 doc.Layers["..."] 字符串。
STANDARD_LAYERS = {
    "B-01 粗实线": "CadLayers.B01",
    "B-02 粗虚线": "CadLayers.B02",
    "B-03 标注": "CadLayers.B03",
    "B-04 细实线": "CadLayers.B04",
    "B-05 细虚线": "CadLayers.B05",
    "B-06 中心线": "CadLayers.B06",
    "B-07 文字": "CadLayers.B07",
    "B-08 地层地质": "CadLayers.B08",
    "B-09 不打印": "CadLayers.B09",
}


def layer_ref(layer: str) -> str:
    """将图层名转换为 C# 引用表达式。"""
    constant = STANDARD_LAYERS.get(layer)
    return f"doc.Layer({constant})" if constant else f'doc.Layers["{cs_string(layer)}"]'


def fmt_num(value: float) -> str:
    """将 CAD 数值格式化为简洁的 C# 浮点数字面量。"""
    text = f"{float(value):.6f}".rstrip("0").rstrip(".")
    return text if text and text != "-0" else "0"


def cs_string(value: str) -> str:
    """转义字符串中的反斜杠和双引号，使其可以安全嵌入 C# 字符串。"""
    return value.replace("\\", "\\\\").replace('"', '\\"')


def normalize_text_rotation(rotation: float) -> float:
    """将水平文字的 0/180/360 度等价角统一输出为 0。"""
    angle = float(rotation) % (2 * math.pi)
    if min(angle, abs(angle - math.pi), abs(angle - 2 * math.pi)) < 1e-6:
        return 0
    return float(rotation)


def point3(values) -> str:
    """将 ZWCAD 三维点转换为 CadCli 的 CadDraw.P 调用代码。"""
    x = values[0]
    y = values[1]
    z = values[2] if len(values) > 2 else 0
    return f"CadDraw.P({fmt_num(x)}, {fmt_num(y)}, {fmt_num(z)})"


def point2(x: float, y: float) -> str:
    """将二维坐标转换为 CadCli 的 CadDraw.P2 调用代码。"""
    return f"CadDraw.P2({fmt_num(x)}, {fmt_num(y)})"


def vertex2(x: float, y: float, bulge: float = 0) -> str:
    """生成可直接放入 Rebar.Vertices 的钢筋顶点代码。"""
    bulge_text = fmt_num(bulge)
    if float(bulge) == 0:
        return f"RebarDetail.V({fmt_num(x)}, {fmt_num(y)})"
    return f'RebarDetail.V({fmt_num(x)}, {fmt_num(y)}, "", {bulge_text})'


def emit_line(index: int, ent) -> str:
    """将 ZWCAD 直线实体转换为 CadDraw.Line C# 代码。"""
    layer = layer_ref(ent.Layer)
    return f"""doc.Entities.Add(CadDraw.Line(
    {point3(ent.StartPoint)},
    {point3(ent.EndPoint)},
    {layer}));"""


def emit_arc(index: int, ent) -> str:
    """将 ZWCAD 圆弧实体转换为 CadDraw.Arc C# 代码。"""
    layer = layer_ref(ent.Layer)
    return f"""doc.Entities.Add(CadDraw.Arc(
    {point3(ent.Center)},
    {fmt_num(ent.Radius)},
    {fmt_num(ent.StartAngle)},
    {fmt_num(ent.EndAngle)},
    {layer}));"""


def emit_circle(index: int, ent) -> str:
    """将 ZWCAD 圆实体转换为 CadDraw.Circle C# 代码。"""
    layer = layer_ref(ent.Layer)
    return f"""doc.Entities.Add(CadDraw.Circle(
    {point3(ent.Center)},
    {fmt_num(ent.Radius)},
    {layer}));"""


def emit_rotated_dimension(index: int, ent) -> str:
    """将 ZWCAD 旋转线性标注转换为 CadDraw.RotatedDimension C# 代码。

    对应 C# 签名（CadDraw.Dimensions.cs）：
        RotatedDimension(XYZ startPoint, XYZ endPoint, XYZ dimensionLinePoint,
                         Layer? layer = null, string text = "")
    接口仅暴露 TextPosition、Measurement、Rotation，无尺寸界线原点，
    因此以文字位置为中心、沿旋转方向上下各取一半
    实际长度 = Measurement / LinearScaleFactor 作为起点/终点，
    尺寸线（文字位置）作为 dimensionLinePoint。
    """
    text = cs_string(getattr(ent, "TextOverride", ""))
    measurement = float(ent.Measurement)
    scale = float(getattr(ent, "LinearScaleFactor", 1))
    rotation = float(ent.Rotation)
    position = ent.TextPosition
    cx, cy = position[0], position[1]
    cz = position[2] if len(position) > 2 else 0
    dx, dy = math.cos(rotation), math.sin(rotation)
    half = (measurement / scale) / 2

    args = [
        point3([cx - dx * half, cy - dy * half, cz]),
        point3([cx + dx * half, cy + dy * half, cz]),
        point3([cx, cy, cz]),
    ]
    # layer = layer_ref(ent.Layer)
    # if layer != "doc.Layer(CadLayers.B03)":
    #     args.append(layer)
    # if text:
    #     args.append(f'text: "{text}"')

    body = ",\n    ".join(args)
    return f"""doc.Entities.Add(CadDraw.RotatedDimension(
    {body}));"""


def emit_angular_2line(index: int, ent) -> str:
    """将两条线角度标注转换为 CadDraw.Angular2Line C# 代码。"""
    layer = layer_ref(ent.Layer)
    text = cs_string(getattr(ent, "TextOverride", ""))
    return f"""doc.Entities.Add(CadDraw.Angular2Line(
    {point3(ent.ExtensionLine1StartPoint)},
    {point3(ent.ExtensionLine2StartPoint)},
    {point3(ent.AngleVertex)},
    {point3(ent.ArcPoint)},
    {layer},
    text: "{text}"));"""


def emit_angular_3point(index: int, ent) -> str:
    """将三点角度标注转换为 CadDraw.Angular3Point C# 代码。"""
    layer = layer_ref(ent.Layer)
    text = cs_string(getattr(ent, "TextOverride", ""))
    return f"""doc.Entities.Add(CadDraw.Angular3Point(
    {point3(ent.ExtensionLine1StartPoint)},
    {point3(ent.ExtensionLine2StartPoint)},
    {point3(ent.AngleVertex)},
    {point3(ent.ArcPoint)},
    {layer},
    text: "{text}"));"""


def get_bulge(ent, vertex_index: int) -> float:
    """读取多段线顶点的 bulge；不支持时按直线段处理。"""
    try:
        return float(ent.GetBulge(vertex_index))
    except Exception:
        return 0


def emit_lwpolyline(index: int, ent) -> str:
    """将二维多段线转换为可直接粘贴到 Rebar.Vertices 的顶点列表。"""
    coords = list(ent.Coordinates)
    vertices = []
    for i in range(0, len(coords), 2):
        vertex_index = i // 2
        vertices.append(vertex2(coords[i], coords[i + 1], get_bulge(ent, vertex_index)))

    vertices_code = ",\n    ".join(vertices)
    closed_note = " // closed polyline" if bool(getattr(ent, "Closed", False)) else ""
    return f"""[
    {vertices_code}
]{closed_note}"""


def emit_text(index: int, ent) -> str:
    """将单行文字实体转换为 CadDraw.Text C# 代码。"""
    layer = layer_ref(ent.Layer)
    value = cs_string(ent.TextString)
    height = fmt_num(ent.Height)
    rotation = fmt_num(normalize_text_rotation(getattr(ent, "Rotation", 0)))
    return f"""doc.Entities.Add(CadDraw.Text(
    "{value}",
    {point3(ent.InsertionPoint)},
    {height},
    {layer},
    rotation: {rotation}));"""


def iter_selected_objects(zwcad: Autocad):
    """调用 ZWCAD 的屏幕选择，让用户选择需要逆向的图元。"""
    selection = zwcad.get_selection("Select CAD objects to generate C# code")
    for i in range(selection.Count):
        yield selection.Item(i)


def iter_source_objects(zwcad: Autocad, all_objects: bool):
    """根据运行模式返回全图实体或用户选择的实体。"""
    if all_objects:
        yield from zwcad.iter_objects(dont_cast=True)
        return

    yield from iter_selected_objects(zwcad)


def main():
    """读取 ZWCAD 图元并输出可粘贴到 CadCli 的 C# 绘图代码。"""
    parser = argparse.ArgumentParser(
        description="Generate CadCli C# drawing code from selected ZWCAD entities."
    )
    parser.add_argument(
        "--all",
        action="store_true",
        help="Read all objects in the active layout instead of selecting on screen.",
    )
    args = parser.parse_args()

    zwcad = Autocad(create_if_not_exists=False)
    emitters = {
        "AcDbLine": emit_line,
        "AcDbArc": emit_arc,
        "AcDbCircle": emit_circle,
        "AcDbRotatedDimension": emit_rotated_dimension,
        "AcDb2LineAngularDimension": emit_angular_2line,
        "AcDb3PointAngularDimension": emit_angular_3point,
        "AcDbPolyline": emit_lwpolyline,
        "AcDbText": emit_text,
    }

    print("// 普通图元可粘贴到绘图方法体；AcDbPolyline 输出为 Rebar.Vertices 顶点列表。")
    print("// 多段线顶点使用 RebarDetail.V(x, y, text, bulge)，不要改成 CadDraw.V。")
    print(
        "// Paste the generated code into CadCli.Generated.GeneratedDraw.DrawBearingReinforcement(CadDocument doc)"
    )
    print(
        "// or CadCli.Generated.GeneratedDraw.DrawFrameBridgeReinforcement(CadDocument doc)."
    )
    count = 0
    skipped = {}

    for ent in iter_source_objects(zwcad, args.all):
        name = getattr(ent, "ObjectName", "")
        emitter = emitters.get(name)
        if emitter is None:
            skipped[name] = skipped.get(name, 0) + 1
            continue

        count += 1
        print()
        print(f"// {count}: {name}")
        try:
            print(emitter(count, ent))
        except Exception as exc:
            print(f"// Failed to emit {name}: {exc}")

    print()
    print(f"// Emitted entities: {count}")
    if skipped:
        print("// Skipped entities:")
        for name, item_count in sorted(skipped.items()):
            print(f"//   {name}: {item_count}")


if __name__ == "__main__":
    print()
    main()
