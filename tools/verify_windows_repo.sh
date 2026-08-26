#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

fail() {
  printf 'ERROR: %s\n' "$1" >&2
  exit 1
}

for forbidden_file in \
  Assets/CursedMod/CursedMobileInput.cs \
  Assets/CursedMod/CursedMobileHoldButton.cs \
  Assets/CursedMod/Editor/CursedAndroidSetup.cs \
  Assets/Resources/CursedMod/MobileLookBackButton.png \
  Assets/Resources/CursedMod/MobileRunButton.png \
  Assets/Resources/CursedMod/MobileUseItemButton.png; do
  [[ ! -e "$forbidden_file" ]] || fail "mobile-only file remains: $forbidden_file"
done

if grep -RInE 'CursedMobile|CursedAndroid|BuildTarget\.Android|NamedBuildTarget\.Android|Input\.touch|UNITY_ANDROID' Assets/CursedMod Assets/Scripts; then
  fail 'mobile-only runtime or build code remains'
fi

grep -q 'BuildTarget.StandaloneWindows64' Assets/CursedMod/Editor/CursedWindowsSetup.cs \
  || fail 'Windows x86_64 build target is missing'

if grep -q 'com.unity.modules.unitywebrequest' Packages/manifest.json; then
  fail 'Unity web-request modules are enabled'
fi

if grep -RInE 'UnityWebRequest|HttpClient|WebClient|System\.Net|TcpClient|UdpClient|Process\.Start|System\.Diagnostics\.Process|Application\.OpenURL|DllImport|Assembly\.Load|BinaryFormatter' Assets --include='*.cs'; then
  fail 'network, process, native-loading, or unsafe deserialization API detected'
fi

if grep -RInE 'ghp_[A-Za-z0-9]+|github_pat_[A-Za-z0-9_]+|BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY' Assets Packages ProjectSettings; then
  fail 'credential material detected'
fi

grep -q 'allowUnsafeCode: 0' ProjectSettings/ProjectSettings.asset \
  || fail 'unsafe C# compilation is not disabled'

if command -v git >/dev/null 2>&1; then
  git diff --check
fi

printf 'Windows x86_64 repository verification passed.\n'
