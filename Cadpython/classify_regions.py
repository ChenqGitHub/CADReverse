"""把 CAD 图纸导出 JSON 切分成「大样区 / 综合图区 / 数量表区」并打分。

输入支持两种格式：

- compact 导出：``objects`` 数组（``type`` / ``geometry`` / ``text``）；
- region 导出：``entities`` 数组（``object_name`` / ``geometry`` / ``bounding_box``），
  即 ``region_export.py`` 的输出。

处理流程：

1. 归一化实体：类型、图层、文字、几何包围盒（多段线 bulge 按真实圆弧展开）；
2. 按「图面空白」聚类：包围盒按 ``padding`` 膨胀后做并查集，得到候选视图块；
3. 对每个视图块打分：
   - ``detail``    钢筋大样区：编号多、尺寸多、文字密度高、小轮廓重复；
   - ``schedule``  数量表区：闭合表框 + 规则行列 + 单元格文字；
   - ``composite`` 综合图区：构件轮廓 + 标注 + 钢筋放置，面积大、文字密度低；
   - 标题文字（如「钢筋大样图」「墩身钢筋数量表」「侧面钢筋布置图」）是最高权重先验；
4. 输出区域清单，保留每个源实体的 handle，供下游生成与三区对账追溯。

设计约束：本脚本只做分类，不生成 C#；坐标单位与导出文件一致（compact 为绝对坐标，
region 为相对 ``source.selection_origin`` 的坐标），分类不依赖两者差异。
"""

from __future__ import annotations

import argparse
import json
import math
import re
import statistics
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable

TAU = math.pi * 2.0

# 标题关键字 → 区域类型。顺序即优先级：数量表 > 大样 > 综合图。
_KEYWORD_RULES: list[tuple[str, re.Pattern[str]]] = [
    ("schedule", re.compile(r"数量表|工程数量表|钢筋表|材料表|明细表|汇总表")),
    ("detail", re.compile(r"钢筋大样|大样图|大样|钢筋形状|配料")),
    (
        "composite",
        re.compile(
            r"布置图|构造图|立面图|侧面图|剖面图|断面图|配筋图|构造|布置|截面|剖面|断面|示意"
        ),
    ),
]

# 钢筋编号：N0、N18、N1-1、N2-3 等。
_NUMBER_PATTERN = re.compile(r"^N\d+(?:-\d+)?$")

# 标注类图层关键字（用于引线识别）。
_ANNOTATION_LAYER_HINTS = ("标注", "尺寸", "B-03", "B-07")

# 粗实线 / 细实线图层关键字（用于构件轮廓识别）。
_OUTLINE_LAYER_HINTS = ("粗实线", "细实线", "B-01", "B-04")

REGION_KIND_LABELS = {
    "detail": "钢筋大样区",
    "composite": "综合图区",
    "schedule": "数量表区",
    "unknown": "未识别",
}


# --------------------------------------------------------------------------
# 基础几何
# --------------------------------------------------------------------------


def _num(value: Any) -> float | None:
    if isinstance(value, bool):
        return None
    try:
        number = float(value)
    except (TypeError, ValueError):
        return None
    return number if math.isfinite(number) else None


def _xy(value: Any) -> tuple[float, float] | None:
    """解析一个二维点：``[x, y]``、``[x, y, z]`` 或 ``{"x": .., "y": ..}``。"""
    if isinstance(value, dict):
        try:
            return float(value["x"]), float(value["y"])
        except (KeyError, TypeError, ValueError):
            return None
    if isinstance(value, (list, tuple)) and len(value) >= 2:
        try:
            return float(value[0]), float(value[1])
        except (TypeError, ValueError):
            return None
    return None


def _xy_list(value: Any) -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []
    if not isinstance(value, (list, tuple)):
        return points
    for item in value:
        point = _xy(item)
        if point is not None:
            points.append(point)
    return points


def _bbox_of_points(points: Iterable[tuple[float, float]]) -> tuple[float, float, float, float] | None:
    xs: list[float] = []
    ys: list[float] = []
    for x, y in points:
        xs.append(x)
        ys.append(y)
    if not xs:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def _is_wide_char(char: str) -> bool:
    return ord(char) > 0x2E7F


def _text_width(text: str, height: float) -> float:
    if height <= 0:
        return 0.0
    width = sum(height if _is_wide_char(char) else height * 0.6 for char in text)
    return width or height


def _distance(a: tuple[float, float], b: tuple[float, float]) -> float:
    return math.hypot(b[0] - a[0], b[1] - a[1])


def _arc_cardinal_points(
    center: tuple[float, float],
    radius: float,
    start_angle: float,
    sweep: float,
) -> list[tuple[float, float]]:
    """圆弧包围盒用的采样点：两端点 + 落在扫掠范围内的四个象限点。"""
    cx, cy = center
    points = [
        (cx + radius * math.cos(start_angle), cy + radius * math.sin(start_angle)),
        (cx + radius * math.cos(start_angle + sweep), cy + radius * math.sin(start_angle + sweep)),
    ]
    for quarter in range(4):
        angle = quarter * math.pi / 2.0
        if sweep >= 0:
            delta = (angle - start_angle) % TAU
        else:
            delta = (start_angle - angle) % TAU
        if delta <= abs(sweep) + 1e-9:
            points.append((cx + radius * math.cos(angle), cy + radius * math.sin(angle)))
    return points


def _polyline_vertices(geometry: dict[str, Any]) -> list[tuple[float, float, float]]:
    """返回 ``[(x, y, bulge), ...]``，兼容两种导出格式的顶点写法。"""
    raw = geometry.get("vertices")
    if raw is None:
        raw = geometry.get("points")
    vertices: list[tuple[float, float, float]] = []
    if not isinstance(raw, (list, tuple)):
        return vertices
    for item in raw:
        point = _xy(item.get("point")) if isinstance(item, dict) and "point" in item else _xy(item)
        if point is None:
            continue
        bulge = 0.0
        if isinstance(item, dict):
            value = _num(item.get("bulge"))
            if value is not None:
                bulge = value
        vertices.append((point[0], point[1], bulge))
    return vertices


