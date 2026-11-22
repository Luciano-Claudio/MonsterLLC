# D9.6a+b (escala dos monstros, XP e nível) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Os monstros passam a seguir a tabela por tier (vida, dano, velocidade e XP); o herói ganha XP, nível com saltos fortes, cap por dia com excedente em gold, e o nível sobrevive ao save.

**Architecture:**
- Regras puras no Core (`MonsterScaling`, `HeroLevelRules`) com testes, e dois settings com as tabelas aprovadas.
- Um botão de Editor aplica a escala nos prefabs, com relatório e protocolo de prefab.
- O `HeroLevel` acumula XP pelo evento novo de kill e reescreve `stats.damage`/`stats.maxHealth` do herói a partir da base guardada. Isso vale pros 10 heróis sem tocar nas fórmulas.

**Tech Stack:** Unity 6000.3.8f1, NUnit EditMode, Unity CLI.

**Spec:** `docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md`

## Global Constraints

- Prosa em pt-BR. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Push só com confirmação.
- Protocolo de prefab: diff limpo, backup, `ImportAsset ForceUpdate`, nunca com o prefab aberto no Prefab Mode, diff depois.
- Tabelas: exatamente as das Seções 3.1–3.4 da spec. Ranged: vida ×0,7 e dano ×1,4. Mini boss: vida ×4, dano ×1,5, XP ×3. Boss: XP ×10.
- Herói: dano ×1,40 (piso +1) e vida ×1,30 (piso +5) por nível. Máximo nível 20. Cap do dia = 2·dia + 1. Excedente: 1 gold por XP, até 500 por dia.
- **Bosses não têm stats alterados.**

## Review Focus

1. **Kill com o herói já no cap e no limite de gold:** nada quebra, o XP se perde e a barra fica cheia. Teste `AddXp_AtCapAndGoldLimit_DiscardsRest`.
2. **Um kill grande que vale vários níveis:** sobe vários de uma vez, sem passar do cap. Teste `AddXp_MultipleLevelsInOneKill`.
3. **Nível 20:** não procura `XpToNext(20)` (fora da tabela). Teste `AddXp_AtMaxLevel_NoOverflowIndex`.
4. **Monstro sem tier** (spawn de teste ou summon sem definição): dá o XP do T1, sem erro. Teste `XpForKill_UnknownTier_IsT1`.
5. **Save/load no meio da run:** o nível volta com os stats certos (sem aplicar o nível duas vezes sobre a base). Conferir no jogo, com o Continue.

---

### Task 1: Regras puras no Core

**Files:**
- Create: `Assets/Scripts/Core/Progression/HeroLevelRules.cs` e `Assets/Scripts/Core/AI/MonsterScaling.cs`
- Tests: `Assets/Tests/EditMode/HeroLevelRulesTests.cs` e `Assets/Tests/EditMode/MonsterScalingTests.cs`

**Interfaces (produz):**
- `HeroLevelRules`:
  - `static float DamageAt(float baseDamage, int level)`
  - `static float HealthAt(float baseHealth, int level)`
  - `static long XpToNext(int level)` (tabela 3.3; nível ≥ 20 → `long.MaxValue`)
  - `static int LevelCapForDay(int day)` (min(20, 2·dia + 1))
  - `struct XpResult { int level; long xpIntoLevel; long goldGained; long goldConvertedToday; int levelsGained; }`
  - `static XpResult AddXp(int level, long xpIntoLevel, long xpGained, int day, long goldConvertedToday, int goldDailyLimit)`
- `MonsterScaling`:
  - `static readonly float[] TierHealth/TierDamage/TierXp`
  - `static float TargetHealth(int tier, MonsterKind kind, float ratioToTierAverage)`
  - `static float TargetDamage(...)`
  - `static int XpForKill(int tier, bool isBoss, bool isMiniBoss)`
  - `static float TierSpeed(int tier)`
  - `enum MonsterKind { Melee, Ranged, MiniBoss }`

- [ ] **Step 1: Testes**:
  - `DamageAt_Barbarian`: (2, 1) = 2; (2, 2) = 3 (piso); (2, 10) ≈ 44 (±1); (2, 20) ≈ 1.280 (±2%)
  - `HealthAt_Barbarian`: (42, 2) = 55 (±1); (42, 10) ≈ 446 (±1%); (42, 20) ≈ 6.150 (±2%)
  - `XpToNext_Table`: 1 → 40, 9 → 15.500, 19 → 3.570.000, 20 → `MaxValue`
  - `LevelCap`: dia 1 → 3, dia 5 → 11, dia 10 → 20
  - `AddXp_LevelsUp`: (1, 0, 50, dia 1) → nível 2, `xpIntoLevel` 10
  - `AddXp_MultipleLevelsInOneKill`: (1, 0, 1.000, dia 5) → nível 4, `xpIntoLevel` 180 (40 + 260 + 520 = 820)
  - `AddXp_StopsAtDayCap_OverflowToGold`: (3, 0, 1.000, dia 1, 0, 500) → nível 3, `xpIntoLevel` = 519 (`XpToNext(3)` − 1), `goldGained` = 481
  - `AddXp_AtCapAndGoldLimit_DiscardsRest`: no cap, com `goldConvertedToday` 500 → `goldGained` 0
  - `AddXp_AtMaxLevel_NoOverflowIndex`: nível 20 → continua 20, sem exceção, e o XP vira gold até o limite
  - `TargetHealth_T5MeleeAverage` = 485; `TargetHealth_T5Ranged` = 485 × 0,7; `TargetHealth_MiniBossT5` = 485 × 4; e a proporção multiplica (1,5 → 727,5)
  - `TargetDamage_T9Ranged` = 910 × 1,4
  - `XpForKill`: T4 → 50; T4 mini → 150; T4 boss → 500
  - `XpForKill_UnknownTier_IsT1`: tier 0 → 1
  - `TierSpeed`: T1 → 2,5; T10 → 4; monotônico
