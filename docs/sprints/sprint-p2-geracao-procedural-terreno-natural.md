# Sprint P2 — Geração Procedural: Terreno Natural

## Objetivo

Pintar o primeiro terreno real e jogável do mundo único — camadas de terreno aninhadas + props de decoração — em cima da Fundação de chunks da Sprint P1, sem costura visível entre chunks.

## Como a sprint evoluiu (histórico — 3 modelos testados até o definitivo)

1. **Premissa original errada:** a ideia de "reaproveitar a arte de grama/água já pronta de Floor 1A/2A" estava errada — Floor 1A/2A é um tileset de masmorra Dwarven. O usuário importou a arte real durante a própria sprint (`Assets/Sprites/Terrian/Tier1-3_Nature/Grass_Dirt/` — pacote "Minifantasy ForgottenPlains").
2. **Modelo 1 (abandonado) — 1 RuleTile por PAR de terrenos** (`Water-Grass`, `Water-Dirt`...), usando `Not This` pra "deve ser o outro terreno". Quebrava sempre que 2 terrenos sem relação direta se tocavam (`DryDirt` encostando em `DarkSoil` sem grama no meio) — `Not This` aceita QUALQUER vizinho diferente, não só o esperado.
3. **Modelo 2 (abandonado) — `TerrainRuleTile` com "Specific Tile"** (`m_SpecificTiles`), mais uma extensão pra transição rasa vs. funda (`DeepCategory`/`m_DeepTiles`) e uma restrição de geração ("faixa intermediária obrigatória" — terreno sem variante Deep nunca nasce encostado em água/lava). Funcionava, mas o usuário testou visualmente e não gostou do resultado.
4. **Modelo 3 (abandonado) — bordas genéricas** (`biome_transition`, 1 `RuleTile` comum por terreno, Piso+Borda no mesmo footprint). Resolvia o bug do Modelo 1 de outro jeito, mas também não agradou visualmente depois de testado.
5. **Modelo final — grids aninhados por profundidade.** Decisão do usuário após testar os anteriores.

## Decisão final de arquitetura — grids aninhados por profundidade

Cada profundidade de aninhamento é 1 Tilemap SEPARADO: Grama no grid 0 (base, sempre presente), um patch que nasce direto sobre ela (ex.: DryDirt, Água, Pedra) no grid 1, um patch que nasce DENTRO desse patch (ex.: DarkSoil só nasce onde já é DryDirt) no grid 2, e assim por diante conforme a árvore de aninhamento precisar.

**Por que isso resolve o bug do Modelo 1 sem precisar de Specific Tile:** como DarkSoil nunca é pintado no MESMO Tilemap que DryDirt (está num grid acima), o `RuleTile` do DryDirt nunca "vê" DarkSoil como vizinho — só vê "mais DryDirt" ou "vazio" no próprio grid dele. O bug (`Not This` confundindo terrenos sem relação) deixa de poder acontecer **por construção**, não por uma regra extra de matching. Por isso o projeto volta a usar `RuleTile` comum da Unity, sem nenhuma classe customizada — `TerrainRuleTile`, a ferramenta geradora (`TerrainRuleTileGeneratorWindow`) e a extensão Deep/"faixa intermediária obrigatória" foram todas removidas (decisão explícita do usuário, confirmada por pergunta direta): o aninhamento já resolve o caso de água precisar de um terreno-tampão ao redor — "água precisa de areia ao redor" vira simplesmente "água é um patch aninhado dentro da areia", garantido pela própria árvore, sem precisar de flags (`isDeepTerrain`/`supportsDeepTransition`) nem checagem de vizinhança.

