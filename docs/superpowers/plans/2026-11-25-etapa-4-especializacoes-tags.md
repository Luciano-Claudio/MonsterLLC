# Etapa 4: especializações, tags e oferta 60% upgrade (plano de implementação)

> **Para agentes:** sub-skill obrigatória: use superpowers:subagent-driven-development (recomendado) ou
> superpowers:executing-plans para implementar este plano tarefa por tarefa. Os passos usam checkbox (`- [ ]`).

**Objetivo:** antes da run começar, o jogador escolhe 1 de 2 especializações do herói (Barbarian, Ranger,
Mage). A escolha muda como o herói joga e filtra as cartas por tag. O level up comum passa a ter 60% de
chance de upgrade por carta, com no máximo 2 itens por oferta.

**Arquitetura:**
- Uma especialização é um ScriptableObject `SpecializationDefinition` (id, heroId, slot, nome, descrição,
  ícone, tags `[Flags] SpecTag`), listado num `SpecializationCatalog` em Resources.
- A escolha da run fica num estático `RunSpecialization`, zerado em todo `OnDayStart` (começo da run).
- `CardRules` filtra pelas tags exigidas da carta (`requiredTags`, regra "basta uma").
- Cada herói implementa `ApplySpecialization`. Esse método liga e desliga o comportamento, e é aplicado
  depois do recálculo de perks para ganhar deles.

**Stack:** Unity 6000.3.8f1, C#, Input System, TextMeshPro, NUnit (EditMode).

**Spec:** `docs/superpowers/specs/2026-11-24-etapa-4-especializacoes-tags-design.md`

## Restrições globais

- **Língua:** toda prosa para o usuário, comentários e commits em pt-BR. Texto do jogo (títulos e
  descrições de cartas e especializações) em inglês.
- **Branch:** `etapa-4-especializacoes`, criada a partir de `etapa-3-bau-vendedor-itens`. A etapa 3 ainda
  não foi mergeada na main.
- **Git:** perguntar com AskUserQuestion antes de **todo** push. Nunca fazer merge sem perguntar.
- **Commits:** terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Cena e prefab:**
  - Antes de salvar, fazer `git diff` e um backup. Depois, conferir o diff.
  - A `_TestScene.unity` usa LF.
  - fileIDs nunca no esquema max+1.
  - Antes de `LoadPrefabContents`/`SaveAsPrefabAsset`, rodar `ImportAsset ForceUpdate`.
- **Evals:**
  - Limpam tudo: `OnDisable` via reflexão antes de `DestroyImmediate`, destruir todo `CreateInstance`,
    zerar estáticos (`RunSpecialization.Reset()`, `HeroController.Cards.Reset()`, `IsDying=false`).
  - Reabrir a `_TestScene` do disco quando mexerem em objetos da cena.
  - O domain reload está DESLIGADO.
- **Clipes de animação:** gerados sem linhas em branco no YAML. Preferir a API `AnimationUtility` e
  `AnimatorController` a escrever YAML na mão. Conferir os clipes no Animator antes de dizer que estão
  prontos.
- **Precedente primeiro:** o pet urso copia a estrutura da Fênix: `Assets/Prefabs/Heros/Mage/Phoenix.prefab`,
  `Assets/Animation/Heros/Mage/Passiva/Phoenix/Phoenix.controller` e os `summonpet_*.anim` do Mage.
- **Ferramentas:**

  | Para quê | Comando |
  |---|---|
  | Compilar | `unity recompile` |
  | Suíte EditMode | `.superpowers/tools/edittests.sh` |
  | Eval no Editor | `.superpowers/tools/eval.sh "$(cat arquivo.cs)"` (passar o código, não o caminho) |

  Os evals ficam em `.superpowers/sdd/2026-11-25-etapa-4-especializacoes-tags/evals/`.
- **Linha de base da suíte:** tudo passa, menos as 3 falhas conhecidas do `BagTests`.
- **Arquivos do usuário:** o usuário mudou a `_TestScene` (tela `Specialization_Choose`), o `Icons.png` e
  os sprites em `Assets/Sprites/Heros/Ranger/Passiva/`. Eles só entram em commit com a autorização dada
  na revisão deste plano.

## Pontos para revisar

1. **Play direto na `_TestScene`**, sem passar pelo Main_Menu (`MainMenuUI.CurrentRun == null`): a tela de
   escolha precisa aparecer do mesmo jeito para Barbarian, Ranger e Mage. O jogo nunca pode ficar preso
   pausado. A Task 4 testa isso.
2. **Segunda run depois de morrer** (domain reload desligado): a especialização da run anterior não pode
   continuar valendo. A tela aparece de novo e as tags voltam a `None` até a escolha. A Task 1
   (EditMode) e a Task 4 (eval) testam isso.
3. **Esc ou menu de pausa com a tela aberta:** fechar o menu de pausa não pode despausar a run. As teclas
   1 e 2 são ignoradas com `PauseMenuUI.IsOpen`. A Task 4 testa isso.
4. **Herói recriado no meio da run** (`RespawnHeroFresh`): a nova instância recebe a mesma
   especialização no `Start`. A Task 4 testa isso.
5. **Carta com tag que a especialização não tem:** nunca pode sair, em nenhuma fonte (level up, baú,
   vendedor). Por exemplo, Earthquake para Bloodlust. A Task 2 testa isso com 1000 sorteios por fonte.

