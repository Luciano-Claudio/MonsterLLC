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

        bool hasItem = unlocked && slot != null && !slot.IsEmpty;
        icon.gameObject.SetActive(hasItem);
        quantityLabel.gameObject.SetActive(hasItem);
        if (!hasItem) return;

        icon.sprite = owner.Visuals.GetSprite(slot.itemName, slot.quantity);
        quantityLabel.text = LargeNumberFormatter.Format(slot.quantity);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked) selector.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) => selector.SetActive(false);

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isUnlocked) return;
        owner.BeginDrag(index, icon.sprite, eventData.position);
    }

    public void OnDrag(PointerEventData eventData) => owner.UpdateDrag(eventData.position);
    public void OnEndDrag(PointerEventData eventData) => owner.EndDrag();
    public void OnDrop(PointerEventData eventData) => owner.DropOnto(index);
}