**Variantes de grama (HighGrass/FreshGrass/...)** também usam `RuleTile` comum agora, mas NÃO entram na árvore de aninhamento (`patchTerrains`) — são um sistema separado, `BiomeProfileSO.baseVariants`, resolvido por `TerrainSelector.ChooseBaseVariant` e pintado numa Tilemap própria (`ChunkTerrainGenerator.baseVariantTilemap`). Motivo: decisão do usuário (resposta direta à pergunta sobre onde elas podem aparecer) foi **"só grama pura"** — uma variante de grama nunca nasce onde já existe um patch de Dirt/Pedra/Água por cima; ela só decora a célula onde a pilha de terreno tem tamanho 1 (só o `baseTerrain`, nada aninhado). Colocar essas variantes dentro de `patchTerrains` (erro do próprio desenvolvimento desta sprint, encontrado na auditoria do bioma do usuário) faria elas competirem pela mesma célula que Dirt/Água/Pedra, o que não é o comportamento desejado.

**Margem de 1 célula obrigatória entre um filho aninhado e a borda do próprio pai** (confirmado pelo usuário, com exemplo prático: pedra dentro de um caminho de terra nunca pode encostar na grama por baixo da terra). `TerrainSelector.ChooseTerrainStack` só desce pra um patch aninhado se a célula for "interior" ao pai que acabou de bater — isto é, se os 8 vizinhos dessa célula TAMBÉM pertencerem à máscara do pai (`IsInteriorToMask`, público e testável isoladamente). Garante pelo menos 1 anel de material do pai entre qualquer filho e o que vem antes dele na árvore, sem precisar de números fixos (tipo "9×9 vira 8×8") — a regra funciona igual pra manchas orgânicas de ruído de qualquer tamanho/forma.

**Fora do escopo desta sprint, registrado pra não se perder (ver GDD/docs futuros):** o usuário quer, no futuro, (a) usar o mesmo princípio de `RuleTile` nos terrenos-base de biomas diferentes (grama → grama escura → cinzas), com bordas na fronteira entre biomas, e (b) fazer a evolução por dia ser uma transformação PARCIAL/gradual (metade do cenário reconhecível, metade novo — "lembro dessa poça, mas não lembro desse caminho"), não uma troca completa de bioma. Nenhuma das duas coisas foi implementada aqui — a árvore de aninhamento atual já é compatível com ambas as ideias, mas nenhuma regra de conexão entre biomas foi fixada de propósito, porque o usuário ainda não decidiu quantos biomas existirão por dia.

## Variante de grama só nasce formando bloco (pedido do usuário, fim da sprint)

Diferente do resto do terreno (`patchTerrains`, que aceita qualquer disposição de ruído), `baseVariants` tem uma regra extra: a arte das variantes de grama não tem sprite pra "ilha de 1 célula" — só pra formas que fecham bloco com o vizinho (confirmado pelo usuário com exemplos de matriz: `1 0 0 / 0 0 0` isolado = inválido; `1 1 0 / 1 1 1 1` conectado = válido, mesmo que irregular).

**1ª tentativa (errada):** checar se o MESMO `BiomeTerrainEntry` também bate num dos 8 vizinhos, pelo próprio ruído dele. Falhava de 2 jeitos: (1) o vizinho podia bater pelo ruído daquele entry, mas na hora de pintar de verdade acabava sendo roubado por Dirt/Pedra/Água (célula impura) ou vencido por OUTRA entrada de `baseVariants` que vem antes na lista; (2) encosto só na diagonal (canto) contava como conectado.

**2ª tentativa (também errada, corrigida com uma 2ª rodada de fotos do usuário mostrando que o bug persistia idêntico):** trocou pra "qualquer entrada de `baseVariants` bate no vizinho reto, não precisa ser a MESMA" — partindo do pressuposto de que o RuleTile não distingue qual variante está do lado. Esse pressuposto estava ERRADO: conferido direto no código-fonte do pacote (`RuleTile.cs`, método `RuleMatch`), o valor `This` compara `other == this` — **identidade do asset**, não "tem algum tile ali". HighGrass do lado de ThickGrass é `NotThis` pro RuleTile do HighGrass, mesmo os dois estando pintados na mesma Tilemap — então "qualquer variante" como critério de conectividade não refletia o que a Tilemap realmente renderiza.

