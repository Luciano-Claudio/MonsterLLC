using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

// Game Feel (ideia experimental do usuário, fácil de desligar/ajustar se não ficar bom no
// teste) — reage a GameEvents.OnDamageTaken com Cinemachine Impulse (shake) e, só quando o
// JOGADOR acerta um monstro, um "punch zoom" suave direto no Lens.OrthographicSize da câmera.
// Posição do impulso não importa aqui (GenerateImpulseWithForce, sem propagação por
// distância) — câmera já fica sempre centrada perto da ação num jogo top-down desse tamanho.
public class HitFeelController : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    public CinemachineImpulseSource impulseSource;

    [Header("Jogador acerta um monstro")]
    public float monsterHitShakeForce = 0.08f;
    public float zoomInAmount = 0.1f;
    public float zoomDuration = 0.15f;

    [Header("Herói leva dano (só shake, sem zoom)")]
    public float heroHitShakeForce = 0.25f;

    // Bug real reportado pelo usuário: 1 hit treme normal, mas acertar/levar DEZENAS ou
    // CENTENAS de hits no mesmo instante (AOE, enxame denso, Ground Crack tiquando em vários
    // inimigos ao mesmo tempo) disparava 1 GenerateImpulseWithForce POR HIT — o impulso do
    // Cinemachine soma/acumula, e a câmera saía tremendo tão forte que parecia bugada. Cada
    // categoria (jogador acerta monstro / herói leva dano) tem seu PRÓPRIO orçamento
    // independente: no máximo maxShakesPerWindow impulsos a cada shakeWindowSeconds — o resto
    // dos hits na mesma janela continua aplicando dano normal, só não empilha MAIS tremida/zoom
    // em cima da que já disparou. Os 2 campos ficam expostos de propósito (pedido do usuário:
    // "um cooldown pequeno") — ajustável ao vivo sem recompilar.
    [Header("Limite anti-spam de tremida/zoom")]
    public int maxShakesPerWindow = 3;
    public float shakeWindowSeconds = 0.15f;

    private float monsterHitWindowStart = float.NegativeInfinity;
    private int monsterHitShakesInWindow;
    private float heroHitWindowStart = float.NegativeInfinity;
    private int heroHitShakesInWindow;

    private Coroutine zoomRoutine;

    private void OnEnable() => GameEvents.OnDamageTaken += HandleDamageTaken;
    private void OnDisable() => GameEvents.OnDamageTaken -= HandleDamageTaken;

    private void HandleDamageTaken(Vector3 position, float amount, bool isPlayer, bool isDirectPlayerHit)
    {
        if (impulseSource == null) return;

        if (isPlayer)
        {
            if (!TryConsumeShakeBudget(ref heroHitWindowStart, ref heroHitShakesInWindow)) return;
            impulseSource.GenerateImpulseWithForce(heroHitShakeForce);
            return;
        }

        // Sem juice em dano repetido/passivo (DoT, pulse/grounded tick, Pet) — só no hit
        // direto do próprio jogador (decisão do usuário: ult explode = juice, dot dela = não;
        // primário do mage = juice, pet batendo = não).
        if (!isDirectPlayerHit) return;
        if (!TryConsumeShakeBudget(ref monsterHitWindowStart, ref monsterHitShakesInWindow)) return;

        impulseSource.GenerateImpulseWithForce(monsterHitShakeForce);

        if (cinemachineCamera == null) return;
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(PunchZoom());
    }

    // Janela deslizante simples via Time.time — nenhum Update()/Coroutine próprio precisa
    // rodar só pra isto: a cada chamada, se a janela anterior já expirou, reseta o contador;
    // senão só soma e compara contra o teto.
    private bool TryConsumeShakeBudget(ref float windowStart, ref int countInWindow)
    {
        if (Time.time - windowStart > shakeWindowSeconds)
        {
            windowStart = Time.time;
            countInWindow = 0;
        }

        if (countInWindow >= maxShakesPerWindow) return false;
        countInWindow++;
        return true;
    }

    private IEnumerator PunchZoom()
    {
        float baseSize = cinemachineCamera.Lens.OrthographicSize;
        float targetSize = baseSize - zoomInAmount;
        float half = zoomDuration * 0.5f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(baseSize, targetSize, t / half);
            yield return null;
        }

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(targetSize, baseSize, t / half);
            yield return null;
        }

        cinemachineCamera.Lens.OrthographicSize = baseSize;
        zoomRoutine = null;
    }
}
