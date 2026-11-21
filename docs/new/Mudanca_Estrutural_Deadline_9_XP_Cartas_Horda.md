# Deadline 9 — Redesign: XP, Cartas, Itens e Horda

_Decisões fechadas em sessões de debate de 2026-11-17 e 2026-11-18, depois de playtest com
amigos. **Substitui** a `Mudanca_Estrutural_Jogo_Incremental_Deadline_8_1.md` (loop de loja/perks/
loot) e tudo do `Projeto_Torre_Plano_Producao_v7.md` da Deadline 9 em diante (Economia Expandida/
Weapon Tier, Employees, Floor Variants — design antigo, anterior ao mundo aberto). Os dois
documentos antigos ficam no repositório só como histórico._

_Itens marcados com **⏳ PENDENTE** ainda não foram decididos. Itens marcados com **🧪 DEFINIDO PELO
TESTE** dependem do resultado da Fase 0B._

---

## 0. Diagnóstico que motivou o redesign

Playtest com amigos (novembro de 2026), feedback unânime:

- **O que está bom:** o combate. Matar monstro, desviar, as animações e o visual foram elogiados.
- **O que está ruim:** o sistema em volta do combate. Todos acharam monótono e repetitivo. A
  maioria largou antes do Dia 5, só um chegou ao Dia 8 e cansou.
- **Sintoma revelador:** todos ficaram perto do Vendor fixo, vendendo loot. Ninguém descobriu que
  o mapa é procedural e evolui com a distância, e ninguém descobriu que existem baús. O Vendor fixo
  virou uma âncora que pagava o jogador pra não explorar.
- **Problema de manutenção:** 930 fichas de perk (90 por herói + 30 de Mundo), só 3 heróis com
  efeito real, preços re-escalados várias vezes na mesma semana. Impossível de balancear e fácil
  de o jogador quebrar o jogo antes da hora.

**Direção escolhida:** aceitar que o jogo é da família de Vampire Survivors e se diferenciar onde
ele já é diferente: **combate ativo** (o jogador mira, escolhe a hora de cada habilidade e desvia,
com heróis animados à mão e kit fixo), mundo procedural que evolui com a distância, e bosses
dominando o dia. A progressão sai da Loja entre dias e vai pra **dentro do combate** (nível,
cartas, itens) e pra **fora da run** (progressão permanente no menu).

---

## 1. Estrutura da run

- A run começa no Dia 1 e o jogador sobrevive dia após dia.
- **Morte encerra a run.** Não existe mais retry do mesmo dia. (Isso elimina o risco de
  travamento em que um dia impossível prendia o jogador pra sempre.)
- Ao morrer, o jogador leva **100% do gold permanente** coletado na run (Seção 9).
- **O dia termina poucos segundos depois que o último boss do dia morre** (Seção 8), não mais por
  tempo.
- **A run tem vitória:** termina ao matar o **Divine God**, o boss final. Assim como na morte, o
  jogador leva 100% do gold permanente.
- Ao fim do dia: toca uma animação de fim de dia (criada pelo usuário, em todos os heróis) e abre
  uma **tela de resumo** com monstros mortos, gold coletado e bosses mortos. Enquanto essa tela
  está aberta, a geração do mundo do próximo dia carrega em background.
- **Não existe mais Loja entre dias.** Sem perks, sem aba de talentos, sem Redefine.

### 1.1 Atacar andando (todos os heróis)

Testado em 2026-11-18 com o herói deslizando durante o ataque. O usuário gostou muito da
gameplay. É a primeira coisa desta Deadline (Fase 0A) e vale pra **todos os 10 heróis**, não só
pros 3 do escopo principal, porque o combate de todos já está pronto e a mudança é simples.

- **O herói pode andar durante o ataque primário.** O Shift e a Ultimate continuam travando o
  movimento.
- **Animação nova `attack_walk`** pra cada herói, criada pelo usuário. Os heróis que têm animações
  de ataque separadas (Rogue, Gunslinger e outros) ganham o próprio `attack_walk` de cada uma.
- **Regra das animações:** o `attack_walk` tem **o mesmo número de frames e os Animation Events
  nos mesmos frames** do ataque parado. Assim, se o jogador começa o golpe parado e começa a andar
  no meio (ou o contrário), a animação troca no mesmo ponto e o golpe sai no tempo certo. Os
  eventos de dano já têm proteção contra disparar duas vezes (ex.: `attackHitFired` do Barbarian).
- **Direções:** o ataque do Barbarian tem 4 direções (ne, nw, se, sw), mas o do Ranger e o do Mage
  têm 8. **⏳ PENDENTE** se o `attack_walk` deles tem 4 ou 8 direções.
- O corpo continua virado pra **mira** (presa no momento do clique), não pra direção em que o
  herói anda. Andar pra um lado e atacar pro outro faz o herói "andar de costas".
- **Velocidade durante o ataque:** existe um multiplicador ajustável (0 a 1). Começa em 1. Se o
  Ranger e o Mage ficarem fortes demais atacando e se afastando, testar uns 0.7.
