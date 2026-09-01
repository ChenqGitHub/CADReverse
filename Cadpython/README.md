## Region Export

默认使用 `pyautocad.Autocad.get_selection()`，在 CAD 中手动选择需要交给 AI 识别的大样：

```powershell
uv run python region_export.py --output .\artifacts\cad-region.json
```

如果需要矩形框选，可以使用：

```powershell
uv run python region_export.py --selection rectangle --mode crossing --output .\artifacts\cad-region.json
```

`screen` 模式返回用户在 CAD 中实际选中的实体；`rectangle` 的 `window` 只选择完全位于矩形内的实体，`crossing` 还选择与矩形相交的实体。
