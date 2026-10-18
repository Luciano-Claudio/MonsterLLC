using UnityEngine;

// 1 boss do mundo aberto — paralelo a EnemyDefinitionSO, mas sem `weight`: decisão do
// usuário pro Sprint P7, quando o Tier sorteado tem mais de 1 boss possível, o sorteio entre
// os ainda não usados hoje (UsedBossIdsToday) é uniforme (todos a mesma chance), sem peso
// configurável por enquanto.
[CreateAssetMenu(fileName = "BossDefinition", menuName = "MonsterLLC/World Generation/Boss Definition")]
public class BossDefinitionSO : ScriptableObject
{
    [Range(1, 10)]
    public int tier = 1;

    public GameObject prefab;
}
