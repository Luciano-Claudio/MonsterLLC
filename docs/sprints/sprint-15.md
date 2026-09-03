# Sprint 15 — Floor Sleep/Activation v1

## Objetivo

Com os 3 Floors existentes (Ground/Floor 1/Floor 2), só o Current Floor executa simulação completa — inimigos e população dos outros congelam exatamente onde estavam, sem perder estado. Peça de maior risco técnico do projeto (sinalizada desde a Etapa 1 da análise de produção). Escopo v1, validado com 3 Floors — a prova em escala real (10 Floors + térreo) é a Sprint 32 (Deadline 8).

## Sistemas adicionados

- **`FloorActivationCheck`** (lógica pura) — `IsActive(ownerFloor, currentFloor)`; `ownerFloor == null` sempre ativo, preservando objetos sem Floor dono (Player, Vendor, etc.).
- **`EnemyController.ownerFloor`** — gateado no topo do `Update()`; estado (posição, HP, timer de ataque em andamento) congela exatamente onde parou.
- **`FloorPopulationManager.ownerFloor`** — mesma gate; `respawnTimer` congela junto (efeito colateral correto de onde o gate foi colocado).
- **`PopulationManager_Floor2`** — segunda instância de população, independente do Floor 1.

## Decisões técnicas

- **Regra central aplicada literalmente (GDD Seção 24): Floor existir ≠ Floor simular.** Nenhum mecanismo de "salvar e restaurar" estado foi construído — como nada roda `Update()` num Floor dormindo, o estado fica congelado no exato ponto em que parou, de graça.
- **Bug real corrigido antes de testar — `AttackBudgetManager` era um singleton global único, criado na Sprint 14 antes do Floor Sleep existir.** A GDD Seção 24 (Combat Scope) é explícita: *"Attack Budget considera apenas as ameaças do Floor atual — os budgets Melee/Ranged não se tornam um pool global somando monstros de todos os Floors da Scene."* Sem o fix, um inimigo congelado em pleno ataque ao sair do Floor prenderia seu slot reservado pra sempre (só a transição Recovery→Idle libera, e isso não roda mais dormindo), roubando permanentemente capacidade do Floor realmente ativo — exatamente o cenário que o próprio passo 2 do teste manual desta sprint monta (deixar um inimigo em telegraph, subir de Floor). Corrigido fazendo `AttackBudgetManager` manter um `AttackBudgetTracker` **por Floor** (dicionário lazy, chave `FloorDefinition`) em vez de um único — `AttackBudgetTracker.cs` (lógica pura, testada na Sprint 14) não mudou nada, só o wrapper `MonoBehaviour` passou a indexar por Floor.
- **Bug real corrigido, fora do escopo original do texto — `EnemyProjectile` não tinha nenhum gating de Floor.** Um projétil em voo no momento em que o Floor dorme continuaria se movendo e expirando "fora" do Floor congelado, violando o próprio critério de pronto da sprint ("sem se mover"). Ganhou `ownerFloor`, preenchido pelo `RangedEnemyController` ao instanciar.
- **Fix pedido pelo usuário em teste manual — desvio aleatório no spawn.** Inimigos nascidos exatamente no mesmo `spawnPoint` (ou parados fora do `observationRadius`, nunca se movendo) ficavam empilhados perfeitamente sobrepostos — o `Rigidbody2D` (Sprint 14) só separa corpos que estão de fato se movendo, então um grupo parado nunca se desempilhava sozinho. `SpawnOne()` agora aplica um offset aleatório (`0` a `2` em X/Y) sobre a posição do `spawnPoint`.

## Arquivos/classes principais

- `Assets/Scripts/Core/FloorActivationCheck.cs`.
- `Assets/Scripts/Core/AttackBudgetManager.cs` — reescrito pra tracker por Floor.
- `Assets/Scripts/Enemies/EnemyController.cs`, `EnemyProjectile.cs`, `RangedEnemyController.cs` — `ownerFloor`.
- `Assets/Scripts/World/FloorPopulationManager.cs` — `ownerFloor`, desvio de spawn.
- `Assets/Scripts/UI/AttackBudgetIndicatorUI.cs` — mostra o budget do Floor atual, não mais um total global.

## Eventos adicionados

Nenhum evento novo em `GameEvents`.

## Testes executados

- **Automatizado (EditMode):** `FloorActivationCheckTests` (3 testes: mesmo Floor, Floor diferente, sem Floor dono) — passou, junto com os 32 testes totais anteriores (35 no total, confirmado pelo usuário no Test Runner).
- **Manual (Play Mode):** os 8 passos do teste manual da sprint confirmados pelo usuário — população cresce independente por Floor; inimigo deixado em telegraph no Floor 1 continuou exatamente de onde parou ao voltar (não reiniciou, não pulou pro hit); Floor 2 manteve os 3 slots de Melee completos mesmo com o Floor 1 "preso" em telegraph (confirma o fix do budget por Floor); contagem de população do Floor 1 idêntica antes/depois de sair; ciclos rápidos Floor 1 ↔ Floor 2 não geraram reset perceptível de população nem de HP.
- **Regressão pega em teste manual, corrigida:** inimigos parados (fora do raio de observação) ficavam empilhados no mesmo `spawnPoint" — resolvido com o desvio aleatório de spawn.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **Validação em escala real (10 Floors + térreo, com profiling) é a Sprint 32 (Deadline 8), não esta sprint** — se a arquitetura precisar de ajuste ao escalar, é esperado, não sinal de que esta sprint saiu errada (já registrado no texto original).
- **`LootDrop` não respeita Floor Sleep** — continua fazendo checagem de proximidade a cada frame mesmo em Floors dormindo. Não é bug funcional (o jogador não pode estar fisicamente perto de loot num Floor onde não está), só um ciclo de CPU não suspenso que o critério de pronto desta sprint não cobria explicitamente ("inimigos e população", não loot). Sem sprint atribuída.
- Herdada (Sprint 14): reação dinâmica de população (aumentar frequência de reposição matando rápido) não implementada; spawn ainda sem checagem automática de câmera/distância.
- Herdada (Sprint 13): `TargetPosition` não trava no início do telegraph (GDD Seção 22) — sem sprint atribuída.
- Pendência formal da própria GDD (Seção 24, não desta sprint): comportamento exato de efeitos de combate persistentes (áreas, DoT) ao trocar de Floor enquanto o efeito ainda está ativo — GDD marca como 🟡 pendente, depende de como Floor Sleep trata timers/efeitos (Seção 53).

## Próximos passos

Com Floor Sleep v1 validado e o Attack Budget corretamente escopado por Floor, a Sprint 16 (Bestiary Batch 1, Floors 1-2) tem uma base sólida pra colocar conteúdo real de monstros — 2-3 tipos distintos, não clones do protótipo — sem risco de os novos tipos herdarem os mesmos bugs de escopo cross-Floor que esta sprint corrigiu.
