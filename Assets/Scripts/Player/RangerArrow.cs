using UnityEngine;

// speed/lifetime/knockbackForce NÃO são serializados aqui de propósito — são valores de
// upgrade, decididos pelo Ranger (controlador) e recebidos em Launch(). Upgrades futuros só
// editam o Ranger.cs, nunca este prefab.
public class RangerArrow : MonoBehaviour
{
    // 1 sprite só, rotacionado de verdade em vez de trocar de pose (Animator/Blend Tree
    // removido) — a flecha voa em direção livre (RawAimDirection, não travada nas 8
    // direções), então só rotação contínua acompanha o ângulo exato sem parecer estranho.
    // Bônus: o collider (mesmo Transform) gira junto com o sprite, então fica sempre
    // alinhado com o visual, não importa a direção — antes, com sprite fixo trocando de
    // pose, o collider "justinho" só batia certo com a pose Norte original.
    private Vector2 direction;

    // Reserva de perfuração (GDD Seção 13/17.2) — mesmo mecanismo do HeroProjectile do
    // Barbarian: ao acertar, gasta só o mínimo entre a reserva e a vida do alvo, e continua
    // a mesma trajetória se sobrar, em vez de morrer no primeiro contato. Exceção: #30
    // "Enxame Perfurante" (infinitePierce) nunca gasta a reserva — ver OnTriggerEnter2D.
    private float damageReserve;
    private float speed;
    private float lifetime;
    private float knockbackForce;
    private float timer;

    // #27 "Tiro à Distância" — dano +X%/rank conforme a FRAÇÃO do alcance máximo já
    // percorrida no instante do hit (0 no nascimento, 1 no fim de "lifetime") — timer/lifetime
    // já é exatamente essa fração, porque a flecha sempre viaja a velocidade constante.
    private float distanceDamageBonusPercent;

    // #28 "Flechas Sangrentas" — chance/rank (não garantido) de aplicar Bleeding no hit.
    private float bleedChancePercent;
    private float bleedDamagePerSecond;
    private float bleedDuration;

    // #29 "Flecha Bumerangue" — ao esgotar o "lifetime" SEM gastar toda a reserva, inverte a
    // direção e volta pelo mesmo caminho 1x (nunca mais que isso, pra não virar um
    // "lifetime" infinito) — reseta o timer e continua viva em vez de morrer.
    private bool boomerangEnabled;
    private bool hasBoomeranged;

    // #30 "Enxame Perfurante" — nunca gasta a reserva de perfuração: toda flecha atinge
    // TODOS os monstros no caminho com dano cheio, sem perder força (GDD, gate final).
    private bool infinitePierceEnabled;

    // Ver PhysicsUtility — sem Rigidbody2D, o trigger deste projétil às vezes nunca disparava
    // (tunelamento, bug crítico reportado pelo usuário, 2026-10-25).
    private void Awake() => PhysicsUtility.EnsureKinematicRigidbody2D(gameObject);

    public void Launch(Vector2 dir, float reserve, float arrowSpeed, float arrowLifetime, float arrowKnockbackForce,
        float distanceBonusPercent = 0f, float bleedChance = 0f, float bleedPerSecond = 0f, float bleedDur = 0f,
        bool boomerang = false, bool infinitePierce = false)
    {
        direction = dir.normalized;
        damageReserve = reserve;
        speed = arrowSpeed;
        lifetime = arrowLifetime;
        knockbackForce = arrowKnockbackForce;
        distanceDamageBonusPercent = distanceBonusPercent;
        bleedChancePercent = bleedChance;
        bleedDamagePerSecond = bleedPerSecond;
        bleedDuration = bleedDur;
        boomerangEnabled = boomerang;
        infinitePierceEnabled = infinitePierce;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — por isso o -90°: sem
        // ele, ângulo 0 (Leste) deixaria o sprite ainda apontando pra cima em vez de deitado
        // na horizontal.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // position direto (espaço de mundo), não Translate — Translate usa espaço LOCAL por
        // padrão, e agora que a flecha está rotacionada os eixos locais giraram junto,
        // fazendo "direction" (um vetor de mundo) apontar pro lado errado.
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer < lifetime) return;

        // #29 "Flecha Bumerangue" — só volta se ainda tiver reserva sobrando (senão já
        // teria morrido no hit) e só 1x (hasBoomeranged trava a 2ª tentativa).
        if (boomerangEnabled && !hasBoomeranged && damageReserve > 0f)
        {
            hasBoomeranged = true;
            direction = -direction;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            timer = 0f;
            return;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;

        // #27 "Tiro à Distância" — bônus proporcional à fração do alcance já percorrida.
        float distanceFraction = lifetime > 0f ? Mathf.Clamp01(timer / lifetime) : 0f;
        float effectiveReserve = damageReserve * (1f + distanceDamageBonusPercent / 100f * distanceFraction);

        if (infinitePierceEnabled)
        {
            // #30 "Enxame Perfurante" — dano cheio em TODO monstro no caminho, a reserva
            // nunca é consumida (nunca morre por perfuração — só por "lifetime"/bumerangue).
            enemy.TakeDamage(effectiveReserve);
            enemy.ApplyKnockback(direction, knockbackForce);
            RollBleed(enemy);
            return;
        }

        float damageDealt = Mathf.Min(effectiveReserve, enemy.stats.health);
        enemy.TakeDamage(damageDealt);
        enemy.ApplyKnockback(direction, knockbackForce);
        RollBleed(enemy);

        // A reserva real gasta é a fração NÃO bonificada (o bônus de distância é "dano
        // extra", não reserva extra) — senão a Enxame Perfurante ficaria redundante com
        // Tiro à Distância em rank alto, e a perfuração ficaria mais generosa à toa.
        damageReserve -= Mathf.Min(damageReserve, damageDealt / (1f + distanceDamageBonusPercent / 100f * distanceFraction));
        if (damageReserve <= 0f) Destroy(gameObject);
    }

    private void RollBleed(EnemyController enemy)
    {
        if (bleedChancePercent <= 0f || Random.value * 100f >= bleedChancePercent) return;
        var statusEffects = enemy.GetComponent<StatusEffectController>();
        if (statusEffects != null) statusEffects.ApplyStatusEffect(StatusEffectType.Bleeding, bleedDuration, bleedDamagePerSecond);
    }
}
