using UnityEngine;

public class PlayerInputTest : MonoBehaviour
{
    private PlayerControls controls;
    private Vector2 moveInput;
    public float moveSpeed = 5f;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Gameplay.Attack.performed += ctx => Debug.Log("Attack (LMB)");
        controls.Gameplay.Ultimate.performed += ctx => Debug.Log("Ultimate (RMB)");
        controls.Gameplay.Interact.performed += ctx => Debug.Log("Interact (E)");
        controls.Gameplay.Inventory.performed += ctx => Debug.Log("Inventory (TAB)");
        controls.Gameplay.RemoteControl.performed += ctx => Debug.Log("RemoteControl (Q)");
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        transform.Translate(moveInput * moveSpeed * Time.deltaTime);
    }
}
