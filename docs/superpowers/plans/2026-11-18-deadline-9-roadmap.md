# Deadline 9 — Roadmap de Sprints e Tasks

**Spec:** `docs/new/Mudanca_Estrutural_Deadline_9_XP_Cartas_Horda.md`

**Status:** rascunho pra revisão do usuário. Este documento define **o que** entra em cada sprint
e em que ordem. O plano detalhado de cada sprint (arquivos, assinaturas, testes, passo a passo) é
escrito só quando a sprint for começar, um plano por sprint.

**Numeração:** prefixo **D9.x**, pra não colidir com a numeração antiga (Sprints 1 a 67 do
`Projeto_Torre_Plano_Producao_v7.md`) nem com a série P (mundo procedural).

---

## Regras que valem pra todas as sprints

- **O jogo continua jogável ao fim de cada sprint.** Um sistema antigo só sai na mesma sprint em
  que o substituto entra.
- **Telemetria acompanha cada sprint.** Quando um sistema sai (perks, loot, venda), os eventos
  dele saem do `RunTelemetryRecorder` na mesma sprint, e os eventos do sistema novo entram. Não
  fica tudo pra última sprint.
- **Testes** seguem o padrão do projeto: métodos `[ContextMenu]` que retornam string, rodados via
  `script eval` (padrão do `RunTelemetryTest.cs`).
- **Números ficam em settings editáveis** (ScriptableObject). Nada de valor de balanceamento fixo
  no código.
- **Animator Controllers** são editados pela API do Editor (`AnimatorController`), nunca à mão no
  YAML.
- Cada sprint termina com commit. O push só acontece depois de perguntar.

---

## Visão geral

| Sprint | Fase | Tema | Depende de | Bloqueada por decisão? |
|---|---|---|---|---|
| **D9.1** | 0A | Atacar andando (10 heróis) | — | Animações `attack_walk` (usuário) |
| **D9.2** | 0B | Teste de densidade (descartável) | — | — |
| **D9.3** | 1 | Horda: performance | D9.2 | Resultado do D9.2 |
| **D9.4** | 1 | Horda: spawn em cerco, fluxo contínuo, anti-kite, avisos de explosão | D9.3 | Formato do anel, setores |
| **D9.5** | 2 | Dia e bosses: morte encerra a run, vitória, bosses em sequência, tela de resumo | D9.4 | Ordem e quantidade de bosses |
| **D9.6** | 2 | XP, nível e cartas (Barbarian), saída da Loja e dos perks, rebalanceamento dos monstros | D9.5 | **Escala nova dos monstros**, cap por dia |
| **D9.7** | 3 | Gold no chão, Vendor itinerante, itens, inventário de 6 | D9.6 | **Lista de itens e preços** |
| **D9.8** | 4 | Specs, talentos com reroll, baú novo | D9.6 | **Lista de talentos**, valor de X |
| **D9.9** | 5 | Gold permanente e upgrades no menu | D9.5 | **Lista de upgrades**, nome da moeda |
| **D9.10** | 6 | Ranger e Mage completos, tutorial, build pros amigos | D9.6 a D9.9 | Valores das cartas |

D9.1 e D9.2 são independentes entre si: dá pra fazer o D9.2 enquanto as animações do D9.1 ficam
prontas.

---

## D9.1 — Atacar andando (todos os heróis)

**Objetivo:** o herói anda durante o ataque primário, com a animação `attack_walk` própria. O
Shift e a Ultimate continuam travando o movimento.

**Base que já existe:** `allowMoveDuringPrimaryAttack` e `moveSpeedMultiplierDuringPrimaryAttack`
no `HeroController` (desligados).

**Tasks:**

1. **Troca Attack ↔ AttackWalk no `HeroController` (genérico, pros 10 heróis).**
   Durante o estado de ataque, se o jogador começa ou para de andar, troca de estado **no mesmo
   ponto da animação** (mesmo `normalizedTime`). O herói sem estado `AttackWalk` cai no
   comportamento de hoje (desliza tocando o ataque normal), então nada quebra enquanto as
   animações não chegam.
   *Pronto quando:* com um clipe placeholder (cópia do Attack), começar parado e andar no meio do
   golpe troca de estado sem reiniciar a animação.
2. **Auditoria dos Animation Events dos 10 heróis.**
   Garantir que todo evento de dano e de fim de ataque é à prova de disparo duplo (o padrão
   `attackHitFired` do Barbarian). Corrige os que não forem.
   *Pronto quando:* teste dispara o evento 2 vezes no mesmo golpe e o dano só sai 1 vez, em cada
   herói.
