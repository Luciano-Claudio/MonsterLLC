[System.Serializable]
public class EnemyStats
{
    public float health = 30f;
    public float maxHealth = 30f;
    public float moveSpeed = 2f;
    public float observationRadius = 6f;
    public float attackRadius = 1.5f; // alcance de contato (Melee) ou de disparo/manutenção de distância (Ranged)

    // Único dano do jogo agora: o golpe/disparo real da animação de ataque, decidido por
    // Animation Event — testamos ter também um dano de contato passivo em paralelo
    // (Sprint 16, correção) e ficou confuso; removido de vez.
    public float attackDamage = 10f;
    public float attackAnimationCooldown = 2.5f;
}
