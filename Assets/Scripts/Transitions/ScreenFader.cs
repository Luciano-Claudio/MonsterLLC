using System;
using System.Collections;
using EasyTransition;
using UnityEngine;
using UnityEngine.UI;

// D9.5 (docs/superpowers/specs/2026-11-21-d9-5-estrutura-run-design.md) — toca as METADES de uma
// transição do Easy Transitions separadas, o que o TransitionManager do pacote não faz (ele
// sempre cobre e, na troca de cena, revela). Decisão do usuário:
// - menu -> loading: só a metade que escurece (FadeToBlack);
// - loading -> jogo: só a metade que clareia (SnapToBlack + FadeFromBlack).
// Usa os próprios prefabs IN/OUT do TransitionSettings. Persiste entre cenas (a transição do menu
// atravessa a troca de cena). Tempo real (unscaled): pausa não trava o fade.
public class ScreenFader : MonoBehaviour
{
    private const int CanvasSortingOrder = 32760; // logo abaixo do canvas do pacote (32767)

    private static ScreenFader instance;
    private RectTransform panel;
    private GameObject current;

    private static ScreenFader Instance
    {
        get
        {
            if (instance != null) return instance;
            var go = new GameObject("[ScreenFader]");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>(); // com o CanvasGroup de cada metade, bloqueia clique durante o fade (ver Replace)
            instance = go.AddComponent<ScreenFader>();
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(go.transform, false);
            instance.panel = (RectTransform)panelGo.transform;
            instance.panel.anchorMin = Vector2.zero;
            instance.panel.anchorMax = Vector2.one;
            instance.panel.offsetMin = instance.panel.offsetMax = Vector2.zero;
            return instance;
        }
    }

    private static float Duration(TransitionSettings s)
    {
        float t = s.transitionTime;
        if (s.autoAdjustTransitionTime && s.transitionSpeed > 0f) t /= s.transitionSpeed;
        return t;
    }

    // Toca a metade que cobre a tela; ao terminar, segura o preto e chama onBlack.
    public static void FadeToBlack(TransitionSettings settings, Action onBlack)
    {
        if (settings == null) { onBlack?.Invoke(); return; }
        var f = Instance;
        f.StopAllCoroutines();
        f.Replace(settings.transitionIn, settings, jumpToEnd: false);
        f.StartCoroutine(f.After(Duration(settings), onBlack));
    }

    // Preto na hora (a metade que cobre, já no último frame).
    public static void SnapToBlack(TransitionSettings settings)
    {
        if (settings == null) return;
        var f = Instance;
        f.StopAllCoroutines();
        f.Replace(settings.transitionIn, settings, jumpToEnd: true);
    }

    // A partir do preto, toca a metade que revela; ao terminar, some e chama onClear.
    public static void FadeFromBlack(TransitionSettings settings, Action onClear)
    {
        if (settings == null) { ClearInstant(); onClear?.Invoke(); return; }
        var f = Instance;
        f.StopAllCoroutines();
        f.Replace(settings.transitionOut, settings, jumpToEnd: false);
        f.StartCoroutine(f.After(Duration(settings), () => { ClearInstant(); onClear?.Invoke(); }));
    }

    // Revisão final (achado 3): sair pro menu (RunExit) cancela o fade em andamento — o callback
    // do clarear (SetState(Gameplay)) não pode disparar já no menu.
    public static void Cancel()
    {
        if (instance == null) return;
        instance.StopAllCoroutines();
        ClearInstant();
    }

    public static void ClearInstant()
    {
        if (instance == null || instance.current == null) return;
        Destroy(instance.current);
        instance.current = null;
    }

    private void Replace(GameObject prefab, TransitionSettings settings, bool jumpToEnd)
    {
        if (current != null) Destroy(current);
        current = null;
        if (prefab == null) return;
        current = Instantiate(prefab, panel);
        // Mesmo critério do pacote (Transition.cs): o blockRaycasts do TransitionSettings decide se
        // a tela aceita clique durante o fade — sem isso, um 2º clique no menu reiniciava o fade.
        if (!current.TryGetComponent(out CanvasGroup group)) group = current.AddComponent<CanvasGroup>(); // sem "??": objeto Unity tem nulo falso no Editor
        group.blocksRaycasts = settings.blockRaycasts;
        foreach (var anim in current.GetComponentsInChildren<Animator>(true))
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            if (settings.transitionSpeed > 0f) anim.speed = settings.transitionSpeed;
            if (jumpToEnd)
            {
                anim.Play(0, 0, 1f);
                anim.Update(0f);
            }
        }
    }

    private IEnumerator After(float seconds, Action action)
    {
        yield return new WaitForSecondsRealtime(seconds);
        action?.Invoke();
    }
}
