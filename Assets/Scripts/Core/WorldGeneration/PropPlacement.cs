using System.Collections.Generic;
using UnityEngine;

// Gera, de forma pura e determinística, as posições candidatas de 1 EnvironmentPropSO dentro
// de 1 chunk — grade + jitter determinístico + rejeição por espaçamento mínimo (docs/new/
// Mudanca_Estrutural_Geracao_Procedural..., Seção 13: densidade/chance/espaçamento; Seção 18:
// "objetos não devem ocupar... footprints já reservados"). Não instancia nada — quem chama
// decide o que fazer com as posições (Instantiate, teste, preview no Editor).
public static class PropPlacement
{
    public static List<Vector2> GenerateCandidatePositions(
        int chunkOriginX, int chunkOriginY, int chunkSize,
        float minSpacing, float density, float jitter, int worldSeed)
    {
        var positions = new List<Vector2>();
        if (minSpacing <= 0f) return positions;

        int step = Mathf.Max(1, Mathf.RoundToInt(minSpacing));

        for (int gx = 0; gx < chunkSize; gx += step)
        {
            for (int gy = 0; gy < chunkSize; gy += step)
            {
                int cellX = chunkOriginX + gx;
                int cellY = chunkOriginY + gy;

                float densityRoll = WorldHash.Hash2DFloat(cellX, cellY, channel: 500, seed: worldSeed);
                if (densityRoll > density) continue;

                float jitterX = (WorldHash.Hash2DFloat(cellX, cellY, channel: 501, seed: worldSeed) - 0.5f) * jitter * step;
                float jitterY = (WorldHash.Hash2DFloat(cellX, cellY, channel: 502, seed: worldSeed) - 0.5f) * jitter * step;
                var candidate = new Vector2(cellX + 0.5f + jitterX, cellY + 0.5f + jitterY);

                // Rejeita se ficar mais perto do que minSpacing de outro já aceito nesta
                // mesma passada — sem isso, jitter máximo de 2 pontos vizinhos da grade pode
                // colapsar os 2 props quase no mesmo lugar ("sem bloquear corredores").
                bool tooClose = false;
                foreach (var existing in positions)
                {
                    if (Vector2.Distance(existing, candidate) < minSpacing) { tooClose = true; break; }
                }
                if (!tooClose) positions.Add(candidate);
            }
        }

        return positions;
    }
}
