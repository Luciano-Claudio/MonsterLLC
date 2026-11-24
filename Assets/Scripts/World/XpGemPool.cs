using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.1) — gemas
// de XP no chão. Todo monstro morto deixa gemas (1 XP cada; mini boss/boss deixam várias). O herói
// coleta passando por cima; dentro do raio do ímã (RunSettings × carta de ímã) a gema voa até ele.
// Sem física de propósito (D9.3: centenas de colliders derrubam o FPS): este pool atualiza todas as
// gemas num único Update, por distância. Limite de objetos (RunSettings.maxGems): passou dele, uma
// gema que já existe absorve o XP da nova — nada de XP se perde e nada de objeto novo nasce.
public class XpGemPool : MonoBehaviour
{
    public static XpGemPool Instance { get; private set; }

    private const float CollectDistance = 0.3f;

    private readonly List<Gem> active = new();
    private readonly Stack<Gem> free = new();
    private RunSettings settings;
    private int sortingLayerId;
    private bool renderInfoLoaded;

    private class Gem
    {
        public GameObject go;
        public Transform transform;
        public SpriteRenderer renderer;
        public int xp;
        public bool attracted;
    }

    public int ActiveCount => active.Count;

    // Raio do ímã: base do RunSettings × perks de coleta × carta de ímã.
    public float MagnetRadius => Settings.gemMagnetRadius * HeroController.PickupRadiusMultiplier * HeroController.Cards.Get(CardAbility.None, CardStat.Magnet);

    public long ActiveXpSum
    {
        get
        {
            long sum = 0;
            for (int i = 0; i < active.Count; i++) sum += active[i].xp;
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
        if (xp <= 0) return;
        if (active.Count >= Mathf.Max(1, Settings.maxGems))
        {
            var target = active[0]; // limite: a gema mais antiga absorve o XP
            target.xp += xp;
            target.transform.localScale = Vector3.one * Settings.gemScale * 1.5f;
            return;
        }

        var gem = free.Count > 0 ? free.Pop() : Create();
        gem.xp = xp;
        gem.attracted = false;
        gem.transform.position = position;
        gem.transform.localScale = Vector3.one * Settings.gemScale;
        gem.go.SetActive(true);
        active.Add(gem);
    }

    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) Release(i);
    }

    private Gem Create()
    {
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
        renderer.sprite = Settings.gemSprite;
        renderer.sortingLayerID = sortingLayerId;
        return new Gem { go = go, transform = go.transform, renderer = renderer };
    }

    private void Release(int index)
    {
        var gem = active[index];
        int last = active.Count - 1;
        active[index] = active[last];
        active.RemoveAt(last);
        gem.go.SetActive(false);
        free.Push(gem);
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
                int xp = gem.xp;
                Release(i);
                GameEvents.XpCollected(xp);
                continue;
            }
            if (!gem.attracted && sqr > magnetSqr) continue;
            gem.attracted = true; // entrou no ímã: vai até o herói mesmo se ele se afastar
            gem.transform.position = Vector2.MoveTowards(pos, heroPos, step);
        }
    }
}
