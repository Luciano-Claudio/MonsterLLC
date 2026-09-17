# Bestiário — Fichas de Monstros e Bosses

> **Documento Especializado**, referenciado pelo GDD Mestre (Seção 22 — arquitetura de comportamento — e Seção 51 — lista de documentos especializados). Fonte: documento de visão original do jogo, migrado para cá para deixar de depender de um PDF solto. O GDD Mestre Seção 22 continua sendo a fonte de verdade da **arquitetura** de combate (categorias, comportamento padrão, exceções) que toda criatura listada aqui precisa seguir quando implementada.

## Regra padrão de combate (Sprint 16, correção — decisão final, ver GDD Seção 22) ✅

A pendência de viabilidade (timing/telegraph completo vs. simplificação estilo Vampire Survivors) passou por duas rodadas de teste na Sprint 16: primeiro o modelo completo (Telegraph/Hitbox/Recovery), depois o modelo simplificado puro (contato/auto-disparo, sem `attack` nenhum). Nenhum dos dois ficou bom — o primeiro é caro demais pra produzir sozinho, o segundo perdeu a identidade visual do ataque. A decisão final é um meio-termo, descrito abaixo.

- **Melee comum:** ataca com uma animação `attack` real (a arte que já existe), no seu próprio cooldown. Um **Animation Event** no frame do golpe ativa um **trigger direcional** fixo na frente do monstro (4 triggers, um por diagonal — o GameObject nunca vira, só a animação muda) — só causa dano se o jogador estiver dentro dele naquele instante exato; fora disso, o golpe erra. **Sem dano de contato passivo.**
- **Ranged comum:** ataca com uma animação de conjuração real, no seu próprio cooldown — o projétil só nasce quando o **Animation Event** da animação dispara, mirando a posição real do jogador naquele instante (sem telegraph). Também sem dano de contato. Se o jogador chegar perto demais, o monstro se afasta pra manter distância.
- **Animações padrão de todo monstro comum (Melee ou Ranged):** `idle` (patrulha, antes de detectar o jogador — toca até o fim, nunca é interrompida no meio para andar), `walk` (blend tree 2D direcional — NE/NW/SE/SW, ou as 8 direções quando o asset tiver), `idle_combat` (parado depois de detectar o jogador, entre um golpe/disparo e outro; blend tree simples de 4 direções), `attack` (com Animation Event), `damage`, `die`. Ver GDD Seção 22 ("Idle de patrulha vs. `idle_combat`") pra regra completa do `idle`/`idle_combat`.
- **Variante de idle "emboscada"** (Gargoyle, Skeleton, Headless Skeleton, Skeletal Horse, Skeleton Mage, Skeleton Minotaur, Skeleton Rider e Skeleton Warrior — todos do Andar 5): em vez do `idle` de patrulha (blend tree direcional, vagando pelo andar), fica parado numa única pose estática **não-direcional** até detectar o jogador. Ao detectar, toca `activate` (clipe não-direcional, 1x) antes de entrar em `walk`/`idle_combat` normalmente — usa um Animator base à parte (`Base_Melee_Ambush`/`Base_Ranged_Ambush`), já que o `Idle` deixa de ser Blend Tree. É a **única exceção do jogo em que a detecção pode reverter**: se o jogador sair do raio de observação, volta pra pose parada instantaneamente, **sem** animação de transição (o jogador não estaria nem olhando pra ele nesse instante — só o resto do Bestiário persegue pra sempre depois de detectar). Fora essa diferença de ativação/desativação, ataque, dano e morte seguem 100% o padrão Melee/Ranged comum.
- **Direção de movimento ≠ direção de mira:** `MoveX`/`MoveY` alimentam o `walk` (podem apontar pra longe do jogador, ex.: Ranged fugindo); `AimX`/`AimY` alimentam `attack`/`idle_combat` e sempre apontam pro jogador de verdade, recalculados a cada frame de combate.
- **Única exceção permanente: Slimes** (Slime Green/Blue comuns e Mother Slime Green/Blue) — ficam só no dano de contato, sem `attack`, pra sempre. Nenhum outro monstro comum ou boss simplificado tem esse tratamento.
- **Attack Budget (GDD Seção 14) foi removido** — não existe mais limite de quantos monstros atacam ao mesmo tempo; população (GDD Seção 23) continua sendo o único controle de quantos monstros existem.
- **Exceções com arquitetura própria além do padrão acima, cada uma na própria ficha:** Goblin Sapper (Andar 1), Orc Shaman (Andar 3, só o totem — o Shaman em si já segue o padrão), Burning Skull (Andar 6), Serpent (Andar 9), Bicephalous (Andar 9, só o projétil é especial), Skeleton Mage e Zombie Mage (Andar 5, ataque vira uma área de conjuração no chão em vez do projétil vivo padrão do Ranged — ver as próprias fichas). Skeleton Rider não tem mais exceção de morte (deixou de gerar 2 monstros ao morrer), mas ainda entra na variante de idle "emboscada" acima. Todo o resto do Bestiário segue a regra padrão acima.
- **Regra de reação a dano (GDD Seção 22): receber dano ≠ reagir visualmente ≠ interromper uma ação.** Comprometido com a animação `attack`, o dano nunca cancela ela — só um flash leve, sem trocar de animação (não existe transição `Attack → Damage`). Fora do ataque, dano toca a animação `damage` normalmente.
- Os valores de drop são valores-base por abate, antes de bônus de run, employees ou multiplicadores futuros.
- Itens liberados em andares inferiores continuam disponíveis nos andares superiores.
- Cada item de drop faz sua própria rolagem de chance, independente dos outros — um único monstro pode dropar vários tipos de item ao mesmo tempo (GDD Seção 38).
- 🔢 Todos os valores de Dano/Vida estimados são referência do documento original — sujeitos a ajuste em playtest, não são valores finais travados.
- O campo "Ataque" de cada ficha comum descreve o mecanismo real (golpe/disparo real via Animation Event, ou contato normal só pros Slimes), não mais uma ação com nome próprio ("morde", "arranha", "espadada") — esse tipo de flavor agora vive só no nome do monstro e na sua "Função no combate".

---

## Monstros Comuns

### Andar 1

#### Rat
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Wolf
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 8-30 | Chance: 100%
  - Monster Fragment: 2-8 | Chance: 70%
  - Spirit Dust: 2-4 | Chance: 50%
  - Arcane Shard: 2 | Chance: 2%
  - Dark Crystal: 2 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 3 | **🔢 Vida estimada:** 10

#### Bat
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Slime Green
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Dano por contato normal (cooldown próprio) — pula e causa dano ao encostar, **sem animação de ataque dedicada, único caso do jogo além das exceções nomeadas.** Fica assim pra sempre, mesmo com o resto do Melee tendo voltado a ter `attack` real.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Slime Blue
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Dano por contato normal (cooldown próprio) — pula e causa dano ao encostar, **sem animação de ataque dedicada, único caso do jogo além das exceções nomeadas.** Fica assim pra sempre, mesmo com o resto do Melee tendo voltado a ter `attack` real.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Goblin
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 8-30 | Chance: 100%
  - Monster Fragment: 2-8 | Chance: 70%
  - Spirit Dust: 2-4 | Chance: 50%
  - Arcane Shard: 2 | Chance: 2%
  - Dark Crystal: 2 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 3 | **🔢 Vida estimada:** 10

#### Goblin Raider
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma tocha em direção ao player, essa tocha explode ao contato (ou quando chega no limite de distância que ela percorre) causando um pequeno dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra tocha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis
- **Drops:**
  - Monster Essence: 10-35 | Chance: 100%
  - Monster Fragment: 2-10 | Chance: 70%
  - Spirit Dust: 2-5 | Chance: 50%
  - Arcane Shard: 2 | Chance: 2%
  - Dark Crystal: 2 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 4 | **🔢 Vida estimada:** 8

#### Goblin Sapper — **Exceção à regra padrão** 🟡
- **Tipo:** Melee especial (não segue contato simples — tem ciclo de arma/fuga com objeto no mundo)
- **Movimentação:** Corre em direção ao jogador usando `Bomb_Walk` enquanto está armado; ao soltar a bomba, foge do jogador usando `Walk` (sem bomba), mantendo-se num raio visível até recarregar.
- **Comportamento (ciclo completo):**
  1. Detecta o jogador → persegue com `Bomb_Walk` até ficar em alcance corpo a corpo.
  2. Ao ficar corpo a corpo: cria uma bomba nos pés do jogador (objeto próprio no mundo, com seu próprio `Animator`) e entra em `Walk` (sem bomba).
  3. Foge do jogador, andando de um lado pro outro dentro de um raio de segurança, até o cooldown de recarga terminar.
  4. Recarrega → volta pro passo 1 (`Bomb_Walk`).
  5. Se o jogador encostar nele em qualquer momento (armado ou não): **body damage normal** (contato + cooldown, igual a qualquer Melee comum) — a bomba não substitui isso, soma-se a ela.
- **Bomba (objeto separado, não é o Goblin Sapper):** nasce nos pés do jogador com sua própria animação de "carregando/prestes a explodir". Tem um **Animation Event** no frame da explosão — se o jogador ainda estiver dentro do círculo de dano naquele frame exato, sofre dano em área. O jogador pode se afastar da bomba antes desse frame e evitar o dano completamente — é a única janela de esquiva real do Goblin Sapper.
- **Ao morrer:** a própria animação `Die` também tem um Animation Event de explosão em área (auto-explosão), com seu próprio círculo de dano — igual em espírito à explosão da bomba, só que centrada no próprio Goblin Sapper no momento da morte.
- **Função no combate:** Inimigo de pressão com ameaça em duas camadas (contato direto + área temporizada no chão) — obriga o jogador a gerenciar posição em relação a um perigo que continua existindo depois que o Sapper já fugiu.
- **Drops:**
  - Monster Essence: 12-40 | Chance: 100%
  - Monster Fragment: 3-10 | Chance: 70%
  - Spirit Dust: 2-5 | Chance: 50%
  - Arcane Shard: 2 | Chance: 2%
  - Dark Crystal: 2 | Chance: 0,5%
- **Animações necessárias (Goblin Sapper):** `idle`, `idle_bomb`, `walk`, `bomb_walk`, `damage`, `bomb_damage`, `die` (7 animações).
- **Animações necessárias (Bomba, objeto separado):** animação de carga/explosão com Animation Event no frame de dano.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado (contato):** 6 | **🔢 Dano estimado (explosão da bomba/morte):** 🔢 pendente de balanceamento, deve ser maior que o de contato | **🔢 Vida estimada:** 8

---

### Andar 2

#### Rat People
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma pedra em direção ao player, essa pedra explode ao contato (ou quando chega no limite de distância que ela percorre) causando um pequeno dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra pedra novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 30-120 | Chance: 100%
  - Monster Fragment: 5-20 | Chance: 85%
  - Spirit Dust: 3-10 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 6 | **🔢 Vida estimada:** 30

#### Centaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 38-150 | Chance: 100%
  - Monster Fragment: 6-25 | Chance: 85%
  - Spirit Dust: 4-12 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 7 | **🔢 Vida estimada:** 35

#### Minotaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 45-180 | Chance: 100%
  - Monster Fragment: 8-30 | Chance: 85%
  - Spirit Dust: 5-15 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 10 | **🔢 Vida estimada:** 40

#### Gnoll
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 38-150 | Chance: 100%
  - Monster Fragment: 6-25 | Chance: 85%
  - Spirit Dust: 4-12 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 7 | **🔢 Vida estimada:** 35

#### Spider
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 30-120 | Chance: 100%
  - Monster Fragment: 5-20 | Chance: 85%
  - Spirit Dust: 3-10 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6 | **🔢 Vida estimada:** 30

#### Ancient Troll
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 50-200 | Chance: 100%
  - Monster Fragment: 10-35 | Chance: 85%
  - Spirit Dust: 6-18 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 12 | **🔢 Vida estimada:** 50

---

### Andar 3

#### Warg
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 150-700 | Chance: 100%
  - Monster Fragment: 30-120 | Chance: 95%
  - Spirit Dust: 20-80 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 15 | **🔢 Vida estimada:** 100

#### Orc Blade
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 170-760 | Chance: 100%
  - Monster Fragment: 34-130 | Chance: 95%
  - Spirit Dust: 22-85 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 18 | **🔢 Vida estimada:** 110

#### Orc Rider
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar. Após morrer, aparecerá um warg e um orc blade.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 220-980 | Chance: 100%
  - Monster Fragment: 44-168 | Chance: 95%
  - Spirit Dust: 28-112 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 25 | **🔢 Vida estimada:** 130

