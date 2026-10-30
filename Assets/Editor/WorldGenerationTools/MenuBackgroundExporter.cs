using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Ferramenta de Editor (não entra no build) — gera um PNG estático usando o pipeline de
// geração REAL do jogo (ChunkTerrainGenerator/RuleTile/props), pra servir de base de
// background do menu principal. Pinta uma área isolada e distante da origem (nunca interfere
// com o mundo normal de uma sessão de Play em andamento), força forcedTierGroupOverride pro
// Tier4-5 — sem fronteira de tier/ruído de blend, bioma 100% puro — fotografa com uma Camera
// ortográfica alinhada ao PPU do projeto e limpa tudo (tiles + props) depois.
public static class MenuBackgroundExporter
{
    private const int Tier4_5GroupIndex = 2; // ChunkTerrainGenerator.tierBiomes: [0]=Tier1 [1]=Tier2-3 [2]=Tier4-5 ...
    private const int OutputWidth = 1920;
    private const int OutputHeight = 1080;
    private const float PixelsPerUnit = 8f; // mesmo PPU das sprites de terreno do projeto

    // Longe o bastante da origem (safeZoneRadius=12, chunkSize=64) pra nunca cair na safe zone
    // nem esbarrar em chunks já pintados por uma sessão de Play normal em andamento.
    private const int ExportChunkCenterX = 30;
    private const int ExportChunkCenterY = 30;
    private const int ExportChunksWide = 5; // 320 unidades — margem confortável sobre os 240 capturados
    private const int ExportChunksTall = 3; // 192 unidades — margem confortável sobre os 135 capturados

    [MenuItem("MonsterLLC/World Generation/Exportar Background Tier4-5 (PNG)")]
    public static void Export()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[MenuBackgroundExporter] Só funciona em Play Mode (precisa do Tilemap/pipeline de geração rodando de verdade).");
            return;
        }

        var generator = Object.FindFirstObjectByType<ChunkTerrainGenerator>();
        if (generator == null)
        {
            Debug.LogWarning("[MenuBackgroundExporter] Nenhum ChunkTerrainGenerator na cena.");
            return;
        }

        int chunkSize = generator.settings.chunkSize;
        int startX = ExportChunkCenterX - ExportChunksWide / 2;
        int startY = ExportChunkCenterY - ExportChunksTall / 2;

        var paintedCoords = new List<ChunkCoord>();
        generator.forcedTierGroupOverride = Tier4_5GroupIndex;

        try
        {
            for (int cx = startX; cx < startX + ExportChunksWide; cx++)
            {
                for (int cy = startY; cy < startY + ExportChunksTall; cy++)
                {
                    var coord = new ChunkCoord(cx, cy);
                    generator.PaintChunkForExport(coord);
                    paintedCoords.Add(coord);
                }
            }

            CaptureAndSave(startX, startY, chunkSize);
        }
        finally
        {
            foreach (var coord in paintedCoords) generator.ClearChunkForExport(coord);
            generator.forcedTierGroupOverride = -1;
        }
    }

    private static void CaptureAndSave(int startChunkX, int startChunkY, int chunkSize)
    {
        // Centro geométrico da área pintada, em unidades de mundo.
        float areaWorldX = (startChunkX + ExportChunksWide * 0.5f) * chunkSize;
        float areaWorldY = (startChunkY + ExportChunksTall * 0.5f) * chunkSize;

        float captureHeightUnits = OutputHeight / PixelsPerUnit;

        var cameraObj = new GameObject("MenuBackgroundExportCamera");
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = captureHeightUnits * 0.5f;
        cam.transform.position = new Vector3(areaWorldX, areaWorldY, -10f);
        cam.cullingMask = 1 << 0; // Default — onde Tilemap/props vivem, exclui UI/Player/etc.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.allowMSAA = false;
        cam.allowHDR = false;

        var rt = new RenderTexture(OutputWidth, OutputHeight, 24, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point
        };
        var tex = new Texture2D(OutputWidth, OutputHeight, TextureFormat.RGBA32, false);

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, OutputWidth, OutputHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string folder = Path.Combine(Application.dataPath, "Sprites/UI/MainMenu_Backgrounds");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "MenuBackground_Tier4-5.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());

            Debug.Log("[MenuBackgroundExporter] PNG salvo em Assets/Sprites/UI/MainMenu_Backgrounds/MenuBackground_Tier4-5.png");
            AssetDatabase.Refresh();
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cameraObj);
        }
    }
}
