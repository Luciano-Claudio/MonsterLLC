# Sprint 20 — Floor 1A–2A + Bestiary Batch 1+2 Completo (Floors 1–2, todo o elenco)

## Objetivo

Sprint saiu muito além do planejado. O escopo original (`docs/sprint-20-task-breakdown.md`) era só conteúdo — 10 fichas novas de monstro (reskins em cima de `MeleeEnemyController`/`RangedEnemyController` já existentes) + montar Floor 1A/2A na Scene. Isso foi entregue, mas no meio do caminho o usuário abriu uma discussão sobre performance/pathfinding de monstros que terminou em decisão de arquitetura real: integrar o **A* Pathfinding Project Pro** (Aron Granberg) + **RVO Local Avoidance**, substituindo o movimento por `transform.Translate` cru que todo monstro usava desde a Sprint 16. Isso nunca esteve no escopo original da sprint — foi decisão tomada em cima da hora, com o mesmo precedente já registrado nas Sprints 17/18b/19b (sprint real maior que a sprint planejada, sem renumerar o resto).

## Sistemas adicionados — conteúdo (escopo original)

- **10 fichas novas de Bestiário**, Floors 1–2: Wolf, Bat, Slime Green/Blue, Goblin Raider, Goblin Sapper (Floor 1); Centaur, Minotaur, Gnoll, Spider, Ancient Troll (Floor 2). 8 são reskins puros de `MeleeEnemyController`; Goblin Raider usa `RangedEnemyController`.
- **`EnemyContactDamage.cs`** — dano de contato passivo compartilhado (`OnTriggerStay2D` + cooldown próprio), usado pelos Slimes (permanente) e pelo Goblin Sapper (camada extra, somada à bomba).
- **`SlimeEnemyController.cs`** — única classe que nunca persegue até parar fora do `attackRadius` nem usa `MeleeAttackSlotManager`; só dano de contato, sem `attack`.
- **`GoblinSapperController.cs` + `GoblinSapperBomb.cs`** — ciclo armado (persegue, `Walk_Bomb`) → planta bomba no contato → foge (recarrega) → volta a perseguir; bomba nasce nos pés do jogador, dano só se o jogador ainda estiver na área no frame exato da explosão (Animation Event); auto-explosão ao morrer armado (gizmo próprio, com offset em Y pro centro da área).
- **2º collider por monstro (retrofit, 12 dos 14)** — todo monstro comum (exceto Slimes) ganhou um `CircleCollider2D` trigger extra, separado do `CapsuleCollider2D` físico, só pra receber dano de ataque do herói — zero mudança de código, os scripts de ataque do herói já detectavam via `GetComponent<EnemyController>()` em qualquer collider.
- **`FloorPopulationManager`** dos dois Floors montados na Scene com o elenco completo (8 no Floor 1, 6 no Floor 2).

## Sistemas adicionados — fora do escopo original (A*/RVO)

- **A* Pathfinding Project + RVO integrado nos 14 monstros comuns**: `Seeker`+`AIPath`+`RVOController` em todo prefab, 1 `GridGraph` por Floor (`Grid Graph Floor 1`/`Grid Graph Floor 2`, nomes lidos por `FloorDefinition.astarGraphName`) e 1 `RVOSimulator` global na Scene. `EnemyController` ganhou a ponte entre o movimento próprio do jogo (Blend Tree, `MoveX/MoveY`) e o AIPath (`ai.destination`, `ai.velocity`, `ai.SearchPath()` chamado manualmente por throttle).
- **`FloorPopulationManager` reescrito** — spawn não usa mais `spawnPoints[]` manuais; sorteia ponto caminhável direto no `GridGraph` do próprio Floor (`PathUtilities.GetPointsOnNodes`). Sistema de população também ganhou um modelo de crise: `target` é preenchido instantaneamente ao entrar no Floor; cair até `minimum` reabastece instantaneamente (ignora cooldown); `crisisStreakToEscalate` crises seguidas sobem o patamar de vez (`minimum`/`target` viram o antigo `target`/`maximum`, uma vez só).
- **`Stair.cs` reformulado** — `arrivalPoint` (Transform filho, posição fixa de chegada, independente do pivô do `FloorDefinition`) + `stairTransition` (EasyTransition, teleporte acontece com a tela já coberta, evitando o "pulo" de câmera).
- **`MeleeAttackSlotManager`** — `flankRadius` saiu de `EnemyStats` (por monstro) pra virar campo geral do manager, junto do `maxMeleeNearPlayer`; `RVOController.priority` calculado por dano (`EnemyController.DamageToRvoPriority`, curva própria, sem depender do resto do elenco) — quem dá mais dano tem prioridade maior no desvio; Goblin Sapper usa o dano da bomba, não o de contato, pro próprio cálculo.
- **`FlippedColliderOffsetX.cs`** (novo) — corrige o `offset.x` do collider físico quando o sprite espelha (FlipX) pra cobrir a direção oposta; captura o offset "E" já tunado no prefab e inverte sozinho conforme o `flipX` do `SpriteRenderer` muda.
- **`EnemyProjectile.cs` — `explodesOnImpact`** (novo) — a Sprint 20 (Seção 0, item 2 do próprio breakdown) assumiu que "explode em área ao contato ou na distância máxima" já era comportamento padrão do projétil de monstro, copiando do Rat People pro Goblin Raider sem checar o código. Confirmado em teste manual que **não era** — nem um nem outro explodiam, só aplicavam dano direto. Corrigido nos dois (`GoblinRaiderTorch.prefab` e `RubishProjectile.prefab`).
- **Gizmo de `attackRadius`** — `showAttackRadiusGizmo` + `attackRadiusGizmoOffsetY`, bool serializado em todo `EnemyController`, só editor, pra acelerar o ajuste visual de monstros futuros.

