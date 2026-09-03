using UnityEngine;

public class SystemsBootstrap : MonoBehaviour
{
    private PlayerControls controls;

    private void Awake()
    {
        ProgressTracker.Init();
        controls = new PlayerControls();

        // Inventory (TAB) deixou de ser placeholder de pausa na Sprint 10 —
        // agora tem dono de verdade (BagController), que já pausa/despausa sozinho.
        controls.Gameplay.RemoteControl.performed += ctx => TimeManager.Instance.TogglePause();
        // Interact (E) deixou de ser placeholder de EnemyKilled na Sprint 6 —
        // agora aciona interações reais via InteractionManager (escadas, baús no futuro).
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
