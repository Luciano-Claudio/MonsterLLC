using UnityEngine;

// Ataque primário do Mage — regra do usuário (2026-11-25). Cada animação do attack_continuous do
// corpo (12 quadros) é 1 CICLO e dá o dano do Mage naquele momento. O fogo do attack hitbox casa
// quadro a quadro com o corpo:
//   1ª animação:  fire_start (quadros 1-4, sem dano) -> fire_continuous (dano)
//   do meio:      fire_continuous em loop nos 12 quadros (dano)
//   última:       fire_continuous (dano) -> fire_end (quadros 10-12, sem dano)
// Lasting Flames: +1 animação inteira por nível (nunca meia); nível 10 = infinito enquanto o botão
// estiver apertado. A decisão de parar é no início do quadro 10 — soltou depois disso, roda mais 1
// animação inteira (o fogo nunca é cortado no meio).
// O dano do ciclo é dividido em 3 pedaços iguais da parte de dano DAQUELA animação: em cada pedaço o
// monstro na chama leva 1 tick (na hora em que entra, se entrou no meio do pedaço).
public static class PrimaryDuration
{
    public const int InfiniteLevel = 10;
    public const int Infinite = int.MaxValue;
    public const int CycleFrames = 12;
    public const int FireStartFrames = 4;
    public const int FireEndFrames = 3;
    public const int PiecesPerCycle = 3;

    public static int ExtraCycles(int level) =>
        level >= InfiniteLevel ? Infinite : Mathf.Max(0, level);

    // completedCycles: animações que já chegaram ao quadro 10 (contando a atual).
    public static bool ShouldEnd(int completedCycles, int extraCycles, bool attackHeld) =>
        extraCycles == Infinite ? !attackHeld : completedCycles > extraCycles;

    // cycleIndex: 0 = 1ª animação. No infinito nunca se sabe antes — conta como "do meio".
    public static bool IsLastCycle(int cycleIndex, int extraCycles) =>
        extraCycles != Infinite && cycleIndex >= extraCycles;

    public static int DamageStartFrame(bool first) => first ? FireStartFrames : 0;

    public static int DamageFrames(bool first, bool last) =>
        (last ? CycleFrames - FireEndFrames : CycleFrames) - DamageStartFrame(first);

    public static int Piece(float elapsedInWindow, float windowSeconds)
    {
        if (windowSeconds <= 0f) return 0;
        int piece = Mathf.FloorToInt(elapsedInWindow / (windowSeconds / PiecesPerCycle));
        return Mathf.Clamp(piece, 0, PiecesPerCycle - 1);
    }
}
