# Customização — SafetyLens

## Conteúdo do cenário

Edite `Assets/Resources/safetylens_scenario.json`:

- `targetName` deve coincidir com o nome na Reference Image Library (`SafetyLensEquipmentTarget`).
- `hazards[]`: zonas `alert` (placa + zona vermelha) e `epi` (modelo 3D), ancoradas por **UV no marcador**.
- `quiz[]`: pergunta, opções e `correctIndex` (base 0).
- `altTargets[]`: Image Targets alternativos (ex.: QR Code) com UVs próprios por hazard.
- `equipment`: ficha obrigatória (nome, função, resumo e destaques de risco) após o tracking.
- `telemetry` (RF12): leitura simulada após o quiz. Com `enabled: false` (ou bloco ausente), o quiz vai direto ao resultado — útil para o protocolo medido do artigo.

### Telemetria simulada (RF12 / WL05)

Bloco opcional no mesmo JSON. Sem REST: o roteiro é local e determinístico.

| Campo | Significado |
|---|---|
| `enabled` | `true` abre a fase depois do quiz; `false` pula |
| `equipmentName` | Título no painel (fallback se `equipment.name` faltar) |
| `thresholds` | Limiares de atenção e crítico para temperatura, pressão e RPM |
| `samples[].atSeconds` | Instantes do roteiro (precisa existir `0`; tempos estritamente crescentes) |
| `samples[].temperature` / `pressure` / `rpm` | Valores naquele instante |
| `statusCopy` | Frases da UI (Ideal / Cuidado / Perigoso). Obrigatório se `enabled` |

O status CSV (NORMAL / ATENCAO / CRITICO) é calculado em runtime pelos limiares. A tela usa Ideal / Cuidado / Perigoso. O CSV recebe `telemetryStatus`, temp/pressão/RPM e, no fim, os tempos de etapa (`briefSeconds` … `finishedAtSeconds`). `telemetryEndedAtSeconds` é `0.00` se a fase não rodou.

O tempo de inspeção (RF04) inclui a leitura da ficha após o reconhecimento do marcador. Inspeção ativa ≈ `inspectionSeconds - briefSeconds`.

### Âncoras UV (A / B / C)

Cada hazard usa frações do alvo físico (0–1), não metros de HUD na câmera:

| Campo | Significado |
|---|---|
| `u` | 0 = borda esquerda do marcador, 1 = direita |
| `v` | 0 = borda inferior, 1 = superior |
| `x`, `y` | Ajuste fino em **metros** no plano da arte (+X direita, +Y sobe a arte) |
| `z` | Elevação em metros saindo do plano (+Y do `ARTrackedImage`). `0` = token padrão |

No cenário demo (marcador 20 × 13,3 cm; círculos A/B/C medidos no PNG):

| Letra na arte | Hazard | UV | Nota |
|---|---|---|---|
| **A** (azul / ENERGIA) | HOT | `u=0.238`, `v=0.470` | Centro do círculo em `v=0.493`; placas descem ~0,31 cm (billboard) sem invadir ENERGIA |
| **B** (laranja / PROTECAO) | PINCH | `u=0.500`, `v=0.470` | Idem |
| **C** (verde / SAIDA) | EPI (capacete) | `u=0.763`, `v=0.493` | Centro do C (modelo 3D não faz billboard) |

Tokens (`SafetyLensTheme`): placa **2,6 cm** (ativa 2,81 cm); capacete **1,6 cm** (ativa 1,73 cm, menor que o círculo de 2,0 cm); anel da placa **2,85 cm**; anel do capacete **2,4 cm**; lift 2 mm / 1 mm.

