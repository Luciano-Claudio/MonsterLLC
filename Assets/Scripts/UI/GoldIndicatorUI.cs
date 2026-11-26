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
        // Gold permanente (2026-11-26) — o mesmo indicador no jogo e no menu.
        GameEvents.OnPermanentGoldChanged += UpdateLabel;
        UpdateLabel(PermanentGold.Amount);
    }

    private void OnDisable() => GameEvents.OnPermanentGoldChanged -= UpdateLabel;
    private void UpdateLabel(long gold) => label.text = LargeNumberFormatter.Format(gold);
}
