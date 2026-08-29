# Projeto Torre — Plano de Produção (Revisão 2)
## Etapa 1: Análise de Produção · Etapa 2: Ordem Macro · Etapa 3: 14 Deadlines Revisadas

> Fonte de verdade de gameplay: **GDD Mestre v1.01235** (estruturalmente congelado). Este documento define **como construir**, nunca **o que o jogo é**. Revisão cirúrgica sobre a versão anterior: mesma profundidade de análise, distribuição de carga corrigida.

---

# OBRIGAÇÕES PRESENTES EM TODAS AS 56 SPRINTS (nota global)

Nenhuma das 56 sprints futuras será aceita sem:

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
Enemy Framework maduro (Melee/Ranged/Attack Budget/Population) + início do Bestiary real
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
| 4 | 4 | Combat & Enemy Framework + Floor Sleep v1 | Combate com hordas reais + arquitetura de simulação seletiva | Vários inimigos diferentes atacam o jogador com budget limitado; Floors fora do atual não simulam integralmente | Enemy Framework (Melee/Ranged), timing/telegraph de ataque, Attack Budget, Population esqueleto, Projectile framework, início real do Bestiary (2–3 monstros), **Floor Sleep/Activation v1** | Deadline 3 | Attack Budget visivelmente limita quantos inimigos atacam ao mesmo tempo; 2+ tipos de monstro reais em cena com IA funcional; **com 3 Floors existentes na Scene, somente o Current Floor executa simulação completa — Floors fora dele preservam Floor State (loot, Population State, Boss Timer, Floor Variant, Original Floor Identity, Active Floor Position) sem continuar rodando AI/Animator/pathfinding desnecessariamente** |
| 5 | 5 | Heróis I + Floor Content I | 3 heróis jogáveis, primeiros Floors A reais | Escolher entre Barbarian/Ranger/Mage; 2–3 Floors originais já com layout Variant A real (não testbed) | Ranger (projétil/leque/área persistente), Mage (pet/impacto/fogo persistente), início da produção de Floor 1A–3A | Deadline 4 | 3 heróis completos e balanceáveis; pelo menos 2 Floors rodando em Variant A real, com spawn/baú/trap posicionados |
| 6 | 6 | Heróis II + Boss Framework | 6 heróis jogáveis, primeiro boss real | Escolher entre 6 heróis; 1 boss enfrentável com Boss Timer funcionando | Druid (transformação + conversão de HP), Rogue (hitbox orbital), Cleric (homing + efeito global + cura), Boss Framework, Boss Timer, boss de teste/primeiro real, +2 Floors A, +Bestiary | Deadline 5 | Conversão de HP do Druid correta em teste automatizado; Boss Timer acumula/persiste conforme regra do GDD; boss derrotável |
| 7 | 7 | Heróis III + Bestiary Expansion | 10 heróis do MVP completos | Todos os 10 heróis do MVP jogáveis | Paladin (shield), Gunslinger (hitscan), Assassin (dash/stealth), Blood Mage (lifesteal+pet), Bestiary expandindo, +2 bosses quando possível, +2 Floors A | Deadline 6 | Os 10 heróis passam por um checklist manual de ataque/ultimate/passiva/morte sem erro de console |
| 8 | 8 | **Loot Completo + Torre Variant A** | Torre inteira jogável do térreo ao Floor 10 | Run estrutural completa Ground→Floor 10 usando só Variant A | 15 LootDefinitions, rolagens independentes, agregação visual, Floor 1A–10A estruturalmente completos, primeiros full-run tests, **validação de Floor Sleep em escala (Ground + 10 Floors)** | Deadlines 5–7 (Floors A progressivos) | Uma run de teste consegue visitar todos os 10 Floors originais em sequência sem erro; todos os 15 materiais dropam corretamente em algum ponto da torre; **navegação entre Ground e Floor 1–10 preserva estado, Floors off-screen não executam simulação completa desnecessária, sair e voltar a um Floor não causa reset artificial/explorável, e um profiling básico foi realizado** |
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
**Principais entregas:** Melee/Ranged genéricos, Attack Budget visível, início do Bestiary, e a primeira versão de Floor Sleep/Activation: Current Floor com simulação completa, Floors fora dele com sistemas caros suspensos mas Floor State preservado (loot, Population State, Boss Timer, Floor Variant, Original Floor Identity, Active Floor Position).
**Riscos:** Attack Budget mal calibrado; Floor Sleep v1 subestimando quanto precisa ser preservado versus suspenso.
**Contingência:** valores de budget ficam como 🔢 placeholder ajustável; se Floor Sleep não fechar de forma elegante, aceitar uma versão simplificada (ex.: suspender só Update de IA, sem otimizar Animator ainda) documentada como dívida técnica com prazo de resolução até a Deadline 8.

