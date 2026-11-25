# Catálogo de cartas (30 itens + 30 atributos) — plano de implementação

> **Para agentes:** sub-skill obrigatória: superpowers:subagent-driven-development (recomendado) ou superpowers:executing-plans. Passos com checkbox (`- [ ]`).

**Objetivo:** substituir as cartas de item e atributo atuais pelas 60 do catálogo do usuário. As 56 sorteáveis
funcionam no Barbarian, no Ranger e no Mage. Entra também a carta Lasting Flames do Mage.

**Arquitetura:**
- Todo dano direto dos 3 heróis passa por um ponto único, `HeroDamage.Direct`. Ele calcula crítico e bônus
  ligados ao alvo, aplica o dano e dispara um evento com o resultado.
- Cada efeito de carta é um componente pequeno no `Systems` (padrão do `BlastCharmEffect`). Ele lê o próprio
  valor com `HeroController.Cards.Sum(CardAbility.None, CardStat.X)`.
- As regras puras (contas de dano, pilhas de DOT, contadores) ficam em classes estáticas no `Core`, com testes
  EditMode.
- O estado que muda durante a run (contadores, pilhas, buffs) fica no `CardRunState`, zerado em `OnDayStart`.

**Stack:** Unity 6000.3.8f1, C#, NUnit EditMode, evals no Editor.

**Spec:** `docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md`. As tabelas de números estão lá e
são a fonte. Este plano não repete os valores.

## Restrições globais

- **Escrita:** prosa e commits em pt-BR. Textos de carta em inglês.
- **Branch:** `catalogo-cartas`. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Push e merge:** sempre com AskUserQuestion antes.
- **Heróis:** só Barbarian, Ranger e Mage. Os outros 7 heróis não precisam funcionar com as cartas novas.
- **Enum `CardStat`:** valores novos só no **fim**. Os assets guardam o número.
- **Valores secundários** (raios, durações, recargas, limites): ficam em `RunSettings`, num cabeçalho
  "Catálogo de cartas", com `// 🔢`. Os valores iniciais são os da spec.
- **Estado da run:** tudo zera em `GameEvents.OnDayStart`, porque o domain reload está desligado.
- **Evals:** limpam tudo. Chamam `OnDisable` por reflexão, destroem os `CreateInstance`, zeram os estáticos
  (`RunSpecialization`, `HeroController.Cards`, `CardRunState`) e reabrem a cena do disco se mexeram nela.
- **Prefab e cena:** backup e diff antes e depois. Nada de linha em branco em YAML gerado. fileID nunca por max+1.
- **Ferramentas:**
  - `unity recompile`
  - `.superpowers/tools/edittests.sh`: linha de base de 311 testes, mais as 3 falhas conhecidas do `BagTests`
  - `.superpowers/tools/eval.sh "$(cat arq.cs)"`
- **Arquivos do usuário sem commit** (`Main_Menu.unity`): não entram em commit nenhum.

## Pontos de atenção para a revisão

1. **Dano contado duas vezes:** depois do ponto único, nenhum ataque pode aplicar dano pelo caminho antigo
   e pelo novo ao mesmo tempo. Sem carta nenhuma, o dano final de um acerto continua igual ao de hoje (o
   crítico base de 5% fica de fora dessa conta). A Task 3 testa cada ataque.
2. **Segunda run:** nenhum contador, pilha, buff, escudo temporário ou recarga do Last Ember pode continuar
   da run anterior. As Tasks 2, 5 e 6 testam isso disparando o começo de uma run.
3. **Chama infinita do Mage (Lasting Flames no nível 10):** ela para ao soltar o botão. Também para se o Mage
   morrer, for preso ou o jogo pausar.
   - Contadores e buffs por ciclo não podem disparar a cada tick.
   - A Task 10 testa isso.
4. **Last Ember:** dispara no máximo 1 vez a cada 5 minutos. Ele não pode deixar o herói no estado `IsDying`.
   Ao zerar a vida com a carta pronta, o herói nunca morre. A Task 6 testa isso.
5. **Pilhas de DOT:** a 4ª aplicação, com limite 3, substitui a pilha mais antiga. Cada pilha expira no seu
   próprio tempo. O Plague Herald passa só 1 aplicação. A Task 4 testa isso.

---

