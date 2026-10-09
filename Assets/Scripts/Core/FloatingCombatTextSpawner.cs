using UnityEngine;

// Escuta GameEvents.OnDamageTaken e instancia o prefab de texto flutuante DENTRO do Canvas
// de HUD que já existe na cena (arrastar em canvasParent) — reaproveita o Canvas que já
// funciona, em vez de criar um Canvas novo (evita as pegadinhas de World Space/CanvasScaler).
public class FloatingCombatTextSpawner : MonoBehaviour
{
    [SerializeField] private GameObject floatingCombatTextPrefab; // precisa ter FloatingCombatText
    [SerializeField] private Transform canvasParent; // arrastar aqui o Canvas de HUD da cena
    [SerializeField] private Color healTextColor = new Color(0.6f, 1f, 0.6f); // 🔢 verde claro, ajustável

    private void OnEnable()
    {
        GameEvents.OnDamageTaken += HandleDamageTaken;
        GameEvents.OnDamageBlocked += HandleDamageBlocked;
        GameEvents.OnHealReceived += HandleHealReceived;
    }

    private void OnDisable()
    {
        GameEvents.OnDamageTaken -= HandleDamageTaken;
        GameEvents.OnDamageBlocked -= HandleDamageBlocked;
        GameEvents.OnHealReceived -= HandleHealReceived;
    }

    private void HandleDamageTaken(Vector3 position, float amount)
    {
        if (floatingCombatTextPrefab == null || canvasParent == null) return;

        var obj = Instantiate(floatingCombatTextPrefab, canvasParent);
        var text = obj.GetComponent<FloatingCombatText>();
        if (text != null) text.Setup(position, amount);
    }

    private void HandleDamageBlocked(Vector3 position, float amount, Color color)
    {
        if (floatingCombatTextPrefab == null || canvasParent == null) return;

        var obj = Instantiate(floatingCombatTextPrefab, canvasParent);
        var text = obj.GetComponent<FloatingCombatText>();
        if (text != null) text.Setup(position, amount, color);
    }

    private void HandleHealReceived(Vector3 position, float amount)
    {
        if (floatingCombatTextPrefab == null || canvasParent == null) return;

        var obj = Instantiate(floatingCombatTextPrefab, canvasParent);
        var text = obj.GetComponent<FloatingCombatText>();
        if (text != null) text.Setup(position, amount, healTextColor, isHeal: true);
    }
}
