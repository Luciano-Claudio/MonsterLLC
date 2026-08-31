using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
    public GameObject iconVisual;
    public Vector3 offset = new Vector3(0, 1, 0);

    private void Start() => iconVisual.SetActive(false);

    private void OnEnable() => GameEvents.OnInteractPromptChanged += UpdatePrompt;
    private void OnDisable() => GameEvents.OnInteractPromptChanged -= UpdatePrompt;

    private void UpdatePrompt(Transform anchor)
    {
        if (anchor == null)
        {
            iconVisual.SetActive(false);
            return;
        }
        transform.position = anchor.position + offset;
        iconVisual.SetActive(true);
    }
}
