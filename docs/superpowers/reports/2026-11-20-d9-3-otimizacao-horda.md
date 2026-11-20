# D9.3: resultado da otimização da horda

**Data:** 2026-11-20. **Medição:** no Editor, mesmo harness do D9.2 (`HordeDensityTest`), já com a população descendo entre as etapas. Os dados brutos estão em `DensityReports/density_20261120_073947.md`. O código é o commit `a6791a60`.

**Meta do D9.2 cumprida:** 300 monstros andando a **76 FPS** no Editor (meta: 60).

## Antes e depois

| Etapa | D9.2 | D9.3 | ms médio D9.3 | GC D9.3 |
|---|---|---|---|---|
| 100 parado | 87 FPS | 164 FPS | 6,1 | 38 KB |
| 200 parado | 43 FPS | 88 FPS | 11,3 | 51 KB |
| 300 parado | 23 FPS | 54 FPS | 18,4 | 70 KB |
| 100 andando | — | 151 FPS | 6,6 | 41 KB |
| 200 andando | — | 115 FPS | 8,7 | 41 KB |
| 300 andando | 16–17 FPS | 76 FPS | 13,1 | 42 KB |
| 200 monstros + 200 efeitos | 12 FPS* | 85 FPS | 11,8 | 36 KB |
| 200 monstros + 500 efeitos | 1 FPS* | 50 FPS | 19,8 | 53 KB |

\* No D9.2, essas etapas rodaram com 300 monstros (defeito do harness, corrigido).

Os scripts dos monstros (`Update`) caíram de 23,3 ms pra 6,0 ms com 300 parados. Com 500 efeitos, a física caiu de ~580 ms pra 5,8 ms, porque os efeitos agora estão na layer `HeroAttack`, que colide só com `Enemy`.

## O que foi feito

1. **Lixo de memória:**
   - A espécie do monstro é guardada como número (`SpeciesRegistry`).
   - As referências do herói são cacheadas (`PlayerCollider`/`PlayerHero`).
   - Os delegates da patrulha e do flanco são cacheados.
2. **Coesão de bando:** roda a cada 0,25 s 🔢, com fase sorteada por monstro. As consultas de vizinhos enxergam só a layer `Enemy`.
3. **Simulation Rings:** o monstro em combate a mais de 20 unidades 🔢 do herói decide a cada 0,3 s e fica sem RVO. Boss sempre tem simulação completa.
4. **Matriz de colisão:**
   - Layer `HeroAttack`, que colide só com `Enemy`, e layer `EnemyAttack`, que colide só com `Player`.
   - 27 prefabs de projétil movidos pra elas.
5. **Animator desligado fora da tela:**
   - Usa `CullCompletely`.
   - Fora da tela, o monstro anda sem esperar a animação Walk.
   - Morte e golpe fora da tela resolvem na hora.

## O que sobrou

- **300 parado: 54 FPS.** Parado, a horda inteira fica perto do herói, dentro do anel de simulação completa. Por isso é o pior caso.
- **Lixo de memória: 40–70 KB/frame.** Ainda não é zero, e a origem não foi investigada.
- **Lixo de memória: investigado e corrigido** (commit `636a4773`, medição `density_20261120_080247.md`). O harness gravou capturas do Profiler com pilha de chamada. A soma por função apontou:
  - `HeroPickupRadius.OnTriggerStay2D`: 74 KB/frame com 300 parados. Todo monstro encostado no trigger de coleta disparava a chamada, e cada `GetComponent` nulo gera lixo no Editor. Corrigido com a layer `Pickup` (colide só com `Default`, onde o loot está) e `TryGetComponent`.
  - `ChunkStreamer`/`ChunkTerrainGenerator`: ~5 KB/frame. Corrigido: só recalcula quando o herói troca de chunk, os buffers são reaproveitados e o `ChunkCoord` virou `IEquatable`.

  Depois disso, o lixo medido caiu pra 31–40 KB/frame, e quase tudo é ferramenta: overlay do harness (~20 KB) e pacotes LevelPlay/IAP do Editor (~7,5 KB). Do jogo, sobra só lixo por evento: números de dano no herói (~5 KB/frame parado) e projéteis de monstro (~1 KB). Os dois resolvem com pooling, se um dia valer a pena.
- **Etapa "200 monstros + 500 efeitos" é instável** (50 FPS numa rodada; 2, 6 e 11 FPS em outras, sem mudança relevante de código). A causa é a espiral do `FixedUpdate`: os scripts de física subiram de 1,1 pra 9 ms por frame, ou seja, uns 8 passos de física por frame. Com 500 colliders de efeito, o jogo fica no limite, e pequenas diferenças (quantos monstros grudam no herói) decidem se ele despenca. Isso confirma a decisão do usuário: o efeito de chamas no jogo é só animação, e dano em área é por consulta (`Overlap`), nunca por centenas de triggers.
- **`Auto Sync Transforms`: testado e descartado** (`DensityReports/density_20261120_074913.md`). Ligado, a sincronização custava no máximo 0,09 ms por frame. Desligado, tudo piorou: 300 andando caiu de 76 pra 61 FPS, e 200 monstros + 500 efeitos de 50 pra 11 FPS (a física subiu de 5,8 pra 59 ms). A configuração voltou a ficar ligada. Não tentar de novo sem um motivo novo.
- **Um só collider de física por monstro:** não foi feito.
- **Herói preso pelos monstros empilhados:** problema de jogo, fica pro D9.4.
