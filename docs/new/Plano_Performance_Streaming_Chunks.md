# Plano de Performance — Geração de Terreno/Props por Chunk

**Status: PLANEJADO, NÃO IMPLEMENTADO.** Documento preparado ao fim de uma sessão de trabalho
(Sprint P3) pra não perder o raciocínio/debate — a implementação começa na próxima sessão.
Contexto: depois de criar os ~199 prefabs de prop (Sprint P3) e cortar a densidade deles em
87,5% (4 cortes de 50% sucessivos), o usuário reportou que o lag "continua absurdo" tanto ao
caminhar pra um chunk novo quanto ao clicar em "Regenerar Mundo" — e também ao simplesmente
circular por área já visitada, sem gerar nada novo. Esse último dado é a pista central: **o
problema principal nunca foi a quantidade de props**, é o custo de gerar/limpar o TERRENO em
si, e o padrão de destruir-e-recriar tudo sempre que um chunk sai/entra do raio de streaming.

## Diagnóstico (confirmado lendo o código, não é só suposição)

`ChunkTerrainGenerator.IsTierBorderCell` roda pra CADA uma das 4096 células de CADA chunk
(decide se aquela célula pinta patch/nested/variante ou só terreno-base puro), e chama
`ActiveTierGroupAt` **9 vezes por célula** (a própria + 8 vizinhos de Moore) — ou seja, até
~37 mil chamadas por chunk só nessa checagem. Cada chamada de `ActiveTierGroupAt` faz:
`Mathf.Sqrt`, busca em `DaySpawnTableSO.GetDay()` (scan linear de 30 entradas),
`WorldTierSelector.ComputeTierGroupPercentsNonAlloc` (loop de 6 grupos) e
`WorldTierSelector.ChooseActiveTierGroup` (Perlin + soma cumulativa). Isso roda **independente
de prop nenhum existir** — por isso cortar 87,5% da densidade de prop quase não mexeu no lag:
o gargalo real é o terreno, não os props.

Além disso, `SpawnProps` gera a grade de candidatos (`PropPlacement.GenerateCandidatePositions`,
até ~1024 iterações pra props densos) pra **todos os props de todos os 6 tiers em todo chunk**,
mesmo quando aquele tier nem existe ali — só descobre isso depois, dentro de `CanPlacePropAt`.

E o padrão de streaming hoje é destrutivo: todo chunk que sai do raio (`ChunkStreamer`) é
imediatamente limpo (`ClearChunkIncremental`/`ClearChunkImmediate`, que também `Destroy()` os
props). Se o jogador volta pra uma área já visitada, paga o custo de gerar tudo de novo do
zero — inclusive se ele só estiver "dando voltas" dentro da mesma sessão/dia, sem o bioma ali
ter mudado nada.

## Decisão de escopo (conversa com o usuário, importante registrar o PORQUÊ)

Existem 3 ideias (do usuário) + 3 ideias (minhas) discutidas em debate antes de codar. Duas
delas (cache de chunk, e "ocultar em vez de destruir") dependem de um conceito de "o dia
mudou" como fronteira de reset — mas **hoje não existe Loja/Menu real**: `ShopHandler` é um
stub só com botões de `ContextMenu` pra teste, sem UI nem fluxo de jogo real por trás. O
usuário decidiu explicitamente: implementar e testar agora só o que já dá pra validar com o
que existe (Play Mode + botões de `ContextMenu` já existentes, como "Regenerar Mundo (Day
Override)"), e documentar o resto como pendência pra quando a Loja/Menu/UI completos
existirem (próxima-próxima Deadline, depois da Deadline de Bestiário + Loot restante). Isso
evita construir/testar no escuro algo que vai mudar de forma quando a UI real chegar.