#### Orc Shaman — **Exceção à regra padrão** 🟡 *(o Shaman em si usa a animação de conjuração padrão de qualquer Ranged — a exceção está no que o Animation Event produz: um totem, não um projétil)*
- **Tipo:** Suporte/Ranged especial (não lança projétil — planta totens no chão)
- **Movimentação:** Persegue o jogador (ou reposiciona em relação a um aliado ferido) tentando ficar num raio onde consiga plantar um totem no alvo certo. Enquanto não tiver alvo válido, anda aleatoriamente.
- **Comportamento:** Igual a um Ranged comum — mantém distância, toca a animação `attack`/conjuração real no seu próprio cooldown. A diferença é o que o **Animation Event** dessa animação produz: em vez de instanciar um projétil, instancia um **totem** (prefab separado, com seu próprio `Animator`) na posição do alvo escolhido.
- **Escolha de alvo/totem, nesta ordem de prioridade a cada disparo do cooldown:**
  1. **Totem de Heal** — se existir alguma unidade inimiga (incluindo ele mesmo) com vida < 100% e o Heal não estiver no próprio cooldown dele, planta o totem nos pés dela.
  2. **Totem de Fire** — senão, se o Fire não estiver em cooldown, planta nos pés do jogador (dano).
  3. **Totem de Ice** — senão, se o Ice não estiver em cooldown, planta nos pés do jogador (stun por alguns segundos).
  4. Se os 3 estiverem em cooldown, o Orc Shaman simplesmente não age nesse ciclo (sem penalidade, só espera o próximo).
- **Totens (prefab próprio, separado do Orc Shaman, cada um com seu próprio cooldown independente):** só possuem `totem_appear` (o totem surgindo no chão) e `totem_disappear` (o totem sumindo ao fim da vida útil dele). O efeito de verdade — cura (Heal), dano (Fire) ou stun (Ice) — dispara por um **Animation Event dentro de `totem_disappear`**, checando quem está dentro do círculo trigger daquele totem **nesse instante**, não no instante em que o totem apareceu. Isso significa dá pra desviar saindo do círculo antes do totem sumir. *(🟡 assumindo que o evento fica em `totem_disappear`, não em `totem_appear` — sinalizar se a intenção for outra)*
- **Função no combate:** Inimigo de pressão (Fire/Ice) e inimigo de suporte (Heal), criado pra punir o jogador que ignora os monstros de suporte e foca só nos que causam dano direto.
- **Drops:**
  - Monster Essence: 180-800 | Chance: 100%
  - Monster Fragment: 36-140 | Chance: 95%
  - Spirit Dust: 24-90 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias (Orc Shaman):** `idle`, `walk`, `idle_combat`, `attack`/`cast` (com Animation Event — spawna o totem, não um projétil), `damage`, `die` — igual ao padrão de qualquer Ranged comum.
- **Animações necessárias (cada Totem — Heal/Fire/Ice, prefab próprio):** `totem_appear`, `totem_disappear` (com o Animation Event do efeito — ver acima).
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado (Fire):** 15 | **🔢 Cura estimada (Heal):** 🔢 pendente | **🔢 Duração do stun (Ice):** 🔢 pendente | **🔢 Vida estimada:** 100

#### Warbreed Blade
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 190-850 | Chance: 100%
  - Monster Fragment: 38-145 | Chance: 95%
  - Spirit Dust: 25-95 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 20 | **🔢 Vida estimada:** 120

#### Warbreed Berserker
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 240-1.120 | Chance: 100%
  - Monster Fragment: 48-192 | Chance: 95%
  - Spirit Dust: 32-128 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 30 | **🔢 Vida estimada:** 150

#### Warbreed Phalanx
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque (que é mais alto um pouco pq ele possui uma lança). Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 220-1.000 | Chance: 100%
  - Monster Fragment: 44-175 | Chance: 95%
  - Spirit Dust: 30-115 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 25 | **🔢 Vida estimada:** 160

#### Orc Scout
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha em direção ao player. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 160-720 | Chance: 100%
  - Monster Fragment: 32-125 | Chance: 95%
  - Spirit Dust: 21-82 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 18 | **🔢 Vida estimada:** 100

#### Warbreed Arbalist
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha em direção ao player. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 190-850 | Chance: 100%
  - Monster Fragment: 38-145 | Chance: 95%
  - Spirit Dust: 25-95 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 20 | **🔢 Vida estimada:** 110

---

### Andar 4

#### Acolyte
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente pelo andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo melee básico do andar, criado para pressionar o jogador e ocupar espaço durante os combates.
- **Drops:**
  - Monster Essence: 800-4.000 | Chance: 100%
  - Monster Fragment: 150-800 | Chance: 100%
  - Spirit Dust: 100-600 | Chance: 95%
  - Arcane Shard: 10-50 | Chance: 35%
  - Dark Crystal: 5-25 | Chance: 18%
  - Soul Fragment: 2-10 | Chance: 10%
  - Corrupted Core: 1-5 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** `idle`, `walk`, `idle_combat`, `damage`, `die`, `possession` (com Animation Event — ver Dark Channeler, Andar 4 Bosses), `idle_possession`, `walk_possession`, `idle_combat_possession`, `damage_possession`, `die_possession` — 11 no total. A transformação em Dark Cultist não destrói/instancia um novo objeto: é o mesmo GameObject trocando de sprites/Animator e de vida/dano pros valores da forma possuída, no fim da animação `possession`.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 70 | **🔢 Vida estimada:** 500

#### Zealot
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato. Enquanto não detectar o jogador, fica andando aleatoriamente pelo andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo. Perdeu o salto com dano em área ao aterrissar — simplificado por completo.
- **Função no combate:** Inimigo de pressão e controle de área, criado para obrigar o jogador a abandonar posições e continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.120-5.600 | Chance: 100%
  - Monster Fragment: 210-1.120 | Chance: 100%
  - Spirit Dust: 140-840 | Chance: 95%
  - Arcane Shard: 14-70 | Chance: 35%
  - Dark Crystal: 7-35 | Chance: 18%
  - Soul Fragment: 3-14 | Chance: 10%
  - Corrupted Core: 1-7 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** `idle`, `walk`, `idle_combat`, `damage`, `die`, `possession` (com Animation Event — ver Dark Channeler, Andar 4 Bosses), `idle_possession`, `walk_possession`, `idle_combat_possession`, `damage_possession`, `die_possession` — 11 no total. A transformação em Dark Abomination não destrói/instancia um novo objeto: é o mesmo GameObject trocando de sprites/Animator e de vida/dano pros valores da forma possuída, no fim da animação `possession`.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 100 | **🔢 Vida estimada:** 600

#### Hound
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato. Enquanto não detectar o jogador, fica andando aleatoriamente pelo andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo rápido de pressão, criado para reduzir o espaço de reação do jogador e dificultar reposicionamentos.
- **Drops:**
  - Monster Essence: 880-4.400 | Chance: 100%
  - Monster Fragment: 165-880 | Chance: 100%
  - Spirit Dust: 110-660 | Chance: 95%
  - Arcane Shard: 11-55 | Chance: 35%
  - Dark Crystal: 6-28 | Chance: 18%
  - Soul Fragment: 2-11 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** `idle`, `walk`, `idle_combat`, `damage`, `die`, `possession` (com Animation Event — ver Dark Channeler, Andar 4 Bosses), `idle_possession`, `walk_possession`, `idle_combat_possession`, `damage_possession`, `die_possession` — 11 no total. A transformação em Dark Hound não destrói/instancia um novo objeto: é o mesmo GameObject trocando de sprites/Animator e de vida/dano pros valores da forma possuída, no fim da animação `possession`.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 80 | **🔢 Vida estimada:** 520

#### Devoted Blade
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente pelo andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo melee de pressão com área frontal maior, criado para punir jogadores que permanecem próximos por muito tempo.
- **Drops:**
  - Monster Essence: 1.040-5.200 | Chance: 100%
  - Monster Fragment: 195-1.040 | Chance: 100%
  - Spirit Dust: 130-780 | Chance: 95%
  - Arcane Shard: 13-65 | Chance: 35%
  - Dark Crystal: 6-32 | Chance: 18%
  - Soul Fragment: 3-13 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 95 | **🔢 Vida estimada:** 620

#### Devoted Sentinel
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se e tenta permanecer no alcance de sua lança, que é levemente maior que o alcance melee convencional. Enquanto não detectar o jogador, fica andando aleatoriamente pelo andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de controle de espaço, criado para ameaçar o jogador antes que ele consiga entrar confortavelmente em alcance corpo a corpo.
- **Drops:**
  - Monster Essence: 1.120-5.600 | Chance: 100%
  - Monster Fragment: 210-1.120 | Chance: 100%
  - Spirit Dust: 140-840 | Chance: 95%
  - Arcane Shard: 14-70 | Chance: 35%
  - Dark Crystal: 7-35 | Chance: 18%
  - Soul Fragment: 3-14 | Chance: 10%
  - Corrupted Core: 1-7 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 90 | **🔢 Vida estimada:** 700

#### Devoted Stalker
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador e tenta manter distância suficiente para utilizar seu ataque ranged.
- **Comportamento:** Ao detectar o jogador, aproxima-se até alcançar uma distância adequada para atacar e tenta manter esse espaçamento. Enquanto não detectar o jogador, fica andando aleatoriamente pelo andar.
- **Ataque:** Dispara uma flecha na direção do jogador. Após atacar, possui um pequeno intervalo antes de poder disparar novamente.
- **Função no combate:** Inimigo ranged de pressão, criado para obrigar o jogador a se movimentar enquanto enfrenta os inimigos melee do andar.
- **Drops:**
  - Monster Essence: 960-4.800 | Chance: 100%
  - Monster Fragment: 180-960 | Chance: 100%
  - Spirit Dust: 120-720 | Chance: 95%
  - Arcane Shard: 12-60 | Chance: 35%
  - Dark Crystal: 6-30 | Chance: 18%
  - Soul Fragment: 2-12 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 85 | **🔢 Vida estimada:** 520

---

### Transformações exclusivas do Dark Channeler

Estas não são unidades separadas com spawn próprio — **é o mesmo Acolyte/Hound/Zealot** que já estava em cena, mutando em lugar (mesmo GameObject, nunca destruído/reinstanciado). No fim da animação `possession` daquele indivíduo, um Animation Event troca as sprites (Animator) e os valores de vida/dano do componente para os da forma possuída abaixo. As fichas a seguir descrevem o resultado dessa troca, não uma entidade nova sendo criada do zero.

#### Dark Cultist
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Forma possuída de um Acolyte — mesmo GameObject, sprites e stats trocados no fim da animação `possession` (ver Acolyte, Andar 4 comum, e Dark Channeler, Andar 4 Bosses). Após a troca, persegue agressivamente o jogador e tenta permanecer em alcance de ataque.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Unidade fortalecida criada pelo Dark Channeler para aumentar rapidamente a pressão da luta.
- **Drops:**
  - *Nenhum. É o mesmo Acolyte possuído — se ainda não morreu como Acolyte antes de ser transformado, dropa como Acolyte ao morrer nesta forma; a transformação em si não gera loot adicional.*
- **Animações necessárias:** `idle_possession`, `walk_possession`, `idle_combat_possession`, `damage_possession`, `die_possession` (ver conjunto completo na ficha do Acolyte, Andar 4 comum).
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **Andar estimado:** 4 — transformação exclusiva do Dark Channeler
- **🔢 Dano estimado:** 110 | **🔢 Vida estimada:** 750

#### Dark Hound
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Forma possuída de um Hound — mesmo GameObject, sprites e stats trocados no fim da animação `possession` (ver Hound, Andar 4 comum, e Dark Channeler, Andar 4 Bosses). Após a troca, persegue agressivamente o jogador e tenta permanecer em alcance de mordida.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Unidade rápida fortalecida, criada para perseguir o jogador durante a luta contra o Dark Channeler.
- **Drops:**
  - *Nenhum. É o mesmo Hound possuído — se ainda não morreu como Hound antes de ser transformado, dropa como Hound ao morrer nesta forma; a transformação em si não gera loot adicional.*
- **Animações necessárias:** `idle_possession`, `walk_possession`, `idle_combat_possession`, `damage_possession`, `die_possession` (ver conjunto completo na ficha do Hound, Andar 4 comum).
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **Andar estimado:** 4 — transformação exclusiva do Dark Channeler
- **🔢 Dano estimado:** 120 | **🔢 Vida estimada:** 700

#### Dark Abomination
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Forma possuída de um Zealot — mesmo GameObject, sprites e stats trocados no fim da animação `possession` (ver Zealot, Andar 4 comum, e Dark Channeler, Andar 4 Bosses). Após a troca, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Transformação pesada do Dark Channeler, criada para ocupar espaço e aumentar a ameaça melee durante a luta.
- **Drops:**
  - *Nenhum. É o mesmo Zealot possuído — se ainda não morreu como Zealot antes de ser transformado, dropa como Zealot ao morrer nesta forma; a transformação em si não gera loot adicional.*