- Base no código: `allowMoveDuringPrimaryAttack` e `moveSpeedMultiplierDuringPrimaryAttack` no
  `HeroController`, hoje desligados até as animações existirem.

**Assassin.** O ataque dele faz ele se desmaterializar e virar o projétil. Por isso, durante o
ataque, **os monstros (bosses também) passam a perseguir o projétil** em vez do Assassin. No fim
da animação, o alvo volta pro Assassin.

- **Vários projéteis** (leque do `dashProjectileCount`, Thousand Blades): **⏳ PENDENTE**.
  Recomendação: no momento em que os projéteis nascem, cada monstro escolhe **o projétil mais
  perto dele** e fica com ele até o fim do ataque. Fica mais legível do que um sorteio (com
  sorteio, os monstros se cruzam de um jeito estranho) e é estável (o monstro não troca de alvo a
  cada frame).
- Regra do usuário: **um projétil nunca termina a viagem antes dos outros.** Todos voltam juntos,
  então não existe o caso de um monstro perder o projétil no meio do ataque.

## 2. Horda e spawn de monstros

### 2.1 Aggro

- **Mantém o aggro global atual:** a horda vai atrás do player. (Chegou a ser proposto voltar ao
  target antigo com alerta de 1 salto, mas foi **revertido** pelo feedback dos amigos: sem aggro
  global o jogo fica lento e o jogador só desvia das hordas difíceis.)
- **O dia conta desde o começo e a horda já nasce perseguindo** (decisão de 2026-11-20, D9.4).
  Sai a regra da D8.1 de "o cronômetro só começa no primeiro aggro". Todo monstro comum nasce com o
  herói como alvo. **Nada conta durante o loading**: o cronômetro e a horda só correm quando o jogo
  fica jogável.

### 2.2 Densidade

- O mundo precisa **sempre parecer cheio**. Monstros espalhados em pelo menos 1 chunk de raio ao
  redor do player.
- Quando o player se afasta, os monstros que ficaram pra trás são **reposicionados** pro chunk da
  frente em vez de destruídos e recriados.
- **Spawn gradual:** começa com poucos (por volta de 10), vai subindo ao longo do dia, com
  **picos** em certos momentos em que o mundo enche de verdade.
  - **Decidido em 2026-11-20 (D9.4):** a horda segue o **tempo real do dia** (não pausa com boss
    vivo, de propósito: quem demora pra matar boss enfrenta mais monstros). Cada dia tem **começo e
    teto próprios** (dia 1: 10 → 150; dia 3: 25 → 210; dias 6 a 10: 50 → 300), a mesma forma de
    subida (dia 1: 10/15/30/60/100/150 em 0/0:30/1:00/2:00/3:00/4:00) e picos de +50% por 15 s a
    cada minuto. Teto absoluto 300. Tudo ajustável no `MonsterSpawnSettings`. Detalhes:
    `docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md`.
- **No final, muitos monstros.** O jogador forte nunca pode ficar esperando reposição. (Problema
  real do playtest: um amigo muito forte matava rápido e ficava esperando os monstros nascerem.
  Aumentar o crisis spawn pra 20 fez eles nascerem em rajada e pareceu estranho.)
- A reposição deve ser um **fluxo contínuo**, na mesma velocidade em que o jogador mata, sempre
  fora da câmera, nunca em rajada visível.
- **Nascer sempre fora da tela, em anel ao redor do player** (decisão de 2026-11-18). Todo monstro
  nasce ou é reposicionado fora da câmera e entra andando na tela. Isso vale pro spawn normal, pra
  reposição e pro reposicionamento por distância.
  - Sai a regra atual de que uma porcentagem dos monstros nasce de propósito dentro da tela
    (`outsideCameraViewChance`).
  - Sai também o "plano B" atual: quando nenhuma das tentativas acha um lugar fora da tela, hoje o
    jogo aceita um lugar visível. No sistema novo, ele não nasce e tenta de novo no frame seguinte.
  - **Formato do anel:** a tela é retangular (16:9), então um círculo perfeito que passe fora dos
    cantos fica muito mais longe das bordas de cima e de baixo do que das laterais, e os monstros
    de cima e de baixo demorariam bem mais pra aparecer. Recomendação: uma **elipse** com a mesma
    proporção da tela, passando um pouco fora dos cantos. Assim os monstros entram na tela em tempos
    parecidos de qualquer lado. **Decidido (2026-11-20): elipse**, semi-eixos = meia-largura e
    meia-altura visíveis × √2 + margem (~2 unidades, ajustável). Sem lugar fora da tela, não nasce
    e tenta no frame seguinte.
  - Sugestão (a confirmar): o teleporte do boss quando o jogador foge (Seção 8) segue a mesma
    regra. O boss aparece fora da tela e entra andando.
