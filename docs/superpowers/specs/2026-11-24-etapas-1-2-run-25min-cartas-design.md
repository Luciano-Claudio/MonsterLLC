# Etapas 1 + 2: run de 25 minutos, gemas de XP e cartas do Barbarian (design)

- **Data:** 2026-11-24.
- **Base:** a branch `d9-6ab-monstros-xp`, que ainda não foi mergeada.
- **Memória:** `project_simplification_25min_run`.
- **Revisão:** o documento passou por uma revisão de quem vai construir. As respostas do usuário estão incorporadas, e as decisões que tomei sozinho estão marcadas com *(decisão minha)*.

## 1. Intenção e o porquê

O usuário jogou Vampire Survivors e Everything is Crab e chegou a esta conclusão: esses jogos prometem pouco e cumprem tudo. O MonsterLLC promete demais (dias, gold na run, loja, perks, atributos, baús, vendedor) e não cumpre bem nenhuma dessas promessas.

A decisão é simplificar:

- A run vira **uma partida de 25 minutos, sem dias**.
- Nasce **1 boss por minuto**, na ordem escolhida pelo usuário.
- O herói fica forte **só pelas cartas**. Subir de nível abre 3 cartas para escolher 1 e não dá status.

O trabalho todo tem 7 etapas. Por decisão do usuário, **as etapas 1 e 2 são feitas juntas**, porque sem as cartas o herói não aguenta a run e o resto do loop não dá para testar.

| Etapa | Conteúdo | Quando |
|---|---|---|
| 1 | O loop: relógio da run, bosses por minuto, enrage, troca de tier, dia e noite, mini boss, escala nova, saída dos dias | Este documento |
| 2 | Gemas de XP com ímã, curva de nível até 60, tela de 3 cartas com reroll, cartas do Barbarian | Este documento |
| 3 | Baú, mimic e vendedor dando cartas; itens | Depois |
| 4 | Atributos (os antigos talentos) e combos | Depois |
| 5 | Ranger e Mage | Depois |
| 6 | Gold permanente e buffs por herói | Depois |
| 7 | Fases | Depois |

## 2. Objetivos, não-objetivos e anti-objetivos

**Objetivos.** Uma run completa de 25 minutos, jogável do começo ao fim com o Barbarian:
- os bosses por minuto, o enrage e a troca de tier;
- o ciclo de dia e noite e os mini bosses;
- a escala nova dos monstros;
- as gemas de XP;
- as cartas a cada nível;
- a vitória ao matar o Divine God.

**Não-objetivos:**
- baús, mimic, vendedor e itens;
- atributos e combos;
- o Ranger e o Mage;
- o gold permanente;
- as fases.

Bosses também **nunca** ganham mecânicas novas: eles ficam como estão, só mais fortes.

**Anti-objetivos (seria fracasso mesmo funcionando):**
- Criar qualquer regra, moeda ou tela fora desta lista. O objetivo é **cortar** promessas.
- Deixar números presos no código. Toda regra de tempo, força, XP e carta precisa ser editável num asset.
- Quebrar o que já foi aprovado:
  - a horda em cerco;
  - o anti-kite;
  - o herói empurrando a horda;
  - os avisos de explosão;
  - o desempenho do D9.3;
  - a tela de Resumo total.

## 3. Restrições

- **Motor:** Unity 6000.3.8f1.
- **Regras puras:** ficam em Core com testes EditMode, no mesmo padrão de HordeCurve, MiniBossRules e HeroLevelRules.
- **Desempenho:** nenhuma alocação por frame nos caminhos quentes. O limite continua sendo 300 monstros comuns vivos.
  - As gemas no chão também têm limite e se juntam numa gema grande quando passam dele.
  - A tela de cartas não pode alocar enquanto está aberta.
- **Idioma:** prosa e comentários em pt-BR.
- **Prefab e cena:** seguem o protocolo de backup (git diff, ImportAsset ForceUpdate, conferir depois).
- **Telas novas:** o usuário monta o visual na cena e o código só preenche e liga, como foi feito com a barra de XP e o Resumo total.

## 4. Abordagens consideradas

1. **Adaptar o que existe.** O `DayTimer` viraria um "dia" de 25 min. É rápido, mas as regras ficariam espalhadas por scripts feitos para dias.
2. **Reescrever do zero.** Um diretor novo no lugar do `DayTimer` e do `DayResolver`. Fica limpo, mas quebra a luz, o relógio, a horda, a telemetria e o save.
3. **Regras novas no centro, scripts antigos adaptados.** Peças de regras puras e testadas respondem a tudo que depende do tempo, do XP e das cartas. Os scripts que já funcionam passam a perguntar a elas, e o ciclo de dias sai.