- **Animações necessárias:** `idle_possession`, `walk_possession`, `idle_combat_possession`, `damage_possession`, `die_possession` (ver conjunto completo na ficha do Zealot, Andar 4 comum).
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **Andar estimado:** 4 — transformação exclusiva do Dark Channeler
- **🔢 Dano estimado:** 150 | **🔢 Vida estimada:** 1.000

---

### Andar 5

#### Skeleton
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5.000-30.000 | Chance: 100%
  - Monster Fragment: 1.000-8.000 | Chance: 100%
  - Spirit Dust: 800-5.000 | Chance: 100%
  - Arcane Shard: 100-700 | Chance: 60%
  - Dark Crystal: 50-300 | Chance: 35%
  - Soul Fragment: 20-100 | Chance: 20%
  - Corrupted Core: 5-30 | Chance: 8%
  - Elemental Shard: 1-5 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 800 | **🔢 Vida estimada:** 2.200

#### Headless Skeleton
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5.500-33.000 | Chance: 100%
  - Monster Fragment: 1.100-8.800 | Chance: 100%
  - Spirit Dust: 880-5.500 | Chance: 100%
  - Arcane Shard: 110-770 | Chance: 60%
  - Dark Crystal: 55-330 | Chance: 35%
  - Soul Fragment: 22-110 | Chance: 20%
  - Corrupted Core: 6-33 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 2.200

#### Jumping Skull
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5.000-30.000 | Chance: 100%
  - Monster Fragment: 1.000-8.000 | Chance: 100%
  - Spirit Dust: 800-5.000 | Chance: 100%
  - Arcane Shard: 100-700 | Chance: 60%
  - Dark Crystal: 50-300 | Chance: 35%
  - Soul Fragment: 20-100 | Chance: 20%
  - Corrupted Core: 5-30 | Chance: 8%
  - Elemental Shard: 1-5 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 1.760

#### Skeletal Horse
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 6.250-37.500 | Chance: 100%
  - Monster Fragment: 1.250-10.000 | Chance: 100%
  - Spirit Dust: 1.000-6.250 | Chance: 100%
  - Arcane Shard: 125-875 | Chance: 60%
  - Dark Crystal: 62-375 | Chance: 35%
  - Soul Fragment: 25-125 | Chance: 20%
  - Corrupted Core: 6-38 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 960 | **🔢 Vida estimada:** 2.860

#### Skeleton Rider
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição. **Simplificação registrada aqui:** perdeu a mecânica de gerar 1 Skeleton + 1 Skeletal Horse ao morrer — hoje é um Melee comum como qualquer outro, sem efeito especial de morte.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 6.500-39.000 | Chance: 100%
  - Monster Fragment: 1.300-10.400 | Chance: 100%
  - Spirit Dust: 1.040-6.500 | Chance: 100%
  - Arcane Shard: 130-910 | Chance: 60%
  - Dark Crystal: 65-390 | Chance: 35%
  - Soul Fragment: 26-130 | Chance: 20%
  - Corrupted Core: 6-39 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.040 | **🔢 Vida estimada:** 3.080

#### Skeleton Minotaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 7.500-45.000 | Chance: 100%
  - Monster Fragment: 1.500-12.000 | Chance: 100%
  - Spirit Dust: 1.200-7.500 | Chance: 100%
  - Arcane Shard: 150-1.050 | Chance: 60%
  - Dark Crystal: 75-450 | Chance: 35%
  - Soul Fragment: 30-150 | Chance: 20%
  - Corrupted Core: 8-45 | Chance: 8%
  - Elemental Shard: 2-8 | Chance: 2%
  - Ancient Fragment: 2 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.120 | **🔢 Vida estimada:** 3.960

#### Zombie Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque (que é mais alto um pouco pq ele possui uma lança). Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 6.000-36.000 | Chance: 100%
  - Monster Fragment: 1.200-9.600 | Chance: 100%
  - Spirit Dust: 960-6.000 | Chance: 100%
  - Arcane Shard: 120-840 | Chance: 60%
  - Dark Crystal: 60-360 | Chance: 35%
  - Soul Fragment: 24-120 | Chance: 20%
  - Corrupted Core: 6-36 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 960 | **🔢 Vida estimada:** 2.860

#### Zombie Bear
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 7.500-45.000 | Chance: 100%
  - Monster Fragment: 1.500-12.000 | Chance: 100%
  - Spirit Dust: 1.200-7.500 | Chance: 100%
  - Arcane Shard: 150-1.050 | Chance: 60%
  - Dark Crystal: 75-450 | Chance: 35%
  - Soul Fragment: 30-150 | Chance: 20%
  - Corrupted Core: 8-45 | Chance: 8%
  - Elemental Shard: 2-8 | Chance: 2%
  - Ancient Fragment: 2 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.040 | **🔢 Vida estimada:** 3.960

#### Zombie Minotaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 8.000-48.000 | Chance: 100%
  - Monster Fragment: 1.600-12.800 | Chance: 100%
  - Spirit Dust: 1.280-8.000 | Chance: 100%
  - Arcane Shard: 160-1.120 | Chance: 60%
  - Dark Crystal: 80-480 | Chance: 35%
  - Soul Fragment: 32-160 | Chance: 20%
  - Corrupted Core: 8-48 | Chance: 8%
  - Elemental Shard: 2-8 | Chance: 2%
  - Ancient Fragment: 2 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.200 | **🔢 Vida estimada:** 4.400

#### Ghost
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5.000-30.000 | Chance: 100%
  - Monster Fragment: 1.000-8.000 | Chance: 100%
  - Spirit Dust: 800-5.000 | Chance: 100%
  - Arcane Shard: 100-700 | Chance: 60%
  - Dark Crystal: 50-300 | Chance: 35%
  - Soul Fragment: 20-100 | Chance: 20%
  - Corrupted Core: 5-30 | Chance: 8%
  - Elemental Shard: 1-5 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 1.760

#### Skeleton Archer
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha em direção ao player. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 5.000-30.000 | Chance: 100%
  - Monster Fragment: 1.000-8.000 | Chance: 100%
  - Spirit Dust: 800-5.000 | Chance: 100%
  - Arcane Shard: 100-700 | Chance: 60%
  - Dark Crystal: 50-300 | Chance: 35%
  - Soul Fragment: 20-100 | Chance: 20%
  - Corrupted Core: 5-30 | Chance: 8%
  - Elemental Shard: 1-5 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 1.650

#### Skeleton Mage — **Exceção à regra padrão de ataque** 🟡
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em um range onde conseguirá atacar o player com sua magia. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição.
- **Ataque:** **Voltou a ser uma área no chão** (deixou de ser projétil de contato direto). Ao entrar em alcance, toca uma animação de conjuração (`cast`) com um Animation Event que faz nascer um prefab de área embaixo do player, na posição dele naquele instante exato (não é mirado, não persegue depois). Esse prefab tem sua própria animação de aviso, com seu próprio Animation Event: só causa dano se o player ainda estiver dentro do trigger dele naquele frame — dá tempo de fugir do círculo antes do estouro.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para sair da área antes dela estourar.
- **Drops:**
  - Monster Essence: 6.000-36.000 | Chance: 100%
  - Monster Fragment: 1.200-9.600 | Chance: 100%
  - Spirit Dust: 960-6.000 | Chance: 100%
  - Arcane Shard: 120-840 | Chance: 60%
  - Dark Crystal: 60-360 | Chance: 35%
  - Soul Fragment: 24-120 | Chance: 20%
  - Corrupted Core: 6-36 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, cast (com Animation Event que instancia o prefab de área), damage e die. *(o prefab de área tem sua própria animação de aviso + Animation Event de dano — não faz parte do conjunto de clipes deste monstro)*
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.080 | **🔢 Vida estimada:** 1.540

#### Zombie Archer
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha em direção ao player. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 5.500-33.000 | Chance: 100%
  - Monster Fragment: 1.100-8.800 | Chance: 100%
  - Spirit Dust: 880-5.500 | Chance: 100%
  - Arcane Shard: 110-770 | Chance: 60%
  - Dark Crystal: 55-330 | Chance: 35%
  - Soul Fragment: 22-110 | Chance: 20%
  - Corrupted Core: 6-33 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 920 | **🔢 Vida estimada:** 1.760

#### Zombie Mage — **Exceção à regra padrão de ataque** 🟡
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde conseguirá atacar o player com sua magia. Enquanto não detectar o jogador, fica andando aleatoriamente no andar. *(sem a variante de idle "emboscada" — só o Skeleton Mage tem esse comportamento no Andar 5.)*
- **Ataque:** **Voltou a ser uma área no chão** (deixou de ser projétil de contato direto). Ao entrar em alcance, toca uma animação de conjuração (`cast`) com um Animation Event que faz nascer um prefab de área embaixo do player, na posição dele naquele instante exato (não é mirado, não persegue depois). Esse prefab tem sua própria animação de aviso, com seu próprio Animation Event: só causa dano se o player ainda estiver dentro do trigger dele naquele frame — dá tempo de fugir do círculo antes do estouro.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para sair da área antes dela estourar.
- **Drops:**
  - Monster Essence: 6.250-37.500 | Chance: 100%
  - Monster Fragment: 1.250-10.000 | Chance: 100%
  - Spirit Dust: 1.000-6.250 | Chance: 100%
  - Arcane Shard: 125-875 | Chance: 60%
  - Dark Crystal: 62-375 | Chance: 35%
  - Soul Fragment: 25-125 | Chance: 20%
  - Corrupted Core: 6-38 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, cast (com Animation Event que instancia o prefab de área), damage e die. *(o prefab de área tem sua própria animação de aviso + Animation Event de dano — não faz parte do conjunto de clipes deste monstro)*
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.120 | **🔢 Vida estimada:** 1.650

#### Flying Skull
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 6.000-36.000 | Chance: 100%
  - Monster Fragment: 1.200-9.600 | Chance: 100%
  - Spirit Dust: 960-6.000 | Chance: 100%
  - Arcane Shard: 120-840 | Chance: 60%
  - Dark Crystal: 60-360 | Chance: 35%
  - Soul Fragment: 24-120 | Chance: 20%
  - Corrupted Core: 6-36 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Enchanted_Companions_v1.0
- **🔢 Dano estimado:** 1.000 | **🔢 Vida estimada:** 1.760

#### Gargoyle
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única, como se fosse parte do cenário da sala, até detectar o jogador. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta a ficar parado instantaneamente, sem animação de transição.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 7.000-42.000 | Chance: 100%
  - Monster Fragment: 1.400-11.200 | Chance: 100%
  - Spirit Dust: 1.120-7.000 | Chance: 100%
  - Arcane Shard: 140-980 | Chance: 60%
  - Dark Crystal: 70-420 | Chance: 35%
  - Soul Fragment: 28-140 | Chance: 20%
  - Corrupted Core: 7-42 | Chance: 8%
  - Elemental Shard: 1-7 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 1.120 | **🔢 Vida estimada:** 3.300

#### Wraith
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo. Perdeu o duplo-hit de ida e volta da espadada — simplificado por completo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 7.500-45.000 | Chance: 100%
  - Monster Fragment: 1.500-12.000 | Chance: 100%
  - Spirit Dust: 1.200-7.500 | Chance: 100%
  - Arcane Shard: 150-1.050 | Chance: 60%
  - Dark Crystal: 75-450 | Chance: 35%
  - Soul Fragment: 30-150 | Chance: 20%
  - Corrupted Core: 8-45 | Chance: 8%
  - Elemental Shard: 2-8 | Chance: 2%
  - Ancient Fragment: 2 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 1.440 | **🔢 Vida estimada:** 2.420

#### Mummy
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5.500-33.000 | Chance: 100%
  - Monster Fragment: 1.100-8.800 | Chance: 100%
  - Spirit Dust: 880-5.500 | Chance: 100%
  - Arcane Shard: 110-770 | Chance: 60%
  - Dark Crystal: 55-330 | Chance: 35%
  - Soul Fragment: 22-110 | Chance: 20%
  - Corrupted Core: 6-33 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 800 | **🔢 Vida estimada:** 2.640

#### Burning Skull — **Exceção à regra padrão** 🟡
- **Tipo:** Melee suicida (não tem contato normal — a aproximação em si é a ameaça)
- **Movimentação:** Persegue diretamente o jogador (`Walk`).
- **Comportamento:** Ao entrar no raio de contato do jogador, vai **instantaneamente** para a animação `Die` — não tem `Idle` (está sempre perseguindo ou já explodindo, nunca parado) nem `Damage` separado de reação a golpe (se for morto por dano antes de chegar perto, também vai direto pra `Die`).
- **Explosão:** a própria animação `Die` tem um **Animation Event** no frame da explosão — um círculo trigger nasce ali; se o jogador estiver dentro dele naquele frame exato, sofre o dano. O Burning Skull morre de qualquer forma ao terminar essa animação (seja por ter explodido perto do jogador, seja por ter sido morto por dano antes).
- **Função no combate:** Ameaça de contato instantâneo — pune ficar perto sem cuidado, mas dá o mesmo aviso visual (a corrida final + a própria animação de explosão) que qualquer outro telegraph.
- **Drops:**
  - Monster Essence: 6.500-39.000 | Chance: 100%
  - Monster Fragment: 1.300-10.400 | Chance: 100%
  - Spirit Dust: 1.040-6.500 | Chance: 100%
  - Arcane Shard: 130-910 | Chance: 60%
  - Dark Crystal: 65-390 | Chance: 35%
  - Soul Fragment: 26-130 | Chance: 20%
  - Corrupted Core: 6-39 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** `walk`, `damage` (só usada se levar dano à distância antes de chegar perto — GDD Seção 22, dano ≠ reação visual ≠ interromper ação), `die` (a explosão em si, com o Animation Event de dano).
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.400 | **🔢 Vida estimada:** 1.100

