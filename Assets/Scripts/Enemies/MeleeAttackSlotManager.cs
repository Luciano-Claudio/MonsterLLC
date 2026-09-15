using UnityEngine;

// Limita quantos Melee podem estar perto do player (dentro do attackRadius de cada um) ao
// mesmo tempo — os que não conseguem vaga ficam flanqueando (ver MeleeEnemyController).
// Só pra Melee: Ranged já fica parado no próprio alcance, não lota o corpo a corpo.
public class MeleeAttackSlotManager : MonoBehaviour
{
    public static MeleeAttackSlotManager Instance { get; private set; }

    [SerializeField] private int maxMeleeNearPlayer = 12;

    private SlotPool slots;

    private void Awake()
    {
        Instance = this;
        slots = new SlotPool(maxMeleeNearPlayer);
    }

    public bool TryReserveSlot() => slots.TryReserve();
    public void ReleaseSlot() => slots.Release();
}
