using System.Collections.Generic;
using UnityEngine;

// Invocação de pets de herói (D9.1, 2026-11-19, decisão do usuário) — genérico (Mage agora,
// Blood Mage quando chegar a vez dele). "Tem pet?" (hasPet) -> "tem a quantidade petCount em
// jogo?" -> se não, o herói toca a animação de invocar 1x POR PET que falta, esperando ficar
// livre (nunca corta ataque/habilidade). Pet que já está em jogo nunca é destruído/reinvocado.
//
// Quando confere: (1) quando alguém pede (RequestCheck — início do dia), e (2) SOZINHO assim
// que hasPet ou a quantidade desejada mudam, a qualquer momento do dia — cartas/upgrades que
// dão pet (futuro) acontecem com o dia rolando; também cobre ligar pelo Inspector em Play Mode.
//
// Cada pet nasce num dos petSpawnPoints sorteado entre os que nenhum pet vivo está usando
// (PetSummonRules.PickSpawnIndex); esse mesmo ponto vira o "teleportPoint" do pet.
//
// Este componente não toca animação: o herói pergunta ShouldStartSummon(busy) todo frame,
// dispara o próprio SummonPetTrigger quando for true e chama SpawnOne() no Animation Event
// da invocação (o herói é quem sabe os multiplicadores de tamanho/velocidade/dano do pet).
public class PetSummoner : MonoBehaviour
{
    [SerializeField] private bool hasPet = false; // false até o sistema de variantes existir (decisão do usuário)
    [SerializeField, Min(0)] private int petCount = 1; // 🔢 quantos pets em jogo ao mesmo tempo
    [SerializeField] private GameObject petPrefab; // precisa ter PetController
    [SerializeField] private Transform[] petSpawnPoints = new Transform[0];

    private readonly List<PetController> pets = new List<PetController>();
    private readonly List<Transform> petPoints = new List<Transform>(); // ponto de cada pet (mesmo índice de pets)
    private readonly List<bool> occupiedBuffer = new List<bool>();
    private bool checkPending;
    private bool lastHasPet;
    private int lastWantedCount;

    // Pets extras além de petCount (ex.: perk "Segunda Fênix" do Mage, que sai no D9.6).
    public int ExtraPets { get; set; }
    public int WantedCount => petCount + ExtraPets;
    public int BasePetCount => petCount;

    public bool HasPet
    {
        get => hasPet;
        set => hasPet = value; // a mudança é notada sozinha no próximo frame (ver ShouldStartSummon)
    }

    public int PetCount
    {
        get => petCount;
        set => petCount = Mathf.Max(0, value);
    }

    public IReadOnlyList<PetController> Pets
    {
        get { PruneDestroyed(); return pets; }
    }

    public void RequestCheck() => checkPending = true;

    // Chamado pelo herói todo frame. true = começar UMA animação de invocar agora. A conferência
    // continua pendente até não faltar nenhum pet (a próxima invocação sai no fim desta, quando
    // o herói ficar livre de novo).
    public bool ShouldStartSummon(bool heroBusy)
    {
        if (hasPet != lastHasPet || WantedCount != lastWantedCount)
        {
            lastHasPet = hasPet;
            lastWantedCount = WantedCount;
            checkPending = true;
        }
        if (!checkPending) return false;

        PruneDestroyed();
        int missing = PetSummonRules.Missing(hasPet && petPrefab != null, WantedCount, pets.Count);
        var step = PetSummonRules.Next(checkPending, heroBusy, missing);
        if (step == PetSummonStep.Done) checkPending = false;
        return step == PetSummonStep.StartSummon;
    }

    // 1 pet por chamada (Animation Event da invocação). damageMultiplier já decidido pelo herói.
    public PetController SpawnOne(Transform owner, float sizeMultiplier, float actionSpeedMultiplier,
        float damageMultiplier, float cooldownReductionMultiplier, float chaseRadiusMultiplier)
    {
        if (petPrefab == null) return null;
        PruneDestroyed();
        Transform point = PickFreeSpawnPoint();
        Vector3 spawnPos = point != null ? point.position : owner.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity, HeroController.HerosContainer);
        var pet = obj.GetComponent<PetController>();
        if (pet == null) return null;
        pet.Initialize(owner, point, sizeMultiplier, actionSpeedMultiplier, damageMultiplier, cooldownReductionMultiplier, chaseRadiusMultiplier);
        pets.Add(pet);
        petPoints.Add(point);
        return pet;
    }

    private Transform PickFreeSpawnPoint()
    {
        occupiedBuffer.Clear();
        // Ponto vazio no array (slot sem Transform) conta como ocupado — nunca é sorteado.
        foreach (var candidate in petSpawnPoints) occupiedBuffer.Add(candidate == null || petPoints.Contains(candidate));
        int index = PetSummonRules.PickSpawnIndex(occupiedBuffer, Random.value);
        return index >= 0 ? petSpawnPoints[index] : null;
    }

    private void PruneDestroyed()
    {
        for (int i = pets.Count - 1; i >= 0; i--)
        {
            if (pets[i] != null) continue;
            pets.RemoveAt(i);
            petPoints.RemoveAt(i);
        }
    }
}