#### Skeleton Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Variante de idle "emboscada" (ver regra padrão acima) — fica parado numa pose única até detectar o jogador, sem vagar pelo andar. Ao detectar, ativa (`activate`, 1x) e aproxima-se agressivamente, tentando permanecer em alcance de ataque. Se o jogador sair do raio de observação, volta pra pose parada instantaneamente, sem animação de transição.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 6.500-39.000 | Chance: 100%
  - Monster Fragment: 1.300-10.400 | Chance: 100%
  - Spirit Dust: 1.040-6.500 | Chance: 100%
  - Arcane Shard: 130-910 | Chance: 60%
  - Dark Crystal: 65-390 | Chance: 35%
  - Soul Fragment: 26-130 | Chance: 20%
  - Corrupted Core: 6-39 | Chance: 8%
  - Elemental Shard: 1-6 | Chance: 2%
  - Ancient Fragment: 1 | Chance: 0,4%
- **Animações necessárias:** idle (pose estática, sem direção), activate (ao detectar, 1x), walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.040 | **🔢 Vida estimada:** 3.080

---

### Andar 6

#### Ancient Danger Heavy Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 70.000-420.000 | Chance: 100%
  - Monster Fragment: 14.000-112.000 | Chance: 100%
  - Spirit Dust: 11.200-84.000 | Chance: 100%
  - Arcane Shard: 1.400-11.200 | Chance: 80%
  - Dark Crystal: 700-5.600 | Chance: 60%
  - Soul Fragment: 280-1.400 | Chance: 40%
  - Corrupted Core: 70-420 | Chance: 20%
  - Elemental Shard: 14-84 | Chance: 8%
  - Ancient Fragment: 4-28 | Chance: 3%
  - Infernal Ash: 1-3 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 26.000 | **🔢 Vida estimada:** 96.000

#### Armored Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 75.000-450.000 | Chance: 100%
  - Monster Fragment: 15.000-120.000 | Chance: 100%
  - Spirit Dust: 12.000-90.000 | Chance: 100%
  - Arcane Shard: 1.500-12.000 | Chance: 80%
  - Dark Crystal: 750-6.000 | Chance: 60%
  - Soul Fragment: 300-1.500 | Chance: 40%
  - Corrupted Core: 75-450 | Chance: 20%
  - Elemental Shard: 15-90 | Chance: 8%
  - Ancient Fragment: 4-30 | Chance: 3%
  - Infernal Ash: 2-3 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 20.000 | **🔢 Vida estimada:** 108.000

#### Rock Golem
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 85.000-510.000 | Chance: 100%
  - Monster Fragment: 17.000-136.000 | Chance: 100%
  - Spirit Dust: 13.600-102.000 | Chance: 100%
  - Arcane Shard: 1.700-13.600 | Chance: 80%
  - Dark Crystal: 850-6.800 | Chance: 60%
  - Soul Fragment: 340-1.700 | Chance: 40%
  - Corrupted Core: 85-510 | Chance: 20%
  - Elemental Shard: 17-102 | Chance: 8%
  - Ancient Fragment: 5-34 | Chance: 3%
  - Infernal Ash: 2-3 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 30.000 | **🔢 Vida estimada:** 132.000

#### Fire Elemental
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma bola de fogo em direção ao player, essa bola de fogo explode ao contato (ou quando chega no limite de distância que ela percorre) causando um grande dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra bola de fogo novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 60.000-360.000 | Chance: 100%
  - Monster Fragment: 12.000-96.000 | Chance: 100%
  - Spirit Dust: 9.600-72.000 | Chance: 100%
  - Arcane Shard: 1.200-9.600 | Chance: 80%
  - Dark Crystal: 600-4.800 | Chance: 60%
  - Soul Fragment: 240-1.200 | Chance: 40%
  - Corrupted Core: 60-360 | Chance: 20%
  - Elemental Shard: 12-72 | Chance: 8%
  - Ancient Fragment: 4-24 | Chance: 3%
  - Infernal Ash: 1-2 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 26.000 | **🔢 Vida estimada:** 48.000

#### Air Elemental
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança um furacão em direção ao player, esse furacão explode ao contato (ou quando chega no limite de distância que ela percorre) causando um grande dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra furacão novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 55.000-330.000 | Chance: 100%
  - Monster Fragment: 11.000-88.000 | Chance: 100%
  - Spirit Dust: 8.800-66.000 | Chance: 100%
  - Arcane Shard: 1.100-8.800 | Chance: 80%
  - Dark Crystal: 550-4.400 | Chance: 60%
  - Soul Fragment: 220-1.100 | Chance: 40%
  - Corrupted Core: 55-330 | Chance: 20%
  - Elemental Shard: 11-66 | Chance: 8%
  - Ancient Fragment: 3-22 | Chance: 3%
  - Infernal Ash: 1-2 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 24.000 | **🔢 Vida estimada:** 45.000

#### Water Elemental
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo. *(A "explosão" é a própria animação `attack` — dispara no cooldown normal, repetível, não causa dano nele mesmo, e não é suicídio como o Burning Skull.)*
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 65.000-390.000 | Chance: 100%
  - Monster Fragment: 13.000-104.000 | Chance: 100%
  - Spirit Dust: 10.400-78.000 | Chance: 100%
  - Arcane Shard: 1.300-10.400 | Chance: 80%
  - Dark Crystal: 650-5.200 | Chance: 60%
  - Soul Fragment: 260-1.300 | Chance: 40%
  - Corrupted Core: 65-390 | Chance: 20%
  - Elemental Shard: 13-78 | Chance: 8%
  - Ancient Fragment: 4-26 | Chance: 3%
  - Infernal Ash: 1-3 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 32.000 | **🔢 Vida estimada:** 60.000

#### Earth Elemental
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 75.000-450.000 | Chance: 100%
  - Monster Fragment: 15.000-120.000 | Chance: 100%
  - Spirit Dust: 12.000-90.000 | Chance: 100%
  - Arcane Shard: 1.500-12.000 | Chance: 80%
  - Dark Crystal: 750-6.000 | Chance: 60%
  - Soul Fragment: 300-1.500 | Chance: 40%
  - Corrupted Core: 75-450 | Chance: 20%
  - Elemental Shard: 15-90 | Chance: 8%
  - Ancient Fragment: 4-30 | Chance: 3%
  - Infernal Ash: 2-3 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 24.000 | **🔢 Vida estimada:** 108.000

#### Ancient Danger
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 75.000-450.000 | Chance: 100%
  - Monster Fragment: 15.000-120.000 | Chance: 100%
  - Spirit Dust: 12.000-90.000 | Chance: 100%
  - Arcane Shard: 1.500-12.000 | Chance: 80%
  - Dark Crystal: 750-6.000 | Chance: 60%
  - Soul Fragment: 300-1.500 | Chance: 40%
  - Corrupted Core: 75-450 | Chance: 20%
  - Elemental Shard: 15-90 | Chance: 8%
  - Ancient Fragment: 4-30 | Chance: 3%
  - Infernal Ash: 2-3 | Chance: 0,25%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 28.000 | **🔢 Vida estimada:** 90.000

---

### Andar 7

#### Fire Draco
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma bola de fogo em direção ao player, essa bola de fogo explode ao contato (ou quando chega no limite de distância que ela percorre) causando um pequeno dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra bola de fogo novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 600.000-6.000.000 | Chance: 100%
  - Monster Fragment: 120.000-1.200.000 | Chance: 100%
  - Spirit Dust: 96.000-840.000 | Chance: 100%
  - Arcane Shard: 12.000-120.000 | Chance: 95%
  - Dark Crystal: 6.000-60.000 | Chance: 80%
  - Soul Fragment: 1.200-12.000 | Chance: 60%
  - Corrupted Core: 360-3.600 | Chance: 35%
  - Elemental Shard: 60-600 | Chance: 18%
  - Ancient Fragment: 24-240 | Chance: 8%
  - Infernal Ash: 2-12 | Chance: 2%
  - Chaos Crystal: 1 | Chance: 0,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Enchanted_Companions_v1.0
- **🔢 Dano estimado:** 1.650.000 | **🔢 Vida estimada:** 4.000.000

#### Magma Hound
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 500.000-5.000.000 | Chance: 100%
  - Monster Fragment: 100.000-1.000.000 | Chance: 100%
  - Spirit Dust: 80.000-700.000 | Chance: 100%
  - Arcane Shard: 10.000-100.000 | Chance: 95%
  - Dark Crystal: 5.000-50.000 | Chance: 80%
  - Soul Fragment: 1.000-10.000 | Chance: 60%
  - Corrupted Core: 300-3.000 | Chance: 35%
  - Elemental Shard: 50-500 | Chance: 18%
  - Ancient Fragment: 20-200 | Chance: 8%
  - Infernal Ash: 2-10 | Chance: 2%
  - Chaos Crystal: 1 | Chance: 0,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 1.500.000 | **🔢 Vida estimada:** 4.000.000

#### Magma Orc
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 650.000-6.500.000 | Chance: 100%
  - Monster Fragment: 130.000-1.300.000 | Chance: 100%
  - Spirit Dust: 104.000-910.000 | Chance: 100%
  - Arcane Shard: 13.000-130.000 | Chance: 95%
  - Dark Crystal: 6.500-65.000 | Chance: 80%
  - Soul Fragment: 1.300-13.000 | Chance: 60%
  - Corrupted Core: 390-3.900 | Chance: 35%
  - Elemental Shard: 65-650 | Chance: 18%
  - Ancient Fragment: 26-260 | Chance: 8%
  - Infernal Ash: 3-13 | Chance: 2%
  - Chaos Crystal: 1 | Chance: 0,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 1.800.000 | **🔢 Vida estimada:** 6.000.000

#### Magma Golem
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 850.000-8.500.000 | Chance: 100%
  - Monster Fragment: 170.000-1.700.000 | Chance: 100%
  - Spirit Dust: 136.000-1.190.000 | Chance: 100%
  - Arcane Shard: 17.000-170.000 | Chance: 95%
  - Dark Crystal: 8.500-85.000 | Chance: 80%
  - Soul Fragment: 1.700-17.000 | Chance: 60%
  - Corrupted Core: 510-5.100 | Chance: 35%
  - Elemental Shard: 85-850 | Chance: 18%
  - Ancient Fragment: 34-340 | Chance: 8%
  - Infernal Ash: 3-17 | Chance: 2%
  - Chaos Crystal: 2 | Chance: 0,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.250.000 | **🔢 Vida estimada:** 10.000.000

#### Dragon Hatchling
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 750.000-7.500.000 | Chance: 100%
  - Monster Fragment: 150.000-1.500.000 | Chance: 100%
  - Spirit Dust: 120.000-1.050.000 | Chance: 100%
  - Arcane Shard: 15.000-150.000 | Chance: 95%
  - Dark Crystal: 7.500-75.000 | Chance: 80%
  - Soul Fragment: 1.500-15.000 | Chance: 60%
  - Corrupted Core: 450-4.500 | Chance: 35%
  - Elemental Shard: 75-750 | Chance: 18%
  - Ancient Fragment: 30-300 | Chance: 8%
  - Infernal Ash: 3-15 | Chance: 2%
  - Chaos Crystal: 2 | Chance: 0,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.100.000 | **🔢 Vida estimada:** 7.000.000

---

### Andar 8

#### Diablo
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma bola de fogo em direção ao player, essa bola de fogo explode ao contato (ou quando chega no limite de distância que ela percorre) causando um grande dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra bola de fogo novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 14.000.000-140.000.000 | Chance: 100%
  - Monster Fragment: 2.800.000-28.000.000 | Chance: 100%
  - Spirit Dust: 1.400.000-21.000.000 | Chance: 100%
  - Arcane Shard: 140.000-1.400.000 | Chance: 100%
  - Dark Crystal: 70.000-700.000 | Chance: 95%
  - Soul Fragment: 14.000-140.000 | Chance: 80%
  - Corrupted Core: 4.200-42.000 | Chance: 60%
  - Elemental Shard: 700-7.000 | Chance: 35%
  - Ancient Fragment: 140-1.400 | Chance: 20%
  - Infernal Ash: 14-140 | Chance: 8%
  - Chaos Crystal: 3-28 | Chance: 2%
  - Nightmare Residue: 1 | Chance: 0,15%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 420.000.000 | **🔢 Vida estimada:** 1.000.000.000

