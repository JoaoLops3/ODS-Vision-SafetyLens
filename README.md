# SafetyLens — Realidade Aumentada para segurança industrial

Protótipo móvel de **Realidade Aumentada (RA)** para treinamento de segurança e identificação de riscos
em equipamentos industriais. Ao apontar a câmera para um marcador colado no equipamento, o app sobrepõe
placas de advertência e EPIs obrigatórios, guia o participante na identificação de cada item, aplica um
quiz de fixação e registra métricas (tempo, erros, acertos) para análise.

Projeto alinhado ao **ODS 8 (Trabalho decente)** e **ODS 9 (Indústria e inovação)**, desenvolvido no
contexto do UPX / Facens.

---

## Funcionalidades

| Etapa | O que acontece |
|---|---|
| Identificação | Participante informa um ID (ex.: `P01`). Câmera só liga ao tocar em **Abrir câmera**. |
| Reconhecimento | `ARTrackedImageManager` encontra o marcador e inicia o cronômetro. |
| Inspeção em RA | Placas ISO 7010 (superfície quente, pinçamento) e capacete 3D aparecem sobre o marcador. O item da vez tem anel pulsante; o participante toca no item e confirma. Toque errado conta erro. |
| Quiz | 3 perguntas de fixação com feedback de certo/errado. |
| Resultado | Pontuação, tempo, erros e itens. Sessão salva offline em JSON + CSV. |
| Compartilhar | Planilha CSV via folha de compartilhamento do iOS (AirDrop, Mail, Arquivos). |
| Histórico | Lista das sessões salvas no aparelho, com exportação individual ou de todas em um CSV. |

### Requisitos do artigo cobertos

| ID | Requisito | Implementação |
|---|---|---|
| RF01 | Rastreamento de imagem | `ARTrackedImageManager` + Reference Image Library |
| RF02 | Conteúdo em RA | Placas e EPIs filhos do marcador rastreado (`SafetyLensArVisuals`) |
| RF03 | Toque / quiz | Raycast com raio mínimo de toque + quiz |
| RF04 | Cronometragem | Do reconhecimento do marcador ao fim do quiz |
| RF05 | Registro de erros | Toques e respostas incorretas |
| RF06 | Exportação JSON/CSV | `SafetyLensSessionStore` em `Application.persistentDataPath` |
| RNF01 | Android 8+ / iOS | minSdk 26 (ARCore) / ARKit |
| RNF05 | Offline | Sem rede; tudo gravado localmente |

---

## Stack

- Unity **6000.3.0f1** (Unity 6.3 LTS), Built-in Render Pipeline
- AR Foundation **6.3.5**, Apple ARKit XR Plugin **6.3.5**, Google ARCore XR Plugin **6.3.5**
- XR Plug-in Management **4.6.1**
- C# (UI em IMGUI com design tokens próprios) + plugin nativo iOS em Objective-C++ (compartilhamento)

---

## Como rodar localmente

### Pré-requisitos

- Unity Hub com **Unity 6000.3.0f1** + módulos **iOS Build Support** e/ou **Android Build Support**
- iOS: macOS com Xcode e um iPhone com ARKit (testado no **iPhone 12**)
- Android: aparelho compatível com ARCore e depuração USB ativa

