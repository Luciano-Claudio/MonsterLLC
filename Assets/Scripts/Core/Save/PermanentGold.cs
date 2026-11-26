using System.IO;
using UnityEngine;

// Gold permanente (2026-11-26) — moeda rara de qualquer monstro (RunSettings.goldDropChance), gasta
// fora da run (loja por herói, próxima etapa). Arquivo PRÓPRIO (profile.json): o save da run
// (save.json, SaveManager) é apagado quando o herói morre, o gold não pode ir junto.
// Salva a cada moeda — arquivo minúsculo, e um fechamento inesperado não perde o gold da run.
public static class PermanentGold
{
    [System.Serializable]
    private class Profile
    {
        public long gold;
    }

    private static string overridePath;
    private static Profile profile;

    private static string FilePath => overridePath ?? Path.Combine(Application.persistentDataPath, "profile.json");

    public static long Amount => Current.gold;

    private static Profile Current
    {
        get
        {
            if (profile != null) return profile;
            try
            {
                profile = File.Exists(FilePath) ? JsonUtility.FromJson<Profile>(File.ReadAllText(FilePath)) : null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PermanentGold] Não consegui ler {FilePath}: {e.Message}");
            }
            return profile ??= new Profile();
        }
    }

    public static void Add(long amount)
    {
        if (amount <= 0) return;
        Current.gold += amount;
        Save();
        GameEvents.PermanentGoldChanged(Current.gold);
    }

    // Sorteio da moeda: roll em [0,1) abaixo da chance.
    public static bool RollsDrop(float chance, float roll) => roll < chance;

    private static void Save()
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true)); }
        catch (System.Exception e) { Debug.LogWarning($"[PermanentGold] Não consegui salvar {FilePath}: {e.Message}"); }
    }

    // Testes: aponta para outro arquivo (null volta ao profile.json) e relê do disco.
    public static void UseFileForTests(string path)
    {
        overridePath = path;
        profile = null;
    }
}
