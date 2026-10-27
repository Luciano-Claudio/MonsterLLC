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

    // Bicephalous (Bestiário Andar 9) — "caso 4" da taxonomia de projéteis (Seção 13): o
    // próprio projétil vira o monstro ao fim da trajetória (hit ou limite), nasce ali, vivo.
    // Independente do impactMode (que só decide o destino VISUAL do próprio projétil) — o
    // spawn acontece de qualquer forma, no ponto exato onde o projétil estava.
    [Header("Vira monstro ao impacto/limite (opcional — 'caso 4' da taxonomia de projéteis)")]
    [SerializeField] private bool spawnsMonsterOnImpact = false;
    [SerializeField] private GameObject monsterPrefab;

    [Header("Área persistente (só ImpactSurface)")]
    [SerializeField] private CircleCollider2D circleCollider; // mesmo collider do voo, redimensionado ao virar área
    [SerializeField] private float groundedRadius = 1.2f; // 🔢 ajustável
    [SerializeField] private float groundedDuration = 4f; // 🔢 ajustável
    [SerializeField] private float groundedTickInterval = 1f; // 🔢 ajustável
    [SerializeField] private float groundedDamagePerSecond = 2f; // 🔢 ajustável

    // Gizmo de debug (Editor) — mostra o círculo REAL usado pelo Physics2D.OverlapCircle de
    // ApplyImpactExplosion()/BecomeGrounded() (explodesOnImpact/ImpactSurface), pra investigar
    // casos onde o trigger de voo claramente encosta no player (anima impacto normalmente) mas
    // o dano não sai — suspeita do usuário (2026-10-26) é que essa consulta separada, não o
    // toque em si, é quem está falhando. OnDrawGizmos (não Selected) — mesmo critério do
    // OrcShamanTotem: o projétil é transiente, não dá tempo de clicar nele em voo.
    [Header("Debug — gizmo da área de dano/explosão (Editor)")]
    [SerializeField] private bool showExplosionGizmo = true;

    private Animator animator;
    private Vector2 direction;
    private float timer;
    private Phase phase = Phase.Flying;
    private float groundedElapsed;
    private AttackCooldown groundedTick;
    private HeroController heroInGroundedArea; // só existe 1 Player no jogo — referência única, sem lista

    // Guard contra o 2º OnTriggerEnter2D do MESMO impacto — desde que o herói ganhou um 2º
    // Collider2D (ProjectileHitbox, trigger, além do físico), a Unity entrega um evento de
    // trigger POR COLLIDER que entra em overlap, não um por GameObject. O 1º evento já chama
    // HitPlayer() (que pode só agendar Destroy(), não remove o objeto NA HORA) — sem este
    // guard, o 2º evento (mesmo frame, outro collider do herói) processava o impacto de novo,
    // dano duplo.
    private bool hasImpacted;

    private void Awake()
    {
        animator = GetComponent<Animator>(); // pode não existir (ImpactMode.Simple não precisa de Animator)
        PhysicsUtility.EnsureKinematicRigidbody2D(gameObject); // ver PhysicsUtility — sem isso, o trigger pode nunca disparar (tunelamento)
    }

    // minTravelDistance (opcional) — distância real até o player no instante do tiro
    // (RangedEnemyController já mede isso pra decidir mirar). "lifetime" sozinho é um valor
    // FIXO por prefab (🔢, pensado pra cobrir só stats.attackRadius) — mas o player pode ter
    // se afastado durante o windup da animação (entre o InAttackRange() que autorizou o
    // ataque e o Animation Event que chama Launch() de verdade, alguns frames depois), ficando
    // mais longe do que o lifetime original cobre. Nesse caso o projétil "morria" (ReachedLimit)
    // bem antes de alcançar o player, mesmo mirado certinho — bug real reportado: o projétil
    // caía extremamente próximo do player, sem acertar. Aqui só ESTENDE o lifetime quando
    // necessário (nunca encurta), então nenhum prefab já calibrado perde alcance.
    public void Launch(Vector2 dir, float dmg, float minTravelDistance = 0f)
    {
        direction = dir.normalized;
        damage = dmg;

        const float travelMargin = 1f; // 🔢 cobre o raio do próprio player + folga de movimento residual
        float requiredLifetime = (minTravelDistance + travelMargin) / Mathf.Max(speed, 0.01f);
        if (requiredLifetime > lifetime) lifetime = requiredLifetime;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — por isso o -90°: sem
        // ele, ângulo 0 (Leste) deixaria o sprite ainda apontando pra cima em vez de deitado
        // na horizontal.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

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
            if (hasImpacted) return; // 2º collider do herói entrando no mesmo instante — já processado
            hasImpacted = true;

            var hero = other.GetComponent<HeroController>();

            // Se explode em área, o dano vem só do burst (ApplyImpactExplosion, chamado dentro
            // de HitPlayer) — não aplica dano direto aqui também, senão o player levaria hit
            // duplo (contato + explosão) no mesmo instante.
            if (!explodesOnImpact)
            {
                if (hero != null)
                {
                    hero.TakeDamage(damage);
                    if (appliesStatusEffect) ApplyStatus(hero);
                }
            }
            HitPlayer(hero);
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

    // Só aplica se o dano em si não foi bloqueado (ex.: Cambalhota do Rogue, Owl do Druid) —
    // TakeDamage() sozinho não avisava disso, por isso o check extra via
    // IsCurrentlyDamageImmune antes de cada chamada (ver HeroController).
    private void ApplyStatus(HeroController hero)
    {
        if (hero.IsCurrentlyDamageImmune) return;
        var statusController = hero.GetComponent<StatusEffectController>();
        if (statusController != null) statusController.ApplyStatusEffect(statusEffectType, statusEffectDuration, statusEffectDamagePerSecond);
    }

    // Chegou no fim da trajetória (lifetime) sem acertar ninguém.
    private void ReachedLimit()
    {
        if (hasImpacted) return; // já impactou (OnTriggerEnter2D) no mesmo frame — não processa de novo
        hasImpacted = true;

        // null = ninguém tocou de verdade (chegou no limite sozinho) — ApplyImpactExplosion()
        // cai pro OverlapCircle, única opção aqui (não tem collider nenhum pra apontar).
        if (explodesOnImpact) ApplyImpactExplosion(null);
        if (spawnsMonsterOnImpact) SpawnMonster();
        if (impactMode == ImpactMode.Simple) { Destroy(gameObject); return; }
        StartImpact();
    }

    private void HitPlayer(HeroController directHero)
    {
        if (explodesOnImpact) ApplyImpactExplosion(directHero);
        if (spawnsMonsterOnImpact) SpawnMonster();
        if (impactMode == ImpactMode.Simple) { Destroy(gameObject); return; }
        StartImpact();
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null) return;
        Instantiate(monsterPrefab, transform.position, Quaternion.identity);
    }

    // Burst instantâneo no ponto de impacto — dispara 1 vez só (via ReachedLimit/HitPlayer),
    // "ao contato ou na distância máxima", nunca as duas.
    //
    // 2026-10-26 — bug crítico reportado pelo usuário, intermitente, confirmado NÃO resolvido
    // só com Auto Sync Transforms (Fire/Air Elemental, entre outros): projétil com
    // explodesOnImpact às vezes simplesmente não causava dano, mesmo acertando visivelmente.
    // A causa exata dentro do motor de física da Unity nunca foi 100% confirmada apesar de
    // várias tentativas — em vez de continuar caçando, a solução agora é parar de depender de
    // QUALQUER consulta de física (OverlapCircle/Overlap) pra achar o player aqui. Só existe 1
    // player no jogo e sua posição real (Transform) está sempre disponível — comparar
    // distância direto não tem ponto de falha físico nenhum pra dar errado.
    //
    // directHero: quando o toque já é conhecido (veio de OnTriggerEnter2D — o caso mais
    // comum), usa essa referência direto. null = ninguém tocou de verdade (ReachedLimit,
    // chegou no limite da trajetória sozinho) — aí sim precisa achar o player via tag.
    private void ApplyImpactExplosion(HeroController directHero)
    {
        HeroController hero = directHero;
        if (hero == null)
        {
            // Só cai aqui sem um toque confirmado (ReachedLimit) — aí sim precisa checar
            // distância, já que FindPlayer() não prova contato nenhum.
            hero = FindPlayer();
            if (hero == null) return;
            if (Vector2.Distance(transform.position, hero.transform.position) > explosionRadius) return;
        }
        // directHero != null: o toque já é um fato confirmado por OnTriggerEnter2D — os
        // colliders genuinamente se tocaram. Bug real encontrado (2026-10-27, log confirmou):
        // uma checagem de distância pivô-a-pivô aqui rejeitava o dano mesmo com o toque
        // confirmado, porque o ProjectileHitbox do herói tem offset (não fica exatamente no
        // pivô) — a distância entre os 2 PIVÔS passava do explosionRadius com frequência,
        // mesmo com os colliders de verdade se tocando. Nunca re-valida um toque já provado.

        hero.TakeDamage(damage);
        if (appliesStatusEffect) ApplyStatus(hero);
    }

    // Único player do jogo — nunca via física, só pela tag. Usado pelos casos que não têm um
    // Collider2D conhecido pra apontar (ver comentário de ApplyImpactExplosion acima).
    private static HeroController FindPlayer()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        return obj != null ? obj.GetComponent<HeroController>() : null;
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
        // detalhe já resolvido em MageFireball). Distância direto, não OverlapCircle — mesmo
        // critério de ApplyImpactExplosion acima.
        var hero = FindPlayer();
        if (hero != null && Vector2.Distance(transform.position, hero.transform.position) <= groundedRadius)
            heroInGroundedArea = hero;
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

    // Só pra visualização em Editor — não afeta gameplay. Laranja = explosão de impacto
    // (ApplyImpactExplosion, só desenha se explodesOnImpact estiver ligado); ciano = área
    // persistente no chão (BecomeGrounded/UpdateGrounded, só em ImpactSurface — mesmo raio do
    // circleCollider real, mas aqui pra bater exatamente com o que o OverlapCircle usa).
    private void OnDrawGizmos()
    {
        if (!showExplosionGizmo) return;

        if (explodesOnImpact)
        {
            Gizmos.color = new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        if (impactMode == ImpactMode.ImpactSurface)
        {
            Gizmos.color = new Color(0.35f, 0.85f, 1f);
            Gizmos.DrawWireSphere(transform.position, groundedRadius);
        }
    }
}