### Deadline 5 — Heróis I + Floor Content I
**Por que agora?** Ranger e Mage validam famílias técnicas (leque, área persistente, pet) que o framework de habilidades precisa suportar cedo; produção de Floor A começa em paralelo alternado para não concentrar depois.
**Principais entregas:** 3 heróis, 2–3 Floors A reais.
**Riscos:** pet do Mage (summon-lock, orçamento ofensivo) subestimado tecnicamente.
**Contingência:** se o pet atrasar, entregar o Mage sem a passiva por alguns dias e completar na sprint seguinte — não travar o herói inteiro por causa da passiva.

### Deadline 6 — Heróis II + Boss Framework
**Por que agora?** Druid é o herói mais arriscado tecnicamente (transformação + conversão proporcional de HP) — melhor validar cedo que perto do fim. Boss Framework entra assim que a IA comum estiver madura (Deadline 4 concluída).
**Principais entregas:** Druid/Rogue/Cleric, Boss Framework, primeiro boss.
**Riscos:** conversão de HP do Druid com edge cases (morte durante transformação) mal cobertos.
**Contingência:** escrever o teste automatizado da conversão de HP *antes* de finalizar a Deadline — é barato de testar e caro de debugar depois.

### Deadline 7 — Heróis III + Bestiary Expansion
**Por que agora?** Fecha o framework de habilidades contra toda a variedade real do GDD (hitscan, dash/stealth, lifesteal, área em duas fases) antes de qualquer polimento.
**Principais entregas:** os 10 heróis do MVP completos.
**Riscos:** acúmulo de pequenos bugs de interação entre 10 kits diferentes e o Attack Budget/Combat Scope.
**Contingência:** reservar a última sprint do mês só para checklist manual dos 10 heróis, não para features novas.

> **Nota — o que significa "os 10 heróis completos" ao final da Deadline 7:** significa **Gameplay Complete**, não polish final. Ou seja: ataque, ultimate e passiva (quando houver) funcionais; morte/vida corretas; integração com Combat Scope, com o sistema de pausa e com upgrades/cards funcionando estruturalmente; sem erros críticos de console. **Não** significa VFX final, áudio final, balance final ou animação perfeita — esses elementos continuam refináveis nas Deadlines seguintes, inclusive durante o Balance Pass da Deadline 13. Essa distinção vale para evitar que a decomposição das 56 sprints trate "herói completo" como sinônimo de "herói polido".

### Deadline 8 — Loot Completo + Torre Variant A
**Por que agora?** É o segundo grande milestone de integração: prova que a torre inteira (não só um Floor) funciona estruturalmente antes de investir em economia mais profunda ou em Employees. Também é o ponto de validar em escala o Floor Sleep/Activation que nasceu na Deadline 4 — de 3 Floors de teste para os 10 Floors reais mais o térreo.
**Principais entregas:** 15 materiais, torre completa em Variant A, primeiros full-run tests, validação de Floor Sleep em escala real.
**Riscos:** algum Floor A ficar estruturalmente incompleto (sem baú/trap posicionado) e passar despercebido; Floor Sleep v1 não escalar bem de 3 para 11 Floors.
**Contingência:** checklist objetivo por Floor (entrada, saída, escada, ao menos 1 posição de baú, população mínima) antes de considerar qualquer Floor "pronto"; se o profiling revelar problema de escala no Floor Sleep, tratar como dívida técnica com prioridade P1 antes de avançar para Employees (Deadline 10), já que Employees cross-Floor dependem dessa base estar sólida.

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

