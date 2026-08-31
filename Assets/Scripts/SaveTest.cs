using UnityEngine;

public class SaveTest : MonoBehaviour
{
    [ContextMenu("Test Save")]
    public void TestSave()
    {
        var state = new RunState { day = 5, gold = 1200 };
        SaveManager.Save(state);
    }

    [ContextMenu("Test Load")]
    public void TestLoad()
    {
        var loaded = SaveManager.Load();
        if (loaded != null)
            Debug.Log($"[SaveTest] day={loaded.day}, gold={loaded.gold}, hero={loaded.hero}");
    }
}
