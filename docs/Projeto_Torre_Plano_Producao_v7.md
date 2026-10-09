# Projeto Torre — Plano de Produção (Revisão 3)
## Etapa 1: Análise de Produção · Etapa 2: Ordem Macro · Etapa 3: 14 Deadlines Revisadas

> Fonte de verdade de gameplay: **GDD Mestre v1.01238** (estruturalmente congelado). Este documento define **como construir**, nunca **o que o jogo é**. Revisão cirúrgica sobre a versão anterior: mesma profundidade de análise, distribuição de carga corrigida.

> **Nota de versão (Revisão 3, atualizada após o task breakdown da correção):** a Sprint 16 testou o sistema de combate completo (Telegraph/Hitbox/Recovery + Animator real) em 3 monstros reais e acionou a contingência do GDD Seção 22 — monstros comuns passaram a usar dano por contato (Melee) / auto-disparo em alcance (Ranged), cada um só com cooldown próprio, sem Telegraph nem animação de ataque dedicada, mais uma IA de patrulha nova (`idle`/`walk` aleatório + `idle_combat`) e morte orientada por Animation Event; o **Attack Budget foi removido** (GDD Seção 14).
>
> **Segunda nota (pós-correção, mesma sprint):** esse modelo puro de contato/auto-disparo não durou — testado na prática, perdeu a identidade visual do ataque, então a decisão final virou um meio-termo (GDD v1.01238): todo monstro comum volta a ter uma animação `attack`/conjuração real, com Animation Event decidindo o instante do golpe (trigger direcional pro Melee) ou do disparo (Ranged, sem telegraph). **O dano de contato foi removido de vez** — só os Slimes (comuns e Mother Slime Green/Blue) ficam nele pra sempre. O Attack Budget continua removido. Isso muda o texto de "causa dano por contato/projétil" que aparecia no Exit Criteria original da Deadline 4 (linha da tabela abaixo) — a intenção do critério (2+ monstros com IA funcional causando dano de verdade ao jogador) continua satisfeita, só o mecanismo descrito mudou; não é motivo pra reabrir o Exit Criteria em si. Isso torna o conteúdo de Bestiary genuinamente mais barato por ficha do que o previsto nesta tabela (que foi calibrada assumindo Animator completo por criatura, incluindo telegraph/dodge) — **exceto** num pequeno grupo de exceções com arquitetura própria por Animation Event (5 comuns: Goblin Sapper, Orc Shaman, Burning Skull, Serpent, projétil do Bicephalous; e 8 bosses: Mother Slime Green/Blue, Rat People Royalty, Spider Queen, Dark Channeler, Lich, Dragon, Undead Dragon, Divine God), que mantêm complexidade equivalente à original. O task breakdown da correção (Sprint 16) deixou claro que reescrever o framework + construir a IA de patrulha já ocupam a sprint inteira — por isso, **única exceção ao Distribution Freeze nesta revisão:** o Batch 1 de Bestiário (Floors 1–2 iniciais) saiu da Sprint 16/Deadline 4 e foi absorvido pelo Batch 2 na Sprint 20/Deadline 5, que passa a entregar o elenco completo de Floors 1–2 de uma vez. Todo o resto do freeze (números de Sprint/Deadline, ordem, demais Batches) continua exatamente como estava.
>
> **Terceira nota (segunda rodada de teste do modelo híbrido, já commitada — 571b089/9ca252f):** duas correções em cima da Segunda nota. Primeiro, **a premissa de custo da Segunda nota estava errada** — o `attack` real não some, a maioria dos 30 bosses e todo monstro comum (exceto Slimes) usa Animation Event pra atacar, então não é "sem Animator". O corte de custo real veio da ferramenta nova `MonsterAnimationGeneratorWindow` (gera clipes direcionais sobre 2 Animators base compartilhados, `AnimationHitEvent`/`AnimationAttackEndEvent`/`AnimationDieEndEvent` inseridos automaticamente) — configurar Animator do zero por criatura, que era o custo caro original, deixou de existir; ver a linha "Bestiary" na auditoria final abaixo, já corrigida. Segundo, **a contagem "8 bosses" da Segunda nota estava errada** — Mother Slime Green/Blue não é uma exceção bespoke, cai na mesma regra permanente dos Slimes comuns (só contato, sem `attack`); são 7 bosses com arquitetura própria de verdade (Rat People Royalty, Spider Queen, Dark Channeler, Lich, Dragon, Undead Dragon, Divine God), não 8. Dois bugs de arquitetura também corrigidos nesta rodada: detecção agora troca pra combate imediatamente (o atraso de reação é só o Exit Time do Animator, não mais um timer no código, que chegava a atrasar até 4s); e o monstro só translada de verdade quando o Animator já está no estado `Walk`, não no frame em que a intenção foi marcada. Pendências em aberto, não fechar Deadline 4 sem revisitar: Spectre (boss Andar 5, 🟡 no Bestiário — só 3 animações, dar `attack` de volta quebraria o design minimalista, decisão não tomada) e um re-teste manual do Floor Sleep (Sprint 15) depois desta rodada de mudanças no Enemy Framework.
>
> **Quarta nota (flanco, `MeleeAttackSlotManager`, commits `b9ce437`/`23223c1`/`dcb3597`):** hordas grandes lotavam o corpo a corpo — teto configurável (padrão 12) de quantos Melee ficam em alcance de contato do jogador ao mesmo tempo; quem não consegue vaga flanqueia num anel (`flankRadius`), reaproveitando o `PatrolAI` já existente, sem mudança de Animator. **Isso é um limitador novo, além da população (Seção 23)** — o texto "sem limite artificial de quantos simultâneos" que aparecia no Exit Criteria original da Deadline 4 (linha da tabela abaixo) ficou errado e já foi corrigido lá. Dois bugs pegos e corrigidos na revisão deste mecanismo, os dois com precedente direto no próprio Attack Budget original: (1) o pool de vagas tinha nascido global pra Scene inteira, não por Floor — mesmo bug que o Attack Budget teve antes do fix da Sprint 15 (um Melee com vaga cujo Floor dorme nunca libera, rouba capacidade do Floor ativo); corrigido, agora é por Floor. (2) a vaga só liberava na morte — um Melee que perdesse o jogador de vista continuava com a vaga presa pra sempre, enquanto ameaças reais próximas não conseguiam nenhuma; corrigido, a vaga também libera se o dono sai do próprio `flankRadius`. A Seção 14 do GDD ainda não foi atualizada pra refletir esse novo limitador — continua dizendo "nada precisa substituir [o Attack Budget]... sem limite artificial" — vale um ajuste pontual la (fora do escopo deste documento de produção, mas registrado aqui pra não se perder).
>
> **Quinta nota (reestruturação da Deadline 6 em diante, pós-Sprint 21):** a Sprint 21 fechou só com o Druid completo (primário/ultimate/secundária — sem o Floor 3A–4A que estava planejado junto), e essa sprint deixou claro na prática um padrão que vale para o resto da produção: é muito mais fácil achar bugs e testar quando se está numa sequência de ações do mesmo tipo (vários bugs de base encontrados construindo o Druid — corrida de Trigger na morte de monstro, collider físico nunca espelhando de verdade, energia gasta sem a habilidade ativar — já existiam em todo herói, e só ficaram visíveis por repetição do mesmo fluxo de teste; ver `docs/sprints/sprint-21.md`). Decisão: agrupar toda a frente "heróis + combate" (heróis restantes, Boss Framework, bosses) nas Deadlines 6–7, e só depois abrir uma Deadline 8 única dedicada inteiramente à Torre (tileset final dos Floors 3A–10A + Bestiário completo dos Andares 3–10 + UI final de gameplay), sem interromper com sistemas de código no meio. Isso empurra o Floor 3A–4A (e todo o resto de Floor Content) pra dentro dessa Deadline 8, que passa a ser bem maior que as demais — 13 sprints em vez de 4, refletido na renumeração abaixo (o total do projeto passa de 56 para **67 sprints**; nenhuma sprint das Deadlines 1–5, já fechadas, muda de número). Deadlines 9–14 mantêm exatamente o mesmo escopo e ordem de antes, só deslocadas +11 sprints. Detalhe completo nas seções "Deadline 6" em diante.

---

# OBRIGAÇÕES PRESENTES EM TODAS AS 67 SPRINTS (nota global)

Nenhuma das 67 sprints futuras será aceita sem:

```text
Código + Testes (EditMode/PlayMode/Manual conforme aplicável) + Regression +
Documentação atualizada no GitHub Pages + Git (branch/commit/merge) + Build semanal quando possível
```

Documentação e testes **não são uma Deadline** — são obrigação transversal de toda sprint, desde a Sprint 1. Isso vale igualmente para QA: não existe "mês de QA", existe QA contínuo mais um período final de estabilização concentrada (Deadline 14).

---

# ETAPA 1 — ANÁLISE DE PRODUÇÃO

## 1. Maiores sistemas do jogo

| Sistema | Complexidade | Por quê |
|---|---|---|
| **Floor System** | Muito alta | Scene única, 3 identidades de Floor (Original/Active Position/Variant), Stair Routing relativo, Floor Sleep, Remove Tower Layer remapeando tudo em cascata |
| **Combat System** | Muito alta | 10 heróis com famílias técnicas distintas (transformação, pets, homing, hitscan, orbital, dash/stealth, área em duas fases), Ability Timing genérico, Attack Budget, Combat Scope = Current Floor |
| **Economia/Loot** | Alta | 15 materiais com rolagens independentes, agregação visual, Weapon Tier (15 tiers sequenciais), Demanda diária, Pickup Radius |
| **Employees** | Alta | Árvore de promoção, Strong/Fast, virtualização (contagem lógica ≠ simulada), coleta cross-Floor — grande o suficiente para exigir 2 janelas de produção (funcional e escala) |
| **Save/Game State** | Média-alta | Checkpoint único compartilhado entre 2 modos, semântica de overwrite, RunState que cresce ao longo de todo o projeto |
| **Quests/Chest/Cards** | Média-alta | Magnet integrado ao Pickup Radius, Mimic, UI de 3 opções, pools universais/por herói/por employee — três sistemas interligados, não uma tarefa pequena |
| **Progress Tracking** | Média | 3 escopos (Daily/Run/Lifetime) + separação Account Progression × Lifetime Statistics, mode-aware; precisa de esqueleto cedo para não virar retrabalho |
| **Bestiary/Bosses** | Alta (por volume) | Framework é rápido; o conteúdo real (dezenas de monstros e bosses por Floor) é o que consome tempo, e precisa de janela própria |
| **UI/HUD/Localization** | Média | Mode-aware (demanda só aparece no Padrão), IDs desde o início, nascem provisórias com cada sistema |
| **GameEvents/Large Number/Test Framework** | Fundacional | Baixa complexidade individual, mas altíssimo custo de retrofitar se nascerem tarde — por isso entram no Mês 1 |

## 2. Dependências entre sistemas

- **Floor System é a fundação estrutural mais transversal.** Combat Scope, Boss Timer, Employees cross-Floor, Magnet Floor Range, Remote Controller e Remove Tower Layer dependem de Active Floor Position existir antes deles.
- **Hero Framework precisa existir antes de qualquer conteúdo de herói ou monstro** — dano/morte/Ability Timing são a base compartilhada de quem ataca e de quem apanha.
- **O Vertical Slice precisa de um inimigo real (perseguir → atacar → morrer → dropar), não um dummy parado** — validar loot/economia/save contra um alvo estático esconde bugs de timing e de interação com IA que só aparecerão depois, mais caros de corrigir.
- **GameEvents, Large Number Abstraction, Save/RunState skeleton, Progress Tracker skeleton e Localization skeleton precisam nascer no Mês 1**, mesmo incompletos — cada um desses, se nascer tarde, obriga a reabrir todos os sistemas que já existiam para conectá-los retroativamente.
- **Pickup Radius precisa existir antes do Magnet** (o Magnet reutiliza o raio do jogador, não tem raio próprio).
- **Loot/Economia precisam existir antes de Employees**; e Employees precisam de **duas janelas**: uma para o framework funcional (compra/venda/promoção/1 Ajudante/1 Coletor) e outra, separada, para escala (Strong/Fast, virtualização, cross-Floor em volume) — comprimir as duas num único mês foi o erro da versão anterior.
- **Remove Tower Layer e Remote Controller dependem do Stair Routing estar validado** — ambos recalculam destinos por Active Floor Position.
- **Free Mode depende do ciclo do Modo Padrão estar completo e estável** — é "Padrão sem uma validação", implementável em pouco tempo dentro de uma Deadline de sistemas de meta-progressão, não precisa de mês próprio.
- **Bestiary e Bosses têm framework barato e conteúdo caro** — a tabela precisa mostrar as duas janelas separadamente, e a produção de conteúdo real deve andar em paralelo (alternado) com os meses de expansão de heróis, não depois deles.
- **Floor Variants B–E só devem começar depois que Floor Framework + Variant A completa + regras de level design estiverem maduras** (por volta da Deadline 8/9) — e mesmo assim, distribuídas ao longo de várias Deadlines, nunca concentradas num único mês.

