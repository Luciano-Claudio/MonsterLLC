using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Etapas 1+2 — uma das 3 cartas da tela (CardScreenUI). Campos opcionais: o que estiver ligado é
// preenchido. Nível mostrado = o que a carta VAI ter se escolhida.
public class CardSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text level;
    [SerializeField] private Button button;
    [Tooltip("Formato do nível. {0} = nível depois de escolher, {1} = nível máximo.")]
    [SerializeField] private string levelFormat = "Nv {0}/{1}";

    private CardDefinition card;
    private Action<CardDefinition> onClick;

    public void Bind(CardDefinition definition, int currentLevel, Action<CardDefinition> clicked)
    {
        card = definition;
        onClick = clicked;
        gameObject.SetActive(true);
        if (icon != null) { icon.sprite = definition.icon; icon.enabled = definition.icon != null; }
        if (title != null) title.text = definition.title;
        if (description != null) description.text = string.Format(definition.description ?? "", ValueText(definition));
        if (level != null) level.text = string.Format(levelFormat, currentLevel + 1, definition.maxLevel);
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    // "+25%" do próximo nível: dano multiplica (1,25 -> 25%); o resto soma (0,15 -> 15%).
    private static string ValueText(CardDefinition d)
    {
        float percent = d.Multiplicative ? (d.valuePerLevel - 1f) * 100f : d.valuePerLevel * 100f;
        return $"{percent:0.#}%";
    }

    private void HandleClick()
    {
        if (card != null) onClick?.Invoke(card);
    }
}
