using System.Collections.Generic;
using UnityEngine;

// D9.1 (Assassin, decisão do usuário): enquanto os projéteis do ataque do Assassin estão no ar,
// os monstros (bosses também) perseguem o projétil MAIS PERTO DELES e, quando o projétil volta
// (Unregister), voltam a perseguir o Assassin. Registro global (só existe 1 herói jogável por
// vez, mesmo critério do HeroController.IsPlayerUntargetable). Só MOVIMENTO usa isto — ataque,
// mira e aggro continuam no herói (EnemyController).
public static class HeroDecoys
{
    private static readonly List<Transform> decoys = new List<Transform>();

    // Incrementa a cada Register — útil pra quem quiser reagir a "nasceu isca nova".
    public static int Version { get; private set; }

    public static int Count
    {
        get { PruneDestroyed(); return decoys.Count; }
    }

    public static void Register(Transform decoy)
    {
        if (decoy == null || decoys.Contains(decoy)) return;
        decoys.Add(decoy);
        Version++;
    }

    public static void Unregister(Transform decoy) => decoys.Remove(decoy);

    // Troca de andar / reset de run / morte do herói.
    public static void Clear() => decoys.Clear();

    public static bool IsActive(Transform decoy) => decoy != null && decoys.Contains(decoy);

    // Isca viva mais perto de `from`; null sem nenhuma. Poucas iscas (1 por projétil do
    // ataque), então varredura linear sem alocar.
    public static Transform PickNearest(Vector2 from)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        for (int i = decoys.Count - 1; i >= 0; i--)
        {
            var decoy = decoys[i];
            if (decoy == null) { decoys.RemoveAt(i); continue; } // destruída sem Unregister
            float sqr = ((Vector2)decoy.position - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = decoy; }
        }
        return best;
    }

    private static void PruneDestroyed()
    {
        for (int i = decoys.Count - 1; i >= 0; i--)
            if (decoys[i] == null) decoys.RemoveAt(i);
    }
}