# PARTE 2 — TABELA RESUMIDA DAS 56 SPRINTS (Correção Mecânica Final)

Última correção mecânica. As 14 Deadlines e os intervalos de sprint **não mudaram**. Correções desta rodada: Variants D/E deixaram de estar concentradas em S49/S50 (agora distribuídas em batches de até 4 desde a Deadline 11); sprints de herói sobrecarregadas (S23/24/27/28) foram aliviadas dentro da própria Deadline; Main Menu/New Game/Continue/Game Over/Day 15/Day 30 ganharam sprint responsável; Employee Sell foi adicionado; Collector Filter tem dono; Cards de herói só entram depois do Card Framework (S35); HUD, Death Flow, Settings e Store Prep tornaram-se progressivos em vez de concentrados no fim.

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
| 16 | 4 | Bestiary Batch 1 (Floors 1–2, inicial) | Primeiro conteúdo real de inimigos | 2–3 tipos de monstro distintos e balanceáveis em cena | Sprint 15 |

## Deadline 5 — Heróis I + Floor Content I (Sprints 17–20)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 17 | 5 | Ranger — Primário | Leque de flechas | Ranger dispara flechas retas que formam leque ao aumentar quantidade | Sprint 16 |
| 18 | 5 | Ranger — Ultimate (completa Ranger, sem carta ainda) | Ranger jogável completo | Facas persistentes no chão causam dano contínuo; teto de 5 flechas e hook de configuração prontos para a carta específica (que só será implementada após o Card Framework, Sprint 35) | Sprint 17 |
| 19 | 5 | Mage — Primário + Pet Phoenix | Phoenix funcional | Mage ataca em arco; Phoenix é sumonada no início do dia (summon-lock) e ataca sozinha | Sprint 18 |
| 20 | 5 | Mage — Ultimate (completa Mage) + Floor 1A–2A + Bestiary Batch 2 (Floors 1–2) | Mage completo + primeiros Floors reais habitados | Fireball explode e deixa rastro persistente; Floor 1A e 2A jogáveis com layout e inimigos reais | Sprint 19 |

## Deadline 6 — Heróis II + Boss Framework (Sprints 21–24)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 21 | 6 | Druid — Primário + Floor 3A–4A | Vinhas com distribuição inteligente + novos Floors | Vinhas se distribuem entre múltiplos monstros ou concentram num só quando há poucos; Floors 3A e 4A jogáveis (conteúdo de inimigos entra na sprint seguinte) | Sprint 20 |
| 22 | 6 | Druid — Ultimate + Conversão de HP (completa Druid) + Bestiary Batch 3 (Floors 3–4) | Regra de HP proporcional validada | Teste automatizado confirma conversão correta ao voltar da forma de urso, sem conversão em caso de morte; Floors 3A/4A habitados | Sprint 21 |
| 23 | 6 | Rogue completo (orbital + bomba Ground Target) + Cleric — Primário + Ultimate (paralisia/DoT) | Rogue Gameplay Complete + Cleric quase completo | Facas orbitais do Rogue e bomba da ultimate funcionam com cooldown baixo e velocidade base maior; projétil do Cleric persegue o alvo mais próximo do Floor atual; oração paralisa e causa DoT em todos os monstros do Floor | Sprint 22 |
| 24 | 6 | Cleric — Cura Periódica (completa Cleric) + Boss Framework + Boss Timer + Boss 1 | Cleric completo + primeiro boss enfrentável, com capacidade real para o Boss Framework | Cura periódica com aura visível fecha o Cleric; boss aparece via Boss Timer e é derrotável | Sprint 23 |

