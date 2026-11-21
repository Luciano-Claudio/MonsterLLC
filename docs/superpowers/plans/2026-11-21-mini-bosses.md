# Mini bosses Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cinco bosses viram mini bosses: monstros comuns, sem nenhuma mudança neles, que nascem sozinhos a cada 30–40 s pelo tier do dia, sem repetir o último.

**Architecture:** A regra pura do sorteio fica no Core, com teste. O `WorldPopulationManager` ganha uma seção de mini bosses que reaproveita o anel e os setores, o reposicionamento e o relógio da horda (D9.4). Os assets são reorganizados pelo Editor.

**Tech Stack:** Unity 6000.3.8f1, NUnit EditMode, Unity CLI (`.superpowers/tools/eval.sh`, `edittests.sh`).

**Spec:** `docs/superpowers/specs/2026-11-21-mini-bosses-design.md`

## Global Constraints

- Prosa em pt-BR. Commits terminam com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Push só com confirmação do usuário.
- Os mini bosses são: Werewolf (T2), Pale_Champion (T3), Mummy_King, Undead_Knight e Headless_Horseman (T5).
- Prefabs vão pra `Assets/Prefabs/MiniBosses/tier_2`, `tier_3` e `tier_5`, via `AssetDatabase.MoveAsset` (o GUID se mantém).
- Os assets de definição vão pra `Assets/Settings/MiniBossDefinitions/`.
- Intervalo de nascimento: 🔢 mínimo 30 s, máximo 40 s, no tempo real do dia (`HordeElapsed`).
- Nenhum caminho novo por frame pode alocar. Mini boss nunca chama `SetIsBoss`.

## Review Focus

1. **Dia sem nenhum elegível** (dia 1, só T1): não nasce nada, sem erro e sem travar. Teste: `Pick_NoneEligible_ReturnsMinusOne`.
2. **Só o último é elegível:** ele pode repetir. Teste: `Pick_OnlyLastEligible_Repeats`.
3. **Sem ponto válido fora da tela:** não nasce e tenta no frame seguinte, sem reiniciar o intervalo. Passo manual na Task 2.
4. **Mini boss que fica longe:** é reposicionado igual a qualquer monstro comum. Passo manual na Task 2.
5. **Regenerar mundo ou limpar monstros:** os mini bosses também somem (`HandleClearAllEnemies`). Passo manual na Task 2.

---

### Task 1: Regra do sorteio (Core)

**Files:** Create `Assets/Scripts/Core/AI/MiniBossRules.cs`; Test `Assets/Tests/EditMode/MiniBossRulesTests.cs`

**Interfaces:**
- Produces:
  - `public static class MiniBossRules`
  - `static int Pick(bool[] eligible, int count, int lastIndex, float random01)`: índice sorteado de forma uniforme entre os elegíveis, excluindo `lastIndex` se houver outro elegível; `-1` se nenhum for elegível. Não aloca.
  - `static float NextInterval(float min, float max, float random01)`: `Lerp(min, max, random01)`.

- [ ] **Step 1: Testes:**
  - `Pick_NoneEligible_ReturnsMinusOne`
  - `Pick_ExcludesLast`: elegíveis `{0,1}`, `last = 0` → 1, pra qualquer `random01`
  - `Pick_OnlyLastEligible_Repeats`: elegíveis `{2}`, `last = 2` → 2
  - `Pick_UniformOverEligible`: elegíveis `{0,2,4}`, `last = -1`; `random01` 0 → 0, 0,5 → 2, 0,99 → 4
  - `NextInterval_Range`: (30, 40, 0) → 30; (30, 40, 1) → 40
- [ ] **Step 2:** Recompilar e confirmar o RED (tipos inexistentes).
- [ ] **Step 3:** Implementar.
- [ ] **Step 4:** Rodar a suíte. Esperado: `failed=0` (fora os 3 `BagTests`).
- [ ] **Step 5: Commit** `feat(mini-bosses): regra do sorteio (Core)`.

### Task 2: Spawn dos mini bosses no `WorldPopulationManager`

**Files:** Modify `Assets/Scripts/Core/WorldGeneration/MonsterSpawnSettings.cs` e `Assets/Scripts/World/WorldPopulationManager.cs`

