# Sprint 12 — Save Real + Continue Game + Ciclo de Dia Completo

**Depende de:** Sprint 11.
**Objetivo:** fecha o **Primeiro Vertical Slice** — `New Game → Dia 1 → combate → venda → demanda → Results → Loja → compra → Save real no checkpoint → Start Day 2`, sem intervenção manual em nenhum ponto do meio. Fecha a Deadline 3.

> Esta sprint também fecha duas dívidas específicas: **validar de verdade** o caminho "Continue sem save" (sinalizado desde a Sprint 9) e confirmar que **New Game não sobrescreve** um save antigo antes do primeiro dia bem-sucedido da run nova (regra do GDD Seção 43, nunca testada na prática até agora).

---

## 1. RunState ganha um campo — weapon tier stub

`RunState.cs` (Sprint 4) ganha:
```csharp
public string weaponTier = "Basic";
```
**Isto não é o sistema real de Weapon Tier do GDD** (15 tiers, compra sequencial, multiplicadores de dano/vida — isso é Deadline 9, Sprint 33). Aqui é só o suficiente pra provar que "comprar algo na Loja" afeta o `RunState` e sobrevive ao Save/Load. Vira o sistema de verdade só na Sprint 33.

## 2. Save real — dispara ao entrar na Loja, não antes

Atualizar `ShopHandler.cs` (Sprint 11):
```csharp
private void HandleStateChanged(GameState state)
{
    if (state != GameState.Shop) return;

    SaveManager.Save(MainMenuUI.CurrentRun);
    Debug.Log("[ShopHandler] Save automático realizado ao entrar na Loja.");
    Debug.Log("[ShopHandler] Loja aberta. Use \"Buy Next Weapon Tier\" e/ou \"Start Next Day\".");
}
```
Isso é o único lugar do projeto que chama `SaveManager.Save()`. Como só existe um caminho pra chegar em `GameState.Shop` (via `DayResolver.ResolveEndOfDay()`, que só roda quando a demanda foi cumprida), a regra do GDD ("New Game não sobrescreve até o primeiro dia bem-sucedido") sai de graça — não precisou de nenhuma lógica extra de "proteção", é consequência direta de onde o save é chamado.

## 3. Compra stub (pra provar o loop, não o sistema real)

Em `ShopHandler.cs`:
```csharp
private static readonly string[] tierOrder = { "Basic", "Copper", "Iron" };

[ContextMenu("Buy Next Weapon Tier")]
public void BuyNextTier()
{
    int currentIndex = System.Array.IndexOf(tierOrder, MainMenuUI.CurrentRun.weaponTier);
    if (currentIndex == -1 || currentIndex >= tierOrder.Length - 1)
    {
        Debug.Log("[ShopHandler] Nenhum tier seguinte disponível (stub só vai até Iron nesta sprint).");
        return;
    }

    int price = (currentIndex + 1) * 50; // placeholder — preços reais são Deadline 9
    if (MainMenuUI.CurrentRun.gold < price)
    {
        Debug.Log($"[ShopHandler] Gold insuficiente ({MainMenuUI.CurrentRun.gold}/{price}) para {tierOrder[currentIndex + 1]}.");
        return;
    }

    MainMenuUI.CurrentRun.gold -= price;
    MainMenuUI.CurrentRun.weaponTier = tierOrder[currentIndex + 1];
    GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
    Debug.Log($"[ShopHandler] Comprou {MainMenuUI.CurrentRun.weaponTier} por {price} gold.");
}
```

## 4. Continue Game — corrigido pra abrir a Loja, não o Gameplay direto

`MainMenuUI.ContinueGame()` (Sprint 9) estava jogando direto pra `GameState.Gameplay`. Isso está errado: o GDD (Seção 43) é específico — o checkpoint é "a Loja que precede o próximo dia", e Continue Game abre **essa Loja**, não o dia em andamento. Corrigido:
```csharp
public void ContinueGame()
{
    if (!SaveManager.HasSave())
    {
        Debug.Log("[MainMenu] Continue indisponível — nenhum save encontrado.");
        return;
    }

    CurrentRun = SaveManager.Load();
    Debug.Log($"[MainMenu] Save carregado — Dia {CurrentRun.day}, Gold {CurrentRun.gold}, Hero {CurrentRun.hero}, Weapon {CurrentRun.weaponTier}");

    GameEvents.GoldChanged(CurrentRun.gold);
    BagController.Instance.Bag.Clear(); // Bag é Daily — nunca persiste no save, nem no Continue
    GameEvents.BagChanged(BagController.Instance.Bag);

    GameStateManager.Instance.SetState(GameState.Shop);
}
```
Repara que isso **não** chama `SaveManager.Save()` — só carrega. O próximo save só acontece quando o jogador avançar mais um dia a partir daqui (mesmo caminho da Seção 2).

