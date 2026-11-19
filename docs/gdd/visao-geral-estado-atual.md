# MonsterLLC — Visão Geral Completa do Jogo (Estado Real, Novembro de 2026)

> **Nota de metodologia:** este documento descreve o jogo **como ele funciona de verdade no código hoje**, não a visão original ou aspiracional. O projeto já teve pelo menos 3 fases de design diferentes (`docs/gdd/index.md`, o GDD original, descreve uma TORRE de 10 andares fixos com "Monster Essence", "Employees" e demandas diárias — **nada disso existe mais**; os docs em `docs/new/` descrevem a reestruturação pra mundo aberto, e alguns deles já estão parcialmente desatualizados frente ao código atual, ex.: falam em 30 dias de evolução de mapa, mas o código trava em 10). Toda afirmação abaixo foi conferida direto no código-fonte ou nos assets do Unity nesta sessão (16/11/2026). Onde existe uma diferença entre "o que o GDD antigo diz" e "o que o código faz", o código vence, e a diferença é apontada explicitamente. Uma seção dedicada no fim (**"O que é visão, não realidade"**) reúne tudo que foi pedido/descrito mas ainda não existe de verdade.

---

## 1. O que é o jogo

MonsterLLC é um **roguelite incremental 2D de mundo aberto**, visto de cima, com 10 heróis jogáveis. Não existe final definido: o jogador sobrevive dia após dia, matando monstros e bosses, juntando gold, comprando upgrades (Perks) na Loja entre os dias, e a dificuldade escala até o Dia 10 — a partir daí, tudo (gold por kill, mix de monstros, bioma) **trava no nível do Dia 10 "pra sempre"**. É um jogo pensado pra ser jogado indefinidamente, ficando cada vez mais forte contra uma dificuldade que também escalou e depois estabilizou.

Não existe vitória, não existe "Game Over" formal: morrer só reinicia o dia atual (mesmo mapa, sem perder progresso de dias anteriores), e sobreviver avança pro próximo dia com um mundo novo.

---

## 2. Heróis

### 2.1 O roster — 10 heróis

| Herói | Arquivo | Status dos Perks |
|---|---|---|
| Barbarian | `Barbarian.cs` | ✅ **Com efeito real** (perks conectados ao personagem) |
| Ranger | `Ranger.cs` | ✅ **Com efeito real** |
| Mage | `Mage.cs` | ✅ **Com efeito real** |
| Assassin | `Assassin.cs` | ⚠️ Só dado — perks compráveis, sem efeito nenhum no personagem ainda |
| Blood_Mage | `BloodMage.cs` | ⚠️ Só dado |
| Cleric | `Cleric.cs` | ⚠️ Só dado |
| Druid | `Druid.cs` | ⚠️ Só dado |
| Gunslinger | `Gunslinger.cs` | ⚠️ Só dado |
| Paladin | `Paladin.cs` | ⚠️ Só dado |
| Rogue | `Rogue.cs` | ⚠️ Só dado |

**O que "só dado" significa:** cada um desses 7 heróis já tem as mesmas 90 fichas de Perk (30 por árvore × 3 árvores) que Barbarian/Ranger/Mage têm, compráveis na Loja, custando gold de verdade — mas comprar um perk desses **não muda nada no personagem**, porque o método que aplicaria o efeito (`RecomputePerkEffects()`) nunca foi escrito pra esses 7. É dado pronto esperando lógica, não um sistema quebrado.

Essa sessão de trabalho (ver memória do projeto) tratou **só o Barbarian até ele ficar 100% balanceado/divertido** antes de avançar pros outros — por isso Barbarian é o mais maduro dos 3 já ligados.

### 2.2 Stats base (valores de fábrica, antes de qualquer Perk)

