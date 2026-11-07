using UnityEngine;
using UnityEngine.EventSystems;

// Hover de 1 card de herói no Hero Select (GDD Deadline 8.1, Seção 6) — adicionado em runtime
// por MainMenuNavigationUI.WireHeroCards, mesmo critério do InventorySlotUI (AddComponent, não
// pré-colocado na cena). Ainda não existe conceito de herói bloqueado (achado confirmado em
// pesquisa — nem como stub), então todo herói mostra o popup "ativo" por enquanto; entry nula
// (herói sem linha na HeroDisplayTableSO ainda) mostra "???" em vez de quebrar.
public class HeroCardHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string heroKey;
    private HeroDisplayTableSO table;

    public void Setup(string heroKey, HeroDisplayTableSO table)
    {
        this.heroKey = heroKey;
        this.table = table;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string description = table != null && table.TryGetEntry(heroKey, out var entry)
            ? $"HP Base: {entry.baseHp} | Ataque Base: {entry.baseDamage}\n\n{entry.story}"
            : "???";
        PopupController.Instance?.ShowSimple(heroKey, description);
    }

    public void OnPointerExit(PointerEventData eventData) => PopupController.Instance?.Hide();
}
