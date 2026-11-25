# Etapa 4 — Especializações, tags de carta e oferta 60% upgrade

Data: 2026-11-24. Etapa seguinte à 3 (baú/vendedor/itens) da simplificação da run de 25 min.

## Por que

O usuário vai criar ~60 cartas novas (30 itens + 30 atributos, mais upgrades por habilidade). Boa parte
só faz sentido para certos estilos: carta de DOT para quem não aplica DOT, item de projétil para quem não
solta projétil ou armadura de espinhos para um herói à distância seriam cartas mortas. Antes de criar as
cartas, cada herói passa a ter **2 especializações** com estilos diferentes, e cada especialização declara
**tags**. As cartas exigem tags, e o sorteio só oferece o que serve.

Sucesso: o jogador escolhe a especialização no começo da run, o herói joga de forma visivelmente diferente
em cada uma, e nenhuma oferta (level up, baú, vendedor) traz carta de uma tag que a especialização não tem.

## Objetivos

1. **Tela de escolha de especialização:** exclusiva, não é a tela das 3 cartas. Aparece logo no começo
   da run, com o jogo pausado e 2 botões. O jogo só começa depois da escolha, que é a primeira de todas.
   A escolha vale até o fim da run, sem troca.
2. **Tags:** cada especialização tem uma lista de tags. Cada carta pode exigir tags; basta a
   especialização ter **uma** delas. Carta sem tag aparece para todos. O filtro vale em **todas** as
   ofertas: level up, baú e vendedor.
3. **Oferta de level up comum, 60% upgrade:** cada uma das 3 cartas tem 60% de chance de ser Upgrade e
   40% de ser Item. **No máximo 2 itens por oferta**: uma carta que sortearia o 3º item vira Upgrade.
   Três upgrades podem sair. O level up múltiplo de 5 continua só com Stats. Baú e vendedor não mudam.
4. **6 especializações dos 3 primeiros heróis:** Barbarian, Ranger e Mage, cada um com 2.

## Não-objetivos (ficam para depois)

- As 60 cartas novas da tabela do usuário. Entram em etapas seguintes, em lotes, começando pelas que
  servem a essas 6 especializações.
- Os upgrades que evoluem cada especialização, como as Fendas do Terremoto, o Ground Crack criado pela
  Ultimate e os upgrades de nível máximo. A etapa só cria a tag "Terremoto" para essas cartas futuras.
- As especializações dos outros 7 heróis: Druid, Rogue, Cleric, Paladin, Gunslinger, Assassin e
  BloodMage.

## Anti-objetivos

- Uma carta de tag errada aparecer, por exemplo DOT para o Terremoto.
- O jogo rodar, com inimigos ou com o tempo da run correndo, antes da escolha da especialização.
- A escolha de uma run vazar para a próxima. Com o domain reload desligado, todo estado estático
  precisa zerar no começo da run.
- Quebrar os heróis sem especialização.

## Tags

**Tags:** DOT, Projétil, Hitscan, Pet, Tank, Escudo, Cura, Tamanho, Terremoto.

Regras definidas na conversa:
- Uma especialização só recebe a tag de um estilo quando ele é o centro do jogo dela. Se o estilo só
  aparece na Ultimate, a tag fica de fora. Por isso Beast Master e Summoner não têm DOT, e o Druid
  Florest também não terá.
- Hitscan existe para o Gunslinger. Os tiros dele não são objetos voando, então as cartas de "objeto
  voando" (perseguir, tamanho e velocidade do projétil) não servem para ele. As cartas de "acerto à
  distância" (Ricochet Fate e Shattered Prism) vão exigir Projétil **ou** Hitscan.

Tabela de referência, para as etapas seguintes. Nesta etapa só as 6 primeiras linhas são implementadas.

Nomes oficiais (em inglês, definidos pelo usuário em 2026-11-25). Nesta etapa só as 6 primeiras linhas são implementadas.

