using UnityEngine;

// Dano de contato passivo com cooldown — usado só nos 2 casos que o Bestiário documenta como
// exceção à regra geral de "sem dano de contato": Slime Green/Blue (exceção permanente) e a
// camada extra de contato do Goblin Sapper (soma-se à bomba, não a substitui, ativa em
// qualquer fase dele). Componente à parte em vez de método duplicado nas duas classes.
public class EnemyContactDamage : MonoBehaviour
{
    private float damage;
    private AttackCooldown cooldown;

    public void Initialize(float contactDamage, float cooldownDuration)
    {
        damage = contactDamage;
        cooldown = new AttackCooldown(cooldownDuration);
    }

    private void Update()
    {
        if (cooldown == null) return;
        if (!GameplayGate.IsActive) return;
        cooldown.Tick(Time.deltaTime);
    }

    // OnTriggerStay2D — precisa de um CircleCollider2D próprio marcado como trigger no mesmo
    // GameObject (separado do collider físico não-trigger que já empurra o Player), do
    // tamanho do attackRadius. Voltou a ser trigger em vez de colisão física porque dá
    // controle real sobre o alcance do dano (igual attackRadius), independente do tamanho do
    // collider físico que só existe pra bloquear/empurrar.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (cooldown == null) return;
        if (!GameplayGate.IsActive) return;
        if (!other.CompareTag("Player")) return;
        if (!cooldown.TryConsume()) return;

        var hero = other.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(damage);
    }
}
