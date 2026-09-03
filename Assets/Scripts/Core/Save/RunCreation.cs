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
            weaponTier = "Basic"
        };
    }
}
