# Plano — máquina do QR (primeira fatia)

Esta fatia está no app. EPI, zona quente e pinçamento ficam para a seguinte. A pergunta continua: depois de ler o QR, a máquina convence?

## O que vamos ver

1. A câmera liga só para escanear o QR. Um reconhecimento estável basta.
2. A câmera desliga. O fundo fica escuro.
3. Aparece um compressor nesse fundo, no centro da tela e com margem nas laterais. Não precisa apontar de novo para o código.
4. Os pontos ficam na superfície da máquina. Nenhum cartão aparece sozinho.
5. Dá para girar na horizontal.
6. O toque só vale no ponto que está na face virada para a pessoa. Ponto de lado ou de trás não abre nada.
7. No toque válido, abre a tabela daquele ponto.

Exemplo de tabela, com texto provisório, só depois do clique:

| | |
|---|---|
| O que é | Saída de ar quente |
| O que pode acontecer | Queimar a mão |

Os textos desta fatia são de mentira. Servem para ver o ponto na máquina e a tabela que ele abre. O conteúdo real (queimadura, pinçamento, EPI) entra depois, nas mesmas tabelas.

## Como o clique funciona

A frente da máquina é o lado virado para a pessoa.

- Ponto nessa face: o toque abre a tabela.
- Ponto na face oposta, ou na lateral que não está de frente: o toque não abre. A pessoa gira a máquina até essa face virar para ela, e aí o mesmo ponto passa a aceitar o clique.
- Se a frente tiver mais de um ponto, os que estão nessa face aceitam o toque. Os das outras faces continuam fechados até o giro.
- Girar a face para longe fecha a tabela. O ponto continua na máquina.

O ponto é uma marca na superfície. A tabela é um cartão na tela, com as duas linhas. Ela não flutua para fora da máquina o tempo todo.

## O que entra nesta fatia

- Câmera só no scan. Depois do primeiro reconhecimento estável, o feed desliga e não volta.
- Fundo escuro e o compressor no centro da tela, menor que a largura do celular. O modelo é o FBX realista em `Assets/Models/Motor`.
- Giro só na horizontal, com o dedo.
- Três marcas de exemplo em faces diferentes do volume, para o giro ser obrigatório.
- Toque na marca da face da frente abre a tabela de duas linhas.

## O que fica de fora

- Câmera ligada durante a inspeção da máquina.
- Máquina parada na sala, em cima da mesa ou na frente do monitor.
- Ordem dos riscos, toque que conta erro, botão Confirmar.
- Quiz e leitura de temperatura.
- Capacete, calor no dedo, vibração.
- Qualquer mudança no marcador impresso.

## Como a gente julga no iPhone

- Um scan basta. A câmera apaga e o compressor fica no fundo escuro.
- As marcas ficam grudadas na superfície quando a máquina gira.
- Com a frente para a pessoa, só a marca dessa face abre a tabela.
- Depois de girar, a marca que estava atrás passa a abrir, e a anterior deixa de abrir.
- Dá para ler a tabela sem ela tampar a máquina inteira.

## O que ainda dá para mudar

1. **Tabela.** Duas linhas, como acima. Mais campos deixam o cartão alto e cobrem a máquina.
2. **Quando a máquina nasce.** No reconhecimento estável, a câmera apaga e o compressor entra. Se preferir um botão entre o scan e a máquina, a câmera continua só nessa espera e desliga no toque.
