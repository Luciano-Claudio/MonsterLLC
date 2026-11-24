# Etapa 3 — Baú, mímico, vendedor, itens e Stats

Data: 2026-11-24. Continua a simplificação "run de 25 min" (etapas 1+2 já na `main`:
`docs/superpowers/specs/2026-11-24-etapas-1-2-run-25min-cartas-design.md`).

## 1. Intenção

A run hoje só tem uma fonte de força: as cartas de upgrade que saem ao subir de nível. A etapa 3
traz de volta, como recompensas no mundo, o baú (e o mímico) e o vendedor, e cria a segunda e a
terceira fonte de força:

- **Itens** — vêm do vendedor (e do level up). Dão **vida** (a vida do herói passa a vir dos itens)
  e um efeito passivo. Sobem de nível. Ocupam 6 espaços.
- **Stats** (atributos) — cartas raras e fortes. Vêm do level up a cada 5 níveis e, raramente, do baú.
  A etapa 4 depois transforma os talentos antigos em Stats com combos; aqui entram os 6 primeiros.

Tudo entra pela mesma tela de 3 cartas que já existe (`Cards_Popup`, `CardScreenUI`), cada carta
com o fundo/rótulo/cor do seu tipo (Item azul, Upgrade dourado, Stat roxo).

## 2. Objetivos, não-objetivos, anti-objetivos

**Objetivos**
- Baú e vendedor aparecem em lugares aleatórios do mundo, com mínimo/máximo de aparições por run.
- Baú/mímico → 3 cartas (upgrades, às vezes um Stat). Vendedor → 3 cartas de item, grátis.
- 8 itens e 6 Stats jogáveis no Barbarian.
- Level up: níveis comuns misturam Upgrade + Item; a cada 5 níveis a oferta é de Stats.

**Não-objetivos (ficam para depois)**
- Gold, preço, compra/venda (não existe gold na run; gold permanente é a etapa 6).
- Trocar/descartar item com os 6 espaços cheios.
- Combos de Stats e conversão dos talentos antigos (etapa 4). Ranger/Mage (etapa 5).
- Ícones definitivos (o usuário escolhe do `Icons.png`; até lá a carta pode ficar sem ícone).

**Anti-objetivos (seria falha mesmo funcionando)**
- O jogo travar pausado (oferta que nunca fecha, tela sem cartas).
- O vendedor oferecer item que não cabe (7º item) ou oferta vazia que pausa à toa.
- Voltar qualquer coisa de gold/loot/Bag de materiais para dentro da run.
- Números fixos no código: tudo que é 🔢 fica em asset ajustável no Inspector.

## 3. Restrições do projeto

- Unity 6000.3.8f1, Input System novo, Enter Play Mode sem domain reload (estáticos vazam entre Plays:
  tudo que é estático precisa ser zerado no início da run).
- Regras puras em `Assets/Scripts/Core` com testes EditMode; MonoBehaviours verificados por eval no Editor.
- Texto de jogo em inglês (como as cartas); comentários/documentação em pt-BR.
- Não editar cena/prefab sem backup + diff (protocolo do projeto).

## 4. Abordagens consideradas

1. **Item como um tipo de carta (escolhida pelo usuário).** `CardDefinition` (kind = Item) ganha
   "vida por nível"; o bônus do item usa o mesmo cálculo de bônus das cartas (`CardBonuses`). Só os
   2 itens especiais têm um comportamento próprio pequeno. Baú, vendedor e level up mandam ofertas
   pela mesma fila (`HeroCards.EnqueueOffer`). Menos código, um só lugar pra editar, reaproveita
   reroll/fila/pausa.
2. **Sistema de itens separado** (asset, inventário e bônus próprios; a carta só exibe). Mais isolado
   pra crescer (troca/venda), mas duplica nível/bônus e é bem mais código.

## 5. O que foi decidido

### 5.1 Tipos de carta e de onde vêm

| Fonte | O que oferece |
|---|---|
| Level up comum | 3 cartas sorteadas entre Upgrades + Itens, **sorteio igual** (toda carta possível tem a mesma chance — decisão do usuário) |
| Level up múltiplo de 5 (5, 10, 15…) | 3 Stats |
| Baú / mímico | 3 cartas de Upgrade; cada uma com ~15% 🔢 de chance de ser um Stat |
| Vendedor | 3 Itens |

- Itens só podem ser oferecidos se cabem: com 6 itens, só os que o herói já tem (e não estão no
  nível máximo). Isso vale em toda fonte (vendedor e level up).
- Se uma fonte não tiver cartas suficientes, oferece as que tiver (1 ou 2); se não tiver nenhuma,
  não abre a tela (sem pausa à toa). Level up múltiplo de 5 sem Stat disponível → oferta comum.
