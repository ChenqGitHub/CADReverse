"""用可复现的几何/数据检查对比「语义模型 → C# → DWG」和源图三区。

用法：
    python validate_reconstruction.py `
        --model ..\\pier_body_model.json `
        --dwg ..\\CadCli\\bin\\Debug\\net10.0\\pier-body-reinforcement-semantic.dwg `
        --json-out ..\\artifacts\\semantic-validation.json

检查分三层：
1. 数据层：大样模板 ↔ 数量表（编号/直径/单根长/根数/总长/总重）、放置、表结构、联动结论；
2. 几何层：参数化构件的顶点回验、源图几何包围盒 ↔ 生成 DWG 回读包围盒；
3. DWG 层：实体/图层计数与模型推算值一致。

DWG 计数通过 `dotnet run --project CadCli -- --stats <dwg> --json` 回读。
"""

from __future__ import annotations

import argparse
import json
import math
import subprocess
import sys
from pathlib import Path
from typing import Any

import extract_pier_model as extractor

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MODEL = REPO_ROOT / "pier_body_model.json"
DEFAULT_DWG = REPO_ROOT / "CadCli" / "bin" / "Debug" / "net10.0" / "pier-body-reinforcement-semantic.dwg"
DEFAULT_JSON = REPO_ROOT / "artifacts" / "semantic-validation.json"


def _bulge_points(
    x1: float, y1: float, bulge: float, x2: float, y2: float, steps: int = 12
) -> list[tuple[float, float]]:
    """把带 bulge 的线段采样成折线，用于包围盒/几何对比。"""
    if abs(bulge) <= extractor.BULGE_TOLERANCE:
        return [(x1, y1), (x2, y2)]
    theta = 4.0 * math.atan(bulge)
    dx = x2 - x1
    dy = y2 - y1
    chord = math.hypot(dx, dy)
    if chord <= 1e-12:
        return [(x1, y1)]
    radius = chord / (2.0 * math.sin(abs(theta) / 2.0))
    nx, ny = -dy / chord, dx / chord
    height = radius * math.cos(theta / 2.0)
    sign = 1.0 if bulge > 0 else -1.0
    cx = (x1 + x2) / 2.0 + nx * height * sign
    cy = (y1 + y2) / 2.0 + ny * height * sign
    start = math.atan2(y1 - cy, x1 - cx)
    return [
        (cx + radius * math.cos(start + theta * index / steps), cy + radius * math.sin(start + theta * index / steps))
        for index in range(steps + 1)
    ]


def _polyline_points(vertices: list[dict[str, float]], closed: bool) -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []
    count = len(vertices)
    for index in range(count - (0 if closed else 1)):
        current = vertices[index]
        following = vertices[(index + 1) % count]
        points.extend(_bulge_points(current["x"], current["y"], current["bulge"], following["x"], following["y"]))
    return points


def _arc_points(
    x: float, y: float, radius: float, start: float, end: float, steps: int = 16
) -> list[tuple[float, float]]:
    sweep = end - start
    while sweep <= 0:
        sweep += 2.0 * math.pi
    return [
        (x + radius * math.cos(start + sweep * index / steps), y + radius * math.sin(start + sweep * index / steps))
        for index in range(steps + 1)
    ]


class Bounds:
    def __init__(self) -> None:
        self.min_x = math.inf
        self.min_y = math.inf
        self.max_x = -math.inf
        self.max_y = -math.inf

    def add(self, x: float, y: float) -> None:
        self.min_x = min(self.min_x, x)
        self.min_y = min(self.min_y, y)
        self.max_x = max(self.max_x, x)
        self.max_y = max(self.max_y, y)

    def add_many(self, points: list[tuple[float, float]]) -> None:
        for x, y in points:
            self.add(x, y)

    @property
    def valid(self) -> bool:
        return self.min_x <= self.max_x and self.min_y <= self.max_y

    def as_dict(self) -> dict[str, float]:
        return {
            "minX": round(self.min_x, 3),
            "minY": round(self.min_y, 3),
            "maxX": round(self.max_x, 3),
            "maxY": round(self.max_y, 3),
        }


