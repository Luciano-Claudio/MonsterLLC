using UnityEngine;

// D9.4 — regras puras do spawn em cerco (docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md,
// 4.5, 4.6 e 4.8). Nada aqui aloca: tudo roda por spawn ou por frame.

// Anel de spawn: elipse com a proporção da tela, passando fora dos cantos, dividida em setores.
public static class SpawnRing
{
    // Setor 0 começa no ângulo 0 (eixo +X) e cresce no sentido anti-horário.
    public static int SectorOf(Vector2 offsetFromHero, int sectorCount)
    {
        float deg = Mathf.Atan2(offsetFromHero.y, offsetFromHero.x) * Mathf.Rad2Deg;
        if (deg < 0f) deg += 360f;
        int sector = Mathf.FloorToInt(deg / (360f / sectorCount));
        return Mathf.Clamp(sector, 0, sectorCount - 1);
    }

    public static Vector2 PointOnEllipse(float angleRad, float halfWidth, float halfHeight) =>
        new Vector2(Mathf.Cos(angleRad) * halfWidth, Mathf.Sin(angleRad) * halfHeight);

    public static bool IsOutsideView(Vector2 offsetFromHero, float viewHalfWidth, float viewHalfHeight) =>
        Mathf.Abs(offsetFromHero.x) > viewHalfWidth || Mathf.Abs(offsetFromHero.y) > viewHalfHeight;

    // O centro do setor fica a até arcDegrees/2 da direção de fuga.
    public static bool IsFrontSector(int sector, int sectorCount, Vector2 fleeDirection, float arcDegrees)
    {
        float width = 360f / sectorCount;
        float centerDeg = (sector + 0.5f) * width;
        float fleeDeg = Mathf.Atan2(fleeDirection.y, fleeDirection.x) * Mathf.Rad2Deg;
        return Mathf.Abs(Mathf.DeltaAngle(centerDeg, fleeDeg)) <= arcDegrees * 0.5f;
    }
}

// Ordem de tentativa dos setores: menos monstros primeiro (o peso do anti-kite conta como
// "monstros a menos"). Empate: vence quem vem primeiro a partir de tieRotation. Quem chama tenta
// na ordem até um setor dar ponto válido.
public static class SpawnSectorPicker
{
    public static void FillOrder(int[] counts, float[] bias, int sectorCount, int tieRotation, int[] orderOut)
    {
        int rotation = ((tieRotation % sectorCount) + sectorCount) % sectorCount;
        for (int i = 0; i < sectorCount; i++) orderOut[i] = (rotation + i) % sectorCount;

        // Insertion sort estável (no máximo ~12 itens) — preserva a rotação nos empates.
        for (int i = 1; i < sectorCount; i++)
        {
            int current = orderOut[i];
            float score = counts[current] - bias[current];
            int j = i - 1;
            while (j >= 0 && counts[orderOut[j]] - bias[orderOut[j]] > score)
            {
                orderOut[j + 1] = orderOut[j];
                j--;
            }
            orderOut[j + 1] = current;
        }
    }
}

// Anti-kite: direção média em que o herói anda (média móvel exponencial) e quantos dos últimos
// spawns foram pra frente dele (pro limite que impede o "todo monstro nasce na frente").
public class KiteTracker
{
    private readonly float windowSeconds;
    private readonly float minSpeed;
    private readonly bool[] recentFront;
    private int recentCount;
    private int recentHead;
    private int recentFrontCount;
    private Vector2 averageVelocity;

    public KiteTracker(float windowSeconds, float minSpeed, int shareWindowSpawns)
    {
        this.windowSeconds = windowSeconds;
        this.minSpeed = minSpeed;
        recentFront = new bool[Mathf.Max(1, shareWindowSpawns)];
    }

    public void AddVelocity(Vector2 velocity, float deltaTime)
    {
        float alpha = windowSeconds > 0f ? Mathf.Clamp01(deltaTime / windowSeconds) : 1f;
        averageVelocity = Vector2.Lerp(averageVelocity, velocity, alpha);
    }

    public bool TryGetFleeDirection(out Vector2 dir)
    {
        if (averageVelocity.magnitude < minSpeed) { dir = Vector2.zero; return false; }
        dir = averageVelocity.normalized;
        return true;
    }

    public void RecordSpawn(bool wasFront)
    {
        if (recentCount == recentFront.Length && recentFront[recentHead]) recentFrontCount--;
        recentFront[recentHead] = wasFront;
        if (wasFront) recentFrontCount++;
        recentHead = (recentHead + 1) % recentFront.Length;
        if (recentCount < recentFront.Length) recentCount++;
    }

    public float FrontShare => recentCount == 0 ? 0f : (float)recentFrontCount / recentCount;

    public void Reset()
    {
        averageVelocity = Vector2.zero;
        recentCount = recentHead = recentFrontCount = 0;
    }
}

// Empurrão do herói: o monstro à frente é deslocado pro lado em que já está (perpendicular ao
// caminho). Exatamente na frente: vai pra esquerda.
public static class CrowdPushRules
{
    public static Vector2 LateralPush(Vector2 moveDir, Vector2 heroToMonster)
    {
        Vector2 left = new Vector2(-moveDir.y, moveDir.x).normalized;
        float cross = moveDir.x * heroToMonster.y - moveDir.y * heroToMonster.x;
        return cross < 0f ? -left : left;
    }
}