def _polyline_bbox(
    vertices: list[tuple[float, float, float]],
    closed: bool,
) -> tuple[float, float, float, float] | None:
    if not vertices:
        return None
    points = [(x, y) for x, y, _ in vertices]
    if not closed:
        segments = len(vertices) - 1
    else:
        segments = len(vertices)
    for index in range(segments):
        x1, y1, bulge = vertices[index]
        x2, y2, _ = vertices[(index + 1) % len(vertices)]
        if abs(bulge) < 1e-12:
            continue
        chord = math.hypot(x2 - x1, y2 - y1)
        if chord <= 1e-12:
            continue
        theta = 4.0 * math.atan(bulge)
        half = theta / 2.0
        sin_half = math.sin(half)
        if abs(sin_half) < 1e-12:
            continue
        radius = chord / (2.0 * sin_half)
        if not math.isfinite(radius) or abs(radius) < 1e-12:
            continue
        ux, uy = (x2 - x1) / chord, (y2 - y1) / chord
        nx, ny = -uy, ux
        offset = radius * math.cos(half)
        center = ((x1 + x2) / 2.0 + nx * offset, (y1 + y2) / 2.0 + ny * offset)
        start_angle = math.atan2(y1 - center[1], x1 - center[0])
        points.extend(_arc_cardinal_points(center, abs(radius), start_angle, theta))
    return _bbox_of_points(points)


def _point_like_values(geometry: dict[str, Any]) -> list[tuple[float, float]]:
    """兜底：几何里所有能解析成点的值（用于标注等没有统一字段的类型）。"""
    points: list[tuple[float, float]] = []
    for value in geometry.values():
        point = _xy(value)
        if point is not None:
            points.append(point)
            continue
        if isinstance(value, (list, tuple)):
            points.extend(_xy_list(value))
    return points


def _entity_bbox(
    kind: str,
    geometry: dict[str, Any],
    text: str,
    text_height: float | None,
) -> tuple[float, float, float, float] | None:
    """计算实体包围盒；未知类型回退到几何中的离散点。"""
    points: list[tuple[float, float]] = []

    if kind in {"Line", "AcDbLine"}:
        for key in ("start", "end"):
            point = _xy(geometry.get(key))
            if point is not None:
                points.append(point)
    elif kind in {"LwPolyline", "Lwpolyline", "AcDbPolyline", "AcDb2dPolyline", "AcDb3dPolyline"}:
        box = _polyline_bbox(_polyline_vertices(geometry), bool(geometry.get("closed")))
        return box
    elif kind in {"Circle", "AcDbCircle"}:
        center = _xy(geometry.get("center"))
        radius = _num(geometry.get("radius"))
        if center is not None and radius is not None:
            return (center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius)
        return None
    elif kind in {"Arc", "AcDbArc"}:
        center = _xy(geometry.get("center"))
        radius = _num(geometry.get("radius"))
        start = _num(geometry.get("start_angle"))
        end = _num(geometry.get("end_angle"))
        if center is not None and radius is not None and start is not None and end is not None:
            sweep = (end - start) % TAU
            if sweep <= 1e-12:
                sweep = TAU
            return _bbox_of_points(_arc_cardinal_points(center, abs(radius), start, sweep))
        return None
    elif kind in {"Text", "MText", "AcDbText", "AcDbMText"}:
        point = _xy(geometry.get("position"))
        if point is None:
            point = _xy(geometry.get("insertion_point"))
        height = text_height or 0.0
        if point is not None:
            width = _text_width(text, height)
            return (point[0], point[1], point[0] + width, point[1] + height)
        return None
    elif "dimension" in kind.lower():
        points = _point_like_values(geometry)
    elif kind in {"Insert", "AcDbBlockReference"}:
        point = _xy(geometry.get("position"))
        if point is None:
            point = _xy(geometry.get("insertion_point"))
        if point is not None:
            points.append(point)
        for attribute in geometry.get("attributes") or []:
            if not isinstance(attribute, dict):
                continue
            anchor = _xy(attribute.get("alignment_point"))
            if anchor is not None:
                points.append(anchor)
    elif kind in {"Solid", "AcDbSolid"}:
        points = _xy_list(geometry.get("points"))
    elif kind in {"Hatch", "AcDbHatch"}:
        hatch = geometry.get("hatch") or {}
        points = _xy_list(hatch.get("seed_points"))
    else:
        points = _point_like_values(geometry)

    if not points:
        points = _point_like_values(geometry)
    return _bbox_of_points(points)


# --------------------------------------------------------------------------
# 实体归一化
# --------------------------------------------------------------------------


@dataclass
class Entity:
    handle: str
    kind: str
    layer: str
    text: str
    text_height: float | None
    geometry: dict[str, Any]
    bbox: tuple[float, float, float, float] | None
    raw: dict[str, Any]
    source_file: str
    source_label: str
    entity_id: Any
    attributes: dict[str, str] = field(default_factory=dict)
    measurement: float | None = None

    @property
    def annotation_like(self) -> bool:
        return any(hint in self.layer for hint in _ANNOTATION_LAYER_HINTS)

    @property
    def outline_like(self) -> bool:
        return any(hint in self.layer for hint in _OUTLINE_LAYER_HINTS)

    @property
    def attribute_numbers(self) -> list[str]:
        """块属性里的钢筋编号（如 ``编号`` = ``N1-1``）。"""
        return [value for value in self.attributes.values() if _NUMBER_PATTERN.match(value.strip())]


def _text_of(raw: dict[str, Any], geometry: dict[str, Any]) -> str:
    for candidate in (raw.get("text"), geometry.get("text")):
        if isinstance(candidate, str):
            return candidate
    return ""


def _attribute_items(raw: dict[str, Any], geometry: dict[str, Any]) -> list[dict[str, Any]]:
    """块属性在 compact 导出里位于对象顶层，在 region 导出里位于 geometry 内。"""
    for candidate in (geometry.get("attributes"), raw.get("attributes")):
        if isinstance(candidate, (list, tuple)):
            return [item for item in candidate if isinstance(item, dict)]
    return []


def _attributes_of(items: list[dict[str, Any]]) -> dict[str, str]:
    """读取块属性：compact 导出用 ``value``，region 导出用 ``text``。"""
    attributes: dict[str, str] = {}
    for item in items:
        tag = item.get("tag")
        value = item.get("value", item.get("text"))
        if isinstance(tag, str) and isinstance(value, str) and value.strip():
            attributes[tag] = value.strip()
    return attributes


