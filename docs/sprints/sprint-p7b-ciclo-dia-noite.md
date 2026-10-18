# Sprint P7b — Ciclo Dia/Noite Real

## Objetivo

Virar o `DayTimer` (até aqui um cronômetro único) em fases Dia/Noite de verdade (50%/50% da duração do dia), e fazer spawn de monstro/boss trocar de `dayWeights` pra `nightWeights` na fase certa — lógica de gameplay e visual numa rodada só, decisão do usuário ("fechar a feature de ponta a ponta").

Sem número de Sprint/Deadline oficial no plano de produção (`Projeto_Torre_Plano_Producao_v7.md`) — o `DayTimer` como cronômetro único já vinha de uma sprint numerada bem anterior, e P8 ali é oficialmente "Simulation Rings + Limpeza de Combat Scope", não Dia/Noite. Por isso o sufixo "b" (mesmo critério de 18b/19b/27b/30b: trabalho inserido entre sprints numeradas, sem deslocar as seguintes) em vez de um `Px` novo inventado.

## Sistemas adicionados

- **`DayTimer.IsNight`** — fase calculada a partir do tempo decorrido vs. a duração REAL do dia corrente (`currentDayDuration`, guardada a cada `ResetForNewDay(duration)` — cobre duração customizada, não só `defaultDayDuration`). Sempre 50% Dia / 50% Noite, nunca quebra essa proporção mesmo com duração customizada. Todo dia novo começa em Dia.
- **`GameEvents.OnDayNightPhaseChanged`** — dispara só na troca de fase (edge trigger), nunca todo frame.
- **`WorldTierSelector.ComputeTierPercentsNonAlloc` ganhou `isNight`** (default `false`, aditivo) — troca `dayWeights` por `nightWeights`. `ComputeTierGroupPercentsNonAlloc` (terreno/bioma) NÃO ganhou esse parâmetro de propósito — decisão já registrada: bioma nunca oscila a cada troca de fase, só avança com dia/distância reais.
- **`WorldPopulationManager`/`WorldBossSpawnManager`** leem `DayTimer.Instance.IsNight` e passam pro cálculo de Tier — monstro comum e boss já reagem à fase real (Tiers superiores ganham mais peso à noite, conforme `nightWeights` de cada Dia).
- **`DayNightLightController`** (novo) — crossfade suave (smoothstep, ~5s configurável) de cor/intensidade de um `Light2D` Global do URP 2D Renderer, disparado por `OnDayNightPhaseChanged`. Respeita a pausa do jogo.

## Decisões técnicas

- **Terreno nunca lê `isNight`** — reafirma decisão anterior (Sprint P4-P6): bioma não deve oscilar a cada ciclo.
- **Sem número de Sprint oficial nos comentários do código** — decisão explícita do usuário depois de eu errar e chutar "Sprint P8" (que já é outra coisa no plano de produção real). Melhor não citar nenhum `Px` do que citar um errado.

## Arquivos/classes principais

- `Assets/Scripts/DayCycle/DayTimer.cs` — `IsNight`, `currentDayDuration`, `UpdateDayNightPhase`.
- `Assets/Scripts/DayCycle/DayNightLightController.cs` — novo.
- `Assets/Scripts/Core/GameEvents.cs` — `OnDayNightPhaseChanged`.
- `Assets/Scripts/Core/WorldGeneration/WorldTierSelector.cs` — parâmetro `isNight`.
- `Assets/Scripts/World/WorldPopulationManager.cs`, `WorldBossSpawnManager.cs` — leem `DayTimer.Instance.IsNight`.

## Eventos adicionados

- `GameEvents.OnDayNightPhaseChanged` (`Action<bool>`).

## Testes executados

Manual (Play Mode) — mundo escurecendo gradualmente passada a metade do dia, Tiers mais altos aparecendo à noite mesmo perto da origem, pausa não deixando o crossfade continuar rodando.

## Bugs conhecidos

- **[CORRIGIDO] Achado real de pausa**: a pausa deste projeto é só uma flag (`TimeManager.IsPaused`), `Time.timeScale` continua 1 — sem o gate explícito `GameplayGate.IsActive` em `DayNightLightController.Update()`, o crossfade continuaria rodando com o menu de pausa/loja aberto.

## Dívida técnica

Nenhuma nova registrada nesta sprint.

## Próximos passos

Discussão de "game feel/juice" usando mecânicas do próprio Cinemachine (shake/zoom em hit, reveal de boss) — vira a próxima rodada, antes de voltar pro P8 (Simulation Rings) oficial.