### Task 1: Assets das 60 cartas e troca do catálogo

**Arquivos:**
- Modificar:
  - `Assets/Scripts/Core/Cards/CardDefinition.cs`: valores novos no fim do `CardStat`.
  - `Assets/Resources/CardCatalog.asset`.
- Criar: 30 assets em `Assets/Settings/Cards/Items/` e 30 em `Assets/Settings/Cards/Stats/`, mais
  `Assets/Settings/Cards/Upgrades/Mage/mage_primary_duration.asset`.
- Apagar:
  - os 5 itens que saem: `item_iron_plate`, `item_power_gauntlet`, `item_magnet_ring`, `item_scholar_tome`,
    `item_blast_charm`;
  - os 6 `stat_*` atuais.
- Renomear o arquivo e o id, mantendo o GUID:
  - `item_troll_heart` → `item_everliving_heart`;
  - `item_swift_boots` → `item_stormstep_boots`;
  - `item_thorn_mail` → `item_thornwake_armor`.
- Eval: `t1_catalog.cs`.

**Interfaces que esta task produz:**
- `CardStat` novos, nesta ordem, depois de `PetCooldown`:
  - Itens: `BossHealthPercentDamage`, `EchoFang`, `ExecuteBonus`, `Lifesteal`, `CritDamage`, `PetBond`,
    `CritSecondImpact`, `ShiftHeal`, `DotStacks`, `DotDuration`, `DotSpread`, `DotTargetBonus`, `DotCrit`,
    `DotExplosion`, `ThornSpikes`, `HeroSize`, `HealEffectiveness`, `CheatDeath`, `CrowdArmor`,
    `ProjectileHoming`, `ProjectileSplit`, `ProjectileSize`, `ProjectileSpeed`, `KillHeal`, `KillEnergy`,
    `GoldGain`, `CritChance`.
  - Atributos (cada um é 1 flag com valor 1): `TitansTribute`, `TrophyHunter`, `EndlessHunger`,
    `GoldenMomentum`, `SurvivorsLegacy`, `DangerousBargain`, `RelentlessMomentum`, `PredatorsFocus`,
    `CriticalFever`, `CrowdBreaker`, `BloodFrenzy`, `DeathsEcho`, `Overkill`, `FinalStand`, `ScarsOfBattle`,
    `Unbroken`, `AdrenalineRush`, `ShieldBattery`, `WarriorsFeast`, `MovingFortress`, `RicochetFate`,
    `AfflictionHarvest`, `ElementalSupremacy`, `RelentlessCasting`, `SpiritLink`, `FinalJudgment`,
    `CollectorsInstinct`, `MerchantsWhisper`, `GreedboundSoul`, `TreasureSense`, `FlameDuration`.
- Itens com efeito que já existe usam o `CardStat` antigo:
  - Everliving Heart → `Regen`;
  - Stormstep Boots → `MoveSpeed`;
  - Ironroot Core → nenhum efeito, `healthPerLevel = 0.20`.
- Ids: `item_<nome_em_snake>` e `stat_<nome_em_snake>`, por exemplo `item_sovereigns_bane` e
  `stat_titans_tribute`.
- `valuePerLevel` com os valores da spec, em fração: 1% vira 0,01; +25% de dano crítico vira 0,25.
- Fora do catálogo (o asset existe, para o ícone): `item_gilded_hand`, `item_menders_seal`,
  `stat_greedbound_soul`, `stat_shield_battery`.
