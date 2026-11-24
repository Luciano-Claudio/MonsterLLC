using TMPro;
using UnityEngine;
using UnityEngine.UI;

// D9.5 — tela "Resumo total" da morte, montada pelo usuário na _TestScene (Canvas desativado,
// Sort Order abaixo do da transição pra o CircleWipe revelar). O RunSummaryUI ativa esta tela no
// corte do CircleWipe e preenche os valores; o botão vai pro menu sem transição (RunExit).
public class RunSummaryScreen : MonoBehaviour
{
    [Tooltip("Objeto que liga/desliga a tela inteira (o Canvas do resumo).")]
    [SerializeField] private GameObject root;
    [Tooltip("Dias sobrevividos (formato do usuário: \"X / totalDays\").")]
    [SerializeField] private TMP_Text daysText;
    [Tooltip("Gold permanente coletado (formato do usuário: \"XG\"). Vale 0 até o D9.9.")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button continueButton;
    [Tooltip("Etapas 1+2 — opcional: Vitória ou Derrota.")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("Etapas 1+2 — opcional: tempo sobrevivido (mm:ss).")]
    [SerializeField] private TMP_Text timeText;

    // Etapas 1+2: o texto que mostrava "dias / 10" (daysText) passa a mostrar "bosses / 25" — o
    // usuário pode trocar o rótulo dele; o gold permanente continua 0 até a etapa 6.
    public void Show(bool victory, float secondsSurvived, int bossesKilled, int totalBosses)
    {
        if (resultText != null) resultText.text = RunSummaryRules.ResultText(victory);
        if (timeText != null) timeText.text = RunSummaryRules.FormatTime(secondsSurvived);
        if (daysText != null) daysText.text = $"{bossesKilled} / {totalBosses}";
        if (goldText != null) goldText.text = "0G";
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(RunExit.ToMainMenu);
            continueButton.onClick.AddListener(RunExit.ToMainMenu);
        }
        (root != null ? root : gameObject).SetActive(true);
    }
}
