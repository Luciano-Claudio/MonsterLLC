using System.Collections.Generic;
using UnityEngine;

// Escuta GameEvents.OnDamageTaken e instancia o prefab de texto flutuante DENTRO do Canvas
// de HUD que já existe na cena (arrastar em canvasParent) — reaproveita o Canvas que já
// funciona, em vez de criar um Canvas novo (evita as pegadinhas de World Space/CanvasScaler).
//
// Fila de textos (pedido do usuário, 2026-11-27): no carregamento cria `prewarmCount` textos desligados;
// cada acerto liga um (cada acerto continua com o próprio número, nada é juntado) e, no fim da animação,
// ele se desliga e volta pra fila. Se faltar (mais de prewarmCount na tela ao mesmo tempo), cria mais e
// guarda também — a fila só cresce.
public class FloatingCombatTextSpawner : MonoBehaviour
{
    [SerializeField] private GameObject floatingCombatTextPrefab; // precisa ter FloatingCombatText
    [SerializeField] private Transform canvasParent; // arrastar aqui o Canvas de HUD da cena
    [SerializeField] private Color healTextColor = new Color(0.6f, 1f, 0.6f); // 🔢 verde claro, ajustável
    [Tooltip("Textos criados (desligados) no carregamento da cena. Se precisar de mais ao mesmo tempo, cria e guarda.")]
    [SerializeField] private int prewarmCount = 300;

    private readonly Stack<FloatingCombatText> pool = new Stack<FloatingCombatText>();

    private void Start()
    {
        if (floatingCombatTextPrefab == null || canvasParent == null) return;
        for (int i = pool.Count; i < prewarmCount; i++)
        {
            var text = Create();
            if (text == null) return;
            text.gameObject.SetActive(false);
            pool.Push(text);
        }
    }

    private FloatingCombatText Create()
    {
        var text = Instantiate(floatingCombatTextPrefab, canvasParent).GetComponent<FloatingCombatText>();
        if (text != null) text.Owner = this;
        return text;
    }

    // Pega um texto desligado da fila (ou cria um novo, que depois volta pra fila também) e liga.
    private FloatingCombatText Take()
    {
        if (floatingCombatTextPrefab == null || canvasParent == null) return null;
        FloatingCombatText text = null;
        while (pool.Count > 0 && text == null) text = pool.Pop(); // ignora algum destruído por fora
        if (text == null) text = Create();
        if (text == null) return null;
        text.transform.SetAsLastSibling(); // o mais novo desenha por cima, como antes
        text.gameObject.SetActive(true);
        return text;
    }

    // Chamado pelo próprio texto no fim da animação (ou pelo FloatingCombatText.DestroyAll).
    public void Release(FloatingCombatText text)
    {
        if (text == null || !text.gameObject.activeSelf) return;
        text.gameObject.SetActive(false);
        pool.Push(text);
    }

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

    // `isPlayer`/`isDirectPlayerHit` não importam pro texto flutuante (mesmo visual sempre,
    // inclusive DoT/Pet) — só existem no evento pra quem precisa diferenciar (HitFeelController).
    private void HandleDamageTaken(Vector3 position, float amount, bool isPlayer, bool isDirectPlayerHit)
    {
        var text = Take();
        if (text != null) text.Setup(position, amount);
    }

    private void HandleDamageBlocked(Vector3 position, float amount, Color color)
    {
        var text = Take();
        if (text != null) text.Setup(position, amount, color);
    }

    private void HandleHealReceived(Vector3 position, float amount)
    {
        var text = Take();
        if (text != null) text.Setup(position, amount, healTextColor, isHeal: true);
    }
}
