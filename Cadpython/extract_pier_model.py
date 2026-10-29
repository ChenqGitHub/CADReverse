"""从桥墩钢筋图的三个区域 JSON 提取语义模型（参数 / 大样模板 / 综合图放置 / 数量表）。

输出供 emit_pier_csharp.py 生成手写风格的 CadCli 代码；提取规则见
skills/cad-rebar-skill/references/json-to-csharp.md。

用法：
    python extract_pier_model.py general.json detail.json table.json -o model.json --report
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any

import classify_regions as classify
import linkage_report as linkage

BULGE_TOLERANCE = 1e-9
QUARTER_ARC_BULGE = 0.4142135623730951  # tan(90° / 4)
FIT_TOLERANCE = 1e-2


def _simplify_closed(vertices: list[dict[str, float]]) -> list[dict[str, float]]:
    """去掉闭合多段线上共线的冗余顶点（导出时附着在直线段上的捕捉点）。"""
    simplified = [dict(vertex) for vertex in vertices]
    changed = True
    while changed and len(simplified) > 4:
        changed = False
        count = len(simplified)
        result = []
        for index, vertex in enumerate(simplified):
            previous = simplified[(index - 1) % count]
            following = simplified[(index + 1) % count]
            if abs(vertex["bulge"]) <= BULGE_TOLERANCE:
                ax = vertex["x"] - previous["x"]
                ay = vertex["y"] - previous["y"]
                bx = following["x"] - vertex["x"]
                by = following["y"] - vertex["y"]
                if abs(ax * by - ay * bx) <= 1e-3 and ax * bx + ay * by > 0:
                    changed = True
                    continue
            result.append(vertex)
        simplified = result
    return simplified


def _channel_section_vertices(
    width: float,
    height: float,
    radius: float,
    channel_span: float,
) -> list[dict[str, float]]:
    """生成「圆角矩形 + 上下对称凹槽」截面的顶点（逆时针，16 个关键点）。

    与源图一致：凹槽深 = 圆角半径，槽口两侧是 45° 斜边；
    圆角段用 bulge = tan(90°/4) 表示。
    """
    half_span = channel_span / 2.0
    center = width / 2.0
    channel_start = center - half_span
    channel_end = center + half_span
    depth = radius

    def point(x: float, y: float, bulge: float = 0.0) -> dict[str, float]:
        return {"x": x, "y": y, "bulge": bulge}

    return [
        point(radius, 0.0),
        point(channel_start, 0.0),
        point(channel_start + radius, depth),
        point(channel_end - radius, depth),
        point(channel_end, 0.0),
        point(width - radius, 0.0, QUARTER_ARC_BULGE),
        point(width, radius),
        point(width, height - radius, QUARTER_ARC_BULGE),
        point(width - radius, height),
        point(channel_end, height),
        point(channel_end - radius, height - depth),
        point(channel_start + radius, height - depth),
        point(channel_start, height),
        point(radius, height, QUARTER_ARC_BULGE),
        point(0.0, height - radius),
        point(0.0, radius, QUARTER_ARC_BULGE),
    ]


def _sequence_error(canonical: list[dict[str, float]], source: list[dict[str, float]]) -> float:
    """两个顶点序列按任意起点、任意方向的逐点最大距离。"""
    if len(canonical) != len(source):
        return float("inf")

    def best_error(sequence: list[dict[str, float]]) -> float:
        error = float("inf")
        count = len(sequence)
        for offset in range(count):
            maximum = 0.0
            for index, item in enumerate(canonical):
                other = sequence[(index + offset) % count]
                maximum = max(maximum, math.hypot(item["x"] - other["x"], item["y"] - other["y"]))
                if maximum >= error:
                    break
            error = min(error, maximum)
        return error

    return min(best_error(source), best_error(list(reversed(source))))


def _fit_channel_section(
    vertices: list[dict[str, float]], record_layer: str = "细实线"
) -> dict[str, Any] | None:
    if len(vertices) < 16:
        return None
    simplified = _simplify_closed(vertices)
    if len(simplified) != 16:
        return None

    xs = [vertex["x"] for vertex in simplified]
    ys = [vertex["y"] for vertex in simplified]
    width = max(xs) - min(xs)
    height = max(ys) - min(ys)
    if width < 1500 or height < 1500:
        return None

    quarter_arcs = [
        vertex
        for vertex in simplified
        if abs(abs(vertex["bulge"]) - QUARTER_ARC_BULGE) <= 1e-3
    ]
    if len(quarter_arcs) != 4:
        return None
    if any(
        abs(vertex["bulge"]) > 1e-6
        and abs(abs(vertex["bulge"]) - QUARTER_ARC_BULGE) > 1e-3
        for vertex in simplified
    ):
        return None

    origin = (min(xs), min(ys))
    radius_estimates = []
    count = len(simplified)
    for index, vertex in enumerate(simplified):
        if abs(abs(vertex["bulge"]) - QUARTER_ARC_BULGE) > 1e-3:
            continue
        following = simplified[(index + 1) % count]
        chord = math.hypot(following["x"] - vertex["x"], following["y"] - vertex["y"])
        radius_estimates.append(chord / math.sqrt(2.0))
    if not radius_estimates:
        return None
    radius_estimates.sort()
    radius = radius_estimates[len(radius_estimates) // 2]

    channel_vertices: set[float] = set()
    for index, vertex in enumerate(simplified):
        if abs(vertex["bulge"]) > 1e-6:
            continue  # 圆角圆弧端点不是槽口斜边端点
        if abs(vertex["y"] - origin[1]) > 1e-3:
            continue
        for other_index in ((index - 1) % count, (index + 1) % count):
            other = simplified[other_index]
            if abs(other["bulge"]) > 1e-6:
                continue
            if abs(abs(other["y"] - vertex["y"]) - radius) <= 1e-2 and abs(
                abs(other["x"] - vertex["x"]) - radius
            ) <= 1e-2:
                channel_vertices.add(round(vertex["x"], 3))
    if len(channel_vertices) != 2:
        return None
    channel_start, channel_end = sorted(channel_vertices)
    channel_span = channel_end - channel_start

    canonical = _channel_section_vertices(width, height, radius, channel_span)
    offset = [
        {"x": vertex["x"] + origin[0], "y": vertex["y"] + origin[1], "bulge": vertex["bulge"]}
        for vertex in canonical
    ]
    error = _sequence_error(offset, simplified)
    if error > FIT_TOLERANCE:
        return None

    return {
        "type": "channel_section",
        "origin": [round(origin[0], 3), round(origin[1], 3)],
        "width": round(width, 3),
        "height": round(height, 3),
        "radius": round(radius, 3),
        "channel_span": round(channel_span, 3),
        "layer": record_layer,
        "fit_error": round(error, 6),
    }


def extract_components(polylines: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """识别可参数化的构件轮廓，并在原始多段线记录上打标记。"""
    components: list[dict[str, Any]] = []
    for record in polylines:
        if not record.get("closed") or len(record.get("vertices") or []) < 16:
            continue
        fit = _fit_channel_section(record["vertices"], record.get("layer", "细实线"))
        if fit is None:
            continue
        record["component"] = len(components)
        components.append(fit)
    return components


def _rect_distance(bbox: tuple[float, float, float, float], x: float, y: float) -> float:
    """点到包围盒的最短距离；点在盒内为 0。"""
    dx = max(bbox[0] - x, 0.0, x - bbox[2])
    dy = max(bbox[1] - y, 0.0, y - bbox[3])
    return math.hypot(dx, dy)


def _insert_position(entity: classify.Entity) -> tuple[float, float] | None:
    point = entity.geometry.get("position")
    if isinstance(point, dict):
        return float(point.get("x", 0.0)), float(point.get("y", 0.0))
    return None


def _polyline_vertices(entity: classify.Entity) -> list[dict[str, float]]:
    vertices = entity.geometry.get("vertices") or []
    return [
        {
            "x": float(vertex.get("x", 0.0)),
            "y": float(vertex.get("y", 0.0)),
            "bulge": float(vertex.get("bulge") or 0.0),
        }
        for vertex in vertices
    ]


def _is_shape_candidate(entity: classify.Entity) -> bool:
    """大样钢筋形状：粗实线上的多段线，排除文本下划线。

    弯折钢筋的大样外接盒可能接近正方形（如 1859×2100 的 U 形），
    因此只要求较短边不超过 2100、较长边不超过 9000。
    """
    if entity.kind != "LwPolyline" or not entity.geometry.get("vertices"):
        return False
    if entity.layer not in ("粗实线", "轮廓线"):
        return False
    width = entity.bbox[2] - entity.bbox[0]
    height = entity.bbox[3] - entity.bbox[1]
    return 300 <= max(width, height) <= 9000 and min(width, height) <= 2100


def extract_detail_templates(
    detail_entities: list[classify.Entity],
) -> tuple[dict[str, dict[str, Any]], list[dict[str, Any]]]:
    inserts = []
    for entity in detail_entities:
        if entity.kind != "Insert":
            continue
        attributes = entity.attributes or {}
        raw_number = attributes.get("编号") or ""
        if not raw_number:
            continue
        position = _insert_position(entity)
        numbers = linkage._expand_number(raw_number)
        spec = linkage._parse_spec(attributes.get("直径") or "")
        inserts.append(
            {
                "raw_number": raw_number,
                "numbers": numbers,
                "spec": spec,
                "position": position,
                "handle": entity.handle,
            }
        )

    candidates = [(entity, _polyline_vertices(entity)) for entity in detail_entities if _is_shape_candidate(entity)]
    pairs: list[tuple[float, int, int]] = []
    for insert_index, insert in enumerate(inserts):
        # 区间编号（N2-1～2）只是合并标注，形状归精确编号使用。
        if len(insert["numbers"]) != 1:
            continue
        for candidate_index, (entity, _vertices) in enumerate(candidates):
            distance = _rect_distance(entity.bbox, *insert["position"])
            pairs.append((distance, insert_index, candidate_index))
    pairs.sort(key=lambda item: item[0])

    assigned_insert: dict[int, int] = {}
    used_candidates: set[int] = set()
    for distance, insert_index, candidate_index in pairs:
        if insert_index in assigned_insert or candidate_index in used_candidates:
            continue
        if distance > 2500:
            break
        assigned_insert[insert_index] = candidate_index
        used_candidates.add(candidate_index)

    templates: dict[str, dict[str, Any]] = {}
    warnings: list[dict[str, Any]] = []
    for insert_index, insert in enumerate(inserts):
        numbers = insert["numbers"]
        if len(numbers) != 1:
            warnings.append(
                {
                    "kind": "range_detail",
                    "number": insert["raw_number"],
                    "message": "区间编号的大样未单独建模板，按精确编号模板复用",
                }
            )
            continue
        number = numbers[0]
        candidate_index = assigned_insert.get(insert_index)
        if candidate_index is None:
            warnings.append(
                {
                    "kind": "missing_template",
                    "number": number,
                    "message": "未找到与编号块足够接近的钢筋形状多段线",
                }
            )
            continue

        entity, vertices = candidates[candidate_index]
        origin = (vertices[0]["x"], vertices[0]["y"])
        local = [
            {
                "x": round(vertex["x"] - origin[0], 6),
                "y": round(vertex["y"] - origin[1], 6),
                "bulge": round(vertex["bulge"], 6),
            }
            for vertex in vertices
        ]
        distance = _rect_distance(entity.bbox, *insert["position"])
        spec = insert["spec"]
        true_length = spec.evaluate(None) if spec else None
        templates[number] = {
            "number": number,
            "diameter": spec.diameter if spec else None,
            "spec": spec.raw if spec else "",
            "formula": spec.formula if spec else None,
            "average": bool(spec and spec.average),
            "true_length": true_length,
            "closed": bool(entity.geometry.get("closed")),
            "origin": [round(origin[0], 3), round(origin[1], 3)],
            "vertices": local,
            "insert_position": [round(insert["position"][0], 3), round(insert["position"][1], 3)],
            "confidence": "high" if distance <= 800 else "medium" if distance <= 1800 else "low",
            "source_handles": [entity.handle, insert["handle"]],
        }
        if distance > 800:
            warnings.append(
                {
                    "kind": "template_distance",
                    "number": number,
                    "message": f"编号块与形状距离 {distance:.0f}，模板关联需人工复核",
                }
            )
    return templates, warnings


def extract_composite(
    general_path: Path,
) -> dict[str, Any]:
    entities, _inputs = classify.load_inputs([general_path])
    padding = classify.auto_padding(entities)
    merge_padding = classify.auto_merge_padding(entities)
    regions, views, _unassigned, noise = classify.build_regions(entities, padding, merge_padding)
    # 远离所有视图的孤立实体（噪声）不属于综合图，跳过以免拉大包围盒。
    noise_ids = {entity.entity_id for entity in noise}

    placements: dict[str, list[dict[str, Any]]] = defaultdict(list)
    labels: list[dict[str, Any]] = []
    sections: list[dict[str, Any]] = []
    texts: list[dict[str, Any]] = []
    outlines: list[dict[str, Any]] = []
    geometry: dict[str, list[dict[str, Any]]] = {
        "lines": [],
        "polylines": [],
        "arcs": [],
        "circles": [],
    }
    dimension_count = 0

    for entity in entities:
        if entity.entity_id in noise_ids:
            continue
        if entity.kind == "Insert":
            values = entity.attributes or {}
            position = _insert_position(entity)
            if position is None:
                continue
            raw = entity.raw or {}
            block = (
                raw.get("block")
                or raw.get("name")
                or entity.geometry.get("block")
                or entity.geometry.get("name")
                or ""
            )
            numbers = linkage._expand_number(values.get("编号") or "")
            spec = linkage._parse_spec(values.get("直径") or "")
            item = {
                "x": round(position[0], 3),
                "y": round(position[1], 3),
                "rotation": float(entity.geometry.get("rotation") or 0.0),
                "scale": float((entity.geometry.get("scale") or [1.0])[0] or 1.0),
                "block": block,
                "spec": spec.raw if spec else "",
            }
            if numbers:
                item["numbers"] = numbers
                labels.append(item)
                for number in numbers:
                    placements[number].append({**item, "handle": entity.handle})
            elif block in ("钢筋断面", "钢筋轮廓（双筋）", "钢筋断面（双筋）"):
                sections.append(item)
        elif entity.kind in ("Text", "MText"):
            texts.append(
                {
                    "text": entity.text,
                    "x": round(entity.bbox[0], 3),
                    "y": round(entity.bbox[1], 3),
                    "height": float(entity.text_height or 0.0),
                    "layer": entity.layer,
                }
            )
        elif entity.kind == "LwPolyline" and entity.geometry.get("vertices"):
            record = {
                "layer": entity.layer,
                "closed": bool(entity.geometry.get("closed")),
                "vertices": [
                    {"x": round(v["x"], 3), "y": round(v["y"], 3), "bulge": round(v["bulge"], 3)}
                    for v in _polyline_vertices(entity)
                ],
            }
            geometry["polylines"].append(record)
            width = entity.bbox[2] - entity.bbox[0]
            height = entity.bbox[3] - entity.bbox[1]
            if record["closed"] and max(width, height) >= 1500:
                outlines.append(record.copy())
        elif entity.kind == "Line":
            start = entity.geometry.get("start") or {}
            end = entity.geometry.get("end") or {}
            geometry["lines"].append(
                {
                    "layer": entity.layer,
                    "x1": round(float(start.get("x", 0.0)), 3),
                    "y1": round(float(start.get("y", 0.0)), 3),
                    "x2": round(float(end.get("x", 0.0)), 3),
                    "y2": round(float(end.get("y", 0.0)), 3),
                }
            )
        elif entity.kind == "Arc":
            center = entity.geometry.get("center") or {}
            geometry["arcs"].append(
                {
                    "layer": entity.layer,
                    "x": round(float(center.get("x", 0.0)), 3),
                    "y": round(float(center.get("y", 0.0)), 3),
                    "radius": round(float(entity.geometry.get("radius") or 0.0), 3),
                    "start_angle": round(float(entity.geometry.get("start_angle") or 0.0), 6),
                    "end_angle": round(float(entity.geometry.get("end_angle") or 0.0), 6),
                }
            )
        elif entity.kind == "Circle":
            center = entity.geometry.get("center") or {}
            geometry["circles"].append(
                {
                    "layer": entity.layer,
                    "x": round(float(center.get("x", 0.0)), 3),
                    "y": round(float(center.get("y", 0.0)), 3),
                    "radius": round(float(entity.geometry.get("radius") or 0.0), 3),
                }
            )
        elif entity.kind == "Dimension":
            dimension_count += 1

    view_summaries = [
        {
            "id": view.id,
            "title": view.title,
            "kind": view.kind,
            "bbox": [round(value, 1) for value in view.bbox],
            "entity_count": view.entity_count,
        }
        for view in views
    ]

    def view_for(x: float, y: float) -> str:
        containing = [view for view in views if _rect_distance(view.bbox, x, y) == 0]
        if containing:
            return min(containing, key=lambda view: (view.bbox[2] - view.bbox[0]) * (view.bbox[3] - view.bbox[1])).id
        nearest = min(views, key=lambda view: _rect_distance(view.bbox, x, y))
        return nearest.id

    for number, items in placements.items():
        for item in items:
            item["view"] = view_for(item["x"], item["y"])

    return {
        "views": view_summaries,
        "placements": dict(placements),
        "labels": labels,
        "sections": sections,
        "texts": texts,
        "outlines": outlines,
        "components": extract_components(geometry["polylines"]),
        "geometry": geometry,
        "dimension_count": dimension_count,
        "excluded_noise": len(noise_ids),
        "parameters": {"padding": padding, "merge_padding": merge_padding},
    }


def extract_table_layout(table_path: Path) -> dict[str, Any]:
    payload = json.loads(table_path.read_text(encoding="utf-8"))
    objects = linkage._objects(payload)
    frame = None
    for obj in objects:
        if obj.get("type") != "LwPolyline":
            continue
        geometry = obj.get("geometry") or {}
        if not geometry.get("closed"):
            continue
        vertices = geometry.get("vertices") or []
        if len(vertices) != 4:
            continue
        xs = [float(v.get("x", 0.0)) for v in vertices]
        ys = [float(v.get("y", 0.0)) for v in vertices]
        if max(xs) - min(xs) > 3000 and max(ys) - min(ys) > 3000:
            frame = (min(xs), min(ys), max(xs), max(ys))
            break
    if frame is None:
        raise ValueError("数量表里没有找到闭合表框")

    lines = []
    for obj in objects:
        if obj.get("type") != "Line":
            continue
        start = (obj.get("geometry") or {}).get("start") or {}
        end = (obj.get("geometry") or {}).get("end") or {}
        lines.append(
            (
                float(start.get("x", 0.0)),
                float(start.get("y", 0.0)),
                float(end.get("x", 0.0)),
                float(end.get("y", 0.0)),
            )
        )

    table = linkage.extract_schedule(linkage.load_regions([table_path])[0])

    def local(value: float, base: float) -> float:
        return round(value - base, 3)

    verticals = sorted(
        {
            local((x1 + x2) / 2.0, frame[0])
            for x1, y1, x2, y2 in lines
            if abs(x1 - x2) <= 1.0 and abs(y2 - y1) > 100
        }
    )
    horizontals = sorted(
        {
            local((y1 + y2) / 2.0, frame[1])
            for x1, y1, x2, y2 in lines
            if abs(y1 - y2) <= 1.0 and abs(x2 - x1) > 100
        }
    )

    header_labels: list[dict[str, Any]] = []
    summary_labels: list[dict[str, Any]] = []
    for obj in objects:
        if obj.get("type") not in ("Text", "MText"):
            continue
        point = linkage._xy(obj)
        if point is None:
            continue
        text = linkage._clean(str(obj.get("text") or ""))
        if not text:
            continue
        height = float((obj.get("geometry") or {}).get("height") or 0.0)
        item = {
            "value": text,
            "x": local(point[0], frame[0]),
            "y": local(point[1], frame[1]),
            "height": height,
            "type": obj.get("type"),
        }
        if height >= 200 or item["y"] < 700:
            summary_labels.append(item)
        elif item["y"] >= 5000:
            header_labels.append(item)

    bottom_lines = sorted(value for value in horizontals if value <= 5100)
    row_step = 0.0
    first_row_y = 0.0
    header_bottom = 0.0
    if len(bottom_lines) >= 3:
        diffs = [
            round(bottom_lines[index + 1] - bottom_lines[index], 3)
            for index in range(len(bottom_lines) - 1)
        ]
        # 取中位差，避开底部合计区的第一道不同间距线。
        unique = sorted(diffs)
        row_step = unique[len(unique) // 2]
        header_bottom = bottom_lines[-1]
        first_row_y = round(header_bottom - row_step / 2.0, 3)

    return {
        "frame": [round(value, 3) for value in frame],
        "width": round(frame[2] - frame[0], 3),
        "height": round(frame[3] - frame[1], 3),
        "verticals": verticals,
        "horizontals": horizontals,
        "header_labels": header_labels,
        "summary_labels": summary_labels,
        "title": table.title,
        "height_value": table.height,
        "rows": table.rows,
        "columns": {key: value for key, value in table.columns.items()},
        "columns_local": {
            key: round(value - frame[0], 3) for key, value in table.columns.items()
        },
        "row_step": row_step,
        "first_row_y": first_row_y,
        "header_bottom": round(header_bottom, 3),
    }


def build_model(paths: list[Path], report: bool) -> dict[str, Any]:
    regions = linkage.load_regions(paths)
    by_role = {region.role: region for region in regions}
    for role in ("composite", "detail", "schedule"):
        if role not in by_role:
            raise ValueError(f"缺少 {role} 区域")

    detail_entities, _ = classify.load_inputs([by_role["detail"].path])
    templates, template_warnings = extract_detail_templates(detail_entities)
    composite = extract_composite(by_role["composite"].path)
    table = extract_table_layout(by_role["schedule"].path)

    linkage_report = linkage.build_report(regions)
    schedule_rows = {
        row["number"]: row for row in linkage_report["rows"]
    }
    warnings = list(template_warnings) + list(linkage_report.get("warnings", []))

    for number, template in templates.items():
        row = schedule_rows.get(number)
        if row is None:
            warnings.append(
                {"kind": "missing_schedule_row", "number": number, "message": "大样模板在数量表里没有对应行"}
            )
            continue
        template["true_length"] = row["evaluated_single_length"]
        template["count"] = row["schedule"].get("count")
        template["diameter"] = template["diameter"] or row["detail_diameter"]

    for number, row in schedule_rows.items():
        if number not in templates:
            warnings.append(
                {"kind": "missing_detail_template", "number": number, "message": "数量表编号没有提取到大样模板"}
            )

    model = {
        "schema_version": "0.1",
        "source": [
            {
                "role": region.role,
                "path": str(region.path),
                "label": region.label,
                "source": region.source,
            }
            for region in regions
        ],
        "parameters": {
            "pier_height": table["height_value"],
            "table": {
                "origin": table["frame"][:2],
                "width": table["width"],
                "height": table["height"],
                "verticals": table["verticals"],
                "horizontals": table["horizontals"],
                "columns": table["columns_local"],
                "row_step": table["row_step"],
                "first_row_y": table["first_row_y"],
                "header_labels": table["header_labels"],
                "summary_labels": table["summary_labels"],
                "title": table["title"],
            },
            "composite": composite["parameters"],
        },
        "composite_views": composite["views"],
        "detail_templates": templates,
        "placements": composite["placements"],
        "composite_labels": composite["labels"],
        "composite_sections": composite["sections"],
        "composite_texts": composite["texts"],
        "composite_outlines": composite["outlines"],
        "components": composite["components"],
        "composite_geometry": composite["geometry"],
        "composite_excluded_noise": composite["excluded_noise"],
        "schedule_rows": [
            {
                "number": row["number"],
                "diameter": row["detail_diameter"],
                "single_length": row["schedule"].get("single_length"),
                "count": row["schedule"].get("count"),
                "total_length": row["schedule"].get("total_length"),
                "unit_weight": row["schedule"].get("unit_weight"),
                "total_weight": row["schedule"].get("total_weight"),
            }
            for row in linkage_report["rows"]
        ],
        "linkage": {
            "height": linkage_report["height"],
            "counts": linkage_report["counts"],
            "number_sets": linkage_report["number_sets"],
            "mismatches": linkage_report["mismatches"],
        },
        "warnings": warnings,
    }

    if report:
        _print_report(model)
    return model


def _print_report(model: dict[str, Any]) -> None:
    print("语义模型提取")
    print("=" * 96)
    parameters = model["parameters"]
    print(f"墩高 H={parameters['pier_height']}；数量表 {parameters['table']['width']:.0f}×{parameters['table']['height']:.0f}")
    print(
        f"综合图视图：{len(model['composite_views'])} 个；"
        f"大样模板：{len(model['detail_templates'])} 个；"
        f"放置：{sum(len(v) for v in model['placements'].values())} 处；"
        f"参数化构件：{len(model.get('components', []))} 个；"
        f"数量表：{len(model['schedule_rows'])} 行"
    )
    print("-" * 96)
    for component in model.get("components", []):
        print(
            f"构件 {component['type']:<16} {component['width']:.0f}×{component['height']:.0f} "
            f"R={component['radius']:.0f} 槽宽={component['channel_span']:.0f} "
            f"拟合误差={component['fit_error']:.4f}"
        )
    if model.get("composite_excluded_noise"):
        print(f"综合图排除孤立噪声实体：{model['composite_excluded_noise']} 个")
    print("-" * 96)
    for number, template in model["detail_templates"].items():
        vertices = len(template["vertices"])
        print(
            f"{number:<8} Φ{template['diameter']:<4} {template['spec']:<22} "
            f"L={template['true_length']} 顶点={vertices} 置信={template['confidence']}"
        )
    print("-" * 96)
    warnings = model["warnings"]
    if warnings:
        print(f"提示 {len(warnings)} 条：")
        for item in warnings:
            print(f"  [{item['kind']}] {item.get('number')}: {item['message']}")
    else:
        print("没有提示。")


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
    except (AttributeError, ValueError):
        pass

    parser = argparse.ArgumentParser(description="提取桥墩钢筋图三区的语义模型。")
    parser.add_argument("inputs", nargs="+", help="综合图 / 大样 / 数量表三个 JSON。")
    parser.add_argument("-o", "--output", default="pier_body_model.json")
    parser.add_argument("--report", action="store_true")
    parser.add_argument("--strict", action="store_true", help="存在提取提示时返回非零。")
    args = parser.parse_args(argv)

    paths = [Path(item) for item in args.inputs]
    missing = [str(path) for path in paths if not path.exists()]
    if missing:
        print("找不到输入文件: " + ", ".join(missing), file=sys.stderr)
        return 2
    try:
        model = build_model(paths, args.report)
    except (ValueError, OSError, json.JSONDecodeError) as exc:
        print(f"提取失败: {exc}", file=sys.stderr)
        return 1

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(model, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"已写出 {output.resolve()}")
    if args.strict and model["warnings"]:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
