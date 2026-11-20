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
- **`Auto Sync Transforms` ligado** nas configurações de física 2D: candidato a corte, ainda não alterado.
- **Um só collider de física por monstro:** não foi feito.
- **Herói preso pelos monstros empilhados:** problema de jogo, fica pro D9.4.