- **Spawn espalhado por igual ao redor do player (cerco).** Hoje cada spawn sorteia um ângulo
  independente. Às vezes vários monstros saem do mesmo lado e outro lado fica vazio, e esse lado
  vazio vira a rota de fuga do kite (Mage e Ranger, principalmente).
  - Como: o anel é dividido em **setores** (ex.: 12). Cada spawn vai pro setor que tem **menos
    monstros vivos** naquele momento (contados pelo ângulo de cada monstro em relação ao player).
    Assim a horda sempre fecha um cerco, sem buracos.
  - **Decidido (2026-11-20): 12 setores** (ajustável).
- **Anti-kite: mais spawn na direção da fuga.** O jogo acompanha a direção média em que o player
  andou nos últimos segundos. Se ele estiver fugindo sempre pro mesmo lado, os setores **na frente
  dele** ganham peso extra no spawn. O monstro continua nascendo fora da tela, só que mais do lado
  pra onde o player está indo.
  - Fica mais importante ainda com o atacar andando (Seção 1.1), que deixa o kite mais forte.
  - O reposicionamento por distância já ajuda nisso: o monstro que ficou pra trás reaparece na
    frente.
  - Cuidado 1: o peso precisa ter **teto**. Se todo monstro nascer na frente, o jogador sente que o
    jogo está trapaceando, e o kite deixa de ser uma tática válida. A ideia é dificultar o kite,
    não proibir.
  - Cuidado 2: ir até um baú ou até o Vendor também é "andar sempre pro mesmo lado". O anti-kite
    não pode transformar a exploração em castigo. **Decidido pelo usuário (2026-11-20): sempre
    ligado**, inclusive andando até baú/Vendor, com limite (no máximo ~40% dos spawns recentes na
    frente).
  - Força do peso, janela de tempo e teto: settings ajustáveis. A telemetria (posição a cada 1s) dá
    pra medir o quanto os amigos fazem kite no próximo teste.
- ~~Regra do "mínimo 3 vezes vira máximo"~~: já tinha virado rampa na D8.1 e **saiu de vez** no
  D9.4, junto com a crise e o "1 a cada 3 s". A reposição é um fluxo contínuo (~10 por segundo,
  espalhados pelos frames) até a meta da curva.
- **O herói empurra a horda** (decisão de 2026-11-20, D9.4): andando contra os monstros, avança a
  ~50% da velocidade e empurra os da frente pros lados. Parado, continua cercado. Boss não é
  empurrado.
- Números de população (mínimo, target, máximo, picos): **🧪 DEFINIDO PELO TESTE**.

### 2.3 Monstros ranged

- **Ficam fora do começo do dia.** Não são ruins, mas atrapalham muito quando o jogador ainda é
  fraco (ex.: Goblin Raider). Entram na lista de spawn só depois de um tempo de dia.
  **Decidido (2026-11-20): 30 s de tempo real, só no dia 1.** "Ranged" é quem atira de longe
  (`RangedEnemyController`); o Goblin Sapper não conta. **Exceção (2026-11-21):** o Burning Skull
  também fica fora dos primeiros 30 s (marcado como `isRanged` no `EnemyDefinitionSO`).

### 2.4 Números dos monstros (vida e dano)

- **PENDENTE — velocidade de movimento dos monstros** (pedido do usuário, 2026-11-21): aumentar
  um pouco a velocidade de movimento dos monstros, subindo gradativamente por tier (T1 mais
  lentos, tiers altos mais rápidos). Entra junto com o rebalanceamento dos números dos monstros
  (D9.6). Cuidado: a predição de movimento e o reposicionamento por distância usam a velocidade;
  conferir que o `AiDestinationLookahead` (4 unidades) continua maior que velocidade × 0,3 s.

- Hoje a vida e o dano dos monstros chegam a trilhões e quatrilhões, porque foram calibrados pros
  perks. Os perks saem, então **os monstros vão ser rebalanceados**.
- **⏳ PENDENTE** como. O usuário ainda não decidiu a nova escala. Precisa ser decidido antes de
  calibrar as cartas, os itens e os talentos, porque os valores deles dependem da escala dos
  monstros.

### 2.5 Aviso visual de explosão (legibilidade)

- Monstros explosivos do Dia 5 (Burning Skull): mostrar a **área da explosão** e um aviso de que o
  monstro está **perto de explodir**. Hoje é muito difícil de ver.
- Bomba do Goblin Sapper: mostrar a **área** que o jogador precisa evitar.
- Princípio: o jogador nunca pode morrer sem entender do quê. O projeto já tem áreas desenhadas no
  chão em outros ataques, então o mesmo padrão pode ser reaproveitado.

---

## 3. XP e nível

- **1 XP por monstro morto.** XP não é loot, entra direto.
- Tabela de XP em um **settings editável** (ScriptableObject):

| Nível | XP pra subir | Nível | XP pra subir |
|---|---|---|---|
| 1 → 2 | 10 | 11 → 12 | 400 |
| 2 → 3 | 25 | 12 → 13 | 450 |
| 3 → 4 | 50 | 13 → 14 | 500 |
| 4 → 5 | 75 | 14 → 15 | 550 |
| 5 → 6 | 100 | 15 → 16 | 600 |
| 6 → 7 | 150 | 16 → 17 | 700 |
| 7 → 8 | 200 | 17 → 18 | 800 |
| 8 → 9 | 250 | 18 → 19 | 900 |
| 9 → 10 | 300 | 19 → 20 | 1000 |
| 10 → 11 | 350 | | |