def source_geometry_bounds(model: dict[str, Any]) -> dict[str, float]:
    """源图几何包围盒：只统计会生成几何的实体（线/多段线/弧/圆/构件/大样/表框）。"""
    bounds = Bounds()
    geometry = model["composite_geometry"]
    for item in geometry["lines"]:
        bounds.add(item["x1"], item["y1"])
        bounds.add(item["x2"], item["y2"])
    for item in geometry["polylines"]:
        bounds.add_many(_polyline_points(item["vertices"], bool(item["closed"])))
    for item in geometry["arcs"]:
        bounds.add_many(_arc_points(item["x"], item["y"], item["radius"], item["start_angle"], item["end_angle"]))
    for item in geometry["circles"]:
        bounds.add(item["x"] - item["radius"], item["y"] - item["radius"])
        bounds.add(item["x"] + item["radius"], item["y"] + item["radius"])

    for component in model.get("components", []):
        origin_x, origin_y = component["origin"]
        for x, y in (
            (origin_x, origin_y),
            (origin_x + component["width"], origin_y),
            (origin_x + component["width"], origin_y + component["height"]),
            (origin_x, origin_y + component["height"]),
        ):
            bounds.add(x, y)

    for template in model["detail_templates"].values():
        origin_x, origin_y = template["origin"]
        points = _polyline_points(template["vertices"], bool(template.get("closed")))
        bounds.add_many([(origin_x + x, origin_y + y) for x, y in points])

    table = model["parameters"]["table"]
    origin_x, origin_y = table["origin"]
    bounds.add(origin_x, origin_y)
    bounds.add(origin_x + table["width"], origin_y + table["height"])
    return bounds.as_dict()


def expected_entity_counts(model: dict[str, Any]) -> dict[str, int]:
    geometry = model["composite_geometry"]
    table = model["parameters"]["table"]
    components = model.get("components", [])
    templates = model["detail_templates"]
    rows = model["schedule_rows"]
    raw_polylines = [item for item in geometry["polylines"] if "component" not in item]

    header_types = [item["type"] for item in table["header_labels"]]
    summary_types = [item["type"] for item in table["summary_labels"]]
    text_types = [item.get("type") for item in model["composite_texts"]]

    return {
        "Line": len(geometry["lines"]) + len(table["verticals"]) + len(table["horizontals"]),
        "LwPolyline": len(raw_polylines) + len(components) + len(templates) * 2 + 1,
        "Insert": len(model["composite_labels"]) + len(model["composite_sections"]) + len(rows) + len(templates),
        "Arc": len(geometry["arcs"]),
        "Circle": len(geometry["circles"]),
        "MText": len(rows) * 6
        + header_types.count("MText")
        + summary_types.count("MText")
        + text_types.count("MText"),
        "TextEntity": header_types.count("Text")
        + summary_types.count("Text")
        + text_types.count("Text")
        + text_types.count(None),
    }


def read_dwg_stats(dwg_path: Path) -> dict[str, Any]:
    command = [
        "dotnet",
        "run",
        "--no-build",
        "--project",
        str(REPO_ROOT / "CadCli"),
        "--",
        "--stats",
        str(dwg_path),
        "--json",
    ]
    completed = subprocess.run(command, cwd=REPO_ROOT, capture_output=True, text=True, encoding="utf-8")
    if completed.returncode != 0:
        raise RuntimeError(f"读取 DWG 失败：{completed.stderr.strip() or completed.stdout.strip()}")
    lines = [line.strip() for line in completed.stdout.splitlines() if line.strip()]
    if not lines:
        raise RuntimeError("读取 DWG 没有输出")
    return json.loads(lines[-1])


def _check(checks: list[dict[str, Any]], name: str, ok: bool, expected: Any = None, actual: Any = None, detail: str = "") -> None:
    checks.append(
        {
            "name": name,
            "ok": bool(ok),
            "expected": expected,
            "actual": actual,
            "detail": detail,
        }
    )


