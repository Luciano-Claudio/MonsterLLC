using UnityEngine;

public enum PetSummonStep { Idle, Wait, StartSummon, Done }

// Regras do PetSummoner (D9.1, 2026-11-19, decisão do usuário): "tem pet?" (bool) -> "tem a
// quantidade x em jogo?" -> se não, invoca 1 pet por vez (1 animação de invocar por pet que
// falta), esperando o herói ficar livre. Só roda quando alguém pede a conferência (escolha da
// variante do herói, início do dia) — nunca destrói pet que já está em jogo.
public static class PetSummonRules
{
    public static int Missing(bool hasPet, int wanted, int alive) =>
        hasPet ? Mathf.Max(0, wanted - alive) : 0;

    // Sorteia o ponto de nascimento do próximo pet entre os pontos LIVRES (nenhum pet vivo
    // nascido nele); com todos ocupados (mais pets que pontos), sorteia entre todos. random01
    // em [0,1) — quem chama passa Random.value. -1 sem nenhum ponto.
    public static int PickSpawnIndex(System.Collections.Generic.IReadOnlyList<bool> occupied, float random01)
    {
        int count = occupied.Count;
        if (count == 0) return -1;
        int free = 0;
        for (int i = 0; i < count; i++) if (!occupied[i]) free++;

        int pool = free > 0 ? free : count;
        int pick = Mathf.Min(pool - 1, Mathf.FloorToInt(Mathf.Clamp01(random01) * pool));
        for (int i = 0; i < count; i++)
        {
            if (free > 0 && occupied[i]) continue;
            if (pick == 0) return i;
            pick--;
        }
        return count - 1;
    }

    // Herói "ocupado" pra invocação: em ação, no Shift, preso (a invocação disparava o
    // SummonPetTrigger e travava isAttacking durante a Prisão — revisão final do D9.1) ou morto.
    public static bool HeroBusy(bool isAttacking, bool isUsingSecondaryAbility, bool isTrapped, bool isDead) =>
        isAttacking || isUsingSecondaryAbility || isTrapped || isDead;

    public static PetSummonStep Next(bool pending, bool heroBusy, int missing)
    {
        if (!pending) return PetSummonStep.Idle;
        if (missing <= 0) return PetSummonStep.Done;
        return heroBusy ? PetSummonStep.Wait : PetSummonStep.StartSummon;
    }
}
