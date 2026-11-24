using UnityEngine;

// Vendedor itinerante — etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.5).
// Criado em runtime pelo WorldChestSpawnManager nos segundos sorteados da run. Fica no mapa até ser
// usado: interagir abre 3 cartas de ITEM de graça (HeroCards, fonte Vendor) e ele vai embora.
// Não existe mais venda de materiais nem gold na run. O Vendor fixo antigo da cena se desliga sozinho.
public class Vendor : Interactable
{
    // A seta de borda de tela (WorldTargetPointerUI) aponta para o vendedor vivo.
    public static Vendor Instance { get; private set; }

    [Tooltip("Carrinho decorativo do Vendor antigo da cena — só é desligado junto com ele.")]
    public VendorCart cart;

    private bool spawnedByManager;
    private bool used;

    // Chamado pelo WorldChestSpawnManager logo depois de criar o vendedor.
    public void MarkSpawned()
    {
        spawnedByManager = true;
        Instance = this;
        var animator = GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("OpenTrigger");
    }

    private void Start()
    {
        if (spawnedByManager) return;
        if (cart != null) cart.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void Interact(Transform interactor)
    {
        if (used) return;
        var cards = HeroCards.Instance;
        if (cards == null || !cards.CanOffer(OfferSource.Vendor)) return; // nada possível: o vendedor fica
        used = true;
        if (InteractionManager.Instance != null) InteractionManager.Instance.ClearAvailable(this);
        cards.EnqueueOffer(OfferSource.Vendor);
        if (Instance == this) Instance = null;
        Destroy(gameObject);
    }
}
