using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private RectTransform headBar;
    [SerializeField] private TMP_Text valueLabel;

    [Header("Conteúdo visível dentro do canvas das sprites")]
    [Tooltip("Fração (0-1) de onde começa o conteúdo opaco do Health_Bar dentro do próprio RectTransform. Ex.: canvas 64, conteúdo 40 centralizado = 12/64 = 0.1875.")]
    [SerializeField] private float healthBarContentLeftFraction = 0.1875f;
    [Tooltip("Fração (0-1) de onde termina o conteúdo opaco do Health_Bar. Ex.: (12+40)/64 = 0.8125.")]
    [SerializeField] private float healthBarContentRightFraction = 0.8125f;

    [Header("Debug — posição do Head_Bar")]
    [Tooltip("Valor de anchoredPosition.x que a fórmula calculou nesta última atualização. Só leitura — mostrado pra comparar com a posição correta.")]
    [SerializeField] private float debugComputedHeadX;
    [Tooltip("Liga pra travar o Head_Bar num X manual (ignora a fórmula) e poder arrastar/digitar até achar a posição certa em Play mode.")]
    [SerializeField] private bool debugOverridePosition;
    [Tooltip("X manual usado quando debugOverridePosition está ligado.")]
    [SerializeField] private float debugOverrideHeadX;

    private CanvasGroup visibility;

    private void Awake()
    {
        visibility = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable() => GameEvents.OnHealthChanged += UpdateHealth;
    private void OnDisable() => GameEvents.OnHealthChanged -= UpdateHealth;

    private void Start()
    {
        // OnHealthChanged só dispara quando a vida muda (dano/cura) — sem isso, antes do
        // primeiro hit o Head_Bar fica com o anchoredPosition salvo na cena, não com a
        // posição calculada.
        var heroObj = GameObject.FindGameObjectWithTag("Player");
        if (heroObj == null) return;
        var hero = heroObj.GetComponent<HeroController>();
        if (hero != null) UpdateHealth(hero.stats.health, hero.stats.maxHealth);
    }

    private void UpdateHealth(float current, float max)
    {
        bool isDead = current <= 0f;
        visibility.alpha = isDead ? 0f : 1f;
        if (isDead) return;

        float fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        HealthBarVisuals.Apply(healthBar, headBar, fillAmount, healthBarContentLeftFraction, healthBarContentRightFraction);

        debugComputedHeadX = headBar.anchoredPosition.x;
        if (debugOverridePosition) headBar.anchoredPosition = new Vector2(debugOverrideHeadX, headBar.anchoredPosition.y);

        valueLabel.text = $"{LargeNumberFormatter.Format(current)}/{LargeNumberFormatter.Format(max)}";
    }
}
