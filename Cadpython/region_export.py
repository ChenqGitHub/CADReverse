"""Export a user-selected CAD region as AI-friendly JSON."""

from __future__ import annotations

import argparse
import json
import math
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from pyautocad import APoint, Autocad


def _value(value: Any) -> Any:
    if value is None or isinstance(value, (str, bool, int)):
        return value
    if isinstance(value, float):
        return value if math.isfinite(value) else None
    try:
        return [_value(item) for item in value]
    except TypeError:
        return str(value)


def _get(obj: Any, name: str, default: Any = None) -> Any:
    try:
        return _value(getattr(obj, name))
    except Exception:
        return default


def _point(value: Any) -> list[float] | None:
    if value is None:
        return None
    try:
        values = list(value)
        return [float(values[0]), float(values[1]), float(values[2]) if len(values) > 2 else 0.0]
    except (TypeError, ValueError, IndexError):
        return None


def _relative_point(value: Any, origin: list[float]) -> list[float] | None:
    point = _point(value)
    if point is None:
        return None
    return [point[index] - origin[index] for index in range(3)]


_POINT_KEYS = {
    "point",
    "start",
    "end",
    "center",
    "min",
    "max",
    "insertion_point",
    "text_position",
    "start_point",
    "end_point",
    "dimension_line_point",
    "angle_vertex",
    "arc_point",
}


def _make_relative(value: Any, origin: list[float], key: str | None = None) -> Any:
    """Translate coordinate-bearing values while leaving lengths and angles unchanged."""
    if isinstance(value, dict):
        return {child_key: _make_relative(child, origin, child_key) for child_key, child in value.items()}
    if isinstance(value, list):
        if key in _POINT_KEYS or (key is not None and key.endswith("Point")):
            point = _relative_point(value, origin)
            if point is not None:
                return point
        return [_make_relative(child, origin) for child in value]
    return value


def _first_point(entity: Any, property_names: tuple[str, ...]) -> list[float] | None:
    for property_name in property_names:
        point = _point(_get(entity, property_name))
        if point is not None:
            return point
    return None


def _bbox(entity: Any) -> dict[str, list[float]] | None:
    try:
        lower, upper = entity.GetBoundingBox()
        return {"min": _point(lower), "max": _point(upper)}
    except Exception:
        return None


def _common(entity: Any, index: int) -> dict[str, Any]:
    return {
        "id": index,
        "handle": _get(entity, "Handle"),
        "object_name": _get(entity, "ObjectName"),
        "layer": _get(entity, "Layer"),
        "color_index": _get(entity, "Color"),
        "line_type": _get(entity, "Linetype"),
        "line_weight": _get(entity, "Lineweight"),
        "line_type_scale": _get(entity, "LinetypeScale"),
        "visible": _get(entity, "Visible"),
        "bounding_box": _bbox(entity),
    }


