# Sprint 25 — Bosses Floor 2 (Padrão): Werewolf, Centaur King

## Objetivo

Tornar derrotáveis os 2 bosses de arquitetura genérica do Floor 2 (Werewolf, Centaur King) e ligar o `BossSpawnManager` do Floor 2.

## Sistemas adicionados

Nenhum sistema novo — os 2 bosses reaproveitam 100% o `MeleeEnemyController` já existente (golpe real via Animation Event, 4 hitboxes diagonais, cooldown próprio), igual a qualquer Melee comum do Bestiário. Sem mecânica bespoke, sem script novo.

## Decisões técnicas

- Prefabs montados à mão (YAML), espelhando `Goblin_King.prefab` (Transform/SpriteRenderer/Animator/Rigidbody2D/CapsuleCollider2D/`MeleeEnemyController`/`StatusEffectController`/`Seeker`/`AIPath`/`RVOController` + 4 `AttackHitbox_NE/NW/SE/SW` + filho `Status`) — sem mudança de arquitetura, só de stats/sprites/Animator Controller próprios.
- `BossSpawnManager` do Floor 2 criado como componente irmão de `FloorDefinition`/`FloorPopulationManager` do Floor 2 (fileID `40895283`), `bossPrefabs = [Centaur_King, Werewolf]`.

## Arquivos/classes principais

- `Assets/Prefabs/Bosses/Floor2/Werewolf.prefab`, `Centaur_King.prefab` (novos).
- Animator Controllers via `MonsterAnimationGeneratorWindow` (override controller sobre `Base_Melee`).
- `Assets/Scenes/_TestScene.unity` — `BossSpawnManager` novo no Floor 2.

## Testes executados

Validação manual em Play Mode — **confirmado pelo usuário** ("Funcionou perfeitamente!"): golpe real conecta, hitboxes ajustadas visualmente pelo usuário no Editor após a criação dos prefabs, `statusAnimator` sobrevive ao ajuste manual.

## Bugs conhecidos

Nenhum.

## Dívida técnica

Valores 🔢 de balanceamento (dano/vida/stats) são placeholder de teste.

## Próximos passos

Com Werewolf e Centaur King fechados, falta só a Sprint 26 (os 2 bosses de arquitetura própria) pra fechar Floor 1–2 100%.
