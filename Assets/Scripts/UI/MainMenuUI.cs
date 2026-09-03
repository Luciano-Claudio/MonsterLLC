using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public static RunState CurrentRun { get; private set; }

    [ContextMenu("New Game (Standard / Barbarian / Tower)")]
    public void NewGame()
    {
        GameStateManager.Instance.SetState(GameState.ModeSelect);
        Debug.Log("[MainMenu] Mode selecionado: Standard");

        GameStateManager.Instance.SetState(GameState.HeroSelect);
        Debug.Log("[MainMenu] Hero selecionado: Barbarian");

        GameStateManager.Instance.SetState(GameState.MapSelect);
        Debug.Log("[MainMenu] Map selecionado: Tower");

        CurrentRun = RunCreation.CreateNewRun("Standard", "Barbarian", "Tower");
        Debug.Log($"[MainMenu] RunState criado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}");

        BagController.Instance.Bag.Clear();
        GameEvents.GoldChanged(CurrentRun.gold);
        GameEvents.BagChanged(BagController.Instance.Bag);

        GameStateManager.Instance.SetState(GameState.Gameplay);
    }

    [ContextMenu("Continue Game")]
    public void ContinueGame()
    {
        if (!SaveManager.HasSave())
        {
            Debug.Log("[MainMenu] Continue indisponível — nenhum save encontrado.");
            return;
        }

        CurrentRun = SaveManager.Load();
        Debug.Log($"[MainMenu] Save carregado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}, Hero {CurrentRun.hero}");

        BagController.Instance.Bag.Clear();
        GameEvents.GoldChanged(CurrentRun.gold);
        GameEvents.BagChanged(BagController.Instance.Bag);

        GameStateManager.Instance.SetState(GameState.Gameplay);
    }
}
