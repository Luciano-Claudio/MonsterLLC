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

        DayTimer.Instance.ResetForNewDay(100f);
        DemandTracker.Instance.StartDay(CurrentRun.day);

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
        Debug.Log($"[MainMenu] Save carregado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}, Hero {CurrentRun.hero}, Weapon {CurrentRun.weaponTier}");

        BagController.Instance.Bag.Clear(); // Bag é Daily — nunca persiste no save, nem no Continue
        GameEvents.GoldChanged(CurrentRun.gold);
        GameEvents.BagChanged(BagController.Instance.Bag);

        // GDD Seção 8/43: o checkpoint é a Loja que precede o próximo dia — Continue Game
        // abre essa Loja, não o Gameplay direto. Não chama SaveManager.Save() — só carrega.
        GameStateManager.Instance.SetState(GameState.Shop);
    }
}
