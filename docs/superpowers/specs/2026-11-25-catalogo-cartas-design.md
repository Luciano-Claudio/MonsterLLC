# Catálogo de cartas: 30 itens e 30 atributos (design)

Data: 2026-11-25. Baseado na tabela do usuário em `2026-11-24-catalogo-itens-atributos.md`.

## Por que

O usuário quer uma alpha para os amigos testarem com os 3 heróis (Barbarian, Ranger e Mage). Para isso, as runs
precisam de variedade de build: itens e atributos com efeitos de verdade, não só números. O usuário quer todas
as cartas criadas de uma vez, para colocar os ícones de uma só vez.

## Decisões já tomadas pelo usuário

- **O catálogo novo substitui o atual.**
  - Troll Heart vira **Everliving Heart**, Swift Boots vira **Stormstep Boots** e Thorn Mail vira
    **Thornwake Armor**.
  - Saem: Iron Plate, Power Gauntlet, Magnet Ring, Scholar Tome, Blast Charm e os 6 atributos atuais (Ancestral
    Strength, Iron Will, Quick Mind, Vitality, Hunter's Eye e Greed).
- **Regras dos itens:** todo item dá +10% de vida máxima por nível e vai até o nível 5. Exceção: Ironroot Core
  dá +20% de vida por nível e nenhum outro efeito.
- **Atributos:** no máximo 6 por run, sem níveis.
- **Repetidos:**
  - Plague Herald (item) espalha o DOT quando o inimigo morre. Death's Echo (atributo) só explode, sem espalhar.
  - Withering Eye (item) dá mais dano em todos os ataques diretos contra inimigos com DOT. Elemental Supremacy
    (atributo) dá um bônus maior, mas só no ataque primário.
- **Coisas que a run não tem mais:**
  - Last Ember funciona uma vez a cada 5 minutos da run.
  - Golden Momentum e Collector's Instinct usam as gemas de XP.
  - Treasure Sense dá +chance de baú e +1 reroll no baú. Merchant's Whisper dá o mesmo para o vendedor.
- **Ficam fora do sorteio até existir o que elas usam** (o asset é criado agora, para o ícone):
  - Gilded Hand e Greedbound Soul: precisam do Gold permanente (etapa 6).
  - Mender's Seal e Shield Battery: precisam das tags Cura ou Escudo, que nenhuma das 6 especializações da alpha tem.
- **Arquitetura:** opção 1, um ponto único de acerto do herói (detalhes abaixo).
- **Ordem do projeto:** este catálogo vem antes das animações das cartas. O sistema de som e os outros 7 heróis
  ficam para depois da alpha.

## Objetivos

1. Criar os 60 assets de carta, nas pastas `Items/` e `Stats/`, com título, descrição, tag e valores.
2. Fazer as 56 cartas sorteáveis funcionarem nos 3 heróis da alpha.
3. Remover as 11 cartas antigas que saem, sem deixar nada quebrado.

## Não-objetivos

- Animações e efeitos visuais próprios das cartas. Vêm na próxima etapa; agora, o que precisa ser visto usa
  um projétil ou efeito que já existe.
- Som.
- Os outros 7 heróis. As cartas só precisam funcionar no Barbarian, no Ranger e no Mage.
- Balanceamento fino. Os números abaixo são o ponto de partida para o teste no Play.

## Anti-objetivos

- Uma carta que aparece mas não faz nada no herói da run.
- Uma carta que repete o efeito de outra em outro lugar do código, deixando o dano contado duas vezes.
- Os bônus de uma run passando para a próxima. O domain reload está desligado, então todo estado estático
  zera no começo da run.

## Arquitetura (opção 1, escolhida pelo usuário)

### Ponto único de acerto do herói

- Todo dano **direto** dos 3 heróis passa a ir por `HeroDamage.Direct(...)`. São estes os acertos:
  - golpe do Barbarian e Thunder Blades;
  - flechas e facas do Ranger;
  - chama e bola de fogo do Mage.
- Essa função:
  1. calcula o crítico (chance e multiplicador vêm das cartas);
  2. aplica os bônus que dependem do alvo (vida máxima do alvo, vida baixa, alvo com DOT, chefe, pilhas
     de foco);
  3. aplica o dano;
  4. avisa todo mundo num evento (`OnHeroDirectHit`), com alvo, dano final, habilidade, se foi crítico,
     se matou e quanto dano passou do necessário para matar.
- DOT, pets e espinhos **não** são dano direto. Eles têm os próprios ganchos.
- O dano do pet não passa pelos bônus do herói. Só a carta de pet (Primal Bond) mexe nele.

### Efeitos das cartas

- Cada efeito é um componente pequeno no `Systems`, como o `BlastCharmEffect` de hoje. O componente lê o
  valor da carta com `Cards.Sum(None, stat)`.
- Cada efeito novo ganha um valor próprio no fim do enum `CardStat`. A ordem não pode mudar, porque os assets
  guardam o número.
- Valores secundários (raio, duração, recarga interna, limite de pilhas) ficam no `RunSettings`, com 🔢, para
  ajustar no Inspector.

### Sistemas que precisam existir

- **Crítico geral:** chance e multiplicador iguais para todos os ataques diretos dos 3 heróis.
- **Pilhas de DOT:** o `StatusEffectController` passa a aceitar várias aplicações do mesmo tipo.
  - Cada aplicação tem a própria duração e o próprio dps.
  - O padrão continua 1 aplicação; o Blight Crucible aumenta o limite.
  - O visual continua um só por tipo.
- **Quem matou:** o inimigo passa a saber se o último dano foi de DOT, para as cartas de "morreu por DOT".
- **Tamanho do herói:** um multiplicador geral de escala, usado por Colossus Heart e Titan's Tribute.
- **Escudo temporário:** o escudo atual (`AddShield`) ganha duração.
- **Movimento contínuo:** o herói conta há quanto tempo está andando sem parar.
- **Gemas:** ímã infinito (Collector's Instinct) e evento de gema coletada (Golden Momentum).
- **Baú e vendedor:** +1 aparição na agenda da run, e reroll por fonte em vez de 1 fixo.
- **Projéteis do Ranger:** perseguir, dividir, ricochetear, tamanho, velocidade e alcance. Só o Ranger tem a tag
  Projétil na alpha. Valem para as flechas (primário) e as facas (Ultimate).

## As cartas: números propostos (o usuário revisa)

### Itens (nível máximo 5; todos dão +10% de vida por nível, menos o Ironroot)

| # | Item | Tag | Efeito por nível | Detalhes |
|---|---|---|---|---|
| 1 | Sovereign's Bane | — | +1% da vida máxima do alvo como dano extra no primário | Contra chefe vale o mesmo % (por isso é forte contra chefe) |
| 2 | Echo Fang | — | A cada 4 acertos do primário, repete 20% do dano do último acerto | Sem animação; aplica no mesmo alvo |
| 3 | Executioner's Mark | — | +10% de dano contra inimigos abaixo de 30% da vida | Todos os ataques diretos |
| 4 | Crimson Pact | — | Cura 1% do dano direto causado | Roubo de vida |
| 5 | Razor Crown | — | +25% de dano crítico (o crítico sai de ×2 para até ×3,25) | |
| 6 | Primal Bond | Pet | Pet: +10% de dano, +5% de velocidade de ataque, +5% de tamanho | Fênix e urso |
| 7 | Shattered Fortune | — | Crítico tem 10% de chance de um 2º impacto de 50% do dano crítico | |
| 8 | Wellspring Chalice | — | Usar o Shift cura 5% da vida máxima | Recarga própria de 10 s |
| 9 | Blight Crucible | DOT | Nível 1: até 2 pilhas por tipo de DOT. Nível 2+: até 3. Do nível 3 em diante, +10% de dano de DOT por nível | Cada pilha com duração própria |
| 10 | Ashen Hourglass | DOT | +15% de duração dos DOTs | |
| 11 | Plague Herald | DOT | Inimigo que morre com DOT tem 20% de chance de passar o mesmo DOT a até 2 vizinhos | Raio 2,5 |
| 12 | Withering Eye | DOT | +8% de dano direto contra inimigos com DOT | Todos os ataques diretos |
| 13 | Cursed Diadem | DOT | Cada tick de DOT tem 5% de chance de crítico | Usa o mesmo multiplicador de crítico |
| 14 | Affliction Heart | DOT | A cada 8 ticks de DOT, explosão de 30% do dano do primário em volta do alvo | Raio 1,5 |
| 15 | Thornwake Armor | Tank | Ao levar dano direto, lança 2 + nível espinhos em círculo. Cada espinho dá 3% da vida máxima do herói | Recarga de 0,5 s. Visual provisório: Thunder Blade pequena. Sem DOT |
| 16 | Colossus Heart | Tamanho | +8% de tamanho do herói e +8% de área de todos os ataques | |
| 17 | Ironroot Core | — | Só vida: +20% de vida máxima por nível | Sem outro efeito |
| 18 | Everliving Heart | — | Regenera 0,5% da vida máxima por segundo | Substitui o Troll Heart, com o mesmo valor |
| 19 | Mender's Seal | Cura, Escudo | +15% de cura e escudo recebidos | Fora do sorteio na alpha |
| 20 | Last Ember | — | Evita uma morte: volta com 10% da vida por nível e fica 2 s invulnerável | Uma vez a cada 5 minutos |
| 21 | Gravebound Plate | Tank | +1% de redução de dano por inimigo a até 2,5 de distância, até 10 inimigos | Respeita o teto de 75% |
| 22 | Predator's Eye | Projétil | Flechas e facas perseguem o inimigo mais perto (raio 3 + 1 por nível). Se matarem e ainda tiverem dano, perseguem outro | |
| 23 | Shattered Prism | Projétil, Hitscan | 10% de chance, ao acertar, de soltar 2 estilhaços com 40% do dano para os lados | |
| 24 | Gravity Core | Projétil | +15% de tamanho e de área de colisão dos projéteis | |
| 25 | Windpiercer Charm | Projétil | +10% de velocidade e +10% de alcance dos projéteis | |
| 26 | Stormstep Boots | — | +8% de velocidade de movimento | Substitui o Swift Boots |
| 27 | Reaper's Feast | — | Cada abate cura 0,5% da vida máxima | Limite de 5% por segundo |
| 28 | Soul Furnace | — | +15% de energia da Ultimate ganha nos abates | |
| 29 | Gilded Hand | — | (raio e quantidade de Gold) | Fora do sorteio até a etapa 6 |
| 30 | Deadeye Lens | — | +5% de chance de crítico em todos os ataques diretos (Primário, Shift e Ultimate) | |

### Atributos (nível único, no máximo 6 por run)

| # | Atributo | Tag | Efeito |
|---|---|---|---|
| 1 | Titan's Tribute | Tamanho | A cada 25 abates, +1% de vida máxima e +1% de tamanho, até +50% de cada. A % vale sobre a vida máxima atual, então acompanha a vida ganha depois |
| 2 | Trophy Hunter | — | Cada chefe morto dá +5% de dano contra chefes, até +50% |
| 3 | Endless Hunger | — | A cada 30 abates, +1% de dano, até +25% |
| 4 | Golden Momentum | — | Pegar uma gema dá +20% de velocidade por 2 s. Cada gema renova o tempo |
| 5 | Survivor's Legacy | — | Cada chefe morto dá +5% de energia da Ultimate, até +50% |
| 6 | Dangerous Bargain | — | +30% de XP e +15% de dano recebido |
| 7 | Relentless Momentum | — | Depois de 2 s andando sem parar, o próximo primário dá +50% de dano e +30% de área |
| 8 | Predator's Focus | — | Cada acerto seguido do primário no mesmo inimigo dá +5% de dano nele, até +50%. Trocar de alvo zera |
| 9 | Critical Fever | — | Cada crítico dá +2% de dano e +2% de velocidade por 4 s, acumulando até 10 vezes |
| 10 | Crowd Breaker | — | Se um primário acertar 3 ou mais inimigos, o próximo primário dá +40% de dano |
| 11 | Blood Frenzy | — | Matar com menos de 40% da vida dá +25% de dano e +25% de velocidade por 4 s |
| 12 | Death's Echo | DOT | Inimigo morto por DOT explode com 50% do dano do primário (raio 1,5). Não espalha DOT |
| 13 | Overkill | — | O dano que passa do necessário para matar, num acerto direto, vai 50% para o inimigo mais perto (até 3 de distância). Não encadeia |
| 14 | Final Stand | — | Abaixo de 30% da vida: +20% de redução de dano e +20% de dano |
| 15 | Scars of Battle | Tank | Cada dano direto recebido dá +3% de redução de dano por 4 s, até 5 pilhas |
| 16 | Unbroken | — | 15 abates seguidos sem levar dano dão um escudo de 20% da vida máxima por 8 s |
| 17 | Adrenaline Rush | — | Ao cair abaixo de 25% da vida, +30% de velocidade por 3 s. Recarga de 10 s |
| 18 | Shield Battery | Escudo | Escudo quebrado vira energia da Ultimate. Fora do sorteio na alpha |
| 19 | Warrior's Feast | — | Chefe morto: cura 25% da vida máxima e dá escudo de 15% por 10 s |
| 20 | Moving Fortress | — | Depois de 1,5 s andando sem parar, 15% de redução de dano enquanto continuar andando |
| 21 | Ricochet Fate | Projétil, Hitscan | Flechas e facas ricocheteiam 1 vez para outro inimigo (até 4 de distância), com 60% do dano |
| 22 | Affliction Harvest | DOT | A cada 20 ticks de DOT, +2% de energia da Ultimate |
| 23 | Elemental Supremacy | DOT | O primário dá +25% de dano em inimigos com DOT |
| 24 | Relentless Casting | — | Cada acerto direto do primário tira 0,1 s da recarga do Shift, até 0,5 s por segundo |
| 25 | Spirit Link | — | Usar o Shift dá 5% de energia da Ultimate |
| 26 | Final Judgment | — | A Ultimate dá +30% de dano em chefes e mini chefes |
| 27 | Collector's Instinct | — | Ímã infinito: toda gema da tela vem para o herói |
| 28 | Merchant's Whisper | — | +1 aparição do vendedor na run e +1 reroll na tela do vendedor |
| 29 | Greedbound Soul | — | (Gold a cada 25 abates) Fora do sorteio até a etapa 6 |
| 30 | Treasure Sense | — | +1 aparição de baú na run e +1 reroll na tela do baú |

## Decisões do usuário depois da revisão do construtor

- **Crítico base:** todo ataque direto dos 3 heróis tem **5% de chance de crítico** (×2). O Deadeye Lens soma
  em cima. Os statics antigos de crítico dos perks deixam de ser a fonte.
- **Chama do Mage**, explicada pelo usuário:
  - A unidade é o **ciclo da animação de ataque**, de 12 quadros. Cada ciclo dá o dano cheio do Mage,
    dividido entre os ticks daquele ciclo. Exemplo: 3 de dano, com tick a cada 0,25 s.
  - Se a duração do ataque passar de um ciclo, a animação recomeça e o dano recomeça junto. Com 24 quadros, o
    Mage dá 3 + 3 = 6.
  - **Efeitos "por acerto"** (Sovereign's Bane, Crimson Pact, Echo Fang, Relentless Casting...): o valor de um
    acerto é dividido entre os ticks do ciclo da mesma forma.
    - Exemplo: um item que daria 1 de dano por ciclo dá 1 dividido pelos ticks, e 2 no total em 24 quadros.
  - **Contadores e "próximo primário":** cada ciclo conta como 1 ataque.
    - Echo Fang: conta 1 ataque por ciclo.
    - Predator's Focus: 1 pilha por ciclo.
    - Crowd Breaker: olha quantos inimigos o ciclo acertou.
    - Relentless Momentum: o bônus vale para o próximo ciclo inteiro.
- **Carta nova do Mage: Lasting Flames** (`mage_primary_duration`). Upgrade de +10% de duração da chama por
  nível, até o nível 10. Vem do usuário: faltava uma carta de duração. No máximo, a duração dobra: um ataque
  de 12 quadros vira 24, ou seja, 2 ciclos de dano.

## Decisões do Claude depois da revisão do construtor (o usuário pode corrigir)

- **Overkill e flechas:** a flecha já carrega o dano que sobra (perfuração). Ela não gera Overkill nem
  Predator's Eye extra; esses usam o que sobrar da reserva quando a flecha some. Nos outros ataques, o
  Overkill é o dano do acerto menos a vida que o alvo tinha.
- **"Dano do primário" nas explosões** (Affliction Heart, Death's Echo): é o dano de um acerto do primário do
  herói naquele momento, com as cartas de dano, sem crítico e sem o buff do Shift. No Mage, é o dano por
  segundo da chama.
- **Pilhas de DOT:**
  - O Plague Herald passa 1 aplicação nova do DOT mais forte de cada tipo, com o dps dela e a duração cheia.
    Não copia todas as pilhas, para não virar bola de neve.
  - "Inimigo com DOT" vale para qualquer tipo (sangramento, fogo, veneno).
- **Cartas antigas com o mesmo efeito:**
  - O Everliving Heart usa o `Regen` que já existe e o Stormstep Boots usa o `MoveSpeed`.
  - O `Thorns` antigo (devolver dano do golpe corpo a corpo) sai. O Thornwake Armor é a mecânica nova de
    espinhos, para o dano não ser contado duas vezes.

## Ficam para o construtor

- Os nomes dos valores novos do `CardStat` e dos componentes de efeito.
- Onde exatamente cada ataque dos 3 heróis chama o `HeroDamage.Direct`.
- Os textos em inglês das descrições, curtos, com `{0}` para o valor por nível, como as cartas atuais.
- Testes EditMode para as regras puras (crítico, pilhas de DOT, contadores) e evals no Editor para os heróis.
- Os ícones ficam vazios; o usuário coloca.
