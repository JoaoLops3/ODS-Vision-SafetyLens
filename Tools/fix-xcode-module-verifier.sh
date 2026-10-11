#!/usr/bin/env bash
# Desliga Module Verifier no projeto Xcode gerado e trava o pbxproj
# para o Xcode não reescrever ENABLE_MODULE_VERIFIER=YES ao salvar.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PBX="$ROOT/Builds/Unity-iPhone.xcodeproj/project.pbxproj"

if [[ ! -f "$PBX" ]]; then
  echo "Projeto Xcode nao encontrado: $PBX"
  exit 1
fi

osascript -e 'tell application "Xcode" to quit' >/dev/null 2>&1 || true
sleep 1

chflags nouchg "$PBX" 2>/dev/null || true

python3 - <<'PY' "$PBX"
import re, sys
p = sys.argv[1]
s = open(p).read()
flags = {
    "ENABLE_MODULE_VERIFIER": "NO",
    "ENABLE_USER_SCRIPT_SANDBOXING": "NO",
    "CLANG_WARN_QUOTED_INCLUDE_IN_FRAMEWORK_HEADER": "NO",
    "CLANG_ALLOW_NON_MODULAR_INCLUDES_IN_FRAMEWORK_MODULES": "YES",
}
for k, v in flags.items():
    s = re.sub(rf"{k}\s*=\s*[^;]+;", f"{k} = {v};", s)

def inject(m):
    block = m.group(0)
    for k, v in flags.items():
        if re.search(rf"{k}\s*=", block):
            continue
        block = block.replace("buildSettings = {", f"buildSettings = {{\n\t\t\t\t{k} = {v};", 1)
    return block

s = re.sub(r"buildSettings = \{.*?\n\t\t\t\};", inject, s, flags=re.S)
open(p, "w").write(s)
yes = len(re.findall(r"ENABLE_MODULE_VERIFIER\s*=\s*YES", s))
no = len(re.findall(r"ENABLE_MODULE_VERIFIER = NO;", s))
print(f"Module Verifier: YES={yes} NO={no}")
if yes:
    sys.exit(2)
PY

chflags uchg "$PBX"
rm -rf "${HOME}/Library/Developer/Xcode/DerivedData/Unity-iPhone-"*
echo "pbxproj travado (uchg). DerivedData limpo."
open "$ROOT/Builds/Unity-iPhone.xcodeproj"
echo "Xcode aberto. Clean (⇧⌘K) e Run. Se pedir para salvar o projeto: Don't Save."
