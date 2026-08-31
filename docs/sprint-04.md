# Sprint 4 — Localization + Save/RunState + Large Number + Docs Pipeline Maduro

**Depende de:** Sprint 3.
**Objetivo:** os 3 últimos esqueletos fundacionais da Deadline 1 nascem — Localization, Save/RunState e Large Number — e a documentação passa de Markdown estático pra um pipeline que também extrai docs de API do código, publicado automaticamente.

> Nota de processo (decisão da Sprint 3, aplicada aqui): sempre que um passo depender do Editor **resolver** alguma coisa (codegen, criação de asset com GUID interno, import pipeline) — não de só ler um arquivo de dados — o passo pede clique na UI, não arquivo escrito à mão. Marcado explicitamente abaixo onde isso se aplica.

---

## 1. Localization

O pacote de Localization gera assets internos (Locale, String Table Collection, Localization Settings) que dependem do Editor rodar lógica de import/Addressables por trás. **Os passos abaixo são para fazer na UI**, não arquivos pra eu escrever.

1. `Window > Package Manager` → Unity Registry → instalar **"Localization"**.
2. `Edit > Project Settings > Localization` → botão **"Create Localization Settings"** (gera o asset default e já deixa selecionado no Project Settings).
3. `Window > Asset Management > Localization Tables` → **New Language List**: adicionar `English (en)` e `Portuguese (Brazil) (pt-BR)`.
4. Ainda na janela de Localization Tables → **New Table Collection**, tipo **String Table Collection**, nome `UI Text`, marcando os dois locales criados.
5. Abrir a `UI Text` collection recém-criada → adicionar 1 entrada de teste: chave `test.hello`, valor em `en` = `"Hello, Tower"`, valor em `pt-BR` = `"Olá, Torre"`.

Depois disso, o script de teste (autocontido, escrevo direto):

`Assets/Scripts/LocalizationTest.cs`:
```csharp
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationTest : MonoBehaviour
{
    public LocalizedString testString; // no Inspector: Table = "UI Text", Entry = "test.hello"

    private void Start()
    {
        testString.StringChanged += value => Debug.Log($"[Localization] {value}");
    }

    [ContextMenu("Switch To English")]
    public void SwitchToEnglish() => SetLocale("en");

    [ContextMenu("Switch To Portuguese")]
    public void SwitchToPortuguese() => SetLocale("pt-BR");

    private void SetLocale(string code)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
    }
}
```

Adicionar esse componente no `Systems` (dentro de `//SYSTEMS`), arrastar a entrada `test.hello` no campo `Test String` no Inspector (o Unity mostra um seletor de Table/Entry — é a UI resolvendo a referência, não um GUID que eu poderia chutar certo).

**Testar:** botão direito no componente no Inspector → "Switch To English" / "Switch To Portuguese" → Console mostra `[Localization] Hello, Tower` / `[Localization] Olá, Torre`.

---

## 2. Save / RunState skeleton

Puro dado + serialização — sem nada que o Editor precise resolver. Escrevo os arquivos direto.

`Assets/Scripts/Core/RunState.cs`:
```csharp
[System.Serializable]
public class RunState
{
    public string mode = "Standard";
    public string hero = "Barbarian";
    public string map = "Tower";
    public int day = 1;
    public long gold = 0;
}
```

`Assets/Scripts/Core/SaveManager.cs`:
```csharp
using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(RunState state)
    {
        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] Saved to {SavePath}");
    }

    public static RunState Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save found.");
            return null;
        }
        return JsonUtility.FromJson<RunState>(File.ReadAllText(SavePath));
    }

    public static bool HasSave() => File.Exists(SavePath);
}
```

`Assets/Scripts/SaveTest.cs`:
```csharp
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    [ContextMenu("Test Save")]
    public void TestSave()
    {
        var state = new RunState { day = 5, gold = 1200 };
        SaveManager.Save(state);
    }

    [ContextMenu("Test Load")]
    public void TestLoad()
    {
        var loaded = SaveManager.Load();
        if (loaded != null)
            Debug.Log($"[SaveTest] day={loaded.day}, gold={loaded.gold}, hero={loaded.hero}");
    }
}
```

Adicionar `SaveTest` no `Systems`. **Testar:** botão direito → "Test Save" (loga o caminho do arquivo) → botão direito → "Test Load" (loga `day=5, gold=1200, hero=Barbarian`).

**Nota:** `SaveManager`/`RunState` moram em `Assets/Scripts/Core/` — devem cair no mesmo `Core.asmdef` da Sprint 3. Se o Inspector reclamar de referência ao compilar, é o mesmo tipo de problema do `EditMode.asmdef`: resolve pela UI (arrastar o script pra dentro da pasta já é suficiente na maioria dos casos, já que o `.asmdef` se aplica por pasta).

---

## 3. Large Number Abstraction

Decisão técnica registrada nesta sprint: **`double`** como tipo de valor econômico por enquanto. Faixa (~15-17 dígitos significativos) cobre confortavelmente até trilhões sem overflow; se algum dia a precisão virar problema real em valores extremos, é uma troca isolada nesta classe, não espalhada pelo projeto — por isso ela existe como camada própria em vez de `double` usado direto em todo lugar.

