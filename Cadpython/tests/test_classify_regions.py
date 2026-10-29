"""classify_regions 的回归与单元测试（标准库 unittest，无第三方依赖）。

运行：
    cd Cadpython
    .\\.venv\\Scripts\\python.exe -m unittest discover -s tests -v
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import classify_regions as classify  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parents[2]
PIER_FILES = [
    REPO_ROOT / "pier_shaft_general_compact.json",
    REPO_ROOT / "pier_shaft_rebar_detail_compact.json",
    REPO_ROOT / "pier_shaft_rebar_table_compact.json",
]


def _entity(kind: str = "Line", bbox=(0.0, 0.0, 10.0, 10.0), **kwargs):
    geometry = kwargs.pop("geometry", {})
    text = kwargs.pop("text", "")
    height = kwargs.pop("text_height", None)
    raw = kwargs.pop("raw", {"handle": "H", "type": kind})
    return classify.Entity(
        handle=kwargs.pop("handle", "H"),
        kind=kind,
        layer=kwargs.pop("layer", ""),
        text=text,
        text_height=height,
        geometry=geometry,
        bbox=bbox,
        raw=raw,
        source_file=kwargs.pop("source_file", "test.json"),
        source_label=kwargs.pop("source_label", ""),
        entity_id=kwargs.pop("entity_id", 0),
        **kwargs,
    )


class GeometryTests(unittest.TestCase):
    def test_arc_bbox_uses_real_sweep_not_full_circle(self):
        # radius 25250 arc sweeping 0..0.1 rad: bbox must stay near the chord,
        # not become a 50500-wide box.
        import math

        box = classify._entity_bbox(
            "Arc",
            {"center": (0.0, 0.0), "radius": 25250.0, "start_angle": 0.0, "end_angle": 0.1},
            "",
            None,
        )
        self.assertIsNotNone(box)
        xmin, ymin, xmax, ymax = box
        self.assertAlmostEqual(xmin, 25250 * math.cos(0.1), places=3)
        self.assertAlmostEqual(ymax, 25250 * math.sin(0.1), places=3)
        self.assertLess(xmax - xmin, 500)

    def test_polyline_bulge_expands_to_semicircle(self):
        # bulge=1 on a 2-unit chord is a semicircle below the chord (AutoCAD CCW).
        box = classify._entity_bbox(
            "LwPolyline",
            {"vertices": [{"x": 0.0, "y": 0.0, "bulge": 1.0}, {"x": 2.0, "y": 0.0, "bulge": 0.0}]},
            "",
            None,
        )
        self.assertIsNotNone(box)
        xmin, ymin, xmax, ymax = box
        self.assertAlmostEqual(ymin, -1.0, places=6)
        self.assertAlmostEqual(ymax, 0.0, places=6)
        self.assertAlmostEqual(xmax - xmin, 2.0, places=6)

    def test_polyline_bulge_negative_flips_side(self):
        box = classify._entity_bbox(
            "LwPolyline",
            {"vertices": [{"x": 0.0, "y": 0.0, "bulge": -1.0}, {"x": 2.0, "y": 0.0, "bulge": 0.0}]},
            "",
            None,
        )
        self.assertIsNotNone(box)
        self.assertAlmostEqual(box[1], 0.0, places=6)
        self.assertAlmostEqual(box[3], 1.0, places=6)

    def test_axis_rect_accepts_rectangle_and_rejects_skew(self):
        rect = _entity(
            "LwPolyline",
            geometry={
                "closed": True,
                "vertices": [
                    {"x": 0.0, "y": 0.0},
                    {"x": 100.0, "y": 0.0},
                    {"x": 100.0, "y": 50.0},
                    {"x": 0.0, "y": 50.0},
                ],
            },
        )
        self.assertEqual(classify._axis_rect(rect), (0.0, 0.0, 100.0, 50.0))

        skewed = _entity(
            "LwPolyline",
            geometry={
                "closed": True,
                "vertices": [
                    {"x": 0.0, "y": 0.0},
                    {"x": 100.0, "y": 10.0},
                    {"x": 100.0, "y": 50.0},
                    {"x": 0.0, "y": 50.0},
                ],
            },
        )
        self.assertIsNone(classify._axis_rect(skewed))

    def test_text_bbox_width_scales_with_height(self):
        box = classify._entity_bbox(
            "Text", {"position": (0.0, 0.0), "height": 250.0}, "墩身钢筋数量表", 250.0
        )
        self.assertIsNotNone(box)
        self.assertAlmostEqual(box[1], 0.0)
        self.assertAlmostEqual(box[3], 250.0)
        self.assertGreater(box[2] - box[0], 6 * 250)


class ClassificationTests(unittest.TestCase):
    def test_keyword_priority(self):
        self.assertEqual(classify._keyword_kind("墩身钢筋数量表"), "schedule")
        self.assertEqual(classify._keyword_kind("钢筋大样图"), "detail")
        self.assertEqual(classify._keyword_kind("侧面钢筋布置图"), "composite")
        self.assertEqual(classify._keyword_kind("Ⅰ-Ⅰ截面"), "composite")
        self.assertIsNone(classify._keyword_kind("1:50"))

    def test_whitespace_clustering_splits_distant_views(self):
        left = _entity(bbox=(0.0, 0.0, 100.0, 100.0))
        right = _entity(bbox=(5000.0, 0.0, 5100.0, 100.0))
        clusters, unassigned = classify.cluster_entities([left, right], padding=500.0)
        self.assertEqual(len(clusters), 2)
        self.assertEqual(unassigned, [])

    def test_isolated_entity_is_noise(self):
        lone = _entity("Insert", bbox=(0.0, 0.0, 10.0, 10.0))
        view = classify.Region(entities=[lone], bbox=(0.0, 0.0, 10.0, 10.0))
        self.assertTrue(classify._is_noise(view, median_height=250.0))

        with_text = _entity("Text", bbox=(0.0, 0.0, 100.0, 20.0), text="说明", text_height=20.0)
        self.assertFalse(
            classify._is_noise(
                classify.Region(entities=[with_text], bbox=(0.0, 0.0, 100.0, 20.0)), 250.0
            )
        )

    def test_merge_only_joins_same_kind(self):
        detail_a = classify.Region(entities=[_entity()], bbox=(0.0, 0.0, 100.0, 100.0))
        detail_a.kind = "detail"
        detail_b = classify.Region(entities=[_entity()], bbox=(300.0, 0.0, 400.0, 100.0))
        detail_b.kind = "detail"
        composite = classify.Region(entities=[_entity()], bbox=(0.0, 200.0, 100.0, 300.0))
        composite.kind = "composite"
        groups = classify._merge_groups([detail_a, detail_b, composite], merge_padding=500.0)
        sizes = sorted(len(group) for group in groups)
        self.assertEqual(sizes, [1, 2])

    def test_aggregate_section_scores_take_max(self):
        first = classify.Region(entities=[_entity()], bbox=(0, 0, 1, 1))
        first.evidence = {"grid": {"score": 0.3, "horizontal": 2}}
        second = classify.Region(entities=[_entity()], bbox=(0, 0, 1, 1))
        second.evidence = {"grid": {"score": 0.9, "horizontal": 5}}
        totals = classify._aggregate_section([first, second], "grid")
        self.assertEqual(totals["score"], 0.9)
        self.assertEqual(totals["horizontal"], 7)


@unittest.skipUnless(all(path.exists() for path in PIER_FILES), "桥墩三区导出文件不在仓库根目录")
class PierRegressionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.entities, _ = classify.load_inputs(PIER_FILES)
        cls.padding = classify.auto_padding(cls.entities)
        cls.merge_padding = classify.auto_merge_padding(cls.entities)
        cls.regions, cls.views, cls.unassigned, cls.noise = classify.build_regions(
            cls.entities, cls.padding, cls.merge_padding
        )

    def test_detects_all_three_region_kinds(self):
        kinds = {region.kind for region in self.regions}
        self.assertIn("composite", kinds)
        self.assertIn("detail", kinds)
        self.assertIn("schedule", kinds)

    def test_schedule_region_matches_manual_split(self):
        schedule = [region for region in self.regions if region.kind == "schedule"]
        self.assertEqual(len(schedule), 1)
        self.assertEqual(schedule[0].entity_count, 148)
        self.assertEqual(schedule[0].dominant_label, "桥墩墩身钢筋图-墩身钢筋数量表")
        self.assertGreater(len(schedule[0].numbers), 10)

    def test_detail_region_majority_matches_manual_split(self):
        detail_labels = [
            entity.source_label
            for region in self.regions
            if region.kind == "detail"
            for entity in region.entities
        ]
        self.assertGreater(len(detail_labels), 100)
        manual = sum(
            1 for label in detail_labels if label == "桥墩墩身钢筋图-钢筋大样图"
        )
        self.assertGreaterEqual(manual / len(detail_labels), 0.8)

    def test_composite_region_keeps_majority_entities(self):
        composite_labels = [
            entity.source_label
            for region in self.regions
            if region.kind == "composite"
            for entity in region.entities
        ]
        manual = sum(
            1 for label in composite_labels if label == "桥墩墩身钢筋图-综合图"
        )
        self.assertGreaterEqual(manual / len(composite_labels), 0.8)

    def test_no_entity_is_classified_twice(self):
        seen: set[int] = set()
        for region in self.regions:
            for entity in region.entities:
                identity = id(entity)
                self.assertNotIn(identity, seen, f"{entity.handle} 被重复归类")
                seen.add(identity)

    def test_every_entity_accounted_for(self):
        classified = sum(region.entity_count for region in self.regions)
        total = len(self.entities)
        self.assertEqual(classified + len(self.unassigned) + len(self.noise), total)


if __name__ == "__main__":
    unittest.main()
