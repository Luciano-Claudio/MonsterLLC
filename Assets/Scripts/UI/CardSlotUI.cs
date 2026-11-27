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
    // Fundo do tipo da carta atual. O Animator do card (Write Defaults ligado; o clipe Disabled anima o
    // sprite) escreve de volta o sprite da carta anterior depois de um reroll (bug real, 2026-11-27: item com
    // fundo de upgrade e vice-versa). Depois que o Animator roda, o card repõe o fundo certo.
    private Sprite boundBackground;

    private void LateUpdate()
    {
        if (background == null || boundBackground == null || background.sprite == boundBackground) return;
        if (button != null && !button.IsInteractable()) return; // Disabled: o verso da carta é de propósito
        background.sprite = boundBackground;
    }

    public void Bind(CardDefinition definition, int currentLevel, CardScreenUI.KindStyle style, Action<CardDefinition> clicked)
    {
        card = definition;
        onClick = clicked;
        gameObject.SetActive(true);
        boundBackground = style != null ? style.background : null;
        if (background != null && boundBackground != null) background.sprite = boundBackground;
        if (kindLabel != null && style != null) { kindLabel.text = style.label; kindLabel.color = style.labelColor; }
        if (icon != null) { icon.sprite = definition.icon; icon.enabled = definition.icon != null; }
        if (nameText != null) nameText.text = definition.title;
        // {0} = ganho do próximo nível; {1} = vida do item por nível (etapa 3).
        if (description != null) description.text = string.Format(definition.description ?? "", ValueText(definition, currentLevel), $"{definition.healthPerLevel * 100f:0.#}%");
        if (level != null) level.text = string.Format(levelFormat, currentLevel + 1, definition.maxLevel);
        if (button != null)
        {
            button.onClick.RemoveListener(Click);
            button.onClick.AddListener(Click);
        }
    }

    // "+25%" do próximo nível: dano multiplica (1,25 -> 25%); o resto soma (0,15 -> 15%). No 1º nível,
    // o valor base entra junto (Blast Charm: 5% + 5% = 10%).
    private static string ValueText(CardDefinition d, int currentLevel)
    {
        if (d.stat == CardStat.FlameDuration) return $"{d.valuePerLevel:0}"; // Lasting Flames: +1 animação
        if (d.stat == CardStat.ProjectileCount) return $"{d.valuePerLevel + (currentLevel == 0 ? d.baseValue : 0f):0}"; // etapa 4
        float percent = d.Multiplicative ? (d.valuePerLevel - 1f) * 100f
            : (d.valuePerLevel + (currentLevel == 0 ? d.baseValue : 0f)) * 100f;
        return $"{percent:0.#}%";
    }

    // Clique ou tecla 1/2/3 (CardScreenUI).
    public void Click()
    {
        if (card != null && gameObject.activeInHierarchy) onClick?.Invoke(card);
    }
}