| Herói | Especialização | Nome antigo na conversa | Tags |
|---|---|---|---|
| Barbarian | **Bloodlust** | Sangramento | DOT, Tank, Tamanho |
| Barbarian | **Earthquake** | Terremoto | Tank, Tamanho, Terremoto |
| Ranger | **Crimson Arrow** | Olhos de Águia | DOT, Projétil |
| Ranger | **Beastmaster** | Beast Master | Pet, Projétil |
| Mage | **Immolation** | Ignite | DOT |
| Mage | **Phoenix Pact** | Summoner | Pet |
| Druid | **Verdant Grace** | Florest | Cura (sem DOT: o DOT só vem da Ultimate) |
| Druid | **Primal Fury** | Feral | Tank, Tamanho |
| Rogue | **Bloodletter** | Bleeding | DOT |
| Rogue | **Bombardier** | Bomberman | Projétil |
| Cleric | **Penance** | Pain | DOT, Cura, Projétil |
| Cleric | **Benediction** | Healer | Cura, Projétil |
| Paladin | **Divine Aegis** | Bubble | Tank, Escudo |
| Paladin | **Judgment** | Judgment | Tank |
| Gunslinger | **Deadeye** | Tiro Certeiro | Hitscan |
| Gunslinger | **Hellfire** | Infernal Shot | Hitscan, DOT |
| Assassin | **Bloodhunt** | Sanguinary | DOT |
| Assassin | **Deathstrike** | Mortal | (nenhuma; o avanço até o alvo não é projétil) |
| Blood Mage | **Blood Rite** | Sanguinary | DOT, Cura |
| Blood Mage | **Soulbinder** | Summoner | Pet, Cura |

A tag "Terremoto" se chama **Earthquake** no código e nos assets.

## As 6 especializações (comportamento, como o usuário definiu)

| Especialização | DOT | Pet | Outras mudanças |
|---|---|---|---|
| Barbarian Bloodlust | Bleeding nos ataques que dão dano: Primário (inclusive os golpes buffados pelo Shift) e Ultimate | — | **Sem** Ground Crack. O Shift aprimora o DOT (detalhe abaixo) |
| Barbarian Earthquake | Nenhum | — | Fica com o Ground Crack, como hoje |
| Ranger Crimson Arrow | Bleeding em tudo: todas as flechas e a Ultimate | — | — |
| Ranger Beastmaster | Bleeding **só na Ultimate**; flechas do primário sem Bleeding | **Pet novo** (animal) | — |
| Mage Immolation | Fire em **tudo** | **Sem** a Fênix | — |
| Mage Phoenix Pact | Fire **só na Ultimate**; primário sem Fire | Fênix | — |

O que já existe no código para isso:
- **Barbarian:**
  - O Bleeding no primário já existe via o perk antigo "Sangramento Perfurante" (`sangramentoPerfuranteDamagePerSecond` e `Duration`, `StatusEffectController`).
  - O Ground Crack é criado no primário (`SpawnGroundCrackAtWithDamage`).
- **Ranger:**
  - As flechas têm chance de Bleeding (`arrowBleedChancePercent`, hoje vinda de perk).
  - As facas da Ultimate já aplicam Bleeding (`knifeBleedDuration`).
- **Mage:**
  - A bola de fogo do primário e a da Ultimate aplicam o status Fire (`fireStatusDamageMultiplier`, `fireballFireStatusDuration`).
  - A Fênix depende do `PetSummoner` (`HasPet`).
- **Pet do Ranger:** é novo. Mesmo comportamento dos pets do Mage e do BloodMage: não morre e persegue o inimigo mais próximo do herói. Deve copiar a estrutura do `PetSummoner` e do pet do Mage, conforme a regra do projeto de procurar o precedente antes de criar. **O animal é um urso.**
  - **Sprites já fatiados:** em `Assets/Sprites/Heros/Ranger/Passiva/`:
    - `Minifantasy_BearIdle` (76 sprites);
    - `BearWalk` (24);
    - `BearAttack` (20);
    - `BearDie` (24);
    - `Summon` (16): o urso surgindo;
    - `Ranger_Summon` (12): o Ranger chamando o pet, como o `summonpet_*` do Mage.
  - **Ainda não existem:** clipes, Animator Controller e prefab do urso, nem os clipes `summonpet_*` do Ranger.
  - **O construtor gera tudo isso copiando a estrutura da Fênix:**
    - `Assets/Prefabs/Heros/Mage/Phoenix.prefab`;
    - `Assets/Animation/Heros/Mage/Passiva/Phoenix/` (`Phoenix.controller`);
    - `Assets/Animation/Heros/Mage/Passiva/summonpet_*.anim`.
  - Os clipes gerados não podem ter linhas em branco no YAML.
  - Os clipes precisam ser conferidos no Animator antes de dizer que estão prontos.

