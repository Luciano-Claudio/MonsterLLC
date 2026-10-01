using UnityEngine;
using Unity.Cinemachine;

public class MainMenuUI : MonoBehaviour
{
    public static RunState CurrentRun { get; private set; }

    // Registro de heróis prontos — 1 entrada por herói, nome igual ao usado em RunState.hero.
    // Adicionar um herói novo (quando a sprint dele fechar) é só arrastar o prefab aqui, sem
    // mexer em mais nada deste script.
    [System.Serializable]
    private class HeroEntry
    {
        public string name;
        public GameObject prefab;
    }

    [Header("Heróis disponíveis")]
    [SerializeField] private HeroEntry[] availableHeroes;

    [SerializeField] private string heroToSpawn = "Barbarian";

    [Header("Onde o herói nasce / câmera que o segue")]
    [SerializeField] private Transform herosContainer; // GameObject "//HEROS" na cena — criado na hora se ninguém atribuir um aqui
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [ContextMenu("New Game (Standard / heroToSpawn / Tower)")]
    public void NewGame()
    {
        GameStateManager.Instance.SetState(GameState.ModeSelect);
        Debug.Log("[MainMenu] Mode selecionado: Standard");

        GameStateManager.Instance.SetState(GameState.HeroSelect);
        Debug.Log($"[MainMenu] Hero selecionado: {heroToSpawn}");

        GameStateManager.Instance.SetState(GameState.MapSelect);
        Debug.Log("[MainMenu] Map selecionado: Tower");

        CurrentRun = RunCreation.CreateNewRun("Standard", heroToSpawn, "Tower");
        Debug.Log($"[MainMenu] RunState criado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}");

        BagController.Instance.Bag.Clear();
        GameEvents.GoldChanged(CurrentRun.gold);
        GameEvents.BagChanged(BagController.Instance.Bag);

        DayTimer.Instance.ResetForNewDay(100f);
        DemandTracker.Instance.StartDay(CurrentRun.day);

        SpawnHero(heroToSpawn);

        GameStateManager.Instance.SetState(GameState.Gameplay);
    }

    // Instancia o herói escolhido em (0,0,0) dentro do GameObject "//HEROS" e aponta a câmera do
    // Cinemachine pra ele — sem isso, a câmera continuaria seguindo qualquer herói antigo que
    // já estivesse na cena antes (ou nenhum).
    private void SpawnHero(string heroName)
    {
        // HeroController.IsPlayerUntargetable é static (camuflagem do Ranger, Coruja do
        // Druid) — se ficou travado em true de uma sessão de Play anterior (Reload Domain
        // desligado no Enter Play Mode Settings não reseta static entre Stop/Play), todo
        // monstro ficava cego pro herói novo. Começar uma run do zero sempre garante
        // "visível" de novo, independente do que sobrou de testes anteriores.
        HeroController.IsPlayerUntargetable = false;

        HeroEntry entry = System.Array.Find(availableHeroes, h => h.name == heroName);
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"[MainMenu] Nenhum prefab configurado pra '{heroName}' em availableHeroes.");
            return;
        }

        if (herosContainer == null)
        {
            var existing = GameObject.Find("//HEROS");
            herosContainer = existing != null ? existing.transform : new GameObject("//HEROS").transform;
        }

        GameObject heroObj = Instantiate(entry.prefab, Vector3.zero, Quaternion.identity, herosContainer);

        if (cinemachineCamera != null) cinemachineCamera.Target.TrackingTarget = heroObj.transform;
        else Debug.LogWarning("[MainMenu] CinemachineCamera não atribuída no Inspector — câmera não vai seguir o herói.");
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
