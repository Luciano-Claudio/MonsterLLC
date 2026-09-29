using System.Collections.Generic;
using UnityEngine;

public class Vine : MonoBehaviour
{
    // Rede de segurança — mesmo padrão do maxDieDuration/maxActionDuration usado em todo
    // herói/monstro do projeto: se o Animation Event de fim (AnimationVineEndEvent) nunca
    // disparar (clipe sem o evento configurado), a vinha some sozinha depois desse tempo em
    // vez de ficar presa na cena pra sempre. Fonte de verdade continua sendo a animação.
    [SerializeField] private float lifetime = 1.5f; // 🔢 ajustável
    [SerializeField] private float hitRadius = 0.5f; // 🔢 ajustável — o "circle trigger pequeno" da GDD Seção 17.4

    // Gizmo de debug (só para testes) — mesmo padrão do showAttackRadiusGizmo do
    // EnemyController: booleano serializado pra ligar/desligar, e um Y pra mover o centro do
    // círculo pra cima sem depender do pivot exato do sprite.
    [SerializeField] private bool showHitRadiusGizmo = false;
    [SerializeField] private float hitRadiusGizmoOffsetY = 0f;

    private float damage;
    private LayerMask enemyLayerMask;
    private HashSet<EnemyController> hitThisActivation;

    private Vector3 HitCenter => transform.position + Vector3.up * hitRadiusGizmoOffsetY;

    // Chamado pelo Druid (AnimationVineSummonEvent) logo depois do Instantiate — configura o
    // que essa vinha específica vai causar quando acertar, já que ela não tem acesso direto
    // ao stats do Druid. sharedHitSet é a MESMA referência em todas as vinhas nascidas na
    // mesma ativação — garante que um monstro não tome dano de 2 vinhas diferentes mesmo se
    // os raios delas se sobrepuserem (monstros agrupados), sem depender de timing/cooldown.
    public void Launch(float damageAmount, LayerMask targetLayerMask, HashSet<EnemyController> sharedHitSet)
    {
        damage = damageAmount;
        enemyLayerMask = targetLayerMask;
        hitThisActivation = sharedHitSet;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Animation Event, no frame exato em que a vinha aperta o alvo (GDD Seção 17.4/13,
    // Summoned Target Hit) — busca própria na hora, não um alvo fixo guardado: cobre o caso
    // de "acertar mais de 1 monstro se estiverem muito próximos" que a GDD descreve.
    public void AnimationVineHitEvent()
    {
        // Sem checagem de "já morreu" aqui de propósito — EnemyController.TakeDamage() já se
        // protege sozinho (if (isDead) return;), então chamar num alvo morto é inofensivo.
        var hits = Physics2D.OverlapCircleAll(HitCenter, hitRadius, enemyLayerMask);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) continue;

            // Uma vinha pode acertar vários monstros próximos (GDD) — o que não pode é um
            // monstro ser acertado por 2 vinhas DIFERENTES na mesma ativação (ver Launch()).
            if (hitThisActivation != null && !hitThisActivation.Add(enemy)) continue;

            enemy.TakeDamage(damage);
        }
    }

    // Animation Event, no último quadro do clipe — a vinha some no instante certo (fonte de
    // verdade é a animação, mesmo critério de attack/die em herói e monstro), não só quando o
    // timer de segurança do Start() estourar.
    public void AnimationVineEndEvent()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showHitRadiusGizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitCenter, hitRadius);
    }
}
