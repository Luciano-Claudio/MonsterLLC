using UnityEngine;
using UnityEngine.UI;

// Healthbar de monstro comum (não-boss) em world space, acima da cabeça, seguindo o monstro.
// Mesma estrutura de 3 imagens do PlayerHealthBarUI (Health_Background, Health_Bar,
// Health_Head_Bar), sem texto. Escuta EnemyController.OnHealthChanged (por instância — ao
// contrário do GameEvents.OnHealthChanged global do herói, cada monstro só reage à própria
// vida, nunca à de um vizinho).
public class MonsterHealthBarUI : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;
    [SerializeField] private Image healthBar;
    [SerializeField] private RectTransform headBar;

    [Header("Posicionamento")]
    [Tooltip("Offset acima do pivô do monstro onde a barra fica (world space).")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Conteúdo visível dentro do canvas das sprites")]
    [Tooltip("Fração (0-1) de onde começa o conteúdo opaco do Health_Bar dentro do próprio RectTransform. Ex.: canvas 64, conteúdo 40 centralizado = 12/64 = 0.1875.")]
    [SerializeField] private float healthBarContentLeftFraction = 0.1875f;
    [Tooltip("Fração (0-1) de onde termina o conteúdo opaco do Health_Bar. Ex.: (12+40)/64 = 0.8125.")]
    [SerializeField] private float healthBarContentRightFraction = 0.8125f;

    private Transform follow;
    private CanvasGroup visibility;

    private void Awake()
    {
        visibility = gameObject.AddComponent<CanvasGroup>();

        // Esperado como filho (direto ou aninhado) do GameObject raiz do monstro — acha o
        // EnemyController sozinho, sem precisar arrastar manualmente em cada um dos prefabs.
        if (enemy == null) enemy = GetComponentInParent<EnemyController>();
        follow = enemy != null ? enemy.transform : transform.parent;
    }

    private void OnEnable()
    {
        if (enemy == null) return;
        enemy.OnHealthChanged += UpdateHealth;
        UpdateHealth(enemy.stats.health, enemy.stats.maxHealth);
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.OnHealthChanged -= UpdateHealth;
    }

    private void LateUpdate()
    {
        if (follow == null) return;
        transform.position = follow.position + worldOffset;
    }

    private void UpdateHealth(float current, float max)
    {
        bool isDead = current <= 0f;
        visibility.alpha = isDead ? 0f : 1f;
        if (isDead) return;

        float fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        HealthBarVisuals.Apply(healthBar, headBar, fillAmount, healthBarContentLeftFraction, healthBarContentRightFraction);
    }
}
