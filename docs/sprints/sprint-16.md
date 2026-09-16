# Sprint 16 — Teste de Viabilidade de Combate Real + Correção + Pivô Híbrido Final

## Objetivo

Objetivo original: testar na prática se o combate 100% orientado por animação (Telegraph → Hitbox Ativa → Recovery, com Animation Events reais) é viável pra um dev solo, usando 3 monstros reais (Rat, Goblin, Rat People) com Animator de verdade — não placeholder. Na prática, essa sprint acabou não fechando com um teste e uma resposta simples: o resultado do teste disparou uma reabertura de regra estrutural (GDD Seção 22), que por sua vez foi testada de novo, revertida parcialmente, e só fechou de vez depois de mais duas rodadas de decisão e vários bugs reais pegos em revisão. Este relatório documenta a sprint inteira, incluindo as reversões — não só o estado final.

**Por que uma sprint só virou isso tudo:** o combate comum é a peça mais transversal do jogo (todo Floor depende dele) e o "Attack Budget" que a estava limitando desde a Sprint 14 foi removido no meio do caminho — qualquer decisão errada aqui se propagaria pra Bestiary inteiro (99 criaturas + 30 bosses) e pro Boss Framework (Deadline 6). Por isso as idas e vindas, em vez de travar a primeira ideia e seguir.

---

## Linha do tempo — decisões e reversões, na ordem real

### Fase 1 — Teste do combate completo (Telegraph/Hitbox/Recovery real)

- Construídos Rat, Goblin e Rat People com Animator real: Blend Trees 2D Freeform Directional pra `idle`/`walk`/`attack`/`damage`, clipe único pra `die`.
- `lockedTargetPosition` implementado: a posição do player trava no início do ataque, não no frame do hit — habilita esquiva real por reposicionamento.
- Descoberto e corrigido: `animator?.SetBool(...)` explode `MissingComponentException` em prefab sem Animator — o operador `?.` do C# ignora a sobrecarga de `==`/`!=` que o Unity usa pra detectar "fake-null". Toda chamada ao Animator neste projeto passa a usar `if (animator != null)` explícito, nunca `?.` — regra que se manteve até o fim da sprint.
- Regra nova, proposta pelo usuário, testada e adotada: **"receber dano ≠ reagir visualmente ≠ interromper uma ação."** Um monstro atacando nunca tem a animação de `attack` cancelada por dano — só um flash leve. Implementado via topologia do próprio Animator (sem transição `Attack → Damage`) + um branch no `TakeDamage()`.
- Descoberto em teste: o Rat People tem 8 clipes de ataque direcionais (não 2 como a ficha antiga sugeria) — o parâmetro `AttackDirectionType` (blend 1D) que tinha sido desenhado pra esse caso foi descartado, substituído por reaproveitar `MoveX`/`MoveY` (2D Freeform), travados no início do ataque.
- **Resultado do teste:** funcionou tecnicamente (o usuário confirmou esquiva real acontecendo), mas o **custo de produção não é viável em escopo solo** — cada um dos ~130 monstros/bosses exigiria Blend Trees direcionais completos + Animation Events calibrados individualmente.

### Fase 2 — Primeira decisão: modelo simplificado (contato/auto-disparo puro)

- Contingência já registrada no GDD Seção 22 acionada: **Melee vira dano por contato, Ranged vira auto-disparo em alcance**, ambos só com cooldown próprio — sem Telegraph, sem `attack` dedicado, sem Attack Budget.
- Lista de exceções nomeadas que mantêm arquitetura própria por Animation Event: Goblin Sapper, Orc Shaman, Skeleton Rider (depois removido da lista — virou Melee comum), Burning Skull, Serpent, Bicephalous (só o projétil).
- Tabela de 30 bosses recebida do usuário, floor a floor: a maioria vira body-damage puro (perde `attack`); um grupo pequeno mantém complexidade (Mother Slime Green/Blue, Rat People Royalty, Spider Queen, Dark Channeler, Lich, Dragon, Undead Dragon, Divine God).
- GDD (v1.01236 → v1.01237) e Bestiary reescritos ficha a ficha pra esse modelo — bulk edits com `replace_all`, com cuidado extra pra não atropelar as exceções (Dark Channeler, Lich, Dragon, Undead Dragon foram achatados por engano num primeiro bulk edit e precisaram de correção individual).
- Decisão adicional do usuário durante essa fase: **Dark Channeler não spawna mais unidades novas** ao "transformar" Acolyte/Hound/Zealot — é o mesmo GameObject trocando sprites/stats via Animation Event (`possession`). **Chest Mimic** ganha um ciclo de disfarce (ativação/desativação com `E`) e vira o único monstro do jogo com stats calculados por porcentagem (não tabela fixa por andar) — decisão registrada, formato de dados (JSON/XML) ainda não implementado.

