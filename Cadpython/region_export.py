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


def _read_entity(entity: Any, index: int) -> dict[str, Any]:
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
    return item


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


def _select_region(zwcad: Autocad, mode: str) -> tuple[Any, dict[str, Any]]:
    utility = zwcad.doc.Utility
    # AutoCAD's COM binding rejects None for the optional BasePoint argument.
    # APoint is an array type understood by both AutoCAD and ZWCAD COM APIs.
    first = utility.GetPoint(APoint(0, 0, 0), "Select region first corner: ")
    second = utility.GetCorner(first, "Select region opposite corner: ")
    p1, p2 = _point(first), _point(second)
    if p1 is None or p2 is None:
        raise RuntimeError("CAD did not return valid selection points.")

    selection = zwcad.doc.SelectionSets.Add(f"CADReverse_{uuid.uuid4().hex}")
    selection.Select(0 if mode == "window" else 1, first, second)
    return selection, {
        "mode": mode,
        "min": [min(p1[0], p2[0]), min(p1[1], p2[1]), min(p1[2], p2[2])],
        "max": [max(p1[0], p2[0]), max(p1[1], p2[1]), max(p1[2], p2[2])],
    }


def _select_on_screen(zwcad: Autocad) -> tuple[Any, dict[str, Any]]:
    """Use pyautocad's native SelectOnScreen interaction."""
    selection = zwcad.get_selection("Select objects in the CAD detail region")
    boxes = [_bbox(selection.Item(i)) for i in range(selection.Count)]
    boxes = [box for box in boxes if box and box["min"] and box["max"]]
    if boxes:
        minimum = [min(box["min"][axis] for box in boxes) for axis in range(3)]
        maximum = [max(box["max"][axis] for box in boxes) for axis in range(3)]
    else:
        minimum = maximum = None
    return selection, {"mode": "screen", "min": minimum, "max": maximum}


def export_region(selection_mode: str, mode: str) -> dict[str, Any]:
    zwcad = Autocad(create_if_not_exists=False)
    if selection_mode == "screen":
        selection, region = _select_on_screen(zwcad)
    else:
        selection, region = _select_region(zwcad, mode)
    try:
        entities = [_read_entity(selection.Item(i), i + 1) for i in range(selection.Count)]
        return {
            "schema_version": "1.0",
            "purpose": "CAD region for semantic recognition and C# generation",
            "source": {
                "application": _get(zwcad.app, "Name"),
                "document": _get(zwcad.doc, "Name"),
                "layout": _get(zwcad.doc.ActiveLayout, "Name"),
                "exported_at_utc": datetime.now(timezone.utc).isoformat(),
            },
            "region": region,
            "entity_count": len(entities),
            "entities": entities,
            "repetition_candidates": _detect_line_arrays(entities),
            "unsupported_object_names": sorted(
                {item["object_name"] for item in entities if item.get("unsupported")}
            ),
        }
    finally:
        try:
            selection.Delete()
        except Exception:
            pass


def main() -> None:
    parser = argparse.ArgumentParser(description="Export a selected CAD region to AI-friendly JSON.")
    parser.add_argument("-o", "--output", default="cad-region.json", help="Output JSON path.")
    parser.add_argument(
        "--selection",
        choices=("screen", "rectangle"),
        default="screen",
        help="screen uses pyautocad get_selection; rectangle uses a rectangular selection window.",
    )
    parser.add_argument("--mode", choices=("window", "crossing"), default="crossing")
    args = parser.parse_args()
    payload = export_region(args.selection, args.mode)
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
