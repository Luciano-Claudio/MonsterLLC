# Game Design Document — Projeto Torre (nome provisório)
### Versão 1.01235 — Auditoria Final de Consistência e Freeze

> **Legenda de status**
> - ✅ **Decisão confirmada**
> - 🟡 **Direção de design / pendência** — precisa ser fechada antes da implementação daquele ponto específico
> - 🔢 **Pendência de balanceamento**
> - 🔭 **Visão Expandida**

> **Nota de versão:** esta é a última revisão editorial do GDD Mestre antes do início da implementação. Ela corrigiu os últimos vazamentos de regras do Modo Padrão para o Modo Free em todo o documento (Core Loop, morte ao zerar o timer, Ciclo de Dia, Vitória/Pós-Game, HUD, Feedback, Magnet, Demanda Diária, Ciclo Emocional — cada um agora deixa explícito o que é exclusivo do Padrão e o que é compartilhado); restaurou a definição funcional completa do **Controle Remoto** (Seção 26), que havia se perdido em revisões anteriores apesar de outras seções ainda referenciá-lo; alinhou o Chest System e a ficha do Mimic ao fluxo real de baú (E abre, UI de 3 opções, sem exigir um pergaminho físico coletável); e separou **Account Progression** de **Lifetime Statistics** dentro de Permanent Account State, deixando explícito que o Free pode registrar estatísticas informativas sem nunca acionar avaliação de progressão/unlock. Duas pendências deixaram de existir por serem simples decisões de UI (Continue sem save desabilitado; New Game sem exigência de confirmação). Nenhum sistema fora desse escopo foi redesenhado — combate, economia, Employees, Inventory, Pickup Radius, Magnet, Floor System, Save, Floor Variants, Remove Tower Layer, Stair Routing, Boss Timer, Druid, Ultimate, Drops, Cards, Quests e heróis permanecem exatamente como já estavam. **A partir desta versão, o GDD Mestre está estruturalmente congelado** (ver Seção 53).

---

## 1. Visão Geral

Roguelite de ação, exploração e economia. O jogador sobe uma torre de 10 andares fixos, cumprindo demandas diárias crescentes de Monster Essence ao longo de 15 dias por run, com pós-game opcional até o Dia 30. Combate ativo (mira e ataque pelo mouse), sem XP e sem level-up tradicional — o poder vem de progressão de arma persistente na run, economia, logística e automação via employees.

## 2. Pitch

*Você entra fraco, com uma bag minúscula e uma arma qualquer. A torre está infestada — e o reino quer sua cota diária de Essência. Cada subida é uma aposta: você consegue matar mais rápido do que consegue carregar?*

## 3. Gêneros e Referências

- **Gênero primário:** roguelite de ação com forte camada de gestão de tempo e economia.
- **Referência de loop econômico:** Coal LLC (demandas diárias, loja entre turnos, employees promovíveis).
- **Referência de combate:** ação com mira pelo mouse, ataques desviáveis, hordas — estética de Vampire Survivors, mas com ataque ativo, não automático.

## 4. Pilares de Design ✅

1. O jogador deve ficar absurdamente forte.
2. Toda vez que fica forte, deve existir algo acima dele.
3. Progressão econômica e habilidade devem coexistir.
4. O jogo não termina quando o jogador "vence".
5. Logística é tão importante quanto dano.
6. Runs recomeçam de verdade.

Fantasia central: *"Eu comecei contando Monster Essence de 1 em 1 e agora estou produzindo trilhões enquanto uma multidão de funcionários faz o trabalho por mim."*

---

## 5. Escopo — MVP vs. Visão Expandida

### MVP ✅
Torre de 10 andares (50 Floor Variants artesanais, 5 por andar — Seção 25) · 15 dias + pós-game até Dia 30 · 10 heróis (Seção 17) · sistema de demanda/venda/morte/save · inventário de loot · employees (ajudante + coletor) com árvore de promoção · 3 linhas de quest · baús com mimic + cartas · 2 traps (Falling Rock, Floor Spikes) · loja com 3 abas · Modo Padrão e Modo Free · agregação visual de drops e suporte a números grandes · Menu Principal com New Game / Continue Game / Settings / Exit · Map Selection (MVP: só a Torre).

### Visão Expandida 🔭
Heróis futuros (Demonologist, Necromancer, The Gambler, Plague Doctor — Seção 18) · modos futuros além de Padrão/Free · novos mapas além da Torre (Cripta, Oceano) · novas traps.

---

## 6. Menu Principal

Sistema formal com 4 opções: ✅

- **New Game** — inicia o fluxo completo de nova run (Seção 8).
- **Continue Game** — carrega diretamente o save existente, sem repetir nenhuma escolha (Seção 8, Seção 43).
- **Settings** — abre opções; conteúdo específico fica no documento de UI/UX (Seção 51).
- **Exit** — fecha o jogo.

---

## 7. Map Selection

Etapa formal do fluxo de **New Game**, que **não** aparece em Continue Game (a run salva já carrega o mapa escolhido anteriormente). ✅

- **MVP:** apenas 1 mapa jogável — **a Torre**.
- A arquitetura já suporta a etapa mesmo com um único mapa, para permitir expansão futura sem retrabalho estrutural.
- Mapas futuros (Cripta, Oceano) usariam a mesma estrutura geral de run, economia, heróis e progressão — sem regra própria definida agora. 🔭

---

## 8. Estrutura da Partida — Máquina de Estados

```text
MENU PRINCIPAL
│
├── New Game
│      ↓
│   Escolha do modo (Padrão / Free)
│      ↓
│   Escolha do herói (entre os desbloqueados)
│      ↓
│   Map Selection (MVP: só Torre)
│      ↓
│   Cria nova run — sorteia 1 Floor Variant por andar (Seção 25)
│      ↓
│   DIA 1 — Gameplay
│
├── Continue Game
│      ↓
│   Carrega o save existente — o checkpoint da última Loja alcançada (Seção 43)
│      ↓
│   Abre diretamente essa Loja
│      ↓
│   Start Day N
│      ↓
│   Gameplay
│
├── Settings
│
└── Exit
```

### Regra importante — Continue Game ✅
Ao escolher Continue Game, o jogador **não** passa novamente por escolha de modo, herói, mapa, ou pelo sorteio de Floor Variants — tudo já pertence à run salva e é carregado diretamente. O fluxo vai direto para a Loja do checkpoint salvo.

### Fluxo completo do Modo Padrão, dia a dia ✅

```text
DIA N — Gameplay
   ↓
Tempo chega a zero  OU  jogador usa a porta
   ↓
Validar demanda
   ↓
   ├─ Demanda NÃO cumprida → GAME OVER → run encerrada → Menu Principal
   │  (o save NÃO é apagado — continua sendo a Loja que precedia o Dia N; ver Seção 43)
   │
   └─ Demanda cumprida → Tela de Resultados → Loja → SAVE AUTOMÁTICO → Start Day N+1
```

**No Modo Free**, o mesmo fluxo ocorre sem a etapa de validação de demanda: `Tempo zera OU porta → Resultados → Loja → SAVE → Start Day N+1` (Seção 42).

Repete até o Dia 15.

### Dia 15 (Modo Padrão) — dois caminhos com consequências de save diferentes ✅

```text
DIA 15 — Demanda cumprida → Tela de Vitória Oficial
   │
   ├─ Menu Principal → run termina como vitória
   │      (NÃO passa por Resultados/Loja/novo autosave — o save
   │       permanece sendo a Loja que precedia o Dia 15; ver Seção 43)
   │
   └─ Continuar → Tela de Resultados do Dia 15 → Loja → AUTOSAVE →
                   (novo checkpoint = Loja que precede o Dia 16) → Start Day 16 → pós-game
```

**No Modo Free**, o Dia 15 é atingido pela conclusão normal do dia (sem demanda), mas a Tela de Vitória e as duas escolhas (Menu/Continuar) funcionam exatamente igual (Seção 42/44).

### Dias 16–30 (pós-game) ✅
Mesmo core loop, mesmo ciclo diário, mesma regra de save a cada Loja. No Padrão as demandas continuam crescendo; no Free não existem demandas (Seção 42).

```text
DIA 30 (Modo Padrão) — Demanda cumprida → Tela de Encerramento Definitivo
   ↓
Conquista Steam concedida
   ↓
Run termina obrigatoriamente
   ↓
Menu Principal
   (o save continua sendo a última Loja alcançada antes do Dia 30;
   não há autosave adicional após a tela de encerramento — Seção 43)
```

**No Modo Free:** `Dia 30 concluído normalmente → Tela de Encerramento Definitivo → Run termina → Menu Principal`, com o mesmo tratamento de save — mas **sem** a conquista Steam, que é Account Progression exclusiva do Padrão (Seção 44).

### Modo Standard vs. Modo Free — mesma máquina de estados, uma diferença ✅
Free usa exatamente a mesma estrutura de dia acima (gameplay → tempo zera/porta → Resultados → Loja → Save → Start Day N+1), incluindo Dia 15 e Dia 30. A única diferença está na validação de fim de dia: no Standard, o fim de dia verifica a demanda (não cumprida → Game Over); **no Free não existe demanda, então não existe essa verificação nem Game Over por ela** — o dia simplesmente encerra e segue direto para Resultados. Regras de progressão do Free (o que ele não concede) estão na Seção 42.

### O que salva, o que reseta (ver também Seção 15 e Seção 43) ✅
- Save é um **checkpoint da última Loja alcançada** — nunca um registro "por dia" que se apaga; ver semântica completa na Seção 43.
- **Run-Persistent:** gold, tier de arma por herói, bonuses comprados, employees possuídos, progresso de quests, cartas de pergaminho ativas, dia atual, modo, herói e mapa da run, e os 10 Floor Variants sorteados no início da run (Seção 25).
- **Daily:** tempo restante, demanda/progresso de venda (quando aplicável — Seção 42), inventário do jogador, monstros/bosses no mundo, loot no chão, Boss Timer de cada Floor (Seção 22), Energia da Ultimate (Seção 11).
- **Reseta ao iniciar uma nova run** (inclusive trocar de modo): tudo Run-Persistent e Daily, exceto Permanent Account State (heróis, achievements, estatísticas de conta — Seção 15).
- **Existe um único slot de save de run, compartilhado entre os modos** — não há save separado por modo. Um novo autosave sempre sobrescreve o anterior, independentemente de qual modo pertencia o save anterior ou o novo (Seção 43).

---

## 9. Sistema Central de Pausa

Regra central única, referenciada por todos os subsistemas dependentes de tempo. ✅

### O que aciona a pausa
Abrir o Inventário (TAB), abrir o Controle Remoto (Q), a seleção de cartas de pergaminho, e futuramente o Pause/Menu geral (Seção 46).

### O que a pausa interrompe
> **Regra: PAUSA = NENHUM TEMPO DE GAMEPLAY AVANÇA.**

Relógio do dia, monstros (movimento e IA), ataques, projéteis, employees, pets/summons dependentes de simulação, cooldown de ataque, cooldown do Controle Remoto, cooldown/ausência de Employees, duração de buffs temporários, duração da Ultimate, duração de transformações, Boss Timer de qualquer Floor (Seção 22), qualquer outro timer dependente da gameplay.

**Energia da Ultimate:** pausa não reduz nem zera a Energia — ela só avança por kills, nunca por tempo (Seção 11).

### Relação com o Sistema de Habilidades (Seção 12) ✅
Pausar entre o início da animação e o instante do hit congela o progresso sem cancelá-lo. Ao despausar, a habilidade continua exatamente daquele ponto. Isso é diferente da morte (Seção 11), que **cancela** em vez de congelar.

---

## 10. Core Loop

Loop compartilhado pelos dois modos (Seção 42):

```text
COMEÇA O DIA → entra na torre → combate ativo (LMB/RMB) →
coleta loot (chão → bag, TAB para ver) →
decide: volta e vende OU continua explorando com mais risco →
tempo esgota OU sai pela porta → resolução de fim do dia
```

**Modo Padrão:**
```text
resolução de fim do dia → validar demanda →
não cumprida → GAME OVER
cumprida → Resultados → Loja → Save → Start Day N+1
```

**Modo Free:**
```text
resolução de fim do dia → sem validação de demanda →
Resultados → Loja → Save → Start Day N+1
```

---

## 11. Sistema do Jogador

### Atributos base ✅
Vida, Vida Máxima, Dano (via fórmula abaixo), Chance de Crítico, Velocidade de Movimento, Velocidade de Ataque, Energia / Energia Total da Ultimate, Velocidade da Passiva.

**Não existe atributo de Armadura.** Defesa base é exclusivamente Vida/Vida Máxima; mecânicas defensivas adicionais (shield, cura, lifesteal, transformação) são recursos próprios de heróis específicos, nunca um sistema universal. ✅

### Fórmulas de dano e vida ✅
- **Poder Ofensivo Total** = Dano Base do Herói × Multiplicador de Dano da Arma (tier atual — Seção 19).
- **Dano final de uma fonte** = Poder Ofensivo Total × Participação da Fonte × Coeficiente da Habilidade × (1 + bônus de dano da run e da fonte).
- **Vida Máxima** = Vida Base do Herói × Multiplicador de Vida da Arma (tier atual — Seção 19) × (1 + soma dos bônus de Vida da run).
- Crítico dobra o dano do hit final quando ocorre.
- Pets/summons/passivas ofensivas automáticas consomem uma fatia do Poder Ofensivo Total (referências de proporção: ~40% para pet único / ~60% kit ativo; ~50% pool de summons do Necromancer com diminishing returns; ~25% para outras passivas automáticas — ajustáveis em balanceamento).
- Shields e curas passivas escalam sobre a **Vida Máxima final**; lifesteal usa a Vida Máxima apenas como teto de cura por hit (Seção 33 detalha a separação entre os três mecanismos).

### Energia da Ultimate — persistência diária (Daily) ✅
- A Ultimate carrega por **kills**, nunca por tempo — monstros mais fortes concedem mais carga por kill. O total necessário varia por herói.
- **A Energia é estado Daily do herói.** Permanece acumulada ao longo do dia inteiro, independentemente de o jogador trocar de Floor, retornar ao térreo, ou abrir menus:
  - Trocar de Floor: **não reseta.**
  - Voltar ao térreo: **não reseta.**
  - Abrir inventário/Controle Remoto (pausa): **não reseta nem reduz.**
- **A Energia zera em exatamente 3 situações, e apenas nelas:**
  1. O jogador **usa** a Ultimate (RMB com Energia cheia → Energia volta a 0).
  2. O herói **morre**.
  3. **O dia termina.**
- Fora dessas três situações, a Energia nunca reseta.

### Quais kills carregam a Ultimate — Hero-Owned Combat Source ✅
Nem toda kill próxima ao herói concede Energia. A regra: **fontes de combate pertencentes ao herói ("Hero-Owned") carregam a Ultimate; kills de Employees não.**

