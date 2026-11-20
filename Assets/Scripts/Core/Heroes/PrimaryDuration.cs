// Ataque primário com DURAÇÃO (Mage, D9.1 — decisão do usuário, 2026-11-19): 1 clique = 1
// ataque completo (start -> continuous em voltas -> end). No frame 7 de cada volta do
// attack_continuous do corpo, o fogo termina se o tempo desde que ele acendeu já cobriu a
// duração EXTRA. Extra 0 = termina na 1ª volta (= ataque base, igual ao de antes do rework).
// Contado em segundos, não em voltas: velocidade de animação não muda quanto tempo o fogo
// fica aceso (só encurta start/end do corpo).
public static class PrimaryDuration
{
    public static bool ShouldEnd(float elapsedSinceFireStart, float extraDuration) =>
        elapsedSinceFireStart >= extraDuration;
}