- Total pra chegar ao nível 20: 7.410 kills. A tabela pressupõe horda densa (Seção 2).
- **Meta de ritmo:** nível 20 no Dia 9 ou 10. Por volta de 2 níveis por dia (fim do Dia 1 no nível
  2, fim do Dia 2 no nível 4...), com margem de erro de no máximo 1 nível.
- **Ponto a calibrar no teste:** o primeiro level up talvez precise vir mais cedo (5 XP em vez de
  10) pra dar a sensação de progresso nos primeiros minutos. Fica 10 por enquanto.
- **Cap de nível por dia:** cada dia tem um nível máximo, pra que o jogador não torne o dia atual e
  o seguinte triviais. **O XP que passar do cap vira gold** (pra não parecer desperdício).
  **⏳ PENDENTE** os valores de cap por dia.
- Subir de nível aumenta automaticamente: **dano, vida, cura** (heróis com cura) e **bolha**
  (heróis com escudo, como o Paladin). **⏳ PENDENTE** os valores por nível.

---

## 4. Especialização (escolha no nível 1)

Assim que a run começa, o jogador escolhe 1 de 2 especializações do herói. A escolha define o
estilo de jogo da run inteira. O kit (Primary, Survival, Ultimate) **nunca muda**: a spec só muda
como as habilidades interagem.

| Herói | Opção A | Opção B |
|---|---|---|
| Barbarian | **Sanguinário:** todo ataque (Primary e Ultimate) aplica Sangramento. | **Terremoto:** o Primary gera um Ground Crack no local. |
| Ranger | **Sanguinário:** todo ataque (Primary e Ultimate) aplica Sangramento. | **Olhos de Águia:** os ataques causam mais dano quanto maior a distância percorrida. A Ultimate não aplica mais Sangramento, só causa dano a quem passa por cima. |
| Mage | **Imolação:** todo ataque (Primary e Ultimate) aplica Fogo. Sem pet. | **Familiar:** começa a run com o pet. |
| Druid | **Floresta:** os upgrades de nível podem aumentar a quantidade de vinhas. Ao usar a Ultimate a vida não é recuperada, é proporcional (com 20% de vida, o Urso/Alce entra com 20%). | **Bestial:** ~~os upgrades podem aumentar o tamanho do Urso/Alce~~ (🔁 tamanho vai pros itens). A transformação cura a vida inteira, a run começa com 100% de carga de Ultimate, e a forma de Alce não tem mais duração (é infinita). |
| Cleric | **Dor:** os ataques normais podem aplicar o DoT da Ultimate, porém mais curto. | **Cura:** de tempos em tempos solta uma cura em si mesmo (animação nova: variação de poucos frames da cura do Shift, que é lançada 3x; a nova é lançada 1x e só acontece se ele não estiver na cura do Shift). |
| Paladin | **Bolha Sagrada:** de tempos em tempos recebe uma bolha de 1/4 da vida atual que absorve dano. | **Julgamento:** os ataques aplicam Stun. |
| Rogue | **Envenenamento:** todos os ataques aplicam um DoT de Poison. | **Bombardeiro:** ganho de energia muito alto, e as bombas podem ganhar mais projéteis. |
| Gunslinger | **Tiro Certeiro:** a variação de ângulo dos tiros fica muito menor. | **Pistola Infernal:** todos os ataques (Primary, Survival e Ultimate) aplicam Fogo. |
| Assassin | **Mortal:** a Ultimate não tem cargas, é por duração (aumentável por upgrades de nível e itens). A invisibilidade dura a Ultimate inteira, não funciona com bosses, e o monstro atacado pega target mesmo com o Assassin invisível. | **Sanguinário:** todos os ataques (Primary e Ultimate) aplicam Sangramento. A Ultimate é por cargas de ação com duração: acaba quando a duração passa ou as cargas acabam. ⚠️ Atenção na implementação: é o único herói com 2 modos de Ultimate. |
| Blood Mage | **Sangramento:** todos os ataques (Primary e Ultimate) aplicam Sangramento. Sem pet. | **Familiar:** começa a run com o pet. |

Os pets já existem. A mudança é que eles deixam de vir por padrão: só aparecem na spec Familiar.

---

## 5. Cartas de upgrade por nível

Modelo: o "Ctrl+Q/W/E/R" do League of Legends. Subir de nível melhora uma habilidade do kit, de
forma previsível, de propósito. A variação entre runs vem da spec, dos itens e dos talentos.

A cada nível, o jogador recebe **3 cartas, sempre uma de cada:** Primary, Survival e Ultimate.
Escolhe 1. A tela de cartas **pausa o jogo**.