def validate(model: dict[str, Any], dwg_path: Path, geometry_tolerance: float = 1.0) -> dict[str, Any]:
    checks: list[dict[str, Any]] = []
    schedule = {row["number"]: row for row in model["schedule_rows"]}
    templates = model["detail_templates"]

    # ---------------- 数据层 ----------------
    _check(checks, "数量表行数", len(model["schedule_rows"]) == 14, 14, len(model["schedule_rows"]))
    _check(checks, "大样模板数", len(templates) == 13, 13, len(templates))
    missing_templates = sorted(set(schedule) - set(templates))
    _check(checks, "缺模板编号", missing_templates == ["N1-6"], ["N1-6"], missing_templates)

    for number, template in templates.items():
        row = schedule.get(number)
        if row is None:
            _check(checks, f"{number} 数量表行", False, "存在", "缺失")
            continue
        length_ok = abs(float(template["true_length"]) - float(row["single_length"])) <= 0.5
        _check(checks, f"{number} 单根长", length_ok, row["single_length"], template["true_length"])
        _check(
            checks,
            f"{number} 直径",
            int(template["diameter"] or 0) == int(row["diameter"] or 0),
            row["diameter"],
            template["diameter"],
        )

    for number, row in schedule.items():
        count = int(row["count"] or 0)
        single = float(row["single_length"] or 0)
        total_length = float(row["total_length"] or 0)
        unit = float(row["unit_weight"] or 0)
        total_weight = float(row["total_weight"] or 0)
        expected_length = round(count * single / 1000.0, 1)
        expected_weight = round(count * single / 1000.0 * unit, 1)
        _check(
            checks,
            f"{number} 总长 = 根数×单根长",
            abs(expected_length - total_length) <= 0.15,
            expected_length,
            total_length,
        )
        _check(
            checks,
            f"{number} 总重 = 根数×单根长×单位重",
            abs(expected_weight - total_weight) <= 0.15,
            expected_weight,
            total_weight,
        )

    placements = model["placements"]
    _check(
        checks,
        "综合图放置数",
        sum(len(items) for items in placements.values()) == 83,
        83,
        sum(len(items) for items in placements.values()),
    )
    _check(checks, "编号块数", len(model["composite_labels"]) == 83, 83, len(model["composite_labels"]))
    _check(checks, "断面符号数", len(model["composite_sections"]) == 288, 288, len(model["composite_sections"]))
    _check(checks, "参数化构件数", len(model.get("components", [])) == 3, 3, len(model.get("components", [])))
    _check(
        checks,
        "综合图孤立噪声",
        model.get("composite_excluded_noise") == 2,
        2,
        model.get("composite_excluded_noise"),
    )

    table = model["parameters"]["table"]
    _check(checks, "表宽", abs(table["width"] - 6500) <= 0.01, 6500, round(table["width"], 3))
    _check(checks, "表高", abs(table["height"] - 6200.017) <= 0.1, 6200.017, round(table["height"], 3))
    _check(checks, "行距", abs(table["row_step"] - 316.667) <= 0.01, 316.667, table["row_step"])
    _check(checks, "首行 Y", abs(table["first_row_y"] - 4928.788) <= 0.1, 4928.788, table["first_row_y"])

    mismatches = model.get("linkage", {}).get("mismatches", [])
    _check(checks, "三区联动无硬错误", not mismatches, [], mismatches)

    # 参数化构件：用参数重建顶点，回验源图多段线。
    marked = [
        (item, item["component"])
        for item in model["composite_geometry"]["polylines"]
        if "component" in item
    ]
    for item, index in marked:
        component = model["components"][index]
        canonical = extractor._channel_section_vertices(
            component["width"], component["height"], component["radius"], component["channel_span"]
        )
        offset = [
            {"x": vertex["x"] + component["origin"][0], "y": vertex["y"] + component["origin"][1], "bulge": vertex["bulge"]}
            for vertex in canonical
        ]
        simplified = extractor._simplify_closed(item["vertices"])
        error = extractor._sequence_error(offset, simplified)
        _check(
            checks,
            f"构件 {component['width']:.0f}×{component['height']:.0f} 顶点回验",
            error <= extractor.FIT_TOLERANCE,
            f"≤{extractor.FIT_TOLERANCE}",
            round(error, 6),
        )

    for component in model.get("components", []):
        _check(
            checks,
            f"构件 {component['width']:.0f}×{component['height']:.0f} 拟合误差",
            component["fit_error"] <= extractor.FIT_TOLERANCE,
            f"≤{extractor.FIT_TOLERANCE}",
            component["fit_error"],
        )

    # ---------------- DWG 层 ----------------
    report: dict[str, Any] = {
        "model": str(DEFAULT_MODEL),
        "dwg": str(dwg_path),
        "checks": checks,
    }
    if dwg_path.exists():
        try:
            stats = read_dwg_stats(dwg_path)
            report["dwg"] = str(dwg_path)
            report["dwg_stats"] = stats

            expected_counts = expected_entity_counts(model)
            actual_counts = stats.get("byType", {})
            for entity_type, expected in expected_counts.items():
                actual = int(actual_counts.get(entity_type, 0))
                _check(checks, f"DWG {entity_type} 计数", actual == expected, expected, actual)
            _check(
                checks,
                "DWG 实体总数",
                int(stats.get("entities", 0)) == sum(expected_counts.values()),
                sum(expected_counts.values()),
                stats.get("entities"),
            )

            source_bounds = source_geometry_bounds(model)
            actual_bounds = stats.get("geometryBbox")
            report["source_geometry_bounds"] = source_bounds
            report["dwg_geometry_bounds"] = actual_bounds
            if actual_bounds:
                edges = ("minX", "minY", "maxX", "maxY")
                worst = max(abs(actual_bounds[edge] - source_bounds[edge]) for edge in edges)
                _check(
                    checks,
                    "DWG 几何包围盒",
                    worst <= geometry_tolerance,
                    f"各边误差 ≤{geometry_tolerance}",
                    round(worst, 3),
                    detail=json.dumps({"source": source_bounds, "dwg": actual_bounds}, ensure_ascii=False),
                )
            else:
                _check(checks, "DWG 几何包围盒", False, "可回读", None)
        except (RuntimeError, json.JSONDecodeError) as exc:
            _check(checks, "DWG 回读", False, "成功", "失败", str(exc))
    else:
        _check(checks, "DWG 文件", False, "存在", str(dwg_path))

    report["passed"] = all(item["ok"] for item in checks)
    report["total"] = len(checks)
    report["failed"] = [item for item in checks if not item["ok"]]
    return report


