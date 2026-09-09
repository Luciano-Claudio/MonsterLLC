# Sprint 16 — Task Breakdown — Teste de Viabilidade de Combate Real

> Substitui o escopo original ("Bestiary Batch 1 — conteúdo real de inimigos"). O número de monstros entregues (2-3) não muda; o que muda é o objetivo: esta sprint decide se o Bestiário inteiro (99 fichas) é produzido no formato completo (Telegraph/Hitbox/Recovery + Animator real) ou na contingência simplificada já registrada no GDD Seção 22 (Melee por contato / Ranged simples / Explosivo). Dependência: Sprint 15 (concluída).

## Objetivo

Ter 3 monstros reais (Rat, Goblin, Rat People) com Animator Controllers de verdade, jogáveis nos Floors 1-2 já existentes, e sair da sprint com uma decisão registrada — não uma impressão — sobre qual arquitetura de combate comum o resto do Bestiário vai seguir.

## Por que estes 3 monstros

- **Rat** (Andar 1, Melee) — baseline. Já tem comportamento validado desde a Sprint 8; a única variável nova é Animator real substituindo o placeholder.
- **Goblin** (Andar 2, Melee) — segundo ponto de dado com asset de origem diferente (`Minifantasy_Creatures_v3.3_Commercial_Version` vs. `Minifantasy_Monster_Creatures_v1.0` do Rat), pra não tirar conclusão de um único sprite sheet.
- **Rat People** (Andar 2, Ranged) — o caso mais complexo do Bestiário inteiro: variantes direcionais (`attack_orthogonal`/`attack_diagonal`) + projétil Ground Target/Impact Area (Seção 13). Validar o caso difícil agora des-risca as ~96 fichas restantes de uma vez — se este funcionar, a maioria do resto tende a funcionar.

Population de teste: Rat no Floor 1 (target ~8, como já configurado); Goblin + Rat People misturados no Floor 2 (~5-6 total) — a Seção 22 do GDD é explícita que "o desafio emerge da combinação simultânea de ameaças", então testar Melee+Ranged juntos no mesmo Floor é o cenário que realmente importa, não cada um isolado.

---

## S16-T00 — Pré-requisito: travar TargetPosition no início do Telegraph

**Por quê primeiro:** dívida técnica da Sprint 13 — hoje o Melee não rechecha distância (acerta sempre que o telegraph completa) e o Ranged mira a posição atual do player no instante do hit, não uma posição travada no início do telegraph. Isso torna esquiva fisicamente impossível hoje, nos dois tipos. Rodar o playtest de sensação sem corrigir isso mediria um bug, não a decisão de arquitetura que a sprint existe pra tomar.

**O que fazer** (adapte nomes de campo/método ao `EnemyController` atual — os nomes abaixo seguem o que os relatórios das Sprints 13-15 descrevem):

1. No `EnemyController`, adicione um campo para a posição travada:
   ```csharp
   protected Vector2 lockedTargetPosition;
   ```
2. No momento da transição `Idle → Telegraph` (onde o attackState muda), capture a posição atual do player uma única vez:
   ```csharp
   case AttackState.Idle:
       if (/* condição de entrar em alcance/ataque já existente */)
       {
           lockedTargetPosition = player.position;
           attackState = AttackState.Telegraph;
           telegraphTimer = 0f;
       }
       break;
   ```
3. Em `MeleeEnemyController.ExecuteHit()`: rechecar se o player ainda está dentro do raio de ataque **a partir de `lockedTargetPosition`**, não da posição atual — se o player se afastou o suficiente durante o telegraph, o hit erra:
   ```csharp
   protected override void ExecuteHit()
   {
       float distanceAtLock = Vector2.Distance(lockedTargetPosition, transform.position);
       if (distanceAtLock <= attackRange)
       {
           // aplica dano normalmente
       }
       // senão: golpe erra — sem dano, sem log de erro, só não conecta
   }
   ```
4. Em `RangedEnemyController.ExecuteHit()`: o projétil deve mirar `lockedTargetPosition`, não `player.position` no instante do disparo:
   ```csharp
   protected override void ExecuteHit()
   {
       Vector2 direction = (lockedTargetPosition - (Vector2)transform.position).normalized;
       // instancia EnemyProjectile com essa direção, como já funciona hoje
   }
   ```

**Teste manual:** com o `Enemy_Test`/`Enemy_Test_Ranged` (placeholder, sem esperar pelos Animators), confirmar em Play Mode: (a) andar em círculo durante o Telegraph do Melee agora pode fazer o golpe errar; (b) sair da linha de tiro do Ranged durante o Telegraph agora pode fazer o projétil não te atingir, mesmo tendo sido disparado. Sem isso, não avance para T01.

---

## S16-T01 — Preparar os assets dos 3 monstros

1. Confirmar no Editor que os sprite sheets do Rat (`Minifantasy_Monster_Creatures_v1.0`), Goblin (`Minifantasy_Creatures_v3.3_Commercial_Version`) e Rat People (`Minifantasy_Monster_Creatures_v1.0`) já estão importados e fatiados (Sprite Editor) com os frames de `idle/walk/attack/damage/die`.
2. Para o Rat People especificamente: verificar quantos clipes reais existem para `attack_orthogonal`/`attack_diagonal` no sprite sheet — alguns assets Minifantasy resolvem diagonal via flip horizontal do clipe ortogonal em vez de ter frames próprios para os 4 eixos diagonais. Confirmar antes de montar o Animator Controller pra não desenhar 8 estados achando que são 8 clipes distintos.
3. Criar as pastas `Assets/Animations/Rat/`, `Assets/Animations/Goblin/`, `Assets/Animations/RatPeople/`.