def _read_entity(entity: Any, index: int, origin: list[float]) -> dict[str, Any]:
    item = _common(entity, index)
    name = item["object_name"] or ""
    geometry: dict[str, Any] = {}

    if name == "AcDbLine":
        geometry = {"start": _point(_get(entity, "StartPoint")), "end": _point(_get(entity, "EndPoint"))}
    elif name == "AcDbArc":
        geometry = {
            "center": _point(_get(entity, "Center")),
            "radius": _get(entity, "Radius"),
            "start_angle": _get(entity, "StartAngle"),
            "end_angle": _get(entity, "EndAngle"),
        }
    elif name == "AcDbCircle":
        geometry = {"center": _point(_get(entity, "Center")), "radius": _get(entity, "Radius")}
    elif name in {"AcDbPolyline", "AcDb2dPolyline", "AcDb3dPolyline"}:
        coordinates = _get(entity, "Coordinates") or []
        values = list(coordinates)
        stride = 3 if name == "AcDb3dPolyline" else 2
        vertices = []
        for offset in range(0, len(values), stride):
            point = [float(values[offset]), float(values[offset + 1]), 0.0]
            if stride == 3 and offset + 2 < len(values):
                point[2] = float(values[offset + 2])
            try:
                bulge = float(entity.GetBulge(offset // stride))
            except Exception:
                bulge = 0.0
            vertices.append({"point": point, "bulge": bulge})
        geometry = {
            "vertices": vertices,
            "closed": bool(_get(entity, "Closed", False)),
            "elevation": _get(entity, "Elevation", 0),
            "constant_width": _get(entity, "ConstantWidth"),
        }
    elif name == "AcDbText":
        geometry = {
            "text": _get(entity, "TextString", ""),
            "insertion_point": _point(_get(entity, "InsertionPoint")),
            "height": _get(entity, "Height"),
            "rotation": _get(entity, "Rotation", 0),
            "style": _get(entity, "StyleName"),
            "horizontal_alignment": _get(entity, "HorizontalAlignment"),
            "vertical_alignment": _get(entity, "VerticalAlignment"),
        }
    elif name == "AcDbMText":
        geometry = {
            "text": _get(entity, "TextString", _get(entity, "Contents", "")),
            "insertion_point": _point(_get(entity, "InsertionPoint")),
            "height": _get(entity, "Height"),
            "rotation": _get(entity, "Rotation", 0),
            "style": _get(entity, "StyleName"),
            "attachment_point": _get(entity, "AttachmentPoint"),
            "width": _get(entity, "Width"),
        }
    elif name == "AcDbRotatedDimension":
        geometry = {
            "text": _get(entity, "TextOverride", ""),
            "measurement": _get(entity, "Measurement"),
            "text_position": _point(_get(entity, "TextPosition")),
            "start_point": _first_point(
                entity, ("ExtLine1Point", "ExtensionLine1Point", "ExtensionLine1StartPoint")
            ),
            "end_point": _first_point(
                entity, ("ExtLine2Point", "ExtensionLine2Point", "ExtensionLine2StartPoint")
            ),
            "dimension_line_point": _first_point(entity, ("DimLinePoint",)),
            "rotation": _get(entity, "Rotation"),
            "dimension_style": _get(entity, "StyleName"),
        }
        for property_name in (
            "ExtLine1Point", "ExtLine2Point",
            "ExtensionLine1Point", "ExtensionLine2Point",
            "ExtensionLine1StartPoint", "ExtensionLine1EndPoint",
            "ExtensionLine2StartPoint", "ExtensionLine2EndPoint",
            "DimLinePoint",
        ):
            point = _point(_get(entity, property_name))
            if point is not None:
                geometry[property_name] = point
    elif "Dimension" in name:
        geometry = {
            "text": _get(entity, "TextOverride", ""),
            "measurement": _get(entity, "Measurement"),
            "text_position": _point(_get(entity, "TextPosition")),
            "rotation": _get(entity, "Rotation"),
            "dimension_style": _get(entity, "StyleName"),
        }
        for property_name in (
            "ExtensionLine1StartPoint", "ExtensionLine1EndPoint",
            "ExtensionLine2StartPoint", "ExtensionLine2EndPoint",
            "AngleVertex", "ArcPoint",
        ):
            point = _point(_get(entity, property_name))
            if point is not None:
                geometry[property_name] = point
    elif name == "AcDbBlockReference":
        geometry = {
            "block_name": _get(entity, "EffectiveName", _get(entity, "Name")),
            "insertion_point": _point(_get(entity, "InsertionPoint")),
            "rotation": _get(entity, "Rotation", 0),
            "scale": _point(_get(entity, "ScaleFactors")),
            "attributes": [],
        }
        try:
            geometry["attributes"] = [
                {"tag": _get(attribute, "TagString"), "text": _get(attribute, "TextString")}
                for attribute in entity.GetAttributes()
            ]
        except Exception:
            pass
    else:
        item["unsupported"] = True

    item["geometry"] = geometry
    return _make_relative(item, origin)


def _detect_line_arrays(entities: list[dict[str, Any]], tolerance: float = 1e-4) -> list[dict[str, Any]]:
    """Find equally translated line sequences without asking AI to infer spacing."""
    groups: dict[tuple[Any, ...], list[dict[str, Any]]] = {}
    for entity in entities:
        if entity.get("object_name") != "AcDbLine":
            continue
        geometry = entity.get("geometry", {})
        start, end = geometry.get("start"), geometry.get("end")
        if not start or not end:
            continue
        vector = tuple(round(end[i] - start[i], 6) for i in range(3))
        key = (entity.get("layer"), vector)
        groups.setdefault(key, []).append(entity)

    arrays = []
    for (layer, vector), group in groups.items():
        group.sort(key=lambda item: tuple(item["geometry"]["start"]))
        if len(group) < 3:
            continue

        first = group[0]["geometry"]["start"]
        second = group[1]["geometry"]["start"]
        spacing = [second[i] - first[i] for i in range(3)]
        if math.sqrt(sum(value * value for value in spacing)) <= tolerance:
            continue

        valid = all(
            all(
                abs(
                    group[index]["geometry"]["start"][axis]
                    - group[index - 1]["geometry"]["start"][axis]
                    - spacing[axis]
                )
                <= tolerance
                for axis in range(3)
            )
            for index in range(2, len(group))
        )
        if not valid:
            continue

        arrays.append(
            {
                "type": "linear_array",
                "layer": layer,
                "object_name": "AcDbLine",
                "base_entity_id": group[0]["id"],
                "entity_ids": [item["id"] for item in group],
                "count": len(group),
                "translation_per_item": spacing,
                "spacing": math.sqrt(sum(value * value for value in spacing)),
                "element_direction": list(vector),
            }
        )
    return arrays


def _select_origin(zwcad: Autocad) -> list[float]:
    value = zwcad.doc.Utility.GetPoint(
        APoint(0, 0, 0),
        "Select local coordinate origin (center point): ",
    )
    origin = _point(value)
    if origin is None:
        raise RuntimeError("CAD did not return a valid center point.")
    return origin


def _resolve_origin(
    zwcad: Autocad,
    origin_mode: str,
    selection_mode: str,
) -> list[float]:
    """整图导出默认用绝对坐标；手动框选默认让用户点局部原点。"""
    if origin_mode == "zero" or (origin_mode == "auto" and selection_mode == "all"):
        return [0.0, 0.0, 0.0]
    return _select_origin(zwcad)


def _iter_layout_objects(zwcad: Autocad):
    """遍历当前布局（模型空间或图纸空间）的全部实体。"""
    block = zwcad.doc.ActiveLayout.Block
    for index in range(int(block.Count)):
        yield block.Item(index)


def _select_rectangle(zwcad: Autocad, mode: str, origin: list[float]):
    """矩形框选；``window`` 只选完全包含的实体，``crossing`` 还包含相交实体。"""
    utility = zwcad.doc.Utility
    # AutoCAD's COM binding rejects None for the optional BasePoint argument.
    # APoint is an array type understood by both AutoCAD and ZWCAD COM APIs.
    first = utility.GetPoint(APoint(*origin), "Select region first corner: ")
    second = utility.GetCorner(first, "Select region opposite corner: ")
    selection = zwcad.doc.SelectionSets.Add(f"CADReverse_{uuid.uuid4().hex}")
    selection.Select(0 if mode == "window" else 1, first, second)
    return selection


def _entities_bbox(entities: list[dict[str, Any]]) -> dict[str, Any] | None:
    """由已导出实体的包围盒汇总区域范围（相对原点，单位与图纸一致）。"""
    boxes = [item.get("bounding_box") for item in entities]
    boxes = [box for box in boxes if box and box.get("min") and box.get("max")]
    if not boxes:
        return None
    minimum = [min(box["min"][axis] for box in boxes) for axis in range(3)]
    maximum = [max(box["max"][axis] for box in boxes) for axis in range(3)]
    return {"min": minimum, "max": maximum}


def export_region(selection_mode: str, mode: str, origin_mode: str) -> dict[str, Any]:
    zwcad = Autocad(create_if_not_exists=False)
    origin = _resolve_origin(zwcad, origin_mode, selection_mode)

    selection = None
    if selection_mode == "all":
        source_objects = list(_iter_layout_objects(zwcad))
    elif selection_mode == "screen":
        selection = zwcad.get_selection("Select objects in the CAD region")
        source_objects = [selection.Item(index) for index in range(selection.Count)]
    else:
        selection = _select_rectangle(zwcad, mode, origin)
        source_objects = [selection.Item(index) for index in range(selection.Count)]

    try:
        entities = [
            _read_entity(entity, index + 1, origin)
            for index, entity in enumerate(source_objects)
        ]
    finally:
        if selection is not None:
            try:
                selection.Delete()
            except Exception:
                pass

    box = _entities_bbox(entities)
    return {
        "schema_version": "1.1",
        "purpose": "CAD region for semantic recognition and C# generation",
        "source": {
            "application": _get(zwcad.app, "Name"),
            "document": _get(zwcad.doc, "Name"),
            "layout": _get(zwcad.doc.ActiveLayout, "Name"),
            "exported_at_utc": datetime.now(timezone.utc).isoformat(),
            "selection_origin": origin,
        },
        "selection_mode": selection_mode,
        "region": {
            "mode": selection_mode,
            "min": box["min"] if box else None,
            "max": box["max"] if box else None,
        },
        "entity_count": len(entities),
        "entities": entities,
        "repetition_candidates": _detect_line_arrays(entities),
        "unsupported_object_names": sorted(
            {item["object_name"] for item in entities if item.get("unsupported")}
        ),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Export a selected CAD region to AI-friendly JSON.")
    parser.add_argument("-o", "--output", default="cad-region.json", help="Output JSON path.")
    parser.add_argument(
        "--selection",
        choices=("screen", "rectangle", "all"),
        default="screen",
        help=(
            "screen 用 pyautocad 屏幕选择；rectangle 用矩形窗口；"
            "all 导出当前布局全部实体（供 classify_regions.py 自动分区）。"
        ),
    )
    parser.add_argument("--mode", choices=("window", "crossing"), default="crossing")
    parser.add_argument(
        "--origin",
        choices=("auto", "pick", "zero"),
        default="auto",
        help="坐标原点：auto 为整图取绝对坐标、框选取交互点；pick 强制交互；zero 用 (0,0,0)。",
    )
    args = parser.parse_args()
    payload = export_region(args.selection, args.mode, args.origin)
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Exported {payload['entity_count']} entities to {output.resolve()}")
    if payload["unsupported_object_names"]:
        print("Unsupported entity types:")
        for name in payload["unsupported_object_names"]:
            print(f"  {name}")


if __name__ == "__main__":
    main()
