using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Loja permanente (docs/superpowers/specs/2026-11-26-loja-permanente-design.md) — o Stats_Painel do menu.
// Acha tudo pelo nome dentro do painel (mesmo padrão do MainMenuNavigationUI): o usuário monta e estiliza
// a cena, o código só preenche. Adicionado em tempo de execução pelo MainMenuNavigationUI.
//   Background/Hero_Image, Background/Text (TMP) (topo), Background/Sell_Everything,
//   Background/Scroll View/Viewport/Content/<Herói>/<buttonName>/{Name_Permanent_Stat, Background_icon/Image, Pips/Pip...},
//   Background/Background_Description/{Background_icon/Image, Name_Permanent_Stat, Description_Permanent_Stat, Value, Buy, Active}
public class PermanentShopUI : MonoBehaviour
{
    private const string HeaderText = "Permanent Upgrades";
    private const string SellText = "Refund All Upgrades";

    private string hero;
    private IReadOnlyList<PermanentUpgradeDefinition> upgrades = new List<PermanentUpgradeDefinition>();
    private readonly Dictionary<PermanentUpgradeDefinition, Button> buttons = new();
    private readonly Dictionary<Image, Sprite> pipOffSprite = new();
    private bool wired;

    private Transform content;
    private Image heroImage;
    private Transform description;
    private Image descIcon;
    private TMP_Text descName, descText, descValue;
    private Button buyButton;
    private Toggle activeToggle;
    private Sprite pipOnSprite;

    public PermanentUpgradeDefinition Selected { get; private set; }

    private void Wire()
    {
        if (wired) return;
        wired = true;
        content = FindDeep(transform, "Content");
        heroImage = FindDeep(transform, "Hero_Image")?.GetComponent<Image>();
        description = FindDeep(transform, "Background_Description");
        if (description != null)
        {
            descIcon = description.Find("Background_icon/Image")?.GetComponent<Image>();
            descName = description.Find("Name_Permanent_Stat")?.GetComponent<TMP_Text>();
            descText = description.Find("Description_Permanent_Stat")?.GetComponent<TMP_Text>();
            descValue = description.Find("Value")?.GetComponent<TMP_Text>();
            buyButton = description.Find("Buy")?.GetComponent<Button>();
            activeToggle = description.Find("Active")?.GetComponent<Toggle>();
        }
        if (activeToggle != null && activeToggle.graphic is Image on) pipOnSprite = on.sprite;
        if (buyButton != null) buyButton.onClick.AddListener(Buy);
        if (activeToggle != null) activeToggle.onValueChanged.AddListener(OnActiveChanged);
        var sell = FindDeep(transform, "Sell_Everything");
        if (sell != null)
        {
            sell.GetComponent<Button>()?.onClick.AddListener(RefundAll);
            var label = sell.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = SellText;
        }
        var header = transform.Find("Background/Text (TMP)")?.GetComponent<TMP_Text>();
        if (header != null) header.text = HeaderText;
    }

    private void OnEnable() => GameEvents.OnPermanentGoldChanged += HandleGoldChanged;
    private void OnDisable() => GameEvents.OnPermanentGoldChanged -= HandleGoldChanged;
    private void HandleGoldChanged(long gold) => RefreshDescription();

    // Abre o painel do herói: só o grupo dele no Content, Damage selecionado.
    public void Open(string heroKey)
    {
        Wire();
        hero = heroKey;
        var catalog = PermanentUpgradeCatalog.Load();
        upgrades = catalog != null ? catalog.For(heroKey) : new List<PermanentUpgradeDefinition>();
        var entry = catalog != null ? catalog.Find(heroKey) : null;
        if (heroImage != null && entry != null && entry.heroImage != null) heroImage.sprite = entry.heroImage;

        buttons.Clear();
        Transform group = null;
        if (content != null)
            foreach (Transform g in content)
            {
                bool mine = g.name == heroKey;
                g.gameObject.SetActive(mine);
                if (mine) group = g;
            }
        if (group != null)
            foreach (var d in upgrades)
            {
                if (d == null) continue;
                var t = group.Find(d.buttonName);
                var button = t != null ? t.GetComponent<Button>() : null;
                if (button == null) { Debug.LogWarning($"[PermanentShopUI] Botão '{d.buttonName}' não achado em {heroKey}."); continue; }
                buttons[d] = button;
                var label = t.Find("Name_Permanent_Stat")?.GetComponent<TMP_Text>();
                if (label != null) label.text = d.title;
                var captured = d;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Select(captured));
            }

