using System.Collections.Generic;
using UnityEngine;

// Totem do Orc Shaman (Bestiário Andar 3) — 1 script, 3 variações por prefab (campo `kind`),
// em vez de 3 subclasses: o ciclo de vida é idêntico nos 3, só QUEM é afetado e COMO muda.
//
// Ciclo de vida (3 animações, confirmado com o usuário em 2026-10-20): `totem_appear` ->
// `totem_idle` (loop) — SOMENTE durante essa fase ativa o efeito tica em área, a cada
// `tickInterval` segundos, em qualquer unidade dentro de `radius` (igual RoguePulseArea/
// MageFireball.UpdateGrounded, mesmo padrão de tick já usado no projeto) — depois de
// `activeDuration`, dispara `totem_disappear` (Trigger) e PARA de ticar; o totem já não causa
// mais nada durante a animação de desaparecer, só se destrói no final dela.
public class OrcShamanTotem : MonoBehaviour
{
    public enum TotemKind { Heal, Fire, Ice }

    [Header("Tipo e área")]
    public TotemKind kind;
    public float radius = 1.5f;

    [Header("Tick (só durante appear+idle)")]
    public float tickInterval = 1f;
    public float activeDuration = 5f;

    [Header("Efeito — só os campos do kind escolhido importam")]
    public float healPerTick = 20f;
    public float damagePerTick = 15f;
    public float iceStunDuration = 2f;

    // Indicador visual (pedido do usuário) — círculo com a arte própria dele, cor/formato por
    // kind (ver prefab). Escala 100% manual, calibrada à mão no Inspector de cada prefab pra
    // casar com a arte — o código NUNCA mexe nela (já tentei auto-calcular a partir do
    // tamanho da sprite, decisão do usuário foi reverter: o valor que ele ajustou já bate
    // perfeito, não precisa/não deve de conta nenhuma em cima).
    [Header("Indicador de área (filho — escala ajustada à mão, não mexer por código)")]
    [SerializeField] private Transform areaIndicator;

    // Gizmo de debug (Editor) — mostra o raio REAL do efeito (`radius`), independente de a
    // sprite do indicador estar certa ou não. Ligado por padrão (diferente do
    // showAttackRadiusGizmo do EnemyController, que é opt-in por monstro) porque o pedido aqui
    // foi explicitamente "ver a área real" pra calibrar a arte contra ela.
    [Header("Debug — só pra visualização em Editor, não afeta gameplay")]
    [SerializeField] private bool showAreaGizmo = true;

    private Animator animator;
    private AttackCooldown tickCooldown;
    private float activeElapsed;
    private bool disappearing;

    private readonly List<EnemyController> allyBuffer = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        tickCooldown = new AttackCooldown(tickInterval);

        // areaIndicator.localScale NUNCA é tocado aqui — decisão do usuário, ver comentário
        // no campo acima. A escala fica inteiramente a cargo do que foi ajustado no prefab.
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (disappearing) return;

        activeElapsed += Time.deltaTime;
        if (activeElapsed >= activeDuration)
        {
            disappearing = true;
            if (animator != null) animator.SetTrigger("DisappearTrigger");
            return;
        }

        tickCooldown.Tick(Time.deltaTime);
        if (tickCooldown.TryConsume()) ApplyTick();
    }

    private void ApplyTick()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, radius);

        switch (kind)
        {
            case TotemKind.Heal:
                ApplyHealTick(hits);
                break;
            case TotemKind.Fire:
                ApplyPlayerTick(hits, isFire: true);
                break;
            case TotemKind.Ice:
                ApplyPlayerTick(hits, isFire: false);
                break;
        }
    }

    // GDD: "se existir alguma unidade inimiga (incluindo ele mesmo) com vida < 100%" — aqui
    // generalizado pro raio do totem (não só o alvo original), cura QUALQUER monstro ferido
    // que esteja dentro — dedup obrigatório (cada monstro tem 2 Collider2D, corpo + trigger,
    // mesmo critério de EnemyController.CollectDistinct).
    private void ApplyHealTick(Collider2D[] hits)
    {
        EnemyController.CollectDistinct(hits, hits.Length, allyBuffer);

        foreach (var ally in allyBuffer)
        {
            if (ally.stats.health < ally.stats.maxHealth) ally.Heal(healPerTick);
        }
    }

    // Fire/Ice só afetam o jogador (totens ofensivos, plantados nos pés dele) — nunca os
    // próprios aliados do Shaman, mesmo que um deles cruze o raio por acaso.
    private void ApplyPlayerTick(Collider2D[] hits, bool isFire)
    {
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            if (isFire)
            {
                var damageable = hit.GetComponent<IDamageable>();
                // false: dano em área por tick, não é hit direto — mesmo critério de qualquer
                // outro DoT/rastro do jogo (MageFireball.UpdateGrounded, etc.).
                damageable?.TakeDamage(damagePerTick, isDirectPlayerHit: false);
            }
            else
            {
                var status = hit.GetComponent<StatusEffectController>();
                // damagePerSecond 0 — Ice aqui é só incapacitação, mesmo critério da teia da
                // Spider Queen.
                status?.ApplyStatusEffect(StatusEffectType.Ice, iceStunDuration, 0f);
            }

            return; // só 1 jogador
        }
    }

    // Animation Event, no fim do clipe totem_disappear.
    public void AnimationTotemDisappearEndEvent()
    {
        Destroy(gameObject);
    }

    // Só pra visualização em Editor (calibrar a arte contra a área real) — não roda em build,
    // não afeta gameplay. Cor por kind, mesma associação visual do indicador (verde/
    // laranja-vermelho/ciano). OnDrawGizmos (sempre visível), não só Selected — totem é um
    // objeto transiente (nasce/some sozinho em segundos), não dá tempo real de clicar nele na
    // Hierarchy toda vez só pra comparar o tamanho da arte contra o raio de verdade.
    private void OnDrawGizmos()
    {
        if (!showAreaGizmo) return;
        Gizmos.color = kind switch
        {
            TotemKind.Heal => new Color(0.35f, 1f, 0.35f),
            TotemKind.Fire => new Color(1f, 0.35f, 0.15f),
            TotemKind.Ice => new Color(0.35f, 0.85f, 1f),
            _ => Color.white,
        };
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