### Abrir o projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com/JoaoLops3/ODS-Vision-SafetyLens.git
   ```
2. Unity Hub → **Add project from disk** → selecione a pasta do repositório.
3. Abra com Unity 6000.3.0f1 e aguarde a importação. O editor configura cena, XR, materiais e o prefab
   do capacete automaticamente.
4. Menu **SafetyLens → Validar projeto** — deve logar `VALIDACAO OK`.

### Marcador

Use `Assets/Markers/safetylens-equipment-target.png`. Impresso com **~20 cm de largura** dá a escala
correta; também funciona exibido na tela do computador (retrato ou paisagem — o app ajusta a orientação).

### Build iOS (iPhone)

1. Menu **SafetyLens → Build iOS (Xcode)** — gera o projeto em `Builds/`.
2. **Feche o Xcode** se estiver aberto e abra `Builds/Unity-iPhone.xcodeproj`.
3. Selecione seu time em *Signing & Capabilities*, conecte o iPhone, **Clean (⇧⌘K)** e **Run**.

> **Erro "double-quoted include in framework header" / Module Verifier**
> O Xcode reativa `ENABLE_MODULE_VERIFIER` no UnityFramework. O pós-build já desliga a flag e trava o
> `project.pbxproj`. Se voltar, rode `./Tools/fix-xcode-module-verifier.sh` (ou o menu
> **SafetyLens → Corrigir Module Verifier (Xcode)**) com o Xcode fechado. Se o Xcode pedir para salvar
> o projeto, escolha **Don't Save**.

### Build Android

1. Conecte o aparelho com depuração USB.
2. **SafetyLens → Build and Run Android** (ou **Build Android APK** → `Builds/SafetyLens.apk`).

### Variáveis de ambiente

**Nenhuma.** O app é 100% offline: não chama APIs, não tem chaves nem banco remoto. Os dados das
sessões ficam apenas no aparelho (`Application.persistentDataPath`, arquivos `safetylens_*`).

---

## Personalizar o cenário

Edite `Assets/Resources/safetylens_scenario.json`:

```json
{
  "id": "HOT",
  "label": "Zona quente",
  "kind": "alert",
  "instruction": "Toque na placa de alerta: superfície quente / risco de queimadura.",
  "pictogram": "hot",
  "x": -0.05, "y": 0.0, "z": 0.0
}
```

- `kind`: `alert` (placa de risco) ou `epi` (equipamento de proteção).
- `pictogram` (alert): `hot`, `pinch` ou `generic` (ponto de exclamação).
- `model` (epi): `helmet`.
- `x/y/z` em metros: `x` = direita na tela, `z` = para cima, `y` = afastamento do marcador.
- `quiz`: perguntas com `options` e `correctIndex`.

Para trocar o capacete, substitua `Assets/Models/EPI/HardHat.fbx` e rode
**SafetyLens → Regerar prefab do capacete (FBX)**.

---

## Estrutura

```text
Assets/
  Editor/
    SafetyLensProjectSetup.cs        # configura XR, cena, build iOS/Android
    SafetyLensVisualAssetsSetup.cs   # materiais, prefab do capacete, luz da cena
    SafetyLensIosPostProcess.cs      # flags do Xcode + Info.plist (câmera)
  Markers/                           # imagem do marcador
  Models/EPI/HardHat.fbx             # capacete 3D
  Plugins/iOS/SafetyLensNativeShare.mm
  Resources/
    safetylens_scenario.json         # cenário (itens + quiz)
    SafetyLens/{Materials,Prefabs}   # gerados pelo setup
  Scripts/
    SafetyLensSessionController.cs   # fluxo da sessão, tracking, toque, métricas
    SafetyLensStatusOverlay.cs       # UI (home, câmera, quiz, resultado, histórico)
    SafetyLensArVisuals.cs           # fábrica dos objetos de RA
    SafetyLensPictograms.cs          # placas ISO 7010 rasterizadas em runtime
    SafetyLensBillboard.cs           # rótulos alinhados à tela
    SafetyLensSessionStore.cs        # JSON/CSV, histórico, CSV agregado
    SafetyLensNativeShare.cs         # ponte C# → share sheet do iOS
    SafetyLensTheme.cs               # design tokens (cores, espaços, tamanhos)
    SafetyLensModels.cs              # modelos de dados
Tools/fix-xcode-module-verifier.sh
docs/ARQUITETURA.md, docs/CUSTOMIZACAO.md
TESTE_DE_ACEITACAO.md
```

---

## Decisões de arquitetura

- **Câmera sob demanda.** O `ARSession` é salvo inativo na cena e só liga em **Abrir câmera**: evita o
  indicador de câmera sem motivo e economiza bateria.
- **Referencial próprio para o conteúdo.** Os eixos do marcador mudam conforme ele está (mesa/tela,
  retrato/paisagem). Ao reconhecer o marcador, o conteúdo é alinhado à gravidade (marcador em pé) ou ao
  topo do celular (marcador deitado), então a fileira de itens sempre aparece lado a lado e legível.
- **Placas geradas em código.** Não havia modelos gratuitos confiáveis das placas ISO 7010; elas são
  desenhadas por distância assinada (SDF) em uma `Task` fora da thread principal e só enviadas à GPU na
  thread principal.
- **Shaders garantidos no build.** Materiais ficam em `Resources/` porque o Built-in RP só inclui
  shaders referenciados por assets; `Shader.Find` sozinho dá magenta no aparelho.
- **Toque tolerante.** Raycast exato e, se errar, aceita o item mais próximo dentro de 44 pt — objetos de
  poucos centímetros viram poucos pixels de longe.
- **UI em pontos lógicos.** IMGUI com base de 390 pt (iPhone 12) convertida para pixels, alturas medidas
  pelo texto e safe area respeitada; cores e espaços vêm de `SafetyLensTheme`.
- **Offline e privado.** Sem rede, sem conta; o ID é higienizado (só letras, números, `-`, `_`, espaço)
  antes de virar nome de arquivo/CSV.

Mais detalhes em `docs/ARQUITETURA.md` e `docs/CUSTOMIZACAO.md`. Roteiro de teste em
`TESTE_DE_ACEITACAO.md`.

---

## Origem

Mescla dos baselines **WL01** (image tracking + toque), **WL03** (procedimento, timing, setup do editor)
e **WL04** (sequência/erros), seguindo o artigo do projeto (participante, quiz, RF05/RF06).