- [ ] **Step 2:** RED. **Step 3:** Implementar. **Step 4:** GREEN e suíte. **Step 5: Commit.**

### Task 2: XP do kill (monstros)

**Files:** `EnemyController.cs`, `WorldPopulationManager.cs`, `WorldBossSpawnManager.cs`, `GameEvents.cs`, os pontos de summon com `dropsLoot = false` (grep), o spawn dos filhos do Orc Rider (grep) e o `ChestMimicController`.

- [ ] **Step 1:**
  - `EnemyController` ganha `public int Tier`, `public bool IsMiniBoss`, `public bool GrantsXp = true` e `SetSpawnInfo(int tier, bool miniBoss)`;
  - no `Die()`, dispara `GameEvents.EnemyKilledXp(MonsterScaling.XpForKill(Tier, isBoss, IsMiniBoss))` se `GrantsXp`.
- [ ] **Step 2: Os spawns preenchem.**
  - Spawn comum: tier da definição. Mini boss: `miniBoss = true`. Boss: tier do `BossDefinitionSO`.
  - Summons de boss: `GrantsXp = false`.
  - Filhos herdam o tier do pai.
  - O mimic recebe o tier do dia.
- [ ] **Step 3:** Recompilar e rodar a suíte. **Commit.**

### Task 3: Nível do herói

**Files:** Create `Assets/Scripts/Player/HeroLevel.cs`. Modify `RunState.cs` (`level` = 1, `xpIntoLevel`, `goldFromXpToday`), `HeroController.cs`, `Paladin.cs` (escudo × fator de vida), `GameEvents.cs` (`HeroLeveledUp(int)`) e o reset de dia (`goldFromXpToday` = 0 no `OnDayStart`).

- [ ] **Step 1:** `HeroController` guarda `baseDamage` e `baseMaxHealth` no `Awake`. `ApplyLevel(int level)` faz `stats.damage = DamageAt(base, L)` e `stats.maxHealth = HealthAt(base, L)`; a vida atual sobe na mesma proporção. Expõe `LevelHealthFactor`.
- [ ] **Step 2:** `HeroLevel` (no "Systems", que persiste) escuta o `EnemyKilledXp` e chama `AddXp` com o dia da run. Atualiza o `RunState` (nível, XP, gold convertido, `gold += goldGained` com `GoldChanged`) e, se subiu, `ApplyLevel` + `HeroLeveledUp` + log.
  - Quando o herói nasce (`OnHeroSpawned`), aplica o nível salvo.
  - Run nova: nível 1.
- [ ] **Step 3:** Paladin: escudo máximo × `LevelHealthFactor`.
- [ ] **Step 4:** Recompilar e rodar a suíte. Conferir por `eval` (herói de teste, `ApplyLevel(10)` no Barbarian → dano 44 ±1). **Commit.**

### Task 4: Settings e botão "Aplicar nos prefabs"

**Files:** Create `Assets/Scripts/Core/AI/MonsterScalingSettings.cs` (ScriptableObject com as tabelas, padrões = 3.1) e `Assets/Editor/MonsterScalingApplier.cs` (ContextMenu "Aplicar nos prefabs").

> **Ruling planejado:** `MonsterScaling` lê as tabelas do settings quando houver um; os padrões estáticos são os da spec (pros testes).

- [ ] **Step 1:** Applier:
  - médias atuais por tier e tipo (melee e ranged; mini boss contra os melee comuns), sobre os `EnemyDefinitionSO` do roster e dos mini bosses;
  - por monstro: `maxHealth`, `health`, `attackDamage` e `moveSpeed` (velocidade = `TierSpeed` × proporção da velocidade dele na média do tier);
  - campos float com "damage" ou "heal" no nome, nos componentes do próprio prefab, multiplicados pela razão do dano;
  - prefabs referenciados (`GameObject` serializados com componente de inimigo ou de dano) multiplicados pela mesma razão;
  - modo "prévia" (só o relatório) e modo "aplicar". Os bosses ficam de fora.
- [ ] **Step 2:** Rodar a prévia e mostrar o relatório ao usuário antes de aplicar *(stop: mudança grande em ~80 prefabs)*.
- [ ] **Step 3:** Aplicar, conferir o `git diff --stat` (só campos de stats) e a suíte. **Commit.**

### Task 5: Barra de XP, telemetria e docs

- [ ] `XpBarUI` (preenchimento `Image` + texto do nível, escutando `HeroLeveledUp` e o XP). O usuário monta o visual na `_TestScene`.
- [ ] Telemetria: nível e XP no snapshot por segundo, kills por dia.
- [ ] Spec D9 (Seções 2.4 e 3) e memória. Revisão final, merge e push com confirmação.