## S16-T02 — Animator Controllers reais

Para Rat e Goblin (4-5 estados: Idle, Walk, Attack, Damage, Die):

1. Criar `Rat.controller` / `Goblin.controller` em cada pasta.
2. Estados: `Idle` (default, loop) → `Walk` (loop, transição por parâmetro bool `IsMoving`) → `Attack` (trigger `AttackTrigger`, sem loop, `Has Exit Time` desligado — a máquina de estado do `EnemyController` já controla a duração real via `AttackTiming`, o Animator só precisa tocar o clipe visual sincronizado) → `Damage` (trigger `DamageTrigger`, interrompe o clipe atual) → `Die` (trigger `DieTrigger`, sem transição de volta).

Para Rat People (mesmos 5 + variantes direcionais):

1. Substituir o único estado `Attack` por um Blend Tree ou por 2 estados (`Attack_Orthogonal`/`Attack_Diagonal`) selecionados por um parâmetro `int AttackDirectionType` calculado a partir do `AimDirection` já existente no projeto desde a Sprint 7 (mesma lógica de arredondar para 45° — reaproveite, não reimplemente).

## S16-T03 — Prefabs e bind com o EnemyController

1. Criar `Enemy_Rat.prefab`, `Enemy_Goblin.prefab`, `Enemy_RatPeople.prefab` a partir de `Enemy_Test`/`Enemy_Test_Ranged` (já têm `Rigidbody2D` correto desde a Sprint 14).
2. Adicionar `Animator` component + o Controller correspondente.
3. Preencher `AttackTiming` de cada um com os valores 🔢 estimados do Bestiário (Rat: dano 2/vida 5; Goblin: dano 6/vida 30; Rat People: dano 6/vida 30 — são estimativas do documento original, não travadas, está tudo bem usar como estão).
4. No `EnemyController`, dispare os triggers do Animator nas mesmas transições de estado que já existem (`Idle→Telegraph` dispara `AttackTrigger`, `TakeDamage` dispara `DamageTrigger`, `Die()` dispara `DieTrigger` antes de destruir o objeto) — sem criar uma segunda máquina de estado paralela, só espelhando a que já existe.

## S16-T04 — Popular os Floors e rodar o teste

1. `FloorPopulationManager` do Floor 1: trocar `Enemy_Test` por `Enemy_Rat` no `PopulationConfig`.
2. `FloorPopulationManager` do Floor 2: `PopulationConfig` com mix de `Enemy_Goblin` + `Enemy_RatPeople` (spawn alternado ou 50/50).
3. Jogar de verdade — Barbarian, população no target, Attack Budget ativo (valores atuais de debug da Sprint 14 servem, não é o momento de fechar o número final).

## S16-T05 — Critérios de decisão (preencher durante o Play Mode)

Direto da contingência do GDD Seção 22 — três perguntas, não "ficou bom?":

| Critério | Pergunta objetiva | Resultado |
|---|---|---|
| **Legibilidade** | Com Goblin+Rat People no budget cheio simultaneamente, dá pra distinguir os telegraphs de cada um a tempo de reagir? | 🔲 |
| **Custo solo** | Quanto tempo real levou para os 3 Animator Controllers + bind (T01-T03)? Extrapolado pras ~96 fichas restantes, isso é sustentável dentro do modelo de "produção em paralelo alternado com meses de heróis" do Plano de Produção? | 🔲 |
| **Diversão / sensação** | Agora que esquivar é fisicamente possível (T00), desviar do telegraph parece uma decisão do jogador, ou continua parecendo dano garantido na prática? | 🔲 |

**Se as 3 respostas forem positivas:** contingência descartada, Bestiário segue no formato completo. Bump de versão no GDD Seção 22 (🟡 → ✅, contingência resolvida como "não acionada").

**Se qualquer uma for negativa:** aciona a simplificação (Melee contato / Ranged simples / Explosivo) — isso teria efeito retroativo sobre o `EnemyController`/`AttackTiming` já implementado (Sprints 13-15), então viraria uma sprint própria de retrabalho antes da Sprint 20 (Bestiary Batch 2), não um ajuste dentro dela.

---

## Fora de escopo desta sprint (não fazer)

- Fechar o valor final de Attack Budget (Seção 14, 🔢) — observações informais são bem-vindas no relatório, mas o número trava na Deadline 13 (balanceamento).
- Produzir qualquer uma das ~96 fichas restantes do Bestiário.
- Corrigir a reação dinâmica de população (dívida da Sprint 14) ou o gating de Floor Sleep no `LootDrop` (dívida da Sprint 15) — ambas sem sprint atribuída, não bloqueiam este teste.

## Testes esperados no relatório final

- Automatizado: nenhum novo esperado (comportamento de Animator/timing é runtime, mesmo padrão já estabelecido desde a Sprint 5).
- Manual: os 3 cenários de T00 confirmando esquiva real funcionando, seguido do playtest estruturado de T05 com as 3 respostas preenchidas.
