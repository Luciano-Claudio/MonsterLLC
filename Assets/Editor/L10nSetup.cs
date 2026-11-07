using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ferramenta de Editor pra fechar o R1 (Sistema de Tradução, GDD Seção 1) — roda 1x por cena e
// liga os labels já existentes de Main_Menu/_TestScene na String Table "UI Text". Não roda sozinha
// em nenhum compile/build: só executa quando o usuário clica no menu abaixo, de propósito (mexe em
// 2 cenas inteiras, não é algo pra disparar sem querer).
public static class L10nSetup
{
    private const string TableName = "UI Text";
    private const string MainMenuScenePath = "Assets/Scenes/Main_Menu.unity";
    private const string TestScenePath = "Assets/Scenes/_TestScene.unity";

    private readonly struct Row
    {
        public readonly string Target; // nome do GameObject (botão/label) dentro do escopo buscado
        public readonly string Key;
        public readonly string En;
        public readonly string PtBr;

        public Row(string target, string key, string en, string ptBr)
        {
            Target = target;
            Key = key;
            En = en;
            PtBr = ptBr;
        }
    }

    // Linhas compartilhadas por Sound/Display/Controls — mesma estrutura nas 2 cenas (GDD 1.1/1.2).
    private static readonly Row[] SoundRows =
    {
        new("Master", "settings.sound.master", "Master", "Volume Geral"),
        new("Music", "settings.sound.music", "Music", "Música"),
        new("SFX", "settings.sound.sfx", "SFX", "Efeitos Sonoros"),
        new("Reset_Default", "settings.reset_default", "Reset Default", "Restaurar Padrão"),
    };

    private static readonly Row[] DisplayRows =
    {
        new("Theme", "settings.display.theme", "Theme", "Tema"),
        new("Brightness", "settings.display.brightness", "Brightness", "Brilho"),
        new("Contrast", "settings.display.contrast", "Contrast", "Contraste"),
        new("Saturation", "settings.display.saturation", "Saturation", "Saturação"),
        new("Max_FPS", "settings.display.max_fps", "Max FPS", "FPS Máximo"),
        new("Camera-Max_Zoom_Out", "settings.display.camera_max_zoom_out", "Camera Max Zoom Out", "Zoom Máximo da Câmera"),
        // Fora do escopo original do GDD Seção 1.1 — achado na cena, não documentado. Traduzido
        // mesmo assim pra não sobrar 1 label em inglês no meio do resto (reportar como extra).
        new("Fullscreen", "settings.display.fullscreen", "Fullscreen", "Tela Cheia"),
        new("Reset_Default", "settings.reset_default", "Reset Default", "Restaurar Padrão"),
        // "Language_Dropdown" de propósito NÃO entra aqui: o texto visível nele é o nome do
        // idioma SELECIONADO (gerenciado em runtime por LanguageSelector.cs), não um título
        // estático — ligar isso aqui via ResolveTextComponent pegaria o "Label" interno do
        // Dropdown e brigaria com o LanguageSelector. Se existir um texto-título separado tipo
        // "Language:" do lado do dropdown, não achei na cena — avisar se houver.
    };

    private static readonly Row[] ControlsRows =
    {
        new("Action", "settings.controls.action", "Action", "Ação"),
        new("Primary_Attack", "settings.controls.primary_attack", "Primary Attack", "Ataque Primário"),
        new("Ultimate", "settings.controls.ultimate", "Ultimate", "Habilidade Definitiva"),
        new("Survival_Skill", "settings.controls.survival_skill", "Survival Skill", "Habilidade de Sobrevivência"),
        new("Move_UP", "settings.controls.move_up", "Move Up", "Mover para Cima"),
        new("Move_DOWN", "settings.controls.move_down", "Move Down", "Mover para Baixo"),
        new("Move_LEFT", "settings.controls.move_left", "Move Left", "Mover para a Esquerda"),
        new("Move_RIGHT", "settings.controls.move_right", "Move Right", "Mover para a Direita"),
        new("Interact", "settings.controls.interact", "Interact", "Interagir"),
        new("Open_Inventory", "settings.controls.open_inventory", "Open Inventory", "Abrir Inventário"),
        // "Use_TP" removido (mecânica de Controle Remoto/tecla Q extinta, GDD Seção 3) — não
        // existe mais keybind de TP pra traduzir.
        new("Menu", "settings.controls.menu", "Menu", "Menu"),
        new("Pick_Up_Magnet", "settings.controls.pick_up_magnet", "Pick Up Magnet", "Ímã de Coleta"),
        new("Reset_Default", "settings.reset_default", "Reset Default", "Restaurar Padrão"),
    };

