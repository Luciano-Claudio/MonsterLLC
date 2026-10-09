using System.Collections.Generic;
using UnityEngine;

// GameObject filho do Mage (girado pelo Mage.cs no Update(), não por si próprio) — trigger
// retangular fixo, sempre grudado no Mage, que só existe pra guardar o dano pendente e
// aplicá-lo no instante certo da PRÓPRIA animação de fogo saindo (GDD Seção 17.3). Não viaja
// (ao contrário de HeroProjectile/RangerArrow) — é sempre a mesma instância, reaproveitada a
// cada ataque.
public class MageAttackHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D hitbox;
    private Animator animator;
    private float pendingDamage;
    private bool hitFired;
    private readonly List<EnemyController> hitTargets = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // actionSpeedMultiplier vem do Mage (upgrade de velocidade de ação) — Animator próprio,
    // separado do corpo do Mage, então o SetFloat do HeroController não chega aqui sozinho;
    // repassado a cada Fire() pra sempre refletir o valor atual, sem depender de ordem de
    // Awake() entre os dois GameObjects.
    public void Fire(float damage, float actionSpeedMultiplier)
    {
        pendingDamage = damage;
        hitFired = false;
        if (animator != null)
        {
            animator.SetFloat("ActionSpeedMultiplier", actionSpeedMultiplier);
            animator.SetTrigger("FireTrigger");
        }
    }

    // Animation Event, no instante exato em que o fogo sai de verdade (dentro do clipe deste
    // próprio filho, não do Mage).
    public void AnimationFireHitEvent()
    {
        if (hitFired) return;
        hitFired = true;
        if (hitbox == null) return;

        var results = new Collider2D[16];
        int count = hitbox.Overlap(ContactFilter2D.noFilter, results);
        // Dedup obrigatório — todo monstro tem 2 Collider2D no mesmo GameObject (corpo +
        // trigger genérico), então Overlap() sem isso acertava o mesmo monstro 2x (bug real:
        // dano dobrado no primário do Mage). Ver EnemyController.CollectDistinct().
        EnemyController.CollectDistinct(results, count, hitTargets);
        foreach (var enemy in hitTargets) enemy.TakeDamage(pendingDamage);
    }
}
