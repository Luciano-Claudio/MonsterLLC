using UnityEngine;

// D9.5 (revisão final, achado crítico): a morte agora SEMPRE recarrega o Main_Menu (e o "Menu" da
// pausa já recarregava). Sem esta trava, cada volta pro menu criava um 2º "Systems" e o antigo
// continuava vivo — 2 DayTimer (o velho, com o tempo restante da run morta, encerrava o Dia 1 da
// run seguinte antes da hora), 2 ResultsHandler, 2 BagController. Roda antes de todo componente
// do mesmo GameObject (DefaultExecutionOrder) e destrói a cópia nova antes de eles acordarem.
[DefaultExecutionOrder(-10000)]
public class SystemsBootstrap : MonoBehaviour
{
    private static SystemsBootstrap instance;

    [Tooltip("Gold fixo por kill (monstro comum/boss), por dia — GDD Deadline 8.1, Seção 3.1. Mesmo critério de toda outra tabela de dia do projeto (Inspector, nunca Resources.Load).")]
    [SerializeField] private DayGoldTableSO goldTable;

    [Tooltip("Telemetria de run (docs/superpowers/specs/2026-11-16-run-telemetry-design.md) — grava um JSON completo da sessão pra análise de balanceamento/diversão. Desmarcado = custo zero (não assina nada, não escreve nada).")]
    [SerializeField] private bool telemetryEnabled = true;

    private PlayerControls controls;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            DestroyImmediate(gameObject); // cópia da cena Main_Menu recarregada — o original (DontDestroyOnLoad) continua
            return;
        }
        instance = this;

        // Este GameObject ("Systems") carrega junto GameStateManager/TimeManager/BagController/
        // DayTimer/ShopHandler/ResultsHandler — tudo que precisa existir desde o boot do jogo
        // (cena Main_Menu) e sobreviver a qualquer troca de cena depois (ex.: carregar a cena de
        // gameplay). DontDestroyOnLoad aqui protege o grupo inteiro de uma vez, já que todos
        // vivem no mesmo GameObject.
        DontDestroyOnLoad(gameObject);

        ProgressTracker.Init();
        KillGoldReward.Init(goldTable); // GDD Deadline 8.1, Seção 3.1 — gold fixo por kill/boss, por dia
        DayStatsTracker.Init(); // Resumo do Dia — contadores/gold/loot vendido, por dia
        gameObject.AddComponent<RunTelemetryRecorder>().Init(telemetryEnabled);
        gameObject.AddComponent<HeroLevel>(); // D9.6b — XP e nível do herói (estado no RunState)
        var scaling = MonsterScalingSettings.Load(); // D9.6a — tabela por tier editável (XP por kill em runtime)
        if (scaling != null) scaling.ApplyToRuntime();
        controls = new PlayerControls();

        // Inventory (TAB) deixou de ser placeholder de pausa na Sprint 10 —
        // agora tem dono de verdade (BagController), que já pausa/despausa sozinho.
        // ESC deixou de ser placeholder de pausa nesta sprint — agora tem dono de verdade
        // (PauseMenuUI, na cena de gameplay), que pausa/despausa junto com abrir/fechar o
        // próprio menu (inclusive clicando em "Resume", não só via Esc).
        // Interact (E) deixou de ser placeholder de EnemyKilled na Sprint 6 —
        // agora aciona interações reais via InteractionManager (escadas, baús no futuro).
    }

    private void OnEnable() => controls?.Enable();
    private void OnDisable() => controls?.Disable();
}
