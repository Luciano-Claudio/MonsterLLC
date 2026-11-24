# Etapa 3 — Baú, mímico, vendedor, itens e Stats — Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Itens (vendedor/level up) e Stats (a cada 5 níveis/baú) entram como novos tipos de carta na tela de 3 cartas; baú, mímico e vendedor voltam ao mundo com aparições sorteadas por run.

**Architecture:** Item e Stat são `CardDefinition` (kind Item/Stat) somados pelo mesmo `CardBonuses`; bônus gerais (habilidade `None`) passam a multiplicar os de cada habilidade dentro de `CardBonuses.Get`. Uma regra pura `CardRules.RollOffer(OfferSource …)` decide o que cada fonte oferece; `HeroCards` guarda a fonte da oferta (reroll sorteia da mesma). Uma regra pura `RewardSchedule` sorteia os segundos de aparição; `WorldChestSpawnManager` passa a soltar baú e vendedor por ela.

**Tech Stack:** Unity 6000.3.8f1, C#, NUnit (EditMode), evals no Editor (`.superpowers/tools/eval.sh`).

**Spec:** `docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md`

## Global Constraints

- Regras puras em `Assets/Scripts/Core/**` com teste EditMode; MonoBehaviour verificado por eval (o asmdef de teste não enxerga Assembly-CSharp). Baseline da suíte: 273 passando + 3 falhas conhecidas de `BagTests`.
- Evals em `.superpowers/sdd/2026-11-24-etapa-3-bau-vendedor-itens/evals/`. Toda eval desfaz o que fez: chama `OnDisable` (reflexão) antes de `DestroyImmediate`, destrói `CreateInstance`, zera estáticos usados (`HeroController.IsDying=false`, `HeroController.Cards.Reset()`), e se mexer em objeto de cena reabre `_TestScene` do disco no fim. Domain reload está DESLIGADO.
- Todo número 🔢 em asset (carta, `RunSettings`), nunca fixo no código.
- Texto de jogo em inglês; comentários em pt-BR.
- Enums serializados (`CardStat`, `CardKind`): só ACRESCENTAR valores no fim, nunca reordenar.
- Não editar cena/prefab sem backup + diff; arquivo de cena é LF.
- Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Nunca push sem perguntar.

## Review Focus

1. Com 6 itens, nenhuma fonte oferece item novo (7º) — nem level up, nem vendedor, nem reroll. (teste na Task 2)
2. Fonte sem nenhuma carta possível não abre a tela nem pausa; o vendedor não é consumido. (testes nas Tasks 2, 3 e 8)
3. Nível múltiplo de 5 com todos os Stats no máximo cai pra oferta comum, sem travar. (teste na Task 2)
4. Hora do próximo baú chegando com um baú ainda fechado: espera, não some nem duplica. (eval na Task 7)
5. Run nova depois de uma run com itens: barra de itens, bônus e ordem dos itens zerados. (eval na Task 9)

---

### Task 1: Novos atributos de carta e bônus gerais

**Files:**
- Modify: `Assets/Scripts/Core/Cards/CardDefinition.cs`, `Assets/Scripts/Core/Cards/CardBonuses.cs`
- Test: `Assets/Tests/EditMode/CardRulesTests.cs`

**Interfaces:**
- Produces:
  - `CardStat` ganha, no fim: `DamageTaken, Regen, XpGain, Thorns, BlastChance`.
  - `CardDefinition.healthPerLevel` (float, fração da vida base por nível; 0 = nenhuma) e `CardDefinition.baseValue` (float somado uma vez quando nível ≥ 1; 0 padrão).
  - `CardBonuses.Get(ability, stat)`: para `ability != None` combina com o geral — Damage: `dmg[a] * dmg[None]`; Cooldown: `max(0.2, (1 - sumA) * (1 - sumNone))`; demais: `(1 + sumA) * (1 + sumNone)`. `Get(None, X)` não muda.
  - `CardBonuses.Sum(CardAbility ability, CardStat stat) -> float`: soma crua (aditivos), sem o 1+.
  - `Recompute`: aditivo de cada carta = `baseValue + valuePerLevel * nível`; `healthPerLevel * nível` soma em `(None, MaxHealth)`.