- Tags (`requiredTags`) conforme a coluna "Tag" da spec.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t1_catalog.cs` verifica:
  - O catálogo tem 26 itens e 28 atributos sorteáveis, mais os upgrades.
  - Não tem nenhum dos 11 ids que saem.
  - Não tem os 4 ids que ficam de fora.
  - Todos os 60 assets novos existem, com `kind`, `maxLevel` (5 para item, 1 para atributo) e tag certos.
  - `item_everliving_heart` mantém o GUID antigo do `item_troll_heart`.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:**
  - acrescentar os `CardStat`;
  - criar, renomear e apagar os assets por script no Editor, com `AssetDatabase.MoveAsset` e `DeleteAsset`;
  - montar o catálogo;
  - escrever títulos e descrições em inglês, curtos, com `{0}`.
- [ ] **Passo 4:**
  - remover o código que lia os stats removidos: `BlastCharmEffect` e o espinho antigo (`CardStat.Thorns` em
    `HeroController.HitByMelee`). O valor `Thorns` continua no enum, sem uso, para não renumerar.
  - `unity recompile`, depois o eval, depois `edittests.sh`.
  - Esperado: o eval dá `PASS`, e a suíte fica na linha de base, ajustando os testes que usavam as cartas
    removidas.
- [ ] **Passo 5:** commit `feat(catalogo): 60 cartas novas e troca do catalogo`.

### Task 2: Crítico geral, estado da run e regras de dano (puro)

**Arquivos:**
- Criar:
  - `Assets/Scripts/Core/Cards/CardRunState.cs`;
  - `Assets/Scripts/Core/Cards/HeroDamageRules.cs`;
  - `Assets/Tests/EditMode/HeroDamageRulesTests.cs`.
- Modificar: `Assets/Scripts/Core/Run/RunSettings.cs` e `Assets/Resources/RunSettings.asset` (cabeçalho
  "Catálogo de cartas").

**Interfaces que esta task produz:**
- `HeroDamageRules.CritChance(float cardCritChance, float baseChance) => baseChance + cardCritChance`.
  - O `RunSettings.baseCritChancePercent` é `5`.
- `HeroDamageRules.CritMultiplier(float cardCritDamage) => 2f + cardCritDamage`.
- `HeroDamageRules.TargetMultiplier(in TargetInfo t, in DamageCards c) -> float`. Junta os bônus ligados ao alvo:
  - Executioner (alvo abaixo de 30% da vida);
  - Withering Eye (alvo com DOT, qualquer ataque);
  - Elemental Supremacy (alvo com DOT, só no primário);
  - Trophy Hunter (alvo chefe);
  - Final Judgment (Ultimate contra chefe ou mini chefe);
  - Predator's Focus (pilhas no alvo).
  - `TargetInfo { float health01; bool hasDot; bool isBoss; bool isMiniBoss; CardAbility ability; int focusStacks; }`.
- `HeroDamageRules.SelfMultiplier(in SelfInfo s, in DamageCards c) -> float`. Junta os bônus ligados ao herói:
  Endless Hunger, Final Stand, Critical Fever, Blood Frenzy, e os buffs de "próximo primário" (Relentless
  Momentum e Crowd Breaker).
- `HeroDamageRules.SovereignBonus(float targetMaxHealth, float percent, float hitWeight) -> float`.
- `CardRunState` (estático): contadores e buffs da run, com `Reset()`.
  - Abates totais e abates desde o último dano recebido.
  - Chefes mortos.
  - Pilhas e tempo do Critical Fever.
  - Buffs de "próximo primário" pendentes.
  - Último alvo e pilhas do Predator's Focus.
  - Ticks de DOT contados.
  - Tempo da última vez que o Last Ember disparou.
  - Tempo andando sem parar.
  - Pilhas do Scars of Battle.
- Assinado em `GameEvents.OnDayStart` por um componente `CardRunStateReset` no `Systems`, que chama `Reset()`.
  - O componente é adicionado pelo `SystemsBootstrap`, como o `BlastCharmEffect` era.

**Passos:**
- [ ] **Passo 1: escrever os testes.** Em `HeroDamageRulesTests`:
  - `Base_crit_is_5_percent_plus_cards`
  - `Crit_multiplier_is_2_plus_razor_crown`: com 0,5, o multiplicador é 2,5.
  - `Executioner_only_below_30_percent`: 0,29 ganha o bônus, 0,31 não.
  - `Elemental_supremacy_only_primary_with_dot`
  - `Final_judgment_only_ultimate_vs_boss_or_miniboss`
  - `Predator_focus_caps_at_50_percent`
  - `Sovereign_scales_with_hit_weight`: 1% de uma vida de 1000 dá 10; com peso 0,25, dá 2,5.
  - `Self_bonuses_multiply`
  - `Run_state_reset_clears_everything`
- [ ] **Passo 2:** rodar `edittests.sh`. Esperado: não compila.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar `edittests.sh`. Esperado: tudo passa, menos as 3 falhas do `BagTests`.
- [ ] **Passo 5:** commit `feat(catalogo): regras de dano, critico base e estado da run`.

### Task 3: Ponto único de acerto e os 3 heróis passando por ele

**Arquivos:**
- Criar: `Assets/Scripts/Player/HeroDamage.cs`.
- Modificar:
  - `Barbarian.cs`: golpe do primário, Thunder Blades (via `HeroProjectile`) e a área de pouso da Ultimate.
  - `RangerArrow.cs` e `RangerKnife.cs`: só o acerto em voo.
  - `MageAttackHitbox.cs` e `Mage.cs`: tick da chama.
  - `MageFireball.cs`: só o impacto.
  - `HeroProjectile.cs`.
  - `HeroController.cs`: `RollCritMultiplier` e os statics de crítico deixam de ser usados por esses ataques.
- Eval: `t3_pipeline.cs`.

**Interfaces que esta task produz:**
- `HeroDamage.Direct(EnemyController target, float baseDamage, CardAbility ability, float hitWeight = 1f, bool canCrit = true) -> HeroHit`
  - `HeroHit { float damage; bool crit; bool killed; float overkill; CardAbility ability; float hitWeight; EnemyController target; }`.
  - Faz, em ordem:
    1. `baseDamage` × `SelfMultiplier` × `TargetMultiplier`;
    2. + `SovereignBonus` (só no primário);
    3. sorteia o crítico;
    4. `target.TakeDamage(final)`;
    5. calcula `killed` e `overkill`;
    6. dispara `HeroDamage.OnDirectHit(HeroHit)`.
- `public static event System.Action<HeroHit> OnDirectHit`.
- `baseDamage` é o dano que cada ataque já calcula hoje, **sem** o crítico antigo.
- **Mage:**
  - Tick da chama: `hitWeight = fireTickInterval / FlameCycleSeconds`.
  - Dano do tick: `stats.damage × cartas × fireTickInterval / FlameCycleSeconds`. Cada ciclo dá o dano cheio.
  - `FlameCycleSeconds` é a duração do clipe `attack_continuous`, dividida pela velocidade de ação. É lida do
    Animator no `Awake`, com 1 s se não achar o clipe.
- **Flechas do Ranger:** a reserva continua igual. O que conta como dano é `damageDealt`. Overkill sempre 0.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t3_pipeline.cs`. Para cada ataque, assina `OnDirectHit` e
  confere:
  - (a) Barbarian primário: 1 evento por inimigo atingido, `ability == Primary`. O dano é o mesmo de antes
    sem crítico, com `RunSettings.baseCritChancePercent = 0` no eval.
  - (b) Thunder Blade: 1 evento, `ability == Ultimate`.
  - (c) Flecha do Ranger: 1 evento, `Primary`, `overkill == 0`.
  - (d) Faca do Ranger: 1 evento, `Ultimate`.
  - (e) Tick da chama do Mage: 1 evento, com `hitWeight == fireTickInterval / FlameCycleSeconds`. A soma dos
    ticks de 1 ciclo dá `stats.damage` (±1%).
  - (f) Bola de fogo: 1 evento, `Ultimate`.
  - (g) Nenhum desses ataques tira vida do inimigo duas vezes: a vida perdida é igual à soma dos `damage`.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar o `HeroDamage` e trocar os `enemy.TakeDamage(...)` diretos desses ataques por
  `HeroDamage.Direct`. Ticks no chão (facas fincadas, poça do fogo) e DOTs continuam com `TakeDamage(...,
  isDirectPlayerHit: false)`.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): ponto unico de acerto do heroi (Barbarian, Ranger, Mage)`.

### Task 4: Pilhas de DOT, morte por DOT e as cartas de DOT

**Arquivos:**
- Modificar:
  - `Assets/Scripts/Core/StatusEffectController.cs`;
  - `Assets/Scripts/Enemies/EnemyController.cs`: `LastHitWasDot`;
  - `Assets/Scripts/Core/GameEvents.cs`: `OnDotTick(EnemyController, StatusEffectType, float damage)`, mais o
    `killedByDot` no fim de `OnEnemyKilledDetailed`.
- Criar:
  - `Assets/Scripts/Core/Cards/DotStackRules.cs`;
  - `Assets/Scripts/Systems/Cards/DotCardEffects.cs`: Plague Herald, Affliction Heart, Death's Echo e
    Affliction Harvest;
  - `Assets/Tests/EditMode/DotStackRulesTests.cs`.
- Eval: `t4_dot.cs`.

**Regras:**
- `ApplyStatusEffect(type, duration, dps)` cria uma pilha nova enquanto houver espaço, até o limite.
  - Limite: `DotStackRules.MaxStacks(blightLevel)`. Nível 0 = 1, nível 1 = 2, nível 2 ou mais = 3.
  - Cheio: substitui a pilha mais antiga.
  - A duração é multiplicada por `1 + Sum(DotDuration)`.
  - O dps é multiplicado por `DotStackRules.DotDamageMultiplier(blightLevel)`: +10% por nível a partir do 3.
  - A **mesma** chamada de hoje continua funcionando para todo mundo.
- **Cada pilha dá tick sozinha.**
  - Crítico do tick: chance `Sum(DotCrit)`, multiplicador `HeroDamageRules.CritMultiplier`.
  - Cada tick dispara `OnDotTick` e marca `LastHitWasDot = true`.
  - Dano direto marca `LastHitWasDot = false`.
- **`HasAnyDot`:** propriedade pública, usada pelo `TargetInfo.hasDot`.
- **Plague Herald:** passa 1 aplicação nova (dps e duração cheia) da pilha mais forte de cada tipo.

**Passos:**
- [ ] **Passo 1: escrever os testes que falham.**
  - `DotStackRulesTests`:
    - `Max_stacks_by_blight_level`
    - `Fourth_application_replaces_oldest_at_cap_3`: modelo puro de lista de pilhas em `DotStackRules`.
    - `Each_stack_expires_on_its_own`
    - `Dot_damage_bonus_from_level_3`
  - `t4_dot.cs`, com inimigo real:
    - (a) 3 aplicações com Blight Crucible no nível 2 dão 3 pilhas.
    - (b) Morte por tick deixa `killedByDot` verdadeiro no evento.
    - (c) Plague Herald com chance 100%: ao morrer, 1 vizinho ganha exatamente 1 pilha.
    - (d) Affliction Heart: a explosão sai no 8º tick.
    - (e) Death's Echo explode só quando a morte é por DOT.
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval, `edittests.sh` e os evals da etapa 4 que usam DOT (sangramento do Bloodlust,
  fogo do Immolation). Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): pilhas de DOT e cartas de DOT`.

