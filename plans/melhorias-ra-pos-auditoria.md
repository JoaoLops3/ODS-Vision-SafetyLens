# Plano de melhorias — SafetyLens (pós-auditoria RA)

**Data:** 2026-10-12  
**Contexto:** protótipo de campo sólido em iOS (image tracking + âncoras A/B/C + sessão/quiz/export). Ainda não está fechado como instrumento de medição nem como produto multiplataforma.

**Objetivo deste plano:** transformar “demo que funciona” em “estudo/treino confiável”, sem trocar de engine — o núcleo espacial já está correto.

---

## Veredito da auditoria


| Área                                                      | Estado   |
| --------------------------------------------------------- | -------- |
| Um trackable + âncoras UV A/B/C                           | OK       |
| Sync `onBeforeRender` com pose ARKit                      | OK       |
| EPI no espaço do marcador / placas billboard              | OK       |
| Robustez iOS (feed, FBX Camera, Module Verifier, shaders) | OK       |
| Design tokens + cenário JSON                              | OK       |
| Toque tolerante + câmera sob demanda + export offline     | OK       |
| Rigor de medição (RF03) / multiplataforma / testes        | Pendente |


---



## Sprint sugerida (primeira)

Ordem recomendada para o próximo ciclo (só o que ainda falta):

1. **P1-5** — Calibração fina UV/escala (placas + capacete no C)

Já feitos neste bloco P0: ~~P0-1~~ · ~~P0-2~~ · ~~P0-3~~ · ~~P0-4~~ (2026-10-12).

---



## ✅ P0 — risco de demo / validade do experimento

### ✅ P0-1 — “Confirmar item” sem toque no objeto AR


|                        |                                                                                                                                   |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **Status**             | **Feito** (2026-10-12)                                                                                                            |
| **Onde**               | `SafetyLensSessionController` (`SelectCurrentHazardFromAr`, `CanConfirmCurrentHazard`); `SafetyLensStatusOverlay.DrawCameraSheet` |
| **O que foi feito**    | Fluxo em 2 passos: toque no overlay AR do item ativo → habilita “Confirmar item”; botão sem seleção não confirma (RF03)           |
| **Critério de pronto** | Não é possível concluir um hazard sem interação válida no objeto AR                                                               |
| **QA**                 | Sucesso: toque no item certo + confirmar. Borda: botão desabilitado sem hit. Falha: toque no item errado conta erro               |




### ✅ P0-2 — Validar projeto checa ARKit (não só ARCore)


|                        |                                                                                                                      |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Status**             | **Feito** (2026-10-12)                                                                                               |
| **Onde**               | `Assets/Editor/SafetyLensProjectSetup.cs` → `ValidateProject`                                                        |
| **O que foi feito**    | Exige ARCore (Android) + ARKit + Metal + `cameraUsageDescription` (iOS); `targetName` JSON = library; UVs em `[0,1]` |
| **Critério de pronto** | Console: `VALIDACAO OK: ... Android=ARCore; iOS=ARKit+Metal+camera privacy.`                                         |




### ✅ P0-3 — Docs de customização (`u`/`v` no marcador)


|                        |                                                                                           |
| ---------------------- | ----------------------------------------------------------------------------------------- |
| **Status**             | **Feito** (2026-10-12)                                                                    |
| **Onde**               | `README.md`, `docs/CUSTOMIZACAO.md`, `docs/ARQUITETURA.md`                                |
| **O que foi feito**    | Documentação alinhada ao modelo UV A/B/C; removida a narrativa de HUD `x/y/z` / gravidade |
| **Critério de pronto** | Alguém novo consegue mudar âncoras só lendo `docs/CUSTOMIZACAO.md`                        |




### ✅ P0-4 — Drift iOS target 12.0 vs 15.0


|                        |                                                                                                                         |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| **Status**             | **Feito** (2026-10-12)                                                                                                  |
| **Onde**               | `SafetyLensProjectSetup.ConfigureIos`; `ProjectSettings/ProjectSettings.asset`                                          |
| **O que foi feito**    | `ConfigureIos` e `iOSTargetOSVersionString` alinhados em **12.0** (fim do drift 12 vs 15). Mínimo oficial adotado: 12.0 |
| **Opcional depois**    | Validar projeto falhar se PlayerSettings ≠ constante do setup (cinto de segurança; não bloqueia o P0)                   |
| **Critério de pronto** | Setup e ProjectSettings na mesma versão — cumprido                                                                      |


---



## P1 — fidelidade RA / manutenção



### P1-5 — Calibração fina A/B/C


|                        |                                                                                                                              |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **Status**             | Pendente                                                                                                                     |
| **Por quê**            | Placas um pouco grandes/altas; capacete ainda cobre o C demais                                                               |
| **Onde**               | `safetylens_scenario.json` (`u`/`v`/`z`); `SafetyLensTheme` (`ArSignWidth`, `ArHelmetWidth`, `ArMarkerLift`, `ArHelmetLift`) |
| **Plano**              |                                                                                                                              |
| 1                      | Foto de referência com régua mental: centro do círculo A/B/C                                                                 |
| 2                      | Ajustar `v` (descer ~0,5–1 cm) e larguras (~3,0–3,4 cm)                                                                      |
| 3                      | EPI: confirmar assento no plano + footprint que deixe o “C” legível                                                          |
| **Critério de pronto** | Em 3 ângulos (frente / 30° / mesa), overlays “sentam” nas letras sem cobrir o rótulo ENERGIA/PROTECAO/SAIDA                  |
| **QA**                 | Sucesso no alvo impresso 20 cm; borda na tela do Mac; falha com tracking perdido                                             |




