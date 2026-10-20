# Sprint 31 — 15 LootDefinitions

## Objetivo

Todos os 15 materiais econômicos da GDD (Seção 39) passam a existir de verdade no drop table de cada monstro/boss, com rolagem de chance independente por material — e o Vendor passa a vender qualquer um deles, não só Monster Essence.

## Sistemas adicionados

- **Drop table por prefab (`lootDrops`)** — `EnemyController` ganhou `public List<LootDropEntry> lootDrops`, substituindo o antigo `monsterEssenceDropAmount` (1 valor fixo, só pra Monster Essence). Cada `LootDropEntry` é `itemName` + `dropChancePercent` + `minQuantity`/`maxQuantity`.
- **`DropLoot()`** — chamado em `AnimationDieEndEvent()` quando `dropsLoot` é true. Testa CADA entrada de `lootDrops` com sua própria rolagem de chance, independente das demais (GDD Seção 38: "cada tipo de loot realiza sua própria rolagem") — um abate pode dropar vários materiais ao mesmo tempo, ou nenhum, se alguma rolagem com chance < 100% falhar. Quantidade final passa por `HeroController.LootMultiplier` (passiva do Gunslinger) antes de instanciar o `LootDrop`.
- **`LootPricingSettings`** (ScriptableObject, `Assets/Settings/LootPricingSettings.asset`) — os 15 valores de venda reais da GDD Seção 39 (Monster Essence 1g até Divine Core 1 trilhão). `Vendor.cs` foi generalizado: antes vendia só "Monster Essence" com um `pricePerEssence` fixo; agora itera a Bag inteira e consulta `LootPricingSettings.TryGetPrice()` pra qualquer item.
- **21 prefabs populados com os valores reais do Bestiário** — todos os 14 comuns e 7 bosses existentes (Tier1-2) tiveram `lootDrops` preenchido com as chances/quantidades exatas já documentadas em `docs/gdd/bestiary.md` (nenhum valor novo/placeholder inventado).

## Decisões técnicas

- **`lootDrops` vive direto no `EnemyController`, não num `EnemyDefinitionSO`/asset separado** — seguindo o mesmo padrão já estabelecido pelo campo antigo `monsterEssenceDropAmount` (também um campo per-prefab simples, sem indireção via SO). `WorldPopulationManager`/`WorldBossSpawnManager` só fazem `Instantiate(definition.prefab)`, sem guardar referência de volta pro SO que spawnou — manter o loot no mesmo lugar que já funcionava evitou introduzir uma arquitetura nova sem necessidade.
- **`long` para `LootPriceEntry.sellValue`** — Divine Core sozinho já vale 1 trilhão (10¹²), e Celestial Fragment 10 bilhões; `int` (~2,1 bi de teto) não cobre isso. `Vendor.goldEarned` também passou de `int` pra `long` pelo mesmo motivo (soma múltiplos materiais de uma vez).
- **Erro real encontrado na 1ª versão do planejamento (`sprint-31-task-breakdown.md`)** — o doc de planejamento original assumia que só Monster Essence tinha drop table pronta; na prática o `bestiary.md` já tinha tabelas completas (chance + quantidade) para 8 dos 15 materiais em todos os monstros/bosses do jogo. Corrigido no próprio doc antes da implementação.

## Arquivos/classes principais

- `Assets/Scripts/Core/Loot/LootDropEntry.cs` (novo) — 1 linha de drop table (`itemName`/`dropChancePercent`/`minQuantity`/`maxQuantity`).
- `Assets/Scripts/Core/Loot/LootPricingSettings.cs` (novo) — `LootPriceEntry` + `TryGetPrice()`.
- `Assets/Scripts/Enemies/EnemyController.cs` — `lootDrops`/`dropsLoot` novos (substituem `monsterEssenceDropAmount`); `DropLoot()` novo.
- `Assets/Scripts/World/Vendor.cs` — generalizado pra vender qualquer item via `LootPricingSettings`, não só Monster Essence.
- 14 prefabs em `Assets/Prefabs/Monstros/Floor1/` e `Floor2/` + 7 em `Assets/Prefabs/Bosses/Floor1/` e `Floor2/` — `lootDrops` populado com os valores reais do Bestiário.
- `Assets/Scenes/New Scene.unity` — `Vendor` ligado ao novo `LootPricingSettings.asset`.

## Eventos adicionados

Nenhum em `GameEvents` — o fluxo de loot usa `GameEvents.BagChanged` (já existente).

## Testes executados

Validação manual em Play Mode: matar monstros comuns e bosses de Tier1/Tier2 e confirmar no Console os logs de coleta (`[LootDrop] Coletado: Nx <item>`) batendo com as faixas documentadas no Bestiário; interação com o Vendor vendendo múltiplos materiais de uma vez e somando gold corretamente (`[Vendor] Vendeu itens por N gold`).

## Bugs conhecidos

Nenhum no sistema de loot em si. Durante o teste manual desta sprint, 2 bugs **não relacionados** foram encontrados e corrigidos de passagem (não fazem parte do escopo de loot):
- `MageFireball.UpdateGrounded()` — `InvalidOperationException` por modificar o `HashSet<EnemyController> enemiesInRange` durante a própria enumeração (`TakeDamage` podia disparar `OnTriggerExit2D` no mesmo frame). Corrigido iterando uma cópia.
- `BiomeTerrainEntry.nestedPatches` — aviso "Serialization depth limit 10 exceeded" no Console (tipo recursivo `List<BiomeTerrainEntry>` dentro de si mesmo). Corrigido com `[SerializeReference]`; os 4 `BiomeProfileSO` com dados aninhados reais (Tier1, Tier2-3, Tier4-5, Tier6-8) foram reconvertidos pro novo formato e os valores confirmados idênticos aos anteriores.

## Dívida técnica

- O drop físico no chão (`LootDrop`) ainda não tem sprite nem física — ele existe só como lógica (trigger de coleta por `pickupRadius`), sem nada visível caindo do monstro. Isso é escopo da Sprint 32.
- Nenhum valor de `dropChancePercent`/quantidade foi balanceado além do que já existia no Bestiário — são os números de design já aprovados, não houve ajuste nesta sprint.

## Próximos passos

Sprint 32 (Agregação Visual + Large Number) entra com a física real do drop (Rigidbody2D, hop ao morrer), os 4 sprites de pilha por quantidade (1/2+/5+/20+, já com os assets e o `LootVisualSettings` prontos — ver abaixo) e o stacking por proximidade/trigger.

**Adiantamento consumido parcialmente nesta janela:** a UI completa do inventário (HUD de 10 slots + TAB de 40 slots + drag/swap), originalmente escopo da Sprint 42, foi implementada agora porque o usuário já tinha montado a hierarquia visual na cena — ver `docs/sprints/adiantamentos.md` (entrada 2026-10-19/20) para o detalhamento completo, incluindo a correção da progressão de slots (10→20→30→40, substituindo os 5→10→15→20 documentados antes) e o `LootVisualSettings` (ícones de pilha dos 15 materiais, reaproveitável pela Sprint 32 pro drop físico).
