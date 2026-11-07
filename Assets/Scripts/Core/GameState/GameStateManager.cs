using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    MainMenu,
    ModeSelect,
    HeroSelect,
    MapSelect,
    Gameplay,
    Paused,
    Results,
    Shop
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // Achado real (usuário testando morte com o Cleric — a Loja nunca aparecia): ResultsHandler
    // chama SetState(Shop) DE DENTRO do próprio HandleStateChanged(Results) — ou seja, ainda
    // durante o despacho do evento "Results" pros inscritos. Isso reentra GameEvents.
    // GameStateChanged, que dispara "Shop" pra todo mundo (ativa o painel certinho) ANTES do
    // despacho original de "Results" terminar de percorrer o resto da lista de inscritos — e
    // qualquer um que ainda não tivesse sido chamado nessa rodada original (ex.: um
    // ShopUIController que se inscreveu depois do ResultsHandler) recebe "Results" por último,
    // desfazendo o "Shop" que acabou de ligar. Fila evita a reentrância: uma troca de estado
    // chamada DURANTE outro despacho só roda depois que o despacho atual termina por completo.
    private bool isDispatching;
    private readonly Queue<GameState> pendingStates = new();

    private void Awake() => Instance = this;

    public void SetState(GameState state)
    {
        if (isDispatching)
        {
            pendingStates.Enqueue(state);
            return;
        }

        CurrentState = state;
        isDispatching = true;
        GameEvents.GameStateChanged(state);
        isDispatching = false;

        if (pendingStates.Count > 0) SetState(pendingStates.Dequeue());
    }
}
