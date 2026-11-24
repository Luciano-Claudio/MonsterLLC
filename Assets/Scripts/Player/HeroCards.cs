using System;
using System.Collections.Generic;
using UnityEngine;

// Etapas 1+2 (docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md, 6.3) — as
// cartas do herói na run. Mora no "Systems" (SystemsBootstrap).
// - Cada nível ganho (GameEvents.HeroLeveledUp) enfileira 1 oferta de 3 cartas (CardRules.Roll);
//   várias de uma vez abrem uma depois da outra.
// - Oferta aberta pausa o jogo (TimeManager); escolher fecha e abre a próxima, ou despausa.
// - 1 reroll por oferta. Baú/mimic/vendedor (etapa 3) usam EnqueueOffer com cartas prontas.
// - Sem a tela de cartas na cena (CardScreenUI), escolhe a 1ª carta sozinho — nunca trava o jogo.
// - Os bônus ficam em HeroController.Cards (lidos na hora do uso pelos heróis).
public class HeroCards : MonoBehaviour
{
    public const int CardsPerOffer = 3;

    public static HeroCards Instance { get; private set; }

    private readonly Dictionary<string, int> owned = new();
    private readonly Queue<List<CardDefinition>> pendingPrepared = new(); // ofertas prontas (etapa 3)
    private int pendingLevelOffers;
    private readonly List<CardDefinition> currentOffer = new();
    private readonly System.Random rng = new();
    private bool rerollUsed;
    private CardCatalog catalog;

    public bool OfferOpen { get; private set; }
    public int PendingOffers => pendingLevelOffers + pendingPrepared.Count;
    public IReadOnlyList<CardDefinition> CurrentOffer => currentOffer;
    public bool RerollAvailable => OfferOpen && !rerollUsed;
    public IReadOnlyDictionary<string, int> Owned => owned;

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
    private static string HeroId => HeroController.Instance != null ? HeroController.Instance.GetType().Name : "Barbarian";

    public int LevelOf(CardDefinition card) => CardRules.LevelOf(owned, card);

    // Run nova: zera cartas, filas e bônus.
    public void HandleRunStart()
    {
        owned.Clear();
        pendingPrepared.Clear();
        pendingLevelOffers = 0;
        CloseOffer(resume: false);
        HeroController.Cards.Reset();
    }

    private void HandleRunEnded(bool victory)
    {
        // Fim da run: fecha sem despausar o jogo por baixo do resumo (o menu reseta a pausa).
        pendingPrepared.Clear();
        pendingLevelOffers = 0;
        CloseOffer(resume: false);
    }

    private void HandleLeveledUp(int level)
    {
        pendingLevelOffers++;
        if (!OfferOpen) OpenNext();
    }

    public void EnqueueOffer(IReadOnlyList<CardDefinition> cards)
    {
        if (cards == null || cards.Count == 0) return;
        pendingPrepared.Enqueue(new List<CardDefinition>(cards));
        if (!OfferOpen) OpenNext();
    }

    private void OpenNext()
    {
        while (PendingOffers > 0)
        {
            currentOffer.Clear();
            if (pendingPrepared.Count > 0) currentOffer.AddRange(pendingPrepared.Dequeue());
            else
            {
                pendingLevelOffers--;
                CardRules.Roll(Catalog != null ? Catalog.cards : null, owned, HeroId, CardsPerOffer, rng, currentOffer);
            }
            if (currentOffer.Count == 0) continue; // todas as cartas no máximo: nível sem oferta

            rerollUsed = false;
            var screen = CardScreenUI.Find();
            if (screen == null)
            {
                Debug.Log($"[HeroCards] Sem tela de cartas na cena — escolhida automaticamente: {currentOffer[0].title}.");
                Grant(currentOffer[0]);
                continue;
            }

            OfferOpen = true;
            if (TimeManager.Instance != null) TimeManager.Instance.Pause();
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
        CardRules.Roll(Catalog != null ? Catalog.cards : null, owned, HeroId, CardsPerOffer, rng, currentOffer);
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

    // Sobe o nível da carta e recalcula os bônus (também usado pelo baú/vendedor na etapa 3).
    public void Grant(CardDefinition card)
    {
        if (card == null || string.IsNullOrEmpty(card.id)) return;
        var hero = HeroController.Instance;
        float oldMaxHealth = hero != null ? hero.EffectiveMaxHealth : 0f;
        owned[card.id] = Mathf.Min(card.maxLevel, CardRules.LevelOf(owned, card) + 1);
        HeroController.Cards.Recompute(Catalog != null ? Catalog.cards : null, owned);
        if (hero != null) hero.OnCardBonusesChanged(oldMaxHealth);
        Debug.Log($"[HeroCards] {card.title} -> nível {owned[card.id]}.");
        GameEvents.CardGranted(card.id, owned[card.id]);
    }

    private void CloseOffer(bool resume)
    {
        bool wasOpen = OfferOpen;
        OfferOpen = false;
        currentOffer.Clear();
        var screen = CardScreenUI.Find();
        if (screen != null) screen.Hide();
        if (resume && TimeManager.Instance != null) TimeManager.Instance.Resume();
        if (wasOpen || resume) OfferClosed?.Invoke();
    }
}
