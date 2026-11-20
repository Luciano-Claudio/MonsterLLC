using System.Collections.Generic;

// D9.3 — regras puras do corte de custo da horda (medido no D9.2: com 300 monstros, os scripts
// dos monstros eram o gargalo nº 1 e geravam ~265 KB de lixo por frame).

// Espécie do monstro como número, calculada 1x por instância. Antes, a coesão de bando lia
// go.name (string nova a cada leitura) e fazia Substring, 2x por vizinho, por monstro, todo
// frame — era a principal fonte do lixo de memória. O projeto não tem campo de espécie: o nome
// do prefab ("Rat" -> "Rat(Clone)") é a espécie, mesma heurística de antes.
public static class SpeciesRegistry
{
    private const string CloneSuffix = "(Clone)";
    private static readonly Dictionary<string, int> Ids = new Dictionary<string, int>();

    public static int IdFor(string gameObjectName)
    {
        string key = gameObjectName.EndsWith(CloneSuffix)
            ? gameObjectName.Substring(0, gameObjectName.Length - CloneSuffix.Length)
            : gameObjectName;
        if (!Ids.TryGetValue(key, out int id))
        {
            id = Ids.Count + 1;
            Ids.Add(key, id);
        }
        return id;
    }
}

// Disparo periódico com fase inicial sorteada — sem a fase, todo monstro nascido no mesmo frame
// faria a consulta cara no mesmo frame, e o custo voltaria em picos em vez de diluído.
// Depois de uma pausa longa, dispara 1x só (sem rajada de ticks atrasados).
public struct IntervalTimer
{
    private readonly float interval;
    private float nextTime;

    public IntervalTimer(float interval, float now, float startPhase01)
    {
        this.interval = interval;
        nextTime = now + interval * startPhase01;
    }

    public bool TryTick(float now)
    {
        if (now < nextTime) return false;
        nextTime = now + interval;
        return true;
    }
}

// Simulation Rings (D9.3): "simulação completa" (IA todo frame + RVO ligado) só perto do herói.
// Antes, todo monstro em combate contava como perto, então os 300 da horda ficavam com RVO e IA
// todo frame. Agora o monstro em combate longe (fora da tela) decide com menos frequência —
// ele continua andando até o último destino nesse meio tempo. Boss sempre em full: tem
// telegraph/arena e é 1 só.
public static class SimulationRingRules
{
    public static bool IsFullSimulation(float distanceToPlayer, bool inCombat, bool isBoss,
        float nearRingRadius, float combatNearRingRadius)
    {
        if (isBoss && inCombat) return true;
        return distanceToPlayer <= (inCombat ? combatNearRingRadius : nearRingRadius);
    }
}
