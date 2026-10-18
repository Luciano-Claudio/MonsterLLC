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

    private Coroutine zoomRoutine;

    private void OnEnable() => GameEvents.OnDamageTaken += HandleDamageTaken;
    private void OnDisable() => GameEvents.OnDamageTaken -= HandleDamageTaken;

    private void HandleDamageTaken(Vector3 position, float amount, bool isPlayer, bool isDirectPlayerHit)
    {
        if (impulseSource == null) return;

        if (isPlayer)
        {
            impulseSource.GenerateImpulseWithForce(heroHitShakeForce);
            return;
        }

        // Sem juice em dano repetido/passivo (DoT, pulse/grounded tick, Pet) — só no hit
        // direto do próprio jogador (decisão do usuário: ult explode = juice, dot dela = não;
        // primário do mage = juice, pet batendo = não).
        if (!isDirectPlayerHit) return;

        impulseSource.GenerateImpulseWithForce(monsterHitShakeForce);

        if (cinemachineCamera == null) return;
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(PunchZoom());
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
