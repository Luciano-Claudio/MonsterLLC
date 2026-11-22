using UnityEngine;

// D9.5 — tela "Resumo total" da morte (decisão do usuário, 2026-11-21): "Sobreviveu X dias" conta
// só os dias completos — morreu no dia 3, sobreviveu 2; no dia 1, 0.
public static class RunSummaryRules
{
    public static int DaysSurvived(int dayOfDeath) => Mathf.Max(0, dayOfDeath - 1);
}
