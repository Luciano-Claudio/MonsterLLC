using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.1) — gemas
// de XP no chão. Todo monstro morto deixa gemas (1 XP cada; mini boss/boss deixam várias). O herói
// coleta passando por cima; dentro do raio do ímã (RunSettings × carta de ímã) a gema voa até ele.
// Sem física de propósito (D9.3: centenas de colliders derrubam o FPS): este pool atualiza todas as
// gemas num único Update, por distância. Limite de objetos (RunSettings.maxGems): passou dele, uma
// gema que já existe absorve o XP da nova — nada de XP se perde e nada de objeto novo nasce.
// Gold permanente (2026-11-26): o mesmo pool cuida das moedas de gold (SpawnGold) — mesmo ímã, mesma
// coleta; a moeda vai para PermanentGold. Gema e moeda são prefabs com animação em loop
// (RunSettings.gemPrefab / goldCoinPrefab), instanciados 1x e reaproveitados.
public class XpGemPool : MonoBehaviour
{
    public static XpGemPool Instance { get; private set; }

    private const float CollectDistance = 0.3f;

    private readonly List<Gem> active = new();
    private readonly Stack<Gem> free = new();
    private readonly Stack<Gem> freeGold = new();
    private RunSettings settings;
    private int sortingLayerId;
    private bool renderInfoLoaded;

    private class Gem
    {
        public GameObject go;
        public Transform transform;
        public SpriteRenderer renderer;
        public int xp;      // XP da gema, ou gold da moeda
        public bool gold;   // moeda de gold permanente
        public bool attracted;
    }

    public int ActiveCount => active.Count;

    // Raio do ímã: base do RunSettings × perks de coleta × carta de ímã.
    public float MagnetRadius => HeroController.Cards.Sum(CardAbility.None, CardStat.CollectorsInstinct) > 0f
        ? CollectorsInstinctRadius // catálogo de cartas — Collector's Instinct: toda gema vem
        : Settings.gemMagnetRadius * HeroController.PickupRadiusMultiplier * HeroController.Cards.Get(CardAbility.None, CardStat.Magnet);
    private const float CollectorsInstinctRadius = 100000f;

    public long ActiveXpSum
    {
        get
        {
            long sum = 0;
            for (int i = 0; i < active.Count; i++) if (!active[i].gold) sum += active[i].xp;
            return sum;
        }
    }

    private void Awake() => Instance = this;
    // O pool mora no Systems (sobrevive à troca de cena): limpa no início e no fim da run.
    private void OnEnable() { GameEvents.OnDayStart += ClearAll; GameEvents.OnRunEnded += ClearOnRunEnd; }
    private void OnDisable() { GameEvents.OnDayStart -= ClearAll; GameEvents.OnRunEnded -= ClearOnRunEnd; }
    private void ClearOnRunEnd(bool victory) => ClearAll();

    private RunSettings Settings => settings != null ? settings : settings = RunSettings.Current;

    public void Spawn(Vector2 position, int xp)
    {
        var hero = HeroController.Instance;
        Spawn(position, xp, hero != null ? (Vector2)hero.transform.position : position);
    }

