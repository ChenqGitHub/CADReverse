---
name: cad-drawing-reverse-engineering
description: Convert AutoCAD region JSON into maintainable C# CAD drawing code by inspecting project APIs, reconstructing geometry and semantics, parameterizing verified repetition, merging safely connected segments, and validating both compilation and generated DWG output.
metadata:
  short-description: Reverse-engineer CAD JSON into project-native C# drawing code
---

# CAD Drawing Reverse Engineering

## Purpose

Use this skill when a user provides a CAD-region JSON export and asks to recreate, extend, or repair a drawing in an existing C# CAD/DWG project.

The goal is not merely to copy entities. Reconstruct the drawing as project-native code that preserves geometry, layers, repeated patterns, engineering meaning, and the user's requested file and method boundaries.

This skill is project-local and must adapt to the actual repository. Do not assume a particular drawing name, C# file name, JSON entity order, or CAD API.

## Inputs

Required inputs:

- The current `cad-region.json` or equivalent JSON export.
- The user-provided target C# file, class, or method boundary.
- The repository's CAD construction APIs and generation entry point.

Before editing, inspect all three. If the JSON has `entity_count == 0`, no entities, missing geometry, or an unexpected document, stop the conversion and report that the input is not usable. Do not invent coordinates from an earlier scan.

## Outputs

Produce the smallest scoped change that satisfies the request:

- C# methods and entities in the user-specified target file.
- Reuse of existing drawing APIs, layers, blocks, and generation conventions.
- Parameterization only where repeated geometry is demonstrably regular.
- A compilation result, and a clear distinction between code validation and DWG visual validation.

Do not rewrite unrelated user changes or replace the user's existing drawing methods unless explicitly requested.

## Evidence Levels

Separate conclusions into two categories:

- **Verified**: directly supported by the current JSON schema, current source code, current generated drawings, build output, or an observed correction in the work history.
- **【待验证】**: a reasonable engineering hypothesis that has not been confirmed by the current repository or by comparing a generated DWG with the source drawing.

Never present a plausible geometric interpretation as verified merely because it looks conventional.

## Workflow

### 1. Inspect the Repository Before Generating Code

Search for the target file and related code using repository search. Read:

- the target class and neighboring generated drawing classes;
- the project entry point that creates `CadDocument`, initializes CAD standards, invokes drawing methods, and writes DWG;
- `CadDraw` partial classes and any geometry helpers;
- layer, block, text, dimension, and steel/rebar helpers;
- existing generated drawings that use the same kind of geometry;
- `git status`, relevant diff, and recent history when available.

The existing API is authoritative. Do not invent overloads or assume that a model type still exists just because an older file or example used it.

### 2. Validate and Parse the JSON

Read the entire JSON structurally. Record:

- `schema_version`, source document, layout, and export time;
- `entity_count` and actual entity count;
- `entities` and their `object_name` values;
- layers, visibility, color, line type, line weight, and scale;
- geometry fields for each entity;
- `repetition_candidates` and `unsupported_object_names`.

Observed project exports contain fields such as `AcDbLine`, `AcDbPolyline`, and `AcDbCircle`. The workflow must remain open to other object names. Treat unsupported objects as explicit omissions, not as lines or circles by default.

Build an internal geometry table before writing C#. Preserve source entity ids for traceability during analysis, but do not assume ids are ordered geometrically.

### 3. Build a Geometry Model

Normalize coordinates only for analysis, not silently for output:

- Line: start point and end point.
- Polyline: ordered vertices, bulge per vertex, closed flag, elevation, and constant width.
- Circle: center and radius.
- Arc or other supported types: preserve center, radius, angles, and direction.

Keep the original coordinates and a separately normalized comparison representation. Very small Z values and scan noise may be treated as planar noise only after confirming the drawing is 2D.

### 4. Infer Geometric Relationships

Analyze relationships independently of JSON order:

- endpoint coincidence;
- collinearity and direction;
- parallel or perpendicular lines;
- closed loops and open contours;
- mirrored coordinates and symmetry axes;
- equal spacing and translation vectors;
- repeated rows and columns;
- circles aligned with steel or boundary lines;
- polylines whose bulge values encode rounded corners or arcs.

A relationship is strong when several signals agree: same layer, compatible orientation, shared endpoints, consistent spacing, matching lengths, and coherent location in the drawing. A single coincident coordinate is not enough to infer engineering meaning.

### 5. Merge Connected Line Segments Safely

The relevant relation is “any endpoint of segment A equals any endpoint of segment B”, not only `A.End == B.Start`.

For every candidate pair, compare all four combinations:

- A.Start to B.Start;
- A.Start to B.End;
- A.End to B.Start;
- A.End to B.End.

If a pair matches, reverse one segment as needed and append the non-shared endpoint. A chain can therefore be formed as `P1 -> P2 -> P3` even when the source orientations are `P1 -> P2` and `P3 -> P2`.