---

### Task 1: Modelo de dados das especializações

**Arquivos:**
- Criar:
  - `Assets/Scripts/Core/Specializations/SpecTag.cs`
  - `Assets/Scripts/Core/Specializations/SpecializationDefinition.cs`
  - `Assets/Scripts/Core/Specializations/SpecializationCatalog.cs`
  - `Assets/Scripts/Core/Specializations/RunSpecialization.cs`
- Modificar: `Assets/Scripts/Core/Cards/CardDefinition.cs` (campo novo `requiredTags`, no fim)
- Testes: `Assets/Tests/EditMode/RunSpecializationTests.cs`

**Interfaces que esta task produz:**
- `[System.Flags] public enum SpecTag { None = 0, Dot = 1, Projectile = 2, Hitscan = 4, Pet = 8, Tank = 16, Shield = 32, Heal = 64, Size = 128, Earthquake = 256 }`
- `SpecializationDefinition : ScriptableObject`:
  - campos: `string id`, `string heroId` (igual a `GetType().Name` do herói: "Barbarian", "Ranger",
    "Mage"), `int slot` (0 = botão da esquerda, 1 = da direita), `string displayName`,
    `[TextArea] string description`, `Sprite icon`, `SpecTag tags`;
  - `[CreateAssetMenu(menuName = "MonsterLLC/Specialization")]`.
- `SpecializationCatalog : ScriptableObject`:
  - `List<SpecializationDefinition> specializations`;
  - `static SpecializationCatalog Load()` faz `Resources.Load<SpecializationCatalog>("SpecializationCatalog")`;
  - `void ForHero(string heroId, List<SpecializationDefinition> result)` devolve as do herói, ordenadas
    por `slot`.
- `public static class RunSpecialization`:
  - `SpecializationDefinition Current { get; }`
  - `SpecTag Tags` (`Current?.tags ?? SpecTag.None`)
  - `bool IsChosen`
  - `void Choose(SpecializationDefinition spec)`
  - `void Reset()`
  - `event System.Action<SpecializationDefinition> Chosen`
  - `bool Is(string specId)`
- `CardDefinition.requiredTags` (`SpecTag`, padrão `None`, que quer dizer "aparece para todos").

- [ ] **Passo 1: escrever os testes que falham.** Em `RunSpecializationTests`:
  - `Tags_are_none_before_choosing`: depois de `Reset()`, `Tags == SpecTag.None`, `IsChosen == false`,
    `Current == null`.
  - `Choose_sets_tags_and_fires_event`: um spec com `Dot | Tank`; depois de `Choose`,
    `Tags == (Dot | Tank)`, `Is(spec.id)` é verdadeiro e o evento foi recebido uma vez.
  - `Reset_clears_choice` (ponto de revisão 2): `Choose`, depois `Reset`, e `Tags == None`.
  - `ForHero_returns_only_that_hero_sorted_by_slot`: um catálogo com Ranger slot 1, Barbarian slot 0
    e Ranger slot 0; `ForHero("Ranger")` devolve `[slot 0, slot 1]`.
  - O `TearDown` chama `RunSpecialization.Reset()` e destrói todo `CreateInstance`.
- [ ] **Passo 2:** rodar `.superpowers/tools/edittests.sh`. Esperado: erro de compilação (tipos não existem).
- [ ] **Passo 3:** criar os 4 arquivos e o campo `requiredTags` (no fim de `CardDefinition`, depois de
  `heroId`).
- [ ] **Passo 4:** rodar `.superpowers/tools/edittests.sh`. Esperado: os 4 testes novos passam; a linha de
  base fica igual.
- [ ] **Passo 5:** commit `feat(spec): modelo de especializacao, tags e escolha da run`.

### Task 2: Filtro de tags e oferta 60% upgrade

**Arquivos:**
- Modificar:
  - `Assets/Scripts/Core/Cards/CardRules.cs`
  - `Assets/Scripts/Core/Run/RunSettings.cs` e `Assets/Resources/RunSettings.asset`
  - `Assets/Scripts/Player/HeroCards.cs`
- Testes: `Assets/Tests/EditMode/CardRulesTests.cs` (ajustar as chamadas e acrescentar testes)

**Interfaces:**
- Consome: `SpecTag`, `CardDefinition.requiredTags`, `RunSpecialization.Tags`.
- Produz:
  - Assinatura nova: `CardRules.RollOffer(OfferSource source, int heroLevel, IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string,int> owned, string heroId, SpecTag heroTags, int count, System.Random rng, OfferRules rules, List<CardDefinition> result)`.
    `CardRules.Roll` também ganha `SpecTag heroTags` depois de `heroId`.
  - `OfferRules` ganha `float levelUpUpgradeChance01` e `int maxItemsPerOffer`.
  - `RunSettings` ganha `levelUpUpgradeChancePercent = 60f` e `maxItemsPerOffer = 2`, ambos com `// 🔢`.
  - `HeroCards` passa `RunSpecialization.Tags` em todas as chamadas.

**Regras:**
- **Filtro de tag**, em `Collect`: uma carta com `requiredTags != None` e `(requiredTags & heroTags) == 0`
  fica de fora. Vale para todas as fontes.
