using UnityEngine;

// Script genérico de projétil de monstro — qualquer Ranged usa esse mesmo componente no
// prefab do próprio projétil (RangedEnemyController chama Launch() no Animation Event de
// disparo). 3 comportamentos configuráveis por prefab (impactMode), do mais simples ao mais
// completo — mesma arquitetura de fases já usada em MageFireball/RangerKnife (voo -> impacto
// sem rotação -> [opcional] área persistente no chão), só que do lado do monstro: quem sofre
// o hit/a área é sempre o Player, nunca outro monstro (só existe 1 Player no jogo).
public class EnemyProjectile : MonoBehaviour
{
    private enum Phase { Flying, Impacting, Grounded, Disappearing }

    public enum ImpactMode
    {
        Simple,          // Comportamento 1 — some na hora, ao acertar o player ou ao chegar no limite da trajetória.
        ImpactAnimation, // Comportamento 2 — igual ao 1, mas toca uma animação de impacto (rotação travada em Quaternion.identity) antes de sumir de verdade.
        ImpactSurface    // Comportamento 3 — igual ao 2, e depois da animação de impacto vira uma área persistente no chão.
    }

    [Header("Voo")]
    public float speed = 6f;
    public float damage = 5f;
    public float lifetime = 3f;
    public FloorDefinition ownerFloor;

    [Header("Comportamento no impacto/limite")]
    [SerializeField] private ImpactMode impactMode = ImpactMode.Simple;

    // Efeito Nocivo opcional (ex.: a tocha do Goblin Raider aplica Fire) — aplicado tanto no
    // hit direto de voo quanto em cada tick da área persistente (se o modo for
    // ImpactSurface), mesmo padrão já usado na Ultimate do Ranger (RangerKnife: hit em voo +
    // tick no chão aplicam o mesmo Bleeding).
    [Header("Efeito Nocivo (opcional — hit direto e/ou tick da área)")]
    [SerializeField] private bool appliesStatusEffect = false;
    [SerializeField] private StatusEffectType statusEffectType;
    [SerializeField] private float statusEffectDuration = 5f; // 🔢 ajustável
    [SerializeField] private float statusEffectDamagePerSecond = 2f; // 🔢 ajustável

    // Sprint 20 assumiu que isso já existia como comportamento padrão (Rat People "explode em
    // área ao contato ou na distância máxima") — checado na prática (Goblin Raider, mesma
    // classe) e não existia: sem isso, o impacto só aplicava dano direto no que tocou o
    // trigger, nunca uma explosão de verdade. Correção isolada (Seção 0, item 2 da sprint):
    // burst instantâneo no ponto de impacto, uma vez só, seja o impacto por contato direto ou
    // por alcançar o limite da trajetória sem acertar ninguém.
    [Header("Explosão em área ao impacto (opcional)")]
    [SerializeField] private bool explodesOnImpact = false;
    [SerializeField] private float explosionRadius = 1.5f; // 🔢 ajustável

    [Header("Área persistente (só ImpactSurface)")]
    [SerializeField] private CircleCollider2D circleCollider; // mesmo collider do voo, redimensionado ao virar área
    [SerializeField] private float groundedRadius = 1.2f; // 🔢 ajustável
    [SerializeField] private float groundedDuration = 4f; // 🔢 ajustável
    [SerializeField] private float groundedTickInterval = 1f; // 🔢 ajustável
    [SerializeField] private float groundedDamagePerSecond = 2f; // 🔢 ajustável

    private Animator animator;
    private Vector2 direction;
    private float timer;
    private Phase phase = Phase.Flying;
    private float groundedElapsed;
    private AttackCooldown groundedTick;
    private HeroController heroInGroundedArea; // só existe 1 Player no jogo — referência única, sem lista

    private void Awake()
    {
        animator = GetComponent<Animator>(); // pode não existir (ImpactMode.Simple não precisa de Animator)
    }

    public void Launch(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — por isso o -90°: sem
        // ele, ângulo 0 (Leste) deixaria o sprite ainda apontando pra cima em vez de deitado
        // na horizontal.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;

        if (phase == Phase.Flying) UpdateFlying();
        else if (phase == Phase.Grounded) UpdateGrounded();
    }

