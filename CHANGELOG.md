# Changelog — SafetyLens

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
