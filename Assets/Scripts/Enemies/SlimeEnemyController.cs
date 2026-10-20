using UnityEngine;

// Única exceção permanente do jogo (Bestiário) — nunca tem attack real, só dano de contato.
// hasAttackAnimation precisa ficar FALSE no Inspector deste prefab: isso já desliga sozinho
// todo o fluxo de animação de ataque herdado de EnemyController (Update()/UpdateCombat()
// pulam attackAnimationCooldown.Tick()/TryStartAttackAnimation() inteiros quando é false).
public class SlimeEnemyController : EnemyController
{
    // Mesmo collider trigger que o EnemyContactDamage já escuta (OnTriggerStay2D não precisa
    // de referência pra isso) — aqui serve só pra medir contato real em vez de distância. Único
    // trigger (não 4 como o Melee comum): Slime não tem direção de ataque, só precisa saber
    // "o player está encostando em mim".
    [Header("Trigger de contato (mesmo collider usado pelo EnemyContactDamage)")]
    [SerializeField] private Collider2D attackHitbox;

    protected override void Awake()
    {
        base.Awake();
        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, stats.attackAnimationCooldown);
    }

    // Sempre persegue direto — Slime não flanqueia, não usa MeleeAttackSlotManager (a fila
    // de vaga existe pra evitar hordas competindo pela MESMA janela de animação de ataque;
    // Slime não tem janela nenhuma, cada um aplica seu próprio dano de contato de forma
    // independente, sem disputa). Decisão do usuário: Slime também deixou de usar "attack
    // radius" (distância) — já colado (tocando o trigger de contato de verdade), para de
    // tentar avançar mais, em vez de Vector2.Distance <= stats.attackRadius (mesmo motivo do
    // MeleeEnemyController: RVO/colisão entre vários monstros às vezes nunca deixava fechar a
    // distância exata). Sem isso, o Translate() continuava empurrando o Rigidbody2D pra
    // dentro do player todo frame, e o player ficava sendo arrastado pela tela enquanto o
    // Slime bate nele.
    protected override void Move(float deltaTime)
    {
        Vector2 toPlayer = player.position - transform.position;

        if (IsPlayerTouchingHitbox())
        {
            SetMoving(false);
            return;
        }

        SetMoving(true);
        MoveInDirection(toPlayer.normalized);
    }

    private bool IsPlayerTouchingHitbox()
    {
        if (attackHitbox == null) return false;
        var heroCollider = player.GetComponent<Collider2D>();
        return heroCollider != null && attackHitbox.IsTouching(heroCollider);
    }

    // Nunca chamados de verdade — hasAttackAnimation=false já impede o fluxo que os invocaria.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
