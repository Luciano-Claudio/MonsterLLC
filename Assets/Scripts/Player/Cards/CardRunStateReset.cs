using UnityEngine;

// Catálogo de cartas — zera o CardRunState em todo começo de run (OnDayStart). Fica no Systems
// (adicionado pelo SystemsBootstrap), como os outros efeitos de carta.
public class CardRunStateReset : MonoBehaviour
{
    private void OnEnable() { GameEvents.OnDayStart += HandleRunStart; GameEvents.OnRunEnded += HandleRunEnded; }
    private void OnDisable() { GameEvents.OnDayStart -= HandleRunStart; GameEvents.OnRunEnded -= HandleRunEnded; }
    // Loja permanente: fora da run, os bônus comprados não valem (montados de novo no próximo SpawnHero).
    private static void HandleRunEnded(bool victory) => PermanentBonuses.Reset();
    private static void HandleRunStart() => CardRunState.Reset();
}