| Herói | Vida | Dano base | Chance de Crítico | Velocidade | Velocidade de Ataque |
|---|---|---|---|---|---|
| Assassin | 28 | 1.8 | 15% | 5.5 | 0.6 |
| Barbarian | 42 | 2 | 10% | 5 | 1 |
| Blood_Mage | 26 | 1.6 | 10% | 5 | 0.5 |
| Cleric | 32 | 3 | 10% | 5 | 1 |
| Druid | 36 | 5 | 10% | 5 | 0.5 |
| Gunslinger | 30 | 1.6 | 10% | 5 | 0.5 |
| Mage | 24 | 2.4 | 10% | 5 | 0.5 |
| Paladin | 50 | 2 | 10% | 5 | 1 |
| Ranger | 30 | 3 | 10% | 5 | 0.5 |
| Rogue | 28 | 3 | 10% | 5 | 1 |


A fórmula real de vida em combate (depois de Perks) é:
```
VidaEfetivaMáxima = stats.maxHealth × MaxHealthMultiplier × CentralHealthMultiplier × (bônus de Perks)
```
`CentralHealthMultiplier` é o multiplicador do "Perk Central" (um perk especial, por herói, tratado junto dos World Perks por conveniência de código, mas não é tecnicamente um World Perk).

### 2.3 O kit de cada herói (Primário / Shift / Ultimate)

Todo herói tem exatamente 3 ações: **Primary Attack** (ataque base, sem cooldown real — só a janela da própria animação), **Shift** (habilidade secundária, cooldown próprio) e **Ultimate** (carregada por Energia, ganha matando monstros).

| Herói | Primary Attack | Shift | Ultimate |
|---|---|---|---|
| **Barbarian** | Golpe corpo-a-corpo em 4 direções fixas (fenda/corte, árvore "Fenda_*") | Buff temporário: +50% dano e +50% velocidade de movimento por 5s (não bloqueia ação, não cancelável) | Salto + N projéteis retos em leque ("Chuva de Lanças", 8 direções base, escalável por Perk) |
| **Ranger** | Flechas em formação de cunha (V), perfuram múltiplos alvos | **Modo duplo** — clássico (camuflagem, bloqueia ação do jogador) ou "Veterano de Guerra" (fica visível, não bloqueia nada, só aplica bônus já comprados) | 8 facas nas 8 direções fixas, aplicam Bleeding (dano contínuo por 5s) |
| **Mage** | Bola de fogo em ângulo livre (projétil reto, deixa rastro de fogo no chão) | Teleporte — imune a dano durante toda a viagem | Versão ampliada da bola de fogo do Primário |
| **Assassin** | "Deadly Dash" — fica invisível no lugar, um projétil-fantasma viaja, bate em área e volta pro Assassin (não é um dash físico de verdade) | Teleporte | Forma sombria: durante a Ultimate, o Dash vira "Thousand Blades" sem cooldown |
| **Blood_Mage** | Projétil reto, 1 hit só (ajustado pra não perfurar demais) | Extração de sangue | Onda de choque em anel, em 3 estágios |
| **Cleric** | Projétil Homing que persegue o monstro mais próximo, N projéteis por ativação | Reza/cura ativa | Oração — não é global, só funciona dentro do raio de visão do Cleric; cada tick causa sempre metade do dano normal |
| **Druid** | Vinhas que brotam do chão | Transformação em Coruja | Transformação em Alce (4 hitboxes de garra que escalam junto com o corpo) |
| **Gunslinger** | Rajada hitscan de N tiros, com cone de imprecisão por tiro | Rajada de pelotas + recuo (o próprio Gunslinger é empurrado) | Tiro teleguiado, sempre acerta o monstro mais próximo, sem animação própria |
| **Paladin** | Martelo arremessado em leque (upgrade futuro permite até 5 ao mesmo tempo) | Shield bash | Espadas orbitando o Paladin |
| **Rogue** | Pulso de área que segue o próprio Rogue — em 3 estágios (início → área de dano contínuo enquanto o jogador se move livremente → fim) | Cambalhota | Bomba arremessada |

### 2.4 Árvores de Perk

Cada herói tem **3 árvores bespoke de 30 perks cada** (PrimaryAttack / Survival / Ultimate = 90 por herói), mais a árvore **World** (30 perks, genérica, vale pra todo herói igual). No total: **10 heróis × 90 + 30 World = 930 fichas de Perk no jogo**.

