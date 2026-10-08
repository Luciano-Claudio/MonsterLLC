# Sprint 26 — Bosses Floor 2 (Exceções): Rat People Royalty, Spider Queen

## Objetivo

Tornar derrotáveis os 2 bosses de arquitetura própria do Floor 2 (Rat People Royalty, Spider Queen) — fecha Floor 1–2 com os 7/7 bosses planejados derrotáveis.

## Sistemas adicionados

- **Híbrido Melee/Ranged com 2 raios de ataque independentes** — cada boss tem `stats.attackRadius` (melee) e um `rangedAttackRadius` novo (habilidade à distância), igual todo Ranged comum já tem o próprio raio. Dentro do melee: ataque corpo a corpo real (hitbox + Animation Event). Entre o melee e o `rangedAttackRadius`: a habilidade à distância. Além do `rangedAttackRadius`: nenhuma das duas alcança, só persegue — nenhum dos dois bosses foge, diferente de um Ranged comum.
  - **Rat People Royalty** — melee (`attack_*`, 4 diagonais) substitui o antigo dano de contato passivo; arremesso (`Throw_Rat_*`, 4 cardeais N/S/E/W) nasce um Rat People vivo na posição de impacto, some loot (`EnemyController.dropsLoot`, campo novo).
  - **Spider Queen** — bite (`bite_*`, 4 diagonais) substitui o contato passivo; teia (`attack_*`, 8 direções) aplica o Efeito `Ice` (incapacitação via `HeroController.SetTrapped()`, sem dano).
- **`CustomAnimationGeneratorWindow`** (novo, `Tools/Custom Animation Generator`) — ferramenta de animação genérica pra bosses com mecânica própria: os mesmos 6 slots padrão do gerador antigo (idle/walk/idleCombat/damage/attack diagonal/attack orthogonal), todos opcionais sem seletor Melee/Ranged, mais uma lista livre de clipes customizados (nome + sheet + layout). Opcionalmente injeta os clipes direto num Animator Controller existente (sem transições — isso fica pra depois, manual).
- **`MonsterAnimationUtility`** (novo) — 9 métodos/enum extraídos de `MonsterAnimationGeneratorWindow` pra serem compartilhados pela ferramenta nova, comportamento idêntico ao que já existia.
- `EnemyController.dropsLoot` (novo, default `true`) — summons (ex.: Rat People nascido do arremesso) não geram loot.
- `EnemyController.IsDead` (novo, protected getter) — exposto pra controllers que precisam de `Update()` próprio correndo em paralelo ao ciclo de combate da base.

## Decisões técnicas

- **Correção de design em 2 rodadas, não planejado assim desde o início** — a 1ª versão (aprovada inicialmente) tinha a teia/arremesso disparando sempre que o player estivesse fora do melee, sem limite de distância (teia) ou totalmente independente da distância (arremesso, decisão original do usuário). O usuário corrigiu depois de testar: os 2 precisam de um raio próprio, igual um Ranged comum — sem isso, o arremesso disparava mesmo com o player do outro lado do Floor. `rangedAttackRadius` foi adicionado nos 2 controllers substituindo a regra antiga.
- **Troca de rótulo descoberta em teste:** os clipes `Throw_Rat_ne/nw/se/sw` do Rat People Royalty continham sprites cardeais (ne=S, nw=N, se=E, sw=W) — engano ao alimentar a sheet na ferramenta nova. Renomeados pra `throw_rat_n/s/e/w` (guid preservado, nada quebrou) e a Blend Tree do estado `Throw` trocou de `DiagonalAimX/Y` (4 diagonais) pra `AimX/AimY` cru (cardeal), mesma convenção que o Ranged comum já usa pro Attack de 8 direções.
- **Pipeline genérico de Attack nunca é usado por nenhum dos 2** (`hasAttackAnimation = false`, `InAttackRange() => false`, `ExecuteAttackHit() {}` puros no-ops) — melee e habilidade à distância são tratados inteiramente em `Move()`, mesmo padrão arquitetural do Goblin Sapper (Sprint 24).
- **`EnemyContactDamage` removido dos 2** — é reservado só pra Slime/Goblin Sapper (comentário da própria classe); agora que os 2 têm ataque corpo a corpo animado de verdade, o componente nem deve ser adicionado aos prefabs (ficaria sem uso, mas inofensivo se esquecido — `cooldown == null` até ser inicializado).
- **Guids de asset gerados à mão** pros 2 Animator Controllers novos, os 2 prefabs de boss, os 2 prefabs de projétil e os clipes/controllers dos projéteis — todos criados fora da Unity (igual a todo asset hand-written desta sprint), sem depender de reimport pra fechar as referências entre eles.

