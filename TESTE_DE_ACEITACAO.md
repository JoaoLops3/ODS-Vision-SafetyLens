# Teste de aceitação — SafetyLens

Alinhado aos testes descritos no artigo e aos requisitos RF/RNF.  
**Roteiro completo (abaixo): &lt; 20 min no iPhone 12.**

## Pré-requisitos

- Unity 6.3 LTS (6000.3)
- Smartphone Android 8.0+ com ARCore (ou iOS com ARKit)
- Marcador: **impresso ~20 cm** (critério do artigo) **ou** PNG 1200×800 na tela, nas condições abaixo

## Condição de QA registrada (2026-10-15)

**Validado com o marcador exibido em tela de computador; alvo impresso pendente.**

P1-5 e P1-6 saíram de “pendente QA” nessa condição. O critério de pronto do plano (e do artigo) continua citando o alvo impresso de 20 cm — essa linha fica explícita até a repetição no papel.

### Para o teste na tela valer

O PNG é 1200×800 (3:2). Sem isso, o tracking e as proporções placa/letra não são comparáveis ao impresso.

- **Proporção sem distorção.** Abrir no Preview **sem esticar** e sem zoom “ajustar à janela” que corte.
- **Largura próxima de 20 cm.** Medir na tela com régua e ajustar o zoom até ficar perto disso. A proporção placa/letra depende pouco do tamanho exato; a largura real influencia a estabilidade do tracking.
- **Brilho alto, sem auto-dim.** Desativar escurecimento automático e proteção de tela — senão o tracking cai por motivo que não é do app.
- **Evitar reflexo.** Inclinar o Mac ou mudar a luz; celular firme (reflexo e blur invalidam a foto).
- **Moiré.** Se aparecerem faixas na imagem, afastar um pouco o celular.

### Como adaptar cada teste (tela do Mac)


| Teste | Na tela do Mac |
|---|---|
| Frente / ~30° / mesa | Inclinar o **celular** (não a tela) para ~30°. “Mesa”: Mac aberto na mesa, câmera de cima, mais rasante. |
| Anel verde de “feito” | Toque + confirmar; fotografar HOT e PINCH concluídos. |
| Toque no capacete a 30–40 cm | Afastar o celular e tocar no capacete (item 3). |
| Unstable (~1 s) | Cobrir a tela com a mão ou uma folha por 1 s e descobrir. |
| Lost (≥ 3 s) | Cobrir 3–4 s, ou tirar a câmera do marcador. Depois descobrir e conferir `ElapsedSeconds` no JSON. |

Cobrir a tela costuma disparar `Limited` com a imagem ainda parcialmente visível. Se o banner não aparecer, desviar o celular de lado até o marcador sair do quadro e voltar.

## Testes automatizados (EditMode) — P1-7

No Editor: **Window → General → Test Runner → EditMode → Run All**.

Via CLI (ajuste o path do Unity):

```bash
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode \
  -assemblyNames Facens.SafetyLens.EditMode.Tests \
  -testResults Library/editmode-results.xml
```

Cobertura: `TrackingLossGate`, `LocalPositionOnMarker` (UV→metros), `SanitizeParticipantId` / `SanitizeFilePart`, `BuildCsv` (InvariantCulture + escape).

## Roteiro executável (&lt; 20 min) — P1-9

| ID | Pré-condição | Passo | Resultado esperado | ~min | Passou? |
|---|---|---|---|---|---|
| 1 | App instalado | Abrir app | Tela de ID do participante | 1 | [ ] |
| 2 | Sessão limpa | ID vazio → iniciar; depois ID válido + Abrir câmera | Validação sem ID; câmera pede permissão (se 1ª vez) | 1 | [ ] |
| 3 | Câmera aberta | Fluxo feliz: HOT → PINCH → EPI → quiz | JSON/CSV em persistência; métricas coerentes | 3 | [ ] |
| 4 | Inspeção | Frente / ~30° (inclinar **celular**) / mesa | Placas e capacete sem cobrir ENERGIA/PROTECAO/SAIDA | 2 | [ ] |
| 5 | Inspeção (RF03 / P0-1) | Toque fora; toque no **errado**; toque no **certo**; Confirmar sem toque; Confirmar com toque | Errado ↑ `ErrorCount`; Confirmar bloqueado sem seleção; certo + Confirmar avança | 2 | [ ] |
| 6 | Inspeção (P1-6) | Cobrir ~1 s e descobrir | Banner amarelo; overlays visíveis; toque/Confirmar bloqueados; seleção preservada | 1 | [ ] |
| 7 | Inspeção | Cobrir ≥ 3 s; descobrir; conferir JSON | “Marcador perdido”; overlays somem; seleção zera; `ElapsedSeconds` **não** recomeça do zero | 1 | [ ] |
| 8 | Inspeção | ~35 cm, tocar capacete (item 3); confirmar HOT/PINCH | Toque RF03 no EPI; anel verde nas placas feitas | 2 | [ ] |
| 9 | Fim / Histórico | Abrir histórico; compartilhar CSV | Lista a sessão; Share Sheet iOS abre | 2 | [ ] |
| 10 | (Opcional) | Device sem ARKit / `ARSessionState.Unsupported` | Banner vermelho: não suporta RA (não confundir com marcador perdido) | 1 | [ ] |
| 11 | Artigo | Repetir 4 no **alvo impresso 20 cm** | Pendência explícita — ainda não feito | — | [ ] |