Use a documented geometric tolerance for floating-point comparison. The repository contains local `1e-9` checks in boundary and length helpers, but it does not provide a verified, reusable endpoint-merging tolerance. Therefore the exact tolerance for a new merger is **【待验证】** and must be chosen relative to drawing units and scan noise, then checked against the source drawing.

When building chains:

1. Treat each source segment as an unused edge.
2. Start with a segment and extend from either end by finding one unused segment with a matching endpoint.
3. Reverse the candidate when its matching endpoint is its end rather than its start.
4. Continue until no unique continuation exists.
5. Mark a chain closed only when its final endpoint matches its first endpoint within tolerance.
6. Preserve branches as separate polylines unless the requested drawing semantics explicitly require a branched representation.
7. Do not merge merely because lines are close, parallel, collinear, or on the same layer.

The current project has manually created merged polylines in generated drawing methods, but no generic graph-based merge helper. Treat automatic branching resolution, ambiguous matches, and tolerance selection as **【待验证】** until implemented and visually checked.

### 6. Preserve Existing Polylines and Bulges

An input polyline is not automatically a set of straight lines. Preserve its vertex order, closed flag, and bulge values.

For project-native code, inspect the available `CadDraw.Polyline` overloads:

- `IEnumerable<XY>` creates a straight-segment `LwPolyline`.
- `IEnumerable<CadPolylineVertex>` preserves bulge and width data.
- `CadDraw.V(x, y, bulge: ...)` creates a bulge-aware vertex.
- `closed: true` is required when the source says the polyline is closed.

Do not replace an arc/bulge with a visually similar straight corner. If the source represents a true `Arc`, use the project's `CadDraw.Arc` API when the target semantics call for an `Arc` entity instead of a bulged polyline.

The sign and direction of bulge depend on vertex order. If a contour is reconstructed in reverse, reverse the vertex order and negate the corresponding bulges as required by the CAD representation, then verify visually. The exact conversion of arbitrary arc formats not observed in the current export is **【待验证】**.

### 7. Infer Engineering Semantics Conservatively

Map geometry to engineering meaning using this evidence order:

1. Layer and line type conventions.
2. Position inside a known view or contour.
3. Repetition, spacing, and symmetry.
4. Relationship to boundaries, centerlines, and other entities.
5. User-provided view name and requested method purpose.
6. Existing project conventions.

In this project, `CadLayers` defines meanings such as B-01 structural/rebar/thick lines, B-03 annotations, B-04 fine structural/rebar-outline lines, B-06 center lines, and B-07 text. Use `doc.Layer(CadLayers.Bxx)` rather than hard-coded layer names.

Do not infer “main rebar”, “stirrup”, “pier outline”, or “bearing pedestal” from shape alone when the layer and spatial context do not support it. Ask or mark the interpretation **【待验证】** if it changes the output materially.

### 8. Detect Real Repetition Before Parameterizing

Parameterize only when the source entities demonstrate a stable rule:

- equal translation vector;
- equal orientation and length/radius;
- consistent count and endpoints;
- regular row/column structure;
- explicit `repetition_candidates` support;
- symmetry or mirror relation confirmed by coordinates.

Derive and record start point, direction, spacing, count, and boundary behavior. Do not infer count by rounding a range unless it reproduces all source entities.

Use project helpers when they express the detected rule:

- `CadDraw.EntityArray` for translation arrays;
- `CadDraw.EntityArrayAlongPolyline` for path arrays, noting that its current implementation rejects bulged path segments;
- `CadDraw.LinesBetweenBoundaries` and `LinesBetweenSideBoundaries` for boundary-derived line arrays;
- ordinary loops when the pattern is simple and a helper would obscure intent.

Do not parameterize irregular geometry, semantically distinct entities, or a pattern that has not been visually verified. Code reduction alone is not evidence of a real CAD pattern.

### 9. Convert Entities Using Existing APIs

Use the repository's actual APIs after inspecting their current signatures:

| Source geometry | Project-native construction |
|---|---|
| 3D point | `CadDraw.P(x, y, z)` |
| 2D point | `CadDraw.P2(x, y)` |
| straight line | `CadDraw.Line(start, end, layer)` or a two-point `CadDraw.Polyline` when the user requires polylines |
| straight polyline | `CadDraw.Polyline(points, layer, closed: ...)` |
| bulged polyline | `CadDraw.Polyline(vertices, layer, closed: ...)` with `CadDraw.V` |
| circle | `CadDraw.Circle(center, radius, layer)` |
| arc | `CadDraw.Arc(center, radius, startAngle, endAngle, layer)` |
| repeated entity | `CadDraw.EntityArray`, an existing boundary helper, or a verified loop |
| steel section marker | `SteelSection.Insert(doc, point, scale, rotation)` after confirming the requested symbol semantics |

