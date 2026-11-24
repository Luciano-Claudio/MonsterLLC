using TMPro;
using UnityEngine;
using UnityEngine.UI;

// D9.6b — XP_Background/XP_Bar/XP_Head_Bar/XP_Value — mesma lógica do DayTimeBarUI e das
// healthbars (fill + head acompanhando, via HealthBarVisuals). Lê o estado do HeroLevel
// (RunState) — nenhum estado próprio. Só reescreve a barra/texto quando o XP ou o nível mudam.
public class XpBarUI : MonoBehaviour
{
    [SerializeField] private Image xpBar;
    [SerializeField] private RectTransform headBar;
    [SerializeField] private TMP_Text valueLabel;
    [Tooltip("Opcional: texto do nível. {0} = nível.")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private string levelFormat = "Lv {0}";
    [Tooltip("Texto do XP_Value no nível máximo.")]
    [SerializeField] private string maxLabel = "MAX";

    [Header("Conteúdo visível dentro do canvas das sprites")]
    [Tooltip("Fração (0-1) de onde começa o conteúdo opaco do XP_Bar. Sprite UI_Inventory_2 é opaca de ponta a ponta -> 0.")]
    [SerializeField] private float contentLeftFraction = 0f;
    [Tooltip("Fração (0-1) de onde termina o conteúdo opaco do XP_Bar -> 1.")]
    [SerializeField] private float contentRightFraction = 1f;

    private int shownLevel = -1;
    private long shownXp = -1;

    private void Update()
    {
        var level = HeroLevel.Instance;
        if (level == null) return;
        if (level.Level == shownLevel && level.XpIntoLevel == shownXp) return;

        long toNext = level.XpToNext;
        bool max = toNext == long.MaxValue || toNext <= 0;
        float fill = max ? 1f : Mathf.Clamp01((float)level.XpIntoLevel / toNext);
        if (xpBar != null) HealthBarVisuals.Apply(xpBar, headBar, fill, contentLeftFraction, contentRightFraction);

        if (valueLabel != null) valueLabel.text = max ? maxLabel : $"{level.XpIntoLevel}/{toNext}";
        if (levelText != null && level.Level != shownLevel) levelText.text = string.Format(levelFormat, level.Level);

        shownLevel = level.Level;
        shownXp = level.XpIntoLevel;
    }
}