**Registro por execução**

| Data | Build/Hash | Aparelho | Marcador | Testador | Resultado |
|---|---|---|---|---|---|
| 2026-10-15 | HEAD | iPhone 12 | Monitor Mac | | PENDENTE |

## Checklist funcional (resumo)

1. Abrir o app e ver a tela de **ID do participante**.
2. Tentar iniciar sem ID → mensagem de validação.
3. Informar ID e iniciar → câmera em modo de busca do marcador.
4. Apontar para o marcador → overlay 3D (alertas + EPI) aparece e acompanha o alvo.
5. Tocar no elemento **errado** → `ErrorCount` aumenta; elemento correto não avança indevidamente.
6. Confirmar hazards na ordem (**toque AR obrigatório** + botão Confirmar — RF03).
7. Responder o quiz; resposta errada incrementa erros; correta avança.
8. Ao finalizar, ver tempo total, erros e caminhos JSON/CSV.
9. Reiniciar e repetir com outro ID.

## Persistência

- Arquivos criados em `persistentDataPath` sem internet.
- JSON e CSV com o mesmo participantId / métricas.
- Floats no CSV com **ponto** decimal (`InvariantCulture`), mesmo em aparelho pt-BR.

## Calibração A/B/C (P1-5)

**QA:** validado com o marcador exibido em tela de computador; **alvo impresso pendente.**

Em **3 ângulos** (frente / ~30° / mesa), HOT e PINCH sentam nas letras A/B e o capacete assenta no C **sem cobrir** ENERGIA / PROTECAO / SAIDA. O “C” verde permanece identificável em volta do capacete.

| Caso | Procedimento (tela) | Esperado |
|---|---|---|
| Frente | PNG ~20 cm de largura, celular de frente | Overlays nas letras; rótulos legíveis |
| ~30° | Inclinar o **celular** (~30°), tela parada | Continua sentado; rótulos visíveis |
| Mesa | Mac na mesa; câmera de cima, rasante | EPI assentado no plano; placas visíveis |
| Borda | Marcador na tela do Mac | Tracking e âncoras coerentes com A/B/C |
| Anel “feito” | Toque + confirmar HOT e PINCH | Anel **verde** visível em volta da placa (não some atrás do quad) |
| Toque no capacete | 30–40 cm, item EPI ativo | Toque seleciona; botão Confirmar habilita (RF03) |
| Tracking perdido | Cobrir a tela vários segundos | Ver casos P1-6 abaixo |
| **Impresso 20 cm** | Repetir frente / 30° / mesa no papel | Pendência explícita do artigo — ainda não feito |

## Tracking Limited (P1-6)

**QA:** validado com o marcador exibido em tela de computador; **alvo impresso pendente.**

Cronômetro e fase **não** pausam nem resetam em nenhum caso.

| Caso | Procedimento | Esperado |
|---|---|---|
| Cobrir ~1 s e descobrir | Mão no marcador ~1 s | Banner amarelo; overlays **visíveis**; toque e Confirmar bloqueados; seleção AR preservada; inspeção continua ao descobrir |
| Cobrir ≥ 3 s | Manter coberto | “Marcador perdido”; overlays escondidos; seleção zerada |
| Reaquisição após Lost | Descobrir o marcador | Volta à inspeção; fase igual; `ElapsedSeconds` no JSON **não** recomeça do zero |
| Scanning | Cobrir **antes** de reconhecer | Comportamento antigo: reticle de busca, **sem** banner Unstable |
| Quiz com banner | 3º item selecionado; cobrir (banner); tentar Confirmar; descobrir; Confirmar | Com banner, Confirmar **bloqueado**; após recuperar tracking, entra no quiz, banner some, câmera desliga |

## Desempenho / tracking (dispositivo)

- Manter tracking estável com iluminação moderada.
- Observar fluidez (~30 FPS mínimo do artigo).