Preço de cada perk sobe por rank dentro da própria ficha (`costPerRank`, um array de custos — o tamanho do array é o rank máximo daquele perk). A escala de preço cresce muito rápido conforme o perk fica mais tardio/poderoso — exemplo real do Barbarian (`PrimaryAttack`):

| Perk | Rank 1 | Rank 2 | Rank 3 |
|---|---|---|---|
| Força Bruta (early) | 450.000 | 1.000.000 | 1.500.000 |
| Terremoto (late, rebalanceado 5x nesta sessão) | 25.000 | 150.000 | 900.000 |

(A posição na árvore não necessariamente determina qual é mais caro — depende do pré-requisito e do rank daquele perk específico, não existe uma ordenação linear simples.)

---

## 3. Bestiário — Monstros Comuns

### 3.1 Regra de combate padrão

- **Melee comum**: golpe real via animação + Animation Event, com um trigger direcional (1 de 4 diagonais fixas na frente do monstro) — só causa dano se o jogador estiver dentro do trigger no frame exato do golpe. Não existe dano de contato passivo pra monstro melee comum.
- **Ranged comum**: o projétil nasce no Animation Event da animação de conjuração e mira a posição REAL do jogador naquele instante exato (sem telegraph prévio de "vou atirar aqui").
- **Única exceção permanente a essa regra**: Slimes (comuns e o boss Mother Slime) — nunca tiveram ataque dedicado, só dano por contato.
- Receber dano **nunca** cancela uma ação já comprometida (não existe transição Attack→Damage) — só um flash visual de "levei dano", o golpe sempre termina.
- **Variante "emboscada"** (Andar 5 — Skeleton, Headless Skeleton, Jumping Skull, Skeletal Horse, Skeleton Rider, Skeletal Minotaur, Skeleton Warrior): fica numa pose estática até detectar o jogador — é a ÚNICA família do jogo onde a detecção pode **reverter** (o monstro "desiste" e volta a dormir se o jogador se afastar).

### 3.2 Monstros comuns por Andar (Tier)

| Andar | Monstros comuns |
|---|---|
| 1 | Rat, Wolf, Bat, Slime Green, Slime Blue, Goblin, Goblin Raider, Goblin Sapper |
| 2 | Rat People, Centaur, Minotaur, Gnoll, Spider, Ancient Troll |
| 3 | Warg, Orc Blade, Orc Rider, Orc Shaman, Warbreed Blade, Warbreed Berserker, Warbreed Phalanx, Orc Scout, Warbreed Arbalist |
| 4 | Acolyte, Zealot, Hound, Devoted Blade, Devoted Sentinel, Devoted Stalker (+ versões "possuídas" Dark Cultist/Dark Hound/Dark Abomination — mesmo GameObject, só troca sprite/stats quando o boss Dark Channeler os transforma) |
| 5 | Skeleton, Headless Skeleton, Jumping Skull, Skeletal Horse, Skeleton Rider, Skeleton Minotaur, Zombie Warrior, Zombie Bear, Zombie Minotaur, Ghost, Skeleton Archer, Skeleton Mage, Zombie Archer, Zombie Mage, Flying Skull, Gargoyle, Wraith, Mummy, Burning Skull, Skeleton Warrior (21 arquivos — o Andar mais populoso do bestiário) |
| 6 | Ancient Danger Heavy Warrior, Armored Warrior, Rock Golem, Fire Elemental, Air Elemental, Water Elemental, Earth Elemental, Ancient Danger |
| 7 | Fire Draco, Magma Hound, Magma Orc, Magma Golem, Dragon Hatchling |
| 8 | Diablo, Imp, Armored Demon |
| 9 | Observer, Serpent, Shifted, Stalker, Winged, Worm, Bicephalous |
| 10 | Divine Minion, Divine Warden, Divine Guardian, Divine Angel |

