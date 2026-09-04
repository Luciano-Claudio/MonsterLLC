# Bestiário — Fichas de Monstros e Bosses

> **Documento Especializado**, referenciado pelo GDD Mestre (Seção 22 — arquitetura de comportamento — e Seção 51 — lista de documentos especializados). Fonte: documento de visão original do jogo, migrado para cá para deixar de depender de um PDF solto. O GDD Mestre Seção 22 continua sendo a fonte de verdade da **arquitetura** de combate (categorias, timing de ataque, Attack Budget) que toda criatura listada aqui precisa seguir quando implementada.

## Regras de balanceamento (aplicam-se a todas as fichas abaixo)

- Os valores de drop são valores-base por abate, antes de bônus de run, employees ou multiplicadores futuros.
- Itens liberados em andares inferiores continuam disponíveis nos andares superiores.
- Cada item de drop faz sua própria rolagem de chance, independente dos outros — um único monstro pode dropar vários tipos de item ao mesmo tempo (GDD Seção 38).
- 🔢 Todos os valores de Dano/Vida estimados são referência do documento original — sujeitos a ajuste em playtest, não são valores finais travados.
- 🔢 Nenhuma ficha abaixo define o timing exato de ataque (Telegraph/Hitbox/Recovery, GDD Seção 22) — só descreve o comportamento geral ("após atacar, possui um pequeno intervalo"). Os valores exatos de timing por criatura são pendência de balanceamento, preenchidos conforme cada uma for implementada.
- 🟡 **Pendência de design em aberto, ligada à Seção 22 do GDD:** a viabilidade de manter timing/telegraph completo com animação real para todas essas criaturas (em vez de simplificar para o estilo Vampire Survivors — perseguição + dano por contato) depende de um momento de teste dedicado, ainda não realizado. Ver nota ao final deste documento.

---

## Monstros Comuns

### Andar 1

#### Rat
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Wolf
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 8-30 | Chance: 100%
  - Monster Fragment: 2-8 | Chance: 70%
  - Spirit Dust: 2-4 | Chance: 50%
  - Arcane Shard: 2 | Chance: 2%
  - Dark Crystal: 2 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 3 | **🔢 Vida estimada:** 10

#### Bat
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Slime Green
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Pula e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

#### Slime Blue
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Pula e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 5-20 | Chance: 100%
  - Monster Fragment: 1-5 | Chance: 70%
  - Spirit Dust: 1-3 | Chance: 50%
  - Arcane Shard: 1 | Chance: 2%
  - Dark Crystal: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 2 | **🔢 Vida estimada:** 5

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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 6 | **🔢 Vida estimada:** 30

#### Goblin
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 30-120 | Chance: 100%
  - Monster Fragment: 5-20 | Chance: 85%
  - Spirit Dust: 3-10 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 6 | **🔢 Vida estimada:** 30

#### Goblin Raider
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma tocha em direção ao player, essa tocha explode ao contato (ou quando chega no limite de distância que ela percorre) causando um pequeno dano em uma área circular, caso o player estiver nessa área, da dano nele. Após atacar, possui um pequeno intervalo antes de poder lançar outra tocha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis
- **Drops:**
  - Monster Essence: 38-150 | Chance: 100%
  - Monster Fragment: 6-25 | Chance: 85%
  - Spirit Dust: 4-12 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 7 | **🔢 Vida estimada:** 35

#### Goblin Sapper
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de largar a bomba. Enquanto não detectar o jogador, fica andando aleatoriamente no andar. Após largar a bomba, ele corre para longe do jogador, ficando num range ainda visível, até ganhar a bomba novamente e repetir o ataque
- **Ataque:** Solta uma bomba próximo ao player e sai correndo. Após atacar, possui um pequeno intervalo antes de ganhar a bomba novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 38-150 | Chance: 100%
  - Monster Fragment: 6-25 | Chance: 85%
  - Spirit Dust: 4-12 | Chance: 70%
  - Arcane Shard: 1-2 | Chance: 6%
  - Dark Crystal: 1 | Chance: 2%
  - Soul Fragment: 1 | Chance: 0,8%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 15 | **🔢 Vida estimada:** 25

### Andar 3

#### Centaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 150-700 | Chance: 100%
  - Monster Fragment: 30-120 | Chance: 95%
  - Spirit Dust: 20-80 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 15 | **🔢 Vida estimada:** 110

#### Minotaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um pulo quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder pular novamente. Esse pulo, causa um dano em área (um círculo trigger, que após o pulo acontecer, da dano no player se ele tiver dentro desse círculo. Esse círculo é criado a partir do centro do minotaur.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 210-980 | Chance: 100%
  - Monster Fragment: 42-168 | Chance: 95%
  - Spirit Dust: 28-112 | Chance: 85%
  - Arcane Shard: 3-7 | Chance: 15%
  - Dark Crystal: 1-4 | Chance: 7%
  - Soul Fragment: 1-3 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 20 | **🔢 Vida estimada:** 120

