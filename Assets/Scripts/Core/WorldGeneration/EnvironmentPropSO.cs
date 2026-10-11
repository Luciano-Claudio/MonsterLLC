using UnityEngine;

// Prefab de decoração posicionado por regra (docs/new/Mudanca_Estrutural_Geracao_
// Procedural..., Seção 13: "EnvironmentPropSO — prefab, footprint, terrain tags, density,
// min distance, sorting pivot"). O spawn de environments NÃO é o spawn de monstros — é só
// decoração; nenhum Collider de gameplay é assumido aqui.
[CreateAssetMenu(fileName = "EnvironmentProp", menuName = "MonsterLLC/World Generation/Environment Prop")]
public class EnvironmentPropSO : ScriptableObject
{
    public string id;
    public GameObject prefab;

    [Tooltip("Espaçamento mínimo entre instâncias, em unidades de mundo — também define o passo da grade de candidatos.")]
    public float minSpacing = 4f;

    [Range(0f, 1f)]
    [Tooltip("Chance de cada ponto candidato da grade efetivamente receber uma instância.")]
    public float density = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Deslocamento determinístico aplicado a cada candidato dentro da própria célula da grade, pra não parecer um grid óbvio.")]
    public float jitter = 0.6f;
}