**Contam** (Hero-Owned Combat Sources):
1. Kills causadas diretamente pelo herói — ataque primário, Ultimate, área persistente, DoT, hitboxes, qualquer habilidade do próprio kit.
2. Kills causadas por **pets do kit** (Phoenix, Blood Elemental, equivalentes futuros).
3. Kills causadas por **summons pertencentes ao kit** (summons do Necromancer, criaturas temporárias de habilidades, outras entidades ofensivas do próprio herói).

Isso inclui explicitamente kills por DoT/área persistente mesmo que o herói já tenha se afastado fisicamente do local — **desde que o efeito continue ativo dentro do Current Combat Floor** (Seção 24): ex.: um monstro que morre no rastro de fogo do Mage ou nas facas persistentes do Ranger depois que o herói já se afastou para outro canto do **mesmo Floor** ainda conta, pois a fonte de dano pertence ao kit do herói e continua participando do Combat Scope corrente. Isso é diferente de **trocar de Floor**: um efeito deixado para trás ao mudar de Floor deixa de poder causar dano enquanto aquele Floor não for o Current Combat Floor (Seção 24) — o comportamento temporal exato do efeito nesse caso continua pendente (Seção 53), mas ele não gera kills nem Energia enquanto estiver fora do Combat Scope corrente.

**Não contam:**
- Kills causadas por **Ajudantes/Employees**, independentemente do tier, quantidade, dano, Strong/Fast, Floor, ou de quem iniciou o combate. Exemplo: monstro com 100 HP, jogador causa 90, Ajudante causa os 10 finais → a kill pertence ao Ajudante → **não concede Energia da Ultimate**.

Esta regra define apenas **quais kills contam** — não altera quanto de Energia cada monstro concede, o threshold de cada herói, ou a velocidade de carregamento (tudo isso continua 🔢 balanceamento, conforme já estabelecido: monstros mais fortes concedem mais Energia por kill).

### Movimentação e mira ✅
- **Movimento:** WASD. **Mira:** posição do mouse, resolvida em 8 direções (N, S, L, O, NE, NO, SE, SO). **Ataque primário:** LMB. **Ultimate:** RMB.

### Morte — ordem de eventos ✅
A penalidade de tempo não é uma animação com duração própria — é uma **subtração imediata**. Sequência estrutural:

```text
Player morre
   ↓
Cancela estados temporários ativos
   ↓
Destrói loot carregado
   ↓
Zera Energia da Ultimate
   ↓
TimeRemaining -= 30 segundos
   ↓
TimeRemaining <= 0 ?
   │
   ├─ NÃO → fade de tela → respawn no térreo com vida cheia → dia continua
   │
   └─ SIM → resolve encerramento do dia
              ↓
           Mode?
              ├─ Padrão → validar demanda
              │      ├─ Cumprida → Tela de Resultados
              │      └─ Não cumprida → Game Over (o save não é apagado — Seção 43)
              │
              └─ Free → sem validação de demanda → Tela de Resultados
```

- **Cancela imediatamente qualquer estado temporário ativo** (diferente de pausa, que apenas congela — Seção 9): Ultimate em andamento é interrompida, transformações (ex.: forma de urso do Druid — Seção 17.4) terminam **sem conversão proporcional de HP**, áreas/efeitos temporários no chão desaparecem, uma interação em curso é cancelada, summons **temporários** são destruídos.
- **Pets permanentes de kit** retornam junto com o personagem na transição. 🟡 Se a animação de summon/bloqueio inicial se repete nesse retorno ainda não está fechado.
- **Sem multiplicador de penalidade por profundidade do andar.**
- **Necromancer é exceção explícita a esta seção inteira quando implementado** — ver Seção 18 e Seção 33.

---

## 12. Sistema de Habilidades — Timing

Regra estrutural central: **o dano de uma habilidade não está automaticamente preso ao fim da animação.**

```text
Clique → Cast Start → Instante do primeiro Hit → Hits adicionais (se houver) →
Fim da animação → Recovery → Cooldown
```

Suporta ataques simples, multi-hit, em área, projéteis (Seção 13), ataques em duas fases, habilidades contínuas/persistentes, transformações, ultimates. **Relação com a pausa:** ver Seção 9. **A mesma filosofia estrutural se aplica ao timing de ataque dos monstros (Seção 22).**

---

## 13. Projéteis — Taxonomia

Regras gerais ✅: direção fixada no instante do disparo, não controlável depois de solto, tempo de vida próprio, quantidade máxima de alvos que pode atingir.

| Categoria | Comportamento | Exemplos |
|---|---|---|
| **Straight Projectile** | Reto, com ou sem perfuração | Flecha do Ranger, martelo do Paladin, projétil do Blood Mage |
| **Multi-Straight (leque)** | Múltiplos retos em ângulos distribuídos | Ranger com mais de 1 flecha |
| **Hitscan** | Dano instantâneo sem projétil visível | Gunslinger |
| **Homing** | Segue o alvo mais próximo | Ataque primário do Cleric |
| **Ground Target / Impact Area** | Viaja até colidir ou alcançar distância máxima, explode em área | Ultimate do Mage, ultimate do Blood Mage, Rat People, Goblin Raider |
| **Persistent Area** | Fica no lugar, dano contínuo | Facas da ultimate do Ranger, rastro de fogo do Mage |
| **Orbiting Hitbox** | Gira ao redor do herói | Adagas do Rogue, espadas da ultimate do Paladin, ossos do Necromancer |
| **Dash Damage** | Deslocamento curto, dano na área de chegada | Ataque primário do Assassin |
| **Summoned Target Hit** | Entidade sumonada mirando o alvo mais próximo | Vinhas do Druid, osso-boomerang do Necromancer |
| **Rotating Line / Sweep** | Linha de mira que gira progressivamente | Ultimate do Gunslinger |
| **Rectangular Beam** | Retângulo de dano fixo na direção da mira | Ataque primário do Demonologist |

Ataques em área centrados no próprio herói (Barbarian, Plague Doctor) e a ultimate global do Cleric (Seção 17.6) não usam projétil — são hitbox de área ou efeito de campo, não uma entidade que viaja.

---

## 14. Attack Budget

**Objetivo:** evitar que muitos monstros ataquem o jogador simultaneamente. ✅

- Melee e Ranged têm budgets separados.
- Monstros fora do budget continuam existindo e agindo (perseguindo, cercando, esperando slot).
- **Bosses não participam de nenhum Attack Budget comum.**
- **Employees não participam.**
- **Traps não participam** — ameaça ambiental independente, podem causar dano mesmo com budgets cheios.
- **Não limita a população total do andar** (Seção 23) — só quantos estão efetivamente atacando.

🔢 Valores máximos dos budgets Melee e Ranged pendentes de playtest/balanceamento.

---

## 15. Terminologia de Persistência

Três categorias de dado, usadas de forma consistente em todo o documento: ✅

- **Permanent Account State** — nunca reseta, independente de run. Divide-se em duas categorias distintas (Seção 49):
  - **Account Progression** — heróis desbloqueados, achievements, e demais recompensas/critérios permanentes de unlock.
  - **Lifetime Statistics** — dados puramente informativos de perfil (total histórico de kills, gold vendido, dias jogados, bosses mortos, etc.). Uma estatística registrada **não** é, por si só, progressão.
- **Run-Persistent** — persiste entre os dias de uma run, reseta ao iniciar nova run: tier de arma (Seção 19), **buffs persistentes da run** obtidos por cartas de pergaminho (Seção 31), bonuses comprados (Seção 41) incluindo upgrades de Pickup Radius (Seção 37), employees (Seção 34), progresso de quests (Seção 29), gold, modo/herói/mapa da run, os 10 Floor Variants sorteados no início da run (Seção 25), Remove Tower Layers aplicados (Seção 27).
- **Daily** — reseta todo dia: tempo restante, vendas realizadas no dia, progresso da demanda quando aplicável ao Modo Padrão (Seção 42), inventário do jogador, monstros/bosses no mundo, loot no chão, Boss Timer de cada Floor (Seção 22), Energia da Ultimate (Seção 11).

**Escadas não fazem parte de nenhuma lista de reset** — são geografia fixa de cada Floor Variant (Seção 26), não são um dado de estado do jogador.

O termo "permanente" isolado é evitado — cada sistema Run-Persistent é descrito como tal. Cartas de baú são descritas como **"buffs persistentes da run"**, nunca "buffs temporários".

---

## 16. Regras Comuns a Todos os Heróis

- Todos possuem os atributos da Seção 11.
- Ataque primário no LMB, Ultimate no RMB carregada por Energia via kills (Seção 11).
- **Não existe progressão por XP ou level-up durante a exploração.** Buffs persistentes da run (Seção 15) são obtidos exclusivamente através de baús/pergaminhos encontrados durante os dias (Seção 30–31), enquanto upgrades econômicos e a progressão de arma são adquiridos na loja entre dias (Seção 19, Seção 41).
- Desbloqueio é Account Progression (Seção 15).
- O jogo precisa rastrear tudo que o jogador faz numa run para viabilizar qualquer critério de desbloqueio (Seção 49).

---

## 17. Heróis do MVP (10)

### 17.1 Barbarian — inicial ✅
- **Dano Base / Vida Base:** 2,0 / 42.
- **Ataque primário:** golpe frontal em área circular à frente do personagem — hitbox própria, sem projétil. A área é relativa à direção do personagem no momento do ataque, sem necessidade de alvo travado; o raio da área pode aumentar através de baús/upgrades da loja.
- **Ultimate:** salto no chão seguido de múltiplos projéteis soltos em área, disparados em todas as direções ao mesmo tempo (categoria Ground Target/Impact Area — Seção 13).
- **Passiva:** nenhuma.
- **Targeting:** nenhum — área fixa relativa à direção do personagem.
- **Cartas específicas:** nenhuma.
- **Desbloqueio:** disponível desde o início da conta.
- **Particularidade/filosofia:** é o herói de referência para "posicionamento importa mais que dano puro" (Seção 21) — seu desempenho depende diretamente de o jogador agrupar inimigos antes de atacar, já que tanto o primário quanto a ultimate são ataques em área que recompensam múltiplos alvos agrupados.

### 17.2 Ranger ✅
- **Dano Base / Vida Base:** 1,6 / 30.
- **Ataque primário:** flechas retas na direção da mira (categoria Straight Projectile). Aumentar a quantidade de flechas forma um leque distribuído (2 flechas = 30° entre si, 3 = 15°...), classificado como Multi-Straight (Seção 13), até o **limite de 5 flechas**.
- **Ultimate:** facas disparadas em todas as direções que, ao pousarem, **permanecem temporariamente no chão** (Persistent Area) causando dano a qualquer monstro que passe por cima delas durante sua duração.
- **Passiva:** nenhuma.
- **Targeting:** direcional pela mira, sem travamento de alvo.
- **Cartas específicas:** aumento da quantidade de flechas do ataque primário, até o teto de 5 — ao atingir o limite, essa carta **deixa de aparecer** nos baús para aquele herói pelo resto da run.
- **Desbloqueio:** vencer 1 partida com o Barbarian.

### 17.3 Mage ✅
- **Dano Base / Vida Base:** 2,4 / 24.
- **Ataque primário:** fogo frontal formando um arco na direção da mira — hitbox de área, sem projétil que viaja; cooldown e dano melhoráveis via baú/loja.
- **Ultimate:** bola de fogo (Fireball) lançada na direção da mira que **explode** ao colidir com uma entidade ou ao alcançar uma distância curta, causando dano em área no ponto de impacto, e **deixa um rastro de fogo persistente no chão** que continua causando dano contínuo a quem passar por cima (Ground Target/Impact Area + Persistent Area combinadas — Seção 13).
- **Passiva:** pet **Phoenix**. A Phoenix **não é um alvo válido** para monstros (nunca é atacada). É sumonada automaticamente no **início de cada dia**, com uma animação de aproximadamente 2 segundos durante a qual o Mage fica com o **movimento bloqueado**. Consome uma fatia do orçamento ofensivo do herói (Seção 11), não soma dano extra ao kit ativo.
- **Cartas específicas:** dano, velocidade e velocidade de ataque da Phoenix, todas em %.
- **Desbloqueio:** completar X vendas de Essência em um único dia. 🔢 valor de X pendente de balanceamento.

### 17.4 Druid ✅
- **Dano Base / Vida Base:** 1,8 / 36.
- **Ataque primário:** vinhas nascem na posição dos monstros mais próximos (categoria Summoned Target Hit — Seção 13). A distribuição entre alvos é inteligente: com múltiplas vinhas disponíveis e vários monstros ao redor, o sistema distribui cobrindo o máximo de monstros diferentes possível; **se houver poucos monstros no alcance, múltiplas vinhas podem se concentrar no mesmo alvo** em vez de ficarem ociosas. Começa com **1 vinha** por ativação, quantidade ampliável via baú/loja.
- **Ultimate — transformação em urso, regra completa:**
  1. Ao ativar, o Druid se transforma em urso: **Vida Máxima aumenta temporariamente** (multiplicador 🔢 pendente de balanceamento — o valor ×2 usado nos exemplos é apenas ilustrativo); o **ataque primário muda** para um golpe frontal em arco (mesma família de área do Barbarian) enquanto transformado; **mais dano melee** e **maior velocidade de movimento** na forma de urso.
  2. **No instante da transformação, o Druid é curado para 100% da Vida Máxima da forma de urso** — funciona como uma cura completa.
  3. Enquanto transformado, a vida se comporta normalmente (dano recebido reduz a vida atual do urso normalmente).
  4. **Quando a transformação termina normalmente, ou é cancelada por qualquer motivo que NÃO seja morte, o percentual de vida é convertido proporcionalmente para a forma humana** — nunca um valor absoluto, nunca travado no máximo humano, nunca restaurado automaticamente para 100%:

     ```text
     HealthRatio = CurrentBearHealth / BearMaxHealth

     HumanCurrentHealth = HumanMaxHealth × HealthRatio
     ```

     Exemplo ilustrativo (valores de HP apenas para explicar a regra, não confirmados como balanceamento): forma humana com 20/100 → ativa a Ultimate → cura para 200/200 (urso) → recebe dano, fica em 180/200 (90%) → Ultimate termina → retorna como 90/100 na forma humana.

  5. **Morte durante a transformação é a única exceção — NÃO exige a conversão proporcional acima.** Se o HP do urso chega a 0, o Druid morre e segue diretamente o fluxo universal de morte (Seção 11): a transformação é cancelada, a Energia zera, o loot é perdido, os 30s de penalidade se aplicam, e ele reaparece no térreo em forma humana **com vida cheia**, igual a qualquer outro herói.