#### Imp
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma bola de fogo em direção ao player, essa bola de fogo explode ao contato (ou quando chega no limite de distância que ela percorre) causando um grande dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra bola de fogo novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 10.000.000-100.000.000 | Chance: 100%
  - Monster Fragment: 2.000.000-20.000.000 | Chance: 100%
  - Spirit Dust: 1.000.000-15.000.000 | Chance: 100%
  - Arcane Shard: 100.000-1.000.000 | Chance: 100%
  - Dark Crystal: 50.000-500.000 | Chance: 95%
  - Soul Fragment: 10.000-100.000 | Chance: 80%
  - Corrupted Core: 3.000-30.000 | Chance: 60%
  - Elemental Shard: 500-5.000 | Chance: 35%
  - Ancient Fragment: 100-1.000 | Chance: 20%
  - Infernal Ash: 10-100 | Chance: 8%
  - Chaos Crystal: 2-20 | Chance: 2%
  - Nightmare Residue: 1 | Chance: 0,15%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 300.000.000 | **🔢 Vida estimada:** 600.000.000

#### Armored Demon
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 17.000.000-170.000.000 | Chance: 100%
  - Monster Fragment: 3.400.000-34.000.000 | Chance: 100%
  - Spirit Dust: 1.700.000-25.500.000 | Chance: 100%
  - Arcane Shard: 170.000-1.700.000 | Chance: 100%
  - Dark Crystal: 85.000-850.000 | Chance: 95%
  - Soul Fragment: 17.000-170.000 | Chance: 80%
  - Corrupted Core: 5.100-51.000 | Chance: 60%
  - Elemental Shard: 850-8.500 | Chance: 35%
  - Ancient Fragment: 170-1.700 | Chance: 20%
  - Infernal Ash: 17-170 | Chance: 8%
  - Chaos Crystal: 3-34 | Chance: 2%
  - Nightmare Residue: 2 | Chance: 0,15%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 390.000.000 | **🔢 Vida estimada:** 2.000.000.000

---

### Andar 9

#### Observer
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 500.000.000-5.000.000.000 | Chance: 100%
  - Monster Fragment: 100.000.000-1.000.000.000 | Chance: 100%
  - Spirit Dust: 50.000.000-500.000.000 | Chance: 100%
  - Arcane Shard: 5.000.000-50.000.000 | Chance: 100%
  - Dark Crystal: 2.000.000-20.000.000 | Chance: 100%
  - Soul Fragment: 500.000-5.000.000 | Chance: 95%
  - Corrupted Core: 100.000-1.000.000 | Chance: 80%
  - Elemental Shard: 20.000-200.000 | Chance: 60%
  - Ancient Fragment: 5.000-50.000 | Chance: 40%
  - Infernal Ash: 500-5.000 | Chance: 20%
  - Chaos Crystal: 100-1.000 | Chance: 8%
  - Nightmare Residue: 5-50 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 200.000.000.000 | **🔢 Vida estimada:** 700.000.000.000

#### Serpent — **Exceção à regra padrão** 🟡
- **Tipo:** Melee especial (invulnerável e inofensivo enquanto submerso)
- **Movimentação:** Persegue o jogador **debaixo do solo** (`Walk`, variante submersa) até ficar em alcance de contato — nesse momento faz `Emerge` e fica exposto por um tempo, depois `Submerge` de novo.
- **Comportamento — regra central:** o Serpent **só pode causar dano, e só pode receber dano, enquanto estiver na animação `Emerge`** (exposto). Durante `Walk`/`Idle` (submerso), ele é efetivamente intangível: não dá dano de contato e ataques do jogador não o afetam (nem `TakeDamage` nem colisão). Isso significa que a única janela de combate real com o Serpent é a exposição — o resto do tempo ele é só uma ameaça visível se aproximando por baixo do solo.
- **Ciclo:** Submerso perseguindo → alcance de contato → `Emerge` (exposto, vulnerável, causa dano de contato normal com cooldown enquanto durar) → `Submerge` de novo → repete.
- **Função no combate:** Inimigo de janela de oportunidade — obriga o jogador a esperar a exposição pra revidar, em vez de conseguir acertá-lo a qualquer momento como um Melee comum.
- **Drops:**
  - Monster Essence: 600.000.000-6.000.000.000 | Chance: 100%
  - Monster Fragment: 120.000.000-1.200.000.000 | Chance: 100%
  - Spirit Dust: 60.000.000-600.000.000 | Chance: 100%
  - Arcane Shard: 6.000.000-60.000.000 | Chance: 100%
  - Dark Crystal: 2.400.000-24.000.000 | Chance: 100%
  - Soul Fragment: 600.000-6.000.000 | Chance: 95%
  - Corrupted Core: 120.000-1.200.000 | Chance: 80%
  - Elemental Shard: 24.000-240.000 | Chance: 60%
  - Ancient Fragment: 6.000-60.000 | Chance: 40%
  - Infernal Ash: 600-6.000 | Chance: 20%
  - Chaos Crystal: 120-1.200 | Chance: 8%
  - Nightmare Residue: 6-60 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** `idle` (submerso, sem detectar), `walk` (submerso, perseguindo), `emerge` (exposto — é aqui que dá e recebe dano), `submerge` (transição de volta), `damage`, `die`.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 240.000.000.000 | **🔢 Vida estimada:** 770.000.000.000

#### Shifted
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 700.000.000-7.000.000.000 | Chance: 100%
  - Monster Fragment: 140.000.000-1.400.000.000 | Chance: 100%
  - Spirit Dust: 70.000.000-700.000.000 | Chance: 100%
  - Arcane Shard: 7.000.000-70.000.000 | Chance: 100%
  - Dark Crystal: 2.800.000-28.000.000 | Chance: 100%
  - Soul Fragment: 700.000-7.000.000 | Chance: 95%
  - Corrupted Core: 140.000-1.400.000 | Chance: 80%
  - Elemental Shard: 28.000-280.000 | Chance: 60%
  - Ancient Fragment: 7.000-70.000 | Chance: 40%
  - Infernal Ash: 700-7.000 | Chance: 20%
  - Chaos Crystal: 140-1.400 | Chance: 8%
  - Nightmare Residue: 7-70 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 260.000.000.000 | **🔢 Vida estimada:** 980.000.000.000

#### Stalker
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 550.000.000-5.500.000.000 | Chance: 100%
  - Monster Fragment: 110.000.000-1.100.000.000 | Chance: 100%
  - Spirit Dust: 55.000.000-550.000.000 | Chance: 100%
  - Arcane Shard: 5.500.000-55.000.000 | Chance: 100%
  - Dark Crystal: 2.200.000-22.000.000 | Chance: 100%
  - Soul Fragment: 550.000-5.500.000 | Chance: 95%
  - Corrupted Core: 110.000-1.100.000 | Chance: 80%
  - Elemental Shard: 22.000-220.000 | Chance: 60%
  - Ancient Fragment: 5.500-55.000 | Chance: 40%
  - Infernal Ash: 550-5.500 | Chance: 20%
  - Chaos Crystal: 110-1.100 | Chance: 8%
  - Nightmare Residue: 6-55 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 230.000.000.000 | **🔢 Vida estimada:** 630.000.000.000

#### Winged
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança um laser em direção ao player, essa laser explode ao contato (ou quando chega no limite de distância que ela percorre) causando um pequeno dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra laser novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 600.000.000-6.000.000.000 | Chance: 100%
  - Monster Fragment: 120.000.000-1.200.000.000 | Chance: 100%
  - Spirit Dust: 60.000.000-600.000.000 | Chance: 100%
  - Arcane Shard: 6.000.000-60.000.000 | Chance: 100%
  - Dark Crystal: 2.400.000-24.000.000 | Chance: 100%
  - Soul Fragment: 600.000-6.000.000 | Chance: 95%
  - Corrupted Core: 120.000-1.200.000 | Chance: 80%
  - Elemental Shard: 24.000-240.000 | Chance: 60%
  - Ancient Fragment: 6.000-60.000 | Chance: 40%
  - Infernal Ash: 600-6.000 | Chance: 20%
  - Chaos Crystal: 120-1.200 | Chance: 8%
  - Nightmare Residue: 6-60 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 240.000.000.000 | **🔢 Vida estimada:** 560.000.000.000

#### Worm
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo. Perdeu o dash com dano na trajetória — simplificado por completo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 650.000.000-6.500.000.000 | Chance: 100%
  - Monster Fragment: 130.000.000-1.300.000.000 | Chance: 100%
  - Spirit Dust: 65.000.000-650.000.000 | Chance: 100%
  - Arcane Shard: 6.500.000-65.000.000 | Chance: 100%
  - Dark Crystal: 2.600.000-26.000.000 | Chance: 100%
  - Soul Fragment: 650.000-6.500.000 | Chance: 95%
  - Corrupted Core: 130.000-1.300.000 | Chance: 80%
  - Elemental Shard: 26.000-260.000 | Chance: 60%
  - Ancient Fragment: 6.500-65.000 | Chance: 40%
  - Infernal Ash: 650-6.500 | Chance: 20%
  - Chaos Crystal: 130-1.300 | Chance: 8%
  - Nightmare Residue: 6-65 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 300.000.000.000 | **🔢 Vida estimada:** 700.000.000.000

#### Bicephalous — **Exceção pontual** 🟡 (projétil especial, comportamento geral é Ranged padrão)
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Igual a um Ranged comum — mantém distância, auto-dispara ao entrar em alcance, cooldown normal, sem animação de disparo dedicada.
- **Ataque:** Lança um projétil de "slug" em direção ao player, causando dano por contato se acertar. Ao fim da trajetória — seja por acertar o player, seja por atingir o alcance máximo — **o projétil se transforma numa unidade real de Slug** (nasce ali, viva, com IA própria). Ou seja, o projétil não é só um efeito visual: é o "caso 4" da taxonomia de projéteis (Seção 13) — o próprio projétil vira o monstro.
- **Função no combate:** Inimigo de pressão que gera pressão adicional com o tempo — cada disparo que não te acerta ainda assim adiciona um Slug na sala.
- **Drops:**
  - Monster Essence: 700.000.000-7.000.000.000 | Chance: 100%
  - Monster Fragment: 140.000.000-1.400.000.000 | Chance: 100%
  - Spirit Dust: 70.000.000-700.000.000 | Chance: 100%
  - Arcane Shard: 7.000.000-70.000.000 | Chance: 100%
  - Dark Crystal: 2.800.000-28.000.000 | Chance: 100%
  - Soul Fragment: 700.000-7.000.000 | Chance: 95%
  - Corrupted Core: 140.000-1.400.000 | Chance: 80%
  - Elemental Shard: 28.000-280.000 | Chance: 60%
  - Ancient Fragment: 7.000-70.000 | Chance: 40%
  - Infernal Ash: 700-7.000 | Chance: 20%
  - Chaos Crystal: 140-1.400 | Chance: 8%
  - Nightmare Residue: 7-70 | Chance: 1,5%
  - Void Shard: 1 | Chance: 0,08%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 240.000.000.000 | **🔢 Vida estimada:** 770.000.000.000

---

### Andar 10

#### Divine Minion
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 50.000.000.000-500.000.000.000 | Chance: 100%
  - Monster Fragment: 10.000.000.000-100.000.000.000 | Chance: 100%
  - Spirit Dust: 5.000.000.000-50.000.000.000 | Chance: 100%
  - Arcane Shard: 500.000.000-5.000.000.000 | Chance: 100%
  - Dark Crystal: 200.000.000-2.000.000.000 | Chance: 100%
  - Soul Fragment: 50.000.000-500.000.000 | Chance: 100%
  - Corrupted Core: 10.000.000-100.000.000 | Chance: 95%
  - Elemental Shard: 2.000.000-20.000.000 | Chance: 85%
  - Ancient Fragment: 500.000-5.000.000 | Chance: 70%
  - Infernal Ash: 50.000-500.000 | Chance: 50%
  - Chaos Crystal: 10.000-100.000 | Chance: 30%
  - Nightmare Residue: 100-1.000 | Chance: 12%
  - Void Shard: 5-25 | Chance: 3%
  - Celestial Fragment: 1 | Chance: 0,1%
  - Divine Core: 1 | Chance: 0,001%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 15.000.000.000.000 | **🔢 Vida estimada:** 40.000.000.000.000