### Fase 3 — Sprint 16 (Correção): implementação de código do modelo simplificado

Esta é a parte que efetivamente tem um task breakdown próprio (`docs/sprint-16-correcao-task-breakdown.md`, escrito por outra sessão de IA a partir do resumo desta). Entregue:

- `AttackCooldown` e `PatrolAI` (classes puras, testadas) substituindo o `AttackTiming`/estado de ataque antigo.
- `EnemyController` reescrito: sem estado `Attack`, sem `AttackTrigger`, sem `AttackBudgetManager`. Patrulha aleatória (`idle`/`walk`) antes de detectar o jogador; `idle_combat` depois.
- `AttackBudgetManager`/`AttackBudgetTracker`/`AttackType`/`AttackBudgetIndicatorUI` deletados do projeto, junto com os 4 testes de `AttackBudgetTrackerTests`.
- Morte orientada por Animation Event (`AnimationDieEndEvent`) — o `GameObject` só é destruído e o loot só aparece no fim do clipe `die`, com timeout de segurança (`maxDieDuration`) caso o evento não seja configurado.
- `DirectionUtility` extraído do `HeroController` pra ser compartilhado com os inimigos.
- **Bug pego em revisão, corrigido antes de virar problema:** o breakdown recebido propunha um timeout de segurança da morte via `Coroutine` + `WaitForSeconds` — Coroutine não respeita o `GameplayGate` (decisão já fixada desde a Sprint 13), então o timeout continuaria contando durante uma pausa. Implementado como timer manual dentro do `Update()` gateado, igual todo o resto do projeto já fazia.
- Suíte de testes fechou em 39 verdes (35 anteriores − 4 removidos + 8 novos: `AttackCooldownTests`, `PatrolAITests`).

### Fase 4 — Segunda decisão: reversão parcial pro modelo híbrido final

Depois de testar o modelo puro de contato/auto-disparo na prática (jogando de verdade), o usuário decidiu que **faltava a identidade visual do ataque** — a arte já estava pronta, não fazia sentido não usar. Decisão final, meio-termo entre as duas pontas:

- **Melee:** volta a ter uma animação `attack` real, no próprio cooldown. Um Animation Event no frame do golpe ativa um **trigger direcional** (4 triggers filhos, um por diagonal — o `GameObject` nunca vira, só a animação muda) — só causa dano se o player estiver dentro do trigger certo naquele instante. Nada de posição travada nem cálculo de esquiva por reposicionamento como no modelo completo original.
- **Ranged:** volta a ter uma animação de conjuração real; o projétil só nasce no Animation Event, mirando a posição real do jogador (sem telegraph). Ganha também uma regra de fuga se o jogador chegar perto demais.
- **`AimX`/`AimY` (novo par de parâmetros de Animator):** `MoveX`/`MoveY` continuam sendo só a direção de movimento (pode apontar pra longe do jogador, ex.: Ranged fugindo); `Attack`/`IdleCombat` passam a usar `AimX`/`AimY`, recalculado a cada frame de combate em direção real ao jogador — resolve um bug descoberto em teste (Ranged fugindo mirava e atirava pro canto errado, o dano saía certo mas a pose visual mirava pra longe do alvo).
- **Dano de contato passivo removido de vez** (chegou a coexistir com o golpe real por uma iteração e ficou confuso testando) — única exceção permanente: **Slimes** (comuns e Mother Slime Green/Blue), que ficam só no contato pra sempre.
- GDD (v1.01237 → v1.01238) e Bestiary reescritos de novo — 74 fichas de Melee tiveram o texto de "dano por contato" trocado pelo mecanismo de golpe real; ~90 listas de animação ganharam `attack` de volta; a regra passou a valer também pra maioria dos 30 bosses (perderam só a complexidade extra que tinham — telegraph, área, múltiplos hits — não o `attack` em si).