**Interfaces:**
- Consumes: `MiniBossRules` (Task 1), e do D9.4 `HordeElapsed`, `TryPickRingPosition` e `Kite`.
- Produces:
  - `MonsterSpawnSettings.miniBossRoster` (`List<EnemyDefinitionSO>`), `miniBossIntervalMin` = 30 e `miniBossIntervalMax` = 40;
  - `WorldPopulationManager.MiniBossAliveCount`.

- [ ] **Step 1:** Adicionar os campos ao `MonsterSpawnSettings`.
- [ ] **Step 2: Seção de mini bosses no `WorldPopulationManager`:**
  - Lista `aliveMiniBosses` (`List<EnemyController>`), `nextMiniBossAt` e `lastMiniBossIndex`. No `HandleDayStart`, reset com `nextMiniBossAt = HordeElapsed + NextInterval` e `lastMiniBossIndex = -1`.
  - Buffer fixo `bool[] miniBossEligible`, preenchido por `tierPercentsBuffer[def.tier - 1] > 0`. Os percentuais vêm do mesmo `ComputeTierPercentsNonAlloc` do `SpawnOne`: extrair o cálculo pra um método compartilhado `ComputeTierPercentsForNow()`.
  - Quando `HordeElapsed >= nextMiniBossAt`, sorteia com `Pick`:
    - `-1`: agenda o próximo intervalo;
    - sem ponto válido no anel: tenta no próximo frame, sem reagendar;
    - deu certo: `Instantiate` + `RestrictToOpenWorldGraph` + `ForceImmediateAggro`, adiciona na lista, grava o `lastMiniBossIndex` e agenda o próximo.
  - Os mini bosses entram na contagem de setores (`PrepareSectors`), no laço de reposicionamento e no `HandleClearAllEnemies`. Ficam fora da meta da horda (`aliveEnemies.Count < CurrentTarget` não muda). `AliveCount` passa a ser comuns + mini bosses (telemetria).
- [ ] **Step 3:** Recompilar e rodar a suíte. Esperado: `failed=0`.
- [ ] **Step 4: Commit** `feat(mini-bosses): spawn a cada 30-40s pelo tier do dia, sem repetir o ultimo`.

### Task 3: Assets e organização

**Files:** prefabs dos 5; `Assets/Settings/BossSpawnSettings.asset`, `Assets/Settings/MonsterSpawnSettings.asset`; 5 `BossDefinition_*` (apagados); 5 `MiniBossDefinition_*` (novos).

- [ ] **Step 1:** Via `eval`:
  - criar as pastas;
  - `MoveAsset` dos 5 prefabs pra `tier_N` (conferir que o GUID se manteve);
  - criar os `EnemyDefinitionSO` `MiniBossDefinition_<Nome>` com prefab, tier e `weight` 1;
  - preencher o `miniBossRoster`;
  - tirar os 5 do `BossSpawnSettings.roster` e apagar os `BossDefinition_*` deles com `AssetDatabase.DeleteAsset`.
  Retornar a contagem de bosses por tier depois da remoção (esperado: 1 a 3 em cada tier).
- [ ] **Step 2:** `git status` e diff dos settings: só as entradas esperadas mudaram, e os prefabs aparecem como renomeados.
- [ ] **Step 3:** Rodar a suíte. Esperado: `failed=0`.
- [ ] **Step 4: Commit** `chore(mini-bosses): prefabs em MiniBosses/tier_N, definicoes novas, saem do roster de bosses`.
- [ ] **Step 5: Manual (usuário), num dia com T2 ou acima:**
  - um mini boss aparece a cada 30–40 s, sem pausa e sem câmera;
  - nunca nasce o mesmo duas vezes seguidas;
  - vários se acumulam quando você não mata;
  - os bosses de verdade continuam iguais.

### Task 4: Documentação e fechamento

- [ ] **Step 1:** Spec D9, Seção 8: registrar que os 5 viraram mini bosses e que cada tier tem de 1 a 3 bosses. Atualizar a memória do projeto.
- [ ] **Step 2:** Revisão final da branch, corrigir os achados e perguntar antes de merge e push.
