using NUnit.Framework;

// D9.5 — tela "Resumo total" da morte: "Sobreviveu X dias" conta só os dias completos.
public class RunSummaryRulesTests
{
    [Test] public void DiedOnDay3_Survived2() => Assert.AreEqual(2, RunSummaryRules.DaysSurvived(3));
    [Test] public void DiedOnDay1_Survived0() => Assert.AreEqual(0, RunSummaryRules.DaysSurvived(1));
    [Test] public void InvalidDay_Survived0() => Assert.AreEqual(0, RunSummaryRules.DaysSurvived(0));
}