def _iter_export_objects(data: dict[str, Any]) -> tuple[list[dict[str, Any]], str]:
    if isinstance(data.get("objects"), list):
        return data["objects"], "compact"
    if isinstance(data.get("entities"), list):
        return data["entities"], "region"
    return [], "unknown"


def _normalize_entity(
    raw: dict[str, Any],
    form: str,
    index: int,
    source_file: str,
    source_label: str,
) -> Entity:
    if form == "compact":
        kind = str(raw.get("type") or "Unknown")
        geometry = raw.get("geometry") if isinstance(raw.get("geometry"), dict) else {}
        layer = str(raw.get("layer") or "")
    else:
        kind = str(raw.get("object_name") or "Unknown")
        geometry = raw.get("geometry") if isinstance(raw.get("geometry"), dict) else {}
        layer = str(raw.get("layer") or "")

    text_height = _num(geometry.get("height"))
    text = _text_of(raw, geometry)

    bbox = None
    bounding = raw.get("bounding_box")
    if isinstance(bounding, dict):
        minimum = _xy(bounding.get("min"))
        maximum = _xy(bounding.get("max"))
        if minimum is not None and maximum is not None:
            bbox = (
                min(minimum[0], maximum[0]),
                min(minimum[1], maximum[1]),
                max(minimum[0], maximum[0]),
                max(minimum[1], maximum[1]),
            )
    if bbox is None:
        attribute_items = _attribute_items(raw, geometry)
        bbox_geometry = geometry
        if attribute_items and "attributes" not in geometry:
            bbox_geometry = {**geometry, "attributes": attribute_items}
        bbox = _entity_bbox(kind, bbox_geometry, text, text_height)

    handle_value = raw.get("handle")
    return Entity(
        handle=str(handle_value) if handle_value is not None else f"#{index}",
        kind=kind,
        layer=layer,
        text=text,
        text_height=text_height,
        geometry=geometry,
        bbox=bbox,
        raw=raw,
        source_file=source_file,
        source_label=source_label,
        entity_id=raw.get("id", index),
        attributes=_attributes_of(_attribute_items(raw, geometry)),
        measurement=_num(geometry.get("measurement")),
    )


# --------------------------------------------------------------------------
# 空白聚类
# --------------------------------------------------------------------------


class _UnionFind:
    def __init__(self, size: int) -> None:
        self._parent = list(range(size))
        self._rank = [0] * size

    def find(self, item: int) -> int:
        parent = self._parent
        while parent[item] != item:
            parent[item] = parent[parent[item]]
            item = parent[item]
        return item

    def union(self, left: int, right: int) -> None:
        root_left, root_right = self.find(left), self.find(right)
        if root_left == root_right:
            return
        if self._rank[root_left] < self._rank[root_right]:
            root_left, root_right = root_right, root_left
        self._parent[root_right] = root_left
        if self._rank[root_left] == self._rank[root_right]:
            self._rank[root_left] += 1


def _touches(
    left: tuple[float, float, float, float],
    right: tuple[float, float, float, float],
    padding: float,
) -> bool:
    return (
        left[0] - padding <= right[2]
        and right[0] - padding <= left[2]
        and left[1] - padding <= right[3]
        and right[1] - padding <= left[3]
    )


def cluster_entities(
    entities: list[Entity],
    padding: float,
) -> tuple[list[list[Entity]], list[Entity]]:
    """按图面空白把实体聚成视图块；返回 ``(clusters, 无几何实体)``。"""
    geometric = [entity for entity in entities if entity.bbox is not None]
    unassigned = [entity for entity in entities if entity.bbox is None]
    if not geometric:
        return [], unassigned

    union = _UnionFind(len(geometric))
    cell = max(padding * 2.0, 1e-6)
    buckets: dict[tuple[int, int], list[int]] = defaultdict(list)
    for index, entity in enumerate(geometric):
        assert entity.bbox is not None
        xmin, ymin, xmax, ymax = entity.bbox
        minimum_x = math.floor((xmin - padding) / cell)
        maximum_x = math.floor((xmax + padding) / cell)
        minimum_y = math.floor((ymin - padding) / cell)
        maximum_y = math.floor((ymax + padding) / cell)
        for grid_x in range(minimum_x, maximum_x + 1):
            for grid_y in range(minimum_y, maximum_y + 1):
                buckets[(grid_x, grid_y)].append(index)

    for members in buckets.values():
        for position, left_index in enumerate(members):
            left_box = geometric[left_index].bbox
            assert left_box is not None
            for right_index in members[position + 1 :]:
                right_box = geometric[right_index].bbox
                assert right_box is not None
                if _touches(left_box, right_box, padding):
                    union.union(left_index, right_index)

    groups: dict[int, list[Entity]] = defaultdict(list)
    for index, entity in enumerate(geometric):
        groups[union.find(index)].append(entity)
    clusters = list(groups.values())
    clusters.sort(key=lambda group: -len(group))
    return clusters, unassigned


# --------------------------------------------------------------------------
# 区域特征与打分
# --------------------------------------------------------------------------


@dataclass
class Region:
    entities: list[Entity]
    bbox: tuple[float, float, float, float]
    id: str = ""
    kind: str = "unknown"
    title: str = ""
    dominant_label: str = ""
    scores: dict[str, float] = field(default_factory=dict)
    evidence: dict[str, Any] = field(default_factory=dict)
    hints: list[dict[str, Any]] = field(default_factory=list)
    numbers: list[str] = field(default_factory=list)
    member_ids: list[str] = field(default_factory=list)

    @property
    def entity_count(self) -> int:
        return len(self.entities)

    @property
    def area(self) -> float:
        return max(0.0, self.bbox[2] - self.bbox[0]) * max(0.0, self.bbox[3] - self.bbox[1])

    @property
    def handle_list(self) -> list[str]:
        return [entity.handle for entity in self.entities]


def _cluster_values(values: Iterable[float], tolerance: float) -> list[float]:
    ordered = sorted(values)
    groups: list[list[float]] = []
    for value in ordered:
        if groups and abs(value - groups[-1][-1]) <= tolerance:
            groups[-1].append(value)
        else:
            groups.append([value])
    return [statistics.fmean(group) for group in groups]


