using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu de pause da _TestScene (Esc). Vive no GameObject "Canvas" (o HUD, sempre ativo) e acha o
// resto por Find relativo — mesmo padrão do MainMenuNavigationUI: zero campo pra arrastar no
// Inspector, só precisa adicionar este componente ao "Canvas".
//
// Esc (ação "Pause" do PlayerControls) sempre volta exatamente 1 passo: Settings -> Menu_Esc;
// Menu_Esc -> fecha (volta pro jogo); fechado -> abre o Menu_Esc. O botão "Resume" faz a mesma
// coisa que o Esc faria estando no Menu_Esc, e "Close" do Settings faz a mesma coisa que o Esc
// faria estando no Settings — de propósito (botão visível = mesmo atalho).
public class PauseMenuUI : MonoBehaviour
{
    private enum MenuState { Closed, Menu, Settings }

    private GameObject menuEscPanel;
    private GameObject settingsPanel;

    private MenuState state = MenuState.Closed;
    private PlayerControls controls;

    private void Awake()
    {
        menuEscPanel = transform.Find("Menu_Esc").gameObject;
        settingsPanel = transform.Find("Settings").gameObject;

        WireButton(menuEscPanel.transform, "Resume", CloseMenu);
        WireButton(menuEscPanel.transform, "Settings", OpenSettings);
        WireButton(menuEscPanel.transform, "Main_Menu", GoToMainMenu);
        WireButton(menuEscPanel.transform, "Exit", QuitGame);

        WireButton(settingsPanel.transform, "Close", OpenMenu);

        controls = new PlayerControls();
        controls.Gameplay.Pause.performed += ctx => GoBack();

        menuEscPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void OnEnable() => controls?.Enable();
    private void OnDisable() => controls?.Disable();

    private static void WireButton(Transform root, string path, UnityEngine.Events.UnityAction action) =>
        root.Find(path).GetComponent<Button>().onClick.AddListener(action);

    private void GoBack()
    {
        switch (state)
        {
            case MenuState.Closed:
                OpenMenu();
                break;
            case MenuState.Menu:
                CloseMenu();
                break;
            case MenuState.Settings:
                OpenMenu(); // já fecha o Settings e reabre o Menu_Esc
                break;
        }
    }

    private void OpenMenu()
    {
        bool wasClosed = state == MenuState.Closed;
        state = MenuState.Menu;
        menuEscPanel.SetActive(true);
        settingsPanel.SetActive(false);

        // Só pausa de verdade na transição Closed -> Menu — vindo do Settings já está pausado,
        // chamar de novo não quebra nada (TimeManager.Pause() é idempotente), mas não precisa.
        if (wasClosed) TimeManager.Instance.Pause();
    }

    private void CloseMenu()
    {
        state = MenuState.Closed;
        menuEscPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Despausa aqui, não só reagindo ao Esc — clicar em "Resume" com o mouse precisa
        // despausar igual, já que esse é o único jeito de fechar o menu a partir dele.
        TimeManager.Instance.Resume();
    }

    private void OpenSettings()
    {
        state = MenuState.Settings;
        menuEscPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private static void GoToMainMenu() => SceneManager.LoadScene("Main_Menu");

    private static void QuitGame() => Application.Quit();
}
