// Hash determinístico puro (sem UnityEngine.Random, sem Time, sem estado) — mesma entrada
// sempre produz a mesma saída, independente de quando ou em que ordem é chamada. Base de
// qualquer canal de aleatoriedade da geração procedural (docs/new/Mudanca_Estrutural_
// Geracao_Procedural..., Seção 20 "Determinismo, Seeds e Reprodutibilidade"): terreno,
// variantes, rios, props e estruturas usam canais independentes sem nunca depender da ordem
// de carregamento de chunk.
public static class WorldHash
{
    private const uint Noise1 = 0x68E31DA4;
    private const uint Noise2 = 0xB5297A4D;
    private const uint Noise3 = 0x1B56C4E9;

    // Hash 1D puro ("Squirrel3", Squirrel Eiserloh — algoritmo de domínio público, GDC 2017).
    // Base de todo o resto desta classe.
    public static uint Hash1D(int position, uint seed)
    {
        uint mangled = (uint)position;
        mangled *= Noise1;
        mangled += seed;
        mangled ^= mangled >> 8;
        mangled += Noise2;
        mangled ^= mangled << 8;
        mangled *= Noise3;
        mangled ^= mangled >> 8;
        return mangled;
    }

    // Combina (x, y, channel) num único hash — permite canais independentes de aleatoriedade
    // (terreno, props, rios...) pra mesma célula sem colidir entre si.
    public static uint Hash2D(int x, int y, int channel, int seed)
    {
        uint h = Hash1D(x, (uint)seed);
        h = Hash1D(y, h);
        h = Hash1D(channel, h);
        return h;
    }

    // Normaliza o hash pra [0, 1) — conveniência pra consumidores que querem um float de
    // densidade/probabilidade em vez do uint bruto.
    public static float Hash2DFloat(int x, int y, int channel, int seed)
    {
        return Hash2D(x, y, channel, seed) / (float)uint.MaxValue;
    }
}
