# Sprint 27b — Infraestrutura de Upgrades (todos os heróis) + correções de base no Mage

## Objetivo

Depois de fechar o Paladin (Sprint 27), o usuário levantou um problema de arquitetura real antes de planejar upgrades/cards de verdade: vários heróis tinham valor de balanceamento duplicado entre o controlador e o prefab filho/projétil que ele instancia — um upgrade futuro precisaria editar 2+ lugares pro mesmo efeito. Corrigir isso exigiu mexer em **todos os 7 heróis existentes**, não só no Paladin, então virou sprint própria em vez de um adendo na 27 (mesmo precedente da 18b/19b). Aproveitando a janela, também entrou a primeira peça de infraestrutura de upgrade de verdade (parâmetro de Animator pra velocidade de ação) e uma leva de pedidos específicos do Mage/Barbarian testados em Play Mode.

## Sistemas adicionados

- **Centralização de valores de upgrade no controlador** — todo filho/projétil instanciado por um herói (`HeroProjectile`, `ClericProjectile`, `Vine`, `MageFireball`, `MageTeleportProjectile`, `RogueBomb`, `RangerArrow`, `RangerKnife`) teve os próprios campos `[SerializeField]` de balanceamento removidos; os valores agora vivem no script do herói (controlador) e são repassados por parâmetro em `Launch()`/`Activate()` a cada uso. Os prefabs filhos ficaram só com wiring (Animator/Collider) e timing puro de animação (ex.: `impactDuration`, `groundedStateName`). Regra geral documentada em comentário no topo de cada script filho, pra não regredir no futuro.
- **`ActionSpeedMultiplier`** (`HeroController.cs`, novo parâmetro de Animator, default 1) — acelera a *playback* de Attack/Ultimate (todo herói) e de Shift que causa dano/cura (Barbarian, Cleric, Paladin — não Rogue/Mage/Ranger/Druid, cujo Shift é sobrevivência/mobilidade). Ligado como Speed Parameter só nos estados certos em cada Animator Controller; `RefreshActionSpeedMultiplier()` reaplica sempre que um herói troca o `runtimeAnimatorController` inteiro em runtime (só o Druid faz isso, ao virar Alce). Animators FILHOS com Animator próprio (MageAttackHitbox, Phoenix) recebem o mesmo valor via `protected float ActionSpeedMultiplier` exposto na base, repassado a cada ativação (não uma vez no `Awake()`, pra não depender de ordem entre os 2 `Awake()`).
- **Upgrade de tamanho (Transform) ligado em 3 lugares** — Barbarian (`sizeMultiplier`, até 3x, escala o corpo inteiro E os 8 projéteis da Ultimate), Mage (`attackHitboxSizeMultiplier` no `MageAttackHitbox` do primário, `ultimateSizeMultiplier` na bola de fogo da Ultimate — explosão e área de fogo no chão escalam junto, não só o visual), Mage pet (`petSizeMultiplier`/`petActionSpeedMultiplier`, sem teto definido ainda).
- **FloatingCombatText de cura** — `GameEvents.OnHealReceived`, verde claro com "+" na frente; `HeroController.Heal(amount)` centraliza clamp em `maxHealth` + evento de HP + o texto, usado por `Cleric.ApplyHealWave()` e a regeneração do Ranger (camuflagem). `Druid.AnimationElkTransformInEndEvent()` (cura pro HP máximo do Alce) calcula o `healedAmount` real antes de trocar `maxHealth`, não usa o `Heal()` genérico porque o teto muda no mesmo instante da cura.
- **`shieldBashDamageMultiplier`** (Paladin) — dano do shield bash estava fixo em 1x `stats.damage` direto no código, sem multiplicador exposto; agora ajustável no Inspector como todo outro dano de habilidade.

## Decisões técnicas