**Decisão explícita também pelo usuário**: nenhuma thread real. "Segundo plano" aqui significa
só "o jogador não percebe", via fatiamento de trabalho em coroutines no main thread — nunca
`System.Threading`/`Task`. A cena da Loja, quando existir, **não é uma cena separada** — é só
uma UI por cima da MESMA cena de gameplay, que nunca pausa por baixo. Isso importa pro item 5
(futuro): "gerar em segundo plano durante a Loja" não vai precisar de um sistema dedicado,
é só a mesma fila de streaming paceada continuando a rodar atrás da UI — ganhando de graça o
tempo que o jogador ficar na tela da Loja antes de voltar a andar.

## AGORA — implementar na próxima sessão, testável sem UI nova

### 1. Buffer de tier ativo por chunk + margem de 1 célula (elimina os 9x)

Calcular `ActiveTierGroupAt` 1 vez por célula do chunk (mais 1 célula de halo em volta, pra
cobrir os vizinhos da borda), guardar num array `int[(size+2)*(size+2)]` reaproveitado entre
chunks (igual os outros buffers já existentes). `ComputeChunkBuffers` e `IsTierBorderCell`
passam a LER desse buffer em vez de chamar `ActiveTierGroupAt` de novo — elimina a
redundância dos 9x.

### 2. Cache de `effectiveDay`/`tierGroupPercentsBuffer` por chunk

`EffectiveSpawnDay` só depende da distância até a origem — dentro de um único chunk de 64
células, quase sempre é o MESMO valor inteiro em toda célula (só muda perto de uma fronteira
exata de `distancePerSpawnDay`, caso raro). Hoje `ActiveTierGroupAt` recalcula a tabela de 30
dias (`DaySpawnTableSO.GetDay`, scan linear) e os % dos 6 grupos
(`ComputeTierGroupPercentsNonAlloc`) EM TODA chamada — até ~37 mil vezes por chunk, quando na
prática o resultado não muda. Calcular isso 1x (ou poucas vezes, se o chunk cruzar uma
fronteira de distância) por chunk, guardado junto com o buffer do item 1, e deixar só
`WorldTierSelector.ChooseActiveTierGroup` (Perlin + soma cumulativa, bem mais barato) correndo
célula a célula de verdade. Esse é provavelmente o ganho MAIOR dos dois (mais que o item 1).

### 3. `SpawnProps` pula tier ausente no chunk antes do scan caro

Reaproveitando o buffer do item 1 (já sabe quais grupos aparecem no chunk), checar ANTES de
chamar `PropPlacement.GenerateCandidatePositions` pra cada prop de um tier — se aquele tier
não aparece em NENHUMA célula do chunk, pula a lista de props dele inteira, sem gerar grade de
candidato nenhuma.

### 4. Pacing por orçamento de milissegundos (não células) no streaming normal

Hoje só o `SetTilesBlock` final é espalhado entre frames (1 por frame) — o cálculo pesado
(`ComputeChunkBuffers` inteiro + `SpawnProps` inteiro) roda tudo síncrono, de uma vez, sem
yield nenhum, mesmo no caminho "incremental" do streaming. Trocar por uma coroutine que
processa um número de células por iteração limitado por um orçamento de tempo real (ex.:
`System.Diagnostics.Stopwatch`, pausa a cada ~1-2ms de trabalho e devolve o frame) — isso se
adapta ao hardware e à pressão de frame do momento, em vez de um número fixo de células
chutado. Importante: esse pacing é só pro caminho de streaming NORMAL (chunk entrando no raio
enquanto o jogador anda). Início de jogo (`NewGame`/`ContinueGame`) e o botão "Regenerar
Mundo" continuam rápidos/síncronos como hoje — são eventos pontuais onde uma espera curta é
aceitável, e o usuário explicitamente não quer que ESSES fiquem lentos.

### 5. "Ocultar, não destruir" — streaming normal não limpa mais ao sair do raio