**Exceções com arquitetura de IA própria** (não usam o padrão genérico melee/ranged):
- **Goblin Sapper**: arma uma bomba, foge, recarrega — o dano de contato se soma ao dano da bomba.
- **Orc Shaman**: em vez de projétil, planta um totem no chão (Heal, Fire ou Ice — prioridade Heal > Fire > Ice).
- **Burning Skull**: explode numa área própria.
- **Serpent**: controller dedicado (Andar 9).
- **Skeleton Mage / Zombie Mage**: o ataque vira uma área no chão, não um projétil "vivo" que viaja.
- **Acolyte / Hound / Zealot**: usam uma classe base que suporta a transformação em versão "possuída" pelo Dark Channeler.

### 3.3 Sistema de população de monstros comuns

Configurado globalmente (não por Andar) em `MonsterSpawnSettings`: população mínima 3, alvo 8, máximo 12.

A densidade real de monstros **escala com o ciclo Dia/Noite, não com o número do dia**: no início do dia a população sobe de 3→8; no instante exato que vira Noite, continua subindo de 8→12 e fica nesse platô o resto da Noite. **A Noite é objetivamente mais cheia de monstro que o Dia, dentro do mesmo dia.**

Monstros muito distantes do jogador (>40 unidades) são removidos sem contar como kill/loot e substituídos num anel de 15-30 unidades ao redor do jogador. 60% dos spawns preferem nascer fora da visão da câmera. Se a população cair pro mínimo ou menos ("crise"), o jogo repõe até o alvo, mas limitado a 8 spawns por frame (pra não travar o jogo — achado real de profiling).

---

## 4. Bosses (30 no total)

| Andar | Bosses | Mecânica notável |
|---|---|---|
| 1 | Mother Slime Green, Mother Slime Blue, Goblin King | Mother Slimes nascem 3 filhotes em posições fixas ao morrer |
| 2 | Rat People Royalty, Werewolf, Centaur King, Spider Queen | Rat People Royalty e Spider Queen são híbridos melee+ranged com 2 raios de ataque independentes, nunca fogem; Spider Queen aplica Efeito Gelo (stun) com a teia |
| 3 | Giant, Pale Champion, Wise Orc, Cave Troll | Escala padrão (HP/dano); Cave Troll ainda está na escala antiga do Andar 2, pendente de rebalanceamento |
| 4 | Flagelant, Ritual Guard, Dark Channeler | Dark Channeler causa **0 de dano direto** — a mecânica dele é transformar Acolyte/Hound/Zealot próximos em versões possuídas |
| 5 | Zombie Giant, **Lich**, Undead Knight, Spectre, Headless Horseman, Mummy King | Lich cria área de gelo e invoca zombies/skeletons que morrem junto com ele; Spectre deixa uma superfície de gelo persistente se o golpe conectar |
| 6 | Ancient Danger Leader, Krampus, Supreme Elemental | Escala de HP/dano já em milhões |
| 7 | Dragon, Undead Dragon | A baforada de fogo cria uma área de dano persistente no chão |
| 8 | Balrog | Único boss do Andar — HP/dano em bilhões |
| 9 | Lobster, Stickman, Ambuster | Ambuster tem golpe duplo (2 Animation Events no mesmo clipe); HP/dano em trilhões |
| 10 | **Divine God** | O topo do roster — explosão em área ao encostar + raios caindo em posições aleatórias ao redor do jogador + **soma dano de contato normal por cima de tudo isso** (única criatura do jogo com as 2 camadas de dano simultâneas). HP: 2 quatrilhões |

Não existe "boss final" no sentido de encerrar o jogo — Divine God é só o topo da escala de bosses do Andar 10, e o Andar 10 (como todo o resto) vale pra sempre depois que o jogador chega lá.

### 4.1 Como os bosses nascem

- **Boss Timer**: um boss por ciclo (confirmado: `bossesPerSpawnCycle = 1` hoje), a cada ~50 segundos reais, nunca nos últimos 10s antes do fim do dia.
- O Tier do boss que nasce é sempre o Tier predominante **na posição atual do jogador** (não é um sorteio independente).
- **Sem repetição no mesmo dia**: um boss já sorteado não nasce de novo até esgotar o roster daquele Tier; zera todo dia novo.
- Replacement por distância: um boss muito longe do jogador (>45) pode ser teleportado pra um anel de 15-25 unidades ao redor dele, com cooldown de 10s entre replacements — nunca nasce dentro da Safe Zone.

