using UnityEngine;

// Sprint 32 (GDD Seção 38) — física real de "hop" ao nascer e stacking por proximidade com
// itens do MESMO material. Collect() é chamado de fora (HeroController — o Pickup Radius mora
// no player, não aqui, ver Seção 37) em vez de um polling próprio por distância.
// Nasce via `new GameObject()`/`AddComponent<LootDrop>()` em EnemyController.DropLoot() — sem
// prefab nenhum, por isso TODOS os componentes (Rigidbody2D, Collider2D, SpriteRenderer) são
// adicionados aqui mesmo, em Awake(), em vez de pré-configurados num prefab/cena.
public class LootDrop : MonoBehaviour
{
    public LootDefinition loot = new LootDefinition { itemName = "Monster Essence", quantity = 1 };

    // 🔢 chute inicial de balanceamento visual, não calibrado em playtest ainda.
    public float hopForce = 2.5f;
    public float maxSpinSpeed = 180f; // graus/s — só efeito visual, gira livre ao sair voando
    public float linearDrag = 6f; // atrito alto — assenta rápido, sem deslizar longe do ponto de morte
    public float stackMergeRadius = 0.4f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // Trava contra os 2 lados do mesmo encontro tentarem se fundir ao mesmo tempo — só quem tem
    // o InstanceID menor absorve (ver OnTriggerEnter2D); o absorvido marca isso ao ser destruído,
    // pra nenhuma 2ª colisão no mesmo frame processá-lo de novo antes do Destroy() de fato acontecer.
    private bool beingDestroyed;

    private void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // jogo top-down — gravidade real empurraria o item pro "sul" do mapa, não faz sentido aqui
        rb.linearDamping = linearDrag;
        rb.freezeRotation = false; // GDD Seção 38: "pode girar livremente ao cair"

        // 1 collider só, em trigger — não existe nada no mundo que o loot precise colidir de
        // verdade (parar numa parede), e o MESMO trigger serve pra detectar outro loot do mesmo
        // material por perto (stacking) sem precisar de um 2º Collider2D separado.
        var trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = stackMergeRadius;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Hop curto numa direção aleatória + giro visual — só uma vez, no instante em que nasce.
        Vector2 hopDirection = Random.insideUnitCircle.normalized;
        rb.AddForce(hopDirection * hopForce, ForceMode2D.Impulse);
        rb.angularVelocity = Random.Range(-maxSpinSpeed, maxSpinSpeed);

        RefreshSprite();
    }

    // GDD Seção 38: "cada item de loot tem um trigger que detecta outros itens do MESMO tipo
    // próximos — ao detectar, se juntam numa única pilha". Decisão do usuário: 1 dos dois é
    // destruído e absorvido pelo outro (mais simples/barato que uma 3ª entidade "pilha").
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (beingDestroyed) return;

        var otherDrop = other.GetComponent<LootDrop>();
        if (otherDrop == null || otherDrop == this || otherDrop.beingDestroyed) return;
        if (otherDrop.loot.itemName != loot.itemName) return; // só funde com o MESMO material

        // Só quem tem o InstanceID menor age — evita os 2 OnTriggerEnter2D (1 por lado) tentarem
        // absorver um ao outro ao mesmo tempo.
        if (GetInstanceID() < otherDrop.GetInstanceID())
            otherDrop.MergeInto(this);
    }

    private void MergeInto(LootDrop target)
    {
        if (beingDestroyed) return;
        beingDestroyed = true;

        target.loot.quantity += loot.quantity;
        target.RefreshSprite();
        Destroy(gameObject);
    }

    // Reavalia qual dos 4 sprites de pilha usar (1/2+/5+/20+) — chamado sempre que a quantidade
    // muda: nascimento (item já pode nascer grande, Seção 0 item 3), fusão por proximidade, ou
    // coleta parcial reduzindo o que sobra no chão (pode cruzar um limiar pra baixo).
    private void RefreshSprite()
    {
        var visuals = LootVisualSettings.Instance;
        if (visuals == null) return;
        spriteRenderer.sprite = visuals.GetSprite(loot.itemName, loot.quantity);
    }

    // Chamado pelo HeroController.OnTriggerStay2D (Pickup Radius mora no player, GDD Seção 37) —
    // público porque mora numa classe diferente de quem decide "o jogador está perto o
    // suficiente".
    public void Collect()
    {
        if (beingDestroyed) return; // já sendo absorvido por outro loot neste mesmo frame

        int added = BagController.Instance.AddItem(loot.itemName, loot.quantity);

        if (added > 0)
            Debug.Log($"[LootDrop] Coletado: {added}x {loot.itemName}");

        if (added < loot.quantity)
        {
            loot.quantity -= added;
            RefreshSprite();
            Debug.Log($"[LootDrop] Bag cheia — {loot.quantity}x {loot.itemName} continuam no chão.");
            return; // não destrói — a pilha remanescente continua existindo, com a quantidade reduzida
        }

        Destroy(gameObject);
    }
}
