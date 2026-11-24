using UnityEngine.SceneManagement;

// D9.5 (revisão final, achados 2 e 3): único jeito de sair de uma run pro menu — botão
// "Continuar" do Resumo total (morte) e "Menu" da pausa. O "Systems" (GameStateManager,
// TimeManager, DayTimer...) sobrevive à troca de cena, então o estado da run morta não pode ir
// junto:
// - despausa (ESC/TAB durante a morte ou o resumo pausavam e nada despausava — a próxima run
//   nascia travada com GameplayGate falso);
// - estado volta pra MainMenu (não fica Gameplay no menu);
// - cancela qualquer fade do ScreenFader em andamento (um callback velho do clarear não pode
//   setar Gameplay já no menu).
public static class RunExit
{
    public const string MainMenuScene = "Main_Menu";

    public static void ToMainMenu()
    {
        ScreenFader.Cancel();
        if (TimeManager.Instance != null) TimeManager.Instance.ResumeAll(); // nenhuma pausa (menu, cartas, câmera) sobrevive à saída
        if (GameStateManager.Instance != null) GameStateManager.Instance.SetState(GameState.MainMenu);
        SceneManager.LoadScene(MainMenuScene);
    }
}
