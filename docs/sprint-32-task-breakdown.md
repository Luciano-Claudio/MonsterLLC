# Sprint 32 — Agregação Visual + Large Number

> Sprint de sistema/visual, não de conteúdo novo (os 15 materiais já existem desde a Sprint 31).
> Entrega demonstrável oficial (plano de produção): "Drops grandes legíveis e performáticos —
> pilha de 10.000 unidades aparece agregada e é coletada corretamente em partes". Dependência:
> Sprint 31.

## Seção 0 — Decisões e suposições (revisar antes de implementar)

0. **Regra de agregação visual REVISADA em 2026-10-18** (GDD Seção 38) — substitui a regra antiga
   do GDD (1–9 individual / 10–49 stacks de 10 / 50–99 stacks de 50 / 100+ quantidade interna,
   que era sobre QUANTOS objetos de pilha apareciam na tela). A regra nova é sobre qual SPRITE
   cada pilha usa: **4 variações de arte já desenhadas pelo usuário — 1 (sozinho) / 2+ / 5+ /
   20+.** Os 3 limiares (2/5/20) são variáveis serializadas, ajustáveis sem recompilar — não
   fixos em código.

1. **Física do drop** (GDD Seção 38, nova) — todo loot nasce com `Rigidbody2D` de verdade, SEM
   "Freeze Rotation Z" (o item pode girar livremente ao cair). No instante da morte do monstro, é
   arremessado pra cima com pouca força (hop curto, não um lançamento forte) — direção/força
   exatas ficam a definir (🔢), mas a intenção é um "pulinho" perceptível, não um voo longo.
   Precisa de atrito suficiente (Linear Drag alto, ou um Physics Material 2D com friction alto)
   pra não deslizar muito longe do ponto de morte antes de assentar.

2. **Stacking por proximidade via trigger — resolvido 2026-10-20 ✅** — cada item de loot tem um
   `Collider2D` (trigger) que detecta outros itens do MESMO material próximos. Ao detectar, eles
   se juntam numa única pilha (visual + lógica — 1 entidade representando a soma, não N objetos
   sobrepostos). A sprite da pilha resultante é escolhida pelos limiares do item 0, pela
   quantidade TOTAL acumulada. **Confirmado: um dos dois GameObjects é destruído e o outro
   absorve a quantidade** (implementado via desempate determinístico por `GetInstanceID()`, pra
   nenhum dos 2 lados tentar destruir o outro ao mesmo tempo).

3. **Drop que já nasce acima do limiar** — se um abate solta, de uma vez, uma quantidade que já
   ultrapassa um limiar (ex.: 25 unidades de um material cujo limiar "20+" é 20), a pilha nasce
   DIRETO com a sprite certa — não precisa esperar o trigger de proximidade ajuntar unidade por
   unidade. Isso implica que o sistema de spawn de loot (Sprint 31) já entrega a quantidade pra
   uma ÚNICA entidade de pilha (não N GameObjects individuais que precisariam se juntar depois)
   quando o resultado da rolagem já é grande — mais barato e evita um "flash" visual de várias
   sprites pequenas se juntando instantaneamente.

4. **Entrega oficial fala em "pilha de 10.000 unidades" — resolvido 2026-10-20 ✅** — isso é ORDEM
   DE GRANDEZA bem acima dos 4 limiares de sprite (1/2+/5+/20+ — o maior limiar documentado hoje
   é 20). **Confirmado (a): a sprite "20+" representa "quantidade grande" de forma genérica, sem
   mais variações de arte conforme a pilha cresce além de 20** — 10.000 unidades usa a MESMA
   sprite de 25 unidades, só muda o número exibido/interno (`LootVisualSettings.GetSprite` já
   não tem teto superior pro limiar `tier20Threshold`). Sem lógica de múltiplas pilhas visuais.

5. **Coleta parcial continua valendo** (GDD Seção 37) — uma pilha maior que o espaço disponível
   na Bag entrega o máximo que couber, o resto fica no chão como entidade separada (nova pilha,
   recomeça a própria física/trigger normalmente).

## Dados de referência (já fechados)

### Limiares de sprite (GDD Seção 38, revisado 2026-10-18)