- **Passiva:** nenhuma.
- **Cartas específicas:** aumento da quantidade de vinhas sumonadas por ativação do primário.
- **Desbloqueio:** vencer 1 partida com o Mage.
- **Particularidade de implementação:** a ultimate substitui temporariamente todo o kit de ataque primário — exige máscara de estado clara ("transformado" vs. "normal"). A conversão proporcional de HP só se aplica a fim natural ou cancelamento sem morte; morte segue a regra padrão, sem exceção adicional.

### 17.5 Rogue ✅
- **Dano Base / Vida Base:** 1,5 / 28.
- **Ataque primário:** facas orbitando o personagem em raio curto (categoria Orbiting Hitbox — Seção 13), causando dano a monstros que colidirem com elas; velocidade de giro e dano melhoráveis via baú/loja.
- **Ultimate:** bomba lançada na posição do mouse no instante do clique (Ground Target), dano em área ao explodir; possui **cooldown mais baixo** que os demais heróis, permitindo uso mais frequente.
- **Passiva:** nenhuma; **velocidade de movimento base superior** aos outros heróis é característica intrínseca do kit, não um efeito periódico.
- **Targeting:** nenhum no primário (orbital ao redor do herói); Ground Target na ultimate.
- **Cartas específicas:** nenhuma.
- **Desbloqueio:** completar 30 dias. 🟡 O escopo exato desse critério — se precisa ocorrer em uma única run ou se pode acumular entre runs — ainda não foi definido pelo designer (ver Seção 53).

### 17.6 Cleric ✅
- **Dano Base / Vida Base:** 1,7 / 32.
- **Ataque primário:** projétil que persegue o monstro mais próximo (categoria Homing — Seção 13).
- **Ultimate:** oração — efeito de área global: **todos os monstros em campo** (não só ao redor do Cleric) ficam paralisados e sofrem dano por segundo durante um tempo curto. Precisa de uma animação de efeito individual sobre a cabeça de cada monstro afetado.
- **Passiva:** cura periódica ao longo do tempo, com uma **animação/aura própria** sobreposta ao personagem (objeto filho do GameObject principal) — mecanismo estrutural distinto de shield e de lifesteal (Seção 33).
- **Cartas específicas:** aumento do valor da cura periódica, em %.
- **Desbloqueio:** vencer 1 partida com o Druid.

### 17.7 Paladin ✅
- **Dano Base / Vida Base:** 1,8 / 50 (maior Vida Base do MVP).
- **Ataque primário:** martelo arremessado, projétil reto na direção da mira (Straight Projectile — Seção 13).
- **Ultimate:** 2 espadas orbitando o Paladin (Orbiting Hitbox), dano a monstros que colidirem, durante alguns segundos.
- **Passiva:** de tempos em tempos ganha um **shield** com **vida própria** (objeto filho, arte sobreposta ao herói) que absorve dano no lugar do Paladin; ao ser destruído, entra em **cooldown** até reaparecer.
- **Cartas específicas:** aumento de quanto o shield pode absorver de dano, em %.
- **Desbloqueio:** vencer 1 partida com o Cleric.

### 17.8 Gunslinger ✅
- **Dano Base / Vida Base:** 1,2 / 28 (menor Dano Base do MVP, compensado por múltiplos tiros).
- **Ataque primário:** tiros instantâneos — sem projétil físico viajando (categoria Hitscan — Seção 13). **Quanto mais balas** o herói tiver, **mais impreciso** o disparo (forma um cone de dispersão); com 1 tiro só, é uma linha reta curta. A **animação de disparo precisa repetir** a cada bala dentro da mesma rajada, exigindo ajuste de velocidade de animação proporcional à quantidade de balas.
- **Ultimate:** linha de mira (Rotating Line/Sweep — Seção 13) que gira progressivamente ao longo da animação, do SE de volta ao SE.
- **Passiva:** nenhuma.
- **Cartas específicas:** aumento da quantidade de balas disparadas no ataque primário.
- **Desbloqueio:** vencer 30 dias jogando com o Ranger.

### 17.9 Assassin ✅
- **Dano Base / Vida Base:** 2,5 / 28 (maior Dano Base do MVP).
- **Ataque primário:** dash curto na direção da mira (categoria Dash Damage — Seção 13) que causa dano em área no ponto de chegada. O deslocamento tem **distância limitada mesmo que o mouse esteja muito mais longe** — o dash sempre percorre a mesma distância curta, independente de onde a mira estiver apontando além desse alcance.
- **Ultimate:** o Assassin fica envolto em sombras (stealth) — **monstros deixam de enxergá-lo** e passam a se comportar como se estivessem sozinhos (voltam ao padrão de movimentação aleatória, sem perseguir); durante a duração, o **dano do dash aumenta muito** e o **dash fica sem cooldown**, permitindo encadear vários dashes seguidos.
- **Passiva:** nenhuma.
- **Cartas específicas:** nenhuma.
- **Desbloqueio:** alcançar o Andar 6. 🟡 Ainda precisa ser definido se esse critério utiliza Original Floor Identity 6 ou Active Floor Position 6 após remoções de Tower Layers (Seção 24, Seção 53).

### 17.10 Blood Mage ✅
- **Dano Base / Vida Base:** 2,1 / 34.
- **Ataque primário:** projétil reto na direção da mira (Straight Projectile) que **cura o próprio Blood Mage ao acertar** — mecanismo de lifesteal estrutural, baseado no dano causado, não uma cura independente (ver separação formal na Seção 33).
- **Ultimate:** bola jogada na posição do mouse que cresce e perde o centro — **dano em 2 fases espaciais**: primeiro um quadrado interno, depois 8 quadrados formados nos vértices ao redor (Ground Target/Impact Area em duas fases — Seção 13).
- **Passiva (dupla):** *(1)* **lifesteal em todo ataque** — cura baseada em percentual do dano causado, com teto por hit baseado em % da Vida Máxima (regra completa na Seção 33); *(2)* pet **Elemental de Sangue**, não alvejado por monstros, com o mesmo bloqueio inicial de movimento (summon-lock) no início do dia que a Phoenix do Mage.
- **Cartas específicas:** dano, velocidade e velocidade de ataque do pet Elemental de Sangue, em %.
- **Desbloqueio:** matar X unidades de um monstro específico em um único dia. 🔢 monstro e valor de X pendentes de balanceamento — a estrutura do critério (monstro específico + quantidade em um único dia) já está definida, só os valores exatos ficam em aberto.

---

## 18. Heróis Futuros — Visão Expandida 🔭

**Não fazem parte dos 10 do MVP.**

- **Demonologist** — ataque primário: raio frontal retangular na direção da mira (Rectangular Beam). Ultimate: pentagrama que sumona uma criatura com dano alto. Passiva: pet não-alvejado, mesma família de Mage/Blood Mage. Cartas previstas: dano/velocidade/atk speed do pet; quantidade de pentagramas. Condição de desbloqueio: não definida.
- **Necromancer** — ataque primário: osso-boomerang (vai e volta, explode ao atingir o limite de alvos). Ultimate: 3 ossos orbitando o personagem (Orbiting Hitbox). Passiva dupla: *(1)* sumona esqueletos periodicamente, alvejáveis por monstros (diferente dos pets de Mage/Blood Mage/Demonologist), tempo de vida próprio ampliável, podem stackar múltiplas instâncias; *(2)* **2 vidas fixas por dia**. Ao perder a primeira vida: os summons ativos **são destruídos** (regra padrão, sem exceção), ele vira uma alma sem ataque/interação/uso de escada, imune a monstros, só podendo se mover; após alguns segundos, retorna com vida cheia (ajustável para ~50% em balanceamento futuro). **A exceção do Necromancer está apenas no comportamento de morte do herói em si** (não retorna imediatamente ao térreo, entra em estado de alma), **não na destruição dos summons**. Cartas previstas: dano/velocidade/atk speed/vida dos summons. Condição de desbloqueio: não definida.
- **The Gambler** — ataque primário: projétil de carta na direção da mira. Ultimate: 6 cartas aparecem ao redor do personagem e caem, explodindo em 6 círculos de dano. Passiva dupla, ativada periodicamente conforme a vida atual: carta de coração cura quando vida <100%; carta de diamante cria shield quando vida >100%. 🟡 **Pendência de design:** o material original não explica como o personagem chegaria acima de 100% de vida — não inventar overheal ou buff de HP temporário até revisão. Cartas previstas: cura da passiva e capacidade de bloqueio do shield, em %. Condição de desbloqueio: não definida.
- **Plague Doctor** — ataque primário: onda de ratos avançando (trigger retangular que caminha para frente e desaparece, dano em quem tocar). Ultimate: dano em área circular centrada no personagem, girando o cajado, sumonando fogos-fátuos ao redor. Passiva: 2 orbs orbitando infinitamente ao redor do personagem, dano ao colidir com monstros, mesmo bloqueio inicial de movimento dos outros summons de início de dia. Cartas específicas: nenhuma. Condição de desbloqueio: não definida.

---

## 19. Upgrades de Arma — Progressão Run-Persistent

### Regras estruturais ✅
- **Não são itens físicos.** Não ocupam inventário, não são vendáveis, não podem ser trocadas por tier inferior.
- 15 tiers compráveis na aba Upgrades: Copper, Iron, Steel, Silver, Sapphire, Emerald, Amethyst, Gold, Ruby, Diamond, Arcane, Infernal, Nightmare, Void, Divine. Arma Básica (tier 0) é gratuita, não conta como um dos 15.
- **Compra sequencial obrigatória:** exige possuir o tier imediatamente anterior; o botão do próximo fica bloqueado até isso.
- **Sistema percentual, nunca absoluto** (fórmulas na Seção 11): a arma multiplica o Dano Base e a Vida Base do herói.
- Prefixo do tier é universal; representação visual muda por herói.
- **Sem venda, sem downgrade.**
- **Reseta por completo em nova run** — volta ao tier 0. Progressão **Run-Persistent** (Seção 15), nunca chamada de "permanente".

### Filosofia de progressão ✅
Regra prática: uma arma ideal domina o andar anterior, é adequada para o andar-alvo, e ainda sofre no andar seguinte — essa relação de 3 andares orienta o ritmo de progressão, não apenas os multiplicadores em si. Tabela completa dos 15 tiers fica no documento de balanceamento (Seção 51).

---

## 20. Economia dos Primeiros Dias — Filosofia de Ritmo

### Padrão de sensação esperado ✅
- **Dia 1:** Arma Básica, bag minúscula (5 slots/stack 16) já cria decisões de risco mesmo no andar mais fácil. Renda esperada ao fim do dia: suficiente para o primeiro upgrade de arma (Copper).
- **Dia 2:** com o primeiro upgrade, o Andar 1 fica sensivelmente mais fácil — mas subir ao Andar 2 devolve a fragilidade, introduzindo ameaças novas (ranged, explosivos). Escolha entre caminho seguro e caminho arriscado.
- **Dia 3 em diante:** o gargalo passa a ser "carregar tudo que consigo matar" — a bag pequena cria desejo genuíno pelos upgrades de slots, stack, employees e filtros.

### Ciclo emocional esperado ✅
Pressão ("preciso cumprir a demanda") → Eficiência ("se eu agrupar esses inimigos consigo matar vários") → Limitação ("minha bag está cheia") → Decisão ("desço agora ou arrisco mais?") → Alívio ("cumpri a demanda") → Recompensa ("tenho dinheiro para um upgrade") → Power Fantasy ("esse andar ficou fácil") → Curiosidade ("será que consigo subir?") → Choque ("esses monstros não morrem") → repete. **Este ciclo emocional descreve principalmente o Modo Padrão, que é a experiência principal de progressão da campanha; o Free preserva combate/economia/logística sem a camada de pressão da quota** (Seção 42). Esse ciclo é o critério para validar calibração de qualquer novo sistema, arma ou andar — não os preços em si. Valores de referência ficam no documento de balanceamento (Seção 51).

---

## 21. Habilidade × Eficiência

Dois jogadores com exatamente os mesmos upgrades podem terminar um dia com resultados bem diferentes — isso é desejado, não falha de balanceamento. ✅ Diferenciais de jogador habilidoso: agrupar inimigos para aproveitar ataques em área (ex.: Barbarian), alinhar arcos de ataque, desviar de telegraphs, escolher rotas de coleta eficientes, retornar ao térreo só quando necessário.

---

## 22. Monstros e Bosses — Arquitetura de Comportamento

### Categorias ✅
Melee, Ranged, Boss. Variações Suporte/híbrido (ex.: Orc Shaman com totens) tratadas como variação dentro de Ranged/Support, sem virar categoria própria no MVP.

### IA comum (Melee/Ranged) ✅
- Movimentação aleatória por padrão, evitando obstáculos.
- Ao entrar no raio de observação, o jogador é detectado e o monstro passa a perseguir diretamente.
- **Melee:** aproxima-se e tenta permanecer em alcance de contato; ao atingir o raio de ataque, executa seu único ataque; entra em cooldown pós-ataque.
- **Ranged:** mantém distância e dispara seu único ataque ao alcançar o raio de ataque; mesma lógica de cooldown.
- Cada monstro comum tem apenas **1 tipo de ataque** — diversidade vem da variedade de monstros, não de múltiplos ataques por indivíduo (exclusivo de Bosses).

### Atributos de monstro comum ✅
Vida, Vida Máxima, velocidade de ataque, velocidade de movimento, raio de observação, raio de ataque. **Sem Armadura** (Seção 11).

### Timing de ataque de monstros — regra completa restaurada ✅
Cada ataque de monstro precisa suportar uma linha do tempo própria, seguindo a mesma filosofia estrutural das habilidades de herói (Seção 12) — **o dano não é simplesmente aplicado ao final da animação**:

```text
Attack Animation Start
   ↓
Telegraph (indicação visual do ataque chegando)
   ↓
Hitbox Activation Moment (instante em que o dano passa a poder ocorrer)
   ↓
Hitbox Active Duration (janela em que a hitbox está de fato ativa)
   ↓
Attack End (fim da animação)
   ↓
Cooldown (até o próximo ataque)
```

Nem todo ataque precisa de um telegraph longo, mas o sistema **precisa suportar** essa linha do tempo configurável para qualquer monstro, do mesmo jeito que suporta para os heróis.

### Leitura de ataque — filosofia de telegraph ✅
- O desafio emerge da combinação simultânea de ameaças, não do ataque isolado.
- Ataques direcionados ao jogador **travam o alvo** (`TargetPosition`) no início da animação, não continuam atualizando até o impacto — preserva a sensação legítima de "eu desviei".
- Cada tipo de ataque tem uma "linguagem" própria de ameaça: espada/mordida pune ficar perto; lança pune manter distância média frontal; projétil obriga movimento lateral; explosão obriga sair de área; ataque no chão pune ficar parado; dash corta rota; stun torna outros monstros mais perigosos; summon aumenta pressão com o tempo; cura faz certas unidades virarem prioridade de alvo.
- Em andares mais altos, alguns inimigos podem usar previsão leve de movimento (`Target = PlayerPosition + PlayerVelocity × PredictionTime`) — ferramenta disponível, não obrigatória em todo monstro.
- Velocidade de movimento do jogador não deve crescer a ponto de tornar todo ataque inimigo irrelevante.

