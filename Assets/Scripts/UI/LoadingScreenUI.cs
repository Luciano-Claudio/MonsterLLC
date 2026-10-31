using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Overlay de loading — cobre a troca pra cena de gameplay (carregamento assíncrono, sem travar
// o frame) e só libera a cena nova quando ela estiver pronta pra ativar. Fica num GameObject
// próprio, DontDestroyOnLoad (igual ao "Systems"), porque é chamado pelo MainMenuUI ANTES da
// troca de cena e precisa sobreviver a ela pra conseguir reportar o progresso até o fim.
//
// Importante (bug real, 2026-10-31): mostrar a tela e SÓ DEPOIS fazer trabalho pesado no mesmo
// frame não adianta nada — o Unity não desenha nada na tela até o frame inteiro terminar, então
// a tela de loading "aparecia" só depois do trabalho pesado já ter travado o jogo por um tempo.
// Por isso LoadSceneRoutine cede 2 frames (yield null + WaitForEndOfFrame) logo depois de ativar
// a tela, ANTES de chamar preLoadSetup ou SceneManager.LoadSceneAsync — garante que o jogador
// VÊ a tela antes de qualquer coisa pesada começar. E ela só se esconde quando alguém chama
// Hide() de fora (GameplayBootstrap, na cena nova) — cobre o Awake/Start da cena nova também,
// não só o carregamento bruto do asset da cena.
public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    [SerializeField] private GameObject root; // painel cheio de tela, começa desativado
    [SerializeField] private Image progressFill; // Image.Type = Filled / Horizontal — fillAmount 0..1
    [SerializeField] private RectTransform headBar; // ProgressBar_Head_Fill — acompanha a ponta do preenchimento
    [SerializeField] private TMP_Text statusText; // "Carregando...", "Gerando o mundo...", etc.
    [SerializeField] private float minimumDuration = 0.5f; // evita "flash" em loads muito rápidos — 🔢 ajustável

    // Mesma lógica de HealthBarVisuals usada no Health_Bar/Time_Bar (Seção de UI) — a sprite do
    // Loading_Bar.png reaproveita o mesmo canvas de 112px do Time_Bar (86px de conteúdo opaco
    // centralizado), por isso as mesmas frações 13/112 e 99/112.
    [Header("Conteúdo visível dentro do canvas da sprite (padrão Health/Time_Bar)")]
    [SerializeField] private float contentLeftFraction = 13f / 112f;
    [SerializeField] private float contentRightFraction = 99f / 112f;

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

    // preLoadSetup roda DEPOIS da tela já estar visível (e renderizada) — é onde quem chamou
    // deve colocar qualquer trabalho síncrono pesado (ex.: MainMenuUI.StartRun criando a
    // RunState), pra nunca travar o jogo antes do jogador ver que algo está acontecendo.
    public void LoadScene(string sceneName, Action preLoadSetup = null)
    {
        // Precisa ativar o GameObject AQUI, antes do StartCoroutine — uma coroutine não começa
        // em GameObject inativo (erro real: "Coroutine couldn't be started because the game
        // object is inactive"). O resto (os 2 frames de espera) fica dentro da coroutine.
        if (root != null) root.SetActive(true);
        SetFill(0f);
        SetStatus("Carregando...");

        StartCoroutine(LoadSceneRoutine(sceneName, preLoadSetup));
    }

    public void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // Mesmo remapeamento de conteúdo do Health_Bar/Time_Bar (HealthBarVisuals) — sem isso, o
    // fillAmount nativo ficaria "parado" em 100%/0% antes de cruzar a margem transparente da
    // sprite, e o Head_Fill não acompanharia a ponta visível do preenchimento.
    private void SetFill(float fillAmount01)
    {
        if (progressFill == null) return;
        HealthBarVisuals.Apply(progressFill, headBar, fillAmount01, contentLeftFraction, contentRightFraction);
    }

    // Chamado de fora (GameplayBootstrap, já na cena nova) quando o setup pós-load termina de
    // verdade — herói spawnado, dia iniciado, etc. Não é a Routine abaixo que decide isso.
    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action preLoadSetup)
    {
        // Cede 2 frames pro Unity realmente desenhar a tela acima antes de seguir — sem isso,
        // o SetActive(true) fica "pendurado" sem nunca chegar à tela enquanto o resto deste
        // método roda tudo no mesmo frame.
        yield return null;
        yield return new WaitForEndOfFrame();

        preLoadSetup?.Invoke();

        float startTime = Time.unscaledTime;

        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Unity trava o progress em 0.9 até allowSceneActivation liberar — por isso a barra
        // normaliza isso pra 0..1 "de verdade" em vez de parar visualmente em 90%.
        while (operation.progress < 0.9f)
        {
            SetFill(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }

        SetFill(1f);
        SetStatus("Entrando no mundo...");

        // Tempo mínimo de tela de loading, pra não "flashar" em cenas pequenas — puramente
        // cosmético, ajustável/removível depois (minimumDuration acima).
        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minimumDuration) yield return new WaitForSecondsRealtime(minimumDuration - elapsed);

        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;

        // Não chama Hide() aqui de propósito — GameplayBootstrap, já rodando na cena nova,
        // chama quando o setup pós-load (spawn do herói, início do dia) realmente terminar.
    }
}
