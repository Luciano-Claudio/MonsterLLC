using UnityEngine;

// D9.6a — tabela por tier editável (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 3.1).
// Fica em Resources/MonsterScalingSettings.asset. O jogo carrega no boot (SystemsBootstrap) e o
// botão "Aplicar nos prefabs" (Editor) usa a mesma tabela. Sem o asset, valem os padrões do
// código (MonsterScaling), que são os testados.
[CreateAssetMenu(fileName = "MonsterScalingSettings", menuName = "MonsterLLC/Monster Scaling Settings")]
public class MonsterScalingSettings : ScriptableObject
{
    [Tooltip("Vida do melee típico por tier (índice 0 = T1).")]
    public float[] tierHealth = { 6.9f, 10.35f, 15.53f, 23.29f, 34.93f, 52.4f, 78.6f, 117.89f, 176.84f, 265.26f };
    [Tooltip("Dano do melee típico por tier (índice 0 = T1).")]
    public float[] tierDamage = { 2.3f, 3.45f, 5.17f, 7.76f, 11.64f, 17.47f, 26.2f, 39.3f, 58.95f, 88.42f };
    [Tooltip("XP por kill de um monstro comum, por tier (índice 0 = T1).")]
    public int[] tierXp = { 1, 4, 15, 50, 160, 500, 1500, 4500, 13000, 38000 };

    [Tooltip("Gravado pelo botão Aplicar: a tabela de dano usada na última aplicação. Os campos \"por segundo\" (venenos, poças) escalam só pela diferença entre a tabela atual e esta — rodar de novo com a mesma tabela não muda nada. Vazio = nunca aplicado (base = T1).")]
    public float[] appliedTierDamage = new float[0];

    public static MonsterScalingSettings Load() => Resources.Load<MonsterScalingSettings>("MonsterScalingSettings");

    // Copia a tabela pro MonsterScaling (índice 0 do MonsterScaling é "sem uso").
    public void ApplyToRuntime()
    {
        MonsterScaling.SetTables(Prepend(tierHealth), Prepend(tierDamage), PrependInt(tierXp));
    }

    private static float[] Prepend(float[] src)
    {
        var dst = new float[src.Length + 1];
        System.Array.Copy(src, 0, dst, 1, src.Length);
        return dst;
    }

    private static int[] PrependInt(int[] src)
    {
        var dst = new int[src.Length + 1];
        System.Array.Copy(src, 0, dst, 1, src.Length);
        return dst;
    }
}