## 5. Teste automatizado — Save/Load round-trip real

`Assets/Tests/EditMode/SaveManagerTests.cs`:
```csharp
using NUnit.Framework;

public class SaveManagerTests
{
    [Test]
    public void SaveAndLoad_RoundTripsCorrectly()
    {
        var original = new RunState { day = 7, gold = 350, weaponTier = "Copper", hero = "Barbarian" };
        SaveManager.Save(original);

        var loaded = SaveManager.Load();

        Assert.AreEqual(original.day, loaded.day);
        Assert.AreEqual(original.gold, loaded.gold);
        Assert.AreEqual(original.weaponTier, loaded.weaponTier);
        Assert.AreEqual(original.hero, loaded.hero);
    }
}
```
**Aviso honesto:** esse teste escreve/lê um arquivo real em `Application.persistentDataPath` — não é isolado como os outros testes EditMode do projeto. Vale a exceção aqui porque é exatamente o mecanismo de disco que precisa estar certo (é o que os testes manuais anteriores já vinham conferindo na mão). Se isso incomodar rodando em CI no futuro, a solução é injetar o caminho do arquivo em vez de fixo — não fazer agora, só registrar.

---

## 6. Teste manual completo (Play Mode) — o Vertical Slice inteiro

**Parte A — o loop principal:**
1. `New Game` (sem save prévio) → Dia 1, Gold 0, Time 100s, Demand 0/40.
2. Matar inimigos, coletar, vender no Vendor até a demanda bater 40.
3. `DayResolver` dispara sozinho → Results (log) → Shop.
4. **Conferir no disco** que `save.json` agora existe (ou foi atualizado) — este é o primeiro autosave real da run.
5. Na Loja: `Buy Next Weapon Tier` → gold desconta, `weaponTier` vira `Copper`.
6. `Start Next Day` → Dia 2, Time volta a 100s, Demand reseta pra `0/80`.

**Parte B — fecha a dívida do "New Game não sobrescreve":**
7. Sair do Play Mode (ou fechar/reabrir), voltar ao fluxo de Menu.
8. `New Game` de novo (run nova) → Dia 1, Gold 0.
9. **De propósito, deixar o tempo zerar sem bater a demanda** → Game Over → Menu.
10. `Continue Game` → deve carregar o save **da Parte A** (Dia 2, `weaponTier = Copper`), **não** a run que acabou de falhar. Se carregar o Dia 2 corretamente, a regra "New Game não sobrescreve até o primeiro dia bem-sucedido" está confirmada na prática, não só no código.

**Parte C — fecha a dívida do "Continue sem save":**
11. Apagar manualmente `save.json` do `persistentDataPath` (ou testar num perfil/máquina limpa).
12. `Continue Game` → loga "indisponível — nenhum save encontrado", não quebra, não cria arquivo nenhum.

---

## 7. Git

```
git add .
git commit -m "feat: weapon tier stub field in RunState"
```
```
git add .
git commit -m "feat: real save on shop entry + weapon tier purchase stub"
```
```
git add .
git commit -m "fix: continue game opens shop (checkpoint), not gameplay directly"
```
```
git add .
git commit -m "test: add save/load round-trip editmode test"
git push
```

## 8. Fechamento — fecha a Deadline 3

`docs/sprints/sprint-12.md` (este arquivo) + linha no `docs/sprints/index.md` + linha no `docs/changelog.md` — e vale o mesmo resumo de fechamento de Deadline que fizemos entre a 2 e a 3, já que esta é a última sprint da Deadline 3 (Primeiro Vertical Slice).

---

**Pronto quando:** o teste de round-trip do `SaveManager` passa verde; as Partes A, B e C do teste manual passam nessa ordem, sem intervenção fora do que está descrito; o arquivo de save no disco reflete corretamente o estado real após cada autosave; Continue Game sempre abre a Loja do checkpoint, nunca o Gameplay direto.