- Reroll: 1 por oferta, em toda fonte, sorteando do mesmo conjunto da fonte (o reroll do vendedor
  sorteia itens; o do baú, upgrades/Stats).

### 5.2 Itens (8)

Todos: **+vida máxima por nível** e um efeito que sobe por nível; nível máximo 5 🔢.
A vida base do herói **fica como está hoje** (decisão do usuário); os itens somam por cima:
+10% 🔢 da vida máxima base do herói por nível de item.

| Item | Efeito por nível (🔢 proposta) |
|---|---|
| Swift Boots | +8% velocidade de movimento |
| Iron Plate | −5% dano recebido |
| Troll Heart | regenera 0,5% da vida máxima por segundo |
| Power Gauntlet | +10% dano geral (todas as habilidades) |
| Magnet Ring | +20% raio do ímã |
| Scholar Tome | +10% XP das gemas |
| **Thorn Mail** (especial) | devolve 30% do dano recebido a quem bateu (melee) |
| **Blast Charm** (especial) | chance do inimigo explodir ao morrer: 10% no nível 1, +5% por nível (30% no 5). Dano em área = 100% 🔢 do dano do ataque primário do herói (com os bônus de cartas/itens/Stats — decisão do usuário: "dano do herói") |

- 6 espaços. A barra de baixo (`Inventory_Background`, já com 6 espaços) passa a mostrar o ícone e
  o nível de cada item; não mostra mais materiais da Bag.
- A carta **Thick Hide** (vida) sai do catálogo de upgrades: vida vem dos itens e do Stat Vitality.

### 5.3 Stats (6)

Cada um sobe até o nível 3 🔢. Fortes (são raros):

| Stat | Efeito por nível (🔢 proposta) |
|---|---|
| Ancestral Strength | +25% dano geral |
| Iron Will | −10% dano recebido |
| Quick Mind | −15% recarga de tudo (Shift e energia da Ultimate) |
| Vitality | +25% vida máxima |
| Hunter's Eye | +20% área de tudo |
| Greed | +20% XP das gemas |

### 5.4 Baú e mímico

- Aparecem **4 a 5** 🔢 vezes por run, espalhados ao longo dos 25 min (sorteados no início da run).
- 20% 🔢 dos baús nascem como mímico (luta; ao morrer dá a mesma recompensa).
- Só 1 baú existe por vez; fica no mapa até ser aberto. Se a hora do próximo chegar com um ainda
  fechado, o próximo espera.
- Seta na borda da tela aponta o baú (já existe: `WorldTargetPointerUI`).
- Abrir → 3 cartas (5.1). Some depois de aberto. A recompensa de gold do baú sai.

### 5.5 Vendedor

- Aparece **6 a 8** 🔢 vezes por run, espalhado (~1 a cada 3 min).
- Nasce num lugar aleatório perto do herói (mesmo anel de spawn do baú), com a seta na borda.
- **Fica até ser usado.** O próximo só aparece depois. Usar = interagir → 3 cartas de item → some.
- Se não houver item possível (6 espaços cheios e todos no máximo), o vendedor não aparece mais.
- O vendedor passa a ser criado em runtime (hoje é um objeto fixo na cena, que fica desligado).

## 6. Decisões do Claude (o usuário pode corrigir)

- Bônus gerais (itens/Stats "de tudo") **multiplicam** com os de cada habilidade: dano final do
  Ground Crack = base × carta do primário × Power Gauntlet × Ancestral Strength.
- Redução de dano: Iron Plate + Iron Will + `DamageReductionPercent` atual se **somam**, com teto de
  75% 🔢 (nunca imortal).
- Regeneração do Troll Heart soma no `HpRegenPercentPerSecond` que já existe.
- Aparições: no início da run sorteia N (entre mín/máx) e divide os 25 min em N faixas iguais; cada
  aparição cai num segundo aleatório da sua faixa (sem nada nos primeiros 60 s 🔢).
- Código antigo de gold do baú e de venda de materiais do vendedor sai (não só desligado).

## 7. Deixado para o builder

- Como representar "dano geral", "área de tudo", "recarga de tudo" e "XP das gemas" no cálculo de
  bônus (ex.: habilidade "None" valendo para todas) — desde que cartas por habilidade continuem
  funcionando e se somem com os gerais.
- Onde ficam os números (🔢): nos assets de carta e no `RunSettings`/`ChestSpawnSettings`.
- Como o sorteio garante o mínimo/máximo de aparições e o espalhamento no tempo.
- Visual da explosão do Blast Charm e do dano do Thorn Mail (pode reaproveitar algo existente).
- Como a barra de itens mostra o nível (texto de quantidade que já existe no slot é aceitável).
- Limpeza do código antigo de gold do baú e da venda de materiais do vendedor.
