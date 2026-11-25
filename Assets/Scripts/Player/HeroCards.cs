using System;
using System.Collections.Generic;
using UnityEngine;

// As cartas do herói na run. Mora no "Systems" (SystemsBootstrap).
// - Etapas 1+2: cada nível ganho (GameEvents.HeroLeveledUp) enfileira 1 oferta de 3 cartas; várias de
//   uma vez abrem uma depois da outra. Oferta aberta pausa o jogo; escolher fecha e abre a próxima.
// - Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.1): toda oferta
//   tem uma FONTE (level up, baú, vendedor). As cartas são sorteadas ao ABRIR (CardRules.RollOffer), e o
//   reroll (1 por oferta) sorteia de novo da mesma fonte. Oferta sem carta possível é pulada sem pausar.
// - Sem a tela de cartas na cena (CardScreenUI), escolhe a 1ª carta sozinho — nunca trava o jogo.
// - Os bônus ficam em HeroController.Cards (lidos na hora do uso pelos heróis).
public class HeroCards : MonoBehaviour
{
    public const int CardsPerOffer = 3;

    public static HeroCards Instance { get; private set; }

    private struct PendingOffer
    {
        public OfferSource source;
        public int level; // nível do herói usado no sorteio (o do level up, ou o atual)
    }

    private readonly Dictionary<string, int> owned = new();
    private readonly List<CardDefinition> ownedItems = new(); // ordem em que os itens foram pegos
    private readonly Queue<PendingOffer> pending = new();
    private readonly List<CardDefinition> currentOffer = new();
    private readonly List<CardDefinition> probe = new();
    private readonly System.Random rng = new();
    private PendingOffer current;
    private bool rerollUsed;
    private CardCatalog catalog;

    public bool OfferOpen { get; private set; }
    public int PendingOffers => pending.Count;
    public IReadOnlyList<CardDefinition> CurrentOffer => currentOffer;
    public OfferSource CurrentSource => current.source;
    public bool RerollAvailable => OfferOpen && !rerollUsed;
    public IReadOnlyDictionary<string, int> Owned => owned;
    public IReadOnlyList<CardDefinition> OwnedItems => ownedItems;

    public event Action<IReadOnlyList<CardDefinition>> OfferReady;
    public event Action OfferClosed;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        GameEvents.OnHeroLeveledUp += HandleLeveledUp;
        GameEvents.OnDayStart += HandleRunStart;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroLeveledUp -= HandleLeveledUp;
        GameEvents.OnDayStart -= HandleRunStart;
        GameEvents.OnRunEnded -= HandleRunEnded;
    }

    private CardCatalog Catalog => catalog != null ? catalog : catalog = CardCatalog.Load();
    private IReadOnlyList<CardDefinition> Pool => Catalog != null ? Catalog.cards : null;
    private static string HeroId => HeroController.Instance != null ? HeroController.Instance.GetType().Name : "Barbarian";
    private static int HeroLevelNow => HeroLevel.Instance != null ? HeroLevel.Instance.Level : 1;
    private static OfferRules Rules => OfferRules.From(RunSettings.Current);

    public int LevelOf(CardDefinition card) => CardRules.LevelOf(owned, card);

    // Run nova: zera cartas, itens, filas e bônus.
    public void HandleRunStart()
    {
        owned.Clear();
        ownedItems.Clear();
        pending.Clear();
        CloseOffer(resume: false);
        if (TimeManager.Instance != null) TimeManager.Instance.ResumeBy(this); // nenhuma pausa de cartas da run anterior
        HeroController.Cards.Reset();
        GameEvents.ItemsChanged();
    }

    private void HandleRunEnded(bool victory)
    {
        // Fim da run: fecha sem despausar o jogo por baixo do resumo (o menu reseta a pausa).
        pending.Clear();
        CloseOffer(resume: false);
    }

    private void HandleLeveledUp(int level) => Enqueue(new PendingOffer { source = OfferSource.LevelUp, level = level });

    // Baú/mímico/vendedor (etapa 3): as cartas são sorteadas quando a oferta abrir.
    public void EnqueueOffer(OfferSource source) => Enqueue(new PendingOffer { source = source, level = HeroLevelNow });

    // Há ao menos 1 carta possível AGORA para essa fonte (o vendedor só aparece/funciona se sim).
    public bool CanOffer(OfferSource source)
    {
        CardRules.RollOffer(source, HeroLevelNow, Pool, owned, HeroId, RunSpecialization.Tags, 1, rng, Rules, probe);
        return probe.Count > 0;
    }

    private void Enqueue(PendingOffer offer)
    {
        pending.Enqueue(offer);
        if (!OfferOpen) OpenNext();
    }

    private void RollCurrent()
        => CardRules.RollOffer(current.source, current.level, Pool, owned, HeroId, RunSpecialization.Tags, CardsPerOffer, rng, Rules, currentOffer);

    private void OpenNext()
    {
        while (pending.Count > 0)
        {
            current = pending.Dequeue();
            RollCurrent();
            if (currentOffer.Count == 0) continue; // nada possível (tudo no máximo / 6 itens): pula sem pausar

            rerollUsed = false;
            var screen = CardScreenUI.Find();
            if (screen == null)
            {
                Debug.Log($"[HeroCards] Sem tela de cartas na cena — escolhida automaticamente: {currentOffer[0].title}.");
                Grant(currentOffer[0]);
                continue;
            }

            OfferOpen = true;
            if (TimeManager.Instance != null) TimeManager.Instance.PauseBy(this);
            screen.Show(this);
            OfferReady?.Invoke(currentOffer);
            return;
        }
        CloseOffer(resume: true);
    }

    public bool Reroll()
    {
        if (!OfferOpen || rerollUsed) return false;
        rerollUsed = true;
        RollCurrent();
        var screen = CardScreenUI.Find();
        if (screen != null) screen.Show(this);
        OfferReady?.Invoke(currentOffer);
        return true;
    }

    public void Choose(CardDefinition card)
    {
        if (!OfferOpen || card == null || !currentOffer.Contains(card)) return;
        Grant(card);
        OfferOpen = false;
        OpenNext();
    }

    // Sobe o nível da carta e recalcula os bônus.
    public void Grant(CardDefinition card)
    {
        if (card == null || string.IsNullOrEmpty(card.id)) return;
        var hero = HeroController.Instance;
        float oldMaxHealth = hero != null ? hero.EffectiveMaxHealth : 0f;
        owned[card.id] = Mathf.Min(card.maxLevel, CardRules.LevelOf(owned, card) + 1);
        if (card.kind == CardKind.Item && !ownedItems.Contains(card)) ownedItems.Add(card);
        HeroController.Cards.Recompute(Pool, owned);
        if (hero != null) hero.OnCardBonusesChanged(oldMaxHealth);
        Debug.Log($"[HeroCards] {card.title} -> nível {owned[card.id]}.");
        GameEvents.CardGranted(card.id, owned[card.id]);
        if (card.kind == CardKind.Item) GameEvents.ItemsChanged();
    }

    private void CloseOffer(bool resume)
    {
        bool wasOpen = OfferOpen;
        OfferOpen = false;
        currentOffer.Clear();
        var screen = CardScreenUI.Find();
        if (screen != null) screen.Hide();
        if (resume && TimeManager.Instance != null) TimeManager.Instance.ResumeBy(this);
        if (wasOpen || resume) OfferClosed?.Invoke();
    }
}
