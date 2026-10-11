# Customização — SafetyLens

## Conteúdo do cenário

Edite `Assets/Resources/safetylens_scenario.json`:

- `targetName` deve coincidir com o nome na Reference Image Library (`SafetyLensEquipmentTarget`).
- `hazards[]`: zonas `alert` (vermelho) e `epi` (azul), com posição local `x/y/z`.
- `quiz[]`: pergunta, opções e `correctIndex` (base 0).

## Marcador físico

1. Imprima `Assets/Markers/safetylens-equipment-target.png` com ~20 cm de largura.
2. Para trocar a imagem: substitua o PNG e use **SafetyLens → Configurar projeto completo** (ou recrie a library).

## Modelos 3D finais

Os hazards/EPIs demo são primitivos gerados em código (`SafetyLensSessionController.CreateHazardVisual`).
Substitua por prefabs mantendo o `SafetyLensHazardTag` com o mesmo `hazardId`.

## Métricas exportadas

Arquivos em `Application.persistentDataPath`:

- `safetylens_<id>_<timestamp>.json`
- `safetylens_<id>_<timestamp>.csv`

Campos: participantId, tempo, erros, hazards, respostas do quiz.
