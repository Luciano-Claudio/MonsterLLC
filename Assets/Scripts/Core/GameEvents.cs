using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    // Sprint 7: passou a carregar o valor de energia do kill (GDD Seção 11 —
    // monstros mais fortes concedem mais Energia por kill). Quebra de assinatura proposital.
    // isBoss adicionado na Deadline 8.1 Seção 3.1 — KillGoldReward precisa saber se foi boss ou
    // monstro comum pra escolher a coluna certa da tabela de gold por dia.
    public static event Action<int, bool> OnEnemyKilled;
    public static void EnemyKilled(int energyValue, bool isBoss) => OnEnemyKilled?.Invoke(energyValue, isBoss);

    // Etapas 1+2 — o herói coletou uma gema de XP (XpGemPool). Escutado pelo HeroLevel.
    public static event Action<int> OnXpCollected;
    public static void XpCollected(int xp) => OnXpCollected?.Invoke(xp);

    // D9.6b — o herói subiu de nível (o D9.6c abre a tela de cartas a partir daqui).
    public static event Action<int> OnHeroLeveledUp;
    public static void HeroLeveledUp(int level) => OnHeroLeveledUp?.Invoke(level);

    // Telemetria de run (docs/superpowers/specs/2026-11-16-run-telemetry-design.md) — versão
    // detalhada do kill, só primitivos/Vector3 (regra do asmdef Core), complementa o
    // OnEnemyKilled acima, nunca o substitui. directHits/dotTicks separados de propósito — ver
    // "Revisão do builder-check" no design (misturar os 2 enviesaria a análise de balanceamento).
    public static event Action<string, bool, int, int, float, int, Vector3> OnEnemyKilledDetailed;
    // Catálogo de cartas — tick de DOT num inimigo (Affliction Heart/Harvest) e morte de inimigo que tinha
    // DOT (Plague Herald, Death's Echo). killedByDot = o golpe final foi um tick.
    public static event Action<StatusEffectController, StatusEffectType, float, bool> OnDotTick;
    public static void DotTicked(StatusEffectController status, StatusEffectType type, float damage, bool crit) => OnDotTick?.Invoke(status, type, damage, crit);
    public static event Action<StatusEffectController, Vector3, bool> OnEnemyDiedWithDot;
    public static void EnemyDiedWithDot(StatusEffectController status, Vector3 position, bool killedByDot) => OnEnemyDiedWithDot?.Invoke(status, position, killedByDot);
    public static void EnemyKilledDetailed(string enemyName, bool isBoss, int directHits, int dotTicks, float maxHealth, int energyReward, Vector3 position) =>
        OnEnemyKilledDetailed?.Invoke(enemyName, isBoss, directHits, dotTicks, maxHealth, energyReward, position);

    public static event Action<float, float> OnEnergyChanged;
    public static void EnergyChanged(float current, float max) => OnEnergyChanged?.Invoke(current, max);

    // UI de Skills (Primary_Attack/Shift) — (timeRemaining, duration) do próprio AttackCooldown
    // do HeroController, disparado todo frame junto com o Tick(). timeRemaining=0 -> pronto.
    public static event Action<float, float> OnPrimaryAttackCooldownChanged;
    public static void PrimaryAttackCooldownChanged(float timeRemaining, float duration) => OnPrimaryAttackCooldownChanged?.Invoke(timeRemaining, duration);

    public static event Action<float, float> OnSecondaryAbilityCooldownChanged;
    public static void SecondaryAbilityCooldownChanged(float timeRemaining, float duration) => OnSecondaryAbilityCooldownChanged?.Invoke(timeRemaining, duration);

    // Estado "em uso agora" (isAttacking/isUsingSecondaryAbility do HeroController) —
    // Ussage (UI de Skills) precisa disso, não só "fora de cooldown": enquanto a habilidade
    // está ativa ela tem prioridade visual sobre o Cooldown, mesmo que o cooldown técnico já
    // tenha começado a contar por baixo (decisão do usuário — o início real do cooldown só
    // depois do fim de verdade da habilidade é mudança futura, documentada em docs/gdd/index.md
    // Seção 53, ainda não implementada).
    public static event Action<bool> OnPrimaryAttackActiveChanged;
    public static void PrimaryAttackActiveChanged(bool isActive) => OnPrimaryAttackActiveChanged?.Invoke(isActive);

    public static event Action<bool> OnSecondaryAbilityActiveChanged;
    public static void SecondaryAbilityActiveChanged(bool isActive) => OnSecondaryAbilityActiveChanged?.Invoke(isActive);

    // Mesma ideia, pra Ultimate — só importa pra heróis cuja Ultimate tem forma/duração
    // própria (Alce do Druid, forma sombria do Assassin — IsUltimateActive de cada um). UI de
    // Skills usa isso pra manter Ultimate_Active aceso o tempo todo que ela estiver em uso, não
    // só quando a energia enche (rework 2026-10-30).
    public static event Action<bool> OnUltimateActiveChanged;
    public static void UltimateActiveChanged(bool isActive) => OnUltimateActiveChanged?.Invoke(isActive);

    // Troca temporária do ícone de uma skill na UI (ex.: Alce do Druid trocando a garra/
    // Rugido/Cabeçada no lugar da vinha/Coruja/Leaves_Spell na forma de urso) — slot: 0 = Primary_Attack,
    // 1 = Shift, 2 = Ultimate (D9.1). icon null = volta pro ícone normal do herói.
    public static event Action<int, Sprite> OnActionIconOverrideChanged;
    public static void ActionIconOverrideChanged(int slot, Sprite icon) => OnActionIconOverrideChanged?.Invoke(slot, icon);

    public static event Action<float, float> OnHealthChanged;
    public static void HealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);

    public static event Action<Bag> OnBagChanged;
    public static void BagChanged(Bag bag) => OnBagChanged?.Invoke(bag);

    // TAB abre/fecha o painel (BagController.ToggleBag) — a UI do inventário escuta isso pra
    // mostrar/esconder o painel em vez de fazer polling de IsOpen todo frame.
    public static event Action<bool> OnBagToggled;
    public static void BagToggled(bool isOpen) => OnBagToggled?.Invoke(isOpen);

    // long, não int — Gold usa o mesmo tipo de RunState.gold (Sprint 4), sem teto de ~2,1bi.
    public static event Action<long> OnGoldChanged;
    public static void GoldChanged(long gold) => OnGoldChanged?.Invoke(gold);

    // Gold permanente (PermanentGold) — total salvo, e cada moeda coletada no chão.
    public static event Action<long> OnPermanentGoldChanged;
    public static void PermanentGoldChanged(long gold) => OnPermanentGoldChanged?.Invoke(gold);
    // Loja permanente — níveis de um herói mudaram (compra, venda, Toggle).
    public static event Action<string> OnPermanentUpgradesChanged;
    public static void PermanentUpgradesChanged(string hero) => OnPermanentUpgradesChanged?.Invoke(hero);
    // Loja permanente — Stat Item (abre a tela de cartas) e Reroll_Dice (+1 reroll) coletados.
    public static event Action OnStatItemCollected;
    public static void StatItemCollected() => OnStatItemCollected?.Invoke();
    public static event Action<int> OnRerollsChanged; // estoque de rerolls da run
    public static void RerollsChanged(int stock) => OnRerollsChanged?.Invoke(stock);
    public static event Action OnRerollDiceCollected;
    public static void RerollDiceCollected() => OnRerollDiceCollected?.Invoke();
    public static event Action<int> OnGoldCoinCollected;
    public static void GoldCoinCollected(int gold) => OnGoldCoinCollected?.Invoke(gold);

    // anchor == null significa "esconder o prompt".
    public static event Action<Transform> OnInteractPromptChanged;
    public static void InteractPromptChanged(Transform anchor) => OnInteractPromptChanged?.Invoke(anchor);

    public static event Action<float> OnTimeChanged;
    public static void TimeChanged(float time) => OnTimeChanged?.Invoke(time);

    public static event Action<GameState> OnGameStateChanged;
    public static void GameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

    // Floating Combat Text — quem sofreu dano (herói ou monstro) não sabe nada de UI/texto,
    // só avisa aqui. position já vem calculada em cima do sprite (topo/"cabeça"), não é o
    // pivot bruto da entidade. `isPlayer` (Game Feel/Cinemachine, quebra de assinatura
    // proposital — único consumidor antes disso era FloatingCombatTextSpawner): true = o
    // HERÓI tomou esse dano, false = um MONSTRO tomou (hoje só o jogador causa dano a
    // monstro neste jogo, então false aqui equivale a "o jogador acertou um hit"). `isDirectPlayerHit`
    // só importa quando isPlayer=false (irrelevante/sempre true no caso do herói) — false pra
    // DoT/pulse tick/dano de Pet (ver EnemyController.TakeDamage), deixa HitFeelController
    // fazer shake/zoom só no hit direto do jogador, nunca em dano repetido/passivo.
    public static event Action<Vector3, float, bool, bool> OnDamageTaken;
    public static void DamageTaken(Vector3 position, float amount, bool isPlayer, bool isDirectPlayerHit) => OnDamageTaken?.Invoke(position, amount, isPlayer, isDirectPlayerHit);

    // Mesma ideia do OnDamageTaken, mas pra dano que foi inteiramente ABSORVIDO antes de
    // chegar na vida real (ex.: shield do Paladin) — a vida não muda, mas o jogador precisa
    // ver que o hit aconteceu, só numa cor diferente (avisa "bloqueado", não "machucado").
    public static event Action<Vector3, float, Color> OnDamageBlocked;
    public static void DamageBlocked(Vector3 position, float amount, Color color) => OnDamageBlocked?.Invoke(position, amount, color);

    // Mesma ideia, pra cura — qualquer herói que curar (Cleric, Ranger, etc.) passa por aqui.
    // Verde claro com "+" na frente (ver FloatingCombatText/HeroController.Heal()).
    public static event Action<Vector3, float> OnHealReceived;
    public static void HealReceived(Vector3 position, float amount) => OnHealReceived?.Invoke(position, amount);

    // Início de um novo dia (inclusive o Dia 1) — dispara de dentro de
    // DayTimer.ResetForNewDay(), fonte única chamada tanto pelo fim de dia normal
    // (ShopHandler) quanto por novo jogo/continuar (MainMenuUI). Pets de início de dia
    // (Phoenix/Elemental de Sangue, Sprint 19+) escutam isso pra se re-sumonar.
    public static event Action OnDayStart;
    public static void DayStarted() => OnDayStart?.Invoke();

    // Fim do dia de gameplay — dispara tanto de DayResolver.ResolveEndOfDay() (sobreviveu o dia
    // inteiro) quanto de DayResolver.ResolvePlayerDeath() (morreu), ANTES de ir pra Results nos
    // dois casos (GDD Deadline 8.1, Seção 3 — não existe mais Game Over). Sprint 32 — Vendor usa
    // isso pra tocar Shop_Closing.
    public static event Action OnDayEnd;
    public static void DayEnded() => OnDayEnd?.Invoke();

    // Ciclo Dia/Noite real (docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
    // ATUALIZADO_V2.md, Seção 3) — dispara 1x só na TROCA de fase dentro do dia atual (Dia ->
    // Noite em 50% da duração, Noite -> próximo Dia só no OnDayStart seguinte). true = virou
    // Noite. Fonte única: DayTimer. Spawn de monstro/boss escuta isso (ou lê
    // DayTimer.Instance.IsNight direto) pra trocar de dayWeights pra nightWeights — terreno/
    // bioma NUNCA escuta isso de propósito (decisão do usuário: bioma não oscila a cada ciclo).
    public static event Action<bool> OnDayNightPhaseChanged;
    public static void DayNightPhaseChanged(bool isNight) => OnDayNightPhaseChanged?.Invoke(isNight);

    // Disparado quando o herói é instanciado de verdade (MainMenuUI.SpawnHero) — não existe
    // herói nenhum na cena antes disso (Sprint P2, Pré-Deadline Mundo Único). ChunkStreamer
    // escuta isso pra saber quem seguir (streamingTarget); qualquer outro sistema que precise
    // do Transform do herói assim que ele nasce pode escutar aqui também, em vez de GameObject
    // .Find/tag polling.
    public static event Action<Transform> OnHeroSpawned;
    public static void HeroSpawned(Transform hero) => OnHeroSpawned?.Invoke(hero);

    // Disparado logo depois do(s) Instantiate() de um CICLO de spawn de boss
    // (WorldBossSpawnManager) — 1 ou mais bosses nascidos juntos no mesmo ciclo (hoje: 2 no
    // Boss Timer periódico, 1 no replacement por distância). Game Feel escuta isso pra tocar a
    // sequência de "boss reveal" (pausa, foca em cada boss do grupo em sequência, volta pro
    // jogador, despausa). Não dispara pra monstro comum, só boss.
    public static event Action<IReadOnlyList<Transform>> OnBossGroupSpawned;
    public static void BossGroupSpawned(IReadOnlyList<Transform> bosses) => OnBossGroupSpawned?.Invoke(bosses);

    // Etapas 1+2 — o Divine God (último boss da run) morreu: vitória (DayResolver.EndRun).
    public static event Action OnFinalBossKilled;
    public static void FinalBossKilled() => OnFinalBossKilled?.Invoke();

    // Etapas 1+2 — a run terminou (true = vitória no Divine God, false = morte). Dispara 1 vez por run.
    public static event Action<bool> OnRunEnded;
    public static void RunEnded(bool victory) => OnRunEnded?.Invoke(victory);

    // Etapas 1+2 — uma carta foi pega (id, nível depois). Escutado pela telemetria.
    public static event Action<string, int> OnCardGranted;
    public static void CardGranted(string cardId, int levelAfter) => OnCardGranted?.Invoke(cardId, levelAfter);

    // Etapa 3: os itens do herói mudaram (item novo, nível de item, run nova) — barra de itens do HUD.
    public static event Action OnItemsChanged;
    public static void ItemsChanged() => OnItemsChanged?.Invoke();

    // Disparado pelo botão de debug "Regenerar Mundo" (ChunkTerrainGenerator.RegenerateWorld)
    // e por qualquer compra de Tier Suppression (ShopHandler.BuyNextTierSuppression) — decisão
    // do usuário: nos dois casos a população precisa renascer do zero sob as regras atuais
    // (Tier suprimido não pode deixar monstro/boss "velho" daquele Tier sobrando na cena).
    // WorldPopulationManager/WorldBossSpawnManager escutam isso e destroem (nunca Die(), sem
    // loot/kill) tudo que está vivo — o próprio ciclo de população/Boss Timer detecta "caiu pra
    // 0" e repõe sozinho no próximo Update(), sem precisar forçar spawn aqui.
    public static event Action OnClearAllEnemies;
    public static void ClearAllEnemies() => OnClearAllEnemies?.Invoke();

    // Mais eventos entram aqui conforme os sistemas nascerem.
    // Nenhum outro script deve declarar um event solto — tudo passa por aqui.
}
