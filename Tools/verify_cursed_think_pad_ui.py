"""Verify Cursed Think Pad display panels and text stay inside the artwork."""

from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
INSTALLER = (ROOT / "Assets/CursedMod/CursedHorrorBootstrap.cs").read_text(
    encoding="utf-8-sig"
)
MATH_GAME = (ROOT / "Assets/Scripts/Core/UI/MathGameScript.cs").read_text(
    encoding="utf-8-sig"
)

expected_panels = {
    "Question Panel": ("questionPanelRect", 462.0, 333.0, 997.0, 570.0),
    "Answer Panel": ("answerPanelRect", 562.0, 643.0, 997.0, 802.0),
}
panel_pattern = re.compile(
    r'PlacePanelFromPixels\(uiLayer\.transform, "([^"]+)", '
    r'(\w+), ([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\);'
)
actual_panels = {
    name: (rect, *map(float, (left, top, right, bottom)))
    for name, rect, left, top, right, bottom in panel_pattern.findall(INSTALLER)
}

failures = []
for name, expected in expected_panels.items():
    if actual_panels.get(name) != expected:
        failures.append(
            f"{name} is {actual_panels.get(name)!r}, expected {expected!r}"
        )

fitted_question_texts = set(re.findall(
    r"FitQuestionTextToPanel\(math\.(questionText\d*)\);", INSTALLER
))
expected_question_texts = {"questionText", "questionText2", "questionText3"}
if fitted_question_texts != expected_question_texts:
    failures.append(
        f"fitted question texts are {sorted(fitted_question_texts)!r}, "
        f"expected {sorted(expected_question_texts)!r}"
    )
fit_method = re.search(
    r"private static void FitQuestionTextToPanel\(.*?\)\s*\{(.*?)\n    \}",
    INSTALLER,
    re.DOTALL,
)
if fit_method is None:
    failures.append("question text fitting helper is missing")
else:
    fit_code = fit_method.group(1)
    for required in (
        "textRect.anchorMin = Vector2.zero;",
        "textRect.anchorMax = Vector2.one;",
        "textRect.offsetMin = new Vector2(12f, 12f);",
        "textRect.offsetMax = new Vector2(-12f, -12f);",
    ):
        if required not in fit_code:
            failures.append(f"question text fitting is missing: {required}")

if "stockAnswerBackground.enabled = false;" in INSTALLER:
    failures.append("the white answer background is still disabled")
if "stockAnswerBackground.enabled = true;" not in INSTALLER:
    failures.append("the white answer background is not explicitly enabled")
if "stockAnswerBackground.color = Color.white;" not in INSTALLER:
    failures.append("the answer panel is not forced to white")
if "math.playerAnswer.textComponent.color = Color.black;" not in INSTALLER:
    failures.append("the live answer text is not forced to black")
if "math.playerAnswer.placeholder.gameObject.SetActive(false);" not in INSTALLER:
    failures.append("the ENTER ANSWER placeholder is not hidden")

message_method = re.search(
    r"private void ShowPhase2FinalNotebookMessage\(\)\s*\{(.*?)\n    \}\n"
    r"    public void OKButton",
    MATH_GAME,
    re.DOTALL,
)
assert message_method is not None, "final notebook message method was not found"
message_code = message_method.group(1)
if "messageObject.transform.SetParent(questionText.transform.parent, false);" not in message_code:
    failures.append("the final message is not parented to the normal question panel")
if "message.characterSpacing = questionText.characterSpacing;" not in message_code:
    failures.append("the final message does not use the normal question text width")
if "rect.anchorMin = new Vector2(0.20f, 0.44f);" in message_code:
    failures.append("the final message still uses the oversized root-screen rectangle")

assert not failures, "\n" + "\n".join(failures)
print("PASS: question and answer panels fill the cursed LCD openings")
print("PASS: all question text layers stay inside the upper LCD panel")
print("PASS: answer background is white and live answer text is black")
print("PASS: ENTER ANSWER is hidden and the final message uses normal text width")
