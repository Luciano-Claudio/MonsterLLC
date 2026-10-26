using UnityEngine;

// Exceção ao Melee comum (Bestiário Andar 4) — Acolyte/Hound/Zealot podem ser possuídos pelo
// Dark Channeler (Andar 4 Bosses), virando Dark Cultist/Dark Hound/Dark Abomination. Mesmo
// GameObject, nunca destruído/reinstanciado: ao fim da animação `possession`, um Animation
// Event troca sprites (via Animator, parâmetro IsPossessed) e vida/dano pros valores da forma
// possuída. Irreversível — diferente do IsArmed do Goblin Sapper (que alterna pra lá e pra
// cá), aqui só acontece 1 vez na vida do monstro.
public class PossessableMeleeEnemyController : MeleeEnemyController
{
    [Header("Forma possuída (Dark Cultist/Dark Hound/Dark Abomination — ver Bestiário)")]
    [SerializeField] private float possessedMaxHealth;
    [SerializeField] private float possessedAttackDamage;

    // 2º conjunto de triggers — a forma possuída tem corpo maior/diferente (ex.: Dark Hound/
    // Abomination usam sprite 40x40, a forma normal usa 32x32), os 4 triggers do corpo normal
    // (herdados de MeleeEnemyController) não cobririam o alcance certo depois da transformação.
    // GetHitboxForFacing()/IsPlayerTouchingAnyHitbox() abaixo escolhem qual conjunto usar com
    // base em isPossessed — nunca os dois ao mesmo tempo.
    [Header("Triggers de ataque — forma possuída (corpo maior)")]
    [SerializeField] private Collider2D attackHitboxPossessedNE;
    [SerializeField] private Collider2D attackHitboxPossessedNW;
    [SerializeField] private Collider2D attackHitboxPossessedSE;
    [SerializeField] private Collider2D attackHitboxPossessedSW;

    private bool isPossessed;

    public bool IsPossessed => isPossessed;

    // Estado real do Animator passa a se chamar "Walk_Possession" depois da transformação —
    // sem isso, MoveInDirection() (MeleeEnemyController.WalkStateName) nunca confirma que o
    // Animator entrou no estado de andar, e o AIPath fica travado (ai.isStopped = true) pra
    // sempre, mesmo com o Animator já tocando Walk_Possession normalmente.
    protected override string WalkStateName => isPossessed ? "Walk_Possession" : base.WalkStateName;

    // Chamado pelo Dark Channeler (Animation Event do próprio cast dele) nos 3 monstros
    // elegíveis mais próximos — "elegível" já deve excluir quem já estiver possuído antes de
    // chamar isto (ver IsPossessed acima), mas o guard abaixo protege de qualquer forma.
    public void TriggerPossession()
    {
        if (isPossessed) return;
        if (animator != null) animator.SetTrigger("PossessionTrigger");
    }

    // Animation Event, no frame certo do clipe `possession` — a troca de verdade acontece
    // aqui, não no instante em que o Dark Channeler termina o próprio cast (ver comentário na
    // ficha do Dark Channeler).
    public void AnimationPossessionEndEvent()
    {
        if (isPossessed) return;

        isPossessed = true;
        stats.maxHealth = possessedMaxHealth;
        stats.health = possessedMaxHealth;
        stats.attackDamage = possessedAttackDamage;
        if (animator != null) animator.SetBool("IsPossessed", true);

        // Pedido do usuário (2026-10-26): a forma possuída já nasce em combate — não espera o
        // player entrar em observationRadius (que o Acolyte/Hound/Zealot original usava antes
        // de ser possuído). Mesmo mecanismo que todo summon "que já nasce sabendo do player"
        // usa no projeto (Rat People arremessado, bosses ao nascer).
        ForceImmediateAggro();
    }

    // Depois de possuído, troca pro 2º conjunto de triggers (corpo maior) — os 2 métodos
    // abaixo espelham a lógica padrão de MeleeEnemyController, só trocando qual conjunto de
    // 4 Collider2D é consultado.
    protected override bool IsPlayerTouchingAnyHitbox()
    {
        if (!isPossessed) return base.IsPlayerTouchingAnyHitbox();

        var heroCollider = player.GetComponent<Collider2D>();
        if (heroCollider == null) return false;

        if (attackHitboxPossessedNE != null && attackHitboxPossessedNE.IsTouching(heroCollider)) return true;
        if (attackHitboxPossessedNW != null && attackHitboxPossessedNW.IsTouching(heroCollider)) return true;
        if (attackHitboxPossessedSE != null && attackHitboxPossessedSE.IsTouching(heroCollider)) return true;
        if (attackHitboxPossessedSW != null && attackHitboxPossessedSW.IsTouching(heroCollider)) return true;
        return false;
    }

    protected override Collider2D GetHitboxForFacing()
    {
        if (!isPossessed) return base.GetHitboxForFacing();

        bool east = AimDirection.x >= 0f;
        bool north = AimDirection.y >= 0f;

        if (north) return east ? attackHitboxPossessedNE : attackHitboxPossessedNW;
        return east ? attackHitboxPossessedSE : attackHitboxPossessedSW;
    }
}
