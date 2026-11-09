using UnityEngine;
using TMPro;

public class GoldIndicatorUI : MonoBehaviour
{
    public TMP_Text label;

    // Resincroniza na hora de ativar (extensão pedida pelo usuário: Gold do Perk_Painel/
    // Talent_Painel, que começam desativados e só ligam de verdade na 1ª vez que o jogador troca
    // de aba) — sem isto, o texto ficava preso no placeholder da cena até o PRÓXIMO gold mudar,
    // porque qualquer GameEvents.GoldChanged disparado enquanto o painel estava desativado nunca
    // chegava a este listener (nunca inscrito ainda).
    private void OnEnable()
    {
        GameEvents.OnGoldChanged += UpdateLabel;
        UpdateLabel(MainMenuUI.CurrentRun?.gold ?? 0);
    }

    private void OnDisable() => GameEvents.OnGoldChanged -= UpdateLabel;
    private void UpdateLabel(long gold) => label.text = LargeNumberFormatter.Format(gold);
}
