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
        Debug.Log($"[LootDrop] Coletado: {loot.quantity}x {loot.itemName}");
        GameEvents.LootCollected(loot);
        Destroy(gameObject);
    }
}