- [ ] **Step 1: Testes que falham** (em `CardRulesTests`, criando cartas com `ScriptableObject.CreateInstance<CardDefinition>()`):
  - `General_damage_multiplies_ability_damage`: primário 1,25 nv1 + geral (ability None, Damage) 1,10 nv1 → `Get(Primary, Damage) == 1.375f` (±1e-4); `Get(None, Damage) == 1.10f`.
  - `General_cooldown_combines_with_floor`: Shift Cooldown 0,10 nv2 + geral Cooldown 0,15 nv1 → `Get(Shift, Cooldown) == 0.8f * 0.85f`; com geral 0,15 nv3 + shift 0,10 nv5 → `>= 0.2f`.
  - `Item_health_adds_to_max_health`: carta kind Item, stat MoveSpeed 0,08, `healthPerLevel` 0,10, nv3 → `Get(None, MaxHealth) == 1.3f`, `Get(None, MoveSpeed) == 1.24f`.
  - `Base_value_counts_once`: BlastChance `baseValue` 0,05 + `valuePerLevel` 0,05 → nv1 `Sum(None, BlastChance) == 0.10f`, nv5 `== 0.30f`, nv0 `== 0f`.
- [ ] **Step 2:** rodar `bash .superpowers/tools/edittests.sh` → os 4 falham (compilação ou asserts).
- [ ] **Step 3:** implementar as mudanças acima.
- [ ] **Step 4:** suíte → os 4 passam, total 277 + 3 `BagTests` conhecidos.
- [ ] **Step 5:** commit `feat(cartas): bonus gerais multiplicam os da habilidade; vida por nivel e novos atributos`.

### Task 2: Regra de oferta por fonte

**Files:**
- Modify: `Assets/Scripts/Core/Cards/CardRules.cs`, `Assets/Scripts/Core/Run/RunSettings.cs`
- Test: `Assets/Tests/EditMode/CardRulesTests.cs`

**Interfaces:**
- Consumes: `CardKind`, `CardDefinition` (Task 1).
- Produces:
  - `public enum OfferSource { LevelUp, Chest, Vendor }` (em `CardRules.cs`).
  - `public struct OfferRules { public int maxItemSlots; public int statEveryLevels; public float chestStatChance01; }` com `static OfferRules From(RunSettings s)`.
  - `CardRules.RollOffer(OfferSource source, int heroLevel, IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string,int> owned, string heroId, int count, System.Random rng, OfferRules rules, List<CardDefinition> result)`.
  - `CardRules.CountOwnedItems(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string,int> owned) -> int`.
  - `RunSettings` novos 🔢: `maxItemSlots = 6`, `statEveryLevels = 5`, `chestStatChancePercent = 15f`.
- Regras (spec 5.1): LevelUp com `heroLevel % statEveryLevels == 0` → só Stat; se não houver Stat elegível, cai pra comum. LevelUp comum → Upgrade + Item, sorteio igual. Chest → sorteia Upgrades; cada carta, com chance `chestStatChance01`, é trocada por um Stat elegível distinto (se houver). Vendor → só Item. Em toda fonte: item que o herói não tem é inelegível quando `CountOwnedItems >= maxItemSlots`; filtros antigos (herói, nível máximo) continuam. Devolve menos de `count` se faltar.

- [ ] **Step 1: Testes que falham:**
  - `Level_5_offers_only_stats` (nível 5 e 10 → todas kind Stat).
  - `Level_5_without_stats_falls_back_to_common` (Stats todos no máximo → kinds ⊆ {Upgrade, Item}, count 3).
  - `Common_level_mixes_upgrades_and_items_never_stats` (nível 3, 200 sorteios com rng semeado → aparece Upgrade e Item, nunca Stat).
  - `Six_items_blocks_new_items_everywhere` (6 itens possuídos em nv1 → LevelUp e Vendor nunca trazem item fora dos 6; Vendor traz só itens possuídos).
  - `Vendor_with_all_items_maxed_returns_empty` (6 itens no máximo → result.Count == 0).
  - `Chest_stat_chance_zero_and_one` (chance 0 → nenhum Stat; chance 1 com 6 Stats → 3 Stats).
