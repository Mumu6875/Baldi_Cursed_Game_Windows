# Shell item

Shell is item ID 12; existing item IDs are unchanged. In Phase 2, one pickup is placed on clear NavMesh floor beside the School scene's Alarm Clock. It uses the existing center-screen interaction and inventory UI on Android and Windows.

Use applies only to an active Baldi with CursedBaldiVisual and a valid NavMeshAgent. Failed uses do not consume the item. A successful use covers the head, starts 10 seconds of gameplay-time blindness and sends Baldi wandering once. Hearing and sound priorities remain unchanged; subsequent sounds can set his destination. Seeing or directly targeting the player is blocked until expiry. Reuse resets the timer to 10 seconds, rather than stacking duration. Pause freezes the timer and pauses the sound. Disabling Baldi, leaving the scene or expiry removes the cover and stops the audio.

The spatial source follows the head, with volume 1, linear attenuation, minimum distance 20, maximum distance 250 Unity units and no Doppler. The supplied M4A was decoded and normalized to mono 48 kHz 16-bit PCM WAV. Its 8.96-second recording loops to cover the 10-second effect. Measured peak: -1.74 dBFS; RMS: -19.51 dBFS; no clipped samples. These are digital levels, not physical speaker loudness.

## Assets

- Assets/Resources/CursedMod/Shell.png
- Assets/Resources/CursedMod/ShellUse.wav

The image was created using the built-in imagegen tool with the user's supplied item reference. Prompt: a single hollow brown/tan ridged nutshell dome, opaque helmet-like shell, front three-quarter view, transparent background; match the reference's crude low-resolution 1990s pre-rendered item cutouts, grainy mottled texture, simple lighting and rough pixel edges. No face, character, text, other objects, ground shadow, glossy modern illustration or elaborate decorative detailing.

The head overlay uses normalized CursedBaldi source coordinates. Its center is (514,165), width 420 and height 540 in the original 1024x1536 coordinate system, independent of texture import size. Alpha analysis confirms coverage of all 42,060 opaque head pixels above source row 315.

## Verification

Before publication: C# syntax parsing for all changed source files, item-ID/use/pickup and vision/hearing call-site checks, timer arithmetic at 30/60/120 FPS including pause and reuse, head alpha coverage, WAV decoding/sample checks, unique Unity meta GUIDs and matching shared asset bytes in both platform repositories passed. The timer check is an independent mathematical model, not a Unity play test.

Unity Editor and a device runtime were unavailable for this check. Unity Build Automation runs ShellBuildValidation to reject missing or incorrectly imported assets. In Unity, run Cursed Baldi > Validate Shell Assets, then verify Phase 2 pickup placement, pickup/use on each platform, head rendering from several angles, pause/reuse, expiry, scene exit and sound tracking while blind. Also verify a use on normal Baldi leaves the item in inventory.
