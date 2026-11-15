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

    // overrideColor == null usa a cor já configurada no TextMeshProUGUI (dano normal). Passar
    // uma cor (ex.: dano bloqueado pelo shield do Paladin, cura) sobrescreve só essa instância.
    // isHeal só controla o "+" na frente do número (GameEvents.OnHealReceived).
    public void Setup(Vector3 spawnWorldPosition, float amount, Color? overrideColor = null, bool isHeal = false)
    {
        rectTransform = (RectTransform)transform;
        cam = Camera.main;

        worldPosition = spawnWorldPosition + new Vector3(Random.Range(-randomXOffset, randomXOffset), 0f, 0f);

        // Desvio horizontal já reaproveitado pra sortear se o "balão" vai pra esquerda ou direita.
        float side = Random.value < 0.5f ? -1f : 1f;
        velocity = new Vector3(side * horizontalSpeed, verticalSpeed, 0f);

        if (label != null)
        {
            label.text = (isHeal ? "+" : "") + LargeNumberFormatter.Format(amount);
            baseColor = overrideColor ?? label.color;
        }

        rectTransform.localScale = Vector3.one;
        UpdateScreenPosition();
    }

    // Pedido do usuário (2026-11-14): texto flutuante NUNCA pode ficar congelado na tela —
    // nem no menu de pausa, nem numa cutscene de spawn de boss. De propósito, sem checar
    // GameplayGate aqui (ao contrário de quase todo outro script do projeto): a pausa deste
    // jogo nunca move Time.timeScale (só uma flag, ver TimeManager), então sem esse gate o
    // texto simplesmente termina a própria animação e se destrói sozinho em ~1s, pausado ou
    // não — resolve de raiz em vez de depender de DestroyAll() ser chamado em todo lugar que
    // possa pausar o jogo (que é exatamente o problema que o usuário relatou: faltava cobrir
    // o pause manual e o spawn de boss). DestroyAll() continua existindo como rede de
    // segurança pros DayResolver/ShopHandler, mas não é mais a única forma de sumir.
    private void Update()
    {
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

    // Rede de segurança pra sumir IMEDIATAMENTE na virada do dia (não esperar o ~1s de
    // lifetime normal) — Update() agora sempre termina a própria animação sozinho (ver
    // comentário lá), então isto não é mais estritamente necessário pra evitar congelamento,
    // só deixa a troca de dia/loja visualmente limpa na hora. Chamado por DayResolver junto
    // com LootDrop.DestroyAllOnGround()/GameEvents.ClearAllEnemies.
    public static void DestroyAll()
    {
        var texts = Object.FindObjectsByType<FloatingCombatText>(FindObjectsSortMode.None);
        foreach (var text in texts)
        {
            if (text != null) Destroy(text.gameObject);
        }
    }

    private void UpdateScreenPosition()
    {
        if (cam == null) return;
        rectTransform.position = cam.WorldToScreenPoint(worldPosition);
    }
}