#### Divine Warden
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha divina em direção ao player, essa flecha explode ao contato (ou quando chega no limite de distância que ela percorre) causando um grande dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 62.500.000.000-625.000.000.000 | Chance: 100%
  - Monster Fragment: 12.500.000.000-125.000.000.000 | Chance: 100%
  - Spirit Dust: 6.250.000.000-62.500.000.000 | Chance: 100%
  - Arcane Shard: 625.000.000-6.250.000.000 | Chance: 100%
  - Dark Crystal: 250.000.000-2.500.000.000 | Chance: 100%
  - Soul Fragment: 62.500.000-625.000.000 | Chance: 100%
  - Corrupted Core: 12.500.000-125.000.000 | Chance: 95%
  - Elemental Shard: 2.500.000-25.000.000 | Chance: 85%
  - Ancient Fragment: 625.000-6.250.000 | Chance: 70%
  - Infernal Ash: 62.500-625.000 | Chance: 50%
  - Chaos Crystal: 12.500-125.000 | Chance: 30%
  - Nightmare Residue: 125-1.250 | Chance: 12%
  - Void Shard: 6-31 | Chance: 3%
  - Celestial Fragment: 1 | Chance: 0,2%
  - Divine Core: 1 | Chance: 0,0025%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 20.000.000.000.000 | **🔢 Vida estimada:** 45.000.000.000.000

#### Divine Guardian
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 75.000.000.000-750.000.000.000 | Chance: 100%
  - Monster Fragment: 15.000.000.000-150.000.000.000 | Chance: 100%
  - Spirit Dust: 7.500.000.000-75.000.000.000 | Chance: 100%
  - Arcane Shard: 750.000.000-7.500.000.000 | Chance: 100%
  - Dark Crystal: 300.000.000-3.000.000.000 | Chance: 100%
  - Soul Fragment: 75.000.000-750.000.000 | Chance: 100%
  - Corrupted Core: 15.000.000-150.000.000 | Chance: 95%
  - Elemental Shard: 3.000.000-30.000.000 | Chance: 85%
  - Ancient Fragment: 750.000-7.500.000 | Chance: 70%
  - Infernal Ash: 75.000-750.000 | Chance: 50%
  - Chaos Crystal: 15.000-150.000 | Chance: 30%
  - Nightmare Residue: 150-1.500 | Chance: 12%
  - Void Shard: 8-38 | Chance: 3%
  - Celestial Fragment: 2 | Chance: 0,35%
  - Divine Core: 2 | Chance: 0,005%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 18.000.000.000.000 | **🔢 Vida estimada:** 75.000.000.000.000

#### Divine Angel
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 87.500.000.000-875.000.000.000 | Chance: 100%
  - Monster Fragment: 17.500.000.000-175.000.000.000 | Chance: 100%
  - Spirit Dust: 8.750.000.000-87.500.000.000 | Chance: 100%
  - Arcane Shard: 875.000.000-8.750.000.000 | Chance: 100%
  - Dark Crystal: 350.000.000-3.500.000.000 | Chance: 100%
  - Soul Fragment: 87.500.000-875.000.000 | Chance: 100%
  - Corrupted Core: 17.500.000-175.000.000 | Chance: 95%
  - Elemental Shard: 3.500.000-35.000.000 | Chance: 85%
  - Ancient Fragment: 875.000-8.750.000 | Chance: 70%
  - Infernal Ash: 87.500-875.000 | Chance: 50%
  - Chaos Crystal: 17.500-175.000 | Chance: 30%
  - Nightmare Residue: 175-1.750 | Chance: 12%
  - Void Shard: 9-44 | Chance: 3%
  - Celestial Fragment: 2 | Chance: 0,5%
  - Divine Core: 2 | Chance: 0,01%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 22.000.000.000.000 | **🔢 Vida estimada:** 60.000.000.000.000

---

### Especiais / sem spawn automático por andar

#### Chest Mimic — **Exceção pontual** 🟡 (disfarce interativo + escala de stats única no jogo)
- **Tipo:** Melee
- **Movimentação:** Começa disfarçado de baú comum, parado, sem perseguir ninguém. Só passa a perseguir o jogador (corpo a corpo, igual a qualquer Melee comum) depois de ativado.
- **Comportamento — ciclo de disfarce:**
  1. **Desativado (disfarçado):** aparece como um baú comum, parado, com a interação padrão de baú — tecla **E** some em cima dele para o jogador abrir. Não persegue, não causa dano, não usa `idle`/`walk` de monstro nessa fase.
  2. **Ativação:** ao interagir (E), toca a animação `activation`. Só depois que essa animação termina por completo é que ele "acorda" de verdade.
  3. **Ativado:** vira um Melee comum — persegue o jogador e ataca com uma animação `attack` real, igual a qualquer Melee comum (trigger direcional no golpe, Animation Event, sem dano de contato passivo). Usa `idle`/`walk`/`idle_combat`/`attack` normalmente nessa fase.
  4. **Desativação:** se o jogador se afastar o suficiente (sai do raio de observação dele), toca a animação `desactivation` e ele volta ao passo 1 — disfarçado de baú comum, interagível de novo com E.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — mesmo mecanismo de qualquer Melee comum (trigger direcional, sem contato passivo) — só ocorre na fase Ativado.
- **Função no combate:** Inimigo troll, criado para mostrar ao jogador que nem tudo é seguro na torre.
- **Drops:**
  - Pergaminho do baú: 1 | Chance: 100%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage, die, activation, desactivation.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **Andar estimado:** Todos os andares podem nascer baús, uma porcentagem desses baús podem ser mimics
- **Escala de stats — único monstro do jogo com essa regra:** ao contrário de todo o resto do Bestiário (que tem Dano/Vida fixos, definidos ficha a ficha por andar), o Chest Mimic calcula Dano e Vida em tempo real como uma **porcentagem** aplicada sobre uma base, crescendo conforme o andar em que ele nasceu (ex.: andar 1 tem a vida-base X, andar 8 tem X vezes Y%, e assim por diante). 🔢 fórmula exata (base X e multiplicador Y% por andar) pendente de balanceamento — a intenção de referência anterior era aproximar 1,3x o dano e 1,8x a vida de um monstro médio do andar, mas isso deixa de ser uma tabela fixa por andar e passa a ser essa fórmula percentual.

#### Slug
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Após nascer persegue o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo.
- **Ataque:** Se rasteja e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - *Nenhum. Summons do Bicephalous não geram loot para impedir farm infinito.*
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **Andar estimado:** é summon, não nasce automaticamente
- **🔢 Dano estimado:** 80.000.000.000 | **🔢 Vida estimada:** 120.000.000.000

---

## Bosses

### Andar 1

#### Mother Slime Green
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Ao morrer, nascem 3 Slimes Green normais em **posições fixas** (3 pontos filhos do próprio GameObject do boss, não aleatórios).
- **Ataque:** Dano por contato normal (cooldown próprio) — pula e causa dano ao encostar, **sem animação de ataque dedicada, único caso do jogo além das exceções nomeadas.** Fica assim pra sempre, mesmo com o resto do Melee tendo voltado a ter `attack` real.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 50-200 | Chance: 100%
  - Monster Fragment: 10-50 | Chance: 100%
  - Spirit Dust: 10-30 | Chance: 75%
  - Arcane Shard: 10 | Chance: 8%
  - Dark Crystal: 10 | Chance: 4%
- **Animações necessárias:** idle, walk, idle_combat, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 8 | **🔢 Vida estimada:** 120

#### Mother Slime Blue
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Ao morrer, nascem 3 Slimes Blue normais em **posições fixas** (3 pontos filhos do próprio GameObject do boss, não aleatórios).
- **Ataque:** Dano por contato normal (cooldown próprio) — pula e causa dano ao encostar, **sem animação de ataque dedicada, único caso do jogo além das exceções nomeadas.** Fica assim pra sempre, mesmo com o resto do Melee tendo voltado a ter `attack` real.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 50-200 | Chance: 100%
  - Monster Fragment: 10-50 | Chance: 100%
  - Spirit Dust: 10-30 | Chance: 75%
  - Arcane Shard: 10 | Chance: 8%
  - Dark Crystal: 10 | Chance: 4%
- **Animações necessárias:** idle, walk, idle_combat, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 8 | **🔢 Vida estimada:** 120

#### Goblin King
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo — igual a qualquer Melee comum.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 60-240 | Chance: 100%
  - Monster Fragment: 12-60 | Chance: 100%
  - Spirit Dust: 12-36 | Chance: 75%
  - Arcane Shard: 12 | Chance: 8%
  - Dark Crystal: 12 | Chance: 4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 10 | **🔢 Vida estimada:** 160

---

### Andar 2

#### Rat People Royalty — **Exceção pontual**
- **Tipo:** Melee especial
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Dano por contato normal (cooldown próprio). Além disso, mantém uma habilidade especial: arremessa um Rat People na direção do jogador (animação própria, `throw_ratpeople`, com Animation Event no frame do arremesso) — causa dano na trajetória e, ao final, o Rat People arremessado nasce como unidade viva no ponto de chegada.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 360-1.440 | Chance: 100%
  - Monster Fragment: 60-240 | Chance: 100%
  - Spirit Dust: 36-120 | Chance: 100%
  - Arcane Shard: 12-24 | Chance: 12%
  - Dark Crystal: 12 | Chance: 8%
  - Soul Fragment: 12 | Chance: 6,4%
  - *Observação: summons criados por este boss não geram loot.*
- **Animações necessárias:** idle, walk, idle_combat, throw_ratpeople (com Animation Event), damage, die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 30 | **🔢 Vida estimada:** 250

#### Werewolf
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 450-1.800 | Chance: 100%
  - Monster Fragment: 75-300 | Chance: 100%
  - Spirit Dust: 45-150 | Chance: 100%
  - Arcane Shard: 15-30 | Chance: 12%
  - Dark Crystal: 15 | Chance: 8%
  - Soul Fragment: 15 | Chance: 6,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 35 | **🔢 Vida estimada:** 300

#### Centaur King
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 540-2.160 | Chance: 100%
  - Monster Fragment: 90-360 | Chance: 100%
  - Spirit Dust: 54-180 | Chance: 100%
  - Arcane Shard: 18-36 | Chance: 12%
  - Dark Crystal: 18 | Chance: 8%
  - Soul Fragment: 18 | Chance: 6,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 40 | **🔢 Vida estimada:** 350

#### Cave Troll
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 720-2.880 | Chance: 100%
  - Monster Fragment: 120-480 | Chance: 100%
  - Spirit Dust: 72-240 | Chance: 100%
  - Arcane Shard: 24-48 | Chance: 12%
  - Dark Crystal: 24 | Chance: 8%
  - Soul Fragment: 24 | Chance: 6,4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 50 | **🔢 Vida estimada:** 450

#### Spider Queen — **Exceção pontual**
- **Tipo:** Melee/Ranged híbrido
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Dano por contato normal quando corpo a corpo (cooldown próprio). Além disso, mantém a habilidade de lançar teia: se o jogador estiver fora de alcance de contato, ela **auto-lança uma teia por cooldown** — sem nenhuma animação dedicada (igual a um Ranged comum) — que gruda no jogador e o prende (não consegue andar por alguns segundos). Continua perseguindo normalmente enquanto essa habilidade estiver em cooldown.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 600-2.400 | Chance: 100%
  - Monster Fragment: 100-400 | Chance: 100%
  - Spirit Dust: 60-200 | Chance: 100%
  - Arcane Shard: 20-40 | Chance: 12%
  - Dark Crystal: 20 | Chance: 8%
  - Soul Fragment: 20 | Chance: 6,4%
- **Animações necessárias:** idle, walk, idle_combat, damage, die. *(Sem `attack`/`shotweb` — a teia é um efeito sem animação própria, igual ao dano de contato)*
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 35 | **🔢 Vida estimada:** 325

---

### Andar 3

#### Giant
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 3.000-14.000 | Chance: 100%
  - Monster Fragment: 600-2.400 | Chance: 100%
  - Spirit Dust: 400-1.600 | Chance: 100%
  - Arcane Shard: 40-100 | Chance: 30%
  - Dark Crystal: 20-60 | Chance: 14%
  - Soul Fragment: 20-40 | Chance: 12%
  - Corrupted Core: 20 | Chance: 5,6%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 130 | **🔢 Vida estimada:** 1440

#### Pale Champion
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 2.700-12.600 | Chance: 100%
  - Monster Fragment: 540-2.160 | Chance: 100%
  - Spirit Dust: 360-1.440 | Chance: 100%
  - Arcane Shard: 36-90 | Chance: 30%
  - Dark Crystal: 18-54 | Chance: 14%
  - Soul Fragment: 18-36 | Chance: 12%
  - Corrupted Core: 18 | Chance: 5,6%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 110 | **🔢 Vida estimada:** 1200

#### Wise Orc
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 2.250-10.500 | Chance: 100%
  - Monster Fragment: 450-1.800 | Chance: 100%
  - Spirit Dust: 300-1.200 | Chance: 100%
  - Arcane Shard: 30-75 | Chance: 30%
  - Dark Crystal: 15-45 | Chance: 14%
  - Soul Fragment: 15-30 | Chance: 12%
  - Corrupted Core: 15 | Chance: 5,6%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 90 | **🔢 Vida estimada:** 1000