## Deadline 7 — Heróis III + Bestiary Expansion (Sprints 25–28)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 25 | 7 | Paladin — Primário + Ultimate + Floor 5A–6A + Bestiary Batch 4 (Floors 5–6) | Martelo e espadas orbitais funcionais + novos Floors habitados | Martelo é um projétil reto; 2 espadas orbitam causando dano; Floors 5A e 6A jogáveis e habitados | Sprint 24 |
| 26 | 7 | Paladin — Shield (completa Paladin) + Gunslinger — Primário + Boss Batch Inicial | Paladin completo + base do Gunslinger + mais bosses | Shield absorve dano com HP próprio e cooldown ao quebrar; tiros do Gunslinger ficam mais imprecisos com mais balas; 1–2 bosses adicionais em Floors iniciais | Sprint 25 |
| 27 | 7 | Gunslinger — Ultimate (completa Gunslinger) + Assassin completo + Blood Mage — Primário (projétil + lifesteal com teto) | Gunslinger e Assassin completos + base do Blood Mage | Linha rotativa da ultimate funciona; dash tem alcance fixo, dano na chegada, e stealth remove aggro dos monstros (movimento aleatório) sem cooldown de dash durante a duração; projétil do Blood Mage cura ao acertar respeitando o teto por hit | Sprint 26 |
| 28 | 7 | Blood Mage — Ultimate (duas fases) + Blood Elemental (pet + summon-lock, completa Blood Mage) + Checklist dos 10 Heróis | **10 heróis MVP Gameplay Complete** | Ultimate causa dano em duas fases espaciais; pet Blood Elemental aparece no início do dia com summon-lock; checklist manual dos 10 heróis (primary/ultimate/passive/health-death/pause/Combat Scope) sem erro crítico | Sprint 27 |

## Deadline 8 — Loot Completo + Torre Variant A (Sprints 29–32)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 29 | 8 | 15 LootDefinitions | Todos os materiais existem | Cada material tem rolagem independente de drop | Sprint 28 |
| 30 | 8 | Agregação Visual + Large Number | Drops grandes legíveis e performáticos | Pilha de 10.000 unidades aparece agregada e é coletada corretamente em partes | Sprint 29 |
| 31 | 8 | Floor 7A–10A (conclui a torre) + Bestiary Batch 5 (Floors 7–10) | **Ground + Floor 1A–10A completos (10/10)** | Todos os 10 Floors originais existem em Variant A, habitados | Sprint 20, 21, 25, 30 |
| 32 | 8 | Full-Run Test + Floor Sleep em Escala | Torre inteira validada | Run de teste visita Ground→Floor 10 sem erro; profiling confirma Floor Sleep funcionando com 11 Floors reais | Sprint 31 |

## Deadline 9 — Economia Expandida + Chest/Card + Início Variant B (Sprints 33–36)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 33 | 9 | 15 Weapon Tiers | Progressão de arma completa | Compra sequencial dos 15 tiers funciona e multiplica dano/vida corretamente | Sprint 32 |
| 34 | 9 | Bonuses Framework + Boss Batch Intermediário | Loja de bonuses funcional + mais bosses | Add Time, Slots, Stack e Pickup Radius compráveis com efeito real; 1–2 bosses adicionais em Floors intermediários | Sprint 33 |
| 35 | 9 | Chest + Card Framework + Traps (Falling Rock, Floor Spikes) | Baú, cartas e hazards ambientais funcionais | E abre o baú, UI de 3 cartas (pool universal) sem tipos repetidos; Falling Rock e Floor Spikes têm telegraph e causam dano em área, sem contar como monstro/kill/budget/demanda | Sprint 34 |
| 36 | 9 | Mimic + Reroll + Primeiras Hero Cards (Ranger, Mage, Druid) + Floor Variants B (Floors 1–4, início) | Recompensas completas + conteúdo de carta começando | Mimic ataca e libera a mesma recompensa ao morrer; cartas específicas de Ranger/Mage/Druid entram no pool (agora que o Card Framework existe); Floors 1–4 em Variant B jogáveis | Sprint 35 |

