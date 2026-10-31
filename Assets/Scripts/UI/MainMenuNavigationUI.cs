using UnityEngine;
using UnityEngine.UI;

// Navegação do Main Menu (Exit/Credits/Settings/Continue/New_Game + Heros_Painel/Map_Painel).
// Vive no GameObject "Panel" (filho do Canvas, sempre ativo) e acha todo o resto por Find
// relativo — mesmo padrão do InventoryPanelController/InventoryTabUI: zero campo pra arrastar
// no Inspector, só precisa adicionar este componente ao "Panel".
//
// Esc (ação "Pause" do PlayerControls) sempre volta exatamente 1 passo:
// Credits/Settings/Heros_Painel -> Main Menu; Map_Painel -> Heros_Painel (resetando a escolha
// do herói). Os botões "Back_To_Menu" (Heros_Painel/Map_Painel) e "Close" (Settings) fazem a
// mesma coisa que o Esc faria na tela em que estão, de propósito (botão visível = mesmo atalho).
public class MainMenuNavigationUI : MonoBehaviour
{
    private enum MenuState { Main, Credits, Settings, HeroSelect, MapSelect }

    private GameObject menuButtons;
    private GameObject creditsPanel;
    private GameObject settingsPanel;
    private GameObject herosPanel;
    private GameObject mapPanel;

    private MenuState state = MenuState.Main;
    private string selectedHero;
    private PlayerControls controls;

    private void Awake()
    {
        menuButtons = transform.Find("Menu_Buttons").gameObject;
        creditsPanel = transform.Find("Credits").gameObject;
        settingsPanel = transform.Find("Settings").gameObject;
        herosPanel = transform.Find("Heros_Painel").gameObject;
        mapPanel = transform.Find("Map_Painel").gameObject;

        WireButton(menuButtons.transform, "Continue", ContinueClicked);
        WireButton(menuButtons.transform, "New_Game", OpenHeroSelect);
        WireButton(menuButtons.transform, "Settings", OpenSettings);
        WireButton(menuButtons.transform, "Credits", OpenCredits);
        WireButton(menuButtons.transform, "Exit", QuitGame);

        WireButton(settingsPanel.transform, "Close", GoBack);
        WireButton(herosPanel.transform, "Back_To_Menu", GoBack);
        WireButton(mapPanel.transform, "Back_To_Menu", GoBack);

        WireHeroCards();
        WireWorldCards();

        controls = new PlayerControls();
        controls.Gameplay.Pause.performed += ctx => GoBack();

        ShowMainMenu();
    }

    private void OnEnable() => controls?.Enable();
    private void OnDisable() => controls?.Disable();

    private static void WireButton(Transform root, string path, UnityEngine.Events.UnityAction action) =>
        root.Find(path).GetComponent<Button>().onClick.AddListener(action);

    // Os 10 cards (<Hero>_Card) ficam em Heros_Scroll_View/Viewport/Content; o Button de
    // verdade mora num filho ANINHADO também chamado "Card" (não no <Hero>_Card raiz) — por
    // isso GetComponentsInChildren, não Find direto. A chave do herói (p.ex. "BloodMage") sai
    // do nome do card raiz, removendo o sufixo "_Card" e os "_" (== nome da classe C# do
    // herói, mesma convenção do heroToSpawn/availableHeroes em MainMenuUI).
    private void WireHeroCards()
    {
        var content = herosPanel.transform.Find("Heros_Scroll_View/Viewport/Content");
        foreach (var button in content.GetComponentsInChildren<Button>(true))
        {
            string heroKey = button.transform.parent.name.Replace("_Card", "").Replace("_", "");
            button.onClick.AddListener(() => OnHeroChosen(heroKey));
        }
    }

    // Só existe 1 mundo (Tower) por hora — qualquer card aqui dentro dispara o mesmo início
    // de run, sem precisar de chave por nome (ver MainMenuUI.StartRun).
    private void WireWorldCards()
    {
        var content = mapPanel.transform.Find("Map_Scroll_View/Viewport/Content");
        foreach (var button in content.GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(OnWorldChosen);
    }

    private void ShowOnly(GameObject active)
    {
        menuButtons.SetActive(active == menuButtons);
        creditsPanel.SetActive(active == creditsPanel);
        settingsPanel.SetActive(active == settingsPanel);
        herosPanel.SetActive(active == herosPanel);
        mapPanel.SetActive(active == mapPanel);
    }

    private void ShowMainMenu()
    {
        state = MenuState.Main;
        ShowOnly(menuButtons);
    }

    private void OpenCredits()
    {
        state = MenuState.Credits;
        ShowOnly(creditsPanel);
    }

    private void OpenSettings()
    {
        state = MenuState.Settings;
        ShowOnly(settingsPanel);
    }

    private void OpenHeroSelect()
    {
        state = MenuState.HeroSelect;
        selectedHero = null;
        ShowOnly(herosPanel);
    }

    private void OnHeroChosen(string heroKey)
    {
        selectedHero = heroKey;
        state = MenuState.MapSelect;
        ShowOnly(mapPanel);
    }

    private void OnWorldChosen()
    {
        if (MainMenuUI.Instance != null) MainMenuUI.Instance.StartRun(selectedHero);
        else Debug.LogWarning("[MainMenuNavigationUI] MainMenuUI não está nesta cena — New Game não inicia a run de verdade ainda.");
    }

    private void ContinueClicked()
    {
        if (MainMenuUI.Instance != null) MainMenuUI.Instance.ContinueGame();
        else Debug.LogWarning("[MainMenuNavigationUI] MainMenuUI não está nesta cena — Continue não faz nada ainda.");
    }

    private void GoBack()
    {
        switch (state)
        {
            case MenuState.Credits:
            case MenuState.Settings:
            case MenuState.HeroSelect:
                ShowMainMenu();
                break;
            case MenuState.MapSelect:
                OpenHeroSelect(); // já reseta selectedHero e reabre o Heros_Painel
                break;
            case MenuState.Main:
                break; // nada a fazer, já está na tela inicial
        }
    }

    private static void QuitGame() => Application.Quit();
}