    // No limite: a gema MAIS LONGE do herói (a que ficou pra trás) é trazida pro lugar do kill
    // levando o XP dela junto — o XP nunca se perde nem fica preso longe (revisão final, achado 1).
    public void Spawn(Vector2 position, int xp, Vector2 heroPosition)
    {
        if (xp <= 0) return;
        if (active.Count >= Mathf.Max(1, Settings.maxGems))
        {
            Gem target = null;
            float farthestSqr = -1f;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].gold) continue; // moeda nunca absorve XP
                float sqr = ((Vector2)active[i].transform.position - heroPosition).sqrMagnitude;
                if (sqr > farthestSqr) { farthestSqr = sqr; target = active[i]; }
            }
            if (target == null) { SpawnItem(position, xp, false); return; }
            target.xp += xp;
            target.attracted = false;
            target.transform.position = position;
            target.transform.localScale = Vector3.one * Settings.gemScale * 1.5f;
            return;
        }

        SpawnItem(position, xp, false);
    }

    // Moeda de gold permanente no chão (fora do limite de gemas: é rara).
    public void SpawnGold(Vector2 position, int gold)
    {
        if (gold <= 0) return;
        SpawnItem(position, gold, true);
    }

    private void SpawnItem(Vector2 position, int value, bool gold)
    {
        var stack = gold ? freeGold : free;
        var gem = stack.Count > 0 ? stack.Pop() : Create(gold);
        gem.xp = value;
        gem.attracted = false;
        gem.transform.position = position;
        gem.transform.localScale = Vector3.one * Settings.gemScale;
        gem.go.SetActive(true);
        active.Add(gem);
    }

    public int GoldCoinCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < active.Count; i++) if (active[i].gold) n++;
            return n;
        }
    }

    // XP das gemas ativas a até `radius` de `position` (verificação/telemetria).
    public int XpNear(Vector2 position, float radius)
    {
        int sum = 0;
        float r2 = radius * radius;
        for (int i = 0; i < active.Count; i++)
            if (!active[i].gold && ((Vector2)active[i].transform.position - position).sqrMagnitude <= r2) sum += active[i].xp;
        return sum;
    }

    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) Release(i);
    }

    private Gem Create(bool gold)
    {
        var prefab = gold ? Settings.goldCoinPrefab : Settings.gemPrefab;
        if (prefab != null)
        {
            var instance = Instantiate(prefab, transform, false);
            instance.name = gold ? "GoldCoin" : "XpGem";
            return new Gem { go = instance, transform = instance.transform, renderer = instance.GetComponentInChildren<SpriteRenderer>(), gold = gold };
        }

        if (!renderInfoLoaded)
        {
            renderInfoLoaded = true;
            var loot = Resources.Load<GameObject>("Loot/LootDrop"); // mesma camada de desenho do loot antigo
            var lootRenderer = loot != null ? loot.GetComponentInChildren<SpriteRenderer>() : null;
            sortingLayerId = lootRenderer != null ? lootRenderer.sortingLayerID : 0;
        }

        var go = new GameObject("XpGem");
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Settings.gemSprite; // reserva: sem prefab configurado
        renderer.sortingLayerID = sortingLayerId;
        return new Gem { go = go, transform = go.transform, renderer = renderer, gold = gold };
    }

    private void Release(int index)
    {
        var gem = active[index];
        int last = active.Count - 1;
        active[index] = active[last];
        active.RemoveAt(last);
        gem.go.SetActive(false);
        (gem.gold ? freeGold : free).Push(gem);
    }

    private void Update()
    {
        if (active.Count == 0 || !GameplayGate.IsActive) return;
        var hero = HeroController.Instance;
        if (hero == null || HeroController.IsDying) return;

        Vector2 heroPos = hero.transform.position;
        float magnet = MagnetRadius;
        float magnetSqr = magnet * magnet;
        float step = Settings.gemFlySpeed * Time.deltaTime;
        const float collectSqr = CollectDistance * CollectDistance;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var gem = active[i];
            Vector2 pos = gem.transform.position;
            Vector2 toHero = heroPos - pos;
            float sqr = toHero.sqrMagnitude;
            if (sqr <= collectSqr)
            {
                int value = gem.xp;
                bool gold = gem.gold;
                Release(i);
                if (gold) { PermanentGold.Add(value); GameEvents.GoldCoinCollected(value); }
                else GameEvents.XpCollected(value);
                continue;
            }
            if (!gem.attracted && sqr > magnetSqr) continue;
            gem.attracted = true; // entrou no ímã: vai até o herói mesmo se ele se afastar
            gem.transform.position = Vector2.MoveTowards(pos, heroPos, step);
        }
    }
}