def _is_regular(values: Iterable[float], tolerance: float) -> bool:
    unique = _cluster_values(values, tolerance)
    if len(unique) < 3:
        return False
    gaps = [right - left for left, right in zip(unique, unique[1:])]
    mean = statistics.fmean(gaps)
    if mean <= tolerance:
        return False
    return statistics.pstdev(gaps) / mean <= 0.3


def _axis_rect(
    entity: Entity,
) -> tuple[float, float, float, float] | None:
    """识别轴对齐闭合矩形多段线（表格外框）。"""
    vertices = _polyline_vertices(entity.geometry)
    if len(vertices) < 4:
        return None
    if any(abs(bulge) > 1e-9 for _, _, bulge in vertices):
        return None
    closed = bool(entity.geometry.get("closed"))
    points = [(x, y) for x, y, _ in vertices]
    if len(points) >= 5 and _distance(points[0], points[-1]) <= 1e-6 * max(
        1.0, abs(points[0][0]), abs(points[0][1])
    ):
        points = points[:-1]
    if not closed and len(points) != 4:
        return None
    if len(points) != 4:
        return None
    xs = [point[0] for point in points]
    ys = [point[1] for point in points]
    xmin, xmax = min(xs), max(xs)
    ymin, ymax = min(ys), max(ys)
    width, height = xmax - xmin, ymax - ymin
    if width <= 0 or height <= 0:
        return None
    tolerance = 0.02 * max(width, height)
    corners = [(xmin, ymin), (xmax, ymin), (xmax, ymax), (xmin, ymax)]
    used: set[int] = set()
    for point in points:
        for index, corner in enumerate(corners):
            if abs(point[0] - corner[0]) <= tolerance and abs(point[1] - corner[1]) <= tolerance:
                used.add(index)
                break
        else:
            return None
    if len(used) != 4:
        return None
    return xmin, ymin, xmax, ymax


def _inside(point: tuple[float, float], frame: tuple[float, float, float, float], margin: float) -> bool:
    return (
        frame[0] - margin <= point[0] <= frame[2] + margin
        and frame[1] - margin <= point[1] <= frame[3] + margin
    )


def _frame_lines(
    region: Region,
    frame: tuple[float, float, float, float],
) -> tuple[list[tuple[float, float, float]], list[tuple[float, float, float]]]:
    """返回表框内的水平线 ``(x1, x2, y)`` 与竖直线 ``(y1, y2, x)``。"""
    width = frame[2] - frame[0]
    height = frame[3] - frame[1]
    margin = 0.02 * max(width, height)
    tolerance = 1e-4 * max(width, height)
    horizontal: list[tuple[float, float, float]] = []
    vertical: list[tuple[float, float, float]] = []
    for entity in region.entities:
        if entity.kind not in {"Line", "AcDbLine"}:
            continue
        start = _xy(entity.geometry.get("start"))
        end = _xy(entity.geometry.get("end"))
        if start is None or end is None:
            continue
        if not (_inside(start, frame, margin) and _inside(end, frame, margin)):
            continue
        if abs(start[1] - end[1]) <= tolerance:
            horizontal.append((min(start[0], end[0]), max(start[0], end[0]), start[1]))
        elif abs(start[0] - end[0]) <= tolerance:
            vertical.append((min(start[1], end[1]), max(start[1], end[1]), start[0]))
    return horizontal, vertical


def _text_grid(region: Region, tolerance: float) -> tuple[int, int]:
    positions_x: list[float] = []
    positions_y: list[float] = []
    for entity in region.entities:
        if entity.kind not in {"Text", "MText", "AcDbText", "AcDbMText"}:
            continue
        if not entity.text.strip():
            continue
        point = _xy(entity.geometry.get("position"))
        if point is None:
            point = _xy(entity.geometry.get("insertion_point"))
        if point is None:
            continue
        positions_x.append(point[0])
        positions_y.append(point[1])
    return len(_cluster_values(positions_x, tolerance)), len(_cluster_values(positions_y, tolerance))


def _grid_evidence(region: Region, median_height: float) -> dict[str, Any]:
    """表格证据：闭合表框 + 内部规则横竖线 + 单元格文字网格。"""
    frames: list[tuple[float, tuple[float, float, float, float], Entity]] = []
    for entity in region.entities:
        if entity.kind not in {"LwPolyline", "Lwpolyline", "AcDbPolyline"}:
            continue
        box = _axis_rect(entity)
        if box is None:
            continue
        area = (box[2] - box[0]) * (box[3] - box[1])
        if area <= 0:
            continue
        frames.append((area, box, entity))
    if not frames:
        return {"score": 0.0, "frame": None, "horizontal": 0, "vertical": 0, "text_grid": (0, 0)}

    frames.sort(key=lambda item: -item[0])
    frame_area, frame, _ = frames[0]
    horizontal, vertical = _frame_lines(region, frame)
    tolerance = max(median_height * 0.5, 1.0)
    rows = len(_cluster_values([line[2] for line in horizontal], tolerance))
    columns = len(_cluster_values([line[2] for line in vertical], tolerance))
    regular_rows = _is_regular([line[2] for line in horizontal], tolerance)
    regular_columns = _is_regular([line[2] for line in vertical], tolerance)
    text_columns, text_rows = _text_grid(region, tolerance)

    score = 0.15
    if rows >= 3 and regular_rows:
        score += 0.3
    if columns >= 3 and regular_columns:
        score += 0.3
    if text_columns >= 3 and text_rows >= 3:
        score += 0.15
    if region.area > 0 and frame_area / region.area > 0.6:
        score += 0.1
    return {
        "score": min(score, 1.0),
        "frame": frame,
        "horizontal": len(horizontal),
        "vertical": len(vertical),
        "rows": rows,
        "columns": columns,
        "regular_rows": regular_rows,
        "regular_columns": regular_columns,
        "text_grid": (text_columns, text_rows),
    }


