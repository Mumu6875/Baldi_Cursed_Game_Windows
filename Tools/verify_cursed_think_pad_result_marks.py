"""Verify result marks align with the three status windows in CursedThinkPad.png."""

from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/CursedMod/CursedHorrorBootstrap.cs"
code = SOURCE.read_text(encoding="utf-8-sig")

window_bounds = [
    (316.0, 333.0, 409.0, 397.0),
    (313.0, 439.0, 393.0, 495.0),
    (311.0, 545.0, 385.0, 598.0),
]
expected_centers = [
    (362.5, 365.0),
    (353.0, 467.0),
    (348.0, 571.5),
]

mark_method = re.search(
    r"private static void AlignResultMarks\(.*?\)\s*\{(.*?)\n    \}\n"
    r"\n    private const float ArtworkWidth",
    code,
    re.DOTALL,
)
assert mark_method is not None, "AlignResultMarks method was not found"
mark_code = mark_method.group(1)

pixel_array = re.search(
    r"Vector2\[\] markPixels\s*=\s*\{(.*?)\};", mark_code, re.DOTALL
)
pixels = [] if pixel_array is None else [
    tuple(map(float, pair))
    for pair in re.findall(
        r"new Vector2\(([\d.]+)f, ([\d.]+)f\)", pixel_array.group(1)
    )
]

failures = []
if "AlignResultMarks(math, uiRect);" not in code:
    failures.append("result marks do not use the cursed Think Pad UI rectangle")
if "layer.transform.SetParent(uiRect, false);" not in mark_code:
    failures.append("result-mark layer is still parented to the full screen")
if "layerRect.anchorMin = Vector2.zero;" not in mark_code:
    failures.append("result-mark layer no longer stretches from the UI minimum")
if "layerRect.anchorMax = Vector2.one;" not in mark_code:
    failures.append("result-mark layer no longer stretches to the UI maximum")
if "layerRect.offsetMin = Vector2.zero;" not in mark_code:
    failures.append("result-mark layer has a nonzero minimum offset")
if "layerRect.offsetMax = Vector2.zero;" not in mark_code:
    failures.append("result-mark layer has a nonzero maximum offset")
if "markPixels[i].x / ArtworkWidth" not in mark_code:
    failures.append("result X position is not converted from artwork pixels")
if "1f - markPixels[i].y / ArtworkHeight" not in mark_code:
    failures.append("result Y position does not convert the artwork's top origin")
if "resultRect.anchorMin = markAnchor;" not in mark_code:
    failures.append("result minimum anchor does not use the converted center")
if "resultRect.anchorMax = markAnchor;" not in mark_code:
    failures.append("result maximum anchor does not use the converted center")
if "resultRect.anchoredPosition = Vector2.zero;" not in mark_code:
    failures.append("result mark has a nonzero offset from its center")


if pixels != expected_centers:
    failures.append(f"result centers are {pixels!r}, expected {expected_centers!r}")
else:
    for index, ((x, y), (left, top, right, bottom)) in enumerate(
        zip(pixels, window_bounds), start=1
    ):
        if not (left <= x <= right and top <= y <= bottom):
            failures.append(f"result {index} center {(x, y)} misses its status window")

if "resultRect.sizeDelta = new Vector2(53f, 53f);" not in mark_code:
    failures.append("normal 53x53 result-mark size was changed")

assert not failures, "\n" + "\n".join(failures)
print("PASS: result marks use the cursed Think Pad UI rectangle")
print("PASS: all three result centers sit inside their artwork windows")
print("PASS: normal 53x53 result-mark size is preserved")
