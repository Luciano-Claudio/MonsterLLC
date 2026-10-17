using NUnit.Framework;

public class WorldTierSelectorTests
{
    // ---- ComputeEffectiveSpawnDay (Seção 8.2: DistanceDayOffset + clamp 1..30) ----

    [Test]
    public void ComputeEffectiveSpawnDay_WithinSafeZone_ReturnsActualDayUnchanged()
    {
        int day = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter: 5f, safeZoneRadius: 12f, distancePerSpawnDay: 100f, actualDay: 3);

        Assert.AreEqual(3, day);
    }

    [Test]
    public void ComputeEffectiveSpawnDay_ExactlyOneStepBeyondSafeZone_AddsOneDay()
    {
        // safeZoneRadius=12, distancePerSpawnDay=100 -> distância 112 = 100 além da safe zone = +1 dia.
        int day = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter: 112f, safeZoneRadius: 12f, distancePerSpawnDay: 100f, actualDay: 3);

        Assert.AreEqual(4, day);
    }

    [Test]
    public void ComputeEffectiveSpawnDay_PartialStep_FloorsDown()
    {
        // 99 além da safe zone, menos que distancePerSpawnDay=100 -> ainda não completou 1 passo.
        int day = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter: 12f + 99f, safeZoneRadius: 12f, distancePerSpawnDay: 100f, actualDay: 3);

        Assert.AreEqual(3, day);
    }

    [Test]
    public void ComputeEffectiveSpawnDay_ClampsAtMax30()
    {
        int day = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter: 100000f, safeZoneRadius: 0f, distancePerSpawnDay: 100f, actualDay: 1);

        Assert.AreEqual(30, day);
    }

    [Test]
    public void ComputeEffectiveSpawnDay_DistancePerSpawnDayZero_NeverAddsOffset()
    {
        int day = WorldTierSelector.ComputeEffectiveSpawnDay(
            distanceFromCenter: 5000f, safeZoneRadius: 0f, distancePerSpawnDay: 0f, actualDay: 7);

        Assert.AreEqual(7, day);
    }

    // ---- ComputeTierGroupPercents (agrupa T1..T10 em 6 grupos + piso de 20%) ----

    [Test]
    public void ComputeTierGroupPercents_Day1_OnlyTier1Active()
    {
        var day1 = new DaySpawnDefinition(1, new float[] { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new float[10]);

        var percents = WorldTierSelector.ComputeTierGroupPercents(day1);

        Assert.AreEqual(1f, percents[0], 0.0001f);
        for (int g = 1; g < 6; g++) Assert.AreEqual(0f, percents[g], 0.0001f);
    }

    [Test]
    public void ComputeTierGroupPercents_UsesStrongestTierInGroup_NotTheSum()
    {
        // Tier6-8 cobre T6,T7,T8 (índices 5,6,7). T6=10, T7=15, T8=5 -> grupo usa o MAIOR
        // isoladamente (15%), nunca a soma (30%) — cada tier de monstro compete sozinho pelo
        // piso/dominância, somar tiers de um mesmo grupo faria um grupo de 3 tiers fracos
        // vencer artificialmente um grupo de 1 tier forte (cenário real: Dias 26-28 da tabela).
        var weights = new float[] { 5, 5, 5, 5, 5, 10, 15, 5, 25, 20 }; // soma 100
        var dayDef = new DaySpawnDefinition(12, weights, new float[10]);

        var percents = WorldTierSelector.ComputeTierGroupPercents(dayDef);

        // Tier6-8 (bruto 15%) < Tier9 (25%) < Tier10 (20%)... na verdade Tier9=25 é o maior
        // isolado; depois do piso e renormalização: Tier6-8=0 (15%<20%, abaixo do piso),
        // Tier9=25/(25+20)=0.5556, Tier10=20/(25+20)=0.4444.
        Assert.AreEqual(0f, percents[3], 0.0001f); // Tier6-8 abaixo do piso (15% isolado, não 30% somado)
        Assert.AreEqual(25f / 45f, percents[4], 0.0001f); // Tier9
        Assert.AreEqual(20f / 45f, percents[5], 0.0001f); // Tier10
    }

    [Test]
    public void ComputeTierGroupPercents_OneStrongTierBeatsTwoWeakerTiersInAnotherGroup()
    {
        // Exemplo do usuário: um tier isolado com 40% deve prevalecer sobre outro grupo cuja
        // soma dá 60% mas cujo tier individual mais forte é só 35% (T4=35,T5=25 -> Tier4-5
        // usa 35, não 60) — Tier9 (T9=40, isolado) tem mais peso isolado que isso.
        var weights = new float[] { 0, 0, 0, 35, 25, 0, 0, 0, 40, 0 }; // soma 100
        var dayDef = new DaySpawnDefinition(1, weights, new float[10]);

        var percents = WorldTierSelector.ComputeTierGroupPercents(dayDef);

        // Tier4-5 = max(35,25) = 0.35; Tier9 = 0.40 -> ambos passam o piso de 20%.
        // Renormalizado: Tier4-5 = 0.35/0.75, Tier9 = 0.40/0.75 -> Tier9 > Tier4-5.
        Assert.AreEqual(0.35f / 0.75f, percents[2], 0.0001f); // Tier4-5
        Assert.AreEqual(0.40f / 0.75f, percents[4], 0.0001f); // Tier9
        Assert.Greater(percents[4], percents[2]); // o tier isolado de 40% domina sobre a soma de 60%
    }

    [Test]
    public void ComputeTierGroupPercents_AllGroupsAboveFloor_SumsToOneUnchanged()
    {
        // 6 grupos, cada um com >=20% isoladamente, exceto Tier9: T1=20,T3=20,T5=20,T7=15,T9=10.
        // Tier6-8 usa o maior isolado entre T6,T7,T8 = 15 (não soma) -> abaixo do piso também.
        var weights = new float[] { 20, 0, 20, 0, 20, 0, 15, 0, 10, 0 }; // T1=20,T3=20,T5=20,T7=15,T9=10
        var dayDef = new DaySpawnDefinition(1, weights, new float[10]);

        var percents = WorldTierSelector.ComputeTierGroupPercents(dayDef);

        // Tier1=0.20, Tier2-3(max(0,20)=20)=0.20, Tier4-5(max(0,20)=20)=0.20, Tier6-8(max(0,15,0)=15)->abaixo do piso,
        // Tier9(10)->abaixo do piso. Soma ativa = 0.20*3 = 0.60 -> cada um dividido por 0.60 -> 1 cada.
        float sum = 0f;
        foreach (var p in percents) sum += p;
        Assert.AreEqual(1f, sum, 0.0001f);
        Assert.AreEqual(0f, percents[3], 0.0001f); // Tier6-8 abaixo do piso (15% isolado)
        Assert.AreEqual(0f, percents[4], 0.0001f); // Tier9 abaixo do piso
    }

    [Test]
    public void ComputeTierGroupPercents_GroupBelowFloor_IsZeroedAndRedistributed()
    {
        // Tier1=15% (abaixo do piso de 20%) e Tier2-3=85% (T2=85,T3=0) -> Tier1 some,
        // Tier2-3 vira 100% sozinho.
        var weights = new float[] { 15, 85, 0, 0, 0, 0, 0, 0, 0, 0 };
        var dayDef = new DaySpawnDefinition(1, weights, new float[10]);

        var percents = WorldTierSelector.ComputeTierGroupPercents(dayDef);

        Assert.AreEqual(0f, percents[0], 0.0001f);
        Assert.AreEqual(1f, percents[1], 0.0001f);
    }

    [Test]
    public void ComputeTierGroupPercentsNonAlloc_MatchesAllocatingVersion()
    {
        var weights = new float[] { 5, 5, 5, 5, 5, 10, 15, 5, 25, 20 };
        var dayDef = new DaySpawnDefinition(12, weights, new float[10]);
        var buffer = new float[WorldTierSelector.TierGroupCount];

        var expected = WorldTierSelector.ComputeTierGroupPercents(dayDef);
        WorldTierSelector.ComputeTierGroupPercentsNonAlloc(dayDef, buffer);

        CollectionAssert.AreEqual(expected, buffer);
    }

    [Test]
    public void ComputeTierGroupPercents_NullDayDefinition_ReturnsAllZeros()
    {
        var percents = WorldTierSelector.ComputeTierGroupPercents(null);

        Assert.AreEqual(WorldTierSelector.TierGroupCount, percents.Length);
        foreach (var p in percents) Assert.AreEqual(0f, p);
    }

    // ---- ChooseActiveTierGroup (seleção por célula, cumulativa sobre ruído coerente) ----

    [Test]
    public void ChooseActiveTierGroup_OnlyFirstGroupActive_AlwaysReturnsZero()
    {
        var percents = new float[] { 1f, 0f, 0f, 0f, 0f, 0f };

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Assert.AreEqual(0, WorldTierSelector.ChooseActiveTierGroup(x, y, 0f, 0f, 0.05f, percents));
            }
        }
    }

    [Test]
    public void ChooseActiveTierGroup_OnlyLastGroupActive_AlwaysReturnsLastIndex()
    {
        var percents = new float[] { 0f, 0f, 0f, 0f, 0f, 1f };

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Assert.AreEqual(5, WorldTierSelector.ChooseActiveTierGroup(x, y, 0f, 0f, 0.05f, percents));
            }
        }
    }

    [Test]
    public void ChooseActiveTierGroup_AlwaysReturnsValidIndex()
    {
        var percents = new float[] { 0.1f, 0.1f, 0.2f, 0.2f, 0.2f, 0.2f };

        for (int x = 0; x < 25; x++)
        {
            for (int y = 0; y < 25; y++)
            {
                int group = WorldTierSelector.ChooseActiveTierGroup(x, y, 0f, 0f, 0.05f, percents);
                Assert.GreaterOrEqual(group, 0);
                Assert.Less(group, WorldTierSelector.TierGroupCount);
            }
        }
    }

    [Test]
    public void ChooseActiveTierGroup_SameInputs_AlwaysReturnsSameResult()
    {
        var percents = new float[] { 0.3f, 0.3f, 0.4f, 0f, 0f, 0f };

        int first = WorldTierSelector.ChooseActiveTierGroup(12, -7, 0f, 0f, 0.05f, percents);
        int second = WorldTierSelector.ChooseActiveTierGroup(12, -7, 0f, 0f, 0.05f, percents);

        Assert.AreEqual(first, second);
    }
}
