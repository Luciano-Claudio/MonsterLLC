using System;
using UnityEngine;

// Projétil do Ambuster (Bestiário Andar 9, boss) — mesma técnica do MageTeleportProjectile
// (lado do herói): o Ambuster desaparece no fim do attack_start, este objeto viaja sozinho
// até a posição que foi coletada do player naquele instante (congelada, não recalculada —
// ver AmbusterController), e só quando chega lá — ou acerta o player no meio do caminho —
// é que o Ambuster reaparece de verdade, exatamente na posição em que o projétil parou.
// Sem Animator/sprite por enquanto (pedido do usuário — a arte entra depois).
public class AmbusterProjectile : MonoBehaviour
{
    private Vector2 targetPosition;
    private float speed;
    private float damage;
    private float touchRadius;
    private Transform player;
    private Action<Vector3> onArrived;
    private bool arrived;

    public void Launch(Vector2 target, float travelSpeed, float projectileDamage, float playerTouchRadius, Transform playerTransform, Action<Vector3> arrivedCallback)
    {
        targetPosition = target;
        speed = travelSpeed;
        damage = projectileDamage;
        touchRadius = playerTouchRadius;
        player = playerTransform;
        onArrived = arrivedCallback;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || arrived) return;

        transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Acertou o player no meio do caminho — pára ali mesmo, não continua até o alvo
        // original (ver Bestiário: "se o jogador andar em direção ao projétil... o ambuster
        // não vai pra posição final que foi calculada, fica ali mesmo onde acertou").
        if (player != null && Vector2.Distance(transform.position, player.position) <= touchRadius)
        {
            var hero = player.GetComponent<HeroController>();
            if (hero != null) hero.TakeDamage(damage);
            Arrive();
            return;
        }

        if (Vector2.Distance(transform.position, targetPosition) <= 0.05f)
            Arrive();
    }

    private void Arrive()
    {
        if (arrived) return;
        arrived = true;
        onArrived?.Invoke(transform.position);
        Destroy(gameObject);
    }
}