- [ ] **Step 2:** suíte → falham.
- [ ] **Step 3:** implementar; `Roll` existente vira o núcleo usado por `RollOffer` com um filtro de kind.
- [ ] **Step 4:** suíte → passam.
- [ ] **Step 5:** commit `feat(cartas): oferta por fonte (level up, stats a cada 5, bau, vendedor) e limite de 6 itens`.

### Task 3: HeroCards abre ofertas por fonte

**Files:**
- Modify: `Assets/Scripts/Player/HeroCards.cs`
- Eval: `evals/t3_sources.cs`

**Interfaces:**
- Consumes: `CardRules.RollOffer`, `OfferSource`, `OfferRules.From(RunSettings.Current)` (Task 2).
- Produces:
  - `HeroCards.EnqueueOffer(OfferSource source)` — enfileira; sorteia ao abrir. (O `EnqueueOffer(IReadOnlyList<CardDefinition>)` antigo sai.)
  - `HeroCards.CanOffer(OfferSource source) -> bool` — há ao menos 1 carta possível agora (usa o nível atual do herói).
  - `HeroCards.CurrentSource` (OfferSource), `HeroCards.OwnedItems` (`IReadOnlyList<CardDefinition>`, itens na ordem em que foram pegos), evento `GameEvents.OnItemsChanged` (Action, novo em `GameEvents`) disparado quando `OwnedItems` muda (grant de item e início de run).
- Fila de level up guarda o NÍVEL de cada oferta (`Queue<int>`), pra saber se é nível de Stat. Reroll sorteia de novo com a mesma fonte e nível. Oferta com 0 cartas é pulada sem pausar (já é o comportamento; manter).

- [ ] **Step 1: Eval que falha** (padrão de `t9_cards` das etapas 1+2, com `TimeManager`/`HeroCards` próprios e `OnEnable`/`OnDisable` por reflexão): `HeroLeveledUp(5)` → oferta aberta com 3 Stats; `Reroll()` → ainda 3 Stats; escolher; `EnqueueOffer(Vendor)` → 3 Items e `CurrentSource == Vendor`; escolher item → `OwnedItems.Count == 1` e `OnItemsChanged` disparou; com 6 itens no máximo, `CanOffer(Vendor) == false` e `EnqueueOffer(Vendor)` não pausa; `HandleRunStart` → `OwnedItems.Count == 0`. Imprime `PASS`/`FAIL`.
- [ ] **Step 2:** rodar → FAIL (API não existe / compilação).
- [ ] **Step 3:** implementar.
- [ ] **Step 4:** eval → PASS; rodar de novo `t9_cards`, `t9_noscreen`, `u2_cards_popup` (pasta das etapas 1+2) adaptados à API nova → PASS.
- [ ] **Step 5:** commit `feat(cartas): HeroCards abre ofertas por fonte, reroll da mesma fonte, itens possuidos`.

### Task 4: Efeitos no herói (redução, regeneração, XP, espinhos, explosão)

**Files:**
- Modify: `Assets/Scripts/Player/Heroes/HeroController.cs` (TakeDamage ~1002, UpdateHpRegen ~1157), `Assets/Scripts/Player/HeroLevel.cs`, `Assets/Scripts/Enemies/MeleeEnemyController.cs:191`, `Assets/Scripts/Core/Run/RunSettings.cs`
- Create: `Assets/Scripts/Player/BlastCharmEffect.cs` (MonoBehaviour no Systems, via `SystemsBootstrap`)
- Eval: `evals/t4_effects.cs`

