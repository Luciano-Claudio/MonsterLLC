# D9.5 enxuto (morte encerra a run + transições) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A morte encerra a run com CircleWipe até o menu, e o menu → loading → jogo usa as duas metades do Fade.

**Architecture:** A morte usa a troca de cena nativa do Easy Transitions. O Fade dividido usa um `ScreenFader` próprio, persistente, que toca os prefabs IN/OUT do pacote. A configuração fica num asset em `Resources`.

**Tech Stack:** Unity 6000.3.8f1, Easy Transitions (`EasyTransition`), NUnit EditMode, Unity CLI.

**Spec:** `docs/superpowers/specs/2026-11-21-d9-5-estrutura-run-design.md`

## Global Constraints

- Prosa em pt-BR. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Push só com confirmação.
- Morte: `Assets/EasyTransitions/Transitions/CircleWipe/CircleWipe.asset`. Menu/loading/jogo: `Assets/EasyTransitions/Transitions/Fade/Fade.asset`.
- Nada de editar cena. O `Cleric.prefab` local do usuário fica fora dos commits.

## Review Focus

1. **Morrer duas vezes no mesmo frame / evento de morte + timeout de segurança:** só uma transição (o `dieHandled` já protege, e o `TransitionManager` recusa transição concorrente). Conferir no jogo.
2. **New Game logo depois de morrer:** run nova limpa, com `PlayerSurvivedLastDay` verdadeiro, sem "Continue" e sem cronômetro andando no fade. Conferir no jogo.
3. **Cena sem `TransitionManager`:** vai pro menu sem transição, sem erro. Conferir no código.
4. **Sem save no disco:** `Delete` não falha. Teste: `Delete_WithoutSave_DoesNotThrow`.
5. **Pausa durante o fade:** o fade usa tempo real e não trava. Conferir no jogo.

---

### Task 1: `SaveManager.Delete` (TDD)

**Files:** Modify `Assets/Scripts/Core/Save/SaveManager.cs` (ou o caminho atual); Test `Assets/Tests/EditMode/SaveManagerTests.cs` (o arquivo existente).

- [ ] **Step 1:** Testes `Delete_RemovesSave` (Save, Delete, `HasSave() == false`) e `Delete_WithoutSave_DoesNotThrow`.
- [ ] **Step 2:** Confirmar o RED.
- [ ] **Step 3:** Implementar `public static void Delete()` (`File.Delete` se existir).
- [ ] **Step 4:** Rodar a suíte. **Step 5: Commit.**

### Task 2: `TransitionsConfig` e `ScreenFader`

**Files:** Create `Assets/Scripts/Transitions/TransitionsConfig.cs`, `Assets/Scripts/Transitions/ScreenFader.cs` e o asset `Assets/Resources/TransitionsConfig.asset` (via Editor, com `fade` e `death` apontando pros assets do pacote).

**Interfaces (produz):**
- `TransitionsConfig { TransitionSettings fade; TransitionSettings death; static TransitionsConfig Load(); }`
- `ScreenFader.FadeToBlack(TransitionSettings, Action)`, `SnapToBlack(TransitionSettings)`, `FadeFromBlack(TransitionSettings, Action)`, `ClearInstant()`

- [ ] **Step 1:** Implementar. O Canvas é overlay, com `sortingOrder` alto. O IN e o OUT são instanciados como filhos, com a mesma escala de referência (`refrenceResolution`) do pacote. O `SnapToBlack` instancia o IN e coloca o `Animator` dele no tempo normalizado 1.
- [ ] **Step 2:** Criar o asset pelo Editor e conferir que os dois campos estão preenchidos.
- [ ] **Step 3:** Recompilar e rodar a suíte. **Step 4: Commit.**

### Task 3: Ligar os fluxos

**Files:** `MainMenuUI.LoadGameplayScene`, `GameplayBootstrap` (New Game e Continue), `DayResolver` (`EndRunAfterDeath` no lugar de `OpenResultsAfterDeath`), `HeroController.AnimationDieEndEvent`.

- [ ] **Step 1:** Menu → loading com `FadeToBlack`.
- [ ] **Step 2:** Loading → jogo com `SnapToBlack`, `Hide` e `FadeFromBlack`, e `SetState(Gameplay)` só no fim. No Continue, o mesmo clarear.
- [ ] **Step 3:** Morte: `EndRunAfterDeath` (apaga o save, `PlayerSurvivedLastDay = true`, CircleWipe pro `Main_Menu`, com fallback).
- [ ] **Step 4:** Recompilar e rodar a suíte. **Step 5: Commit.**
- [ ] **Step 6: Manual (usuário):**
  - New Game: o menu escurece, o loading aparece e o jogo clareia;
  - o cronômetro só anda depois de clarear;
  - morrer: o CircleWipe leva pro menu;
  - o Continue some (sem save);
  - New Game de novo funciona.

### Task 4: Docs e fechamento

- [ ] Spec D9, Seção 1: a morte encerra a run (feito) e as transições. Memória do projeto. Revisão final e merge/push com confirmação.

### Task 3b (atualização do usuário, 2026-11-21): tela "Resumo total" na morte

**Files:** Create `Assets/Scripts/Core/RunSummaryRules.cs` (teste `Assets/Tests/EditMode/RunSummaryRulesTests.cs`) e `Assets/Scripts/UI/RunSummaryUI.cs`.

- [ ] **Step 1: Testes da regra pura** `RunSummaryRules.DaysSurvived`: dia 3 → 2, dia 1 → 0, dia 0 → 0. Confirmar o RED, implementar e ver o GREEN.
- [ ] **Step 2: `RunSummaryUI.Show(int daysSurvived, long permanentGold)`**, montada por código:
  - Canvas overlay com `sortingOrder` abaixo do da transição;
  - "Resumo total", "Sobreviveu X dias" e "Gold permanente: 0";
  - botão "Continuar", que faz `SceneManager.LoadScene("Main_Menu")`.
- [ ] **Step 3: `EndRunAfterDeath`**: `TransitionHelper.PlayTransition(death, () => RunSummaryUI.Show(...))` no lugar da troca de cena. Sem `TransitionManager` na cena, chama `Show` direto.
- [ ] **Step 4:** Rodar a suíte e fazer o commit.
