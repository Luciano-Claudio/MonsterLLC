using System.Collections.Generic;
using UnityEngine;

// Limita quantos Melee podem estar perto do player (dentro do attackRadius de cada um) ao
// mesmo tempo — os que não conseguem vaga ficam flanqueando (ver MeleeEnemyController).
// Só pra Melee: Ranged já fica parado no próprio alcance, não lota o corpo a corpo.
//
// Escopado por Floor — mesmo padrão do AttackBudgetManager (Sprint 15). Um pool único pra
// Scene inteira teria o mesmo bug que aquele sistema teve antes do fix: um Melee que
// reserva uma vaga e depois tem o Floor dele dormindo (Floor Sleep) nunca mais roda
// Update(), nunca libera a vaga, e ela ficaria presa roubando capacidade de um Floor onde
// o player nem está mais. Com pool por Floor, a reserva de um Floor dormindo continua
// válida (o monstro genuinamente ainda ocupa aquele espaço quando o Floor reativar) e não
// interfere em nada no Floor onde o player está agora.
public class MeleeAttackSlotManager : MonoBehaviour
{
    public static MeleeAttackSlotManager Instance { get; private set; }

    [SerializeField] private int maxMeleeNearPlayer = 12;

    private readonly Dictionary<FloorDefinition, SlotPool> pools = new();

    private void Awake() => Instance = this;

    private SlotPool GetPool(FloorDefinition floor)
    {
        if (!pools.TryGetValue(floor, out var pool))
        {
            pool = new SlotPool(maxMeleeNearPlayer);
            pools[floor] = pool;
        }
        return pool;
    }

    public bool TryReserveSlot(FloorDefinition floor) => GetPool(floor).TryReserve();
    public void ReleaseSlot(FloorDefinition floor) => GetPool(floor).Release();
}
