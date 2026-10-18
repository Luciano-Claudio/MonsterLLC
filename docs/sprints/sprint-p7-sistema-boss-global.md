# Sprint P7 — Sistema de Boss Global

## Objetivo

Substituir o `BossSpawnManager` antigo (rotação simples, acoplado a Floor, intervalo fixo de 10s) por um sistema de Boss pro mundo único: Boss Timer decide QUANDO, o Tier de maior peso na distribuição atual (nunca um roll) decide QUAL, nenhum boss repete no mesmo dia, replacement por distância sem resetar o timer principal.

## Sistemas adicionados

- **`BossDefinitionSO`** — paralelo a `EnemyDefinitionSO`, mas sem `weight`: decisão do usuário, quando o Tier sorteado tem mais de 1 boss possível, o sorteio entre os ainda não usados hoje é uniforme (todos a mesma chance), sem peso configurável.
- **`BossSpawnSettings`** (SO) — roster + `bossSpawnInterval` (30s, substitui os 10s fixos), `bossDespawnDistance` (maior que o do monstro comum de propósito — boss é mais raro/importante, mais folga antes de desistir), anel de spawn/replacement próprio, `bossReplacementCooldown` (proteção contra abuso de reroll andando na borda do despawn).
- **`WorldBossSpawnManager`** (substitui `BossSpawnManager`) — mesma arquitetura do monstro comum, com as regras próprias:
  - **Boss Timer** — cada intervalo completo tenta adicionar 1 boss; bosses anteriores continuam vivos, podem acumular (sem teto de boss vivo simultâneo, decisão do usuário).
  - **Boss Tier = Tier predominante** (`TryPickBossDefinition`) — reaproveita `WorldTierSelector.ComputeTierPercentsNonAlloc`, mas pega o Tier de MAIOR peso em vez de sortear (`PickNextStrongestTier`). Se a pool desse Tier esgotou, avança pro próximo Tier por ordem de predominância.
  - **`UsedBossIdsToday`** — nenhum boss repete dentro do mesmo giro de um Tier. Ao esgotar TODOS os `BossDefinitionSO` de um Tier específico, esse Tier reseta sozinho (`TryPickUnusedBossForTier`) — decisão do usuário: continuar girando bosses se sobrar tempo de dia, em vez de travar até o próximo `ActualDay`. Reset diário via `GameEvents.OnDayStart` continua existindo por cima.
  - **Distance Despawn + replacement** — nunca é morte, repõe a vaga na hora sem resetar o Boss Timer. Se o jogador MATA o boss (não despawn por distância), não existe replacement automático — só o próximo Boss Timer repõe.
  - Todo boss nasce já em combate (`EnemyController.SetIsBoss(true)` + `ForceImmediateAggro()`), sem fase de patrulha/idle.
- **Roster inicial**: 7 `BossDefinitionSO` — 3 Tier1 (Goblin King, Mother Slime Blue/Green) + 4 Tier2 (Centaur King, Rat People Royalty, Spider Queen, Werewolf), reaproveitando prefabs já existentes em `Assets/Prefabs/Bosses/`.

## Decisões técnicas

- **Sorteio uniforme dentro do Tier, sem peso por boss** — escolha explícita do usuário entre as alternativas apresentadas, diferente do `weight` do monstro comum.
- **Reset por-Tier em vez de só reset diário** — decisão do usuário depois de entender a trava original (Tier esgotado ficava mudo até o próximo `ActualDay`): continuar girando é melhor gameplay do que silêncio.
- **Sem teto de boss vivo simultâneo** — mesmo depois de alertado sobre o risco de acúmulo ilimitado ao longo de vários dias (nada recicla a identidade de um boss morto/despawnado de volta pro pool do dia), o usuário confirmou manter sem teto — o único freio é a exaustão diária do roster dentro de 1 dia.

## Arquivos/classes principais

- `Assets/Scripts/Core/WorldGeneration/BossDefinitionSO.cs`, `BossSpawnSettings.cs` — novos.
- `Assets/Scripts/World/WorldBossSpawnManager.cs` — novo, lógica completa do Boss Timer/Tier/Identidade/Despawn.
- `Assets/Settings/BossDefinitions/` — 7 assets.
- `Assets/Settings/BossSpawnSettings.asset`.

## Eventos adicionados

Nenhum nesta sprint — `GameEvents.OnBossSpawned` veio depois, na sprint de Game Feel/Camera.

## Testes executados

Manual (Play Mode) — boss nascendo no intervalo certo, Tier batendo com a predominância, identidade não repetindo no mesmo dia, reset por-Tier funcionando, despawn por distância repondo sem resetar o timer principal.

## Bugs conhecidos

Nenhum reportado nesta sprint especificamente — os achados de câmera/Game Feel relacionados ao boss (corte seco na troca de Follow, PositionDamping=0) vieram na sprint seguinte, quando o reveal cinematográfico foi desenhado.

## Dívida técnica

- Nenhuma nova nesta sprint — a limpeza do `BossSpawnManager` antigo e de todo o sistema de Floor (que este sistema substitui) foi tratada na mesma leva de commits, documentada como unidade própria na Sprint P10.

## Próximos passos

Commit e push de tudo (Fase A + limpeza de Floor + Boss) pro `main`. Depois: Ciclo Dia/Noite real (não estava no escopo oficial do P8, mas foi puxado antes dele por decisão do usuário).