---

### Andar 4

#### Flagelant
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Boss melee de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 14.400-72.000 | Chance: 100%
  - Monster Fragment: 2.700-14.400 | Chance: 100%
  - Spirit Dust: 1.800-10.800 | Chance: 100%
  - Arcane Shard: 180-900 | Chance: 52,5%
  - Dark Crystal: 90-450 | Chance: 36%
  - Soul Fragment: 36-180 | Chance: 20%
  - Corrupted Core: 18-90 | Chance: 12%
  - Elemental Shard: 18 | Chance: 4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 400 | **🔢 Vida estimada:** 6.500

#### Ritual Guard
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Boss melee resistente, criado para pressionar o jogador através de contato direto e alta durabilidade.
- **Drops:**
  - Monster Essence: 16.000-80.000 | Chance: 100%
  - Monster Fragment: 3.000-16.000 | Chance: 100%
  - Spirit Dust: 2.000-12.000 | Chance: 100%
  - Arcane Shard: 200-1.000 | Chance: 52,5%
  - Dark Crystal: 100-500 | Chance: 36%
  - Soul Fragment: 40-200 | Chance: 20%
  - Corrupted Core: 20-100 | Chance: 12%
  - Elemental Shard: 20 | Chance: 4%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 480 | **🔢 Vida estimada:** 8.000

#### Dark Channeler — **Exceção à regra padrão**
- **Tipo:** Ranged/Suporte especial (não lança projétil — transforma monstros próprios do Floor)
- **Movimentação:** Mantém distância do jogador e procura permanecer próximo de Acolytes, Hounds e Zealots que possam ser transformados.
- **Comportamento:** Ele detecta automaticamente o jogador e procura grupos de unidades elegíveis para sua habilidade. Sempre que a transformação estiver disponível (fora de cooldown), prioriza três Acolytes, Hounds ou Zealots próximos. Enquanto a habilidade estiver em cooldown, reposiciona-se para permanecer distante do jogador e próximo de possíveis alvos.
- **Ataque:** Mantém a animação `attack`/`cast` (sem body damage — o Dark Channeler não causa dano direto). No frame certo dessa animação, um **Animation Event** dispara a transformação simultânea dos 3 monstros elegíveis mais próximos: Acolytes tornam-se Dark Cultists, Hounds tornam-se Dark Hounds e Zealots tornam-se Dark Abominations. Cada um dos 3 monstros afetados também toca sua **própria** animação `possession` (ver fichas de Acolyte/Hound/Zealot, Andar 4 comum) — a transformação de fato acontece por um Animation Event dentro dessa animação de possessão de cada um, não no instante em que o Dark Channeler termina o cast. **A transformação não destrói/instancia nada:** é o mesmo GameObject do Acolyte/Hound/Zealot original trocando de sprites (Animator) e de vida/dano pros valores da forma possuída, permanecendo assim até morrer. Sua ameaça vem do fortalecimento constante das criaturas do andar, não de dano direto.
- **Função no combate:** Boss de suporte e escalada de pressão. Quanto mais tempo permanecer vivo, mais perigosa a luta se torna por transformar inimigos básicos em versões fortalecidas.
- **Drops:**
  - Monster Essence: 18.000-90.000 | Chance: 100%
  - Monster Fragment: 3.600-18.000 | Chance: 100%
  - Spirit Dust: 2.400-14.000 | Chance: 100%
  - Arcane Shard: 240-1.200 | Chance: 52,5%
  - Dark Crystal: 120-600 | Chance: 36%
  - Soul Fragment: 48-240 | Chance: 20%
  - Corrupted Core: 24-120 | Chance: 12%
  - Elemental Shard: 24 | Chance: 4%
- **Animações necessárias:** idle, walk, idle_combat, attack/cast, damage e die.
- **Asset de origem:** Minifantasy_Dark_Brotherhood_v1.0
- **🔢 Dano estimado:** 0 (dano direto; ameaça através das transformações) | **🔢 Vida estimada:** 9.000

---

### Andar 5

#### Zombie Giant
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 125.000-750.000 | Chance: 100%
  - Monster Fragment: 25.000-200.000 | Chance: 100%
  - Spirit Dust: 20.000-125.000 | Chance: 100%
  - Arcane Shard: 2.500-17.500 | Chance: 90%
  - Dark Crystal: 1.250-7.500 | Chance: 52,5%
  - Soul Fragment: 500-2.500 | Chance: 30%
  - Corrupted Core: 125-750 | Chance: 16%
  - Elemental Shard: 25-125 | Chance: 8%
  - Ancient Fragment: 25 | Chance: 3,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 7.000 | **🔢 Vida estimada:** 100.000

#### Lich — **Exceção à regra padrão**
- **Tipo:** Ranged especial (mantém animação de ataque)
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em um range onde conseguirá atacar o player com sua magia.
- **Ataque:** Mantém a animação `attack` — cria um círculo de gelo abaixo do player (Animation Event no frame certo aplica dano em quem estiver dentro e deixa lento) e, no mesmo cooldown, sumona zombies e skeletons pra ajudar. Após a morte do Lich, os summons dele também morrem.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar da magia.
- **Drops:**
  - Monster Essence: 125.000-750.000 | Chance: 100%
  - Monster Fragment: 25.000-200.000 | Chance: 100%
  - Spirit Dust: 20.000-125.000 | Chance: 100%
  - Arcane Shard: 2.500-17.500 | Chance: 90%
  - Dark Crystal: 1.250-7.500 | Chance: 52,5%
  - Soul Fragment: 500-2.500 | Chance: 30%
  - Corrupted Core: 125-750 | Chance: 16%
  - Elemental Shard: 25-125 | Chance: 8%
  - Ancient Fragment: 25 | Chance: 3,2%
  - *Observação: summons criados por este boss não geram loot.*
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage, die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.000 | **🔢 Vida estimada:** 75.000

#### Undead Knight
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 110.000-660.000 | Chance: 100%
  - Monster Fragment: 22.000-176.000 | Chance: 100%
  - Spirit Dust: 17.600-110.000 | Chance: 100%
  - Arcane Shard: 2.200-15.400 | Chance: 90%
  - Dark Crystal: 1.100-6.600 | Chance: 52,5%
  - Soul Fragment: 440-2.200 | Chance: 30%
  - Corrupted Core: 110-660 | Chance: 16%
  - Elemental Shard: 22-110 | Chance: 8%
  - Ancient Fragment: 22 | Chance: 3,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.500 | **🔢 Vida estimada:** 75.000

#### Spectre — **Exceção pontual** 🟡 (a superfície de gelo, só isso — o resto é padrão)
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador. `idle` dobra como animação de movimento — sem clipe de `walk` separado, o Override Controller aponta o slot de Walk pro mesmo clipe do Idle. Continua usando o `Base_Melee` compartilhado normalmente.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo — igual a qualquer Melee comum. **Só se o golpe conectar de verdade:** cria uma superfície de gelo persistente no chão, no ponto onde o player foi atingido — condição negativa (velocidade reduzida e/ou dano contínuo, 🔢 valores pendentes de balanceamento). Primeiro caso desse padrão no jogo — "chão com condição negativa após o ataque" reaparece depois no Dragon/Undead Dragon/Dragon Hatchling (fogo) e na Ultimate do Mage (Seção 17) — ainda não existe um sistema genérico unificado pra isso; cada caso é implementado quando o andar/herói correspondente chegar (não é trabalho desta sprint).
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 125.000-750.000 | Chance: 100%
  - Monster Fragment: 25.000-200.000 | Chance: 100%
  - Spirit Dust: 20.000-125.000 | Chance: 100%
  - Arcane Shard: 2.500-17.500 | Chance: 90%
  - Dark Crystal: 1.250-7.500 | Chance: 52,5%
  - Soul Fragment: 500-2.500 | Chance: 30%
  - Corrupted Core: 125-750 | Chance: 16%
  - Elemental Shard: 25-125 | Chance: 8%
  - Ancient Fragment: 25 | Chance: 3,2%
- **Animações necessárias:** `idle` (dobra como movimento — sem `walk` separado), `idle_combat`, `attack` (com Animation Event — spawna a superfície de gelo se acertar), `damage`, `die`.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.000 | **🔢 Vida estimada:** 65.000

#### Headless Horseman
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 110.000-660.000 | Chance: 100%
  - Monster Fragment: 22.000-176.000 | Chance: 100%
  - Spirit Dust: 17.600-110.000 | Chance: 100%
  - Arcane Shard: 2.200-15.400 | Chance: 90%
  - Dark Crystal: 1.100-6.600 | Chance: 52,5%
  - Soul Fragment: 440-2.200 | Chance: 30%
  - Corrupted Core: 110-660 | Chance: 16%
  - Elemental Shard: 22-110 | Chance: 8%
  - Ancient Fragment: 22 | Chance: 3,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.500 | **🔢 Vida estimada:** 70.000

#### Mummy King
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 110.000-660.000 | Chance: 100%
  - Monster Fragment: 22.000-176.000 | Chance: 100%
  - Spirit Dust: 17.600-110.000 | Chance: 100%
  - Arcane Shard: 2.200-15.400 | Chance: 90%
  - Dark Crystal: 1.100-6.600 | Chance: 52,5%
  - Soul Fragment: 440-2.200 | Chance: 30%
  - Corrupted Core: 110-660 | Chance: 16%
  - Elemental Shard: 22-110 | Chance: 8%
  - Ancient Fragment: 22 | Chance: 3,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 5.000 | **🔢 Vida estimada:** 80.000

---

### Andar 6

#### Ancient Danger Leader
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo. Perdeu tanto a pisada em área quanto a sequência de 8 explosões — usa um `attack` padrão como qualquer outro Melee, sem mecânica extra.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.500.000-9.000.000 | Chance: 100%
  - Monster Fragment: 300.000-2.400.000 | Chance: 100%
  - Spirit Dust: 240.000-1.800.000 | Chance: 100%
  - Arcane Shard: 30.000-240.000 | Chance: 100%
  - Dark Crystal: 15.000-120.000 | Chance: 90%
  - Soul Fragment: 6.000-30.000 | Chance: 60%
  - Corrupted Core: 1.500-9.000 | Chance: 30%
  - Elemental Shard: 300-1.800 | Chance: 16%
  - Ancient Fragment: 90-600 | Chance: 12%
  - Infernal Ash: 30-60 | Chance: 2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage, die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 225.000 | **🔢 Vida estimada:** 4.000.000

#### Krampus
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.250.000-7.500.000 | Chance: 100%
  - Monster Fragment: 250.000-2.000.000 | Chance: 100%
  - Spirit Dust: 200.000-1.500.000 | Chance: 100%
  - Arcane Shard: 25.000-200.000 | Chance: 100%
  - Dark Crystal: 12.500-100.000 | Chance: 90%
  - Soul Fragment: 5.000-25.000 | Chance: 60%
  - Corrupted Core: 1.250-7.500 | Chance: 30%
  - Elemental Shard: 250-1.500 | Chance: 16%
  - Ancient Fragment: 75-500 | Chance: 12%
  - Infernal Ash: 25-50 | Chance: 2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 180.000 | **🔢 Vida estimada:** 2.800.000

#### Supreme Elemental
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.500.000-9.000.000 | Chance: 100%
  - Monster Fragment: 300.000-2.400.000 | Chance: 100%
  - Spirit Dust: 240.000-1.800.000 | Chance: 100%
  - Arcane Shard: 30.000-240.000 | Chance: 100%
  - Dark Crystal: 15.000-120.000 | Chance: 90%
  - Soul Fragment: 6.000-30.000 | Chance: 60%
  - Corrupted Core: 1.500-9.000 | Chance: 30%
  - Elemental Shard: 300-1.800 | Chance: 16%
  - Ancient Fragment: 90-600 | Chance: 12%
  - Infernal Ash: 30-60 | Chance: 2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 210.000 | **🔢 Vida estimada:** 3.600.000

---

### Andar 7

#### Dragon — **Exceção à regra padrão**
- **Tipo:** Melee especial (mantém animação de ataque)
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Mantém a animação `attack` — baforada de fogo no chão, com Animation Event no frame certo criando uma área de fogo persistente ao redor do ponto de impacto (dura um tempo até apagar; quem passar por cima leva dano).
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 17.500.000-175.000.000 | Chance: 100%
  - Monster Fragment: 3.500.000-35.000.000 | Chance: 100%
  - Spirit Dust: 2.800.000-24.500.000 | Chance: 100%
  - Arcane Shard: 350.000-3.500.000 | Chance: 100%
  - Dark Crystal: 175.000-1.750.000 | Chance: 100%
  - Soul Fragment: 35.000-350.000 | Chance: 90%
  - Corrupted Core: 10.500-105.000 | Chance: 52,5%
  - Elemental Shard: 1.750-17.500 | Chance: 36%
  - Ancient Fragment: 700-7.000 | Chance: 16%
  - Infernal Ash: 70-350 | Chance: 8%
  - Chaos Crystal: 35 | Chance: 1,6%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage, die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 18.000.000 | **🔢 Vida estimada:** 300.000.000

