using UnityEngine;

// Etapa 4 — uma das 2 especializações de um herói, escolhida no começo da run (tela
// Specialization_Choose). Textos e ícone vão pra tela; as tags filtram as cartas; o herói lê o `id`
// em ApplySpecialization pra ligar/desligar comportamento.
[CreateAssetMenu(fileName = "Specialization", menuName = "MonsterLLC/Specialization")]
public class SpecializationDefinition : ScriptableObject
{
    public string id;
    public string heroId; // igual a GetType().Name do herói ("Barbarian", "Ranger", "Mage"...)
    public int slot; // 0 = botão da esquerda, 1 = da direita
    public string displayName;
    [TextArea] public string description;
    public Sprite icon; // o usuário coloca à mão
    public SpecTag tags;
}
