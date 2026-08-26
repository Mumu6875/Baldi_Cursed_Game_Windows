# Baldi Cursed Classroom — Windows x86_64 Horror Mod

This is a real Unity 2018.3.9f1 project based on **Baldi's Basics Open Source Tool v5** (Classic 1.4.3 decompile). It modifies the original Baldi project rather than replacing it with a new imitation game.

Source page: https://pspleaffox.itch.io/baldi-open-source-classic-party

## Included

- Cursed Baldi runtime skin applied to the original Baldi sprite object
- Cursed You Can Think Pad full-screen skin behind the original functional controls
- Dark red fog, low ambient lighting, camera light flicker and proximity danger pulse
- More aggressive original Baldi navigation values
- Cursed Baldi game-over jumpscare
- Native keyboard and mouse controls for Windows
- Windows x86_64-only build validation and one-click build command
- Keyboard support for the Think Pad and Phase 3 password screen

## Controls

- Move: `WASD`
- Look: mouse
- Interact: left mouse button
- Use item: right mouse button
- Select item: `1`, `2`, `3` or mouse wheel
- Run: left Shift
- Look behind / jump rope: Space
- Pause: Escape
- Phase 3 password: number keys, Backspace, Delete and Enter

## Build

Use Unity **6000.3.22f1** and select `Cursed Baldi > Build Windows x86_64`.
The repository rejects Android, 32-bit Windows and other build targets during
pre-build validation. See `WINDOWS_BUILD_README.md` for interactive and batch
build instructions.

If Unity offers to upgrade the project, make a backup first. The source package recommends 2018.3.9f1; large upgrades can alter TextMesh Pro layout and old shaders.

## Non-commercial restriction and credits

The downloaded base identifies itself as a fan-made decompile. Its page states that Baldi, the characters, code, assets and music belong to mystman12/Basically Games and that the decompile may not be used commercially, including ads or in-app purchases. Credit **Mystman12 / Basically Games** in any distributed build.

Generated mod artwork is stored in `Assets/Resources/CursedMod/`. Mod runtime code is stored in `Assets/CursedMod/`.
