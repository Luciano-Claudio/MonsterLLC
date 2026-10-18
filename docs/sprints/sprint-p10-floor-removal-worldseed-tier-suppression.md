# Sprint P10 — Remoção do Floor System Antigo + RunState Migrado

## Objetivo

Remover por completo o sistema antigo de "andares" (Floor) — morto desde que o mundo único existe, mas nunca deletado — e migrar os 2 valores que ainda viviam fixos em assets compartilhados (`WorldGenerationSettings.worldSeed`) ou nem existiam (Tier Suppression) pro `RunState` (save), onde pertencem de verdade.

Trabalho em 2 levas bem separadas no tempo: a limpeza de Floor saiu na mesma leva de commits da Sprint P4-P6 (antes mesmo do Boss existir); WorldSeed + Tier Suppression saíram bem depois, já com P7-P9 prontos. Documentados juntos aqui porque formam 1 unidade só na definição oficial do P10.

## Sistemas adicionados

### Limpeza do Floor

- **15 arquivos + pasta inteira deletados**: `FloorDefinition`, `FloorManager`, `FloorRegistry`, `FloorTrigger`, `StairRouting` (toda a pasta `Assets/Scripts/Core/Floor/`), `FloorActivationCheck`, `Stair`, `FloorPopulationManager`, `FloorSpawnUtility`, `BossSpawnManager` (antigo), `PopulationConfig` (órfão), `FloorIndicatorUI`, `FloorReorderTest`, `StairRoutingTests`, `FloorActivationCheckTests`.
- **19 arquivos editados** — `ownerFloor` removido de `EnemyController` e ~10 projéteis/summons que só repassavam o campo; `MeleeAttackSlotManager` reescrito (1 `SlotPool` global em vez de `Dictionary<FloorDefinition, SlotPool>`); `GameEvents.OnFloorChanged` → `OnHeroRespawned` (parâmetro `FloorDefinition` nunca era usado de verdade pelo único consumidor real, `PetController`); `HeroController.Respawn()` volta sempre pra origem do mundo (0,0,0) em vez do "Floor térreo".
- **Scene `_TestScene.unity`** — limpeza feita pelo USUÁRIO via Editor UI (não hand-editada), seguindo instruções precisas de quais GameObjects remover (achados via grep no arquivo de cena), com as próprias janelas "Missing Script" da Unity como rede de segurança.

### WorldSeed no RunState

- **`RunState.worldSeed`** — sorteado 1x em `RunCreation.CreateNewRun` (chamado por `MainMenuUI.NewGame()`). Antes vinha fixo de `WorldGenerationSettings.worldSeed` (compartilhado entre TODAS as runs — toda run nova gerava o MESMO mundo).
- **`ChunkTerrainGenerator`/`ChunkStreamer`** passam a ler `MainMenuUI.CurrentRun.worldSeed`. O asset vira só o fallback de teste do Editor (`useTestWorldSeedOverride`, mesmo padrão do `useTestDayOverride`) ou pra quando ainda não existe run nenhuma.

### Tier Suppression

- **`RunState.suppressedTierCount`** (`int`) — sequencial (T1, T2, T3...), um único inteiro representa o estado inteiro.
- **`WorldTierSelector.ComputeTierPercentsNonAlloc`/`ComputeTierGroupPercentsNonAlloc`** ganharam `suppressedTierCount` — zera peso dos primeiros N tiers/grupos antes de normalizar. Afeta monstro comum, boss E o bioma visual do terreno (ajuste feito depois de teste do usuário — primeira versão só afetava spawn).
- **`ShopHandler.BuyNextTierSuppression()`** (`[ContextMenu]`) + `DebugGiveGold` (cheat de teste) — stub no mesmo padrão de `BuyNextTier` (Weapon Tier).
- **`GameEvents.OnClearAllEnemies`** — só o botão de debug "Regenerar Mundo" limpa todos os monstros/bosses vivos (decisão revisada: comprar supressão NÃO limpa nada na hora, só o replacement futuro respeita a nova regra).

## Decisões técnicas