**Escolhida pelo usuário: a 3.** É o mesmo modelo que deu certo na D9.4 e na D9.6.

## 5. Decisões — a run (etapa 1)

### 5.1 Relógio, fim da run e save
- **Relógio:** a run tem 25:00 e conta em tempo real. Ele **só pausa no último minuto**: com o Divine God vivo, chega em 00:00 e fica parado até ele morrer.
- **Vitória:** matar o Divine God.
- **Derrota:** morrer, o que encerra a run com o mesmo fluxo do D9.5 (CircleWipe e Resumo total).
- **Sem Continue:** a run é de uma vez só. Sair no meio significa perder a run. O botão Continue sai do menu e o save no meio da run deixa de existir.
- **Tela de Resumo total:** mostra vitória ou derrota, o tempo sobrevivido e os bosses mortos. O usuário acrescenta esses textos na tela que ele criou, e o código só preenche. *(decisão minha)*

### 5.2 Ordem dos bosses
A ordem fica numa **lista editável no Inspector**, num asset de configuração da run com 25 espaços, um por minuto. A lista inicial é esta:

| Min | Boss | Tier |
|---|---|---|
| 1 | Mother_Slime_Green | T1 |
| 2 | Mother_Slime_Blue | T1 |
| 3 | Goblin_King | T1 |
| 4 | Centaur_King | T2 |
| 5 | Spider_Queen | T2 |
| 6 | Rat_People_Royalty | T2 |
| 7 | Wise_Orc | T3 |
| 8 | Giant | T3 |
| 9 | Cave_Troll | T3 |
| 10 | Flagellant | T4 |
| 11 | Ritual_Guard | T4 |
| 12 | Dark_Channeler | T4 |
| 13 | Zombie_Giant | T5 |
| 14 | Spectre | T5 |
| 15 | Lich | T5 |
| 16 | Krampus | T6 |
| 17 | Supreme_Elemental | T6 |
| 18 | Ancient_Danger_Leader | T6 |
| 19 | Undead_Dragon | T7 |
| 20 | Dragon | T7 |
| 21 | Balrog | T8 |
| 22 | Lobster | T9 |
| 23 | Stickman | T9 |
| 24 | Ambuster | T9 |
| 25 | Divine_God | T10 |

- **Nascimento:** o boss do espaço i nasce em (i − 1) × 60 s, então o Divine God nasce em 24:00.
- **Tiers em blocos:** os bosses de cada tier ficam juntos e nenhum se repete.
- **Divine God:** fica sempre no último espaço, e o Inspector avisa se não estiver.
- **Espaço vazio:** gera um aviso, e aquele minuto fica sem boss sem quebrar nada.

### 5.3 Bosses e enrage
- **Vários vivos:** vários bosses podem estar vivos ao mesmo tempo. Só o Divine God pausa o relógio.
- **Enrage** (é novo, nunca tinha sido implementado):
  - com 1 min vivo, o boss ganha velocidade;
  - com 2 min vivo, ganha dano também, e esse dano continua subindo.
- **Valores iniciais** *(editáveis)*:
  - velocidade ×1,5 a partir de 60 s;
  - dano ×1,5 a partir de 120 s;
  - mais +25% de dano a cada 30 s depois disso.

### 5.4 Escala dos monstros e dos bosses
- **Monstros comuns:** o T1 fica exatamente como está, e **cada tier vale 1,5× a vida e o dano do anterior**. Assim, o T9 vale cerca de 26× o T1 e o T10 cerca de 38×.
  - A regra vale a partir do T2, então o T2 deixa de ser o ajuste manual que existia.
- **Ranged e mini boss:** mantêm os fatores do D9.6 em relação ao melee do tier:
  - ranged: vida ×0,7 e dano ×1,4;
  - mini boss: vida ×4 e dano ×1,5.
- **Bosses:** **são um múltiplo do monstro comum do tier deles**.
  - Valores iniciais: vida 20× e dano 2×. O Divine God usa vida 30×.
  - Cada boss mantém a proporção que tem hoje em relação aos outros bosses do mesmo tier.
  - Os golpes especiais (prefabs referenciados como raios, poças e projéteis) escalam na mesma proporção.
- **Ferramenta:** a ferramenta de escala do D9.6 ganha a parte dos bosses (hoje ela não toca neles) e deixa de pular o T2. Ela já pode rodar de novo sem multiplicar duas vezes.

