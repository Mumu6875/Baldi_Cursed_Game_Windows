from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
EXIT_SEQUENCE_PATH = ROOT / "Assets/CursedMod/CursedFinalExitSequence.cs"
BOOTSTRAP_PATH = ROOT / "Assets/CursedMod/CursedHorrorBootstrap.cs"
PHASE4_SCREEN_PATH = ROOT / "Assets/CursedMod/CursedPhase4Screen.cs"
PHASE3_SCREEN_PATH = ROOT / "Assets/CursedMod/CursedPhase3Screen.cs"


def method_body(source: str, signature: str) -> str:
    start = source.find(signature)
    assert start >= 0, f"missing method: {signature}"
    opening_brace = source.index("{", start)
    depth = 0
    for index in range(opening_brace, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[opening_brace + 1:index]
    raise AssertionError(f"unterminated method: {signature}")


exit_sequence = EXIT_SEQUENCE_PATH.read_text(encoding="utf-8")
bootstrap = BOOTSTRAP_PATH.read_text(encoding="utf-8")
phase4_screen = PHASE4_SCREEN_PATH.read_text(encoding="utf-8")
phase3_screen = PHASE3_SCREEN_PATH.read_text(encoding="utf-8")

show_completion = method_body(
    exit_sequence,
    "private void ShowPhase2CompletionScreen()",
)
complete_and_quit = method_body(
    exit_sequence,
    "private void CompletePhase2AndQuit()",
)
scene_loaded = method_body(
    bootstrap,
    "private void OnSceneLoaded(Scene scene, LoadSceneMode mode)",
)
phase3_branch = method_body(scene_loaded, "if (CursedPhaseManager.IsPhase3)")
phase4_branch = method_body(scene_loaded, "if (CursedPhaseManager.IsPhase4)")

assert 'Resources.Load<Texture2D>(\n                "CursedMod/Phase2Completion")' in show_completion
assert re.search(
    r"continueButton\.onClick\.AddListener\(\s*CompletePhase2AndQuit\s*\);",
    show_completion,
), "Phase2Completion.png does not invoke the save-and-quit handler when tapped"
assert "CursedPhaseManager.UnlockPhase3" not in show_completion, (
    "Phase 3 is unlocked before the player taps Phase2Completion.png"
)
assert "CursedPhase3Screen.Show" not in show_completion, (
    "Phase 2 completion opens the Phase 3 password screen in the same launch"
)
assert "CursedPhase3Screen.Show" not in exit_sequence, (
    "the Phase 2 exit sequence must never open the Phase 3 screen directly"
)
assert "CursedPhaseManager.UnlockPhase3(completionCode);" in complete_and_quit
assert "Application.Quit();" in complete_and_quit
assert complete_and_quit.index("CursedPhaseManager.UnlockPhase3(completionCode);") < (
    complete_and_quit.index("Application.Quit();")
), "Phase 3 progress must be saved before the application closes"
assert "if (phase2CompletionHandled) return;" in complete_and_quit, (
    "Phase 2 completion tap is not protected against duplicate handling"
)
assert "if (CursedPhaseManager.IsPhase3)" in scene_loaded
assert "CursedPhase3Screen.Show();" in phase3_branch
assert scene_loaded.count("CursedPhase3Screen.Show();") == 1
assert 'Resources.Load<Texture2D>("CursedMod/Phase4Final")' in phase4_screen
assert 'text.text = "You were just a mistake.";' in phase4_screen
assert "CursedPhase4Screen.Show();" in phase4_branch
assert scene_loaded.count("CursedPhase4Screen.Show();") == 1
assert "Phase4Final" not in exit_sequence
assert "Phase4Final" not in phase3_screen

print("PASS: Phase 2 unlocks Phase 3 only after the completion screen is tapped")
print("PASS: tapping saves Phase 3 progress before closing the application")
print("PASS: Phase 3 password and Phase 4 final screens remain launch-gated")
