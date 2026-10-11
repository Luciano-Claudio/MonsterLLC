using System.Collections.Generic;

// Decide, de forma pura e determinística, a PILHA de terrenos de 1 célula global — 1 por
// profundidade de aninhamento (índice 0 = baseTerrain/grid 0, índice 1 = patch de grid 1 se
// algum bateu, índice 2 = patch aninhado dentro dele se algum bateu, e assim por diante).
// Não depende de Tilemap/Scene — só de TerrainMask (também puro). Em cada profundidade, o
// primeiro BiomeTerrainEntry cuja máscara bater vence; se nenhum bater, a pilha para ali
// (não desce mais fundo).
public static class TerrainSelector
{
    public static List<TerrainDefinitionSO> ChooseTerrainStack(
        int cellX, int cellY, float seedOffsetX, float seedOffsetY, BiomeProfileSO biome, float safeZoneRadius = 0f)
    {
        var stack = new List<TerrainDefinitionSO> { biome.baseTerrain };

        var currentLevel = biome.patchTerrains;
        while (currentLevel != null)
        {
            var matched = FindMatch(currentLevel, cellX, cellY, seedOffsetX, seedOffsetY, safeZoneRadius);
            if (matched == null) break;

            stack.Add(matched.terrain);

            // Margem de 1 célula obrigatória (confirmado pelo usuário): um filho aninhado só
            // pode nascer onde os 8 vizinhos TAMBÉM pertencem ao pai que acabou de ser
            // adicionado — nunca bem na borda externa dele. Sem isso, o filho encostaria
            // direto no avô (ex.: Stone dentro de Dirt tocando a Grama por baixo do Dirt).
            if (!IsInteriorToMask(matched, cellX, cellY, seedOffsetX, seedOffsetY)) break;

            currentLevel = matched.nestedPatches;
        }

        return stack;
    }

    // Mesma lógica de ChooseTerrainStack, mas sem alocar List nenhuma — escreve direto num
    // buffer já existente (reaproveitado célula a célula pelo chamador). Existe só porque
    // ChunkTerrainGenerator chama isso 1x por célula (até 4096 vezes por chunk); a versão com
    // List estava gerando lixo suficiente pra causar engasgos perceptíveis de GC ao streamar
    // chunk novo (confirmado em teste do usuário). `buffer` precisa ter pelo menos tantas
    // posições quanto `layerTilemaps.Count` — profundidades além disso nunca seriam pintadas
    // mesmo assim, então cortar ali é seguro. Devolve a profundidade real preenchida.
    public static int ChooseTerrainStackNonAlloc(
        int cellX, int cellY, float seedOffsetX, float seedOffsetY, BiomeProfileSO biome, TerrainDefinitionSO[] buffer,
        float safeZoneRadius = 0f)
    {
        buffer[0] = biome.baseTerrain;
        int depth = 1;

        var currentLevel = biome.patchTerrains;
        while (currentLevel != null && depth < buffer.Length)
        {
            var matched = FindMatch(currentLevel, cellX, cellY, seedOffsetX, seedOffsetY, safeZoneRadius);
            if (matched == null) break;

            buffer[depth] = matched.terrain;
            depth++;

            if (!IsInteriorToMask(matched, cellX, cellY, seedOffsetX, seedOffsetY)) break;

            currentLevel = matched.nestedPatches;
        }

        return depth;
    }

