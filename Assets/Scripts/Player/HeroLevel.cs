using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.2) — XP e
// nível do herói. Mora no "Systems" (adicionado pelo SystemsBootstrap); o estado fica no RunState
// (zera na run nova).
// - XP vem das gemas coletadas (GameEvents.OnXpCollected, XpGemPool).
// - Subir de nível NÃO dá status: avisa GameEvents.HeroLeveledUp uma vez por nível ganho, e a
//   tela de cartas (HeroCards) abre uma oferta por nível.
public class HeroLevel : MonoBehaviour
{
    public static HeroLevel Instance { get; private set; }

    public int Level => Run != null ? Mathf.Max(1, Run.level) : 1;
    public long XpIntoLevel => Run != null ? Run.xpIntoLevel : 0;
    public long XpToNext => HeroLevelRules.XpToNext(Level);

    private static RunState Run => MainMenuUI.CurrentRun;

    private void Awake() => Instance = this;

    // Etapa 3: Scholar Tome/Greed (cartas XpGain) multiplicam o XP; a fração que sobra fica guardada.
    private float xpFraction;

    private void OnEnable() { GameEvents.OnXpCollected += HandleXp; GameEvents.OnDayStart += ResetFraction; }
    private void OnDisable() { GameEvents.OnXpCollected -= HandleXp; GameEvents.OnDayStart -= ResetFraction; }
    private void ResetFraction() => xpFraction = 0f;

    private void HandleXp(int xp)
    {
        if (Run == null || xp <= 0) return;
        float scaled = xp * HeroController.Cards.Get(CardAbility.None, CardStat.XpGain) + xpFraction;
        xp = Mathf.FloorToInt(scaled);
        xpFraction = scaled - xp;
        if (xp <= 0) return;

        var r = HeroLevelRules.AddXp(Level, Run.xpIntoLevel, xp);
        Run.level = r.level;
        Run.xpIntoLevel = r.xpIntoLevel;

        if (r.levelsGained <= 0) return;
        Debug.Log($"[HeroLevel] Subiu pro nível {r.level} (+{r.levelsGained}).");
        for (int i = r.level - r.levelsGained + 1; i <= r.level; i++) GameEvents.HeroLeveledUp(i);
    }
}
