from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
CURSED_MOD = ROOT / "Assets/CursedMod"
NORMAL_DEBUG_LOG = re.compile(r"\bDebug\s*\.\s*Log\s*\(")


def csharp_code_only(source: str) -> str:
    result = []
    index = 0
    state = "code"

    while index < len(source):
        current = source[index]
        following = source[index + 1] if index + 1 < len(source) else ""

        if state == "code":
            if current == "/" and following == "/":
                result.extend("  ")
                index += 2
                state = "line_comment"
                continue
            if current == "/" and following == "*":
                result.extend("  ")
                index += 2
                state = "block_comment"
                continue
            if current == "@" and following == '"':
                result.extend("  ")
                index += 2
                state = "verbatim_string"
                continue
            if current == '"':
                result.append(" ")
                index += 1
                state = "string"
                continue
            if current == "'":
                result.append(" ")
                index += 1
                state = "char"
                continue
            result.append(current)
            index += 1
            continue

        if current == "\n":
            result.append("\n")
            index += 1
            if state == "line_comment":
                state = "code"
            continue

        if state == "block_comment" and current == "*" and following == "/":
            result.extend("  ")
            index += 2
            state = "code"
            continue

        if state == "verbatim_string" and current == '"':
            if following == '"':
                result.extend("  ")
                index += 2
            else:
                result.append(" ")
                index += 1
                state = "code"
            continue

        if state in {"string", "char"} and current == "\\":
            result.extend("  ")
            index += min(2, len(source) - index)
            continue

        if state == "string" and current == '"':
            state = "code"
        elif state == "char" and current == "'":
            state = "code"

        result.append(" ")
        index += 1

    return "".join(result)


violations = []
for source_path in sorted(CURSED_MOD.rglob("*.cs")):
    if "Editor" in source_path.relative_to(CURSED_MOD).parts:
        continue

    source = source_path.read_text(encoding="utf-8")
    code = csharp_code_only(source)
    source_lines = source.splitlines()
    for match in NORMAL_DEBUG_LOG.finditer(code):
        line_number = code.count("\n", 0, match.start()) + 1
        violations.append(
            f"{source_path.relative_to(ROOT)}:{line_number}: "
            f"{source_lines[line_number - 1].strip()}"
        )

if violations:
    raise SystemExit(
        "normal Debug.Log calls remain in CursedMod runtime code:\n"
        + "\n".join(violations)
    )

print("PASS: CursedMod runtime code contains no normal Debug.Log calls")