## Arquivos/classes principais

- `Assets/Scripts/Enemies/RatPeopleRoyaltyController.cs`, `SpiderQueenController.cs` (novos) — melee + habilidade à distância, 2 cooldowns/raios independentes cada.
- `Assets/Scripts/Enemies/RatPeopleThrowProjectile.cs`, `SpiderWebProjectile.cs` (novos) — projéteis retos, rotação real; o arremesso tem fase de Impact (`AnimationImpactEndEvent`), a teia some na hora.
- `Assets/Editor/MonsterAnimationTools/CustomAnimationGeneratorWindow.cs`, `MonsterAnimationUtility.cs` (novos).
- `Assets/Animation/Bosses/Floor2/Rat_People_Royalty/Rat_People_Royalty.controller`, `Spider_Queen/Spider_Queen.controller` (novos, hand-written) — Idle/Walk/IdleCombat/Damage/Die padrão + 2 estados próprios cada (Attack+Throw / Bite+Web).
- `Assets/Animation/Bosses/Floor2/*/Projectile/` — `Idle.anim`/`Impact.anim` (vazios, sprites adicionados pelo usuário) + mini-controllers próprios (`RatPeopleThrow.controller`, `SpiderWeb.controller`).
- `Assets/Prefabs/Bosses/Floor2/Rat_People_Royalty.prefab`, `Spider_Queen.prefab`, `Projectile/RatPeopleThrowProjectile.prefab`, `Projectile/SpiderWebProjectile.prefab` (novos).
- `Assets/Scenes/_TestScene.unity` — `BossSpawnManager` do Floor 2 atualizado pra `[Spider_Queen, Rat_People_Royalty]`.

## Testes executados

Validação manual em Play Mode — **confirmado pelo usuário** ("ta funcionando 100%"): melee conecta dentro do raio certo, habilidade à distância só dispara dentro do `rangedAttackRadius` (não mais sempre), arremesso nasce um Rat People vivo na posição de impacto, teia aplica o Efeito Ice. Checklist de 14 itens do Boss Timer (Sprint 24) revalidado com os 2 bosses novos na rotação — **confirmado pelo usuário**.

## Bugs conhecidos

Nenhum pendente.

## Dívida técnica

- **Bestiário desatualizado até esta sprint** — as fichas de Rat People Royalty e Spider Queen em `docs/gdd/bestiary.md` descreviam a versão antiga (dano de contato passivo + habilidade sem raio próprio); atualizadas junto com este relatório.
- Hitboxes de melee (`AttackHitbox_NE/NW/SE/SW`) e `CapsuleCollider2D` do corpo nos 2 prefabs ainda usam os valores placeholder copiados do Centaur King — precisam de ajuste visual (mesmo processo já feito pro Werewolf/Centaur King na Sprint 25).
- `webProjectilePrefab`/`throwProjectilePrefab` e o `SpriteRenderer` dos 2 prefabs de projétil têm sprite via a animação `Idle`/`Impact` (adicionada pelo usuário) — o campo estático do `SpriteRenderer` continua vazio, intencionalmente (a animação já sobrescreve no primeiro frame).
- Arte do ícone do Efeito `Ice` (aplicado pela teia) ainda não existe — usuário vai clonar/ajustar de outro Efeito depois; o código já referencia o `StatusEffectType` certo, não precisa de mudança quando a arte chegar.
- Valores 🔢 de balanceamento (`biteCooldownDuration`, `meleeCooldownDuration`, `webCooldownDuration`, `throwCooldownDuration`, `rangedAttackRadius` = 6 nos 2) são placeholder de teste.

## Próximos passos

**Floor 1–2 100% fechado — 7/7 bosses derrotáveis** (Deadline 6 completa do lado de bosses). Próxima peça da Deadline 6 é o que faltar de Heróis II; depois disso, Deadline 7 (Sprint 27, Paladin).