- **Divisão explosionRadius/groundedRadius × escala do GameObject (Mage)** — como `explosionRadius`/`groundedRadius` já chegam **finais** (multiplicados por `ultimateSizeMultiplier` no `Mage.cs`), mas `CircleCollider2D.radius` é um valor LOCAL que a Unity multiplica de novo pela escala do próprio GameObject, a atribuição ao collider precisa dividir pela escala (`groundedRadius / transform.localScale.x`) pra não ficar "ao quadrado" (upgrade 2x virando 4x de raio de verdade). Mesmo critério nos dois raios da bola de fogo.
- **Bug da explosão do Mage ("as vezes pega, as vezes não") — causa raiz e fix.** O dano da explosão usava `Physics2D.OverlapCircle(point, explosionRadius, ...)` com um raio calculado à parte do `CircleCollider2D` real do prefab — risco de dessincronizar do que o collider/visual representava. Troca: o collider é redimensionado pra `explosionRadius` no exato momento da explosão (`Explode()`), e o dano usa `circleCollider.Overlap(...)` (o próprio collider), mesmo padrão já confiável do golpe do Barbarian/shield bash do Paladin/fire do Mage. Mesmo fix aplicado no "pega de graça quem já estava parado em cima" da fase Grounded.
- **Pet do Mage deslizando durante o Summon** — `PetController.MoveTowards()` não tinha nenhum gate de estado de Animator (todo o resto do projeto trava movimento real atrás de `AnimatorStateCheck.IsInState`); o pet se movia desde o frame 1, inclusive durante o clipe de invocação. Corrigido: movimento real só libera quando o Animator já está em `"Fly"` — mesmo critério do `HeroController` pro `Walk`. `DirX`/`DirY` continuam atualizando mesmo parado, pra não dar "pop" de pose quando o Fly começar de verdade.
- **Pré-multiplicação explícita, não escala implícita** — na 1ª rodada do fix do Mage, a compensação de escala tinha sido escondida dentro do `MageFireball.cs` (`* transform.localScale.x` espalhado em cada OverlapCircle); revisado a pedido do usuário pra manter o princípio "controlador decide tudo": o `Mage.cs` agora calcula os raios finais e passa prontos em `Launch()`, o `MageFireball.cs` só compensa a escala no único lugar onde é estritamente necessário (a atribuição ao `CircleCollider2D.radius`, ver acima).

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/HeroController.cs` — `actionSpeedMultiplier`, `RefreshActionSpeedMultiplier()`, `ActionSpeedMultiplier` (getter), `Heal()`, `GetFloatingTextSpawnPosition()` virou `protected` (era `private`).
- `Assets/Scripts/Player/Heroes/{Barbarian,Cleric,Druid,Mage,Rogue,Ranger,Paladin}.cs` — campos de upgrade centralizados, chamadas de `Launch()`/`Activate()` expandidas.
- `Assets/Scripts/Player/{HeroProjectile,ClericProjectile,MageFireball,MageTeleportProjectile,MageAttackHitbox,RangerArrow,RangerKnife}.cs`, `Assets/Scripts/Player/Heroes/{Vine,RogueBomb}.cs` — campos de balanceamento removidos, recebidos por parâmetro.
- `Assets/Scripts/Enemies/PetController.cs` — `Initialize()` ganhou `sizeMultiplier`/`actionSpeedMultiplier`; gate de movimento por estado `Fly`.
- `Assets/Scripts/Core/{GameEvents,FloatingCombatText,FloatingCombatTextSpawner}.cs` — `OnHealReceived`, `isHeal`/cor em `FloatingCombatText.Setup()`.
- 8 Animator Controllers editados (`Barbarian`, `Cleric`, `Druid` + `Druid_Elk`, `Mage` + `AttackHitbox` + `Phoenix`, `Rogue`, `Ranger`, `Paladin`) — parâmetro `ActionSpeedMultiplier` adicionado e ligado como Speed Parameter nos estados certos.
- ~14 prefabs atualizados (valores de upgrade movidos dos filhos pros heróis, preservando os valores já calibrados manualmente pelo usuário).

## Eventos adicionados

- `GameEvents.OnHealReceived(Vector3, float)` — Floating Combat Text de cura (verde, "+"), paralelo ao `OnDamageTaken`/`OnDamageBlocked`.

## Testes executados

Validação manual em Play Mode pelo usuário ao longo de toda a sprint, item por item — **confirmado funcionando** na mensagem de fechamento ("tudo alterado já foi testado e funciona perfeitamente"), incluindo o fix da explosão do Mage e o fix do pet deslizando no Summon.

## Bugs conhecidos

Nenhum pendente.

## Dívida técnica

- **`docs/gdd/balance-values.md` não foi atualizado** — o índice de campos `🔢` já estava parado desde a Sprint 17 (só cobre Barbarian + `HeroController` + monstro genérico); esta sprint moveu/criou dezenas de campos novos em todos os 7 heróis, piorando o atraso. Fica registrado aqui como dívida explícita, não decisão de ignorar — recomendo uma sprint dedicada só a essa atualização antes do Balance Pass (Deadline 13), já citado como a janela certa no próprio documento.
- `petSizeMultiplier`/`petActionSpeedMultiplier` (Mage) não têm teto definido — todo outro multiplicador de tamanho desta sprint tem um máximo explícito (Barbarian 3x, Mage ataque/ultimate 3x); falta o usuário decidir um número quando a carta de upgrade do pet for desenhada.
- Mecânicas de upgrade/card em si **não existem ainda** — esta sprint só constrói a infraestrutura (campos + wiring de Animator/escala) pra quando o Card Framework (Sprint 35, já citado em comentários de código) chegar; nenhum valor muda em runtime hoje fora do que o Inspector já define estaticamente.
- Rogue segue sem nenhuma mecânica de upgrade pensada (ver Próximos Passos) — decisão explícita do usuário de deixar em aberto.

## Próximos passos

- Rogue precisa de uma sessão de brainstorm dedicada pra mecânicas de upgrade "roubadas"/ladras (GDD/identidade do herói) antes de repetir o padrão desta sprint nele.
- Quando o Card Framework (upgrades/cards de verdade) começar, toda a infraestrutura desta sprint (`ActionSpeedMultiplier`, multiplicadores de tamanho, `bladeCount`/`hammerCount`) já está pronta pra receber valor em runtime — não precisa de refactor, só de quem escreve nesses campos.