## 3. Maiores riscos técnicos

1. **Arquitetura de Scene única com Floor Sleep/virtualização** — suspender simulação preservando estado é arquitetura genuinamente não-trivial.
2. **Ability Timing genérico o suficiente para os 10 heróis**, sem virar abstração excessiva.
3. **Combat Scope (Current Floor) dentro de uma Scene única** — exige filtragem real (layers/FloorId).
4. **Large Number Abstraction decidida tarde** — se o tipo numérico for trocado depois que Loot/Economia/Employees já existirem, o retrabalho se propaga por todos eles. Por isso entra no Mês 1, não como "detalhe técnico depois".
5. **Virtualização de Employees em escala** — performance-sensível e iterativa, por isso ganhou uma Deadline própria (11) separada da funcional (10).

## 4. Maiores riscos de produção (solo dev)

1. **Escopo grande para 1 pessoa** — mitigado pela separação framework/conteúdo em todas as frentes (heróis, monstros, bosses, Floor Variants, Employees).
2. **Tendência a refinamento constante** (visível no próprio histórico de revisões do GDD) — pode virar retrabalho de código se a disciplina de "congelado é congelado" não se estender à implementação.
3. **Pipeline de conteúdo pode virar gargalo** mesmo com framework pronto — por isso a produção de Bestiary/Bosses/Floor Variants passa a ocorrer **em paralelo alternado** com os meses de heróis, em vez de esperar os heróis terminarem.
4. **Balanceamento tem cadeia de dependência longa** — reservado como fase própria explícita (Deadline 13), não espremido no fim.
5. **Preparação de Steam** — distribuída a partir da Deadline 12, não inteira no último mês.

## 5. Sistemas que precisam existir muito cedo (Mês 1, em esqueleto)

Input System · Time/Pause Manager · Game State/Flow skeleton · GameEvents central · Localization skeleton · Test Framework (EditMode/PlayMode) · Save/RunState skeleton · Large Number Abstraction · Progress Tracker skeleton · Git + GitHub Pages + pipeline de documentação · organização de projeto (pastas, layers, HierarchySectionHeader).

## 6. O que deve ser adiado

50 Floor Variants completas (só A primeiro, B–E distribuídas depois da Deadline 8) · os 10 heróis simultaneamente (1 por vez) · Employees em escala (Strong/Fast/virtualização só na Deadline 11, depois do funcional na 10) · Integração Steam completa (só groundwork a partir da 12, validação final na 14) · conteúdo de localização em múltiplos idiomas (arquitetura cedo, conteúdo na 13) · polish visual/áudio final (hooks cedo, passe grande perto do fim) · valores finais de balanceamento (Deadline 13) · heróis de Visão Expandida — fora do escopo dos 14 meses.

## 7. Primeiro Vertical Slice (Deadline 3)

```text
Player (Barbarian, arte placeholder)
↓ entra no Floor testbed
↓ MeleeEnemyPrototype detecta, persegue, ataca, recebe dano, morre
↓ Monster Essence dropa
↓ Pickup Radius coleta automaticamente
↓ retorna ao térreo, vende no NPC
↓ tempo esgota ou usa a porta
↓ valida demanda (Modo Padrão)
↓ Tela de Resultados
↓ Loja (compra Copper)
↓ Save real no checkpoint → Start Day 2
```
Diferença da versão anterior: o inimigo já é um comportamento real (perseguir/atacar/morrer/dropar), não um alvo estático — isso valida Ability Timing e Combat Scope contra IA de verdade, não contra um caso degenerado.

## 8. O que pode usar placeholder

Arte de heróis/monstros/tiles · maioria dos monstros além do primeiro (até o Enemy Framework da Deadline 4 existir) · todos os valores 🔢 pendentes de balanceamento · áudio (hooks desde cedo, conteúdo depois).

## 9. Sistemas que precisam de framework antes de conteúdo

Hero Framework → 10 heróis · Enemy Framework → Bestiary · Boss Framework → bosses reais · Floor Variant framework → Variant A e depois B–E · Employee Definition/tier → árvore completa · Card Definition/pools → conteúdo de cartas · Bonuses genérico → cada botão da aba. Cada um desses pares (framework, conteúdo) recebe **janelas distintas e explícitas** na tabela da Etapa 3 — nenhum conteúdo real fica escondido atrás da palavra "Framework".

## 10. Partes que provavelmente consumirão mais tempo

1. Os 10 kits de herói completos.
2. Arquitetura do Floor System.
3. Conteúdo de Bestiary/Bosses distribuído ao longo de várias Deadlines.
4. Produção das 40 Floor Variants B–E, mesmo distribuída.
5. O balanceamento geral da economia (inerentemente iterativo).
6. Employees em escala (virtualização + cross-Floor + profiling).

---

# ETAPA 2 — PROPOSTA DE ORDEM DE PRODUÇÃO

A diferença central desta revisão: em vez de um pipeline puramente serial, a partir da Deadline 5 existem **duas trilhas alternadas dentro do mesmo desenvolvedor** — sistemas principais (a prioridade de cada mês) e produção gradual de conteúdo (o que "enche" as semanas mais leves daquele mês). Isso não é paralelismo de equipe — é alternância real de tarefas ao longo das 4 sprints de cada mês, respeitando que há **1 pessoa só**.

```text
Fundação técnica completa (GameEvents, Localization, Tests,
Save/RunState, Large Number, Progress Tracker — todos em esqueleto)
↓
Floor + Combat Testbed (Scene única, Stair Routing, Barbarian, MeleeEnemyPrototype)
↓
PRIMEIRO VERTICAL SLICE (dia completo, inimigo real, save no checkpoint)
↓
Enemy Framework maduro (Melee/Ranged por contato-cooldown, sem Attack Budget/Population) + início do Bestiary real
↓
┌─── A partir daqui, trilha principal + trilha de conteúdo alternadas ───┐
│                                                                          │
│  Heróis I (Ranger, Mage)        ⟷  Floor Variants A (primeiros Floors) │
│  Heróis II (Druid, Rogue,       ⟷  Boss Framework + primeiro boss real │
│  Cleric) + Boss Framework                                              │
│  Heróis III (Paladin,           ⟷  Bestiary expandindo + Floors A      │
│  Gunslinger, Assassin,                                                 │
│  Blood Mage)                                                           │
└──────────────────────────────────────────────────────────────────────┘
↓
Loot completo (15 materiais) + TORRE VARIANT A COMPLETA (Ground→Floor 10)
↓
Economia expandida (15 tiers) + Bonuses + Chest/Card framework
   ⟷ início de Floor Variants B (agora que o Floor Framework está congelado)
↓
Employees — Fase 1 (funcional: compra/venda/promoção/1 Ajudante/1 Coletor)
   ⟷ continuidade de Variants B/C
↓
Employees — Fase 2 (escala: Strong/Fast/virtualização/cross-Floor) + Quests
   ⟷ continuidade de Variants C/D
↓
Run completa + Meta Systems (Remove Tower Layer, Remote Controller,
Free Mode, Progress Tracking completo, achievements, base Steam)
   ⟷ continuidade de Variants D/E
↓
Content Complete + Balance (finalizar o que restou de conteúdo,
balanceamento geral, performance, localização de conteúdo)
↓
Stabilization & Release Candidate
```

### Por que essa ordem reduz retrabalho

- **Fundação completa no Mês 1** evita que GameEvents, Save, Large Number, Localization e Progress Tracker precisem ser costurados retroativamente em sistemas já existentes — o custo de adicioná-los depois cresce a cada sistema novo que não os usa desde o início.
- **Vertical Slice com inimigo real** valida Combat Scope e Ability Timing contra o caso de uso real (perseguição, ataque, morte) em vez de um caso degenerado que esconderia bugs até a Deadline 4.
- **Framework e conteúdo em janelas separadas para Heróis, Monstros, Bosses e Floor Variants** — nenhuma dessas quatro frentes fica "escondida" atrás da palavra Framework; a tabela da Etapa 3 mostra explicitamente quando o conteúdo real é produzido.
- **Employees dividido em Fase 1 (funcional) e Fase 2 (escala)** — comprimir as duas coisas em um mês só (erro da versão anterior) ignora que virtualização e cross-Floor são, na prática, um segundo projeto de performance em cima do primeiro.
- **Floor Variants B–E começam só depois da Deadline 8** (Floor Framework maduro + Variant A completa validada) e se distribuem por 5 Deadlines (9 a 13) em vez de ficarem concentradas — isso é a correção mais importante da revisão anterior.
- **Quests recebe espaço dedicado (Deadline 11)**, reconhecendo que o Magnet sozinho já interage com Pickup Radius, Floor Range, venda, Results e o filtro pendente — não é uma tarefa pequena.
- **Steam começa a ser preparado a partir da Deadline 12** (não do zero na 14) — a Deadline final só precisa *validar e publicar*, não *começar a integrar*.
- **Mês 13 vira "terminar o que restou" e balancear**, não "produzir a maior parte do conteúdo do zero" — a correção que o prompt pediu explicitamente.
- **Mês 14 é estabilização pura** — sem sistemas estruturais nascendo pela primeira vez ali.

---

# ETAPA 3 — 14 DEADLINES REVISADAS