**Regra de 2026-11-19: as cartas não mexem mais em tamanho.** As cartas melhoram a **velocidade
de animação**, o **cooldown**, a **quantidade de projéteis ou prefabs** (Ranger, Druid, Cleric,
Blood Mage, Paladin, Assassin...), a **duração** de certas ações (ex.: Rogue) e os efeitos
específicos de cada herói. **Tamanho do herói, tamanho de projétil e tamanho de área saem das
cartas e passam a vir dos itens** (Seção 7.3) e de talentos. Nas tabelas abaixo, os efeitos de
tamanho estão marcados com 🔁 (movidos pra itens). **⏳ PENDENTE** o que substitui esses efeitos
nas cartas, e se velocidade e alcance de projétil continuam nas cartas ou também vão pros itens.

### 5.1 Primary

Toda carta de Primary: **−cooldown, +velocidade de animação, +knockback**, mais o efeito
específico do herói:

| Herói | Efeito específico |
|---|---|
| Barbarian | 🔁 ~~Aumenta o tamanho do Barbarian e, com isso, a área do ataque. Na spec Terremoto, também aumenta a área do Ground Crack.~~ ⏳ PENDENTE o substituto. |
| Ranger | Mais flechas. Na spec Olhos de Águia, também aumenta a distância e a velocidade dos projéteis. |
| Mage | 🔁 ~~Aumenta a área do ataque.~~ ⏳ PENDENTE o substituto. |
| Druid | Mais vinhas (na spec Floresta) e mais dano do Primary na forma de Urso/Alce. |
| Cleric | Mais bolas de fogo. |
| Paladin | Mais martelos. |
| Rogue | Aumenta a duração do ataque, diminui o intervalo entre ticks de dano e aumenta a área. |
| Gunslinger | Mais tiros. |
| Assassin | Mais ataques. |
| Blood Mage | Mais ataques. |

### 5.2 Survival

Toda carta de Survival: **−cooldown, +velocidade de animação**, mais o efeito específico:

| Herói | Efeito específico |
|---|---|
| Barbarian | Aumenta o multiplicador de dano e de velocidade durante o Shift. |
| Ranger | Aumenta os ticks da cura. |
| Mage | Nada além do padrão. Na spec Familiar, também aumenta a velocidade e o cooldown de ataque do pet (🔁 ~~e o tamanho dele~~). |
| Druid | Aumenta a velocidade de movimento na forma do Shift e o dano do Shift na forma de Urso/Alce. |
| Cleric | Aumenta a cura e a velocidade dos ticks de cura. |
| Paladin | Aumenta o knockback do escudo. |
| Rogue, Gunslinger, Assassin, Blood Mage | Nada além do padrão. |

### 5.3 Ultimate

Toda carta de Ultimate: **−cooldown, +velocidade de animação**, mais o efeito específico:

| Herói | Efeito específico |
|---|---|
| Barbarian | Mais projéteis, mais rápidos e com mais alcance (🔁 ~~maiores~~). |
| Ranger | Mais projéteis, e projéteis mais rápidos e com mais alcance. |
| Mage | Projétil mais rápido e com mais alcance (🔁 ~~maior~~). |
| Druid | 🔁 ~~Na spec Bestial, aumenta o tamanho da forma da Ultimate.~~ ⏳ PENDENTE o substituto. |
| Cleric | Aumenta a duração do DoT. |
| Paladin | Mais espadas, e espadas mais rápidas. |
| Rogue | 🔁 ~~Aumenta o tamanho da bomba e da área de explosão (nas duas specs).~~ Na spec Bombardeiro, também dá mais projéteis de bomba, lançados em ângulos a partir da posição do mouse, com a mesma fórmula do Primary do Paladin. |
| Gunslinger | Mais tiros na Ultimate. |
| Assassin | Mais duração e/ou mais cargas de ação, conforme a spec. |
| Blood Mage | 🔁 ~~Aumenta o tamanho do prefab.~~ No último nível, cada Ultimate gera uma segunda Ultimate (como o antigo perk "Onda Dupla"). |

**⏳ PENDENTE** os valores numéricos de cada carta, e quantas vezes cada uma pode ser escolhida.

---

## 6. Talentos

- A cada **X níveis** (além da carta de upgrade do nível), o jogador recebe 1 talento.
  **⏳ PENDENTE** o valor de X (referência do usuário: no ARAM de cartinhas do LoL, as cartas fortes
  vêm nos níveis 1, 7, 11 e 15).
- Talentos são **iguais pra todos os heróis**: upgrades gerais. Partem dos talentos já criados na
  Deadline 8.1 (alguns precisam ser alterados) e mais talentos novos serão criados.
- Aparecem em **3 cartas**, o jogador escolhe 1. A tela pausa o jogo.
- **Reroll:** abaixo das cartas existe um botão de reroll por carta. Cada carta pode ser rolada
  **1 vez** por oferta. A carta nova nunca repete as 2 que estão do lado nem a que acabou de sair.
  O jogador pode rolar as 3 cartas, mas cada uma só uma vez. Na próxima oferta, os rerolls voltam.
- A variedade entre runs depende do tamanho desse baralho e do de itens. Começa pequeno pra ter uma
  build jogável, e cresce depois.
- **⏳ PENDENTE** a lista de talentos.

---

## 7. Gold, Vendor itinerante e itens

