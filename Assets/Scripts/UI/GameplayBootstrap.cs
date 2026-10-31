using UnityEngine;

// Termina o fluxo que o MainMenuUI (cena Main_Menu) começou, depois que esta cena de gameplay
// acaba de carregar — GameStateManager/BagController/DayTimer/DemandTracker já existem aqui
// porque sobrevivem à troca de cena via DontDestroyOnLoad (GameObject "Systems", nascido na
// cena Main_Menu — ver SystemsBootstrap). CurrentState ainda reflete de que botão o jogador
// veio (MapSelect = New Game, Shop = Continue Game), decidido lá antes do SceneManager.LoadScene.
public class GameplayBootstrap : MonoBehaviour
{
    [System.Serializable]
    private class HeroEntry
    {
        public string name;
        public GameObject prefab;
    }

    [Header("Heróis disponíveis")]
    [SerializeField] private HeroEntry[] availableHeroes;

    [Header("Onde o herói nasce / câmera que o segue")]
    [SerializeField] private Transform herosContainer; // GameObject "//HEROS" na cena — criado na hora se ninguém atribuir um aqui
    [SerializeField] private Unity.Cinemachine.CinemachineCamera cinemachineCamera;

    private void Start()
    {
        if (MainMenuUI.CurrentRun == null) return; // Play direto nesta cena, sem passar pelo Main_Menu

        switch (GameStateManager.Instance.CurrentState)
        {
            case GameState.MapSelect: // acabou de vir de um New Game
                SpawnHero(MainMenuUI.CurrentRun.hero);

                DayTimer.Instance.ResetForNewDay();
                DemandTracker.Instance.StartDay(MainMenuUI.CurrentRun.day);
                GameStateManager.Instance.SetState(GameState.Gameplay);
                break;

            case GameState.Shop: // acabou de vir de um Continue Game
                // Reemite o evento pra quem só assinou GameEvents.OnGameStateChanged nesta cena
                // nova (uma UI de Loja que more só na cena de gameplay, por exemplo) — o primeiro
                // SetState (feito no MainMenuUI, antes do LoadScene) já passou sem ninguém aqui
                // pra ouvir.
                GameEvents.GameStateChanged(GameState.Shop);
                break;
        }
    }

    // Instancia o herói escolhido em (0,0,0) dentro do GameObject "//HEROS" e aponta a câmera do
    // Cinemachine pra ele — sem isso, a câmera continuaria seguindo qualquer herói antigo que já
    // estivesse na cena antes (ou nenhum).
    private void SpawnHero(string heroName)
    {
        // HeroController.IsPlayerUntargetable é static (camuflagem do Ranger, Coruja do Druid) —
        // se ficou travado em true de uma sessão de Play anterior (Reload Domain desligado no
        // Enter Play Mode Settings não reseta static entre Stop/Play), todo monstro ficava cego
        // pro herói novo. Começar uma run do zero sempre garante "visível" de novo.
        HeroController.IsPlayerUntargetable = false;

        HeroEntry entry = System.Array.Find(availableHeroes, h => h.name == heroName);
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning($"[GameplayBootstrap] Nenhum prefab configurado pra '{heroName}' em availableHeroes.");
            return;
        }

        if (herosContainer == null)
        {
            var existing = GameObject.Find("//HEROS");
            herosContainer = existing != null ? existing.transform : new GameObject("//HEROS").transform;
        }

        GameObject heroObj = Instantiate(entry.prefab, Vector3.zero, Quaternion.identity, herosContainer);

        if (cinemachineCamera != null) cinemachineCamera.Target.TrackingTarget = heroObj.transform;
        else Debug.LogWarning("[GameplayBootstrap] CinemachineCamera não atribuída no Inspector — câmera não vai seguir o herói.");

        GameEvents.HeroSpawned(heroObj.transform);
    }
}