`Assets/Scripts/Core/LargeNumberFormatter.cs`:
```csharp
using System.Globalization;

public static class LargeNumberFormatter
{
    private static readonly string[] Suffixes = { "", "k", "m", "b", "t" };

    public static string Format(double value)
    {
        if (value < 1000) return value.ToString("0", CultureInfo.InvariantCulture);

        int suffixIndex = 0;
        double reduced = value;

        while (reduced >= 1000 && suffixIndex < Suffixes.Length - 1)
        {
            reduced /= 1000;
            suffixIndex++;
        }

        return reduced.ToString("0.#", CultureInfo.InvariantCulture) + Suffixes[suffixIndex];
    }
}
```

`Assets/Scripts/LargeNumberTest.cs`:
```csharp
using UnityEngine;

public class LargeNumberTest : MonoBehaviour
{
    [ContextMenu("Test Formatting")]
    public void TestFormat()
    {
        Debug.Log(LargeNumberFormatter.Format(850));         // 850
        Debug.Log(LargeNumberFormatter.Format(1500));        // 1.5k
        Debug.Log(LargeNumberFormatter.Format(2_300_000));   // 2.3m
        Debug.Log(LargeNumberFormatter.Format(4_000_000_000)); // 4b
        Debug.Log(LargeNumberFormatter.Format(1_000_000_000_000)); // 1t
    }
}
```

Adicionar no `Systems`, testar via botão direito → "Test Formatting", conferir os 5 valores no Console.

## 4. Teste automatizado (EditMode)

`Assets/Tests/EditMode/LargeNumberFormatterTests.cs`:
```csharp
using NUnit.Framework;

public class LargeNumberFormatterTests
{
    [Test]
    public void Format_BelowThousand_ReturnsPlainNumber()
    {
        Assert.AreEqual("850", LargeNumberFormatter.Format(850));
    }

    [Test]
    public void Format_Million_ReturnsMSuffix()
    {
        Assert.AreEqual("2.3m", LargeNumberFormatter.Format(2_300_000));
    }

    [Test]
    public void Format_Trillion_ReturnsTSuffix()
    {
        Assert.AreEqual("1t", LargeNumberFormatter.Format(1_000_000_000_000));
    }
}
```
Rodar no Test Runner (mesmo assembly `EditMode` da Sprint 3, já referencia `Core` por GUID — deve reconhecer `LargeNumberFormatter` automaticamente por estar em `Core.asmdef`). **Se o Inspector não resolver sozinho**, é a mesma categoria de problema da Sprint 3 — resolve pela UI, não editando o `.asmdef` à mão.

---

## 5. Docs Pipeline — já está maduro, sem trabalho extra

Tentativa inicial: DocFX pra extrair documentação de API a partir de `Assets/Scripts/**/*.cs`, publicada via GitHub Actions. **Descartada.**

**Por quê:** o DocFX compila os scripts via Roslyn pra gerar a API, e isso exige a `UnityEngine.dll` disponível pro compilador. Essa DLL só existe numa instalação da Unity — e o runner do GitHub Actions (`ubuntu-latest`) não tem Unity instalada. Resultado testado localmente: `docfx docfx_project/docfx.json` falha com `CS0246` em todo script que usa `MonoBehaviour`/`Debug`/etc., o build inteiro sai com erro, e no workflow isso derrubaria a publicação do site inteiro (o `Upload Pages artifact`/`Deploy to GitHub Pages` nunca rodam se o passo anterior falha).

Consertar isso de verdade exigiria rodar a própria Unity dentro do CI (licença, minutos de build, complexidade real) só pra gerar documentação de API que, num projeto solo, ninguém além de você vai navegar fora do próprio Editor (onde o IntelliSense já mostra os comentários XML sem nenhum site). Não vale o custo.

**O que fica:** nada muda. O GitHub Pages configurado na Sprint 1 (`Deploy from a branch`, Jekyll com `theme: null`) já publica automaticamente toda vez que `main` recebe um push tocando `docs/` — isso **já é** um pipeline de docs maduro o suficiente pro projeto. `docfx_project/` e qualquer workflow de Actions para isso não entram no commit.

---

## 6. Git

```
git add .
git commit -m "feat: localization skeleton (en/pt-BR string table + locale switch test)"
```
```
git add .
git commit -m "feat: save/runstate skeleton + large number abstraction"
```
```
git add .
git commit -m "test: add EditMode tests for LargeNumberFormatter"
git push
```

## 7. Fechamento

`docs/sprints/sprint-04.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md` — incluindo a nota de que a tentativa de DocFX foi avaliada e descartada (motivo documentado acima), para não ser retentada sem necessidade numa sprint futura.

---

**Pronto quando:** trocar de locale muda o texto logado no Console; Save/Load gravam e recuperam um `RunState` de disco; `LargeNumberFormatter` passa nos 3 testes automatizados; o site do GitHub Pages (já publicando desde a Sprint 1) continua no ar normalmente.