### Riders / geração de unidades ao morrer ✅
Alguns monstros, ao morrer, geram outras unidades (ex.: gera 1 esqueleto + 1 montaria esquelética). Unidades geradas podem dropar loot próprio.

### Contingência — simplificação de monstros comuns 🟡
Se playtests mostrarem que hordas grandes com animação individual completa ficam ilegíveis, custosas de implementar em escopo solo, ou menos divertidas, os monstros **comuns** (nunca os heróis) podem ser simplificados para 3 comportamentos: Melee por contato, Ranged simples, Explosivo/contato especial. Contingência documentada, não decisão tomada.

### Bosses ✅
- Possuem mais de um ataque, com cooldowns próprios por ataque (diferente de monstro comum, que tem só 1).
- **Não participam do Attack Budget comum** (Seção 14).
- Matar um boss de topo (ex.: Divine God) **não encerra a run** — é conquista, não condição de vitória.

### Boss Timer — regra final ✅
- **Individual por Floor**, e é estado **Daily**.
- Acumula **apenas o tempo que o jogador passou naquele Floor durante o dia** — não é um timer contínuo que exige o jogador sem sair.
  ```text
  Floor 2: jogador fica 20s → BossTimer[Floor2] = 20s
  Sobe para o Floor 3 → BossTimer[Floor2] permanece guardado em 20s
  Retorna ao Floor 2 → volta a acumular a partir de 20s
  Ao atingir o threshold (🔢 referência ~50s) → boss aparece
  ```
- Trocar de Floor **não reseta**. Morte **não reseta**. Voltar ao térreo **não reseta**. Pausa **não avança** (Seção 9).
- **Reseta apenas quando o dia termina.**
- Se houver múltiplos bosses possíveis no mesmo Floor, todos spawnam simultaneamente quando o timer completa (sujeito a balanceamento).
- 🟡 **Pendência mantida:** como o timer não reseta ao ser atingido, a pergunta real não é "atingir o mesmo threshold de novo" — é: **após o primeiro Boss Spawn de um Floor, como é determinado o próximo spawn periódico daquele Floor, especialmente se o boss anterior ainda estiver vivo?** Não presumir novo intervalo fixo, reset parcial, fila, timer secundário, ou limite de bosses simultâneos.

---

## 23. Sistema de População (Spawn/Respawn)

Regra principal: **um andar nunca deve parecer vazio.** ✅

### Distinção formal — Population ≠ Attack Budget ✅
Population System determina **quantos monstros existem**; Attack Budget (Seção 14) determina **quantos estão atacando simultaneamente**. Podem existir 100 monstros vivos, mas só X melees e Y rangeds atacando naquele instante. Bosses, Employees e Traps ficam fora do Attack Budget.

### Três valores por andar 🟡
Minimum Population, Target Population, Maximum Population. Reposição gradual abaixo do Target.

### Onde monstros nascem ✅
Posições válidas do mapa (da Floor Variant ativa — Seção 25), fora da câmera quando possível, distância mínima do jogador, nunca em paredes/escadas/interações.

### Reação a jogador matando rápido ✅
Aumenta a **frequência de reposição** (até o teto do Maximum), não Vida/Dano dinamicamente.

### Interações ✅
- **Boss:** timer independente da população comum (Seção 22).
- **Troca de Floor:** a população de cada Floor evolui de forma independente — não reseta ao sair e voltar dentro do mesmo dia.
- **Remoção de andar (Seção 27):** população do Floor removido deixa de ser relevante.
- **Employees:** não interferem na população.

### Population State persiste — simulação completa não é obrigatória ✅
O **estado** da população precisa persistir enquanto o jogador está em outro Floor, mas isso não exige que toda a simulação (pathfinding, Animator, targeting, AI Update, ataques, colisões, movimentação) continue rodando em tempo real fora da região ativa — pode ser suspensa, virtualizada, atualizada logicamente, ou reconstruída ao retornar (decisão técnica, Seção 51). A única regra fixa de gameplay: **sair e voltar para um Floor não pode parecer um reset artificial/explorável da população** (ver também Seção 24).

🔢 Valores exatos e curva de reposição pendentes de playtest.

---

## 24. Floor System — Estrutura Técnica de Andares

### Uma única Unity Scene de gameplay ✅
Todos os Floors utilizados durante a run existem dentro de **uma única Scene de gameplay**. Mudar de andar **não carrega outra Scene** — a escada apenas teleporta o jogador entre regiões fisicamente diferentes da mesma Scene (regra completa de teleporte na Seção 26).

### Identidade visual por Floor ✅
Cada Floor pode ter configuração própria de **Global Light**: ao mudar de Floor, o Current Floor muda, a Global Light muda, e o ambiente aparenta ser completamente diferente, mesmo dentro da mesma Scene. Detalhes de arte pertencem aos documentos de Art/UI/Level Design (Seção 51).

### Persistência do mundo entre Floors ✅
Loot deixado num Floor **não desaparece** ao trocar de andar — permanece no mesmo lugar até ser coletado, vendido pelo Coletor, ou destruído ao fim do dia junto com o resto do estado Daily (Seção 15).

### Floor existir ≠ Floor precisar simular tudo — Floor State ≠ Floor Simulation ✅
O Floor onde o jogador está pode ter simulação completa; Floors fora da região ativa podem estar **Sleeping** — sistemas caros suspensos, reduzidos, virtualizados ou simplificados — **desde que o estado do mundo seja preservado**: Floor Variant, Original Floor Identity, Active Floor Position, Boss Timer, loot existente, Population State e demais dados Daily continuam existindo mesmo dormindo. Implementação exata é decisão técnica (Seção 51).

### Coletores e Floors fora da tela ✅
Coletores podem localizar/coletar loot de Floors ativos fora do Floor atual do jogador (Seção 36), sem exigir busca física por todos os GameObjects da Scene nem simulação individual de cada Employee — implementação livre.

### Combat Scope — combate opera no Floor atual ✅
A existência física de monstros de outros Floors dentro da mesma Scene **não os torna alvos válidos** para o jogador. Por padrão, qualquer sistema de combate do herói considera apenas o **Floor atual do jogador** (conceitualmente, `CurrentFloor`/`CurrentActiveFloor` — nome técnico não obrigatório) como domínio de busca e aplicação. Isso se aplica de forma geral, sem precisar repetir a regra em cada ficha de herói:

- **"Inimigo mais próximo"** (ex.: Homing do Cleric, Summoned Target Hit das vinhas do Druid) significa o mais próximo **dentro do Floor atual**, nunca da Scene inteira.
- **"Todos os monstros em campo"** (ex.: ultimate global do Cleric — Seção 17.6) significa todos os monstros **do Floor atual**, não de Floors acima, abaixo, Sleeping, ou fisicamente distantes na mesma Scene.
- Vale igualmente para qualquer projétil, área, hitbox orbital, homing, summoned target hit, dash damage, rotating line, beam, pet ou summon do kit do herói (Seção 13, Seção 33) — nenhum desses sistemas deve atingir acidentalmente uma entidade de outro Floor só porque tudo existe na mesma Scene.
- **Pets e summons do kit do herói** (Phoenix, Blood Elemental, summons do Necromancer, e equivalentes futuros) combatem somente no Floor atual do jogador, salvo exceção futura explicitamente documentada.
- **Ajudantes de combate (Employees)** também operam apenas no Floor atual do jogador (Seção 34) — um Ajudante nunca escolhe como alvo um monstro de outro Floor.
- **Attack Budget** (Seção 14) considera apenas as ameaças do Floor atual — os budgets Melee/Ranged não se tornam um pool global somando monstros de todos os Floors da Scene.
- A implementação técnica (layers, FloorId, registries, filtros, ou outra estratégia) não é definida aqui — o GDD define apenas o comportamento esperado.

**Exceção documentada — Collectors:** a única exceção cross-Floor confirmada continua sendo o Coletor Employee (Seção 34/36), que pode buscar loot em outros Floors ativos por regra logística própria. Isso não transforma ataques, pets, ultimates, homing ou Ajudantes em sistemas cross-Floor — logística de loot (cross-Floor) e escopo de combate (Floor atual) permanecem conceitos separados.

### Efeitos de combate persistentes ao trocar de Floor — sem dano off-Floor, comportamento pendente 🟡
O Combat Scope acima define o domínio de busca/aplicação, mas não resolve sozinho o que acontece com um efeito de combate **já existente** quando o jogador muda de Floor. Exemplos: uma área de fogo do Mage ou facas persistentes do Ranger (Seção 13) ainda ativas no Floor que o jogador acabou de deixar; um summon temporário; qualquer outro efeito com duração restante.

**Já decorre do Combat Scope, e está confirmado:** um efeito de combate deixado no Floor anterior **não pode continuar causando dano ativamente** enquanto aquele Floor não for o Current Combat Floor do jogador — isso evita, por exemplo, um efeito abandonado continuar matando monstros e gerando loot/Ultimate/progressão sozinho fora do Floor atual (a regra de Hero-Owned Kill Ownership, Seção 11, pressupõe que a fonte esteja participando ativamente do Combat Scope corrente).

**Ainda pendente, não decidir agora:** o que acontece internamente com o efeito enquanto o Floor não é o atual — se ele é cancelado imediatamente, se fica congelado com a duração restante preservada até o jogador voltar, ou outro comportamento. Isso depende de como o Floor Sleep (mesma seção, acima) trata timers/efeitos, e pertence ao Combat System Document e ao Technical Architecture Document (Seção 51) — o GDD registra apenas: *(1)* o escopo de combate é o Floor atual; *(2)* efeitos antigos não causam dano off-Floor; *(3)* o comportamento temporal exato ao trocar de Floor permanece pendente (ver Seção 53). Esta é uma pendência localizada do Combat System, não uma pendência crítica da máquina de estados.

### Três identidades do Floor ✅
- **Original Floor Identity** — dificuldade, pool de monstros, bosses, drops, estética base. `Original Floor = 5` continua sendo conteúdo do Floor 5 mesmo que sua posição na torre mude.
- **Active Floor Position** — posição atual daquele Floor dentro da torre naquela run; muda através de Remove Tower Layer (Seção 27).
- **Floor Variant** — qual das 5 versões de layout artesanal foi sorteada para aquele Floor naquela run (Seção 25).

Exemplo: `OriginalFloor = 5, ActiveFloorPosition = 3, Variant = B` — conteúdo original do Floor 5; duas camadas inferiores já removidas; atualmente o 3º Floor ativo; usando a Variant B.

---

## 25. Floor Variants

### Estrutura ✅
Cada um dos **10 Floors originais** terá **5 variações de layout feitas manualmente** — total de **50 variações artesanais**. **Não é geração procedural** — o algoritmo apenas escolhe qual mapa artesanal usar.

### Sorteio ✅
Ao começar uma nova run, sorteia-se **exatamente 1 variante para cada Floor**, fixa durante toda a run: trocar de dia não sorteia de novo; morrer não sorteia de novo; remover Floor não sorteia de novo; Continue Game carrega as variantes já sorteadas (Run-Persistent — Seção 15); nova run sorteia de novo.

### Escala técnica ✅
A run usa apenas as 10 variantes selecionadas — as demais 40 não precisam estar simultaneamente simuladas. Implementação (Prefabs ou equivalente) é decisão técnica, não fixada aqui.

### Conteúdo vs. layout ✅
Cada Floor Variant tem suas próprias posições válidas de entrada, saída, escada de subida, buraco/retorno, baús, traps e demais posições de level design — podendo ter layout, paredes e caminhos completamente diferentes de outra variante do mesmo Floor. **O conteúdo de gameplay do Original Floor continua o mesmo** entre variantes: dificuldade, monster pool, bosses, drops e progressão.

### Nota de produção — não é regra de gameplay ✅
O escopo final continua sendo 5 variantes por Floor (50 no total); a arquitetura deve suportar isso desde o início. **Não é necessário produzir as 50 antes de validar o jogo** — estratégia aprovada: primeiro criar apenas a Variant A dos 10 Floors, suficiente para testar run completa, progressão, economia, combate, bosses, employees, Remove Tower Layer, save e floor transitions; depois, produzir progressivamente B, C, D e E. Registrado também no Production Roadmap (Seção 51).

---

## 26. Travessia entre Andares

### Escadas e buracos — regra estrutural final ✅
Escadas e buracos de descida são **parte física fixa do layout de cada Floor Variant** (Seção 25), definidos no level design. Funcionam como **teleportadores entre Active Floor Positions adjacentes**, dentro da mesma Scene única (Seção 24) — nunca carregam outra Scene, nunca são itens, nunca são comprados, nunca são criados/destruídos/movidos/reposicionados pelo jogador.

### Interação — travessia com E, não automática ✅
Diferente de uma versão anterior deste documento, a travessia **não é automática ao encostar** na escada/buraco: o jogador se aproxima, um ícone de interação aparece sobre o objeto (Seção 47 — feedback de "interação disponível"), e pressionar **E** (Seção 46) efetiva a troca de Floor. Mesmo padrão de interação contextual usado por baús (Seção 30) — escadas e baús compartilham o mesmo sistema de interação, cada um com sua própria ação ao ser ativado.
```text
Player se aproxima da escada → ícone de interação aparece → pressiona E → teleporta para o Active Floor de destino (Seção 26, regra de destino abaixo)
```

### Prioridade entre interações simultâneas — pendência aberta 🟡
Com escadas agora usando E (acima), existe um caso ainda não decidido: **o que acontece se mais de um interagível estiver no alcance ao mesmo tempo** (ex.: escada perto de um baú, ou escada perto de onde o Magnet seria largado — Seção 29 já define que "interações prioritárias próximas, ex.: baú, têm prioridade sobre largar o Magnet", mas não menciona escada). A implementação atual (Sprint 6) não tem nenhuma regra de prioridade — ela reage ao último interagível cujo trigger disparou, sem ordem definida. **Precisa ser decidido antes da Sprint 35** (Deadline 9 — primeira sprint em que baús passam a existir de verdade no jogo, Seção 30); até lá, escadas continuam sendo o único interagível ativo no jogo, então o caso não ocorre na prática.

### Exemplo normal de conexão ✅
```text
Térreo
   ↓ escada
Active Floor 1
   ↓ escada
Active Floor 2
   ↓ escada
Active Floor 3
   ...
```
E os retornos:
```text
Active Floor 1 → (buraco/descida) → Térreo
Active Floor 2 → (buraco/descida) → Active Floor 1
Active Floor 3 → (buraco/descida) → Active Floor 2
```
Cada transição teleporta o jogador para outra posição da mesma Unity Scene.