## Abordagens consideradas

1. **Asset de especialização** (escolhida pelo usuário). Um ScriptableObject por especialização com herói, nome, ícone, descrição e tags. O herói aponta para as suas 2. A carta ganha "tags exigidas".
   - Vantagem: textos, ícones e tags editáveis no Inspector, e os próximos 7 heróis são só assets mais comportamento.
   - Custo: um tipo de asset novo e uma tela nova.
2. **Especializações no código de cada herói.**
   - Vantagem: mais rápido agora.
   - Custo: nomes, ícones e tags no C#, e 20 especializações ficam difíceis de manter.
3. **Usar a árvore de perks antiga.**
   - Descartada: é dividida por habilidade, não por especialização, e está saindo com a simplificação.

## Decisões tomadas na conversa

- A especialização é escolhida durante a run, logo no começo, numa tela própria, pausada, com 2 botões.
- É a primeira escolha, e o jogo só começa depois dela.
- Não pode trocar até o fim da run.
- As especializações precisam mudar como o herói joga, não só as tags.
- Foco nos 3 primeiros heróis.
- A oferta de 60% upgrade é sorteada carta a carta, com no máximo 2 itens; 3 upgrades podem sair.

## Decisões do Claude (o usuário pode corrigir)

- A escolha fica guardada só durante a run e zera no começo de cada run.
- Heróis sem especialização (os outros 7) entram na run sem a tela de escolha e sem tags, vendo só as
  cartas sem tag.
- O Bleeding do Barbarian Sangramento começa com os números do perk antigo "Sangramento Perfurante", e o
  ajuste fica para o teste no Play.
- No Ranger Olhos de Águia, "Bleeding em tudo" significa que toda flecha aplica Bleeding (100%), não uma
  chance.
- As cartas que já existem, de etapas anteriores, ficam sem tag e aparecem para todos.
- O campo `heroId` das cartas continua existindo; as tags são um filtro a mais.
- **Exceção para cartas que já existem:** `barbarian_primary_area` só aumenta o Ground Crack (`Barbarian.cs`), então
  recebe a tag Terremoto. O construtor confere as outras cartas do Barbarian com o mesmo critério: nenhuma
  carta pode ficar sem efeito numa especialização.
- **Oferta sem upgrade possível:** se uma carta sorteada como Upgrade não tiver nenhum upgrade elegível, ela
  vira Item, respeitando o limite de 2 itens. Se não houver item possível, vira Stat. Se nada for possível,
  a oferta sai com menos cartas.
- **"Em tudo" significa os ataques que dão dano:**
  - **Barbarian Bloodlust**, definido pelo usuário em 2026-11-25:
    - **Onde aplica:** Bleeding no Primário e na Ultimate. O dps é 50% do dano do golpe e dura 3 s,
      com os dois valores ajustáveis. O "Sangramento Perfurante" dava um dps fixo por rank, o que não
      combina com a regra de o DOT seguir o ataque inicial. Na Ultimate o código é novo.
    - **O Shift não dá dano:** ele não é ataque e não aplica Bleeding sozinho. Os golpes do Primário
      buffados pelo Shift continuam aplicando Bleeding.
    - **O Shift aprimora o DOT:** um Bleeding aplicado durante o Shift usa o dano do golpe que o aplicou,
      já com o buff do Shift. O valor é congelado no momento da aplicação. Se o Shift acabar e o
      Bleeding continuar, o Bleeding segue aprimorado até o fim da duração. Isso vale para todo DOT: o
      dano é sempre o do ataque inicial.
  - **Mage Ignite:** o teleporte não é ataque, então não aplica Fire.
  - **Mage Summoner:** "primário sem Fire" vale para a bola de fogo e para as manchas de fogo no chão que
    ela deixa.
  - **Ranger:** o Shift é camuflagem e não aplica nada.
