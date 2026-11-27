# Loja permanente, loot de boss e rerolls da run — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Loja de melhorias permanentes por herói no Main_Menu (comprar, vender tudo, desligar),
os efeitos delas na run, os loots Gold Bag / Stat Item / Reroll_Dice e os rerolls como estoque da run.

**Architecture:** Definições em ScriptableObject (`PermanentUpgradeDefinition`) listadas num catálogo
em Resources; níveis por herói salvos no `profile.json` (junto do gold, `PermanentGold`). No início
da run, `PermanentBonuses` (estático, Core) é montado com os níveis ativos do herói escolhido e é
lido pelos pontos que já existem (`CardBonuses.Get`, `HeroController`, `HeroLevel`, `XpGemPool`,
população/inimigos). Os 3 loots novos entram no `XpGemPool` como novos tipos. A loja é um componente
que acha os objetos da cena pelo nome (padrão do `MainMenuNavigationUI`).

**Tech Stack:** Unity 6000.3.8f1, C#, TextMeshPro, uGUI, NUnit (EditMode), evals via `.superpowers/tools/eval.sh`.

**Spec:** `docs/superpowers/specs/2026-11-26-loja-permanente-design.md`

## Global Constraints

- Textos de jogo em inglês; comentários e conversa em pt-BR.
- Compras SÓ no menu; nada se compra na run.
- Vender tudo devolve 100% do gold gasto naquele herói.
- `profile.json` sobrevive à morte; save antigo só com `gold` carrega com tudo no nível 0.
- Limite de 300 monstros comuns vivos (D9.3) continua valendo.
- Preços por nível: A = 50·100·300·500·1000; B = 100·200·500·1000·2000; Maldição = 500·1000·2000·5000·10000; Ressurreição = 10000.
- Cena: editar só pela API do Unity (nunca à mão), com `git diff` antes e depois; o usuário pode ter a cena aberta.
- Evals limpam tudo (OnDisable, DestroyImmediate, estáticos zerados: `PermanentBonuses.Reset()`, `CardRunState`, `HeroController.Cards`), e usam arquivo de profile temporário (`PermanentGold.UseFileForTests`). Nunca tocar o `profile.json`/`save.json` reais.
- Play test sempre começando pelo Main_Menu.

## Review Focus

1. `profile.json` corrompido, antigo (só gold) ou com id de melhoria que não existe mais: carrega sem exceção, ignora o desconhecido.
2. Comprar sem gold suficiente / clique duplo no Buy: nunca deixa o gold negativo nem passa do nível máximo.
3. Melhoria desligada no Toggle não vale na run, mas conta no Refund (devolve o gold dela).
4. Trocar de herói no menu: o Stats_Painel mostra só o grupo do herói escolhido e o Damage selecionado.
5. Stat Item coletado com uma tela de cartas já aberta: entra na fila, não perde a oferta.

---

### Task 1: Regras e save das melhorias permanentes

**Files:**
- Create: `Assets/Scripts/Core/Meta/PermanentUpgradeDefinition.cs`, `Assets/Scripts/Core/Meta/PermanentUpgradeCatalog.cs`, `Assets/Scripts/Core/Meta/PermanentShop.cs`
- Modify: `Assets/Scripts/Core/Save/PermanentGold.cs` (o `Profile` ganha os níveis)
- Test: `Assets/Tests/EditMode/PermanentShopTests.cs`

**Interfaces:**
- Produces:
  - `enum PermanentStat { Damage, MaxHealth, Armor, Regen, MoveSpeed, Cooldown, UltimateCharge, UltimateCost, Magnet, Xp, Gold, Luck, Curse, Revival, Reroll, Size, Multishot, FlameArea }`
  - `class PermanentUpgradeDefinition : ScriptableObject { string id; string buttonName; string title; [TextArea] string description; Sprite icon; PermanentStat stat; int maxLevel; float valuePerLevel; int[] prices; }` (`prices.Length >= maxLevel`)
  - `class PermanentUpgradeCatalog : ScriptableObject { List<HeroUpgrades> heroes; static PermanentUpgradeCatalog Load(); IReadOnlyList<PermanentUpgradeDefinition> For(string heroKey); }` — `HeroUpgrades { string heroKey; List<PermanentUpgradeDefinition> upgrades; }`; asset em `Resources/PermanentUpgradeCatalog`.
  - `static class PermanentShop`: `int Level(string hero, string id)`, `bool IsActive(string hero, string id)`, `int EffectiveLevel(string hero, string id)` (0 se desligada), `int NextPrice(PermanentUpgradeDefinition d, int level)` (-1 no máximo), `bool CanBuy(string hero, PermanentUpgradeDefinition d)`, `bool Buy(string hero, PermanentUpgradeDefinition d)`, `long RefundAll(string hero, IReadOnlyList<PermanentUpgradeDefinition> defs)` (devolve o total e zera), `void SetActive(string hero, string id, bool active)`, `static long TotalSpent(PermanentUpgradeDefinition d, int level)`.
  - `PermanentGold`: `bool TrySpend(long amount)`; profile ganha `List<HeroUpgradeLevels>` (`heroKey`, `List<UpgradeLevel{ id; level; disabled }>`), salvo junto do gold; `GameEvents.PermanentUpgradesChanged(string hero)`.

