using System.Collections.Generic;
using UnityEngine;

// Sprint 32 (adiantamento, debatido com o usuário em 2026-10-20) — efeito 100% cosmético: a
// venda real (Bag esvazia, gold entra) já acontece instantânea em Vendor.Interact(), isto aqui
// nunca atrasa nem bloqueia nada da economia. É só a "mentira visual" de que o loot vendido sai
// voando do herói até o carrinho e vai empilhando lá.
//
// A "pilha" é um pool FIXO de sprites pré-posicionados (layout triangular, tipo pins de
// boliche), todos desativados até serem "preenchidos" — nunca cria/destrói 1 objeto por unidade
// vendida de verdade, e nunca empilha fisicamente nada (sem Rigidbody2D/Collider2D aqui, é
// decoração pura). 1 pilha só, mista (qualquer material cai no próximo slot livre) — decisão do
// usuário. Reseta no início de cada dia.
//
// Timer manual em Update(), NUNCA Coroutine com WaitForSeconds — mesmo motivo já documentado em
// outros scripts do projeto: Coroutine não respeita o GameplayGate/TimeManager.IsPaused.
public class VendorCart : MonoBehaviour
{
    [Header("Pilha fake (pool de slots pré-posicionados, formato triangular)")]
    public int pileRows = 5;
    public int baseRowCount = 8; // quantos slots na linha de baixo; cada linha de cima tem 1 menos
    public float slotSpacingX = 0.3f;
    public float slotSpacingY = 0.25f;
    public float pileOffsetX = 0f; // desloca a pirâmide inteira (local, relativo ao Cart)
    public float pileOffsetY = 0f;

    [Header("Lote de saída — GDD, ajustável sem recompilar")]
    public int smallSaleThreshold = 50;   // quantidade <= isso: 1 por vez
    public int mediumSaleThreshold = 200; // <= isso: 2 por vez
    public int largeSaleThreshold = 1000; // <= isso: 5 por vez
    // acima de largeSaleThreshold: 20 por vez (escala pra cima se passar do teto abaixo)

    [Header("Teto de arcos visuais por venda — 🔢 chute inicial, calibrar ao vivo")]
    public int maxVisualArcsPerSale = 40;

    [Header("Timing do arco (herói → carrinho)")]
    public float arcDuration = 0.5f;
    public float arcHeight = 1.5f;
    public float arcStagger = 0.05f; // intervalo entre o lançamento de 1 arco e o próximo
    public Vector3 flyingIconScale = new(0.5f, 0.5f, 0.5f);

    private readonly List<SpriteRenderer> pileSlots = new();
    private int nextFreeSlot;
    private float elapsedTime;

    private struct PendingArc
    {
        public string itemName;
        public long representedQuantity; // quanto esse arco "representa" de verdade, pra sprite
        public Vector3 origin;
        public float readyAt; // em elapsedTime
    }

    private class ActiveArc
    {
        public string itemName;
        public long representedQuantity;
        public int slotIndex; // -1 = pilha já cheia, voa mas não deixa nada ao chegar
        public Vector3 start;
        public Vector3 end;
        public float elapsed;
        public Transform visual;
    }

    private readonly List<PendingArc> queue = new();
    private readonly List<ActiveArc> activeArcs = new();

    private void Awake() => BuildPile();

    private void OnEnable() => GameEvents.OnDayStart += ResetPile;
    private void OnDisable() => GameEvents.OnDayStart -= ResetPile;

    private void BuildPile()
    {
        for (int row = 0; row < pileRows; row++)
        {
            int countInRow = baseRowCount - row;
            if (countInRow <= 0) break;

            float y = row * slotSpacingY + pileOffsetY;
            float rowWidth = (countInRow - 1) * slotSpacingX;

            for (int i = 0; i < countInRow; i++)
            {
                float x = -rowWidth * 0.5f + i * slotSpacingX + pileOffsetX;

                var slotObj = new GameObject($"PileSlot_{row}_{i}");
                slotObj.transform.SetParent(transform, false);
                slotObj.transform.localPosition = new Vector3(x, y, 0);

                var sr = slotObj.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 1; // acima do sprite do próprio Cart (Foreground, order 0)
                slotObj.SetActive(false);

                pileSlots.Add(sr);
            }
        }
    }

    private void ResetPile()
    {
        foreach (var slot in pileSlots) slot.gameObject.SetActive(false);
        nextFreeSlot = 0;

        queue.Clear();
        foreach (var arc in activeArcs)
            if (arc.visual != null) Destroy(arc.visual.gameObject);
        activeArcs.Clear();
    }

