using UnityEngine;

// Orb de sangue — nasce no monstro atingido pela extração de sangue e viaja (homing, segue a
// posição ATUAL do Blood Mage, que pode se mover livremente durante a viagem) até ele, curando
// ao chegar via callback (Heal() é protected em HeroController, mesmo critério do
// MageTeleportProjectile). Independente de qualquer flag de ação do Blood Mage. Carrega o
// valor de cura (já calculado pelo BloodMage a partir do dano causado NAQUELE alvo) em vez de
// só avisar "chegou" — cada orb pode curar um valor diferente (alvos diferentes = reserva e
// dano diferentes no futuro).
public class BloodOrb : MonoBehaviour
{
    [SerializeField] private float arrivalDistance = 0.3f; // 🔢 ajustável — o quão perto de "chegar" já conta como chegada

    private Transform followTarget;
    private float speed;
    private float healAmount;
    private System.Action<float> onArrived;
    private bool arrived;

    public void Launch(Transform target, float orbSpeed, float orbHealAmount, System.Action<float> arrivedCallback)
    {
        followTarget = target;
        speed = orbSpeed;
        healAmount = orbHealAmount;
        onArrived = arrivedCallback;
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (arrived || followTarget == null) return;

        Vector3 toTarget = followTarget.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= arrivalDistance)
        {
            Arrive();
            return;
        }

        transform.position += toTarget.normalized * speed * Time.deltaTime;
    }

    private void Arrive()
    {
        if (arrived) return;
        arrived = true;
        onArrived?.Invoke(healAmount);
        Destroy(gameObject);
    }
}