- [ ] **Step 1: Escrever os testes** (`PermanentShopTests`, profile temporário como em `PermanentGoldTests`):
  - `Buy_spends_price_and_raises_level`: gold 400, def A (maxLevel 5): `Buy` → level 1, gold 350; `Buy` → level 2, gold 250; `NextPrice` == 300.
  - `Cannot_buy_without_gold_or_past_max`: gold 40 → `Buy` false, gold 40; def maxLevel 1 comprada → `Buy` false.
  - `RefundAll_returns_everything_even_if_disabled`: 2 níveis de A (150) + 1 de B (100), `SetActive(false)` na B → `EffectiveLevel` B == 0, `RefundAll` == 250, gold volta, níveis 0, ativas de novo.
  - `Levels_are_per_hero`: comprar no "Ranger" não muda o "Mage".
  - `Old_profile_with_only_gold_loads`: arquivo `{"gold": 7}` → gold 7, níveis 0.
  - `Corrupt_profile_loads_empty`: arquivo `"{lixo"` → gold 0, sem exceção.
- [ ] **Step 2:** `unity recompile` → falha de compilação (tipos não existem).
- [ ] **Step 3:** Implementar. `EffectiveLevel` = nível se ativa, senão 0. Toda mudança salva e dispara o evento.
- [ ] **Step 4:** Suíte EditMode verde.
- [ ] **Step 5: Commit** `feat(loja): regras e save das melhorias permanentes`

### Task 2: Assets das melhorias (18 definições + catálogo)

