# SafetyLens v2 — briefing para melhorar o visual do QR

Texto de contexto para três leituras (João, Claude, Cursor). Ainda não é decisão de implementação.

## O que a v2 é

A v2 tem **dois caminhos** depois que a câmera reconhece uma imagem. A sessão trava no primeiro alvo. Os dois não se misturam.

| | Marcador WL03 (folha impressa, 20 cm) | QR (tela do computador, 12 cm) |
|---|---|---|
| O que a câmera vê | Placa ilustrada do equipamento | Um QR tratado como imagem. O app **não lê** o código. |
| Onde ficam os riscos | Colados na placa, nas letras A / B / C | Fora do código, numa fileira na frente da câmera |
| Câmera na inspeção | Ligada. A sala continua visível. | **Desligada.** Fundo azul-marinho sólido. |
| Depois do quiz | Card sobre a câmera, números mudando ao vivo | Tabela em tela cheia, sem câmera |

O caminho da folha é o que o artigo chama de RA: zona vermelha no plano da máquina, placa ISO e capacete no lugar certo, toque no que está na câmera.

O caminho do QR foi desenhado de propósito para **não** colocar nada em cima do código. Código pequeno na tela + modelo em cima = tracking cai e o QR some. A solução atual usa o QR só como chave. Reconheceu, mostra a ficha, e a inspeção vira um visor 3D.

## O que a pessoa vê no QR, na ordem

1. **ID e Abrir câmera.** Feed real.
2. **Enquadrar o QR.** Toast “QR reconhecido”. Os modelos **não** aparecem.
3. **Ficha** em card na parte de baixo: nome do compressor, função, três riscos em texto. O botão “Iniciar inspeção” só liga com o QR estável.
4. **Iniciar inspeção.** O feed some. Fundo `#0F1B2D`. Três objetos nascem já em fila, a 62 cm da câmera, um pouco maiores que no marcador.
5. **Arrastar** gira o grupo inteiro (não cada peça). **Toque curto** no item da vez seleciona. Toque no item errado conta erro. **Confirmar item** avança. A ordem é fixa: zona quente → pinçamento → capacete.
6. **Quiz** em tela cheia, sem 3D.
7. **Leitura do equipamento** em tabela Ideal / Cuidado / Perigoso. Os números não se movem. Não há máquina, não há câmera.

A frase da própria interface admite o limite: “Arraste para girar. Toque no item destacado para selecionar.”

## O que são esses “3 models”

Não são três máquinas.

- **Zona quente** e **pinçamento** são placas amarelas ISO, desenhadas em código (quadrado + pictograma). Não há malha 3D.
- **Capacete** é o único modelo de verdade (`HardHat.fbx`), amarelo, pequeno.
- Debaixo de cada placa há um disco vermelho translúcido. O item da vez cresce um pouco (escala 1,08), ganha anel e o rótulo. Os outros ficam acinzentados (escala 0,88). O concluído ganha anel verde.
- O pulso do anel desliga se o iPhone estiver com “Reduzir movimento”.

Por isso a cena parece um menu 3D: dois cartazes e um capacete flutuando no escuro, que dá para girar juntos.

## Por que ficou assim

Não foi preguiça de arte. Foi uma troca registrada na 1.4.2:

- No QR da tela, modelo em cima do código cobria o alvo e o tracking oscilava.
- A ficha ficou só texto, sem modelos.
- A inspeção tirou o conteúdo do `ARTrackedImage` e parentou num pivô da câmera (`SafetyLensQrViewerLayout`).
- `SetCameraFeedVisible(false)` troca o fundo da câmera AR por cor sólida.
- A telemetria do QR virou tabela porque, sem câmera, “número ao vivo sobre o feed” não tinha onde sentar.

O marcador impresso não passou por essa troca. Lá a câmera fica, o disco fica deitado na arte, a placa olha para a pessoa, o capacete assenta na letra C.

## O que não pode quebrar numa melhoria visual

Qualquer ideia bonita que mexa nisto invalida o experimento ou o outro caminho:

- O **WL03 continua como está.** A melhoria é do caminho QR.
- Ordem **HOT → PINCH → EPI.** Toque no certo, depois botão Confirmar. Toque no errado soma `errorCount`. Isso é o RF03.
- Tempos: a ficha entra em `inspectionSeconds`. Quiz é outro relógio. A telemetria não altera esses dois.
- Sem rede. Os números da leitura já estão no JSON (42 °C → 58 °C → 73 °C).
- Sem detecção de plano. O artigo é image tracking.
- iPhone 12, meta de 30 FPS na inspeção. Sem partícula decorativa, sem malha pesada.
- “Reduzir movimento” desliga pulso. A surpresa não pode depender só de animação contínua.
- O QR continua imagem de 12 cm. Não vamos decodificar o código.
- A interface em volta pode continuar no card de baixo. A surpresa tem que estar na cena, não num HUD novo.

## O problema, numa frase

No QR, a realidade aumentada acaba no instante em que a inspeção começa. O que resta é um visor escuro com três ícones.

A surpresa que falta não é “modelo maior”. É consequência no espaço: a sala continua, a máquina aparece nela, e o dedo muda a máquina.

---

## Ideia deste lado (Cursor)

Não recolocar os três ícones em cima do QR. Isso já foi tentado e o tracking da tela não aguenta.

**O QR continua sendo a chave. A inspeção volta a ser a sala.**

No “Iniciar inspeção” a câmera **não desliga**. No quadro daquele instante, a uns 60 cm, monta **um compressor só** — bloco simples de aço, frio, sem neon. Os três riscos são lugares nesse bloco, não três cartazes lado a lado.

- Zona quente: um trecho da carcaça acende (o amarelo de atenção que o app já usa), não uma placa solta.
- Pinçamento: duas partes do mesmo bloco se aproximam quando aquele passo está ativo.
- Capacete: o único FBX que já existe desce até a posição de uso no volume, em vez de flutuar na fila.

O gesto que já existe muda de sentido:

- Arrastar ainda gira, mas gira **a máquina**, com a mesa e a sala atrás.
- Toque no ponto certo: aquele trecho responde (acende, fecha ou assenta) e o anel trava. Aí o botão Confirmar sela — o trecho apaga para “feito” e o próximo lugar acende sozinho.
- Toque no lugar errado: aquele trecho recua um instante e o erro conta. O toast sozinho não basta.

Depois do quiz, a mesma máquina volta no lugar da tabela. O corpo muda Ideal → Cuidado → Perigoso com as três amostras que o JSON já tem. Os números ficam no card, porque o CSV precisa deles. A pessoa vê a máquina esquentar, não uma planilha.

Uma assinatura só: **aço frio, e um único trecho aceso por vez.** O resto da tela continua o card que já existe. Sem fundo preto com um accent ácido, sem partículas, sem três ícones maiores no vazio.

O que isso não resolve: alguém que queira o risco **impresso em cima do QR de 12 cm**. Isso continua sendo o trabalho do marcador de 20 cm.

## O que pedir ao Claude

Responder em cima deste texto, não de um app genérico de RA. Três coisas:

1. Onde esta ideia falha na prática do iPhone 12 (tracking, dedo, FPS, o card cobrindo a máquina).
2. Uma alternativa que também deixe a sala visível, se discordar do “um compressor só”.
3. O gesto único que a pessoa vai lembrar — um, não uma lista de efeitos.

Não propor: decodificar QR, detecção de plano, nuvem, trocar a UI inteira, nem redesenhar o WL03.