def _detail_evidence(region: Region, median_height: float) -> dict[str, Any]:
    """大样证据：编号比例、尺寸比例、文字密度、小轮廓重复、引线密度。"""
    texts = [
        entity
        for entity in region.entities
        if entity.kind in {"Text", "MText", "AcDbText", "AcDbMText"} and entity.text.strip()
    ]
    numbers = [entity for entity in texts if _NUMBER_PATTERN.match(entity.text.strip())]
    attribute_numbers = [
        value for entity in region.entities for value in entity.attribute_numbers
    ]
    dimensions = [entity for entity in region.entities if "dimension" in entity.kind.lower()]
    lines = [entity for entity in region.entities if entity.kind in {"Line", "AcDbLine"}]

    leaders = 0
    for entity in lines:
        if not entity.annotation_like or entity.bbox is None:
            continue
        length = _distance(
            (entity.bbox[0], entity.bbox[1]), (entity.bbox[2], entity.bbox[3])
        )
        if length <= 3.0 * median_height:
            leaders += 1

    signature_counts: Counter[tuple[int, int]] = Counter()
    for entity in region.entities:
        if entity.kind not in {"LwPolyline", "Lwpolyline", "AcDbPolyline"}:
            continue
        vertices = _polyline_vertices(entity.geometry)
        if not 2 <= len(vertices) <= 8:
            continue
        box = _polyline_bbox(vertices, bool(entity.geometry.get("closed")))
        if box is None:
            continue
        width = box[2] - box[0]
        height = box[3] - box[1]
        if width <= 0 or height <= 0 or max(width, height) > 20 * median_height:
            continue
        signature_counts[(len(vertices), round(width / max(height, 1e-9)), 1)] += 1
    repeated = sum(count for count in signature_counts.values() if count >= 3)

    text_ratio = len(texts) / max(1, len(region.entities))
    number_count = len(numbers) + len(attribute_numbers)
    number_ratio = number_count / max(1, len(texts))
    leader_ratio = leaders / max(1, len(lines))
    dimension_ratio = len(dimensions) / max(1, len(region.entities))
    score = (
        1.2 * min(1.0, text_ratio / 0.45)
        + 0.8 * min(1.0, number_ratio)
        + 0.4 * min(1.0, repeated / 6.0)
        + 0.3 * min(1.0, leader_ratio / 0.3)
        + 0.3 * min(1.0, dimension_ratio / 0.05)
    )
    return {
        "score": min(score, 2.5),
        "texts": len(texts),
        "numbers": number_count,
        "attribute_numbers": len(attribute_numbers),
        "dimensions": len(dimensions),
        "lines": len(lines),
        "leaders": leaders,
        "repeated_shapes": repeated,
        "text_ratio": round(text_ratio, 3),
        "number_ratio": round(number_ratio, 3),
    }


def _composite_evidence(region: Region, median_height: float, largest_area: float) -> dict[str, Any]:
    """综合图证据：构件长轮廓、标注数量、文字稀疏、面积占比。"""
    texts = [
        entity
        for entity in region.entities
        if entity.kind in {"Text", "MText", "AcDbText", "AcDbMText"} and entity.text.strip()
    ]
    dimensions = [entity for entity in region.entities if "dimension" in entity.kind.lower()]
    inserts = [
        entity for entity in region.entities if entity.kind in {"Insert", "AcDbBlockReference"}
    ]

    long_outlines = 0
    dots = 0
    for entity in region.entities:
        if entity.kind in {"Line", "AcDbLine"} and entity.bbox is not None:
            length = _distance(
                (entity.bbox[0], entity.bbox[1]), (entity.bbox[2], entity.bbox[3])
            )
            if entity.outline_like and length >= 8.0 * median_height:
                long_outlines += 1
        elif entity.kind in {"Circle", "AcDbCircle"}:
            radius = _num(entity.geometry.get("radius")) or 0.0
            if 0 < radius <= 2.0 * median_height:
                dots += 1

    text_ratio = len(texts) / max(1, len(region.entities))
    area_ratio = region.area / largest_area if largest_area > 0 else 0.0
    score = (
        1.0 * (1.0 - min(1.0, text_ratio / 0.25))
        + 0.5 * (1.0 if len(dimensions) >= 5 else 0.0)
        + 0.4 * min(1.0, long_outlines / 20.0)
        + 0.3 * min(1.0, len(inserts) / 20.0)
        + 0.3 * area_ratio
    )
    return {
        "score": min(score, 2.5),
        "texts": len(texts),
        "dimensions": len(dimensions),
        "inserts": len(inserts),
        "long_outlines": long_outlines,
        "steel_dots": dots,
        "text_ratio": round(text_ratio, 3),
        "area_ratio": round(area_ratio, 3),
    }


def _keyword_kind(text: str) -> str | None:
    for kind, pattern in _KEYWORD_RULES:
        if pattern.search(text):
            return kind
    return None


def _find_title(region: Region) -> tuple[str, str | None, Entity | None]:
    texts = [
        entity
        for entity in region.entities
        if entity.kind in {"Text", "MText", "AcDbText", "AcDbMText"} and entity.text.strip()
    ]
    if not texts:
        return "", None, None
    keyword_hits = [entity for entity in texts if _keyword_kind(entity.text)]
    candidates = keyword_hits or texts
    chosen = max(
        candidates,
        key=lambda entity: (entity.text_height or 0.0, len(entity.text), -len(entity.handle)),
    )
    return chosen.text.strip(), _keyword_kind(chosen.text), chosen


def _classify_region(region: Region, median_height: float, largest_area: float) -> None:
    grid = _grid_evidence(region, median_height)
    detail = _detail_evidence(region, median_height)
    composite = _composite_evidence(region, median_height, largest_area)
    title, title_kind, title_entity = _find_title(region)

    region.title = title
    region.scores = {
        "detail": round(detail["score"], 3),
        "schedule": round(grid["score"] * 2.0, 3),
        "composite": round(composite["score"], 3),
    }
    region.evidence = {
        "grid": grid,
        "detail": detail,
        "composite": composite,
        "title_kind": title_kind,
        "title_handle": title_entity.handle if title_entity is not None else None,
    }

    if title_kind:
        region.kind = title_kind
        region.scores[title_kind] = round(region.scores[title_kind] + 3.0, 3)
    else:
        best_kind = max(region.scores, key=region.scores.get)
        region.kind = best_kind if region.scores[best_kind] >= 1.0 else "unknown"
        region.hints.append(
            {"kind": "missing_title", "message": "区域内没有找到标题关键字，按几何特征判定。"}
        )

    # 综合图里可能嵌着一张表：报告表格候选框，供人工/后续步骤拆分。
    if region.kind == "composite" and grid.get("frame") is not None and grid["score"] >= 0.7:
        frame = grid["frame"]
        frame_area = (frame[2] - frame[0]) * (frame[3] - frame[1])
        ratio = frame_area / region.area if region.area > 0 else 0.0
        if 0.1 <= ratio <= 0.6:
            region.hints.append(
                {
                    "kind": "table_candidate",
                    "message": "综合图区内检测到闭合表框，可能是同区的钢筋数量表。",
                    "bbox": list(frame),
                    "area_ratio": round(ratio, 3),
                }
            )


