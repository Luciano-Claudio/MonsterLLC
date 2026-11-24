using System.Collections.Generic;
using UnityEngine;

// Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.2) — item Blast
// Charm: ao morrer, o inimigo tem chance (soma das cartas BlastChance) de explodir, causando em área
// (RunSettings.blastRadius) dano = dano do ataque primário do herói × blastDamagePercent.
// Mora no "Systems" (SystemsBootstrap). Explosão não encadeia: inimigo morto por explosão não explode.
public class BlastCharmEffect : MonoBehaviour
{
    private readonly Collider2D[] results = new Collider2D[128];
    private readonly List<EnemyController> targets = new();
    private bool exploding;

    private void OnEnable() => GameEvents.OnEnemyKilledDetailed += HandleEnemyKilled;
    private void OnDisable() => GameEvents.OnEnemyKilledDetailed -= HandleEnemyKilled;

    private void HandleEnemyKilled(string enemyName, bool isBoss, int directHits, int dotTicks, float maxHealth, int energyReward, Vector3 position)
    {
        if (exploding) return; // morto pela própria explosão
        float chance = HeroController.Cards.Sum(CardAbility.None, CardStat.BlastChance);
        if (chance <= 0f || Random.value >= chance) return;
        Explode(position);
    }

    public void Explode(Vector3 position)
    {
        var hero = HeroController.Instance;
        if (hero == null) return;
        var settings = RunSettings.Current;
        float damage = hero.EffectsBaseDamage * settings.blastDamagePercent / 100f;
        if (damage <= 0f) return;

        int count = Physics2D.OverlapCircle(position, settings.blastRadius, ContactFilter2D.noFilter, results);
        EnemyController.CollectDistinct(results, count, targets);
        exploding = true;
        try
        {
            foreach (var enemy in targets)
                if (enemy != null && !enemy.IsDead) enemy.TakeDamage(damage, false);
        }
        finally { exploding = false; }
    }
}