### Fase 5 — Bugs de arquitetura pegos em revisão, nesta mesma rodada

- **`IsMoving` grudado em `true` por até 4 segundos:** o código adiava a entrada em combate usando o timer da própria fase de patrulha do `PatrolAI` (até `maxWalkDuration`), em vez de deixar só o Exit Time do Animator proteger o `idle`. `PatrolAI` foi simplificado (perdeu `RequestCombatTransition`/`ShouldEnterCombat`) e a detecção do jogador agora vira combate imediatamente no código — a suavidade visual continua garantida, só que pelo mecanismo certo (Exit Time, rápido, do tamanho do clipe).
- **Monstro "andando parado":** `PatrolAI` conta a fase "Walking" por um tempo aleatório fixo, sem saber a distância real até o alvo — se o monstro chegava no alvo de patrulha antes desse tempo acabar, `IsMoving` continuava `true` (Animator preso em `Walk`) até o timer estourar. Corrigido checando a distância real ao alvo, não só a fase.
- **Movimento deslizando durante transições com Exit Time:** o código já translada o transform no mesmo frame que marca a intenção (`IsMoving = true`), sem esperar o Animator realmente trocar de estado. Criado `AnimatorStateCheck.IsInState()` (utilitário compartilhado, pensado também pro Herói quando a animação dele for wireada) — `EnemyController.MoveInDirection()` só translada de verdade quando o Animator já está no estado `Walk`.
- **Erro de compilação (`CS0034`):** `player.position - (Vector2)transform.position` mistura `Vector3` com `Vector2` explicitamente castado só de um lado, o que deixa o operador `-` ambíguo pro compilador. Corrigido removendo o cast, deixando a conversão implícita `Vector3 → Vector2` cuidar disso (mesmo padrão já usado no resto do arquivo).

### Fase 6 — Ferramenta de produção: `MonsterAnimationGeneratorWindow`

Com mais de 100 monstros pela frente (~2000+ clipes de animação no total), o usuário construiu uma ferramenta de Editor (`Assets/Editor/MonsterAnimationTools/`) que:

- Gera os clipes direcionais de cada monstro a partir de sprite sheets (Idle/Walk/Attack Diagonal/Attack Orthogonal quando Ranged/Damage/Die), fatiados em 4 ou 8 direções.
- Cria/atualiza um `AnimatorOverrideController` por monstro, remapeando os clipes gerados sobre 2 Animators base compartilhados — **`Base_Melee`/`Base_Ranged`** (`Assets/Animation/Base/`) — por nome normalizado (com alias `attack↔atk`, `damage↔dmg`).
- Gera sozinho o `idle_combat` (pose parada em combate) a partir do primeiro frame do Idle de cada direção, sem precisar de arte nova.
- **Ajuste pedido depois:** a ferramenta passou a inserir sozinha os 3 Animation Events padrão em todo clipe **recém-criado** — `AnimationHitEvent` em 50% do clipe de `attack` (só um ponto de partida, o frame exato do golpe continua ajuste manual), `AnimationAttackEndEvent` no fim do `attack`, `AnimationDieEndEvent` no fim do `die`. Nunca sobrescreve um clipe já existente, pra não apagar ajuste manual feito depois da geração.
- Consequência importante: a configuração de Animator (parâmetros/estados/transições) que antes era feita **por monstro** passou a ser feita **uma vez só**, nos 2 Animators base — inclusive `AimX`/`AimY`, adicionados e ligados aos Blend Trees de `Attack`/`IdleCombat` nos dois.

### Fase 7 — Limite de Melee perto do jogador ("flanco")

