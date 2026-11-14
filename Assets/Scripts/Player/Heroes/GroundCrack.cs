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

    // Bug real reportado pelo usuário: a #29 "Fenda Permanente" (nunca expira por timer)
    // deixava o jogador empilhar fendas infinitas na mesma área — qualquer monstro que
    // passasse ali acumulava o dano de TODAS ao mesmo tempo, morrendo com 1 hit. Regra final
    // pedida pelo usuário: existe uma "safe zone" MENOR que o raio da fenda onde 2 fendas PODEM
    // coexistir lado a lado — só deleta a mais antiga quando a distância entre os centros for
    // <= o raio da fenda (não a soma dos 2 raios, que impedia até fendas lado a lado de
    // sobreviverem juntas — tentativa anterior, rejeitada).
    //
    // Bug meu corrigido agora: "centro"/"raio" têm que ser os do PRÓPRIO COLLIDER ativo (pedido
    // explícito do usuário), nunca transform.position/transform.localScale da RAIZ — os 4
    // CircleCollider2D moram em filhos com offset local fixo (±1.01, ver Crack_NE/NW/SE/SW no
    // prefab), então o centro de verdade do collider ativo NUNCA é o mesmo ponto que
    // transform.position da raiz. Collider2D.bounds já resolve escala + offset + hierarquia
    // inteira sozinho — fonte única de centro/raio reais, em vez de recalcular na mão.
    private static readonly List<GroundCrack> ActiveCracks = new();

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
    // areaMultiplier (default 1 — sem efeito em quem não passar) é INDEPENDENTE do
    // sizeMultiplier do Corpo (Esquerda #9-11) — Esquerda #15-17 ("Área do Ground Crack") escala
    // só a fenda, nunca o Barbarian. Os 2 se multiplicam juntos na escala final.
    public void Initialize(Direction direction, float sizeMultiplier, float damagePerSecond, float tickInterval, float areaMultiplier = 1f)
    {
        this.damagePerSecond = damagePerSecond;
        this.tickInterval = tickInterval;

        transform.localScale = Vector3.one * sizeMultiplier * areaMultiplier;
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

        var myCollider = GetActiveCollider(direction);
        if (myCollider != null)
        {
            Vector2 myCenter = myCollider.bounds.center;
            float myRadius = myCollider.bounds.extents.x; // circle: extents.x == extents.y == raio real em mundo

            for (int i = ActiveCracks.Count - 1; i >= 0; i--)
            {
                var other = ActiveCracks[i];
                if (other == null) { ActiveCracks.RemoveAt(i); continue; }
                if (other == this) continue;

                var otherCollider = other.GetCurrentlyEnabledCollider();
                if (otherCollider == null) continue;

                // Safe zone: distância entre os CENTROS DOS COLLIDERS <= raio DO COLLIDER desta
                // fenda = deleta a mais antiga (ver comentário na declaração de ActiveCracks).
                // Distância > raio = as 2 sobrevivem lado a lado.
                if (Vector2.Distance(myCenter, otherCollider.bounds.center) <= myRadius)
                    Destroy(other.gameObject);
            }
        }

        ActiveCracks.Add(this);
    }

    private CircleCollider2D GetActiveCollider(Direction direction) => direction switch
    {
        Direction.NE => neCollider,
        Direction.NW => nwCollider,
        Direction.SE => seCollider,
        Direction.SW => swCollider,
        _ => null
    };

    // Qual dos 4 está ligado AGORA — usado só pra checar overlap contra fendas já existentes
    // (cada uma já decidiu sua direção no próprio Initialize(), não precisa guardar à parte).
    private CircleCollider2D GetCurrentlyEnabledCollider()
    {
        if (neCollider != null && neCollider.enabled) return neCollider;
        if (nwCollider != null && nwCollider.enabled) return nwCollider;
        if (seCollider != null && seCollider.enabled) return seCollider;
        if (swCollider != null && swCollider.enabled) return swCollider;
        return null;
    }

    private void OnDestroy() => ActiveCracks.Remove(this);

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
        // Causa real do floating text "fantasma" no Results/Loja depois de SOBREVIVER o dia
        // (nunca acontecia na morte): Time.timeScale nunca muda neste projeto (GameplayGate é só
        // flag), e no caminho de sobreviver a limpeza de Ground Crack/inimigos só roda bem depois,
        // em ShopHandler.ProceedFromShop — então, sem este gate, a fenda continuava tiquando dano
        // em qualquer inimigo ainda parado nela durante TODO o Results+Loja, gerando
        // GameEvents.OnDamageTaken -> FloatingCombatText novo a cada tick. Esse texto novo nasce
        // DEPOIS do único DestroyAll() já disparado na virada do dia, e como o próprio Update()
        // dele já respeita GameplayGate, ele nunca anima nem se autodestrói sozinho — fica
        // congelado na tela até o jogador sair da loja. Todo outro script de dano por tick do
        // projeto já tem este gate (RogueBomb, GroundFireHazard, GroundTargetHazard etc.) — este
        // aqui tinha ficado de fora.
        if (!GameplayGate.IsActive) return;

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
            // isDirectPlayerHit: false — mesmo critério já usado pro DoT da Ultimate do Mage
            // (dano repetido/em área não deve tremer a tela, só o hit direto do jogador; ver
            // HitFeelController.HandleDamageTaken). Sem isso, o Ground Crack tiquando em vários
            // inimigos ao mesmo tempo deixava a câmera tremendo sem parar.
            enemy.TakeDamage(damageThisTick, isDirectPlayerHit: false);
        }

        foreach (var stale in staleScratch) overlapCount.Remove(stale);
    }
}
