using System;
using UnityEngine;

public static class GameEvents
{
    // Sprint 7: passou a carregar o valor de energia do kill (GDD Seção 11 —
    // monstros mais fortes concedem mais Energia por kill). Quebra de assinatura proposital.
    public static event Action<int> OnEnemyKilled;
    public static void EnemyKilled(int energyValue) => OnEnemyKilled?.Invoke(energyValue);

    public static event Action<float, float> OnEnergyChanged;
    public static void EnergyChanged(float current, float max) => OnEnergyChanged?.Invoke(current, max);

    public static event Action<float, float> OnHealthChanged;
    public static void HealthChanged(float current, float max) => OnHealthChanged?.Invoke(current, max);

    // [Limpeza sistema de Floor] Substitui OnFloorChanged — único consumidor real era
    // PetController.HandleFloorChanged, que nem usava o parâmetro FloorDefinition (só reagia
    // ao disparo). Disparado por HeroController.Respawn() (volta pra origem do mundo).
    public static event Action OnHeroRespawned;
    public static void HeroRespawned() => OnHeroRespawned?.Invoke();

    public static event Action<Bag> OnBagChanged;
    public static void BagChanged(Bag bag) => OnBagChanged?.Invoke(bag);

    // TAB abre/fecha o painel (BagController.ToggleBag) — a UI do inventário escuta isso pra
    // mostrar/esconder o painel em vez de fazer polling de IsOpen todo frame.
    public static event Action<bool> OnBagToggled;
    public static void BagToggled(bool isOpen) => OnBagToggled?.Invoke(isOpen);

    // long, não int — Gold usa o mesmo tipo de RunState.gold (Sprint 4), sem teto de ~2,1bi.
    public static event Action<long> OnGoldChanged;
    public static void GoldChanged(long gold) => OnGoldChanged?.Invoke(gold);

    // anchor == null significa "esconder o prompt".
    public static event Action<Transform> OnInteractPromptChanged;
    public static void InteractPromptChanged(Transform anchor) => OnInteractPromptChanged?.Invoke(anchor);

    public static event Action<float> OnTimeChanged;
    public static void TimeChanged(float time) => OnTimeChanged?.Invoke(time);

    public static event Action<int, int> OnDemandChanged;
    public static void DemandChanged(int sold, int target) => OnDemandChanged?.Invoke(sold, target);

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

    // Fim do dia de gameplay — dispara de DayResolver.ResolveEndOfDay() (timer de DayTimer
    // chegando a zero), ANTES de decidir Results ou GameOver (dispara nos dois casos: o Vendor
    // fecha a loja de qualquer forma, sucesso ou fracasso na Demanda). Sprint 32 — Vendor usa
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

    // Disparado logo depois do Instantiate() de um boss (WorldBossSpawnManager.TrySpawnBoss)
    // — Game Feel escuta isso pra tocar a sequência de "boss reveal" (pausa, Cinemachine foca
    // no boss, volta pro jogador, despausa, troca pro CinemachineTargetGroup). Não dispara pra
    // monstro comum, só boss.
    public static event Action<Transform> OnBossSpawned;
    public static void BossSpawned(Transform boss) => OnBossSpawned?.Invoke(boss);

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