| # | Mês | Deadline | Objetivo | Estado jogável ao final | Principais sistemas/conteúdo | Dependências | Exit Criteria |
|---|---|---|---|---|---|---|---|
| 1 | 1 | Fundação Técnica Completa | Projeto profissionalmente estruturado | Player placeholder move e pausa numa Scene vazia | Unity setup, Input, Hierarchy/HierarchySectionHeader, pastas, Layers/Tags, Git+GitHub Pages+docs pipeline, GameEvents skeleton, Time/Pause, Localization skeleton, Game State skeleton, RunState/Save skeleton, Large Number abstraction, Progress Tracker skeleton, Test Framework | Nenhuma (ponto de partida) | Build roda fora do Editor; 1 evento de teste disparado via GameEvents e logado; troca de idioma de teste funcional; 1 teste automatizado passando; RunState salva/carrega um valor de exemplo |
| 2 | 2 | Floor + Combat Testbed | Navegar e lutar de verdade | Player anda entre 2–3 Floors placeholder e mata 1 inimigo funcional | Scene única, Current Floor, Original Floor Identity, Active Floor Position, Stair Routing, Floor Bounds, Combat Scope inicial, Hero Framework, Barbarian, MeleeEnemyPrototype, dano/HP/morte/respawn, Ultimate básica | Deadline 1 | Player sobe/desce entre Floors via escada; inimigo persegue, ataca, morre e o Combat Scope impede dano cross-Floor num teste manual |
| 3 | 3 | **Primeiro Vertical Slice** | Um dia inteiro jogável de ponta a ponta | New Game → Dia 1 → combate real → venda → demanda → Results → Loja → Dia 2, sem intervenção no Inspector | Monster Essence, Pickup Radius, Inventory básico, Vendor, Gold, Demanda, Results, Shop esqueleto, Weapon Basic/Copper/Iron, Save real no checkpoint | Deadline 2 | O fluxo completo do slice roda do início ao fim sem intervenção manual; Save carrega corretamente o Dia 2 via Continue Game |
| 4 | 4 | Combat & Enemy Framework + Floor Sleep v1 | Combate com hordas reais + arquitetura de simulação seletiva | Vários inimigos diferentes causam dano ao jogador (golpe/disparo real via Animation Event, cada um no seu cooldown); Floors fora do atual não simulam integralmente | Enemy Framework (Melee/Ranged), timing/telegraph testado, substituído por contato/auto-disparo e depois revisado pro modelo híbrido final — attack real com Animation Event, sem contato (Sprint 16 + correção — Attack Budget removido), flanco (`MeleeAttackSlotManager`, teto de Melee simultâneos em contato, escopado por Floor), Population esqueleto, Projectile framework (+ modificadores pós-voo: gruda/explode/condição no chão/vira monstro/teleguiado), início real do Bestiary (2–3 monstros), **Floor Sleep/Activation v1** | Deadline 3 | 2+ tipos de monstro reais em cena com IA funcional causando dano ao jogador (mecanismo evoluiu de contato/projétil pro modelo híbrido final — ver nota de versão no topo do documento); população (Seção 23) continua sendo o único controle de **quantos monstros existem** no Floor — o flanco (Seção 22) controla um eixo diferente, **quantos Melee ficam em contato simultâneo**, não é o mesmo limitador e não contradiz este critério; **com 3 Floors existentes na Scene, somente o Current Floor executa simulação completa — Floors fora dele preservam Floor State (loot, Population State, Boss Timer, Floor Variant, Original Floor Identity, Active Floor Position) sem continuar rodando AI/Animator/pathfinding desnecessariamente** |
| 5 | 5 | Heróis I + Floor Content I | 3 heróis jogáveis, primeiros Floors A reais | Escolher entre Barbarian/Ranger/Mage; 2–3 Floors originais já com layout Variant A real (não testbed) | Ranger (projétil/leque/área persistente), Mage (pet/impacto/fogo persistente), início da produção de Floor 1A–3A | Deadline 4 | 3 heróis completos e balanceáveis; pelo menos 2 Floors rodando em Variant A real, com spawn/baú/trap posicionados |
| 6 | 6 | Heróis II + Bosses (Floor 1–2) | 9 heróis jogáveis, os 8 bosses de Floor 1–2 reais | Escolher entre 9 heróis; todos os bosses de Floor 1 e 2 enfrentáveis com Boss Timer funcionando | Druid completo (Sprint 21 — vinhas + Alce + Coruja, sem Floor novo — ver nota abaixo), Rogue completo, Cleric completo, Boss Framework, Boss Timer, os 8 bosses de Floor 1–2 (Mother Slime Green/Blue, Goblin King, Rat People Royalty, Werewolf, Centaur King, Cave Troll, Spider Queen) | Deadline 5 | Conversão de HP do Druid correta em teste automatizado, sem conversão em caso de morte durante a transformação; Boss Timer acumula/persiste conforme regra do GDD, inclusive quando múltiplos bosses do mesmo Floor aparecem simultaneamente (Floor 2 tem 5 ao mesmo tempo); os 8 bosses de Floor 1–2 são derrotáveis |
| 7 | 7 | Heróis III | 10 heróis do MVP completos | Todos os 10 heróis do MVP jogáveis | Paladin (projétil reto + espadas orbitais + shield), Gunslinger (hitscan em rajada por Tier + giro 8 direções), Assassin (dash contínuo + stealth completo), Blood Mage (onda em anel + pet Elemental de Sangue + extração de sangue) | Deadline 6 | Os 10 heróis passam por um checklist manual de ataque/ultimate/secundária/passiva/morte sem erro de console |
| 8 | 8 | **Torre Completa A** | Torre inteira em qualidade final, do térreo ao Floor 10, com UI de gameplay pronta | Run estrutural completa Ground→Floor 10 em Variant A, tileset final (mesma qualidade de Floor 1/2), Bestiário completo e UI final de gameplay | 15 LootDefinitions + agregação visual, Floor 3A–10A em tileset final (não rascunho), Bestiário completo dos Andares 3–10 (62 fichas comuns + 22 bosses), Floor Sleep validado em escala real (11 Floors), UI final de gameplay completa (loot, vida, ataque, ultimate/shift, temporizador do dia, Demanda, bag) — **foco 100% em uso dos assets/sistemas já existentes, sem sistema novo de código** | Deadlines 6–7 (heróis completos, Boss Framework maduro) | Uma run de teste visita todos os 10 Floors originais em sequência sem erro; todos os 15 materiais dropam; nenhum monstro/boss planejado dos Andares 1–10 falta; UI final de gameplay funcional, sem placeholder; profiling confirma Floor Sleep estável com 11 Floors reais |
| 9 | 9 | Economia Expandida + Chest/Card Base + **Início Variant B** | Decisões econômicas reais na loja | Loja oferece build real: arma, bonuses, cartas de baú | 15 Weapon Tiers com dados completos, Bonuses framework + itens iniciais (Add Time, Slots, Stack, Pickup Radius, filtros), Chest Framework, Card Framework, UI de 3 opções, reroll base, Mimic, **início da produção de Floor Variants B** | Deadline 8 | Compra sequencial dos 15 tiers funciona; abrir um baú mostra 3 cartas sem tipos repetidos e aplica o buff Run-Persistent corretamente |
| 10 | 10 | Employees — Fase 1 (Funcional) + **Continuidade B / Início C** | Automação básica existe de verdade | 1 Ajudante e 1 Coletor funcionam em campo | Employee Definition, compra/venda/promoção básica, Helper e Collector funcionais, integração com Shop/Loot/Combat/Save; **continuidade de Variants B e início de Variants C** | Deadline 9 | Um Ajudante mata um monstro e um Coletor vende Essência automaticamente numa run de teste, refletindo corretamente no Gold e na demanda; **checkpoint de produção de mapas realizado (ver nota abaixo da tabela)** |
| 11 | 11 | Employees — Fase 2 (Escala) + Quests + **Continuidade C / Início D** | Fantasia de automação avançada + Quests principais | Dezenas de Employees em campo; Magnet vendendo loot automaticamente | Strong/Fast, árvore de promoção completa, virtualização, cross-Floor Collector, profiling, Collector Filter, Magnet (integrado ao Pickup Radius), Chest Pointer, Chaos Crystal; **continuidade de Variants C e início de Variants D** | Deadline 10 | Profiling mostra que 1000+ Employees "possuídos" não travam o frame rate; Magnet vende Monster Essence e isso conta para a demanda no Modo Padrão |
| 12 | 12 | Run Completa + Meta Systems + **Continuidade D / Início E** + Steamworks Groundwork | Todas as mecânicas estruturais do GDD integradas | Uma run Dia 1–30 é jogável de ponta a ponta, nos dois modos | Remove Tower Layer, Remote Controller, Free Mode, Progress Tracking completo (3 escopos + mode-aware), hero unlocks, achievements internos, **Steamworks App configuration, App ID/ambiente de teste, achievements em ambiente de teste, build de teste, estrutura inicial de depots/branches**; **continuidade de Variants D e início de Variants E** | Deadline 11 | Remove Tower Layer remapeia escadas corretamente num teste automatizado; Free Mode completa um dia sem validar demanda; ao menos 1 herói desbloqueia via critério real |
| 13 | 13 | **Content Complete + Balance + Store Preparation** | Conteúdo do MVP completo e calibrado, comercialmente preparado | Jogo completo com as 50 Floor Variants, dificuldade calibrada, Steam page pronta para revisão | **Finalização da Variant E e qualquer Variant atrasada**, Bestiary e bosses restantes, cartas restantes, balance pass (economia/Attack Budget/Population/scaling de arma), localização de conteúdo, passe de performance, full-run playtests, **Steam Store Page (descrição, tags, requisitos, screenshots, capsule assets, trailer), configuração/teste de depots, testing branch, checklist de publicação** | Deadlines 9–12 (produção gradual de Variants já em andamento) | Nenhuma Floor Variant pendente; 3 playtests completos de Dia 1 a 30 sem bug bloqueante; métricas de ritmo econômico dentro do ciclo emocional definido no GDD (Seção 20); Store Page pronta para revisão |
| 14 | 14 | Stabilization & Release Candidate | Build shippable | Jogo pronto para lançar | Bug fixing (P0/P1/P2), regression completa, save/load validado, performance final, **validação (não criação) de achievements/build/depot/branch Steam, upload final, checklist de publicação**, settings, validação de localização, polish residual de UI/áudio, Release Candidate | Deadline 13 | Zero bugs P0/P1 abertos; build final roda em máquina limpa; save/load validado em pelo menos 5 cenários de checkpoint diferentes; Release Candidate aprovado; Steam pronto para publicação sem nenhuma integração começando do zero |

---

## Explicação por Deadline

### Deadline 1 — Fundação Técnica Completa
**Por que agora?** Todo sistema que nascer depois vai precisar se conectar a GameEvents, Save, Localization, Large Number e Progress Tracker — construí-los primeiro é mais barato que retrofitar.
**Principais entregas:** projeto Unity configurado, Git/GitHub Pages funcionando, os 6 esqueletos fundacionais (GameEvents, Time/Pause, Localization, Game State, Save/RunState, Large Number, Progress Tracker) e o Test Framework.
**Riscos:** tentação de "arquitetura astronauta" — construir abstrações demais antes de qualquer gameplay existir.
**Contingência:** se o mês ficar apertado, os esqueletos podem ser deliberadamente mínimos (uma classe + um teste), desde que o *padrão* de uso esteja documentado para os sistemas futuros seguirem.

### Deadline 2 — Floor + Combat Testbed
**Por que agora?** Floor System é a dependência mais transversal do jogo; validar Stair Routing e Combat Scope cedo, mesmo com arte placeholder, evita retrabalho em cascata depois.
**Principais entregas:** Scene única com 2–3 Floors placeholder navegáveis, Barbarian com ataque/ultimate/morte, um inimigo real (não dummy).
**Riscos:** subestimar a complexidade do Stair Routing relativo (não fixo por índice).
**Contingência:** se o roteamento completo não fechar na semana, aceitar temporariamente rotas fixas documentadas como dívida técnica, com prazo de resolução antes da Deadline 8 (Remove Tower Layer depende disso).

### Deadline 3 — Primeiro Vertical Slice
**Por que agora?** É o menor recorte que já "parece o jogo" — expõe problemas de integração entre Floor/Combat/Loot/Economia/Save enquanto ainda são baratos de corrigir.
**Principais entregas:** ciclo de dia completo, Save real, Shop mínimo.
**Riscos:** Results Screen ou Save absorverem tempo desproporcional por serem "a última peça antes do slice fechar".
**Contingência:** Results pode nascer com layout mínimo (lista de texto) — visual entra depois, o que importa é a lógica de resumir vendas corretamente.

### Deadline 4 — Combat & Enemy Framework + Floor Sleep v1
**Por que agora?** Framework de inimigo precisa existir antes de qualquer conteúdo de Bestiary; e a arquitetura de Scene única (maior risco técnico do projeto) precisa ganhar um dono explícito cedo, validada num caso pequeno (3 Floors) antes de escalar para a torre inteira na Deadline 8.
**Principais entregas:** Melee/Ranged genéricos, timing/telegraph de ataque + Attack Budget (Sprints 13-14) testados na prática contra 3 monstros reais na Sprint 16 — **contingência acionada**: modelo substituído por dano de contato/auto-disparo com cooldown próprio; testado de novo na correção da Sprint 16 e revisado pra decisão final: cada monstro comum volta a ter uma animação `attack`/conjuração real (com Animation Event decidindo o golpe/disparo — trigger direcional pro Melee, sem telegraph pro Ranged), **dano de contato removido por completo** exceto pros Slimes (única exceção permanente); IA de patrulha nova (`idle`/`walk` aleatório + `idle_combat`, GDD Seção 22), morte orientada por Animation Event; Attack Budget removido em definitivo (GDD Seção 14); e a primeira versão de Floor Sleep/Activation: Current Floor com simulação completa, Floors fora dele com sistemas caros suspensos mas Floor State preservado (loot, Population State, Boss Timer, Floor Variant, Original Floor Identity, Active Floor Position).
**Riscos:** o risco original ("Attack Budget mal calibrado") foi encerrado por remoção do sistema, não por calibração — ver "Nota de versão" no topo deste documento; risco atual é a dívida de rework nas Sprints 13/14 (a state machine de Telegraph/Hitbox/Recovery e `AttackBudgetTracker`/`AttackBudgetManager` viram código morto a remover, não só a ajustar) e Floor Sleep v1 subestimando quanto precisa ser preservado versus suspenso.
**Contingência:** se Floor Sleep não fechar de forma elegante, aceitar uma versão simplificada (ex.: suspender só Update de IA, sem otimizar Animator ainda) documentada como dívida técnica com prazo de resolução até a Deadline 8.
**Estado: Deadline 4 confirmada fechada.** As duas pendências que restavam foram resolvidas: (1) Spectre (boss Andar 5) — decisão tomada, ganha `attack` real igual a qualquer Melee (só o `idle` continua dobrando como `walk`), mais uma superfície de gelo no golpe que conectar; ficha atualizada no Bestiário. (2) Floor Sleep — re-testado manualmente sobre a arquitetura híbrida final, incluindo o caso de borda "matar um monstro no exato instante de trocar de Floor" (o dano e o drop de loot aconteceram antes da troca completar, o loot persistiu e foi coletável ao voltar) — sem regressão.

**Lição aprendida no re-teste, vale registrar pros próximos Batches de Bestiary:** o primeiro teste manual revelou monstros perseguindo o player entre Floors sem nunca perder o alvo. Causa: `FloorActivationCheck.IsActive` trata `ownerFloor == null` como "sempre ativo" (fallback de compatibilidade proposital, pra objetos sem Floor dono) — mas isso também mascara silenciosamente um monstro que **deveria** ter `ownerFloor` preenchido e ficou sem, por esquecimento no Inspector do prefab/instância. Não gera erro nem warning, só o monstro nunca dorme. Preenchido o campo, o comportamento ficou correto. Como os próximos Batches vão colocar dezenas de monstros novos em cena, vale um checklist manual (ou futuramente uma validação automática) conferindo que todo monstro em cena tem `Owner Floor` atribuído antes de considerar aquele Floor "pronto".

### Deadline 5 — Heróis I + Floor Content I
**Por que agora?** Ranger e Mage validam famílias técnicas (leque, área persistente, pet) que o framework de habilidades precisa suportar cedo; produção de Floor A começa em paralelo alternado para não concentrar depois.
**Principais entregas:** 3 heróis, 2–3 Floors A reais.
**Riscos:** pet do Mage (summon-lock, orçamento ofensivo) subestimado tecnicamente.
**Contingência:** se o pet atrasar, entregar o Mage sem a passiva por alguns dias e completar na sprint seguinte — não travar o herói inteiro por causa da passiva.
**Nota (pós-Sprint 17):** a Sprint 17 devia ser só "Ranger — Primário", mas virou a sprint que construiu a infraestrutura de Animator de herói do zero (nenhum herói tinha isso, nem o Barbarian desde a Sprint 7) — mesmo padrão do que a Sprint 16 foi pro lado dos monstros. `HeroController` ganhou Animator real, `AimX`/`AimY`, cooldown universal de ataque, aggro instantâneo, morte adiada por Animation Event, knockback e Floating Combat Text — tudo isso compartilhado, não específico do Ranger. Consequência prática: **Mage (Sprint 19) e os heróis das Deadlines 6/7 partem de uma base pronta que o Ranger teve que construir do zero** — o custo real desses heróis deve ser menor do que a tabela original assumia, mesmo padrão de folga que o `MonsterAnimationGeneratorWindow` trouxe pro Bestiário (ver nota da Deadline 4). Ver linha da Sprint 17 corrigida abaixo pro escopo real entregue.

