"""extract_pier_model / emit_pier_csharp 的回归测试。"""

from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

import emit_pier_csharp
import extract_pier_model

REPO_ROOT = Path(__file__).resolve().parents[2]
GENERAL = REPO_ROOT / "pier_shaft_general_compact.json"
DETAIL = REPO_ROOT / "pier_shaft_rebar_detail_compact.json"
TABLE = REPO_ROOT / "pier_shaft_rebar_table_compact.json"
MODEL = REPO_ROOT / "pier_body_model.json"
GENERATED = REPO_ROOT / "CadCli" / "Generated" / "Pier" / "PierBodyReinforcementDrawingSemantic.cs"


class PierModelExtractionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.model = extract_pier_model.build_model([GENERAL, DETAIL, TABLE], report=False)

    def test_parameters(self) -> None:
        table = self.model["parameters"]["table"]
        self.assertEqual(self.model["parameters"]["pier_height"], 10500)
        self.assertAlmostEqual(table["width"], 6500, places=3)
        self.assertAlmostEqual(table["height"], 6200, places=0)
        self.assertAlmostEqual(table["row_step"], 316.667, places=3)
        self.assertAlmostEqual(table["first_row_y"], 4928.788, places=2)

    def test_detail_templates(self) -> None:
        templates = self.model["detail_templates"]
        self.assertEqual(len(templates), 13)
        self.assertNotIn("N1-6", templates)  # 大样由线组合绘制，未找到单条多段线
        self.assertEqual(templates["N1-1"]["true_length"], 11914)
        self.assertEqual(templates["N1-2"]["true_length"], 11675)
        self.assertTrue(templates["N1-2"]["average"])
        self.assertEqual(len(templates["N2-1"]["vertices"]), 8)
        self.assertEqual(templates["N2-1"]["diameter"], 14)
        for template in templates.values():
            self.assertGreaterEqual(len(template["vertices"]), 2)
            self.assertIn(template["confidence"], {"high", "medium", "low"})

    def test_placements_and_labels(self) -> None:
        placements = self.model["placements"]
        self.assertEqual(sum(len(items) for items in placements.values()), 83)
        self.assertEqual(len(placements["N1-1"]), 26)
        self.assertEqual(len(placements["N1-2"]), 11)
        view_ids = {view["id"] for view in self.model["composite_views"]}
        for items in placements.values():
            for item in items:
                self.assertIn(item["view"], view_ids)

    def test_composite_geometry(self) -> None:
        geometry = self.model["composite_geometry"]
        self.assertEqual(len(geometry["lines"]), 588)
        self.assertEqual(len(geometry["polylines"]), 153)
        self.assertEqual(len(geometry["arcs"]), 6)
        self.assertEqual(len(geometry["circles"]), 9)
        self.assertEqual(len(self.model["composite_labels"]), 83)
        self.assertEqual(len(self.model["composite_sections"]), 288)
        self.assertEqual(len(self.model["composite_texts"]), 20)
        self.assertEqual(self.model["composite_excluded_noise"], 2)

    def test_parameterized_components(self) -> None:
        components = self.model["components"]
        self.assertEqual(len(components), 3)
        sizes = sorted((component["width"], component["height"]) for component in components)
        self.assertEqual([(round(w), round(h)) for w, h in sizes], [(2800, 2200), (3592, 2350), (5000, 2600)])
        for component in components:
            self.assertEqual(component["type"], "channel_section")
            self.assertAlmostEqual(component["radius"], 200, places=3)
            self.assertLessEqual(component["fit_error"], extract_pier_model.FIT_TOLERANCE)
        marked = [item for item in self.model["composite_geometry"]["polylines"] if "component" in item]
        self.assertEqual(len(marked), 3)

    def test_schedule_rows_match_linkage(self) -> None:
        rows = {row["number"]: row for row in self.model["schedule_rows"]}
        self.assertEqual(len(rows), 14)
        self.assertEqual(rows["N1-1"]["count"], 92)
        self.assertAlmostEqual(rows["N1-1"]["total_length"], 1096.1, places=1)
        self.assertEqual(rows["N5-1"]["count"], 378)
        self.assertAlmostEqual(rows["N1-1"]["unit_weight"], 4.83, places=2)

    def test_warnings(self) -> None:
        kinds = {item["kind"] for item in self.model["warnings"]}
        self.assertIn("missing_template", kinds)
        self.assertIn("revision_mismatch", kinds)
        missing = [item for item in self.model["warnings"] if item["kind"] == "missing_template"]
        self.assertEqual([item["number"] for item in missing], ["N1-6"])


class EmitterTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.model = json.loads(MODEL.read_text(encoding="utf-8"))
        cls.code = emit_pier_csharp.Emitter(cls.model).emit()

    def test_structure(self) -> None:
        self.assertIn("public static class PierBodyReinforcementDrawingSemantic", self.code)
        self.assertIn("DrawCompositeRegion(doc)", self.code)
        self.assertIn("DrawDetailRegion(doc)", self.code)
        self.assertIn("DrawScheduleRegion(doc)", self.code)
        self.assertIn("DrawParameterizedComponents(doc)", self.code)
        self.assertIn("private static void DrawChannelSection", self.code)
        outline_block = self.code.split("ComponentOutlines =", 1)[1].split("];", 1)[0]
        self.assertEqual(outline_block.count("new(\"细实线\""), len(self.model["components"]))
        self.assertEqual(self.code.count("RebarDetail.V("), sum(
            len(template["vertices"]) for template in self.model["detail_templates"].values()
        ))
        self.assertEqual(self.code.count("DrawDetail(doc, options"), 13)
        self.assertIn('"N1-1"', self.code)
        self.assertIn("11914", self.code)
        self.assertGreater(len(self.code.splitlines()), 1000)

    def test_checked_in_file_is_in_sync(self) -> None:
        self.assertTrue(GENERATED.exists(), f"缺少生成文件: {GENERATED}")
        checked_in = GENERATED.read_text(encoding="utf-8")
        self.assertEqual(checked_in, self.code, "生成文件与当前模型不一致，请重跑 emit_pier_csharp.py")


class CliTests(unittest.TestCase):
    def test_extract_cli_writes_model(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            output = Path(tmp) / "model.json"
            status = extract_pier_model.main([
                str(GENERAL), str(DETAIL), str(TABLE), "-o", str(output), "--strict",
            ])
            self.assertEqual(status, 1)  # N1-6 等提示存在时 strict 返回非零
            self.assertTrue(output.exists())
            model = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(len(model["detail_templates"]), 13)


if __name__ == "__main__":
    unittest.main()
