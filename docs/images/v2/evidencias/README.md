# Versão 2 — Evidências de funcionamento

## Sessão exportada (CSV)

Arquivo bruto: [`safetylens_Teste_evidencias_20261018_182340.csv`](safetylens_Teste_evidencias_20261018_182340.csv)

### Identificação

| Campo | Valor |
|---|---|
| Participante | `Teste evidências` |
| Cenário | `demo-industrial-risk` — Identificação de Riscos Industriais |
| Marcador | `SafetyLensEquipmentTarget` (WL03) |
| Início (UTC) | 2026-10-18T18:22:36Z |
| Fim (UTC) | 2026-10-18T18:23:17Z |
| Export (UTC) | 2026-10-18T18:23:40Z |

### Tempos (RF04 + etapas)

| Métrica | Valor |
|---|---|
| `elapsedSeconds` (total até fim do quiz) | 41,08 s |
| `briefSeconds` (ficha do equipamento) | 16,93 s |
| `inspectionSeconds` (ficha + inspeção A/B/C) | 35,07 s |
| `quizSeconds` | 6,00 s |
| `inspectionEndedAtSeconds` | 35,07 |
| `quizEndedAtSeconds` | 41,08 |
| `telemetryEndedAtSeconds` | 64,50 |
| `finishedAtSeconds` | 64,50 |

### Inspeção e quiz

| Métrica | Valor |
|---|---|
| Riscos confirmados | 3 / 3 |
| Erros (`errorCount`) | 2 |
| Quiz | 2 / 3 corretas |
| Q1 | Superfície quente / queimadura — OK |
| Q2 | Capacete de segurança — OK |
| Q3 | Ignorar as zonas de alerta se já conhece a máquina — ERR |

### Desempenho (RNF02) e telemetria (RF12)

| Métrica | Valor |
|---|---|
| FPS médio | 53,88 |
| FPS low 1% | 29,85 |
| Fração de frames &lt; 30 FPS | 0,088 |
| Pico de memória | 96,40 MB |
| Status telemetria | `CRITICO` |
| Temperatura | 72,80 |
| Pressão | 3,35 |
| RPM | 1840 |

## Vídeo demonstrativo

| Onde | Link / arquivo |
|---|---|
| YouTube (quando publicado) | _(preencher URL)_ |
| No repositório | `Video_V2.mov` |

## Fotos locais (opcional)

| Arquivo sugerido | Conteúdo |
|---|---|
| `evidencia-ra-iphone.png` | Foto do iPhone apontado ao marcador físico com overlays visíveis |
| `evidencia-ficha-equipamento.png` | Ficha do equipamento com tracking estável (CTA ativo) |
| `evidencia-telemetria-rf12.png` | Leitura simulada Ideal / Cuidado / Perigoso |
