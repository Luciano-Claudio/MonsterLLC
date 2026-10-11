using NUnit.Framework;

public class TerrainMaskTests
{
    [Test]
    public void ShouldPaintOverlay_SameInputs_AlwaysReturnsSameResult()
    {
        bool first = TerrainMask.ShouldPaintOverlay(10, 20, 123.4f, 56.7f, 0.08f, 0.12f);
        bool second = TerrainMask.ShouldPaintOverlay(10, 20, 123.4f, 56.7f, 0.08f, 0.12f);

        Assert.AreEqual(first, second);
    }

    [Test]
    public void ShouldPaintOverlay_ZeroCoverage_NeverPaints()
    {
        for (int x = 0; x < 20; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                Assert.IsFalse(TerrainMask.ShouldPaintOverlay(x, y, 0f, 0f, 0.08f, 0f));
            }
        }
    }

    [Test]
    public void ShouldPaintOverlay_FullCoverage_AlwaysPaints()
    {
        for (int x = 0; x < 20; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                Assert.IsTrue(TerrainMask.ShouldPaintOverlay(x, y, 0f, 0f, 0.08f, 1f));
            }
        }
    }

    [Test]
    public void ShouldPaintOverlay_FormsContiguousPatches_NotSaltAndPepper()
    {
        // Verifica que o mask não é "sal e pimenta": em algum ponto, uma célula pintada tem
        // um vizinho direto também pintado (ruído coerente, não sorteio independente por
        // célula) — docs/new/Mudanca_Estrutural_Geracao_Procedural..., Seção 8/9.
        bool foundAdjacentPair = false;
        for (int x = 0; x < 40 && !foundAdjacentPair; x++)
        {
            for (int y = 0; y < 40 && !foundAdjacentPair; y++)
            {
                bool a = TerrainMask.ShouldPaintOverlay(x, y, 0f, 0f, 0.08f, 0.3f);
                bool b = TerrainMask.ShouldPaintOverlay(x + 1, y, 0f, 0f, 0.08f, 0.3f);
                if (a && b) foundAdjacentPair = true;
            }
        }
        Assert.IsTrue(foundAdjacentPair);
    }
}
