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
if "stockResultBackground.enabled = false;" not in mark_code:
    failures.append("the oversized stock ResultBG is still visible")

background_pattern = re.compile(
    r'CreateResultBackgroundFromPixels\(layerRect, "(\d)", '
    r'([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\);'
)
backgrounds = [
    tuple(map(float, values))
    for _, *values in background_pattern.findall(mark_code)
]
if backgrounds != window_bounds:
    failures.append(
        f"white result backgrounds are {backgrounds!r}, expected {window_bounds!r}"
    )


if pixels != expected_centers:
    failures.append(f"result centers are {pixels!r}, expected {expected_centers!r}")
else:
    for index, ((x, y), (left, top, right, bottom)) in enumerate(
        zip(pixels, window_bounds), start=1
    ):
        if not (left <= x <= right and top <= y <= bottom):
            failures.append(f"result {index} center {(x, y)} misses its status window")

if "PlaceResultMarkFromPixels(resultRect, markPixels[i], 48f);" not in mark_code:
    failures.append("result marks are not constrained to 48 artwork pixels")
if "resultRect.sizeDelta = new Vector2(53f, 53f);" in mark_code:
    failures.append("the oversized 53x53 local-unit result mark remains")

mark_layout = re.search(
    r"private static void PlaceResultMarkFromPixels\(.*?\)\s*\{(.*?)\n    \}",
    code,
    re.DOTALL,
)
if mark_layout is None:
    failures.append("artwork-relative result-mark layout helper is missing")
else:
    layout_code = mark_layout.group(1)
    for required in (
        "(centerPixels.x - halfSize) / ArtworkWidth",
        "(centerPixels.x + halfSize) / ArtworkWidth",
        "1f - (centerPixels.y + halfSize) / ArtworkHeight",
        "1f - (centerPixels.y - halfSize) / ArtworkHeight",
        "resultRect.offsetMin = Vector2.zero;",
        "resultRect.offsetMax = Vector2.zero;",
    ):
        if required not in layout_code:
            failures.append(f"result-mark layout is missing: {required}")

assert not failures, "\n" + "\n".join(failures)
print("PASS: result marks use the cursed Think Pad UI rectangle")
print("PASS: all three result centers sit inside their artwork windows")
print("PASS: stock ResultBG is hidden and three white windows match the artwork")
print("PASS: result marks are constrained to 48 artwork pixels")
