[System.Serializable]
public class EnemyStats
{
    public float health = 30f;
    public float maxHealth = 30f;
    public float moveSpeed = 2f;
    public float observationRadius = 6f;
    public float attackRadius = 1.5f; // alcance de contato (Melee) ou de disparo/manutenção de distância (Ranged)

    // flankRadius saiu daqui — agora é geral, configurado direto no MeleeAttackSlotManager
    // (ver MeleeEnemyController.FlankRadius), não mais por monstro.

    // Único dano do jogo agora: o golpe/disparo real da animação de ataque, decidido por
    // Animation Event — testamos ter também um dano de contato passivo em paralelo
    // (Sprint 16, correção) e ficou confuso; removido de vez.
    public float attackDamage = 10f;
    public float attackAnimationCooldown = 2.5f;

    // 2026-11-01 — Deadline 8.1, IA de perseguição inteligente. Multiplica o lead time da
    // predição de movimento (EnemyController.GetSmartApproachPoint) — 0 = ignora a velocidade
    // do player, persegue a posição crua (burro de propósito); 1 = predição cheia. Default 1
    // pra todo mundo por agora; fica pronto pra, no futuro, diferenciar "inimigo de pressão"
    // (Rat/Bat/Slime, Bestiário Andar 1) de monstros mais espertos, sem precisar programar de
    // novo depois.
    public float predictionSkill = 1f;
}