Com hordas grandes, todo Melee tentando ficar dentro do próprio `attackRadius` ao mesmo tempo lotaria o corpo a corpo e ficaria ilegível. Implementado:

- `SlotPool` (classe pura, testada) + `MeleeAttackSlotManager` (singleton, teto configurável no Inspector, padrão **12**) — quantos Melee podem estar em alcance de contato do jogador ao mesmo tempo.
- `EnemyStats.flankRadius` (novo, por monstro) — o raio do anel onde quem não consegue vaga fica **flanqueando**: reaproveita o próprio `PatrolAI` (mesma alternância com timers aleatórios) pra andar na borda do anel e parar em `idle_combat`, tentando pegar vaga de novo a cada frame.
- **Nenhuma mudança de Animator precisou ser feita** — `Walk`/`IdleCombat`/`AimX`/`AimY` já existentes cobrem 100% do visual do flanco.
- **Dois bugs reais pegos em revisão pela outra sessão de IA (que já tinha visto o `AttackBudgetManager` cometer os mesmos dois erros nas Sprints 14/15):**
  1. **Escopo global em vez de por Floor** — o `SlotPool` tinha nascido único pra Scene inteira. Um Melee com vaga reservada cujo Floor dorme (Floor Sleep) nunca mais roda `Update()`, nunca libera a vaga, e ela ficaria presa pra sempre roubando capacidade do Floor realmente ativo — exatamente o bug que o `AttackBudgetManager` teve antes do fix da Sprint 15. Corrigido replicando o mesmo padrão já estabelecido (`Dictionary<FloorDefinition, SlotPool>`, criado sob demanda).
  2. **A vaga só liberava na morte** (essa parte, na verdade, já estava certa desde o início — liberava em `Die()`, não esperando o `AnimationDieEndEvent`). O bug de verdade, achado pelo próprio usuário depois: a vaga **nunca liberava se o jogador simplesmente se afastasse correndo** — quem pegasse vaga primeiro ficava com ela pra sempre perseguindo o mapa inteiro, enquanto monstros de verdade perto agora não conseguiam nenhuma. Corrigido: a vaga também libera se o próprio dono cair fora do `flankRadius`.

### Fase 8 — Resolução do Spectre (última pendência do Bestiário)

O Spectre (boss, Andar 5) tinha ficado como pendência 🟡 desde a rodada anterior — o design original dele era minimalista (3 animações no total, `idle` dobrando como `walk`), e dar um `attack` de volta pra ele quebraria essa economia deliberada. Decisão final: ele ganha `attack` real igual a qualquer Melee comum (só o `idle` continua dobrando como `walk`, mesmo clipe nos dois slots do Override Controller), mais uma mecânica própria — cria uma **superfície de gelo persistente no chão** se o golpe conectar. Registrado como o primeiro caso de um padrão que vai se repetir (Dragon/Undead Dragon/Dragon Hatchling com fogo, e a Ultimate do Mage) — sem sistema genérico ainda, cada caso nasce isolado quando o conteúdo correspondente chegar; e uma observação separada, também pro futuro: entidades vão precisar de uma camada visual de efeitos de status (congelado/fogo/cura) na frente do sprite base, ainda não desenhada.

### Fase 9 — Re-validação do Floor Sleep e o bug do `ownerFloor`

Primeiro teste manual (subir a escada sendo perseguido por 3 monstros) **falhou**: os monstros não perdiam o alvo, tentavam atravessar o mapa inteiro pra chegar no Floor 2. Causa raiz: `FloorActivationCheck.IsActive()` trata `ownerFloor == null` como "sempre ativo" — um fallback de compatibilidade proposital pra objetos sem Floor dono, que sem querer também mascara silenciosamente um monstro que deveria ter `ownerFloor` preenchido e ficou sem, por esquecimento no Inspector. Não gera erro nem warning — só o monstro nunca dorme. Preenchido o campo nos prefabs, o segundo teste (incluindo o caso de borda "matar um monstro no instante exato da troca de Floor") passou sem nenhuma regressão: dano e drop de loot aconteceram antes da troca completar, o loot persistiu no Floor antigo e foi coletável ao voltar.

### Fase 10 — Housekeeping