Quando um chunk sai do raio de streaming (`ChunkStreamer`), parar de chamar
`ClearChunkIncremental`/destruir os props. Em vez disso: deixar o Tilemap como está (os tiles
pintados não custam nada relevante parados fora de tela — Unity já faz culling) e só
`SetActive(false)` nos props instanciados daquele chunk (guardados em `spawnedProps`, já
existe o dicionário). Ao reentrar no raio, se o chunk já está em `paintedChunks`, não
repintar nada — só reativar (`SetActive(true)`) os props escondidos. **Sem limite de
tamanho/LRU** — o usuário decidiu reset completo só na fronteira de "dia" (ver seção Futuro),
não um cache com eviction. O botão "Regenerar Mundo" (já existe, já funciona) continua
fazendo o reset de verdade — serve de "fronteira de dia" pra teste hoje, sem precisar de Loja:
precisa só garantir que ele também destrói/limpa os chunks que estavam OCULTOS (não só os
`paintedChunks` ativos), já que o código dele já itera por `paintedChunks` inteiro (que vai
continuar contendo os ocultos, já que eles nunca saem desse set agora).

### 6. Pré-busca preditiva (direção de movimento do jogador)

`ChunkStreamer` (ou `ChunkTerrainGenerator.Update`) olha a direção que o alvo de streaming
está se movendo (delta de posição entre frames, ou a velocidade do Rigidbody2D) e, se for
consistente por um tempinho, antecipa a entrada na fila de pintura dos chunks NAQUELA direção
antes do raio de streaming "oficialmente" pedir — dá a folga que o item 4 (pacing mais lento)
precisa pra nunca aparecer incompleto bem na frente do jogador.

## FUTURO — documentar como pendência, não implementar agora

- **Gancho de troca de dia real** ligado à Loja/UI de verdade, pra invalidar os chunks
  ocultos/pintados e disparar a repintura paceada no momento em que o dia realmente avança no
  fluxo de jogo (hoje `ShopHandler.StartNextDay()` é só stub de teste, sem UI real por trás).
  Reaproveitaria a MESMA fila paceada do item 4/6 — ganha de graça o tempo que o jogador
  passar na tela da Loja (que nunca pausa a cena por baixo), sem precisar de nenhum sistema
  novo de "pré-geração em segundo plano" dedicado. Só não dá pra testar/ajustar de verdade sem
  a UI real (quanto tempo o jogador realmente passa na Loja, etc.) — por isso fica pra quando
  a cena de Menu/UI completos existir (próxima-próxima Deadline).
- **Cancelar e reiniciar a geração se o jogador comprar remoção de tier no meio dela** — pedido
  original do usuário, mas depende 100% de uma mecânica de compra que ainda não existe em
  código nenhum.
- **Ajuste fino de UX** (orçamento de ms ideal pro pacing, indicador de carregamento se a
  repintura não terminar a tempo do jogador sair da Loja) — só faz sentido calibrar com a Loja
  de verdade na frente pra observar tempo real de permanência.

## Riscos/observações pra lembrar na implementação

- O buffer do item 1/2 é só pra `ComputeChunkBuffers`/`IsTierBorderCell` (vizinhança de 1
  célula) — `CanPlacePropAt`/footprint de prop grande (ex.: Volcano, 24×13 células) continua
  chamando `ActiveTierGroupAt` direto pra posições fora do halo pequeno, não dá pra reaproveitar
  o mesmo buffer ali sem alargá-lo bastante (não parece valer a pena, já que prop grande é raro).
- "Ocultar, não destruir" sem eviction cresce sem limite dentro de 1 sessão/dia (mais props
  escondidos vivos na memória conforme o jogador explora mais) — aceitável pro escopo de 1 dia
  de sessão, mas vale reavaliar se sessões ficarem muito longas antes de avançar o dia.
- Testar o item 5 com o botão "Regenerar Mundo" precisa confirmar que ele limpa TUDO que está
  em `paintedChunks` mesmo os ocultos (provável que já funcione, já que ele não depende do
  raio ativo do `ChunkStreamer` pra decidir o que limpar) — mas validar na prática antes de
  assumir.