**Nota (pós-Sprint 18, inserção da Sprint 18b):** antes de abrir o Mage, entrou uma sprint fora da tabela original — GDD Seção 16 ganhou o conceito "Habilidade Secundária (Shift)": todo herói MVP tem uma terceira habilidade além de primário/ultimate. Barbarian e Ranger (os dois heróis já completos) saíram desta sprint com o Shift de verdade implementado (arquitetura genérica nova em `HeroController`: cooldown próprio, `UseSecondaryAbility()`/`CancelSecondaryAbility()`, flag `IsPlayerUntargetable` reaproveitável por Druid/Assassin depois); as outras 8 habilidades secundárias (incluindo a do Mage) ficam só especificadas no GDD até a sprint de cada herói abrir. Junto: bosses ganharam imunidade a knockback (`isBoss`) e o ESC passou a pausar o jogo (só o input, sem menu). **Mesmo precedente da Sprint "16-correção" (Deadline 4): sprint real fora da numeração original, sem renumerar o resto — aqui registrada como Sprint 18b**, entre a 18 e a 19, sem deslocar Sprint 19/20. **Consequência direta na distribuição do Mage:** como o Mage agora nasce já com a arquitetura de Shift pronta (reaproveitada, não construída do zero), o usuário decidiu fechar o Mage inteiro numa sprint só — **Sprint 19 passa a ser "Mage — Completo" (primário + ultimate + secundária/Shift + Pet Phoenix)**, absorvendo o que antes era a metade do Mage na Sprint 20. A Sprint 20 perde a parte de Mage e fica só com Floor 1A–2A + Bestiary Batch 1+2 — sem mudança na entrega real dessas duas frentes, só na sprint em que o Mage terminava. Ver linhas 18b/19/20 corrigidas abaixo.

**Nota (pós-Sprint 19, inserção da Sprint 19b):** mesmo precedente de novo — depois de fechar o Mage, entrou outra sprint fora da tabela original antes de abrir o Floor Content. Testando o Mage em Play Mode apareceram bugs de base reais que afetavam **todo herói**, não só ele (mira presa depois de ações longas, o "micro-teleport" do Shift do Mage, espelhamento perto dos eixos cardeais em Blend Trees só-diagonal) — corrigi-los exigiu mexer em `HeroController` de ponta a ponta, então virou sprint própria em vez de um adendo na 19. Aproveitando que a base de mira já estava em revisão, o sistema de Efeitos Nocivos (só conceito no GDD até então) foi construído de verdade: `StatusEffectController`/`IDamageable`, compartilhado entre herói e monstro, com Fire (Mage) e Bleeding (Ranger) como primeiras mecânicas reais, e imunidade por tipo configurável. **Registrada como Sprint 19b**, entre a 19 e a 20, sem deslocar a numeração — a Sprint 20 (Floor 1A–2A + Bestiary Batch 1+2) seguiu com o mesmo conteúdo, só a dependência mudou pra "Sprint 19b". Ver linha 19b nova e dependência da 20 corrigida abaixo.

**Nota (pós-Sprint 20):** mesmo padrão da Sprint 17 (não da 18b/19b) — a sprint estourou o escopo mas **ficou registrada como Sprint 20 mesmo**, sem virar "20b", porque não é um adendo depois de fechada: o breakdown original (conteúdo — 10 fichas de Bestiário + Floor 1A/2A) foi todo entregue, e no meio do caminho uma discussão de performance/pathfinding virou decisão de arquitetura real — integração do **A* Pathfinding Project Pro + RVO Local Avoidance** nos 14 monstros comuns do jogo, substituindo o `transform.Translate` cru usado desde a Sprint 16. Isso trouxe consigo: `FloorPopulationManager` reescrito pra sortear ponto caminhável direto no `GridGraph` (não mais `spawnPoints[]` manuais) e ganhou um modelo de crise/escalação de população; `MeleeAttackSlotManager` ganhou prioridade de RVO por dano; `Stair.cs` reformulado (`arrivalPoint` + transição sem "pulo" de câmera); e os 3 heróis existentes (Barbarian/Ranger/Mage) ganharam `Linear Damping` no `Rigidbody2D` pra não serem empurrados pela física do pathfinding dos monstros. Nenhuma mudança de GDD — é infraestrutura de movimento, não de design/comportamento. Ver linha da Sprint 20 corrigida abaixo pro escopo real entregue.

**Estado: Deadline 5 confirmada fechada.** 3 heróis completos (Barbarian, Ranger, Mage — cada um com primário/ultimate/secundária, mais Barbarian e Ranger com o kit desde a 18b), sistema de Efeitos Nocivos funcionando dos dois lados (19b), Floor 1A e 2A jogáveis com o elenco completo de Floors 1–2 rodando em cima de pathfinding real + RVO (20). Nenhuma pendência em aberto.

### Deadline 6 — Heróis II + Bosses (Floor 1–2)
**Por que agora?** Druid é o herói mais arriscado tecnicamente (transformação + conversão proporcional de HP) — melhor validar cedo que perto do fim. Boss Framework entra assim que a IA comum estiver madura (Deadline 4 concluída).
**Nota (reestruturação pós-Sprint 21):** o escopo desta Deadline mudou em relação às revisões anteriores deste documento. A Sprint 21 fechou como "Druid completo" (primário + ultimate + secundária, as 3 ações do herói implementadas juntas por compartilharem estado profundamente acoplado — mesmo precedente das Sprints 17/18b/19b/20) — **sem** o Floor 3A–4A que estava planejado junto dela. O motivo é uma decisão maior: a partir daqui, toda a frente "heróis + combate" (heróis restantes das Deadlines 6–7, Boss Framework, e agora também **todos** os bosses de Floor 1–2, não só "o primeiro boss") fica agrupada nas Deadlines 6–7, sem interromper com produção de Floor/Bestiário no meio — o raciocínio completo (facilidade de achar bugs de base numa sequência de tarefas do mesmo tipo) está na Quinta nota, no topo deste documento, e no relatório `docs/sprints/sprint-21.md`. O Floor 3A–4A (e o resto do Floor Content de Floors 3–10) migra inteiro para a Deadline 8, que passa a ser dedicada só a isso.
**Nota pós-Sprint 16 (correção, mantida das revisões anteriores):** a maioria dos 30 bosses (definida ficha a ficha no Bestiário) reaproveita a base comum híbrida — **mantém a animação `attack` real com Animation Event**, só perdendo a complexidade extra que tinha antes (telegraph, dano em área, múltiplos hits, sequências). Só um grupo pequeno e nomeado precisa de arquitetura própria além do padrão; dos 7 bosses de Floor 1–2 desta Deadline, **Mother Slime Green/Blue** ficam no dano de contato simples (igual Slime comum, mais o spawn de filhotes), **Rat People Royalty** e **Spider Queen** são exceções bespoke (arquitetura própria por Animation Event), e **Goblin King, Werewolf, Centaur King** seguem o padrão genérico do Boss Framework.
**Correção (pós-Sprint 23):** Cave Troll deixou de ser boss de Floor 2 — movido pro Floor 3 (ver Bestiário), onde entra junto do resto do conteúdo de Floor 3 na Deadline 8. Por isso o total de bosses de Floor 1–2 caiu de 8 pra 7.
**Principais entregas:** Druid (✅ Sprint 21), Rogue completo, Cleric completo, Boss Framework, Boss Timer, os 7 bosses de Floor 1–2.
**Riscos:** conversão de HP do Druid com edge cases (morte durante transformação) mal cobertos — já mitigado na prática pelo teste manual T09 da Sprint 21 (ver relatório), mas ainda sem teste automatizado; o Boss Timer virou um intervalo periódico de 10s que empilha bosses sem fila nem limite (GDD Seção 22, correção Sprint 24) — Boss Framework precisa suportar múltiplos bosses vivos ao mesmo tempo num Floor com 4 bosses (Floor 2) desde o desenho inicial, não como adendo.
**Contingência:** escrever o teste automatizado da conversão de HP do Druid antes de fechar a Deadline — é barato de testar e caro de debugar depois; se o empilhamento de bosses em Floor 2 se provar difícil de calibrar (legibilidade/dificuldade), aceitar uma versão inicial sem escalonamento de dificuldade entre eles, documentada como dívida técnica de balanceamento (não de arquitetura) com prazo até a Deadline 13.

### Deadline 7 — Heróis III
**Por que agora?** Fecha o framework de habilidades contra toda a variedade real do GDD (hitscan, dash/stealth, lifesteal, área em duas fases) antes de qualquer polimento. Sem Floor/Bestiário misturado (motivo na nota da Deadline 6) — é a segunda e última Deadline só de heróis.
**Principais entregas:** Paladin, Gunslinger, Assassin e Blood Mage completos — os 10 heróis do MVP.
**Riscos:** acúmulo de pequenos bugs de interação entre 10 kits diferentes e o Combat Scope; Gunslinger e Paladin compartilham o mesmo mecanismo de 4 triggers fixos (shield bash/chicote) — regressão num afeta o outro.
**Contingência:** reservar a última sprint do mês só para checklist manual dos 10 heróis, não para features novas.

> **Nota — o que significa "os 10 heróis completos" ao final da Deadline 7:** significa **Gameplay Complete**, não polish final. Ou seja: ataque, ultimate e secundária/passiva (quando houver) funcionais; morte/vida corretas; integração com Combat Scope, com o sistema de pausa e com upgrades/cards funcionando estruturalmente; sem erros críticos de console. **Não** significa VFX final, áudio final, balance final ou animação perfeita — esses elementos continuam refináveis nas Deadlines seguintes, inclusive durante o Balance Pass da Deadline 13. Essa distinção vale para evitar que a decomposição das 67 sprints trate "herói completo" como sinônimo de "herói polido".

**Nota (pós-Sprint 27, inserção da Sprint 27b):** mesmo precedente da 18b/19b — depois de fechar o Paladin, apareceu um problema de arquitetura real que afetava **todos os 7 heróis existentes**, não só ele: valor de upgrade duplicado entre o controlador e o prefab filho/projétil que ele instancia (um upgrade futuro precisaria editar 2+ lugares pro mesmo efeito). Corrigir isso — mais o primeiro parâmetro de Animator de upgrade de verdade (`ActionSpeedMultiplier`) e multiplicadores de tamanho em Barbarian/Mage — exigiu mexer em `HeroController` e nos 7 heróis de ponta a ponta, então virou sprint própria em vez de um adendo na 27. Aproveitando a janela, também entraram 2 bugs de base achados testando o Mage (explosão da Ultimate inconsistente, pet deslizando durante o Summon). **Registrada como Sprint 27b**, entre a 27 e a 28, sem deslocar a numeração — a Sprint 28 (Gunslinger) segue com o mesmo conteúdo, só a dependência mudou pra "Sprint 27b". Ver linha 27b nova e dependência da 28 corrigida abaixo.

