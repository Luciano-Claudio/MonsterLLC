// Loja permanente (2026-11-27) — rerolls são um ESTOQUE da run (começa com PermanentBonuses.StartRerolls,
// +1 por Reroll_Dice). Treasure Sense (só no baú) e Merchant's Whisper (só no vendedor) dão o 1º reroll
// daquela tela de graça, sem gastar o estoque.
public static class RerollRules
{
    public static bool FreeFor(OfferSource source, bool treasureSense, bool merchantsWhisper) =>
        (source == OfferSource.Chest && treasureSense) || (source == OfferSource.Vendor && merchantsWhisper);

    public static bool CanReroll(int stock, bool freeAvailable) => freeAvailable || stock > 0;

    // Gasta o grátis primeiro; depois o estoque. false = nenhum reroll disponível.
    public static bool Spend(ref int stock, ref bool freeAvailable)
    {
        if (freeAvailable) { freeAvailable = false; return true; }
        if (stock <= 0) return false;
        stock--;
        return true;
    }
}