**Files:**
- Create: `Assets/Settings/PermanentUpgrades/*.asset` (via eval), `Assets/Resources/PermanentUpgradeCatalog.asset`
- Eval: `evals/p2_assets.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces: ids `perm_<stat_snake>` (ex.: `perm_damage`, `perm_multishot`); `buttonName` = nome do GameObject na cena (`Damage_Button`, `Life_Button`, `Defense_Button`, `Life_Regen_Button`, `Cooldown_Button`, `Ultimate_Generation_Button`, `Ultimate_Energy_Button`, `Move_Speed_Button`, `Magnet_Button`, `Xp_Button`, `Luck_Button`, `Gold_Button`, `Curse_Button`, `Revival_Button`, `Reroll_Button`, `Size_Button`, `Multishoot_Button`, `Area_Button`).

Valores (do spec): Damage 5×0.10 A · MaxHealth 3×0.10 A · Armor 5×0.02 A · Regen 3×0.001 A · MoveSpeed 3×0.05 A · Size 3×0.05 A · Multishot 5×1 A · FlameArea 3×0.10 A · Cooldown 3×0.05 B · UltimateCharge 3×0.10 B · UltimateCost 3×0.05 B · Magnet 3×0.20 B · Xp 5×0.05 B · Gold 5×0.10 B · Luck 4×0.10 B · Reroll 5×1 B · Curse 5×0.20 Maldição · Revival 1×1 Ressurreição.
Títulos: Damage, Max Health, Armor, Regeneration, Move Speed, Size, Multishot, Flame Area, Cooldown, Ultimate Charge, Ultimate Cost, Magnet, Growth, Greed, Luck, Reroll, Curse, Revival. Descrições em inglês escritas pelo executor, com os números da tabela do spec. Ícone = sprite do `Background_icon/Image` do botão de mesmo nome no `Stats_Painel` (lido do arquivo da cena pelo eval, ou pela API com a cena aberta só para leitura).
Catálogo: Barbarian = 15 comuns + Size; Ranger = 15 comuns + Multishot; Mage = 15 comuns + FlameArea.

- [ ] **Step 1:** Eval `p2_assets.cs` que FALHA hoje: carrega o catálogo e confere 3 heróis × 16 melhorias, preços do grupo certo, `prices[maxLevel-1]` certo (ex.: Curse 10000, Revival 10000, MaxHealth 300), ícone não nulo.
- [ ] **Step 2:** Eval gerador (cria os assets pela API do Unity).
- [ ] **Step 3:** `p2_assets.cs` PASS.
- [ ] **Step 4: Commit** `feat(loja): 18 melhorias permanentes e catálogo`

### Task 3: PermanentBonuses e os efeitos no herói

**Files:**
- Create: `Assets/Scripts/Core/Meta/PermanentBonuses.cs`
- Modify: `Assets/Scripts/Core/Cards/CardBonuses.cs` (`Get`), `Assets/Scripts/Player/Heroes/HeroController.cs` (armadura, regen, tamanho, energia da Ultimate), `Assets/Scripts/Player/HeroLevel.cs` (XP), `Assets/Scripts/Core/Save/PermanentGold.cs` (`AddFromRun`), `Assets/Scripts/World/XpGemPool.cs` (gold via `AddFromRun`), o ponto que inicia a run (monta os bônus)
- Test: `Assets/Tests/EditMode/PermanentBonusesTests.cs`, eval `evals/p3_effects.cs`

**Interfaces:**
- Consumes: Task 1 (`PermanentShop.EffectiveLevel`), Task 2 (catálogo).
- Produces: `static class PermanentBonuses` com campos somente-leitura de fora:
  `float DamageFactor, MaxHealthFactor, ArmorAdd, RegenPercentPerSecond, MoveSpeedFactor, CooldownFactor, UltimateChargeFactor, UltimateCostFactor, MagnetFactor, XpFactor, GoldFactor, SizeFactor, FlameAreaFactor; int ExtraArrows, StartRerolls, LuckLevel, CurseLevel; bool HasRevival;`
  `void Build(string heroKey)` (lê os níveis ativos), `void BuildFrom(IReadOnlyDictionary<PermanentStat,int> levels, IReadOnlyDictionary<PermanentStat,float> values)` (testável), `void Reset()` (tudo neutro: fatores 1, somas 0, `StartRerolls` 1).
  Fórmulas: Damage `1+0.10n`; MaxHealth `(1+0.10n)·(1+0.20c)`; MoveSpeed `(1+0.05n)·(1+0.20c)` (c = nível da Maldição); Armor `0.02n`; Regen `0.001n`; Cooldown `1−0.05n`; UltimateCharge `1+0.10n`; UltimateCost `1−0.05n`; Magnet `1+0.20n`; Xp `(1+0.05n)·(1+0.20c)`; Gold `(1+0.10n)·(1+0.20c)`; Size `1+0.05n`; FlameArea `1+0.10n`; ExtraArrows `n`; StartRerolls `1+n`.
  `PermanentGold.AddFromRun(int raw)`: aplica `GoldFactor` guardando a fração entre coletas.

- [ ] **Step 1:** Testes `PermanentBonusesTests` com `BuildFrom`: todos os níveis 0 = neutro; Damage 5 → 1.5; MaxHealth 3 + Curse 5 → 1.3·2.0 = 2.6; MoveSpeed 3 + Curse 5 → 1.15·2 = 2.3; Cooldown 3 → 0.85; Reroll 5 → 6; `Reset` volta ao neutro. Fração de gold: Gold 1 (×1.1), 10 coletas de 1 → +11 no total.
- [ ] **Step 2:** Compila → falha.
- [ ] **Step 3:** Implementar e ligar: `CardBonuses.Get(_, Damage)` × DamageFactor; `Get(None, MaxHealth)` × MaxHealthFactor; `Get(Primary|Shift, Cooldown)` × CooldownFactor; `Get(Ultimate, Cooldown)` × UltimateCostFactor (é a energia necessária); `Get(None, Magnet)` × MagnetFactor; `Get(None, MoveSpeed)` × MoveSpeedFactor; `Get(Primary, Area)` × FlameAreaFactor (só é ≠1 no Mage); `Sum(Primary, ProjectileCount)` + ExtraArrows (só ≠0 no Ranger); `HeroController.CardSizeFactor` × SizeFactor; redução de dano + ArmorAdd (abaixo do teto); regen + RegenPercentPerSecond × vida máxima; ganho de energia × UltimateChargeFactor; `HeroLevel.HandleXp` × XpFactor; coleta de gold → `AddFromRun`. Montar no início da run com o herói escolhido; `Reset` ao fim da run.
- [ ] **Step 4:** Eval `p3_effects.cs` (profile temporário, herói instanciado): com Damage 5 o `Direct` dá 1,5× o dano; com Armor 5 o dano recebido cai 10%; com Ranger Multishot 2 saem 2 flechas a mais; com Mage FlameArea 3 a escala da chama é 1,3×; com tudo 0, nada muda. Suíte verde.
- [ ] **Step 5: Commit** `feat(loja): efeitos das melhorias permanentes na run`

### Task 4: Ressurreição

**Files:**
- Modify: `Assets/Scripts/Player/Heroes/HeroController.cs` (`TakeDamage`, logo depois do Last Ember), `Assets/Scripts/Core/Run/CardRunState.cs` (`RevivalUsed`)
- Eval: `evals/p4_revival.cs`

**Interfaces:**
- Consumes: `PermanentBonuses.HasRevival`; `RunSettings.lastEmberInvulnerable` (2 s) reaproveitado.
- Produces: `CardRunState.RevivalUsed` (zera na run nova).

- [ ] **Step 1:** Eval que falha: herói com Revival, golpe fatal → vivo com 50% da vida, invulnerável 2 s, `RevivalUsed` true; 2º golpe fatal → morre. Com Last Ember pronto: o Ember age e a Ressurreição NÃO é gasta.
- [ ] **Step 2:** Implementar (se ainda morto depois do Ember e `HasRevival && !RevivalUsed`: vida = 50% da efetiva, invulnerável, `RevivalUsed = true`).
- [ ] **Step 3:** Eval PASS, suíte verde.
- [ ] **Step 4: Commit** `feat(loja): ressurreição (1 por run, depois do Last Ember)`

### Task 5: Maldição no mundo

**Files:**
- Modify: `Assets/Scripts/World/WorldPopulationManager.cs` (alvo da horda), `Assets/Scripts/Enemies/EnemyController.cs` (vida, velocidade e dano no `Awake`)
- Test: eval `evals/p5_curse.cs`

**Interfaces:**
- Consumes: `PermanentBonuses.CurseLevel`.
- Produces: `PermanentBonuses.HordeFactor` (`1+0.20c`) e `EnemyFactor` (`1+0.10c`).

- [ ] **Step 1:** Eval que falha: Curse 5 → `CurrentTarget` 2× o de antes (respeitando o limite de 300); inimigo instanciado com 1,5× vida máxima, vida, velocidade e dano de ataque; Curse 0 → igual a antes.
- [ ] **Step 2:** Implementar (o fator do inimigo entra antes do `SetRunMultipliers` guardar a base).
- [ ] **Step 3:** Eval PASS, suíte verde.
- [ ] **Step 4: Commit** `feat(loja): maldição (hordas e inimigos)`

### Task 6: Gold Bag, Stat Item e Reroll_Dice

**Files:**
- Create: `Assets/Scripts/Core/Run/LootDropRules.cs`, prefabs + animações (`Assets/Prefabs/Loot/GoldBag.prefab`, `StatItem.prefab`, `RerollDice.prefab`, clipes em `Assets/Animation/Loot/`, via eval como o XpGem)
- Modify: `Assets/Scripts/World/XpGemPool.cs` (tipos de loot), `Assets/Scripts/Enemies/EnemyController.cs` (`DropXpGems`), `Assets/Scripts/Core/Run/RunSettings.cs` (+ asset), `Assets/Scripts/Core/GameEvents.cs`
- Test: `Assets/Tests/EditMode/LootDropRulesTests.cs`, eval `evals/p6_loot.cs`

**Interfaces:**
- Consumes: `PermanentBonuses.LuckLevel`, `GoldFactor` (via `AddFromRun`).
- Produces:
  - `enum LootKind { Xp, Gold, StatItem, RerollDice }`; `XpGemPool.SpawnLoot(Vector2 pos, LootKind kind, int value)` (o `Spawn`/`SpawnGold` atuais passam por ele).
  - `LootDropRules`: `float StatItemChance(int luck)` = `0.10 + 0.10·luck`; `float DiceChance(bool bossOrMini, int luck)` = `(bossOrMini ? 0.05 : 0.01)·(1+0.25·luck)`.
  - RunSettings: `goldBagPrefab, statItemPrefab, rerollDicePrefab`, `goldBagValue = 25`, `statItemBaseChance = 0.10`, `diceChanceCommon = 0.01`, `diceChanceBoss = 0.05`.
  - Eventos: `GameEvents.StatItemCollected()`, `GameEvents.RerollDiceCollected()`.
  - Drop: boss e mini boss → Gold Bag sempre; boss → Stat Item com `StatItemChance`; todo monstro → Reroll_Dice com `DiceChance`.
  - Coleta: Gold Bag → `AddFromRun(25)`; Stat Item → `HeroCards.EnqueueOffer(OfferSource.StatItem)`; dado → +1 no estoque de rerolls (Task 8). Os 3 disparam o Golden Momentum.

- [ ] **Step 1:** Testes de `LootDropRules` (Luck 0/4: Stat Item 0.10/0.50; dado comum 0.01/0.02; dado boss 0.05/0.10). Compila → falha.
- [ ] **Step 2:** Implementar regras, tipos no pool, prefabs (Gold_Bag 11 quadros, Stat_Item 12, Reroll_Dice: os quadros do sprite), drop e coleta.
- [ ] **Step 3:** Eval `p6_loot.cs`: os 3 prefabs saem animados; Gold Bag soma 25 no profile temporário; Stat Item enfileira uma oferta StatItem (inclusive com uma oferta já aberta); dado soma 1 reroll; nenhum desses absorve XP no limite de gemas. Suíte verde.
- [ ] **Step 4: Commit** `feat(loot): gold bag, stat item e dado de reroll`

### Task 7: Oferta do Stat Item

**Files:**
- Modify: `Assets/Scripts/Core/Cards/CardRules.cs` (`OfferSource.StatItem`, `RollOffer`), `Assets/Scripts/Core/Run/RunSettings.cs`
- Test: `Assets/Tests/EditMode/CardRulesTests.cs` (casos novos)

**Interfaces:**
- Produces: `OfferSource.StatItem`; `OfferRules.statItemItemsChance01 = 0.40`, `statItemUpgradesChance01 = 0.40` (o resto, 0.20, é Stats).

- [ ] **Step 1:** Testes: com rng fixo, o tipo sorteado dá 3 cartas do mesmo tipo; em 10.000 sorteios ~40/40/20 (±3%); tipo sem 3 cartas possíveis completa com os outros tipos; respeita 6 itens / 6 atributos / tags.
- [ ] **Step 2:** Falha → implementar → passa. Suíte verde.
- [ ] **Step 3: Commit** `feat(cartas): oferta do stat item (40/40/20)`

### Task 8: Rerolls como estoque da run

**Files:**
- Modify: `Assets/Scripts/Player/HeroCards.cs` (estoque, reroll grátis), `Assets/Scripts/UI/CardScreenUI.cs` (`rerollQuantityText`), `Assets/Scenes/_TestScene.unity` (ligar `Reroll_Quantity` no campo, pela API), cartas `stat_treasure_sense` e `stat_merchants_whisper` (descrições)
- Create: `Assets/Scripts/Core/Cards/RerollRules.cs`
- Test: `Assets/Tests/EditMode/RerollRulesTests.cs`, eval `evals/p8_rerolls.cs`

**Interfaces:**
- Consumes: `PermanentBonuses.StartRerolls`; `GameEvents.RerollDiceCollected`.
- Produces:
  - `HeroCards.RerollStock` (int, começa em `StartRerolls` na run nova), `HeroCards.AddRerolls(int)`, `bool FreeRerollAvailable`; `RerollAvailable` = oferta aberta e (grátis ou estoque > 0).
  - `RerollRules.Spend(ref int stock, ref bool freeAvailable)`: gasta o grátis primeiro; `FreeFor(OfferSource source, bool treasureSense, bool merchantsWhisper)` = baú+TS ou vendedor+MW.
  - `GameEvents.RerollsChanged(int stock)`; o texto `Reroll_Quantity` mostra o estoque.
  - Descrições: Treasure Sense "1 more chest appears this run, and your first reroll at each chest is free"; Merchant's Whisper "The merchant shows up 1 more time this run, and your first reroll at each merchant is free".

- [ ] **Step 1:** Testes de `RerollRules` (grátis só no baú com TS e só no vendedor com MW; gasta o grátis antes do estoque; estoque nunca fica negativo). Falha.
- [ ] **Step 2:** Implementar; atualizar `t9`-like eval: estoque 1 no começo, +1 por dado, gasto atravessa telas, zera na run nova.
- [ ] **Step 3:** Eval PASS, suíte verde.
- [ ] **Step 4: Commit** `feat(cartas): rerolls como estoque da run`

### Task 9: Luck no baú e no vendedor

**Files:**
- Modify: `Assets/Scripts/World/WorldChestSpawnManager.cs`
- Test: eval `evals/p9_luck.cs`

**Interfaces:**
- Consumes: `PermanentBonuses.LuckLevel`.
- Produces: na montagem da agenda, com chance `0.10·luck` (cada um), +1 baú e +1 vendedor (mesma regra de horário do `AddExtraChest`/`AddExtraVendor`).

- [ ] **Step 1:** Eval que falha: Luck 4, 2.000 agendas com seed → ~40% com 1 baú extra e ~40% com 1 vendedor extra; Luck 0 → nenhum extra.
- [ ] **Step 2:** Implementar, eval PASS, suíte verde.
- [ ] **Step 3: Commit** `feat(loja): luck aumenta baús e vendedores`

### Task 10: Loja no menu

**Files:**
- Create: `Assets/Scripts/UI/PermanentShopUI.cs`
- Modify: `Assets/Scripts/UI/MainMenuNavigationUI.cs` (Select_Confirm, Stats_Painel, Esc), `Assets/Scenes/Main_Menu.unity` (pela API: textos do topo e do Sell_Everything, nº de pips por botão, nomes nos botões)
- Eval: `evals/p10_shop.cs`

**Interfaces:**
- Consumes: Tasks 1-2 (`PermanentShop`, catálogo), `PermanentGold`.
- Produces: `PermanentShopUI` no `Stats_Painel`: `void Open(string heroKey)`, `void Select(PermanentUpgradeDefinition d)`.

Comportamento (do spec):
- Card do herói → Select_Confirm (`Icon_Hero`, `Hero_Name`); Play_Game → Map_Painel (fluxo atual); Upgrade → fecha o Select_Confirm e abre o Stats_Painel. Esc: Select_Confirm aberto → fecha; Stats_Painel → volta à lista de heróis. `Back_To_Hero_Painel` = Esc.
- Abrir: só o grupo do herói ativo no Content; `Hero_Image` = sprite do SpriteRenderer do prefab do herói; Damage selecionado.
- Selecionado: fica no estado Selected do Animator do botão até clicar em outro (clicar em Buy/Toggle não tira a seleção).
- Background_Description: ícone, título, descrição, `Value` = só o número do próximo preço; Buy desabilitado sem gold. No máximo: `Value` e `Buy` somem, `Active` aparece (desligar = `SetActive(false)`).
- Pips: o botão tem exatamente `maxLevel` pips (sobra é removida pela API na cena); comprados usam o sprite "ligado" do Toggle Active, os outros mantêm o sprite atual.
- Textos: topo "Permanent Upgrades"; Sell_Everything "Refund All Upgrades" (vende direto); nomes dos botões = `title`.

- [ ] **Step 1:** Eval que falha (cena carregada aditivamente, profile temporário, restaurar depois): abrir Ranger → só o grupo Ranger ativo, Damage selecionado; comprar Damage com 100 gold → 1 pip aceso, gold 50; Buy desabilitado com 0 gold; Revival comprado → `Value`/`Buy` somem, `Active` aparece; Refund → gold de volta, pips apagados.
- [ ] **Step 2:** Implementar o componente e a navegação; ajustar a cena pela API (backup + `git diff`).
- [ ] **Step 3:** Eval PASS, suíte verde.
- [ ] **Step 4: Commit** `feat(loja): stats painel no menu`

### Task 11: Verificação final

- [ ] Todos os evals (p2–p10) e a suíte EditMode verdes.
- [ ] Revisão final do branch inteiro.
- [ ] Relatório ao usuário com o checklist de Play test (sempre pelo Main_Menu): comprar e vender no menu, efeitos na run (dano, vida, Multishot, área, Maldição), Gold Bag / Stat Item / dado caindo do boss, rerolls no `Reroll_Quantity`, Ressurreição.