## Deadline 10 — Employees Fase 1 + Continuidade B (Sprints 37–40)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 37 | 10 | Employee Definition + Compra | Comprar funcionários | Popup de compra com scroll+input clampado pelo Gold disponível | Sprint 36 |
| 38 | 10 | Promoção (Duplo Limite) + Sell | Árvore de promoção e venda funcionais | Promoção consome employee anterior e respeita limite de Gold + quantidade; painel de venda com scroll+input clampado pela quantidade possuída, Confirm vende e X fecha sem vender | Sprint 37 |
| 39 | 10 | Helper + Collector Básicos + Boss Batch Superior 1 + Floor Variants B (Floors 5–7) | Automação real em campo + mais conteúdo | 1 Ajudante mata um monstro; 1 Coletor vende Essência automaticamente; boss adicional em Floor superior; Floors 5–7 em B jogáveis; indicador básico de Employees na HUD | Sprint 38 |
| 40 | 10 | Floor Variants B (Floors 8–10, **conclui B = 10/10**) + Variants C (Floor 1, início) + Checkpoint de Produção de Mapas | Variant B fechada + C nasce | Floors 8–10 em B e Floor 1 em C jogáveis; relatório de progresso vs. meta de 50 Variants | Sprint 39 |

## Deadline 11 — Employees Fase 2 + Quests + Continuidade C / Início D (Sprints 41–44)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 41 | 11 | Strong/Fast + Árvore Completa + Floor Variants C (Floors 2–4) | Especialização de Employees + conteúdo | Strong e Fast têm atributos diferentes entre si, nenhum pior que o Senior; Floors 2–4 em C jogáveis | Sprint 40 |
| 42 | 11 | Virtualização + Profiling + Floor Variants C (Floors 5–6) | Escala de milhares sem travar + conteúdo | Profiling confirma 1000+ Employees "possuídos" sem impacto grave de frame rate; Floors 5–6 em C jogáveis | Sprint 41 |
| 43 | 11 | Cross-Floor Collector + Collector Filter + Magnet + Floor Variants C (Floors 7–8) | Coleta automática avançada + conteúdo | Coletor vende loot de Floor diferente do atual e respeita o Collector Filter; Magnet vende automaticamente dentro do Pickup Radius; Floors 7–8 em C jogáveis | Sprint 42, Sprint 30 |
| 44 | 11 | Chest Pointer + Chaos Crystal + Employee Cards + Floor Variants C (Floors 9–10, **conclui C = 10/10**) + Variants D (Floors 1–2, **início — nasce na Deadline 11**) | Quests completas + primeira Variant D existe | As 3 quests funcionais; cartas de Employee entram no pool (Employees já existem); Floors 9–10 em C e 1–2 em D jogáveis | Sprint 43 |

## Deadline 12 — Run Completa + Meta Systems + Continuidade D / Início E (Sprints 45–48)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 45 | 12 | Remove Tower Layer + Settings Mínimo (áudio/display/idioma) + Floor Variants D (Floors 3–5) | Remoção de andar + configurações essenciais + conteúdo | Compra remove o Active Floor 1 automaticamente e remapeia escadas corretamente; menu de Settings com volume/resolução/idioma funcional; Floors 3–5 em D jogáveis | Sprint 6, 44 |
| 46 | 12 | Remote Controller + Floor Variants D (Floors 6–8) | Teleporte entre Floors + conteúdo | Q abre interface pausada, lista Floors por Active Floor Position (cooldown indicator na HUD), teleporta; Floors 6–8 em D jogáveis | Sprint 45 |
| 47 | 12 | Free Mode + Progress Tracking Completo + Day 15 Victory/Continue Flow + Floor Variants D (Floors 9–10, **conclui D = 10/10**) + Variants E (Floors 1–2, **início — nasce na Deadline 12**) | Segundo modo + desbloqueios reais + marco de vitória | Free completa um dia sem validar demanda; ao menos 1 herói desbloqueia por critério real; Dia 15 mostra Vitória, "Menu" mantém o checkpoint anterior e "Continuar" gera novo save pré-Dia 16; Floors 9–10 em D e 1–2 em E jogáveis | Sprint 46 |
| 48 | 12 | Steamworks Groundwork + Commercial Prep Drafts + Day 30 Ending Flow + Floor Variants E (Floors 3–5) | Base técnica de Steam + marco de encerramento + conteúdo | Achievement de teste dispara em ambiente Steam de teste; rascunhos de copy/tags/feature list/screenshot shot list/trailer storyboard produzidos; Dia 30 mostra Encerramento Definitivo sem gerar autosave extra; Floors 3–5 em E jogáveis | Sprint 47 |

