using UnityEngine;
using TMPro;

// Número de dano subindo e sumindo. Vive dentro do Canvas de HUD existente (Screen Space -
// Overlay), e a cada frame converte a posição de mundo (que só existe em memória, o objeto
// em si nunca sai do Canvas) pra posição de tela via Camera.WorldToScreenPoint — é a forma
// mais comum e testada de fazer isso, sem depender de Canvas World Space (que já causou
// dor de cabeça). Sem pooling — mesmo padrão de todo projétil do projeto.
public class FloatingCombatText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float lifetime = 1f; // 🔢 referência 0.8-1.2s, ajustável
    [SerializeField] private float horizontalSpeed = 0.6f; // 🔢 ajustável — velocidade lateral inicial
    [SerializeField] private float verticalSpeed = 1.5f; // 🔢 ajustável — velocidade pra cima inicial
    [SerializeField] private float gravity = 3f; // 🔢 ajustável — desacelera a subida até cair um pouco no fim
    [SerializeField] private float randomXOffset = 0.15f; // 🔢 unidades de mundo, ajustável

    private RectTransform rectTransform;
    private Camera cam;
    private Vector3 worldPosition;
    private Vector3 velocity;
    private float elapsed;
    private Color baseColor;

    public void Setup(Vector3 spawnWorldPosition, float amount)
    {
        rectTransform = (RectTransform)transform;
        cam = Camera.main;

        worldPosition = spawnWorldPosition + new Vector3(Random.Range(-randomXOffset, randomXOffset), 0f, 0f);

        // Desvio horizontal já reaproveitado pra sortear se o "balão" vai pra esquerda ou direita.
        float side = Random.value < 0.5f ? -1f : 1f;
        velocity = new Vector3(side * horizontalSpeed, verticalSpeed, 0f);

        if (label != null)
        {
            label.text = LargeNumberFormatter.Format(amount);
            baseColor = label.color;
        }

        rectTransform.localScale = Vector3.one;
        UpdateScreenPosition();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        velocity += Vector3.down * gravity * Time.deltaTime;
        worldPosition += velocity * Time.deltaTime;
        UpdateScreenPosition();

        rectTransform.localScale = Vector3.one * (1f - t);

        if (label != null)
        {
            Color c = baseColor;
            c.a = 1f - t;
            label.color = c;
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void UpdateScreenPosition()
    {
        if (cam == null) return;
        rectTransform.position = cam.WorldToScreenPoint(worldPosition);
    }
}
