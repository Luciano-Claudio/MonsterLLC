using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public static MainMenuUI Instance { get; private set; }
    public static RunState CurrentRun { get; private set; }

    private void Awake() => Instance = this;

    [SerializeField] private string heroToSpawn = "Barbarian";

    // Cena de gameplay (mundo/Tower) — GameplayBootstrap, lá dentro, termina o fluxo (spawna o
    // herói ou reabre a Loja) usando os singletons persistentes (GameStateManager/
    // BagController/DayTimer/DemandTracker/SystemsBootstrap), que sobrevivem à troca de cena via
    // DontDestroyOnLoad (GameObject "Systems", nascido aqui na cena Main_Menu).
    private const string GameplaySceneName = "_TestScene";

    [ContextMenu("New Game (Standard / heroToSpawn / Tower)")]
    public void NewGame() => StartRun(heroToSpawn);

    // Chamado pela UI (MainMenuNavigationUI) com o herói escolhido no Heros_Painel — NewGame()
    // acima continua existindo só pro ContextMenu de teste manual, usando o heroToSpawn fixo.
    public void StartRun(string heroName)
    {
        GameStateManager.Instance.SetState(GameState.ModeSelect);
        Debug.Log("[MainMenu] Mode selecionado: Standard");

        GameStateManager.Instance.SetState(GameState.HeroSelect);
        Debug.Log($"[MainMenu] Hero selecionado: {heroName}");

        GameStateManager.Instance.SetState(GameState.MapSelect);
        Debug.Log("[MainMenu] Map selecionado: Tower");

        CurrentRun = RunCreation.CreateNewRun("Standard", heroName, "Tower");
        Debug.Log($"[MainMenu] RunState criado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}");

        BagController.Instance.Bag.Clear();
        GameEvents.GoldChanged(CurrentRun.gold);
        GameEvents.BagChanged(BagController.Instance.Bag);

        // Fica em GameState.MapSelect de propósito — é o sinal que o GameplayBootstrap usa, na
        // cena de gameplay, pra saber que acabou de vir de um New Game (e não de um Continue) e
        // só então spawnar o herói, resetar o dia e avançar pra GameState.Gameplay.
        LoadGameplayScene();
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

        // GDD Seção 8/43: o checkpoint é a Loja que precede o próximo dia — Continue Game abre
        // essa Loja, não o Gameplay direto. Não chama SaveManager.Save() — só carrega.
        GameStateManager.Instance.SetState(GameState.Shop);

        // 🟡 PENDÊNCIA (Sprint P2, Pré-Deadline Mundo Único): diferente do StartRun(), o
        // GameplayBootstrap não spawna o herói neste caminho — hoje ele pode não ser
        // reinstanciado ao voltar de um save. Registrado de propósito pra resolver junto da UI
        // final do Menu (Deadline 8), não agora — decisão do usuário, fora de escopo.
        LoadGameplayScene();
    }

    // Passa pela tela de loading (LoadingScreenUI, viva desde o boot via DontDestroyOnLoad) —
    // só cai no SceneManager.LoadScene direto (sem tela de loading) se ela não existir por algum
    // motivo, pra nunca travar a troca de cena por falta dela.
    private static void LoadGameplayScene()
    {
        if (LoadingScreenUI.Instance != null) LoadingScreenUI.Instance.LoadScene(GameplaySceneName);
        else SceneManager.LoadScene(GameplaySceneName);
    }
}