### Task 5: Cartas de acerto e de abate

**Arquivos:**
- Criar: `Assets/Scripts/Systems/Cards/HitCardEffects.cs`, que assina `HeroDamage.OnDirectHit` e
  `GameEvents.OnEnemyKilledDetailed`.
- Modificar: `HeroController.cs`: `Heal`, ganho de energia por abate e `IsBoss` do inimigo.
- Eval: `t5_hits.cs`.

**Cartas:** Crimson Pact, Echo Fang, Shattered Fortune, Overkill, Relentless Casting, Critical Fever,
Crowd Breaker, Predator's Focus (atualiza as pilhas), Reaper's Feast, Soul Furnace, Endless Hunger,
Titan's Tribute (só os contadores; o tamanho é da Task 6), Trophy Hunter, Survivor's Legacy, Blood Frenzy,
Warrior's Feast e Unbroken.

**Regras:**
- Os efeitos "por acerto" usam `hit.hitWeight`. Os contadores por acerto do Echo Fang contam só eventos com
  `hitWeight == 1`. No Mage, contam 1 vez por ciclo, na virada do ciclo que a Task 10 dispara.
- Echo Fang, Shattered Fortune e Overkill causam dano por `HeroDamage.Direct(..., canCrit:false)`, com uma
  guarda de reentrada (`CardRunState.inEcho`) para nunca gerar outro efeito em cadeia.
