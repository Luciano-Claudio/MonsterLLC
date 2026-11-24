using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Uma das 3 cartas do Cards_Popup (CardScreenUI). Formato montado pelo usuário: o próprio "Card"
// (Image = fundo da carta + Button) com os filhos Title (tipo: Item/Upgrade/Stat, na cor do tipo),
// Name, Description e Icon. Fundo, rótulo e cor vêm do estilo do tipo (CardScreenUI); o resto da carta.
public class CardSlotUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [Tooltip("Rótulo do tipo (Item / Upgrade / Stat).")]
    [SerializeField] private TMP_Text kindLabel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text description;
    [Tooltip("Opcional: nível que a carta vai ter se escolhida.")]
    [SerializeField] private TMP_Text level;
    [SerializeField] private Button button;
    [Tooltip("Formato do nível. {0} = nível depois de escolher, {1} = nível máximo.")]
    [SerializeField] private string levelFormat = "Nv {0}/{1}";

    private CardDefinition card;
    private Action<CardDefinition> onClick;

    public void Bind(CardDefinition definition, int currentLevel, CardScreenUI.KindStyle style, Action<CardDefinition> clicked)
    {
        card = definition;
        onClick = clicked;
        gameObject.SetActive(true);
        if (background != null && style != null && style.background != null) background.sprite = style.background;
        if (kindLabel != null && style != null) { kindLabel.text = style.label; kindLabel.color = style.labelColor; }
        if (icon != null) { icon.sprite = definition.icon; icon.enabled = definition.icon != null; }
        if (nameText != null) nameText.text = definition.title;
        if (description != null) description.text = string.Format(definition.description ?? "", ValueText(definition));
        if (level != null) level.text = string.Format(levelFormat, currentLevel + 1, definition.maxLevel);
        if (button != null)
        {
            button.onClick.RemoveListener(Click);
            button.onClick.AddListener(Click);
        }
    }

    // "+25%" do próximo nível: dano multiplica (1,25 -> 25%); o resto soma (0,15 -> 15%).
    private static string ValueText(CardDefinition d)
    {
        float percent = d.Multiplicative ? (d.valuePerLevel - 1f) * 100f : d.valuePerLevel * 100f;
        return $"{percent:0.#}%";
    }

    // Clique ou tecla 1/2/3 (CardScreenUI).
    public void Click()
    {
        if (card != null && gameObject.activeInHierarchy) onClick?.Invoke(card);
    }
}
