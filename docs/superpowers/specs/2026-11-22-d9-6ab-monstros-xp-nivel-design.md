# D9.6a + D9.6b — escala dos monstros, XP e nível do herói (design)

**Data:** 2026-11-22. **Status:** aguardando aprovação do usuário.
**Base:** spec `docs/new/Mudanca_Estrutural_Deadline_9_XP_Cartas_Horda.md` (Seções 2.4 e 3) e roadmap D9.6. O D9.6 foi dividido em 3 partes:
- **a:** monstros;
- **b:** XP e nível;
- **c:** cartas e saída da Loja/perks.

Este design cobre **a** e **b**. A **c** fica pra depois.

## 1. Intenção

Um ciclo de **risco e recompensa**. Subir de dia (tier) é perigoso, porque os monstros novos são bem mais fortes, mas é necessário, porque eles pagam desproporcionalmente mais XP. Ficar num tier mais fácil é seguro e lento demais. O nível dá saltos que se sentem de verdade (+40% de dano por nível), mas escala **menos** que os monstros: **a build (itens e cartas, uns ×8 a ×10 juntos no fim) é o que torna os dias finais jogáveis.**

## 2. Decisões do usuário (2026-11-22)

1. **A base dos heróis não muda** (ex.: Barbarian com 2 de dano e 42 de vida). O T1 pode ter um pouco mais de vida.
2. **Ranged:** mais dano e menos vida. **Melee:** mais vida e menos dano.
3. **O nível tem que se sentir:** +40% de dano e +30% de vida por nível, com piso de +1 de dano e +5 de vida.
4. **Os monstros escalam bem mais que o herói.** Os itens e as cartas fecham a diferença (uns ×8 a ×10 juntos no fim).
5. **XP por kill cresce forte por tier** (mais rápido que a vida), pra subir de tier compensar.
6. **A curva de nível acompanha:** ~100 kills do tier do dia por nível, e 3 a 4 vezes mais no tier anterior.
7. **Voltar o dia vai existir.** XP, gold e gold permanente não podem virar farm em tier baixo. O cap de nível por dia segura o XP. O gold segue o mesmo princípio, com números no D9.7 e no D9.9.
8. **Velocidade dos monstros:** subindo um pouco por tier (pendência de 2026-11-21).

## 3. Tabelas aprovadas

### 3.1 Monstros por tier (melee típico)

| Tier | Vida | Dano | XP por kill |
|---|---|---|---|
| T1 | 12 | 3 | 1 |
| T2 | 27 | 6 | 4 |
| T3 | 69 | 13 | 15 |
| T4 | 180 | 29 | 50 |
| T5 | 485 | 64 | 160 |
| T6 | 1.300 | 126 | 500 |
| T7 | 3.400 | 255 | 1.500 |
| T8 | 9.300 | 480 | 4.500 |
| T9 | 26.000 | 910 | 13.000 |
| T10 | 75.000 | 2.050 | 38.000 |

- **Ranged:** vida ×0,7 e dano ×1,4 em relação ao melee típico do tier.
- **Mini boss:** vida ×4 e dano ×1,5. XP ×3 *(decisão minha)*.
- **Boss:** XP ×10 do tier dele *(decisão minha)*. A vida e o dano dos bosses **não mudam** neste trabalho, porque o rework deles fica pro fim.
- **Dentro de cada tier,** cada monstro mantém a proporção que tem hoje em relação aos colegas do mesmo tipo (melee ou ranged). Ex.: se o Rock Golem tem hoje 1,2× a vida média dos melee T6, continua com 1,2× do valor novo.
- **Velocidade de movimento** (🔢, uma faixa por tier): T1 2–3, T3 ~3, T5 ~3,3, T7 ~3,6, T10 ~4. Cada monstro mantém a proporção dele dentro do tier; quem hoje é rápido (Wolf, Hound, Rider) continua mais rápido que os colegas. Conferir que `velocidade × 0,3 s < 4` (o `AiDestinationLookahead`).

### 3.2 Herói por nível

- `dano(L) = max(dano(L−1) × 1,40; dano(L−1) + 1)`, partindo da base do herói.
- `vida(L) = max(vida(L−1) × 1,30; vida(L−1) + 5)`.
- Ex. Barbarian:

  | Nível | 1 | 2 | 4 | 6 | 8 | 10 | 14 | 20 |
  |---|---|---|---|---|---|---|---|---|
  | Dano | 2 | 3 | 5,9 | 11,5 | 22,6 | 44 | 170 | ~1.280 |
  | Vida | 42 | 55 | 92 | 156 | 264 | 446 | 1.274 | ~6.150 |

- O mesmo fator da vida vale pra **cura** (Cleric e outros que curam) e pro **escudo/bolha** (Paladin), como a spec pede.

### 3.3 XP pra subir de nível

| Nível | XP | Nível | XP |
|---|---|---|---|
| 1→2 | 40 | 11→12 | 46.000 |
| 2→3 | 260 | 12→13 | 104.000 |
| 3→4 | 520 | 13→14 | 136.000 |
| 4→5 | 1.000 | 14→15 | 294.000 |
| 5→6 | 1.400 | 15→16 | 426.000 |
| 6→7 | 3.300 | 16→17 | 874.000 |
| 7→8 | 4.600 | 17→18 | 1.216.000 |
| 8→9 | 10.000 | 18→19 | 2.530.000 |
| 9→10 | 15.500 | 19→20 | 3.570.000 |
| 10→11 | 33.500 | | |