### Deadline 8 — Torre Completa A
**Por que agora?** Com heróis, Boss Framework e todos os bosses de Floor 1–2 fechados nas Deadlines 6–7, esta é a Deadline "foco único" da produção: 8 Floors seguidos (3A–10A) em qualidade final, mesmo tipo de trabalho (tileset + level design + conteúdo de Bestiário), sem interromper com sistemas de código no meio — o espelho exato do raciocínio que levou a agrupar os heróis nas Deadlines 6–7 (ver Quinta nota, topo do documento). É também o segundo grande milestone de integração do projeto (a torre inteira, não só um Floor, funcionando estruturalmente) e o ponto de validar em escala o Floor Sleep/Activation que nasceu na Deadline 4 — de 3 Floors de teste para os 10 Floors reais mais o térreo. **Não deveria nascer sistema novo de código aqui** — tudo (Boss Framework, Enemy Framework, `MonsterAnimationGeneratorWindow`, A*/RVO, `FloorPopulationManager` baseado em `GridGraph`) já existe e foi validado em Floor 1–2; esta Deadline é 100% aplicação desses sistemas em conteúdo novo.
**Principais entregas (13 sprints — a maior Deadline do projeto):**
- 15 LootDefinitions + agregação visual (2 sprints, igual à revisão anterior).
- Floor 3A–10A em tileset final, com o Bestiário completo dos Andares 3–10 (62 fichas comuns — 9+6+20+8+5+3+7+4 por Floor, incluindo as exceções Orc Shaman, Burning Skull, Skeleton Mage e Zombie Mage; os 14 comuns de Floor 1–2 já foram entregues na Sprint 20/Deadline 5, fora do escopo desta Deadline) e os 22 bosses correspondentes (incluindo as exceções Dark Channeler, Lich, Dragon, Undead Dragon e Divine God) — organizados **1 sprint por Floor**, exceto o Andar 5 (sozinho tem 20 fichas comuns + 6 bosses, o maior volume do Bestiário — ganha 2 sprints) e os Andares 8–9 (volume baixo o suficiente pra dividir 1 sprint) — 8 sprints no total.
- UI final de gameplay completa: loot, vida, ataque, ultimate e shift (cooldown ou energia atual conforme o caso), temporizador do dia, Demanda, bag — 2 sprints.
- Full-Run Test (Ground→Floor 10) + validação de Floor Sleep em escala real (11 Floors) — 1 sprint, ao final.
**Riscos:** algum Floor A ficar estruturalmente incompleto (sem baú/trap posicionado) e passar despercebido; Floor Sleep não escalar bem de 3 para 11 Floors; o Andar 5 (maior volume) e as exceções bespoke (Dark Channeler, Lich, Dragon, Undead Dragon, Divine God) subestimarem o tempo real de produção, já que cada uma exige arquitetura própria por Animation Event além do padrão; o volume total (62 comuns + 22 bosses) é uma estimativa de planejamento, não uma contagem final — a distribuição por sprint pode precisar de ajuste fino conforme a produção real andar, sem que isso mude o total de 13 sprints como teto inicial.
**Contingência:** checklist objetivo por Floor (entrada, saída, escada, ao menos 1 posição de baú, população mínima) antes de considerar qualquer Floor "pronto"; se algum Floor com exceção bespoke atrasar, isolar a exceção numa sprint própria em vez de atrasar o Floor inteiro (mesmo padrão já usado com o Goblin Sapper); se o profiling revelar problema de escala no Floor Sleep, tratar como dívida técnica com prioridade P1 antes de avançar para Employees (Deadline 10), já que Employees cross-Floor dependem dessa base estar sólida.

### Deadline 9 — Economia Expandida + Chest/Card Base + Início Variant B
**Por que agora?** Com a torre completa, faz sentido aprofundar a progressão econômica; Chest/Card Framework depende do sistema de pausa e da UI básica já existirem desde a Deadline 1. A produção de Floor Variants B começa aqui, logo após o Floor Framework estar validado em escala (Deadline 8) — não antes.
**Principais entregas:** 15 tiers de arma, Bonuses iniciais, Chest/Card completo com Mimic, primeiras Floor Variants B.
**Riscos:** pool de cartas (universal/herói/employee) crescer em complexidade de dados mais do que o esperado.
**Contingência:** começar só com o pool universal + 2–3 cartas por herói; completar o resto na Deadline 13 (Content Complete).

### Deadline 10 — Employees Fase 1 (Funcional) + Continuidade B / Início C
**Por que agora?** Combat, Loot, Economia e Floors já maduros — a dependência real que o GDD já declarava para Employees existirem. A produção de mapas segue em ritmo: B continua, C começa.
**Principais entregas:** compra/venda/promoção básica, 1 Ajudante, 1 Coletor, Variants B avançando e C iniciando.
**Riscos:** tentar validar virtualização/escala já nesta fase, misturando as duas Deadlines de Employees; ritmo de produção de mapas ficar atrás do necessário para os 50 até a Deadline 13.
**Contingência:** limitar deliberadamente a quantidade de Employees testados nesta fase (dezenas, não milhares) — escala é Deadline 11.

> **Checkpoint de produção de mapas (fim da Deadline 10):** medir quantas Floor Variants extras (B/C) foram efetivamente produzidas até aqui e comparar com a projeção necessária para chegar às 50 até a Deadline 13. Se estiver atrasado, redistribuir carga das Deadlines 11/12 e reduzir polish não essencial daquelas semanas — **nunca cortar Floor Variants confirmadas do MVP** para compensar o atraso.

### Deadline 11 — Employees Fase 2 (Escala) + Quests + Continuidade C / Início D
**Por que agora?** Escala é, na prática, um problema de performance separado do funcional; Quests (Magnet) só fazem sentido depois do Pickup Radius e do Coletor existirem. Mapas: C é concluída, D começa.
**Principais entregas:** Strong/Fast, virtualização, cross-Floor, Magnet, Chest Pointer, Chaos Crystal, Variants C finalizando e D iniciando.
**Riscos:** virtualização de Employees em escala é o risco técnico mais alto do projeto depois do Floor System.
**Contingência:** se a virtualização "elegante" não fechar a tempo, aceitar uma versão simplificada (cap de representantes simulados fixo) documentada como dívida técnica com prazo até a Deadline 13.

### Deadline 12 — Run Completa + Meta Systems + Continuidade D / Início E + Steamworks Groundwork
**Por que agora?** Remove Tower Layer e Remote Controller exigem Stair Routing maduro (Deadline 2/8); Free Mode exige o Padrão estável; faz sentido fechar toda a máquina de estados junto. Steam começa aqui como groundwork técnico (não comercial ainda), para a Deadline 13 poder focar na parte de produção/loja. Mapas: D é concluída, E começa.
**Principais entregas:** Remove Tower Layer, Remote Controller, Free Mode, Progress Tracking completo, primeiro unlock real de herói, Steamworks App configurado com App ID de teste, achievements em ambiente de teste, build de teste, estrutura inicial de depots/branches, Variants D finalizando e E iniciando.
**Riscos:** Remove Tower Layer remapeando escadas incorretamente em casos de borda (remover o 5º Floor consecutivo).
**Contingência:** testes automatizados específicos para as 5 remoções possíveis antes de considerar o sistema fechado.

### Deadline 13 — Content Complete + Balance + Store Preparation
**Por que agora?** Toda a produção gradual de Floor Variants B–E das Deadlines 9–12 converge aqui para **finalização** (só a Variant E e qualquer atrasada), nunca para início de uma família inteira de mapas. Em paralelo, a preparação comercial da Steam Store Page começa aqui — depois do groundwork técnico da Deadline 12 — para a Deadline 14 ser só validação e publicação.
**Principais entregas:** finalização da Variant E e atrasados, Bestiary/bosses restantes, cartas restantes, balance pass completo, localização de conteúdo, performance, Steam Store Page (descrição, tags, requisitos, screenshots, capsule assets, trailer), depots/testing branch configurados.
**Riscos:** descobrir tarde que a produção gradual de Variants ficou atrasada e esta Deadline vira a antiga "40 mapas de uma vez"; store assets (trailer/screenshots) competindo por tempo com o balance pass.
**Contingência:** o checkpoint da Deadline 10 existe justamente para evitar essa surpresa; se mesmo assim houver atraso, priorizar completar as Floor Variants sobre o polish dos assets de loja (a Store Page pode ser finalizada até o início da Deadline 14, desde que não seja código).

### Deadline 14 — Stabilization & Release Candidate
**Por que agora?** É o fim natural da cadeia Fundação→Slice→Framework→Conteúdo→Run Completa→Balance.
**Principais entregas:** bug fixing, regression, **validação (não criação) de achievements/build/depot/branch Steam**, upload final, checklist de publicação, Release Candidate.
**Riscos:** bugs P0/P1 descobertos tarde demais para corrigir com segurança.
**Contingência:** se surgir um P0 estrutural nesta Deadline, a prioridade é cortar escopo de polish, nunca adiar a correção — build shippable é inegociável.

---

# Macro Roadmap — Freeze ✅

**A estrutura dos 14 meses está aprovada e congelada como baseline de produção.** Mudanças futuras devem ocorrer apenas se a execução real das sprints demonstrar atraso, bloqueio técnico ou necessidade de replanejamento — nunca por preferência editorial. A ordem macro (Fundação → Floor+Combat Testbed → Vertical Slice → Enemy Framework+Floor Sleep → Heróis I/II/III em paralelo alternado com conteúdo de Floor/Bestiary/Bosses → Torre Variant A completa → Economia/Chest/Card → Employees Fase 1/2 → Run Completa/Meta Systems → Content Complete/Balance/Store Prep → Stabilization) não deve ser reaberta por este documento novamente.


---

# PARTE 2 — TABELA RESUMIDA DAS 67 SPRINTS (Correção Mecânica Final + Emenda pós-Sprint 21)

> **Nota:** este cabeçalho e o parágrafo abaixo registram a "Correção Mecânica Final" original, feita ainda sob a numeração de 56 sprints (por isso cita S23/24/27/28, S49/50 e S35 — números que valiam *naquela* revisão, antes da reestruturação da Deadline 6 em diante). Os números de sprint mudaram a partir da Sprint 21 (ver Quinta nota, topo do documento, e a "AUDITORIA MECÂNICA FINAL" abaixo, que é a versão atualizada e vale como referência corrente); o conteúdo histórico do parágrafo é mantido como registro, não como numeração vigente.

Última correção mecânica (histórica, numeração de 56 sprints). As 14 Deadlines e os intervalos de sprint **não mudaram nessa rodada**. Correções desta rodada: Variants D/E deixaram de estar concentradas em S49/S50 (agora distribuídas em batches de até 4 desde a Deadline 11); sprints de herói sobrecarregadas (S23/24/27/28) foram aliviadas dentro da própria Deadline; Main Menu/New Game/Continue/Game Over/Day 15/Day 30 ganharam sprint responsável; Employee Sell foi adicionado; Collector Filter tem dono; Cards de herói só entram depois do Card Framework (S35); HUD, Death Flow, Settings e Store Prep tornaram-se progressivos em vez de concentrados no fim.

## Deadline 1 — Fundação Técnica Completa (Sprints 1–4)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 1 | 1 | Setup do Projeto + Git + Docs Skeleton | Projeto versionado com documentação já tendo onde morar | Repositório no GitHub abrindo sem erros; GitHub Pages publicado com Home/GDD/Sprint Reports em esqueleto | — |
| 2 | 2 | Input System + Organização de Projeto | Estrutura de input e hierarquia consistentes | Player placeholder responde a WASD/Mouse/LMB/RMB/E/TAB/Q; Hierarchy organizada com HierarchySectionHeader | Sprint 1 |
| 3 | 3 | GameEvents + Time/Pause + Game State + Progress Tracker Skeleton + Testes | Núcleo reativo e de estatísticas funcionando | `EnemyKilledEvent` de teste dispara via GameEvents, Progress Tracker incrementa um counter; pausar via Q/TAB congela um timer de teste; 1 teste automatizado passa | Sprint 2 |
| 4 | 4 | Localization + Save/RunState + Large Number + Docs Pipeline Maduro | Esqueletos fundacionais completos | Troca de idioma de teste funcional; RunState salva/carrega um valor de exemplo; "1.5m" formatado corretamente; DocFX/GitHub Actions publicando automaticamente | Sprint 3 |

## Deadline 2 — Floor + Combat Testbed (Sprints 5–8)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 5 | 2 | Floor System Skeleton | Scene única com múltiplos Floors navegáveis | Ground + 2 Floors placeholder na mesma Scene, Current Floor identificado corretamente | Sprint 4 |
| 6 | 2 | Stair Routing + Active Floor Position | Travessia entre Floors por posição relativa | Subir/descer teleporta corretamente mesmo trocando a ordem dos Floors manualmente; Floor indicator placeholder mostra o Floor atual na tela | Sprint 5 |
| 7 | 2 | Hero Framework + Barbarian | Primeiro herói jogável, Ultimate Energy framework definido | Barbarian se move, ataca em área e usa a ultimate; Energia carrega só por kill (nunca por tempo), zera ao usar/morrer/fim do dia; Energy HUD placeholder visível | Sprint 6 |
| 8 | 2 | MeleeEnemyPrototype + Death Flow (fase 1) | Primeiro combate real completo | Inimigo persegue, ataca, recebe dano e morre; Barbarian morre (HP zero → cancela estados → -30s hook → respawn no Ground com HP cheio e Energia zerada); Health HUD placeholder visível | Sprint 7 |

## Deadline 3 — Primeiro Vertical Slice (Sprints 9–12)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 9 | 3 | Main Menu + Run Creation Flow + Loot Básico | Fluxo de menu real + primeiro loot | New Game → Mode Select → Hero Select (só Barbarian) → Map Select (só Torre) → Create Run instancia o RunState; Monster Essence dropa e é coletada via Pickup Radius | Sprint 8 |
| 10 | 3 | Inventory + Vendor + Gold | Venda funcional | Jogador vende Essência no NPC do térreo e o Gold aumenta | Sprint 9 |
| 11 | 3 | Demanda + Results + Shop Skeleton + Game Over Funcional | Ciclo econômico do dia fecha, incluindo falha | Demanda valida corretamente; Results mostra o que foi vendido; loot ainda na Bag é destruído ao fim do dia; se a demanda falha, Game Over funcional leva ao Menu sem apagar o save; Demand HUD (`X/Y Monster Essence`) visível | Sprint 10 |
| 12 | 3 | Save Real + Continue Game + Ciclo de Dia Completo | **Vertical Slice fechado** | New Game → Dia 1 → combate → venda → demanda → Results → Loja → compra → Save real no checkpoint → Start Day 2; Continue Game lê o save e abre a Loja do checkpoint; Continue fica desabilitado sem save existente; New Game não exige confirmação e não apaga o save antigo antes do primeiro autosave | Sprint 11 |

