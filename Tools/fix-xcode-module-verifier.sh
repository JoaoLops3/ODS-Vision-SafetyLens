#!/usr/bin/env bash
# Desliga Module Verifier e trava o pbxproj para o Xcode não reescrever YES.
# Uso: ./Tools/fix-xcode-module-verifier.sh
#      NO_OPEN=1 ./Tools/fix-xcode-module-verifier.sh   # só patch, sem abrir o Xcode
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PBX="$ROOT/Builds/Unity-iPhone.xcodeproj/project.pbxproj"

if [[ ! -f "$PBX" ]]; then
  echo "Projeto Xcode nao encontrado: $PBX"
  exit 1
fi

# Mata o Xcode de verdade — quit via AppleScript às vezes deixa processo zumbi com o projeto em memória.
osascript -e 'tell application "Xcode" to quit' >/dev/null 2>&1 || true
sleep 1
killall Xcode 2>/dev/null || true
sleep 1
# Garante que nenhum Xcode sobreviveu (senão ele regrava YES ao fechar/salvar).
if pgrep -x Xcode >/dev/null 2>&1; then
  killall -9 Xcode 2>/dev/null || true
  sleep 1
fi

chflags nouchg "$PBX" 2>/dev/null || true
chmod u+w "$PBX" 2>/dev/null || true

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
# Passada 1: substitui qualquer valor existente (YES/NO/etc).
for k, v in flags.items():
    s = re.sub(rf"{k}\s*=\s*[^;]+;", f"{k} = {v};", s)

# Passada 2: em todo bloco buildSettings, injeta flags ausentes.
def inject(m):
    block = m.group(0)
    for k, v in flags.items():
        if re.search(rf"{k}\s*=", block):
            continue
        block = block.replace("buildSettings = {", f"buildSettings = {{\n\t\t\t\t{k} = {v};", 1)
    return block

s = re.sub(r"buildSettings = \{.*?\n\t\t\t\};", inject, s, flags=re.S)

# Passada 3: cinto e suspensório — zera qualquer YES residual de Module Verifier.
s = re.sub(r"ENABLE_MODULE_VERIFIER\s*=\s*YES\s*;", "ENABLE_MODULE_VERIFIER = NO;", s)

open(p, "w").write(s)
yes = len(re.findall(r"ENABLE_MODULE_VERIFIER\s*=\s*YES", s))
no = len(re.findall(r"ENABLE_MODULE_VERIFIER\s*=\s*NO", s))
print(f"Module Verifier: YES={yes} NO={no}")
if yes:
    for i, line in enumerate(s.splitlines(), 1):
        if "ENABLE_MODULE_VERIFIER" in line and "YES" in line:
            print(f"  residual L{i}: {line.strip()}")
    sys.exit(2)
PY

# uchg + só leitura: o Xcode não consegue gravar YES de volta no disco.
chmod a-w "$PBX"
chflags uchg "$PBX"

# Confirma no disco depois do lock.
YES_AFTER=$(rg -c 'ENABLE_MODULE_VERIFIER\s*=\s*YES' "$PBX" || true)
NO_AFTER=$(rg -c 'ENABLE_MODULE_VERIFIER\s*=\s*NO' "$PBX" || true)
echo "Pos-lock: YES=${YES_AFTER:-0} NO=${NO_AFTER:-0} flags=$(ls -lO "$PBX" | awk '{print $5}')"
if [[ -n "${YES_AFTER}" && "${YES_AFTER}" != "0" ]]; then
  echo "ERRO: ainda ha ENABLE_MODULE_VERIFIER=YES apos o patch."
  exit 2
fi

rm -rf "${HOME}/Library/Developer/Xcode/DerivedData/Unity-iPhone-"*
echo "pbxproj travado (uchg + a-w). DerivedData limpo."

if [[ "${NO_OPEN:-0}" != "1" ]]; then
  open "$ROOT/Builds/Unity-iPhone.xcodeproj"
  echo ""
  echo "Xcode aberto. Agora:"
  echo "  1) Clean Build Folder (⇧⌘K)"
  echo "  2) Run (⌘R)"
  echo "  3) Se pedir Unlock/Save no project.pbxproj: Don't Unlock / Don't Save / Revert"
  echo "     (Unlock traz o Module Verifier de volta e quebra o UnityFramework.)"
fi