    // Variantes do terreno-base (ex.: HighGrass/ThickGrass) — sistema separado da pilha de
    // patches aninhados, só pra decorar célula "pura" (onde nenhum patchTerrain bateu). Quem
    // chama decide quando usar isso (ver ChunkTerrainGenerator — só quando a pilha tem
    // tamanho 1, i.e., só o baseTerrain, sem nenhum Dirt/Stone/Água por cima).
    public static TerrainDefinitionSO ChooseBaseVariant(
        int cellX, int cellY, float seedOffsetX, float seedOffsetY, BiomeProfileSO biome)
    {
        // Variantes de grama nunca são perigosas — safeZoneRadius 0 (desligado) de propósito,
        // elas sempre podem aparecer na safe zone.
        var matched = FindMatch(biome.baseVariants, cellX, cellY, seedOffsetX, seedOffsetY, safeZoneRadius: 0f);
        if (matched == null) return null;

        // Diferente do resto do terreno (que aceita qualquer disposição), variante de grama só
        // pode nascer formando bloco com pelo menos 1 vizinho reto (cima/baixo/esquerda/
        // direita) pintado com A MESMA variante — nunca 1 célula isolada, nem uma "ponta"
        // encostando só na diagonal, nem "encostando" numa variante DIFERENTE.
        //
        // IMPORTANTE (verificado direto no código-fonte do pacote, RuleTile.cs: `RuleMatch`
        // pro valor `This` faz `other == this` — comparação de IDENTIDADE do asset, não "tem
        // algum tile ali"). Ou seja, o RuleTile do HighGrass só enxerga outro HighGrass do lado
        // como vizinho "This" — ThickGrass do lado é "NotThis" pra ele, mesmo os dois estando
        // pintados na mesma Tilemap. A 1ª versão desta regra ("qualquer variante no vizinho já
        // conta") estava errada por causa disso — corrigido com fotos reais do bug do usuário
        // mostrando que o problema persistia mesmo depois do primeiro fix.
        if (!HasMatchingOrthogonalNeighbor(matched, cellX, cellY, seedOffsetX, seedOffsetY, biome)) return null;

        return matched.terrain;
    }

    private static readonly (int dx, int dy)[] OrthogonalOffsets = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    // Pública pra ser testável isoladamente. Confirma que pelo menos 1 dos 4 vizinhos RETOS vai
    // nascer com a MESMA entrada — célula pura (sem Dirt/Pedra/Água por cima) E essa entrada
    // específica é a que realmente VENCE ali (não basta bater pelo ruído dela: se outra entrada
    // antes dela na lista também bate naquela célula, é ESSA outra que ganha a pintura — então
    // comparamos o resultado de FindMatch inteiro, não só o teste de ruído isolado).
    public static bool HasMatchingOrthogonalNeighbor(
        BiomeTerrainEntry entry, int cellX, int cellY, float seedOffsetX, float seedOffsetY, BiomeProfileSO biome)
    {
        foreach (var (dx, dy) in OrthogonalOffsets)
        {
            int nx = cellX + dx;
            int ny = cellY + dy;

            bool neighborIsPure = FindMatch(biome.patchTerrains, nx, ny, seedOffsetX, seedOffsetY, safeZoneRadius: 0f) == null;
            if (!neighborIsPure) continue; // Dirt/Pedra/Água por cima — nenhuma variante nasce ali mesmo

            var neighborMatch = FindMatch(biome.baseVariants, nx, ny, seedOffsetX, seedOffsetY, safeZoneRadius: 0f);
            if (neighborMatch == entry) return true;
        }

        return false;
    }

    // `safeZoneRadius > 0` pula qualquer entrada marcada `blockedInSafeZone` (água, lava...)
    // cuja célula caia dentro do raio a partir da origem do mundo (0,0) — é onde o herói nasce
    // (MainMenuUI.SpawnHero usa Vector3.zero) e onde NPCs de posição fixa vão ficar. Sem isso,
    // o herói podia nascer em cima de água (perderia a run inteira, sem colisor ainda) ou um
    // NPC fixo podia cair dentro de um lago gerado por acaso.
    private static BiomeTerrainEntry FindMatch(
        List<BiomeTerrainEntry> level, int cellX, int cellY, float seedOffsetX, float seedOffsetY, float safeZoneRadius)
    {
        bool inSafeZone = safeZoneRadius > 0f
            && (float)cellX * cellX + (float)cellY * cellY <= safeZoneRadius * safeZoneRadius;

        foreach (var entry in level)
        {
            if (entry == null || entry.terrain == null) continue;
            if (inSafeZone && entry.blockedInSafeZone) continue;

            bool claimed = TerrainMask.ShouldPaintOverlay(
                cellX, cellY, seedOffsetX, seedOffsetY, entry.noiseScale, entry.coverageTarget);

            if (claimed) return entry;
        }

        return null;
    }

    // Pública pra ser testável isoladamente, sem precisar reproduzir uma borda real de ruído
    // num teste (`TerrainMaskTests` já cobre o comportamento do ruído em si).
    public static bool IsInteriorToMask(
        BiomeTerrainEntry entry, int cellX, int cellY, float seedOffsetX, float seedOffsetY)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                bool neighborClaims = TerrainMask.ShouldPaintOverlay(
                    cellX + dx, cellY + dy, seedOffsetX, seedOffsetY, entry.noiseScale, entry.coverageTarget);

                if (!neighborClaims) return false;
            }
        }

        return true;
    }
}