### 5.5 Troca de tier da horda
- **Quando:** a troca começa quando nasce o **último boss de um tier na lista**. A posição é lida da lista do usuário, então a troca acompanha qualquer reordenação.
- **Rampa** *(decisão minha)*: os monstros do tier seguinte vão de 0% a 100% da horda em cerca de 90 s (editável). Assim nunca ficam mais de 2 tiers ao mesmo tempo, e o tier antigo vai sumindo aos poucos.
- **T8:** tem só o Balrog, então a horda do T8 dura pouco. O usuário aceitou isso.
- **T10:** não tem horda própria. Com o Divine God, a horda continua no T9.

### 5.6 Dia e noite
- **Ciclo:** 1:00 de dia e 0:30 de noite, desde o início da run (editável).
- **Buff noturno:** à noite todos os monstros, bosses incluídos, ficam **+15% mais rápidos**. O bônus soma com o enrage.
- **Noite travada:** a partir do nascimento do Divine God a noite não termina mais. Com ciclos de 1:30, a noite que começa em 23:30 emenda nele.
- **Luz e relógio do HUD:** continuam no `DayNightLightController` e no `DayClockUI`, que já existem.

### 5.7 Mini bosses
- **Quantos:** 1 entre um boss e o seguinte, num momento aleatório entre 15 s e 45 s depois do boss (editável).
- **Sorteio:** entre os mini bosses do tier atual ou de tiers abaixo, sem repetir o anterior.
- **No T1:** não existe nenhum, então os primeiros aparecem a partir do T2.

### 5.8 Mundo
- **Bioma:** o mundo procedural usa **um único bioma**, o do T1, porque é a fase 1. O bioma não muda com o tier nem com a distância.
- **Itens no mundo:** saem nesta etapa e voltam na etapa 3 dando cartas *(decisão minha)*:
  - baús e mimic (`WorldChestSpawnManager`, `ChestMimicController`);
  - o drop de loot (`LootDrop`) para a Bag;
  - o Vendor.

  Sem gold na run, eles seriam promessas que não levam a nada.

## 6. Decisões — XP e cartas (etapa 2)

### 6.1 Gemas de XP
- **Drop:** todo monstro morto deixa **uma gema de XP no chão**, e **todas valem o mesmo (1 XP)**. Mini boss e boss deixam a mesma gema, só que em maior quantidade (editável).
- **Coleta:** o herói coleta passando por cima. Um **ímã** puxa as gemas dentro de um raio, e esse raio cresce com cartas.
- **Limite:** se houver gemas demais no chão, as mais antigas ou mais distantes se juntam numa gema grande que soma o valor delas, como no Vampire Survivors.
- **Substituição:** a gema substitui o XP que hoje entra direto ao matar (`GameEvents.EnemyKilledXp`).

### 6.2 Curva de nível
- **Nível máximo: 60.**
- **Âncoras do usuário:**
  - o 1º nível custa cerca de **15 XP**;
  - chegar ao **nível 40 até o minuto 25 tem que ser bem possível** para um jogador normal;
  - o **nível 60 é só para quem quebra o jogo**, e o usuário quer esse tipo de jogador.
- **Forma da curva:** sobe devagar no começo, ao contrário da tabela antiga do D9.6, e fica muito cara depois do 40. A tabela é editável num asset.
- **Calibragem:** a tabela inicial é calibrada pela taxa real de kills, medida pela telemetria numa run de teste.
- **Barra de XP:** a barra que o usuário montou continua funcionando, agora mostrando "xp/necessário" da curva nova.
- **Saem do D9.6:**
  - o ganho de vida e dano por nível (`ApplyLevel` e o fator do escudo do Paladin);
  - o cap de nível por dia;
  - o XP excedente que virava gold.

### 6.3 Tela de cartas
- **Quando abre:** em cada level-up. O jogo **pausa** e mostra **3 cartas para escolher 1**.
- **Reroll:** **1 por tela**, que troca as 3 cartas.
- **Vários níveis de uma vez:** as telas abrem uma depois da outra.
- **Mesma tela para tudo:** baú, mimic e vendedor vão usar esta tela na etapa 3, então ela recebe uma lista de cartas de qualquer origem.
- **Atributos:** os níveis 10, 20, 30… serão de atributos na etapa 4. Nesta etapa eles oferecem cartas normais.
- **Visual:** o usuário monta na cena (3 cartas com ícone, nome, descrição e nível, mais o botão de reroll). O código preenche e liga.

