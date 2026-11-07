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
    // BagController/DayTimer/SystemsBootstrap), que sobrevivem à troca de cena via
    // DontDestroyOnLoad (GameObject "Systems", nascido aqui na cena Main_Menu).
    private const string GameplaySceneName = "_TestScene";

    [ContextMenu("New Game (Standard / heroToSpawn / Tower)")]
    public void NewGame() => StartRun(heroToSpawn);

    // Chamado pela UI (MainMenuNavigationUI) com o herói escolhido no Heros_Painel — NewGame()
    // acima continua existindo só pro ContextMenu de teste manual, usando o heroToSpawn fixo.
    //
    // 2026-10-31: a criação da run (RunCreation, Bag.Clear, GameState) agora roda DENTRO do
    // callback passado pra LoadGameplayScene, não antes — bug real: fazer esse trabalho antes de
    // chamar a tela de loading travava o jogo por um tempo ANTES da tela sequer aparecer,
    // deixando o jogador sem feedback nenhum no clique do mapa.
    public void StartRun(string heroName)
    {
        LoadGameplayScene(() =>
        {
            GameStateManager.Instance.SetState(GameState.ModeSelect);
            Debug.Log("[MainMenu] Mode selecionado: Standard");

            GameStateManager.Instance.SetState(GameState.HeroSelect);
            Debug.Log($"[MainMenu] Hero selecionado: {heroName}");

            // Fica em GameState.MapSelect de propósito — é o sinal que o GameplayBootstrap usa,
            // na cena de gameplay, pra saber que acabou de vir de um New Game (e não de um
            // Continue) e só então spawnar o herói, resetar o dia e avançar pra
            // GameState.Gameplay.
            GameStateManager.Instance.SetState(GameState.MapSelect);
            Debug.Log("[MainMenu] Map selecionado: Tower");

            CurrentRun = RunCreation.CreateNewRun("Standard", heroName, "Tower");
            Debug.Log($"[MainMenu] RunState criado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}");

            BagController.Instance.Bag.Clear();
            GameEvents.GoldChanged(CurrentRun.gold);
            GameEvents.BagChanged(BagController.Instance.Bag);
        });
    }

    [ContextMenu("Continue Game")]
    public void ContinueGame()
    {
        if (!SaveManager.HasSave())
        {
            Debug.Log("[MainMenu] Continue indisponível — nenhum save encontrado.");
            return;
        }

        LoadGameplayScene(() =>
        {
            CurrentRun = SaveManager.Load();
            Debug.Log($"[MainMenu] Save carregado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}, Hero {CurrentRun.hero}, Weapon {CurrentRun.weaponTier}");

            BagController.Instance.Bag.Clear(); // Bag é Daily — nunca persiste no save, nem no Continue
            GameEvents.GoldChanged(CurrentRun.gold);
            GameEvents.BagChanged(BagController.Instance.Bag);

            // GDD Seção 8/43: o checkpoint é a Loja que precede o próximo dia — Continue Game
            // abre essa Loja, não o Gameplay direto. Não chama SaveManager.Save() — só carrega.
            GameStateManager.Instance.SetState(GameState.Shop);

            // 🟡 PENDÊNCIA (Sprint P2, Pré-Deadline Mundo Único): diferente do StartRun(), o
            // GameplayBootstrap não spawna o herói neste caminho — hoje ele pode não ser
            // reinstanciado ao voltar de um save. Registrado de propósito pra resolver junto da
            // UI final do Menu (Deadline 8), não agora — decisão do usuário, fora de escopo.
        });
    }

    // Passa pela tela de loading (LoadingScreenUI, viva desde o boot via DontDestroyOnLoad) —
    // setup roda só depois que a tela já estiver visível na tela (ver comentário em
    // LoadingScreenUI.LoadSceneRoutine). Só cai no caminho direto (sem tela, setup na hora) se
    // ela não existir por algum motivo, pra nunca travar a troca de cena por falta dela.
    private static void LoadGameplayScene(System.Action setup)
    {
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.LoadScene(GameplaySceneName, setup);
        }
        else
        {
            setup();
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
