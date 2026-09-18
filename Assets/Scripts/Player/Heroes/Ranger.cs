using UnityEngine;

public class Ranger : HeroController
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int arrowCount = 1; // 🔢 teto de 15, +1 por tier de arma — hook de Tier real chega na Sprint 33
    [SerializeField] private float attackDamageMultiplier = 2f; // 🔢 GDD Seção 17.2: dano total do golpe = stats.damage × isso

    // Formação em cunha (V) — todas as flechas viajam paralelas na mesma direção (a mira),
    // só nascem deslocadas: flechas de dentro nascem mais à frente, as de fora mais atrás e
    // mais afastadas lateralmente (mesma lógica de bando de pássaros voando).
    [SerializeField] private float arrowLateralStep = 0.5f; // 🔢 espaço entre flechas vizinhas — flecha tem 3px (~0.375u a 8 PPU), ajustável
    [SerializeField] private float arrowForwardStep = 0.3f; // 🔢 quanto cada rank nasce mais à frente, ajustável

    // Rede de segurança — mesmo padrão do Barbarian: se o Animation Event de fim nunca
    // disparar, força o fim da ação em vez de travar isAttacking pra sempre.
    [SerializeField] private float maxActionDuration = 3f; // 🔢 ajustável
    private float actionElapsed;

    // Attack é uma Blend Tree 2D Freeform Directional (8 pontos, N/NE/E/SE/S/SW/W/NW) —
    // igual o Attack do Barbarian, mesmo mecanismo. Isso mistura 2 clipes ao mesmo tempo pra
    // quase qualquer ângulo, e cada clipe carrega seu próprio Animation Event — por isso a
    // trava aqui é OBRIGATÓRIA (mesma correção aplicada em Barbarian.cs e
    // EnemyController.AnimationHitEvent()), não apenas defensiva.
    private bool shotFired;

    protected override void Update()
    {
        if (!GameplayGate.IsActive) return;

        base.Update();

        if (isAttacking)
        {
            actionElapsed += Time.deltaTime;
            if (actionElapsed >= maxActionDuration)
            {
                Debug.LogWarning("[Ranger] Animation Event de fim de ataque nunca chegou — forçando fim (verifique o Animator Controller).");
                isAttacking = false;
            }
        }
    }

    protected override void PrimaryAttack()
    {
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        shotFired = false;
        AnimatorTrigger("AttackTrigger");
    }

    // Animation Event, no frame exato em que o Ranger solta as flechas.
    public void AnimationShootEvent()
    {
        if (shotFired) return;
        shotFired = true;

        // Direção crua do mouse (não travada nas 8 direções) — só a pose do personagem e a
        // sprite da flecha (RangerArrow.Launch escolhe o sub-sprite mais próximo) ficam
        // presas nas 8 poses possíveis; a trajetória de voo em si mira exato.
        Vector2 forward = RawAimDirection;
        Vector2 lateral = new Vector2(-forward.y, forward.x);

        // GDD Seção 17.2: dano total do golpe é dividido igualmente entre as flechas —
        // cada uma carrega essa fração como sua própria reserva de perfuração.
        float reservePerArrow = stats.damage * attackDamageMultiplier / arrowCount;

        Vector2[] offsets = ArrowFormation.GetOffsets(arrowCount, arrowLateralStep, arrowForwardStep);
        foreach (var offset in offsets)
        {
            Vector3 spawnPos = transform.position + (Vector3)(forward * offset.x + lateral * offset.y);
            var arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            arrowObj.GetComponent<RangerArrow>().Launch(forward, reservePerArrow);
        }
    }

    // Animation Event, no fim do clipe de ataque.
    public void AnimationAttackEndEvent()
    {
        isAttacking = false;
    }

    protected override void UseUltimate()
    {
        Debug.Log("[Ranger] Ultimate (facas persistentes) ainda não implementada — Sprint 18.");
    }

    // Sem Card Framework ainda (Sprint 35) — incremento manual só pra provar o leque nesta sprint.
    [ContextMenu("Debug: +1 Flecha")]
    private void DebugIncreaseArrowCount()
    {
        arrowCount++;
        Debug.Log($"[Ranger] arrowCount = {arrowCount}");
    }
}