**Versão final (correta):** `TerrainSelector.HasMatchingOrthogonalNeighbor(entry, ...)` (público/testável) checa só os 4 vizinhos RETOS e, pra cada um, confirma 2 coisas: a célula é pura (`FindMatch(patchTerrains,...) == null`) E a MESMA `entry` é quem vence ali (`FindMatch(baseVariants,...) == entry` — não basta bater pelo ruído isolado dela, precisa ser a que realmente ganha a prioridade da lista). `ChooseBaseVariant` descarta o resultado se isso for falso pra todos os 4 vizinhos. Célula isolada (sozinha, só na diagonal, ou encostando numa variante diferente) vira grama lisa, sem variante.

**Observação registrada, não endereçada hoje:** o usuário notou que Dirt/Pedra podem "roubar" uma célula de um bloco de variante que, de outra forma, teria fechado um quadrado perfeito (ex.: 4 células formando quadrado, mas 1 delas virou Dirt, sobrando um "L" de 3). Isso não viola a regra em si (formas irregulares conectadas são válidas, confirmado pelo próprio usuário com o exemplo do "staircase"), mas é um ponto a pensar melhor no futuro se o visual incomodar.

## Safe zone (pedido do usuário, fim da sprint)

O herói nasce sempre em `Vector3.zero` (`MainMenuUI.SpawnHero`) e futuramente terá NPCs de posição fixa perto dali — sem isso, água podia nascer embaixo do herói logo de cara (sem colisor ainda, perderia a run inteira) ou embaixo de um NPC fixo. `WorldGenerationSettings.safeZoneRadius` (raio em células a partir da origem, `0` desativa) + `BiomeTerrainEntry.blockedInSafeZone` (flag por entrada, não por nome/string — preparado pra Lava e outros hazards futuros de outros biomas) resolvem isso: `TerrainSelector.FindMatch` pula qualquer entrada marcada `blockedInSafeZone` enquanto a célula estiver dentro do raio, em qualquer profundidade (top-level ou aninhado — ex.: `Water-Dirt` aninhado dentro de `Dirt-Grass` também precisa da flag marcada). Variantes de grama (`baseVariants`) nunca são filtradas — não são perigosas. Props também são bloqueados dentro do raio, direto em `ChunkTerrainGenerator.SpawnProps` (rejeição simples por distância da origem, sem flag — todo prop é bloqueado ali, não só os perigosos).

**Pendência pro usuário:** marcar `blockedInSafeZone = true` nas entradas de `Water-Grass` e `Water-Dirt` do `BiomeProfile_Tier1.asset` (hoje desmarcado, `false` por padrão) — sem isso o raio existe mas não bloqueia nada ainda.

## Sistemas adicionados (estado final)

- **`TerrainDefinitionSO`** — de volta ao simples: `{id, tile}`. 1 `Tile`/`AnimatedTile`/`RuleTile` já configurado resolve tudo sozinho (interior + borda), porque nunca compartilha Tilemap com um terreno de profundidade diferente.
- **`BiomeTerrainEntry`** — 1 terreno de mancha + sua máscara de ruído (`coverageTarget`/`noiseScale`) + `nestedPatches` (lista recursiva de patches que podem nascer dentro dele).
- **`BiomeProfileSO`** — `baseTerrain` (grid 0) + `patchTerrains` (grid 1, cada entrada pode aninhar mais fundo) + `baseVariants` (variações do terreno-base, só em célula pura — sistema separado, ver seção de decisão acima) + `props`.
- **`EnvironmentPropSO`** — prefab de decoração com `minSpacing`/`density`/`jitter`.
- **`TerrainMask`** (puro, testável) — decide se uma célula global é reivindicada por um terreno, via `Mathf.PerlinNoise` em coordenadas globais.
- **`TerrainSelector`** (puro, testável) — `ChooseTerrainStack` percorre a árvore de aninhamento e devolve a PILHA de terrenos de 1 célula (1 por profundidade efetivamente ocupada; para no primeiro nível onde nada bate, ou onde a célula não é "interior" ao pai — ver `IsInteriorToMask`, a margem de 1 célula obrigatória). `ChooseBaseVariant` resolve, separadamente, qual variante de `baseVariants` bate numa célula (quem chama decide quando usar — só em célula pura). `ChooseTerrainStackNonAlloc` é a mesma lógica sem alocar `List` nenhuma — escreve num buffer reaproveitado pelo chamador; existe só por performance (ver Decisões técnicas).
- **`PropPlacement`** (puro, testável) — posições candidatas de prop por grade + jitter determinístico + rejeição por espaçamento mínimo.
- **`ChunkTerrainGenerator`** (World/) — pinta/limpa uma LISTA de Tilemaps (`layerTilemaps`, 1 por profundidade) conforme o `ChunkStreamer` (P1) ativa/desativa chunks; para cada célula, profundidades além do que a pilha ocupou são explicitamente limpas (`SetTile(null)`). Tem também um `baseVariantTilemap` opcional, pintado só quando a pilha daquela célula tem tamanho 1.
- **`EnvironmentPropPlaceholder`** — gizmo simples pra visualizar onde um prop foi colocado antes da arte real existir.

