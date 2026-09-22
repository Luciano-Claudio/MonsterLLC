using UnityEngine;

// Única exceção permanente do jogo (Bestiário) — nunca tem attack real, só dano de contato.
// hasAttackAnimation precisa ficar FALSE no Inspector deste prefab: isso já desliga sozinho
// todo o fluxo de animação de ataque herdado de EnemyController (Update()/UpdateCombat()
// pulam attackAnimationCooldown.Tick()/TryStartAttackAnimation() inteiros quando é false).
public class SlimeEnemyController : EnemyController
{
    protected override void Awake()
    {
        base.Awake();
        var contactDamage = GetComponent<EnemyContactDamage>();
        if (contactDamage != null) contactDamage.Initialize(stats.attackDamage, stats.attackAnimationCooldown);
    }

    // Sempre persegue direto — Slime não flanqueia, não usa MeleeAttackSlotManager (a fila
    // de vaga existe pra evitar hordas competindo pela MESMA janela de animação de ataque;
    // Slime não tem janela nenhuma, cada um aplica seu próprio dano de contato de forma
    // independente, sem disputa). Já colado (dentro do attackRadius), para de tentar avançar
    // mais — sem isso, o Translate() continuava empurrando o Rigidbody2D pra dentro do
    // player todo frame, e o player ficava sendo arrastado pela tela enquanto o Slime bate
    // nele (mesmo corte que MeleeEnemyController já usa, só que aqui nunca sai desse estado
    // pra atacar, já que não existe attack real).
    protected override void Move()
    {
        Vector2 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= stats.attackRadius)
        {
            SetMoving(false);
            return;
        }

        SetMoving(true);
        MoveInDirection(toPlayer.normalized);
    }

    // Nunca chamados de verdade — hasAttackAnimation=false já impede o fluxo que os invocaria.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