- **Level up comum**, carta a carta, `count` vezes:
  1. Sorteia Upgrade com `levelUpUpgradeChance01`; senão, Item.
  2. Se der Item e a oferta já tiver `maxItemsPerOffer` itens, vira Upgrade.
  3. Sem Upgrade elegível, vira Item, desde que caiba no limite. Sem Item possível, vira Stat. Sem nada,
     a oferta sai com menos cartas.
  4. As cartas nunca se repetem na mesma oferta. Retire do buffer de elegíveis a cada pick.
- **Level up múltiplo de 5, baú e vendedor:** iguais à etapa 3, só com o filtro de tag.
- **Atributos (Stats)**, decididos pelo usuário em 2026-11-25:
  - No máximo 6, e **não sobem de nível**: `maxLevel = 1` nos 6 assets `stat_*`.
  - Novo `RunSettings.maxStatSlots = 6` e `OfferRules.maxStatSlots`. Com 6 Stats possuídos, nenhum Stat
    é elegível.
  - Os fallbacks já existentes cuidam do resto:
    - o nível múltiplo de 5 vira oferta comum;
    - o baú não converte nem completa com Stat.
  - Helper `CardRules.CountOwned(pool, owned, CardKind kind)`. `CountOwnedItems` passa a chamar esse
    helper.
  - Os valores por nível dos Stats ficam como estão, valendo o nível 1.
- **Itens:** sem mudança. São no máximo 6; depois disso, só os que o jogador já tem, até o nível máximo.
- **Alocação:** nenhuma além da lista `result`. Reaproveite os buffers estáticos, como hoje.

- [ ] **Passo 1: escrever os testes que falham.** Em `CardRulesTests`:
  - Atualizar as chamadas existentes para a assinatura nova, passando `SpecTag.None`.
  - Dar ao helper `Card(...)` o parâmetro `SpecTag tags = SpecTag.None`.
  - Testes novos:
    - `Tagged_card_never_offered_without_tag` (ponto de revisão 5): pool com 1 Upgrade
      `requiredTags = Earthquake` e mais cartas suficientes de cada tipo. Para LevelUp (nível 2 e
      nível 5), Chest e Vendor, 1000 sorteios cada com `heroTags = Dot | Tank | Size`: a carta tagueada
      nunca aparece.
    - `Tagged_card_offered_when_hero_has_one_of_its_tags`: carta com `Projectile | Hitscan`,
      `heroTags = Hitscan`. Aparece em algum dos 200 sorteios.
    - `Untagged_cards_show_for_heroes_without_specialization`: com `heroTags = None`, as cartas sem tag
      continuam saindo.
    - `LevelUp_never_more_than_two_items`: pool com 10 Upgrades e 10 Items. Em 1000 sorteios de nível 2,
      cada oferta tem 3 cartas, distintas, com no máximo 2 Items.
    - `LevelUp_upgrade_share_is_about_sixty_percent`: mesmo pool, 3000 ofertas. A fração de Upgrades
      fica entre 0,58 e 0,70. A esperada é cerca de 0,616, porque o limite de itens empurra para cima.
    - `LevelUp_can_give_three_upgrades`: em 1000 ofertas, existe pelo menos uma com 3 Upgrades.
    - `LevelUp_without_upgrades_falls_back_to_items_then_stats`: pool com 0 Upgrades, 1 Item e 5 Stats,
      nível 2. Toda oferta tem 3 cartas: 1 Item e 2 Stats.
    - `No_stats_after_six_owned`: pool com 8 Stats (`maxLevel` 1), 6 deles possuídos, mais Upgrades.
      - Nível 5 vira oferta comum, sem Stat.
      - Baú nunca traz Stat.
      - Com 5 possuídos, o nível 5 volta a trazer Stats.
- [ ] **Passo 1b:** mudar `maxLevel` de 3 para 1 nos 6 assets `stat_*` em `Assets/Settings/Cards/`, e pôr
  `maxStatSlots: 6` no `RunSettings.asset`.
- [ ] **Passo 2:** rodar a suíte. Esperado: os testes novos falham (ou não compilam por causa da
  assinatura).
- [ ] **Passo 3:** implementar em `CardRules`, `OfferRules.From`, `RunSettings` (com os valores no asset)
  e `HeroCards`.
- [ ] **Passo 4:** rodar a suíte. Esperado: tudo passa, menos as 3 falhas do `BagTests`.
- [ ] **Passo 5:** commit `feat(cartas): filtro por tag e level up 60% upgrade (max 2 itens)`.

### Task 3: Assets das 6 especializações e a tag Earthquake na carta antiga

**Arquivos:**
- Criar:
  - `Assets/Settings/Specializations/` com 6 assets.
  - `Assets/Resources/SpecializationCatalog.asset`, listando os 6.
- Modificar: `Assets/Settings/Cards/barbarian_primary_area.asset`, com `requiredTags = Earthquake` (256).
- Eval: `t3_spec_assets.cs`

Valores exatos (`icon` fica vazio; o usuário coloca à mão):

| arquivo / id | heroId | slot | displayName | tags | description |
|---|---|---|---|---|---|
| barbarian_earthquake | Barbarian | 0 | Earthquake | Tank, Size, Earthquake | Your primary attack splits the ground, damaging enemies over time. |
| barbarian_bloodlust | Barbarian | 1 | Bloodlust | Dot, Tank, Size | Your attacks apply Bleeding. Bleeds applied while your Shift is active are stronger. |
| ranger_crimson_arrow | Ranger | 0 | Crimson Arrow | Dot, Projectile | Every arrow and your Ultimate apply Bleeding. |
| ranger_beastmaster | Ranger | 1 | Beastmaster | Pet, Projectile | A loyal bear fights by your side. Only your Ultimate applies Bleeding. |
| mage_immolation | Mage | 0 | Immolation | Dot | All your attacks set enemies on fire. |
| mage_phoenix_pact | Mage | 1 | Phoenix Pact | Pet | A phoenix fights by your side. Only your Ultimate burns. |