3. **Ligar os `attack_walk` no Animator de cada herói** (conforme as animações forem chegando).
   Estados novos, mesmas direções do ataque, mesmos eventos nos mesmos frames. Rogue, Gunslinger e
   os outros com ataques separados ganham um `attack_walk` por ataque.
   *Pronto quando:* cada herói ataca andando nas 4 (ou 8) direções, com o golpe saindo no frame
   certo.
4. **Assassin: monstros perseguem o projétil.**
   No nascimento dos projéteis, cada monstro (bosses também) escolhe o projétil mais perto e
   persegue ele até o fim do ataque. No fim da animação, o alvo volta pro Assassin. Todos os
   projéteis terminam juntos.
   *Pronto quando:* com 3 projéteis em leque, a horda se divide por eles e volta pro Assassin no
   fim.
5. **Ligar a flag e ajuste de velocidade.** Ligar `allowMoveDuringPrimaryAttack` em todos os
   heróis, testar a velocidade durante o ataque (começa em 1) e ajustar Ranger e Mage se o kite
   ficar forte demais.

**Precisa do usuário:** as animações `attack_walk`. Decidir 4 ou 8 direções pro Ranger e o Mage.

---

## D9.2 — Teste de densidade da horda (descartável)

**Objetivo:** descobrir quantos monstros vivos o jogo aguenta hoje e o que trava primeiro.

**Tasks:**

1. **Cenário de teste no `_TestScene`.** Script descartável que mantém N monstros vivos em volta
   do player (100, 200, 300), com o player parado e depois andando em círculo. Também mede
   **muitos projéteis e efeitos vivos ao mesmo tempo** (centenas), porque os itens vão encher a
   tela deles (Seção 7.3 da spec).
2. **Medição.** FPS e profiler em cada N, separando o custo de: A* (pedido de caminho), RVO,
   `Instantiate`/`Destroy`, Animator, slots de melee, física e o `Update` do `EnemyController`.
3. **Relatório.** Ranking do que pesa mais, com os números, e a recomendação de quais soluções
   entram no D9.3 (flow field, RVO só perto, pooling, Animator desligado fora da tela, Simulation
   Rings).

**Pronto quando:** você recebe o relatório e decide o escopo do D9.3. O código do teste é apagado
e não entra no jogo.

---

## D9.3 — Horda: performance

**Objetivo:** aguentar a densidade que o design pede. **O escopo final depende do D9.2.** As
tasks 1 e 2 entram de qualquer jeito, as outras só se o teste mostrar que são necessárias.

**Tasks:**

1. **Pooling de monstros.** O monstro morto volta pra uma "piscina" e é reaproveitado no próximo
   spawn, em vez de `Destroy`/`Instantiate`. O ponto delicado é **zerar todo o estado** na volta:
   vida, DoTs, aggro, slot de melee, Animator, contadores da telemetria (`directHitsThisLife`,
   `dotTicksThisLife`), sem eventos de morte duplicados.
   *Pronto quando:* um monstro reciclado 10 vezes se comporta igual a um recém-criado (teste
   compara o estado).
2. **Reposicionar em vez de destruir.** O despawn por distância (`WorldPopulationManager`,
   hoje `Destroy` + `SpawnOne`) passa a mover o monstro pra um ponto novo fora da tela.
3. *(condicional)* **Flow field.** Um mapa único de direções até o player, recalculado de tempos
   em tempos, no lugar de um A* por monstro.
4. *(condicional)* **RVO só perto do player**, com separação simples longe.
5. *(condicional)* **Animator desligado fora da tela.**
6. *(condicional)* **Ajuste dos Simulation Rings.**

**Pronto quando:** o número de monstros definido depois do D9.2 roda no FPS alvo.

---

## D9.4 — Horda: spawn em cerco, fluxo contínuo e anti-kite

> **Implementado em 2026-11-21** (branch `d9-4-horda-cerco`): design
> `docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md`, plano
> `docs/superpowers/plans/2026-11-20-d9-4-horda-cerco.md`. Mudou em relação às tasks abaixo: a
> curva segue o **tempo real** do dia (não o cronômetro), com começo/teto por dia; o anti-kite fica
> sempre ligado; a task 6 caiu (a regra já não existia); entraram o "herói empurra a horda", o "dia
> conta desde o começo" e o reposicionamento sem destruir.

**Objetivo:** o mundo sempre cheio, os monstros sempre vindo de fora da tela e de todos os lados.

**Tasks:**

1. **Nascer sempre fora da tela, em anel** (`WorldSpawnUtility`). Sai o `outsideCameraViewChance`
   e o "plano B" visível: sem lugar fora da tela, tenta de novo no frame seguinte. Vale pro spawn,
   pra reposição e pro reposicionamento por distância.