## Deadline 4 — Enemy Framework + Floor Sleep v1 (Sprints 13–16)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 13 | 4 | Enemy Framework Genérico | Base reutilizável para monstros | Melee e Ranged genéricos funcionam com timing/telegraph configurável | Sprint 12 |
| 14 | 4 | Attack Budget + Population Skeleton | Hordas legíveis | Com 10+ inimigos no Floor, apenas N atacam simultaneamente (budget visível) | Sprint 13 |
| 15 | 4 | Floor Sleep/Activation v1 | Simulação seletiva por Floor | Com 3 Floors, só o Current Floor roda IA completa; os outros preservam estado | Sprint 6, 14 |
| 16 | 4 | Teste de Combate Real + Duas Rodadas de Pivot Arquitetural (correção) | Decisão de arquitetura de combate comum tomada, testada duas vezes e implementada na forma final | Rat/Goblin/Rat People (os 3 do teste) rodando no modelo híbrido definitivo (GDD v1.01238): `attack`/conjuração real por Animation Event (trigger direcional fixo pro Melee, disparo mirado na hora pro Ranged), **dano de contato removido** (só Slimes ficam nele, exceção permanente); `MoveX/MoveY` (movimento) separado de `AimX/AimY` (mira, sempre recalculada pro jogador); detecção troca pra combate imediatamente — o atraso de reação passou a ser só o Exit Time do Animator, não mais um timer no código (bug real corrigido: chegava a atrasar até 4s); movimento só translada quando o Animator já está no estado `Walk`; IA de patrulha (`idle`/`walk` aleatório) + `idle_combat`; morte por Animation Event (`AnimationDieEndEvent`) com timeout de segurança; `AttackBudgetTracker`/`AttackBudgetManager`/`AttackType` removidos do projeto; ferramenta nova `MonsterAnimationGeneratorWindow` gerando clipes direcionais sobre 2 Animators base compartilhados (`Base_Melee`/`Base_Ranged`); GDD Seção 22/14 atualizadas. **Sem produção de Bestiário nova** — ver nota abaixo. Pendências abertas ao fim: Spectre (🟡, ver Riscos/Estado da Deadline 4) e re-teste manual de Floor Sleep. | Sprint 15 |

> **Nota (correção pós-Sprint 16):** o Batch 1 (Floors 1–2, inicial) saiu da Sprint 16 — o task breakdown da correção deixou explícito que produzir o resto do elenco de Floors 1–2 é fora de escopo dessa sprint (framework + IA de patrulha nova já enchem a semana sozinhos). Isso não atrasa a Deadline 4: o Exit Criteria dela só exige "2+ tipos de monstro reais em cena com IA funcional" — Rat/Goblin/Rat People já cobrem isso assim que a correção fechar, sem depender de conteúdo novo. O Batch 1 inteiro (Floors 1-2) passou a nascer de uma vez só na Sprint 20, junto do que já era o Batch 2 — ver linha da Sprint 20 abaixo.

## Deadline 5 — Heróis I + Floor Content I (Sprints 17–20, + Sprints 18b e 19b fora da numeração — ver nota acima)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 17 | 5 | Ranger — Primário + Infra de Animator de Herói (pivô) + Barbarian Completo | Ranger jogável (primário) + primeiro herói 100% animado do jogo | `HeroController` ganha Animator real (`AimX`/`AimY`, `IsMoving`, cooldown universal de ataque, aggro instantâneo, morte adiada por Animation Event, knockback), infraestrutura nova compartilhada por todo herói futuro; Barbarian reescrito e 100% animado (golpe de 4 hitboxes fixas, ultimate com 8 `HeroProjectile`, passiva de dano por vida perdida); Floating Combat Text; bug real de dano duplicado corrigido (Animation Event disparando 2x por golpe em blend trees com peso misto — afetava monstros comuns e Barbarian); Ranger primário completo: formação em cunha, mira livre com rotação real do sprite, perfuração por reserva de dano, quantidade de flechas ligada a **Tier de Arma** (teto 15, não carta) | Sprint 16 |
| 18 | 5 | Ranger — Ultimate (completa Ranger) | Ranger jogável completo | Facas disparadas em todas as direções que, ao pousarem, permanecem no chão (Persistent Area) causando dano contínuo a quem passar por cima | Sprint 17 |
| 18b | 5 | Habilidade Secundária (Shift) — Barbarian + Ranger Completo | GDD Seção 16 nova (Shift pra todo herói) + Barbarian e Ranger com o kit inteiro (primário+ultimate+secundária) | Arquitetura genérica de Shift em `HeroController` (cooldown próprio, `UseSecondaryAbility()`/`CancelSecondaryAbility()`, `IsPlayerUntargetable` reaproveitável); Barbarian: buff temporário de dano (2×) e velocidade (1.5×); Ranger: camuflagem em 3 fases, cura por tick, cancelável, remove aggro de verdade; bosses (`isBoss`) imunes a knockback; ESC pausa o jogo (só input, sem menu) | Sprint 18 |
| 19 | 5 | Mage — Completo (Primário + Ultimate + Secundária/Shift + Pet Phoenix) | Mage jogável completo | Mage ataca em arco; Phoenix é sumonada no início do dia (summon-lock) e ataca sozinha; Fireball explode e deixa rastro persistente; Shift do Mage (GDD Seção 16) | Sprint 18b |
| 19b | 5 | Sistema de Efeitos Nocivos (Fire + Bleeding) + correções de base em Mira/Shift | `StatusEffectController` genérico (herói e monstro) + Bleeding real na Ultimate do Ranger + bugs de base corrigidos em todo herói | `StatusEffectController`/`IDamageable` compartilhado entre `HeroController` e `EnemyController`, com imunidade por tipo; Fire (Mage) e Bleeding (Ranger, voo+chão) implementados; enum com os 12 tipos de Efeito já nomeados pro futuro; `DiagonalAimX/Y` corrige o espelhamento de Blend Tree de 4 pontos perto dos eixos cardeais (Barbarian, Ranger e Mage); mira agora lida por polling (não só em evento de movimento) e relida à força antes de congelar em Ultimate/Shift; `isAttacking` trava a Habilidade Secundária contra sobreposição de ação | Sprint 19 |
| 20 | 5 | Floor 1A–2A + Bestiary Batch 1+2 completo + A* Pathfinding/RVO (retrofit dos 14 monstros comuns) | Primeiros Floors reais habitados e povoados, rodando em cima de pathfinding real | Floor 1A e 2A jogáveis com layout e inimigos reais; Floors 1–2 com o elenco completo (Wolf, Bat, Slime Green/Blue, Goblin Raider, Goblin Sapper, Centaur, Minotaur, Gnoll, Spider, Ancient Troll — além de Rat/Goblin/Rat People já prontos desde a Sprint 16); todo monstro comum migrado de `transform.Translate` pra A* Pathfinding Project + RVO Local Avoidance (`GridGraph` por Floor), `FloorPopulationManager` com modelo de crise/escalação | Sprint 19b |

## Deadline 6 — Heróis II + Bosses (Floor 1–2) (Sprints 21–26)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 21 | 6 | ✅ Druid — Completo (Primário + Ultimate + Secundária) | Druid Gameplay Complete, sem Floor novo | Vinhas se distribuem entre múltiplos monstros mais próximos sem repetir alvo; Ultimate transforma em Alce (troca completa de animações, cura pra 100% do HP do Alce, conversão proporcional de HP ao voltar, exceto morte durante a transformação); Secundária transforma em Coruja (bloqueia só ataque, movimento livre, imune a dano, sem colisão física) — ver `docs/sprints/sprint-21.md` | Sprint 20 |
| 22 | 6 | ✅ Rogue — Completo | Rogue Gameplay Complete | Self Area Pulse (trigger circular nos pés, 1x por Animation Event) funciona com cooldown e knockback; bomba da ultimate viaja em ângulo livre até colidir ou alcançar o alcance máximo e explode em área (4× o dano); cambalhota (Shift) com movimento livre e pose em 1 de 4 diagonais, imune a dano e sem colisão física com monstro, aplica knockback; passiva rende 4× mais Energia de Ultimate por kill — ver `docs/sprints/sprint-22.md` | Sprint 21 |
| 23 | 6 | ✅ Cleric — Completo | Cleric Gameplay Complete | Projétil homing persegue o(s) monstro(s) mais próximos (só ataca com 1+ monstro no raio); oração (ultimate) paraliza e aplica DoT (Efeito WordOfPain) em todo monstro no raio de visão do Cleric, dano por tick = metade do dano normal; cura vira ativa via Shift, em 4 ondas; passiva dobra o dano do hit do projétil — ver `docs/sprints/sprint-23.md` | Sprint 22 |
| 24 | 6 | ✅ Boss Framework + Boss Timer + Bosses Floor 1 — Completo | Primeiros bosses reais, com Boss Timer funcionando | Boss Timer acumula por Floor/dia e persiste entre trocas de Floor/mortes/pausa; Mother Slime Green/Blue (contato, spawn de filhotes) e Goblin King (padrão genérico do Boss Framework) derrotáveis — ver `docs/sprints/sprint-24-boss-framework-floor1.md` | Sprint 23 |
| 25 | 6 | ✅ Bosses Floor 2 — Padrão (Werewolf, Centaur King) — Completo | 2 bosses de arquitetura genérica | Os 2 bosses usam o Boss Framework padrão (attack real com Animation Event, cooldowns próprios); Boss Timer de Floor 2 gira entre os 4 bosses do Floor a cada 10s, empilhando se o anterior ainda estiver vivo (GDD Seção 22, correção Sprint 24). Cave Troll saiu desta sprint — movido pro Floor 3 — ver `docs/sprints/sprint-25-floor2-padrao.md` | Sprint 24 |
| 26 | 6 | ✅ Bosses Floor 2 — Exceções (Rat People Royalty, Spider Queen) — Completo | 2 bosses de arquitetura própria + Floor 1–2 100% fechado | Rat People Royalty e Spider Queen com Melee/Ranged híbrido (2 raios de ataque independentes) e mecânica bespoke própria por Animation Event; **7/7 bosses de Floor 1–2 derrotáveis** — ver `docs/sprints/sprint-26-floor2-excecoes.md` | Sprint 25 |