        RefreshAllPips();
        PermanentUpgradeDefinition first = null;
        foreach (var d in upgrades) if (d != null && d.stat == PermanentStat.Damage) { first = d; break; }
        Select(first ?? (upgrades.Count > 0 ? upgrades[0] : null));
    }

    public void Select(PermanentUpgradeDefinition d)
    {
        Selected = d;
        KeepSelectedVisual();
        RefreshDescription();
    }

    public void Buy()
    {
        if (Selected == null || string.IsNullOrEmpty(hero)) return;
        if (PermanentShop.Buy(hero, Selected)) RefreshPips(Selected);
        RefreshDescription();
        KeepSelectedVisual();
    }

    public void RefundAll()
    {
        if (string.IsNullOrEmpty(hero)) return;
        PermanentShop.RefundAll(hero, upgrades);
        RefreshAllPips();
        RefreshDescription();
        KeepSelectedVisual();
    }

    private void OnActiveChanged(bool on)
    {
        if (Selected == null || string.IsNullOrEmpty(hero)) return;
        if (PermanentShop.IsActive(hero, Selected.id) != on) PermanentShop.SetActive(hero, Selected.id, on);
        KeepSelectedVisual();
    }

    private void RefreshDescription()
    {
        if (Selected == null || description == null) return;
        if (descIcon != null) descIcon.sprite = Selected.icon;
        if (descName != null) descName.text = Selected.title;
        if (descText != null) descText.text = Selected.description;
        int level = PermanentShop.Level(hero, Selected.id);
        int price = PermanentShop.NextPrice(Selected, level);
        bool maxed = price < 0;
        if (descValue != null)
        {
            descValue.gameObject.SetActive(!maxed);
            if (!maxed) descValue.text = price.ToString();
        }
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!maxed);
            buyButton.interactable = !maxed && PermanentShop.CanBuy(hero, Selected);
        }
        if (activeToggle != null)
        {
            activeToggle.gameObject.SetActive(maxed);
            if (maxed) activeToggle.SetIsOnWithoutNotify(PermanentShop.IsActive(hero, Selected.id));
        }
    }

    private void RefreshAllPips()
    {
        foreach (var kv in buttons) RefreshPips(kv.Key);
    }

    // Pips: exatamente maxLevel visíveis; os comprados com o sprite "ligado" do Toggle Active.
    private void RefreshPips(PermanentUpgradeDefinition d)
    {
        if (!buttons.TryGetValue(d, out var button)) return;
        var pips = button.transform.Find("Pips");
        if (pips == null) return;
        int level = PermanentShop.Level(hero, d.id);
        for (int i = 0; i < pips.childCount; i++)
        {
            var pip = pips.GetChild(i);
            pip.gameObject.SetActive(i < d.maxLevel);
            var image = pip.GetComponent<Image>();
            if (image == null) continue;
            if (!pipOffSprite.ContainsKey(image)) pipOffSprite[image] = image.sprite;
            image.sprite = i < level && pipOnSprite != null ? pipOnSprite : pipOffSprite[image];
        }
    }

    // O botão escolhido fica no estado Selected até o jogador escolher outro (Buy e Toggle não tiram).
    private void KeepSelectedVisual()
    {
        if (Selected == null || !buttons.TryGetValue(Selected, out var button)) return;
        var es = EventSystem.current;
        if (es != null && es.currentSelectedGameObject != button.gameObject) es.SetSelectedGameObject(button.gameObject);
    }

    private void LateUpdate()
    {
        if (Selected != null) KeepSelectedVisual();
    }

    private static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