2. **Cerco por setores.** O anel é dividido em setores, e cada spawn vai pro setor com menos
   monstros vivos.
3. **Fluxo contínuo.** Sai o `respawnInterval` fixo (1 monstro a cada 3s) e as rajadas da crise
   (8 por frame). Entra uma reposição que acompanha a taxa de kill, alguns por frame, sempre fora
   da tela.
4. **Curva da horda pelo cronômetro.** Começo com poucos (~10), subida gradual, picos em momentos
   do dia, muitos no final. Os ranged só entram depois de um tempo de dia.
5. **Anti-kite.** Peso extra pros setores na direção em que o player foge, com teto. Por
   recomendação, só liga quando o player está atacando enquanto foge.
6. **Rever a regra do "mínimo 3 vezes vira máximo"**, que o fluxo contínuo pode ter deixado
   desnecessária.
7. **Avisos de explosão.** Área e aviso de "vai explodir" no Burning Skull, área da bomba do
   Goblin Sapper, reaproveitando as áreas no chão que já existem.

**Precisa do usuário:** elipse ou círculo e a margem fora da tela; número de setores; tempo até
os ranged; se o anti-kite só liga atacando. Os números da curva saem do D9.2.

---

## D9.5 — Dia e bosses

**Objetivo:** o dia passa a ser decidido pelos bosses, e a run passa a ter começo, fim e
vitória.

**Tasks:**

1. **Morte encerra a run.** Sai o retry do mesmo dia (`DayResolver.ResolvePlayerDeath`). Morrer
   leva pra tela de fim de run e de volta ao menu. Ajustar o save e o "Continue", que hoje
   assumem o retry.
2. **Cronômetro pausando com boss vivo** (`DayTimer`). Sai o fim do dia por tempo. A horda, os
   ranged e o Dia/Noite continuam seguindo o cronômetro.
3. **Bosses em sequência e em ordem fixa** (`WorldBossSpawnManager`). Um boss vivo por vez, o
   próximo nasce x segundos depois da morte do anterior. Sai o sorteio por tier do lugar. Os
   bosses atuais são usados como estão.
4. **Teleporte em vez de troca.** Se o player foge, o boss é teleportado pra fora da tela, perto
   dele, mantendo o enrage. Sai o "remover e trocar o boss".
5. **Enrage.** Depois de x tempo vivo, o boss ganha velocidade e dano aos poucos.
   *Já existe:* bosses ignoram a invisibilidade na escolha de alvo (`EnemyController`). Só
   conferir.
6. **Fim do dia e vitória.** O dia acaba poucos segundos depois da morte do último boss. A run
   termina em vitória ao matar o Divine God.
7. **Tela de resumo.** Animação de fim de dia, depois a tela com monstros mortos, gold e bosses,
   enquanto o próximo dia carrega em background.

**Nesta sprint a Loja entre dias ainda existe** (sai no D9.6, junto com a entrada do XP). O gold
continua tendo uso até lá.

**Precisa do usuário:** quantos bosses por dia e a ordem deles; os tempos (intervalo, enrage,
fim do dia).

---

## D9.6 — XP, nível e cartas (Barbarian)

**Objetivo:** a progressão sai da Loja e vai pra dentro do combate.

**Tasks:**

1. **Rebalanceamento dos monstros** (sai dos trilhões). Primeira task porque os valores das
   cartas dependem da escala nova.
2. **XP e nível.** 1 XP por kill, tabela num settings editável (valores da Seção 3), barra de XP
   no HUD.
3. **Stats por nível.** Subir de nível aumenta dano, vida e cura automaticamente.
4. **Cap de nível por dia**, com o XP que passa do cap virando gold.
5. **Tela de cartas.** Pausa o jogo, mostra 3 cartas (Primary, Survival, Ultimate), o jogador
   escolhe 1.
6. **Cartas do Barbarian** (Seção 5): os 3 tipos, com os efeitos padrão (cooldown, velocidade de
   animação, knockback) e os específicos dele. **Sem efeitos de tamanho**, que agora são dos itens
   (regra de 2026-11-19).
7. **Saída da Loja, dos perks e do Redefine.** `PerkState`, a Loja entre dias e a aba de
   talentos antigos saem. A telemetria troca "perk comprado" por "carta escolhida" e "nível por
   minuto".

**Precisa do usuário:** **a escala nova dos monstros** (bloqueia a sprint); os caps por dia; os
stats por nível; os valores das cartas.

---

## D9.7 — Gold no chão, Vendor itinerante e itens

**Objetivo:** o gold volta a ter uso, e o Vendor passa a fazer o jogador se mover.

**Tasks:**