### 7.1 Gold da run

- **Não existe mais loot nem venda de loot.** Os 15 itens de loot e o Vendor comprador saem do
  jogo.
- O gold vem só de matar monstros. Cada monstro dropa uma **quantidade aleatória** de gold, como
  um **item no chão** com sprite própria (no lugar de todos os loots atuais). Não entra mais
  automaticamente: o jogador precisa coletar.
- **⏳ PENDENTE** as faixas de gold por monstro e por tier.

### 7.2 Vendor itinerante

- O Vendor fixo some. Entra um **NPC vendedor que aparece em lugares aleatórios do mapa**, parecido
  com o baú, pra fazer o jogador se movimentar.
- Aparece todo dia, mais de uma vez:
  - primeira aparição depois de um tempo aleatório dentro de uma faixa (x segundos);
  - fica no mesmo lugar por y segundos (referência: uns 30 segundos);
  - some por z segundos (referência: uns 10 segundos);
  - reaparece em outro lugar.
- **⏳ PENDENTE** os valores de x, y e z.
- A loja do Vendor **pausa o jogo**.

### 7.3 Itens

- O Vendor vende **itens passivos**, os mesmos pra todos os heróis, pensados como **combos de
  build** (no estilo dos itens do LoL).
- Exemplos levantados: aumentar a velocidade do jogador, aumentar a duração dos DoTs, aumentar o
  dano dos DoTs, um item no estilo "Armadura de Espinhos" do LoL.
- **Não existem itens ativos**, só passivos.

