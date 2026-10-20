using UnityEngine;
using UnityEngine.UI;

// Grade de 40 slots do inventário aberto com TAB (GDD Seção 37). Fica em "Inventory_UI", que só
// ativa de verdade quando o painel "Inventory" (pai) abre pela 1ª vez — por isso o AddComponent
// dos 40 InventorySlotUI mora no Awake (roda 1x, na 1ª ativação) e a assinatura de BagChanged
// mora no OnEnable (roda de novo a cada abertura do TAB).
public class InventoryTabUI : MonoBehaviour
{
    public LootVisualSettings visuals;
    public Sprite unlockedSlotSprite;
    public Sprite lockedSlotSprite;

    public LootVisualSettings Visuals => visuals;

    private InventorySlotUI[] slots;
    private Image[] slotBackgrounds;

    private RectTransform dragGhost;
    private Image dragGhostImage;
    private int dragSourceIndex = -1;

    private Bag CurrentBag => BagController.Instance.Bag;

    private void Awake()
    {
        var container = transform.Find("Inventory_Slots");
        if (container == null)
        {
            Debug.LogError($"[InventoryTabUI] Não achei o filho \"Inventory_Slots\" em \"{name}\" — este componente precisa estar no GameObject \"Inventory_UI\" (não em \"Inventory\" nem em outro).", this);
            enabled = false;
            return;
        }

        int count = container.childCount;
        slots = new InventorySlotUI[count];
        slotBackgrounds = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var slotTransform = container.GetChild(i);
            var slotUI = slotTransform.gameObject.AddComponent<InventorySlotUI>();
            slotUI.Setup(this, i);
            slots[i] = slotUI;
            slotBackgrounds[i] = slotTransform.GetComponent<Image>();
        }

        CreateDragGhost();
    }

    private void OnEnable()
    {
        GameEvents.OnBagChanged += HandleBagChanged;
        Refresh();
    }

    private void OnDisable() => GameEvents.OnBagChanged -= HandleBagChanged;

    private void HandleBagChanged(Bag bag) => Refresh();

    private void Refresh()
    {
        var bag = CurrentBag;
        for (int i = 0; i < slots.Length; i++)
        {
            bool unlocked = i < bag.maxSlots;
            slotBackgrounds[i].sprite = unlocked ? unlockedSlotSprite : lockedSlotSprite;
            slots[i].Refresh(bag.Slots[i], unlocked);
        }
    }

    // Ícone fantasma que acompanha o mouse durante o drag (GDD Seção 37: "mesmo feedback visual
    // de arrastar um arquivo no sistema operacional"). Assume Canvas em Screen Space - Overlay
    // (coordenadas de tela == coordenadas do Canvas); se o Canvas virar Camera/World Space, essa
    // posição precisa passar por RectTransformUtility.ScreenPointToWorldPointInRectangle.
    private void CreateDragGhost()
    {
        var canvas = GetComponentInParent<Canvas>();
        var go = new GameObject("InventoryDragGhost", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        dragGhost = (RectTransform)go.transform;
        dragGhostImage = go.GetComponent<Image>();
        dragGhostImage.raycastTarget = false; // não pode roubar o OnDrop do slot embaixo dele
        dragGhostImage.preserveAspect = true;
        dragGhost.sizeDelta = new Vector2(80, 80);
        dragGhost.SetAsLastSibling();
        go.SetActive(false);
    }

    public void BeginDrag(int index, Sprite sprite, Vector2 screenPosition)
    {
        var bag = CurrentBag;
        if (index >= bag.maxSlots || bag.Slots[index].IsEmpty) return;

        dragSourceIndex = index;
        dragGhostImage.sprite = sprite;
        dragGhost.gameObject.SetActive(true);
        // eventData.position (Input System), nunca UnityEngine.Input.mousePosition — o projeto
        // usa o Input System package (Player Settings), que desliga a classe Input legada.
        dragGhost.position = screenPosition;
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (dragSourceIndex < 0) return;
        dragGhost.position = screenPosition;
    }

    public void EndDrag()
    {
        dragSourceIndex = -1;
        dragGhost.gameObject.SetActive(false);
    }

    // GDD Seção 37 (resolvido 2026-10-18): swap, nunca bloqueio — exceto contra slot travado,
    // que nunca pode receber item (índice >= maxSlots não é "ocupável", é inexistente ainda).
    public void DropOnto(int targetIndex)
    {
        if (dragSourceIndex < 0) return;

        var bag = CurrentBag;
        if (targetIndex < bag.maxSlots && targetIndex != dragSourceIndex)
        {
            bag.Swap(dragSourceIndex, targetIndex);
            GameEvents.BagChanged(bag);
        }
    }
}