A ordem dos slots do Barbarian segue o layout que o usuário montou: Earthquake na esquerda e Bloodlust na
direita.

- [ ] **Passo 1: escrever o eval que falha.** `t3_spec_assets.cs`:
  - `SpecializationCatalog.Load()` não é nulo e tem os 6 ids da tabela.
  - `ForHero` devolve 2 por herói, com os slots e tags da tabela.
  - `barbarian_primary_area.requiredTags == Earthquake`.
  - Saída: `PASS`/`FAIL` mais detalhes.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL` (catálogo nulo).
- [ ] **Passo 3:** criar os assets no Editor (`CreateInstance` + `AssetDatabase.CreateAsset`), preencher o
  catálogo e marcar a carta.
- [ ] **Passo 4:** rodar o eval de novo. Esperado: `PASS`.
- [ ] **Passo 5:** commit `feat(spec): assets das 6 especializacoes e Earthquake no Wide Crack`.

### Task 4: Tela de escolha, fluxo da run e gancho no herói

**Arquivos:**
- Criar: `Assets/Scripts/UI/SpecializationScreenUI.cs`
- Modificar:
  - `Assets/Scripts/Player/Heroes/HeroController.cs`
  - `Assets/Scenes/_TestScene.unity` (componente no Canvas e referências)
- Eval: `t4_spec_screen.cs`

**Interfaces:**
- Consome: `SpecializationCatalog.ForHero`, `RunSpecialization`, `TimeManager.PauseBy/ResumeBy`,
  `GameEvents.OnDayStart`, `GameStateManager`, `PauseMenuUI.IsOpen`.
- Produz:
  - `HeroController`:
    - `protected virtual void ApplySpecialization(SpecializationDefinition spec)` (vazio na base).
    - O `HeroController` assina `RunSpecialization.Chosen`. No `Start`, se `RunSpecialization.IsChosen`,
      chama `ApplySpecialization(RunSpecialization.Current)`. Isso cobre o herói recriado (ponto de
      revisão 4).
    - Os heróis que recalculam perks chamam `ApplySpecialization(RunSpecialization.Current)` no **fim** do
      recálculo, para a especialização ganhar dos perks. As Tasks 5, 6 e 7 fazem isso em cada herói.
  - `SpecializationScreenUI` (no Canvas, ao lado do `CardScreenUI`):
    - Campos serializados: `GameObject root` (`Specialization_Choose`), `TMP_Text heroName`
      (`Background_Header/Hero_Name`) e `SpecButtonView[] buttons` (2 itens).
    - `[System.Serializable] struct SpecButtonView { Button button; Image icon; TMP_Text name; TMP_Text description; }`.
      Mapear: `Button/Background_Icon/Icon`, `Button/Background_Name/Name`, `Button/Description`. O
      Button em x=-260 é o índice 0.
    - `bool IsVisible`, `void PressSlot(int i)`.

**Comportamento:**
- **Quando abre:** quando o estado vira `GameState.Gameplay` (e também no `Start`, se já estiver em
  Gameplay, que é o caso do Play direto da cena, ponto de revisão 1). Só abre se não houver escolha e o
  herói tiver 2 especializações.
- **Ao abrir:**
  - Preenche `heroName` com o nome do herói (`heroId`).
  - Preenche cada botão com `displayName`, `description` e `icon`. O ícone nulo fica com o slot vazio,
    mantendo o sprite do layout.
  - Chama `TimeManager.Instance.PauseBy(this)` e mostra `root`.
- **Ao escolher** (clique ou teclas 1/2 e numpad 1/2, ignoradas com `PauseMenuUI.IsOpen`, ponto de
  revisão 3): chama `RunSpecialization.Choose(spec)`, `ResumeBy(this)` e esconde `root`.
- **Começo de run:** `GameEvents.OnDayStart` chama `RunSpecialization.Reset()`. Use a mesma assinatura do
  `HeroCards.HandleRunStart`.
- **Herói sem especialização** (catálogo devolve menos de 2): a tela não abre e as tags ficam `None`.
- **Awake:** esconde `root`. `OnDisable` desfaz as assinaturas e, se a tela estiver aberta, chama
  `ResumeBy(this)`.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t4_spec_screen.cs` instancia o herói Barbarian e uma
  `SpecializationScreenUI` de teste, com `root` e botões criados no eval. Antes, faz `DestroyImmediate`
  das telas da cena. Verifica:
  - (a) Com `GameState.Gameplay` e sem escolha, `IsVisible` fica verdadeiro e `TimeManager.IsPaused` também.
  - (b) Os textos dos botões são "Earthquake" e "Bloodlust", nessa ordem.
  - (c) Com `PauseMenuUI.IsOpen = true`, `PressSlot` via teclado é ignorado. Testar a rotina de teclado
    ou deixar o guard num método testável.
  - (d) `PressSlot(1)`: `RunSpecialization.Is("barbarian_bloodlust")`, a tela some e `IsPaused` fica falso.
  - (e) Disparar `GameEvents` de começo de run: `RunSpecialization.IsChosen` volta a falso.
  - (f) Um segundo herói instanciado depois da escolha recebe `ApplySpecialization` no `Start`. Verificar
    com um contador, ou pelo efeito observável da Task 5, se ela já existir. Nesta task, basta logar que
    o gancho foi chamado.
  - Limpeza completa, reabrindo a cena do disco.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL` (não compila ou a tela não abre).
- [ ] **Passo 3:** implementar `SpecializationScreenUI` e o gancho no `HeroController`.
- [ ] **Passo 4: ligar a tela na cena.** Seguir o protocolo de cena: abrir a `_TestScene` no Editor,
  adicionar o componente ao Canvas, ligar as referências via `SerializedObject` e salvar. Conferir o
  `git diff` da cena: só o componente novo e as referências podem aparecer além do que o usuário já
  tinha. Manter LF.
- [ ] **Passo 5:** rodar o eval de novo, `unity recompile` e a suíte EditMode. Esperado: eval `PASS`, a
  suíte na linha de base.
- [ ] **Passo 6:** commit `feat(spec): tela de escolha no comeco da run`. Inclui a `_TestScene` com a
  tela do usuário, o `Icons.png` e o `ChoosesUI.png`, se o usuário autorizou na revisão do plano.

### Task 5: Barbarian (Earthquake e Bloodlust)

**Arquivos:**
- Modificar: `Assets/Scripts/Player/Heroes/Barbarian.cs`
- Eval: `t5_barbarian.cs`

**Comportamento:**
- **Earthquake:** como hoje. O primário cria o Ground Crack e não aplica Bleeding.
- **Bloodlust:**
  - O primário **não** cria Ground Crack. Pule as chamadas de `SpawnGroundCrackAtWithDamage` e
    `SpawnGroundCrackAt` quando a especialização for Bloodlust.
  - O primário e a Ultimate (machado arremessado, `FireUltimateVolley` e o acerto dele) aplicam
    `StatusEffectType.Bleeding` no inimigo atingido, com:
    - `dps = danoDoGolpe * bloodlustBleedPercentOfHit / 100`;
    - `[SerializeField] float bloodlustBleedPercentOfHit = 50f; // 🔢`;
    - duração `[SerializeField] float bloodlustBleedDuration = 3f; // 🔢`.
  - `danoDoGolpe` é o dano final daquele acerto, já com o multiplicador do Shift
    (`GetSecondaryAbilityDamageMultiplier`), sem o crítico. O valor fica congelado na aplicação, porque o
    `StatusEffectController` guarda o dps passado. Assim, um golpe durante o Shift gera um Bleeding mais
    forte, que continua forte depois que o Shift acaba.
  - O Shift sozinho não aplica nada.
  - O antigo "Sangramento Perfurante" (`sangramentoPerfurante*`) não é usado pela especialização. Os
    perks estão desligados.