**Direção de 2026-11-19: os itens são a "bagunça" na tela.** Vem da pergunta de um amigo ("se você
achasse o MonsterLLC na Steam, feito por outra pessoa, o que você teria vontade de jogar?"). A
resposta do usuário: atacar andando e **muita coisa acontecendo na tela ao mesmo tempo**.

- O jogo tem um limite físico: **todo herói tem só 3 habilidades**, e as cartas evoluem o que ele
  já tem. A variedade e o caos vêm dos itens, que fazem coisas **automaticamente**, sem novo botão
  e sem quebrar o kit de 3 habilidades.
- Os itens servem pra **todos os heróis**. Um item de tamanho, por exemplo, deixa qualquer herói
  gigante, não só o Barbarian.
- **Dois tipos de item convivem:**
  - **itens de stat fixo** (vida, velocidade, etc.);
  - **itens de efeito**, que fazem coisas sozinhos e **escalam com stats** pra recompensar quem
    combina itens. Exemplo do usuário: a **Armadura de Espinhos** causa dano baseado na **vida** do
    player, então uma build de vida passa a aumentar o dano. O objetivo é deixar "roubado" quem
    souber combar.
- A lista de itens vai ser pensada com calma mais pra frente (debate próprio, antes do D9.7).
- **PENDENTE — efeito "chamas orbitando o herói"** (2026-11-20). O usuário gostou muito do
  efeito falso do teste de densidade (D9.2) e quer algo no jogo com ele: item, talento ou carta,
  ainda sem definir. Receita do teste, pra recriar igual, já que o script `HordeDensityTest` vai ser apagado:
  - Cada chama é um sprite com o clipe **`Fire`** do `Assets/Animation/Status/Status.controller`,
    que é a animação de fogo dos status, em loop. Foi desenhada com `sortingOrder` 50.
  - Cada chama orbita o herói num **raio sorteado entre 2 e 9 unidades**, com **velocidade
    angular sorteada entre -3 e 3 rad/s** (no mínimo 1 em módulo) e um ângulo inicial aleatório.
    Tem chama girando nos dois sentidos, em distâncias diferentes.
  - No teste, o movimento usava um `Rigidbody2D` cinemático com `MovePosition` no `FixedUpdate`
    e um `CircleCollider2D` trigger de raio 0,4.
  - **No jogo, o que importa é só a ANIMAÇÃO das chamas, sem collider** (decisão do usuário,
    2026-11-20). O collider do teste existia só pra medir o custo de física: com 500 chamas na
    layer `Default`, o jogo caiu pra 1 FPS. Sem collider, basta mover o `Transform`. Se a versão
    do jogo causar dano, que seja por consulta (`Overlap` a cada tick), nunca por trigger em cada chama.
- **PENDENTE — laboratório de animações** (2026-11-20). Fazer testes com as animações e efeitos
  que já existem no projeto (status, projéteis, hazards de monstros, pets), combinando e repetindo
  do jeito que o efeito das chamas surgiu, pra dar inspiração de "poderes" novos (itens, talentos
  e cartas). Fazer antes do debate da lista de itens (D9.7).
- Exemplos do usuário:
  - **aumentar o tamanho do herói** (no lugar da carta de tamanho do Barbarian); também pode
    existir como talento que aumenta o tamanho a cada nível;
  - **espinhos:** ao sofrer dano, dispara espinhos pra todas as direções;
  - **projéteis teleguiados:** os projéteis perseguem monstros até matar. Se matar e ainda sobrar
    "dano pra dar", continuam perseguindo o próximo;
  - aumentar tamanho de projétil e de área (saiu das cartas, Seção 5).
- **Cuidados técnicos (da discussão):**
  - **Ganchos comuns pra todos os heróis.** Hoje cada herói tem o próprio tipo de projétil e o
    próprio jeito de causar dano. Pra um item funcionar em qualquer herói, o jogo precisa de
    eventos comuns: "herói sofreu dano", "projétil nasceu", "acertou um monstro", "matou um
    monstro", e um stat de escala do herói e dos projéteis.
  - **Item que não serve pro herói.** Projétil teleguiado num Barbarian, que só tem projétil na
    Ultimate, vale quase nada. **⏳ PENDENTE** se o Vendor filtra os itens pelo herói, ou se todo
    item é desenhado pra servir em qualquer um.
  - **Reação em cadeia.** Um item que dispara ao acertar, combinado com outro que dispara ao
    acertar, pode virar um loop infinito (espinho acerta, dispara outro efeito, que dispara outro
    espinho...). Os efeitos disparados por item **não disparam outros itens**, ou têm um limite de
    profundidade.
  - **Legibilidade no caos.** Com a tela poluída de efeitos do jogador, os avisos dos monstros
    (Seção 2.5) precisam continuar visíveis: os efeitos dos itens ficam numa camada abaixo e mais
    transparentes que os avisos de perigo.
  - **Tamanho do herói tem custo.** Herói maior tem hitbox maior (é acertado mais vezes) e pode
    enroscar em props. Pode ser um custo de design proposital do item.
  - **Performance.** Centenas de monstros mais centenas de projéteis e espinhos. O teste de
    densidade (Fase 0B) mede também muitos projéteis vivos, não só monstros.
- **⏳ PENDENTE** a lista completa de itens e preços (precisa de debate próprio).
- **⏳ PENDENTE** o que acontece com um item ruim comprado cedo: vender de volta, substituir, ou
  item repetido virar upgrade do mesmo item.

### 7.4 Inventário

- Passa a ter **6 espaços**, só pra guardar os itens comprados do Vendor.

### 7.5 Baú

- O baú continua existindo, mas **não dá mais gold**. Ele dá **1 carta de talento**, a mesma oferta
  de 3 cartas com reroll que vem a cada X níveis (Seção 6).
- Passa a ser **mais raro**.
- **Não aparece mais no começo do dia.** Hoje ele já está no mapa desde o início, e isso está ruim.
  Ele passa a **nascer como um spawn durante o dia**.
- Podem existir talentos que **aumentam a chance de o baú aparecer**.
- **⏳ PENDENTE** a chance ou o intervalo de spawn do baú, e se o mimic continua existindo.

---

## 8. Bosses e fim do dia

- **Os bosses decidem a duração do dia.** O cronômetro continua existindo e continua sendo o
  relógio do dia, mas **pausa enquanto existe um boss vivo**.
- Sequência:
  1. o dia começa e o cronômetro corre;
  2. depois de x segundos nasce o 1º boss e o cronômetro pausa;
  3. quando ele morre, o cronômetro volta a correr; x segundos depois nasce o próximo boss;
  4. só existe **um boss vivo por vez**;
  5. quando morre o último boss do dia, o dia acaba **poucos segundos depois** (referência do
     usuário: uns 10 segundos).
- O cronômetro fica parado com boss vivo e continua controlando a troca Dia/Noite. **A horda não**
  (decisão de 2026-11-20, D9.4): curva, picos e liberação dos ranged seguem o **tempo real** do dia,
  que não pausa com boss — quem demora pra matar boss enfrenta mais monstros.
- **Ordem fixa:** os bosses deixam de ser sorteados e passam a ter uma ordem definida.
- **Teleporte:** se o jogador se afastar demais, o boss é teleportado pra perto dele (não é mais
  removido e trocado). O enrage continua valendo depois do teleporte, então fugir não serve pra
  escapar dele.
- **Enrage:** se o boss não morrer em x tempo, entra em fúria, ganhando velocidade e dano
  gradativamente a cada x tempo. Isso obriga o jogador a matar ou morrer, e impede que ele deixe o
  boss vivo pra farmar a horda.
- **Bosses enxergam o player mesmo invisível.**
- **Nesta Deadline os bosses atuais são usados como estão** (só a sequência, o enrage e a visão de
  invisível entram agora). O rework de mecânicas dos bosses fica pra depois, fora desta Deadline:
  remoção de alguns, alguns viram monstros comuns e outros ganham mecânicas novas exclusivas.
- **⏳ PENDENTE** quantos bosses por dia, a ordem deles, o intervalo entre eles, o tempo até o fim
  do dia depois do último boss e os tempos do enrage.

---

## 9. Progressão permanente (meta-progressão)

- Um **segundo loot**, mais raro mas com queda frequente o bastante: o **gold permanente**, usado
  fora da run.
- Na tela de seleção de herói, ao selecionar um herói (ex.: Barbarian), aparecem os **status base**
  dele e **upgrades permanentes** pra comprar com o gold permanente. Cada herói evolui
  separadamente.
- Objetivo: o jogo deixa de ser uma run só. As runs servem pra melhorar as runs futuras.
- Ao morrer, o jogador leva **100% do gold permanente** coletado.
- **Princípios** (da discussão):
  - os upgrades precisam de **teto**, senão o jogador acaba quebrando o jogo depois de muitas runs;
  - de preferência, upgrades que dão **opções** (ex.: mais um reroll, começar com um item) em vez
    de só números crescendo pra sempre;
  - as primeiras runs, sem nenhum upgrade comprado, precisam ser divertidas por si só;
  - o gold permanente precisa parecer diferente do gold da run (nome e sprite próprios).
- Quantidade de gold permanente que cai e custo dos upgrades em settings editáveis.
- **⏳ PENDENTE** a lista de upgrades permanentes, se bosses dão uma quantidade garantida, e o nome
  da moeda.

---

## 10. Tutorial

- Toggle na tela de seleção de heróis (nome provisório: "Ability Tutorial").
- Ativo, o jogo pausa na **primeira vez** de cada evento e mostra um texto explicando:
  - um baú aparece: ir atrás do baú;
  - o Vendor aparece: ir atrás dele pra comprar itens;
  - um boss aparece: matar rápido pra ele não entrar em enrage.
- Sugestão aceita na discussão: uma mensagem sobre a regra do mundo **"quanto mais longe, mais
  perigoso e mais recompensa"**, já que essa é a única regra sem um evento visível pra disparar o
  texto.
- As setinhas que já existem continuam.

---

## 11. Telemetria

O `RunTelemetryRecorder` (criado em 2026-11-16) é a ferramenta pra validar esta Deadline nos
testes com amigos. Precisa ser adaptado: troca "perk comprado" e "loot vendido" por "carta
escolhida", "talento escolhido", "item comprado" e "nível por minuto".

---

## 12. Fases de implementação

Escopo desta Deadline: **Barbarian, Ranger e Mage 100%**. Ao fim, o usuário monta uma build pra
testar com os amigos. Exceção: a Fase 0A (atacar andando) vale pra todos os 10 heróis.

**Princípio:** cada sistema antigo sai na mesma fase em que o novo entra. O jogo continua jogável
ao fim de cada fase.

| Fase | Entra | Sai |
|---|---|---|
| **0A** | **Atacar andando, todos os heróis** (Seção 1.1): movimento no ataque primário, `attack_walk`, monstros perseguindo o projétil do Assassin | Trava de movimento no ataque primário |
| **0B** | **Teste de densidade da horda** (ver 12.1) | — |
| 1 | Horda com fluxo contínuo e reposicionamento, spawn gradual com picos, ranged fora do começo do dia, aviso visual das explosões | Rajadas de crisis spawn |
| 2 | XP, nível, cartas de upgrade (Barbarian), morte encerra a run, vitória no Divine God, dia termina no último boss, cronômetro pausando com boss, bosses em ordem fixa, teleporte mantendo o enrage, enrage, tela de resumo, rebalanceamento dos monstros | Loja, perks, retry do dia, sorteio de boss |
| 3 | Gold no chão, Vendor itinerante, itens passivos, inventário de 6 slots | Loot, Vendor comprador |
| 4 | Especializações (nível 1), talentos com reroll, baú dando talento e nascendo durante o dia | Baú dando gold, baú no início do dia |
| 5 | Gold permanente e upgrades permanentes no menu | — |
| 6 | Ranger e Mage completos, tutorial, telemetria adaptada | — |

Depois desta Deadline, na ordem do usuário: rework dos bosses → Druid e Cleric → página na Steam.

### 12.1 Fase 0B — Teste de densidade (descartável)

- Objetivo: descobrir quantos monstros vivos o jogo aguenta hoje, e o que trava primeiro.
- Como: no `_TestScene`, subir a população pra 100, 200 e 300 monstros, medir FPS e o profiler.
- Custos suspeitos a medir: pathfinding (A\* por monstro), desvio entre monstros (RVO), criação e
  destruição de monstros (Instantiate/Destroy), animação, e o sistema de slots de melee.
- Soluções candidatas, a escolher conforme o resultado:
  - **flow field:** como todos vão atrás do mesmo alvo, um único mapa de "pra onde andar" calculado
    uma vez, no lugar de um caminho A\* por monstro;
  - RVO só perto do player e separação simples longe;
  - **pooling:** reaproveitar monstros mortos e reposicionar, em vez de criar e destruir;
  - animação desligada pra monstros fora da tela;
  - Simulation Rings (já existe) com raios ajustados.
- O código do teste é descartável e não entra no jogo. O resultado define os números da Seção 2.2.

---

## 13. Fora do escopo desta Deadline

- Rework de mecânicas dos bosses.
- Druid, Cleric, Paladin, Rogue, Gunslinger, Assassin e Blood Mage (as specs deles estão
  registradas na Seção 4 pra não se perderem, mas só são implementadas depois). Exceção: o
  atacar andando (Seção 1.1) entra pra todos eles já nesta Deadline.
- Página na Steam.