Add entities with the local convention (`doc.Entities.Add`, `AddRange`, `AddTranslated`, or `AddTransformed`) after inspecting neighboring methods. Build local lists only when they make the method clearer.

For generated drawing code, the target file and method are user-provided. Do not create a fixed file mapping based on drawing names. Preserve the existing class structure, public method conventions, and prior code unless the user requests integration into `Draw`.

### 10. Compile Early and Interpret Failures

Run the repository's project build after each meaningful generation pass, normally:

```powershell
dotnet build <project>.csproj --no-restore
```

Also run `git diff --check` for whitespace errors. Classify failures before editing:

- wrong overload or type: inspect `CadDraw` signatures and model types;
- missing namespace or layer/block: inspect neighboring files and initialization;
- malformed method/class structure: inspect braces and placement;
- duplicate or stale model API: inspect current Git state, because generated code and helpers may be changing together.

A successful build proves only that the C# compiles. It does not prove that the generated DWG matches the source.

### 11. Generate and Compare DWG

Use the actual command-line entry point to generate the relevant DWG. The current project initializes CAD standards and writes separate DWG files through `DwgWriter`.

Compare the generated drawing with the original CAD view, preferably by opening both in CAD or using a deterministic visual/diff workflow. Check:

- overall placement and extents;
- contour closure and dimensions;
- line and layer assignments;
- bulge direction and rounded corners;
- circle/steel-section positions and scale;
- repeated count, spacing, direction, and symmetry;
- missing, duplicated, or accidentally merged entities.

Do not report “complete” from compilation alone when visual comparison is part of the user's request. If visual comparison was not performed, state that explicitly.

### 12. Turn Corrections Into Rules

For every post-build correction, record:

1. Actual mismatch.
2. C# correction.
3. Geometric or semantic reason.
4. General rule that can be reused.
5. Whether the rule is verified or **【待验证】**.

Examples supported by this project workflow:

- Floating scan coordinates near a shared vertex required normalization before treating an outline as closed; general rule: compare endpoints with a documented tolerance and preserve the source's closure semantics.
- A source polyline with bulges could not be placed in an `XY` collection; general rule: use the bulge-aware vertex overload consistently when any vertex carries bulge data.
- Repeated circles and lines were more faithfully expressed with loops or entity arrays; general rule: parameterize only after proving equal spacing and count.
- Several source lines had shared endpoints in different orientations; general rule: test all four endpoint pairings and reverse segments before composing a polyline.
- A build passed while geometry could still be wrong; general rule: treat compile validation and DWG visual validation as separate gates.

## Verified Project Facts

The following are supported by the current repository:

- The command-line program creates a `CadDocument`, calls `CadInitializer.InitCad`, invokes drawing delegates, and writes DWG files with `DwgWriter`.
- CAD standards include named layers and line types; `CadInitializer` also ensures common blocks such as `HRB400Block` and `SteelSection`.
- `CadDraw` currently provides point, line, polyline, bulge vertex, circle, arc, entity-array, path-array, and boundary-array helpers.
- Generated pier and bearing drawings use hard-coded coordinates for recovered geometry, loops for regular repetition, layers for visual semantics, and `SteelSection.Insert` where the drawing calls for a steel-section symbol.
- Existing generated methods contain manually reasoned merged polylines; a general endpoint-chain utility is not present.
- Build checks were repeatedly used and succeeded after corrections in the current workflow.
- The current `cad-region.json` may legitimately be empty; empty input must be detected before conversion.

## Rules That Remain 【待验证】

- A universal endpoint tolerance appropriate for every source drawing and unit system.
- Automatic graph merging in the presence of branches, duplicate segments, near-coincident endpoints, or multiple valid continuations.
- A fully automatic mapping from layer/geometry to detailed engineering roles such as main reinforcement versus stirrup.
- Exact visual equivalence of every generated DWG until the corresponding source and generated files have been compared.
- General arc-format conversion beyond the observed bulge representation.
- Whether every repeated CAD pattern should use a shared helper rather than a local loop.

## Final Checklist

- [ ] Read the user-specified target file and existing project APIs.
- [ ] Confirm the JSON is non-empty, structurally valid, and from the intended drawing.
- [ ] Inventory actual entity types and unsupported objects.
- [ ] Build geometry relationships independently of JSON order.
- [ ] Test all four endpoint pairings before merging lines.
- [ ] Use a documented tolerance; do not silently equate near points.
- [ ] Preserve polyline closure, bulges, widths, and elevation where present.
- [ ] Use layers and blocks from project standards.
- [ ] Parameterize only verified repetition, symmetry, or arrays.
- [ ] Keep target file/method scope exactly as requested.
- [ ] Compile the relevant project and run `git diff --check`.
- [ ] Generate the DWG when possible.
- [ ] Compare generated geometry with the source drawing.
- [ ] Report verified facts separately from **【待验证】** assumptions.
- [ ] Capture meaningful corrections as reusable rules.
