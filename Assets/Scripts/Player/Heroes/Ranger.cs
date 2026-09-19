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

    [Header("Ultimate — 8 facas nas 8 direções fixas (GDD Seção 17.2)")]
    [SerializeField] private GameObject knifePrefab; // precisa ter RangerKnife
    [SerializeField] private float ultimateDamageMultiplier = 2f; // 🔢 GDD: "2x o dano do Ranger" em voo, 1x no chão

    // A Ultimate também é Blend Tree 2D Freeform Directional (4 pontos diagonais, igual
    // Idle/Walk/Damage) — mesmo risco de evento duplicado do Attack, só que aqui cada uma
    // das 8 facas tem seu PRÓPRIO Animation Event/método (pedido explícito do usuário, não
    // um método único parametrizado), então a trava precisa ser por direção: um clique só
    // pode disparar o evento de uma direção 2x sem afetar as outras 7.
    private readonly bool[] knifeThrown = new bool[8];

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
        // Guarda própria — HeroController não bloqueia Ultimate por isAttacking, então cada
        // herói se protege (mesmo padrão do Barbarian).
        if (isAttacking) return;
        isAttacking = true;
        actionElapsed = 0f;
        for (int i = 0; i < knifeThrown.Length; i++) knifeThrown[i] = false;
        AnimatorTrigger("UltimateTrigger");
    }

    // 8 Animation Events, um por direção fixa (E/NE/N/NW/W/SW/S/SE, mesma ordem de
    // DirectionUtility) — método próprio por direção em vez de 1 método parametrizado,
    // pra ligar cada um no frame exato do giro em que aquela direção "atira" de verdade.
    public void AnimationThrowKnife_E() => ThrowKnife(0);
    public void AnimationThrowKnife_NE() => ThrowKnife(1);
    public void AnimationThrowKnife_N() => ThrowKnife(2);
    public void AnimationThrowKnife_NW() => ThrowKnife(3);
    public void AnimationThrowKnife_W() => ThrowKnife(4);
    public void AnimationThrowKnife_SW() => ThrowKnife(5);
    public void AnimationThrowKnife_S() => ThrowKnife(6);
    public void AnimationThrowKnife_SE() => ThrowKnife(7);

    private void ThrowKnife(int directionIndex)
    {
        if (knifeThrown[directionIndex]) return;
        knifeThrown[directionIndex] = true;

        Vector2 dir = DirectionUtility.DirectionFromIndex(directionIndex);
        float flightDamage = stats.damage * ultimateDamageMultiplier;
        float groundedDamage = stats.damage;

        var knifeObj = Instantiate(knifePrefab, transform.position, Quaternion.identity);
        knifeObj.GetComponent<RangerKnife>().Launch(dir, flightDamage, groundedDamage);
    }

    // Animation Event, no fim do clipe da Ultimate.
    public void AnimationUltimateEndEvent()
    {
        isAttacking = false;
    }

    // Sem Card Framework ainda (Sprint 35) — incremento manual só pra provar o leque nesta sprint.
    [ContextMenu("Debug: +1 Flecha")]
    private void DebugIncreaseArrowCount()
    {
        arrowCount++;
        Debug.Log($"[Ranger] arrowCount = {arrowCount}");
    }
}
