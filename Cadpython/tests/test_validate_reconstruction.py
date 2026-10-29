"""validate_reconstruction 的纯函数回归测试（不启动 dotnet）。"""

from __future__ import annotations

import json
import unittest
from pathlib import Path

import extract_pier_model
import validate_reconstruction

REPO_ROOT = Path(__file__).resolve().parents[2]
MODEL = REPO_ROOT / "pier_body_model.json"


class GeometryHelperTests(unittest.TestCase):
    def test_straight_segment(self) -> None:
        points = validate_reconstruction._bulge_points(0, 0, 0, 10, 0)
        self.assertEqual(points, [(0, 0), (10, 0)])

    def test_semicircle_midpoint(self) -> None:
        points = validate_reconstruction._bulge_points(0, 0, 1.0, 1, 0, steps=2)
        middle = points[1]
        self.assertAlmostEqual(middle[0], 0.5, places=6)
        self.assertAlmostEqual(middle[1], -0.5, places=6)

    def test_quarter_arc_midpoint(self) -> None:
        points = validate_reconstruction._bulge_points(
            0, 0, extract_pier_model.QUARTER_ARC_BULGE, 1, 0, steps=2
        )
        middle = points[1]
        self.assertAlmostEqual(middle[0], 0.5, places=6)
        self.assertAlmostEqual(middle[1], 0.5 - 2 ** 0.5 / 2, places=6)

    def test_arc_points_bbox(self) -> None:
        points = validate_reconstruction._arc_points(0, 0, 10, 0, 3.141592653589793)
        xs = [x for x, _ in points]
        ys = [y for _, y in points]
        self.assertAlmostEqual(max(xs), 10, places=6)
        self.assertAlmostEqual(min(ys), 0, places=6)
        self.assertAlmostEqual(max(ys), 10, places=6)


class ModelValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.model = json.loads(MODEL.read_text(encoding="utf-8"))

    def test_expected_entity_counts(self) -> None:
        counts = validate_reconstruction.expected_entity_counts(self.model)
        self.assertEqual(
            counts,
            {
                "Line": 612,
                "LwPolyline": 180,
                "Insert": 398,
                "Arc": 6,
                "Circle": 9,
                "MText": 105,
                "TextEntity": 21,
            },
        )
        self.assertEqual(sum(counts.values()), 1331)

    def test_source_geometry_bounds(self) -> None:
        bounds = validate_reconstruction.source_geometry_bounds(self.model)
        self.assertAlmostEqual(bounds["minX"], -67177.93, places=2)
        self.assertAlmostEqual(bounds["minY"], -29414.744, places=2)
        self.assertAlmostEqual(bounds["maxX"], 16619.194, places=2)
        self.assertAlmostEqual(bounds["maxY"], 10140.653, places=2)

    def test_component_roundtrip(self) -> None:
        marked = [
            (item, item["component"])
            for item in self.model["composite_geometry"]["polylines"]
            if "component" in item
        ]
        self.assertEqual(len(marked), 3)
        for item, index in marked:
            component = self.model["components"][index]
            canonical = extract_pier_model._channel_section_vertices(
                component["width"], component["height"], component["radius"], component["channel_span"]
            )
            offset = [
                {
                    "x": vertex["x"] + component["origin"][0],
                    "y": vertex["y"] + component["origin"][1],
                    "bulge": vertex["bulge"],
                }
                for vertex in canonical
            ]
            simplified = extract_pier_model._simplify_closed(item["vertices"])
            error = extract_pier_model._sequence_error(offset, simplified)
            self.assertLessEqual(error, extract_pier_model.FIT_TOLERANCE)

    def test_schedule_consistency(self) -> None:
        for row in self.model["schedule_rows"]:
            count = int(row["count"] or 0)
            single = float(row["single_length"] or 0)
            unit = float(row["unit_weight"] or 0)
            expected_length = round(count * single / 1000.0, 1)
            expected_weight = round(count * single / 1000.0 * unit, 1)
            self.assertAlmostEqual(float(row["total_length"]), expected_length, delta=0.15)
            self.assertAlmostEqual(float(row["total_weight"]), expected_weight, delta=0.15)


if __name__ == "__main__":
    unittest.main()
