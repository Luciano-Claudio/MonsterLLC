using NUnit.Framework;

// D9.5 — tela "Resumo total" da morte: "Sobreviveu X dias" conta só os dias completos.
public class RunSummaryRulesTests
{
    [Test] public void DiedOnDay3_Survived2() => Assert.AreEqual(2, RunSummaryRules.DaysSurvived(3));
    [Test] public void DiedOnDay1_Survived0() => Assert.AreEqual(0, RunSummaryRules.DaysSurvived(1));
    [Test] public void InvalidDay_Survived0() => Assert.AreEqual(0, RunSummaryRules.DaysSurvived(0));

    // Etapas 1+2 — tempo sobrevivido no Resumo (mm:ss) e o resultado da run.
    [Test] public void Time_is_minutes_and_seconds() => Assert.AreEqual("12:05", RunSummaryRules.FormatTime(725.9f));
    [Test] public void Time_over_25_minutes_keeps_counting() => Assert.AreEqual("26:40", RunSummaryRules.FormatTime(1600f));
    [Test] public void Negative_time_is_zero() => Assert.AreEqual("00:00", RunSummaryRules.FormatTime(-3f));
    [Test] public void Result_text() { Assert.AreEqual("Vitória", RunSummaryRules.ResultText(true)); Assert.AreEqual("Derrota", RunSummaryRules.ResultText(false)); }
}