- O escudo temporário usa `HeroController.AddShield(amount, duration)`, um overload novo. O escudo some
  quando a duração acaba.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t5_hits.cs` verifica:
  - (a) Crimson Pact nível 2: cura 2% do dano.
  - (b) Echo Fang: o 4º acerto gera mais um evento com 20% do dano, e esse evento não gera eco de novo.
  - (c) Overkill: um acerto que mata com 50 de sobra dá 25 no vizinho, e o vizinho não encadeia.
  - (d) Reaper's Feast respeita o limite de 5% por segundo.
  - (e) Trophy Hunter: matar 1 chefe dá +5% contra chefes.
  - (f) Unbroken: 15 abates sem dano dão o escudo, e levar dano zera a contagem.
  - (g) `GameEvents.DayStarted()` zera todos os contadores (ponto de atenção 2).
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): cartas de acerto e de abate`.

### Task 6: Cartas de defesa, movimento e estado do herói

**Arquivos:**
- Criar: `Assets/Scripts/Systems/Cards/HeroStateCardEffects.cs`.
- Modificar: `HeroController.cs`. São 4 ganchos:
  - redução de dano extra: Gravebound, Final Stand, Scars of Battle, Moving Fortress e Dangerous Bargain
    negativo;
  - escala geral do herói: `HeroSizeMultiplier = (1 + Sum(HeroSize)) × Titan`;
  - cheat death no caminho da morte;
  - multiplicador de velocidade temporária: Golden Momentum, Adrenaline Rush, Critical Fever e Blood Frenzy.
