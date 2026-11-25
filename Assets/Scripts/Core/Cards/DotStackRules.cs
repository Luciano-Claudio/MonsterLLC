using System.Collections.Generic;

// Catálogo de cartas (docs/superpowers/specs/2026-11-25-catalogo-cartas-design.md) — pilhas de efeito de
// status. Cada aplicação é uma pilha com duração e dps próprios. Com o limite cheio, a nova substitui a
// MAIS ANTIGA do mesmo tipo (com limite 1, isso é exatamente o "renova duração/dano" de antes).
public class StatusStack
{
    public StatusEffectType type;
    public float remaining;
    public float baseDuration;    // duração antes dos bônus (Plague Herald repassa a duração cheia)
    public float damagePerSecond;
    public long order;            // ordem de aplicação (menor = mais antiga)
    public AttackCooldown tick;
}

public static class DotStackRules
{
    // Blight Crucible: nível 0 = 1 pilha; nível 1 = 2; nível 2+ = 3.
    public static int MaxStacks(int blightLevel) => blightLevel <= 0 ? 1 : blightLevel == 1 ? 2 : 3;

    // Blight Crucible a partir do nível 3: +10% de dano de DOT por nível.
    public static float DotDamageMultiplier(int blightLevel) => blightLevel >= 3 ? 1f + 0.10f * (blightLevel - 2) : 1f;

    public static StatusStack Apply(List<StatusStack> stacks, StatusEffectType type, float duration, float baseDuration,
        float damagePerSecond, int maxStacks, ref long order, float tickInterval)
    {
        int count = 0;
        StatusStack oldest = null;
        for (int i = 0; i < stacks.Count; i++)
        {
            var s = stacks[i];
            if (s.type != type) continue;
            count++;
            if (oldest == null || s.order < oldest.order) oldest = s;
        }

        order++;
        if (count >= System.Math.Max(1, maxStacks) && oldest != null)
        {
            oldest.remaining = duration;
            oldest.baseDuration = baseDuration;
            oldest.damagePerSecond = damagePerSecond;
            oldest.order = order;
            return oldest;
        }

        var stack = new StatusStack
        {
            type = type, remaining = duration, baseDuration = baseDuration, damagePerSecond = damagePerSecond,
            order = order, tick = new AttackCooldown(tickInterval),
        };
        stacks.Add(stack);
        return stack;
    }

    // Passa o tempo e remove as pilhas que acabaram (cada uma no seu tempo).
    public static void Advance(List<StatusStack> stacks, float deltaTime)
    {
        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            stacks[i].remaining -= deltaTime;
            if (stacks[i].remaining <= 0f) stacks.RemoveAt(i);
        }
    }
}

// Modificadores de DOT das cartas do herói (preenchidos pelo DotCardEffects a cada frame). Só valem
// para quem marcou UseHeroDotModifiers (inimigos) — DOT que o herói recebe não muda.
public static class DotModifiers
{
    public static int MaxStacks = 1;
    public static float DurationMultiplier = 1f;
    public static float DamageMultiplier = 1f;
    public static float CritChance;
    public static float CritMultiplier = 2f;

    public static void Reset()
    {
        MaxStacks = 1; DurationMultiplier = 1f; DamageMultiplier = 1f; CritChance = 0f; CritMultiplier = 2f;
    }
}