- `ApplySpecialization` guarda o id e é chamado no fim do recálculo de perks do Barbarian.
- Herói sem escolha (nunca acontece com a tela, mas pode acontecer em testes): comporta-se como
  Earthquake.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t5_barbarian.cs` instancia o prefab do Barbarian e um
  inimigo de `Assets/Prefabs/Monstros` (não mímico). Mesmo padrão do `f3_thorns.cs` da etapa 3: chamar o
  método do acerto via reflexão, ou o helper que o implementador expuser. Verifica:
  - (a) Earthquake: o acerto do primário cria um `GroundCrack` na cena e o inimigo fica sem Bleeding.
  - (b) Bloodlust: o acerto do primário não cria `GroundCrack`, e o inimigo fica com Bleeding ativo
    (`IsEffectActive(Bleeding)`) com dps = 50% do dano do golpe.
  - (c) Bloodlust com `secondaryAbilityRemaining > 0`: o dps é maior que em (b), na razão do
    multiplicador do Shift.
  - (d) Bloodlust: o acerto do machado da Ultimate aplica Bleeding.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar.
- [ ] **Passo 4:** rodar o eval e a suíte. Esperado: eval `PASS`, a suíte na linha de base.
- [ ] **Passo 5:** commit `feat(barbarian): especializacoes Earthquake e Bloodlust`.

### Task 6: Mage (Immolation e Phoenix Pact) e os 8 upgrades do Mage

**Arquivos:**
- Modificar:
  - `Assets/Scripts/Player/Heroes/Mage.cs`
  - `Assets/Scripts/Player/MageAttackHitbox.cs` (aplicar o Fire nos ticks)
- Criar: 8 assets em `Assets/Settings/Cards/`, acrescentados ao `Assets/Resources/CardCatalog.asset`.
- Eval: `t6_mage.cs`

**Comportamento:**
- **Phoenix Pact:**
  - `petSummoner.HasPet = true`.
  - A chama do primário **não** aplica o status Fire, como hoje.
  - A bola de fogo da Ultimate aplica Fire, como hoje.
- **Immolation:**
  - `petSummoner.HasPet = false`, e uma Fênix que já exista vai embora.
  - Cada tick da chama do primário que acerta um inimigo aplica `StatusEffectType.Fire` com
    `dps = danoDoTick * fireStatusDamageMultiplier` e a duração `fireballFireStatusDuration`.
  - A Ultimate continua aplicando Fire.
  - Mudança em `MageAttackHitbox.StartFire`: novo parâmetro opcional
    `System.Func<float> fireStatusDps = null` e `float fireStatusDuration = 0f`. Quando houver
    `fireStatusDps`, cada inimigo atingido no tick recebe o status. Sem alocar por tick.
- **O teleporte (Shift)** não aplica nada nas duas especializações.
- **Cartas do Mage nas habilidades:**
  - `RollFireTickDamage` multiplica por `Cards.Get(CardAbility.Primary, CardStat.Damage)`.
  - O tamanho da chama (`attackHitbox.transform.localScale`) multiplica por
    `Cards.Get(Primary, Area)`.
  - `impactDamage` da Ultimate multiplica por `Cards.Get(Ultimate, Damage)`.
  - `explosionRadius` da Ultimate multiplica por `Cards.Get(Ultimate, Area)`.
  - Cooldown do Shift, energia da Ultimate e velocidade de animação já são genéricos no `HeroController`.
- `ApplySpecialization` é chamado no fim do recálculo de perks do Mage, para ganhar do perk que liga
  `HasPet`.

**Os 8 assets** (`kind: Upgrade`, `heroId: Mage`, `requiredTags: None`, `maxLevel: 5`):

| id | title | description | ability | stat | valuePerLevel |
|---|---|---|---|---|---|
| mage_primary_damage | Searing Flames | +{0} Flame Damage | Primary | Damage | 1.25 |
| mage_primary_area | Wildfire | +{0} Flame Size | Primary | Area | 0.15 |
| mage_shift_cooldown | Blink Mastery | -{0} Teleport Cooldown | Shift | Cooldown | 0.1 |
| mage_shift_anim_speed | Swift Blink | +{0} Teleport Speed | Shift | AnimSpeed | 0.1 |
| mage_ultimate_damage | Meteor Core | +{0} Fireball Damage | Ultimate | Damage | 1.25 |
| mage_ultimate_area | Great Fireball | +{0} Fireball Explosion Size | Ultimate | Area | 0.15 |
| mage_ultimate_cooldown | Arcane Patience | -{0} Ultimate Energy Cost | Ultimate | Cooldown | 0.1 |
| mage_ultimate_anim_speed | Quick Cast | +{0} Ultimate Cast Speed | Ultimate | AnimSpeed | 0.1 |

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t6_mage.cs` verifica:
  - (a) Phoenix Pact deixa `HasPet` verdadeiro; Immolation deixa falso.
  - (b) Immolation: um tick da chama no inimigo deixa o Fire ativo; Phoenix Pact: um tick não deixa.
  - (c) Com a carta `mage_primary_damage` no nível 1 (`Cards.Recompute`), o dano do tick fica ×1,25.
  - (d) Os 8 ids estão no `CardCatalog`, com os campos da tabela.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar e criar os assets.
