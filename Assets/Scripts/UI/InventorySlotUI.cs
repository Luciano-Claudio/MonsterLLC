using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1 dos 40 slots fixos do TAB (GDD Seção 37) — criado em runtime via AddComponent pelo
// InventoryTabUI, nunca pré-colocado na cena (os 40 Slot_N já existem na hierarquia, feitos
// à mão pelo usuário; isso só liga o comportamento neles).
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private InventoryTabUI owner;
    private int index;
    private bool isUnlocked;
    private bool hasItem;
    private InventorySlot currentSlot;

    private Image icon;
    private TMP_Text quantityLabel;
    private GameObject selector;

    private void Awake()
    {
        icon = transform.Find("Item").GetComponent<Image>();
        quantityLabel = transform.Find("Quantity").GetComponent<TMP_Text>();
        selector = transform.Find("Selector").gameObject;
        selector.SetActive(false);
    }

    public void Setup(InventoryTabUI owner, int index)
    {
        this.owner = owner;
        this.index = index;
    }

    public void Refresh(InventorySlot slot, bool unlocked)
    {
        isUnlocked = unlocked;
        currentSlot = slot;
        hasItem = unlocked && slot != null && !slot.IsEmpty;
        icon.gameObject.SetActive(hasItem);
        quantityLabel.gameObject.SetActive(hasItem);
        if (!hasItem) return;

        icon.sprite = owner.Visuals.GetSprite(slot.itemName, slot.quantity);
        quantityLabel.text = LargeNumberFormatter.Format(slot.quantity);
    }

    // Popup de hover (GDD Deadline 8.1, Seção 6: "nome do item + preço") — só pra slot
    // desbloqueado com item e com preço cadastrado em LootPricingSettings.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isUnlocked) return;
        selector.SetActive(true);

        if (hasItem && owner.Pricing != null && owner.Pricing.TryGetPrice(currentSlot.itemName, out long sellValue))
        {
            owner.Pricing.TryGetDescription(currentSlot.itemName, out string description);
            PopupController.Instance?.ShowSimple(gameObject, currentSlot.itemName, description ?? "???", $"{LargeNumberFormatter.Format(sellValue)}g");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        selector.SetActive(false);
        PopupController.Instance?.Hide();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isUnlocked) return;
        owner.BeginDrag(index, icon.sprite, eventData.position);
    }

    public void OnDrag(PointerEventData eventData) => owner.UpdateDrag(eventData.position);
    public void OnEndDrag(PointerEventData eventData) => owner.EndDrag();
    public void OnDrop(PointerEventData eventData) => owner.DropOnto(index);
}
