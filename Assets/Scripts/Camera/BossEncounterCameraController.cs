using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

// Sequência de "boss reveal" (Game Feel) — ao nascer um grupo de bosses
// (GameEvents.OnBossGroupSpawned, 1+ bosses no mesmo ciclo), pausa o jogo, dá foco em
// sequência em CADA boss do grupo (hold configurável por boss), depois volta o foco pro
// jogador (hold configurável) e só então despausa.
//
// Reescrito (2026-10-21) — bug real reportado pelo usuário: a versão anterior trocava o
// Follow de UM vcam só e simulava o pan subindo CinemachineFollow.PositionDamping à mão, o
// que dava corte seco/tremida (jitter do Pixel Perfect lutando contra a troca de alvo sem
// blend de verdade). CinemachineBrain só blenda de fato TROCANDO de vcam (por Priority),
// nunca trocando o Follow dentro do MESMO vcam — então agora o reveal usa 2 vcams dedicados
// (instanciados aqui a partir de revealCameraPrefab, NUNCA o vcam principal do jogador) em
// ping-pong: aponta o Follow do vcam OCIOSO pro próximo alvo e SÓ DEPOIS sobe a Priority dele
// acima do vcam ativo — isso dispara um blend real do Brain, pixel-perfect incluso (o prefab
// tem CinemachinePixelPerfect, igual o vcam principal). Pra voltar ao jogador não precisa
// trocar o Follow de ninguém: o vcam principal nunca solta o Follow do jogador (ver
// MainMenuUI), só baixa a Priority de quem estava ativo e o principal retoma sozinho.
public class BossEncounterCameraController : MonoBehaviour
{
    [Header("Vcam principal (do jogador) — o Follow dele nunca é tocado por aqui")]
    public CinemachineCamera cinemachineCamera;
    [Tooltip("Priority explícita do vcam do jogador — precisa ficar SEMPRE acima da Priority ociosa dos vcams de reveal (0) e SEMPRE abaixo da Priority ativa deles durante o reveal.")]
    public int playerCameraPriority = 10;

    [Header("Vcam de reveal — instanciado 2x em Awake a partir deste prefab (nunca editar a cena pra isso)")]
    public CinemachineCamera revealCameraPrefab;
    [Tooltip("Priority que o vcam de reveal assume quando é a vez dele ficar em foco — soma +1 por alvo da sequência, então o 2º boss sempre fica ACIMA do 1º (garante o blend pro lado certo).")]
    public int revealCameraPriority = 20;

    [Header("Durações (ajustável — o jogo fica pausado durante a sequência inteira)")]
    [Tooltip("Quanto tempo a câmera segura em CADA boss do grupo, em sequência.")]
    public float bossRevealHoldDuration = 1f;
    [Tooltip("Quanto tempo a câmera segura de volta no jogador antes de despausar.")]
    public float returnToPlayerHoldDuration = 1f;

    private Transform player;
    private CinemachineCamera revealCamA;
    private CinemachineCamera revealCamB;
    private bool revealInProgress;
    private Coroutine revealRoutine;

    private void Awake()
    {
        if (revealCameraPrefab == null) return; // Game Feel desligado (prefab não configurado no Inspector) — HandleBossGroupSpawned vira no-op

        revealCamA = Instantiate(revealCameraPrefab, transform);
        revealCamB = Instantiate(revealCameraPrefab, transform);
        revealCamA.Priority = 0;
        revealCamB.Priority = 0;
    }

    private void OnEnable()
    {
        GameEvents.OnHeroSpawned += HandleHeroSpawned;
        GameEvents.OnBossGroupSpawned += HandleBossGroupSpawned;
    }

    private void OnDisable()
    {
        GameEvents.OnHeroSpawned -= HandleHeroSpawned;
        GameEvents.OnBossGroupSpawned -= HandleBossGroupSpawned;

        // Defesa contra o jogo ficar travado pausado pra sempre — se este objeto for
        // desativado/destruído NO MEIO de uma sequência (ex.: troca de cena), a coroutine
        // morre sem nunca chegar no TimeManager.Resume() do fim. Força o resume e devolve as
        // Priorities pro estado ocioso antes de sumir.
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
        if (revealInProgress)
        {
            if (revealCamA != null) revealCamA.Priority = 0;
            if (revealCamB != null) revealCamB.Priority = 0;
            TimeManager.Instance?.Resume();
            revealInProgress = false;
        }
    }

    // Só guarda a referência e trava a Priority do vcam do jogador — quem seta o Follow dele
    // continua sendo o MainMenuUI (já fazia isso antes desta feature existir).
    private void HandleHeroSpawned(Transform hero)
    {
        player = hero;
        if (cinemachineCamera != null) cinemachineCamera.Priority = playerCameraPriority;
    }

    private void HandleBossGroupSpawned(IReadOnlyList<Transform> bosses)
    {
        if (bosses == null || bosses.Count == 0) return;
        if (revealInProgress || cinemachineCamera == null || player == null) return;
        if (revealCamA == null || revealCamB == null) return;

        revealRoutine = StartCoroutine(PlayBossRevealSequence(bosses));
    }

    private IEnumerator PlayBossRevealSequence(IReadOnlyList<Transform> bosses)
    {
        revealInProgress = true;
        TimeManager.Instance?.Pause();

        var active = revealCamA;
        var idle = revealCamB;

        for (int i = 0; i < bosses.Count; i++)
        {
            var boss = bosses[i];
            if (boss == null) continue; // defesa — não deveria acontecer com o jogo pausado, mas não trava a sequência se acontecer

            idle.Follow = boss;
            idle.Priority = revealCameraPriority + i + 1; // sempre maior que o passo anterior, pra garantir o blend pro alvo novo
            yield return new WaitForSecondsRealtime(bossRevealHoldDuration);

            (active, idle) = (idle, active);
        }

        // Os 2 vcams de reveal voltam pra Priority 0 — devolve o controle pro vcam do
        // jogador, que nunca teve o Follow alterado por aqui.
        revealCamA.Priority = 0;
        revealCamB.Priority = 0;
        yield return new WaitForSecondsRealtime(returnToPlayerHoldDuration);

        TimeManager.Instance?.Resume();
        revealInProgress = false;
        revealRoutine = null;
    }
}
