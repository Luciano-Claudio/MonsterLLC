using TMPro;
using UnityEngine;
using UnityEngine.UI;

// D9.6b — barra de XP do HUD. O usuário monta o visual na _TestScene (como fez com o Resumo
// total) e liga aqui: uma Image de preenchimento (Image Type = Filled) e o texto do nível.
// Lê o estado do HeroLevel (RunState) — nenhum estado próprio.
public class XpBarUI : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text levelText;
    [Tooltip("Formato do texto do nível. {0} = nível.")]
    [SerializeField] private string levelFormat = "Lv {0}";

    private int shownLevel = -1;

    private void Update()
    {
        var level = HeroLevel.Instance;
        if (level == null) return;

        if (fill != null)
        {
            long toNext = level.XpToNext;
            fill.fillAmount = toNext == long.MaxValue || toNext <= 0 ? 1f : Mathf.Clamp01((float)level.XpIntoLevel / toNext);
        }

        if (levelText != null && level.Level != shownLevel)
        {
            shownLevel = level.Level;
            levelText.text = string.Format(levelFormat, shownLevel);
        }
    }
}
