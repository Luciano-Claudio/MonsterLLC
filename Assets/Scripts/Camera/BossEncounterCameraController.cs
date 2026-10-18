using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

// Sequência de "boss reveal" (Cinemachine, Game Feel — ideia experimental do usuário, cada
// trecho fácil de remover se não ficar bom no teste): ao nascer um boss, pausa o jogo, troca o
// Follow da câmera pro boss, segura nele, volta o Follow pro jogador e despausa. SEM
// CinemachineTargetGroup — removido por decisão do usuário (o Group Framing afastando a
// câmera durante o combate atrapalhava a visão/controle do jogador). Depois do reveal a
// câmera simplesmente volta a seguir só o jogador, igual sempre foi.
// Achado real: CinemachineBrain.DefaultBlend só blenda troca ENTRE vcams diferentes, nunca
// troca de Follow dentro do MESMO vcam — e o CinemachineFollow daqui roda com
// PositionDamping=0 (parte da correção do jitter do Pixel Perfect), então sem o pan suave
// abaixo a troca de Follow seria um corte seco.
public class BossEncounterCameraController : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;

    [Header("Boss Reveal")]
    public float bossRevealHoldDuration = 1.5f;
    [Tooltip("Espera depois de voltar o Follow pro jogador, antes de despausar — dá tempo do pan de volta terminar antes do jogo se mexer de novo.")]
    public float returnToPlayerHoldDuration = 0.3f;

    [Header("Pan suave durante o reveal")]
    [Tooltip("O CinemachineFollow da câmera roda com PositionDamping 0 (de propósito — parte da correção do jitter do Pixel Perfect). Sem isso, trocar o Follow vira um corte seco em vez de pan. Aqui sobe o damping SÓ durante o reveal (jogo pausado, alvos parados) e devolve a 0 antes de despausar — nunca toca no damping durante gameplay normal.")]
    public CinemachineFollow followComponent;
    public float revealPanDamping = 1.5f;

    private Transform player;
    private bool revealInProgress;

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnBossSpawned += HandleBossSpawned;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnBossSpawned -= HandleBossSpawned;
    }

    // Só guarda a referência — quem seta o Follow inicial da câmera pro jogador continua sendo
    // o MainMenuUI (já fazia isso antes desta feature existir).
    private void HandleHeroSpawned(Transform hero) => player = hero;

    private void HandleBossSpawned(Transform boss)
    {
        if (boss == null) return;

        // Já tem outro reveal rolando (2 bosses nascendo muito perto no tempo, ex.: replacement
        // por distância logo depois de um Boss Timer) — não briga por Follow no meio de uma
        // sequência já em andamento, simplesmente ignora o reveal deste segundo boss.
        if (revealInProgress || cinemachineCamera == null || player == null) return;

        StartCoroutine(PlayBossRevealSequence(boss));
    }

    private IEnumerator PlayBossRevealSequence(Transform boss)
    {
        revealInProgress = true;
        TimeManager.Instance?.Pause();

        Vector3 originalDamping = followComponent != null ? followComponent.TrackerSettings.PositionDamping : Vector3.zero;
        if (followComponent != null) followComponent.TrackerSettings.PositionDamping = Vector3.one * revealPanDamping;

        cinemachineCamera.Follow = boss;
        yield return new WaitForSecondsRealtime(bossRevealHoldDuration);

        cinemachineCamera.Follow = player;
        yield return new WaitForSecondsRealtime(returnToPlayerHoldDuration);

        // Volta o damping pro valor original ANTES de despausar — gameplay normal (jogador se
        // movendo de verdade) nunca deve ver nenhum damping > 0 aqui, senão reintroduz o
        // jitter do Pixel Perfect que já foi resolvido.
        if (followComponent != null) followComponent.TrackerSettings.PositionDamping = originalDamping;

        TimeManager.Instance?.Resume();

        revealInProgress = false;
    }
}
