using UnityEngine;

// Serpent (Bestiário Andar 9 — exceção à regra padrão): Melee comum em tudo (triggers,
// cooldown, Animation Event do golpe) — a única diferença é que o "andar" de verdade
// acontece debaixo da terra. Não precisa de Move()/InAttackRange() customizados pra isso: o
// Animator Controller já faz Idle/IdleCombat/Attack/Damage apontarem pra "Submerge" em vez
// de ir direto pro "Walk" sempre que IsMoving vira true, e só entra em "Walk" de verdade via
// Exit Time (sem condição) depois que o Submerge termina de tocar — MoveInDirection() (ver
// EnemyController) só translada o transform quando o Animator já confirmou que está no
// estado "Walk" mesmo, então o Serpent fica parado (visualmente submergindo) até esse
// instante, sem precisar de nenhuma trava própria aqui.
//
// Regra central (Bestiário): só pode causar OU receber dano enquanto não estiver submerso —
// "submerso" aqui é desde o 1º frame do Submerge (Animation Event) até o último frame do
// Emerge (outro Animation Event), cobrindo Submerge+Walk+Emerge inteiros.
public class SerpentController : MeleeEnemyController
{
    private bool isInvulnerable;

    // Animation Event, 1º frame do Submerge.
    public void AnimationBecomeInvulnerableEvent()
    {
        isInvulnerable = true;
    }

    // Animation Event, último frame do Emerge.
    public void AnimationBecomeVulnerableEvent()
    {
        isInvulnerable = false;
    }

    public override void TakeDamage(float amount, bool isDirectPlayerHit = true)
    {
        if (isInvulnerable) return;
        base.TakeDamage(amount, isDirectPlayerHit);
    }

    // Golpe real só é possível já emerso — sem isso, tocar o trigger de ataque enquanto
    // ainda está em Submerge/Walk (debaixo da terra, só por coincidência de distância)
    // dispararia o AttackTrigger (AnyState) e cortaria o ciclo de emersão pela metade.
    protected override bool InAttackRange()
    {
        if (isInvulnerable) return false;
        return base.InAttackRange();
    }
}
