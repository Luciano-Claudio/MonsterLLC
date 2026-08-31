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
        controls.Gameplay.Interact.performed += ctx => GameEvents.EnemyKilled(); // placeholder até existir inimigo real
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