## Deadline 7 — Heróis III (Sprints 27–30)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 27 | 7 | ✅ Paladin — Completo | Paladin Gameplay Complete | Martelo é projétil reto com mira livre, escalável até 5 por upgrade (leque angular); espadas orbitam (GameObject filho dedicado, até 8 slots, 2/4/8 por upgrade); shield bash (Shift) golpeia as 4 direções fixas simultaneamente, imune a dano durante a animação; passiva gera shield periódico com HP próprio absorvendo dano antes do Paladin, nasce em cooldown (não carregado) — ver `docs/sprints/sprint-27.md` | Sprint 26 |
| 27b | 7 | ✅ Infraestrutura de Upgrades (todos os heróis) + correções de base no Mage | Valores de upgrade centralizados no controlador, sem duplicação entre herói e prefab filho | Todo filho/projétil instanciado por um herói (`HeroProjectile`, `ClericProjectile`, `Vine`, `MageFireball`, `MageTeleportProjectile`, `RogueBomb`, `RangerArrow`, `RangerKnife`) recebe o próprio balanceamento por parâmetro, não mais campo duplicado; `ActionSpeedMultiplier` (Animator) acelera Attack/Ultimate/Shift-de-dano; multiplicadores de tamanho em Barbarian (corpo + projéteis), Mage (hitbox do primário, bola de fogo da Ultimate, pet); Floating Combat Text de cura; fix da explosão inconsistente do Mage e do pet deslizando no Summon — ver `docs/sprints/sprint-27b-upgrade-infrastructure.md` | Sprint 27 |
| 28 | 7 | ✅ Gunslinger — Completo | Gunslinger Gameplay Complete | Primeiro hitscan do projeto (raycast com buffer, filtrado por tag "Enemy" — necessário porque os monstros têm os próprios hitboxes de ataque Untagged no mesmo layer); rajada do primário com 1 a 15 tiros (upgradable comum, sem Tier de Arma), cada tiro com desvio aleatório independente calibrado por Gizmo em Play Mode; ultimate dispara nas 8 direções fixas via 8 Animation Events, cada uma soltando N tiros instantâneos (ajustável) com o mesmo desvio aleatório do primário; chicote (Shift) usa o mesmo mecanismo de 4 triggers fixos do shield bash do Paladin, sem a imunidade a dano; passiva dobra o loot dropado por qualquer monstro — ver `docs/sprints/sprint-28.md` | Sprint 27b |
| 29 | 7 | ✅ Assassin — Completo | Assassin Gameplay Complete | Deadly Dash redesenhado em sprint (a pedido do usuário) pra um "teleporte de ida-e-volta": Assassin fica parado e invisível enquanto um projétil (`AssassinDashProjectile`) viaja, bate 1x em área e volta pro ponto de origem antes dele reaparecer — sem dano contínuo ao longo do trajeto; Thousand Blades usa a MESMA mecânica (2× dano) durante a forma sombria da Ultimate; stealth troca o `RuntimeAnimatorController` inteiro (mesmo critério do Alce do Druid), remove o aggro dos monstros (fix de base: monstros de emboscada não podiam re-dormir durante stealth — `EnemyController.UpdatePatrol()`); teleporte (Shift) em 4 diagonais, com arte própria pra forma sombria também; trava de segurança impede a transição de saída da forma sombria de interromper um Attack/Shift em andamento — ver `docs/sprints/sprint-29.md` | Sprint 28 |
| 30 | 7 | ✅ Blood Mage — Completo | **10 heróis MVP Gameplay Complete** | Primário dispara em leque (1–5 projéteis via upgrade, mesmo critério do martelo do Paladin), cada projétil vale exatamente 1 hit por padrão (reserva = dano, upgradable pra perfuração real); Ultimate é um anel de dano CIRCULAR em 3 estágios (diâmetro 2→4→8, dano na borda, zona segura dinâmica, 5× o dano, escala com upgrade de tamanho via `transform.localScale`); Shift aplica o status visual "Drain" (novo — 1x só, prioridade sobre qualquer Efeito exceto incapacitação) no(s) monstro(s) vivo(s) mais próximo(s) — mesmo critério multi-alvo da vinha do Druid — e solta uma orb de sangue que cura exatamente o dano real causado (upgradable); pet Elemental de Sangue com summon-lock (mesmo comportamento da Phoenix, `PetController` generalizado pra aceitar Idle/Move separados além do Fly único) — ver `docs/sprints/sprint-30.md`. Checklist manual formal dos 10 heróis ainda pendente antes da Deadline 8 | Sprint 29 |
| 30b | 7 | ✅ Ajustes de Progressão (Assassin/Blood Mage/Rogue/Druid) | Preparação pra um sistema de progressão futuro (ainda não desenhado) | Leque de projéteis configurável no Deadly Dash/Thousand Blades do Assassin (1–5, simétrico e centrado no mouse — diferente de propósito do Paladin/Blood Mage, que mantêm 1 projétil fixo nele — com dedup compartilhado entre projéteis da mesma leva pra não dobrar dano em raios que se interceptam); Assassin e Blood Mage passam a seguir o mouse de verdade (`RawAimDirection`, não mais 8 direções travadas — Paladin já usava); Rogue redesenha o primário (Self Area Pulse) em 3 estágios — attack_start → filho dedicado (`RoguePulseArea`, `CircleCollider2D` real, Cycle em loop, dano por segundo) → attack_end, com tamanho/duração/cadência configuráveis; Druid ganha `elkSizeMultiplier` (escala visual + collider + hitboxes de garra só durante a forma Alce) — ver `docs/sprints/sprint-30b-ajustes-progressao.md` | Sprint 30 |

## Deadline 8 — Torre Completa A (Sprints 31–43)

> Nenhum sistema novo de código nesta Deadline — Enemy/Boss Framework, `MonsterAnimationGeneratorWindow`, A*/RVO e `FloorPopulationManager` (GridGraph) já existem e estão validados em Floor 1–2 desde a Deadline 5/6. É produção de conteúdo pura: tileset final, Bestiário, loot e UI, aplicando o que já existe.

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 31 | 8 | 15 LootDefinitions | Todos os materiais existem | Cada material tem rolagem independente de drop | Sprint 30b |
| 32 | 8 | Agregação Visual + Large Number | Drops grandes legíveis e performáticos | Pilha de 10.000 unidades aparece agregada e é coletada corretamente em partes | Sprint 31 |
| 33 | 8 | Floor 3A (tileset final) + Bestiary Andar 3 + Bosses Floor 3 | Floor 3 100% fechado | Floor 3A jogável em qualidade final; 9 comuns (incl. Orc Shaman — totem, não projétil) e os bosses Giant/Pale Champion/Wise Orc derrotáveis | Sprint 32 |
| 34 | 8 | Floor 4A (tileset final) + Bestiary Andar 4 + Bosses Floor 4 | Floor 4 100% fechado | Floor 4A jogável em qualidade final; 6 comuns + as 3 transformações do Dark Channeler (Dark Cultist/Hound/Abomination, mesmo GameObject mutando); bosses Flagelant/Ritual Guard/Dark Channeler (exceção bespoke) derrotáveis | Sprint 33 |
| 35 | 8 | Bestiary Andar 5 — Comuns (20 fichas, maior volume do Bestiário) | Elenco comum do Andar 5 completo | Os 20 comuns do Andar 5 completos, incluindo a variante de idle "emboscada" (Gargoyle/Skeletons) e as exceções Burning Skull, Skeleton Mage e Zombie Mage (área de conjuração no chão) | Sprint 34 |
| 36 | 8 | Floor 5A (tileset final) + Bosses Floor 5 | Floor 5 100% fechado | Floor 5A jogável em qualidade final; bosses Zombie Giant, Lich (exceção bespoke), Undead Knight, Spectre, Headless Horseman e Mummy King derrotáveis | Sprint 35 |
| 37 | 8 | Floor 6A (tileset final) + Bestiary Andar 6 + Bosses Floor 6 | Floor 6 100% fechado | Floor 6A jogável em qualidade final; 8 comuns; bosses Ancient Danger Leader/Krampus/Supreme Elemental derrotáveis | Sprint 36 |
| 38 | 8 | Floor 7A (tileset final) + Bestiary Andar 7 + Bosses Floor 7 | Floor 7 100% fechado | Floor 7A jogável em qualidade final; 5 comuns; bosses Dragon e Undead Dragon (as 2 exceções bespoke do Floor) derrotáveis | Sprint 37 |
| 39 | 8 | Floor 8A + 9A (tileset final) + Bestiary Andares 8–9 + Bosses Floor 8–9 | Floors 8 e 9 100% fechados | Floor 8A/9A jogáveis em qualidade final; 3 comuns (Andar 8) + 7 comuns incl. Serpent e Bicephalous — exceções bespoke — (Andar 9), mais o Slug (summon do Bicephalous, sem spawn próprio); boss Balrog (Floor 8) e bosses Lobster/Stickman/Ambuster (Floor 9) derrotáveis | Sprint 38 |
| 40 | 8 | Floor 10A (tileset final) + Bestiary Andar 10 + Boss Divine God | **Ground + Floor 1A–10A completos (10/10), Bestiário 100% fechado (76 comuns + 22 bosses)** | Floor 10A jogável em qualidade final; 4 comuns; Divine God (exceção bespoke, único híbrido Melee/Ranged com contato) derrotável — matá-lo não encerra a run | Sprint 39 |
| 41 | 8 | UI Final de Gameplay — Parte 1 (Vida, Ataque, Ultimate/Shift) | HUD de combate pronto | Vida/Vida Máxima, cooldown do ataque primário e Energia/cooldown de Ultimate e Shift (conforme o herói) visíveis e corretos para os 10 heróis | Sprint 40 |
| 42 | 8 | UI Final de Gameplay — Parte 2 (Loot, Temporizador do Dia, Demanda, Bag) | HUD de economia/tempo pronto | Loot agregado visualmente, tempo restante do dia, Demanda (`X/Y Monster Essence`, só Modo Padrão) e Bag (slots/stacks) visíveis e corretos | Sprint 41 |
| 43 | 8 | Full-Run Test + Floor Sleep em Escala Real | Torre inteira validada | Run de teste visita Ground→Floor 10 sem erro; profiling confirma Floor Sleep estável com 11 Floors reais; nenhum Floor incompleto (checklist por Floor: entrada, saída, escada, baú, população mínima) | Sprint 33–42 |

## Deadline 9 — Economia Expandida + Chest/Card + Início Variant B (Sprints 44–47)

> Renumerada a partir daqui (+11 em relação à revisão anterior) por causa do crescimento da Deadline 8 — ver Quinta nota no topo do documento. Escopo desta Deadline e das seguintes **inalterado**.

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 44 | 9 | 15 Weapon Tiers | Progressão de arma completa | Compra sequencial dos 15 tiers funciona e multiplica dano/vida corretamente | Sprint 43 |
| 45 | 9 | Bonuses Framework | Loja de bonuses funcional | Add Time, Slots, Stack e Pickup Radius compráveis com efeito real | Sprint 44 |
| 46 | 9 | Chest + Card Framework + Traps (Falling Rock, Floor Spikes) | Baú, cartas e hazards ambientais funcionais | E abre o baú, UI de 3 cartas (pool universal) sem tipos repetidos; Falling Rock e Floor Spikes têm telegraph e causam dano em área, sem contar como monstro/kill/budget/demanda | Sprint 45 |
| 47 | 9 | Chest Mimic + Reroll + Primeiras Hero Cards (Ranger, Mage, Druid) + Floor Variants B (Floors 1–4, início) | Recompensas completas + conteúdo de carta começando | Mimic (ciclo de disfarce de baú + escala de stats por andar) ativa e libera a mesma recompensa ao morrer; cartas específicas de Ranger/Mage/Druid entram no pool (agora que o Card Framework existe); Floors 1–4 em Variant B jogáveis | Sprint 46 |

## Deadline 10 — Employees Fase 1 + Continuidade B (Sprints 48–51)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 48 | 10 | Employee Definition + Compra | Comprar funcionários | Popup de compra com scroll+input clampado pelo Gold disponível | Sprint 47 |
| 49 | 10 | Promoção (Duplo Limite) + Sell | Árvore de promoção e venda funcionais | Promoção consome employee anterior e respeita limite de Gold + quantidade; painel de venda com scroll+input clampado pela quantidade possuída, Confirm vende e X fecha sem vender | Sprint 48 |
| 50 | 10 | Helper + Collector Básicos + Floor Variants B (Floors 5–7) | Automação real em campo + mais conteúdo | 1 Ajudante mata um monstro; 1 Coletor vende Essência automaticamente; Floors 5–7 em B jogáveis; indicador básico de Employees na HUD | Sprint 49 |
| 51 | 10 | Floor Variants B (Floors 8–10, **conclui B = 10/10**) + Variants C (Floor 1, início) + Checkpoint de Produção de Mapas | Variant B fechada + C nasce | Floors 8–10 em B e Floor 1 em C jogáveis; relatório de progresso vs. meta de 50 Variants | Sprint 50 |

## Deadline 11 — Employees Fase 2 + Quests + Continuidade C / Início D (Sprints 52–55)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 52 | 11 | Strong/Fast + Árvore Completa + Floor Variants C (Floors 2–4) | Especialização de Employees + conteúdo | Strong e Fast têm atributos diferentes entre si, nenhum pior que o Senior; Floors 2–4 em C jogáveis | Sprint 51 |
| 53 | 11 | Virtualização + Profiling + Floor Variants C (Floors 5–6) | Escala de milhares sem travar + conteúdo | Profiling confirma 1000+ Employees "possuídos" sem impacto grave de frame rate; Floors 5–6 em C jogáveis | Sprint 52 |
| 54 | 11 | Cross-Floor Collector + Collector Filter + Magnet + Floor Variants C (Floors 7–8) | Coleta automática avançada + conteúdo | Coletor vende loot de Floor diferente do atual e respeita o Collector Filter; Magnet vende automaticamente dentro do Pickup Radius; Floors 7–8 em C jogáveis | Sprint 53, Sprint 32 |
| 55 | 11 | Chest Pointer + Chaos Crystal + Employee Cards + Floor Variants C (Floors 9–10, **conclui C = 10/10**) + Variants D (Floors 1–2, **início — nasce na Deadline 11**) | Quests completas + primeira Variant D existe | As 3 quests funcionais; cartas de Employee entram no pool (Employees já existem); Floors 9–10 em C e 1–2 em D jogáveis | Sprint 54 |

## Deadline 12 — Run Completa + Meta Systems + Continuidade D / Início E (Sprints 56–59)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 56 | 12 | Remove Tower Layer + Settings Mínimo (áudio/display/idioma) + Floor Variants D (Floors 3–5) | Remoção de andar + configurações essenciais + conteúdo | Compra remove o Active Floor 1 automaticamente e remapeia escadas corretamente; menu de Settings com volume/resolução/idioma funcional; Floors 3–5 em D jogáveis | Sprint 6, 55 |
| 57 | 12 | Remote Controller + Floor Variants D (Floors 6–8) | Teleporte entre Floors + conteúdo | Q abre interface pausada, lista Floors por Active Floor Position (cooldown indicator na HUD), teleporta; Floors 6–8 em D jogáveis | Sprint 56 |
| 58 | 12 | Free Mode + Progress Tracking Completo + Day 15 Victory/Continue Flow + Floor Variants D (Floors 9–10, **conclui D = 10/10**) + Variants E (Floors 1–2, **início — nasce na Deadline 12**) | Segundo modo + desbloqueios reais + marco de vitória | Free completa um dia sem validar demanda; ao menos 1 herói desbloqueia por critério real; Dia 15 mostra Vitória, "Menu" mantém o checkpoint anterior e "Continuar" gera novo save pré-Dia 16; Floors 9–10 em D e 1–2 em E jogáveis | Sprint 57 |
| 59 | 12 | Steamworks Groundwork + Commercial Prep Drafts + Day 30 Ending Flow + Floor Variants E (Floors 3–5) | Base técnica de Steam + marco de encerramento + conteúdo | Achievement de teste dispara em ambiente Steam de teste; rascunhos de copy/tags/feature list/screenshot shot list/trailer storyboard produzidos; Dia 30 mostra Encerramento Definitivo sem gerar autosave extra; Floors 3–5 em E jogáveis | Sprint 58 |

