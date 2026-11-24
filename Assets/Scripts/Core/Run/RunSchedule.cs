using UnityEngine;

// Etapas 1+2 — regras puras da run de 25 min (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md,
// 5.1-5.7). Responde, para qualquer segundo da run: qual boss já devia ter nascido, a mistura de
// tiers da horda, dia/noite, enrage, mini boss e relógio pausado. Sem alocação nas consultas.
public class RunSchedule
{
    private readonly int[] bossTiers;
    private readonly float interval, runSeconds, daySeconds, nightSeconds;
    private readonly float speedAfter, speedMult, damageAfter, damageMult, damageStepSeconds, damageStepPercent;
    private readonly float ramp, miniMin, miniMax;
    private readonly bool hasFinalBoss;

    // Trocas de tier (de -> para), começando no nascimento do último boss do tier "de".
    private readonly float[] transitionStart;
    private readonly int[] transitionFrom, transitionTo;
    private readonly int startTier;

    public float FinalBossSecond { get; }
    public bool HasFinalBoss => hasFinalBoss;

    public RunSchedule(int[] bossTiers, RunSettings s)
    {
        this.bossTiers = bossTiers != null ? (int[])bossTiers.Clone() : new int[0];
        interval = s.bossIntervalSeconds; runSeconds = s.runSeconds;
        daySeconds = s.daySeconds; nightSeconds = s.nightSeconds;
        speedAfter = s.enrageSpeedAfter; speedMult = s.enrageSpeedMultiplier;
        damageAfter = s.enrageDamageAfter; damageMult = s.enrageDamageMultiplier;
        damageStepSeconds = s.enrageDamageStepSeconds; damageStepPercent = s.enrageDamageStepPercent;
        ramp = s.tierRampSeconds; miniMin = s.miniBossMinDelay; miniMax = s.miniBossMaxDelay;

        int finalIndex = -1;
        for (int i = this.bossTiers.Length - 1; i >= 0; i--) if (this.bossTiers[i] == 10) { finalIndex = i; break; }
        hasFinalBoss = finalIndex >= 0;
        FinalBossSecond = hasFinalBoss ? BossSpawnSecond(finalIndex) : runSeconds;

        // Tiers de horda presentes (1-9; o T10 nunca entra como horda), em ordem crescente.
        var lastIndexOfTier = new int[11];
        for (int t = 0; t < 11; t++) lastIndexOfTier[t] = -1;
        for (int i = 0; i < this.bossTiers.Length; i++)
        {
            int t = this.bossTiers[i];
            if (t >= 1 && t <= 9) lastIndexOfTier[t] = i;
        }
        startTier = 1;
        for (int t = 1; t <= 9; t++) if (lastIndexOfTier[t] >= 0) { startTier = t; break; }

        int count = 0;
        var from = new int[9]; var to = new int[9]; var start = new float[9];
        int current = startTier;
        for (int t = current + 1; t <= 9; t++)
        {
            if (lastIndexOfTier[t] < 0) continue; // tier sem boss na lista é pulado
            from[count] = current; to[count] = t;
            start[count] = lastIndexOfTier[current] >= 0 ? BossSpawnSecond(lastIndexOfTier[current]) : 0f;
            if (count > 0 && start[count] < start[count - 1]) start[count] = start[count - 1]; // nunca volta no tempo
            count++; current = t;
        }
        transitionFrom = new int[count]; transitionTo = new int[count]; transitionStart = new float[count];
        System.Array.Copy(from, transitionFrom, count);
        System.Array.Copy(to, transitionTo, count);
        System.Array.Copy(start, transitionStart, count);
    }

    // Espaço N nasce no FIM do minuto N+1 (1:00, 2:00 ... 25:00): a run nunca começa com boss.
    // O Divine God (espaço 25) nasce aos 25:00, quando o relógio chega a 0 e fica parado.
    public float BossSpawnSecond(int slot) => (slot + 1) * interval;

    public int SlotsDueBy(float t)
    {
        if (bossTiers.Length == 0 || t < 0f || interval <= 0f) return 0;
        return Mathf.Min(bossTiers.Length, Mathf.FloorToInt(t / interval));
    }

    public int SlotTier(int slot) => slot >= 0 && slot < bossTiers.Length ? bossTiers[slot] : 0;
    public int SlotCount => bossTiers.Length;

    public bool IsNight(float t)
    {
        if (hasFinalBoss && t >= FinalBossSecond) return true;
        float cycle = daySeconds + nightSeconds;
        if (cycle <= 0f) return false;
        return Mathf.Repeat(t, cycle) >= daySeconds;
    }

    // Quantos tiers estão ativos (1 ou 2). shareA = parte do tierA; o tierB fica com o resto.
    // Uma troca nova que começa antes da anterior terminar conclui a anterior na hora.
    public int TierShares(float t, out int tierA, out float shareA, out int tierB)
    {
        int k = -1;
        for (int i = 0; i < transitionStart.Length; i++) if (transitionStart[i] <= t) k = i;
        if (k < 0) { tierA = startTier; shareA = 1f; tierB = startTier; return 1; }

        float elapsed = t - transitionStart[k];
        if (ramp <= 0f || elapsed >= ramp) { tierA = transitionTo[k]; shareA = 1f; tierB = tierA; return 1; }

        tierA = transitionFrom[k]; tierB = transitionTo[k];
        shareA = 1f - elapsed / ramp;
        return 2;
    }

    public int CurrentTier(float t)
    {
        int n = TierShares(t, out int a, out float shareA, out int b);
        return n == 1 || shareA >= 0.5f ? a : b;
    }

    public float EnrageSpeedMultiplier(float aliveSeconds) => aliveSeconds >= speedAfter ? speedMult : 1f;

    public float EnrageDamageMultiplier(float aliveSeconds)
    {
        if (aliveSeconds < damageAfter) return 1f;
        int steps = damageStepSeconds > 0f ? Mathf.FloorToInt((aliveSeconds - damageAfter) / damageStepSeconds) : 0;
        return damageMult * (1f + steps * damageStepPercent / 100f);
    }

    public bool ClockPaused(float t, bool finalBossAlive) => t >= runSeconds && finalBossAlive;

    public float MiniBossSecond(int slot, float random01) => BossSpawnSecond(slot) + Mathf.Lerp(miniMin, miniMax, random01);
}