## Decisões técnicas

- **1 único conjunto de Tilemaps pro mundo inteiro, não 1 por chunk.** Elimina costura entre chunks sem precisar de "halo" manual — a Unity reavalia vizinhos sozinha quando um `SetTile` entra perto de um tile existente no mesmo `Tilemap`.
- **`Mathf.PerlinNoise` em coordenadas globais, com offset derivado da seed via `WorldHash`** (nunca `UnityEngine.Random`) — determinismo e continuidade entre chunks ao mesmo tempo.
- **Lógica pura separada da orquestração de Scene** — `TerrainMask`/`TerrainSelector`/`PropPlacement` em `Core/WorldGeneration/` (testáveis via NUnit EditMode); `ChunkTerrainGenerator` (depende de `Tilemap`/`Instantiate`) em `World/Generation/`.
- **Profundidade de aninhamento é dado, não código** — `layerTilemaps` é uma lista no Inspector, dimensionada conforme a árvore do bioma precisar (2 níveis hoje, pode crescer sem mexer em `ChunkTerrainGenerator`).
- **Rejeição por espaçamento dentro da mesma passada de posicionamento de props** — evita que jitter de 2 pontos vizinhos da grade colapse 2 props quase no mesmo lugar. **Limitação conhecida:** só dentro do próprio chunk.
- **Nenhum asset novo (`.asset`/`.prefab`) foi criado por código** — só os `.cs`. Os assets de verdade são criados pelo usuário na Editor.
- **Pintura/limpeza de chunk em lote, sem alocação por célula** (encontrado em teste real do usuário — travada grande ao cruzar chunk + engasgos de GC periódicos ao andar). `ChunkTerrainGenerator.PaintChunk`/`ClearChunk` usam `Tilemap.SetTilesBlock` (1 chamada por camada, não 1 por célula) com buffers `TileBase[]` reaproveitados entre chunks, alocados 1x em `Awake`. `TerrainSelector.ChooseTerrainStackNonAlloc` substitui `ChooseTerrainStack` no laço de pintura por célula, escrevendo num `TerrainDefinitionSO[]` reaproveitado em vez de alocar uma `List` nova a cada uma das até 4096 células por chunk (`chunkSize=64`). `ChunkStreamer`/`ChunkTerrainGenerator.Update` também param de alocar uma `List<ChunkCoord>` nova por frame.
- **Pintura/limpeza de chunk vira uma pipeline incremental (coroutine), 1 `SetTilesBlock` por frame** (`ChunkTerrainGenerator.ProcessPipeline`/`PaintChunkIncremental`/`ClearChunkIncremental`) — diagnosticado com o Profiler real do usuário (frame de 135,99ms, travada "extrema urgência", `Tilemap.FindAllRefreshPositions()` custando ~5-6ms POR CHAMADA de `SetTilesBlock`, custo fixo que o batching da seção acima não reduz). Primeira tentativa foi limitar a 1 chunk inteiro por frame (ainda ~4 chamadas = ~20-24ms de pico); o usuário sugeriu ir além — como o chunk pintado está sempre longe o bastante do jogador pra nunca ser notado demorando, não tem motivo pra sequer terminar 1 chunk inteiro no mesmo frame. Agora `pendingPaint`/`pendingClear` são filas processadas 1 chunk de cada vez (nunca 2 chunks "em voo" ao mesmo tempo — os buffers reaproveitados não suportariam concorrência), e dentro de cada chunk, cada uma das até 4 chamadas de `SetTilesBlock` (3 camadas + variante) acontece em frames separados (`yield return null` entre cada uma). Pico por frame cai pra 1 `SetTilesBlock` (~5-6ms) em vez de até 4 juntos. Efeito colateral aceito: um chunk recém-revelado leva alguns frames a mais pra terminar de aparecer — imperceptível, já que ele só fica visível de perto bem depois.