- Eval: `t6_state.cs`.

**Cartas:** Thornwake Armor (espinhos com o `HeroProjectile` em escala 0,5, prefab da Thunder Blade, recarga de
0,5 s), Colossus Heart (tamanho + área geral via `Cards.Get(None, Area)`), Gravebound Plate, Last Ember,
Wellspring Chalice, Spirit Link, Final Stand, Scars of Battle, Moving Fortress, Adrenaline Rush, Golden Momentum
(precisa de um evento novo `GameEvents.OnGemCollected`, disparado pelo `XpGemPool`), Relentless Momentum
(marca o buff de "próximo primário" depois de 2 s andando), Dangerous Bargain (+30% de XP no `HeroLevel` e
+15% de dano recebido) e o tamanho do Titan's Tribute.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t6_state.cs` verifica:
  - (a) Last Ember: dano letal deixa o herói vivo com 10% × nível da vida, invulnerável por 2 s. Um 2º dano
    letal antes de 5 min mata. Nenhum caso deixa `IsDying` verdadeiro enquanto o herói está vivo.
  - (b) Gravebound com 4 inimigos perto, no nível 1: 4% a mais de redução, respeitando o teto de 75%.
  - (c) Colossus nível 2: a escala do herói fica ×1,16.
  - (d) Thornwake: um dano direto recebido solta 2 + nível espinhos. Dois danos em menos de 0,5 s soltam só
    uma vez.
  - (e) Dangerous Bargain: XP ×1,3 e dano recebido ×1,15.
  - (f) `DayStarted` zera a recarga do Last Ember, as pilhas do Scars e o tempo andando.
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): cartas de defesa, movimento e estado do heroi`.

### Task 7: Projéteis do Ranger

**Arquivos:**
- Modificar: `Assets/Scripts/Player/RangerArrow.cs`, `Assets/Scripts/Player/RangerKnife.cs` e o disparo em
  `Ranger.cs`.
- Eval: `t7_projectiles.cs`.

**Cartas:**
- **Gravity Core:** escala do projétil × (1 + Sum).
- **Windpiercer:** velocidade e alcance × (1 + Sum).
- **Predator's Eye:** procura o inimigo mais perto num raio de 3 + nível e vira para ele numa curva suave. Ao
  matar com reserva sobrando, procura outro alvo.
- **Shattered Prism:** ao acertar, chance de 2 estilhaços de 40% para os lados, como flechas menores.
  - Os estilhaços não geram estilhaço.
- **Ricochet Fate:** depois do acerto, 1 salto até 4 de distância, com 60% do dano.
  - Os ricochetes não geram ricochete.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t7_projectiles.cs` verifica:
  - (a) Gravity nível 2: escala ×1,3.
  - (b) Windpiercer nível 1: velocidade e alcance ×1,1.
  - (c) Predator's Eye: uma flecha disparada a 90° do inimigo vira para ele em até 1 s de simulação
    (`Update` chamado à mão).
  - (d) Shattered Prism com 100%: 2 estilhaços, e os estilhaços não geram outros.
  - (e) Ricochet: 1 salto só.
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): cartas de projetil do Ranger`.

### Task 8: Primal Bond (pets)

**Arquivos:**
- Modificar:
  - `PetController.cs`: `ApplyMultipliers` ganha o dano.
  - `Mage.cs` e `Ranger.cs`: as propriedades `PetSize`, `PetActionSpeed` e `PetCooldown` somam o Primal Bond,
    e o dano do pet também.
- Eval: `t8_pet.cs`.

**Regra:** Primal Bond por nível dá +10% de dano, +5% de velocidade de ataque e +5% de tamanho. Soma com as
cartas de pet da etapa 4. Vale para o pet vivo na hora, pelo `OnCardsChanged`.

- [ ] **Passo 1: escrever o eval que falha.** Primal Bond nível 2 no urso: dano ×1,2, tamanho ×1,1 e velocidade
  ×1,1.
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): Primal Bond`.