| Quantidade na pilha | Sprite |
|---|---|
| 1 | "sozinho" |
| 2 até (limiarMedio - 1) | "2+" |
| limiarMedio até (limiarGrande - 1) | "5+" |
| limiarGrande ou mais | "20+" |

Valores de exemplo (2/5/20) são os padrões sugeridos pelo usuário — ficam expostos como campo
serializado, ajustáveis por item ou globalmente (🔢 decisão de escopo: 1 conjunto de limiares
pra TODOS os materiais, ou configurável por material individualmente? presumo 1 conjunto global
por simplicidade, já que as 4 artes foram desenhadas de forma genérica — confirmar).

### Pickup Radius e coleta (GDD Seção 37, já existente)

A lógica de coleta por proximidade do jogador já existe e não muda — ao entrar no Pickup Radius,
a quantidade INTERNA real da pilha é o que conta pra Bag/venda, independente de qual sprite
estava sendo exibida no chão.

## Estrutura necessária (conceitual — sem código nesta rodada)

- **Componente de loot com física** — substitui o `LootDrop` atual (hoje sem física nenhuma, só
  checa distância até o player em `Update()`). Precisa de `Rigidbody2D` (gravidade 2D ligada,
  sem Freeze Rotation Z), um impulso inicial pra cima no spawn, e atrito/drag alto.
- **Trigger de stacking** — `Collider2D` isTrigger. **Decisão tomada na implementação (diferente
  da suposição conceitual acima):** só 1 collider (trigger), não 2 — loot não precisa de colisão
  física sólida contra nada do mundo (não existe "parede" que o item precise respeitar), então o
  mesmo `CircleCollider2D` trigger cobre física (o Rigidbody2D funciona sem collider sólido) e
  detecção de stacking ao mesmo tempo.
- **Resolução de sprite por quantidade** — função pura (quantidade → qual dos 4 sprites usar),
  reavaliada toda vez que a quantidade da pilha muda (fusão por proximidade, spawn inicial
  grande, ou coleta parcial reduzindo o que sobra no chão).
- **Campos serializados pros limiares** — `stackTierMedium`/`stackTierLarge` (nomes ilustrativos)
  em algum asset de configuração de loot, não hardcoded em código.

## Arquivos/classes envolvidos (existentes, vão precisar de mudança)

- `Assets/Scripts/World/LootDrop.cs` — reescrita significativa: ganha física, trigger de
  stacking, resolução de sprite por quantidade. Hoje é um script simples sem Rigidbody.
- Prefab de loot (hoje genérico, 1 só reaproveitado por todo material) — precisa de
  `Rigidbody2D` + 2 `Collider2D` (físico + trigger) + `SpriteRenderer` configurado nos Settings
  do Sprite Editor.

## Eventos (GameEvents) — previstos

Nenhum evento novo antecipado.

## Checklist de teste (planejado, pós-implementação)

1. Item cai do monstro morto, salta um pouco, gira livremente durante a queda, assenta sem
   deslizar muito longe do ponto de morte.
2. 2 itens do MESMO material, próximos, se juntam numa pilha só — sprite muda pro limiar certo.
3. Itens de materiais DIFERENTES, próximos, NÃO se juntam — continuam separados.
4. Drop que já nasce grande (ex.: 25 unidades de um material com limiar 20) já aparece
   diretamente com a sprite "20+", sem salto visual de sprite pequena → grande.
5. Reduzir uma pilha (coleta parcial) reavalia a sprite pra baixo se cruzar um limiar — ex.:
   pilha "20+" perde unidades até ficar com 4, sprite vira "2+".
6. Pilha de 10.000 unidades (ou o equivalente que a Seção 0, item 4, definir) continua
   performática — sem lag ao gerar/coletar.

## Dívida técnica antecipada

- Valores exatos de força do "hop"/atrito/gravidade são 🔢, pendentes de ajuste visual em
  playtest.
- Pergunta em aberto da Seção 0 (itens 2, 3, 4) precisa de resposta antes de fechar a
  implementação — risco de retrabalho se a leitura de "pilha de 10.000" (item 4) virar código
  antes de decidir.

## Próximos passos

Depois desta sprint, o loot está visualmente pronto pra qualquer Tier futuro (3-10, Sprints
33-40) sem precisar de mais trabalho de sistema — só conteúdo (fichas de monstro com os drop
tables, já em grande parte prontos no Bestiário).