**Interfaces:**
- Consumes: `HeroController.Cards.Sum/Get` (Task 1).
- Produces / valores:
  - Redução: `total = DamageReductionPercent/100 + Cards.Sum(None, DamageTaken)`, limitado a `RunSettings.damageReductionCapPercent = 75f` 🔢 /100.
  - Regeneração: `HpRegenPercentPerSecond + Cards.Sum(None, Regen) * 100` (carta em fração de vida/s).
  - XP: `HeroLevel` multiplica o XP coletado por `Cards.Get(None, XpGain)`, guardando a fração que sobra (nada se perde; zera na run nova).
  - Espinhos: no golpe melee (`MeleeEnemyController:191`), se `Cards.Sum(None, Thorns) > 0`, o inimigo leva `dano causado × Sum(None, Thorns)` (`isDirectPlayerHit=false`).
  - Explosão: `BlastCharmEffect` escuta `GameEvents.OnEnemyKilledDetailed`; com chance `Cards.Sum(None, BlastChance)`, causa em área de raio `RunSettings.blastRadius = 1.5f` 🔢 dano = `HeroController.Instance.EffectsBaseDamage × RunSettings.blastDamagePercent (100f) 🔢 /100` nos inimigos vivos (mesma consulta de inimigos que o Ground Crack usa). Explosão não encadeia (inimigo morto por explosão não rola explosão).
  - `HeroController.EffectsBaseDamage` (virtual float): dano do ataque primário com os multiplicadores do primário e das cartas; `Barbarian` sobrescreve com a mesma fórmula do golpe (sem crítico).

- [ ] **Step 1: Eval que falha:** com `Cards.Recompute` dando DamageTaken 0,5 + `DamageReductionPercent` 50 → herói de teste com 100 de vida leva `TakeDamage(100)` e fica com 75 (teto 75%); Regen 0,01 → `UpdateHpRegen` cura > 0 com `HpRegenPercentPerSecond = 0`; XpGain 0,5 → `XpCollected(1)` duas vezes = 3 XP no `RunState`; Thorns 0,3 → golpe de um `MeleeEnemyController` de 10 de dano tira 3 dele; BlastChance 1 → kill dispara dano em inimigo a 1 unidade, não em um a 3 unidades. Imprime PASS/FAIL.
- [ ] **Step 2:** rodar → FAIL.
- [ ] **Step 3:** implementar.
- [ ] **Step 4:** eval → PASS; suíte sem regressão.
- [ ] **Step 5:** commit `feat(itens): reducao com teto, regeneracao, XP, espinhos e explosao ao matar`.

### Task 5: Assets — 8 itens, 6 Stats, sai a Thick Hide

**Files:**
- Create: `Assets/Settings/Cards/item_*.asset` (8), `Assets/Settings/Cards/stat_*.asset` (6) — via eval no Editor (`AssetDatabase.CreateAsset`), nunca YAML à mão.
- Modify: `Assets/Resources/CardCatalog.asset`; Delete: `Assets/Settings/Cards/any_max_health.asset`
- Eval: `evals/t5_assets.cs`

Valores (spec 5.2/5.3; `ability = None` em todos; descrições com `{0}`):

| id | kind | title | stat | valuePerLevel | baseValue | healthPerLevel | maxLevel |
|---|---|---|---|---|---|---|---|
| item_swift_boots | Item | Swift Boots | MoveSpeed | 0.08 | 0 | 0.10 | 5 |
| item_iron_plate | Item | Iron Plate | DamageTaken | 0.05 | 0 | 0.10 | 5 |
| item_troll_heart | Item | Troll Heart | Regen | 0.005 | 0 | 0.10 | 5 |
| item_power_gauntlet | Item | Power Gauntlet | Damage | 1.10 | 0 | 0.10 | 5 |
| item_magnet_ring | Item | Magnet Ring | Magnet | 0.20 | 0 | 0.10 | 5 |
| item_scholar_tome | Item | Scholar Tome | XpGain | 0.10 | 0 | 0.10 | 5 |
| item_thorn_mail | Item | Thorn Mail | Thorns | 0.30 | 0 | 0.10 | 5 |
| item_blast_charm | Item | Blast Charm | BlastChance | 0.05 | 0.05 | 0.10 | 5 |
| stat_ancestral_strength | Stat | Ancestral Strength | Damage | 1.25 | 0 | 0 | 3 |
| stat_iron_will | Stat | Iron Will | DamageTaken | 0.10 | 0 | 0 | 3 |
| stat_quick_mind | Stat | Quick Mind | Cooldown | 0.15 | 0 | 0 | 3 |
| stat_vitality | Stat | Vitality | MaxHealth | 0.25 | 0 | 0 | 3 |
| stat_hunters_eye | Stat | Hunter's Eye | Area | 0.20 | 0 | 0 | 3 |
| stat_greed | Stat | Greed | XpGain | 0.20 | 0 | 0 | 3 |

