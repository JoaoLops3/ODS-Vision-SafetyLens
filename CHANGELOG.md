# Changelog — SafetyLens

## 1.5.0

Caminho QR, primeira fatia da máquina:

- Um reconhecimento estável do QR desliga a câmera e mostra um compressor no fundo escuro
- Giro só na horizontal; os pontos ficam na superfície
- A tabela (o que é / o que pode acontecer) abre só no toque da face virada para a pessoa
- O marcador WL03 permanece no fluxo da ficha, inspeção, quiz e telemetria
- Compressor do QR deixa de ser um bloco: tanque deitado, base, cabeçote com aletas, ventilador e tubo de saída
- O compressor entra menor e com o centro do volume no meio da tela
- O QR usa o FBX `Assets/Models/Motor/air-compressor.fbx` (CC0). Sem o prefab, volta às primitivas

## 1.4.2

Dois caminhos de RA (marcador WL03 intacto; QR redesenhado):

- **WL03:** overlays continuam ancorados no Image Target (fluxo atual)
- **QR (tela):** na ficha os models **não** aparecem em cima do código; ao iniciar a inspeção abre um viewer
- No viewer QR: **fundo sólido** (sem feed da câmera); models menores; arraste para girar e toque no item (RF03)
- Telemetria QR: tela com **tabela** Ideal/Cuidado/Perigoso (sem câmera nem valores ao vivo). Marcador WL03 inalterado
- Layout puro em `SafetyLensQrViewerLayout` + testes EditMode

## 1.4.1

- RF10: SUS e retenção 7 dias definidos como **protocolo externo** (Google Forms), não módulo no app
- Links dos Forms documentados no README (Dia 0 e Dia +7)
- Scripts Apps Script em `Tools/google-forms-sus-dia0.gs` e `Tools/google-forms-retencao-dia7.gs`

## 1.4.0

Ficha do equipamento + tempos por etapa no CSV:

- Após o reconhecimento do marcador/QR, abre a ficha (`equipment` no JSON) antes da inspeção
- O tempo de inspeção (RF04) **inclui a leitura da ficha** após o tracking; inspeção ativa ≈ `inspectionSeconds - briefSeconds`
- Colunas novas no fim do CSV: `briefSeconds`, `inspectionEndedAtSeconds`, `quizEndedAtSeconds`, `telemetryEndedAtSeconds`, `finishedAtSeconds`
- `telemetryEndedAtSeconds` é 0 se a leitura RF12 não rodou
- Telemetria na tela: Ideal / Cuidado / Perigoso (`statusCopy`); CSV de status inalterado
- CTA **Iniciar inspeção** só com tracking estável (P1-6)

## 1.3.0

Refatoração interna (SOLID) sem mudar o experimento:

- `SafetyLensSessionController` virou facade; fluxo, relógio, tracking, conteúdo 3D e telemetria saíram para colaboradores
- Fórmulas de `ElapsedSeconds` / `InspectionSeconds` / `QuizSeconds` e a origem de `ErrorCount` permanecem as mesmas
- IMGUI intacto; Image Tracking only; export JSON/CSV offline
- `SafetyLensScenarioLoader` e `SessionStore` com mensagens claras em falha de I/O
- Testes EditMode do clock, do fluxo e do loader

## 1.2.0

RF12 — leitura simulada do equipamento (idéia WL05 / aula 10):

- Fase `Telemetry` depois do quiz: temperatura, pressão, RPM e status (NORMAL / ATENÇÃO / CRÍTICO) por roteiro no `safetylens_scenario.json`
- Painel 2D com câmera ligada e overlays 3D ocultos; sem REST (RNF05)
- `telemetry.enabled: false` (ou bloco ausente) mantém o fluxo medido do artigo (quiz → resultado)
- CSV/JSON: colunas `telemetryStatus`, `temperature`, `pressure`, `rpm` no fim da linha
- Domínio puro em `SafetyLensTelemetry` + testes EditMode e validação de cenário

## 1.1.0

Alinhamento com o artigo científico (RF04, RNF02, interface e marcadores):

- RF04: tempo de inspeção e tempo de quiz medidos em separado (`inspectionSeconds` / `quizSeconds` no JSON/CSV); o total (`elapsedSeconds`) permanece
- RNF02: FPS médio, percentil 1 de FPS, fração de frames abaixo de 30 FPS e pico de memória gravados por sessão (`SafetyLensPerformanceProbe`); `targetFrameRate = 60` para o iOS não travar em 30
- Zonas vermelhas de alerta no plano do marcador, sob as placas ISO (interface descrita no artigo); opacidade por estado e pulso desligado com reduce motion
- QR Code como Image Target alternativo (`SafetyLensQrTarget`, 12 cm): rastreado como imagem, com layout próprio de âncoras via `altTargets` no cenário; sessão presa ao primeiro marcador reconhecido e `markerName` no export
- Arte do QR reproduzível via `Tools/generate-qr-marker.py`
- Validação de cenário cobre `altTargets`; validação de overlays cobre a zona vermelha
- Testes EditMode novos: sonda de desempenho, tempos no registro, `ResolveUv`, validador de `altTargets`

## 1.0.0

- Projeto Unity unificado em `Upx8/SafetyLens`
- Mescla WL01 (tracking/toque), WL03 (procedimento/timing/setup), WL04 (ordem → erros)
- Sem WL02 plane detection (fora do escopo do artigo)
- RF01–RF06: image tracking, overlay demo, toque, quiz, tempo, erros, export JSON/CSV
- Tela de identificação do participante (IMGUI)
- Cenário demo em `Resources/safetylens_scenario.json`
- Menu Editor `SafetyLens` (configurar, validar, build Android)