#### Gnoll
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 150-700 | Chance: 100%
  - Monster Fragment: 30-120 | Chance: 95%
  - Spirit Dust: 20-80 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 15 | **🔢 Vida estimada:** 110

#### Spider
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 150-700 | Chance: 100%
  - Monster Fragment: 30-120 | Chance: 95%
  - Spirit Dust: 20-80 | Chance: 85%
  - Arcane Shard: 2-5 | Chance: 15%
  - Dark Crystal: 1-3 | Chance: 7%
  - Soul Fragment: 1-2 | Chance: 3%
  - Corrupted Core: 1 | Chance: 0,7%
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 15 | **🔢 Vida estimada:** 100

#### Ancient Troll
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um pulo quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder pular novamente. Esse pulo, causa um dano em área (um círculo trigger, que após o pulo acontecer, da dano no player se ele tiver dentro desse círculo. Esse círculo é criado a partir do centro do ancient troll.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 240-1.120 | Chance: 100%
  - Monster Fragment: 48-192 | Chance: 95%
  - Spirit Dust: 32-128 | Chance: 85%
  - Arcane Shard: 3-8 | Chance: 15%
  - Dark Crystal: 2-5 | Chance: 7%
  - Soul Fragment: 2-3 | Chance: 3%
  - Corrupted Core: 2 | Chance: 0,7%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 25 | **🔢 Vida estimada:** 140

### Andar 4

#### Warg
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 800-4.000 | Chance: 100%
  - Monster Fragment: 150-800 | Chance: 100%
  - Spirit Dust: 100-600 | Chance: 95%
  - Arcane Shard: 10-50 | Chance: 35%
  - Dark Crystal: 5-25 | Chance: 18%
  - Soul Fragment: 2-10 | Chance: 10%
  - Corrupted Core: 1-5 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 70 | **🔢 Vida estimada:** 500

#### Orc Blade
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 800-4.000 | Chance: 100%
  - Monster Fragment: 150-800 | Chance: 100%
  - Spirit Dust: 100-600 | Chance: 95%
  - Arcane Shard: 10-50 | Chance: 35%
  - Dark Crystal: 5-25 | Chance: 18%
  - Soul Fragment: 2-10 | Chance: 10%
  - Corrupted Core: 1-5 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 80 | **🔢 Vida estimada:** 540

#### Orc Rider
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar. Após morrer, aparecerá um warg e um orc blade.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.120-5.600 | Chance: 100%
  - Monster Fragment: 210-1.120 | Chance: 100%
  - Spirit Dust: 140-840 | Chance: 95%
  - Arcane Shard: 14-70 | Chance: 35%
  - Dark Crystal: 7-35 | Chance: 18%
  - Soul Fragment: 3-14 | Chance: 10%
  - Corrupted Core: 1-7 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 100 | **🔢 Vida estimada:** 580

#### Orc Shaman
- **Tipo:** Suport/Ranged
- **Movimentação:** 
- **Comportamento:** Ao detectar o jogador ou outra unidade com vida < 100%, aproxima-se e tenta permanecer em um range onde conseguirá colocar seus totems. Enquanto não detectar o jogador ou outra unidade com vida < 100%, fica andando aleatoriamente no andar.
- **Ataque:** Cria 3 tipos de totem: 1° o totem de heal, ele coloca nos pés de unidades inimigas que tenham a vida < 100%. 2° O totem de fire, ele coloca nos pés do jogador causando dano nele. 3° O totem de ice, coloca nos pés do jogador fazendo com que ele fique stunado por alguns segundos. A ordem de decisão dos totens é a seguinte: Heal, Fire, Ice. Se a de heal tiver em cooldown, ele coloca a de fire, se a de fire estiver em cooldown ele coloca a de gelo.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando e inimigo de suporte, focado em curar os monstros inimigos.
- **Drops:**
  - Monster Essence: 960-4.800 | Chance: 100%
  - Monster Fragment: 180-960 | Chance: 100%
  - Spirit Dust: 120-720 | Chance: 95%
  - Arcane Shard: 12-60 | Chance: 35%
  - Dark Crystal: 6-30 | Chance: 18%
  - Soul Fragment: 2-12 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 65 | **🔢 Vida estimada:** 530

#### Warbreed Blade
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 880-4.400 | Chance: 100%
  - Monster Fragment: 165-880 | Chance: 100%
  - Spirit Dust: 110-660 | Chance: 95%
  - Arcane Shard: 11-55 | Chance: 35%
  - Dark Crystal: 6-28 | Chance: 18%
  - Soul Fragment: 2-11 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 90 | **🔢 Vida estimada:** 600

#### Warbreed Berserker
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.120-5.600 | Chance: 100%
  - Monster Fragment: 210-1.120 | Chance: 100%
  - Spirit Dust: 140-840 | Chance: 95%
  - Arcane Shard: 14-70 | Chance: 35%
  - Dark Crystal: 7-35 | Chance: 18%
  - Soul Fragment: 3-14 | Chance: 10%
  - Corrupted Core: 1-7 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 110 | **🔢 Vida estimada:** 650

#### Warbreed Phalanx
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque (que é mais alto um pouco pq ele possui uma lança). Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um ataque com a lança quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder atacar novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 1.040-5.200 | Chance: 100%
  - Monster Fragment: 195-1.040 | Chance: 100%
  - Spirit Dust: 130-780 | Chance: 95%
  - Arcane Shard: 13-65 | Chance: 35%
  - Dark Crystal: 6-32 | Chance: 18%
  - Soul Fragment: 3-13 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 90 | **🔢 Vida estimada:** 700

#### Orc Scout
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha em direção ao player. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 800-4.000 | Chance: 100%
  - Monster Fragment: 150-800 | Chance: 100%
  - Spirit Dust: 100-600 | Chance: 95%
  - Arcane Shard: 10-50 | Chance: 35%
  - Dark Crystal: 5-25 | Chance: 18%
  - Soul Fragment: 2-10 | Chance: 10%
  - Corrupted Core: 1-5 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 80 | **🔢 Vida estimada:** 510

#### Warbreed Arbalist
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança uma flecha em direção ao player. Após atacar, possui um pequeno intervalo antes de poder lançar outra flecha novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
- **Drops:**
  - Monster Essence: 960-4.800 | Chance: 100%
  - Monster Fragment: 180-960 | Chance: 100%
  - Spirit Dust: 120-720 | Chance: 95%
  - Arcane Shard: 12-60 | Chance: 35%
  - Dark Crystal: 6-30 | Chance: 18%
  - Soul Fragment: 2-12 | Chance: 10%
  - Corrupted Core: 1-6 | Chance: 3%
  - Elemental Shard: 1 | Chance: 0,5%
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 95 | **🔢 Vida estimada:** 540

### Andar 5

#### Skeleton
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 800 | **🔢 Vida estimada:** 2.200

#### Headless Skeleton
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 2.200

#### Jumping Skull
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 1.760

#### Skeletal Horse
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 960 | **🔢 Vida estimada:** 2.860

#### Skeleton Rider
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar. Após morrer, aparecerá um skeletal horse e um skeleton.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - normalmente.
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.040 | **🔢 Vida estimada:** 3.080

#### Skeleton Minotaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma cabeçada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder cabecear novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.120 | **🔢 Vida estimada:** 3.960

#### Zombie Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque (que é mais alto um pouco pq ele possui uma lança). Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um ataque com a lança quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder atacar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 960 | **🔢 Vida estimada:** 2.860

#### Zombie Bear
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.040 | **🔢 Vida estimada:** 3.960

#### Zombie Minotaur
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um murro quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder dar um murro novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.200 | **🔢 Vida estimada:** 4.400

#### Ghost
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 880 | **🔢 Vida estimada:** 1.650

#### Skeleton Mage
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde conseguirá atacar o player com sua magia. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Cria um círculo abaixo do player e quando a animação acaba, da dano nessa área desse círculo, apenas no jogador. Após atacar, possui um pequeno intervalo antes de poder lançar outra magia novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar da magia.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 920 | **🔢 Vida estimada:** 1.760

#### Zombie Mage
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde conseguirá atacar o player com sua magia. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Cria um círculo abaixo do player e quando a animação acaba, da dano nessa área desse círculo, apenas no jogador. Após atacar, possui um pequeno intervalo antes de poder lançar outra magia novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar da magia.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.120 | **🔢 Vida estimada:** 1.650

#### Flying Skull
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um ataque chamado Scare Spell quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder atacar novamente. O Scare Spell é extremamente simples, ele é uma ataque em uma área pequena do Flying Skull. Criará-se um círculo trigger ao redor dele e o dano será causado se o jogador estiver nesse círculo na hora da animação.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Enchanted_Companions_v1.0
- **🔢 Dano estimado:** 1.000 | **🔢 Vida estimada:** 1.760

#### Gargoyle
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica parado, como se fosse parte do cenário da sala.
- **Ataque:** Executa uma cabeçada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder cabecear novamente.
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
- **Animações necessárias:** idle, walk, activation, desactivation, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 1.120 | **🔢 Vida estimada:** 3.300

#### Wraith
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma dupla espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro), ela terá um duplo dano, um de ida e um de volta (pq na animação ele ataca uma vez com a espada e volta a espada pro local, atacando novamente de trás para frente), ou seja, se o player continuar nessa área, sofrerá um duplo ataque. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 1.440 | **🔢 Vida estimada:** 2.420

