using System.Collections.Generic;
using UnityEngine;

// Ground Crack do ataque primário do Barbarian (teste em andamento — Deadline 8.1). Nasce em
// Barbarian.AnimationAttackHitEvent, toca 1 das 4 animações diagonais (NE/NW/SE/SW, mesmos
// nomes dos states no Animator Controller) conforme a direção do golpe que o sumonou, e fica
// causando dano embaixo dele até Destroy() (chamado por quem instancia, com delay =
// groundCrackDuration — este script não se autodestrói).
//
// 4 CircleCollider2D, 1 PRA CADA animação — cada uma tem sua própria forma/posição de fenda na
// arte, então só o collider da direção escolhida fica ativo (os outros 3 são desligados em
// Initialize). São triggers reais (OnTrigger Enter/Exit), não Overlap() manual: um monstro tem
// 2 Collider2D (corpo + trigger genérico), por isso o contador por inimigo em vez de um bool —
// só sai da lista de dano quando as 2 combinações realmente se separaram.
public class GroundCrack : MonoBehaviour
{
    public enum Direction { NE, NW, SE, SW }

    [SerializeField] private Animator animator;
    [SerializeField] private CircleCollider2D neCollider;
    [SerializeField] private CircleCollider2D nwCollider;
    [SerializeField] private CircleCollider2D seCollider;
    [SerializeField] private CircleCollider2D swCollider;

    // Dano e tick NÃO ficam aqui de propósito — vêm do Barbarian via Initialize() (pedido do
    // usuário: perks futuros de Esquerda só precisam alterar o herói, nunca o prefab da fenda).
    // Duração já era assim desde o início (Barbarian.groundCrackDuration — Destroy() com delay
    // é chamado por quem instancia, não por este script).
    private float damagePerSecond;
    private float tickInterval;

    private float tickElapsed;
    private readonly Dictionary<EnemyController, int> overlapCount = new();
    private readonly List<EnemyController> staleScratch = new();
    private readonly List<EnemyController> damageScratch = new();

    // Chamado por quem instancia (Barbarian.AnimationAttackHitEvent) antes do 1º frame — decide
    // a animação certa pra essa fenda, ajusta o tamanho igual ao sizeMultiplier atual do
    // Barbarian (corpo maior já sumona fenda maior, mesmo critério dos projéteis da Ultimate em
    // Barbarian.AnimationUltimateLandEvent), liga SÓ o collider da direção escolhida — os outros
    // 3 ficam desligados a vida inteira desta instância (nunca dão dano por engano) — e recebe
    // o dano/tick que o Barbarian decidir (upgradável por perk sem tocar neste prefab).
    public void Initialize(Direction direction, float sizeMultiplier, float damagePerSecond, float tickInterval)
    {
        this.damagePerSecond = damagePerSecond;
        this.tickInterval = tickInterval;

        transform.localScale = Vector3.one * sizeMultiplier;
        if (animator != null) animator.Play(direction.ToString());

        if (neCollider != null) neCollider.enabled = direction == Direction.NE;
        if (nwCollider != null) nwCollider.enabled = direction == Direction.NW;
        if (seCollider != null) seCollider.enabled = direction == Direction.SE;
        if (swCollider != null) swCollider.enabled = direction == Direction.SW;

        // Os 4 colliders moraram num GameObject filho cada (Crack_NE/NW/SE/SW) — Unity só
        // entrega OnTrigger Enter/Exit pro script que está no MESMO objeto do collider, nunca
        // sobe pro pai. Em vez de pedir pra alguém anexar um script relay manualmente em cada
        // um dos 4 filhos (risco de guid que já tivemos antes), adiciona na hora via código.
        AttachRelay(neCollider);
        AttachRelay(nwCollider);
        AttachRelay(seCollider);
        AttachRelay(swCollider);
    }

    private void AttachRelay(CircleCollider2D col)
    {
        if (col == null) return;
        if (col.GetComponent<GroundCrackHitRelay>() != null) return; // já tem (reaproveitando a mesma instância em pool, por exemplo)
        col.gameObject.AddComponent<GroundCrackHitRelay>().Owner = this;
    }

    public void HandleTriggerEnter(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;
        overlapCount.TryGetValue(enemy, out int count);
        overlapCount[enemy] = count + 1;
    }

    public void HandleTriggerExit(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;
        if (!overlapCount.TryGetValue(enemy, out int count)) return;

        count--;
        if (count <= 0) overlapCount.Remove(enemy);
        else overlapCount[enemy] = count;
    }

    private void Update()
    {
        tickElapsed += Time.deltaTime;
        if (tickElapsed < tickInterval) return;
        tickElapsed = 0f;
        ApplyDamageTick();
    }

    private void ApplyDamageTick()
    {
        float damageThisTick = damagePerSecond * tickInterval;

        // Snapshot — bug real reportado: TakeDamage pode matar o inimigo, que dispara
        // OnTriggerExit2D NA HORA (via o relay), que chama overlapCount.Remove() ainda dentro
        // deste loop. Iterar direto overlapCount.Keys quebra ("Collection was modified");
        // iterar uma cópia separada não.
        damageScratch.Clear();
        damageScratch.AddRange(overlapCount.Keys);

        staleScratch.Clear();
        foreach (var enemy in damageScratch)
        {
            // Monstro pode ter morrido/sido destruído sem disparar OnTriggerExit2D a tempo —
            // "enemy == null" aqui usa o operador sobrecarregado da Unity (detecta objeto
            // destruído), não a referência crua.
            if (enemy == null) { staleScratch.Add(enemy); continue; }
            enemy.TakeDamage(damageThisTick);
        }

        foreach (var stale in staleScratch) overlapCount.Remove(stale);
    }
}
