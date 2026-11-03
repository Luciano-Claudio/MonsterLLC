# Deadline 8.1 — Redesign Incremental (Fase 1: até a Loja)

_Decisões fechadas em sessão de planejamento pós-Deadline 8 (fechada em 2026-10-31). Cobre as
mecânicas que já existem e só precisam de ajuste, o sistema de popups/tooltips, o pool de 30
Talentos (fechado — Seção 7), os 30 Perks de Baixo (fechado — Seção 8), as 3 abas bespoke do
Barbarian — Esquerda (Seção 9), Direita (Seção 10) e Cima (Seção 11) —, as 3 abas bespoke do
Ranger — Esquerda (Seção 12), Direita (Seção 13) e Cima (Seção 14) —, e as 3 abas bespoke do
Mage — Esquerda (Seção 15), Direita (Seção 16) e Cima (Seção 17) —, e as 3 abas bespoke do Druid
— Esquerda (Seção 18), Direita (Seção 19) e Cima (Seção 20) —, e as 3 abas bespoke do Rogue —
Esquerda (Seção 21), Direita (Seção 22) e Cima (Seção 23) —, e as 3 abas bespoke do Cleric —
Esquerda (Seção 24), Direita (Seção 25) e Cima (Seção 26) —, 120 perks completos pra cada um dos
6 primeiros heróis. A Loja nova (3 abas) e o restante das abas bespoke (Paladin, Gunslinger,
Assassin, Blood Mage) ainda estão em debate — ver Seção 28._

## 0. Diagnóstico que motivou o redesign

Depois de vários testes (inclusive com terceiros), o jogo mostrou monotonia: falta identidade,
falta um arco de progressão com fim visível, e a loja/demanda atuais não geram decisão real.
Direção escolhida: transformar o jogo num **incremental dinâmico** — build-crafting por herói via
árvore de perks, variância por run via talentos RNG, e um objetivo final claro (sobreviver 10 dias
e derrotar o Divine God).

Isso **fecha a Deadline 8** (conteúdo/UI dela já estavam prontos — ver changelog) e abre a
Deadline 8.1, que reescreve partes do loop central em vez de só adicionar conteúdo em cima do que
já existia.

---

## 1. Sistema de Tradução (Localização)

Decisão: começar a localização AGORA, enquanto a superfície de texto ainda é pequena — adiar isso
viraria bola de neve conforme a Loja/Perks/Talentos forem crescendo.

### 1.1 — Cena Main_Menu

- **Menu principal:** New Game, Continue, Settings, Credits, Exit.
- **Settings — Header:** botões Sound, Display, Controls.
- **Settings — aba Sound:** Master, Music, SFX, Reset Default.
- **Settings — aba Display:** Theme, Brightness, Contrast, Saturation, Max FPS, Camera Max Zoom
  Out, Reset Default.
- **Settings — aba Controls:** Action, Input1, Input2, Primary Attack, Ultimate, Survival Skill,
  Move Up, Move Down, Move Left, Move Right, Interact, Open Inventory, Use TP, Menu, Pick Up
  Magnet, Reset Default.

### 1.2 — Cena _TestScene (gameplay)

- **Painel Inventory:** botões Inventory e Stats (existem duplicados — em cada uma das 2 abas,
  precisa traduzir os 2).
- **Menu_Esc (pause):** Resume, Settings, Main Menu, Exit.
- **Settings:** mesmo escopo exato do item 1.1 (estrutura idêntica à da Main_Menu).

---

## 2. Estrutura de 10 Dias (bioma + monstros)

Substitui a ideia antiga de 15–30 dias. Mantém a regra já existente de "distância da Safe Zone
simula dias futuros" (EffectiveSpawnDay), mas o teto agora é fixo em 10 dias, cada um mapeado a um
Tier específico de ameaça.

Os 6 grupos de bioma já existentes (`BiomeProfile_Tier1`, `Tier2-3`, `Tier4-5`, `Tier6-8`, `Tier9`,
`Tier10`) batem exatamente com o agrupamento usado abaixo — não precisa de bioma novo, só da
tabela de peso por dia.

| Dia | Bioma (dia) | Monstros — Dia | Monstros — Noite |
|---|---|---|---|
| 1 | 100% Tier1 | — | 80% T1 / 20% T2 |
| 2 | 80% Tier2-3 / 20% Tier1 | 80% T2 / 20% T1 | 80% T2 / 20% T3 |
| 3 | 100% Tier2-3 | 80% T3 / 20% T2 | 80% T3 / 20% T4 |
| 4 | 80% Tier4-5 / 20% Tier2-3 | 80% T4 / 20% T3 | 80% T4 / 20% T5 |
| 5 | 100% Tier4-5 | 80% T5 / 20% T4 | 80% T5 / 20% T6 |
| 6 | 80% Tier6-8 / 20% Tier4-5 | 80% T6 / 20% T5 | 80% T6 / 20% T7 |
| 7 | 100% Tier6-8 | 80% T7 / 20% T6 | 80% T7 / 20% T8 |
| 8 | 100% Tier6-8 | 100% T8 | 100% T8 |
| 9 | 100% Tier9 | 100% T9 | 100% T9 |
| 10 | 100% Tier10 | 100% T10 | 100% T10 — metade do dia inicia o Divine God |

**Nota de design (confirmado):** os dias 8–10 não têm blend de transição (diferente de 2→3, 4→5,
6→7) — é proposital, reforça a reta final "seca". Gancho narrativo futuro: popups do Divine God
"falando" com o jogador, cada vez mais irritado conforme ele avança, guardando os monstros mais
difíceis pro final.

**Dia 10 — regra especial:** depois de 50s (valor inicial, ajustável em teste), a Loja deixa de
aparecer ao fim do timer normal — em vez disso soma o Divine God, sem limite de tempo, com spawns
normais continuando. Fim de jogo ao derrotá-lo.

### 2.1 — População gradativa ao longo do dia

Decisão que **substitui a ideia de "venda de qualquer lugar" da Seção 3.1** (revertida — ver nota
lá). Em vez de tirar a necessidade de ir até o Vendor pra vender, a população de monstros no mapa
cresce GRADATIVAMENTE com o tempo do dia passando — não nasce toda de uma vez no gatilho do aggro
global (Seção 4). Começa baixa (dia claro, logo depois da Loja) e sobe até o pico; **a quantidade
MÁXIMA de monstros no mapa é sempre à noite, todo dia, por design** (não é mais um
"`crisisStreak`" de emergência do `WorldPopulationManager` — passa a ser o ritmo normal de
qualquer dia). Isso dá ao jogador uma janela real no começo do dia pra farmar uns kills, acumular
gold e ir até o Vendor vender sem risco de morte instantânea — o perigo de verdade (o enxame
cheio) chega só perto da noite.

**Em aberto:** a curva exata de crescimento (linear? por etapas, amarrada ao mesmo blend dia/
noite da tabela acima? reaproveita `populationMinimum/Target/Maximum` do `WorldPopulationManager`,
só agora amarrados ao tempo do dia em vez de só ao `crisisStreak`?) — ainda não decidido, só a
forma geral (gradual, pico à noite) está fechada.

---

## 3. Loop de Morte e Regeneração de Mundo

- **Morrer:** vai pra Loja (mesmo dia). Perde todo loot que ainda estava na bag (não vendido).
  Mantém o gold já ganho (kills + loot já vendido antes de morrer). O dia reinicia do zero: jogador
  volta pra `(0,0,0)`, todos os monstros e todo o loot no chão são removidos e ressumonados do
  zero. **O terreno/mapa NÃO é regenerado** — fica exatamente como estava (o jogador não percebe
  isso, só sente que "começou de novo").
- **Sobreviver o dia inteiro:** Loja abre, e só AQUI o mundo do dia seguinte é regenerado —
  reaproveitando a otimização já discutida de gerar o próximo mapa em background durante a visita
  à Loja. Ganho de performance direto: regeneração de mundo passa a ser rara (só em sucesso, nunca
  em retry por morte).
- Isso também fecha a pendência antiga da Deadline 8 ("UI final... Demanda") — **Demanda é
  removida do jogo**, nunca mais volta.
- **Controle Remoto (tecla Q) é removido inteiro** — GDD Seção 26: hoje é a mesma feature que (1)
  pausa o jogo e (2) abre o popup de TP de volta pro centro/Safe Zone. As duas coisas somem
  juntas: **não existe mais pausar com Q, nem TP de volta pra Safe Zone.** Não entra como ideia
  reaproveitável nos Perks de Baixo (ver Seção 8) — é remoção pura, não upgrade de algo que
  continua existindo.

**Exemplo — a progressão de dia nunca retrocede, só o dia atual reinicia:**

> Jogador está no Dia 2. Morre. Volta pro Dia 2 (não volta pro Dia 1) — mapa do Dia 2 continua o
> mesmo, monstros/loot resetam, posição volta pra `(0,0,0)`. Morre de novo: Dia 2 de novo. Ele
> fica preso no Dia 2 quantas vezes forem necessárias, até finalmente sobreviver os segundos
> inteiros daquele dia — só aí a Loja regenera o mundo e avança pro Dia 3. Morrer nunca "desfaz"
> um dia já vencido anteriormente, só repete o dia atual até ele ser superado.

### 3.1 — Economia de Combate: Gold por Kill

Contexto: o playtest do aggro global (Seção 4) mostrou 0 vendas em todos os testes — com o
enxame perseguindo desde o 1º contato, ficar parado perto do Vendor virava morte certa. A solução
fechada pra isso é a população gradativa (Seção 2.1), não mudar a venda em si — a venda continua
igual a hoje (ver abaixo). O gold direto por kill é uma decisão separada, que fica de qualquer
forma.

- **Gold direto por kill:** todo monstro morto dá **1 gold fixo** pro jogador, instantâneo, sem
  precisar vender nada. Soma-se ao loot (que continua dropando normalmente e precisa ser vendido
  pra virar gold) — não substitui a economia de loot, só dá um piso líquido que nunca depende de
  sobreviver até um vendor.