### 6.4 Cartas do Barbarian (base genérica, aprovada)
- **Por habilidade:**

  | Habilidade | Cartas |
  |---|---|
  | Primário | +dano, +área |
  | Shift | +dano, −cooldown, +área, +velocidade da animação |
  | Ultimate | +dano, −cooldown, +área, +velocidade da animação |

  O usuário decidiu que acelerar a animação **não vale para o Primário**, porque agora o herói ataca andando.
- **Gerais:** +velocidade de movimento, +raio do ímã e +vida máxima.
- **Níveis de carta:** cada carta tem níveis (I, II, III…), e pegar a mesma carta de novo sobe o nível dela.
- **Valores iniciais** *(editáveis)*:
  - os bônus de dano **se multiplicam entre si** (cada nível ×1,25), para acompanhar a escala de 1,5× por tier;
  - os outros bônus somam por nível.
- **Cartas específicas do Barbarian:** ficam fora desta etapa e são desenhadas com o usuário quando a base estiver funcionando. A estrutura de cartas precisa aceitar cartas específicas por herói.

## 7. O que cada script existente passa a fazer

**Regras novas (Core, com testes):**
- **`RunSchedule` + asset `RunSettings`:** boss no segundo t, mistura de tiers, noite, enrage, mini boss e relógio pausado.
- **Curva de nível nova:** substitui a tabela do `HeroLevelRules`.
- **Cartas:** um catálogo como asset; a regra de sorteio das 3 cartas sem repetir e respeitando o nível máximo de cada uma; a aplicação dos bônus.

**Relógio, HUD e luz:**
- **`DayTimer`:** vira o relógio da run (25:00 regressivo, com a pausa de 5.1). Continua disparando `OnTimeChanged` e `DayNightPhaseChanged`, então a luz, o relógio e a barra do HUD seguem funcionando.
- **`DayTimeBarUI` e `BossSignTimelineUI`:** a barra passa a representar os 25 min, com os sinais marcando os 25 bosses.

**Spawn:**
- **`WorldBossSpawnManager`:**
  - o nascimento passa a vir do `RunSchedule`;
  - aplica o enrage e o buff noturno;
  - saem o timer por dia e a escolha por peso de tier.
- **`WorldPopulationManager`:**
  - a mistura de tiers e o mini boss passam a vir do `RunSchedule`;
  - aplica o buff noturno;
  - saem os pesos de tier por dia (`DaySpawnDefinition` / `WorldTierSelector`).
- **`HordeCurve`:**
  - vira **uma curva única de 25 min**, partindo da forma aprovada do dia 1 (10 → 150 em 4 min) e subindo até o teto de 300 por volta do minuto 15 *(decisão minha, editável)*;
  - os picos periódicos continuam.

**Fim da run e mundo:**
- **`DayResolver` / `ResultsHandler`:**
  - saem o fim de dia, o retry, a passagem para a Loja e o save de fim de dia;
  - a morte encerra a run;
  - matar o Divine God dá a vitória.
- **`ChunkTerrainGenerator`:** usa um único bioma.

**Herói e estado da run:**
- **`HeroController`:**
  - os bônus das cartas entram em cima dos status do prefab;
  - o crescimento por nível do D9.6 sai;
  - cada carta só mexe em valores que o Barbarian já expõe (dano, cooldown, área e velocidade de cada habilidade, velocidade de movimento, vida máxima).
- **`RunState`:** saem os campos de dia e de save; entram o nível, o XP e as cartas que o jogador tem.

**Telemetria:**
- Passa a registrar uma tentativa por run.
- Os snapshots passam a ter:
  - o segundo da run e se é noite;
  - os bosses vivos e o enrage;
  - o nível e as cartas escolhidas.
- É ela que dá a taxa de kills para calibrar a curva de nível.

**O que sai do fluxo:**
- o ciclo de dias e o painel de resumo do dia;
- a Loja e os perks *(desligados agora; os arquivos são apagados quando as etapas 2 e 3 estiverem aprovadas)*;
- o gold da run;
- o botão Continue;
- baús, mimic, loot e vendedor (voltam na etapa 3).

## 8. O que fica para quem constrói
- Os nomes dos campos e menus dos assets novos, seguindo o padrão dos settings existentes (🔢 nos números ajustáveis).
- A forma exata da rampa de tiers e da curva de horda de 25 min, respeitando o que está escrito aqui.
- Como o enrage aparece visualmente. Uma sugestão é tingir o boss de vermelho; é opcional.
- O jeito de juntar as gemas quando passam do limite.
- Como desligar a Loja, os perks, os baús e o vendedor sem apagar os arquivos ainda.