### P1-6 — Tracking `Limited` some tudo de forma abrupta


|                        |                                                                                                        |
| ---------------------- | ------------------------------------------------------------------------------------------------------ |
| **Status**             | Pendente                                                                                               |
| **Por quê**            | UX “nervosa”: conteúdo pisca some/aparece                                                              |
| **Onde**               | `SafetyLensSessionController.HandleTrackedImage` / `SetTargetFound` (hoje só `TrackingState.Tracking`) |
| **Plano**              |                                                                                                        |
| 1                      | Tratar `Limited` como “ainda mostrar, com aviso” (ex.: opacidade ou toast “Segure firme”)              |
| 2                      | Só esconder overlays após N frames fora de Tracking/Limited ou timeout                                 |
| 3                      | Manter cronômetro; não resetar fase                                                                    |
| **Critério de pronto** | Passar a câmera em ângulo ruim por 1–2 s não apaga a fileira inteira                                   |




### P1-7 — Testes automatizados (EditMode)


|                        |                                                               |
| ---------------------- | ------------------------------------------------------------- |
| **Status**             | Pendente                                                      |
| **Por quê**            | UV→metros, sanitize ID, CSV, fases — tudo manual hoje         |
| **Onde**               | Novo `Assets/Tests/EditMode/` (Unity Test Framework)          |
| **Plano**              |                                                               |
| 1                      | `LocalPositionOnMarker` (centro, A, C, size fallback)         |
| 2                      | `SanitizeParticipantId` / nome de arquivo                     |
| 3                      | `BuildCsv` escape e colunas                                   |
| 4                      | (Opcional) máquina de fases sem cena AR                       |
| **Critério de pronto** | `Unity Test Runner` verde no CI ou no Editor em menos de 30 s |




### P1-8 — ARKit como Required (não Optional)


|                        |                                                                           |
| ---------------------- | ------------------------------------------------------------------------- |
| **Status**             | Pendente                                                                  |
| **Por quê**            | App pode instalar em device sem ARKit e falhar sem mensagem clara         |
| **Onde**               | `ARKitSettings.asset` (`m_Requirement`); PlayerSettings `iOSRequireARKit` |
| **Plano**              |                                                                           |
| 1                      | Marcar ARKit **Required** no setup iOS                                    |
| 2                      | Validar projeto falha se Optional                                         |
| 3                      | Mensagem amigável se sessão AR não iniciar (overlay)                      |
| **Critério de pronto** | Build iOS declara necessidade de ARKit; Validar projeto cobre isso        |




### P1-9 — Aceitação mais completa


|                        |                                                                                                                        |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| **Status**             | Pendente                                                                                                               |
| **Onde**               | `TESTE_DE_ACEITACAO.md`                                                                                                |
| **Plano**              | Incluir casos: perda de tracking, mesa vs parede/monitor, share iOS, histórico, UV A/B/C visual, botão vs toque (P0-1) |
| **Critério de pronto** | Checklist executável em menos de 20 min no iPhone 12                                                                   |


---



## P2 — polish / escala


| ID    | Problema                                                     | Onde (pista)                                    | Status   |
| ----- | ------------------------------------------------------------ | ----------------------------------------------- | -------- |
| P2-10 | Share nativo Android (Intent)                                | `SafetyLensNativeShare`                         | Pendente |
| P2-11 | a11y IMGUI + `prefers-reduced-motion` no pulso do anel       | `SafetyLensStatusOverlay`, `LateUpdate` do anel | Pendente |
| P2-12 | Extrair domínio (fases/quiz/métricas) do `SessionController` | Split `SafetyLensSessionController`             | Pendente |
| P2-13 | Validar schema JSON no boot (`u/v`, `correctIndex`)          | `LoadScenario`                                  | Pendente |
| P2-14 | Retenção/limpeza de sessões + UI apagar                      | `SafetyLensSessionStore`, `DrawHistory`         | Pendente |
| P2-15 | Remover stubs `Models/HOT|PINCH` ou alinhar docs             | `Assets/Models/…`, docs                         | Pendente |


---



## Leitura de especialista (norte)

O gap não é trocar de engine. Priorizar rigor de produto:

1. **Medição** — toque AR obrigatório (ou telemetria clara de como confirmou).
2. **Tracking UX** — degradar com elegância em `Limited`.
3. **Calibração** — fechar o último centímetro nas âncoras A/B/C.
4. **Operação** — validação Editor = verdade do iPhone; docs = código; testes mínimos no domínio.

---



## Como usar este plano

- Marque **Status** (`Pendente` → `Em andamento` → `Feito`) ao concluir cada item.
- Ao fechar um P0/P1, anote data + commit no bloco do item.
- Não misturar P2 na mesma PR de P0-1, salvo se for trivial (docs/stub).



## Referências

- `docs/ARQUITETURA.md`
- `docs/CUSTOMIZACAO.md`
- `TESTE_DE_ACEITACAO.md`
- Auditoria em chat (2026-10-12) — âncoras UV já em `main` (`2e17574`)

