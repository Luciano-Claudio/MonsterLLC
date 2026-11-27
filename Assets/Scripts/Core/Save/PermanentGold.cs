using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Gold permanente (2026-11-26) — moeda rara de qualquer monstro (RunSettings.goldDropChance), gasta
// fora da run (loja por herói, PermanentShop). Arquivo PRÓPRIO (profile.json): o save da run
// (save.json, SaveManager) é apagado quando o herói morre, o gold não pode ir junto.
// Salva a cada moeda — arquivo minúsculo, e um fechamento inesperado não perde o gold da run.
// Loja permanente (2026-11-27): o mesmo arquivo guarda os níveis comprados de cada herói.
public static class PermanentGold
{
    [System.Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
        public bool disabled;
    }

    [System.Serializable]
    public class HeroUpgradeLevels
    {
        public string heroKey;
        public List<UpgradeLevel> upgrades = new();
    }

    [System.Serializable]
    private class Profile
    {
        public long gold;
        public List<HeroUpgradeLevels> heroes = new();
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
                // Revisão final: o arquivo estragado vira profile.json.corrupt ANTES de qualquer gravação
                // nova, senão a próxima moeda apagaria todo o gold e as melhorias de vez.
                Debug.LogWarning($"[PermanentGold] Não consegui ler {FilePath}: {e.Message}. Cópia em {FilePath}.corrupt");
                try { File.Copy(FilePath, FilePath + ".corrupt", true); } catch (System.Exception) { }
                profile = null;
            }
            profile ??= new Profile();
            profile.heroes ??= new List<HeroUpgradeLevels>();
            return profile;
        }
    }

    public static void Add(long amount)
    {
        if (amount <= 0) return;
        Current.gold += amount;
        Save();
        GameEvents.PermanentGoldChanged(Current.gold);
    }

    public static bool TrySpend(long amount)
    {
        if (amount < 0 || Current.gold < amount) return false;
        Current.gold -= amount;
        Save();
        GameEvents.PermanentGoldChanged(Current.gold);
        return true;
    }

    // Sorteio da moeda: roll em [0,1) abaixo da chance.
    public static bool RollsDrop(float chance, float roll) => roll < chance;

    // ---- níveis da loja (PermanentShop) ----
    public static UpgradeLevel FindLevel(string hero, string id)
    {
        if (string.IsNullOrEmpty(hero) || string.IsNullOrEmpty(id)) return null;
        foreach (var h in Current.heroes)
            if (h != null && h.heroKey == hero && h.upgrades != null)
                foreach (var u in h.upgrades)
                    if (u != null && u.id == id) return u;
        return null;
    }

    public static UpgradeLevel GetOrAddLevel(string hero, string id)
    {
        var found = FindLevel(hero, id);
        if (found != null) return found;
        HeroUpgradeLevels entry = null;
        foreach (var h in Current.heroes) if (h != null && h.heroKey == hero) { entry = h; break; }
        if (entry == null) { entry = new HeroUpgradeLevels { heroKey = hero }; Current.heroes.Add(entry); }
        entry.upgrades ??= new List<UpgradeLevel>();
        var level = new UpgradeLevel { id = id };
        entry.upgrades.Add(level);
        return level;
    }

    public static void SaveProfile() => Save();

    private static void Save()
    {
        // Gravação atômica: escreve no .tmp e só então troca pelo arquivo real (queda no meio não estraga).
        try
        {
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Current, true));
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);
        }
        catch (System.Exception e) { Debug.LogWarning($"[PermanentGold] Não consegui salvar {FilePath}: {e.Message}"); }
    }

    // Testes: aponta para outro arquivo (null volta ao profile.json) e relê do disco.
    public static void UseFileForTests(string path)
    {
        overridePath = path;
        profile = null;
    }
}