### Regra de destino — baseada em Active Floor Position, não em identidade fixa ✅
As escadas **não** são ligadas rigidamente por `OriginalFloorId + 1` como regra de gameplay. Conceitualmente:
- **Subida** leva ao **próximo Active Floor**.
- **Descida** leva ao **Active Floor anterior**; se não existir Floor anterior (ou seja, a partir do Active Floor Position 1), leva ao **Térreo**.

Essa regra baseada em posição ativa — e não em identidade original fixa — é o que permite Remove Tower Layer funcionar corretamente sem precisar mover fisicamente o conteúdo de nenhum Floor (Seção 27). A implementação concreta pertence ao Technical Architecture Document (Seção 51).

### Disponibilidade, não destruição individual ✅
Escadas e buracos nunca são individualmente criados, destruídos, movidos ou reposicionados pelo jogador. Sua disponibilidade depende de o Floor ao qual pertencem estar ativo; seus destinos de teleporte são recalculados/remapeados conforme a ordem atual de Active Floor Positions — não porque a escada em si "se move", mas porque o alvo dela é sempre relativo, não fixo.

### Controle Remoto 🟡
- **Comprado na aba Bonuses** (Seção 41). **Não é item físico** — não ocupa a Bag, ao contrário de qualquer material de loot (Seção 37).
- Acessado através da tecla **Q** (Seção 46).
- Abrir a interface **pausa completamente o jogo**, seguindo a regra central de pausa (Seção 9).
- Possui **cooldown entre usos**. 🔢 valor exato pendente de balanceamento (referência anterior: 10s).
- **Função:** permite selecionar um Floor válido dentre os disponíveis e se deslocar diretamente para ele, sem precisar percorrer fisicamente as escadas intermediárias.
- **Utiliza Active Floor Position, não Original Floor Identity** — a lista de Floors mostrados/disponíveis é sempre calculada pela posição ativa atual da torre, do mesmo jeito que a Seção 24 define para o restante do Floor System.
- **Remove Tower Layer recalcula naturalmente** os Floors mostrados/disponíveis no Controle — ao remover um Floor, a lista se ajusta à nova torre ativa sem exigir nenhuma lógica adicional (Seção 27).

**Fluxo conceitual:**
```text
Q → jogo pausa → abre interface do Controle Remoto →
mostra Floors atualmente permitidos → player escolhe um Floor →
teleporta → interface fecha → cooldown começa
```
Layout visual da interface não é definido aqui — pertence ao documento de UI/UX (Seção 51).

**Alcance do Controle — duas variantes, decisão de playtest:** a arquitetura suporta duas variantes através de um único bool/configuração, sem uma terceira opção e sem escolha definitiva nesta versão:
- **Variante A:** acesso ao térreo + todos os Active Floors já visitados naquele dia.
- **Variante B:** acesso ao térreo + todos os Active Floors atualmente existentes, independentemente de visita prévia.

A decisão final entre A e B **continua sendo de playtest** (Seção 53) — a arquitetura deve permitir trocar a regra facilmente, sem que isso seja uma pendência bloqueante para o início do desenvolvimento.

---

## 27. Remove Tower Layer

### Regra final confirmada ✅
- Comprado na aba Bonuses. **O jogador não escolhe qual Floor remover** — não existe seleção de alvo.
- **Cada compra remove automaticamente o primeiro Floor da posição ativa atual (Active Floor Position 1).**
- Máximo de **5 compras por run** → no limite, remove sequencialmente **Original Floors 1, 2, 3, 4 e 5**. **Original Floors 6–10 nunca podem ser removidos.**
- NPCs nunca residem em Floors removíveis (vivem no térreo).

### Exemplo completo de remapeamento ✅
Antes:
```text
Térreo → Original Floor 1 → Original Floor 2 → Original Floor 3
```
Remove Tower Layer remove o Active Floor 1 atual (= Original Floor 1). Depois:
```text
Térreo → Original Floor 2 → Original Floor 3
```
- **Escada do Térreo:** antes levava a Original Floor 1; agora leva a Original Floor 2.
- **Buraco/descida do Original Floor 2:** antes levava a Original Floor 1; agora leva ao Térreo (pois Original Floor 2 passou a ser o Active Floor Position 1, sem Floor anterior).
- **Escada de subida do Original Floor 2:** continua levando ao próximo Active Floor (agora Original Floor 3), sem mudança.
- **Original Floor Identity não muda:** mesmo com a escada do térreo agora levando a "Original Floor 2", esse Floor continua sendo Original Floor 2 para todos os efeitos de monster pool, bosses, loot e dificuldade — apenas sua Active Floor Position passou a ser 1.

### Remove Tower Layer na Scene ✅
Como todos os Floors estão na mesma Scene (Seção 24), o Floor removido simplesmente deixa de fazer parte da **lista de Floors ativos**. A implementação técnica não é determinada aqui.

### Consequências sobre outros sistemas ✅
- **Floor Variant:** a variante do Floor removido também deixa de ser acessível; os Floors restantes mantêm suas variantes já sorteadas, sem novo sorteio.
- **Escadas:** remapeadas conforme a nova adjacência ativa (exemplo acima).
- **Magnet/Controle Remoto:** recalculam pela Active Floor Position, não pela identidade original (Seções 26 e 29).
- **UI:** qualquer indicador de "Andar N" reflete a posição ativa, não a identidade original.
- **População:** cada nova posição ativa usa a população própria daquele Original Floor.
- **Conteúdo (baús, loot pool, boss pool):** permanece vinculado à identidade original do Floor.

---

## 28. Traps

- Adicionam risco ao deslocamento, sem virar puzzle. ✅
- Precisam de telegraph antes do dano.
- **Não são monstros:** não dropam loot, não contam como kill, não carregam Ultimate, não contam para demanda.
- **Não participam do Attack Budget** (Seção 14) — ameaça ambiental independente.
- Fazem parte do layout de cada Floor Variant (Seção 25), assim como baús e escadas.

### MVP ✅
**Falling Rock** (sombra no chão → pedra cai, dano em área) e **Floor Spikes** (indicação de furos → espinhos surgem, dano em área). Novas traps ficam em Visão Expandida. 🔭

---

## 29. Quests

Todas Run-Persistent — recompensas se perdem ao iniciar nova run. ✅

### Magnet (3 etapas) — regra completa restaurada ✅
| Etapa | Entrega | Recompensa |
|---|---|---|
| 1 | 5 Arcane Shard | Magnet Tier 1 — acompanha até o 1º andar ativo disponível |
| 2 | 20 Dark Crystal | Magnet Tier 2 — até o 2º andar ativo disponível |
| 3 | 40 Soul Fragment | Magnet Tier 3 — limite aumenta novamente |

- Começa **todo dia** no térreo.
- **E** pega/larga o Magnet. **Interações prioritárias próximas (ex.: baú) têm prioridade sobre largar o Magnet** — só larga com E se não houver interação prioritária no momento.
- Alcance é baseado na **Active Floor Position** (Seção 24), não na identidade original — Remove Tower Layer (Seção 27) pode beneficiar indiretamente seu alcance efetivo.
- **Dentro da área/limite permitido pelo tier atual, o Magnet coleta e vende loot automaticamente** — esta é uma regra estrutural central da recompensa, não implícita.
- **Dois conceitos distintos, não confundir:**
  - **Magnet Floor Range** (os tiers acima) define **até quais Active Floor Positions** o Magnet consegue acompanhar/funcionar — preservado exatamente como já definido, sem alteração.
  - **Pickup Radius** (Seção 37) define **quão perto fisicamente do jogador** o loot precisa estar para ser processado. **O Magnet utiliza o Pickup Radius atual do jogador** como sua área de captura ao redor do jogador — ele não possui um segundo sistema independente de raio horizontal próprio. Consequentemente, comprar **Increase Pickup Radius** (Bonuses — Seção 41) também aumenta naturalmente a área efetiva de atuação do Magnet.
- **Comportamento de venda:** sem Magnet, loot que entra no Pickup Radius vai para a Bag normalmente (Seção 37). **Com o Magnet ativo/acompanhando o jogador**, o loot que entra no Pickup Radius é processado pelo Magnet e **vendido automaticamente** — não precisa entrar na Bag primeiro. Fantasia resultante: com o Magnet, atravessar uma pilha de loot dentro do Floor Range permitido vende tudo que entra no raio, sem gerenciar a Bag.
- **O Magnet só processa loot vendável** — o mesmo conjunto de materiais econômicos que o Pickup Radius já reconhece como "loot válido" (Seção 37). Ele **não** pode vender pergaminhos, outro Magnet, baús, escadas, quest objects não destinados à venda, ou qualquer elemento estrutural do mapa — esses continuam usando suas próprias regras de interação, sem interferência do Magnet.
- **No Modo Padrão, Monster Essence vendida pelo Magnet conta normalmente para a demanda do dia** (Seção 39), e o Gold correspondente é adicionado normalmente, do mesmo jeito que uma venda pelo NPC vendedor ou pelo Coletor Employee — não existe categoria especial de venda para o Magnet. **No Modo Free**, a mesma venda de Monster Essence pelo Magnet ocorre normalmente e o Gold é recebido normalmente, mas não existe quota para incrementar (Seção 42).
- **Vendas do Magnet aparecem normalmente na Tela de Resultados** (Seção 40), que resume todas as vendas do dia independentemente da origem (NPC, Magnet, Coletor) — a estrutura da Tela de Resultados não muda.
- **O Magnet não aumenta slots, stack ou capacidade da Bag** — ele desvia o fluxo do loot capturado para venda automática, evitando o gargalo da Bag enquanto estiver em funcionamento dentro de suas regras.
- 🟡 **Pendência:** o Magnet utiliza o Filtro de Bag do jogador (Seção 37) para decidir quais tipos de loot vender automaticamente, ignora esse filtro, ou terá uma regra própria de filtro? Não definido — não existe um "Magnet Filter" distinto do Filtro de Bag e do Filtro de Coletor já documentados, e não presumir qual dos dois filtros existentes (se algum) ele deveria seguir.

### Chest Pointer ✅
Entregar 100 Spirit Dust → seta visual que aponta para baús próximos, atualizando direção conforme o jogador se move. Não tem vida, não ataca, não coleta, não interage com monstros.

### Chaos Crystal ✅
Entregar 1 Chaos Crystal → **Royal Contract** (+100% valor de venda pelo resto da run). 🟡 Alternativa em avaliação: Epic Card Selection (3 cartas mais fortes que baú comum, escolhe 1) — decisão em playtest.

---

## 30. Baús

### Spawn ✅
Gerados aleatoriamente dentro das posições válidas de cada **Floor Variant** (Seção 25). Quantidade por andar, distância mínima entre baús e eventual aumento de frequência por andar não estão definidos — configuráveis por playtest/balanceamento.

### Chest Mimic — regra completa restaurada ✅
- Uma pequena % (🔢 configurável) de baús em qualquer andar pode ser um **Mimic**.
- **Ao tentar abrir o baú, o Mimic se revela** e passa a atacar o jogador como um monstro comum de combate.
- Fica **mais forte em Floors superiores**, escalando junto com a dificuldade do andar.
- **Ao morrer, o Mimic libera a mesma recompensa que o baú normal teria fornecido e abre a mesma UI de 3 opções** (Seção 30) — o jogador **não perde a recompensa** apenas por ter encontrado um Mimic; ele só precisa vencer o combate primeiro para recebê-la.

### Interação — abertura com E, escolha com mouse ✅
O jogador se aproxima do baú e pressiona **E** para abri-lo. Isso vale tanto para baú normal quanto para Mimic:
```text
Player se aproxima do baú → pressiona E → baú é aberto
```
- **Baú normal:** ao abrir com E, a recompensa é liberada e a UI de 3 pergaminhos/cartas aparece diretamente — **não existe uma segunda interação de "pegar o pergaminho do chão"**; o pergaminho não é um objeto separado que exige outra tecla ou é coletado pelo Pickup Radius (Seção 37).
- **Mimic:** ao pressionar E, o Mimic se revela e passa a atacar o jogador; ao morrer, libera a mesma recompensa que o baú normal teria dado, abrindo a mesma UI de 3 opções.
- **Escolha da recompensa:** a UI de 3 pergaminhos/cartas **pausa o jogo** (Seção 9) e o jogador escolhe **1 das 3 opções clicando com o mouse** — não com E, Enter, ou qualquer tecla do teclado. Após a escolha, o buff é aplicado, a UI fecha, e o gameplay continua.

```text
Baú → E → recompensa liberada → UI de 3 opções (jogo pausado) →
clique do mouse em 1 opção → buff Run-Persistent aplicado → UI fecha → gameplay continua
```

Preservado sem alteração: 3 opções sem duplicar tipo no mesmo sorteio, escolha de exatamente 1, 1 reroll gratuito por dia, rerolls extras compráveis, buffs Run-Persistent (Seção 31).

---

## 31. Cartas (Pergaminho)

### Regras de sorteio ✅
3 cartas aleatórias por pergaminho, nunca repetindo tipo entre si no mesmo sorteio. Escolhe exatamente 1. Bônus aumentam por nível do andar. 🔢 curva exata. Buffs escolhidos são **Run-Persistent** (Seção 15). Buffs repetidos entre pergaminhos diferentes acumulam. **1 reroll gratuito por dia de run**, confirmado. Rerolls adicionais compráveis na aba Bonuses. 🔢 preço. Cartas com teto (ex.: 5 flechas do Ranger) param de aparecer ao atingir o limite.

### Pools ✅
**Universal:** Velocidade de Ataque, Dano de Ataque, Velocidade de Movimento, Vida — em %. **Específica por herói:** listadas em cada ficha da Seção 17. **De Employee:** só entra no sorteio se houver employees possuídos naquele dia.

---

## 32. Attack Speed — Escalonamento Multi-Fonte

Attack Speed não é uma única porcentagem aplicada uniformemente. Cada família de fonte (ataque primário, passivas periódicas, cooldown de summon, hitboxes orbitais) tem seu próprio coeficiente/curva de conversão, para impedir que uma passiva chegue a 100% de uptime só por acúmulo de Attack Speed pensado no ataque primário. 🔢 curvas e tetos por família ficam no documento de balanceamento.

---

## 33. Pets, Summons e Fontes Automáticas

**Pet ≠ Employee.** ✅

### Pets permanentes de kit (Mage, Blood Mage, Demonologist) ✅
Não alvejados por monstros. Sumonados no início de cada dia com animação (~2s referência) e bloqueio de movimento. Consomem uma fatia do orçamento ofensivo do herói (Seção 11). Ao morrer o herói, o pet retorna junto na transição (Seção 11); 🟡 reanimação exata da animação de summon ainda não fechada.

