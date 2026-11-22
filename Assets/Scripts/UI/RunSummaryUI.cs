using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// D9.5 (decisão do usuário, 2026-11-21 — docs/superpowers/specs/2026-11-21-d9-5-estrutura-run-design.md):
// tela "Resumo total" da morte. Aparece com a tela coberta pelo CircleWipe e é revelada por ele:
// "Sobreviveu X dias" (dias completos, RunSummaryRules) e o gold permanente coletado (0 até o
// D9.9 existir). O botão "Continuar" vai pro menu direto, sem transição.
// Montada por código (sem editar cena) — placeholder visual: o usuário vai reformular a arte.
public class RunSummaryUI : MonoBehaviour
{
    private const int CanvasSortingOrder = 30000; // abaixo do canvas da transição (32767), que a revela

    public static RunSummaryUI Show(int daysSurvived, long permanentGold)
    {
        // Tela do usuário (RunSummaryScreen na cena, desativada) tem prioridade. A montada por
        // código abaixo é só a reserva, pra cena sem a tela dele.
        var screen = Object.FindAnyObjectByType<RunSummaryScreen>(FindObjectsInactive.Include);
        if (screen != null)
        {
            screen.Show(daysSurvived, permanentGold);
            return null;
        }

        var go = new GameObject("[RunSummaryUI]");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();

        var background = CreateChild(go.transform, "Background");
        Stretch(background);
        background.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.07f, 1f);

        CreateText(go.transform, "Title", "Resumo total", 96, new Vector2(0f, 260f));
        CreateText(go.transform, "Days", $"Sobreviveu {daysSurvived} {(daysSurvived == 1 ? "dia" : "dias")}", 64, new Vector2(0f, 80f));
        CreateText(go.transform, "Gold", $"Gold permanente: {permanentGold}", 64, new Vector2(0f, -30f));

        var buttonRect = CreateChild(go.transform, "ContinueButton");
        buttonRect.sizeDelta = new Vector2(480f, 120f);
        buttonRect.anchoredPosition = new Vector2(0f, -260f);
        var buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.75f, 0.2f, 0.15f, 1f);
        var button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(GoToMenu);
        var label = CreateText(buttonRect, "Label", "Continuar", 56, Vector2.zero);
        Stretch((RectTransform)label.transform);

        return go.AddComponent<RunSummaryUI>();
    }

    // Sem transição (decisão do usuário) — mesmo caminho do "voltar ao menu" do menu de pausa.
    private static void GoToMenu() => RunExit.ToMainMenu(); // despausa, estado MainMenu, cancela fade — ver RunExit

    private static RectTransform CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Vector2 position)
    {
        var rect = CreateChild(parent, name);
        rect.sizeDelta = new Vector2(1600f, 140f);
        rect.anchoredPosition = position;
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }
}
