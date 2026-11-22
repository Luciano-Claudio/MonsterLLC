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
    [Tooltip("Total de dias da run (mostrado depois da barra).")]
    [SerializeField] private int totalDays = 10;

    public void Show(int daysSurvived, long permanentGold)
    {
        if (daysText != null) daysText.text = $"{daysSurvived} / {totalDays}";
        if (goldText != null) goldText.text = $"{permanentGold}G";
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(RunExit.ToMainMenu);
            continueButton.onClick.AddListener(RunExit.ToMainMenu);
        }
        (root != null ? root : gameObject).SetActive(true);
    }
}
