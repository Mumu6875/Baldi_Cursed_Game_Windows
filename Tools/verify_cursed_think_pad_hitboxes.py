"""Verify Cursed Think Pad touch regions against the shipped artwork."""

from pathlib import Path
import re
import struct


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/CursedMod/CursedHorrorBootstrap.cs"
ARTWORK = ROOT / "Assets/Resources/CursedMod/CursedThinkPad.png"
MATH_GAME_PREFABS = {
    ROOT / "Assets/PrefabInstance/MathGame.prefab": (800.0, 600.0),
    ROOT / "Assets/PrefabInstance/MathGameNoKeypad.prefab": (800.0, 800.0),
}


def png_size(path):
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", "CursedThinkPad.png is not a PNG"
    assert data[-12:] == b"\x00\x00\x00\x00IEND\xaeB\x60\x82", "CursedThinkPad.png is truncated"
    width, height = struct.unpack(">II", data[16:24])
    return width, height


width, height = png_size(ARTWORK)
assert (width, height) == (1448, 1086), (width, height)
code = SOURCE.read_text(encoding="utf-8-sig")

for prefab_path, expected_size in MATH_GAME_PREFABS.items():
    prefab = prefab_path.read_text()
    yctp = re.search(
        r"m_Name: YCTP\n.*?--- !u!224 &\d+\nRectTransform:.*?"
        r"m_SizeDelta: \{x: ([\d.]+), y: ([\d.]+)\}",
        prefab,
        re.DOTALL,
    )
    assert yctp is not None, f"{prefab_path.name} has no YCTP RectTransform"
    assert tuple(map(float, yctp.groups())) == expected_size, (
        prefab_path.name,
        yctp.groups(),
    )

assert "RectTransform stockRect = stockThinkPad.GetComponent<RectTransform>();" in code, (
    "Cursed layout does not read the normal YCTP RectTransform"
)
assert "CopyRect(stockRect, rect);" in code, (
    "Cursed skin does not match the normal YCTP rectangle"
)
assert "CopyRect(stockRect, controlsRect);" in code, (
    "Cursed hitbox container does not match the normal YCTP rectangle"
)

pixel_pattern = re.compile(
    r'CreateKeyFromPixels\(controls\.transform, "([^"]+)", '
    r'([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f, '
    r'math, (-?\d+), (true|false)\);'
)
boxes = {
    name: (float(left), float(top), float(right), float(bottom), int(value), submit == "true")
    for name, left, top, right, bottom, value, submit in pixel_pattern.findall(code)
}

# Keep the test useful before the pixel-coordinate helper exists: decode the
# legacy normalized anchors so the failure reports the actual wrong targets.
if not boxes:
    legacy_pattern = re.compile(
        r'CreateKey\(controls\.transform, "([^"]+)", '
        r'new Vector2\(([\d.]+)f, ([\d.]+)f\), '
        r'new Vector2\(([\d.]+)f, ([\d.]+)f\), '
        r'math, (-?\d+), (true|false)\);'
    )
    for name, x0, y0, x1, y1, value, submit in legacy_pattern.findall(code):
        boxes[name] = (
            float(x0) * width,
            (1.0 - float(y1)) * height,
            float(x1) * width,
            (1.0 - float(y0)) * height,
            int(value),
            submit == "true",
        )

visual_bounds = {
    "7": (1099, 305, 1162, 367), "8": (1189, 307, 1252, 369), "9": (1282, 308, 1343, 371),
    "4": (1101, 398, 1162, 461), "5": (1191, 398, 1254, 461), "6": (1283, 399, 1345, 461),
    "1": (1101, 490, 1162, 552), "2": (1191, 492, 1254, 553), "3": (1282, 491, 1345, 555),
    "C": (1102, 585, 1164, 648), "0": (1191, 587, 1253, 649), "Minus": (1280, 589, 1342, 652),
    "OK": (1114, 669, 1327, 881),
}
expected_actions = {
    "7": (7, False), "8": (8, False), "9": (9, False),
    "4": (4, False), "5": (5, False), "6": (6, False),
    "1": (1, False), "2": (2, False), "3": (3, False),
    "C": (-2, False), "0": (0, False), "Minus": (-1, False),
    "OK": (0, True),
}

failures = []
for name, (left, top, right, bottom) in visual_bounds.items():
    center = ((left + right) / 2, (top + bottom) / 2)
    hits = [
        candidate for candidate, (x0, y0, x1, y1, _, _) in boxes.items()
        if x0 <= center[0] <= x1 and y0 <= center[1] <= y1
    ]
    if hits != [name]:
        failures.append(f"{name} center hits {hits}, expected [{name!r}]")
        continue
    x0, y0, x1, y1, value, submit = boxes[name]
    if not (x0 <= left and y0 <= top and x1 >= right and y1 >= bottom):
        failures.append(f"{name} hitbox does not cover its visible key")
    if (value, submit) != expected_actions[name]:
        failures.append(f"{name} action is {(value, submit)}, expected {expected_actions[name]}")

names = list(boxes)
for index, first in enumerate(names):
    a = boxes[first]
    for second in names[index + 1:]:
        b = boxes[second]
        overlap_width = min(a[2], b[2]) - max(a[0], b[0])
        overlap_height = min(a[3], b[3]) - max(a[1], b[1])
        if overlap_width > 0 and overlap_height > 0:
            failures.append(f"{first} overlaps {second}")

assert not failures, "\n" + "\n".join(failures)
print("PASS: PNG is complete and 1448x1086")
print("PASS: cursed skin and controls match the active prefab's normal YCTP rectangle")
print("PASS: all 13 visible keys map to exactly one matching action")
print("PASS: every visible key is covered and no hitboxes overlap")