- Um artefato de auto-recuperação do Unity (`Assets/_Recovery/`, gerado sozinho pelo Editor) entrou sem querer num commit — removido do controle de versão e adicionado ao `.gitignore`.
- `docs/Projeto_Torre_Plano_Producao_v7.md` passou por 4 rodadas de correção ao longo da sprint, cada uma registrada como uma "Nota" numerada no topo do documento — a versão anterior (v6) descrevia o modelo de combate completo original, completamente superado.

---

## Sistemas adicionados

- **`AttackCooldown`** (`Assets/Scripts/Core/Combat/`) — cooldown genérico, puro, testado.
- **`PatrolAI`** (`Assets/Scripts/Core/AI/`) — alternância idle/walk de patrulha com timers aleatórios; reaproveitada depois pro flanco do Melee.
- **`AnimatorStateCheck`** (`Assets/Scripts/Core/`) — verifica se um Animator está de fato num estado, usado pra gatear movimento real.
- **`SlotPool`** (`Assets/Scripts/Core/Combat/`) — contador genérico de vagas com teto, puro, testado.
- **`MeleeAttackSlotManager`** — limite de Melee em contato simultâneo com o jogador, escopado por Floor.
- **`MonsterAnimationGeneratorWindow`** (`Assets/Editor/MonsterAnimationTools/`) — gera clipes direcionais + Override Controller por monstro a partir de sprite sheets, sobre os 2 Animators base compartilhados.
- **`Base_Melee`/`Base_Ranged`** (`Assets/Animation/Base/`) — Animators compartilhados por todos os monstros comuns (e a maioria dos bosses), configurados uma vez só.
- **`DirectionUtility`** — extraído do `HeroController`, compartilhado com inimigos.

## Decisões técnicas

Cobertas em detalhe na Linha do Tempo acima. Resumo das que mais importam pra sprints futuras:

- **Modelo de combate final (GDD v1.01238):** `attack` real com Animation Event pra 100% do Melee/Ranged comum e a maioria dos bosses, sem dano de contato passivo (exceção permanente: Slimes). Attack Budget continua removido — não voltou.
- **`AimX`/`AimY` separado de `MoveX`/`MoveY`** — regra nova pra qualquer Animator de monstro (e, no futuro, do Herói).
- **Flanco (`MeleeAttackSlotManager`) é um limitador novo, diferente da população** — quantos Melee ficam em contato, não quantos monstros existem.
- **Todo limitador por-instância-de-jogo (Attack Budget original, e agora o flanco) precisa ser escopado por Floor**, nunca um pool global pra Scene inteira — lição repetida 2 vezes agora (Sprint 15 e aqui).
- **`ownerFloor` vazio falha em aberto (silenciosamente sempre ativo), não em fechado** — checklist manual necessário nos próximos Batches de Bestiary pra garantir que todo monstro novo tem `Owner Floor` preenchido.
- **Chão com condição negativa após ataque** (Spectre) e **stats/loot de monstro via JSON/XML** são decisões registradas pro futuro, não implementadas nesta sprint.

## Arquivos/classes principais

- `Assets/Scripts/Enemies/EnemyController.cs` — reescrito 3 vezes ao longo da sprint (completo → simplificado → híbrido).
- `Assets/Scripts/Enemies/MeleeEnemyController.cs` — golpe real por trigger direcional + flanco.
- `Assets/Scripts/Enemies/RangedEnemyController.cs` — conjuração real + fuga + `AimX`/`AimY`.
- `Assets/Scripts/Enemies/EnemyStats.cs` — `attackDamage`, `attackAnimationCooldown`, `flankRadius`; `damage`/`attackCooldown` (contato) removidos.
- `Assets/Scripts/Enemies/MeleeAttackSlotManager.cs`, `Assets/Scripts/Core/Combat/SlotPool.cs`, `Assets/Scripts/Core/AI/PatrolAI.cs`, `Assets/Scripts/Core/Combat/AttackCooldown.cs`, `Assets/Scripts/Core/AnimatorStateCheck.cs`.
- `Assets/Scripts/DayCycle/DayTimer.cs` — passou a respeitar `GameplayGate` (achado à parte, não relacionado ao combate, mas corrigido nesta sprint).
- Deletados: `AttackTiming.cs`, `AttackBudgetManager.cs`, `AttackBudgetTracker.cs`, `AttackType.cs`, `AttackBudgetIndicatorUI.cs`, `AttackBudgetTrackerTests.cs`.
- `docs/gdd/index.md` — passou por 2 rewrites completos da Seção 22 (v1.01237 e v1.01238), mais ajustes nas Seções 13/14/23/50.
- `docs/gdd/bestiary.md` — passou por 2 rewrites completos (ficha a ficha, ~130 criaturas cada vez).
- `docs/Projeto_Torre_Plano_Producao_v7.md` — 4 rodadas de correção registradas como notas numeradas.

