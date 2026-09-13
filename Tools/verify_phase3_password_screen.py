from pathlib import Path
import re

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
IMAGE_PATH = ROOT / "Assets/Resources/CursedMod/Phase3Password.png"
SCREEN_PATH = ROOT / "Assets/CursedMod/CursedPhase3Screen.cs"

WIDTH = 1672
HEIGHT = 941


image = Image.open(IMAGE_PATH).convert("RGB")
assert image.size == (WIDTH, HEIGHT), f"Phase3Password.png is {image.size}, expected {(WIDTH, HEIGHT)}"

pixels = image.load()
device_pixels = [
    pixels[x, y]
    for y in range(350, HEIGHT)
    for x in range(280, 1370)
]
magenta_pixels = sum(
    red > 120 and blue > 100 and red > green * 1.4 and blue > green * 1.3
    for red, green, blue in device_pixels
)
brown_pixels = sum(
    red > 80 and red > green * 1.2 and green > blue * 1.1
    for red, green, blue in device_pixels
)
assert magenta_pixels > 200_000, "Phase 3 still uses the old brown Think Pad artwork"
assert brown_pixels < 20_000, "old brown Think Pad remains visible in the Phase 3 asset"

for name, bounds in {
    "upper display": (620, 480, 1050, 660),
    "answer display": (680, 770, 1030, 850),
}.items():
    left, top, right, bottom = bounds
    channel_total = 0
    sample_count = 0
    for y in range(top, bottom):
        for x in range(left, right):
            channel_total += sum(pixels[x, y])
            sample_count += 3
    assert channel_total / sample_count < 15, f"{name} is not clean and empty"

source = SCREEN_PATH.read_text(encoding="utf-8")

def method_body(signature):
    match = re.search(signature + r"\s*\{(.*?)\n    \}", source, re.DOTALL)
    assert match is not None, f"missing layout helper: {signature}"
    return match.group(1)


anchor_min_body = method_body(
    r"private static Vector2 PixelAnchorMin\(float left, float bottom\)"
)
anchor_max_body = method_body(
    r"private static Vector2 PixelAnchorMax\(float right, float top\)"
)
text_helper_body = method_body(
    r"private Text MakeTextFromPixels\(string objectName, float left, float top, "
    r"float right, float bottom, int fontSize\)"
)
click_helper_body = method_body(
    r"private void MakeClickAreaFromPixels\(string objectName, float left, float top, "
    r"float right, float bottom, UnityAction action\)"
)

assert "new Vector2(left / 1672f, 1f - bottom / 941f)" in anchor_min_body
assert "new Vector2(right / 1672f, 1f - top / 941f)" in anchor_max_body
assert (
    "MakeText(objectName, PixelAnchorMin(left, bottom), "
    "PixelAnchorMax(right, top), fontSize)" in text_helper_body
)
assert re.search(
    r"Make(?:TouchButton|ClickArea)\(objectName, PixelAnchorMin\(left, bottom\), "
    r"PixelAnchorMax\(right, top\), action\)",
    click_helper_body,
), "click areas do not delegate through the artwork-relative anchors"

digit_pattern = re.compile(
    r"CreateDigitButtonFromPixels\((\d),\s*"
    r"([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f\);"
)
action_pattern = re.compile(
    r'MakeClickAreaFromPixels\("([^"]+)",\s*'
    r"([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*"
    r"(ClearPassword|Backspace|SubmitPassword)\);"
)

regions = {
    str(digit): (float(left), float(top), float(right), float(bottom))
    for digit, left, top, right, bottom in digit_pattern.findall(source)
}
regions.update({
    action: (float(left), float(top), float(right), float(bottom))
    for _, left, top, right, bottom, action in action_pattern.findall(source)
})

expected = {
    "7": (1135.0, 430.0, 1197.0, 497.0),
    "8": (1207.0, 430.0, 1268.0, 497.0),
    "9": (1278.0, 429.0, 1340.0, 497.0),
    "4": (1135.0, 508.0, 1197.0, 574.0),
    "5": (1207.0, 508.0, 1269.0, 574.0),
    "6": (1279.0, 508.0, 1342.0, 574.0),
    "1": (1134.0, 585.0, 1197.0, 651.0),
    "2": (1207.0, 585.0, 1269.0, 650.0),
    "3": (1279.0, 585.0, 1343.0, 650.0),
    "ClearPassword": (1135.0, 663.0, 1197.0, 730.0),
    "0": (1207.0, 663.0, 1270.0, 730.0),
    "Backspace": (1279.0, 663.0, 1342.0, 730.0),
    "SubmitPassword": (1142.0, 733.0, 1331.0, 927.0),
}
assert regions == expected, f"Phase 3 hitboxes are {regions!r}, expected {expected!r}"

items = list(regions.items())
for index, (name, (left, top, right, bottom)) in enumerate(items):
    assert 0 <= left < right <= WIDTH and 0 <= top < bottom <= HEIGHT, f"{name} is outside the artwork"
    for other_name, (other_left, other_top, other_right, other_bottom) in items[index + 1:]:
        overlap = left < other_right and right > other_left and top < other_bottom and bottom > other_top
        assert not overlap, f"{name} overlaps {other_name}"

text_pattern = re.compile(
    r'MakeTextFromPixels\("([^"]+)",\s*'
    r"([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*([\d.]+)f,\s*(\d+)\)"
)
texts = {
    name: (float(left), float(top), float(right), float(bottom), int(size))
    for name, left, top, right, bottom, size in text_pattern.findall(source)
}
assert texts == {
    "Password Prompt": (620.0, 480.0, 1050.0, 660.0, 54),
    "Entered Password": (680.0, 770.0, 1030.0, 850.0, 70),
}, f"Phase 3 text layers miss the empty displays: {texts!r}"

assert 'prompt.text = "ENTER PASSWORD";' in source
assert "prompt.color = Color.white;" in source
assert "enteredText.color = Color.white;" in source
assert 'Resources.Load<Texture2D>("CursedMod/Phase3Password")' in source

print("PASS: Phase 3 uses the clean purple cursed Think Pad artwork")
print("PASS: password text stays inside the two empty black displays")
print("PASS: every visible Phase 3 key maps to one non-overlapping action")
