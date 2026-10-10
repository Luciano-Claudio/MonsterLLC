# Sprint P1 — Geração Procedural: Fundação (Chunks + Seed)

## Objetivo

Abrir a Pré-Deadline — Mundo Único com a fundação do gerador procedural de mundo: um sistema de chunks determinístico por seed, sem costura entre chunks, que não depende de nenhum outro sistema da pré-deadline (Safe Zone/População/Boss/Simulation Rings todos virão depois, construídos sobre terreno real, não um testbed).

## Sistemas adicionados

- **`WorldHash`** — hash determinístico puro (algoritmo "Squirrel3"), sem `UnityEngine.Random` nem `Time`, combinando `(x, y, channel, seed)` num `uint`. É o canal de aleatoriedade que todo o resto da geração procedural (terreno, props, rios — Sprints seguintes) vai consumir.
- **`ChunkCoord`** — identidade de chunk por coordenada global (`FromWorldPosition`), com igualdade por valor (nunca por referência/ordem de criação).
- **`ChunkRuntimeData`** — dados de 1 chunk, 100% derivados de `(WorldSeed, ChunkCoord)`; nesta sprint só carrega um `DebugSeedValue` (hash representativo), prova de determinismo antes de qualquer Tilemap real existir (Sprint P2).
- **`WorldGenerationSettings`** — primeiro `ScriptableObject` do projeto: `worldSeed`, `chunkSize`, `streamingRadiusInChunks`, `haloInChunks`, editável como asset sem recompilar.
- **`ChunkStreamer`** — ativa/desativa chunks ao redor de um `streamingTarget` (o jogador, nas sprints seguintes) a cada frame; expõe `GenerateChunkData`/`PeekChunkData` (consulta vizinho mesmo sem estar ativo — base do "halo" que a Sprint P2 vai precisar pra autotile sem costura); desenha os chunks ativos como gizmos coloridos pelo próprio hash, pra inspeção visual direta no Editor.

## Decisões técnicas

- **Separação Core (lógica pura, testável) vs. World (orquestração de Scene)** — mesmo critério já usado no projeto pra `FloorActivationCheck` (Core) vs. `FloorPopulationManager` (World): `WorldHash`/`ChunkCoord`/`ChunkRuntimeData`/`WorldGenerationSettings` vivem em `Core/WorldGeneration/` (testáveis via NUnit EditMode, já que `EditMode.asmdef` referencia o assembly `Core`); `ChunkStreamer` (MonoBehaviour, depende de `Transform`/`Update`) vive em `World/Generation/`.
- **"Squirrel3" como hash base** — algoritmo de domínio público, simples, sem alocação, puramente matemático (positions → uint). Escolhido porque o requisito central da sprint é exatamente "nenhuma dependência de estado/ordem", e esse algoritmo não tem absolutamente nenhum estado interno — a mesma entrada sempre produz a mesma saída, em qualquer thread, em qualquer ordem.
- **`WorldGenerationSettings.worldSeed` ainda não vem do `RunState`** — por enquanto é um campo direto no asset. A integração com Save (GDD Seção 15/29: `WorldSeed` como dado Run-Persistent) é explicitamente escopo da Sprint P10 ("Remoção do Floor System Antigo + RunState Migrado"), não desta sprint — registrado como comentário no próprio código (`Tooltip`) pra não se perder.
- **`ChunkRuntimeData` não gera terreno nenhum ainda** — de propósito. Esta sprint prova só a Fundação (determinismo + streaming); a Sprint P2 ("Terreno Natural") é quem decide máscaras de bioma, autotile de grama alta e reaproveitamento da arte de Floor 1A/2A. Misturar os dois nesta sprint teria acoplado a prova de determinismo a decisões visuais que ainda não foram tomadas.

## Arquivos/classes principais

- `Assets/Scripts/Core/WorldGeneration/WorldHash.cs` (novo) — hash determinístico puro.
- `Assets/Scripts/Core/WorldGeneration/ChunkCoord.cs` (novo) — identidade de chunk por coordenada.
- `Assets/Scripts/Core/WorldGeneration/ChunkRuntimeData.cs` (novo) — dados determinísticos de 1 chunk.
- `Assets/Scripts/Core/WorldGeneration/WorldGenerationSettings.cs` (novo) — `ScriptableObject` de configuração.
- `Assets/Scripts/World/Generation/ChunkStreamer.cs` (novo) — streaming + gizmos de debug.
- `Assets/Scripts/Tests/ChunkStreamingDeterminismTest.cs` (novo) — teste manual (`ContextMenu`) comparando geração direta vs. geração após os 8 vizinhos, em ordem invertida.
- `Assets/Tests/EditMode/WorldHashTests.cs`, `Assets/Tests/EditMode/ChunkCoordTests.cs` (novos) — testes automatizados NUnit.

## Eventos adicionados

Nenhum em `GameEvents` — nada nesta sprint ainda é visível/relevante pra outros sistemas consumirem via evento; a integração (ex.: trocar de dia recalculando chunks) é escopo de sprints futuras (P3 em diante).

## Testes executados

- **EditMode (automatizado):** `WorldHashTests` (determinismo, variação por seed/canal/coordenada, range do float normalizado) e `ChunkCoordTests` (floor correto em coordenadas positivas e negativas, igualdade por valor) — todos cobrindo diretamente o Exit Criteria da sprint.
- **Manual:** `ChunkStreamingDeterminismTest.CompareOrderIndependence()` — gera o mesmo chunk por 2 caminhos diferentes (direto vs. depois dos 8 vizinhos, em ordem deliberadamente invertida) e compara o `DebugSeedValue`; loga OK/FALHA.
- **Visual (Editor, Scene view):** gizmos do `ChunkStreamer` — mover o `streamingTarget` e confirmar que um chunk já visitado, ao ser descarregado e recarregado, sempre volta com a mesma cor.

## Bugs conhecidos

Nenhum.

## Dívida técnica

- `ChunkStreamer` ainda não está numa Scene/prefab real — precisa ser instanciado manualmente (ou via um GameObject de teste) até a Sprint P2 trazer conteúdo real pra olhar.
- `haloInChunks` está declarado em `WorldGenerationSettings` mas ainda não é consumido por nada — vale pra configurar antecipadamente, mas quem vai precisar de verdade é o autotile da Sprint P2 (resolver vizinhança na borda de um chunk olhando o vizinho mesmo que ele não esteja "ativo" pelo raio de streaming).
- Nenhum valor de `chunkSize`/`streamingRadiusInChunks` foi calibrado por teste de performance real — são os mesmos valores ilustrativos do documento de origem (Seção 26: "não fixar sem teste").

## Próximos passos

Sprint P2 (Geração Procedural — Terreno Natural): usar os canais de `WorldHash` pra decidir máscaras de bioma reais, registrar o tileset/autotile de grama alta já existente (Floor 1A/2A) como o primeiro `BiomeProfile`, e posicionar os primeiros `EnvironmentPropSO` (árvores/rochas) sem bloquear corredores — tudo em cima da Fundação de chunks construída aqui, sem precisar revisitar `WorldHash`/`ChunkCoord`/`ChunkStreamer`.
