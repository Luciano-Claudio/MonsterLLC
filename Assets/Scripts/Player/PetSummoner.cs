using System.Collections.Generic;
using UnityEngine;

// Invocação de pets de herói (D9.1, 2026-11-19, decisão do usuário) — genérico (Mage agora,
// Blood Mage quando chegar a vez dele). "Tem pet?" (hasPet) -> "tem a quantidade petCount em
// jogo?" -> se não, o herói toca a animação de invocar 1x POR PET que falta, esperando ficar
// livre (nunca corta ataque/habilidade). Só confere quando alguém pede (RequestCheck): início
// do dia e, no futuro, a escolha da variante do herói (ex.: Mage "Familiar"). Pet que já está
// em jogo nunca é destruído/reinvocado.
//
// Este componente não toca animação: o herói pergunta ShouldStartSummon(busy) todo frame,
// dispara o próprio SummonPetTrigger quando for true e chama SpawnOne() no Animation Event
// da invocação (o herói é quem sabe os multiplicadores de tamanho/velocidade/dano do pet).
public class PetSummoner : MonoBehaviour
{
    [SerializeField] private bool hasPet = false; // false até o sistema de variantes existir (decisão do usuário)
    [SerializeField, Min(0)] private int petCount = 1; // 🔢 quantos pets em jogo ao mesmo tempo
    [SerializeField] private GameObject petPrefab; // precisa ter PetController
    [SerializeField] private Transform petSpawnPoint;

    private readonly List<PetController> pets = new List<PetController>();
    private bool checkPending;

    // Pets extras além de petCount (ex.: perk "Segunda Fênix" do Mage, que sai no D9.6).
    public int ExtraPets { get; set; }
    public int WantedCount => petCount + ExtraPets;
    public int BasePetCount => petCount;

    public bool HasPet
    {
        get => hasPet;
        set => hasPet = value; // escolha da variante (futuro): setar e chamar RequestCheck()
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
        Vector3 spawnPos = petSpawnPoint != null ? petSpawnPoint.position : owner.position;
        var obj = Instantiate(petPrefab, spawnPos, Quaternion.identity, HeroController.HerosContainer);
        var pet = obj.GetComponent<PetController>();
        if (pet == null) return null;
        pet.Initialize(owner, petSpawnPoint, sizeMultiplier, actionSpeedMultiplier, damageMultiplier, cooldownReductionMultiplier, chaseRadiusMultiplier);
        pets.Add(pet);
        return pet;
    }

    private void PruneDestroyed() => pets.RemoveAll(p => p == null);
}
