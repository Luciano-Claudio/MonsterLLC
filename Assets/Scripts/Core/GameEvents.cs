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

    public static event Action<FloorDefinition> OnFloorChanged;
    public static void FloorChanged(FloorDefinition floor) => OnFloorChanged?.Invoke(floor);

    public static event Action<Bag> OnBagChanged;
    public static void BagChanged(Bag bag) => OnBagChanged?.Invoke(bag);

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
    // pivot bruto da entidade.
    public static event Action<Vector3, float> OnDamageTaken;
    public static void DamageTaken(Vector3 position, float amount) => OnDamageTaken?.Invoke(position, amount);

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

    // Mais eventos entram aqui conforme os sistemas nascerem.
    // Nenhum outro script deve declarar um event solto — tudo passa por aqui.
}
