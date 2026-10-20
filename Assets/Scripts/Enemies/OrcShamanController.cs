using System.Collections.Generic;
using UnityEngine;

// Orc Shaman (Bestiário Andar 3) — Exceção ao Ranged comum: em vez de 1 animação de cast
// genérica instanciando um projétil, tem 3 clipes de cast DISTINTOS (Put_Totem_Fire/Heal/Ice —
// achado real nos sprites já importados, "Attack.png" não existe aqui). Por isso NÃO usa o
// pipeline genérico de Attack/AttackTrigger da base (hasAttackAnimation deve ficar FALSE no
// Inspector) — mesmo critério do RatPeopleRoyaltyController/SpiderQueenController: a decisão de
// QUAL ação tocar precisa acontecer ANTES de disparar o Animator, não dentro de um
// ExecuteAttackHit() genérico que já não sabe mais qual clipe estava tocando.
//
// Prioridade fixa a cada ciclo (GDD): Heal > Fire > Ice. Se os 3 estiverem em cooldown, o
// Shaman simplesmente não tenta castar nada nesse ciclo (sem penalidade — ele só continua
// posicionando/mantendo distância do player via Move(), igual um Ranged comum parado esperando).
public class OrcShamanController : RangedEnemyController
{
    [Header("Totens (prefabs com OrcShamanTotem)")]
    public GameObject healTotemPrefab;
    public GameObject fireTotemPrefab;
    public GameObject iceTotemPrefab;

    [Header("Cooldowns independentes por tipo")]
    public float healCooldownDuration = 6f;
    public float fireCooldownDuration = 4f;
    public float iceCooldownDuration = 8f;
    public float castCooldownDuration = 2f; // ritmo geral entre tentativas de cast (qualquer tipo)

    // Raio de busca por aliado ferido (Heal) — DIFERENTE de stats.attackRadius, que aqui só
    // controla a distância que o Shaman mantém do PLAYER (kiting), sem relação com procurar
    // aliados pra curar.
    public float allySearchRadius = 6f;

    private AttackCooldown healCooldown;
    private AttackCooldown fireCooldown;
    private AttackCooldown iceCooldown;
    private AttackCooldown castCooldown;
    private readonly List<EnemyController> allyBuffer = new();

    private bool isCasting;
    private GameObject pendingTotemPrefab;
    private Vector3 pendingTotemPosition;

    protected override void Awake()
    {
        base.Awake();
        healCooldown = new AttackCooldown(healCooldownDuration);
        fireCooldown = new AttackCooldown(fireCooldownDuration);
        iceCooldown = new AttackCooldown(iceCooldownDuration);
        castCooldown = new AttackCooldown(castCooldownDuration);
    }

    protected override void Move(float deltaTime)
    {
        base.Move(deltaTime); // kiting padrão de Ranged — reaproveitado sem mudança

        healCooldown.Tick(deltaTime);
        fireCooldown.Tick(deltaTime);
        iceCooldown.Tick(deltaTime);
        castCooldown.Tick(deltaTime);

        if (isCasting) return; // comprometido com o cast atual — não inicia outro por cima
        if (Vector2.Distance(transform.position, player.position) > stats.attackRadius) return;
        if (!castCooldown.TryConsume()) return;

        TryStartCast();
    }

    private void TryStartCast()
    {
        if (healCooldown.IsReady && TryFindHurtAlly(out var ally))
        {
            StartCast(healTotemPrefab, ally.transform.position, "CastHealTrigger");
            healCooldown.TryConsume();
            return;
        }

        if (fireCooldown.IsReady)
        {
            StartCast(fireTotemPrefab, player.position, "CastFireTrigger");
            fireCooldown.TryConsume();
            return;
        }

        if (iceCooldown.IsReady)
        {
            StartCast(iceTotemPrefab, player.position, "CastIceTrigger");
            iceCooldown.TryConsume();
            return;
        }

        // Os 3 em cooldown — GDD: "simplesmente não age nesse ciclo, sem penalidade".
    }

    private void StartCast(GameObject totemPrefab, Vector3 position, string triggerName)
    {
        isCasting = true;
        pendingTotemPrefab = totemPrefab;
        pendingTotemPosition = position;
        SetMoving(false);
        if (animator != null) animator.SetTrigger(triggerName);
    }

    // "Alguma unidade inimiga (incluindo ele mesmo) com vida < 100%" — busca por proximidade
    // ao próprio Shaman, não ao player; dedup obrigatório (2 Collider2D por monstro).
    private bool TryFindHurtAlly(out EnemyController ally)
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, allySearchRadius);
        EnemyController.CollectDistinct(hits, hits.Length, allyBuffer);

        foreach (var candidate in allyBuffer)
        {
            if (candidate.stats.health < candidate.stats.maxHealth)
            {
                ally = candidate;
                return true;
            }
        }

        ally = null;
        return false;
    }

    // Animation Event — mesmo nome nos 3 clipes de cast (Put_Totem_Fire/Heal/Ice), o script já
    // sabe qual totem plantar (decidido em StartCast, não aqui).
    public void AnimationCastEvent()
    {
        if (pendingTotemPrefab != null) Instantiate(pendingTotemPrefab, pendingTotemPosition, Quaternion.identity);
    }

    // Animation Event, no fim de qualquer um dos 3 clipes de cast.
    public void AnimationCastEndEvent()
    {
        isCasting = false;
        pendingTotemPrefab = null;
    }

    // Pipeline genérico de Attack nunca é usado — os 3 casts são tratados inteiramente em
    // Move()/TryStartCast() acima, igual ao Rat People Royalty/Goblin Sapper.
    protected override bool InAttackRange() => false;
    protected override void ExecuteAttackHit() { }
}