`ValueText` de `CardSlotUI` precisa mostrar certo: Damage multiplicativo (1,10 → 10%), Regen em % por segundo (0,005 → "0.5%"), BlastChance com baseValue (nv1 10%). Descrições de item mencionam a vida: ex. "+10% Max Health, +{0} Movement Speed".

- [ ] **Step 1: Eval que falha:** catálogo tem 25 cartas (12 − 1 + 14), nenhuma com id `any_max_health`, 8 Item / 6 Stat / 11 Upgrade, todos os ids únicos, `RunSettings` sem aviso.
- [ ] **Step 2:** rodar → FAIL.
- [ ] **Step 3:** criar os assets pela eval de criação, atualizar catálogo, apagar Thick Hide (`AssetDatabase.DeleteAsset`).
- [ ] **Step 4:** eval → PASS; `u2_cards_popup` com um Item e um Stat reais → PASS.
- [ ] **Step 5:** commit `feat(itens): 8 itens e 6 stats no catalogo; Thick Hide sai`.

### Task 6: Agenda de aparições

**Files:**
- Create: `Assets/Scripts/Core/Run/RewardSchedule.cs`; Test: `Assets/Tests/EditMode/RewardScheduleTests.cs`
- Modify: `Assets/Scripts/Core/Run/RunSettings.cs`

**Interfaces:**
- Produces: `RewardSchedule.Build(int minCount, int maxCount, float runSeconds, float quietSeconds, System.Random rng) -> float[]` (ordenado). N sorteado em [min, max]; `[quietSeconds, runSeconds)` dividido em N faixas iguais; um segundo aleatório por faixa.
- `RunSettings` novos 🔢: `chestMinCount = 4`, `chestMaxCount = 5`, `vendorMinCount = 6`, `vendorMaxCount = 8`, `rewardQuietSeconds = 60f`, `vendorPrefab` (GameObject → `Assets/Prefabs/World/Vendor.prefab`).

- [ ] **Step 1: Testes que falham:** `Count_within_min_max` (100 seeds, 4..5); `Sorted_and_inside_its_band` (cada valor i em `[q + i·w, q + (i+1)·w)`); `Nothing_before_quiet_seconds`; `Min_greater_than_max_or_zero_is_safe` (0/0 → vazio; 5/3 → usa 5).
- [ ] **Step 2:** suíte → falham. **Step 3:** implementar. **Step 4:** suíte → passam.
- [ ] **Step 5:** commit `feat(run): agenda de aparicoes de bau e vendedor`.

### Task 7: Baú e mímico pela agenda, dando cartas

**Files:**
- Modify: `Assets/Scripts/World/WorldChestSpawnManager.cs`, `Assets/Scripts/World/ChestRewardSystem.cs`, `Assets/Resources/RunSettings.asset` (`chestsEnabled = true`)
- Eval: `evals/t7_chest.cs`

**Interfaces:**
- Consumes: `RewardSchedule.Build` (Task 6), `HeroCards.EnqueueOffer(OfferSource.Chest)` (Task 3).
- Produces: `WorldChestSpawnManager.DebugTick()` (editor) e `ChestTimes` (`IReadOnlyList<float>`); `ChestRewardSystem.GrantReward(GameObject collected)` = `EnqueueOffer(Chest)` + destrói o baú. Caminho de gold sai (e os campos de gold de `ChestSpawnSettings` ficam sem uso — remover).
- No `DayStarted` (= run): monta a agenda com `chestMinCount/MaxCount`. No `Update` (com `GameplayGate`): quando `RunElapsed` passa a próxima hora e não há baú vivo → `SpawnOneChest()` (mímico em `mimicChancePercent`); se há baú vivo, a hora espera. Sorteio de dia (`chestSpawnChancePercent`) sai.

