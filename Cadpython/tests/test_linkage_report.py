"""linkage_report 的单元测试与桥墩三区回归（标准库 unittest）。"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import linkage_report as linkage  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parents[2]
PIER_FILES = [
    REPO_ROOT / "pier_shaft_general_compact.json",
    REPO_ROOT / "pier_shaft_rebar_detail_compact.json",
    REPO_ROOT / "pier_shaft_rebar_table_compact.json",
]


class SpecParsingTests(unittest.TestCase):
    def test_h_formula(self):
        spec = linkage._parse_spec("28 L=H+1414")
        self.assertIsNotNone(spec)
        self.assertEqual(spec.diameter, 28)
        self.assertEqual(spec.formula, "H+1414")
        self.assertAlmostEqual(spec.evaluate(10500), 11914)

    def test_minus_formula(self):
        spec = linkage._parse_spec("28 L=H-100")
        self.assertAlmostEqual(spec.evaluate(10500), 10400)

    def test_fixed_length_and_average(self):
        spec = linkage._parse_spec("12 L=2461(平均)")
        self.assertTrue(spec.average)
        self.assertEqual(spec.diameter, 12)
        self.assertEqual(spec.evaluate(10500), 2461)

    def test_diameter_only(self):
        spec = linkage._parse_spec("14")
        self.assertEqual(spec.diameter, 14)
        self.assertIsNone(spec.formula)
        self.assertIsNone(spec.evaluate(10500))

    def test_cleaning_mtext_break(self):
        spec = linkage._parse_spec("28 L=H+1175（均）")
        self.assertTrue(spec.average)
        self.assertEqual(spec.formula, "H+1175")


class NumberExpansionTests(unittest.TestCase):
    def test_range_expansion(self):
        self.assertEqual(linkage._expand_number("N2-1～2"), ["N2-1", "N2-2"])
        self.assertEqual(linkage._expand_number("N3-1~2"), ["N3-1", "N3-2"])

    def test_plain_number(self):
        self.assertEqual(linkage._expand_number("N1-1"), ["N1-1"])
        self.assertEqual(linkage._expand_number("墩号"), [])


@unittest.skipUnless(all(path.exists() for path in PIER_FILES), "桥墩三区导出文件不在仓库根目录")
class PierLinkageRegressionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        regions = linkage.load_regions(PIER_FILES)
        cls.report = linkage.build_report(regions)
        cls.rows = {row["number"]: row for row in cls.report["rows"]}

    def test_roles_detected(self):
        regions = linkage.load_regions(PIER_FILES)
        self.assertEqual(
            sorted(region.role for region in regions), ["composite", "detail", "schedule"]
        )

    def test_height_from_table_header(self):
        self.assertEqual(self.report["height"], 10500)

    def test_all_numbers_link_across_three_regions(self):
        counts = self.report["counts"]
        self.assertEqual(counts["schedule"], 14)
        self.assertEqual(counts["composite"], 14)
        self.assertEqual(counts["detail_expanded"], 14)
        self.assertEqual(self.report["number_sets"]["detail_only"], [])
        self.assertEqual(self.report["number_sets"]["schedule_only"], [])

    def test_no_mismatches(self):
        self.assertEqual(self.report["mismatches"], [])

    def test_h_formula_rows_match_schedule(self):
        for number in ("N1-1", "N1-2", "N1-5"):
            row = self.rows[number]
            self.assertEqual(row["detail_formula"][:1], "H")
            self.assertAlmostEqual(
                row["evaluated_single_length"], row["schedule"]["single_length"], places=1
            )

    def test_quantity_comes_from_schedule_not_composite(self):
        row = self.rows["N1-1"]
        self.assertEqual(row["schedule"]["count"], 92)
        self.assertEqual(row["composite_placements"], 26)

    def test_average_length_rows_are_warnings_not_errors(self):
        warned = {item["number"] for item in self.report["warnings"]}
        self.assertTrue({"N1-2", "N1-6", "N4-2", "N5-2"} <= warned)


if __name__ == "__main__":
    unittest.main()