## Arquivos/classes principais

- `Assets/Scripts/Core/WorldGeneration/TerrainDefinitionSO.cs`, `BiomeTerrainEntry.cs`, `BiomeProfileSO.cs`, `EnvironmentPropSO.cs` — catálogo de dados.
- `Assets/Scripts/Core/WorldGeneration/TerrainMask.cs`, `TerrainSelector.cs`, `PropPlacement.cs` — lógica pura.
- `Assets/Scripts/World/Generation/ChunkTerrainGenerator.cs` — pintura/limpeza real (N grids).
- `Assets/Scripts/World/Generation/EnvironmentPropPlaceholder.cs` — gizmo de debug.
- `Assets/Tests/EditMode/TerrainMaskTests.cs`, `TerrainSelectorTests.cs`, `PropPlacementTests.cs` — testes automatizados.
- **Removidos (modelos abandonados):** `AutotileRegionSO.cs`, `TerrainRuleTile.cs`, `TerrainRuleTileGeneratorWindow.cs`, `TerrainRuleTileTests.cs`. Os assets `.asset` que usavam `TerrainRuleTile` (pasta `SpecificTileVersions/`) ficam com script ausente até o usuário apagar a pasta — nenhum deles chegou a ser referenciado por `BiomeProfileSO`/cena, então não há dado real em risco.

## Eventos adicionados

Nenhum em `GameEvents` — mesma justificativa da P1.

## Testes executados

- **EditMode (automatizado):** `TerrainMaskTests`, `TerrainSelectorTests` (sem patch → só base; patch de profundidade 1 bate; patch aninhado também bate → pilha de 3; patch aninhado não bate → pilha para no pai; primeiro da lista vence em cada nível; entrada nula ignorada; determinismo; `IsInteriorToMask` isolado nos 2 extremos — cobertura total/zero; varredura de 625 células confirmando que um filho aninhado nunca aparece numa célula onde o próprio pai está na borda dele; `ChooseBaseVariant` — entrada bate/não bate/lista vazia/determinismo), `PropPlacementTests`.
- **Manual (pendente de validação do usuário):** criar `TerrainDefinitionSO` por terreno (1 `Tile`/`AnimatedTile`/`RuleTile` cada, já resolvendo a própria borda), `BiomeProfileSO_Natural` com a árvore de aninhamento real (Grama → DryDirt → DarkSoil, Grama → Água...) e as 7 variantes de grama dentro de `baseVariants` (não de `patchTerrains`), uma lista de `Tilemap`s no `ChunkTerrainGenerator` (1 por profundidade) mais o `baseVariantTilemap`, e confirmar visualmente que cada grid pinta/some corretamente, sem costura entre chunks, sem nenhum terreno "vendo" outro de profundidade diferente como vizinho, e sem variante de grama nascendo em cima de Dirt/Pedra/Água.

## Bugs conhecidos

Nenhum encontrado nos testes automatizados.

### 🔴 PENDÊNCIA CRÍTICA (não resolvida, retomar amanhã): variante de grama ainda aparece isolada