def _print_report(report: dict[str, Any]) -> None:
    print("语义重建校验")
    print("=" * 104)
    for item in report["checks"]:
        mark = "OK  " if item["ok"] else "FAIL"
        expected = "" if item["expected"] is None else f"期望={item['expected']}"
        actual = "" if item["actual"] is None else f"实际={item['actual']}"
        print(f"[{mark}] {item['name']:<28} {expected} {actual}".rstrip())
    print("-" * 104)
    if report.get("source_geometry_bounds"):
        print(f"源图几何包围盒: {json.dumps(report['source_geometry_bounds'], ensure_ascii=False)}")
    if report.get("dwg_geometry_bounds"):
        print(f"DWG 几何包围盒: {json.dumps(report['dwg_geometry_bounds'], ensure_ascii=False)}")
    print(f"检查 {report['total']} 项，失败 {len(report['failed'])} 项。")
    if report["passed"]:
        print("结论：数据层与几何层全部通过。")


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
    except (AttributeError, ValueError):
        pass

    parser = argparse.ArgumentParser(description="校验桥墩语义重建的数据与几何。")
    parser.add_argument("--model", default=str(DEFAULT_MODEL))
    parser.add_argument("--dwg", default=str(DEFAULT_DWG))
    parser.add_argument("--json-out", default=str(DEFAULT_JSON))
    parser.add_argument("--tolerance", type=float, default=1.0, help="DWG 包围盒各边允许误差（图形单位）")
    parser.add_argument("--skip-dwg", action="store_true", help="只跑数据层与构件回验")
    args = parser.parse_args(argv)

    model_path = Path(args.model)
    if not model_path.exists():
        print(f"找不到模型文件: {model_path}", file=sys.stderr)
        return 2
    model_path = model_path.resolve()
    model = json.loads(model_path.read_text(encoding="utf-8"))

    dwg_path = Path(args.dwg).resolve()
    if args.skip_dwg:
        # 只跑数据层与构件回验：用不存在的路径，再把 DWG 层检查整体移除。
        report = validate(model, Path("__skip_dwg__.dwg"))
        report["checks"] = [item for item in report["checks"] if not item["name"].startswith("DWG")]
        report["dwg"] = None
        report.pop("dwg_stats", None)
        report.pop("dwg_geometry_bounds", None)
        report["passed"] = all(item["ok"] for item in report["checks"])
        report["total"] = len(report["checks"])
        report["failed"] = [item for item in report["checks"] if not item["ok"]]
    else:
        report = validate(model, dwg_path)
    report["model"] = str(model_path)

    _print_report(report)

    output = Path(args.json_out)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"已写出 {output.resolve()}")
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
