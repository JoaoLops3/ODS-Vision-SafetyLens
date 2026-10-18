# SafetyLens

> **ODS Vision — SafetyLens** · UPX: Realidade Aumentada · UniFacens  
> Treinamento de segurança e identificação de riscos industriais via smartphone (Image Tracking).

![Status](https://img.shields.io/badge/status-protótipo%20funcional-success)
![Plataforma](<https://img.shields.io/badge/plataforma-iOS%20(ARKit)%20%7C%20Android%20(ARCore)-blue>)
![Tecnologia](https://img.shields.io/badge/RA-Image%20Tracking-blue)
![Unity](https://img.shields.io/badge/Unity-6000.3.0f1-black)

**Repositório:** [github.com/JoaoLops3/ODS-Vision-SafetyLens](https://github.com/JoaoLops3/ODS-Vision-SafetyLens)  
**Modelo de documentação:** [dr-ohata/upx-modelo](https://github.com/dr-ohata/upx-modelo)

---

## 1. Identificação do Projeto

| Item            | Informação                                       |
| --------------- | ------------------------------------------------ |
| **Projeto**     | SafetyLens (ODS Vision)                          |
| **Equipe**      | ODS Vision — SafetyLens                          |
| **Curso**       | Engenharia de Computação                         |
| **Disciplina**  | UPX — Realidade Aumentada                        |
| **Professor**   | Prof. Daniel Domingos Akira de Sá Pimentel Ohata |
| **Semestre**    | 2026.2 (8º semestre)                             |
| **Instituição** | UniFacens — Sorocaba, SP                         |

### Integrantes

| Nome                             | RA     | E-mail                                                                | Responsabilidade                                                                                                              |
| -------------------------------- | ------ | --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Brunno Eduardo Marfil Marins     | 236051 | [bruno.marfil@hotmail.com](mailto:bruno.marfil@hotmail.com)           | Desenvolvedor AR — integração AR Foundation / marcadores (Image Targets)                                                      |
| Bruno Romi Cervone               | 235212 | [brunoromicr@gmail.com](mailto:brunoromicr@gmail.com)                 | Engenharia de Software e testes de usabilidade — testes funcionais, validação da persistência local e protocolo da escala SUS |
| Giovane Zanzarini Iano           | 236082 | [giovanezanza@gmail.com](mailto:giovanezanza@gmail.com)               | Front-end, UI/UX e Design 3D — telas no Unity, menus do quiz e elementos visuais de risco                                     |
| Hector Silveira Colchiesqui      | 235354 | [colchiesquihector@gmail.com](mailto:colchiesquihector@gmail.com)     | Arquitetura de sistema e modelagem 3D/assets — EPIs, Image Targets e organização das camadas do sistema                       |
| João Gabriel Lopes Aguiar        | 223969 | [joaogabriellops2353@gmail.com](mailto:joaogabriellops2353@gmail.com) | Gestão de projeto e documentação — Kanban/Trello, cronograma ágil, documentação técnica e apoio em interações front-end       |
| Luis Alberto Sant'Anna de Araújo | 236401 | [Santannaluis10@gmail.com](mailto:Santannaluis10@gmail.com)           | Pesquisa científica e análise de dados — métricas (cronometragem, erros) e tratamento estatístico do experimento              |
| Miguel Muran Zvir Novaes         | 222674 | [muranmiguel@gmail.com](mailto:muranmiguel@gmail.com)                 | Redação principal e metodologia — fundamentação teórica, delineamento experimental e padronização do artigo                   |

Artigo científico da equipe (sempre a versão atual no SharePoint): [SafetyLens — Artigo científico](https://facens-my.sharepoint.com/:w:/g/personal/223969_facens_br/IQC635MrvwTJQr-ThWC7yNzgAfASb7rOqj2sZgzheg6sdYE?e=UEWZgv).

### Artigos científicos utilizados como base

Planilha completa (fonte da tabela abaixo):

- [Excel no SharePoint (Facens)](https://facens-my.sharepoint.com/:x:/g/personal/223969_facens_br/IQD6DAxNHPhGSaIQRpkXLhdFAZtFv9VKDNwsFKHG7cuFfUY?e=43COmv)
- Cópia no repositório: [`docs/documents/Tabela_Artigos_Cientificos_SafetyLens_UPX8.xlsx`](docs/documents/Tabela_Artigos_Cientificos_SafetyLens_UPX8.xlsx)

| Integrante responsável | Título do artigo                                                                                                           | Autores                                          | Ano  | Fonte / periódico / evento                                           | Objetivo do estudo                                                                                                      | Metodologia                                                                                              | Principais resultados                                                                                              | Limitações apontadas                                                                              | Contribuição para o SafetyLens (relação com o projeto)                                                                      |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ | ---- | -------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| João Gabriel           | Treinamentos utilizando a Realidade Aumentada e Virtual: comparação da inovação e tradicionalismo na formação profissional | BORGES, P. R. et al.                             | 2019 | IX CIKI (Congresso Internacional de Conhecimento e Inovação)         | Avaliar os impactos do uso de tecnologias imersivas (RA e RV) em treinamentos operacionais e corporativos.              | Estudo comparativo e exploratório sobre engajamento e usabilidade em ambientes de formação.              | Comprovou aumento significativo no engajamento, na retenção de conteúdo e na segurança operacional.                | Custos iniciais de infraestrutura e barreiras técnicas de adoção.                                 | Fundamenta a necessidade de interfaces ativas para treinamento e monitoramento de riscos.                                   |
| Brunno Marfil          | BIM-based immersive virtual reality for construction workspace planning: a safety-oriented approach                        | GETULI, V. et al.                                | 2020 | Automation in Construction                                           | Investigar a integração de ambientes imersivos no planejamento de segurança e prevenção de riscos operacionais.         | Estudo experimental aplicado com simulação de cenários de trabalho e medição de tempos de capacitação.   | Prevenção orientada à segurança com redução expressiva no tempo de capacitação dos colaboradores.                  | Foco restrito a espaços de construção fechados e complexidade de integração.                      | Fornece a base metodológica de métricas para treinamentos imersivos focados em segurança do trabalho.                       |
| Giovane Iano           | Uso de realidade virtual e aumentada no ensino de segurança do trabalho na graduação                                       | FAZINGA, W. R.; LUCENA, A. F. E.; SAFFARO, F. A. | 2019 | XI SIBRAGEC (Simpósio Brasileiro de Gestão e Economia da Construção) | Analisar a eficácia do uso de RA e RV na retenção de diretrizes e normas de segurança do trabalho.                      | Estudo experimental comparativo com aplicação de avaliações práticas antes e após o uso das ferramentas. | Constatação de maior absorção de normas de segurança e aumento na identificação de perigos.                        | Tempo elevado exigido para modelagem tridimensional prévia dos cenários.                          | Ampara a substituição dos manuais 2D estáticos por componentes virtuais interativos no SafetyLens.                          |
| Bruno Romi             | Arquitetura de sistemas para detecção de perigos com IoT e Realidade Aumentada                                             | MURTA, A.; SILVA, B.                             | 2024 | XVI SBCUP (Simpósio Brasileiro de Computação Ubíqua e Pervasiva)     | Propor aprimoramentos para a segurança na Indústria 4.0 conectando sensores IoT e Realidade Aumentada.                  | Proposição de arquitetura de software e testes de sistema para emissão de alertas em tempo real.         | Emissão contínua de alertas preventivos antes do contato do operador com fontes de risco físico.                   | Latência de comunicação de rede e complexidade de integração com sensores de campo.               | Subsidia a estrutura lógica de disparo de alertas de risco e futuras expansões sensoriais do SafetyLens.                    |
| Hector Colchiesqui     | Realidade virtual aplicada à segurança do trabalho na construção civil: uma revisão sistemática da literatura              | NETO, L. et al.                                  | 2022 | Revista Educ@ / SciELO                                               | Mapear como as tecnologias virtuais e imersivas estão sendo aplicadas para treinar a percepção de risco ocupacional.    | Revisão Sistemática da Literatura (RSL) em bases científicas nacionais e internacionais.                 | Constatação de que simulações capacitam a percepção de risco sem expor o trabalhador ao perigo físico real.        | Dependência de óculos imersivos de alto custo e equipamentos pesados de processamento.            | Justifica a escolha técnica do SafetyLens em rodar via smartphones comerciais acessíveis (democratização do acesso).        |
| Luís Alberto           | Usos e benefícios da Realidade Aumentada na Indústria 4.0                                                                  | SILVA, M. P. et al.                              | 2022 | Revista Brasileira de Mecatrônica (v. 4, n. 4)                       | Investigar as aplicações práticas e ganhos operacionais da sobreposição de informações por RA em ambientes industriais. | Artigo de revisão e estudo de caso aplicado em linhas operacionais assistidas.                           | Melhorias na precisão de montagem, redução de falhas humanas e suporte em tempo real com mãos livres.              | Dependência de conexão de rede estável e curva de adaptação dos operadores à interface.           | Fundamenta a sobreposição de instruções de EPIs e alertas de peças móveis diretamente sobre a imagem da máquina.            |
| Miguel Muran           | The application of augmented reality technologies for the improvement of occupational safety in an industrial environment  | TATIC, D.; TESIC, B.                             | 2017 | Computers in Industry (v. 85, p. 1–10)                               | Investigar empiricamente o impacto da RA na diminuição de erros e velocidade de resposta a riscos industriais.          | Estudo experimental com implementação e testes de protótipo de RA em ambiente fabril real.               | Comprovação estatística de redução na ocorrência de erros operacionais e aceleração na detecção de zonas de risco. | Restrições de capacidade gráfica dos dispositivos móveis e necessidade de calibrar as interfaces. | Valida internacionalmente a hipótese central do SafetyLens de que a RA mitiga falhas humanas e otimiza o tempo de resposta. |

---

## 2. Visão Geral

O **SafetyLens** é um protótipo móvel de Realidade Aumentada para **treinamento de segurança** e **identificação de riscos industriais**. O problema tratado é a alta carga cognitiva dos treinamentos tradicionais baseados em manuais 2D: o trabalhador alterna atenção entre o papel e a máquina real, o que favorece erros de percepção e habilidade.

O público são operadores industriais, técnicos de segurança e estudantes em formação. A solução usa o **smartphone** (sem óculos de alto custo): o participante aponta a câmera para um **marcador** (Image Target) afixado no equipamento — placa WL03 (~20 cm) ou QR alternativo (~12 cm); o app sobrepõe zonas vermelhas, placas de alerta e EPIs no contexto espacial, guia a inspeção na ordem correta, aplica um quiz e exporta métricas (tempo de inspeção/quiz, erros, FPS, respostas) em JSON/CSV **offline**.

A RA participa como **camada de ancoramento espacial**: a sessão trava no primeiro `ARTrackedImage` reconhecido; HOT, PINCH e EPI são filhos locais (letras **A / B / C** no WL03, ou layout `altTargets` no QR), alinhados à pose do ARKit/ARCore via AR Foundation.

---

## 3. Problema

**Contexto.** Na Indústria 4.0, capacitação e segurança do trabalho são prioridades alinhadas aos ODS 4.4 e 8.8. Treinamentos ainda dependem fortemente de manuais impressos e materiais 2D.

**Situação atual.** O operador precisa transferir conhecimento do papel para o maquinário 3D, alternando o olhar várias vezes. Essa alternância eleva a carga cognitiva e aumenta a chance de falhas humanas.

**Dificuldade.** Soluções imersivas com óculos dedicados existem, mas têm custo e barreira de adoção altos para muitas empresas.

**Público afetado.** Operadores, técnicos de SST e instituições de formação técnica/engenharia.

**Impacto.** Erros de identificação de risco e EPI inadequado contribuem para acidentes; falta um método ativo, acessível e mensurável (tempo, erros, retenção) via smartphone.

---

## 4. Objetivos

### 4.1 Objetivo Geral

Desenvolver e validar um protótipo móvel de Realidade Aumentada (**SafetyLens**) que apoie a identificação de riscos industriais e a fixação de conhecimento com coleta automática de métricas.

### 4.2 Objetivos Técnicos

- Implementar reconhecimento de imagens (Image Tracking) com AR Foundation (WL03 + QR).
- Sobrepor zonas vermelhas, placas de alerta e modelo 3D de EPI ancorados ao marcador.
- Permitir interação por toque nos overlays (RF03) e quiz de fixação.
- Cronometrar inspeção e quiz em separado e registrar erros (RF04 / RF05).
- Medir FPS/memória na inspeção (RNF02) e exportar JSON/CSV local (RF06) sem rede (RNF05).
- Executar e testar em dispositivo móvel (iOS com ARKit validado; Android com ARCore configurado).
- Documentar instalação, execução, testes e limitações no GitHub.

---

## 5. Público-Alvo

| Aspecto           | Descrição                                                                                            |
| ----------------- | ---------------------------------------------------------------------------------------------------- |
| **Perfil**        | Operadores industriais, técnicos de segurança do trabalho, estudantes de engenharia/formação técnica |
| **Contexto**      | Treinamento / inspeção guiada junto a um equipamento com marcador físico                             |
| **Necessidades**  | Ver o risco **no lugar** da máquina; registrar desempenho; baixo custo de hardware                   |
| **Limitações**    | Precisa de smartphone com ARKit ou ARCore; iluminação e visibilidade do marcador                     |
| **Familiaridade** | Uso básico de smartphone; instrução inicial em menos de 2 minutos (RNF04 do artigo)                  |

Materiais complementares (persona, jornada): pasta [`docs/documents/`](docs/documents/).

---

## 6. Funcionalidades

| ID   | Funcionalidade                    | Descrição                                                                           | Status |
| ---- | --------------------------------- | ----------------------------------------------------------------------------------- | ------ |
| RF01 | Rastreamento de imagem            | Identifica os Image Targets (`SafetyLensEquipmentTarget` e `SafetyLensQrTarget`) via `ARTrackedImageManager` | ✅     |
| RF02 | Exibição de conteúdo em RA        | Zonas vermelhas de alerta + placas HOT/PINCH e capacete EPI filhos do marcador (UV A/B/C)                    | ✅     |
| RF03 | Interação por toque               | Toque no overlay do item ativo + confirmação; toque errado conta erro; quiz                                  | ✅     |
| RF04 | Cronometragem                     | Tempo de inspeção (marcador → último risco, **inclui a leitura da ficha**) e tempo de quiz, mais o total     | ✅     |
| RF05 | Registro de erros                 | Contabiliza seleções e respostas incorretas                                                                  | ✅     |
| RF06 | Exportação local                  | JSON + CSV em `persistentDataPath` (tempos RF04, etapas, FPS/memória, marcador); histórico e share iOS       | ✅     |
| RF07 | Câmera sob demanda                | `ARSession` só ativa após “Abrir câmera”                                                                     | ✅     |
| RF08 | Histórico de sessões              | Lista e reexporta sessões salvas no aparelho                                                                 | ✅     |
| RF09 | Share Android nativo              | Intent + FileProvider (`SafetyLensNativeShare`)                                                              | ✅     |
| RF10 | Experimento SUS / retenção 7 dias | Protocolo do artigo **fora do app** (Google Forms + reteste em 7 dias); o app exporta métricas de sessão     | ✅     |
| RF11 | Marcador QR alternativo           | QR Code rastreado como Image Target (12 cm), com layout próprio de âncoras (`altTargets`)                    | ✅     |
| RF12 | Leitura simulada do equipamento   | Após o quiz: temperatura, pressão, RPM; UI Ideal/Cuidado/Perigoso; CSV NORMAL/ATENCAO/CRITICO; sem REST      | ✅     |

**Legenda:** ✅ Implementado · 🚧 Em desenvolvimento · ⬜ Planejado · ❌ Cancelado

### RF10 — Google Forms (protocolo externo)

| Momento | Formulário | Link |
| --- | --- | --- |
| Dia 0 (após a sessão no app) | [SafetyLens — Usabilidade (SUS) — Dia 0](https://docs.google.com/forms/d/e/1FAIpQLScPRBjHCm_0_71R-zh___C8pVwI2r2aRMy5DMoN1E-1upNS9A/viewform?usp=sharing) | Compartilhar com o participante |
| Dia +7 | [SafetyLens — Retenção de conteúdo — Dia +7](https://docs.google.com/forms/d/e/1FAIpQLSdimYtWhPwys-iYmTVu39yTXjlomWT_0wiKFxOCUhSRv-wsiA/viewform?usp=sharing) | Mesmo nome/e-mail do Dia 0 |

Scripts geradores (Apps Script): [`Tools/google-forms-sus-dia0.gs`](Tools/google-forms-sus-dia0.gs) e [`Tools/google-forms-retencao-dia7.gs`](Tools/google-forms-retencao-dia7.gs).

---

## 7. Requisitos Não Funcionais

| ID    | Categoria       | Requisito                                                                          |
| ----- | --------------- | ---------------------------------------------------------------------------------- |
| RNF01 | Compatibilidade | Smartphones convencionais Android 8+ (ARCore) / iOS (ARKit), sem LiDAR obrigatório |
| RNF02 | Desempenho      | Meta ≥ 30 FPS — **medido automaticamente**: FPS médio, percentil 1, fração < 30 FPS e pico de memória saem no CSV de cada sessão |
| RNF03 | Estabilidade    | Manter ancoramento sob variações moderadas de iluminação                           |
| RNF04 | Usabilidade     | Fluxo compreensível com instrução breve                                            |
| RNF05 | Offline         | RA e gravação de dados sem internet                                                |
| RNF06 | Privacidade     | Sem conta/nuvem; ID higienizado; descrição de uso da câmera no iOS                 |
| RNF07 | Documentação    | Repositório permite configurar e executar o projeto                                |

> **Decisão (RNF02):** o app define `Application.targetFrameRate = 60`. Sem isso o iOS trava a
> renderização em 30 FPS e a média ficaria colada no mínimo do artigo, impossibilitando provar
> "≥ 30 FPS". Custo assumido: mais bateria e aquecimento durante a sessão.

---

## 8. Tecnologias Utilizadas

| Tecnologia              | Versão                   | Utilização                                                                                                                                                                |
| ----------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Unity                   | **6000.3.0f1** (6.3 LTS) | Engine principal (Built-in Render Pipeline)                                                                                                                               |
| C#                      | compatível com Unity 6   | Scripts de sessão, UI, persistência, RA                                                                                                                                   |
| AR Foundation           | **6.3.5**                | `ARTrackedImageManager`, sessão, câmera                                                                                                                                   |
| Apple ARKit XR Plugin   | **6.3.5**                | Image Tracking no iOS                                                                                                                                                     |
| Google ARCore XR Plugin | **6.3.5**                | Image Tracking no Android                                                                                                                                                 |
| XR Plugin Management    | **4.6.1**                | Loaders ARKit/ARCore                                                                                                                                                      |
| Objective-C++           | —                        | Plugin iOS de compartilhamento (`SafetyLensNativeShare.mm`)                                                                                                               |
| Git / GitHub            | —                        | Controle de versão e documentação                                                                                                                                         |
| Trello                  | —                        | Gestão da equipe ([board ODS Vision SafetyLens](https://trello.com/invite/b/6a9c364bf3a2c97532634894/ATTId1a224107fc31f8c12e0758e34583e5a251C5E4B/ods-vision-safetylens)) |

---

## 9. Hardware Utilizado

| Equipamento | Modelo                  | Característica relevante | Utilização                          |
| ----------- | ----------------------- | ------------------------ | ----------------------------------- |
| Computador  | **MacBook (chip M2)**   | macOS; Unity + Xcode     | Desenvolvimento e build iOS         |
| Smartphone  | **iPhone 12**           | iOS + ARKit              | Execução e testes de Image Tracking |
| Alvo físico | Impressão / tela ~20 cm | Marcador WL03            | Image Target principal              |
| Alvo físico | Impressão / tela ~12 cm | Marcador QR              | Image Target alternativo            |

> Não declaramos compatibilidade com dispositivos não testados pela equipe.

---

## 10. Arquitetura do Sistema

![Arquitetura do sistema](docs/diagrams/arquitetura.svg)

### Fluxo resumido

```text
Usuário
  → Interface (SafetyLensStatusOverlay)
    → Sessão / métricas (SafetyLensSessionController)
      → Unity (cena, raycast, render)
        → AR Foundation (ARTrackedImageManager)
          → ARKit / ARCore
            → Câmera e sensores
  → Persistência local JSON/CSV (SafetyLensSessionStore)
```

Detalhes adicionais: `[docs/ARQUITETURA.md](docs/ARQUITETURA.md)`.

---

## 11. Funcionamento da Realidade Aumentada

### 11.1 Técnica utilizada

**Image Tracking** (marcador / Image Target), via **AR Foundation** sobre **ARKit** (iOS) ou **ARCore** (Android).

Há **dois marcadores** na Reference Image Library:

| Marcador | Nome na library | Tamanho físico | Âncoras |
|---|---|---|---|
| Placa WL03 (principal) | `SafetyLensEquipmentTarget` | 20 × 13,3 cm | Overlays no marcador (UV A/B/C) |
| QR Code (alternativo) | `SafetyLensQrTarget` | 12 × 12 cm | Scan na tela → viewer 3D com orbit |

O QR **não é decodificado** como código de barras: ele é um Image Target. A sessão trava no
**primeiro** alvo reconhecido (`markerName` no CSV). Arte do QR: `Tools/generate-qr-marker.py`.

**Dois modos após a ficha:**

- **WL03:** models continuam ancorados na placa (fluxo clássico).
- **QR (sempre na tela do computador):** models **não** ficam em cima do QR; ao tocar **Iniciar inspeção** abre um viewer com **fundo sólido** (câmera desligada) — arraste para girar, toque no item (RF03).

Não utilizamos Plane Detection como fluxo principal, nem óculos dedicados.

### 11.2 Fluxo técnico

![Fluxo de RA](docs/diagrams/fluxo-ra.svg)

1. Usuário informa ID e toca em **Abrir câmera** (`ARSession` ativa).
2. `ARTrackedImageManager` busca `SafetyLensEquipmentTarget` **ou** `SafetyLensQrTarget`.
3. Com `TrackingState.Tracking`, trava a sessão nesse alvo e inicia o cronômetro (RF04).
4. Abre a **ficha do equipamento** (no QR, sem overlays em cima do código).
5. **WL03:** conteúdo parentalizado no `ARTrackedImage` (UV). **QR:** viewer na frente da câmera (`SafetyLensQrViewerLayout`).
6. Toque no item ativo **seleciona** (RF03); **Confirmar item** conclui o passo. No QR, arraste gira os models.
7. Após HOT → PINCH → EPI, segue quiz, telemetria (se habilitada) e exportação (RF06).

### 11.3 Elementos virtuais

- zonas vermelhas de alerta deitadas no plano do marcador, sob as placas (interface do artigo; opacidade por estado, pulso respeita reduce motion);
- placas de advertência (HOT / PINCH) geradas por pictograma;
- capacete 3D (EPI);
- anel de destaque no item ativo;
- rótulos billboard;
- UI de sessão (toast, sheet, quiz, resultado).

Marcadores: [WL03 ~20 cm](docs/images/marcador-wl03-equipment-target.png) (principal) e [QR ~12 cm](docs/images/marcador-qr-target.png) (alternativo). Customização UV: `[docs/CUSTOMIZACAO.md](docs/CUSTOMIZACAO.md)`.

---

## 12. Estrutura do Projeto

```text
SafetyLens/
│
├── Assets/
│   ├── Editor/                 # Setup XR, cena, build iOS/Android, pós-process Xcode
│   ├── Markers/                # Image Targets: WL03 (20 cm) + QR (12 cm)
│   ├── Models/EPI/             # FBX do capacete
│   ├── Plugins/iOS/            # Share sheet nativo
│   ├── ReferenceImages/        # XR Reference Image Library
│   ├── Resources/              # Cenário JSON, materiais, prefab Helmet, shader
│   ├── Scenes/SafetyLensAR.unity
│   ├── Scripts/                # Código da aplicação
│   └── XR/                     # Loaders e settings ARKit/ARCore
│
├── Packages/                   # manifest (AR Foundation 6.3.5, …)
├── ProjectSettings/
├── Tools/                      # fix-xcode-module-verifier.sh + generate-qr-marker.py
├── plans/                      # planos de melhoria pós-auditoria
│
├── docs/
│   ├── diagrams/               # arquitetura, fluxo RA
│   ├── documents/              # planilha de artigos-base e anexos (artigo do projeto via SharePoint)
│   ├── images/                 # screenshots e marcador (ver README da pasta)
│   ├── ARQUITETURA.md
│   └── CUSTOMIZACAO.md
│
├── .gitignore
├── CHANGELOG.md
├── TESTE_DE_ACEITACAO.md
└── README.md
```

| Pasta                                | Finalidade                         |
| ------------------------------------ | ---------------------------------- |
| `Assets/Scripts`                     | Código produzido pela equipe       |
| `Assets/Scenes`                      | Cena única `SafetyLensAR`          |
| `Assets/Markers` + `ReferenceImages` | Image Target e library             |
| `docs/diagrams`                      | Diagramas técnicos                 |
| `docs/images`                        | Marcador + `v1/interface` e `v1/evidencias` |
| `docs/documents`                     | Planilha de artigos-base e anexos (artigo do projeto no SharePoint) |

---

## 13. Principais Scripts

| Script                                 | Responsabilidade                                                  |
| -------------------------------------- | ----------------------------------------------------------------- |
| `SafetyLensSessionController.cs`       | Facade da sessão (API estável para o IMGUI) |
| `SafetyLensSessionFlow.cs`             | Máquina de estados `AppPhase` |
| `SafetyLensSessionClock.cs`            | Relógios RF04 (elapsed / inspeção / quiz), testável |
| `SafetyLensStageTimeline.cs`           | Instantes das etapas (relativos ao 1º tracking) |
| `SafetyLensArTrackingService.cs`       | Image Tracking, graça P1-6, câmera sob demanda |
| `SafetyLensArContentService.cs`        | Overlays 3D, UV, pick RF03 + viewer QR |
| `SafetyLensQrViewerLayout.cs`          | Posições dos models no viewer QR (fora do código) |
| `SafetyLensTelemetry.cs`               | RF12: amostra por tempo da fase + status por limiares (domínio puro) |
| `SafetyLensTelemetrySession.cs`        | Estado da fase RF12 (não altera relógios de pesquisa) |
| `SafetyLensScenarioLoader.cs`          | Load + validação do JSON de cenário |
| `SafetyLensStatusOverlay.cs`           | UI IMGUI (identificação, câmera, quiz, telemetria, resultado, histórico) |
| `SafetyLensArVisuals.cs`               | Fábrica dos overlays (placas + capacete)                          |
| `SafetyLensPictograms.cs`              | Rasterização das placas ISO em background                         |
| `SafetyLensBillboard.cs`               | Orientação legível dos rótulos                                    |
| `SafetyLensSessionStore.cs`            | Persistência JSON/CSV e histórico                                 |
| `SafetyLensPerformanceProbe.cs`        | FPS/memória da inspeção (RNF02), classe pura testável             |
| `SafetyLensNativeShare.cs` + `.mm`     | Compartilhamento iOS                                              |
| `SafetyLensTheme.cs`                   | Design tokens                                                     |
| `SafetyLensModels.cs`                  | Modelos de dados e conversão UV → metros                          |
| `SafetyLensProjectSetup.cs` (Editor)   | Configuração XR, validação, build                                 |
| `SafetyLensIosPostProcess.cs` (Editor) | Flags Xcode / Module Verifier / câmera plist                      |

---

## 14. Dependências

```text
com.unity.xr.arfoundation     6.3.5
com.unity.xr.arkit            6.3.5
com.unity.xr.arcore           6.3.5
com.unity.xr.management       4.6.1
```

Definidas em `Packages/manifest.json`. Obtidas pelo Unity Package Manager ao abrir o projeto.

---

## 15. Configuração do Ambiente

### Pré-requisitos

- macOS (desenvolvimento iOS) ou Windows (Android);
- [Unity Hub](https://unity.com/download);
- **Unity 6000.3.0f1** com módulos:
  - **iOS Build Support** (teste no iPhone), e/ou
  - **Android Build Support** (ARCore);
- Xcode (macOS) para deploy iOS;
- Git;
- iPhone com ARKit **ou** Android com ARCore;
- Marcador impresso/exibido: WL03 com **~20 cm** de largura e/ou QR com **~12 cm**.

### Procedimento

1. Instale o Unity Hub e a versão **6000.3.0f1**.
2. Inclua iOS e/ou Android Build Support.
3. Instale Git (e Xcode, se for iOS).
4. Clone o repositório (comando na seção 16).
5. Unity Hub → **Add** → pasta do projeto.
6. Abra com 6000.3.0f1 e aguarde a importação dos packages.
7. Menu **SafetyLens → Validar projeto** — espere `VALIDACAO OK` no Console.
8. Confira XR: ARKit (iOS) / ARCore (Android) habilitados.

---

## 16. Como Executar

```bash
git clone https://github.com/JoaoLops3/ODS-Vision-SafetyLens.git
cd ODS-Vision-SafetyLens
```

### iOS (iPhone) — fluxo validado pela equipe

1. Abra o projeto no Unity Hub (6000.3.0f1).
2. Abra a cena `Assets/Scenes/SafetyLensAR.unity`.
3. Menu **SafetyLens → Build iOS (Xcode)** → gera `Builds/` (pasta ignorada pelo Git; gerar localmente).
4. Feche o Xcode se estiver aberto; abra `Builds/Unity-iPhone.xcodeproj`.
5. Em caso de erro _Module Verifier / double-quoted include_: `./Tools/fix-xcode-module-verifier.sh` (Xcode fechado). Se pedir salvar o `pbxproj`, escolha **Don't Save**.
6. Signing & Capabilities → seu time Apple.
7. **Clean (⇧⌘K)** e **Run (⌘R)** no iPhone.

### Android

1. Dispositivo com ARCore e USB debugging.
2. **SafetyLens → Build and Run Android** (ou APK em `Builds/`).

---

## 17. Como Utilizar a Aplicação

1. Abra o app e autorize a **câmera**.
2. Digite o ID do participante (ex.: `P01`) → **Abrir câmera**.
3. Aponte para o marcador WL03 (~20 cm) **ou** o QR (~12 cm). A sessão fica no primeiro reconhecido.
4. Aguarde o reconhecimento (cronômetro inicia) e leia a **ficha do equipamento**.
5. Com o marcador estável, toque em **Iniciar inspeção**. Para cada item (HOT → PINCH → EPI):

- toque no overlay destacado na câmera (seleção);
- toque em **Confirmar item** no card inferior.

6. Responda o quiz de fixação.
7. Se a telemetria estiver habilitada no cenário (RF12), observe a leitura do equipamento (valores e status mudam sozinhos) e toque em **Concluir leitura**.
8. Veja o resultado; exporte/compartilhe CSV se necessário.
9. Use **Histórico** para sessões anteriores.

### Screenshots da interface (versão 2 — atual)

Pasta: [`docs/images/v2/interface/`](docs/images/v2/interface/) · nomes em [`v2/interface/README.md`](docs/images/v2/interface/README.md)

| # | Tela | Arquivo | Finalidade |
|---|---|---|---|
| 01 | Inicial | `01-tela-inicial.PNG` | Informar ID e abrir câmera |
| 02 | Histórico | `02-tela-historico.PNG` | Ver sessões anteriores |
| 03 | Câmera aberta | `03-tela-camera-aberta.PNG` | Scanning do marcador |
| 03b | Apontada à imagem | `03b-tela-apontada-imagem.PNG` | Marcador reconhecido |
| 04 | Ficha do equipamento | `04-tela-ficha-equipamento.PNG` | Equipment Brief (CTA com tracking estável) |
| 05 | Zona quente | `05-tela-confirmado-zona-quente.PNG` | Confirmar HOT |
| 06 | Ponto de pinçamento | `06-tela-ponto-pincamento.PNG` | Item PINCH ativo |
| 07 | Confirmando pinçamento | `07-tela-confirmando-pincamento.PNG` | Confirmar PINCH |
| 08 | Capacete / EPI | `08-tela-capacete.PNG` | Item EPI |
| 09 | Quiz 1 | `09-quiz-pergunta1.PNG` | Fixação — pergunta 1 |
| 10 | Quiz 2 | `10-quiz-pergunta2.PNG` | Fixação — pergunta 2 |
| 11 | Quiz 3 | `11-quiz-pergunta3.PNG` | Fixação — pergunta 3 |
| 12 | Telemetria Ideal | `12-tela-telemetria-ideal.PNG` | RF12 — Ideal |
| 12b | Telemetria Cuidado | `12b-tela-telemetria-cuidado.PNG` | RF12 — Cuidado |
| 12c | Telemetria Perigoso | `12c-tela-telemetria-perigoso.PNG` | RF12 — Perigoso |
| 13 | Final | `13-tela-final.PNG` | Resultado e métricas |
| 14 | Compartilhar | `14-tela-compartilhando.PNG` | Share sheet do CSV |

Galeria da **versão 1** (histórico): [`docs/images/v1/interface/`](docs/images/v1/interface/).

---

## 18. Fluxo de Funcionamento

```mermaid
flowchart TD
    A[Início] --> B[Informar ID]
    B --> C[Abrir câmera]
    C --> D[Buscar marcador]
    D --> E{Tracking?}
    E -- Não --> D
    E -- Sim --> Brief[Ficha do equipamento]
    Brief --> F[Overlays A/B/C]
    F --> G[Toque no item ativo]
    G --> H[Confirmar item]
    H --> I{Todos os itens?}
    I -- Não --> F
    I -- Sim --> J[Quiz]
    J --> T{Telemetria RF12?}
    T -- Sim --> U[Leitura do equipamento]
    U --> K[Resultado + export JSON/CSV]
    T -- Não --> K
    K --> L[Fim]
```

---

## 19. Caso de Uso (resumo)

| Ator                    | Objetivo                  | Fluxo principal                                                                 |
| ----------------------- | ------------------------- | ------------------------------------------------------------------------------- |
| Participante            | Completar inspeção guiada | ID → câmera → marcador → ficha → selecionar/confirmar riscos → quiz → export |
| Pesquisador / instrutor | Coletar métricas          | Lê JSON/CSV gerados no aparelho (tempo, erros, respostas)                       |

---

## 20. Fluxo de Navegação

```text
Identificação (ID)
    ↓
Scanning (câmera)
    ↓
Equipment Brief (ficha)
    ↓
Risk Inspection (HOT → PINCH → EPI)
    ↓
Quiz
    ↓
Telemetry (RF12, se enabled no JSON)
    ↓
Finished (resultado / share)
    ↔ History (opcional)
```

---

## 21. Interface

Capturas da **versão 2 — atual** (ficha + RF12) e histórico da v1:

```text
docs/images/
├── marcador-wl03-equipment-target.png   ← Image Target principal (~20 cm)
├── marcador-qr-target.png               ← Image Target alternativo (~12 cm)
├── v1/
│   ├── interface/                    ← screenshots v1 (01…12) ✅
│   └── evidencias/
│       └── Video_V1.mov              ← demonstração v1 ✅
└── v2/
    ├── interface/                    ← screenshots v2 (01…14) ⬜
    └── evidencias/                   ← Video_V2.mov / fotos ⬜
```

**Demonstração em vídeo:**
- v1 — YouTube: [Shorts](https://youtube.com/shorts/QbRCsMurnhY) · [`Video_V1.mov`](docs/images/v1/evidencias/Video_V1.mov)
- v2 — pasta [`docs/images/v2/evidencias/`](docs/images/v2/evidencias/) (pendente)

Ordem das telas v2: [`docs/images/v2/interface/README.md`](docs/images/v2/interface/README.md). Galeria na seção [17. Como Utilizar](#17-como-utilizar-a-aplicação).

---

## 22. Testes Técnicos

| Teste              | Procedimento                     | Resultado esperado            | Resultado obtido          | Status |
| ------------------ | -------------------------------- | ----------------------------- | ------------------------- | ------ |
| Validação Editor   | SafetyLens → Validar projeto     | ARKit + ARCore + UV OK        | `VALIDACAO OK`            | ✅     |
| Câmera sob demanda | Abrir app sem tocar Abrir câmera | Sem feed ativo                | Sessão inicia inativa     | ✅     |
| Image Tracking WL03| Apontar marcador ~20 cm (tela/papel) | Detecta target e inicia tempo | Detectado no iPhone 12 (tela) | ✅     |
| Image Tracking QR  | Apontar QR ~12 cm (tela)         | Detecta `SafetyLensQrTarget`  | Detectado no iPhone 12    | ✅     |
| Overlays A/B/C     | Após tracking WL03               | HOT/PINCH/EPI nas letras      | Âncoras UV estáveis       | ✅     |
| RF03 toque         | Confirmar sem tocar no AR        | Botão desabilitado            | Bloqueado                 | ✅     |
| RF03 correto       | Toque no item + Confirmar        | Avança etapa                  | Funciona                  | ✅     |
| Quiz               | Responder perguntas              | Feedback certo/errado         | Funciona                  | ✅     |
| Export             | Finalizar sessão                 | JSON + CSV (tempos/FPS/marcador) | Funciona               | ✅     |
| Share iOS          | Compartilhar CSV                 | Share sheet                   | Funciona                  | ✅     |
| EditMode           | Test Runner → Run All            | Todos verdes                  | 34/34                     | ✅     |
| Android end-to-end | Build and Run ARCore             | Paridade com iOS              | _A registrar pela equipe_ | 🚧     |

Roteiro ampliado: `[TESTE_DE_ACEITACAO.md](TESTE_DE_ACEITACAO.md)`.

---

## 23. Dispositivos Testados

| Dispositivo | Sistema | Versão     | Resultado                                    |
| ----------- | ------- | ---------- | -------------------------------------------- |
| iPhone 12   | iOS     | **18.7.8** | Compatível — Image Tracking e fluxo completo |
| MacBook M2  | macOS   | —          | Desenvolvimento / build Xcode                |

---

## 24. Limitações Conhecidas

- Dependência de **marcador** visível e bem iluminado (WL03 ~20 cm ou QR ~12 cm).
- Nota `arcoreimg`: QR = **100** (acima da meta 75); WL03 = **10**. O iPhone 12 já rastreia o WL03 na prática; o QR é o alvo mais “legível” para o ARCore.
- Em `TrackingState.Limited` há graça (~1,5 s) com banner; após o prazo o overlay some e a seleção zera (cronômetro não reseta).
- Calibração fina de UV/escala ainda pode ser refinada (capacete vs letra C; layout do QR na tela).
- Share nativo: iOS validado; Android implementado (Intent + FileProvider), QA end-to-end pendente.
- Protocolo SUS + retenção 7 dias: **Google Forms** (fora do app). Links na seção RF10 acima. O SafetyLens grava tempo/erros/CSV; a equipe aplica o questionário e o reteste.
- UI em IMGUI: acessibilidade limitada (VoiceOver / Dynamic Type).
- Build iOS exige workaround do **Module Verifier** do Xcode em alguns ambientes.

Plano de melhorias: `[plans/melhorias-ra-pos-auditoria.md](plans/melhorias-ra-pos-auditoria.md)`.

---

## 25. Evidências de Funcionamento

| Evidência               | Onde colocar / link                                                                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------- |
| Screenshots v2 Marcador | [`docs/images/v2/interface/Marcador/`](docs/images/v2/interface/Marcador/) (folha impressa) ✅        |
| Screenshots v2 QR       | `docs/images/v2/interface/QR/` ⬜                                                                   |
| Tabela CSV v2 (sessão)  | [`docs/images/v2/evidencias/`](docs/images/v2/evidencias/) · [tabela](docs/images/v2/evidencias/README.md) ✅ |
| Evidência v2 no iPhone  | [`docs/images/v2/evidencias/`](docs/images/v2/evidencias/) _(vídeo/fotos)_ ⬜                         |
| Screenshots v1          | [`docs/images/v1/interface/`](docs/images/v1/interface/) (01–12) ✅                                   |
| Vídeo demonstrativo (v1)| [YouTube Shorts](https://youtube.com/shorts/QbRCsMurnhY) · [`Video_V1.mov`](docs/images/v1/evidencias/Video_V1.mov) ✅ |
| Artigo científico       | [SharePoint (Word — versão atualizada)](https://facens-my.sharepoint.com/:w:/g/personal/223969_facens_br/IQC635MrvwTJQr-ThWC7yNzgAfASb7rOqj2sZgzheg6sdYE?e=UEWZgv) |
| SUS Dia 0 (Forms)       | [Usabilidade (SUS)](https://docs.google.com/forms/d/e/1FAIpQLScPRBjHCm_0_71R-zh___C8pVwI2r2aRMy5DMoN1E-1upNS9A/viewform?usp=sharing) ✅ |
| Retenção Dia +7 (Forms) | [Retenção de conteúdo](https://docs.google.com/forms/d/e/1FAIpQLSdimYtWhPwys-iYmTVu39yTXjlomWT_0wiKFxOCUhSRv-wsiA/viewform?usp=sharing) ✅ |

---

## 26. Repositórios e Recursos

| Recurso                                        | Link                                                                                                                                                                                                                                                                         |
| ---------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Código-fonte                                   | [https://github.com/JoaoLops3/ODS-Vision-SafetyLens](https://github.com/JoaoLops3/ODS-Vision-SafetyLens)                                                                                                                                                                     |
| Trello                                         | [https://trello.com/invite/b/6a9c364bf3a2c97532634894/ATTId1a224107fc31f8c12e0758e34583e5a251C5E4B/ods-vision-safetylens](https://trello.com/invite/b/6a9c364bf3a2c97532634894/ATTId1a224107fc31f8c12e0758e34583e5a251C5E4B/ods-vision-safetylens)                           |
| Artigo científico do projeto                    | [SharePoint (Word — versão atualizada)](https://facens-my.sharepoint.com/:w:/g/personal/223969_facens_br/IQC635MrvwTJQr-ThWC7yNzgAfASb7rOqj2sZgzheg6sdYE?e=UEWZgv)                                                                                                           |
| Planilha de artigos científicos (base teórica) | [SharePoint](https://facens-my.sharepoint.com/:x:/g/personal/223969_facens_br/IQD6DAxNHPhGSaIQRpkXLhdFAZtFv9VKDNwsFKHG7cuFfUY?e=43COmv) · [`docs/documents/Tabela_Artigos_Cientificos_SafetyLens_UPX8.xlsx`](docs/documents/Tabela_Artigos_Cientificos_SafetyLens_UPX8.xlsx) |
| Vídeo de demonstração (versão 1)               | [YouTube Shorts](https://youtube.com/shorts/QbRCsMurnhY) · [`docs/images/v1/evidencias/Video_V1.mov`](docs/images/v1/evidencias/Video_V1.mov)                                                                                                                                |
| Modelo de README da disciplina                 | [https://github.com/dr-ohata/upx-modelo](https://github.com/dr-ohata/upx-modelo)                                                                                                                                                                                             |
| Documentação complementar                      | [`docs/`](docs/)                                                                                                                                                                                                                                                             |

---

## 27. Cronograma Técnico

| Atividade                          | Responsável       | Status |
| ---------------------------------- | ----------------- | ------ |
| Artigo / requisitos RF–RNF         | Equipe ODS Vision | ✅     |
| Configuração Unity + XR            | Equipe            | ✅     |
| Image Tracking + overlays          | Equipe            | ✅     |
| Âncoras UV A/B/C + EPI no marcador | Equipe            | ✅     |
| RF03 toque + confirmação           | Equipe            | ✅     |
| Quiz + export JSON/CSV             | Equipe            | ✅     |
| Build / testes iPhone 12           | Equipe            | ✅     |
| Documentação GitHub (este README)  | Equipe            | ✅     |
| Screenshots e vídeo (v1)           | Equipe            | ✅     |
| RF04 tempos separados + RNF02 FPS  | Equipe            | ✅     |
| Zonas vermelhas + marcador QR      | Equipe            | ✅     |
| EditMode 34/34                     | Equipe            | ✅     |
| Testes Android ARCore              | Equipe            | ⬜     |
| Alvo impresso 20 cm                | Equipe            | ⬜     |
| Experimento SUS / retenção 7 dias  | Equipe (Forms)    | ✅ protocolo externo |

---

## 28. Controle de Versão

O repositório usa commits frequentes com Conventional Commits. Exemplos reais:

```text
feat: SafetyLens AR para identificação de riscos industriais
fix: alinhar overlays à pose do ARTrackedImage
feat: ancorar HOT/PINCH/EPI nas letras A/B/C do marcador
fix: exigir toque AR antes de confirmar item (RF03)
docs: documentação técnica no modelo UPX
```

Evita-se um único commit com todo o projeto. Branches/tags conforme necessidade da equipe.

---

## 29. Problemas Encontrados e Soluções

### Problema 1 — Tela preta / capacete “fantasma” na cena

**Problema:** feed da câmera sumia; aparecia capacete branco sem contexto.  
**Causa:** Camera/Light importados do FBX do Blender na hierarquia + clear flags inadequados.  
**Solução:** strip de Camera/Light no import; cena só com Main Camera AR; `ARCameraBackground` BeforeOpaques.

### Problema 2 — Module Verifier no Xcode (`double-quoted include`)

**Problema:** build iOS falhava no UnityFramework.  
**Causa:** `ENABLE_MODULE_VERIFIER=YES` no Xcode moderno.  
**Solução:** pós-process Unity + `Tools/fix-xcode-module-verifier.sh` (não salvar Unlock no `pbxproj`).

### Problema 3 — Overlays “soltos” / espelhados

**Problema:** placas não sentavam nas letras A/B/C; texto espelhado.  
**Causa:** conteúdo tratado como HUD da câmera.  
**Solução:** um trackable + UV locais; placas billboard; EPI no espaço do marcador; billboard de texto sem flip duplo.

### Problema 4 — Confirmar item sem tocar no AR

**Problema:** botão concluía a inspeção sem hit no overlay (invalidava RF03).  
**Causa:** `ConfirmCurrentHazard` chamado direto pela UI.  
**Solução:** seleção AR obrigatória antes de habilitar o botão.

---

## 30. Melhorias Futuras

- Calibração fina UV/escala nas letras A/B/C e no layout do QR.
- (Feito) SUS + retenção 7 dias via Google Forms — não no app.
- QA no alvo impresso de 20 cm e Android ponta a ponta (dependem da equipe).

Ver também `[plans/melhorias-ra-pos-auditoria.md](plans/melhorias-ra-pos-auditoria.md)`.

---

## 31. Custos e Recursos

| Recurso                        | Tipo     | Custo/Condição                     |
| ------------------------------ | -------- | ---------------------------------- |
| Unity Personal / Educacional   | Software | Conforme licença UniFacens / Unity |
| AR Foundation / ARKit / ARCore | Software | Gratuitos via Package Manager      |
| MacBook M2                     | Hardware | Equipamento da equipe              |
| iPhone 12                      | Hardware | Equipamento da equipe              |
| GitHub / Trello                | Serviço  | Contas da equipe                   |

---

## 32. Licença

Projeto acadêmico desenvolvido para fins educacionais na disciplina **UPX — Realidade Aumentada** (UniFacens).  
Uso e redistribuição devem respeitar as políticas da instituição e créditos dos autores.

---

## 33. Autores

- **Brunno Eduardo Marfil Marins** (RA 236051) — Desenvolvedor AR
- **Bruno Romi Cervone** (RA 235212) — Engenharia de Software e testes de usabilidade
- **Giovane Zanzarini Iano** (RA 236082) — Front-end, UI/UX e Design 3D
- **Hector Silveira Colchiesqui** (RA 235354) — Arquitetura e modelagem 3D/assets
- **João Gabriel Lopes Aguiar** (RA 223969) — Gestão de projeto e documentação
- **Luis Alberto Sant'Anna de Araújo** (RA 236401) — Pesquisa científica e análise de dados
- **Miguel Muran Zvir Novaes** (RA 222674) — Redação e metodologia

**Instituição:** UniFacens  
**Disciplina:** UPX — Realidade Aumentada  
**Professor:** Prof. Daniel Domingos Akira de Sá Pimentel Ohata  
**Semestre:** 2026.2 (8º semestre)  
**Equipe:** ODS Vision — SafetyLens

---

## Checklist de Entrega

- [x] Código-fonte atualizado no GitHub
- [x] README no formato do modelo da disciplina
- [x] Versões das tecnologias registradas
- [x] Arquitetura apresentada (`docs/diagrams/arquitetura.svg`)
- [x] Funcionamento da RA explicado (Image Tracking WL03 + QR, UV A/B/C)
- [x] Estrutura de pastas documentada
- [x] Scripts principais descritos
- [x] Procedimento de instalação
- [x] Procedimento de execução (iOS)
- [x] Procedimento de utilização
- [x] Screenshots da v1 em `docs/images/v1/interface/` (fluxo 01–12)
- [ ] Screenshots da v2 em `docs/images/v2/interface/` (fluxo 01–14, ficha + RF12)
- [ ] Evidência em vídeo da v2 em `docs/images/v2/evidencias/`
- [x] Testes registrados (parcial; Android pendente)
- [x] Dispositivos testados informados (iPhone 12 + MacBook M2)
- [x] Limitações conhecidas documentadas
- [x] Evidência em vídeo da v1 ([YouTube](https://youtube.com/shorts/QbRCsMurnhY) + [`Video_V1.mov`](docs/images/v1/evidencias/Video_V1.mov))
- [x] Artigo do projeto linkado no SharePoint (versão sempre atualizada)
- [x] Commits representam a evolução do projeto
- [x] Funções técnicas de **todos** os integrantes preenchidas
- [x] Nome completo do professor confirmado
- [x] Versão do iOS de teste registrada (iPhone 12 — 18.7.8)

---

> Se outra equipe receber apenas este repositório, deve conseguir compreender a proposta, configurar o Unity 6000.3.0f1, gerar o build iOS e reproduzir o fluxo Image Tracking → inspeção → quiz → export — desde que tenha iPhone com ARKit e o marcador WL03 (~20 cm) ou o QR (~12 cm).
