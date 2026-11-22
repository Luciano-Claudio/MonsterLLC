using UnityEngine;

// D9.6b (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md) — XP e nível do
// herói. Mora no "Systems" (DontDestroyOnLoad, adicionado pelo SystemsBootstrap) e guarda o
// estado no RunState (sobrevive aos dias e ao save; zera na run nova; a morte apaga o save).
// - Kill: XP pela tabela por tier (GameEvents.OnEnemyKilledXp).
// - Cap do dia: não passa do nível do dia + 1; no cap a barra enche e o excedente vira gold
//   (1 por XP, até GoldDailyLimit por dia), o resto se perde.
// - Subiu: reescreve os stats do herói (HeroController.ApplyLevel) e avisa HeroLeveledUp (o
//   D9.6c abre a tela de cartas a partir daí).
public class HeroLevel : MonoBehaviour
{
    public const int GoldDailyLimit = 500; // 🔢 — números definitivos de gold ficam pro D9.7

    public static HeroLevel Instance { get; private set; }

    public int Level => Run != null ? Run.level : 1;
    public long XpIntoLevel => Run != null ? Run.xpIntoLevel : 0;
    public long XpToNext => HeroLevelRules.XpToNext(Level);

    private static RunState Run => MainMenuUI.CurrentRun;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        GameEvents.OnEnemyKilledXp += HandleXp;
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnDayStart += HandleDayStart;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilledXp -= HandleXp;
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnDayStart -= HandleDayStart;
    }

    // Herói novo (run nova, Continue): aplica o nível salvo sobre a base do prefab.
    private void HandleHeroSpawned(Transform hero)
    {
        if (Run == null) return;
        if (Run.level < 1) Run.level = 1;
        var controller = hero != null ? hero.GetComponent<HeroController>() : null;
        if (controller != null) controller.ApplyLevel(Run.level);
    }

    private void HandleDayStart()
    {
        if (Run != null) Run.goldFromXpToday = 0;
    }

    private void HandleXp(int xp)
    {
        if (Run == null || xp <= 0) return;

        var r = HeroLevelRules.AddXp(Run.level, Run.xpIntoLevel, xp, Run.day, Run.goldFromXpToday, GoldDailyLimit);
        Run.level = r.level;
        Run.xpIntoLevel = r.xpIntoLevel;
        Run.goldFromXpToday = r.goldConvertedToday;

        if (r.goldGained > 0)
        {
            Run.gold += r.goldGained;
            GameEvents.GoldChanged(Run.gold);
        }

        if (r.levelsGained > 0)
        {
            if (HeroController.Instance != null) HeroController.Instance.ApplyLevel(r.level);
            Debug.Log($"[HeroLevel] Subiu pro nível {r.level} (+{r.levelsGained}).");
            for (int i = r.level - r.levelsGained + 1; i <= r.level; i++) GameEvents.HeroLeveledUp(i);
        }
    }
}
