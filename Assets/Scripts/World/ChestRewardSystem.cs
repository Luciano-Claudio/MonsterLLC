using UnityEngine;

// Fonte única da recompensa de baú — chamado tanto por Chest.Interact() quanto pela morte do Chest
// Mimic (ChestMimicController.AnimationDieEndEvent), pra os 2 caminhos darem a mesma recompensa.
// Etapa 3 (docs/superpowers/specs/2026-11-24-etapa-3-bau-vendedor-itens-design.md, 5.4): 3 cartas de
// upgrade (às vezes um Stat) pela tela de cartas — não existe mais gold na run.
public static class ChestRewardSystem
{
    // `collectedObject`: o baú a destruir (null na morte do mímico, que cuida do próprio ciclo de vida).
    public static void GrantReward(GameObject collectedObject = null)
    {
        if (HeroCards.Instance != null) HeroCards.Instance.EnqueueOffer(OfferSource.Chest);
        if (collectedObject != null) Object.Destroy(collectedObject);
    }
}
