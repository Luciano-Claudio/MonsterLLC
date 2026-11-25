// Ataque primário com DURAÇÃO (Mage, D9.1 — decisão do usuário, 2026-11-19): 1 clique = 1
// ataque completo (start -> continuous em voltas -> end). No frame 7 de cada volta do
// attack_continuous do corpo, o fogo termina se o tempo desde que ele acendeu já cobriu a
// duração EXTRA. Extra 0 = termina na 1ª volta (= ataque base, igual ao de antes do rework).
// Contado em segundos, não em voltas: velocidade de animação não muda quanto tempo o fogo
// fica aceso (só encurta start/end do corpo).
// Catálogo de cartas — Lasting Flames: +10% do ciclo por nível (níveis 1 a 9); no nível 10 a chama é
// infinita enquanto o jogador segura o botão de ataque.
public static class PrimaryDuration
{
    public const int InfiniteLevel = 10;
    public const float ExtraPerLevel = 0.10f; // 🔢 fração do ciclo por nível

    public static bool ShouldEnd(float elapsedSinceFireStart, float extraDuration) =>
        elapsedSinceFireStart >= extraDuration;

    // Com duração infinita, só soltar o botão encerra; senão, o tempo de sempre (o botão não importa).
    public static bool ShouldEnd(float elapsedSinceFireStart, float extraDuration, bool attackHeld) =>
        float.IsPositiveInfinity(extraDuration) ? !attackHeld : elapsedSinceFireStart >= extraDuration;

    public static float ExtraSeconds(int level, float cycleSeconds)
    {
        if (level <= 0) return 0f;
        if (level >= InfiniteLevel) return float.PositiveInfinity;
        return level * ExtraPerLevel * cycleSeconds;
    }
}
