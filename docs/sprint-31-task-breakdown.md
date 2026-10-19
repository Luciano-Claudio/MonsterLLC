# Sprint 31 — 15 LootDefinitions

> Sprint de conteúdo/sistema de dados, não de código de gameplay novo. Entrega demonstrável
> oficial (plano de produção): "Todos os materiais existem — cada material tem rolagem
> independente de drop". Dependência: Sprint 30b, P11 (ambas já fechadas).

## Seção 0 — Decisões e suposições (revisar antes de implementar)

0. **Os 15 materiais e seus valores de venda já estão fechados no GDD** (`docs/gdd/index.md`
   Seção 39, espelhado em `docs/gdd/economy-balance.md` Seção 5) — não é uma decisão de design
   em aberto, é dado de referência pronto (ver tabela abaixo). Nenhuma invenção de nome/valor
   necessária nesta sprint.

1. **"Rolagem independente" já tem regra fechada** (GDD Seção 38): cada material possível de um
   monstro faz sua PRÓPRIA rolagem de chance, de forma independente das demais — um único abate
   pode dropar vários materiais diferentes ao mesmo tempo (não é "escolher 1 item da tabela").
   `docs/gdd/bestiary.md` já tem esse formato pronto por ficha, ex.: Rat comum = "Monster
   Essence: 5-20 | Chance: 100%". A entrega desta sprint é o SISTEMA que lê esse formato e rola
   cada entrada, não inventar a regra.

2. **[CORRIGIDO — suposição errada na 1ª versão deste doc]** Achei inicialmente que só Monster
   Essence tinha drop table pronto no Bestiário. Errado: `docs/gdd/bestiary.md` já tem drop
   tables REAIS (chance + intervalo de quantidade) pra Monster Fragment, Spirit Dust, Arcane
   Shard, Dark Crystal, Soul Fragment, Corrupted Core, Elemental Shard e Ancient Fragment —
   em TODO comum de Tier1 até Tier9, não só Monster Essence. Exemplo real (um comum de Tier1):
   `Monster Fragment: 1-5 | Chance: 70%`, `Spirit Dust: 1-3 | Chance: 50%`, `Arcane Shard: 1 |
   Chance: 2%`, `Dark Crystal: 1 | Chance: 0,5%` — tudo isso JÁ no mesmo arquivo, já pronto pra
   ler. Só os 3 materiais mais raros (Infernal Ash, Chaos Crystal, Nightmare Residue, Void
   Shard, Celestial Fragment, Divine Core — os de Tier 8+) ainda não têm ficha associada, porque
   os próprios monstros desses Tiers não existem ainda. **Isso muda a resposta da pergunta em
   aberto original**: não precisa inventar dados de teste nem decidir se cadastra "vazio" — o
   drop table real de 8 dos 15 materiais já existe pronto no Bestiário pros comuns já
   implementados, só falta o sistema ler esse formato de verdade.

3. **Achado real — `Vendor.cs` hoje só vende "Monster Essence"** (`if (bag.Slots[i].itemName !=
   "Monster Essence") continue;`, hardcoded). Qualquer outro material que passe a dropar nesta
   sprint ficaria preso no inventário pra sempre (destruído no fim do dia, GDD Seção 40, nunca
   vendido) — a menos que o Vendor também seja generalizado nesta sprint. Isso não está escrito
   no objetivo oficial da Sprint 31 (que fala só de "materiais existem" + "rolagem de drop"), mas
   sem isso a entrega fica incompleta na prática (dropar um material que não pode virar gold é
   inútil pro jogador). **Pergunta em aberto**: generalizar o Vendor nesta sprint, ou deixar como
   dívida explícita pra sprint seguinte (Sprint 32, que já mexe em agregação/Large Number)?

