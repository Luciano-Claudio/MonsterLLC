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
        // elas sempre podem aparecer na safe zone. A regra de "só em bloco 2x2" (pra quem tiver
        // BiomeTerrainEntry.requiresFilledQuad = true, ex.: as 7 variantes de grama) já é
        // aplicada dentro do próprio FindMatch — passamos patchTerrains como
        // `purityReferenceLevel` pra isso: um vizinho só conta pro quadrado se NINGUÉM em
        // patchTerrains reivindicar aquela célula (senão o ChunkTerrainGenerator nunca chamaria
        // ChooseBaseVariant ali de verdade, e o "quadrado" seria só nos dados, não na tela).
        var matched = FindMatch(biome.baseVariants, cellX, cellY, seedOffsetX, seedOffsetY, safeZoneRadius: 0f,
            purityReferenceLevel: biome.patchTerrains);
        return matched?.terrain;
    }

    // Os 4 quadrados 2×2 possíveis que contêm (cellX,cellY) em cada um dos 4 cantos possíveis.
    private static readonly (int dx, int dy)[][] Quads =
    {
        new[] { (1, 0), (0, 1), (1, 1) },     // (cellX,cellY) = canto inferior-esquerdo
        new[] { (-1, 0), (0, 1), (-1, 1) },   // canto inferior-direito
        new[] { (1, 0), (0, -1), (1, -1) },   // canto superior-esquerdo
        new[] { (-1, 0), (0, -1), (-1, -1) }, // canto superior-direito
    };

    // Generalizado pra qualquer BiomeTerrainEntry.requiresFilledQuad = true, em qualquer lista
    // (patchTerrains, nestedPatches de qualquer profundidade, ou baseVariants) — não só
    // variantes de grama. Nasceu da pendência da grama (ver histórico abaixo), mas serve pra
    // qualquer terreno futuro cujo RuleTile não tenha arte pra célula única/linha fina (ex.:
    // tipos de água de tiers futuros sem conexão pra formas finas, pedido do usuário).
    //
    // Histórico: a 1ª e 2ª tentativas de corrigir a grama (exigir só "1 vizinho reto igual")
    // pareciam corretas no raciocínio E nos testes sintéticos, mas o usuário continuou vendo o
    // bug depois de recompilar/reiniciar o Editor. Log real (coordenadas + seed de verdade, não
    // offset 0,0) provou que a GERAÇÃO já estava 100% correta — zero células isoladas nos
    // dados, o menor bloco gerado tinha 2 células. O problema real: os RuleTiles das 7
    // variantes de grama só têm 15 das ~47 posições de vizinhança configuradas (confirmado
    // contando `m_Sprites:` nos `.asset`), faltando justamente os casos de bloco fino (ex.: um
    // par de 2 células lado a lado) — sem regra configurada pra essa forma, o RuleTile cai pro
    // sprite padrão, parecendo uma "ilha" mesmo com um vizinho de verdade do lado.
    //
    // Pública pra ser testável isoladamente. Confirma que (cellX,cellY) faz parte de pelo menos
    // 1 quadrado 2×2 totalmente preenchido pela MESMA entrada, dentro do mesmo "level" (lista)
    // que ela pertence — cada uma das outras 3 células do quadrado precisa ser a que realmente
    // VENCE ali (não basta bater pelo ruído: se outra entrada antes dela na lista também bate
    // ali, é essa outra que ganha). Os vizinhos são resolvidos por `RawMatch` (sem reaplicar a
    // própria regra de quadrado neles, de propósito — evita recursão sem necessidade: se as 4
    // células de um quadrado realmente formado batem todas na mesma entrada, cada uma delas já
    // teria um quadrado válido de qualquer jeito, então reverificar de novo é redundante).
    //
    // `purityReferenceLevel` é opcional — só existe pro caso de `level` ser uma lista que
    // depende de OUTRA lista pra decidir se a célula realmente vai ser pintada (caso de
    // `baseVariants`, que só é consultada quando `patchTerrains` NÃO reivindica a célula). Pra
    // listas "normais" da árvore de `patchTerrains`/`nestedPatches`, que são auto-suficientes,
    // deixe `null` (padrão) — não existe nenhum sistema externo que possa "roubar" a célula.
    public static bool HasFilledQuad(
        BiomeTerrainEntry entry, List<BiomeTerrainEntry> level, int cellX, int cellY, float seedOffsetX, float seedOffsetY,
        float safeZoneRadius = 0f, List<BiomeTerrainEntry> purityReferenceLevel = null)
    {
        foreach (var quad in Quads)
        {
            bool quadFilled = true;

            foreach (var (dx, dy) in quad)
            {
                int nx = cellX + dx;
                int ny = cellY + dy;

                if (purityReferenceLevel != null
                    && RawMatch(purityReferenceLevel, nx, ny, seedOffsetX, seedOffsetY, safeZoneRadius) != null)
                {
                    quadFilled = false; // vizinho reivindicado por outro sistema — não vai nascer ali de verdade
                    break;
                }

                if (RawMatch(level, nx, ny, seedOffsetX, seedOffsetY, safeZoneRadius) != entry)
                {
                    quadFilled = false;
                    break;
                }
            }

            if (quadFilled) return true;
        }

        return false;
    }

    // `safeZoneRadius > 0` pula qualquer entrada marcada `blockedInSafeZone` (água, lava...)
    // cuja célula caia dentro do raio a partir da origem do mundo (0,0) — é onde o herói nasce
    // (MainMenuUI.SpawnHero usa Vector3.zero) e onde NPCs de posição fixa vão ficar. Sem isso,
    // o herói podia nascer em cima de água (perderia a run inteira, sem colisor ainda) ou um
    // NPC fixo podia cair dentro de um lago gerado por acaso.
    //
    // Entradas com `requiresFilledQuad = true` que baterem pelo ruído mas não formarem um
    // quadrado 2×2 real (`HasFilledQuad`) são tratadas como "não bateu" — passa pra próxima da
    // lista, exatamente como se o ruído não tivesse batido ali. `purityReferenceLevel` só é
    // usado nesse caso, repassado direto pra `HasFilledQuad` (ver comentário lá).
    private static BiomeTerrainEntry FindMatch(
        List<BiomeTerrainEntry> level, int cellX, int cellY, float seedOffsetX, float seedOffsetY, float safeZoneRadius,
        List<BiomeTerrainEntry> purityReferenceLevel = null)
    {
        bool inSafeZone = safeZoneRadius > 0f
            && (float)cellX * cellX + (float)cellY * cellY <= safeZoneRadius * safeZoneRadius;

        foreach (var entry in level)
        {
            if (entry == null || entry.terrain == null) continue;
            if (inSafeZone && entry.blockedInSafeZone) continue;

            bool claimed = TerrainMask.ShouldPaintOverlay(
                cellX, cellY, seedOffsetX, seedOffsetY, entry.noiseScale, entry.coverageTarget);
            if (!claimed) continue;

            if (entry.requiresFilledQuad
                && !HasFilledQuad(entry, level, cellX, cellY, seedOffsetX, seedOffsetY, safeZoneRadius, purityReferenceLevel))
            {
                continue;
            }

            return entry;
        }

        return null;
    }

    // Versão "crua" de FindMatch — mesma coisa, mas sem checar requiresFilledQuad. Existe só
    // pra HasFilledQuad resolver os vizinhos do quadrado sem recursar nela mesma (ver
    // comentário de HasFilledQuad acima).
    private static BiomeTerrainEntry RawMatch(
        List<BiomeTerrainEntry> level, int cellX, int cellY, float seedOffsetX, float seedOffsetY, float safeZoneRadius)
    {
        bool inSafeZone = safeZoneRadius > 0f
            && (float)cellX * cellX + (float)cellY * cellY <= safeZoneRadius * safeZoneRadius;

        foreach (var entry in level)
        {
            if (entry == null || entry.terrain == null) continue;
            if (inSafeZone && entry.blockedInSafeZone) continue;

            if (TerrainMask.ShouldPaintOverlay(cellX, cellY, seedOffsetX, seedOffsetY, entry.noiseScale, entry.coverageTarget))
            {
                return entry;
            }
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
