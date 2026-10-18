using UnityEngine;

// Limita quantos Melee podem estar perto do player (dentro do attackRadius de cada um) ao
// mesmo tempo — os que não conseguem vaga ficam flanqueando (ver MeleeEnemyController).
// Só pra Melee: Ranged já fica parado no próprio alcance, não lota o corpo a corpo.
//
// [Limpeza sistema de Floor] Antes era escopado por Floor (Dictionary<FloorDefinition,
// SlotPool>) — justificativa original: um Melee que reservasse vaga e depois tivesse o Floor
// dele dormindo (Floor Sleep) nunca mais rodaria Update(), nunca liberaria a vaga, roubando
// capacidade de um Floor onde o player nem estava mais. Mundo Único não tem Floor — todo
// monstro comum sempre simula em full (ver EnemyController.Update()), então essa corrida nem
// existe mais. Já era, na prática, 1 pool global só (ownerFloor sempre null caía no
// globalPool) — agora é isso mesmo, declarado, sem o Dictionary morto por trás.
public class MeleeAttackSlotManager : MonoBehaviour
{
    public static MeleeAttackSlotManager Instance { get; private set; }

    [SerializeField] private int maxMeleeNearPlayer = 8;

    // Raio do anel de flanco — geral pra todo Melee, não mais por monstro (EnemyStats). Um
    // valor central é mais fácil de balancear junto com maxMeleeNearPlayer (os dois definem
    // a mesma "lotação" perto do player) do que 14 campos espalhados por prefab.
    [SerializeField] private float flankRadius = 7f; // 🔢 ajustável
    public float FlankRadius => flankRadius;

    private SlotPool pool;

    private void Awake() => Instance = this;

    private SlotPool Pool => pool ??= new SlotPool(maxMeleeNearPlayer);

    public bool TryReserveSlot() => Pool.TryReserve();
    public void ReleaseSlot() => Pool.Release();
}