4. **`LootDefinition` hoje é só `{itemName, quantity}`, sem campo de chance** — a chance de drop
   não pertence ao material em si, pertence à combinação (monstro, material) — o mesmo material
   pode ter chance/quantidade diferente por ficha de monstro (ex.: Monster Essence do Rat
   comum = 5-20/100%, mas um boss provavelmente dropa mais). Confirma se o "drop table" fica
   anexado a `EnemyDefinitionSO`/`BossDefinitionSO` (1 lista de entradas `{material, min, max,
   chance}` por definição) ou num asset separado referenciado por elas.

5. **Fora de escopo desta sprint (confirmado pela tabela do plano de produção)**: física do drop
   (Rigidbody2D, hop ao morrer, atrito), stacking por proximidade e a sprite por limiar de
   quantidade (1/2+/5+/20+, regra revisada em 2026-10-18 — GDD Seção 38) são explicitamente a
   Sprint 32 ("Agregação Visual + Large Number" — ver `docs/sprint-32-task-breakdown.md`).
   `LootDrop` continua sendo 1 GameObject simples por material dropado nesta sprint, sem física
   nem stacking ainda.

## Dados de referência (já fechados no GDD, sem decisão pendente)

### Os 15 materiais + valor de venda (GDD Seção 39 / `economy-balance.md` Seção 5)

| # | Item | Valor de venda |
|---|---|---|
| 1 | Monster Essence | 1g |
| 2 | Monster Fragment | 3g |
| 3 | Spirit Dust | 4g |
| 4 | Arcane Shard | 8g |
| 5 | Dark Crystal | 10g |
| 6 | Soul Fragment | 40g |
| 7 | Corrupted Core | 70g |
| 8 | Elemental Shard | 300g |
| 9 | Ancient Fragment | 500g |
| 10 | Infernal Ash | 4kg (4.000g) |
| 11 | Chaos Crystal | 7kg |
| 12 | Nightmare Residue | 750kg |
| 13 | Void Shard | 5mg (5.000.000g) |
| 14 | Celestial Fragment | 10bg |
| 15 | Divine Core | 1tg |

### Regra de progressão (GDD Seção 38)

Desbloquear o material de um Tier superior NÃO remove os materiais de Tiers anteriores do pool
de drop — monstro de Tier alto continua podendo dropar Monster Essence normalmente, conforme a
própria ficha dele define.

### Exemplo de drop table já pronto (`docs/gdd/bestiary.md`)

Todo comum já tem, na própria ficha, uma lista de "Drops" com VÁRIAS entradas independentes —
não só Monster Essence. Exemplo real (um comum de Tier1):
```
Monster Essence: 5-20 | Chance: 100%
Monster Fragment: 1-5 | Chance: 70%
Spirit Dust: 1-3 | Chance: 50%
Arcane Shard: 1 | Chance: 2%
Dark Crystal: 1 | Chance: 0,5%
```
Esse é o formato exato (lista de `{material, min-max, chance}`) que a estrutura de dados desta
sprint precisa reproduzir — já coberto pra Monster Essence, Monster Fragment, Spirit Dust,
Arcane Shard, Dark Crystal, Soul Fragment, Corrupted Core, Elemental Shard e Ancient Fragment em
todo comum de Tier1-9.

## Estrutura de dados necessária (conceitual — sem código nesta rodada)

- **Um "drop table" por monstro** — lista de entradas, cada uma com: qual dos 15 materiais,
  quantidade mínima, quantidade máxima, chance de drop (%). Mora em `EnemyDefinitionSO`/
  `BossDefinitionSO` (ver Seção 0, item 4) ou asset próprio referenciado por elas.