### Task 9: Gemas, baú e vendedor

**Arquivos:**
- Modificar:
  - `XpGemPool.cs`: com Collector's Instinct, `MagnetRadius` vira infinito.
  - `WorldChestSpawnManager.cs`: `AddExtraChest()` e `AddExtraVendor()` acrescentam 1 horário entre agora e o
    fim da run, respeitando o tempo de silêncio.
  - `HeroCards.cs`: rerolls por oferta = 1 + Treasure Sense (baú) ou + Merchant's Whisper (vendedor).
  - `CardScreenUI`: o botão de reroll fica ativo enquanto houver reroll.
- Eval: `t9_economy.cs`.

**Regra:**
- Pegar Treasure Sense ou Merchant's Whisper chama `AddExtra...` uma vez, pelo `GameEvents.OnCardGranted`.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.**
  - (a) Collector's Instinct: uma gema a 50 de distância é atraída.
  - (b) Treasure Sense: `ChestTimes.Count` aumenta 1, e o baú tem 2 rerolls.
  - (c) Merchant's Whisper: o mesmo, para o vendedor.
  - (d) Level up continua com 1 reroll.
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(catalogo): gemas, bau e vendedor`.

### Task 10: Lasting Flames e o ciclo da chama

**Arquivos:**
- Modificar:
  - `Mage.cs`;
  - `Assets/Scripts/Core/Heroes/PrimaryDuration.cs`;
  - `Assets/Tests/EditMode/` (teste de `PrimaryDuration`, se já existir; senão, criar).
- Eval: `t10_flames.cs`.

**Regras:**
- **Duração extra:** `PrimaryDuration.ExtraSeconds(level, cycleSeconds)`.
  - Níveis 1 a 9: `level × 0,10 × cycleSeconds`.
  - Nível 10: infinito.
  - `PrimaryDuration.ShouldEnd(elapsed, extra, infinite, attackHeld)` → com infinito, termina só quando
    `!attackHeld`.
- **Corte:** morte, prisão ou pausa encerram a chama, como hoje (`StopFireIfActionCut`). A rede de segurança
  `maxActionDuration` ignora o limite quando a chama é infinita.
- **Ciclo:** a cada volta do `attack_continuous`, o Mage dispara `HeroDamage.OnFlameCycle`.
  - O Echo Fang e o Predator's Focus contam 1 por ciclo.
  - O Crowd Breaker olha quantos inimigos foram atingidos no ciclo.
  - Os buffs de "próximo primário" valem para o ciclo inteiro e são gastos no fim dele.

**Passos:**
- [ ] **Passo 1: escrever os testes que falham.**
  - EditMode:
    - `Extra_seconds_by_level`: nível 5 e ciclo de 1 s dão 0,5.
    - `Level_10_is_infinite_while_held`
    - `Infinite_ends_on_release`
  - Eval `t10_flames.cs`:
    - (a) No nível 10 com o botão segurado, a chama passa de 3 ciclos sem apagar. Ao soltar, apaga no próximo
      check.
    - (b) `OnFlameCycle` dispara 1 vez por volta, não por tick.
    - (c) Prender o Mage apaga a chama infinita.
- [ ] **Passo 2:** rodar. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e `edittests.sh`. Esperado: `PASS` e a linha de base.
- [ ] **Passo 5:** commit `feat(mage): Lasting Flames e ciclo da chama`.

## Fim

- Revisão final da branch inteira por um revisor novo.
- O usuário testa no Play, começando pelo Main_Menu.
- Perguntar antes de qualquer merge ou push.