def _region_bbox(entities: list[Entity]) -> tuple[float, float, float, float]:
    boxes = [entity.bbox for entity in entities if entity.bbox is not None]
    assert boxes
    return (
        min(box[0] for box in boxes),
        min(box[1] for box in boxes),
        max(box[2] for box in boxes),
        max(box[3] for box in boxes),
    )


def _region_numbers(region: Region) -> list[str]:
    """区域内出现过的钢筋编号（文字 + 块属性），按出现顺序去重。"""
    numbers: list[str] = []
    for entity in region.entities:
        candidates: list[str] = []
        if entity.text.strip():
            candidates.append(entity.text.strip())
        candidates.extend(entity.attribute_numbers)
        for value in candidates:
            if _NUMBER_PATTERN.match(value) and value not in numbers:
                numbers.append(value)
    return numbers


def _is_noise(region: Region, median_height: float) -> bool:
    """过滤图面上的孤立实体（单点块、单根引线），它们不构成视图。"""
    if region.entity_count > 2:
        return False
    if any(entity.text.strip() for entity in region.entities):
        return False
    if region.entity_count == 1:
        return True
    width = region.bbox[2] - region.bbox[0]
    height = region.bbox[3] - region.bbox[1]
    return max(width, height) <= 2.0 * median_height


def build_views(
    entities: list[Entity],
    padding: float,
) -> tuple[list[Region], list[Entity], list[Entity], float]:
    """第一级：图面空白聚类得到细粒度视图块，并逐个分类。"""
    clusters, unassigned = cluster_entities(entities, padding)
    heights = [entity.text_height for entity in entities if entity.text_height]
    median_height = statistics.median(heights) if heights else 250.0

    largest_area = 0.0
    for cluster in clusters:
        box = _region_bbox(cluster)
        largest_area = max(
            largest_area,
            max(0.0, box[2] - box[0]) * max(0.0, box[3] - box[1]),
        )

    views: list[Region] = []
    noise: list[Entity] = []
    for cluster in clusters:
        region = Region(entities=cluster, bbox=_region_bbox(cluster))
        if _is_noise(region, median_height):
            noise.extend(cluster)
            continue
        _classify_region(region, median_height, largest_area)
        labels = Counter(entity.source_label for entity in region.entities if entity.source_label)
        if labels:
            region.dominant_label = labels.most_common(1)[0][0]
        region.numbers = _region_numbers(region)
        views.append(region)

    views.sort(key=lambda view: (-round(view.bbox[3], 3), round(view.bbox[0], 3)))
    for index, view in enumerate(views, start=1):
        view.id = f"V{index}"
    return views, unassigned, noise, median_height


def _merge_groups(views: list[Region], merge_padding: float) -> list[list[Region]]:
    """第二级：同类型且相邻的视图块合并成一个区域；不同类型永不合并。"""
    if merge_padding <= 0 or not views:
        return [[view] for view in views]
    union = _UnionFind(len(views))
    for left in range(len(views)):
        for right in range(left + 1, len(views)):
            first, second = views[left], views[right]
            if first.kind != second.kind or first.kind == "unknown":
                continue
            if _touches(first.bbox, second.bbox, merge_padding):
                union.union(left, right)
    groups: dict[int, list[Region]] = defaultdict(list)
    for index, view in enumerate(views):
        groups[union.find(index)].append(view)
    return list(groups.values())


def _aggregate_section(group: list[Region], section: str) -> dict[str, Any]:
    """合并多个视图的同类证据：计数求和、分数与比例取最大。"""
    totals: dict[str, Any] = {}
    for view in group:
        for key, value in view.evidence.get(section, {}).items():
            if isinstance(value, bool) or value is None:
                totals.setdefault(key, value)
            elif isinstance(value, (int, float)):
                if key == "score" or key.endswith("_ratio"):
                    totals[key] = round(max(float(totals.get(key, 0.0)), float(value)), 3)
                else:
                    total = float(totals.get(key, 0.0)) + float(value)
                    totals[key] = int(total) if total.is_integer() else round(total, 3)
            else:
                totals.setdefault(key, value)
    return totals


def _unique_hints(hints: Iterable[dict[str, Any]]) -> list[dict[str, Any]]:
    seen: set[tuple[str, str]] = set()
    result: list[dict[str, Any]] = []
    for hint in hints:
        key = (str(hint.get("kind")), str(hint.get("message")))
        if key in seen:
            continue
        seen.add(key)
        result.append(hint)
    return result


def _compose_region(group: list[Region]) -> Region:
    """合并后的区域：实体取并集，类型取主视图，证据按规则聚合。"""
    entities = [entity for view in group for entity in view.entities]
    main = max(group, key=lambda view: view.entity_count)
    titled = [
        view for view in group if view.title and view.evidence.get("title_kind")
    ]
    title_source = max(titled, key=lambda view: view.entity_count) if titled else main
    region = Region(entities=entities, bbox=_region_bbox(entities), kind=main.kind)
    region.title = title_source.title
    region.scores = dict(main.scores)
    region.evidence = {
        "grid": _aggregate_section(group, "grid"),
        "detail": _aggregate_section(group, "detail"),
        "composite": _aggregate_section(group, "composite"),
        "title_kind": title_source.evidence.get("title_kind"),
        "title_view": title_source.id,
        "titles": [view.title for view in group if view.title],
        "view_count": len(group),
    }
    region.hints = _unique_hints(hint for view in group for hint in view.hints)
    region.member_ids = [view.id for view in group]
    region.numbers = _region_numbers(region)
    labels = Counter(entity.source_label for entity in entities if entity.source_label)
    if labels:
        region.dominant_label = labels.most_common(1)[0][0]
    return region