    private void UpdateFlying()
    {
        // position direto (espaço de mundo), não Translate — Translate usa espaço LOCAL por
        // padrão, e com o projétil rotacionado os eixos locais giram junto, fazendo
        // "direction" (um vetor de mundo) apontar pro lado errado.
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) ReachedLimit();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (phase == Phase.Flying)
        {
            // Se explode em área, o dano vem só do burst (ApplyImpactExplosion, chamado dentro
            // de HitPlayer) — não aplica dano direto aqui também, senão o player levaria hit
            // duplo (contato + explosão) no mesmo instante.
            if (!explodesOnImpact)
            {
                var hero = other.GetComponent<HeroController>();
                if (hero != null)
                {
                    hero.TakeDamage(damage);
                    if (appliesStatusEffect) ApplyStatus(hero);
                }
            }
            HitPlayer();
            return;
        }

        if (phase == Phase.Grounded)
        {
            var hero = other.GetComponent<HeroController>();
            if (hero != null) heroInGroundedArea = hero;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (phase != Phase.Grounded) return;
        if (!other.CompareTag("Player")) return;

        var hero = other.GetComponent<HeroController>();
        if (hero != null && hero == heroInGroundedArea) heroInGroundedArea = null;
    }

    private void ApplyStatus(HeroController hero)
    {
        var statusController = hero.GetComponent<StatusEffectController>();
        if (statusController != null) statusController.ApplyStatusEffect(statusEffectType, statusEffectDuration, statusEffectDamagePerSecond);
    }

    // Chegou no fim da trajetória (lifetime) sem acertar ninguém.
    private void ReachedLimit()
    {
        if (explodesOnImpact) ApplyImpactExplosion();
        if (impactMode == ImpactMode.Simple) { Destroy(gameObject); return; }
        StartImpact();
    }

    private void HitPlayer()
    {
        if (explodesOnImpact) ApplyImpactExplosion();
        if (impactMode == ImpactMode.Simple) { Destroy(gameObject); return; }
        StartImpact();
    }

    // Burst instantâneo no ponto de impacto — dispara 1 vez só (via ReachedLimit/HitPlayer),
    // "ao contato ou na distância máxima", nunca as duas.
    private void ApplyImpactExplosion()
    {
        var results = new Collider2D[4];
        int count = Physics2D.OverlapCircle(transform.position, explosionRadius, ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero == null) continue;

            hero.TakeDamage(damage);
            if (appliesStatusEffect) ApplyStatus(hero);
        }
    }

    private void StartImpact()
    {
        phase = Phase.Impacting;
        // A partir daqui a sprite não pode mais girar conforme a trajetória — a animação de
        // impacto tem orientação fixa própria, não a orientação de voo.
        transform.rotation = Quaternion.identity;
        if (animator != null) animator.SetTrigger("ImpactTrigger");
    }

    // Animation Event, no último frame da animação de impacto.
    public void AnimationImpactEndEvent()
    {
        if (impactMode == ImpactMode.ImpactAnimation) { Destroy(gameObject); return; }
        BecomeGrounded();
    }

    private void BecomeGrounded()
    {
        phase = Phase.Grounded;
        groundedElapsed = 0f;
        groundedTick = new AttackCooldown(groundedTickInterval);
        if (circleCollider != null) circleCollider.radius = groundedRadius;
        if (animator != null) animator.Play("Grounded");

        // Pega de graça quem já estava exatamente em cima do ponto de impacto — mudar o raio
        // do collider não reemite OnTriggerEnter2D pra quem já estava sobreposto (mesmo
        // detalhe já resolvido em MageFireball).
        var results = new Collider2D[4];
        int count = Physics2D.OverlapCircle(transform.position, groundedRadius, ContactFilter2D.noFilter, results);
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Player")) continue;
            var hero = results[i].GetComponent<HeroController>();
            if (hero != null) heroInGroundedArea = hero;
        }
    }

    private void UpdateGrounded()
    {
        groundedElapsed += Time.deltaTime;
        if (groundedElapsed >= groundedDuration)
        {
            phase = Phase.Disappearing;
            if (animator != null) animator.SetTrigger("DisappearTrigger");
            return;
        }

        if (heroInGroundedArea == null) return;

        groundedTick.Tick(Time.deltaTime);
        if (!groundedTick.TryConsume()) return;

        heroInGroundedArea.TakeDamage(groundedDamagePerSecond * groundedTickInterval);
        if (appliesStatusEffect) ApplyStatus(heroInGroundedArea);
    }

    // Animation Event, no último frame da animação de "desaparecer" (só existe em ImpactSurface).
    public void AnimationDisappearEndEvent()
    {
        Destroy(gameObject);
    }
}