- [ ] **Passo 4:** rodar o eval e a suíte. Esperado: eval `PASS`, a suíte na linha de base.
- [ ] **Passo 5:** commit `feat(mage): especializacoes Immolation e Phoenix Pact, upgrades do Mage`.

### Task 7: Ranger (Crimson Arrow e Beastmaster, sem o pet) e os 9 upgrades do Ranger

**Arquivos:**
- Modificar: `Assets/Scripts/Player/Heroes/Ranger.cs`
- Criar: 9 assets em `Assets/Settings/Cards/`, acrescentados ao `CardCatalog`.
- Eval: `t7_ranger.cs`

**Comportamento:**
- **Crimson Arrow:** `arrowBleedChancePercent = 100`. Toda flecha aplica Bleeding.
- **Beastmaster:** `arrowBleedChancePercent = 0`. O pet entra na Task 8.
- **Ultimate (facas):** aplica Bleeding nas duas especializações, como hoje.
- **dps do Bleeding nas flechas e nas facas:** multiplica pelo `GetShiftDamageDealtMultiplier()` no
  momento do disparo. É a mesma regra do DOT do Barbarian: o valor congela com o buff do Shift.
- **Cartas do Ranger nas habilidades:**
  - `reservePerArrow` multiplica por `Cards.Get(Primary, Damage)`.
  - A escala da flecha instanciada multiplica por `Cards.Get(Primary, Area)`.
  - `GetShiftDamageDealtMultiplier`, quando o bônus do Shift estiver ativo, multiplica por
    `Cards.GetOwn(Shift, Damage)`, como o Barbarian.
  - A duração máxima da camuflagem (`maxDuration` em `Ranger.cs`, ~l.294) multiplica por
    `Cards.Get(Shift, AnimSpeed)`.
  - O dano das facas da Ultimate (`flightDamage` e o do chão) multiplica por
    `Cards.Get(Ultimate, Damage)`.
  - A escala das facas multiplica por `Cards.Get(Ultimate, Area)`.
- `ApplySpecialization` é chamado no fim do recálculo de perks do Ranger (onde hoje fica
  `arrowBleedChancePercent = PerkState...`).

**Os 9 assets** (`kind: Upgrade`, `heroId: Ranger`, `requiredTags: None`, `maxLevel: 5`):

