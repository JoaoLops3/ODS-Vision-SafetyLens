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
3. **RiskInspection** — zonas vermelhas + placas ISO + EPIs em RA; toque confirma (RF02, RF03 / tela 3). Cronômetro de inspeção e sonda de FPS/memória (RF04 / RNF02).
4. **Quiz** — perguntas de fixação; erros contabilizados (RF03, RF05 / tela 4). Tempo de quiz separado do de inspeção.
5. **Finished** — tempo total + inspeção + quiz + FPS/memória + marcador usado; exportação JSON/CSV local (RF04, RF06).

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

Posição dos hazards: **UV no marcador** (`u`/`v` 0–1 → letras A/B/C no WL03), convertida em
`SafetyLensHazard.LocalPositionOnMarker`. Alvos alternativos (QR) usam `altTargets` +
`ResolveUv`. Ajuste fino opcional em metros (`x`/`y` no plano, `z` = lift). Ver `docs/CUSTOMIZACAO.md`.

A sessão trava no **primeiro** Image Target reconhecido (`markerName` no export).

### Tracking Limited (P1-6)

`TrackingLossGate` (classe pura) + polling em `Update` (`UpdateTargetVisibility`):

| Estado | Condição | UI / RA |
|---|---|---|
| **Tracking** | `trackingState == Tracking` | Conteúdo visível, toque e confirmar liberados |
| **Unstable** | Limited/None dentro de 1,5 s **e** 10 frames | Conteúdo visível, seleção preservada, banner amarelo, toque/confirmar bloqueados |
| **Lost** | Passou o prazo | `SetTargetFound(false)`: esconde overlays, zera seleção, “Marcador perdido” |

Fase e cronômetro (`_startedAt`) **não** pausam nem resetam. Durante Scanning o gate é resetado (sem banner). Ao entrar no Quiz o gate reseta e a câmera desliga.

## Validação no Editor

**SafetyLens → Validar projeto** exige:

- Cena (ARSession, XROrigin, TrackedImageManager, overlays, câmera única)
- Reference Image Library com **2** imagens: `SafetyLensEquipmentTarget` (WL03, 20 cm) e `SafetyLensQrTarget` (QR, 12 cm)
- Cenário com `targetName` igual ao principal + UVs em `[0,1]`; `altTargets` com um placement por hazard
- Overlay: meia-altura ativa + deslocamento de `v` rumo aos rótulos ≤ 2,03 cm; capacete ativo < círculo C (2,0 cm); zona vermelha ≤ círculo da letra
- **Android:** ARCore Loader
- **iOS:** ARKit Loader + Metal only + `cameraUsageDescription`