- **Resolução no momento da morte** (`EnemyController.Die()`, hoje só sabe dropar "Monster
  Essence" fixo) — percorre a lista de entradas do monstro, rola cada uma independentemente
  (chance própria), e para cada rolagem bem-sucedida instancia 1 `LootDrop` com o material e uma
  quantidade aleatória dentro do intervalo. `HeroController.LootMultiplier` (passiva de sorte do
  Gunslinger) continua se aplicando à quantidade final, igual já faz hoje.
- **`LootDefinition`/`LootDrop` continuam servindo o papel de hoje** (nome + quantidade do item
  já resolvido) — não precisam mudar de forma, só passam a ser instanciados com o material
  certo em vez de sempre "Monster Essence".
- **Valor de venda por material** — precisa morar em algum lugar consultável pelo Vendor (asset
  de configuração com os 15 valores da tabela acima, ou um campo na própria definição do
  material). Hoje o preço vive solto em `Vendor.pricePerEssence` (1 valor só, específico de
  Monster Essence) — precisa generalizar pra 1 valor por material.

## Arquivos/classes envolvidos (existentes, vão precisar de mudança)

- `Assets/Scripts/Core/Loot/LootDefinition.cs` — provavelmente sem mudança de forma, só de uso.
- `Assets/Scripts/World/LootDrop.cs` — sem mudança de forma (continua 1 item por instância).
- `Assets/Scripts/Enemies/EnemyController.cs` — `Die()` precisa trocar o drop fixo de "Monster
  Essence" por iteração sobre o drop table do monstro.
- `Assets/Scripts/Core/WorldGeneration/EnemyDefinitionSO.cs`, `BossDefinitionSO.cs` — candidatos
  a receber o drop table novo (ver Seção 0, item 4).
- `Assets/Scripts/World/Vendor.cs` — hoje hardcoded só pra "Monster Essence" e preço único; ver
  Seção 0, item 3, se entra nesta sprint ou vira dívida explícita.
- `Assets/Scripts/Core/DemandTracker.cs` — já recebe `RegisterSale(itemName, quantity)`
  genérico por nome — conferir se continua só contando "Monster Essence" pra demanda (GDD Seção
  39: "Só Monster Essence conta" — isso é regra confirmada, não bug, não mexer).

## Eventos (GameEvents) — previstos

Nenhum evento novo antecipado — `GameEvents.BagChanged`/`GoldChanged` já existentes devem
continuar suficientes.

## Checklist de teste (planejado, pós-implementação)

1. Monstro com 2+ materiais no drop table, cada um com chance < 100% — rodar vários abates e
   confirmar que os materiais aparecem independentemente (às vezes só 1, às vezes os 2, às
   vezes nenhum, nunca travado em "sempre os 2 juntos" nem "sempre só 1").
2. Quantidade dropada sempre dentro do intervalo `[min, max]` configurado por entrada.
3. `HeroController.LootMultiplier` (Gunslinger) continua multiplicando a quantidade final,
   qualquer que seja o material.
4. Vendor (se entrar no escopo desta sprint) vende qualquer um dos 15 materiais pelo valor
   correto da tabela, não só Monster Essence.
5. Demanda diária continua contando só Monster Essence, mesmo com outros materiais no
   inventário/vendidos.
6. Material de Tier alto não impede materiais de Tier baixo de continuarem dropando do mesmo
   monstro (regra de progressão, GDD Seção 38).

## Dívida técnica antecipada

- Agregação visual de pilha grande fica pra Sprint 32, de propósito.
- Se o Vendor não entrar nesta sprint (Seção 0, item 3), materiais além de Monster Essence
  ficam "mortos" (dropam mas não podem ser vendidos) até a Sprint 32 — vale decidir antes de
  começar a implementação, não descobrir no meio.
- Preços de 10 dos 15 materiais (a partir de Infernal Ash) envolvem notação k/m/b/t — o valor
  mais extremo da tabela (Divine Core = 1tg = 1.000.000.000.000g) cabe folgado no `long` que
  `RunState.gold` já usa (teto de ~9,2 quintilhões), então nenhuma mudança de tipo é necessária
  só por causa desta sprint. Vale reavaliar só se o jogo somar MUITAS vendas em trilhões ao
  longo de uma run — não é um risco concreto agora, só uma suposição a não esquecer depois.

## Próximos passos

Depois de confirmadas as 5 decisões da Seção 0, esta sprint vira pronta pra escrever o código de
verdade (drop table + resolução no `Die()` + possivelmente Vendor generalizado). Sprint 32 em
seguida cuida da agregação visual.
