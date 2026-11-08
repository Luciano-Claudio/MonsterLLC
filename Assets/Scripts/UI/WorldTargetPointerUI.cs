using UnityEngine;
using UnityEngine.UI;

// Seta de borda de tela apontando pra um alvo fora da câmera — Loja ou Baú (GDD Deadline 8.1,
// Seção 8). 1 componente reaproveitável pros 2 casos: criar 2 GameObjects (cada um com sua
// própria Image) dentro do Canvas do HUD, cada um com este script, trocando só targetType no
// Inspector. Fica inativo quando o alvo não existe (ex.: nenhum baú nasceu hoje, Vendor.Instance
// ainda null) OU quando ele já está visível na câmera — mesmo critério de "dentro da visão" que
// WorldSpawnUtility.IsOutsideCameraView usa pro spawn (WorldToViewportPoint).
//
// Algoritmo de clamp na borda da tela é o padrão de mercado pra indicador fora de tela
// (projeta a posição de tela, inverte se estiver atrás da câmera, e resolve a interseção da
// linha centro->alvo com o retângulo da tela encolhido por edgePadding).
public class WorldTargetPointerUI : MonoBehaviour
{
    private enum TargetType { Vendor, Chest }

    [SerializeField] private TargetType targetType;

    [Tooltip("Margem (em pixels de tela) entre a seta e a borda real da tela.")]
    [SerializeField] private float edgePadding = 60f;

    [Tooltip("Graus extras de rotação pra bater com a direção que a sprite já aponta por padrão (0 = sprite desenhada apontando pra direita).")]
    [SerializeField] private float angleOffsetDegrees = 0f;

    private RectTransform selfRect;
    private RectTransform canvasRect;
    private Graphic graphic; // desliga só a Image, nunca o GameObject — o Update() deste
    // script precisa continuar rodando mesmo "invisível", pra conseguir religar sozinho
    // quando o alvo saber da tela de novo (GameObject inativo não recebe Update()).

    private void Awake()
    {
        selfRect = GetComponent<RectTransform>();
        graphic = GetComponent<Graphic>();
        var canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
    }

    private Transform CurrentTarget()
    {
        switch (targetType)
        {
            case TargetType.Vendor: return Vendor.Instance != null ? Vendor.Instance.transform : null;
            case TargetType.Chest: return WorldChestSpawnManager.Instance != null ? WorldChestSpawnManager.Instance.AliveChestTransform : null;
            default: return null;
        }
    }

    private void Update()
    {
        var target = CurrentTarget();
        var cam = Camera.main;

        if (target == null || cam == null || canvasRect == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 viewport = cam.WorldToViewportPoint(target.position);
        bool onScreen = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        if (onScreen)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);
        if (screenPos.z < 0f)
        {
            // Atrás da câmera — espelha pelo centro da tela (truque padrão do algoritmo).
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 dir = (Vector2)screenPos - screenCenter;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float angle = Mathf.Atan2(dir.y, dir.x);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        float halfW = Mathf.Max(1f, screenCenter.x - edgePadding);
        float halfH = Mathf.Max(1f, screenCenter.y - edgePadding);

        Vector2 clamped;
        if (halfW * Mathf.Abs(sin) < halfH * Mathf.Abs(cos))
        {
            // A linha centro->alvo cruza a borda esquerda/direita antes da de cima/baixo.
            float sign = cos > 0f ? 1f : -1f;
            clamped = new Vector2(sign * halfW, sign * halfW * (sin / cos));
        }
        else
        {
            float sign = sin > 0f ? 1f : -1f;
            clamped = new Vector2(sign * halfH * (cos / sin), sign * halfH);
        }

        Vector2 clampedScreenPos = screenCenter + clamped;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, clampedScreenPos, null, out Vector2 localPoint))
        {
            selfRect.anchoredPosition = localPoint;
        }

        selfRect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + angleOffsetDegrees);
    }

    private void SetVisible(bool visible)
    {
        if (graphic != null) graphic.enabled = visible;
    }
}
