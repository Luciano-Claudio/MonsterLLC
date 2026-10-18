using UnityEngine;

// 1 monstro comum do mundo aberto — substitui a falta de qualquer SO de "bestiário" que
// existia até agora (monstro era só um prefab solto com EnemyStats no Inspector, sem tier
// nenhum em código). `tier` aqui é o Tier de MONSTRO (T1..T10, 10 posições — igual
// DaySpawnDefinition.dayWeights/nightWeights), não o grupo de 6 do terreno
// (WorldTierSelector.TierGroupCount) — um monstro de T6 por exemplo cai dentro do GRUPO de
// terreno Tier6-8, mas cada Tier de monstro continua competindo isolado na tabela de spawn
// (ver WorldTierSelector.ComputeTierPercentsNonAlloc).
[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "MonsterLLC/World Generation/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    [Range(1, 10)]
    public int tier = 1;

    public GameObject prefab;

    [Tooltip("Peso relativo DENTRO do próprio tier (\"Roll Monster Inside Tier\", doc de design Seção 16) — todos iguais (1) por padrão, sem precisar normalizar manualmente.")]
    public float weight = 1f;
}