    // Chamado por Vendor.Interact() — 1 vez por material vendido. origin é a posição do herói
    // NO INSTANTE da venda (não acompanha o herói se ele andar durante a animação, de propósito
    // — arcos são curtos, não compensa a complexidade de perseguir um alvo móvel pra um efeito
    // cosmético).
    public void AcceptSale(string itemName, long quantity, Vector3 origin)
    {
        if (quantity <= 0) return;

        // double em vez de Mathf.CeilToInt/float — quantity já passa de 2,1 bilhões no Tier
        // 10 do Bestiário (Mathf.CeilToInt devolve int, reintroduziria o mesmo teto de
        // conversão que quebrou os prefabs). visualEvents em si fica pequeno de verdade (é
        // sempre travado em maxVisualArcsPerSale logo abaixo), só chunkSize/quantity que
        // precisam do range maior.
        long chunkSize = ResolveChunkSize(quantity);
        int visualEvents = (int)System.Math.Ceiling(quantity / (double)chunkSize);

        // Teto de eventos visuais — nunca deixa uma venda gigante virar uma animação gigante.
        // O que cada arco REPRESENTA escala pra cima (a sprite usada já não distingue 20 de
        // 200.000, sempre mostra a mesma "pilha grande" — GetSprite não tem teto superior).
        if (visualEvents > maxVisualArcsPerSale)
        {
            visualEvents = maxVisualArcsPerSale;
            chunkSize = (long)System.Math.Ceiling(quantity / (double)visualEvents);
        }

        for (int i = 0; i < visualEvents; i++)
        {
            queue.Add(new PendingArc
            {
                itemName = itemName,
                representedQuantity = chunkSize,
                origin = origin,
                readyAt = elapsedTime + i * arcStagger,
            });
        }
    }

    private long ResolveChunkSize(long quantity)
    {
        if (quantity <= smallSaleThreshold) return 1;
        if (quantity <= mediumSaleThreshold) return 2;
        if (quantity <= largeSaleThreshold) return 5;
        return 20;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        elapsedTime += Time.deltaTime;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (queue[i].readyAt > elapsedTime) continue;
            LaunchArc(queue[i]);
            queue.RemoveAt(i);
        }

        for (int i = activeArcs.Count - 1; i >= 0; i--)
        {
            var arc = activeArcs[i];
            arc.elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(arc.elapsed / arcDuration);

            Vector3 pos = Vector3.Lerp(arc.start, arc.end, t);
            pos.y += arcHeight * 4f * t * (1f - t); // parábola simples (0 nas pontas, pico no meio)
            arc.visual.position = pos;

            if (t >= 1f)
            {
                LandArc(arc);
                activeArcs.RemoveAt(i);
            }
        }
    }

    private void LaunchArc(PendingArc pending)
    {
        // Reserva o slot JÁ no lançamento (não na chegada) — senão 2 arcos em voo ao mesmo
        // tempo mirariam no mesmo "próximo slot livre" e chegariam um em cima do outro.
        int slotIndex = nextFreeSlot < pileSlots.Count ? nextFreeSlot : -1;
        if (slotIndex >= 0) nextFreeSlot++;

        var visualObj = new GameObject($"FlyingLoot_{pending.itemName}");
        visualObj.transform.position = pending.origin;
        visualObj.transform.localScale = flyingIconScale;

        var sr = visualObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 1; // mesmo critério das piles — acima do sprite do Cart (order 0)
        var visuals = LootVisualSettings.Instance;
        if (visuals != null) sr.sprite = visuals.GetSprite(pending.itemName, pending.representedQuantity);

        Vector3 target = slotIndex >= 0 ? pileSlots[slotIndex].transform.position : transform.position;

        activeArcs.Add(new ActiveArc
        {
            itemName = pending.itemName,
            representedQuantity = pending.representedQuantity,
            slotIndex = slotIndex,
            start = pending.origin,
            end = target,
            elapsed = 0f,
            visual = visualObj.transform,
        });
    }

    private void LandArc(ActiveArc arc)
    {
        if (arc.slotIndex >= 0)
        {
            var slot = pileSlots[arc.slotIndex];
            var visuals = LootVisualSettings.Instance;
            if (visuals != null) slot.sprite = visuals.GetSprite(arc.itemName, arc.representedQuantity);
            slot.transform.localScale = flyingIconScale;
            slot.gameObject.SetActive(true);
        }

        Destroy(arc.visual.gameObject);
    }
}
