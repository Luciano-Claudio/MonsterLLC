using UnityEngine;

public class LootDrop : MonoBehaviour
{
    public LootDefinition loot = new LootDefinition { itemName = "Monster Essence", quantity = 1 };
    public float pickupRadius = 1f;

    private void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (Vector2.Distance(transform.position, player.transform.position) <= pickupRadius)
            Collect();
    }

    private void Collect()
    {
        int added = BagController.Instance.AddItem(loot.itemName, loot.quantity);

        if (added > 0)
            Debug.Log($"[LootDrop] Coletado: {added}x {loot.itemName}");

        if (added < loot.quantity)
        {
            loot.quantity -= added;
            Debug.Log($"[LootDrop] Bag cheia — {loot.quantity}x {loot.itemName} continuam no chão.");
            return; // não destrói — a pilha remanescente continua existindo, com a quantidade reduzida
        }

        Destroy(gameObject);
    }
}