## Decisões técnicas

- **`updatePosition = true` no AIPath, não `false`** — tentativa inicial foi deixar o AIPath só calcular e mover o transform manualmente (`false`), pra evitar o AIPath empurrar o player via física. Isso quebrou a leitura de posição interna do sistema (só sincroniza com `Teleport()`, que por sua vez reseta o histórico de velocidade a cada chamada — testado, e resultava em velocity sempre zero). Voltou pra `true` (jeito documentado de usar a lib) e o empurrão no player foi resolvido do lado certo: `Linear Damping` no Rigidbody2D do herói (decai a velocidade injetada rápido, sem desligar colisão nem física de verdade).
- **`Physics2D.IgnoreCollision` descartado como solução geral** — funcionaria pro empurrão específico, mas a alternativa cogitada antes (desligar a Layer Collision Matrix Player×Enemy) quebrava os triggers de ataque/dano de contato junto (Unity trata colisão e trigger na mesma matriz, não são coisas separadas). `Linear Damping` resolve sem esse efeito colateral.
- **Monstro-vs-monstro: `Rigidbody2D` Kinematic, não Dynamic** — testado nos dois. Dynamic reintroduz o problema original que motivou a migração (física + RVO competindo pelo mesmo espaço, "empurra-empurra"); Kinematic-vs-Kinematic simplesmente não gera resolução de colisão nenhuma entre si (só corpo Dynamic recebe correção), deixando o RVO como único responsável pela separação entre monstros — e ainda resolve o empurrão no player (Dynamic) via o mesmo Linear Damping.
- **`RVOController.radius` por monstro, não uniforme** — primeiro chute foi 0.5 pra todos os 14; corrigido pra metade da largura do próprio `CapsuleCollider2D` físico de cada um (aproximação, RVO usa círculo e o collider é uma cápsula achatada, mas muito mais fiel que um valor fixo).
- **`canSearch`/`autoRepath` não configurável via YAML de prefab** — nessa versão do pacote, `canSearch` é uma property calculada em cima de `autoRepath.mode` (uma classe aninhada), que não é preenchida corretamente quando o componente é escrito à mão em vez de adicionado pela Editor UI. Sem isso, o AIPath nunca buscava path nenhum (`hasPath`/`velocity` ficavam zerados pra sempre) mesmo com tudo mais certo. Corrigido setando `ai.canSearch = true` em código (`EnemyController.Awake()`) e chamando `ai.SearchPath()` manualmente por throttle, em vez de depender do sistema automático de repath.
- **AstarPath/GridGraph não são hand-editáveis** — diferente de prefab/script (declarativo, seguro de escrever à mão), o `AstarData` guarda os graphs como `byte[]` (um ZIP serializado) dentro do componente `AstarPath` — só a Editor UI consegue gerar isso corretamente. Grid Graph, RVOSimulator e o "2D" toggle de cada graph foram configurados pelo usuário direto no Editor, seguindo checklist passado em texto.

## Arquivos/classes principais