## Deadline 13 — Content Complete + Balance + Store Preparation (Sprints 60–63)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 60 | 13 | Floor Variants E (Floors 6–8) + Screenshots | Finalização de conteúdo — onda 1 (Bestiário e bosses já 100% fechados desde a Deadline 8 — nada de "leftover" aqui nesta revisão) | Floors 6–8 em E jogáveis; screenshots de divulgação capturados | Sprint 59 |
| 61 | 13 | Floor Variants E (Floors 9–10, **conclui E = 10/10 → 50/50 Variants**) + Revisão Final de Cards + Capsule Assets + Trailer (captura/edição) | Conteúdo do MVP 100% completo | As 50 Floor Variants existem; todas as pools de carta revisadas e completas; capsule assets e trailer prontos | Sprint 60 |
| 62 | 13 | Balance Pass + Performance Pass (integrado) + UI/UX Pass — Parte 1 + Audio/Juice Pass | Jogo calibrado, mais legível e com feedback sonoro básico | Ritmo econômico dentro do ciclo emocional do GDD; performance estável em run completa (checkpoints já ocorreram nas Sprints 15/43/53); HUD e Shop revisados por legibilidade (o HUD de gameplay em si já é final desde a Deadline 8 — Sprints 41–42); SFX básico de hit/crítico/pickup/venda/boss/morte/upgrade presentes | Sprint 61 |
| 63 | 13 | UI/UX Pass — Parte 2 + Store Page Assembly (usando trailer/capsule já prontos) + Depot/Branch Verification + Localização — Revisão Final | Integração, revisão e fechamento comercial | Results/Inventory/menus revisados; Steam Store Page pronta para revisão; depot/branch de teste verificados; textos traduzidos com revisão final (arquitetura e conteúdo já existiam desde a Sprint 4/47) | Sprint 62 |

## Deadline 14 — Stabilization & Release Candidate (Sprints 64–67)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 64 | 14 | Bug Fixing P0/P1 + Regression 1 | Estabilidade central | Zero bugs P0/P1 conhecidos após a rodada | Sprint 63 |
| 65 | 14 | Save/Load Validation + Performance Final | Persistência confiável | 5+ cenários de checkpoint validados sem erro, incluindo Dia 15 (Menu/Continuar) e Dia 30 | Sprint 64 |
| 66 | 14 | Steam Validation + Settings Validation + Localização Final Validation | Build pronto para Steam (validação, não criação) | Achievements/build/depot/branch validados; Settings e localização, já implementados desde a Deadline 12/13, são apenas conferidos aqui | Sprint 65 |
| 67 | 14 | **Release Candidate / Final Validation** | Build shippable | RC aprovado, pronto para publicação | Sprint 66 |

---

# AUDITORIA MECÂNICA FINAL

> **Nota (reauditoria pós-Sprint 21, Deadlines 6–14):** a auditoria abaixo foi refeita da linha "Heróis" em diante, refletindo a reestruturação descrita na Quinta nota (topo do documento) e nas seções de Deadline 6–8. As linhas que só dependiam de Deadlines 1–5 (Main Menu/Run Flow em sua base, Death Flow fase 1) não mudam de conteúdo, só de numeração onde aplicável.

**Floor Variants (50/50):**
A: S20(2)+S33(1)+S34(1)+S36(1)+S37(1)+S38(1)+S39(2)+S40(1) = 10 ✓ (Floor 1–2 na Deadline 5; Floor 3–10 um por sprint na Deadline 8, exceto Floor 8–9 combinados na S39 — ver Deadline 8)
B: S47(4)+S50(3)+S51(3) = 10 ✓
C: S51(1)+S52(3)+S53(2)+S54(2)+S55(2) = 10 ✓
D: S55(2)+S56(3)+S57(3)+S58(2) = 10 ✓
E: S58(2)+S59(3)+S60(3)+S61(2) = 10 ✓
**Total = 50/50 ✓.** Nenhuma sprint recebe mais de 4 Floor Variants; D nasce na Sprint 55 (Deadline 11, mesma posição relativa de antes, conforme exigido); E nasce na Sprint 58 (Deadline 12, antes do limite da Sprint 59).

**Heróis (10/10 Gameplay Complete):** Barbarian (S7) · Ranger (S17–18) · Mage (S19–20) · Druid (S21, ✅ completo numa sprint só — vinhas+Alce+Coruja juntos, ver `sprint-21.md`) · Rogue (S22) · Cleric (S23) · Paladin (S27) · Gunslinger (S28) · Assassin (S29) · Blood Mage (S30). ✅ Nenhuma sprint de herói combina mais de 1 kit completo (mais estrito que a revisão anterior, que permitia até 2 + 1 conteúdo secundário — deixou de ser necessário combinar, já que Floor/Bestiário saiu das sprints de herói).

**Main Menu / Run Flow:** Menu+New Game+Mode/Hero/Map Select+Create Run (S9) · Continue Game + Continue-sem-save desabilitado + New Game sem confirmação (S12) · Game Over funcional (S11) · Day 15 Victory/Continue (S58) · Day 30 Ending (S59). ✅ Todos com dono.

**Employees:** Buy (S48) · Promote+Sell (S49) · Helper/Collector básicos (S50) · Strong/Fast (S52) · Virtualização (S53) · Cross-Floor+Collector Filter (S54). ✅ Sell e Collector Filter deixaram de estar ausentes.

**Cards:** Framework (S46) → primeiras Hero Cards (S47) → Employee Cards (S55) → revisão final (S61). ✅ Nenhuma carta de herói implementada antes do Framework (Ranger em S18 só prepara o hook, sem a carta real).

**HUD:** Floor indicator (S6) · Energy (S7) · Health (S8) · Demand (S11) · **UI final de gameplay completa — vida, ataque, ultimate/shift, loot, temporizador, demanda, bag (S41–42, Deadline 8, mudança desta revisão)** · Employee indicator (S50) · Remote cooldown (S57) · Pass de legibilidade (S62–63). ✅ Deixou de ser só "progressivo até o UI/UX Pass final" — o HUD de combate/gameplay em si já é final e completo desde a Deadline 8, bem antes do Pass da Deadline 13 (que agora é só Results/Inventory/menus e legibilidade, não o HUD de combate).

**Death Flow progressivo:** Fase 1 básica (S8: HP zero, -30s hook, respawn, Energia zera) → integração com loot/Day Timer/Game Over (S11). ✅ Sem "mega-refatoração" no final — sem mudança em relação à revisão anterior (Deadlines ≤3).

**Bestiary (76 comuns + 22 bosses, 100% fechado ao fim da Deadline 8 — mudança principal desta revisão):** Floor 1–2 (S20 — Rat/Goblin/Rat People já prontos desde a correção da S16, inclui a exceção Goblin Sapper) · Floor 3 (S33, inclui a exceção Orc Shaman) · Floor 4 (S34, inclui as 3 transformações do Dark Channeler) · Floor 5 (S35 — maior volume do Bestiário, 20 fichas comuns, inclui as exceções Burning Skull/Skeleton Mage/Zombie Mage e a variante de idle "emboscada"; Skeleton Rider deixou de ser exceção) · Floor 6 (S37) · Floor 7 (S38) · Floor 8–9 (S39, inclui as exceções Serpent e Bicephalous, e o Slug — summon do Bicephalous, sem spawn próprio, corrigido nesta revisão: não tinha sprint dona na revisão anterior) · Floor 10 (S40). ✅ Distribuído 1 Floor por sprint (Floor 5 com 1 sprint extra pelo volume, Floor 8–9 combinados pelo volume baixo) — **diferença estrutural desta revisão:** deixa de existir um Batch "Leftovers" no fim do projeto (antiga S49) porque não sobra nenhuma ficha pendente; tudo fecha dentro da Deadline 8. **Premissa de custo (decisão final do combate, herdada da revisão anterior, sem mudança):** todo monstro comum e a maioria dos bosses usa uma animação `attack`/conjuração real por Animation Event, gerada pela ferramenta `MonsterAnimationGeneratorWindow` (Sprint 16) sobre 2 Animators base compartilhados — configurar estado/transição do zero por criatura não existe mais. As exceções continuam exigindo trabalho bespoke além do que a ferramenta gera sozinha: 5 comuns (Goblin Sapper, Orc Shaman — só o totem, Burning Skull, Serpent, Bicephalous) e 7 bosses com arquitetura própria de verdade (Rat People Royalty, Spider Queen, Dark Channeler, Lich, Dragon, Undead Dragon, Divine God), além de Mother Slime Green/Blue (cai na exceção permanente dos Slimes, contato sem `attack`). Spectre (boss Andar 5) teve sua pendência 🟡 resolvida ainda na Deadline 4 (ganhou `attack` real + superfície de gelo) — não é mais uma incógnita para a S36.

**Bosses (30/30, 100% fechado ao fim da Deadline 8 — mudança principal desta revisão):** Framework + Floor 1 (S24, Mother Slime Green/Blue + Goblin King) · Floor 2 (S25–26, Werewolf/Centaur King/Cave Troll + Rat People Royalty/Spider Queen) · Floor 3 (S33, Giant/Pale Champion/Wise Orc) · Floor 4 (S34, Flagelant/Ritual Guard/Dark Channeler) · Floor 5 (S36, Zombie Giant/Lich/Undead Knight/Spectre/Headless Horseman/Mummy King) · Floor 6 (S37, Ancient Danger Leader/Krampus/Supreme Elemental) · Floor 7 (S38, Dragon/Undead Dragon) · Floor 8–9 (S39, Balrog + Lobster/Stickman/Ambuster) · Floor 10 (S40, Divine God). ✅ Distribuído 1:1 com o Floor correspondente (mais legível que os antigos "Batch inicial/intermediário/superior", que misturavam bosses de Floors distantes na mesma sprint por conveniência de calendário) — framework antes de conteúdo, e **nenhum boss sobra pra Deadline 13** nesta revisão.

**Settings:** Implementado na S56 (Deadline 12); S66 apenas valida. ✅ Não nasce mais perto do fim.

**Steam/Store:** Groundwork técnico (S59) → rascunhos comerciais (S59) → screenshots (S60) → capsule/trailer (S61) → Store Page/depot (S63) → validação/publicação (S66–67). ✅ Distribuído em 5 pontos, nenhuma sprint concentra tudo.

**Audio/Juice:** Pass explícito na S62 (junto de Balance/Performance/UI Parte 1), hooks via GameEvents existindo desde a S3. ✅ S63 ficou livre para ser só integração/fechamento.

**Localização:** Arquitetura desde S4; conteúdo real usando IDs desde que UI nasce (S9 em diante); revisão final na S63. ✅

**S64–67:** somente bug fixing, regression, save/load, validação de Steam/Settings/Localização e Release Candidate — nenhuma feature estrutural nova. ✅

---

# 67 Sprints — Distribution Freeze ✅ (emenda pós-Sprint 21)

Todos os critérios da auditoria mecânica final foram satisfeitos: 50/50 Variants com volume plausível por sprint (máximo 4), 10/10 heróis com carga realista (agora 1 kit completo por sprint, nunca combinado), Main Menu/Run Flow/Game Over/Day 15/Day 30 cobertos, Employees com Sell e Collector Filter atribuídos, Cards respeitando Framework→Content, HUD de combate final desde a Deadline 8, Death Flow progressivo, **Bestiary e Bosses 100% fechados ao fim da Deadline 8, sem leftover algum**, Settings implementado antes da Deadline 14, Steam/Store distribuído, e a Deadline 14 contendo apenas estabilização/validação.

**Este freeze substitui o freeze de 56 sprints das revisões anteriores especificamente da Deadline 6 em diante**, pelo motivo registrado na Quinta nota (topo do documento) e nas seções de Deadline 6–8: a Sprint 21 fechou fora do formato original (Druid completo, sem Floor), e isso expôs que agrupar "heróis+combate" e depois "Torre" em blocos dedicados — em vez de alternados — reduz retrabalho de forma mensurável (ver `docs/sprints/sprint-21.md`). **As Deadlines 1–5 (Sprints 1–20) permanecem fechadas e não fazem parte desta reabertura** — nenhuma sprint já entregue muda de número, escopo ou Exit Criteria. Esta segunda emenda não deve ser reaberta de novo por preferência editorial — só por atraso real, bloqueio técnico ou necessidade de replanejamento, mesmo critério da primeira versão deste freeze.

---

# DISTRIBUTION FREEZE CONCLUÍDO — PRONTO PARA DETALHAR A SPRINT 1.