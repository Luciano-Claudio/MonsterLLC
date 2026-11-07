using UnityEngine;

public class SystemsBootstrap : MonoBehaviour
{
    [Tooltip("Gold fixo por kill (monstro comum/boss), por dia — GDD Deadline 8.1, Seção 3.1. Mesmo critério de toda outra tabela de dia do projeto (Inspector, nunca Resources.Load).")]
    [SerializeField] private DayGoldTableSO goldTable;

    private PlayerControls controls;

    private void Awake()
    {
        // Este GameObject ("Systems") carrega junto GameStateManager/TimeManager/BagController/
        // DayTimer/ShopHandler/ResultsHandler — tudo que precisa existir desde o boot do jogo
        // (cena Main_Menu) e sobreviver a qualquer troca de cena depois (ex.: carregar a cena de
        // gameplay). DontDestroyOnLoad aqui protege o grupo inteiro de uma vez, já que todos
        // vivem no mesmo GameObject.
        DontDestroyOnLoad(gameObject);

        ProgressTracker.Init();
        KillGoldReward.Init(goldTable); // GDD Deadline 8.1, Seção 3.1 — gold fixo por kill/boss, por dia
        DayStatsTracker.Init(); // Resumo do Dia — contadores/gold/loot vendido, por dia
        controls = new PlayerControls();

        // Inventory (TAB) deixou de ser placeholder de pausa na Sprint 10 —
        // agora tem dono de verdade (BagController), que já pausa/despausa sozinho.
        // ESC deixou de ser placeholder de pausa nesta sprint — agora tem dono de verdade
        // (PauseMenuUI, na cena de gameplay), que pausa/despausa junto com abrir/fechar o
        // próprio menu (inclusive clicando em "Resume", não só via Esc).
        // Interact (E) deixou de ser placeholder de EnemyKilled na Sprint 6 —
        // agora aciona interações reais via InteractionManager (escadas, baús no futuro).
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
