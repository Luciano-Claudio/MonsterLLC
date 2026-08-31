using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(RunState state)
    {
        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] Saved to {SavePath}");
    }

    public static RunState Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save found.");
            return null;
        }
        return JsonUtility.FromJson<RunState>(File.ReadAllText(SavePath));
    }

    public static bool HasSave() => File.Exists(SavePath);
}