    private static readonly Row[] MainMenuRootRows =
    {
        new("New_Game", "menu.new_game", "New Game", "Novo Jogo"),
        new("Continue", "menu.continue", "Continue", "Continuar"),
        new("Settings", "menu.settings", "Settings", "Configurações"),
        new("Credits", "menu.credits", "Credits", "Créditos"),
        new("Exit", "menu.exit", "Exit", "Sair"),
    };

    // _TestScene — Inventory tab (2 botões, achados 1x cada na cena: ver aviso no relatório final
    // sobre a duplicata esperada pelo GDD não ter sido encontrada) + Menu_Esc (pause).
    private static readonly Row[] TestSceneExtraRows =
    {
        new("Inventory_Button", "inventory.tab.inventory", "Inventory", "Inventário"),
        new("Stats_Button", "inventory.tab.stats", "Stats", "Status"),
        new("Resume", "pause.resume", "Resume", "Retomar"),
        new("Main_Menu", "pause.main_menu", "Main Menu", "Menu Principal"),
        // Settings/Exit do Menu_Esc reaproveitam as MESMAS chaves do Main_Menu — texto idêntico,
        // não faz sentido duplicar chave.
    };

    // Shop (Perk_Painel/Resume_Painel/Talent_Painel) — fora do escopo original da GDD Seção 1
    // (ela é anterior à Loja), coberto por um segundo menu (RunShop) pra não re-rodar a varredura
    // de Main_Menu/Settings toda vez que só a Loja mudar.
    private const string PerkPainelName = "Perk_Painel";
    private const string ResumePainelName = "Resume_Painel";
    private const string TalentPainelName = "Talent_Painel";

    private sealed class Report
    {
        public int KeysAdded;
        public int LabelsWired;
        public readonly List<string> NotFound = new();
    }

    [MenuItem("Tools/Localization/Setup R1 (Main_Menu + _TestScene)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[L10nSetup] Saia do Play Mode antes de rodar.");
            return;
        }

        var collection = LocalizationEditorSettings.GetStringTableCollections()
            .FirstOrDefault(c => c.TableCollectionName == TableName) as StringTableCollection;
        if (collection == null)
        {
            Debug.LogError($"[L10nSetup] String Table Collection '{TableName}' não encontrada.");
            return;
        }

