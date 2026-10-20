using UnityEngine;

// Orc Rider (Bestiário Andar 3) — mesma arquitetura do Mother Slime (Bestiário Andar 1,
// Bosses): ao morrer, nascem filhos em POSIÇÕES FIXAS (Transforms filhos deste GameObject,
// configurados no prefab — nunca aleatório). Diferença: aqui são 2 tipos diferentes (1 Warg +
// 1 Orc Blade), não 3 cópias do mesmo.
public class OrcRiderController : MeleeEnemyController
{
    [SerializeField] private GameObject wargPrefab;
    [SerializeField] private GameObject orcBladePrefab;
    [SerializeField] private Transform wargSpawnPoint;
    [SerializeField] private Transform orcBladeSpawnPoint;

    private bool childrenSpawned; // AnimationDieEndEvent() pode ser chamado 2x (evento real + timeout de segurança da base)

    public override void AnimationDieEndEvent()
    {
        if (!childrenSpawned)
        {
            childrenSpawned = true;
            SpawnChild(wargPrefab, wargSpawnPoint);
            SpawnChild(orcBladePrefab, orcBladeSpawnPoint);
        }
        base.AnimationDieEndEvent(); // dieHandled (privado, na base) já evita destruir/dropar loot 2x
    }

    private void SpawnChild(GameObject prefab, Transform point)
    {
        if (prefab == null || point == null) return;

        var childObj = Instantiate(prefab, point.position, Quaternion.identity);
        var childController = childObj.GetComponent<EnemyController>();
        if (childController == null) return;

        // Mesma decisão do usuário já aplicada ao Mother Slime — nasce já com o jogador
        // como alvo, sem fase de patrulha/idle esperando observationRadius detectar.
        childController.ForceImmediateAggro();
    }
}
