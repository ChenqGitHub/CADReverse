"""三区联动校验：综合图 / 钢筋大样 / 数量表 的编号、直径、长度、数量对账。

输入是 Open CAD 导出的 compact JSON（`objects`）或分区导出的区域 JSON。
输出是按钢筋编号连接的三区视图，以及可复现的校验结论，供
skills/cad-rebar-skill/references/json-to-csharp.md 的参数提取步骤使用。

用法：
    python linkage_report.py general.json detail.json table.json --report
    python linkage_report.py regions_dir/ --report
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable

ROLE_KEYWORDS = {
    "schedule": ("数量表", "工程数量", "钢筋表", "材料表", "明细表", "汇总表"),
    "detail": ("大样", "钢筋形状", "配料"),
    "composite": ("综合图", "布置图", "构造图", "配筋图", "立面图", "侧面图", "剖面图", "断面图"),
}

NUMBER_RE = re.compile(r"^(N\d+-\d+)(?:[～~\-–—](\d+))?$")
SPEC_RE = re.compile(r"^\s*(?P<diameter>\d+)\s*(?:L\s*=\s*(?P<formula>.+?))?\s*$", re.IGNORECASE)
FORMULA_H_RE = re.compile(r"H\s*(?P<sign>[+\-])\s*(?P<delta>\d+(?:\.\d+)?)", re.IGNORECASE)
FORMULA_NUM_RE = re.compile(r"^\s*(?P<value>\d+(?:\.\d+)?)\s*(?:[（(](?:平均|均)[)）])?")

HEADER_LABELS = {
    "编号": "number",
    "直径": "diameter",
    "单根长": "single_length",
    "根数": "count",
    "总长": "total_length",
    "单位重": "unit_weight",
    "总重": "total_weight",
    "墩号": "pier",
    "墩高": "height",
}


def _clean(value: str) -> str:
    """去掉 MText 换行控制符和空白，便于表头/单元格匹配。"""
    return re.sub(r"\\[Pp]", "", value).replace("\u00a0", " ").strip()


def _xy(obj: dict[str, Any]) -> tuple[float, float] | None:
    geometry = obj.get("geometry") or {}
    point = geometry.get("position")
    if isinstance(point, dict):
        return float(point.get("x", 0.0)), float(point.get("y", 0.0))
    return None


def _attrs(obj: dict[str, Any]) -> dict[str, str]:
    result: dict[str, str] = {}
    geometry = obj.get("geometry") or {}
    items = geometry.get("attributes")
    if items is None:
        items = obj.get("attributes")
    for item in items or []:
        tag = item.get("tag")
        if not tag:
            continue
        value = item.get("value")
        if value is None:
            value = item.get("text")
        result[str(tag)] = _clean(value or "")
    return result


def _objects(payload: dict[str, Any]) -> list[dict[str, Any]]:
    objects = payload.get("objects")
    if isinstance(objects, list):
        return objects
    entities = payload.get("entities")
    if isinstance(entities, list):
        return [
            {
                "type": (item.get("object_name") or "").replace("AcDb", ""),
                "layer": item.get("layer"),
                "text": (item.get("geometry") or {}).get("text"),
                "geometry": item.get("geometry") or {},
                "attributes": (item.get("geometry") or {}).get("attributes"),
                "handle": item.get("handle"),
            }
            for item in entities
        ]
    raise ValueError("输入既没有 objects 也没有 entities")


def _role(path: Path, payload: dict[str, Any]) -> str:
    label = str(payload.get("label") or payload.get("region", {}).get("label") or path.name)
    for role, words in ROLE_KEYWORDS.items():
        if any(word in label for word in words):
            return role
    raise ValueError(f"无法从标签判断区域类型：{label!r}（{path.name}）")


def _expand_number(raw: str) -> list[str]:
    """把 `N2-1～2` 展开成 `N2-1`、`N2-2`。"""
    text = re.sub(r"\s+", "", raw)
    match = NUMBER_RE.match(text)
    if not match:
        return []
    prefix, end = match.group(1), match.group(2)
    if end is None:
        return [prefix]
    bang = prefix.index("-")
    head, start = prefix[:bang], int(prefix[bang + 1 :])
    return [f"{head}-{index}" for index in range(start, int(end) + 1)]


@dataclass
class Spec:
    raw: str
    diameter: int | None
    formula: str | None
    average: bool

    def evaluate(self, height: float | None, offset: float = 0.0) -> float | None:
        """计算单根长；`H±δ` 公式需要墩高，`L=δ` 直接取值。"""
        if not self.formula:
            return None
        match = FORMULA_H_RE.search(self.formula)
        if match:
            if height is None:
                return None
            delta = float(match.group("delta"))
            return height + delta if match.group("sign") == "+" else height - delta
        match = FORMULA_NUM_RE.match(self.formula)
        if match:
            return float(match.group("value"))
        return None


def _parse_spec(raw: str) -> Spec | None:
    text = _clean(raw)
    if not text:
        return None
    average = "平均" in text or "（均）" in text or "(均)" in text
    match = SPEC_RE.match(text)
    if not match:
        return None
    diameter = int(match.group("diameter"))
    formula = (match.group("formula") or "").strip() or None
    if formula:
        formula = re.sub(r"\s+", "", formula)
        formula = re.sub(r"[（(](平均|均)[)）]", "", formula)
    return Spec(raw=text, diameter=diameter, formula=formula, average=average)


@dataclass
class RegionData:
    path: Path
    role: str
    label: str
    objects: list[dict[str, Any]]
    source: dict[str, Any] = field(default_factory=dict)

    @property
    def revision_key(self) -> tuple[Any, Any]:
        return self.source.get("document_id"), self.source.get("geometry_revision")


def load_regions(paths: list[Path]) -> list[RegionData]:
    regions: list[RegionData] = []
    files: list[Path] = []
    for path in paths:
        if path.is_dir():
            files.extend(sorted(path.glob("*.json")))
        else:
            files.append(path)
    for path in files:
        payload = json.loads(path.read_text(encoding="utf-8"))
        regions.append(
            RegionData(
                path=path,
                role=_role(path, payload),
                label=str(payload.get("label") or ""),
                objects=_objects(payload),
                source=payload.get("source") or {},
            )
        )
    return regions


# ---------------------------------------------------------------- detail


def extract_details(region: RegionData) -> tuple[dict[str, dict[str, Any]], list[dict[str, Any]]]:
    """从大样区提取 编号 → {spec, diameter, handle}；区间编号单独返回。"""
    exact: dict[str, dict[str, Any]] = {}
    ranges: list[dict[str, Any]] = []
    for obj in region.objects:
        if not str(obj.get("type", "")).lower().startswith("insert"):
            continue
        attributes = _attrs(obj)
        raw_number = attributes.get("编号", "")
        if not raw_number:
            continue
        spec = _parse_spec(attributes.get("直径", ""))
        expanded = _expand_number(raw_number)
        item = {
            "raw_number": raw_number,
            "numbers": expanded,
            "spec": spec,
            "diameter": spec.diameter if spec else None,
            "handle": obj.get("handle"),
        }
        if len(expanded) == 1:
            exact.setdefault(expanded[0], item)
        elif expanded:
            ranges.append(item)
    return exact, ranges


# ---------------------------------------------------------------- composite


def extract_composite(region: RegionData) -> dict[str, dict[str, Any]]:
    """从综合图提取 编号 → 放置数量 / 直径。"""
    placements: dict[str, dict[str, Any]] = defaultdict(
        lambda: {"count": 0, "diameters": set(), "handles": []}
    )
    for obj in region.objects:
        if not str(obj.get("type", "")).lower().startswith("insert"):
            continue
        attributes = _attrs(obj)
        for number in _expand_number(attributes.get("编号", "")):
            spec = _parse_spec(attributes.get("直径", ""))
            entry = placements[number]
            entry["count"] += 1
            if spec and spec.diameter:
                entry["diameters"].add(spec.diameter)
            if obj.get("handle"):
                entry["handles"].append(obj.get("handle"))
    return dict(placements)


# ---------------------------------------------------------------- schedule


@dataclass
class ScheduleTable:
    title: str
    height: float | None
    columns: dict[str, float]  # key -> center x
    rows: list[dict[str, Any]]
    header_y: float | None


def _cluster_rows(cells: list[tuple[float, float, str]], tolerance: float) -> list[list[tuple[float, float, str]]]:
    rows: list[list[tuple[float, float, str]]] = []
    for cell in sorted(cells, key=lambda item: (-item[1], item[0])):
        if rows and abs(rows[-1][0][1] - cell[1]) <= tolerance:
            rows[-1].append(cell)
        else:
            rows.append([cell])
    return rows


def extract_schedule(region: RegionData) -> ScheduleTable:
    cells: list[tuple[float, float, str]] = []
    for obj in region.objects:
        kind = str(obj.get("type", ""))
        if kind not in ("Text", "MText"):
            continue
        point = _xy(obj)
        text = _clean(str(obj.get("text") or ""))
        if point and text:
            cells.append((point[0], point[1], text))

    title = next((text for _, _, text in cells if "数量表" in text), "")
    header_row = None
    columns: dict[str, float] = {}
    header_y = None
    for row in _cluster_rows(cells, tolerance=5.0):
        labels: dict[str, float] = {}
        for x, _y, text in row:
            key = HEADER_LABELS.get(text)
            if key is None:
                for prefix, mapped in HEADER_LABELS.items():
                    if text.startswith(prefix):
                        key = mapped
                        break
            if key:
                labels[key] = x
        if "number" in labels and "count" in labels and "total_weight" in labels:
            header_row, columns, header_y = row, labels, row[0][1]
            break
    if header_row is None or header_y is None:
        raise ValueError(f"{region.path} 里没有找到数量表表头")

    # 表头上方可能还有一行：墩高 H 的标签和数值。
    height = None
    for row in _cluster_rows(cells, tolerance=5.0):
        text_values = {text: x for x, _y, text in row}
        label = next((text for text in text_values if text.startswith("墩高")), None)
        if label is None:
            continue
        label_x = text_values[label]
        numeric = [
            (x, float(text))
            for x, _y, text in row
            if re.fullmatch(r"\d+(?:\.\d+)?", text) and x > label_x
        ]
        if numeric:
            height = min(numeric, key=lambda item: item[0])[1]
        break

    inserts = [
        obj for obj in region.objects if str(obj.get("type", "")).lower().startswith("insert")
    ]
    diameter_cells: list[tuple[float, float, int]] = []
    for obj in inserts:
        point = _xy(obj)
        spec = _parse_spec(_attrs(obj).get("直径", ""))
        if point and spec and spec.diameter:
            diameter_cells.append((point[0], point[1], spec.diameter))

    rows: list[dict[str, Any]] = []
    row_ys = sorted({round(y, 1) for x, y, text in cells if "number" in columns and _expand_number(text)})
    # 行高约等于相邻两行的间距；用行中心附近最近单元格判断归属。
    for y in row_ys:
        if header_y is not None and y >= header_y - 1:
            continue
        row: dict[str, Any] = {"y": y}
        for x, _cy, text in cells:
            if abs(_cy - y) > 5:
                continue
            for key, center in columns.items():
                if key != "number" and abs(x - center) <= 400:
                    row[key] = text
            numbers = _expand_number(text)
            if numbers and abs(x - columns["number"]) <= 400:
                row["number"] = numbers[0]
        if "number" not in row:
            continue
        # 直径列由块属性提供
        if "diameter" not in row:
            candidates = [
                (abs(cy - y), diameter)
                for cx, cy, diameter in diameter_cells
                if abs(cx - columns.get("diameter", 0)) <= 400
            ]
            if candidates:
                row["diameter"] = min(candidates)[1]
        for key, value in list(row.items()):
            if key in ("y", "number", "diameter") or not isinstance(value, str):
                continue
            try:
                row[key] = float(value)
            except ValueError:
                pass
        rows.append(row)
    return ScheduleTable(title=title, height=height, columns=columns, rows=rows, header_y=header_y)


# ---------------------------------------------------------------- report


def build_report(regions: list[RegionData]) -> dict[str, Any]:
    by_role = {region.role: region for region in regions}
    for role in ("composite", "detail", "schedule"):
        if role not in by_role:
            raise ValueError(f"缺少 {role} 区域，收到：{[region.role for region in regions]}")

    composite = by_role["composite"]
    detail = by_role["detail"]
    schedule_region = by_role["schedule"]

    exact_details, detail_ranges = extract_details(detail)
    placements = extract_composite(composite)
    table = extract_schedule(schedule_region)

    detail_numbers = set(exact_details)
    range_numbers = {number for item in detail_ranges for number in item["numbers"]}
    composite_numbers = set(placements)
    table_numbers = {row["number"] for row in table.rows}

    rows: list[dict[str, Any]] = []
    mismatches: list[dict[str, Any]] = []
    warnings: list[dict[str, Any]] = []
    for row in table.rows:
        number = row["number"]
        detail_item = exact_details.get(number)
        if detail_item is None:
            range_item = next(
                (item for item in detail_ranges if number in item["numbers"]), None
            )
            detail_item = range_item
        spec: Spec | None = detail_item["spec"] if detail_item else None
        evaluated = spec.evaluate(table.height) if spec else None
        placement = placements.get(number)

        def mismatch(kind: str, message: str) -> None:
            mismatches.append({"number": number, "kind": kind, "message": message})

        if detail_item is None:
            mismatch("missing_detail", "数量表有编号，大样区没有对应模板")
        if placement is None:
            mismatch("missing_placement", "数量表有编号，综合图没有放置记录")
        if spec and spec.diameter and row.get("diameter") and spec.diameter != row["diameter"]:
            mismatch(
                "diameter",
                f"大样直径 {spec.diameter} ≠ 数量表直径 {row['diameter']}",
            )
        if evaluated is not None and row.get("single_length") is not None:
            if abs(evaluated - row["single_length"]) > 1.0:
                mismatch(
                    "single_length",
                    f"大样公式 {spec.raw} 计算 {evaluated:.1f} ≠ 数量表 {row['single_length']:.1f}",
                )
        if table.height is not None and spec and spec.formula and FORMULA_H_RE.search(spec.formula):
            evaluated_h = spec.evaluate(table.height)
            if evaluated_h is None:
                mismatch("formula", f"大样公式 {spec.raw} 依赖 H，但无法计算")
        if spec and spec.average:
            warnings.append(
                {
                    "number": number,
                    "kind": "average_length",
                    "message": "大样规格标注为平均长度，数量表按同一值计列",
                }
            )

        count = row.get("count")
        single = row.get("single_length")
        total_length = row.get("total_length")
        if isinstance(count, float) and isinstance(single, float) and isinstance(total_length, float):
            expected = count * single / 1000.0
            if abs(expected - total_length) > max(0.2, total_length * 0.002):
                mismatch(
                    "total_length",
                    f"根数×单根长 {expected:.3f} m ≠ 总长 {total_length:.3f} m",
                )
        unit_weight = row.get("unit_weight")
        total_weight = row.get("total_weight")
        if isinstance(unit_weight, float) and isinstance(total_weight, float) and isinstance(total_length, float):
            expected = unit_weight * total_length
            if abs(expected - total_weight) > max(0.5, total_weight * 0.002):
                mismatch(
                    "total_weight",
                    f"单位重×总长 {expected:.1f} kg ≠ 总重 {total_weight:.1f} kg",
                )

        rows.append(
            {
                "number": number,
                "role": "detail",
                "detail_spec": spec.raw if spec else None,
                "detail_formula": spec.formula if spec else None,
                "detail_diameter": spec.diameter if spec else None,
                "detail_handles": [detail_item.get("handle")] if detail_item else [],
                "composite_placements": placement["count"] if placement else 0,
                "composite_diameters": sorted(placement["diameters"]) if placement else [],
                "composite_handles": placement["handles"] if placement else [],
                "schedule": {
                    key: row.get(key)
                    for key in (
                        "single_length",
                        "count",
                        "total_length",
                        "unit_weight",
                        "total_weight",
                        "diameter",
                    )
                },
                "evaluated_single_length": evaluated,
            }
        )

    for number in sorted((detail_numbers | range_numbers) - table_numbers):
        mismatches.append(
            {"number": number, "kind": "missing_schedule", "message": "大样区有编号，数量表没有对应行"}
        )

    revisions = {region.revision_key for region in regions}
    if len(revisions) > 1:
        warnings.append(
            {
                "number": "-",
                "kind": "revision_mismatch",
                "message": f"三个区域来自不同修订：{sorted(revisions)}；跨修订对账前需确认几何一致",
            }
        )

    return {
        "inputs": [
            {
                "role": region.role,
                "path": str(region.path),
                "label": region.label,
                "objects": len(region.objects),
                "source": region.source,
            }
            for region in regions
        ],
        "height": table.height,
        "table": {
            "title": table.title,
            "columns": table.columns,
            "row_count": len(table.rows),
        },
        "counts": {
            "detail_exact": len(detail_numbers),
            "detail_ranges": len(detail_ranges),
            "detail_expanded": len(detail_numbers | range_numbers),
            "composite": len(composite_numbers),
            "schedule": len(table_numbers),
        },
        "number_sets": {
            "detail_only": sorted((detail_numbers | range_numbers) - table_numbers),
            "schedule_only": sorted(table_numbers - (detail_numbers | range_numbers)),
            "composite_only": sorted(composite_numbers - table_numbers),
            "schedule_without_composite": sorted(table_numbers - composite_numbers),
        },
        "rows": rows,
        "mismatches": mismatches,
        "warnings": warnings,
    }


def _display_width(text: str) -> int:
    return sum(2 if ord(char) > 0x2E7F else 1 for char in text)


def _pad(text: str, width: int) -> str:
    return text + " " * max(0, width - _display_width(text))


def _print_report(report: dict[str, Any]) -> None:
    counts = report["counts"]
    print("三区联动校验")
    print("=" * 104)
    print(
        f"输入：大样 {counts['detail_expanded']} 个编号（含区间展开）、"
        f"综合图 {counts['composite']} 个编号、数量表 {counts['schedule']} 行；墩高 H={report['height']}"
    )
    for key, values in report["number_sets"].items():
        if values:
            print(f"  {key}: {values}")
    print("-" * 104)
    header = (
        f"{'编号':<8}{'直径':>4}  {'大样规格':<24}{'公式值':>8}{'表单根长':>9}"
        f"{'根数':>6}{'表总长':>9}{'综合放置':>8}"
    )
    print(header)
    for row in report["rows"]:
        schedule = row["schedule"]
        spec = row["detail_spec"] or ""
        print(
            f"{row['number']:<8}{row['detail_diameter'] or '':>4}  {spec:<24}"
            f"{(row['evaluated_single_length'] or 0):>8.0f}{schedule.get('single_length') or 0:>9.0f}"
            f"{int(schedule.get('count') or 0):>6}{schedule.get('total_length') or 0:>9.1f}"
            f"{row['composite_placements']:>8}"
        )
    print("-" * 104)
    mismatches = report["mismatches"]
    if mismatches:
        print(f"发现 {len(mismatches)} 处不一致：")
        for item in mismatches:
            print(f"  [{item['kind']}] {item['number']}: {item['message']}")
    else:
        print("一致性检查通过：编号集合、直径、单根长公式、根数×单根长、单位重×总长全部吻合。")
    if report["warnings"]:
        print(f"提示 {len(report['warnings'])} 条：")
        for item in report["warnings"]:
            print(f"  [{item['kind']}] {item['number']}: {item['message']}")


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
    except (AttributeError, ValueError):
        pass

    parser = argparse.ArgumentParser(description="校验钢筋图三区（综合图/大样/数量表）的编号联动。")
    parser.add_argument("inputs", nargs="+", help="compact/区域 JSON 文件，或包含它们的目录。")
    parser.add_argument("-o", "--output", default=None, help="报告 JSON 输出路径。")
    parser.add_argument("--report", action="store_true", help="打印可读报告。")
    parser.add_argument("--strict", action="store_true", help="存在不一致时以非零码退出。")
    args = parser.parse_args(argv)

    paths = [Path(item) for item in args.inputs]
    missing = [str(path) for path in paths if not path.exists()]
    if missing:
        print("找不到输入文件: " + ", ".join(missing), file=sys.stderr)
        return 2

    try:
        regions = load_regions(paths)
        report = build_report(regions)
    except (ValueError, OSError, json.JSONDecodeError) as exc:
        print(f"校验失败: {exc}", file=sys.stderr)
        return 1

    if args.report:
        _print_report(report)
    if args.output:
        output = Path(args.output)
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"已写出 {output.resolve()}")

    if args.strict and report["mismatches"]:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
