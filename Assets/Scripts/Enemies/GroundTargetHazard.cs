using UnityEngine;

// Área de conjuração no chão do Skeleton Mage/Zombie Mage (GDD/Bestiary Andar 5). Nasce
// sob o player no instante do cast (Animation Event do GroundCasterEnemyController) e não
// o persegue depois — é um telegraph parado. Tem sua própria animação de aviso, com seu
// próprio Animation Event no frame em que o dano é decidido: só acerta quem ainda estiver
// dentro do trigger *naquele instante exato* (mesmo critério de "IsTouching no frame do
// evento" usado pelo golpe do Melee comum).
public class GroundTargetHazard : MonoBehaviour
{
    public float damage;

    // Lich (Bestiário Andar 5) — círculo de gelo: dano + lentidão. Mesmo padrão de campos já
    // usado em EnemyProjectile (appliesStatusEffect/statusEffectType/...), aqui aplicado no
    // mesmo instante do dano (AnimationHazardHitEvent), não por tick — o hazard já se
    // autodestrói logo depois, não ia dar tempo de tickar. damagePerSecond 0 = só a
    // incapacitação, dano real já vem do campo `damage` acima (mesmo critério do Ice do
    // Totem do Orc Shaman).
    [Header("Efeito de status (opcional — ex.: lentidão do Lich)")]
    [SerializeField] private bool appliesStatusEffect = false;
    [SerializeField] private StatusEffectType statusEffectType;
    [SerializeField] private float statusEffectDuration = 3f; // 🔢 ajustável
    [SerializeField] private float statusEffectDamagePerSecond = 0f; // 🔢 ajustável

    // Rede de segurança — mesmo padrão do resto do EnemyController: se o Animation Event
    // de fim nunca disparar (clipe sem o evento configurado), o hazard não fica na cena
    // pra sempre. Timer manual, não Coroutine (Coroutine não respeita o GameplayGate).
    public float maxHazardDuration = 3f;

    private Collider2D hazardCollider;
    private float elapsed;
    private bool ended;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (ended) return;

        elapsed += Time.deltaTime;
        if (elapsed >= maxHazardDuration)
        {
            Debug.LogWarning("[GroundTargetHazard] AnimationHazardEndEvent nunca chegou — forçando destruição (verifique o Animator Controller).");
            AnimationHazardEndEvent();
        }
    }

    // Animation Event, no frame em que o telegraph "estoura".
    public void AnimationHazardHitEvent()
    {
        if (hazardCollider == null) return;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        var playerCollider = playerObj.GetComponent<Collider2D>();
        if (playerCollider == null) return;
        if (!hazardCollider.IsTouching(playerCollider)) return; // fugiu a tempo -> sem dano

        var hero = playerObj.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(damage);

        if (appliesStatusEffect && hero != null && !hero.IsCurrentlyDamageImmune)
        {
            var statusController = hero.GetComponent<StatusEffectController>();
            if (statusController != null)
                statusController.ApplyStatusEffect(statusEffectType, statusEffectDuration, statusEffectDamagePerSecond);
        }
    }

    // Animation Event, no fim do clipe de aviso.
    public void AnimationHazardEndEvent()
    {
        if (ended) return;
        ended = true;
        Destroy(gameObject);
    }
}