- **Perks:** estão desligados desde as etapas 1+2. A especialização manda no comportamento e ignora o
  `PerkState`: chance de Bleeding das flechas, `HasPet` da Fênix e o Sangramento Perfurante.
- **Quando a escolha acontece:** uma vez por run, antes de `DayTimer.StartRun()`. Morrer encerra a run, e
  não há mais dias. A especialização é aplicada à instância do herói que nasce. Se o herói for recriado,
  ela é reaplicada.

## Em aberto: o usuário decide na próxima sessão

1. **Upgrades do Ranger e do Mage.** Hoje só o Barbarian tem cartas de upgrade (9). Sem upgrades, o level
   up desses dois vira quase só item. A proposta do Claude é criar nesta etapa um conjunto igual ao do
   Barbarian (dano, área, cooldown e velocidade de animação de cada habilidade) para o Ranger e para o
   Mage. A outra opção é o usuário desenhar esses upgrades antes.
2. ~~Pet do Ranger~~: **resolvido em 2026-11-25.** É um urso, e o Ranger tem animação de invocar
   (`Ranger_Summon`). Detalhes na seção das 6 especializações.
3. ~~Tela de especialização~~: **criada pelo usuário em 2026-11-25** (ver "Tela de escolha" abaixo).

## Tela de escolha (layout do usuário, `_TestScene`)

O objeto `Specialization_Choose` fica no Canvas e começa desligado:

```
Specialization_Choose
  Background_Header
    Text          "Choose Specialization"   (fixo)
    Hero_Name     ← nome do herói
  Button (x=-260)  e  Button (x=260)        (um por especialização)
    Background_Icon / Icon   ← ícone da especialização
    Background_Name / Name   ← nome da especialização
    Description              ← descrição da especialização
  Background_Footer
    Text          "Choose one specialization to begin"   (fixo)
```

- O código preenche `Hero_Name`, `Icon`, `Name` e `Description` a partir dos assets de especialização.
- Por ordem, o Button da esquerda é a 1ª especialização do herói e o da direita é a 2ª.
- Proposta do Claude: aceitar as teclas 1 e 2, como as cartas aceitam 1, 2 e 3.

Textos em inglês para a tela. Proposta do Claude, editável no asset:

| Especialização | Description |
|---|---|
| Bloodlust | Your attacks apply Bleeding. Bleeds applied while your Shift is active are stronger. |
| Earthquake | Your primary attack splits the ground, damaging enemies over time. |
| Crimson Arrow | Every arrow and your Ultimate apply Bleeding. |
| Beastmaster | A loyal bear fights by your side. Only your Ultimate applies Bleeding. |
| Immolation | All your attacks set enemies on fire. |
| Phoenix Pact | A phoenix fights by your side. Only your Ultimate burns. |

O usuário coloca todos os ícones à mão, os das especializações e os das cartas:
- **Especializações:** campo `icon` de cada asset de especialização. A pasta é definida no plano.
- **Cartas:** campo `icon` de cada `CardDefinition` em `Assets/Settings/Cards/` — `Items/`, `Stats/` e `Upgrades/<Herói>` (`General` = sem herói).

O código não preenche ícone nenhum. Asset sem ícone mostra o slot vazio.

O catálogo futuro de 30 itens e 30 atributos está em `2026-11-24-catalogo-itens-atributos.md`.

## Fica para o construtor

- Como guardar as tags no código (enum de flags ou lista).
- Onde guardar a especialização escolhida: estado estático da run, `HeroCards` ou outro lugar.
- Como pausar e começar a run depois da escolha. Hoje o começo é `GameplayBootstrap` chamando
  `DayTimer.StartRun()`.
- O visual da tela de escolha, seguindo o padrão da tela das cartas (Canvas, `CardScreenUI`).
- Os testes (EditMode mais evals no Editor, limpando tudo).

## Acréscimos do usuário (2026-11-25)

