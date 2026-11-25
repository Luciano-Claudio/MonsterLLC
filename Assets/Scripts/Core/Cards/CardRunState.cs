// Catálogo de cartas — estado das cartas que muda DURANTE a run (contadores, pilhas, buffs, recargas).
// Estático (só existe 1 herói); zerado em todo começo de run (CardRunStateReset, OnDayStart), porque o
// domain reload está desligado e o valor de uma run vazaria para a próxima.
public static class CardRunState
{
    public static int Kills;              // Endless Hunger, Titan's Tribute
    public static int KillsSinceDamage;   // Unbroken
    public static int BossesKilled;       // Trophy Hunter, Survivor's Legacy
    public static int FeverStacks;        // Critical Fever
    public static float FeverRemaining;
    public static float NextPrimaryBonus; // Relentless Momentum + Crowd Breaker pendentes
    public static float NextPrimaryArea;  // Relentless Momentum (área)
    public static float PendingPrimaryBonus; // bônus guardado para o PRÓXIMO ataque primário
    public static float PendingPrimaryArea;
    public static int AttackTargets;      // inimigos atingidos no ataque primário atual (Crowd Breaker)
    public static object FocusTarget;     // Predator's Focus (EnemyController, guardado como object no Core)
    public static int FocusStacks;
    public static int DotTicks;           // Affliction Heart / Affliction Harvest
    public static int PrimaryHits;        // Echo Fang
    public static float LastEmberTime = float.NegativeInfinity; // Last Ember (tempo da run em que disparou)
    public static float MovingSeconds;    // Relentless Momentum / Moving Fortress
    public static int ScarStacks;         // Scars of Battle
    public static float ScarRemaining;
    public static float BloodFrenzyRemaining;
    public static float SpeedBuffRemaining;   // Golden Momentum
    public static float AdrenalineRemaining;
    public static float AdrenalineCooldown;
    public static float WellspringCooldown;
    public static float ThornCooldown;
    public static float KillHealWindowStart;  // Reaper's Feast (limite por segundo)
    public static float KillHealInWindow;
    public static float RelentlessCastingWindowStart;
    public static float RelentlessCastingInWindow;
    public static bool InEcho;            // guarda de reentrada (Echo Fang, Shattered Fortune, Overkill)
    public static float RunSeconds;       // tempo de jogo da run (Last Ember)
    public static float EmberInvulnerableRemaining;
    public static float ExtraReduction;   // Gravebound + Final Stand + Scars + Moving Fortress (recalculado a cada frame)
    public static bool RelentlessArmed;   // Relentless Momentum já guardou o bônus nesta caminhada

    public static void Reset()
    {
        Kills = 0; KillsSinceDamage = 0; BossesKilled = 0;
        FeverStacks = 0; FeverRemaining = 0f;
        NextPrimaryBonus = 0f; NextPrimaryArea = 0f; PendingPrimaryBonus = 0f; PendingPrimaryArea = 0f; AttackTargets = 0;
        FocusTarget = null; FocusStacks = 0;
        DotTicks = 0; PrimaryHits = 0;
        LastEmberTime = float.NegativeInfinity;
        MovingSeconds = 0f;
        ScarStacks = 0; ScarRemaining = 0f;
        BloodFrenzyRemaining = 0f; SpeedBuffRemaining = 0f;
        AdrenalineRemaining = 0f; AdrenalineCooldown = 0f;
        WellspringCooldown = 0f; ThornCooldown = 0f;
        KillHealWindowStart = 0f; KillHealInWindow = 0f;
        RelentlessCastingWindowStart = 0f; RelentlessCastingInWindow = 0f;
        InEcho = false;
        RunSeconds = 0f; EmberInvulnerableRemaining = 0f; ExtraReduction = 0f; RelentlessArmed = false;
    }
}
