using UnityEngine;

public class SystemsBootstrap : MonoBehaviour
{
    private PlayerControls controls;

    private void Awake()
    {
        ProgressTracker.Init();
        controls = new PlayerControls();

        controls.Gameplay.Inventory.performed += ctx => TimeManager.Instance.TogglePause();
        controls.Gameplay.RemoteControl.performed += ctx => TimeManager.Instance.TogglePause();
        // Interact (E) deixou de ser placeholder de EnemyKilled na Sprint 6 —
        // agora aciona interações reais via InteractionManager (escadas, baús no futuro).
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