---

## 5. Geração de Mundo e Ciclo Dia/Noite

### 5.1 Um mundo único, não "andares" nem mapas separados

Não existe mais torre, não existe regeneração diária completa do mapa, e **não existe escolha real de mapa** — o jogo é 1 mundo contínuo, gerado por chunk conforme o jogador explora (`ChunkTerrainGenerator`). As strings "Standard"/"Tower" que aparecem no fluxo de início de run são vestígios do código antigo (torre) e nunca foram atualizadas — a única escolha real ao começar uma run é **qual herói jogar**.

### 5.2 Dia efetivo por distância

Cada ponto do mundo tem um "Dia efetivo" diferente do número real do dia, calculado pela distância até a Safe Zone:
```
DiaEfetivo = clamp(DiaReal + DistanceDayOffset, 1, 10)
DistanceDayOffset = piso((distância - raio da Safe Zone) / distancePerSpawnDay)
```
Ou seja: andar mais longe da Safe Zone é equivalente a "pular pra frente" nos dias, dentro do mesmo dia real — o mundo fica mais perigoso à medida que o jogador se afasta do centro, não só conforme os dias passam. O teto é sempre 10 (era 30 numa versão antiga do design, já substituída).

### 5.3 Biomas (Tiers visuais)

6 grupos de bioma: Tier1 / Tier2-3 / Tier4-5 / Tier6-8 / Tier9 / Tier10. O grupo "mais forte" numa região vence a aparência visual (com piso de 20% — abaixo disso o grupo nem aparece, e o resto é redistribuído proporcionalmente). O bioma usa **sempre** a tabela de Dia (nunca troca com a Noite — decisão de design explícita); já o mix de monstro/boss usa a tabela de Dia OU Noite conforme a fase atual.

### 5.4 Safe Zone

Raio configurável ao redor da origem. Monstro e boss **nunca nascem** dentro dela, mas podem entrar/perseguir/atacar o jogador ali normalmente (não bloqueia pathfinding, só filtra onde novos monstros podem nascer).

### 5.5 Ciclo Dia/Noite

Sempre 50% do tempo do dia em fase "Dia", 50% em fase "Noite" — nunca quebra essa proporção, mesmo com bônus de tempo. O relógio do dia **não começa a contar sozinho**: fica parado até o primeiro gatilho real de combate (um hit, proximidade de um monstro, ou mirar). Duração padrão: 100 segundos reais por dia completo.

### 5.6 Simulation Rings (otimização, mas afeta o que o jogador vê)

Dentro de um raio de 15 unidades do jogador, tudo roda normal (IA a cada frame, RVO de colisão ativo). Fora disso, RVO desliga e a IA decide a cada 0.3s em vez de todo frame — dano, knockback e efeitos ao longo do tempo nunca são afetados por isso, só o "pensar" da IA distante fica mais barato.

---

## 6. Economia

### 6.1 Gold por kill (tabela real, por dia)

| Dia | Gold por monstro comum | Gold por boss |
|---|---|---|
| 1 | 1 | 20 |
| 2 | 100 | 500 |
| 3 | 500 | 2.500 |
| 4 | 2.000 | 10.000 |
| 5 | 10.000 | 50.000 |
| 6 | 50.000 | 250.000 |
| 7 | 200.000 | 1.000.000 |
| 8 | 1.000.000 | 5.000.000 |
| 9 | 4.000.000 | 20.000.000 |
| **10+** | **15.000.000 (pra sempre)** | **75.000.000 (pra sempre)** |

### 6.2 Bônus de sobrevivência

Sobreviver o dia completo concede um bônus que leva o gold total ganho naquele dia (kills + vendas) a **2x** do que seria sem o bônus. Nunca se aplica na morte/retry.

### 6.3 Venda de loot