| id | title | description | ability | stat | valuePerLevel |
|---|---|---|---|---|---|
| ranger_primary_damage | Sharpened Arrows | +{0} Arrow Damage | Primary | Damage | 1.25 |
| ranger_primary_area | Heavy Arrows | +{0} Arrow Size | Primary | Area | 0.15 |
| ranger_shift_damage | Ambush | +{0} Damage After Camouflage | Shift | Damage | 1.25 |
| ranger_shift_cooldown | Quick Hide | -{0} Camouflage Cooldown | Shift | Cooldown | 0.1 |
| ranger_shift_anim_speed | Long Shadow | +{0} Camouflage Duration | Shift | AnimSpeed | 0.1 |
| ranger_ultimate_damage | Serrated Knives | +{0} Knife Damage | Ultimate | Damage | 1.25 |
| ranger_ultimate_area | Broad Blades | +{0} Knife Size | Ultimate | Area | 0.15 |
| ranger_ultimate_cooldown | Hunter's Patience | -{0} Ultimate Energy Cost | Ultimate | Cooldown | 0.1 |
| ranger_ultimate_anim_speed | Quick Draw | +{0} Ultimate Animation Speed | Ultimate | AnimSpeed | 0.1 |

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t7_ranger.cs` verifica:
  - (a) Crimson Arrow: a chance de Bleeding é 100; Beastmaster: 0, inclusive depois de rodar o recálculo
    de perks de novo.
  - (b) Uma flecha disparada no Crimson Arrow, com o `Launch` interceptado ou um campo lido por reflexão
    no `RangerArrow`, carrega o dps de Bleeding maior quando o Shift está ativo.
  - (c) `ranger_primary_damage` no nível 1 deixa `reservePerArrow` ×1,25.
  - (d) Os 9 ids estão no catálogo.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar e criar os assets.
- [ ] **Passo 4:** rodar o eval e a suíte. Esperado: eval `PASS`, a suíte na linha de base.
- [ ] **Passo 5:** commit `feat(ranger): especializacoes Crimson Arrow e Beastmaster, upgrades do Ranger`.

### Task 8: Pet urso do Beastmaster

**Arquivos:**
- Criar:
  - `Assets/Animation/Heros/Ranger/Passiva/Bear/`: `Bear.controller` e os clipes.
  - `Assets/Prefabs/Heros/Ranger/Bear.prefab`
  - `Assets/Animation/Heros/Ranger/Passiva/summonpet_{ne,nw,se,sw}.anim`
- Modificar:
  - `Assets/Animation/Heros/Ranger/Ranger.controller` (estado e trigger de invocar)
  - `Assets/Prefabs/Heros/Ranger/Ranger.prefab` (componente `PetSummoner` apontando para `Bear.prefab`)
  - `Assets/Scripts/Player/Heroes/Ranger.cs` (lógica de invocar, copiada do Mage)
- Eval: `t8_bear.cs`

**Precedente a copiar, na mesma estrutura:**
- **`Phoenix.controller`:**
  - parâmetros `DirX`, `DirY`, `AttackTrigger`, `DieTrigger` e `ActionSpeedMultiplier`;
  - estados `Fly` (blend tree de 4 direções), `Attack` (blend), `Die` (blend) e `Summon`.
- **`Bear.controller`:** usa os mesmos nomes de parâmetros e estados, para o `PetController` funcionar
  sem mudança:
  - `Fly` usa os clipes `walk_*` (o urso anda);
  - `Attack` usa `attack_*`;
  - `Die` usa `die_*`;
  - `Summon` usa `summon` (do `Summon.png`).
  - `Minifantasy_BearIdle` fica sem uso nesta etapa.
- **Direções dos sprites:** as folhas do urso são Minifantasy, como as da Fênix. Descubra qual faixa de
  sprites é cada direção comparando com os índices que `fly_ne`, `fly_nw`, `fly_se` e `fly_sw` da Fênix
  usam na folha dela. Confira visualmente (captura do sprite) antes de gerar.
- **`Bear.prefab`:** copie o `Phoenix.prefab` e troque o controller, o sprite inicial e o
  colisor/escala, se o tamanho for diferente. A Fênix voa, o urso anda: confira se o `PetController` tem
  algo exclusivo de voo.
- **Invocar no Ranger:** copie do Mage:
  - `UpdatePetSummon`, `AnimationSummonPetEvent`, `AnimationSummonPetEndEvent`, o trigger
    `SummonPetTrigger` e o `petSummoner.SpawnOne(...)`;
  - os clipes `summonpet_*` do Ranger vêm do `Ranger_Summon.png`, com os eventos nos mesmos frames
    relativos dos `summonpet_*` do Mage;
  - estado e trigger no `Ranger.controller` via API `AnimatorController`.
- **Beastmaster:** `petSummoner.HasPet = true`. **Crimson Arrow:** `false`.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t8_bear.cs` verifica:
  - (a) `Bear.prefab` existe, tem `PetController` e `Animator` com `Bear.controller`.
  - (b) Todo estado do `Bear.controller` tem `motion` não nulo (blend trees com 4 filhos não nulos).
  - (c) Todo clipe do urso e `summonpet_*` do Ranger tem mais de 0 keyframes de sprite.
  - (d) Nenhum `.anim` gerado tem linha em branco no YAML (ler o arquivo do disco).
  - (e) `Ranger.controller` tem o parâmetro `SummonPetTrigger` e um estado com motion.
  - (f) O prefab do Ranger tem `PetSummoner`. Com Beastmaster, `HasPet` é verdadeiro.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:** gerar os clipes e os controllers, criar o prefab e editar o Ranger (código e prefab),
  seguindo o protocolo de prefab.
