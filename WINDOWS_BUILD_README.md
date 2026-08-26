# Windows x86_64 build guide

## Required environment

- Unity Editor `6000.3.22f1`
- Windows Build Support (IL2CPP)
- Visual Studio 2019 or newer with C++ tools and Windows SDK 10.0.19041.0 or newer
- Windows 10 21H1 or newer for the released player

## Interactive build

1. Run `./tools/verify_windows_repo.sh` from the repository root.
2. Open the project in Unity `6000.3.22f1`.
3. Wait for asset import and script compilation to finish.
4. Open `Assets/Scene/MainMenu.unity` and run a smoke test.
5. Select `Cursed Baldi > Build Windows x86_64`.
6. Select an empty output directory.

The output directory must contain the executable, its `_Data` directory,
`UnityPlayer.dll`, `UnityCrashHandler64.exe`, and any other files emitted by Unity.
Distribute the entire directory as one ZIP archive.

## Batch build

Run Unity with:

```text
-batchmode -quit -projectPath <project> -executeMethod CursedWindowsSetup.BuildWindowsX64Batch
```

The output is written to:

```text
Builds/Windows-x86_64/BaldiCursedClassroom.exe
```

## Enforced settings

- Target: `BuildTarget.StandaloneWindows64`
- Architecture: Intel/AMD 64-bit (`x86_64`)
- Backend: IL2CPP
- Unsafe C#: disabled
- Managed stripping: Low
- Default resolution: 1280×720
- Borderless fullscreen with Alt+Enter switching enabled
- Single running instance
- VSync enabled

The pre-build validator rejects every non-Windows-x86_64 target and verifies all
required horror images and audio assets before Unity starts producing a player.