O **Vendor** é um único NPC fixo no mundo (nunca é instanciado em runtime, nunca se move) — o jogador precisa literalmente caminhar até ele. Interagir vende **a Bag inteira de uma vez** (não item por item): soma o valor de todos os slots e credita tudo junto. Só funciona durante o Dia porque o jogo inteiro só aceita interação nesse estado (`GameplayGate`) — o Vendor em si não tem nenhuma checagem própria de "é dia ou não". O carrinho que aparece (`VendorCart`) é **100% cosmético**: a venda real já aconteceu instantaneamente; o carrinho só anima sprites de loot "voando" até uma pilha falsa pré-posicionada, só pra dar feedback visual.

Fórmula de venda: `valor = quantidade × preço_base_do_item × SellBonusMultiplier` (multiplicador combinado de 4 World Perks — Negociante Nato, Mercador Astuto, Rede de Contatos, Tesouraria Real — que **multiplicam entre si**, não somam).

### 6.4 World Perks (29 perks + 1 "Central")

Perks genéricos que valem pra qualquer herói, comprados com o mesmo gold:

| Categoria | Perks |
|---|---|
| Venda/gold | Mercador Astuto, Negociante Nato, Rede de Contatos, Tesouraria Real (multiplicam venda entre si); Instinto de Caçador + Butim de Guerra (multiplicam gold por kill entre si) |
| Loot/coleta | Ímã Vivo (auto-vende tudo no pickup, sem passar pela Bag), Faro de Saque (+8%/rank chance e quantidade de drop), Atração / Campo de Atração / Horizonte de Coleta (raio de pickup) |
| Bag | Mais Espaço de Bag (+5 slots/rank), Stack Maior (curva fixa de tamanho de stack, não %/rank) |
| Baú | Baú Extra (+5%/rank chance de spawn), Chamado do Baú (+10%/rank chance) |
| Vida | Vigor, Fortitude, Pulso Vital, Vitalidade Suprema (4 perks, 20 ranks no total — todos % de vida máxima) |
| Redução de dano | Pele Grossa, Couraça, Casco de Ferro (empilham entre si) |
| Regeneração | Recuperação, Metabolismo Acelerado, Fôlego Eterno |
| Velocidade | Passo Leve, Vento nas Costas, Pés Ligeiros |
| Sobrevivência | Segunda Chance (sobrevive a 1 hit fatal por dia, 1x) |
| Talento | Reroll de Talento — **inerte hoje**, só vai fazer sentido quando o sistema formal de Talentos existir (ver Seção 8.2) |

### 6.5 O que NÃO existe ainda: Weapon Tier

O GDD antigo (`docs/gdd/economy-balance.md`) descreve 15 Tiers de arma (Copper→Divine) com multiplicadores de dano/vida e preços próprios. **Isso não está implementado** — `ShopHandler.cs` tem um stub explícito com só 3 Tiers de placeholder ("Basic", "Copper", "Iron") e um comentário literal no código dizendo que o sistema de verdade "vira real na Deadline 9". Não tratar isso como sistema atual.

---

## 7. Loot e Itens

### 7.1 Os 15 itens do jogo

| Item | Valor de venda |
|---|---|
| Monster Essence | 1 |
| Monster Fragment | 10 |
| Spirit Dust | 20 |
| Arcane Shard | 50 |
| Dark Crystal | 100 |
| Soul Fragment | 500 |
| Corrupted Core | 1.000 |
| Elemental Shard | 5.000 |
| Ancient Fragment | 100.000 |
| Infernal Ash | 1.000.000 |
| Chaos Crystal | 5.000.000 |
| Nightmare Residue | 10.000.000 |
| Void Shard | 1.000.000.000 |
| Celestial Fragment | 10.000.000.000 |
| Divine Core | 1.000.000.000.000 |

Cada monstro tem sua própria lista de drops possíveis, e cada entrada dessa lista rola sua própria chance **de forma independente** — um único abate pode dropar vários itens diferentes ao mesmo tempo.

### 7.2 Bag

Capacidade inicial: 10 slots, 16 de stack cada (`Mais Espaço de Bag` soma slots, nunca substitui a base). O raio de coleta (pickup) é por herói × multiplicador de World Perks.

### 7.3 Perda de loot

