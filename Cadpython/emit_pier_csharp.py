"""把 extract_pier_model.py 的语义模型生成手写风格的 CadCli C# 文件。

用法：
    python emit_pier_csharp.py model.json -o ..\\CadCli\\Generated\\Pier\\PierBodyReinforcementDrawingSemantic.cs
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


def _f(value: float, digits: int = 3) -> str:
    text = f"{value:.{digits}f}".rstrip("0").rstrip(".")
    return text if text not in ("", "-0") else "0"


class Emitter:
    def __init__(self, model: dict[str, Any]) -> None:
        self.model = model
        self.lines: list[str] = []

    def write(self, text: str = "") -> None:
        self.lines.append(text)

    def emit(self) -> str:
        self._header()
        self._records()
        self._draw_entry()
        self._composite()
        self._detail()
        self._schedule()
        self._data_arrays()
        self.write("}")
        return "\n".join(self.lines) + "\n"

    # ------------------------------------------------------------ header
    def _header(self) -> None:
        source_labels = ", ".join(item["label"] for item in self.model["source"])
        self.write("using System;")
        self.write("using System.Collections.Generic;")
        self.write("using System.Linq;")
        self.write("using ACadSharp;")
        self.write("using ACadSharp.Entities;")
        self.write("using ACadSharp.Tables;")
        self.write("using CSMath;")
        self.write("using DwgSharpKit;")
        self.write("using DwgSharpKit.Blocks;")
        self.write("using DwgSharpKit.Drawing;")
        self.write("using DwgSharpKit.Rebar;")
        self.write("using DwgSharpKit.Standards;")
        self.write("")
        self.write("namespace CadCli.Generated.Pier;")
        self.write("")
        self.write("/// <summary>")
        self.write("/// 由三区语义模型生成的桥墩钢筋图（手写风格）：方法按区域拆分，")
        self.write("/// 大样用 Rebar 模板 + 引线落地，数量表用结构化行生成。")
        self.write(f"/// 来源：{source_labels}。")
        self.write("/// 综合图钢筋曲线目前仍按源几何记录（compact 导出的尺寸标注缺少文本与测量值，")
        self.write("/// 参数化需等更完整的导出，见 skill 的 json-to-csharp.md）。")
        self.write("/// </summary>")
        self.write("public static class PierBodyReinforcementDrawingSemantic")
        self.write("{")

    # ------------------------------------------------------------ records
    def _records(self) -> None:
        self.write("    private readonly record struct PointData(double X, double Y);")
        self.write("    private readonly record struct VertexData(double X, double Y, double Bulge);")
        self.write("    private readonly record struct LineData(string Layer, PointData Start, PointData End);")
        self.write("    private readonly record struct PolylineData(string Layer, bool Closed, VertexData[] Vertices);")
        self.write("    private readonly record struct ArcData(string Layer, PointData Center, double Radius, double StartAngle, double EndAngle);")
        self.write("    private readonly record struct CircleData(string Layer, PointData Center, double Radius);")
        self.write("    private readonly record struct TextData(string Layer, string Value, PointData Position, double Height, double Rotation, string Type);")
        self.write("    private readonly record struct RebarLabel(string Number, string Spec, PointData Position, double Rotation, double Scale);")
        self.write("    private readonly record struct SectionMark(PointData Position, double Rotation, double Scale);")
        self.write("    private readonly record struct ScheduleRow(string Number, double Diameter, double SingleLength, int Count, double TotalLength, double UnitWeight, double TotalWeight);")
        self.write("    private readonly record struct ComponentOutline(string Layer, PointData Origin, double Width, double Height, double Radius, double ChannelSpan);")
        self.write("")

    # ------------------------------------------------------------ entry
    def _draw_entry(self) -> None:
        self.write("    public static void Draw(CadDocument doc)")
        self.write("    {")
        self.write("        DrawCompositeRegion(doc);")
        self.write("        DrawDetailRegion(doc);")
        self.write("        DrawScheduleRegion(doc);")
        self.write("    }")
        self.write("")

    # ------------------------------------------------------------ composite
    def _composite(self) -> None:
        self.write("    /// <summary>综合图区：参数化构件、钢筋曲线、编号块、断面符号与文字。</summary>")
        self.write("    private static void DrawCompositeRegion(CadDocument doc)")
        self.write("    {")
        self.write("        DrawParameterizedComponents(doc);")
        self.write("")
        self.write("        var result = new List<Entity>();")
        self.write("")
        self.write("        foreach (var item in CompositeLines)")
        self.write("        {")
        self.write("            result.Add(CadDraw.Line(CadDraw.P(item.Start.X, item.Start.Y), CadDraw.P(item.End.X, item.End.Y), LayerOf(doc, item.Layer)));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var item in CompositePolylines)")
        self.write("        {")
        self.write("            result.Add(")
        self.write("                CadDraw.Polyline(")
        self.write("                    item.Vertices.Select(v => CadDraw.V(v.X, v.Y, v.Bulge)),")
        self.write("                    LayerOf(doc, item.Layer),")
        self.write("                    closed: item.Closed")
        self.write("                )")
        self.write("            );")
        self.write("        }")
        self.write("")
        self.write("        foreach (var item in CompositeArcs)")
        self.write("        {")
        self.write("            result.Add(CadDraw.Arc(CadDraw.P(item.Center.X, item.Center.Y), item.Radius, item.StartAngle, item.EndAngle, LayerOf(doc, item.Layer)));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var item in CompositeCircles)")
        self.write("        {")
        self.write("            result.Add(CadDraw.Circle(CadDraw.P(item.Center.X, item.Center.Y), item.Radius, LayerOf(doc, item.Layer)));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var item in CompositeTexts)")
        self.write("        {")
        self.write("            var layer = LayerOf(doc, item.Layer);")
        self.write("            result.Add(")
        self.write("                item.Type == \"MText\"")
        self.write("                    ? CadDraw.MText(item.Value, CadDraw.P(item.Position.X, item.Position.Y), item.Height, layer)")
        self.write("                    : CadDraw.Text(item.Value, CadDraw.P(item.Position.X, item.Position.Y), item.Height, layer, item.Rotation)")
        self.write("            );")
        self.write("        }")
        self.write("")
        self.write("        foreach (var item in CompositeLabels)")
        self.write("        {")
        self.write("            result.Add(HRB400Block.Insert(doc, CadDraw.P(item.Position.X, item.Position.Y), grade: item.Number, spec: item.Spec, scale: item.Scale, rotation: item.Rotation));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var item in CompositeSections)")
        self.write("        {")
        self.write("            result.Add(SteelSection.Insert(doc, CadDraw.P(item.Position.X, item.Position.Y), item.Scale, item.Rotation));")
        self.write("        }")
        self.write("")
        self.write("        doc.AddEntities(result);")
        self.write("    }")
        self.write("")
        self.write("    private static Layer LayerOf(CadDocument doc, string sourceLayer) => sourceLayer switch")
        self.write("    {")
        self.write("        \"粗实线\" => doc.Layer(CadLayers.B01),")
        self.write("        \"细实线\" => doc.Layer(CadLayers.B04),")
        self.write("        \"轮廓线\" => doc.Layer(CadLayers.B04),")
        self.write("        \"标注\" => doc.Layer(CadLayers.B03),")
        self.write("        \"B-03 标注\" => doc.Layer(CadLayers.B03),")
        self.write("        \"尺寸线\" => doc.Layer(CadLayers.B03),")
        self.write("        \"中心线\" => doc.Layer(CadLayers.B06),")
        self.write("        \"文本\" => doc.Layer(CadLayers.B07),")
        self.write("        _ => doc.Layer(CadLayers.B04),")
        self.write("    };")
        self.write("")
        self.write("    /// <summary>参数化构件：带上下对称凹槽的圆角矩形截面（凹槽深 = 圆角半径，槽口 45°）。</summary>")
        self.write("    private static void DrawParameterizedComponents(CadDocument doc)")
        self.write("    {")
        self.write("        foreach (var item in ComponentOutlines)")
        self.write("        {")
        self.write("            DrawChannelSection(doc, item);")
        self.write("        }")
        self.write("    }")
        self.write("")
        self.write("    private static void DrawChannelSection(CadDocument doc, ComponentOutline item)")
        self.write("    {")
        self.write("        var radius = item.Radius;")
        self.write("        var depth = radius;")
        self.write("        var center = item.Width / 2.0;")
        self.write("        var halfSpan = item.ChannelSpan / 2.0;")
        self.write("        var channelStart = center - halfSpan;")
        self.write("        var channelEnd = center + halfSpan;")
        self.write("        var outline = CadDraw.Polyline(")
        self.write("            [")
        self.write("                CadDraw.V(radius, 0),")
        self.write("                CadDraw.V(channelStart, 0),")
        self.write("                CadDraw.V(channelStart + radius, depth),")
        self.write("                CadDraw.V(channelEnd - radius, depth),")
        self.write("                CadDraw.V(channelEnd, 0),")
        self.write("                CadDraw.V(item.Width - radius, 0, QuarterArcBulge),")
        self.write("                CadDraw.V(item.Width, radius),")
        self.write("                CadDraw.V(item.Width, item.Height - radius, QuarterArcBulge),")
        self.write("                CadDraw.V(item.Width - radius, item.Height),")
        self.write("                CadDraw.V(channelEnd, item.Height),")
        self.write("                CadDraw.V(channelEnd - radius, item.Height - depth),")
        self.write("                CadDraw.V(channelStart + radius, item.Height - depth),")
        self.write("                CadDraw.V(channelStart, item.Height),")
        self.write("                CadDraw.V(radius, item.Height, QuarterArcBulge),")
        self.write("                CadDraw.V(0, item.Height - radius),")
        self.write("                CadDraw.V(0, radius, QuarterArcBulge),")
        self.write("            ],")
        self.write("            LayerOf(doc, item.Layer),")
        self.write("            closed: true")
        self.write("        );")
        self.write("        doc.AddTranslated(CadDraw.P(item.Origin.X, item.Origin.Y), outline);")
        self.write("    }")
        self.write("")

    # ------------------------------------------------------------ detail
    def _detail(self) -> None:
        self.write("    /// <summary>钢筋大样区：每根钢筋一个 Rebar 模板，原点为其起点。</summary>")
        self.write("    private static void DrawDetailRegion(CadDocument doc)")
        self.write("    {")
        self.write("        var options = new RebarDetailOptions")
        self.write("        {")
        self.write("            ShowText = false,")
        self.write("            RebarLayer = doc.Layer(CadLayers.B01),")
        self.write("            TextLayer = doc.Layer(CadLayers.B07),")
        self.write("        };")
        self.write("")
        for number, template in self.model["detail_templates"].items():
            vertices = ", ".join(
                f"RebarDetail.V({_f(vertex['x'], 6)}, {_f(vertex['y'], 6)}, bulge: {_f(vertex['bulge'], 6)})"
                if abs(vertex["bulge"]) > 1e-9
                else f"RebarDetail.V({_f(vertex['x'], 6)}, {_f(vertex['y'], 6)})"
                for vertex in template["vertices"]
            )
            substitute = (
                f'"{_f(template["true_length"], 0)}(平均)"' if template.get("average") else "null"
            )
            origin = template["origin"]
            leader = template["insert_position"]
            self.write(
                f"        DrawDetail(doc, options, \"{number}\", {_f(template['diameter'] or 0, 0)}, "
                f"{_f(template['true_length'] or 0, 3)}, {substitute},"
            )
            self.write(f"            [{vertices}],")
            self.write(
                f"            origin: new PointData({_f(origin[0], 3)}, {_f(origin[1], 3)}), "
                f"leader: new PointData({_f(leader[0], 3)}, {_f(leader[1], 3)}));"
            )
        self.write("    }")
        self.write("")
        self.write("    private static void DrawDetail(")
        self.write("        CadDocument doc,")
        self.write("        RebarDetailOptions options,")
        self.write("        string number,")
        self.write("        double diameter,")
        self.write("        double trueLength,")
        self.write("        string? substituteLength,")
        self.write("        IReadOnlyList<RebarVertex> vertices,")
        self.write("        PointData origin,")
        self.write("        PointData leader")
        self.write("    )")
        self.write("    {")
        self.write("        var rebar = new Rebar")
        self.write("        {")
        self.write("            Number = number,")
        self.write("            Grade = \"HRB400\",")
        self.write("            Diameter = diameter,")
        self.write("            Count = 1,")
        self.write("            TrueLength = trueLength,")
        self.write("            SubstituteLength = substituteLength,")
        self.write("            Vertices = vertices,")
        self.write("        };")
        self.write("")
        self.write("        doc.AddEntities(")
        self.write("            rebar.CreatePlacedDetail(")
        self.write("                doc,")
        self.write("                CadDraw.P(origin.X, origin.Y),")
        self.write("                CadDraw.P(leader.X, leader.Y),")
        self.write("                scale: 50,")
        self.write("                options")
        self.write("            )")
        self.write("        );")
        self.write("    }")
        self.write("")

    # ------------------------------------------------------------ schedule
    def _schedule(self) -> None:
        table = self.model["parameters"]["table"]
        self.write("    /// <summary>数量表区：表框、网格、表头与行数据都由结构化参数生成。</summary>")
        self.write("    private static void DrawScheduleRegion(CadDocument doc)")
        self.write("    {")
        self.write(f"        const double width = {_f(table['width'], 3)};")
        self.write(f"        const double height = {_f(table['height'], 3)};")
        self.write(f"        const double rowStep = {_f(table['row_step'], 3)};")
        self.write(f"        const double firstRowY = {_f(table['first_row_y'], 3)};")
        self.write("        var layer = doc.Layer(CadLayers.B07);")
        self.write("        var result = new List<Entity>();")
        self.write("")
        self.write("        result.Add(")
        self.write("            CadDraw.Polyline(")
        self.write("                [CadDraw.P2(0, 0), CadDraw.P2(width, 0), CadDraw.P2(width, height), CadDraw.P2(0, height)],")
        self.write("                layer,")
        self.write("                closed: true")
        self.write("            )")
        self.write("        );")
        self.write("")
        self.write("        foreach (var x in ScheduleVerticals)")
        self.write("        {")
        self.write("            result.Add(CadDraw.Line(CadDraw.P(x, 0), CadDraw.P(x, height), layer));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var y in ScheduleHorizontals)")
        self.write("        {")
        self.write("            result.Add(CadDraw.Line(CadDraw.P(0, y), CadDraw.P(width, y), layer));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var label in ScheduleHeaderLabels)")
        self.write("        {")
        self.write("            result.Add(")
        self.write("                label.Type == \"MText\"")
        self.write("                    ? CadDraw.MText(label.Value, CadDraw.P(label.X, label.Y), label.Height, layer)")
        self.write("                    : CadDraw.Text(label.Value, CadDraw.P(label.X, label.Y), label.Height, layer)")
        self.write("            );")
        self.write("        }")
        self.write("")
        self.write("        for (var index = 0; index < ScheduleRows.Length; index++)")
        self.write("        {")
        self.write("            var row = ScheduleRows[index];")
        self.write("            var y = firstRowY - index * rowStep;")
        self.write("            result.Add(CadDraw.MText(row.Number, CadDraw.P(NumberColumnX, y), 125, layer));")
        self.write("            result.Add(CadDraw.MText(row.SingleLength.ToString(\"0.###\"), CadDraw.P(SingleLengthColumnX, y), 125, layer));")
        self.write("            result.Add(CadDraw.MText(row.Count.ToString(), CadDraw.P(CountColumnX, y), 125, layer));")
        self.write("            result.Add(CadDraw.MText(row.TotalLength.ToString(\"0.###\"), CadDraw.P(TotalLengthColumnX, y), 125, layer));")
        self.write("            result.Add(CadDraw.MText(row.UnitWeight.ToString(\"0.000\"), CadDraw.P(UnitWeightColumnX, y), 125, layer));")
        self.write("            result.Add(CadDraw.MText(row.TotalWeight.ToString(\"0.###\"), CadDraw.P(TotalWeightColumnX, y), 125, layer));")
        self.write("            result.Add(HRB400Block.Insert(doc, CadDraw.P(DiameterColumnX, y), grade: \"HRB400\", spec: row.Diameter.ToString(\"0\"), scale: 5, rotation: 0));")
        self.write("        }")
        self.write("")
        self.write("        foreach (var label in ScheduleSummaryLabels)")
        self.write("        {")
        self.write("            result.Add(")
        self.write("                label.Type == \"MText\"")
        self.write("                    ? CadDraw.MText(label.Value, CadDraw.P(label.X, label.Y), label.Height, layer)")
        self.write("                    : CadDraw.Text(label.Value, CadDraw.P(label.X, label.Y), label.Height, layer)")
        self.write("            );")
        self.write("        }")
        self.write("")
        origin = table["origin"]
        self.write(f"        doc.AddTranslated(CadDraw.P({_f(origin[0], 3)}, {_f(origin[1], 3)}), result);")
        self.write("    }")
        self.write("")

        columns = table["columns"]
        self.write(f"    private const double DiameterColumnX = {_f(columns['diameter'], 3)};")
        self.write(f"    private const double NumberColumnX = {_f(columns['number'], 3)};")
        self.write(f"    private const double SingleLengthColumnX = {_f(columns['single_length'], 3)};")
        self.write(f"    private const double CountColumnX = {_f(columns['count'], 3)};")
        self.write(f"    private const double TotalLengthColumnX = {_f(columns['total_length'], 3)};")
        self.write(f"    private const double UnitWeightColumnX = {_f(columns['unit_weight'], 3)};")
        self.write(f"    private const double TotalWeightColumnX = {_f(columns['total_weight'], 3)};")
        self.write("")

    # ------------------------------------------------------------ arrays
    def _data_arrays(self) -> None:
        self.write("    private static readonly RebarLabel[] CompositeLabels =")
        self.write("    [")
        for item in self.model["composite_labels"]:
            self.write(
                f"        new(\"{item['numbers'][0]}\", \"{item['spec']}\", new({_f(item['x'])}, {_f(item['y'])}), "
                f"{_f(item['rotation'], 6)}, {_f(item['scale'], 3)}),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly SectionMark[] CompositeSections =")
        self.write("    [")
        for item in self.model["composite_sections"]:
            self.write(
                f"        new(new({_f(item['x'])}, {_f(item['y'])}), {_f(item['rotation'], 6)}, {_f(item['scale'], 3)}),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly TextData[] CompositeTexts =")
        self.write("    [")
        for item in self.model["composite_texts"]:
            text = item["text"].replace("\\", "\\\\").replace('"', '\\"')
            self.write(
                f"        new(\"{item['layer']}\", \"{text}\", new({_f(item['x'])}, {_f(item['y'])}), "
                f"{_f(item['height'], 3)}, {_f(item.get('rotation', 0.0), 6)}, \"{item.get('type', 'Text')}\"),"
            )
        self.write("    ];")
        self.write("")

        geometry = self.model["composite_geometry"]
        self.write("    private static readonly LineData[] CompositeLines =")
        self.write("    [")
        for item in geometry["lines"]:
            self.write(
                f"        new(\"{item['layer']}\", new({_f(item['x1'])}, {_f(item['y1'])}), new({_f(item['x2'])}, {_f(item['y2'])})),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly PolylineData[] CompositePolylines =")
        self.write("    [")
        for item in geometry["polylines"]:
            if "component" in item:
                continue
            vertices = ", ".join(
                f"new({_f(vertex['x'])}, {_f(vertex['y'])}, {_f(vertex['bulge'], 6)})"
                for vertex in item["vertices"]
            )
            closed = "true" if item["closed"] else "false"
            self.write(f"        new(\"{item['layer']}\", {closed}, [{vertices}]),")
        self.write("    ];")
        self.write("")

        self.write("    private const double QuarterArcBulge = 0.4142135623730951;")
        self.write("")
        self.write("    private static readonly ComponentOutline[] ComponentOutlines =")
        self.write("    [")
        for component in self.model.get("components", []):
            origin = component["origin"]
            self.write(
                f"        new(\"{component['layer']}\", new({_f(origin[0])}, {_f(origin[1])}), "
                f"{_f(component['width'], 3)}, {_f(component['height'], 3)}, "
                f"{_f(component['radius'], 3)}, {_f(component['channel_span'], 3)}),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly ArcData[] CompositeArcs =")
        self.write("    [")
        for item in geometry["arcs"]:
            self.write(
                f"        new(\"{item['layer']}\", new({_f(item['x'])}, {_f(item['y'])}), {_f(item['radius'], 3)}, "
                f"{_f(item['start_angle'], 6)}, {_f(item['end_angle'], 6)}),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly CircleData[] CompositeCircles =")
        self.write("    [")
        for item in geometry["circles"]:
            self.write(
                f"        new(\"{item['layer']}\", new({_f(item['x'])}, {_f(item['y'])}), {_f(item['radius'], 3)}),"
            )
        self.write("    ];")
        self.write("")

        table = self.model["parameters"]["table"]
        self.write("    private static readonly double[] ScheduleVerticals =")
        self.write("    [")
        self.write("        " + ", ".join(_f(value, 3) for value in table["verticals"]))
        self.write("    ];")
        self.write("")
        self.write("    private static readonly double[] ScheduleHorizontals =")
        self.write("    [")
        self.write("        " + ", ".join(_f(value, 3) for value in table["horizontals"]))
        self.write("    ];")
        self.write("")

        self.write("    private static readonly ScheduleCell[] ScheduleHeaderLabels =")
        self.write("    [")
        for item in table["header_labels"]:
            text = item["value"].replace("\\", "\\\\").replace('"', '\\"')
            self.write(
                f"        new(\"{text}\", {_f(item['x'])}, {_f(item['y'])}, {_f(item['height'], 3)}, \"{item['type']}\"),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly ScheduleCell[] ScheduleSummaryLabels =")
        self.write("    [")
        for item in table["summary_labels"]:
            text = item["value"].replace("\\", "\\\\").replace('"', '\\"')
            self.write(
                f"        new(\"{text}\", {_f(item['x'])}, {_f(item['y'])}, {_f(item['height'], 3)}, \"{item['type']}\"),"
            )
        self.write("    ];")
        self.write("")

        self.write("    private static readonly ScheduleRow[] ScheduleRows =")
        self.write("    [")
        for row in self.model["schedule_rows"]:
            count = int(row["count"] or 0)
            self.write(
                f"        new(\"{row['number']}\", {_f(row['diameter'] or 0, 0)}, {_f(row['single_length'] or 0, 3)}, "
                f"{count}, {_f(row['total_length'] or 0, 3)}, {_f(row['unit_weight'] or 0, 3)}, {_f(row['total_weight'] or 0, 3)}),"
            )
        self.write("    ];")
        self.write("")
        self.write("    private readonly record struct ScheduleCell(string Value, double X, double Y, double Height, string Type);")


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # type: ignore[attr-defined]
    except (AttributeError, ValueError):
        pass

    parser = argparse.ArgumentParser(description="把桥墩语义模型生成为手写风格 C#。")
    parser.add_argument("model", nargs="?", default="pier_body_model.json")
    parser.add_argument(
        "-o",
        "--output",
        default=str(Path(__file__).resolve().parents[1] / "CadCli/Generated/Pier/PierBodyReinforcementDrawingSemantic.cs"),
    )
    args = parser.parse_args(argv)

    model_path = Path(args.model)
    if not model_path.exists():
        print(f"找不到模型文件: {model_path}", file=sys.stderr)
        return 2
    model = json.loads(model_path.read_text(encoding="utf-8"))
    code = Emitter(model).emit()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(code, encoding="utf-8")
    print(f"已写出 {output.resolve()}（{len(code.splitlines())} 行）")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