        var report = new Report();
        ProcessMainMenu(collection, report);
        ProcessTestScene(collection, report);

        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) EditorUtility.SetDirty(table);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        AssetDatabase.SaveAssets();

        Debug.Log($"[L10nSetup] Concluído — {report.KeysAdded} chaves novas, {report.LabelsWired} labels ligados.");
        if (report.NotFound.Count > 0)
            Debug.LogWarning("[L10nSetup] Não encontrados (confira manualmente):\n- " + string.Join("\n- ", report.NotFound));
    }

    [MenuItem("Tools/Localization/Setup R1b (Shop)")]
    public static void RunShop()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[L10nSetup] Saia do Play Mode antes de rodar.");
            return;
        }

        var collection = LocalizationEditorSettings.GetStringTableCollections()
            .FirstOrDefault(c => c.TableCollectionName == TableName) as StringTableCollection;
        if (collection == null)
        {
            Debug.LogError($"[L10nSetup] String Table Collection '{TableName}' não encontrada.");
            return;
        }

        var report = new Report();
        var (scene, weOpenedIt) = OpenScene(TestScenePath);
        if (scene != null)
        {
            ProcessPerkPainel(FindAllInScene(scene.Value, PerkPainelName).FirstOrDefault(), collection, report, TestScenePath);
            ProcessResumePainel(FindAllInScene(scene.Value, ResumePainelName).FirstOrDefault(), collection, report, TestScenePath);
            ProcessTalentPainel(FindAllInScene(scene.Value, TalentPainelName).FirstOrDefault(), collection, report, TestScenePath);
            FinishScene(scene.Value, weOpenedIt);
        }

        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) EditorUtility.SetDirty(table);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        AssetDatabase.SaveAssets();

        Debug.Log($"[L10nSetup] R1b concluído — {report.KeysAdded} chaves novas, {report.LabelsWired} labels ligados.");
        if (report.NotFound.Count > 0)
            Debug.LogWarning("[L10nSetup] R1b — não encontrados (confira manualmente):\n- " + string.Join("\n- ", report.NotFound));
    }

    // Desfaz Run()/RunShop() acima — tradução adiada pra depois do MVP (decisão do usuário: a
    // fonte atual não tem acentuação pt-BR, e o locale do sistema assumia pt-BR sozinho ao abrir o
    // jogo). Lê o valor em inglês já salvo na tabela "UI Text", escreve direto no componente de
    // texto, remove o LocalizeStringEvent, e desativa o(s) Language_Dropdown. NÃO apaga o pacote,
    // os Locales nem a String Table Collection — fica tudo parado, pronto pra retomar depois.
    [MenuItem("Tools/Localization/Revert R1+R1b (plain English)")]
    public static void Revert()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[L10nSetup] Saia do Play Mode antes de rodar.");
            return;
        }

        var collection = LocalizationEditorSettings.GetStringTableCollections()
            .FirstOrDefault(c => c.TableCollectionName == TableName) as StringTableCollection;
        if (collection == null)
        {
            Debug.LogError($"[L10nSetup] String Table Collection '{TableName}' não encontrada.");
            return;
        }

        var enTable = collection.StringTables.FirstOrDefault(t => t.LocaleIdentifier.Code == "en");
        if (enTable == null)
        {
            Debug.LogError("[L10nSetup] Tabela 'en' não encontrada.");
            return;
        }

        int reverted = 0, dropdownsReset = 0, missingEnValue = 0;
        RevertScene(MainMenuScenePath, enTable, ref reverted, ref dropdownsReset, ref missingEnValue);
        RevertScene(TestScenePath, enTable, ref reverted, ref dropdownsReset, ref missingEnValue);

        Debug.Log($"[L10nSetup] Revert concluído — {reverted} labels voltaram a texto fixo em inglês, {dropdownsReset} dropdown(s) de idioma desativado(s).");
        if (missingEnValue > 0)
            Debug.LogWarning($"[L10nSetup] Revert — {missingEnValue} LocalizeStringEvent sem valor em 'en' encontrado (componente removido, texto mantido como estava).");
    }

    private static void RevertScene(string path, StringTable enTable, ref int reverted, ref int dropdownsReset, ref int missingEnValue)
    {
        var (scene, weOpenedIt) = OpenScene(path);
        if (scene == null) return;

        foreach (var sceneRoot in scene.Value.GetRootGameObjects())
        {
            foreach (var lse in sceneRoot.GetComponentsInChildren<LocalizeStringEvent>(true))
            {
                string key = lse.StringReference.TableEntryReference.Key;
                var entry = string.IsNullOrEmpty(key) ? null : enTable.GetEntry(key);

                if (entry != null && !string.IsNullOrEmpty(entry.Value))
                {
                    var tmp = lse.GetComponent<TMP_Text>();
                    if (tmp != null) tmp.text = entry.Value;
                    else
                    {
                        var legacy = lse.GetComponent<Text>();
                        if (legacy != null) legacy.text = entry.Value;
                    }
                }
                else
                {
                    missingEnValue++;
                }

                Object.DestroyImmediate(lse);
                reverted++;
            }

            foreach (var dropdownGo in FindAllDeepChildren(sceneRoot.transform, "Language_Dropdown"))
            {
                var selector = dropdownGo.GetComponent<LanguageSelector>();
                if (selector != null) Object.DestroyImmediate(selector);
                dropdownGo.SetActive(false);
                dropdownsReset++;
            }
        }

        FinishScene(scene.Value, weOpenedIt);
    }

    // Perk_Painel — botões de troca de aba, Redefine, e os 4 cabeçalhos de área dentro do mapa de
    // perks (Content). "Start_Day" e "Hero_Perks_x/120" ficam de fora de propósito: os dois têm
    // texto composto com valor dinâmico já embutido no mesmo componente (dia atual / contagem de
    // perks) — precisam virar Smart String ou troca em runtime no sprint que ligar os dados de
    // verdade, não dá pra travar isso como 1 chave estática agora.
    private static void ProcessPerkPainel(GameObject panel, StringTableCollection collection, Report report, string scenePath)
    {
        if (panel == null) { report.NotFound.Add($"{scenePath}: '{PerkPainelName}' não encontrado"); return; }
        var root = panel.transform;

        WireRow(FindAllDeepChildren(root, "Resume_Button").FirstOrDefault(),
            new Row("Resume_Button", "shop.tab.resume", "Resume", "Resumo"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Talents_Button").FirstOrDefault(),
            new Row("Talents_Button", "shop.tab.talents", "Talents", "Talentos"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Redefine").FirstOrDefault(),
            new Row("Redefine", "shop.perks.redefine", "Redefine", "Redefinir"), collection, report, scenePath);

        var content = FindAllDeepChildren(root, "Content").FirstOrDefault();
        if (content == null)
        {
            report.NotFound.Add($"{scenePath}: 'Content' (mapa de perks) não encontrado dentro de {PerkPainelName}");
            return;
        }

        WireRow(FindAllDeepChildren(content.transform, "World_Name").FirstOrDefault(),
            new Row("World_Name", "shop.perks.area.world", "World", "Mundo"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(content.transform, "Primary_Attack").FirstOrDefault(),
            new Row("Primary_Attack", "shop.perks.area.primary_attack", "Primary Attack", "Ataque Primário"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(content.transform, "Survival").FirstOrDefault(),
            new Row("Survival", "shop.perks.area.survival", "Survival", "Sobrevivência"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(content.transform, "Ultimate").FirstOrDefault(),
            new Row("Ultimate", "shop.perks.area.ultimate", "Ultimate", "Habilidade Definitiva"), collection, report, scenePath);
    }

    // Resume_Painel (aba "Resumo do Dia"). Day_X e Survival_Time ficam de fora — mesmo motivo do
    // Start_Day acima (número/estado dinâmico já embutido no texto). Nome/quantidade/unidade/
    // subtotal dos 14 itens de Loot_Sold também ficam de fora: são dado de run (hoje "???"/cor
    // 141414 por padrão, GDD já documenta), não texto estático de chrome.
    private static void ProcessResumePainel(GameObject panel, StringTableCollection collection, Report report, string scenePath)
    {
        if (panel == null) { report.NotFound.Add($"{scenePath}: '{ResumePainelName}' não encontrado"); return; }
        var root = panel.transform;

        WireRow(FindAllDeepChildren(root, "Day_Sumary").FirstOrDefault(),
            new Row("Day_Sumary", "resume.day_summary_header", "Day Summary", "Resumo do Dia"), collection, report, scenePath);

        WireRow(FindAllDeepChildren(root, "Combat_Sumary").FirstOrDefault(),
            new Row("Combat_Sumary", "resume.combat_summary_header", "Combat Summary", "Resumo de Combate"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Creatures_Slain").FirstOrDefault(),
            new Row("Creatures_Slain", "resume.row.creatures_slain", "Creatures Slain:", "Criaturas Abatidas:"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Bosses_Defeated").FirstOrDefault(),
            new Row("Bosses_Defeated", "resume.row.bosses_defeated", "Bosses Defeated:", "Chefes Derrotados:"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Gold_From_Monsters").FirstOrDefault(),
            new Row("Gold_From_Monsters", "resume.row.gold_from_monsters", "Gold From Monsters:", "Ouro de Monstros:"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Gold_From_Bosses").FirstOrDefault(),
            new Row("Gold_From_Bosses", "resume.row.gold_from_bosses", "Gold From Bosses:", "Ouro de Chefes:"), collection, report, scenePath);

        WireRow(FindAllDeepChildren(root, "Loot_Sold").FirstOrDefault(),
            new Row("Loot_Sold", "resume.loot_sold_header", "Loot Sold", "Itens Vendidos"), collection, report, scenePath);

        // "Item"/"Quantity"/"Unit"/"Subtotal" se repetem em cada um dos 14 itens da lista — só o
        // Header tem o texto de coluna de verdade, por isso escopado explicitamente nele (nunca
        // no painel inteiro, senão pega a 1ª ocorrência errada dependendo da ordem de busca).
        var header = FindAllDeepChildren(root, "Header").FirstOrDefault(go => IsDescendantOf(go.transform, "Loot_Sold"));
        if (header == null)
        {
            report.NotFound.Add($"{scenePath}: 'Header' da lista de Loot_Sold não encontrado");
        }
        else
        {
            WireRow(FindAllDeepChildren(header.transform, "Item").FirstOrDefault(),
                new Row("Item", "resume.loot.col_item", "Item", "Item"), collection, report, scenePath);
            WireRow(FindAllDeepChildren(header.transform, "Quantity").FirstOrDefault(),
                new Row("Quantity", "resume.loot.col_qty", "QTY", "QTD"), collection, report, scenePath);
            WireRow(FindAllDeepChildren(header.transform, "Unit").FirstOrDefault(),
                new Row("Unit", "resume.loot.col_unit", "Unit", "Unidade"), collection, report, scenePath);
            WireRow(FindAllDeepChildren(header.transform, "Subtotal").FirstOrDefault(),
                new Row("Subtotal", "resume.loot.col_subtotal", "Subtotal", "Subtotal"), collection, report, scenePath);
        }

        WireRow(FindAllDeepChildren(root, "Kill_Gold").FirstOrDefault(),
            new Row("Kill_Gold", "resume.totals.kill_gold", "Kill Gold:", "Ouro de Abates:"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Loot_Gold").FirstOrDefault(),
            new Row("Loot_Gold", "resume.totals.loot_gold", "Loot Gold:", "Ouro de Itens:"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Total").FirstOrDefault(),
            new Row("Total", "resume.totals.total", "Total:", "Total:"), collection, report, scenePath);

        WireRow(FindAllDeepChildren(root, "+1_Talent_Point").FirstOrDefault(),
            new Row("+1_Talent_Point", "resume.talent_point_gained", "+1 Talent Point", "+1 Ponto de Talento"), collection, report, scenePath);
        // "New_Hero" só tem Animator, sem Text/TMP_Text ainda — o popup de hover ("você liberou
        // um herói novo") depende do sistema de Popups/Tooltips (R7), nada pra localizar aqui.

        WireRow(FindAllDeepChildren(root, "Perk_Painel_Button").FirstOrDefault(),
            new Row("Perk_Painel_Button", "shop.tab.perks", "Perks", "Perks"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Talents_Button").FirstOrDefault(),
            new Row("Talents_Button", "shop.tab.talents", "Talents", "Talentos"), collection, report, scenePath);
    }

    // Talent_Painel. As 3 Card_Buttons (Talent_Outline/Talent_Name_Background/Text_Talent_Details)
    // ficam de fora — conteúdo do talento sorteado em runtime, não chrome estático (mesmo critério
    // dos 121 nós de perk/dos 14 itens do Loot_Sold acima). O texto placeholder "Incress your
    // damage..." das 3 cartas já foi corrigido pra "Increase..." direto na cena (erro de inglês
    // real — não vira chave porque o texto real virá do talento sorteado, não de chrome).
    private static void ProcessTalentPainel(GameObject panel, StringTableCollection collection, Report report, string scenePath)
    {
        if (panel == null) { report.NotFound.Add($"{scenePath}: '{TalentPainelName}' não encontrado"); return; }
        var root = panel.transform;

        WireRow(FindAllDeepChildren(root, "Talents").FirstOrDefault(),
            new Row("Talents", "shop.tab.talents", "Talents", "Talentos"), collection, report, scenePath);

        // Texto de instrução e cabeçalho do Owned_Talents ficam num "Text (TMP)" sem nome
        // próprio — esse nome se repete em dezenas de labels na cena, então buscamos só entre os
        // FILHOS DIRETOS do escopo certo (painel / Owned_Talents), nunca em profundidade.
        var instruction = DirectChildren(root).FirstOrDefault(t => t.name == "Text (TMP)" && t.GetComponent<TMP_Text>() != null);
        WireRow(instruction?.gameObject,
            new Row("Text (TMP)", "shop.talents.instruction", "Choose 1 talent for the game", "Escolha 1 talento para a partida"),
            collection, report, scenePath);

        WireRow(FindAllDeepChildren(root, "Choose_Button").FirstOrDefault(),
            new Row("Choose_Button", "shop.talents.choose_button", "Choose (1pt)", "Escolher (1pt)"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Reroll_Button").FirstOrDefault(),
            new Row("Reroll_Button", "shop.talents.reroll_button", "Reroll", "Reroll"), collection, report, scenePath);

        var ownedTalents = FindAllDeepChildren(root, "Owned_Talents").FirstOrDefault();
        if (ownedTalents == null)
        {
            report.NotFound.Add($"{scenePath}: 'Owned_Talents' não encontrado em {TalentPainelName}");
        }
        else
        {
            var header = DirectChildren(ownedTalents.transform).FirstOrDefault(t => t.name == "Text (TMP)" && t.GetComponent<TMP_Text>() != null);
            WireRow(header?.gameObject,
                new Row("Text (TMP)", "shop.talents.owned_header", "Owned Talents", "Talentos Adquiridos"),
                collection, report, scenePath);
        }

        WireRow(FindAllDeepChildren(root, "Resume_Button").FirstOrDefault(),
            new Row("Resume_Button", "shop.tab.resume", "Resume", "Resumo"), collection, report, scenePath);
        WireRow(FindAllDeepChildren(root, "Perk_Painel_Button").FirstOrDefault(),
            new Row("Perk_Painel_Button", "shop.tab.perks", "Perks", "Perks"), collection, report, scenePath);
    }

    private static List<Transform> DirectChildren(Transform t)
    {
        var result = new List<Transform>();
        for (int i = 0; i < t.childCount; i++) result.Add(t.GetChild(i));
        return result;
    }

    private static void ProcessMainMenu(StringTableCollection collection, Report report)
    {
        var (scene, weOpenedIt) = OpenScene(MainMenuScenePath);
        if (scene == null) return;

        foreach (var row in MainMenuRootRows)
        {
            // "Settings" e "Credits" existem 2x na cena (botão do menu + painel do mesmo nome) —
            // prefere o que tem Button (o painel é só um container, sem Button próprio).
            var go = ResolveAmbiguous(FindAllInScene(scene.Value, row.Target), preferHasButton: true);
            WireRow(go, row, collection, report, MainMenuScenePath);
        }

        var settingsRoot = ResolveAmbiguous(
            FindAllInScene(scene.Value, "Settings"), preferHasButton: false, requireChild: "Header");
        ProcessSettingsPanel(settingsRoot, collection, report, MainMenuScenePath);

        FinishScene(scene.Value, weOpenedIt);
    }

    private static void ProcessTestScene(StringTableCollection collection, Report report)
    {
        var (scene, weOpenedIt) = OpenScene(TestScenePath);
        if (scene == null) return;

        foreach (var row in TestSceneExtraRows)
        {
            var go = ResolveAmbiguous(FindAllInScene(scene.Value, row.Target), preferHasButton: true);
            WireRow(go, row, collection, report, TestScenePath);
        }

        // "Settings"/"Exit" aqui reaproveitam as chaves do Main_Menu (ver TestSceneExtraRows).
        var menuEscSettings = FindAllInScene(scene.Value, "Settings")
            .FirstOrDefault(go => IsDescendantOf(go.transform, "Menu_Esc"));
        if (menuEscSettings != null)
            WireRow(menuEscSettings, new Row("Settings", "menu.settings", "Settings", "Configurações"), collection, report, TestScenePath);
        else
            report.NotFound.Add($"{TestScenePath}: 'Settings' dentro de 'Menu_Esc' não encontrado");

        var menuEscExit = FindAllInScene(scene.Value, "Exit")
            .FirstOrDefault(go => IsDescendantOf(go.transform, "Menu_Esc"));
        if (menuEscExit != null)
            WireRow(menuEscExit, new Row("Exit", "menu.exit", "Exit", "Sair"), collection, report, TestScenePath);
        else
            report.NotFound.Add($"{TestScenePath}: 'Exit' dentro de 'Menu_Esc' não encontrado");

        var settingsRoot = ResolveAmbiguous(
            FindAllInScene(scene.Value, "Settings"), preferHasButton: false, requireChild: "Header");
        ProcessSettingsPanel(settingsRoot, collection, report, TestScenePath);

        FinishScene(scene.Value, weOpenedIt);
    }

    // Settings/Header/<aba> = texto do botão da aba; Settings/<aba> = painel com as linhas de
    // dentro (mesma convenção documentada no SettingsTabsUI.cs já existente).
    private static void ProcessSettingsPanel(GameObject settingsRoot, StringTableCollection collection, Report report, string scenePath)
    {
        if (settingsRoot == null)
        {
            report.NotFound.Add($"{scenePath}: painel 'Settings' não encontrado");
            return;
        }

        WireRow(settingsRoot.transform.Find("Header/Sound")?.gameObject,
            new Row("Sound", "settings.tab.sound", "Sound", "Som"), collection, report, scenePath);
        WireRow(settingsRoot.transform.Find("Header/Display")?.gameObject,
            new Row("Display", "settings.tab.display", "Display", "Tela"), collection, report, scenePath);
        WireRow(settingsRoot.transform.Find("Header/Controls")?.gameObject,
            new Row("Controls", "settings.tab.controls", "Controls", "Controles"), collection, report, scenePath);

        ProcessPanelRows(settingsRoot.transform.Find("Sound"), SoundRows, collection, report, scenePath);
        ProcessPanelRows(settingsRoot.transform.Find("Display"), DisplayRows, collection, report, scenePath);
        ProcessPanelRows(settingsRoot.transform.Find("Controls"), ControlsRows, collection, report, scenePath);

        // Não assume onde mora — o usuário colocou no Header (ao lado dos botões de aba), não
        // dentro do painel Display (achado real: a 1ª versão só buscava dentro de "Display" e
        // nunca encontrava). Busca no painel Settings inteiro.
        var languageDropdownGo = FindAllDeepChildren(settingsRoot.transform, "Language_Dropdown").FirstOrDefault();
        WireLanguageDropdown(languageDropdownGo, report, scenePath);
    }

    private static void ProcessPanelRows(Transform panel, Row[] rows, StringTableCollection collection, Report report, string scenePath)
    {
        if (panel == null)
        {
            report.NotFound.Add($"{scenePath}: painel de conteúdo não encontrado pra um dos grupos de rows");
            return;
        }

        foreach (var row in rows)
        {
            var go = FindAllDeepChildren(panel, row.Target).FirstOrDefault();
            WireRow(go, row, collection, report, scenePath);
        }
    }

    private static void WireRow(GameObject go, Row row, StringTableCollection collection, Report report, string scenePath)
    {
        if (go == null)
        {
            report.NotFound.Add($"{scenePath}: '{row.Target}' não encontrado (chave {row.Key})");
            return;
        }

        EnsureKey(collection, row.Key, row.En, row.PtBr, report);

        var textComp = ResolveTextComponent(go.transform);
        if (textComp == null)
        {
            report.NotFound.Add($"{scenePath}: '{row.Target}' sem Text/TMP_Text localizável (chave {row.Key})");
            return;
        }

        WireLocalizeStringEvent(textComp, collection, row.Key, report);
    }

    // Convenção confirmada na cena: label fica em "<Nome>_Name" (linhas do Settings) OU direto no
    // próprio objeto OU no 1º filho com Text/TMP_Text (botões padrão Unity, filho "Text (TMP)").
    private static Component ResolveTextComponent(Transform target)
    {
        var byConvention = target.Find(target.name + "_Name");
        if (byConvention != null)
        {
            var tmp = byConvention.GetComponent<TMP_Text>();
            if (tmp != null) return tmp;
            var legacy = byConvention.GetComponent<Text>();
            if (legacy != null) return legacy;
        }

        var selfTmp = target.GetComponent<TMP_Text>();
        if (selfTmp != null) return selfTmp;
        var selfLegacy = target.GetComponent<Text>();
        if (selfLegacy != null) return selfLegacy;

        for (int i = 0; i < target.childCount; i++)
        {
            var child = target.GetChild(i);
            var childTmp = child.GetComponent<TMP_Text>();
            if (childTmp != null) return childTmp;
            var childLegacy = child.GetComponent<Text>();
            if (childLegacy != null) return childLegacy;
        }

        return null;
    }

    private static void EnsureKey(StringTableCollection collection, string key, string en, string ptBr, Report report)
    {
        var shared = collection.SharedData;
        long id = shared.GetId(key, addNewKey: true);
        bool isNew = false;

        foreach (var stringTable in collection.StringTables)
        {
            var entry = stringTable.GetEntry(id);
            if (entry != null && !string.IsNullOrEmpty(entry.Value)) continue;

            string value = stringTable.LocaleIdentifier.Code == "pt-BR" ? ptBr : en;
            stringTable.AddEntry(id, value);
            isNew = true;
        }

        if (isNew) report.KeysAdded++;
    }

    // Idempotente: se já existe um LocalizeStringEvent apontando pra essa chave, não duplica.
    private static void WireLocalizeStringEvent(Component textComp, StringTableCollection collection, string key, Report report)
    {
        var go = textComp.gameObject;
        var existing = go.GetComponents<LocalizeStringEvent>()
            .FirstOrDefault(e => e.StringReference != null && e.StringReference.TableEntryReference.Key == key);
        var lse = existing != null ? existing : go.AddComponent<LocalizeStringEvent>();

        lse.StringReference.TableReference = collection.TableCollectionName;
        lse.StringReference.TableEntryReference = key;

        if (existing == null)
        {
            var setText = (UnityEngine.Events.UnityAction<string>)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction<string>), textComp, "set_text");
            UnityEditor.Events.UnityEventTools.AddPersistentListener(lse.OnUpdateString, setText);
            int index = lse.OnUpdateString.GetPersistentEventCount() - 1;
            lse.OnUpdateString.SetPersistentListenerState(index, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        }

        lse.RefreshString();
        EditorUtility.SetDirty(go);
        report.LabelsWired++;
    }

    private static void WireLanguageDropdown(GameObject dropdownGo, Report report, string scenePath)
    {
        if (dropdownGo == null)
        {
            report.NotFound.Add($"{scenePath}: 'Language_Dropdown' não encontrado — seletor de idioma não ligado");
            return;
        }

        if (dropdownGo.GetComponent<LanguageSelector>() == null)
            dropdownGo.AddComponent<LanguageSelector>();

        EditorUtility.SetDirty(dropdownGo);
    }

    private static (Scene? scene, bool weOpenedIt) OpenScene(string path)
    {
        var scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded) return (scene, false);

        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        return (scene, true);
    }

    private static void FinishScene(Scene scene, bool weOpenedIt)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (weOpenedIt) EditorSceneManager.CloseScene(scene, true);
    }

    private static List<GameObject> FindAllInScene(Scene scene, string name)
    {
        var result = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
            result.AddRange(FindAllDeepChildren(root.transform, name));
        return result;
    }

    private static List<GameObject> FindAllDeepChildren(Transform root, string name)
    {
        var result = new List<GameObject>();
        if (root.name == name) result.Add(root.gameObject);
        for (int i = 0; i < root.childCount; i++)
            result.AddRange(FindAllDeepChildren(root.GetChild(i), name));
        return result;
    }

    private static bool IsDescendantOf(Transform t, string ancestorName)
    {
        for (var current = t; current != null; current = current.parent)
            if (current.name == ancestorName) return true;
        return false;
    }

    // Quando um nome aparece mais de 1x na cena: preferHasButton escolhe o controle clicável
    // (não o container); requireChild escolhe quem tem um filho específico (ex.: o painel
    // "Settings" de verdade, que tem "Header", e não o botão homônimo do menu).
    private static GameObject ResolveAmbiguous(List<GameObject> candidates, bool preferHasButton, string requireChild = null)
    {
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        if (requireChild != null)
        {
            var withChild = candidates.FirstOrDefault(c => c.transform.Find(requireChild) != null);
            if (withChild != null) return withChild;
        }

        var withButton = candidates.FirstOrDefault(c => c.GetComponent<Button>() != null || c.GetComponent<TMP_Dropdown>() != null);
        var withoutButton = candidates.FirstOrDefault(c => c.GetComponent<Button>() == null && c.GetComponent<TMP_Dropdown>() == null);

        if (preferHasButton) return withButton != null ? withButton : candidates[0];
        return withoutButton != null ? withoutButton : candidates[0];
    }
}