### Summons temporários alvejáveis (Necromancer) ✅
Alvejáveis por monstros, tempo de vida próprio, podem stackar. **São destruídos ao morrer o herói — regra padrão, sem exceção, mesmo para o Necromancer** (Seção 18).

### Efeitos persistentes/periódicos vinculados ao herói (Paladin — shield; Plague Doctor — orbs orbitais) ✅
Gira/aparece ao redor do herói periodicamente, objeto filho do GameObject principal. Shields têm vida própria distinta da vida do herói.

### Defesa — três mecanismos distintos, sem generalização ✅
- **Cura periódica** (Cleric): recupera vida ao longo do tempo; pode escalar com Vida Máxima.
- **Shields** (Paladin): absorvem dano com vida própria; escalam com Vida Máxima final.
- **Lifesteal** (Blood Mage): nasce do dano causado, não é cura independente — `Cura = percentual do dano causado`, com teto de cura por hit baseado em % da Vida Máxima (referência: 10% do dano como cura, teto de 3% da Vida Máxima por hit — valores no documento de balanceamento). Vida Máxima entra aqui só como **limite superior**, não como base do cálculo.

Essas três mecânicas não constituem sistema defensivo universal — são recursos próprios de heróis específicos (Seção 11).

---

## 34. Employees — Sistema Completo

### Estrutura geral ✅
Ajudante (combate) e Coletor. Nenhum é alvejado. Spawnam no pé do jogador ao entrar em um andar diferente do térreo.

### Compra — fluxo completo ✅
Na aba Employees, lado esquerdo: o jogador seleciona o tipo/tier que deseja comprar. Ao clicar, abre um popup de compra contendo **scroll/slider de quantidade**, **input numérico manual**, e **confirmação da compra**.
- **Limite pelo dinheiro:** o scroll não pode passar da quantidade máxima que o jogador consegue pagar (ex.: preço unitário 100g, gold 850g → máximo comprável = 8, scroll vai até 8).
- **Input manual:** se o jogador digitar um valor acima do que pode pagar (ex.: 999 quando só pode comprar 8), o valor é **clampado para o máximo possível** (8) — nunca rejeita a compra inteira, nunca permite valor acima do possível.

### Promoção — fluxo completo ✅
Árvore: Intern → Junior → Mid-level → Senior → (Strong ou Fast). Cada promoção exige **dinheiro + 1 employee do tier imediatamente anterior**, que é **consumido** no processo (ex.: promover 10 Junior exige 10 Intern + o gold necessário; ao confirmar, -10 Intern, +10 Junior).

**Promoção em lote — duplo limite:** o scroll/input de promoção é limitado por **duas condições simultâneas**: *(1)* gold disponível; *(2)* quantidade disponível do employee do tier anterior. Exemplo: gold permitiria promover 50, mas existem só 12 Intern → máximo = 12. Ou: existem 100 Intern, mas gold só permite 7 promoções → máximo = 7. Um valor digitado acima do máximo possível é **clampado** para o maior valor permitido pelas duas condições.

### Venda — fluxo completo ✅
No lado direito da aba Employees: employees possuídos são exibidos por imagem, tier/tipo e quantidade possuída. Ao clicar em um employee possuído, abre um painel de venda com **scroll/slider de quantidade**, **input numérico manual**, **botão de confirmar** e **botão X/fechar**.
- Máximo vendável = quantidade possuída; valor digitado acima disso é clampado.
- **Fechar no X não realiza venda.** Só **Confirmar** vende a quantidade selecionada.

### Ajudante (combate) ✅
Dano, atk speed, velocidade de movimento, delay. Intern→Senior melhora tudo gradualmente. **Fast:** dano/atk speed = Senior; velocidade muito maior; delay quase inexistente. **Strong:** velocidade/delay = Senior; dano/atk speed muito maiores.

### Coletor ✅
Vai até itens de qualquer Floor ativo com loot disponível (Seção 24/36), coleta instantaneamente ao entrar no raio. Ao atingir capacidade/tempo definido, some por um tempo, vende automaticamente, depois retorna. Intern→Senior melhora capacidade/velocidade e reduz tempo de ausência. **Fast:** capacidade ≈ Senior; velocidade muito maior; ausência extremamente reduzida — ciclos rápidos. **Strong:** velocidade ≈ Senior; capacidade muito maior; ausência também melhora em relação ao Senior — grandes quantidades por ciclo.

### Filtro ✅
Comprado em Bonuses, impede o Coletor de pegar tipos de item específicos.

---

## 35. Virtualização de Employees

Contagem lógica (quanto o jogador possui) é diferente de quantidade fisicamente simulada em cena. O jogo pode representar posse de milhões simulando um número muito menor de unidades mais fortes/eficientes, mantendo a leitura visual de "está muito cheio" sem travar performance. Mesma filosofia vale para loot no chão (Seção 38) e para a existência de Floors fora da região ativa (Seção 24). Parâmetros exatos são decisão técnica/de balanceamento.

---

## 36. Coleta Distribuída entre Floors

Resultado esperado, sem determinar implementação: **Coletores podem localizar/coletar loot de Floors ativos mesmo fora do Floor atual do jogador, sem exigir que todos os Employees sejam simulados individualmente em tempo real.** A forma exata pertence ao Technical Architecture Document (Seção 51).

---

## 37. Inventário (Bag)

### Regras confirmadas ✅
Aberto com **TAB** (pausa — Seção 9). Contém exclusivamente loot — nenhum item utilizável, arma física ou consumível. Início: **5 slots**, stack **16**.

### Progressão de slots — matemática corrigida ✅
```text
5 (inicial)
↓ compra 1
10
↓ compra 2
15
↓ compra 3
20 (máximo)
```
**3 compras** levam de 5 a 20 slots (+5 por compra) — não 4. 🔢 preços de cada compra.

Progressão de stack: 16→32→128→1.024→8.192→131.072→1.048.576 (valores totais). 🔢 preços.

**Descartar:** clique direito. **Reorganizar:** arrastar com clique esquerdo. Compras na loja nunca competem por espaço no inventário.

### Interações de drag/drop ainda não definidas 🟡
Os comportamentos exatos de merge/swap ao arrastar itens não foram fechados pelo designer — não devem ser presumidos. Ver a lista completa na Seção 53 (Pendências Abertas): comportamento ao arrastar uma stack sobre outra stack do mesmo item; merge parcial quando o destino não comporta a stack inteira; comportamento ao arrastar para um slot ocupado por item diferente; quantidade efetivamente removida por um clique direito (stack inteira, 1 unidade, ou seleção de quantidade).

### Coleta parcial ✅
Pilha maior que o espaço disponível: coleta o máximo que couber, resto fica no chão como entidade separada. Nunca tudo-ou-nada.

### Pickup Radius — coleta automática por proximidade ✅
O jogador não precisa encostar exatamente no sprite do loot. Ele possui uma **área/raio de coleta (Pickup Radius)**: quando um **loot válido** entra nessa área, o sistema tenta coletá-lo automaticamente — não exige apertar E, clicar no item, ou encostar pixel a pixel.

```text
Loot entra no Pickup Radius → Player tenta coletar
```

- **O que é "loot válido":** o Pickup Radius atua **somente sobre materiais coletáveis/vendáveis destinados à Bag ou à venda** — os 15 materiais econômicos (Monster Essence, Monster Fragment, Spirit Dust, Arcane Shard, Dark Crystal, e demais da lista — Seção 39). **Interactables especiais não são loot do Pickup Radius** e continuam usando sua própria regra de interação existente, nunca sendo coletados automaticamente só por entrar no raio: baús, o próprio Magnet, escadas, buracos/descidas, NPCs, interações de quest, o Controle Remoto, e qualquer outro interactable especial futuro. **Pergaminhos/recompensas de baú não são materiais econômicos válidos para o Pickup Radius e não são aspirados automaticamente** — a interação de abertura ocorre no baú através de E (Seção 30), e a escolha entre as três opções de recompensa é feita com o mouse na UI, não pelo Pickup Radius. O Pickup Radius não substitui a tecla **E** como sistema de interação contextual — ele é exclusivamente um sistema automático de coleta de loot econômico.
- **Se houver espaço na Bag:** o loot é coletado automaticamente, seguindo as regras já existentes de slots, stacks e filtro de bag.
- **Coleta parcial continua valendo:** se a pilha exceder o espaço disponível, entra o máximo que couber e o restante permanece no chão como entidade separada (mesma regra da subseção acima) — o Pickup Radius não ignora os limites da Bag.
- **Filtro de bag continua valendo:** um tipo de loot bloqueado pelo filtro não é coletado automaticamente mesmo entrando no raio.
- **Valor base:** o jogador possui um Pickup Radius base. 🔢 valor numérico pendente de balanceamento — não fixado em unidades, tiles, metros ou pixels aqui.
- **Upgrade "Increase Pickup Radius"** (aba Bonuses — Seção 41): aumenta o raio do jogador. **Run-Persistent** — persiste entre dias da run, reseta para o raio base em nova run (mesma categoria de qualquer bonus comprado, Seção 15). 🔢 quantidade de compras, curva de aumento e preços pendentes — não presumir tiers, percentuais ou incrementos específicos.
- **Aggregação de drops não muda:** pilhas seguem a mesma lógica de representação visual (1–9 individual, 10–49/50–99 stacks, 100+ quantidade interna — Seção 38); ao entrar no Pickup Radius, a lógica de coleta/venda se aplica sobre a quantidade interna real, não exige que cada unidade visual entre individualmente na área.
- **Feedback visual do raio é decisão de UI/UX** — pode ser invisível normalmente, mostrado ao comprar o upgrade, mostrado em debug, ou outra solução; não definido aqui.
- **Não é o raio do Coletor Employee.** O Pickup Radius é um sistema do jogador; o Coletor continua com sua própria lógica, capacidade, movimento e ciclo (Seção 34), incluindo a coleta Cross-Floor (Seção 36) — os dois sistemas não se conectam.

### Filtro de bag ✅
Toggles por tipo de item.

### Fim de dia ✅
Todo loot ainda no inventário é destruído (Daily — Seção 15).

---

## 38. Drops no Chão — Agregação Visual e Regras de Loot

### Agregação visual — feedback de poder, não só otimização ✅
1–9: individual. 10–49: stacks de 10. 50–99: stacks de 50. 100+: sprite(s) com quantidade real interna, podendo se distribuir em múltiplas pilhas visuais para reforçar a sensação de fartura. Coleta parcial de pilhas grandes segue a mesma lógica da Seção 37.

### Rolagens independentes — regra restaurada ✅
**Cada tipo de loot realiza sua própria rolagem de chance de drop, independentemente das demais.** Um único abate pode dropar múltiplos materiais diferentes simultaneamente se as rolagens correspondentes forem bem-sucedidas — por exemplo, um mesmo monstro pode dropar Monster Essence **+** Monster Fragment **+** Spirit Dust **+** Arcane Shard no mesmo abate. **Não se trata de "escolher apenas um item da tabela"** — é um conjunto de rolagens independentes, uma por material possível daquele monstro.

### Materiais de Floors anteriores continuam no pool ✅
Desbloquear os materiais associados a um Floor superior **não remove** os materiais dos Floors anteriores da tabela de drop. Floors superiores continuam podendo dropar materiais "antigos" normalmente, conforme a tabela de drop de cada monstro (documento de balanceamento — Seção 51).

---

## 39. Economia e Materiais

### Materiais (15 tipos) ✅
Monster Essence, Monster Fragment, Spirit Dust, Arcane Shard, Dark Crystal, Soul Fragment, Corrupted Core, Elemental Shard, Ancient Fragment, Infernal Ash, Chaos Crystal, Nightmare Residue, Void Shard, Celestial Fragment, Divine Core — cada um com valor de venda próprio. Notação de UI: k/m/b/t.

- Cada item faz sua própria rolagem de chance (regra completa na Seção 38).
- Valores são valor-base por abate, antes de bônus de run/employees.

### Demanda diária — Modo Padrão ✅
Só Monster Essence conta. `Demanda do Dia = 40 × 2^(Dia - 1)`. Contabilizada pela venda acumulada no dia. Esta subseção não se aplica ao Modo Free; nesse modo, Monster Essence continua sendo loot econômico vendável normalmente, apenas sem função de quota obrigatória (Seção 42).

### Venda ✅
NPC vendedor no térreo, durante o gameplay do dia (relógio correndo).

---

## 40. Ciclo de Dia

### Duração ✅
Inicial: 100s. Upgrade "Add Time": +100s por compra, até 2 compras (teto 300s). 🔢 preços.

### Encerramento — compartilhado, com validação de demanda apenas no Padrão ✅
Tempo zera **ou** jogador usa a porta — igual nos dois modos.

**Modo Padrão:** valida a demanda. **Não cumprida → GAME OVER** (texto de tom bem-humorado; run encerrada, mas **o save não é apagado** — Seção 43). **Cumprida →** segue para a Tela de Resultados.

**Modo Free:** não valida demanda (Seção 42) — segue diretamente para a Tela de Resultados.

### Tela de Resultados — igual nos dois modos ✅
- Revela **apenas os itens vendidos naquele dia** — nenhum item não obtido aparece.
- Para cada item: **imagem, nome, quantidade vendida, valor unitário, e total daquele item.**
- Os itens aparecem **um por um**, em sequência animada.
- Pressionar **Enter** revela todos imediatamente, pulando a animação.
- Ao final: `TOTAL DO DIA: Xg`.
- Botão **Continuar** → Loja.
- **A Tela de Resultados não vende nada** — ela apenas resume vendas que já aconteceram durante o dia (pelo vendedor do térreo, Seção 39, ou pelo Coletor/Magnet). Loot que ainda estiver no inventário do jogador ao final do dia é **destruído**, não vendido (Seção 37).

### Loja entre dias ✅
3 abas: Upgrades / Bonuses / Employees. Botão **Start Day N**. **Save automático ao entrar na loja** (Seção 43). Idêntica nos dois modos.

---

## 41. Bonuses (aba da loja)

Controle Remoto (Seção 26), Filtro de bag (Seção 37), Filtro de Employee coletor (Seção 34), **Add Time** (+100s por compra, até 2 compras), **Increase Inventory Slots** (+5 por compra, **3 compras** até o máximo de 20 — Seção 37), **Increase Inventory Stack Size** (16→32→128→1.024→8.192→131.072→1.048.576), **Increase Pickup Radius** (aumenta o Pickup Radius do jogador — Seção 37; também beneficia a área efetiva do Magnet, Seção 29; quantidade de compras, curva e preços 🔢), **Remove Tower Layer** (Seção 27), **Reroll de pergaminho** (Seção 31). 🔢 todos os preços.

