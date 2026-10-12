# Arquitetura — SafetyLens (Unity)

Projeto unificado a partir do artigo *SafetyLens* e dos baselines WL01, WL03 e WL04.
WL02 (detecção de planos) **não** foi incluído: o artigo especifica rastreamento de imagens, não plane detection.

## Camadas (artigo)

```text
Hardware (câmera + sensores)
  → ARCore / ARKit
    → AR Foundation (AR Tracked Image Manager + Reference Image Library)
      → Unity (cena / renderização / raycast)
        → Lógica C# (sessão, riscos, quiz, tempo, erros)
          → Persistência local JSON/CSV
```

## Hierarquia da cena

```text
AR Session
XR Origin (Mobile AR)
  ├── Camera Offset / Main Camera (AR Camera Manager + Background)
  ├── ARTrackedImageManager + SafetyLensSessionController
SafetyLens Status Overlay
```

## Fluxo da aplicação (RF / jornada do artigo)

1. **Identificação** — ID do participante (RF06 / tela 1).
2. **Scanning** — câmera busca o Image Target (RF01 / tela 2).
3. **RiskInspection** — zonas de alerta + EPIs em RA; toque confirma (RF02, RF03 / tela 3).
4. **Quiz** — perguntas de fixação; erros contabilizados (RF03, RF05 / tela 4).
5. **Finished** — tempo total + exportação JSON/CSV local (RF04, RF06).

## Origem do código

| Peça | Origem |
|---|---|
| Image tracking + conteúdo filho do `ARTrackedImage` | WL01 / WL03 |
| Toque + `Physics.Raycast` | WL01 / WL03 |
| Procedimento ordenado + timing | WL03 |
| Validação de ordem / rejeição | WL04 (ideia de “fora de ordem” → erros) |
| Overlay IMGUI + Editor setup | WL02/WL03/WL04 (padrão) |
| Tela de participante, quiz, contador de erros, export | Artigo (RF03–RF06), padrão WL |

## Conteúdo externo

`Assets/Resources/safetylens_scenario.json` — hazards, EPIs e quiz separados da lógica C#.

Posição dos hazards: **UV no marcador** (`u`/`v` 0–1 → letras A/B/C), convertida em
`SafetyLensHazard.LocalPositionOnMarker` para o espaço local do `ARTrackedImage`.
Ajuste fino opcional em metros (`x`/`y` no plano, `z` = lift). Ver `docs/CUSTOMIZACAO.md`.

## Validação no Editor

**SafetyLens → Validar projeto** exige:

- Cena (ARSession, XROrigin, TrackedImageManager, overlays, câmera única)
- Reference Image Library com nome `SafetyLensEquipmentTarget`
- Cenário com `targetName` igual + UVs em `[0,1]`
- **Android:** ARCore Loader
- **iOS:** ARKit Loader + Metal only + `cameraUsageDescription`