Loot não vendido se perde **nos dois casos**: na morte (sempre foi assim) e, desde esta sessão (fix recente), também ao **sobreviver e avançar pro próximo dia** — a Bag sempre zera na virada de página, vendido ou não.

### 7.4 "Conhecimento" de item

Um item aparece como "???" até ser vendido 1 vez — a partir daí, o nome é revelado **pra sempre** naquela run (nunca volta a esconder, mesmo em dias seguintes).

---

## 8. Baús e o que seria "Talent Points"

### 8.1 Baús — o que existe de verdade

Cada baú, ao ser aberto, faz 1 sorteio:
- **50% de chance**: dá gold direto (faixa fixa de 50 a 200 — **não escala com o dia**).
- **50% de chance**: abre um popup de escolha (`Chest_Popup`) que **hoje não dá nada de verdade** — é um placeholder que fecha com qualquer clique.
- A chance de "dobrar" o gold existe no código, mas está **zerada** no momento (0%).
- 20% dos baús do mundo nascem como **Chest Mimic**: um monstro disfarçado de baú, que só revela sua natureza quando o jogador tenta abri-lo.

### 8.2 "Talent Points" — não existe sistema formal ainda

Buscando por "talent" no código inteiro: existe uma aba "Talents" na Loja (`Talent_Painel`) e ela **abre e fecha, mas não tem nenhuma lógica por trás**. O popup de baú tem um comentário literal no código dizendo que vai "preencher 3 perks + 3 talentos sorteados... quando Perks/Talentos existirem de verdade". Um World Perk (Reroll de Talento) também está marcado como "inerte até Talentos existirem".

**O que existe de verdade, cobrindo esse papel hoje:** gold (ganho matando monstros/bosses e vendendo loot) gasto comprando Perks na Loja — é tudo a mesma moeda e o mesmo sistema. Não existe uma segunda moeda separada ("Talent Points") nem "1 ponto de talento por dia sobrevivido" — isso é visão de design ainda não construída.

---

## 9. Loop de Gameplay — Passo a Passo

### 9.1 Menu Principal → início de uma run

1. **New Game**: o jogador escolhe um dos 10 heróis (única escolha real do início da run — não existe escolha de mapa/modo de verdade, mesmo que a UI passe por telas chamadas "Mode Select"/"Map Select"). O mundo é gerado com uma seed aleatória nova por run.
2. **Continue Game**: só aparece se existir um save; carrega a run salva e vai direto pra Loja (pula Hero/Map Select). *Pendência conhecida, não resolvida: o herói pode não ser reinstanciado corretamente nesse caminho.*

### 9.2 Dentro de um dia

3. O herói nasce na origem do mundo.
4. O relógio do dia fica pausado até o primeiro gatilho de combate real (hit, proximidade ou mira num monstro) — só a partir daí os 100 segundos do dia começam a contar.
5. O jogador explora o mundo aberto, mata monstros e bosses com Primary/Shift/Ultimate, cada kill dá gold (tabela da Seção 6.1) e pode dropar loot.
6. Loot cai no chão e precisa ser coletado (entra na Bag) — ou é auto-vendido na hora, se o jogador tiver o perk Ímã Vivo.
7. O jogador pode caminhar até o Vendor pra vender a Bag inteira a qualquer momento durante o dia, ou simplesmente deixar acumular.
8. Baús aparecem espalhados pelo mundo — alguns são Chest Mimics disfarçados.
9. O mix de monstro/bioma/boss ao redor do jogador reflete o "Dia efetivo" daquele ponto do mundo (quanto mais longe da Safe Zone, mais difícil, independente do dia real).
10. Na metade do tempo do dia, a fase vira Noite — a população de monstros sobe (até o máximo de 12 simultâneos) e o mix de monstro/boss pode trocar pra tabela de Noite.

### 9.3 Fim do dia — dois caminhos

