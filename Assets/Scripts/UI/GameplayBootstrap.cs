using System.Collections;
using UnityEngine;

// Termina o fluxo que o MainMenuUI (cena Main_Menu) começou, depois que esta cena de gameplay
// acaba de carregar — GameStateManager/BagController/DayTimer já existem aqui porque sobrevivem
// à troca de cena via DontDestroyOnLoad (GameObject "Systems", nascido na cena Main_Menu — ver
// SystemsBootstrap). CurrentState ainda reflete de que botão o jogador veio (MapSelect = New
// Game, Shop = Continue Game), decidido lá antes do SceneManager.LoadScene.
//
// LoadingScreenUI.Instance (se existir) continua visível desde a Main_Menu até aqui — este
// script reporta o status de cada etapa nela e só chama Hide() no fim de verdade, cobrindo
// também o Awake/Start desta cena (não só o carregamento bruto do asset da cena).
public class GameplayBootstrap : MonoBehaviour
{
    // GDD Deadline 8.1, Seção 3 — ShopHandler.ProceedFromShop() chama RespawnHeroFresh() toda
    // vez que sai da Loja (retry OU avanço de dia). Mesmo padrão Instance de todo outro manager.
    public static GameplayBootstrap Instance { get; private set; }

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

    private GameObject currentHeroObj;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (MainMenuUI.CurrentRun == null) return; // Play direto nesta cena, sem passar pelo Main_Menu

        switch (GameStateManager.Instance.CurrentState)
        {
            case GameState.MapSelect: // acabou de vir de um New Game
                LoadingScreenUI.Instance?.SetStatus("Generating world...");
                SpawnHero(MainMenuUI.CurrentRun.hero);

                LoadingScreenUI.Instance?.SetStatus("Loading entities...");
                DayTimer.Instance.ResetForNewDay();
                // GameState.Gameplay só é setado DEPOIS do mundo/câmera assentarem (dentro da
                // coroutine) — achado real do usuário: setar aqui liberava GameplayGate.IsActive
                // (travamento de ação real do HeroController) antes da tela de loading sumir,
                // deixando o jogador agir com o mundo ainda escondido atrás dela.
                StartCoroutine(HideLoadingScreenWhenWorldReady());
                return;

            case GameState.Shop: // acabou de vir de um Continue Game
                LoadingScreenUI.Instance?.SetStatus("Reopening shop...");
                // Reemite o evento pra quem só assinou GameEvents.OnGameStateChanged nesta cena
                // nova (uma UI de Loja que more só na cena de gameplay, por exemplo) — o primeiro
                // SetState (feito no MainMenuUI, antes do LoadScene) já passou sem ninguém aqui
                // pra ouvir.
                GameEvents.GameStateChanged(GameState.Shop);
                break;
        }

        LoadingScreenUI.Instance?.Hide();
    }

    // Achado real do usuário (New Game -> Herói -> Mundo): a câmera do Cinemachine começava
    // num zoom bem alto (mostrando chunk preto/não gerado) e só assentava no normal depois de
    // uns poucos frames — porque a tela de loading sumia na hora que SpawnHero() retornava,
    // bem antes do anel de chunks ao redor do herói terminar de pintar (só o chunk central é
    // síncrono) e antes do Cinemachine Brain terminar de enquadrar o alvo novo. Espera os 2:
    // ChunkTerrainGenerator.IsInitialAreaReady, mais uma folga de frames pro Brain estabilizar.
    private IEnumerator HideLoadingScreenWhenWorldReady()
    {
        while (ChunkTerrainGenerator.Instance != null && !ChunkTerrainGenerator.Instance.IsInitialAreaReady)
            yield return null;

        for (int i = 0; i < 5; i++) yield return null; // folga pro Cinemachine Brain assentar

        // Só AGORA libera GameplayGate.IsActive (travamento de ação real do HeroController,
        // DayTimer, etc.) — antes disso o jogador não deve conseguir fazer nada, mundo ainda
        // escondido atrás da tela de loading.
        GameStateManager.Instance.SetState(GameState.Gameplay);
        LoadingScreenUI.Instance?.Hide();
    }

    // Instancia o herói escolhido em (0,0,0) dentro do GameObject "//HEROS" e aponta a câmera do
    // Cinemachine pra ele — sem isso, a câmera continuaria seguindo qualquer herói antigo que já
    // estivesse na cena antes (ou nenhum).
    private void SpawnHero(string heroName)
    {
        // HeroController.IsPlayerUntargetable/IsDying são static (só existe 1 herói jogável por
        // vez) — se ficaram travados de uma sessão de Play anterior (Reload Domain desligado no
        // Enter Play Mode Settings não reseta static entre Stop/Play) ou de uma morte recém
        // resolvida, todo monstro ficava cego pro herói novo, ou a população ficava travada sem
        // repovoar pra sempre. Todo herói novo (início de run OU RespawnHeroFresh) garante os 2
        // resetados.
        HeroController.IsPlayerUntargetable = false;
        HeroController.IsDying = false;

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
        currentHeroObj = heroObj;

        if (cinemachineCamera != null) cinemachineCamera.Target.TrackingTarget = heroObj.transform;
        else Debug.LogWarning("[GameplayBootstrap] CinemachineCamera não atribuída no Inspector — câmera não vai seguir o herói.");

        GameEvents.HeroSpawned(heroObj.transform);
    }

    // GDD Deadline 8.1, Seção 3 — achado real do usuário: reaproveitar a MESMA instância de
    // herói entre dias (só teleportando ela de volta pra origem) deixava vários valores presos
    // no estado antigo — qualquer coisa que só é aplicada no Instantiate/Start (ex.: size) não
    // atualizava até um New Game do zero. Destrói a instância atual (se ainda existir — na
    // morte ela já foi destruída em HeroController.AnimationDieEndEvent) e instancia 100% nova,
    // do mesmo jeito que o início da run faz. Chamado por ShopHandler.ProceedFromShop(), nos 2
    // casos (retry OU avanço de dia) — nunca só reposiciona.
    public void RespawnHeroFresh()
    {
        if (currentHeroObj != null) Destroy(currentHeroObj);
        PetController.DestroyAll(); // owner (herói antigo) some — sem isso, o pet ficava congelado na cena
        SpawnHero(MainMenuUI.CurrentRun.hero); // já zera IsPlayerUntargetable por dentro; summon do pet novo é automático (AnimationSummonPetEvent)
    }
}