- **Atributos (Stats):**
  - No máximo 6, e não sobem de nível (nível máximo 1).
  - Depois de 6, nenhum Stat aparece mais.
  - Ficam na barra `Atribute_Background`, com 6 ícones.
- **Itens:** no máximo 6. Depois disso, só aparecem os que o jogador já tem, até o nível máximo, como
  já é hoje. O nível do item não aparece mais na barra (o `Quantity` foi removido).
- **Upgrades de habilidade:** não aparecem em barra nenhuma. O jogador só sente o efeito.
- **Limpeza:** remover o Tab (ação `Inventory`, que abria a Bag) e os painéis `Inventory` e `Shop` da
  cena. O `ShopHandler` e o estado `Shop` ficam para a etapa 6.

## Revisão do balanceamento (2026-11-25, decidido pelo usuário)

- **Dano:** não existe dano exclusivo por habilidade.
  - Uma carta de upgrade geral, **Brute Force**, aumenta o dano das 3 habilidades: ×1,3 por nível, até o nível 10 (×13,8).
  - Saíram as 8 cartas de dano por habilidade, do Barbarian, do Ranger e do Mage.
- **Vida:** carta de upgrade geral **Iron Constitution**, ×1,2 por nível, até o nível 10 (×6,2).
  - Ela multiplica a soma da vida dos itens (+10% por nível de cada item) e do Vitality.
- **Itens e atributos que dão dano ou vida:** continuam como estão.
  - Dano: Power Gauntlet ×1,1 por nível; Ancestral Strength ×1,25.
  - Vida: todo item dá +10% por nível; Vitality dá +25%.
- **Níveis máximos:** upgrades específicos (cooldown, animação, área) e itens ficam no nível 5.
- **Ajuste:** os números só serão ajustados depois do teste no Play.
- **Upgrades específicos que ficaram:**
  - Barbarian: área do primário (só Earthquake), cooldown e duração do Shift, área, custo de energia e velocidade de animação da Ultimate.
  - Ranger: área do primário, cooldown e duração da camuflagem, área, custo de energia e velocidade de animação da Ultimate.
  - Mage: área do primário, cooldown e velocidade do teleporte, área, custo de energia e velocidade de animação da Ultimate.

## Ajustes do usuário (2026-11-25, depois da revisão)

- **XP:** tabela escrita por extenso em `HeroLevelRules.XpTable`.
  - Só múltiplos de 5, e cada nível custa mais que o anterior.
  - Níveis 1 a 5: 5, 10, 20, 30, 40. Do 6 ao 44, +5 por nível (45, 50 ... 235). Depois, a curva sobe até 1.495.
  - Total até o nível 40: 4.440 XP. Até o 60: 16.375.
- **1º level up:** a oferta sempre traz Brute Force (dano) e Iron Constitution (vida), configurados em
  `RunSettings.firstLevelUpCards`. A 3ª carta segue o sorteio normal.
- **Peso dos itens:** a chance de sair item continua 40%. Dentro do item, os que o herói já tem pesam
  `RunSettings.ownedItemWeight = 3` (decisão do Claude, ajustável); os novos pesam 1. Vale para o level up e para o
  vendedor.
- **Cartas novas, todas até o nível 10:**
  - Multishot (Ranger): +1 flecha.
  - Thunder Storm (Barbarian): +1 Thunder Blade.
  - Cartas de pet do Phoenix Pact e do Beastmaster: +15% de tamanho, +10% de velocidade de ataque, −10% de cooldown.
- **Upgrades prioritários:** no sorteio de Upgrade do level up (os 60%), os upgrades do ataque primário,
  o Brute Force e o Iron Constitution pesam mais.
  - Peso 2,5 até o nível 5, caindo em linha reta até 1,5 no nível 20, e 1,5 depois disso. Os outros upgrades pesam 1.
  - Ajustável no `RunSettings`: `priorityUpgradeWeightEarly`, `priorityUpgradeWeightLate`, `priorityFadeStartLevel` e
    `priorityFadeEndLevel`. Os números são decisão do Claude a partir de "um pouco mais, principalmente no começo".