- [ ] **Step 1: Eval que falha** (com `debugFixedSpawn` igual ao `WorldBossSpawnManager`): agenda com 4–5 horas; avançar até a 1ª → 1 baú; avançar até a 2ª com o 1º fechado → ainda 1 baú, hora pendente; `Interact` no baú → oferta aberta com `CurrentSource == Chest` e baú destruído; próximo tick → 2º baú. Limpa `//CHESTS` e reabre a cena.
- [ ] **Step 2:** FAIL. **Step 3:** implementar. **Step 4:** PASS.
- [ ] **Step 5:** commit `feat(bau): bau e mimico pela agenda da run, abrindo 3 cartas`.

### Task 8: Vendedor em runtime dando itens

**Files:**
- Modify: `Assets/Scripts/World/Vendor.cs`, `Assets/Scripts/World/WorldChestSpawnManager.cs`, `Assets/Scripts/World/LootDrop.cs:93-110`, `Assets/Resources/RunSettings.asset` (`vendorEnabled = true`, `vendorPrefab`)
- Eval: `evals/t8_vendor.cs`

**Interfaces:**
- Consumes: `RewardSchedule` (Task 6), `HeroCards.CanOffer/EnqueueOffer(OfferSource.Vendor)` (Task 3).
- `WorldChestSpawnManager` também solta o vendedor (mesmo anel de spawn do baú), pela agenda `vendorMin/MaxCount`; no máximo 1 vivo; hora pendente espera. Se `!CanOffer(Vendor)`, não solta (e descarta as horas restantes).
- `Vendor.Interact`: se `CanOffer(Vendor)` → `EnqueueOffer(Vendor)` e o vendedor some (Destroy); senão não faz nada. Ao nascer: `Instance = this`, toca `OpenTrigger`. Venda de materiais, `pricingSettings`, `SellBonusMultiplier`, `TryComputeSaleValue` e o carrinho saem; `LootDrop` deixa de chamar o vendedor (auto-venda sai). O `Vendor` fixo da cena se desliga sozinho (só o criado pelo manager fica ativo — marcar com um campo `spawnedByManager`).

- [ ] **Step 1: Eval que falha:** agenda 6–8; avançar até a 1ª hora → 1 `Vendor` vivo e `Vendor.Instance` aponta pra ele; `Interact` → oferta com 3 Items, vendedor destruído; com 6 itens no máximo → próxima hora não solta vendedor.
- [ ] **Step 2:** FAIL. **Step 3:** implementar. **Step 4:** PASS; `grep -rn "TryComputeSaleValue\|SellBonusMultiplier" Assets/Scripts` → vazio; compilação limpa.
- [ ] **Step 5:** commit `feat(vendedor): vendedor itinerante pela agenda, dando 3 itens; venda de materiais sai`.

### Task 9: Barra de itens no HUD

**Files:**
- Modify: `Assets/Scripts/UI/InventoryHudUI.cs`
- Eval: `evals/t9_hud.cs`

**Interfaces:**
- Consumes: `HeroCards.OwnedItems`, `GameEvents.OnItemsChanged` (Task 3).
- Os filhos de `Inventory_Background` (6) mostram, em ordem, ícone do item e nível no texto `Quantity` (só o número); vazios escondidos. Para de escutar `OnBagChanged`; `visuals`/`LootVisualSettings` saem.

- [ ] **Step 1: Eval que falha** (na `_TestScene`): conceder 2 itens (um 2x) → 2 slots visíveis com os ícones dos itens e "2"/"1"; `HandleRunStart` → 0 visíveis. Reabre a cena no fim.
- [ ] **Step 2:** FAIL. **Step 3:** implementar. **Step 4:** PASS; suíte e todas as evals da etapa → PASS.
- [ ] **Step 5:** commit `feat(hud): barra inferior mostra os itens e o nivel`.