Nível máximo: 20. Assume ~200 kills por dia, que a telemetria vai medir pra calibração.

### 3.4 Cap de nível por dia

- Cap = o nível que fecha aquele dia + 1 de folga: dia 1 → 3, dia 2 → 5, …, dia N → 2N + 1 (máx. 20).
- No cap, o XP de kill vira **gold**: 1 gold por XP excedente, com um limite por dia de 🔢 500 gold. *(Decisão minha. Os valores definitivos ficam pro D9.7, junto com o gold no chão.)*

## 4. Abordagens

| Abordagem | Decisão |
|---|---|
| **A. Tabela por tier (`MonsterScalingSettings`) + botão que reescreve os prefabs.** Os valores continuam visíveis nos prefabs, e o ajuste geral é feito na tabela. | **Escolhida pelo usuário** |
| B. Tabela multiplica em tempo de jogo; prefab vira peso relativo. | Descartada: o número no prefab deixa de ser o do jogo |
| C. Editar os 80 prefabs à mão. | Descartada |

O herói ganha **multiplicadores de nível** compostos ao lado dos `CentralDamageMultiplier`/`CentralHealthMultiplier` que já existem, e a base no prefab fica intocada.

## 5. Design

### 5.1 Regras puras no Core (com testes EditMode)

- **`MonsterScaling`:**
  - pra cada monstro: vida e dano novos a partir do (tier, tipo melee/ranged/mini, proporção dele na média do tier atual, tabela);
  - velocidade nova;
  - XP do kill a partir de (tier, é boss, é mini boss).
- **`HeroLevelRules`:**
  - `DamageAt(base, nível)`, `HealthAt(base, nível)` (com o piso);
  - `XpToNext(nível)` pela tabela;
  - `LevelCapForDay(dia)`;
  - aplicação de XP com overflow: sobe vários níveis de uma vez se precisar, para no cap e devolve o excedente que vira gold.

### 5.2 Monstros (D9.6a)

- **`MonsterScalingSettings`** (ScriptableObject em `Assets/Settings`):
  - a tabela 3.1 (vida, dano, XP e faixa de velocidade por tier);
  - os multiplicadores de ranged e de mini boss;
  - os multiplicadores de XP de mini boss e boss.
- **Botão "Aplicar nos prefabs"** (ContextMenu, só no Editor). Pra cada `EnemyDefinitionSO` do roster comum e de mini bosses:
  - calcula a média atual do tier por tipo e grava `maxHealth`, `health`, `attackDamage` e `moveSpeed` no prefab, mantendo a proporção do monstro;
  - segue o protocolo de prefab: diff limpo, backup e `ImportAsset ForceUpdate`;
  - mostra um relatório (antes → depois) antes de salvar;
  - não mexe nos bosses.
- **XP do kill:**
  - o `EnemyController` ganha `Tier` e `IsMiniBoss`, preenchidos no spawn (`WorldPopulationManager` pelo `EnemyDefinitionSO`, `WorldBossSpawnManager` pelo `BossDefinitionSO`);
  - um evento novo, `GameEvents.EnemyKilledXp(int xp)`, é disparado no `Die()`;
  - o XP vem da tabela; monstro sem tier (spawn de teste, summon) dá o XP do T1.
- **Summons de boss** (`dropsLoot = false`) não dão XP *(decisão minha: mesmo critério do "summon não gera loot")*.

### 5.3 Herói (D9.6b)

- **`HeroLevelSettings`** (ScriptableObject): a tabela 3.3, os fatores 1,40/1,30 e os pisos +1/+5, mais o cap 3.4.
- **`HeroLevel`** (componente no herói, ou no "Systems" com o estado da run):
  - escuta o `EnemyKilledXp`;
  - acumula XP, sobe de nível e respeita o cap do dia;
  - transforma o excedente em gold;
  - dispara `GameEvents.HeroLeveledUp(int nível)`;
  - o D9.6c vai abrir a tela de cartas a partir desse evento.
- **Stats:** `HeroController.LevelDamageMultiplier` e `LevelHealthMultiplier` (estáticos, no mesmo padrão dos `Central*`), compostos em todas as fórmulas de dano que já usam o `CentralDamageMultiplier` e no `EffectiveMaxHealth`. A cura do Cleric passa a usar a vida efetiva, e o escudo do Paladin é multiplicado pelo fator de vida. Ao subir de nível, a vida atual sobe na mesma proporção (não cura totalmente) *(decisão minha)*.
- **Persistência:** nível e XP entram no `RunState` (o save da run). Zeram numa run nova, e a morte apaga o save.
- **HUD:** barra de XP com o nível. *(Decisão minha: o usuário monta o visual na `_TestScene`, como fez com o Resumo total, e eu ligo um componente `XpBarUI` com referências pra imagem de preenchimento e o texto do nível. Até lá, o XP aparece no log.)*

### 5.4 Telemetria

Registrar o nível e o XP por minuto, e os kills por dia (pra calibrar a premissa de ~200 kills por dia).

## 6. Fora do escopo

Cartas e a tela de cartas, a saída da Loja e dos perks (D9.6c); gold no chão e os números de gold (D9.7); gold permanente (D9.9); bosses; voltar o dia.

## 7. Deixado pro construtor

- Onde o `HeroLevel` mora (herói ou Systems), desde que o nível sobreviva a dias e ao save, e zere na run nova.
- A lista exata de fórmulas de dano que recebem o multiplicador de nível: toda fórmula que hoje usa o `CentralDamageMultiplier`.
