// Coloque este arquivo em Assets/Editor/RuleTileClonerWindow.cs
// Requer os pacotes 2D Tilemap Extras (RuleTile) e 2D Sprite (Sprite Editor).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class RuleTileClonerWindow : EditorWindow
{
    private RuleTile ruleTileModelo;
    private Texture2D pngNovo;

    [MenuItem("Tools/RuleTile Cloner")]
    public static void Abrir()
    {
        GetWindow<RuleTileClonerWindow>("RuleTile Cloner");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Clonar RuleTile com outro PNG", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        ruleTileModelo = (RuleTile)EditorGUILayout.ObjectField(
            "RuleTile modelo", ruleTileModelo, typeof(RuleTile), false);

        pngNovo = (Texture2D)EditorGUILayout.ObjectField(
            "PNG novo", pngNovo, typeof(Texture2D), false);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(ruleTileModelo == null || pngNovo == null))
        {
            if (GUILayout.Button("Criar novo RuleTile", GUILayout.Height(34)))
                Criar();
        }

        EditorGUILayout.HelpBox(
            "O novo PNG deve ter os tiles nas mesmas posicoes do original. " +
            "Se ainda nao estiver recortado, a ferramenta copiara automaticamente " +
            "os recortes e pivots do PNG usado pelo RuleTile modelo.",
            MessageType.Info);
    }

    private void Criar()
    {
        string caminhoDestino = EditorUtility.SaveFilePanelInProject(
            "Salvar novo RuleTile",
            pngNovo.name + "_RuleTile",
            "asset",
            "Escolha a pasta e o nome do novo RuleTile.");

        if (string.IsNullOrEmpty(caminhoDestino))
            return;

        RuleTile copia = null;

        try
        {
            // Coleta cada Sprite distinto utilizado pelo RuleTile modelo.
            var spritesOriginaisUsados = new HashSet<Sprite>();

            if (ruleTileModelo.m_DefaultSprite != null)
                spritesOriginaisUsados.Add(ruleTileModelo.m_DefaultSprite);

            foreach (var regra in ruleTileModelo.m_TilingRules)
            {
                if (regra == null || regra.m_Sprites == null)
                    continue;

                foreach (Sprite sprite in regra.m_Sprites)
                {
                    if (sprite != null)
                        spritesOriginaisUsados.Add(sprite);
                }
            }

            if (spritesOriginaisUsados.Count == 0)
                throw new InvalidOperationException("O RuleTile modelo nao possui sprites para substituir.");

            // O modelo deve usar sprites provenientes de uma mesma spritesheet.
            var caminhosOriginais = new HashSet<string>(
                spritesOriginaisUsados.Select(AssetDatabase.GetAssetPath));

            if (caminhosOriginais.Count != 1)
                throw new InvalidOperationException(
                    "O RuleTile modelo usa sprites de mais de um arquivo. " +
                    "Esta ferramenta trabalha com um PNG original por RuleTile.");

            string pngOriginalPath = caminhosOriginais.First();
            string pngNovoPath = AssetDatabase.GetAssetPath(pngNovo);

            if (pngOriginalPath == pngNovoPath)
                throw new InvalidOperationException("Selecione um PNG diferente do original.");

            var spritesOriginais = CarregarSprites(pngOriginalPath);
            PrepararPngNovo(pngOriginalPath, pngNovoPath, spritesOriginais.Length);
            var spritesNovos = CarregarSprites(pngNovoPath);

            if (spritesNovos.Length == 0)
                throw new InvalidOperationException("O PNG novo nao gerou nenhum Sprite.");

            // Primeiro confere TODOS os pares. Nao salva um RuleTile incompleto.
            var substituicoes = new Dictionary<Sprite, Sprite>();
            var spritesDestinoJaUsados = new HashSet<Sprite>();

            foreach (Sprite spriteOriginal in spritesOriginaisUsados)
            {
                Sprite novo = EncontrarCorrespondente(
                    spriteOriginal, spritesOriginais.Length, spritesNovos);

                if (novo == null)
                    throw new InvalidOperationException(
                        "Nao encontrei um sprite correspondente a '" +
                        spriteOriginal.name + "' no PNG novo. " +
                        "Confira a numeracao ou os recortes das duas spritesheets.");

                if (!spritesDestinoJaUsados.Add(novo))
                    throw new InvalidOperationException(
                        "Dois sprites diferentes do modelo apontariam para o mesmo sprite novo ('" +
                        novo.name + "'). Confira os recortes ou a numeracao.");

                substituicoes.Add(spriteOriginal, novo);
            }

            // Instantiate preserva as regras, vizinhos, animacoes, configuracoes
            // e o tipo original do RuleTile, mas cria um asset independente.
            copia = Instantiate(ruleTileModelo);
            copia.name = System.IO.Path.GetFileNameWithoutExtension(caminhoDestino);

            if (copia.m_DefaultSprite != null)
                copia.m_DefaultSprite = substituicoes[copia.m_DefaultSprite];

            foreach (var regra in copia.m_TilingRules)
            {
                if (regra == null || regra.m_Sprites == null)
                    continue;

                for (int i = 0; i < regra.m_Sprites.Length; i++)
                {
                    Sprite sprite = regra.m_Sprites[i];
                    if (sprite != null)
                        regra.m_Sprites[i] = substituicoes[sprite];
                }
            }

            // Evita sobrescrever qualquer asset existente.
            caminhoDestino = AssetDatabase.GenerateUniqueAssetPath(caminhoDestino);
            copia.name = System.IO.Path.GetFileNameWithoutExtension(caminhoDestino);
            AssetDatabase.CreateAsset(copia, caminhoDestino);
            copia = null; // Agora o Unity e dono do asset.

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var criado = AssetDatabase.LoadAssetAtPath<RuleTile>(caminhoDestino);
            Selection.activeObject = criado;
            EditorGUIUtility.PingObject(criado);

            Debug.Log("[RuleTile Cloner] Criado: " + caminhoDestino +
                      " | Sprites substituidos: " + substituicoes.Count);
            EditorUtility.DisplayDialog(
                "RuleTile criado",
                "Novo RuleTile salvo em:\n" + caminhoDestino +
                "\n\nSprites diferentes substituidos: " + substituicoes.Count,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Erro ao clonar RuleTile", ex.Message, "OK");
        }
        finally
        {
            if (copia != null)
                DestroyImmediate(copia);
        }
    }

    private static Sprite[] CarregarSprites(string caminho)
    {
        return AssetDatabase.LoadAllAssetsAtPath(caminho)
            .OfType<Sprite>()
            .ToArray();
    }

    private static bool TentarNumero(string nome, out int numero)
    {
        numero = -1;
        Match match = Regex.Match(nome, @"(?:^|[_\s-])(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out numero);
    }

    private static bool MesmoRecorte(Rect a, Rect b)
    {
        return Mathf.Approximately(a.x, b.x) &&
               Mathf.Approximately(a.y, b.y) &&
               Mathf.Approximately(a.width, b.width) &&
               Mathf.Approximately(a.height, b.height);
    }

    private static Sprite EncontrarCorrespondente(
        Sprite original, int quantidadeOriginal, Sprite[] novos)
    {
        // Se cada PNG tiver um unico Sprite, a correspondencia e direta.
        if (quantidadeOriginal == 1 && novos.Length == 1)
            return novos[0];

        // Prioridade: numero do sprite (ex.: Water_100 -> Lava_100).
        if (TentarNumero(original.name, out int indiceOriginal))
        {
            var porNumero = novos.Where(sprite =>
                TentarNumero(sprite.name, out int indice) && indice == indiceOriginal)
                .ToArray();

            if (porNumero.Length == 1)
                return porNumero[0];
        }

        // Alternativa: mesma posicao e tamanho no PNG.
        var porRecorte = novos.Where(sprite => MesmoRecorte(original.rect, sprite.rect))
            .ToArray();

        return porRecorte.Length == 1 ? porRecorte[0] : null;
    }

    private static void PrepararPngNovo(
        string origemPath, string destinoPath, int quantidadeSpritesOriginais)
    {
        var importadorOrigem = AssetImporter.GetAtPath(origemPath) as TextureImporter;
        var importadorDestino = AssetImporter.GetAtPath(destinoPath) as TextureImporter;

        if (importadorOrigem == null || importadorDestino == null)
            throw new InvalidOperationException("Nao consegui ler os importadores dos dois PNGs.");

        Sprite[] atuais = CarregarSprites(destinoPath);

        // Se ja estiver recortado em varios Sprites, respeita a configuracao existente.
        if (importadorDestino.spriteImportMode == SpriteImportMode.Multiple &&
            atuais.Length == quantidadeSpritesOriginais)
            return;

        // Modelo e destino de sprite unico: so precisa importar como Sprite.
        if (quantidadeSpritesOriginais == 1)
        {
            if (atuais.Length > 0)
                return;

            importadorDestino.textureType = TextureImporterType.Sprite;
            importadorDestino.spriteImportMode = SpriteImportMode.Single;
            importadorDestino.spritePixelsPerUnit = importadorOrigem.spritePixelsPerUnit;
            importadorDestino.SaveAndReimport();
            return;
        }

        var texturaOrigem = AssetDatabase.LoadAssetAtPath<Texture2D>(origemPath);
        var texturaDestino = AssetDatabase.LoadAssetAtPath<Texture2D>(destinoPath);

        if (texturaOrigem.width != texturaDestino.width ||
            texturaOrigem.height != texturaDestino.height)
            throw new InvalidOperationException(
                "O PNG novo ainda nao esta recortado, mas suas dimensoes sao diferentes " +
                "das do PNG original. Nao e seguro copiar os recortes automaticamente.");

        // Unity 2021.2+ / Unity 6: API oficial do Sprite Editor.
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        var provedorOrigem = factory.GetSpriteEditorDataProviderFromObject(importadorOrigem);
        if (provedorOrigem == null)
            throw new InvalidOperationException("Nao foi possivel acessar o Sprite Editor do PNG original.");

        provedorOrigem.InitSpriteEditorDataProvider();
        SpriteRect[] recortesOriginais = provedorOrigem.GetSpriteRects();

        if (recortesOriginais == null || recortesOriginais.Length == 0)
            throw new InvalidOperationException("Nao encontrei os recortes no PNG original.");

        importadorDestino.textureType = TextureImporterType.Sprite;
        importadorDestino.spriteImportMode = SpriteImportMode.Multiple;
        importadorDestino.spritePixelsPerUnit = importadorOrigem.spritePixelsPerUnit;
        importadorDestino.filterMode = importadorOrigem.filterMode;
        importadorDestino.SaveAndReimport();
        importadorDestino = AssetImporter.GetAtPath(destinoPath) as TextureImporter;

        var provedorDestino = factory.GetSpriteEditorDataProviderFromObject(importadorDestino);
        if (provedorDestino == null)
            throw new InvalidOperationException("Nao foi possivel acessar o Sprite Editor do PNG novo.");

        provedorDestino.InitSpriteEditorDataProvider();

        var novosRecortes = new SpriteRect[recortesOriginais.Length];
        var nomesUsados = new HashSet<string>();
        string nomeBase = System.IO.Path.GetFileNameWithoutExtension(destinoPath);

        for (int i = 0; i < recortesOriginais.Length; i++)
        {
            SpriteRect origem = recortesOriginais[i];
            string sufixo = TentarNumero(origem.name, out int indice)
                ? indice.ToString()
                : "slice_" + i;
            string nome = nomeBase + "_" + sufixo;
            if (!nomesUsados.Add(nome))
            {
                nome = nomeBase + "_slice_" + i;
                nomesUsados.Add(nome);
            }

            novosRecortes[i] = new SpriteRect
            {
                name = nome,
                spriteID = GUID.Generate(),
                rect = origem.rect,
                pivot = origem.pivot,
                alignment = origem.alignment,
                border = origem.border
            };
        }

        provedorDestino.SetSpriteRects(novosRecortes);

        var nomesEIds = provedorDestino.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nomesEIds != null)
        {
            nomesEIds.SetNameFileIdPairs(novosRecortes.Select(r =>
                new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        }

        provedorDestino.Apply();
        importadorDestino.SaveAndReimport();
    }
}
