# Customização — SafetyLens

## Conteúdo do cenário

Edite `Assets/Resources/safetylens_scenario.json`:

- `targetName` deve coincidir com o nome na Reference Image Library (`SafetyLensEquipmentTarget`).
- `hazards[]`: zonas `alert` (placa) e `epi` (modelo 3D), ancoradas por **UV no marcador**.
- `quiz[]`: pergunta, opções e `correctIndex` (base 0).

### Âncoras UV (A / B / C)

Cada hazard usa frações do alvo físico (0–1), não metros de HUD na câmera:

| Campo | Significado |
|---|---|
| `u` | 0 = borda esquerda do marcador, 1 = direita |
| `v` | 0 = borda inferior, 1 = superior |
| `x`, `y` | Ajuste fino em **metros** no plano da arte (+X direita, +Y sobe a arte) |
| `z` | Elevação em metros saindo do plano (+Y do `ARTrackedImage`). `0` = token padrão |

No cenário demo:

| Letra na arte | Hazard | UV típica |
|---|---|---|
| **A** (azul / ENERGIA) | HOT | `u≈0.24`, `v≈0.53` |
| **B** (laranja / PROTECAO) | PINCH | `u≈0.50`, `v≈0.53` |
| **C** (verde / SAIDA) | EPI (capacete) | `u≈0.76`, `v≈0.53` |

A conversão para o espaço do `ARTrackedImage` está em `SafetyLensHazard.LocalPositionOnMarker`
(plano XZ do marcador, +Y saindo da arte). Tamanho físico de referência: **20 × 13,3 cm**.

Exemplo:

```json
{
  "id": "HOT",
  "label": "Zona quente",
  "kind": "alert",
  "instruction": "Toque na placa de alerta: superfície quente / risco de queimadura.",
  "pictogram": "hot",
  "u": 0.238,
  "v": 0.530,
  "x": 0.0,
  "y": 0.0,
  "z": 0.0
}
```

- `kind`: `alert` (placa) ou `epi` (equipamento).
- `pictogram` (alert): `hot`, `pinch` ou `generic`.
- `model` (epi): `helmet`.

## Marcador físico

1. Imprima `Assets/Markers/safetylens-equipment-target.png` com **~20 cm de largura** (mesmo valor da library).
2. Para trocar a imagem: substitua o PNG e use **SafetyLens → Configurar projeto completo**.
3. O nome do target na library e no JSON deve permanecer `SafetyLensEquipmentTarget`.

## Visuais de RA

- **Placas** (`alert`): geradas em runtime por `SafetyLensPictograms` + `SafetyLensArVisuals` (shader `SafetyLens/SignUnlit`). Não há prefab HOT/PINCH obrigatório.
- **Capacete** (`epi`): prefab em `Resources/SafetyLens/Prefabs/Helmet`, gerado a partir de `Assets/Models/EPI/HardHat.fbx`.
  - Troque o FBX e rode **SafetyLens → Regerar prefab do capacete (FBX)**.
  - O mesh precisa manter collider (ou o setup cria um) e o toque usa `SafetyLensHazardTag` com o mesmo `hazardId`.

## Métricas exportadas

Arquivos em `Application.persistentDataPath`:

- `safetylens_<id>_<timestamp>.json`
- `safetylens_<id>_<timestamp>.csv`

Campos: participantId, tempo, erros, hazards, respostas do quiz.