## Deadline 13 — Content Complete + Balance + Store Preparation (Sprints 49–52)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 49 | 13 | Floor Variants E (Floors 6–8) + Bestiary/Boss Leftovers + Screenshots | Finalização de conteúdo — onda 1 | Floors 6–8 em E jogáveis; nenhum monstro/boss planejado do MVP falta; screenshots de divulgação capturados | Sprint 48 |
| 50 | 13 | Floor Variants E (Floors 9–10, **conclui E = 10/10 → 50/50 Variants**) + Revisão Final de Cards + Capsule Assets + Trailer (captura/edição) | Conteúdo do MVP 100% completo | As 50 Floor Variants existem; todas as pools de carta revisadas e completas; capsule assets e trailer prontos | Sprint 49 |
| 51 | 13 | Balance Pass + Performance Pass (integrado) + UI/UX Pass — Parte 1 + Audio/Juice Pass | Jogo calibrado, mais legível e com feedback sonoro básico | Ritmo econômico dentro do ciclo emocional do GDD; performance estável em run completa (não é a primeira vez que performance é olhada — checkpoints já ocorreram nas Sprints 15/32/42); HUD e Shop revisados por legibilidade; SFX básico de hit/crítico/pickup/venda/boss/morte/upgrade presentes | Sprint 50 |
| 52 | 13 | UI/UX Pass — Parte 2 + Store Page Assembly (usando trailer/capsule já prontos) + Depot/Branch Verification + Localização — Revisão Final | Integração, revisão e fechamento comercial | Results/Inventory/menus revisados; Steam Store Page pronta para revisão; depot/branch de teste verificados; textos traduzidos com revisão final (arquitetura e conteúdo já existiam desde a Sprint 4/36) | Sprint 51 |

## Deadline 14 — Stabilization & Release Candidate (Sprints 53–56)

| Sprint | Deadline | Nome | Objetivo principal | Entrega demonstrável | Dependência principal |
|---|---|---|---|---|---|
| 53 | 14 | Bug Fixing P0/P1 + Regression 1 | Estabilidade central | Zero bugs P0/P1 conhecidos após a rodada | Sprint 52 |
| 54 | 14 | Save/Load Validation + Performance Final | Persistência confiável | 5+ cenários de checkpoint validados sem erro, incluindo Dia 15 (Menu/Continuar) e Dia 30 | Sprint 53 |
| 55 | 14 | Steam Validation + Settings Validation + Localização Final Validation | Build pronto para Steam (validação, não criação) | Achievements/build/depot/branch validados; Settings e localização, já implementados desde a Deadline 12/13, são apenas conferidos aqui | Sprint 54 |
| 56 | 14 | **Release Candidate / Final Validation** | Build shippable | RC aprovado, pronto para publicação | Sprint 55 |

---

# AUDITORIA MECÂNICA FINAL

**Floor Variants (50/50):**
A: S20(2)+S21(2)+S25(2)+S31(4) = 10 ✓
B: S36(4)+S39(3)+S40(3) = 10 ✓
C: S40(1)+S41(3)+S42(2)+S43(2)+S44(2) = 10 ✓
D: S44(2)+S45(3)+S46(3)+S47(2) = 10 ✓
E: S47(2)+S48(3)+S49(3)+S50(2) = 10 ✓
**Total = 50/50 ✓.** Nenhuma sprint recebe mais de 4 Floor Variants; D nasce na Sprint 44 (Deadline 11, conforme exigido); E nasce na Sprint 47 (Deadline 12, antes do limite da Sprint 48).