1. **Gold no chão.** Cada monstro dropa uma quantidade aleatória de gold, como item com sprite
   que precisa ser coletado. Saem os 15 loots e a venda (`LootDrop`, `LootDefinition`,
   `LootPricingSettings`). A telemetria troca "loot vendido" por "gold coletado".
2. **Vendor itinerante** (`Vendor`, `VendorCart`). Primeira aparição depois de x segundos
   aleatórios, fica y segundos, some z segundos, reaparece em outro lugar. Sai o Vendor fixo e
   comprador.
3. **Ganchos comuns pros itens (a task mais importante da sprint).** Eventos iguais pros 10
   heróis: herói sofreu dano, projétil nasceu, acertou monstro, matou monstro. Mais um stat de
   escala do herói e dos projéteis. Hoje cada herói tem o próprio projétil e o próprio jeito de
   causar dano, então essa é uma refatoração que passa por todos eles. Inclui a trava contra
   reação em cadeia (efeito de item não dispara outro item).
   *Pronto quando:* um item de teste ("+1 de dano por acerto, com log") funciona nos 10 heróis
   sem nenhum código específico de herói.
4. **Sistema de itens passivos.** Definição de item (settings), efeitos que usam os ganchos da task
   3, os stats do herói e os DoTs. Iguais pra todos os heróis. Efeitos dos itens numa camada
   abaixo dos avisos de perigo dos monstros.
5. **Loja do Vendor.** Pausa o jogo, lista os itens, compra com gold da run.
6. **Inventário de 6 slots**, só pra itens (reaproveitando `InventorySlot` e a UI atual).
7. **Primeiro baralho de itens**, pequeno mas já com os "caóticos": tamanho do herói, espinhos ao
   sofrer dano, projéteis teleguiados que continuam perseguindo depois de matar, tamanho de
   projétil e de área.

**Precisa do usuário:** **a lista de itens e preços** (bloqueia as tasks 4 e 7); se o Vendor
filtra itens pelo herói; o que acontece
com item ruim comprado cedo; x, y e z do Vendor; as faixas de gold por monstro.

---

## D9.8 — Specs, talentos e baú novo

**Objetivo:** a variedade entre runs.

**Tasks:**

1. **Tela de spec no início da run.** 2 opções por herói. Começa pelo Barbarian (Sanguinário x
   Terremoto).
2. **Sistema de talentos.** A cada X níveis, 3 cartas com reroll de 1 vez por carta, sem repetir
   as do lado nem a que acabou de sair.
3. **Primeiro baralho de talentos**, partindo dos talentos da Deadline 8.1.
4. **Baú novo** (`WorldChestSpawnManager`, `Chest`, `ChestRewardSystem`). Nasce durante o dia
   (não mais no início), mais raro, e abre uma oferta de talento. Sai o "50% gold, 50% popup".
   Entram talentos que aumentam a chance do baú.

**Precisa do usuário:** **a lista de talentos** e o valor de X; a chance de spawn do baú; se o
mimic continua.

---

## D9.9 — Gold permanente e upgrades no menu

**Objetivo:** as runs passam a melhorar as runs futuras.

**Tasks:**

1. **Drop de gold permanente.** Raro, com sprite e nome próprios, diferente do gold da run.
2. **Guardar 100%** na morte e na vitória, persistido no save (`SaveManager`).
3. **Tela de seleção de herói.** Mostra os status base e os upgrades permanentes do herói, cada
   herói evoluindo separado.
4. **Upgrades permanentes**, com teto e de preferência dando opções (mais um reroll, começar com
   item) em vez de só números.

**Precisa do usuário:** **a lista de upgrades**; o nome da moeda; se boss dá quantidade garantida.

---

## D9.10 — Ranger e Mage completos, tutorial e build

**Objetivo:** os 3 heróis do escopo prontos, e uma build pros amigos testarem.

**Tasks:**

1. **Ranger:** specs (Sanguinário x Olhos de Águia) e cartas.
2. **Mage:** specs (Imolação x Familiar) e cartas. O pet passa a vir só na spec Familiar.
3. **Tutorial opcional.** Toggle na seleção de herói. Pausa e explica na primeira vez que
   aparecem o baú, o Vendor e o boss, mais a mensagem "quanto mais longe, mais perigoso".
4. **Revisão final da telemetria**, pra responder as perguntas do próximo teste (nível por
   minuto, onde morreu, quanto fez kite).
5. **Build pros amigos.**

---

## O que eu deixei de fora de propósito

- **Rework dos bosses, Druid/Cleric e o resto dos heróis, página na Steam:** fora da Deadline 9
  (Seção 13 da spec). A única exceção é o atacar andando, que vale pros 10 heróis.
- **Specs e cartas dos outros 7 heróis:** estão registradas na spec, mas não entram aqui.
