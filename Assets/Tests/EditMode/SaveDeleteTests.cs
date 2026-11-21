using System.IO;
using NUnit.Framework;
using UnityEngine;

// D9.5 — a morte encerra a run: o save dela é apagado, pra o "Continue" não trazer uma run morta
// de volta. Os testes guardam e restauram o save real do jogo (mesmo arquivo).
public class SaveDeleteTests
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    private byte[] backup;

    [SetUp]
    public void SetUp() => backup = File.Exists(SavePath) ? File.ReadAllBytes(SavePath) : null;

    [TearDown]
    public void TearDown()
    {
        if (backup != null) File.WriteAllBytes(SavePath, backup);
        else if (File.Exists(SavePath)) File.Delete(SavePath);
    }

    [Test]
    public void Delete_RemovesSave()
    {
        SaveManager.Save(new RunState { day = 3, hero = "Barbarian" });
        SaveManager.Delete();
        Assert.IsFalse(SaveManager.HasSave());
    }

    [Test]
    public void Delete_WithoutSave_DoesNotThrow()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        Assert.DoesNotThrow(SaveManager.Delete);
        Assert.IsFalse(SaveManager.HasSave());
    }
}