**Heróis (10/10 Gameplay Complete):** Barbarian (S7) · Ranger (S17–18) · Mage (S19–20) · Druid (S21–22) · Rogue (S23) · Cleric (S23–24) · Paladin (S25–26) · Gunslinger (S26–27) · Assassin (S27) · Blood Mage (S28). ✅ Nenhuma sprint de herói combina mais de 2 kits completos + 1 conteúdo secundário.

**Main Menu / Run Flow:** Menu+New Game+Mode/Hero/Map Select+Create Run (S9) · Continue Game + Continue-sem-save desabilitado + New Game sem confirmação (S12) · Game Over funcional (S11) · Day 15 Victory/Continue (S47) · Day 30 Ending (S48). ✅ Todos com dono.

**Employees:** Buy (S37) · Promote+Sell (S38) · Helper/Collector básicos (S39) · Strong/Fast (S41) · Virtualização (S42) · Cross-Floor+Collector Filter (S43). ✅ Sell e Collector Filter deixaram de estar ausentes.

**Cards:** Framework (S35) → primeiras Hero Cards (S36) → Employee Cards (S44) → revisão final (S50). ✅ Nenhuma carta de herói implementada antes do Framework (Ranger em S18 só prepara o hook, sem a carta real).

**HUD progressivo:** Floor indicator (S6) · Energy (S7) · Health (S8) · Demand (S11) · Employee indicator (S39) · Remote cooldown (S46) · Pass de legibilidade (S51–52). ✅ Não nasce só no UI/UX Pass.

**Death Flow progressivo:** Fase 1 básica (S8: HP zero, -30s hook, respawn, Energia zera) → integração com loot/Day Timer/Game Over (S11). ✅ Sem "mega-refatoração" no final.

**Bestiary:** Batch 1 (S16) · Batch 2 (S20) · Batch 3 (S22) · Batch 4 (S25) · Batch 5 (S31) · Leftovers (S49). ✅ Distribuído.

**Bosses:** Framework+Boss 1 (S24) · Batch inicial (S26) · Batch intermediário (S34) · Batch superior 1 (S39) · Leftovers (S49). ✅ Distribuído, framework antes de conteúdo.

**Settings:** Implementado na S45 (Deadline 12); S55 apenas valida. ✅ Não nasce mais na S51 nem na S55.

**Steam/Store:** Groundwork técnico (S48) → rascunhos comerciais (S48) → screenshots (S49) → capsule/trailer (S50) → Store Page/depot (S52) → validação/publicação (S55–56). ✅ Distribuído em 5 pontos, nenhuma sprint concentra tudo — S52 passou a ser predominantemente integração e fechamento.

**Audio/Juice:** Pass explícito na S51 (junto de Balance/Performance/UI Parte 1), hooks via GameEvents existindo desde a S3. ✅ S52 ficou livre para ser só integração/fechamento.

**Localização:** Arquitetura desde S4; conteúdo real usando IDs desde que UI nasce (S9 em diante); revisão final na S52. ✅

**S53–56:** somente bug fixing, regression, save/load, validação de Steam/Settings/Localização e Release Candidate — nenhuma feature estrutural nova. ✅

---

# 56 Sprints — Distribution Freeze ✅

Todos os critérios da auditoria mecânica final foram satisfeitos: 50/50 Variants com volume plausível por sprint (máximo 4), 10/10 heróis com carga realista, Main Menu/Run Flow/Game Over/Day 15/Day 30 cobertos, Employees com Sell e Collector Filter atribuídos, Cards respeitando Framework→Content, HUD e Death Flow progressivos, Bestiary e Bosses distribuídos sem concentração final, Settings implementado antes da Deadline 14, Steam/Store distribuído, e a Deadline 14 contendo apenas estabilização/validação.

**Este é o Freeze definitivo da distribuição.** Não haverá nova auditoria geral desta tabela.

---

# DISTRIBUTION FREEZE CONCLUÍDO — PRONTO PARA DETALHAR A SPRINT 1.
