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

    public static PetSummonStep Next(bool pending, bool heroBusy, int missing)
    {
        if (!pending) return PetSummonStep.Idle;
        if (missing <= 0) return PetSummonStep.Done;
        return heroBusy ? PetSummonStep.Wait : PetSummonStep.StartSummon;
    }
}