Depois de **2 tentativas de correção nesta mesma sprint** (ambas documentadas acima em "Variante de grama só nasce formando bloco"), o usuário confirmou com uma 3ª foto (fim do dia, 2026-10-11) que o problema persiste — um ponto único de variante de grama (`FreshGrass` ou similar) continua aparecendo isolado, sem formar bloco, mesmo depois de:
1. 1ª correção: exigir que o MESMO `BiomeTerrainEntry` bata em 1 dos 8 vizinhos pelo próprio ruído — não resolveu (vizinho podia ser roubado por Dirt/Pedra ou por outra entrada da lista).
2. 2ª correção: exigir que QUALQUER entrada de `baseVariants` vença em 1 dos 4 vizinhos retos — não resolveu, e o pressuposto por trás ("RuleTile não distingue qual variante está do lado") era **errado**, corrigido depois de conferir o código-fonte do pacote (`RuleTile.RuleMatch`: `This` = `other == this`, identidade do asset).
3. 3ª correção (atual, `HasMatchingOrthogonalNeighbor`): exige que seja a MESMA entrada vencendo em 1 dos 4 vizinhos retos. O usuário testou (recompilou com Ctrl+R, reiniciou o Editor inteiro) e reportou que **o problema continua idêntico** — print anexado mostrando 1 ponto de variante isolado, sem nada igual do lado.

**Isso significa que a causa raiz real ainda não foi identificada.** Hipóteses NÃO verificadas ainda, pra investigar amanhã com evidência concreta (não mais só raciocínio):
- Confirmar que o `.asset` do bioma realmente tem os valores esperados após os commits de hoje (reler `BiomeProfile_Tier1.asset` no início da sessão de amanhã).
- Pegar as coordenadas de célula EXATAS do ponto isolado do print (ex.: via algum log temporário em `ChunkTerrainGenerator.PaintChunkIncremental`, ou inspecionando a posição do Tile Palette/Grid no Unity) e rodar `TerrainSelector.ChooseBaseVariant`/`HasMatchingOrthogonalNeighbor` manualmente com essas coordenadas reais (não só testes de varredura 25×25 com offset 0,0 — o mundo real usa `seedOffsetX`/`seedOffsetY` derivados da seed, bem diferentes de 0,0).
- Verificar se existe alguma OUTRA fonte de pintura na `Layer_GrassVariants` além de `ChunkTerrainGenerator` (duplicação de componente, script antigo ainda ativo, etc.).
- Considerar logging temporário (`Debug.Log`) dentro de `ChooseBaseVariant` pra confirmar, em tempo real, se ele está de fato retornando `null` pra essas células ou se está retornando a variante mesmo assim (isolaria se o bug é na lógica de seleção ou em alguma outra camada, tipo RuleTile cache/Editor não reimportando a Tilemap).

Usuário decidiu parar por hoje (cansado, 6ª rodada de teste sem sucesso) — retomar com evidência concreta antes de tentar mais uma correção às cegas.

## Dívida técnica

- **Espaçamento de props não cruza a borda de chunks** — a rejeição por `minSpacing` só olha props já colocados no mesmo chunk.
- **1 terreno de mancha por célula, por nível** — se 2 `BiomeTerrainEntry` do mesmo nível se qualificam na mesma célula, o primeiro da lista vence; não há prioridade por categoria ainda.
- **Macroestruturas (cercas/portões) ainda não têm sistema próprio** — precisam de posicionamento modular com pontos de conexão (docs/new/..., Seção 14), diferente do `PropPlacement`.
- **`layerTilemaps` precisa ser dimensionado manualmente** no Inspector conforme a profundidade máxima real do bioma — se a árvore crescer além do número de Tilemaps configurados, os níveis mais fundos simplesmente não pintam (sem erro, silencioso); vale um aviso no Editor futuramente.
- **Bordas entre biomas diferentes e evolução parcial por dia** — mencionados pelo usuário como direção futura, não implementados aqui (ver seção de decisão acima).

## Próximos passos

Sprint P3 (Evolução por Dia + Editor Preview): usar o `ActualDay` pra transformar a árvore de aninhamento de uma região ao longo do tempo — o usuário já adiantou que não quer troca completa de bioma por dia, e sim uma transformação parcial/gradual (parte do cenário reconhecível, parte novo). O modelo de grids aninhados desta sprint foi pensado de propósito pra comportar isso depois, sem precisar de outra reescrita estrutural.