- [ ] **Passo 4:** rodar o eval e a suíte. Esperado: eval `PASS`, a suíte na linha de base. Abrir o
  `Bear.controller` e o `Ranger.controller` no Animator e conferir os estados.
- [ ] **Passo 5:** commit `feat(ranger): pet urso do Beastmaster`.

### Task 9: Barra de atributos (`Atribute_Background`)

**Arquivos:**
- Modificar:
  - `Assets/Scripts/UI/InventoryHudUI.cs`
  - `Assets/Scripts/Player/HeroCards.cs`
  - `Assets/Scenes/_TestScene.unity` (campo do componente no `Atribute_Background`)
- Eval: `t9_attribute_hud.cs`

**Contexto:**
- O usuário criou o `Atribute_Background` com 6 filhos `Icon`, desligados, e copiou para ele o mesmo
  componente `InventoryHudUI` do `Inventory_Background`. Hoje os dois mostrariam os itens.
- O filho `Quantity` dos itens foi removido pelo usuário. O nível do item não aparece mais na barra.
- Upgrades de habilidade **não** aparecem em barra nenhuma.

**Interfaces:**
- `HeroCards.OwnedStats` (`IReadOnlyList<CardDefinition>`, na ordem em que foram pegos), mantido como o
  `OwnedItems`. Dispara o mesmo `GameEvents.OnItemsChanged` quando um Stat entra.
- `InventoryHudUI` ganha `[SerializeField] CardKind kind = CardKind.Item;`.
  - Com `Item`, mostra `OwnedItems`; com `Stat`, mostra `OwnedStats`.
  - Remover a busca por `Quantity` e o texto de nível.
- Na cena: o componente do `Atribute_Background` fica com `kind = Stat`; o do `Inventory_Background`
  continua `Item`.

**Passos:**
- [ ] **Passo 1: escrever o eval que falha.** `t9_attribute_hud.cs`:
  - Concede 1 Item e 2 Stats via `HeroCards.Grant`.
  - Na barra de itens, só 1 slot ligado, com o ícone do Item.
  - Na barra de atributos, 2 slots ligados, com os ícones dos Stats.
  - Concede um Upgrade: nenhuma barra muda.
  - Limpeza completa.
- [ ] **Passo 2:** rodar o eval. Esperado: `FAIL`.
- [ ] **Passo 3:** implementar e ligar `kind = Stat` na cena, com o protocolo de cena.
- [ ] **Passo 4:** rodar o eval e a suíte. Esperado: eval `PASS`, a suíte na linha de base.
- [ ] **Passo 5:** commit `feat(hud): barra de atributos (6 Stats)`.

### Task 10: Remover o Tab, o painel Inventory e o painel Shop

**Contexto:** o usuário pediu em 2026-11-25. O Tab (ação `Inventory` do Input System) abre a Bag via
`BagController.ToggleBag`, e isso não serve mais. Os painéis `Inventory` e `Shop` da `_TestScene` também
não. Fora deles mesmos, nenhum código usa `InventoryPanelController` ou `ShopUIController`; só há citações
em comentários.

**Arquivos:**
- Modificar:
  - `Assets/PlayerControls.inputactions`: remover a ação `Inventory` e o binding `<Keyboard>/tab`. Usar o
    Editor para que o `PlayerControls.cs` gerado seja refeito.
  - `Assets/Scripts/Player/BagController.cs`: remover a assinatura do Tab.
  - `Assets/Scripts/Tests/PlayerInputTest.cs`: remover a linha do Inventory.
  - `Assets/Scenes/_TestScene.unity`: apagar os GameObjects `Inventory` e `Shop`.
- Apagar: `Assets/Scripts/UI/InventoryPanelController.cs` e `Assets/Scripts/UI/ShopUIController.cs`,
  com os `.meta`. Ajustar os comentários que citam esses arquivos.
- **Fica:** o `ShopHandler` (World) e o estado `GameState.Shop`, usados pelo Continue Game. A limpeza
  deles é da etapa 6.

**Passos:**
- [ ] **Passo 1: eval `t10_cleanup.cs` (escrever primeiro e ver falhar).** Verifica:
  - O `PlayerControls` não tem a ação `Inventory`.
  - A cena aberta não tem `InventoryPanelController` nem `ShopUIController`, nem GameObjects chamados
    `Inventory` ou `Shop` no Canvas.
  - Nenhum componente da cena tem script faltando: varrer
    `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`.
- [ ] **Passo 2:** aplicar as remoções. Antes de apagar, conferir com `git grep` que nenhuma referência
  serializada a esses scripts (pelo GUID do `.meta`) fica em outra cena ou prefab.
- [ ] **Passo 3:** `unity recompile`, o eval e a suíte. Esperado: compila sem erro, eval `PASS`, a suíte
  na linha de base.
- [ ] **Passo 4:** commit `chore: remove Tab, painel Inventory e painel Shop`.

## Fim

- Revisão final da branch inteira por um revisor novo.
- O usuário testa no Play:
  - a tela aparece para os 3 heróis;
  - os 2 botões de cada um mudam o jogo como na tabela da spec;
  - as cartas Earthquake não aparecem para o Bloodlust;
  - o level up nunca traz 3 itens;
  - o urso é invocado e luta.
- Perguntar antes de qualquer push ou merge.
