using UnityEngine;

public class SystemsBootstrap : MonoBehaviour
{
    private PlayerControls controls;

    private void Awake()
    {
        // Este GameObject ("Systems") carrega junto GameStateManager/TimeManager/BagController/
        // DemandTracker/DayTimer/ShopHandler/ResultsHandler/GameOverHandler — tudo que precisa
        // existir desde o boot do jogo (cena Main_Menu) e sobreviver a qualquer troca de cena
        // depois (ex.: carregar a cena de gameplay). DontDestroyOnLoad aqui protege o grupo
        // inteiro de uma vez, já que todos vivem no mesmo GameObject.
        DontDestroyOnLoad(gameObject);

        ProgressTracker.Init();
        controls = new PlayerControls();

        // Inventory (TAB) deixou de ser placeholder de pausa na Sprint 10 —
        // agora tem dono de verdade (BagController), que já pausa/despausa sozinho.
        controls.Gameplay.RemoteControl.performed += ctx => TimeManager.Instance.TogglePause();
        // ESC — mesmo TogglePause do Q. O menu clássico (sair/configurações) em si ainda não
        // existe (peça de UI própria, sem escopo definido ainda) — só a pausa já funciona.
        controls.Gameplay.Pause.performed += ctx => TimeManager.Instance.TogglePause();
        // Interact (E) deixou de ser placeholder de EnemyKilled na Sprint 6 —
        // agora aciona interações reais via InteractionManager (escadas, baús no futuro).
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