#### Mummy
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 800 | **🔢 Vida estimada:** 2.640

#### Burning Skull
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma explosão (um circulo trigger com o burning skull no centro), se ele chegar no alcance do jogador, que causa um dano enorme e mata o burning skull
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
- **Animações necessárias:** idle, walk, explosion, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.400 | **🔢 Vida estimada:** 1.100

#### Skeleton Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Undead_Creatures_v1.1
- **🔢 Dano estimado:** 1.040 | **🔢 Vida estimada:** 3.080

### Andar 6

#### Ancient Danger Heavy Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 26.000 | **🔢 Vida estimada:** 96.000

#### Armored Warrior
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 20.000 | **🔢 Vida estimada:** 108.000

#### Rock Golem
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um pulo quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder pular novamente. Esse pulo, causa um dano em área (um círculo trigger, que após o pulo acontecer, da dano no player se ele tiver dentro desse círculo. Esse círculo é criado a partir do centro do rock golem.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 24.000 | **🔢 Vida estimada:** 45.000

#### Water Elemental
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma explosão (um circulo trigger com o water elemental no centro), se ele chegar no alcance do jogador, que causa um dano enorme. Após atacar, possui um pequeno intervalo antes de poder se explodir novamente. (A explosão não causa dano ao water elemental)
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
- **Animações necessárias:** idle, walk, explosion, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 32.000 | **🔢 Vida estimada:** 60.000

#### Earth Elemental
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma patada quando entra no alcance corpo a corpo (essa patada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a patada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 24.000 | **🔢 Vida estimada:** 108.000

#### Ancient Danger
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 28.000 | **🔢 Vida estimada:** 90.000

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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Enchanted_Companions_v1.0
- **🔢 Dano estimado:** 1.650.000 | **🔢 Vida estimada:** 4.000.000

#### Magma Hound
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 1.500.000 | **🔢 Vida estimada:** 4.000.000

#### Magma Orc
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do orc). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 1.800.000 | **🔢 Vida estimada:** 6.000.000

#### Magma Golem
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma pancada com as duas mão no chão no pé do player quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar essa pancada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.250.000 | **🔢 Vida estimada:** 10.000.000

#### Dragon Hatchling
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma baforada de fogo no chão no pé do player quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar essa baforada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.100.000 | **🔢 Vida estimada:** 7.000.000

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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 300.000.000 | **🔢 Vida estimada:** 600.000.000

#### Armored Demon
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 390.000.000 | **🔢 Vida estimada:** 2.000.000.000

### Andar 9

#### Observer
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma cabeçada quando entra no alcance corpo a corpo (essa cabeçada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a cabeçada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 200.000.000.000 | **🔢 Vida estimada:** 700.000.000.000

#### Serpent
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, ele submerge no solo (onde só é possível ver uma animação dele debaixo do solo caminhando).
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque, quando ele submerge e ataca o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar, debaixo do solo.
- **Ataque:** Executa uma cabeçada quando entra no alcance corpo a corpo (essa cabeçada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a cabeçada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
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
- **Animações necessárias:** idle, walk, emerge, submerge, attack, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 240.000.000.000 | **🔢 Vida estimada:** 770.000.000.000

#### Shifted
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma patada quando entra no alcance corpo a corpo (essa patada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a patada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 260.000.000.000 | **🔢 Vida estimada:** 980.000.000.000

#### Stalker
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma cabeçada quando entra no alcance corpo a corpo (essa cabeçada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a cabeçada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 240.000.000.000 | **🔢 Vida estimada:** 560.000.000.000

#### Worm
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa um dash para frente quando entra no alcance corpo a corpo (essa dash, causa um dano na trajetória feita por esse monstro). Após atacar, possui um pequeno intervalo antes de poder usar a dash novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 300.000.000.000 | **🔢 Vida estimada:** 700.000.000.000

#### Bicephalous
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em um range onde seu projétil conseguirá acertar o player. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Lança um slug em direção ao player, essa slug da dano no player se ele encostar e se ele encostar no player (ou quando chega no limite de distância que ela percorre) nasce um slug. Após atacar, possui um pequeno intervalo antes de poder lançar outra slug novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando, principalmente para desviar dos projéteis.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 240.000.000.000 | **🔢 Vida estimada:** 770.000.000.000

### Andar 10

#### Divine Minion
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
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
- **Animações necessárias:** idle, walk, attack_orthogonal, attack_diagonal, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 20.000.000.000.000 | **🔢 Vida estimada:** 45.000.000.000.000

#### Divine Guardian
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 18.000.000.000.000 | **🔢 Vida estimada:** 75.000.000.000.000

#### Divine Angel
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 22.000.000.000.000 | **🔢 Vida estimada:** 60.000.000.000.000 Especiais / sem spawn automático por andar

### Especiais / sem spawn automático por andar

#### Chest Mimic
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ao detectar o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Enquanto não detectar o jogador, fica andando aleatoriamente no andar.
- **Ataque:** Corre em direção ao player e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo troll, criado para mostrar ao jogador que nem tudo é seguro na torre.
- **Drops:**
  - Pergaminho do baú: 1 | Chance: 100%
- **Animações necessárias:** idle, walk, damage, die e activation.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** Escala por andar (aprox. 1,3x o dano de um monstro médio do andar). | **🔢 Vida estimada:** Escala por andar (aprox. 1,8x a vida de um monstro médio do andar).

#### Slug
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Após nascer persegue o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo.
- **Ataque:** Se rasteja e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Animações necessárias:** idle, walk, damage e die.
- **Asset de origem:** Minifantasy_Nightmare_Creatures_v1.1
- **🔢 Dano estimado:** 80.000.000.000 | **🔢 Vida estimada:** 120.000.000.000  Bosses

---

## Bosses

Regras gerais de Boss (GDD Seção 22): possuem mais de um ataque com cooldowns próprios; não participam do Attack Budget; aparecem por Boss Timer individual por Floor (🔢 referência ~50s); matar o boss de topo (Divine God) não encerra a run.

### Andar 1

#### Mother Slime Green
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Após morrer, faz nascer 3 slimes greens normais.
- **Ataque:** Pula e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 50-200 | Chance: 100%
  - Monster Fragment: 10-50 | Chance: 100%
  - Spirit Dust: 10-30 | Chance: 75%
  - Arcane Shard: 10 | Chance: 8%
  - Dark Crystal: 10 | Chance: 4%
- **Animações necessárias:** idle, walk, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 8 | **🔢 Vida estimada:** 120

#### Mother Slime Blue
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador pulando
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em corpo a corpo. Após morrer, faz nascer 3 slimes blues normais.
- **Ataque:** Pula e ao contato causa dano no jogador. Após o dano acontecer, possui um pequeno intervalo até que possa dar dano novamente
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 50-200 | Chance: 100%
  - Monster Fragment: 10-50 | Chance: 100%
  - Spirit Dust: 10-30 | Chance: 75%
  - Arcane Shard: 10 | Chance: 8%
  - Dark Crystal: 10 | Chance: 4%
- **Animações necessárias:** idle, walk, damage e die.
- **Asset de origem:** Minifantasy_Creatures_v3.3_Commercial_Version
- **🔢 Dano estimado:** 8 | **🔢 Vida estimada:** 120

### Andar 2

#### Rat People Royalty
- **Tipo:** Melee/Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente. Ele também joga um Rat People no player, fazendo surgir um Rat People e dando dano na trajetória do Rat people lançado.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 360-1.440 | Chance: 100%
  - Monster Fragment: 60-240 | Chance: 100%
  - Spirit Dust: 36-120 | Chance: 100%
  - Arcane Shard: 12-24 | Chance: 12%
  - Dark Crystal: 12 | Chance: 8%
  - Soul Fragment: 12 | Chance: 6,4%
  - Observação: summons criados por este boss não geram loot.
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 30 | **🔢 Vida estimada:** 250

#### Goblin King
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 450-1.800 | Chance: 100%
  - Monster Fragment: 75-300 | Chance: 100%
  - Spirit Dust: 45-150 | Chance: 100%
  - Arcane Shard: 15-30 | Chance: 12%
  - Dark Crystal: 15 | Chance: 8%
  - Soul Fragment: 15 | Chance: 6,4%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 36 | **🔢 Vida estimada:** 325

### Andar 3

#### Werewolf
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador, utilizando velocidade superior à de inimigos básicos.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 2.250-10.500 | Chance: 100%
  - Monster Fragment: 450-1.800 | Chance: 100%
  - Spirit Dust: 300-1.200 | Chance: 100%
  - Arcane Shard: 30-75 | Chance: 30%
  - Dark Crystal: 15-45 | Chance: 14%
  - Soul Fragment: 15-30 | Chance: 12%
  - Corrupted Core: 15 | Chance: 5,6%
- **Animações necessárias:** idle, walk, attack, damage e die
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 90 | **🔢 Vida estimada:** 960

#### Centaur King
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 2.250-10.500 | Chance: 100%
  - Monster Fragment: 450-1.800 | Chance: 100%
  - Spirit Dust: 300-1.200 | Chance: 100%
  - Arcane Shard: 30-75 | Chance: 30%
  - Dark Crystal: 15-45 | Chance: 14%
  - Soul Fragment: 15-30 | Chance: 12%
  - Corrupted Core: 15 | Chance: 5,6%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 110 | **🔢 Vida estimada:** 1.040

#### Cave Troll
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma pancada com as duas mão no chão no pé do player quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar essa pancada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 3.000-14.000 | Chance: 100%
  - Monster Fragment: 600-2.400 | Chance: 100%
  - Spirit Dust: 400-1.600 | Chance: 100%
  - Arcane Shard: 40-100 | Chance: 30%
  - Dark Crystal: 20-60 | Chance: 14%
  - Soul Fragment: 20-40 | Chance: 12%
  - Corrupted Core: 20 | Chance: 5,6%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 130 | **🔢 Vida estimada:** 1.440

#### Spider Queen
- **Tipo:** Melee/Ranged
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma mordida curta quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder morder novamente. Ele também lança teias se o jogador estiver ranged da spider queen! Essas teias predem o jogador (fica impossibilitado de andar por alguns segundos) e fazem com que a spider queen continue se aproximando dele. Esse ataque ranged acontece e enquanto ele está em cooldown a spider queen volta a perseguir o player normalmente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 2.700-12.600 | Chance: 100%
  - Monster Fragment: 540-2.160 | Chance: 100%
  - Spirit Dust: 360-1.440 | Chance: 100%
  - Arcane Shard: 36-90 | Chance: 30%
  - Dark Crystal: 18-54 | Chance: 14%
  - Soul Fragment: 18-36 | Chance: 12%
  - Corrupted Core: 18 | Chance: 5,6%
- **Animações necessárias:** idle, walk, attack, shotweb, damage e die
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 100 | **🔢 Vida estimada:** 1.200

### Andar 4

#### Giant
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma pisada no chão quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder pular novamente. Essa pisada, causa um dano em área (um círculo trigger, que após a pisada acontecer, da dano no player se ele tiver dentro desse círculo. Esse círculo é criado a partir do centro do giant).
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 16.000-80.000 | Chance: 100%
  - Monster Fragment: 3.000-16.000 | Chance: 100%
  - Spirit Dust: 2.000-12.000 | Chance: 100%
  - Arcane Shard: 200-1.000 | Chance: 52,5%
  - Dark Crystal: 100-500 | Chance: 36%
  - Soul Fragment: 40-200 | Chance: 20%
  - Corrupted Core: 20-100 | Chance: 12%
  - Elemental Shard: 20 | Chance: 4%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 560 | **🔢 Vida estimada:** 10.000

#### Pale Champion
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 14.400-72.000 | Chance: 100%
  - Monster Fragment: 2.700-14.400 | Chance: 100%
  - Spirit Dust: 1.800-10.800 | Chance: 100%
  - Arcane Shard: 180-900 | Chance: 52,5%
  - Dark Crystal: 90-450 | Chance: 36%
  - Soul Fragment: 36-180 | Chance: 20%
  - Corrupted Core: 18-90 | Chance: 12%
  - Elemental Shard: 18 | Chance: 4%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Dark_Orc_Army_v1.0
- **🔢 Dano estimado:** 480 | **🔢 Vida estimada:** 7.000

#### Wise Orc
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
- **Função no combate:** Inimigo de pressão, criado para obrigar o jogador a continuar se movimentando.
- **Drops:**
  - Monster Essence: 14.400-72.000 | Chance: 100%
  - Monster Fragment: 2.700-14.400 | Chance: 100%
  - Spirit Dust: 1.800-10.800 | Chance: 100%
  - Arcane Shard: 180-900 | Chance: 52,5%
  - Dark Crystal: 90-450 | Chance: 36%
  - Soul Fragment: 36-180 | Chance: 20%
  - Corrupted Core: 18-90 | Chance: 12%
  - Elemental Shard: 18 | Chance: 4%
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 400 | **🔢 Vida estimada:** 6.500

### Andar 5

#### Zombie Giant
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma pancada com as duas mão no chão no pé do player quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar essa pancada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** Minifantasy_Monster_Creatures_v1.0
- **🔢 Dano estimado:** 7.000 | **🔢 Vida estimada:** 100.000

#### Lich
- **Tipo:** Ranged
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em um range onde conseguirá atacar o player com sua magia.
- **Ataque:** Cria um círculo de gelo abaixo do player e da dano nessa área desse círculo, apenas no jogador, além de deixar o jogador com lentidão. Após atacar, possui um pequeno intervalo antes de poder lançar outra magia novamente. Ele também sumona zombies e skeletons para ajudar ele. Após a morte do lich os summons dele morre também
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
  - Observação: summons criados por este boss não geram loot.
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.000 | **🔢 Vida estimada:** 75.000

#### Undead Knight
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque (que é mais alto um pouco pq ele possui uma lança).
- **Ataque:** Executa um ataque com a lança quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder atacar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.500 | **🔢 Vida estimada:** 75.000

#### Spectre
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, aparece no chão do player, caso ele for acertado, um circulo de gelo, que da dano novamente e deixa o jogador lento. Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle(o idle é a própria animação de movimento dele), attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.000 | **🔢 Vida estimada:** 65.000

#### Headless Horseman
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 6.500 | **🔢 Vida estimada:** 70.000

#### Mummy King
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma arranhada quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder arranhar novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 5.000 | **🔢 Vida estimada:** 80.000

### Andar 6

#### Ancient Danger Leader
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma pisada no chão quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder pular novamente. Essa pisada, causa um dano em área (um círculo trigger, que após o pulo acontecer, da dano no player se ele tiver dentro desse círculo. Esse círculo é criado a partir do centro do Ancient Danger Leader). Após essa pisada acontecer, vai acontecer 8 explosões (círculos trigger) ao redor do Ancient Danger Leader, essas explosões são animações que após acontecerem, vai dar dano em quem estiver nesse local.
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
- **Animações necessárias:** idle, walk, attack, damage e die (além da animação da explosão).
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 225.000 | **🔢 Vida estimada:** 4.000.000

#### Krampus
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 180.000 | **🔢 Vida estimada:** 2.800.000

#### Supreme Elemental
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma patada quando entra no alcance corpo a corpo (essa patada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a patada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 210.000 | **🔢 Vida estimada:** 3.600.000

### Andar 7

#### Dragon
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma baforada de fogo no chão no pé do player quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar essa baforada novamente. Essa baforada, cria uma área de fogo ao redor, que fica queimando por bastante tempo até apagar. Se o jogador passar por essa área, leva dano.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 18.000.000 | **🔢 Vida estimada:** 300.000.000

#### Undead Dragon
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma baforada de fogo no chão (um arco na frente do dragão) quando entra no alcance corpo a corpo. Após atacar, possui um pequeno intervalo antes de poder usar essa baforada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 17.000.000 | **🔢 Vida estimada:** 330.000.000

### Andar 8

#### Balrog
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 4.200.000.000 | **🔢 Vida estimada:** 54.000.000.000

### Andar 9

#### Lobster
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma espadada quando entra no alcance corpo a corpo (essa espadada, causa um dano em um arco na frente do monstro). Após atacar, possui um pequeno intervalo antes de poder usar a espada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.000.000.000.000 | **🔢 Vida estimada:** 28.000.000.000.000

#### Stickman
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma patada quando entra no alcance corpo a corpo (essa patada, causa um dano em um arco na frente do monstro). Após atacar, ele cria vários espinhos ao redor dele que causa dano em um círculo trigger com ele no centro. Após atacar, possui um pequeno intervalo antes de poder usar a patada novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 2.800.000.000.000 | **🔢 Vida estimada:** 32.000.000.000.000

#### Ambuster
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa um ataque triplo, onde da 3 danos durante a animação. Primeiro ele pula para cima do jogador, depois ele arranha o chão e puxa esse arranhão para trás. Durante esses 3 estágios, o player leva dano. Para facilitar, podemos criar um círculo trigger no local onde caí esse pulo, e esse círculo causa dano 3 vezes com um tempo separando eles. Após atacar, possui um pequeno intervalo antes de poder usar o pulo novamente.
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
- **Animações necessárias:** idle, walk, attack, damage e die.
- **Asset de origem:** All_Exclusives_20260612
- **🔢 Dano estimado:** 4.000.000.000.000 | **🔢 Vida estimada:** 36.000.000.000.000

### Andar 10

#### Divine God
- **Tipo:** Melee
- **Movimentação:** Persegue diretamente o jogador.
- **Comportamento:** Ele detecta automaticamente o jogador, aproxima-se agressivamente e tenta permanecer em alcance de ataque.
- **Ataque:** Executa uma explosão (um círculo trigger com o divine god no centro), se ele chegar no alcance do jogador, que causa um dano enorme. Após atacar, possui um pequeno intervalo antes de poder se explodir novamente. (A explosão não causa dano ao divine god). Ele também casta diversos raios, primeiro ele casta (animação castando várias vezes) depois, aparece próximo ao player, várias áreas onde os raios vão cair, esses raios serão um círculo trigger que aparecerá próximo ao player (algo como aleatoriamente em locais próximos e ao redor do player), após a animação dos raios caírem, se o player estiver em um desses círculos, da dano. (São diversos círculos desses que aparecem em uma área muito grande ao redor do player, para que, mesmo players com mobilidade alta, ainda tenham a chance de ser atingido)
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
- **🔢 Dano estimado:** 80.000.000.000.000 | **🔢 Vida estimada:** 2.000.000.000.000.000 Ataque Como ameaça o jogador Espada/mordida Pune ficar perto Lança Pune manter distância média frontal Projétil Obriga movimento lateral Explosão Obriga sair de uma área Ataque no chão Pune ficar parado Dash Corta uma rota Stun Torna outros monstros perigosos Summon Aumenta pressão com o tempo Cura Faz certas unidades virarem prioridade E não faça os inimigos acompanharem o jogador durante todo o ataque Esse é um detalhe pequeno que muda tudo. Imagine um Minotaur pulando. Ruim: Ele começa o pulo mirando você e durante os 0,7 segundos continua atualizando sua posição para cair exatamente na sua cabeça. Isso parece injusto. Melhor: No começo da animação: TargetPosition = posição do jogador naquele instante. Então aparece a ameaça. O jogador reage. O Minotaur cai naquela posição antiga. Agora o jogador pensa: “Eu desviei.” Isso dá satisfação. Só que, enquanto ele desviou... o Goblin Archer soltou uma flecha na direção para onde ele estava correndo. A complexidade emerge da combinação. Mas alguns monstros podem prever movimento Especialmente mais tarde. Você pode ter três níveis. Andares baixos O inimigo mira: posição atual. Andares intermediários Alguns ataques usam: posição atual + pequena previsão da velocidade. Andares altos Algumas criaturas calculam: posição futura provável. Não precisa literalmente implementar IA avançada. É só: Target = PlayerPosition + PlayerVelocity * PredictionTime E pronto. Um jogador extremamente rápido continua conseguindo desviar, mas não simplesmente segurando D para sempre. O maior perigo do jogo É: quantidade de monstros atacando ao mesmo tempo. Se 40 inimigos melee cercarem o jogador e todos executarem ataques independentes a cada 0,5 segundos... não existe habilidade humana capaz de interpretar aquela tela. Vai virar ruído visual. Então eu colocaria um sistema que eu considero extremamente importante para o seu projeto: Attack Budget Você pode ter 100 monstros perseguindo. Mas apenas alguns entram simultaneamente no estado de ataque. Por exemplo: máximo de 6 melees atacando simultaneamente perto do jogador. Os outros continuam: ● se aproximando; ● cercando; ● ocupando espaço; ● esperando uma oportunidade. Quando um termina: outro ganha permissão para atacar. Isso não significa que só existem 6 ameaças. Os ranged continuam atirando. AOEs continuam acontecendo. Boss continua atacando. Mas evita: 37 sprites tocando animação de ataque simultaneamente. E visualmente fica MUITO melhor. Jogos de ação fazem variações disso o tempo inteiro. Referência dos Drops Escala monetária: 1kg = 1.000g; 1mg = 1.000.000g; 1bg = 1.000.000.000g; 1tg = 1.000.000.000.000g. Regra de progressão: o desbloqueio de um novo material não remove os anteriores. Quanto mais alto o andar, maiores ficam principalmente as quantidades dos materiais básicos, enquanto os materiais novos começam com chances muito baixas. # Item Valor de venda 1 Monster Essence 1g 2 Monster Fragment 3g 3 Spirit Dust 4g 4 Arcane Shard 8g 5 Dark Crystal 10g 6 Soul Fragment 40g 7 Corrupted Core 70g 8 Elemental Shard 300g 9 Ancient Fragment 500g 10 Infernal Ash 4kg 11 Chaos Crystal 7kg 12 Nightmare Residue 750kg 13 Void Shard 5mg 14 Celestial Fragment 10bg 15 Divine Core 1tg Nota: os valores deste documento são uma base de balanceamento para a progressão exponencial desejada. O objetivo é que builds fortes consigam gerar milhões, bilhões e depois trilhões de recursos sem exigir milhares de kills por dia. Multiplicadores futuros de loot podem aumentar essas quantidades sem alterar a tabela-base dos monstros. Nota2: Representação visual dos drops no chão Como a quantidade de itens dropados crescerá drasticamente nos andares mais altos, os drops não deverão gerar uma entidade individual para cada unidade do item. Para evitar excesso de objetos na cena e, ao mesmo tempo, representar visualmente a quantidade obtida, serão utilizados sprites diferentes de acordo com o tamanho do stack. A representação funcionará da seguinte forma: 1 a 9 unidades: cada unidade será exibida separadamente no chão. 10 a 49 unidades: serão criados stacks visuais de 10 unidades. 50 a 99 unidades: serão criados stacks visuais de 50 unidades. 100 unidades ou mais: toda a quantidade será representada por um único stack, utilizando o sprite de 100+. Caso a quantidade não seja divisível exatamente pelo tamanho do stack, o restante deverá ser representado normalmente seguindo a regra correspondente. Exemplos: Drop de 7 → 7 itens separados. Drop de 26 → 2 stacks de 10 + 6 itens separados. Drop de 75 → 1 stack de 50 + 2 stacks de 10 + 5 itens separados. Drop de 5.000 → 1 único stack 100+, contendo internamente a quantidade 5.000. (Só que aqui, se houver como, eu gostaria de distribuir em mais de um stack, para mostrar pro player que o cenário está lotado de itens, então, mesmo que a gente faça o máximo para os itens não ficarem infinitos e crashar o jogo pela quantidade de game objects na cena, eu quero que haja muitos. Então o critério de proximidade que os itens terão para se juntar será pequeno) O sprite representa apenas visualmente o tamanho aproximado do drop. A quantidade real ficará armazenada no próprio objeto, portanto coletar um stack de 100+ poderá adicionar milhares, milhões ou quantidades ainda maiores do item ao inventário de uma única vez. Essa lógica deverá ser utilizada para reduzir drasticamente a quantidade de GameObjects de loot existentes simultaneamente na cena, principalmente nos andares mais avançados. 8. PÓS DIA

---

## Nota de encerramento — pendência de viabilidade de combate

Este Bestiário assume, por padrão, o sistema de combate completo já implementado desde a Sprint 13 (Enemy Framework): timing de ataque configurável (Telegraph → Hitbox Ativa → Recovery), Attack Budget por categoria, e — quando a Animator entrar em produção — animações reais de `idle/walk/attack/damage/die` (e variantes direcionais como `attack_orthogonal`/`attack_diagonal` em alguns casos, como o Rat People e o Fire Elemental).

**Isso ainda não foi validado com sensação de jogo real.** Existe uma contingência já registrada no GDD Seção 22 ("Contingência — simplificação de monstros comuns 🟡"): se playtest mostrar que hordas grandes com animação individual completa ficam ilegíveis, custosas demais pro escopo solo, ou simplesmente menos divertidas, os monstros comuns (nunca os heróis) podem ser simplificados para 3 comportamentos: Melee por contato, Ranged simples, Explosivo/contato especial — aproximando o jogo de Vampire Survivors nesse aspecto específico.

Essa decisão **depende de um momento de teste real**, com animação de verdade (não só placeholder/log), antes de produzir o resto do Bestiário em escala. Ver proposta de sprint dedicada a esse teste no changelog de produção.