Rótulos ENERGIA/PROTECAO/SAIDA ficam **abaixo** dos círculos (topo do texto a **2,03 cm** do centro). Validar projeto soma meia-altura ativa + deslocamento de `v` para baixo.

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
  "v": 0.470,
  "x": 0.0,
  "y": 0.0,
  "z": 0.0
}
```

- `kind`: `alert` (placa) ou `epi` (equipamento).
- `pictogram` (alert): `hot`, `pinch` ou `generic`.
- `model` (epi): `helmet`.

### Alvos alternativos (`altTargets`)

Cada layout alternativo precisa de `name` (igual ao da Reference Image Library) e **um placement
por hazard** — a validação do cenário rejeita hazard sem placement, `hazardId` inexistente,
UV fora de `[0,1]` e nome duplicado ou igual ao `targetName` principal.

```json
"altTargets": [
  {
    "name": "SafetyLensQrTarget",
    "placements": [
      { "hazardId": "HOT", "u": 0.2, "v": 0.5 },
      { "hazardId": "PINCH", "u": 0.5, "v": 0.5 },
      { "hazardId": "EPI", "u": 0.8, "v": 0.5 }
    ]
  }
]
```

A sessão fica presa ao **primeiro** marcador reconhecido (WL03 ou QR); o outro é ignorado até
reiniciar. O marcador usado sai no JSON/CSV (`markerName`).

## Marcadores físicos

1. **Principal (WL03):** imprima `Assets/Markers/safetylens-equipment-target.png` com **~20 cm de largura** (mesmo valor da library).
2. **Alternativo (QR):** imprima `Assets/Markers/safetylens-qr-target.png` com **~12 cm de largura**. O QR **não é decodificado** — é rastreado como imagem. Para regerar a arte: `python3 Tools/generate-qr-marker.py` (requer `qrcode` + `pillow`).
3. Para trocar uma imagem: substitua o PNG e use **SafetyLens → Configurar projeto completo**.
4. Os nomes na library e no JSON devem permanecer `SafetyLensEquipmentTarget` e `SafetyLensQrTarget`.

## Visuais de RA

- **Placas** (`alert`): geradas em runtime por `SafetyLensPictograms` + `SafetyLensArVisuals` (shader `SafetyLens/SignUnlit`). Não há prefab HOT/PINCH — `Assets/Models/` contém só o EPI.
- **Zonas vermelhas** (`alert`): disco translúcido **deitado no plano do marcador**, sob a placa (interface do artigo). Diâmetro = círculo da letra (`ArZoneDiameter`, 2,0 cm) para não cobrir os rótulos; o estado muda só a opacidade (`ArZoneActiveAlpha` / `Idle` / `Done`) e o item ativo pulsa a opacidade — pulso desligado com *reduce motion*.
- **Capacete** (`epi`): prefab em `Resources/SafetyLens/Prefabs/Helmet`, gerado a partir de `Assets/Models/EPI/HardHat.fbx`.
  - `SafetyLensArVisuals.BuildModel` **reescala em runtime** para `ArHelmetWidth` (o prefab velho não fica com tamanho errado).
  - Trocar o FBX: **SafetyLens → Regerar prefab do capacete (FBX)**. `ConfigureProject` / Validar também regenera (`EnsureHelmetPrefab(true)`).
  - Collider acompanha o mesh (BoxCollider ~1,6 cm no prefab atual). `SafetyLensHazardTag.hazardId` é aplicado no `Body` em runtime.
  - Toque RF03: raycast no collider **e** fallback `PickHazard` com raio mínimo `TouchMin` (44 pt) — o capacete a 30–40 cm continua acionável mesmo pequeno.

## Métricas exportadas

Arquivos em `Application.persistentDataPath`:

- `safetylens_<id>_<timestamp>.json`
- `safetylens_<id>_<timestamp>.csv`

Campos: participantId, tempo total, **tempo de inspeção e de quiz separados (RF04)**, erros,
hazards, respostas do quiz, **FPS médio / percentil 1 / fração abaixo de 30 FPS / pico de
memória (RNF02)**, o **marcador usado** (`markerName`) e **tempos de etapa** (`briefSeconds`,
`inspectionEndedAtSeconds`, `quizEndedAtSeconds`, `telemetryEndedAtSeconds`, `finishedAtSeconds`).
O tempo de inspeção inclui a leitura da ficha após o tracking. Colunas novas sempre entram no fim
do CSV — arquivos antigos continuam legíveis pelas mesmas posições.

## Texto sugerido para o artigo

Trechos para alinhar o PDF ao protótipo (copiar para o Word do SharePoint):

- **RF04:** "O sistema deverá registrar, em segundos, o tempo de inspeção (do reconhecimento do marcador à confirmação do último risco, incluindo a leitura da ficha do equipamento) e o tempo de quiz, além do tempo total da sessão."
- **Interface, item 3:** "Projeção de zonas vermelhas de alerta no plano do equipamento, combinadas com placas de advertência no padrão ISO 7010 e modelos 3D de EPIs."
- **Metodologia:** "O rastreamento usa marcadores visuais 2D (Image Targets): uma placa ilustrada do equipamento e, alternativamente, um QR Code tratado como Image Target."
- **RNF02 / testes de desempenho:** "A taxa de quadros (média e percentil 1%) e o pico de memória são registrados automaticamente em cada sessão."
- **Próximos passos:** "Protótipo funcional validado em iOS; pendentes a validação em Android e o teste com alvo impresso de 20 cm."
- **Inconsistência a resolver pela equipe:** o resumo e a RQ1 falam em "controle digital interativo", e a metodologia fala em "leitura passiva de manuais 2D". Escolher uma das duas condições e usar o mesmo termo nas três seções.