- **Venda continua exigindo o Vendor físico** — decisão revertida (era "vender de qualquer
  lugar", virou a população gradativa da Seção 2.1 em vez disso). `Vendor.Interact()` e
  `VendorCart.AcceptSale()` continuam exatamente como são hoje, sem nenhuma mudança de script.
- **Dependência a observar (só relevante quando a Demanda for removida de verdade):**
  `Vendor.Interact()` hoje chama
  `DemandTracker.Instance.RegisterSale(...)` — a Seção 3 já decidiu remover Demanda do jogo;
  quando isso for implementado, essa chamada precisa sair junto.

---

## 4. Sistema de Target / Aggro Total

Substitui o alcance de detecção individual por monstro. Regras:

- Target é **global e independente de hit** — proximidade do jogador OU o jogador mirando/
  selecionando um monstro também dispara. Assim que QUALQUER gatilho acontece, **todos** os
  monstros do mundo pegam target no jogador ao mesmo tempo (não só quem foi atingido).
- **Simulation Rings continuam existindo**, só passam a valer apenas para quem **não** tem target
  — um monstro com target sai do sistema de anel de distância e simula cheio, não importa a
  distância do jogador.
- **Respawn** (retry do dia após morte) já nasce com target ativo — não tem detecção de novo.
- **Stealth/transformação** (Coruja do Druid, invisibilidade do Assassin) zera o target enquanto
  durar, mas ele **volta sozinho** assim que a forma normal retorna, sem precisar de novo gatilho.
- **Cronômetro do dia** só começa a contar no instante do primeiro target (hit, proximidade, ou
  mira) — antes disso fica parado. Como monstro respawnado já nasce com target, essa "pausa antes
  de começar" só existe de verdade na primeira tentativa de cada dia; em todo retry o relógio já
  começa quase na hora.

---

## 4.1 — Bosses: não despawnar por distância, teleportar

Comportamento atual (`WorldBossSpawnManager`): quando um boss passa de `bossDespawnDistance` do
jogador, ele é destruído sem morte/loot e um boss **novo** (HP cheio) nasce em outra posição via
`SpawnOneBoss()`.

**Decisão nova:** o boss deixa de ser destruído/substituído — em vez disso, **o mesmo boss é
teleportado** para uma posição nova, próxima do jogador mas fora do campo de visão da câmera (sem
spawnar outro). Preserva HP/estado atual do boss entre o teleporte — não é mais sempre um boss
"fresco" depois de se afastar.

**Em aberto (ver Seção 8) — Sistema de HP de Boss:** ainda não decidido como a barra de vida do
boss vai funcionar na UI — fica embaixo do timer do dia? E se houver mais de 1 boss na tela ao
mesmo tempo (hoje o jogo já permite múltiplos bosses simultâneos)? Precisa de uma sessão de
planejamento própria antes de implementar.

---

## 5. Marco de Validação — Loja Fake ("Day X")

Antes de construir a Loja de verdade, criar uma versão mínima só com um botão "Day X" — serve pra
testar o ciclo completo (sobreviver → Loja → regenerar mundo → dia seguinte; morrer → Loja → mesmo
mapa → retry) isolado do risco de design da Loja real. Valida só a arquitetura técnica.

---

## 6. Sistema de Popups/Tooltips (hover)

Regra geral: botões de perk e de talento mostram **só um ícone**, zero texto — toda informação
aparece apenas no hover.

- **Hero Select (Main_Menu):** herói inativo → mostra o que falta pra desbloquear (placeholder
  **"Em breve"** até os critérios de desbloqueio serem definidos — ver pendência abaixo). Herói
  ativo → vida base, ataque base, e uma historinha curta sobre o herói.
- **Inventory:** hover num slot com item → nome do item + preço.
- **Loja — Perks** _(mecânica já decidida, conteúdo ainda não)_: hover → `X/5`, nome do perk, e a
  comparação real do efeito (valor atual → valor se comprar).
- **Talentos:** cada carta revelada → nome do talento + o que ele faz de verdade. O botão único de
  revelar as 3 cartas **não usa popup** — o próprio texto já indica o estado: **vermelho** "1
  Talent Point" se faltar ponto, **branco** se já tiver.

### 6.1 — Estados visuais dos botões de Perk

Resolve uma dor de UX real (relatada em reviews de jogos parecidos: ter que passar o mouse em cada
nó pra saber se dá pra comprar). Três estados distintos, visíveis sem precisar de hover:

1. **Liberado, sem gold suficiente.**
2. **Bloqueado** (pré-requisito da árvore ainda não comprado).
3. **Liberado e com gold suficiente.**

### 6.2 — Regra de desbloqueio da árvore

**1 ponto investido em qualquer nó já libera os nós adjacentes a ele** — mesmo que esse nó não
esteja completo (ex.: um perk `0/5` com só 1 ponto já libera os vizinhos).

---

## 7. Sistema de Talentos — Pool Universal (30 cartas)

Diferente do Perk (Seção 8 — pendência, 100% bespoke por herói), o Talento usa **o mesmo pool pra
todos os 10 heróis**, sem exceção. Regra arquitetural que define o que pode entrar nesse pool: **só
vira Talento o que é um número que TODO herói tem igual** — dano (ataque primário/secundário/
ultimate), HP, velocidade de movimento, cooldown, energia de ultimate, gold/loot. Qualquer coisa
que depende de **como** a habilidade específica funciona (duração de um shift que num herói é dash
e no outro é teleport instantâneo, tipo de projétil, hitbox, size) é bespoke por natureza — e
bespoke só pode virar Perk, nunca Talento.

**Sorteio:** 3 cartas aleatórias aparecem por vez (botão único — ver Seção 6). Pool de 30, chance
base de uma carta específica aparecer é 3/30 (recalcula conforme o pool crescer no futuro). Uma vez
escolhida, a carta fica ativa pro resto da run inteira — **sem ponto fixo pra desfazer**, diferente
do Perk.

O pool se divide em 2 famílias:

### 7.1 — Trade-off (risco grande ↔ recompensa grande)

Regra: pra um trade-off valer a pena de verdade, o ganho precisa ser grande o bastante pro jogador
pensar "vale a pena mesmo compensando o custo" — nunca cosmético ou marginal.

1. **Fúria sem Limites** — dano do ataque primário e secundário x2, cooldown do primário ÷2.
   Custo: Ultimate fica bloqueada (nem carrega) enquanto a carta estiver ativa.
2. **Ultimate Absoluta** — dano da Ultimate x5. Custo: carga máxima necessária x3 (300%).
3. **Golpe Fatal** — 50% de chance do ataque primário causar o dobro de dano. Custo: ataque
   primário perde knockback pra sempre.
4. **Explosão em Cadeia** — monstro morto explode causando dano de área = 100% do golpe que o
   matou. Custo: você recebe 50% desse dano também.
5. **Sangue por Dano** — todo dano causado +10% por cada 10% de HP perdido, sem teto. Custo:
   abaixo de 20% HP, nenhuma cura funciona (poção, regen, etc.).
6. **Vida pelo Fio** — velocidade +60% e dano de ataque primário/secundário +40%. Custo: HP
   máximo -50%.
7. **Casulo** — sobrevive 1 hit fatal por dia (não acumula, reseta a cada dia). Custo: HP
   máximo -40%.
8. **Vampirismo** — cura 25% de todo dano causado como HP. Custo: HP máximo -30%.
9. **Pele de Pedra** — dano recebido -50%. Custo: velocidade de movimento -40%.
10. **Reflexo Rápido** — 25% de chance de esquivar completamente um ataque recebido (dano 0).
    Custo: dano causado -20%.
11. **Recarga Elétrica** — cooldown do shift -50%. Custo: ganho de energia por kill -50%
    enquanto a carta estiver ativa.
12. **Passo Fantasma** — invulnerável (i-frames completos) durante o shift. Custo: cooldown do
    shift dobra (+100%). _(custo original era "-50% de duração do shift" — trocado por violar a
    regra universal: shift de teleport instantâneo, ex. Mage, não tem duração pra cortar.)_
13. **Explosão Automática** — Ultimate dispara sozinha a cada 20s fixos, sem precisar carregar.
    Custo: dano da Ultimate -60%.
14. **Fome Voraz** — ganho de energia por kill +400%. Custo: gold dropado por kill vira 0 (só
    resta vender loot).
15. **Reserva de Emergência** — ao receber um hit fatal, sobrevive com 50% HP em vez de morrer
    (1x por dia, zera a energia de ultimate ao acionar). Custo: ganho de energia -50% pelo resto
    do dia depois de usar.
16. **Avareza** — gold de venda de loot e de kill x4. Custo: PickupRadius vira 0 **e** velocidade
    de movimento -30%.
17. **Mercador de Sangue** — 50% de chance de monstro dropar gold direto, sem precisar vender
    loot. Custo: chance/quantidade de drop de loot -50%.
18. **Fortuna Dobrada** — chance/quantidade de drop de loot dobra. Custo: HP máximo -25%.
19. **Mãos Vazias** — venda de loot vale +200% gold. Custo: quantidade de loot dropado -50%
    (menos itens, cada um vale muito mais).
20. **Corrida Selvagem** — velocidade de animação de ataque/ultimate (`ActionSpeedMultiplier`)
    +100%. Custo: cooldown real entre ataques (`stats.attackSpeed`) +50%.
21. **Ciclo Acelerado** — timer do dia -20% (dia passa mais rápido). Custo: monstros daquele dia
    +20% HP.

### 7.2 — Set-bônus (sem trade-off, fracas sozinhas, fortes combinadas)

Regra: cada carta isolada é fraca de propósito — só fica build-defining se o jogador conseguir
juntar o resto do mesmo grupo na mesma run. Sem custo nenhum embutido.

**Poções (set de 4 — juntas, os 4 valores dobram pra +30% cada):**

22. **Poção Vermelha** — dano +15%.
23. **Poção Azul** — velocidade de movimento +15%.
24. **Poção Verde** — HP máximo +15%.
25. **Poção Dourada** — gold (venda de loot + kill) +15%.

**Trio de Guerra (set de 4 — juntas, dobram pra +30% cada):**

26. **Lâmina Afiada** — ataque primário +10% dano.
27. **Fôlego de Combate** — cooldown do ataque primário -10%.
28. **Impacto** — ataque primário +10% de knockback.

**Dupla de Ultimate (set de 2 — juntas, dobram pra +40% cada):**

29. **Carga Rápida** — ganho de energia por kill +20%.
30. **Fúria Contida** — dano da Ultimate +20%.

_Tamanhos de set variados (4/3/2) de propósito — evita que todo combo dependa da sorte mais difícil
(juntar as 4 poções)._

---

## 7.3 — Pendência de arquitetura: dano independente por habilidade (rework em todos os heróis)

Achado durante a construção dos Perks (Seção 9 em diante): em todo herói já construído
(Barbarian, Ranger, Mage), o dano da **Ultimate** e do **Shift** é calculado como um
multiplicador de `stats.damage` — o MESMO campo que o Ataque Primário usa como base (ex.:
Barbarian `ultimateDamageMultiplier`/`secondaryAbilityDamageMultiplier`; Ranger
`ultimateDamageMultiplier` em voo/chão/`bleedDamageMultiplier`; Mage `ultimateDamageMultiplier`/
`ultimateTrailDamageMultiplier`/`fireStatusDamageMultiplier`).

**Problema real:** os Perks de Esquerda, Cima e Direita precisam ser 100% independentes entre si
(regra já travada). Se um perk de Esquerda aumentar dano através de `stats.damage` diretamente
(em vez de um multiplicador que só existe pro Ataque Primário), esse aumento vaza de graça pra
Ultimate/Shift — mesmo sem nenhum perk de Cima ou Direita comprado. O inverso também vale.

**Decisão:** antes de implementar o sistema de Perks de verdade, cada herói precisa de uma base
de dano independente por habilidade. `stats.damage` continua existindo, mas os perks de cada aba
nunca mexem nele direto — cada aba aplica seu próprio multiplicador acumulado (Esquerda só no
Ataque Primário, Cima só na Ultimate, Direita só no Shift), lidos separadamente na hora de cada
habilidade calcular seu dano, sem um "vazar" pro outro. Rework necessário em Barbarian, Ranger e
Mage (já construídos) e em todos os heróis futuros, desde o início.

**Pets já estão corretos** (`PetController.damage`, campo próprio fixo, não deriva de
`stats.damage`) — não precisam de ajuste, só confirmado aqui pra não ficar pendência escondida.

---

## 8. Perks de Baixo (Mundo/Economia) — 30 perks, fechado

Diferente de Cima/Esquerda/Direita (100% bespoke por herói, ainda em debate pros outros heróis —
ver Seção 28), a
árvore de Baixo é **compartilhada entre os 10 heróis**, sem exceção, e seu conteúdo está **fechado
em 30 perks**. Regras que valem só aqui:

- **Nada de energia, dano (ofensivo), Ultimate, Shift ou Ataque Primário** — isso é território das
  outras 3 abas. Baixo é só mundo/economia/sobrevivência passiva.
- **Árvore deliberadamente "bagunçada"**, não dividida em ramos temáticos visíveis — um perk pode
  liberar outro sem relação nenhuma de tema (ex.: `Vigor` pode liberar `Baú Extra` e `Negociante
  Nato`). O agrupamento por família na lista abaixo é só pra leitura; a posição real no grafo da
  árvore (quando ela for desenhada) vai ser embaralhada de propósito.
- **Cruzamento com a aba Bonuses antiga** (GDD Seção 41): absorveu `Increase Inventory Slots` →
  `Mais Espaço de Bag`, `Increase Inventory Stack Size` → `Stack Maior`, e `Reroll de pergaminho`
  → `Reroll de Talento`. **Não absorveu:** `Add Time` (compra de tempo de dia — já removida do
  jogo, decisão original do brainstorm que abriu esta Deadline), `Controle Remoto` (removido
  inteiro — ver Seção 3), e `Supressão de Ameaça` (não mapeia bem pro Tier-fixo-por-Dia da Seção
  2 — descartada, não entrou).

1. **Instinto de Caçador** — 0/5 · 10/80/640/5.120/40.960 · gold por kill +8%/rank.
2. **Butim de Guerra** — 0/3 · 1M/12M/144M · gold por kill +15%/rank.
3. **Faro de Saque** — 0/5 · 10/80/640/5.120/40.960 · chance/quantidade de drop de loot +8%/rank.
4. **Atração** — 0/5 · 10/80/640/5.120/40.960 · PickupRadius +10%/rank.
5. **Campo de Atração** — 0/3 · 1M/12M/144M · PickupRadius +20%/rank.
6. **Horizonte de Coleta** — 0/3 · 100M/1,5B/22,5B · PickupRadius +30%/rank.
7. **Ímã Vivo** — 0/1 · 5T · auto-vende loot coletado, no instante da coleta, usando o
   PickupRadius atual do jogador como raio do imã. O buff mais forte da árvore — caro de
   propósito.
8. **Negociante Nato** — 0/5 · 10/80/640/5.120/40.960 · venda de loot +10%/rank.
9. **Mercador Astuto** — 0/3 · 1M/12M/144M · venda +15%/rank.
10. **Rede de Contatos** — 0/3 · 100M/1,5B/22,5B · venda +20%/rank.
11. **Tesouraria Real** — 0/2 · 5B/100B · venda +30%/rank.
12. **Baú Extra** — 0/3 · 10K/100K/1M · +1 baú sumonado no mapa/rank.
13. **Baú Duplo** — 0/3 · 1M/12M/144M · +15% de chance do baú dar 2 recompensas/rank.
14. **Mais Espaço de Bag** — 0/3 · 5K/50K/500K · +5 slots de inventário/rank.
15. **Stack Maior** — 0/6 · 50K/750K/11M/169M/2,5B/38B · aumenta o teto de stack por item/rank —
    mesma curva do antigo `Increase Inventory Stack Size` (16→32→128→1.024→8.192→131.072→
    1.048.576).
16. **Reroll de Talento** — 0/3 · 10M/150M/2,25B · +1 reroll grátis por dia das 3 cartas de
    Talento mostradas/rank.
17. **Vigor** — 0/5 · 10/80/640/5.120/40.960 · HP máximo +5%/rank.
18. **Fortitude** — 0/3 · 1M/12M/144M · HP máximo +8%/rank.
19. **Pulso Vital** — 0/3 · 100M/1,5B/22,5B · HP máximo +10%/rank.
20. **Vitalidade Suprema** — 0/2 · 5B/100B · HP máximo +15%/rank.
21. **Pele Grossa** — 0/3 · 10K/100K/1M · dano recebido -3%/rank.
22. **Couraça** — 0/3 · 1M/12M/144M · dano recebido -5%/rank.
23. **Casco de Ferro** — 0/3 · 100M/1,5B/22,5B · dano recebido -7%/rank.
24. **Recuperação** — 0/5 · 10K/100K/1M/10M/100M · regen de HP +%/rank.
25. **Metabolismo Acelerado** — 0/3 · 1M/12M/144M · regen +%/rank.
26. **Fôlego Eterno** — 0/3 · 100M/1,5B/22,5B · regen +%/rank.
27. **Passo Leve** — 0/5 · 10/80/640/5.120/40.960 · velocidade de movimento +4%/rank.
28. **Vento nas Costas** — 0/3 · 1M/12M/144M · velocidade +6%/rank.
29. **Pés Ligeiros** — 0/3 · 100M/1,5B/22,5B · velocidade +8%/rank.
30. **Segunda Chance** — 0/1 · 500B (alcançável "lá pro Tier 8", abaixo do Ímã Vivo) · 1x por dia,
    sobrevive com 1 HP a um hit fatal.

**Contagem por família** (confere 30: 4+4+3+3+3+3+2+8): Venda ×4, HP ×4, Coleta ×3, Defesa ×3,
Regen ×3, Velocidade ×3, Gold por kill ×2, e 8 perks únicos sem repetição (Faro de Saque, Ímã
Vivo, Baú Extra, Baú Duplo, Mais Espaço de Bag, Stack Maior, Reroll de Talento, Segunda Chance).

---

## 9. Perks de Esquerda (Ataque Primário) — Barbarian, 30 perks, fechado

Primeira das 3 abas bespoke por herói (Cima/Esquerda/Direita — ver Seção 28) a ser fechada. Serve
de referência de formato pras próximas 29 abas (10 heróis × 3, Baixo já é compartilhada — Seção
8).

### 9.1 — Mecânica nova: Ground Crack

Validada em playtest nesta sessão (ideia antiga que "não tinha vingado" antes — agora sim). Golpe
primário do Barbarian passa a sumonar uma fenda no chão:

- **Nasce sempre no pé do Barbarian** (nunca na hitbox diagonal, que fica deslocada) — só a
  **animação** (NE/NW/SE/SW) muda, conforme qual das 4 hitboxes diagonais o golpe acionou.
- **Tamanho acompanha `sizeMultiplier`** do Barbarian — corpo maior (perks de Corpo, Seção 9.2)
  já sumona fenda maior também, sem precisar de perk separado pra isso.
- **Dano e duração vivem no Barbarian, não no prefab** (`groundCrackDamagePerSecond`,
  `groundCrackTickInterval`, `groundCrackDuration`) — de propósito, pra perks de Esquerda só
  precisarem alterar o herói, nunca o prefab da fenda.
- 4 `CircleCollider2D` reais (1 por animação, só o da direção atual fica ativo), filhos do
  GameObject da fenda — mesmo padrão já usado pelas hitboxes de ataque do Bestiário
  (`AttackHitbox_NE` etc.).

### 9.2 — Os 30 perks

**Dano**
1. Golpe Pesado — 0/5 · 10/80/640/5.120/40.960 · dano +8%/rank.
2. Fúria do Impacto — 0/3 · 1M/12M/144M · dano +15%/rank.
3. Força Bruta — 0/3 · 100M/1,5B/22,5B · dano +20%/rank.
4. Devastação — 0/2 · 5B/100B · dano +30%/rank.
5. Fúria Definitiva — 0/1 · 50T · dano do ataque primário +50%.

**Knockback**
6. Empurrão — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
7. Impacto Sísmico — 0/3 · 1M/12M/144M · +20%/rank.
8. Onda de Choque — 0/3 · 100M/1,5B/22,5B · +30%/rank.

**Corpo (`sizeMultiplier`)**
9. Corpo Avantajado — 0/5 · 10/80/640/5.120/40.960 · +5%/rank.
10. Crescimento Brutal — 0/3 · 1M/12M/144M · +10%/rank.
11. Corpo de Titã — 0/3 · 100M/1,5B/22,5B · +15%/rank.

**Duração do Ground Crack**
12. Fenda Duradoura — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
13. Rachadura Profunda — 0/3 · 1M/12M/144M · +20%/rank.
14. Abismo — 0/3 · 100M/1,5B/22,5B · +30%/rank.

**Área do Ground Crack**
15. Fenda Larga — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
16. Terremoto — 0/3 · 1M/12M/144M · +15%/rank.
17. Cataclismo — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Aceleração da animação (`ActionSpeedMultiplier`)**
18. Golpe Rápido — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
19. Frenesi — 0/3 · 1M/12M/144M · +15%/rank.
20. Velocidade Brutal — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cooldown real (`stats.attackSpeed`)**
21. Reflexos de Combate — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
22. Instinto de Batalha — 0/3 · 1M/12M/144M · -8%/rank.

**Crítico** _(mecânica nova pro Barbarian)_
23. Golpe Certeiro — 0/5 · 10/80/640/5.120/40.960 · +5% chance de crítico (dano x2)/rank.
24. Precisão Selvagem — 0/3 · 1M/12M/144M · +8% chance de crítico/rank.

**Sinergia (ligadas ao Ground Crack)**
25. Fúria Diagonal — 0/3 · 10K/100K/1M · cada hitbox diagonal que acerta um alvo DIFERENTE no
    mesmo golpe soma +10% de dano nesse golpe, por rank.
26. Golpe Perfurante — 0/3 · 1M/12M/144M · o ataque atravessa o 1º alvo e acerta um 2º atrás
    dele; alcance de perfuração aumenta por rank.
27. Fenda Enfurecida — 0/3 · 1M/12M/144M · durante o Shift (Direita), dano do Ground Crack
    +15%/rank — mora na Esquerda porque o efeito é do Ground Crack, não do Shift.
28. Fenda Selvagem — 0/3 · 1M/12M/144M · a cada 10% de HP perdido (mesmo gatilho da passiva
    `GetPassiveDamageMultiplier`), dano do Ground Crack também sobe (+10/15/20% por step,
    conforme o rank).

**Gates finais**
29. Fenda Permanente — 0/1 · 5T · o Ground Crack nunca desaparece enquanto o Barbarian não se
    afastar do local.
30. Eco do Impacto — 0/1 · 500T · todo inimigo empurrado pelo knockback do ataque primário, ao
    parar, cria um 2º Ground Crack (menor) no ponto de impacto.

---

## 10. Perks de Direita (Shift/Survival) — Barbarian, 30 perks, fechado

Grounded nos campos reais do Shift (`secondaryAbilityDamageMultiplier`, `secondaryAbilitySpeedMultiplier`,
`secondaryAbilityDuration`, cooldown herdado de `HeroController`) e na passiva de HP perdido
(`GetPassiveDamageMultiplier`/`GetPassiveDamageSteps` — hoje +25%/step, teto de 8 steps/+200%).

**Dano do Shift**
1. Fúria Crescente — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
2. Ímpeto Selvagem — 0/3 · 1M/12M/144M · +15%/rank.
3. Avalanche de Golpes — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Fúria Sem Fim — 0/2 · 5B/100B · +30%/rank.

**Velocidade do Shift**
5. Passos Rápidos — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
6. Rajada — 0/3 · 1M/12M/144M · +12%/rank.
7. Vendaval — 0/3 · 100M/1,5B/22,5B · +16%/rank.
8. Velocidade da Tempestade — 0/2 · 5B/100B · +20%/rank.

**Duração do Shift**
9. Fôlego Extra — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
10. Resistência — 0/3 · 1M/12M/144M · +15%/rank.
11. Perseverança — 0/3 · 100M/1,5B/22,5B · +20%/rank.
12. Vontade de Ferro — 0/2 · 5B/100B · +30%/rank.

**Cooldown do Shift**
13. Recuperação Rápida — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
14. Fôlego Renovado — 0/3 · 1M/12M/144M · -8%/rank.
15. Instinto Ágil — 0/3 · 100M/1,5B/22,5B · -10%/rank.
16. Reflexo de Batalha — 0/2 · 5B/100B · -15%/rank.

**Passiva de HP perdido**
17. Sangue Fervente — 0/3 · 1M/12M/144M · +5%/step adicional por rank (acima dos 25% base).
18. Instinto Berserker — 0/3 · 100M/1,5B/22,5B · +8%/step adicional por rank.
19. Fúria sem Limites — 0/2 · 5B/100B · +10%/step adicional por rank.
20. Limite Expandido — 0/2 · 10M/150M · +1 step no teto da passiva por rank (hoje 8 steps/+200%,
    vai até 10/+250%).

**Mecânicas novas (sobrevivência)**
21. Fúria Protetora — 0/3 · 1M/12M/144M · durante o Shift, dano recebido -8%/rank.
22. Sede de Sangue — 0/3 · 1M/12M/144M · durante o Shift, cura 10%/rank do dano causado como HP.
23. Fôlego Recuperado — 0/3 · 1M/12M/144M · matar um monstro durante o Shift reduz o cooldown
    restante em 5%/rank.
24. Último Fôlego — 0/2 · 100M/1,5B · ao ativar o Shift, ganha um escudo (absorve dano) de
    10%/20% do HP máximo por rank.
25. Renovação Selvagem — 0/3 · 1M/12M/144M · regenera HP durante o Shift, %/segundo por rank.
26. Instinto de Sobrevivência — 0/2 · 100M/1,5B · abaixo de 30% HP, cooldown do Shift é reduzido
    em 50%/rank.
27. Vontade Inquebrável — 0/3 · 1M/12M/144M · durante o Shift, HP máximo temporário +5%/rank.
28. Fúria Final — 0/2 · 100M/1,5B · se o Shift for ativado abaixo de 20% HP, concede 1s/2s de
    invulnerabilidade total no início, por rank.
29. **Coração Selvagem** — 0/1 · 5T · upgrade direto do Instinto de Sobrevivência (#26, pré-
    requisito): toda vez que o HP cair abaixo de 10% — sem limite de 1x por dia, repete sempre
    que acontecer — o cooldown do Shift zera na hora, completamente.
30. Avatar da Fúria — 0/1 · 50T · durante o Shift, a passiva de HP perdido perde o teto de 8
    steps (escala sem limite enquanto durar).

---

## 11. Perks de Cima (Ultimate) — Barbarian, 30 perks, fechado

Fecha as 4 abas do Barbarian (Baixo compartilhado + Esquerda/Direita/Cima bespoke = 120 perks
completos pro 1º herói). Grounded em `ultimateDamageMultiplier`, `projectileSpeed`,
`projectileMaxDistance`, `projectileKnockbackForce`, os 8 projéteis fixos, e a carga/energia
específica da Ultimate (pendência citada na Seção 12 — resolvida aqui, Cima ganha um
multiplicador de ganho de energia próprio, separado do `EnergyGainMultiplier` geral de Baixo).

1. Golpe Divino — 0/5 · 10/80/640/5.120/40.960 · dano +10%/rank.
2. Ira Ancestral — 0/3 · 1M/12M/144M · +15%/rank.
3. Poder Esmagador — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Apocalipse Pessoal — 0/2 · 5B/100B · +30%/rank.
5. Juízo Final — 0/1 · 50T · dano +40%.
6. Projétil Veloz — 0/5 · 10/80/640/5.120/40.960 · velocidade +10%/rank.
7. Lança Relâmpago — 0/3 · 1M/12M/144M · +15%/rank.
8. Velocidade Absoluta — 0/3 · 100M/1,5B/22,5B · +20%/rank.
9. Alcance Ampliado — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
10. Longo Alcance — 0/3 · 1M/12M/144M · +15%/rank.
11. Horizonte de Guerra — 0/3 · 100M/1,5B/22,5B · +20%/rank.
12. Impacto Devastador — 0/5 · 10/80/640/5.120/40.960 · knockback +10%/rank.
13. Onda Expansiva — 0/3 · 1M/12M/144M · +15%/rank.
14. Força Absoluta — 0/3 · 100M/1,5B/22,5B · +20%/rank.
15. Canalização Rápida — 0/5 · 10/80/640/5.120/40.960 · carga necessária -5%/rank.
16. Fúria Contida — 0/3 · 1M/12M/144M · -8%/rank.
17. Explosão Iminente — 0/3 · 100M/1,5B/22,5B · -10%/rank.
18. Instinto de Poder — 0/5 · 10/80/640/5.120/40.960 · ganho de energia da Ultimate +10%/rank.
19. Sede de Poder — 0/3 · 1M/12M/144M · +15%/rank.
20. Fome de Destruição — 0/3 · 100M/1,5B/22,5B · +20%/rank.
21. Chuva de Lanças — 0/3 · 1M/12M/144M · +2 projéteis/rank (8 → 10 → 12 → 14).
22. Golpe Crítico Celestial — 0/3 · 1M/12M/144M · +5% chance de crítico (dano x2)/rank.
23. Explosão de Impacto — 0/3 · 1M/12M/144M · explosão em área por projétil, %/rank do dano.
24. Trilha de Fúria — 0/3 · 1M/12M/144M · chance de deixar um Ground Crack (Esquerda) no
    caminho, %/rank.
25. Pouso Sísmico — 0/3 · 1M/12M/144M · dano em área no ponto de aterrissagem, %/rank do dano
    base.
26. Renovação de Batalha — 0/2 · 100M/1,5B · cura %/rank de HP por inimigo atingido.
27. Fúria Repetida — 0/2 · 100M/1,5B · %/rank de chance de disparar uma 2ª rodada completa dos 8
    projéteis automaticamente.
28. Fúria Autônoma — 0/1 · 50T · a Ultimate dispara sozinha a cada N segundos fixos, além do uso
    manual normal.
29. Avatar da Destruição — 0/1 · 500T · a cada 5 usos da Ultimate, o 5º vem automaticamente com
    o dobro de dano e de projéteis, sem custo extra.
30. **Fragmentação Sem Fim** — 0/1 · **1 quadrilhão** (nó mais caro de toda a árvore de Cima) ·
    ao acertar um monstro, o projétil se divide em 2 novos projéteis (metade do dano/reserva de
    perfuração cada), disparados em direções opostas — cada filho pode se dividir de novo no seu
    próprio próximo acerto, recursivamente (cascata exponencial). Extremamente forte de
    propósito.

---

## 12. Perks de Esquerda (Ataque Primário) — Ranger, 30 perks, fechado

Segundo herói na ordem do GDD (Seção 17: 1. Barbarian ✅, 2. **Ranger**, 3. Mage, 4. Druid, 5.
Rogue, 6. Cleric, 7. Paladin, 8. Gunslinger, 9. Assassin, 10. Blood Mage). Grounded em
`attackDamageMultiplier`, `arrowSpeed`, `arrowLifetime`, `arrowKnockbackForce`, `arrowCount`
(hoje soma com o tier de arma) e o Bleeding/camuflagem já existentes no kit.

**Dano**
1. Mira Afiada — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Precisão Mortal — 0/3 · 1M/12M/144M · +15%/rank.
3. Tiro Certeiro — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Chuva Mortal — 0/2 · 5B/100B · +30%/rank.
5. Disparo Supremo — 0/1 · 50T · +40%.

**Velocidade da flecha**
6. Flecha Veloz — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
7. Vento Cortante — 0/3 · 1M/12M/144M · +15%/rank.
8. Velocidade Sônica — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Alcance da flecha**
9. Alcance Estendido — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
10. Vista de Águia — 0/3 · 1M/12M/144M · +15%/rank.
11. Horizonte Infinito — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Knockback da flecha**
12. Impacto Perfurante — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
13. Força do Disparo — 0/3 · 1M/12M/144M · +15%/rank.
14. Onda de Choque — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Quantidade de flechas** (`arrowCount`, soma ao que o tier de arma já dá)
15. Carcaz Cheio — 0/3 · 1M/12M/144M · +1 flecha/rank.
16. Chuva de Flechas — 0/3 · 100M/1,5B/22,5B · +1 flecha/rank.
17. Tempestade de Setas — 0/2 · 5B/100B · +1 flecha/rank.

**Cooldown real**
18. Mãos Rápidas — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
19. Reflexo de Caçador — 0/3 · 1M/12M/144M · -8%/rank.
20. Instinto Ágil — 0/3 · 100M/1,5B/22,5B · -10%/rank.

**Aceleração da animação**
21. Disparo Rápido — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
22. Fluidez de Combate — 0/3 · 1M/12M/144M · +15%/rank.
23. Precisão Veloz — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Crítico**
24. Golpe Vital — 0/3 · 1M/12M/144M · +5% chance de crítico/rank.
25. Olho de Falcão — 0/3 · 100M/1,5B/22,5B · +8% chance de crítico/rank.

**Sinergia/novas mecânicas**
26. Ataque Furtivo — 0/3 · 1M/12M/144M · o primeiro disparo depois de saiu da camuflagem
    (Shift/Direita) causa +20%/rank de dano.
27. Tiro à Distância — 0/3 · 1M/12M/144M · dano da flecha aumenta conforme a distância
    percorrida antes de acertar, até +20%/rank no alcance máximo.
28. Flechas Sangrentas — 0/3 · 1M/12M/144M · cada flecha tem %/rank de chance de aplicar
    Bleeding (mesmo efeito da Ultimate).
29. Flecha Bumerangue — 0/1 · 5T · ao chegar no alcance máximo sem gastar toda a reserva, a
    flecha retorna pelo mesmo caminho, podendo acertar de novo.

**Gate final**
30. Enxame Perfurante — 0/1 · 500T · flechas nunca gastam a reserva de perfuração ao atravessar
    um monstro — atingem TODOS no caminho com dano cheio, sem perder força.

---

## 13. Perks de Direita (Shift/Survival) — Ranger, 30 perks, fechado

Grounded na Camuflagem real (`secondaryAbilityHealPercentPerTick`, `secondaryAbilityHealTickInterval`,
`secondaryAbilityMaxDuration`, `IsPlayerUntargetable`, `IsDamageImmune`). Decisão de design
importante, descoberta em debate nesta sessão: a Camuflagem do Ranger é um **disfarce de objeto
do ambiente** (ele vira uma pedra, não fica translúcido/ninja) — por isso nenhum perk pode dar
movimento ou ataque DURANTE o disfarce (uma pedra não anda nem atira flecha). Dois perks
("mover-se disfarçado", "atacar disfarçado") foram descartados por quebrar essa lógica, e dois
outros ("disfarçar de graça sempre que o HP cair abaixo de X%") foram descartados por serem
broken — camuflagem = monstros perdem o alvo = imortalidade se puder ser acionada de graça
repetidamente.

**Árvore única, sem fork** — a ideia de 2 "trilhas" separadas (ficar disfarçado vs. virar um
lutador ativo) foi abandonada. Em vez disso, é uma progressão só: todo o resto da árvore funciona
igual durante o disfarce normal, e o topo da árvore (#25) remove a necessidade do disfarce em
si, mantendo os efeitos já comprados.

**Cura por tick**
1. Regeneração Furtiva — 0/5 · 10/80/640/5.120/40.960 · cura +2%/rank.
2. Cura Silenciosa — 0/3 · 1M/12M/144M · +3%/rank.
3. Renovação na Sombra — 0/3 · 100M/1,5B/22,5B · +4%/rank.
4. Vida Oculta — 0/2 · 5B/100B · +5%/rank.

**Frequência da cura**
5. Pulso Vital — 0/3 · 1M/12M/144M · intervalo -10%/rank.
6. Fluxo Constante — 0/3 · 100M/1,5B/22,5B · -15%/rank.
7. Ritmo da Sombra — 0/2 · 5B/100B · -20%/rank.

**Cooldown**
8. Reflexo Furtivo — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
9. Instinto Silencioso — 0/3 · 1M/12M/144M · -8%/rank.
10. Sombra Veloz — 0/3 · 100M/1,5B/22,5B · -10%/rank.
11. Dissipação Rápida — 0/2 · 5B/100B · -15%/rank.

**Armadilha do disfarce** (parado, mas não indefeso)
12. Disfarce Perfeito — 0/3 · 1M/12M/144M · monstros que passam muito perto ficam %/rank mais
    lentos, enganados pela armadilha.
13. Emboscada Perfeita — 0/1 · 50T · o primeiro ataque depois de SAIR do disfarce é sempre
    crítico garantido (dano x2).

**Evasão/sobrevivência**
14. Reflexos de Emergência — 0/2 · 100M/1,5B · abaixo de 20% HP, a próxima vez que tomar dano,
    esse dano é reduzido em %/rank.
15. Retorno Sombrio — 0/3 · 1M/12M/144M · cancelar antes do teto devolve %/rank do cooldown
    gasto.
16. Véu Prolongado — 0/3 · 1M/12M/144M · imunidade continua por 0,5s/1s/1,5s depois de sair, por
    rank — curto de propósito.
17. Sombra Vingativa — 0/3 · 1M/12M/144M · ao reaparecer, monstros que estavam perseguindo
    perdem o alvo por segundos/rank.
18. Instinto de Sobrevivência — 0/2 · 100M/1,5B · abaixo de 30% HP, cooldown da camuflagem
    -50%/rank.
19. Fôlego Sombrio — 0/3 · 1M/12M/144M · ao sair do disfarce, HP máximo temporário +%/rank por
    alguns segundos (não cura o atual, abre um colchão temporário).
20. Reflexos nas Sombras — 0/3 · 1M/12M/144M · fases de entrar/sair do disfarce mais rápidas,
    %/rank.
21. Instinto Predador — 0/2 · 100M/1,5B · regen de HP passivo fora do disfarce (durante o
    cooldown), %/rank.
22. Última Linha de Defesa — 0/1 · 5T · abaixo de 10% HP, dano recebido -40% enquanto permanecer
    nesse estado crítico — nunca aciona o disfarce, só resiste mais.
23. Armadilha Camuflada — 0/1 · 50T · monstro que toca o disfarce sem saber (ex.: Slime de
    contato) reflete dano nele mesmo.

**Cross-branch**
24. Instinto Caçador — 0/3 · 1M/12M/144M · ganho de energia da Ultimate +%/rank enquanto a
    camuflagem estiver em cooldown.

**Topo da árvore — remove a necessidade de disfarce**
25. **Veterano de Guerra** — 0/1 · 500T · remove a animação/necessidade de disfarce do Shift —
    continua visível, se movendo e atacando normalmente durante toda a janela. Todos os efeitos
    já comprados nos perks acima (cura, lentidão em área, HP temporário, imunidade breve, reset
    de aggro) continuam disparando automaticamente, na mesma ordem/tempo de antes. Em troca, a
    duração base do Shift (`secondaryAbilityMaxDuration`) cai 30%, compensando a perda do maior
    custo real da habilidade (ficar vulnerável/parado).
26. Postura de Combate — 0/3 · 1M/12M/144M · dano recebido -%/rank enquanto os efeitos do Shift
    estiverem ativos.
27. Fúria Curativa — 0/3 · 1M/12M/144M · dano causado +%/rank enquanto ativo.
28. Vigor de Batalha — 0/3 · 1M/12M/144M · cura adicional exclusiva enquanto ativo, %/rank.
29. Reflexos de Veterano — 0/2 · 100M/1,5B · cooldown do Shift ainda menor, %/rank.
30. **Imortal em Combate** — 0/1 · **1 quadrilhão** · pré-requisito: Veterano de Guerra (#25) —
    dano recebido drasticamente reduzido enquanto os efeitos do Shift estiverem ativos.

---

## 14. Perks de Cima (Ultimate) — Ranger, 30 perks, fechado

Fecha as 4 abas do Ranger (Baixo compartilhado + Esquerda/Direita/Cima bespoke = 120 perks
completos pro 2º herói). Grounded nos campos reais da faca (`RangerKnife`): dano em voo
(`ultimateDamageMultiplier`), dano no chão (hoje fixo em 1x `stats.damage`, ganha multiplicador
próprio aqui), `bleedDamageMultiplier`, `knifeFlightSpeed`, `knifeMaxDistance`,
`knifeFlightKnockbackForce`, `knifeGroundedDuration`, `knifeGroundedRadius`,
`knifeBleedDuration`, e a carga/energia específica da Ultimate (mesmo padrão do Barbarian).

**Dano em voo**
1. Lâmina Certeira — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Corte Profundo — 0/3 · 1M/12M/144M · +15%/rank.
3. Fúria das Lâminas — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Tempestade de Aço — 0/2 · 5B/100B · +30%/rank.
5. Lâmina Suprema — 0/1 · 50T · +40%.

**Dano no chão**
6. Facas Fincadas — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
7. Lâminas Cravadas — 0/3 · 1M/12M/144M · +15%/rank.
8. Armadilha Mortal — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Dano de sangramento**
9. Sangramento Intenso — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
10. Hemorragia — 0/3 · 1M/12M/144M · +15%/rank.
11. Ferida Fatal — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Velocidade em voo**
12. Voo Veloz — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
13. Lâmina Relâmpago — 0/3 · 1M/12M/144M · +15%/rank.
14. Velocidade Mortal — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Alcance em voo**
15. Alcance da Lâmina — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
16. Longo Voo — 0/3 · 1M/12M/144M · +15%/rank.
17. Horizonte de Aço — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Knockback em voo**
18. Impacto da Lâmina — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
19. Força Cortante — 0/3 · 1M/12M/144M · +15%/rank.

**Duração no chão**
20. Armadilha Duradoura — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
21. Lâmina Persistente — 0/3 · 1M/12M/144M · +15%/rank.
22. Campo Minado — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Raio no chão**
23. Área Ampliada — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
24. Zona de Perigo — 0/3 · 1M/12M/144M · +15%/rank.

**Duração do sangramento**
25. Veneno Prolongado — 0/3 · 1M/12M/144M · +15%/rank.

**Carga/energia da Ultimate**
26. Canalização Rápida — 0/5 · 10/80/640/5.120/40.960 · carga necessária -5%/rank.
27. Instinto de Caça — 0/3 · 1M/12M/144M · ganho de energia da Ultimate +15%/rank.

**Crítico**
28. Golpe Crítico Sombrio — 0/3 · 1M/12M/144M · +5% chance de crítico (dano x2)/rank.

**Gates finais**
29. Segunda Rajada — 0/1 · 500T · as 8 facas são lançadas numa 2ª rodada automática, 1s depois
    da primeira — dobra o total de facas por uso.
30. **Chuva de Lâminas** — 0/1 · **1 quadrilhão** · cada um dos 8 lançamentos da Ultimate solta
    2 facas em vez de 1, com um pequeno desvio angular entre elas (±11,25°) — dobra a densidade
    de facas sem precisar de animação nova (mesma Animation Event, só a lógica manda 2 em vez de
    1).

---

## 15. Perks de Esquerda (Ataque Primário) — Mage, 30 perks, fechado

Terceiro herói na ordem do GDD. Grounded no `MageAttackHitbox` real — trigger retangular
giratório, sem knockback nem perfuração (diferente do Barbarian/Ranger): `attackDamageMultiplier`,
`attackHitboxSizeMultiplier` (até 2x), `ActionSpeedMultiplier`, cooldown real. Duas mecânicas
novas de maior porte, grounded no precedente real do Gunslinger (`Attack_Start` → `Attack_Shoot`
em loop → `Attack_End`, GDD Seção 17.8).

**Dano**
1. Chama Ardente — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Fogo Crescente — 0/3 · 1M/12M/144M · +15%/rank.
3. Inferno Pessoal — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Combustão Total — 0/2 · 5B/100B · +30%/rank.
5. Fúria das Chamas — 0/1 · 50T · +40%.

**Alcance da chama**
6. Alcance Ardente — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
7. Chama Longa — 0/3 · 1M/12M/144M · +15%/rank.
8. Fogo Expansivo — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Abertura do arco** (separado do alcance — comprimento vs largura do trigger)
9. Leque de Fogo — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
10. Arco Flamejante — 0/3 · 1M/12M/144M · +15%/rank.
11. Muralha de Chamas — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cooldown real**
12. Conjuração Rápida — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
13. Fluxo Arcano — 0/3 · 1M/12M/144M · -8%/rank.
14. Domínio do Fogo — 0/3 · 100M/1,5B/22,5B · -10%/rank.

**Aceleração da animação**
15. Gesto Rápido — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
16. Fluidez Arcana — 0/3 · 1M/12M/144M · +15%/rank.
17. Precisão Flamejante — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Crítico**
18. Faísca Crítica — 0/3 · 1M/12M/144M · +5% crítico/rank.
19. Chama Perfeita — 0/3 · 100M/1,5B/22,5B · +8% crítico/rank.

**Mecânicas novas (fogo)**
20. Queimadura Residual — 0/3 · 1M/12M/144M · %/rank de chance do golpe aplicar o status Fire.
21. Rastro Flamejante — 0/3 · 1M/12M/144M · deixa uma área de fogo persistente no chão, %/rank
    de dano/segundo.
22. **Rajada Dupla** — 0/2 · 100M/1,5B · %/rank de chance de tocar a animação do `FireTrigger`
    2x mais rápido e disparar o `AnimationFireHitEvent` 2 vezes dentro do tempo de 1 cast
    normal — 2 hits no lugar de 1, sem alongar o tempo total.
23. Fogo Contagiante — 0/3 · 1M/12M/144M · inimigos queimando tem %/rank de chance de espalhar
    a queimadura pros vizinhos.
24. Eco Flamejante — 0/2 · 100M/1,5B · %/rank de chance de atingir 1 alvo adicional próximo ao
    primeiro.
25. **Impacto Explosivo** — 0/3 · 1M/12M/144M · se um inimigo com o status Fire ativo (aplicado
    pela Ultimate ou pelo Queimadura Residual) for morto pelo ataque primário, ele explode
    causando dano em área, %/rank do dano que o matou.
26. Fôlego Arcano — 0/2 · 100M/1,5B · recupera energia da Ultimate a cada acerto do primário,
    %/rank.
27. Chama Vingativa — 0/2 · 100M/1,5B · ao receber dano, o próximo ataque primário causa dano
    extra, %/rank.

**Gates finais**
28. Chama Absoluta — 0/1 · 50T · dano do ataque primário +25%.
29. **Chama Eterna** — 0/1 · **500T** · novo Animator state (`Attack_Start` → `Attack_Continuous`
    em loop → `Attack_End`, mesmo padrão do Gunslinger — GDD Seção 17.8) — o ataque primário
    vira um feixe contínuo enquanto o botão for mantido pressionado, disparando dano a cada
    repetição do loop. O Mage fica **parado** durante a canalização (mesma trava de movimento de
    qualquer ação) — esse é o custo real do perk.
30. **Supernova** — 0/1 · **1 quadrilhão** · a cada 10 casts, o 10º causa uma explosão massiva
    acumulando o dano dos 9 anteriores.

---

## 16. Perks de Direita (Shift/Survival) — Mage, 30 perks, fechado

Combina o Teleporte (`secondaryAbilityMaxRange`, `teleportProjectileSpeed`, cooldown real) com a
Phoenix — GDD Seção 17.3 confirma que a "Passiva" do Mage **é** o pet Phoenix, não duas coisas
separadas. Grounded no `PetController` real (`damage`, `attackCooldownDuration`, `chaseRadius`) e
nos campos que já existem no Mage (`petSizeMultiplier`, `petActionSpeedMultiplier`).

**Alcance do teleporte**
1. Salto Arcano — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
2. Portal Expandido — 0/3 · 1M/12M/144M · +15%/rank.
3. Distância Arcana — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Horizonte Místico — 0/2 · 5B/100B · +30%/rank.

**Velocidade do teleporte**
5. Viagem Rápida — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
6. Fluxo Instantâneo — 0/3 · 1M/12M/144M · +15%/rank.
7. Velocidade Arcana — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cooldown do Shift**
8. Recarga Mística — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
9. Canal Aberto — 0/3 · 1M/12M/144M · -8%/rank.
10. Domínio Arcano — 0/3 · 100M/1,5B/22,5B · -10%/rank.

**Dano do pet (Phoenix)**
11. Bico Flamejante — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
12. Garras Ardentes — 0/3 · 1M/12M/144M · +15%/rank.
13. Fúria da Fênix — 0/3 · 100M/1,5B/22,5B · +20%/rank.
14. Chama Imortal — 0/2 · 5B/100B · +30%/rank.
15. Avatar da Fênix — 0/1 · 50T · +40%.

**Velocidade de ataque do pet**
16. Reflexos da Fênix — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
17. Instinto Rápido — 0/3 · 1M/12M/144M · +15%/rank.
18. Fúria Instantânea — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Tamanho do pet**
19. Fênix Maior — 0/5 · 10/80/640/5.120/40.960 · +5%/rank.
20. Fênix Gigante — 0/3 · 1M/12M/144M · +10%/rank.
21. Fênix Colossal — 0/3 · 100M/1,5B/22,5B · +15%/rank.

**Cooldown de ataque do pet**
22. Garras Ágeis — 0/3 · 1M/12M/144M · -10%/rank.
23. Fúria sem Pausa — 0/2 · 100M/1,5B · -15%/rank.

**Alcance de perseguição do pet**
24. Vínculo Estendido — 0/3 · 1M/12M/144M · raio de perseguição (`chaseRadius`) +15%/rank.

**Mecânicas novas**
25. Chama Residual — 0/3 · 1M/12M/144M · o teleporte deixa uma área de fogo no ponto de origem,
    %/rank de dano/segundo.
26. Pouso Explosivo — 0/3 · 1M/12M/144M · ao reaparecer, causa dano em área no ponto de chegada,
    %/rank.
27. Fênix Vingativa — 0/2 · 100M/1,5B · quando o herói toma dano, o pet ganha dano extra
    temporário, %/rank.
28. Renascimento — 0/2 · 100M/1,5B · ao morrer, cura %/rank do HP máximo instantaneamente no
    respawn (além do pet já voltar sozinho, como já é hoje).

**Gates finais**
29. **Segunda Fênix** — 0/1 · 500T · summona um 2º Phoenix simultâneo, com metade das stats do
    primeiro.
30. **Teleporte Instantâneo** — 0/1 · **1 quadrilhão** · `teleport_start` e `teleport_end`
    continuam existindo, só que bem acelerados — o que desaparece é o projétil carregador que
    viaja a distância combinada; sem ele, o Mage vai direto do fim do `teleport_start` pro
    `teleport_end`, sem a janela de viagem intermediária.

---

## 17. Perks de Cima (Ultimate) — Mage, 30 perks, fechado

Fecha as 4 abas do Mage (Baixo compartilhado + Esquerda/Direita/Cima bespoke = 120 perks
completos pro 3º herói). Grounded na bola de fogo real (`MageFireball`): dano de impacto
(`ultimateDamageMultiplier`), dano/duração do rastro no chão (`ultimateTrailDamageMultiplier`/
`fireballGroundedDuration`), dano/duração do status Fire (`fireStatusDamageMultiplier`/
`fireballFireStatusDuration`), velocidade/alcance de voo, tamanho geral (`ultimateSizeMultiplier`,
escala explosão + rastro juntos) e a carga/energia específica da Ultimate.

**Dano de impacto**
1. Explosão Ardente — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Bola de Fogo Maior — 0/3 · 1M/12M/144M · +15%/rank.
3. Inferno Absoluto — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Cataclismo Arcano — 0/2 · 5B/100B · +30%/rank.

**Dano do rastro no chão**
5. Rastro Ardente — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
6. Chamas Persistentes — 0/3 · 1M/12M/144M · +15%/rank.
7. Fogo Eterno — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Dano do status Fire**
8. Queimadura Arcana — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
9. Combustão Interna — 0/3 · 1M/12M/144M · +15%/rank.
10. Fogo Consumidor — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Duração do status Fire**
11. Queimadura Prolongada — 0/3 · 1M/12M/144M · +15%/rank.
12. Chama Duradoura — 0/2 · 100M/1,5B · +20%/rank.

**Velocidade de voo**
13. Voo Flamejante — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
14. Projétil Arcano — 0/3 · 1M/12M/144M · +15%/rank.
15. Velocidade Infernal — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Alcance de voo**
16. Alcance Flamejante — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
17. Trajetória Longa — 0/3 · 1M/12M/144M · +15%/rank.
18. Horizonte em Chamas — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Tamanho geral da bola** (escala explosão + rastro no chão juntos)
19. Bola Maior — 0/5 · 10/80/640/5.120/40.960 · +5%/rank.
20. Esfera Flamejante — 0/3 · 1M/12M/144M · +10%/rank.
21. Sol Pessoal — 0/3 · 100M/1,5B/22,5B · +15%/rank.

**Duração do rastro no chão**
22. Chão em Chamas — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
23. Fogo que Não Apaga — 0/3 · 1M/12M/144M · +15%/rank.
24. Terra Arrasada — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Carga/energia da Ultimate**
25. Conjuração Suprema — 0/5 · 10/80/640/5.120/40.960 · carga necessária -5%/rank.
26. Fluxo Explosivo — 0/3 · 1M/12M/144M · -8%/rank.
27. Instinto Arcano — 0/5 · 10/80/640/5.120/40.960 · ganho de energia da Ultimate +10%/rank.
28. Fome Arcana — 0/3 · 1M/12M/144M · +15%/rank.

**Crítico**
29. Faísca Devastadora — 0/3 · 1M/12M/144M · +5% chance de crítico (dano x2)/rank.

**Gate final**
30. **Trindade Flamejante** — 0/1 · **1 quadrilhão** · a cada 3 usos da Ultimate, a bola de
    fogo se divide em 3 projéteis simultâneos (ângulos próximos), todos com o dano cheio.

---

## 18. Perks de Esquerda (Ataque Primário) — Druid, 30 perks, fechado

Quarto herói na ordem do GDD. Druid tem 2 formas (humana/Alce) — o Ataque Primário "de verdade"
(Esquerda) é a **vinha**, só existe na forma humana. Em forma Alce o mesmo botão vira a Garra do
Alce, mas como a forma Alce **é** a Ultimate (Cima), a Garra fica pros perks de Cima quando
chegarmos lá, não aqui. Grounded em `vineCount` (placeholder antigo de teto 15 — revisto abaixo,
já não faz sentido com os perks), `vineSearchRadius`, `vineLifetime`, `vineHitRadius`.

**Dano**
1. Raízes Afiadas — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Garras da Terra — 0/3 · 1M/12M/144M · +15%/rank.
3. Fúria Selvagem — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Força da Natureza — 0/2 · 5B/100B · +30%/rank.
5. Ira da Floresta — 0/1 · 50T · +40%.

**Quantidade de vinhas** (7 nós, incrementos crescentes — soma +56 no total, bem além do antigo
placeholder "teto 15" de `vineCount`, que precisa ser revisto/removido na implementação)
6. Vinhas Extras — 0/5 · 10/80/640/5.120/40.960 · +1 vinha/rank.
7. Enxame de Raízes — 0/3 · 1M/12M/144M · +2 vinhas/rank.
8. Floresta Viva — 0/3 · 100M/1,5B/22,5B · +3 vinhas/rank.
9. Raízes Sem Fim — 0/2 · 5B/100B · +5 vinhas/rank.
10. Floresta Densa — 0/3 · 1M/12M/144M · +2 vinhas/rank.
11. Raízes Infinitas — 0/2 · 100M/1,5B · +5 vinhas/rank.
12. Enxame Total — 0/1 · 50T · +10 vinhas de uma vez.

**Raio de busca de alvo**
13. Sentidos Aguçados — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
14. Visão da Floresta — 0/3 · 1M/12M/144M · +15%/rank.
15. Instinto Selvagem — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Raio de acerto da vinha**
16. Alcance das Raízes — 0/3 · 1M/12M/144M · +15%/rank.
17. Vinhas Abrangentes — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cooldown real**
18. Chamado Rápido — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
19. Fôlego Natural — 0/3 · 1M/12M/144M · -8%/rank.
20. Domínio Selvagem — 0/3 · 100M/1,5B/22,5B · -10%/rank.

**Aceleração da animação**
21. Reflexo Natural — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
22. Fluidez Selvagem — 0/3 · 1M/12M/144M · +15%/rank.
23. Graça da Floresta — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Crítico**
24. Golpe Certeiro da Natureza — 0/3 · 1M/12M/144M · +5% crítico/rank.
25. Fúria Precisa — 0/3 · 100M/1,5B/22,5B · +8% crítico/rank.

**Mecânicas novas**
26. Veneno da Floresta — 0/3 · 1M/12M/144M · %/rank de chance da vinha aplicar um status de
    veneno (dano contínuo).
27. Raízes Vampíricas — 0/3 · 1M/12M/144M · cura o Druid em %/rank do dano causado pela vinha.
28. Vinhas Abundantes — 0/2 · 100M/1,5B · %/rank de chance de uma vinha extra nascer num alvo
    fora da lista normal de `vineCount`.

**Gates finais**
29. **Floresta Desperta** — 0/1 · 50T · vinhas nascem em TODOS os monstros dentro do raio de
    busca, não só nos mais próximos.
30. **Prisão Eterna** — 0/1 · **1 quadrilhão** · a vinha nunca desaparece enquanto o alvo
    estiver vivo, prendendo-o no lugar até morrer.

---

## 19. Perks de Direita (Shift/Survival) — Druid, 30 perks, fechado

Druid tem 2 formas, e o botão de Shift faz coisas diferentes em cada uma: na forma humana é a
**Coruja** (voo livre, invisível, sem dano/cura própria hoje); na forma Alce é a **Cabeçada**
(ataque, não sobrevivência). Mesmo critério do Ataque Primário (Seção 18): como a Cabeçada só
existe DENTRO da forma Alce — que é a própria Ultimate — ela fica pros perks de Cima, não aqui.
Esta lista cobre só a Coruja.

**Diferença importante do disfarce do Ranger:** a Coruja voa livre — o movimento nunca trava, só
não pode atacar. Isso abre espaço de design que não existia pro Ranger (velocidade/regen durante
o voo fazem sentido aqui).

**Duração da Coruja**
1. Voo Prolongado — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
2. Asas da Noite — 0/3 · 1M/12M/144M · +15%/rank.
3. Vigília Noturna — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Eternidade Alada — 0/2 · 5B/100B · +30%/rank.
5. Voo Sem Fim — 0/1 · 50T · +40%.

**Cooldown do Shift**
6. Retorno Rápido — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
7. Instinto da Coruja — 0/3 · 1M/12M/144M · -8%/rank.
8. Fôlego Noturno — 0/3 · 100M/1,5B/22,5B · -10%/rank.
9. Domínio da Noite — 0/2 · 5B/100B · -15%/rank.

**Velocidade durante a Coruja**
10. Vento Noturno — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
11. Rajada Silenciosa — 0/3 · 1M/12M/144M · +15%/rank.
12. Vendaval Alado — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Regeneração durante a Coruja** (mecânica nova — hoje a Coruja não cura nada)
13. Cura da Noite — 0/5 · 10/80/640/5.120/40.960 · regen de HP +%/rank.
14. Renovação Alada — 0/3 · 1M/12M/144M · +%/rank.
15. Vida da Floresta — 0/3 · 100M/1,5B/22,5B · +%/rank.

**Evasão/sobrevivência**
16. Reflexos Noturnos — 0/2 · 100M/1,5B · abaixo de 20% HP, a próxima vez que tomar dano, esse
    dano é reduzido em %/rank.
17. Retorno Veloz — 0/3 · 1M/12M/144M · cancelar antes do teto devolve %/rank do cooldown.
18. Véu da Noite — 0/3 · 1M/12M/144M · imunidade continua por 0,5s/1s/1,5s depois de sair, curto
    de propósito.
19. Fuga Confusa — 0/3 · 1M/12M/144M · ao reaparecer, monstros que estavam perseguindo perdem o
    alvo por segundos/rank.
20. Instinto de Sobrevivência — 0/2 · 100M/1,5B · abaixo de 30% HP, cooldown da Coruja -50%/rank.
21. **Última Plumagem** — 0/1 · 5T · upgrade do #20: abaixo de 10% HP, dano recebido -40%
    enquanto permanecer nesse estado crítico (nunca aciona a Coruja de graça).

**Mecânicas novas**
22. Emboscada da Floresta — 0/1 · 50T · o primeiro ataque depois de SAIR da Coruja é sempre
    crítico garantido.
23. Pena Resistente — 0/3 · 1M/12M/144M · ignora/reduz dano de hazards de terreno enquanto na
    forma Coruja, %/rank.
24. Olhos da Noite — 0/2 · 100M/1,5B · aumenta o raio de visão de itens/baús enquanto na forma
    Coruja, %/rank.
25. Fôlego Duplo — 0/2 · 100M/1,5B · %/rank de chance de não consumir o cooldown ao usar a
    Coruja.

**Gates finais**
26. **Caçadora da Noite** — 0/1 · 500T · permite usar o Ataque Primário (vinhas) normalmente
    durante a forma Coruja, sem sair da forma — mas atacar quebra a proteção: o monstro atingido
    (e, pela regra de Aggro Global da Seção 4, o resto do mundo também) recupera o alvo na hora,
    mesmo com a invisibilidade ainda ativa. A Coruja continua escondendo de quem só se aproxima;
    atacar sempre tem o mesmo preço — todo mundo vem pra cima de você.
27. Vínculo Selvagem — 0/2 · 100M/1,5B · ao sair da Coruja, cura %/rank do HP máximo
    instantaneamente.
28. Instinto Caçador — 0/3 · 1M/12M/144M · ganho de energia da Ultimate +%/rank enquanto a
    Coruja estiver em cooldown.
29. Reflexo Selvagem — 0/2 · 100M/1,5B · %/rank de chance de ganhar uma recarga instantânea
    extra do Shift ao receber dano crítico.
30. **Espírito Eterno** — 0/1 · **1 quadrilhão** · pré-requisito: Caçadora da Noite (#26) —
    durante a forma Coruja, dano das vinhas +100% e dano recebido reduzido drasticamente.

---

## 20. Perks de Cima (Ultimate) — Druid, 30 perks, fechado

Fecha as 4 abas do Druid (Baixo compartilhado + Esquerda/Direita/Cima bespoke = 120 perks
completos pro 4º herói). Cobre a forma Alce inteira — transformação, Garra e Cabeçada — já que
tudo isso É a Ultimate. Grounded em `elkDamageMultiplier`, `elkMaxHealthMultiplier`,
`elkMoveSpeedMultiplier`, `elkFormMaxDuration`, `elkSizeMultiplier`, `elkKnockbackForce` (Garra),
e os campos próprios da Cabeçada (`elkHeadbuttLaunchSpeed`, `elkHeadbuttMaxRange`,
`elkHeadbuttDamageMultiplier`).

**Dano do Alce** (afeta Garra e Cabeçada juntos, já que os dois multiplicam `stats.damage`)
1. Fúria Selvagem do Alce — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
2. Força Bestial — 0/3 · 1M/12M/144M · +15%/rank.
3. Poder da Natureza — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Avatar Selvagem — 0/1 · 50T · +30%.

**HP máximo do Alce**
5. Vida Bestial — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
6. Vigor Selvagem — 0/3 · 1M/12M/144M · +15%/rank.
7. Resistência da Natureza — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Velocidade do Alce**
8. Corrida Selvagem — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
9. Galope Veloz — 0/3 · 1M/12M/144M · +15%/rank.
10. Instinto da Manada — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Duração da forma Alce**
11. Transformação Prolongada — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
12. Espírito Resistente — 0/3 · 1M/12M/144M · +15%/rank.
13. Forma Eterna — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Tamanho do Alce**
14. Corpo Avantajado do Alce — 0/5 · 10/80/640/5.120/40.960 · +5%/rank.
15. Porte Colossal — 0/3 · 1M/12M/144M · +10%/rank.
16. Titã da Floresta — 0/3 · 100M/1,5B/22,5B · +15%/rank.

**Knockback da Garra**
17. Garras Pesadas — 0/3 · 1M/12M/144M · +15%/rank.
18. Impacto Bestial — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cabeçada — velocidade de carga**
19. Carga Veloz — 0/3 · 1M/12M/144M · +15%/rank.
20. Impulso Selvagem — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cabeçada — alcance**
21. Alcance da Carga — 0/3 · 1M/12M/144M · +15%/rank.
22. Avanço Total — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cabeçada — dano próprio**
23. Cabeçada Brutal — 0/3 · 1M/12M/144M · +15%/rank.
24. Impacto Fatal — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Carga/energia da Ultimate**
25. Canalização Selvagem — 0/5 · 10/80/640/5.120/40.960 · carga necessária -5%/rank.
26. Fúria Contida da Natureza — 0/3 · 1M/12M/144M · -8%/rank.
27. Instinto Primitivo — 0/5 · 10/80/640/5.120/40.960 · ganho de energia da Ultimate +10%/rank.

**Crítico**
28. Golpe Certeiro Bestial — 0/3 · 1M/12M/144M · +5% chance de crítico/rank.

**Gates finais**
29. **Vigor Primordial** — 0/1 · 50T · o jogador já começa o dia com 100% da energia da
    Ultimate carregada — pode virar Alce imediatamente, sem precisar farmar energia primeiro.
30. **Espírito Indomável** — 0/1 · **1 quadrilhão** · a forma Alce perde o teto de duração —
    dura até você sair manualmente. _(Decisão descartada no caminho: remover o tempo das
    animações de transformar-em/transformar-de-volta — combinado com entrar/sair livre e a cura
    de 100% já existente ao ENTRAR, isso virava um loop de cura infinita. A cura ao entrar
    continua valendo só 1x por entrada, como já era.)_

---

## 21. Perks de Esquerda (Ataque Primário) — Rogue, 30 perks, fechado

Quinto herói na ordem do GDD. Rogue é diferente dos anteriores: o Ataque Primário é um **pulso
de área no próprio corpo** (Self Area Pulse) — o Rogue fica LIVRE pra se mover durante ele, não é
um golpe/projétil. Grounded em `pulseDuration`, `pulseTickInterval`, `pulseSizeMultiplier` (até
3x), e o dano por tick (`stats.damage`, ganha multiplicador próprio igual os outros heróis — ver
Seção 7.3).

**Dano por tick**
1. Veneno Sombrio — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Lâminas Invisíveis — 0/3 · 1M/12M/144M · +15%/rank.
3. Fúria das Sombras — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Corte Perfeito — 0/2 · 5B/100B · +30%/rank.
5. Dança Mortal — 0/1 · 50T · +40%.

**Duração do pulso**
6. Pulso Prolongado — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
7. Fôlego das Sombras — 0/3 · 1M/12M/144M · +15%/rank.
8. Resistência Furtiva — 0/3 · 100M/1,5B/22,5B · +20%/rank.
9. Área Eterna — 0/2 · 5B/100B · +30%/rank.

**Intervalo do tick** (mais frequente = mais dano total no mesmo tempo)
10. Ritmo Mortal — 0/5 · 10/80/640/5.120/40.960 · -10%/rank.
11. Cadência Sombria — 0/3 · 1M/12M/144M · -15%/rank.
12. Fluxo Constante das Lâminas — 0/3 · 100M/1,5B/22,5B · -20%/rank.

**Tamanho da área**
13. Área Ampliada — 0/5 · 10/80/640/5.120/40.960 · +5%/rank.
14. Raio das Sombras — 0/3 · 1M/12M/144M · +10%/rank.
15. Zona Mortal — 0/3 · 100M/1,5B/22,5B · +15%/rank.
16. Domínio Total — 0/2 · 5B/100B · +20%/rank.

**Cooldown real**
17. Reflexos Sombrios — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
18. Instinto Furtivo — 0/3 · 1M/12M/144M · -8%/rank.
19. Precisão Ágil — 0/3 · 100M/1,5B/22,5B · -10%/rank.

**Crítico**
20. Golpe Furtivo — 0/3 · 1M/12M/144M · +5% crítico/rank.
21. Lâmina Certeira — 0/3 · 100M/1,5B/22,5B · +8% crítico/rank.

**Mecânicas novas**
22. Veneno Persistente — 0/3 · 1M/12M/144M · %/rank de chance de cada tick aplicar um status
    de veneno adicional.
23. Sangue nas Sombras — 0/3 · 1M/12M/144M · cura o Rogue em %/rank do dano causado pelo pulso.
24. Silêncio Sombrio — 0/2 · 100M/1,5B · %/rank de chance de atordoar brevemente monstros
    dentro do pulso a cada tick.
25. Ativação Instantânea — 0/3 · 1M/12M/144M · reduz o tempo das animações
    `attack_start`/`attack_end`, %/rank.
26. Pulso Residual — 0/2 · 100M/1,5B · ao terminar o pulso, deixa uma área residual menor por
    mais alguns segundos, %/rank de dano.

**Cross-branch**
27. Instinto Caçador das Sombras — 0/3 · 1M/12M/144M · ganho de energia da Ultimate +%/rank
    enquanto o pulso estiver ativo.

**Gates finais**
28. **Passos Sombrios** — 0/1 · 50T · enquanto o pulso estiver ativo, o Rogue ganha +50% de
    velocidade de movimento.
29. **Fúria Infinita** — 0/1 · 500T · remove o cooldown do ataque primário enquanto o pulso
    durar — pode reativar imediatamente ao terminar.
30. **Pulso Eterno** — 0/1 · **1 quadrilhão** · o pulso nunca mais precisa ser reativado — fica
    permanentemente ativo ao redor do Rogue, desligável manualmente se quiser trocar de build.

---

## 22. Perks de Direita (Shift/Survival) — Rogue, 30 perks, fechado

Direita do Rogue é a **Cambalhota** (`rollSpeed`, `rollKnockbackForce`, `rollHitRadius`,
cooldown). Hoje ela só empurra monstros no caminho (sem dano nenhum), dá imunidade total e
atravessa colisão — bastante espaço pra mecânica nova.

**Velocidade**
1. Passo Sombrio — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
2. Deslize Veloz — 0/3 · 1M/12M/144M · +15%/rank.
3. Rajada Furtiva — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Velocidade Absoluta das Sombras — 0/2 · 5B/100B · +30%/rank.

**Knockback**
5. Impulso Ágil — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
6. Força do Deslize — 0/3 · 1M/12M/144M · +15%/rank.
7. Onda Furtiva — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Raio de detecção**
8. Alcance da Cambalhota — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
9. Trajeto Amplo — 0/3 · 1M/12M/144M · +15%/rank.
10. Deslize Abrangente — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cooldown**
11. Reflexos Rápidos — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
12. Instinto Ágil das Sombras — 0/3 · 1M/12M/144M · -8%/rank.
13. Fôlego Furtivo — 0/3 · 100M/1,5B/22,5B · -10%/rank.
14. Domínio da Esquiva — 0/2 · 5B/100B · -15%/rank.

**Mecânicas novas**
15. **Lâmina no Deslize** — 0/5 · 10/80/640/5.120/40.960 · a Cambalhota passa a causar dano
    aos monstros no caminho, %/rank (hoje só empurra).
16. Corte Veloz — 0/3 · 1M/12M/144M · +dano da Cambalhota, %/rank.
17. Fúria do Deslize — 0/3 · 100M/1,5B/22,5B · +dano, %/rank.
18. Rastro Venenoso — 0/3 · 1M/12M/144M · a Cambalhota deixa um rastro de veneno no caminho,
    %/rank de dano/segundo.
19. Fôlego Recuperado — 0/3 · 1M/12M/144M · acertar um monstro na Cambalhota reduz o cooldown
    restante, %/rank.
20. Cura na Sombra — 0/2 · 100M/1,5B · cura o Rogue em %/rank ao acertar monstros na Cambalhota.
21. Véu Pós-Deslize — 0/3 · 1M/12M/144M · imunidade continua por 0,5s/1s/1,5s depois da
    Cambalhota, curto de propósito.
22. Reflexos de Emergência — 0/2 · 100M/1,5B · abaixo de 20% HP, a próxima vez que tomar dano,
    esse dano é reduzido em %/rank.
23. Instinto de Sobrevivência — 0/2 · 100M/1,5B · abaixo de 30% HP, cooldown da Cambalhota
    -50%/rank.
24. **Última Esquiva** — 0/1 · 5T · upgrade do #23: abaixo de 10% HP, dano recebido -40%
    enquanto permanecer nesse estado crítico (nunca aciona a Cambalhota de graça).
25. Dupla Cambalhota — 0/2 · 100M/1,5B · %/rank de chance de poder usar a Cambalhota 2x em
    sequência, sem esperar o cooldown entre elas.
26. Instinto Caçador — 0/3 · 1M/12M/144M · ganho de energia da Ultimate +%/rank enquanto a
    Cambalhota estiver em cooldown.

**Gates finais**
27. Fúria Sem Fim — 0/1 · 50T · dano da Cambalhota +50%.
28. **Lâmina Giratória** — 0/1 · 500T · a Cambalhota passa a acertar cada monstro no caminho 2
    vezes ao longo do trajeto, em vez de só 1 — dobra o dano/knockback por ativação.
29. Instinto Fatal — 0/3 · 1M/12M/144M · +5% chance de crítico na Cambalhota/rank.
30. **Sombra Absoluta** — 0/1 · **1 quadrilhão** · a Cambalhota passa a conceder o mesmo efeito
    do pulso primário (dano em área) enquanto durar, combinando as duas mecânicas num só
    movimento.

---

## 23. Perks de Cima (Ultimate) — Rogue, 30 perks, fechado

Fecha as 4 abas do Rogue (Baixo compartilhado + Esquerda/Direita/Cima bespoke = 120 perks
completos pro 5º herói). Grounded na Bomba real (`bombDamageMultiplier`, `bombSpeed`,
`bombMaxDistance`, `bombExplosionRadius`) e na carga/energia específica (soma com a passiva já
existente de 4x energia do Rogue). "Mais bombas por lançamento" reaproveita o mesmo critério já
validado do `hammerCount` do Paladin — leque de ângulos, dano cheio por bomba, sem sistema novo.

**Dano**
1. Explosão Mortal — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Pólvora Refinada — 0/3 · 1M/12M/144M · +15%/rank.
3. Fúria Explosiva — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Destruição Total — 0/2 · 5B/100B · +30%/rank.
5. Bomba Suprema — 0/1 · 50T · +40%.

**Velocidade da bomba**
6. Lançamento Veloz — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
7. Arremesso Rápido — 0/3 · 1M/12M/144M · +15%/rank.
8. Velocidade Explosiva — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Alcance**
9. Alcance Explosivo — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
10. Lançamento Longo — 0/3 · 1M/12M/144M · +15%/rank.
11. Horizonte de Fogo — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Raio de explosão**
12. Raio Ampliado — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
13. Explosão Maior — 0/3 · 1M/12M/144M · +15%/rank.
14. Zona de Destruição — 0/3 · 100M/1,5B/22,5B · +20%/rank.
15. Cataclismo Sombrio — 0/2 · 5B/100B · +30%/rank.

**Carga/energia da Ultimate**
16. Fusível Rápido — 0/5 · 10/80/640/5.120/40.960 · carga necessária -5%/rank.
17. Mecha Curta — 0/3 · 1M/12M/144M · -8%/rank.
18. Instinto Explosivo — 0/5 · 10/80/640/5.120/40.960 · ganho de energia da Ultimate +10%/rank.
19. Sede de Caos — 0/3 · 1M/12M/144M · +15%/rank.

**Crítico**
20. Detonação Certeira — 0/3 · 1M/12M/144M · +5% chance de crítico/rank.
21. Explosão Perfeita — 0/3 · 100M/1,5B/22,5B · +8% chance de crítico/rank.

**Mais bombas por lançamento** (mesmo critério do `hammerCount` do Paladin)
22. **Bombas Extras** — 0/3 · 1M/12M/144M · +1 bomba/rank, lançadas num leque de ângulos a
    partir da mira, dano cheio cada.
23. Bomba Ricocheteante — 0/2 · 100M/1,5B · %/rank de chance da bomba quicar numa parede em
    vez de explodir, continuando o trajeto.
24. **Arsenal Ampliado** — 0/3 · 1M/12M/144M · +1 bomba/rank (continuação da família acima,
    mais caro).
25. Nuvem Tóxica — 0/2 · 100M/1,5B · a explosão deixa uma área residual de veneno, %/rank de
    dano/segundo por alguns segundos.
26. Instinto do Caos — 0/3 · 1M/12M/144M · ganho de energia extra ao acertar múltiplos
    monstros com a mesma explosão, %/rank.

**Gates finais**
27. **Bomba Dupla** — 0/1 · 50T · lança 2 bombas simultâneas em ângulos próximos, cada uma com
    dano cheio.
28. **Fúria Explosiva Sem Fim** — 0/1 · 500T · matar um monstro com a Ultimate remove o custo
    de energia da próxima Ultimate por alguns segundos, permitindo relançar na hora.
29. Instinto Mortal — 0/3 · 1M/12M/144M · dano extra se a bomba acertar 3+ monstros na mesma
    explosão, %/rank.
30. **Bomba do Fim** — 0/1 · **1 quadrilhão** · a cada 5 usos da Ultimate, a bomba vem com o
    triplo de raio e de dano, sem custo extra.

---

## 24. Perks de Esquerda (Ataque Primário) — Cleric, 30 perks, fechado

Sexto herói na ordem do GDD. Grounded no projétil homing real: `attackRadius`, `projectileCount`,
`projectileSpeed`, `projectileMaxDistance`, `projectileKnockbackForce`, e a passiva
`ClericDamageMultiplier` (hoje const 2x, só no hit do projétil, nunca na Oração).

**Dano**
1. Luz Penitente — 0/5 · 10/80/640/5.120/40.960 · +8%/rank.
2. Fúria Sagrada — 0/3 · 1M/12M/144M · +15%/rank.
3. Julgamento Divino — 0/3 · 100M/1,5B/22,5B · +20%/rank.
4. Castigo Supremo — 0/2 · 5B/100B · +30%/rank.
5. Ira Celestial — 0/1 · 50T · +40%.

**Raio de detecção de alvo**
6. Visão Sagrada — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
7. Olhos da Fé — 0/3 · 1M/12M/144M · +15%/rank.
8. Presença Divina — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Quantidade de projéteis** (`projectileCount`)
9. Luzes Gêmeas — 0/5 · 10/80/640/5.120/40.960 · +1 projétil/rank.
10. Clamor Sagrado — 0/3 · 1M/12M/144M · +1 projétil/rank.
11. Coro Celestial — 0/3 · 100M/1,5B/22,5B · +1 projétil/rank.
12. Multidão de Luz — 0/2 · 5B/100B · +1 projétil/rank.

**Velocidade do projétil**
13. Luz Veloz — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
14. Rajada Sagrada — 0/3 · 1M/12M/144M · +15%/rank.
15. Velocidade Celestial — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Alcance máximo do projétil**
16. Alcance Sagrado — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
17. Luz Duradoura — 0/3 · 1M/12M/144M · +15%/rank.
18. Horizonte Divino — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Knockback**
19. Toque Firme — 0/3 · 1M/12M/144M · +15%/rank.
20. Impacto Sagrado — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Cooldown real**
21. Fé Rápida — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
22. Oração Breve — 0/3 · 1M/12M/144M · -8%/rank.
23. Devoção Ágil — 0/3 · 100M/1,5B/22,5B · -10%/rank.

**Crítico**
24. Golpe Celestial — 0/3 · 1M/12M/144M · +5% chance de crítico/rank.
25. Luz Perfeita — 0/3 · 100M/1,5B/22,5B · +8% chance de crítico/rank.

**Mecânicas novas**
26. **Luz Fragmentada** — 0/3 · 1M/12M/144M · se o projétil matar o alvo, ele se divide em 2
    novos projéteis com metade do dano cada, que buscam automaticamente os próximos monstros
    vivos mais próximos (mesmo espírito da Fragmentação Sem Fim do Barbarian, só com o abate).
27. Redenção — 0/2 · 100M/1,5B · cura o Cleric em %/rank do dano causado pelo projétil.
28. Marca da Fé — 0/3 · 1M/12M/144M · o alvo atingido recebe %/rank mais dano de qualquer
    fonte por alguns segundos.

**Gates finais**
29. **Retargeting Divino** — 0/1 · 50T · se o alvo original morrer antes do projétil chegar,
    ele busca automaticamente o próximo monstro vivo mais próximo.
30. **Julgamento Final** — 0/1 · **1 quadrilhão** · a cada 5 usos, os projéteis vêm com o
    triplo de dano e perfuram todos os monstros no caminho até o alvo final.

---

## 25. Perks de Direita (Shift/Survival) — Cleric, 30 perks, fechado

Direita do Cleric é a **Reza/Cura Ativa** (`healPercentOfMaxHealth`, 4 ondas, `healFlashDuration`,
cooldown real). Hoje é só auto-cura em pulsos, sem imunidade a dano nem efeito extra nenhum —
diferente de Ranger/Druid/Rogue, Cleric NÃO tem `IsDamageImmune` no Shift hoje, então a
imunidade aqui é mecânica nova (não extensão de algo que já existe).

**Cura total**
1. Bênção Maior — 0/5 · 10/80/640/5.120/40.960 · +5%/rank.
2. Luz Curativa — 0/3 · 1M/12M/144M · +8%/rank.
3. Milagre Sagrado — 0/3 · 100M/1,5B/22,5B · +12%/rank.
4. Graça Divina — 0/2 · 5B/100B · +20%/rank.
5. Redenção Suprema — 0/1 · 50T · +30%.

**Cooldown**
6. Fé Rápida — 0/5 · 10/80/640/5.120/40.960 · -5%/rank.
7. Devoção Breve — 0/3 · 1M/12M/144M · -8%/rank.
8. Oração Ágil — 0/3 · 100M/1,5B/22,5B · -10%/rank.
9. Comunhão Instantânea — 0/2 · 5B/100B · -15%/rank.

**Velocidade das ondas**
10. Reza Veloz — 0/5 · 10/80/640/5.120/40.960 · +10%/rank.
11. Fluxo Sagrado — 0/3 · 1M/12M/144M · +15%/rank.
12. Graça Instantânea — 0/3 · 100M/1,5B/22,5B · +20%/rank.

**Mecânicas novas**
13. Purificação — 0/2 · 100M/1,5B · %/rank de chance de remover um Efeito Nocivo ativo.
14. Escudo da Fé — 0/3 · 1M/12M/144M · concede um escudo temporário, %/rank do HP máximo.
15. Fervor Sagrado — 0/3 · 1M/12M/144M · depois da Reza, dano causado +%/rank por alguns
    segundos.
16. Cura Crescente — 0/3 · 1M/12M/144M · cada onda cura %/rank mais se o HP estiver abaixo de
    50%.
17. Reflexos de Emergência — 0/2 · 100M/1,5B · abaixo de 20% HP, a próxima vez que tomar dano,
    esse dano é reduzido em %/rank.
18. Instinto de Sobrevivência — 0/2 · 100M/1,5B · abaixo de 30% HP, cooldown da Reza -50%/rank.
19. **Última Prece** — 0/1 · 5T · upgrade do #18: abaixo de 10% HP, dano recebido -40% enquanto
    permanecer nesse estado crítico.
20. **Véu da Fé** — 0/3 · 1M/12M/144M · concede imunidade a dano durante a Reza (mecânica
    nova — hoje ela não existe): rank 1 só durante as ondas de cura, rank 2 inclui a animação
    de início, rank 3 cobre a sequência inteira (início + ondas + fim).
21. Instinto Caçador — 0/3 · 1M/12M/144M · ganho de energia da Ultimate +%/rank enquanto a
    Reza estiver em cooldown.
22. Onda Extra — 0/2 · 100M/1,5B · %/rank de chance de uma 5ª onda de cura bônus.
23. Fé Inabalável — 0/3 · 1M/12M/144M · reduz o tempo de recovery pós-Reza, %/rank.
24. Luz Reconfortante — 0/2 · 100M/1,5B · cura instantânea extra ao terminar a Reza, %/rank.
25. Comunhão Perfeita — 0/2 · 100M/1,5B · reduz o custo de energia da próxima Ultimate em
    %/rank após usar a Reza.

**Gates finais**
26. Reza Dupla — 0/1 · 50T · a Reza pode ser usada 2x em sequência, sem esperar cooldown.
27. Instinto Final — 0/3 · 1M/12M/144M · +dano extra no primeiro ataque primário logo após a
    Reza, %/rank.
28. **Graça Sem Fim** — 0/1 · 500T · remove o teto de 4 ondas — a Reza continua curando
    enquanto o botão for mantido pressionado.
29. Fé Absoluta — 0/2 · 100M/1,5B · mais uma camada de cura total, %/rank.
30. **Milagre Eterno** — 0/1 · **1 quadrilhão** · a Reza cura 100% do HP máximo
    instantaneamente numa única onda, sem gastar mais energia/cooldown que o normal.

---

## 26. Perks de Cima (Ultimate) — Cleric, 30 perks, fechado

Grounded na Oração real: `prayerRadius` (6, raio de visão do Cleric), `prayerDuration` (4s,
duração do Efeito WordOfPain), `prayerTickDamageMultiplier` (0.5, fração do dano normal aplicada
por tick) e `ApplyHealWave` (reaproveitada por um dos perks). 2 mudanças de kit base, pré-
requisito pra esta árvore fazer sentido (ainda sem código — mesma pendência de implementação de
toda a árvore de Perks, ver Seção 28):

- A Oração deixa de paralisar por padrão — sem comprar **Grilhões da Fé**, ela só aplica o DoT
  espaçado; os monstros continuam andando/atacando normais entre os ticks.
- A Oração nasce com um teto de **8 monstros atingidos por cast** (hoje `AnimationPrayerCastEvent`
  acerta todo mundo no raio, sem limite) — os perks de teto abaixo sobem esse número até removê-lo.

**Paralisia**
1. **Grilhões da Fé** — 0/1 · 10 · monstros atingidos pela Oração ficam paralisados (IA/ataque/
   movimento travados) enquanto o Efeito WordOfPain durar.
2. Correntes Prolongadas — 0/3 · 1M/12M/144M · requer Grilhões da Fé; a paralisia continua por
   +0,5s/rank depois que o Efeito termina.

**Raio / duração / dano por tick**
3. Alcance Sagrado — 0/5 · 10/80/640/5.120/40.960 · +15%/rank no raio (`prayerRadius`).
4. Fé Duradoura — 0/5 · 10/80/640/5.120/40.960 · +15%/rank na duração do Efeito
   (`prayerDuration`) — também estende a paralisia, se comprada.
5. Castigo Divino — 0/5 · 10/80/640/5.120/40.960 · +10%/rank no dano de cada tick
   (`prayerTickDamageMultiplier`).
6. Fervor Crescente — 0/3 · 1M/12M/144M · cada tick consecutivo no mesmo monstro causa 10%/rank
   mais dano que o anterior (reinicia a cada novo cast).

**Cast (velocidade/recarga)**
7. Recarga Piedosa — 0/5 · 10/80/640/5.120/40.960 · +8%/rank na velocidade de recarga da
   Ultimate.
8. Preparo Sagrado — 0/5 · 10/80/640/5.120/40.960 · +10%/rank na velocidade da animação de
   lançamento.

**Teto de monstros atingidos**
9. Clamor Divino — 0/1 · 1M · teto: 8 → 16.
10. Juízo Coletivo — 0/1 · 12M · teto: 16 → 32.
11. Fúria dos Céus — 0/1 · 100M · teto: 32 → 64.
12. Ira Absoluta — 0/1 · 1,5B · teto: 64 → 128.

**Sinergias**
13. Luz Guiada — 0/3 · 100M/1,5B/22,5B · requer Grilhões da Fé; enquanto um monstro estiver
    paralisado, o projétil homing do ataque primário nele causa +15%/rank de dano.
14. Reza Unificada — 0/1 · 1M · enquanto a Oração estiver ativa em alguém, a Reza (Shift) cura
    10% mais por onda.
15. Zelo Pio — 0/1 · 100M · ao lançar a Oração, reduz o cooldown atual da Reza em 20%.

**Autossustento**
16. Mãos que Curam — 0/3 · 1M/12M/144M · ao lançar, o Cleric recupera 5%/rank da própria Vida
    Máxima.
17. Véu Sagrado — 0/3 · 100M/1,5B/22,5B · ao lançar, o Cleric ganha um escudo de 10%/rank da
    própria Vida Máxima enquanto o Efeito estiver ativo em alguém.
18. Luz Purificadora — 0/1 · 10 · ao lançar, remove 1 Efeito Nocivo que o próprio Cleric esteja
    sofrendo.
19. Fé Inquebrável — 0/3 · 100M/1,5B/22,5B · +1s/rank de imunidade a Efeitos Nocivos após lançar
    a Oração.
20. Comunhão Sagrada — 0/2 · 5B/100B · +1 onda extra de cura (reaproveita `ApplyHealWave`) por
    rank, ao lançar.

**Mecânicas novas**
21. Onda de Choque — 0/1 · 10 · ao lançar, aplica um pequeno empurrão em todos os atingidos.
22. Eco da Fé — 0/1 · 10 · monstros que morrem sob WordOfPain soltam 50% mais ouro.
23. Mão de Ferro — 0/1 · 100M · requer Grilhões da Fé; monstro paralisado perde toda resistência
    a knockback.
24. Juízo Final — 0/2 · 5B/100B · requer Grilhões da Fé; monstro paralisado que morre explode,
    reaplicando WordOfPain em quem estiver perto (+2m/rank de alcance).
25. Segundo Fôlego — 0/1 · 5B · 25% de chance de relançar a Oração automaticamente ao fim do
    cast, sem gastar carga.
26. Correntes Inquebráveis — 0/2 · 5B/100B · requer Correntes Prolongadas maxada; +1s/rank
    adicional de paralisia residual.

**Gates finais**
27. **Oração Relâmpago** — 0/1 · 100B · requer Preparo Sagrado maxado; a animação de lançamento
    fica 100% mais rápida (soma com os 50% do Preparo Sagrado, total ~2,5-3x mais rápido) — o
    Cleric ainda fica travado durante o cast, só que por bem menos tempo.
28. **Julgamento Universal** — 0/1 · 50T · remove o teto: a Oração atinge TODOS os monstros na
    tela.
29. **Comunhão Eterna** — 0/1 · 500T · requer Comunhão Sagrada maxada; cada onda de cura também
    remove 1 Efeito Nocivo do Cleric.
30. **Paralisia Eterna** — 0/1 · **1 quadrilhão** · requer Grilhões da Fé; monstro paralisado
    NÃO-boss (`EnemyController.IsBoss == false`) que chega a menos de 20% de vida morre
    instantaneamente quando o Efeito expira. Bosses recebem o DoT normal, nunca esse execute.

---

## 27. Mudança de código já aplicada fora de escopo (exceção pontual)

`HeroController.EnergyGainMultiplier` (static, default `0.1f`) — nerf de propósito na Energia de
Ultimate recebida por kill (20 → 2 de base, sem nenhum perk comprado), multiplicando por cima do
`UltimateEnergyMultiplier` já existente (passiva por-herói, ex.: 4× do Rogue). Abre espaço pra um
perk de Baixo (mundo) ou de Cima (ultimate específica) devolver esse valor aos poucos. Única
exceção de código feita durante esta fase de planejamento — o resto deste documento é só decisão,
sem implementação ainda.

---

## 28. Explicitamente fora deste documento (próxima fase)

- **Loja real** — as 3 abas (Resumo do Dia / Árvore de Perks / Talentos) e como o desmonte do Shop
  antigo entra em cada uma.
- **Árvore de Perks — restante** (Barbarian, Ranger, Mage, Druid, Rogue e Cleric 100% completos,
  120 perks cada — Seções 9-11, 12-14, 15-17, 18-20, 21-23 e 24-26; faltam os 4 heróis
  restantes: Paladin, Gunslinger, Assassin, Blood Mage). Esqueleto de 4 direções já decidido
  (Cima = Ultimate, Esquerda = Primário, Baixo = Mundo/Economia, Direita = Shift/Survival);
  Cima/Esquerda/Direita são 100% únicos por herói (sem template genérico de stats — cada um
  nasce do que aquele herói realmente faz; Baixo é o único compartilhado, já fechado pra todos).
  Regra de preço "0/5 reaparece mais caro depois" e incrementos pequenos (ex.: `+0.05` por rank)
  já validados como padrão. **Tamanho travado: 30 perks por aba** — 120 perks por herói no
  total. **Próxima aba a debater: Esquerda do Paladin (Ataque Primário).**
- **Critério de desbloqueio dos heróis** ainda travados (Assassin e os futuros Demonologist/
  Necromancer/Gambler/Plague Doctor) — fica para o final, placeholder "Em breve" até lá.
- **Sistema de HP de Boss** (ver Seção 4.1) — ainda sem decisão de UI: posição da barra (embaixo
  do timer do dia?), e o que acontece visualmente quando mais de 1 boss está na tela ao mesmo
  tempo. Precisa de sessão própria antes de implementar.
