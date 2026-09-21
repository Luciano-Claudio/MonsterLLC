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

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Fire(float damage)
    {
        pendingDamage = damage;
        hitFired = false;
        if (animator != null) animator.SetTrigger("FireTrigger");
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
        for (int i = 0; i < count; i++)
        {
            if (!results[i].CompareTag("Enemy")) continue;
            var enemy = results[i].GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(pendingDamage);
        }
    }
}