---

## 42. Modos de Jogo

### Modo Padrão ✅
Demanda diária obrigatória de Monster Essence (Seção 8, Seção 39). Único modo que desbloqueia heróis e conta Account Progression (Seção 15). No fim do dia, valida a demanda: não cumprida → Game Over; cumprida → Resultados → Loja → Save.

### Modo Free — regra final ✅
**O Modo Free é o mesmo jogo do Modo Padrão, apenas sem a demanda obrigatória de Monster Essence.** Não é uma máquina de estados própria, não é sandbox infinito, e nenhum dos sistemas abaixo é removido:

- **Dias, timer, Loja, Resultados, Save, Floors, progressão de arma, Bonuses, Employees, Quests, baús, cards e bosses funcionam normalmente**, exatamente como no Modo Padrão.
- **Timer:** o mesmo do Padrão — começa em 100s, "Add Time" funciona normalmente, pode chegar a 300s pelos upgrades já definidos (Seção 40). Não existe timer infinito.
- **Porta de saída:** funciona normalmente — usá-la encerra o dia voluntariamente.
- **Fim de dia:** tempo acaba ou jogador usa a porta → **não existe validação de demanda** (porque não há demanda no Free) → segue direto para a Tela de Resultados → Loja → Save → Start Day N+1. **O Free não tem Game Over por falha de demanda**, já que não existe demanda a falhar.
- **Morte continua existindo normalmente:** -30s de penalidade, perda de loot, respawn no térreo — a única coisa removida é a possibilidade de falhar o dia por não vender Essência suficiente.
- **Resultados:** exatamente a mesma tela do Padrão (Seção 40) — item por item vendido, imagem, nome, quantidade, valor unitário, total, `TOTAL DO DIA: Xg`, Enter revela tudo, botão Continuar → Loja. Monster Essence vendida no Free continua aparecendo normalmente como item vendido, mesmo sem função de quota.
- **Loja:** as mesmas 3 abas (Upgrades, Bonuses, Employees), mesmos sistemas, mesmo botão "Start Day N".
- **Dia 15:** o marco é atingido pela **conclusão normal do dia** (tempo esgotado ou porta usada, sem demanda a cumprir) em vez de pela demanda cumprida do Padrão — mas apresenta a mesma Tela de Vitória e as mesmas escolhas Menu/Continuar, seguindo a mesma estrutura de fluxo e de save da Seção 43.
- **Dia 30:** mesma lógica — concluído normalmente (sem demanda), apresenta a mesma tela de encerramento definitivo do Padrão (Seção 44).
- **Progressão dentro da run funciona normalmente:** upgrades de arma evoluem, Employees funcionam, cards são sorteadas e aplicadas, Quests progridem — tudo isso opera normalmente **dentro daquela run Free**, do mesmo jeito que no Padrão.

**O que o Free não concede** (Account Progression — Seção 15), preservado sem alteração:
- **Não desbloqueia heróis.**
- **Não libera recompensas/achievements de progressão** — inclusive a conquista Steam do Dia 30 (Seção 44) não é concedida numa run Free.
- **Não contabiliza progressão de campanha** — não pode ser usado para cumprir critérios de unlock (Seção 49).

**Lifetime Statistics continuam sendo registradas normalmente no Free** (Seção 15/49) — ex.: kills, gold vendido, dias jogados podem alimentar contadores informativos do perfil mesmo numa run Free. Isso é puramente informativo: **registrar a estatística nunca aciona avaliação de progressão/unlock**. Isso evita que o perfil do jogador fique artificialmente incompleto sem abrir uma forma de "farmar" desbloqueios pelo Free.

Ou seja: os sistemas Run-Persistent (arma, Employees, cards, quests) funcionam normalmente dentro de uma run Free; apenas o que seria Account Progression nunca é gerado por ela.

### Modos futuros 🔭
"Only Monster Essence" — ideia mencionada, sem regras fechadas.

---

## 43. Save — Semântica Completa e Definitiva

O save deste jogo é simples: **terminou o dia → entrou na Loja → salva. Se já existia um save, o novo sobrescreve o anterior — sem exceções entre modos.**

### Regra Geral — 1 slot ✅
Existe **um único slot de save de run**, compartilhado entre o Modo Padrão e o Modo Free. Não existe save separado por modo, slot protegido, prioridade entre saves, ou backup — apenas o último checkpoint salvo, seja qual for o modo ao qual ele pertence.

### O save é um checkpoint, não um registro de resultado de run ✅
> **O save representa o checkpoint da última Loja alcançada — nunca é apagado pelo fim de uma run**, seja esse fim um Game Over, uma vitória no Dia 15, ou o encerramento definitivo do Dia 30. "A run termina" significa que a sessão/tentativa atual se encerra e o jogador retorna ao Menu Principal — **isso nunca significa deletar o checkpoint da última Loja.** Esses dois conceitos (run terminar / save ser apagado) são estruturalmente diferentes.

### Autosave ✅
Save ocorre **somente** ao entrar na fase de Loja, imediatamente após o encerramento normal de um dia — nunca durante o dia. Isso vale igualmente para os dois modos: no Padrão, "encerramento normal" pressupõe ter cumprido a demanda; no Free, não há demanda, então qualquer fim de dia normal (tempo zerado ou porta usada) leva a esse mesmo fluxo.
```text
Fim do dia (Padrão: demanda cumprida / Free: sempre) → Resultados → Loja → SAVE AUTOMÁTICO
```

### Overwrite — sempre sobrescreve, sem exceção entre modos ✅
Todo novo autosave **sobrescreve** o save existente, não importa a que modo pertencia o save anterior ou o novo. Exemplos:
```text
Save atual = Standard, Dia 10
↓ Player inicia uma run Free, conclui o Dia 1, chega à Loja → SAVE
↓
O save do Free sobrescreve o save Standard anterior.
```
```text
Save atual = Free, Dia 8
↓ Player inicia uma run Standard, conclui o Dia 1 com demanda cumprida, chega à Loja → SAVE
↓
O save Standard sobrescreve o save Free anterior.
```
Isso é intencional — o save não é uma mecânica estratégica do jogo, é apenas um checkpoint automático entre dias. Não existe confirmação obrigatória de overwrite no nível de sistema (uma eventual confirmação de UX ao clicar New Game é decisão de interface, não altera esta regra — Seção 52).

### Game Over não gera novo save ✅
```text
Jogador terminou o Dia 8 → Resultados → Loja → SAVE → Start Day 9
   ↓
Falha na demanda do Dia 9 (Padrão) → GAME OVER → Menu Principal
```
Não houve entrada em uma nova Loja, então **não houve novo autosave** — o save simplesmente **não foi sobrescrito**, continuando a ser a Loja que precede o Dia 9. Continue Game volta a essa Loja, com "Start Day 9" disponível. Isso não é permadeath. O Free não possui esse cenário — sem demanda, não há falha de dia por essa causa (Seção 42); morte continua existindo normalmente (Seção 11), mas não gera Game Over por conta própria.

### Dia 15 — Menu vs. Continuar (Padrão e Free, mesma regra) ✅
```text
Dia 14 concluído → Resultados → Loja → SAVE → Start Day 15
   ↓
Demanda cumprida (Padrão) / dia concluído (Free) → Tela de Vitória
   ↓
Escolhe Menu Principal
```
Essa escolha **não** passa por Resultados → Loja → novo save, então o checkpoint continua sendo **a Loja que precede o Dia 15** — o jogador pode jogar o Dia 15 de novo via Continue Game. Se escolher **Continuar**:
```text
Tela de Vitória → Continuar → Resultados do Dia 15 → Loja → AUTOSAVE (novo checkpoint: Loja pré-Dia 16)
```

### Dia 30 ✅
Ao concluir o Dia 30, a run encerra obrigatoriamente (Seção 44), mas **o save não é apagado** — continua sendo a última Loja em que um autosave ocorreu. Não há autosave adicional após a tela de encerramento definitivo.

### New Game não sobrescreve antes do primeiro autosave da nova run ✅
```text
Save existente: Loja que precede o Dia 12
   ↓
New Game (qualquer modo) → nova run começa no Dia 1
   ↓
Game Over (Padrão) ou fechamento antes da primeira Loja
```
O save antigo continua intacto — Continue Game ainda carrega essa run anterior. Só quando a nova run **conclui seu primeiro dia e chega à Loja com autosave** é que o save anterior é sobrescrito, seguindo exatamente a regra de Overwrite acima.

### Semântica do dia salvo ✅
O save precisa distinguir inequivocamente **qual dia acabou** e **qual é o próximo dia a iniciar** (conceitualmente `LastCompletedDay`/`NextDay`, ou estrutura equivalente — nomes não obrigatórios), evitando risco de repetir, pular, ou mostrar o número errado no botão da Loja.

### Continue Game — não pergunta o modo ✅
```text
Existe save? → SIM → carrega o RunState salvo (que já contém o Mode) →
abre a Loja do checkpoint → Start Day N
```
O `RunState` salvo já contém o **Mode** da run (além de herói, mapa, dia, gold, weapon tier, bonuses, employees, quests, cards, Floor Variants sorteados, Remove Tower Layers aplicados). Continue Game **lê o Mode salvo** e carrega a experiência correspondente automaticamente — nunca pergunta modo, herói ou mapa de novo, seja o save de uma run Padrão ou Free. Esses valores são **carregados**, nunca escolhidos de novo.

---

## 44. Vitória e Pós-Game

- **Dia 15:** marco atingido por **completar a demanda** (Modo Padrão) ou por **concluir normalmente o dia** (Modo Free, sem demanda — Seção 42). Ambos apresentam a mesma Tela de Vitória → jogador escolhe Menu Principal (save permanece na Loja pré-Dia 15, Seção 43) ou Continuar (novo save na Loja pré-Dia 16, Seção 43).
- **Dias 16–30 (pós-game):** mesmo core loop. **No Padrão, as demandas continuam crescendo; no Free não existem demandas** (Seção 42) — o restante do loop (Loja, Employees, upgrades, exploração) continua igual nos dois modos. Matar um boss de topo (ex.: Divine God) não encerra a run — é conquista, não condição de vitória.
- **Dia 30:** limite real da run em ambos os modos. Atingido por **completar a demanda** (Padrão) ou **concluir normalmente o dia** (Free). Em ambos os casos, a mesma tela de encerramento definitivo é exibida e a run termina, com o save permanecendo no último checkpoint (Seção 43). **A conquista Steam de conclusão do Dia 30 é Account Progression (Seção 15) e só é concedida no Modo Padrão** — o Free mostra o mesmo encerramento, mas não concede o achievement nem qualquer outra recompensa de progressão.

---

## 45. UI / HUD

### Sempre visível nos dois modos ✅
Tempo restante, bag (slots/stacks).

### Modo Padrão apenas ✅
Demanda do dia (`Monster Essence: X/Y` → `DEMANDA CUMPRIDA`, Seção 39). **O Modo Free não exibe esse indicador** — sem demanda, não há progresso de quota para mostrar; Monster Essence continua existindo normalmente como loot vendável.

### Necessário por decorrência dos sistemas definidos, sem layout fechado 🟡
Vida/Vida Máxima, Energia/carga da Ultimate, cooldown do ataque primário, cooldown do Controle Remoto, indicador de Employees ativos, indicador de pets/summons ativos, buffs de carta ativos, indicador de Continue Game disponível/indisponível (Seção 52), Floor atual (Active Floor Position), Mapa selecionado na criação da run.

### Outras telas ✅
Tela de Resultados (Seção 40), tela de Loja, tela de Vitória (Dia 15), tela de Encerramento Definitivo (Dia 30), tela de Game Over — todas com texto próprio.

---

## 46. Controles

| Entrada | Função |
|---|---|
| WASD | Movimentação |
| Mouse | Mira / direção |
| LMB | Ataque primário |
| RMB | Ultimate |
| E | Interação contextual (ex.: abrir baús — Seção 30; subir/descer escadas — Seção 26) |
| TAB | Inventário (pausa) |
| Q | Controle Remoto (pausa) |
| Enter | Pula animação da tela de resultados |

🟡 Tecla de pause/menu geral não confirmada.

A escolha entre as 3 opções de recompensa de baú/pergaminho (Seção 30–31) é feita com **clique do mouse** na UI pausada, não por nenhuma tecla desta tabela.

---

## 47. Feedback Visual e Sonoro — Necessidades Funcionais

Dano causado, crítico, Ultimate pronta, Ultimate sem energia suficiente, dano recebido, morte, coleta, venda, **demanda cumprida (Modo Padrão apenas — Seção 42)**, aparição de boss, Mimic revelado, baú abrindo, upgrade de arma comprado, promoção de employee, inventário cheio, item descartado, dinheiro aumentando, grandes quantidades, Controle em cooldown, interação disponível, telegraph de trap, Floor atual (troca de Global Light — Seção 24), Continue Game indisponível quando não há save (Seção 52), mapa selecionado na criação da run. 🟡 Estética exata não definida — só a necessidade funcional.

---

## 48. Números Grandes

Suporte a milhões/bilhões/trilhões e além, sem teto fixo. Notação k/m/b/t. Tipo de dado é decisão técnica. Agregação de drops (Seção 38) e virtualização de employees (Seção 35) preservam lógica interna exata.

---

## 49. Progress Tracking / Achievement Hooks

Quatro escopos formais: três de contagem (Daily/Run/Lifetime, por necessidade de gameplay/estatística) mais a distinção sobre o que cada um pode acionar. ✅

### Daily Counters
Resetam no começo de outro dia. Exemplos: monstros mortos hoje, monstro específico morto hoje, Essência vendida hoje, gold ganho hoje, baús abertos hoje. Necessários para desbloqueios como Mage e Blood Mage (Seção 17) — no Modo Padrão.

### Run Counters
Resetam em nova run. Exemplos: dias concluídos nesta run, maior Floor desta run, bosses mortos nesta run, total de monstros nesta run, quantidade máxima de Employees nesta run.

### Lifetime Statistics (Permanent Account State — Seção 15)
Nunca resetam. Exemplos: total histórico de kills, total de runs vencidas, runs vencidas por herói, maior Floor histórico, bosses específicos mortos historicamente, gold vendido, dias jogados. Pode existir naturalmente um contador de dias jogados/completados para fins estatísticos, mas isso **não** deve ser presumido como o contador usado para o desbloqueio do Rogue — o escopo exato desse critério (run única vs. acumulado) ainda está pendente (Seção 17.5, Seção 53). **Lifetime Statistics são puramente informativas e existem independentemente do modo** — inclusive Daily/Run Counters gerados numa run Free podem alimentar Lifetime Statistics normalmente (Seção 42).

