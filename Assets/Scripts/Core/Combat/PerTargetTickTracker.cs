using System.Collections.Generic;

// Dano por tick contado POR MONSTRO (decisão do usuário, 2026-11-19 — Mage, depois o Rogue):
// o monstro leva dano na hora em que entra na área pela 1ª vez e de novo a cada `interval`
// enquanto continuar dentro. Diferente do tick por ÁREA (timer único — quem entra entre 2
// ticks espera o próximo, "entrou e não levou dano"). Sair e entrar de novo NÃO zera o timer
// do monstro (senão dava pra abusar entrando/saindo). O registro vive entre ativações
// seguidas; Prune() descarta quem já pode levar hit de novo (equivale a não ter registro).
public class PerTargetTickTracker
{
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>();
    private readonly List<int> pruneBuffer = new List<int>();
    private float interval;

    public PerTargetTickTracker(float interval) { this.interval = interval; }

    public int Count => lastHitTime.Count;

    public void SetInterval(float newInterval) { interval = newInterval; }

    // targetId = GetInstanceID() do monstro. True = aplica o dano agora (e registra o horário).
    public bool TryHit(int targetId, float now)
    {
        if (lastHitTime.TryGetValue(targetId, out float last) && now - last < interval) return false;
        lastHitTime[targetId] = now;
        return true;
    }

    public void Prune(float now)
    {
        pruneBuffer.Clear();
        foreach (var entry in lastHitTime)
            if (now - entry.Value >= interval) pruneBuffer.Add(entry.Key);
        foreach (int id in pruneBuffer) lastHitTime.Remove(id);
    }
}
