# Teste de aceitação — SafetyLens

Alinhado aos testes descritos no artigo e aos requisitos RF/RNF.

## Pré-requisitos

- Unity 6.3 LTS (6000.3)
- Smartphone Android 8.0+ com ARCore (ou iOS com ARKit)
- Marcador impresso (~20 cm)

## Checklist funcional

1. Abrir o app e ver a tela de **ID do participante**.
2. Tentar iniciar sem ID → mensagem de validação.
3. Informar ID e iniciar → câmera em modo de busca do marcador.
4. Apontar para o marcador → overlay 3D (alertas + EPI) aparece e acompanha o alvo.
5. Tocar no elemento **errado** → `ErrorCount` aumenta; elemento correto não avança indevidamente.
6. Confirmar hazards na ordem (toque ou botão Confirmar).
7. Responder o quiz; resposta errada incrementa erros; correta avança.
8. Ao finalizar, ver tempo total, erros e caminhos JSON/CSV.
9. Reiniciar e repetir com outro ID.

## Persistência

- Arquivos criados em `persistentDataPath` sem internet.
- JSON e CSV com o mesmo participantId / métricas.

## Desempenho / tracking (dispositivo)

- Manter tracking estável com iluminação moderada.
- Observar fluidez (~30 FPS mínimo do artigo).