def build_regions(
    entities: list[Entity],
    padding: float,
    merge_padding: float = 0.0,
) -> tuple[list[Region], list[Region], list[Entity], list[Entity]]:
    """返回 ``(区域, 视图块, 无几何实体, 孤立实体)``。"""
    views, unassigned, noise, _ = build_views(entities, padding)
    groups = _merge_groups(views, merge_padding)
    regions = [_compose_region(group) for group in groups]
    regions.sort(key=lambda region: (-round(region.bbox[3], 3), round(region.bbox[0], 3)))
    for index, region in enumerate(regions, start=1):
        region.id = f"R{index}"
    return regions, views, unassigned, noise


def auto_padding(entities: list[Entity], scale: float = 4.0) -> float:
    heights = [entity.text_height for entity in entities if entity.text_height]
    median = statistics.median(heights) if heights else 250.0
    return max(50.0, median * scale)


def auto_merge_padding(entities: list[Entity], scale: float = 12.0) -> float:
    heights = [entity.text_height for entity in entities if entity.text_height]
    median = statistics.median(heights) if heights else 250.0
    return max(100.0, median * scale)


# --------------------------------------------------------------------------
# 输入输出
# --------------------------------------------------------------------------


def load_inputs(paths: list[Path]) -> tuple[list[Entity], list[dict[str, Any]]]:
    entities: list[Entity] = []
    inputs: list[dict[str, Any]] = []
    for path in paths:
        data = json.loads(path.read_text(encoding="utf-8"))
        objects, form = _iter_export_objects(data)
        label = data.get("label") or data.get("region", {}).get("mode") or ""
        source = data.get("source") or {}
        inputs.append(
            {
                "path": str(path),
                "form": form,
                "label": label,
                "object_count": len(objects),
                "source": {
                    "path": source.get("path") or source.get("document"),
                    "revision": source.get("revision"),
                    "geometry_revision": source.get("geometry_revision"),
                },
            }
        )
        if not objects:
            continue
        for index, raw in enumerate(objects):
            if not isinstance(raw, dict):
                continue
            entities.append(
                _normalize_entity(
                    raw,
                    form,
                    index,
                    source_file=path.name,
                    source_label=str(label),
                )
            )
    return entities, inputs


def _bbox_to_dict(bbox: tuple[float, float, float, float]) -> dict[str, Any]:
    return {
        "min": [round(bbox[0], 6), round(bbox[1], 6)],
        "max": [round(bbox[2], 6), round(bbox[3], 6)],
        "width": round(bbox[2] - bbox[0], 6),
        "height": round(bbox[3] - bbox[1], 6),
    }


def _view_to_dict(view: Region) -> dict[str, Any]:
    return {
        "id": view.id,
        "kind": view.kind,
        "kind_label": REGION_KIND_LABELS.get(view.kind, view.kind),
        "title": view.title,
        "dominant_source_label": view.dominant_label,
        "bbox": _bbox_to_dict(view.bbox),
        "entity_count": view.entity_count,
        "scores": view.scores,
        "rebar_numbers": view.numbers,
    }


def _region_to_dict(region: Region) -> dict[str, Any]:
    return {
        "id": region.id,
        "kind": region.kind,
        "kind_label": REGION_KIND_LABELS.get(region.kind, region.kind),
        "title": region.title,
        "dominant_source_label": region.dominant_label,
        "bbox": _bbox_to_dict(region.bbox),
        "entity_count": region.entity_count,
        "view_ids": region.member_ids,
        "view_count": len(region.member_ids),
        "rebar_numbers": region.numbers,
        "scores": region.scores,
        "evidence": region.evidence,
        "hints": region.hints,
        "handles": region.handle_list,
    }


def build_payload(
    regions: list[Region],
    views: list[Region],
    unassigned: list[Entity],
    noise: list[Entity],
    inputs: list[dict[str, Any]],
    padding: float,
    merge_padding: float,
) -> dict[str, Any]:
    kind_counts = Counter(region.kind for region in regions)
    return {
        "schema_version": "1.1",
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "purpose": "CAD 视图区域分类：大样区 / 综合图区 / 数量表区",
        "inputs": inputs,
        "parameters": {
            "padding": round(padding, 6),
            "merge_padding": round(merge_padding, 6),
            "view_count": len(views),
            "region_count": len(regions),
        },
        "summary": {
            "region_count": len(regions),
            "view_count": len(views),
            "kind_counts": dict(kind_counts),
            "unassigned_count": len(unassigned) + len(noise),
        },
        "regions": [_region_to_dict(region) for region in regions],
        "views": [_view_to_dict(view) for view in views],
        "unassigned": [
            {
                "handle": entity.handle,
                "kind": entity.kind,
                "layer": entity.layer,
                "reason": "no_geometry",
                "source_file": entity.source_file,
            }
            for entity in unassigned
        ]
        + [
            {
                "handle": entity.handle,
                "kind": entity.kind,
                "layer": entity.layer,
                "reason": "isolated_entity",
                "source_file": entity.source_file,
            }
            for entity in noise
        ],
    }


