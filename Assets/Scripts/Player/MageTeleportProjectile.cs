using System;
using UnityEngine;

// Teleporte do Mage (GDD Seção 16/17.3) — o Mage desaparece no fim de "teleport_start", este
// projétil viaja sozinho pela sala (rotacionado de verdade, ângulo livre, mesma técnica do
// primário) marcando o trajeto, e só quando termina de viajar a distância combinada é que o
// Mage reaparece de fato (posição + "teleport_end") — não é um efeito visual solto, é quem
// decide QUANDO e ONDE o Mage reaparece.
// speed NÃO é serializado aqui de propósito — é valor de upgrade, decidido pelo Mage
// (controlador) e recebido em Launch(). Upgrades futuros só editam o Mage.cs.
public class MageTeleportProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private float distanceTraveled;
    private Action<Vector3> onArrived;

    public void Launch(Vector2 dir, float distance, Action<Vector3> arrivedCallback, float travelSpeed)
    {
        direction = dir.normalized;
        maxDistance = distance;
        onArrived = arrivedCallback;
        speed = travelSpeed;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — mesma técnica da
        // RangerArrow/EnemyProjectile/MageFireball.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        // position direto (espaço de mundo), não Translate — o Transform está rotacionado,
        // então os eixos locais giraram junto (mesmo motivo de sempre).
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;

        if (distanceTraveled >= maxDistance)
        {
            onArrived?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }
}
