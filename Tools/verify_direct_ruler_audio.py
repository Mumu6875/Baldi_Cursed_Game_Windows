"""Verify Baldi's ruler clip is authored directly, without a runtime swap."""

from pathlib import Path
import re
import wave


ROOT = Path(__file__).resolve().parents[1]
SOUNDS = ROOT / "Assets/AudioClip/Characters/Baldi/Sounds"
DIRECT_AUDIO = SOUNDS / "BAL_Slap.wav"
DIRECT_META = SOUNDS / "BAL_Slap.wav.meta"
STALE_OGG = SOUNDS / "BAL_Slap.ogg"
RUNTIME_COPY = ROOT / "Assets/Resources/CursedMod/BaldiRulerLoud.ogg"
BALDI_SCRIPT = ROOT / "Assets/Scripts/NPCFunctions/BaldiScript.cs"
SCENES = (ROOT / "Assets/Scene/School.unity", ROOT / "Assets/Scene/Secret.unity")


errors = []
if not DIRECT_AUDIO.is_file():
    errors.append("direct BAL_Slap.wav asset is missing")
if not DIRECT_META.is_file():
    errors.append("direct BAL_Slap.wav.meta is missing")
if STALE_OGG.exists():
    errors.append("stale BAL_Slap.ogg is still present")
if RUNTIME_COPY.exists():
    errors.append("runtime BaldiRulerLoud.ogg copy is still present")

if DIRECT_AUDIO.is_file():
    try:
        with wave.open(str(DIRECT_AUDIO), "rb") as clip:
            duration = clip.getnframes() / clip.getframerate()
            if clip.getnchannels() != 1:
                errors.append("direct ruler WAV must be mono")
            if clip.getframerate() != 48000:
                errors.append("direct ruler WAV must be 48000 Hz")
            if clip.getsampwidth() != 2:
                errors.append("direct ruler WAV must use 16-bit PCM")
            if not 0.5 <= duration <= 1.0:
                errors.append(f"direct ruler WAV duration is unexpected: {duration:.6f}")
    except (EOFError, wave.Error) as error:
        errors.append(f"direct ruler asset is not a valid PCM WAV: {error}")

guid = ""
if DIRECT_META.is_file():
    match = re.search(r"^guid: ([0-9a-f]{32})$", DIRECT_META.read_text(), re.MULTILINE)
    if match is None:
        errors.append("direct ruler audio meta has no valid GUID")
    else:
        guid = match.group(1)

if guid:
    expected = f"slap: {{fileID: 8300000, guid: {guid}, type: 3}}"
    for scene in SCENES:
        if expected not in scene.read_text():
            errors.append(f"{scene.name} does not reference direct BAL_Slap.wav")

baldi_source = BALDI_SCRIPT.read_text(encoding="utf-8-sig")
if "Resources.Load<AudioClip>" in baldi_source or "BaldiRulerLoud" in baldi_source:
    errors.append("BaldiScript still replaces its ruler audio at runtime")

if errors:
    raise AssertionError("\n".join(errors))

print("PASS: Baldi ruler audio is a single directly referenced PCM WAV")
print("PASS: School and Secret reference the direct clip GUID")
print("PASS: no OGG copy or runtime audio replacement remains")