def _print_report(payload: dict[str, Any]) -> None:
    print("区域分类结果（区域 = 同类型相邻视图块的合并）")
    print("=" * 104)
    print(f"{'ID':<4}{'类型':<12}{'视图':>5}{'实体数':>7}{'宽×高':>22}{'标题':<26}{'主导来源'}")
    print("-" * 104)
    views_by_id = {view["id"]: view for view in payload["views"]}
    for region in payload["regions"]:
        bbox = region["bbox"]
        size = f"{bbox['width']:.0f}×{bbox['height']:.0f}"
        title = (region["title"] or "")[:22]
        print(
            f"{region['id']:<4}{region['kind_label']:<12}{region['view_count']:>5}"
            f"{region['entity_count']:>7}{size:>22}  {title:<24}{region['dominant_source_label']}"
        )
    print("-" * 104)
    print("摘要:", payload["summary"])
    if payload["unassigned"]:
        reasons = Counter(item["reason"] for item in payload["unassigned"])
        print(f"未归类实体: {dict(reasons)}")

    for region in payload["regions"]:
        grid = region["evidence"]["grid"]
        detail = region["evidence"]["detail"]
        composite = region["evidence"]["composite"]
        print()
        print(
            f"[{region['id']}] {region['kind_label']}  "
            f"实体={region['entity_count']} 编号={len(region['rebar_numbers'])} "
            f"scores={region['scores']}"
        )
        print(f"  标题: {region['title'] or '（无）'}")
        print(
            f"  表格: score={grid.get('score')} 表框={'有' if grid.get('frame') else '无'} "
            f"横线={grid.get('horizontal')} 竖线={grid.get('vertical')} "
            f"文字网格={grid.get('text_grid')}"
        )
        print(
            f"  大样: 文字={detail.get('texts')} 编号={detail.get('numbers')} "
            f"尺寸={detail.get('dimensions')} 引线={detail.get('leaders')} "
            f"重复轮廓={detail.get('repeated_shapes')}"
        )
        print(
            f"  综合: 尺寸={composite.get('dimensions')} 块={composite.get('inserts')} "
            f"长轮廓={composite.get('long_outlines')} 钢筋点={composite.get('steel_dots')} "
            f"面积占比={composite.get('area_ratio')}"
        )
        if region["rebar_numbers"]:
            sample = "、".join(region["rebar_numbers"][:10])
            print(f"  钢筋编号: {sample}{' …' if len(region['rebar_numbers']) > 10 else ''}")
        for view_id in region["view_ids"]:
            view = views_by_id.get(view_id)
            if view is None:
                continue
            print(
                f"    {view['id']} {view['kind_label']} {view['entity_count']:>5} "
                f"{(view['title'] or '（无标题）')[:28]}"
            )
        for hint in region["hints"]:
            print(f"  提示: {hint.get('kind')} - {hint.get('message')}")


def _write_split(payload: dict[str, Any], regions: list[Region], directory: Path) -> None:
    directory.mkdir(parents=True, exist_ok=True)
    for region, region_payload in zip(regions, payload["regions"]):
        objects = [entity.raw for entity in region.entities]
        document = {
            "generated_at": datetime.now(timezone.utc).isoformat(),
            "label": f"{region.id}-{REGION_KIND_LABELS.get(region.kind, region.kind)}",
            "region": region_payload,
            "exported_count": len(objects),
            "objects": objects,
        }
        safe_id = re.sub(r"[^0-9A-Za-z._-]+", "_", region.id)
        path = directory / f"{safe_id}_{region.kind}.json"
        path.write_text(json.dumps(document, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"  已写出 {path}")


def _resolve_merge_padding(args: argparse.Namespace, entities: list[Entity]) -> float:
    if args.no_merge:
        return 0.0
    if args.merge_padding is not None:
        return args.merge_padding
    return auto_merge_padding(entities)


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
    except (AttributeError, ValueError):
        pass

    parser = argparse.ArgumentParser(
        description="把 CAD 导出 JSON 分类成钢筋大样区 / 综合图区 / 数量表区。",
    )
    parser.add_argument("inputs", nargs="+", help="一个或多个导出 JSON 文件。")
    parser.add_argument("-o", "--output", default="regions.json", help="分类结果 JSON 输出路径。")
    parser.add_argument(
        "--padding",
        type=float,
        default=0.0,
        help="第一级空白聚类膨胀距离；0 表示按中位字高自动取值（默认 4 倍）。",
    )
    parser.add_argument(
        "--merge-padding",
        type=float,
        default=None,
        help="第二级同类型视图合并距离；默认按中位字高 12 倍自动取值。",
    )
    parser.add_argument(
        "--no-merge",
        action="store_true",
        help="不合并同类型视图，每个视图块单独输出（区域即视图）。",
    )
    parser.add_argument("--report", action="store_true", help="在终端打印可读报告。")
    parser.add_argument(
        "--per-file",
        action="store_true",
        help="每个输入文件单独分类（默认把多个输入合并成一张图）。",
    )
    parser.add_argument(
        "--write-split",
        metavar="DIR",
        default=None,
        help="把每个区域的实体写成独立 JSON（便于分区域交给下游步骤）。",
    )
    args = parser.parse_args(argv)

    paths = [Path(item) for item in args.inputs]
    missing = [str(path) for path in paths if not path.exists()]
    if missing:
        print("找不到输入文件: " + ", ".join(missing), file=sys.stderr)
        return 2

    regions: list[Region] = []
    views: list[Region] = []
    unassigned: list[Entity] = []
    noise: list[Entity] = []
    region_inputs: list[dict[str, Any]] = []
    padding = 0.0
    merge_padding = 0.0

    if args.per_file:
        for path in paths:
            file_entities, file_inputs = load_inputs([path])
            region_inputs.extend(file_inputs)
            if not file_entities:
                continue
            padding = args.padding or auto_padding(file_entities)
            merge_padding = _resolve_merge_padding(args, file_entities)
            file_regions, file_views, file_unassigned, file_noise = build_regions(
                file_entities, padding, merge_padding
            )
            for region in file_regions:
                region.id = f"{path.stem}:{region.id}"
            for view in file_views:
                view.id = f"{path.stem}:{view.id}"
            regions.extend(file_regions)
            views.extend(file_views)
            unassigned.extend(file_unassigned)
            noise.extend(file_noise)
        if not regions:
            print("输入里没有任何可分类实体。", file=sys.stderr)
            return 1
        regions.sort(key=lambda region: (region.dominant_label, region.id))
    else:
        entities, region_inputs = load_inputs(paths)
        if not entities:
            print("输入里没有任何可分类实体。", file=sys.stderr)
            return 1
        padding = args.padding or auto_padding(entities)
        merge_padding = _resolve_merge_padding(args, entities)
        regions, views, unassigned, noise = build_regions(entities, padding, merge_padding)

    payload = build_payload(
        regions, views, unassigned, noise, region_inputs, padding, merge_padding
    )

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"已写出 {output.resolve()}（{payload['summary']['region_count']} 个区域）")

    if args.report:
        _print_report(payload)

    if args.write_split:
        _write_split(payload, regions, Path(args.write_split))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