**Morrendo:**
11. O jogo limpa todos os monstros/loot/efeitos da cena, **zera a Bag** (loot não vendido se perde), e repete o **mesmo dia**, no **mesmo mapa** (nunca regenera terreno) — vai pra tela de Resultados e depois pra Loja.
12. **Não existe limite de retries.** Isso é um risco conhecido: se um dia específico for "impossível" de sobreviver com o build atual do jogador, ele pode ficar travado tentando esse mesmo dia pra sempre.

**Sobrevivendo (o relógio chega a 0):**
13. Concede o bônus de 2x gold do dia (Seção 6.2), **zera a Bag também** (mesmo tendo sobrevivido — fix desta sessão), vai pra Resultados e depois pra Loja.
14. O número do dia avança (+1), e o mundo é regenerado com um mapa novo pra aquele número de dia (bioma/mix de monstro atualizados pro dia novo).

### 9.4 Na Loja

15. A Loja tem 3 abas: **Resume** (resumo do que aconteceu no dia: kills, gold, o que foi vendido), **Perks** (comprar upgrades) e **Talents** (hoje vazia — ver Seção 8.2).
16. Comprar um Perk gasta gold e sobe 1 rank daquele perk — mas só tem efeito real se for Barbarian, Ranger ou Mage (os outros 7 heróis ainda não têm a lógica de efeito escrita).
17. O botão "Redefine" desfaz compras feitas **só nesta visita atual** à Loja — nunca alcança uma visita anterior.
18. Saindo da Loja, o ciclo recomeça do passo 3 (morte: mesmo dia; sobrevivência: dia seguinte).

### 9.5 Daí em diante

19. Isso se repete indefinidamente. Do Dia 10 em diante, a dificuldade e a recompensa de gold travam no nível do Dia 10 — o jogo não tem fim planejado, só "quão longe/forte você consegue chegar".

---

## 10. O que é visão/planejado, e ainda não existe de verdade

| Sistema descrito/pedido | Estado real hoje |
|---|---|
| Escolha de mapa | Não existe — 1 mundo único, só o herói é escolhido |
| 10 dias terminando num "boss final" / vitória | Não existe fim — Dia 10 vale pra sempre, sem condição de vitória |
| Talent Points (1 por dia, ou de baús) | Não existe — hoje é tudo gold + Perks, mesma moeda e sistema |
| Perks com efeito em 7 dos 10 heróis | Dados existem (90 fichas cada), efeito real ainda não foi escrito |
| Weapon Tier (15 tiers, Copper→Divine) | Stub de 3 tiers placeholder; sistema real ainda não existe |
| Popup de recompensa de baú (`Chest_Popup`) | Existe a tela, mas fecha sem dar nada — placeholder |
| Tier Suppression (remover um Tier da run) | Função existe no código, mas só como `[ContextMenu]` de debug — sem preço real nem UI na Loja |
| Continue Game reinstanciando o herói corretamente | Pendência conhecida, sem fix ainda |
| Limite de retries por dia (evitar softlock) | Não existe — risco conhecido, documentado, sem solução ainda |

---

## 11. Fontes

Este documento foi montado cruzando, nesta sessão, os seguintes documentos de design já existentes:
- `docs/gdd/index.md` (GDD original — majoritariamente obsoleto pra tudo exceto a lista de heróis)
- `docs/gdd/bestiary.md` (bestiário — confirmado 100% sincronizado com o código)
- `docs/gdd/economy-balance.md` e `docs/gdd/balance-values.md` (parcialmente obsoletos — Weapon Tier não implementado, preços de item e perk mudaram)
- `docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md`
- `docs/new/Mudanca_Estrutural_Geracao_Procedural_Evolucao_Mapa_30_Dias_V2_ILUSTRADO.md`
- `docs/new/Plano_Spawn_Monstros_Mundo_Aberto.md`
- `docs/new/Plano_Performance_Streaming_Chunks.md`
- `docs/new/Mudanca_Estrutural_Jogo_Incremental_Deadline_8_1.md` (o mais recente — venceu qualquer contradição com os docs acima)

— e confirmando cada afirmação estrutural contra o código-fonte real (`Assets/Scripts/`) e os assets do Unity (`Assets/Settings/`, `Assets/Prefabs/`) listados ao longo do texto.
