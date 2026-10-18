public static class RunCreation
{
    public static RunState CreateNewRun(string mode, string hero, string map)
    {
        return new RunState
        {
            mode = mode,
            hero = hero,
            map = map,
            day = 1,
            gold = 0,
            weaponTier = "Basic",
            // Sorteada 1x por jogo novo — antes era um valor fixo no asset
            // WorldGenerationSettings, gerando o MESMO mundo em toda run.
            worldSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
        };
    }
}
