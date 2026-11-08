using UnityEngine;

// Baú (GDD Deadline 8.1, Seção 8). A recompensa em si (50% gold / 50% abre o Chest_Popup) é
// resolvida por ChestRewardSystem.GrantReward() — fonte única compartilhada com a morte do
// Chest Mimic, pros 2 caminhos darem exatamente a mesma coisa. Decisão do usuário (2026-11-08):
// o baú só existe até ser coletado — é destruído de verdade (não fica como decoração "aberta"),
// na hora (caminho gold) ou quando o Chest_Popup fechar (caminho escolha).
public class Chest : Interactable
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openedSprite;

    private bool opened;

    private void Awake()
    {
        if (spriteRenderer != null && closedSprite != null) spriteRenderer.sprite = closedSprite;
    }

    public override void Interact(Transform interactor)
    {
        if (opened) return;
        opened = true;

        if (spriteRenderer != null && openedSprite != null) spriteRenderer.sprite = openedSprite;

        // Base OnTriggerExit2D só limpa quando o player sai do trigger — se ele interagiu de
        // perto (normal), o prompt de interação ficaria preso num baú já aberto até ele se afastar.
        InteractionManager.Instance.ClearAvailable(this);

        ChestRewardSystem.GrantReward(gameObject);
    }
}