### Account Progression × Mode — regra de avaliação ✅
Daily e Run Counters podem existir e ser registrados normalmente em **qualquer** run, de qualquer modo, por necessidade de gameplay/estatística. Porém a **avaliação de critérios de desbloqueio/achievement** (o que transforma um contador em uma recompensa de Account Progression — Seção 15) depende do modo:
```text
Mode = Padrão → Progression/Unlock Evaluator pode usar os counters normalmente
Mode = Free   → counters continuam sendo registrados → Progression/Unlock Evaluator não concede nada
```
Ou seja: **Statistics Recording** permanece ativo em qualquer modo; **Progression Evaluation** é desativado no Free. Isso vale para todos os critérios de desbloqueio de herói (Mage, Blood Mage, Rogue, Assassin, e qualquer herói futuro) e para achievements de progressão — nenhum deles pode ser concedido por eventos ocorridos numa run Free.

Esse sistema precisa existir desde o início da arquitetura — critérios de desbloqueio dependem diretamente dele.

---

## 50. Arquitetura Técnica de Alto Nível

Responsabilidades conceituais (nomes ilustrativos):

- **Hero Definition** — Dano Base, Vida Base, referências de habilidade, condição de desbloqueio.
- **Ability Definition** — timing (Seção 12), tipo de hitbox/projétil, coeficiente de dano.
- **Projectile Definition** — categoria (Seção 13).
- **Enemy Definition** — categoria, atributos, timing de ataque (Seção 22), drops, andar(es).
- **Boss Definition** — múltiplos ataques, Boss Timer por Floor (Seção 22).
- **Floor Definition** — Original Floor Identity, independente da Active Floor Position (Seção 24).
- **Floor Variant Definition** — as 5 variantes artesanais por Floor (Seção 25), com posições de entrada/saída/escada/baú/trap.
- **Loot Definition** — 15 materiais, valor de venda, regras de drop independentes (Seção 38).
- **Employee Definition** — tier, atributos, custo de promoção.
- **Card Definition** — pool, efeito, teto quando aplicável.
- **Quest Definition** — etapas, requisitos, recompensa.
- **Run State** — modo, herói, mapa, dia do checkpoint, gold, weapon tier, bonuses, employees, quests, cards, Floor Variants sorteados, Remove Tower Layers aplicados.
- **Day State** — tudo Daily (Seção 15), incluindo Boss Timer por Floor e Energia da Ultimate.
- **Permanent Account State** — dividido em *(1)* Account Progression: heróis desbloqueados, achievements, recompensas/critérios permanentes de unlock; *(2)* Lifetime Statistics: contadores informativos de perfil (Seção 15/49).
- **Save Manager** — mantém um único slot de save de run; realiza autosave ao entrar na Loja após o encerramento normal de um dia; cada novo save sobrescreve o anterior, independentemente do modo; Continue carrega o último RunState salvo, que já contém o Mode (Seção 43).
- **Pause Manager** — sistema central de pausa (Seção 9).
- **Population Manager** — Minimum/Target/Maximum por Floor (Seção 23).
- **Attack Budget Manager** — slots de ataque, Melee/Ranged separados (Seção 14).
- **Loot Aggregation** — agregação visual, rolagens independentes (Seção 38).
- **Employee Virtualization** — contagem lógica vs. simulada (Seção 35).
- **Floor Sleep/Activation Manager** — suspensão/virtualização de Floors fora da região ativa (Seção 24).
- **Cross-Floor Loot Access** — Coletores acessando loot de Floors não-ativos (Seção 36).
- **Stair/Hole Routing** — cálculo de destino de teleporte por Active Floor Position, não por identidade fixa (Seção 26).
- **Progress Tracker** — Daily/Run Counters e Lifetime Statistics (Seção 49); precisa saber qual Mode gerou o evento, para que o Progression/Unlock Evaluator só conceda Account Progression quando `Mode = Padrão` (Seção 42).
- **Large Number Abstraction** — suporte a valores grandes (Seção 48).
- **Shop Manager** — 3 abas, popups de compra/promoção/venda.
- **Chest System** — spawn em posições válidas da Floor Variant ativa, chance de Mimic, interação com E e abertura da UI de seleção de 3 cartas/recompensas; o Mimic adia essa UI até ser derrotado. Não exige um objeto de pergaminho físico coletável como requisito técnico (Seção 30).
- **Remote Controller System** — abertura por Q, pausa (Seção 9), lista de Floors válidos por Active Floor Position, teleporte, cooldown, toggle de alcance A/B (Seção 26).
- **Pickup System** — detecta loot válido (materiais econômicos coletáveis/vendáveis — Seção 37) dentro do Pickup Radius do jogador. Na coleta normal, respeita o Bag Filter, a capacidade da Bag e as regras de coleta parcial. Quando o Magnet está ativo, encaminha o loot para o fluxo de venda automática do Magnet, cuja regra de filtro ainda permanece pendente (Seção 29/53). Nome técnico não obrigatório.
- **Combat Scope Resolver** — restringe a seleção de alvos de habilidades, pets, summons, Ajudantes e Attack Budget ao Floor atual do jogador (Seção 24), sem afetar a lógica Cross-Floor do Coletor (Seção 36).

---

## 51. Documentos Especializados

| Documento | Conteúdo |
|---|---|
| **GDD Mestre** (este documento) | Fonte de verdade estrutural |
| **Hero Design Document** | Kits completos, frames de animação, coeficientes finais |
| **Combat System Document** | Timing de ataque, projéteis, Attack Budget |
| **Tower/Floor Document** | As 50 Floor Variants, posições de spawn, população por andar |
| **Employee System Document** | IA detalhada, virtualização técnica |
| **Bestiary** | Fichas completas de monstros e bosses, drop rates exatos |
| **Economy & Balance Document** | Tabela dos 15 tiers, preços, drop rates, curva de demanda, multiplicador de vida da forma de urso |
| **Chest & Card Document** | Pools de carta, curva de bônus, chance de Mimic |
| **Quest Document** | Progresso das 3 linhas |
| **UI/UX Document** | Layout final de HUD, loja, telas, Settings |
| **Technical Architecture Document** | Implementação de Floor Sleep, Stair Routing, Cross-Floor Loot Access, LastCompletedDay/NextDay |
| **Production Roadmap** | Sprints, milestones, estratégia de produção das Floor Variants (Variant A primeiro — Seção 25) |

---

## 52. Casos-Limite

### Menu / Save
| Caso | Resultado esperado |
|---|---|
| Continue Game sem save existente | Botão fica desabilitado — não é pendência de sistema complexo, apenas UI simples |
| New Game com save de run já existente | Não exige confirmação/modal obrigatória — inicia a nova run diretamente. O save antigo continua existindo até a nova run alcançar seu primeiro autosave (linha abaixo); uma confirmação de UX é decisão de interface futura, não requisito do GDD |
| Nova run iniciada e encerrada/fechada antes do primeiro autosave da Loja — por exemplo, o jogador fecha o jogo ou recebe Game Over no Modo Padrão | O save anterior continua intacto porque nenhum novo autosave ocorreu (Seção 43) |
| Continue Game após o cenário acima | Carrega normalmente a run antiga, pois ela nunca foi substituída |
| Game Over de uma run já salva (Modo Padrão — o Free não tem Game Over por demanda, Seção 42) | O checkpoint da última Loja permanece; Continue Game permite tentar o mesmo dia novamente (Seção 43) |
| Vitória no Dia 15 → Menu Principal | Save continua sendo a Loja que precede o Dia 15 — o Dia 15 pode ser jogado de novo via Continue Game (Seção 43) |
| Vitória no Dia 15 → Continuar | Resultados → Loja → novo autosave → checkpoint passa a ser a Loja que precede o Dia 16 |
| Dia 30 concluído | Save continua sendo a última Loja salva antes do Dia 30; sem autosave adicional após o encerramento |
| Continue Game | Nunca pede modo/herói/mapa/Floor Variants novamente — lê o Mode salvo no RunState |
| Save corrompido | Pendência técnica |
| Fechar o jogo na Loja antes de "Start Day N" | Já salvo |
| Fechar o jogo durante o dia | Progresso desde a última Loja é perdido — ao reabrir, volta ao checkpoint |

### Player
| Caso | Resultado esperado |
|---|---|
| Morrer durante a Ultimate | Interrompida imediatamente; Energia zera (Seção 11) |
| Morrer durante transformação (Druid) | **Sem** conversão proporcional — fluxo padrão de morte, retorna em forma humana com vida cheia (Seção 17.4) |
| Morrer com pet permanente ativo | Pet retorna junto; reanimação da animação ainda 🟡 |
| Morrer com summon temporário ativo (Necromancer) | Destruídos — regra padrão, sem exceção |
| Morrer durante interação | Interação cancelada |
| Tempo zera exatamente durante a resolução da penalidade de morte | Segue a ordem estrita da Seção 11: subtrai os 30s, só então verifica se o tempo acabou |
| Jogo pausado durante habilidade em andamento | Congela, não cancela; retoma ao despausar |

### Floors / Escadas / Boss Timer / Floor Variants
| Caso | Resultado esperado |
|---|---|
| Remover andar 6–10 | Bloqueado — nunca removíveis |
| Tentar uma 6ª remoção na mesma run | Bloqueado — máximo 5 |
| Tentar escolher manualmente qual Floor remover | Não existe essa opção — sempre remove o Active Floor 1 atual |
| Remove Tower Layer remove o primeiro Active Floor | Escada do térreo é remapeada para o novo Active Floor 1; o buraco/descida do novo Active Floor 1 passa a levar ao térreo (Seção 27) |
| Floor intermediário (nem primeiro nem último ativo) | Descida leva ao Active Floor anterior; subida leva ao próximo Active Floor (Seção 26) |
| Último Active Floor da run | Não existe subida além dele — sem comportamento visual extra além de não haver escada de subida disponível |
| Controle Remoto em cooldown | Ação bloqueada |
| Boss Timer ao trocar de Floor / morrer / voltar ao térreo | Continua acumulado, não reseta |
| Determinação do próximo Boss Spawn periódico após o primeiro, com o boss anterior ainda vivo | 🟡 não definido (Seção 22) |
| Floor Variant ao trocar de dia/morrer/remover Floor | Nunca sorteia de novo dentro da mesma run |

### Inventário / Loot / Employees
| Caso | Resultado esperado |
|---|---|
| Pilha maior que o espaço disponível | Coleta parcial |
| Fim de dia com loot não vendido | Destruído |
| Um abate droppa múltiplos materiais diferentes | Esperado — cada material rola independentemente (Seção 38) |
| Ajudante sem monstro no alcance | 🟡 não definido |
| Alvo do ajudante morre antes do ataque concluir | 🟡 não definido |
| Coletor buscando loot em Floor fora da tela | Funciona normalmente (Seção 36), sem exigir simulação individual completa |
| Baú é um Chest Mimic | Ataca ao ser aberto; ao morrer, libera a mesma recompensa que o baú normal daria e abre a mesma UI de 3 opções (Seção 30) |

### Gambler (Visão Expandida)
| Caso | Resultado esperado |
|---|---|
| Vida acima de 100% para ativar a Carta de Diamante | 🟡 Pendência de design — não inventar overheal |

---

## 53. Pendências Abertas

🟡 **Pendências localizadas de design/implementação — não reabrem sistemas já definidos e não impedem o início do desenvolvimento:**
- **Controle Remoto (Seção 26):** decisão final entre Variante A (andares visitados) e Variante B (todos os Active Floors) — configurável por playtest, não uma lacuna estrutural.
- Reanimação da animação de summon de pets permanentes ao retornar de uma morte.
- Ajudante sem alvo no alcance / alvo morre antes do ataque concluir.
- Determinação do próximo Boss Spawn periódico após o primeiro, com o boss anterior ainda vivo.
- Tecla de pause/menu geral.
- Condição exata que ativaria a Carta de Diamante do Gambler acima de 100% de vida.
- Condição de desbloqueio de Demonologist, Necromancer, The Gambler, Plague Doctor.
- **Rogue (Seção 17.5):** definir se "completar 30 dias" exige uma única run ou pode ser acumulado entre runs.
- **Assassin (Seção 17.9):** definir se "alcançar Andar 6" utiliza Original Floor Identity 6 ou Active Floor Position 6.
- **Inventário (Seção 37):** drag entre stacks iguais — comportamento de merge ainda não definido.
- **Inventário (Seção 37):** merge parcial quando o destino não comporta a stack inteira ainda não definido.
- **Inventário (Seção 37):** drag para slot ocupado por item diferente — swap/bloqueio ainda não definido.
- **Inventário (Seção 37):** clique direito — quantidade efetivamente descartada ainda não definida.
- **Magnet (Seção 29):** utiliza o Filtro de Bag do jogador para decidir quais tipos de loot vender automaticamente, ignora esse filtro, ou terá uma regra própria de filtro?
- **Combat / Floor Transition (Seção 24):** definir o comportamento de Persistent Areas, projéteis, summons e outros efeitos temporários deixados no Floor anterior ao trocar de Floor. Independentemente da solução futura, eles não podem continuar causando dano enquanto aquele Floor não for o Current Combat Floor — pendência localizada do Combat System, não crítica da máquina de estados.

🔢 **Balanceamento:**
Preços de Bonuses (incluindo as 3 compras de slots corrigidas e o novo Increase Pickup Radius), preço dos rerolls extras, cooldown do Controle Remoto, Attack Budgets (Melee/Ranged), Population (Minimum/Target/Maximum e frequência), curva de bônus de carta por andar, desbloqueio numérico do Mage e do Blood Mage, threshold do Boss Timer (~50s referência), curvas de Attack Speed por família de fonte, quantidade/distância de baús por Floor Variant, % de chance de Mimic, proporções de orçamento ofensivo de pets/summons, multiplicador de Vida Máxima da forma de urso do Druid, valor base do Pickup Radius e curva/preços de seus upgrades (Seção 37).

---

### GDD Mestre — Structural Freeze ✅

**A máquina de estados dos modos, Save, Core Loop, Floor System, Combat Scope, progressão de run, Inventory, Employees, Quests, Chest/Card flow e principais regras de herói estão estruturalmente fechados.** As pendências restantes listadas acima são localizadas e devem ser resolvidas nos documentos especializados (Seção 51) ou em playtest, sem exigir nova auditoria geral deste GDD.

Este documento só deve receber nova versão se: *(1)* o designer mudar uma regra estrutural; *(2)* uma pendência estrutural existente for decidida e precisar refletir no GDD Mestre; *(3)* surgir durante a implementação uma contradição real de gameplay. Não versionar novamente por wording, detalhe técnico, balanceamento, UI fina ou implementação — essas informações vão aos documentos especializados (Hero Design, Combat System, Tower/Floor, Employee System, Bestiary, Economy & Balance, Chest & Card, Quest, UI/UX, Technical Architecture, Production Roadmap).
