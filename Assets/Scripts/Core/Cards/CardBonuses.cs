using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 (spec 6.4) — bônus agregados das cartas que o jogador tem, por (habilidade, atributo).
// Dano: multiplica (valuePerLevel^nível de cada carta). Cooldown: 1 - soma, com piso de 0,2. O resto:
// 1 + soma. Get() devolve sempre um multiplicador pronto pra aplicar em cima do valor base.
public class CardBonuses
{
    public const float CooldownFloor = 0.2f;

    private static readonly int AbilityCount = System.Enum.GetValues(typeof(CardAbility)).Length;
    private static readonly int StatCount = System.Enum.GetValues(typeof(CardStat)).Length;

    private readonly float[] damageMultiplier = new float[AbilityCount];
    private float healthMultiplier = 1f; // etapa 4 — upgrade geral de vida (multiplica a soma dos itens/Stats)
    private readonly float[] additive = new float[AbilityCount * StatCount];

    public CardBonuses() => Reset();

    public void Reset()
    {
        for (int i = 0; i < damageMultiplier.Length; i++) damageMultiplier[i] = 1f;
        healthMultiplier = 1f;
        System.Array.Clear(additive, 0, additive.Length);
    }

    public void Recompute(IReadOnlyList<CardDefinition> pool, IReadOnlyDictionary<string, int> owned)
    {
        Reset();
        if (pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var card = pool[i];
            int level = CardRules.LevelOf(owned, card);
            if (level <= 0) continue;
            int a = (int)card.ability;
            if (card.Multiplicative && card.stat == CardStat.MaxHealth) healthMultiplier *= Mathf.Pow(card.valuePerLevel, level);
            else if (card.Multiplicative) damageMultiplier[a] *= Mathf.Pow(card.valuePerLevel, level);
            else additive[a * StatCount + (int)card.stat] += card.baseValue + card.valuePerLevel * level;
            if (card.healthPerLevel != 0f) additive[(int)CardAbility.None * StatCount + (int)CardStat.MaxHealth] += card.healthPerLevel * level;
        }
    }

    // Etapa 3: o bônus geral (habilidade None — itens e Stats "de tudo") MULTIPLICA o da habilidade.
    public float Get(CardAbility ability, CardStat stat)
    {
        int a = (int)ability, n = (int)CardAbility.None;
        bool withGeneral = ability != CardAbility.None;
        if (stat == CardStat.Damage) return damageMultiplier[a] * (withGeneral ? damageMultiplier[n] : 1f);
        float sum = Sum(ability, stat);
        float general = withGeneral ? Sum(CardAbility.None, stat) : 0f;
        if (stat == CardStat.Cooldown) return Mathf.Max(CooldownFloor, (1f - sum) * (1f - general));
        // Catálogo de cartas — Colossus Heart: +tamanho do herói também é +área de todos os ataques.
        if (stat == CardStat.Area) return (1f + sum) * (1f + general) * (1f + Sum(CardAbility.None, CardStat.HeroSize));
        if (stat == CardStat.MaxHealth && ability == CardAbility.None) return (1f + sum) * healthMultiplier;
        return (1f + sum) * (1f + general);
    }

    // Só a parte da própria habilidade, sem o geral — para multiplicadores que se aplicam POR CIMA de
    // outro Get (ex.: dano do Shift somado ao golpe primário), senão o geral entraria duas vezes.
    public float GetOwn(CardAbility ability, CardStat stat)
    {
        if (stat == CardStat.Damage) return damageMultiplier[(int)ability];
        float sum = Sum(ability, stat);
        return stat == CardStat.Cooldown ? Mathf.Max(CooldownFloor, 1f - sum) : 1f + sum;
    }

    // Soma crua dos aditivos (sem o 1+): redução de dano, regeneração, espinhos, chance de explosão.
    public float Sum(CardAbility ability, CardStat stat) => additive[(int)ability * StatCount + (int)stat];
}