#### Undead Dragon — **Exceção à regra padrão**
- **Tipo:** Melee especial (mantém animação de ataque)
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Mantém a animação `attack` — baforada de fogo em arco à frente do dragão, com Animation Event no frame certo criando a área de fogo persistente (mesmo padrão do Dragon comum, Andar 7).
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 17.500.000-175.000.000 | Chance: 100%
  - Monster Fragment: 3.500.000-35.000.000 | Chance: 100%
  - Spirit Dust: 2.800.000-24.500.000 | Chance: 100%
  - Arcane Shard: 350.000-3.500.000 | Chance: 100%
  - Dark Crystal: 175.000-1.750.000 | Chance: 100%
  - Soul Fragment: 35.000-350.000 | Chance: 90%
  - Corrupted Core: 10.500-105.000 | Chance: 52,5%
  - Elemental Shard: 1.750-17.500 | Chance: 36%
  - Ancient Fragment: 700-7.000 | Chance: 16%
  - Infernal Ash: 70-350 | Chance: 8%
  - Chaos Crystal: 35 | Chance: 1,6%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage, die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 17.000.000 | **🔢 Vida estimada:** 330.000.000

---

### Andar 8

#### Balrog
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 400.000.000-4.000.000.000 | Chance: 100%
  - Monster Fragment: 80.000.000-800.000.000 | Chance: 100%
  - Spirit Dust: 40.000.000-600.000.000 | Chance: 100%
  - Arcane Shard: 4.000.000-40.000.000 | Chance: 100%
  - Dark Crystal: 2.000.000-20.000.000 | Chance: 100%
  - Soul Fragment: 400.000-4.000.000 | Chance: 100%
  - Corrupted Core: 120.000-1.200.000 | Chance: 90%
  - Elemental Shard: 20.000-200.000 | Chance: 52,5%
  - Ancient Fragment: 4.000-40.000 | Chance: 30%
  - Infernal Ash: 400-4.000 | Chance: 16%
  - Chaos Crystal: 80-800 | Chance: 8%
  - Nightmare Residue: 40 | Chance: 1,2%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 4.200.000.000 | **🔢 Vida estimada:** 54.000.000.000

---

### Andar 9

#### Lobster
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 22.500.000.000-225.000.000.000 | Chance: 100%
  - Monster Fragment: 4.500.000.000-45.000.000.000 | Chance: 100%
  - Spirit Dust: 2.250.000.000-22.500.000.000 | Chance: 100%
  - Arcane Shard: 225.000.000-2.250.000.000 | Chance: 100%
  - Dark Crystal: 90.000.000-900.000.000 | Chance: 100%
  - Soul Fragment: 22.500.000-225.000.000 | Chance: 100%
  - Corrupted Core: 4.500.000-45.000.000 | Chance: 100%
  - Elemental Shard: 900.000-9.000.000 | Chance: 90%
  - Ancient Fragment: 225.000-2.250.000 | Chance: 60%
  - Infernal Ash: 22.500-225.000 | Chance: 30%
  - Chaos Crystal: 4.500-45.000 | Chance: 16%
  - Nightmare Residue: 225-2.250 | Chance: 6%
  - Void Shard: 45 | Chance: 0,64%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.000.000.000.000 | **🔢 Vida estimada:** 28.000.000.000.000

#### Stickman
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 25.000.000.000-250.000.000.000 | Chance: 100%
  - Monster Fragment: 5.000.000.000-50.000.000.000 | Chance: 100%
  - Spirit Dust: 2.500.000.000-25.000.000.000 | Chance: 100%
  - Arcane Shard: 250.000.000-2.500.000.000 | Chance: 100%
  - Dark Crystal: 100.000.000-1.000.000.000 | Chance: 100%
  - Soul Fragment: 25.000.000-250.000.000 | Chance: 100%
  - Corrupted Core: 5.000.000-50.000.000 | Chance: 100%
  - Elemental Shard: 1.000.000-10.000.000 | Chance: 90%
  - Ancient Fragment: 250.000-2.500.000 | Chance: 60%
  - Infernal Ash: 25.000-250.000 | Chance: 30%
  - Chaos Crystal: 5.000-50.000 | Chance: 16%
  - Nightmare Residue: 250-2.500 | Chance: 6%
  - Void Shard: 50 | Chance: 0,64%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.800.000.000.000 | **🔢 Vida estimada:** 32.000.000.000.000

#### Ambuster
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de contato.
- **Ataque:** Golpe real via animação de ataque, com Animation Event — um trigger direcional na frente do monstro aplica o dano só se o player estiver dentro dele no frame exato do evento. Cooldown próprio da animação, sem dano de contato passivo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 27.500.000.000-275.000.000.000 | Chance: 100%
  - Monster Fragment: 5.500.000.000-55.000.000.000 | Chance: 100%
  - Spirit Dust: 2.750.000.000-27.500.000.000 | Chance: 100%
  - Arcane Shard: 275.000.000-2.750.000.000 | Chance: 100%
  - Dark Crystal: 110.000.000-1.100.000.000 | Chance: 100%
  - Soul Fragment: 27.500.000-275.000.000 | Chance: 100%
  - Corrupted Core: 5.500.000-55.000.000 | Chance: 100%
  - Elemental Shard: 1.100.000-11.000.000 | Chance: 90%
  - Ancient Fragment: 275.000-2.750.000 | Chance: 60%
  - Infernal Ash: 27.500-275.000 | Chance: 30%
  - Chaos Crystal: 5.500-55.000 | Chance: 16%
  - Nightmare Residue: 275-2.750 | Chance: 6%
  - Void Shard: 55 | Chance: 0,64%
- **Animações necessárias:** idle, walk, idle_combat, attack (com Animation Event), damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 4.000.000.000.000 | **🔢 Vida estimada:** 36.000.000.000.000

---

### Andar 10

#### Divine God
- **Tipo:** Melee/Ranged híbrido — *(Exceção à regra padrão 🟡: único monstro do jogo que soma a arquitetura completa por Animation Event a uma camada de dano por contato)*
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma explosão (um círculo trigger com o divine god no centro), se ele chegar no alcance do jogador, que causa um dano enorme. Após atacar, possui um pequeno intervalo antes de poder se explodir novamente. (A explosão não causa dano ao divine god). Ele também casta diversos raios, primeiro ele casta (animação castando várias vezes) depois, aparece próximo ao player, várias áreas onde os raios vão cair, esses raios serão um círculo trigger que aparecerá próximo ao player (algo como aleatoriamente em locais próximos e ao redor do player), após a animação dos raios caírem, se o player estiver em um desses círculos, da dano. (São diversos círculos desses que aparecem em uma área muito grande ao redor do player, para que, mesmo players com mobilidade alta, ainda tenham a chance de ser atingido). Além de toda essa mecânica orientada por Animation Event (inalterada), o Divine God também aplica dano por contato normal (cooldown próprio, sem animação dedicada) sempre que o corpo dele encosta no player — uma camada extra de pressão que independe do ciclo de explosão/raios.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5.000.000.000.000-20.000.000.000.000 | Chance: 100%
  - Monster Fragment: 1.000.000.000.000-5.000.000.000.000 | Chance: 100%
  - Spirit Dust: 500.000.000.000-3.000.000.000.000 | Chance: 100%
  - Arcane Shard: 50.000.000.000-300.000.000.000 | Chance: 100%
  - Dark Crystal: 20.000.000.000-120.000.000.000 | Chance: 100%
  - Soul Fragment: 5.000.000.000-30.000.000.000 | Chance: 100%
  - Corrupted Core: 1.000.000.000-8.000.000.000 | Chance: 100%
  - Elemental Shard: 200.000.000-1.500.000.000 | Chance: 100%
  - Ancient Fragment: 50.000.000-400.000.000 | Chance: 100%
  - Infernal Ash: 5.000.000-40.000.000 | Chance: 100%
  - Chaos Crystal: 1.000.000-8.000.000 | Chance: 100%
  - Nightmare Residue: 10.000-100.000 | Chance: 100%
  - Void Shard: 100-500 | Chance: 50%
  - Celestial Fragment: 5-15 | Chance: 75%
  - Divine Core: 1 | Chance: 100%
- **Animações necessárias:** idle(o idle é a própria animação de movimento dele), attack, explosion, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 80.000.000.000.000 | **🔢 Vida estimada:** 2.000.000.000.000.000

## Nota de encerramento — decisão de viabilidade de combate (resolvida na Sprint 16, correção)

O Bestiário assumia, por padrão, o sistema de combate completo desenhado nas Sprints 13-15 (Enemy Framework): timing de ataque configurável (Telegraph → Hitbox Ativa → Recovery), Attack Budget por categoria, e animações reais de `idle/walk/attack/damage/die` (com variantes direcionais `attack_orthogonal`/`attack_diagonal` nalguns rangeds). Isso foi testado na prática, com Animator real (não placeholder), em 3 monstros representativos (Rat, Goblin, Rat People) na Sprint 16.

**Primeira rodada — contingência acionada.** A contingência já registrada no GDD Seção 22 ("Contingência — simplificação de monstros comuns") foi acionada: o Bestiário passou a seguir, por padrão, o modelo totalmente simplificado — Melee por contato, Ranged por auto-disparo em alcance, ambos com cooldown próprio, sem timing de Telegraph/Hitbox/Recovery, sem `attack` nenhum e sem Attack Budget.

**Segunda rodada — meio-termo final.** Depois de testar esse modelo puro na prática (Sprint 16, correção), a simplificação total não ficou boa — faltava a identidade visual do ataque, e a arte já estava pronta pra usar. A decisão final, e a que vale hoje em todo este documento: **todo monstro comum e a maioria dos bosses voltam a ter uma animação `attack`/conjuração real, com Animation Event decidindo o instante do golpe/disparo**, mas sem o custo do telegraph completo — pro Melee, um trigger direcional simples (4 posições) substitui o cálculo de esquiva por reposicionamento; pro Ranged, o projétil nasce direto na posição real do jogador, sem posição travada. **O dano de contato passivo foi removido de vez** (não coexiste mais com o golpe real) — a única exceção permanente são os Slimes (comuns e Mother Slime Green/Blue), que ficam só no contato, sem `attack`, pra sempre. Attack Budget continua removido em definitivo. Ver a regra padrão completa no topo deste documento.

**Isso não significa "sem exceção nenhuma".** Monstros com uma mecânica genuinamente distinta do padrão (além dos Slimes) continuam documentados individualmente com sua própria arquitetura, orientada por Animation Events reais: Goblin Sapper, Orc Shaman (só o totem — o Shaman em si segue o padrão), Burning Skull, Serpent e o projétil do Bicephalous. Skeleton Rider deixou de ser exceção (perdeu a mecânica de gerar 2 monstros ao morrer) e hoje segue o padrão comum.

**A mesma regra híbrida vale pros 30 bosses do jogo.** A maioria mantém `attack` real (Goblin King, Centaur King, Cave Troll, Giant, Pale Champion, Wise Orc, Flagelant, Ritual Guard, Zombie Giant, Undead Knight, Headless Horseman, Mummy King, Ancient Danger Leader, Krampus, Supreme Elemental, Balrog, Lobster, Stickman, Ambuster) — perderam só a complexidade extra que tinham antes (telegraph, área, múltiplos hits), não a animação em si. **`Spectre` estava pendente e foi resolvido:** também ganha `attack` real, sem manter mais a economia de "3 animações só" — a única particularidade que sobra é o `idle` dobrando como `walk` (mesmo clipe nos dois slots do Override Controller) e um efeito próprio no golpe (ver ficha, Andar 5 — cria uma superfície de gelo se acertar, primeiro caso de "chão com condição negativa" do jogo, padrão que volta a aparecer no Dragon/Undead Dragon/Dragon Hatchling). Um pequeno grupo manteve arquitetura própria por Animation Event além do padrão, seja porque a mecânica não faz sentido sem ela ou porque é a própria identidade do boss: Mother Slime Green/Blue (só contato, como os Slimes comuns, mais o spawn de 3 filhotes em posições fixas), Rat People Royalty (`throw_ratpeople`), Spider Queen (mantém a teia, mas sem animação própria — dispara como um Ranged), Dark Channeler (transforma os 3 Acolyte/Hound/Zealot mais próximos), Lich, Dragon, Undead Dragon e Divine God — este último ganhando, além disso, uma camada extra de dano por contato somada à sua arquitetura completa (único boss que ainda tem contato).

**Sprint de referência:** Sprint 16 — ver `docs/sprints/sprint-16.md` pro relatório completo (todas as fases, reversões e bugs corrigidos) e `docs/sprint-16-correcao-task-breakdown.md` pro task breakdown da correção (pivô pro modelo de contato puro, já superado por esta revisão).