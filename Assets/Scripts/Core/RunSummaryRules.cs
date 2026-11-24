using UnityEngine;

// D9.5 — tela "Resumo total" da morte (decisão do usuário, 2026-11-21): "Sobreviveu X dias" conta
// só os dias completos — morreu no dia 3, sobreviveu 2; no dia 1, 0.
public static class RunSummaryRules
{
    public static int DaysSurvived(int dayOfDeath) => Mathf.Max(0, dayOfDeath - 1);

    // Etapas 1+2 — tempo sobrevivido (mm:ss, segundos inteiros) e o resultado da run.
    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    public static string ResultText(bool victory) => victory ? "Vitória" : "Derrota";
}
