using UnityEngine;

// Catálogo de cartas — zera o CardRunState em todo começo de run (OnDayStart). Fica no Systems
// (adicionado pelo SystemsBootstrap), como os outros efeitos de carta.
public class CardRunStateReset : MonoBehaviour
{
    private void OnEnable() => GameEvents.OnDayStart += HandleRunStart;
    private void OnDisable() => GameEvents.OnDayStart -= HandleRunStart;
    private static void HandleRunStart() => CardRunState.Reset();
}