- `Assets/Scripts/Enemies/EnemyController.cs` — ponte com `IAstarAI` (`ai.destination`/`velocity`/`SearchPath()`), `DamageToRvoPriority()`, gizmo de `attackRadius`, `SetAiSimulating()` (desliga AIPath/RVOController de verdade — não só `isStopped` — durante pausa/Floor dormente, preservando o custo-zero do Floor Sleep).
- `Assets/Scripts/Enemies/{Slime,GoblinSapper}Controller.cs`, `GoblinSapperBomb.cs`, `EnemyContactDamage.cs` — conteúdo novo da sprint.
- `Assets/Scripts/Enemies/EnemyProjectile.cs` — `explodesOnImpact`/`ApplyImpactExplosion()`.
- `Assets/Scripts/Enemies/FlippedColliderOffsetX.cs` — novo.
- `Assets/Scripts/Enemies/MeleeAttackSlotManager.cs` — `flankRadius` migrado pra cá.
- `Assets/Scripts/World/FloorPopulationManager.cs` — spawn por GridGraph + modelo de crise/escalação.
- `Assets/Scripts/World/Stair.cs` — `arrivalPoint`/`stairTransition`.
- `Assets/Scripts/Core/Floor/FloorDefinition.cs` — `astarGraphName`.
- 14 prefabs de monstro (`Assets/Prefabs/Monstros/Floor{1,2}/*.prefab`) — `Seeker`+`AIPath`+`RVOController`+`FlippedColliderOffsetX`, `Rigidbody2D` Kinematic.
- 3 heróis (`Barbarian`/`Ranger`/`Mage`) — `Linear Damping: 10` no `Rigidbody2D`.

## Bugs conhecidos / corrigidos durante a sprint

- **`Bat.prefab` corrompido (0 bytes)** — um comando de edição em lote (`PowerShell`/stream error) esvaziou o arquivo sem sinalizar erro claro na hora. Restaurado do último commit (`e566df8`) e reaplicados os componentes que faltavam (o 2º collider de dano e o trio A*, que tinham sido adicionados depois daquele commit).
- **`EnemyProjectile` nunca explodia em área** — ver "Sistemas adicionados fora do escopo", `explodesOnImpact`.
- **`MeleeAttackSlotManager` nunca existiu na Scene** — o teto de melee perto do player (`maxMeleeNearPlayer`) tinha fallback seguro pra "sem limite" quando `Instance == null`, e esse era o estado real da Scene o tempo todo; ninguém percebeu porque o fallback não gera erro. Criado o GameObject na Scene.
- **Sprint 20 (Seção 0, item 2) tinha uma suposição não verificada** — "o `EnemyProjectile` já implementa explosão em área" — confirmado errado em teste manual, não em leitura de código na hora (o próprio documento já previa esse risco e como isolar a correção).

## Dívida técnica

- **`FloorPopulationManager` dos dois Floors está em `minimum:50/target:100/maximum:150`, `respawnInterval:1`** — bem acima do default da classe (3/8/12) que a sprint original previa. Decisão explícita do usuário pra manter assim (não é resquício de teste) — só registrando que é uma escala de população bem maior que o resto do MVP até aqui.
- **Escada "subir a partir do Floor 2" sem `arrivalPoint` configurado** — hoje não quebra nada (não existe Floor 3 pra ir, `FloorRegistry.GetNextFloor` retorna null antes de tentar teleportar), mas fica pendente pro dia que um novo Floor for adicionado.
- **`RVOController.radius` por monstro é aproximação** (metade da largura do `CapsuleCollider2D`, não um valor geometricamente exato — RVO usa círculo, o collider é cápsula) — funciona bem na prática, mas pode precisar de ajuste fino visual por monstro.
- Valores `🔢` de balanceamento novos desta sprint (multiplicadores da bomba do Sapper, raios de explosão, `crisisStreakToEscalate`, prioridade RVO por dano) são chutes de primeiro teste, mesmo critério de sempre.

## Próximos passos

Floor 1A e 2A jogáveis de ponta a ponta, com o elenco completo de Floors 1–2 (13 fichas comuns) rodando em cima de pathfinding real + RVO. Deadline 5 fecha aqui (Sprints 17–20 + 18b/19b). Próxima peça é a Deadline 6 (Sprint 21, Druid — Primário + Floor 3A–4A) — o `GridGraph` por Floor e o padrão de retrofit dos 14 monstros (Seeker/AIPath/RVOController/FlippedColliderOffsetX) já ficam prontos pra reaplicar em qualquer Floor/monstro novo sem precisar redescobrir nada disso.