## Eventos adicionados

Nenhum evento novo em `GameEvents`.

## Testes executados

- **Automatizado (EditMode):** suíte fechou em **39 testes verdes** — `AttackCooldownTests` (4), `PatrolAITests` (4, reduzido de 4 depois de simplificar a API), `SlotPoolTests` (5), removidos os 4 de `AttackBudgetTrackerTests`.
- **Manual (Play Mode), confirmado pelo usuário:**
  - Patrulha idle/walk alternando sozinha, sem cortar `idle` no meio, mesmo com o player detectado no meio do clipe.
  - Combate real: Melee gruda e bate no trigger certo (testado nas 4 direções diagonais); Ranged mantém distância, foge se o player chegar perto, mira certo mesmo fugindo.
  - Flanco: hordas grandes formam fila visível, vagas trocam de dono corretamente quando alguém morre ou quando o player se afasta.
  - Morte só destrói o `GameObject`/dropa loot no fim do clipe `die`; timeout de segurança validado com o evento desconfigurado de propósito.
  - Floor Sleep: monstro engajado congela exatamente onde estava ao trocar de Floor (incluindo mid-`attack`, sem o timeout de segurança forçar o fim), retoma exatamente de onde parou ao voltar; loot persiste e é coletável; caso de borda de matar um monstro no instante exato da troca de Floor validado sem regressão.

## Bugs conhecidos

Nenhum em aberto ao final da sprint. Todos os encontrados durante o processo (`IsMoving` grudado, monstro andando parado, deslizar em transição, `CS0034`, escopo global do flanco, vaga nunca liberando por distância, `ownerFloor` vazio, artefato `_Recovery` commitado por engano) foram corrigidos dentro da própria sprint.

## Dívida técnica

- **`flankRadius` precisa ficar maior que `attackRadius` em cada monstro configurado manualmente** — não há validação automática disso ainda; se configurado errado, o monstro nunca chega a flanquear (entra direto em contato, ignorando o teto).
- **Formato de dados de monstro (JSON/XML)** — decisão registrada (GDD Seção 50), zero código ainda. Fica pra quando a produção de conteúdo do Bestiary começar de verdade (Deadline 5+, Sprint 20).
- **"Chão com condição negativa" (Spectre, e depois Dragon/Undead Dragon/Dragon Hatchling/Ultimate do Mage)** — cada caso nasce isolado por enquanto, sem sistema genérico. Vale reconsiderar unificar quando houver 2-3 exemplos reais construídos.
- **Camada visual de efeitos de status** (congelado/fogo/cura, na frente do sprite base) — ainda não desenhada, provavelmente um GameObject filho dedicado por entidade.
- **`HeroController` ainda não tem Animator wireado** — o `AnimatorStateCheck` já existe e está pronto pra ser usado lá quando isso acontecer (comentário já deixado no código nesse sentido).

## Próximos passos

Deadline 4 fechada. A Deadline 5 (Heróis I — Ranger/Mage — + início real de Floor Content) pode começar. Quando a produção de conteúdo do Bestiary chegar (Sprint 20, Batch 1+2 — Floors 1-2 completos), a `MonsterAnimationGeneratorWindow` já está pronta pra acelerar a criação de clipes/Override Controllers, e a decisão de dados via JSON/XML deve ser revisitada nesse ponto, não antes.