- **`ownerFloor == null` já era "sempre ativo"** — achado real que simplificou a limpeza inteira: não precisou tocar na lógica de ativação de nenhum monstro, só remover o campo morto.
- **Limpeza completa pedida explicitamente pelo usuário** ("Limpeza completa — remover ownerFloor de tudo também"), com instrução de cuidado redobrado ("anote direitinho a remoção pra, caso der erro, a gente saber onde resolver").
- **`suppressedTierCount` como `int` único, não `bool[]`** — a regra é sempre sequencial (nunca escolha livre), um inteiro já representa o estado inteiro sem precisar de array de flags.
- **Fallback hardcoded pro Tier1 corrigido** — `WorldPopulationManager.TryPickEnemyDefinition` caía pro Tier1 quando o Tier sorteado não tem bestiário (hoje só Tier1/Tier2 têm conteúdo); sem correção, suprimir o Tier1 e rolar um Tier vazio ainda assim spawnava Tier1, anulando a compra. Corrigido pro Tier NÃO suprimido mais baixo.
- **Bug real do "spawn parava de funcionar"**: o Dia 1 da tabela é 100% Tier1 — suprimir o Tier1 zerava o ÚNICO peso que existia, `RollMonsterTier` devolvia -1 pra sempre (nenhum monstro nunca mais sorteado), e o terreno ficava sem bioma nenhum. Corrigido: quando a soma dá 0, força 100% no Tier/grupo não suprimido mais baixo, nunca deixa buraco.
- **Timing de `seedOffsetX`/`seedOffsetY`** — `ChunkTerrainGenerator.Awake()` roda ANTES do jogador clicar "Novo Jogo" (`MainMenuUI.CurrentRun` ainda `null`); corrigido recalculando de novo no INÍCIO de `HandleHeroSpawned` (ponto em que nenhum chunk foi pintado ainda).

## Arquivos/classes principais

- `Assets/Scripts/Core/Save/RunState.cs`, `RunCreation.cs` — `worldSeed`, `suppressedTierCount`.
- `Assets/Scripts/Core/WorldGeneration/WorldTierSelector.cs`, `WorldGenerationSettings.cs` — suppression + seed fallback.
- `Assets/Scripts/World/Generation/ChunkTerrainGenerator.cs`, `ChunkStreamer.cs` — `EffectiveWorldSeed`/`SuppressedTierCount`, `RecomputeSeedOffsets`.
- `Assets/Scripts/World/ShopHandler.cs` — `BuyNextTierSuppression`, `DebugGiveGold`.
- `Assets/Scripts/World/WorldPopulationManager.cs`, `WorldBossSpawnManager.cs` — `HandleClearAllEnemies`, fallback de Tier corrigido.
- `Assets/Scripts/Enemies/EnemyController.cs`, `MeleeAttackSlotManager.cs`, `PetController.cs`, `Player/Heroes/HeroController.cs`, `BloodMage.cs`, `Mage.cs` — remoção de `ownerFloor`/`FloorDefinition`.
- `Assets/Scripts/Core/GameEvents.cs` — `OnHeroRespawned`, `OnClearAllEnemies`.

## Eventos adicionados

- `GameEvents.OnHeroRespawned` (`Action`) — substitui `OnFloorChanged`.
- `GameEvents.OnClearAllEnemies` (`Action`).

## Testes executados

Manual (Play Mode) — 2 "Novo Jogo" seguidos gerando mundos diferentes (prova de seed variando), save/load mantendo o mesmo mundo, compra de Tier Suppression funcionando (com o cheat de gold de debug), "Regenerar Mundo" limpando a população, Console sem erro de "Missing Script" depois da limpeza manual da cena.

## Bugs conhecidos

Nenhum em aberto ao fim da sprint.

## Dívida técnica

- **Preços de Tier Suppression (`próximoTier × 100`) e Weapon Tier são placeholder** — balanceamento real fica pra quando a Loja de verdade existir (mesma ressalva já registrada no stub de Weapon Tier).
- **`BuyNextTierSuppression`/`BuyNextTier` continuam `[ContextMenu]`, sem UI real.**

## Próximos passos

Full Migration Test (P11) — checklist cobrindo terreno, Safe Zone, população, Boss, Dia/Noite, Simulation Rings, Game Feel e esta limpeza/migração, tudo junto.
