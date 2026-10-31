using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Overlay de loading — cobre a troca pra cena de gameplay (carregamento assíncrono, sem travar
// o frame) e só libera a cena nova quando ela estiver pronta pra ativar. Fica num GameObject
// próprio, DontDestroyOnLoad (igual ao "Systems"), porque é chamado pelo MainMenuUI ANTES da
// troca de cena e precisa sobreviver a ela pra conseguir reportar o progresso até o fim.
public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    [SerializeField] private GameObject root; // painel cheio de tela, começa desativado
    [SerializeField] private Image progressFill; // Image.Type = Filled / Horizontal — fillAmount 0..1
    [SerializeField] private float minimumDuration = 0.5f; // evita "flash" em loads muito rápidos — 🔢 ajustável

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (root != null) root.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        if (root != null) root.SetActive(true);
        if (progressFill != null) progressFill.fillAmount = 0f;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        float startTime = Time.unscaledTime;

        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Unity trava o progress em 0.9 até allowSceneActivation liberar — por isso a barra
        // normaliza isso pra 0..1 "de verdade" em vez de parar visualmente em 90%.
        while (operation.progress < 0.9f)
        {
            if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        if (progressFill != null) progressFill.fillAmount = 1f;

        // Tempo mínimo de tela de loading, pra não "flashar" em cenas pequenas — puramente
        // cosmético, ajustável/removível depois (minimumDuration acima).
        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumDuration) yield return new WaitForSecondsRealtime(minimumDuration - elapsed);

        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;

        if (root != null) root.SetActive(false);
    }
}
