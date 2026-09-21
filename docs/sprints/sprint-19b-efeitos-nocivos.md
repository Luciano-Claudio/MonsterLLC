# Sprint 19b — Sistema de Efeitos Nocivos (Fire + Bleeding) + correções de base em Mira/Shift

## Objetivo

Depois de fechar o Mage (Sprint 19), construir o sistema de Efeitos Nocivos de verdade (até então só existia como conceito no GDD) — Fire e Bleeding primeiro, dos dois lados (herói↔monstro) — e corrigir uma leva de bugs de base na mira/Habilidade Secundária encontrados testando o Mage, que afetavam todo herói, não só ele.

## Sistemas adicionados

- **`StatusEffectController` + `IDamageable`** (`Assets/Scripts/Core/`) — componente genérico de dano-ao-longo-do-tempo, compartilhado entre `HeroController` e `EnemyController` (os 2 únicos `IDamageable` do jogo), em vez de duplicar a lógica que já existia só no `EnemyController`. Cada entidade afetável tem um filho `Status` com Animator próprio (`Animator.Play()` direto pelo nome do efeito, sem Blend Tree/transições).
- **Enum `StatusEffectType`** com os 12 efeitos já desenhados (`Fire`, `Bleeding`, `Fear`, `Heal`, `Ice`, `Nature`, `Petrification`, `Poison`, `Shock`, `Sickness`, `Sleep`, `Stun`) — só nomeação pronta; mecânica de dano-ao-longo-do-tempo hoje só serve Fire/Bleeding (confirmados pro MVP junto de Ice/Fear/Heal, que ainda não têm mecânica própria).
- **Imunidade por tipo** — `StatusEffectController.immunities` (lista configurável por monstro no Inspector). Bloqueia o Efeito em si; quem aplicou o dano decide se o dano de ÁREA/superfície também é bloqueado pro alvo imune (ex.: `MageFireball` consulta a imunidade antes de aplicar o tick do rastro no chão, não só antes de aplicar o Efeito).
- **Fire** (Mage) e **Bleeding** (Ranger) com mecânica real: Fire no rastro da Ultimate do Mage (já existia desde a Sprint 19, migrado pro componente novo); Bleeding novo na Ultimate do Ranger — tanto o hit em voo quanto o tick no chão aplicam metade do dano do Ranger por segundo, 5s.
- **Regra de farm de energia da Ultimate esclarecida** — o lockout de energia pós-ultimate (`ultimateEnergyLockoutDuration`, já existia desde antes) cobre só o impacto inicial (explosão/projétil); farmar energia com a área persistente que a própria Ultimate deixa no chão depois é intencional, não precisa de proteção.
- **Correções de base na mira (afetam todo herói)**:
  - Mira agora é lida por **polling** todo frame (`Update()`), não só em evento de movimento do mouse — sem isso, mover o mouse durante uma ação longa (teleporte) e parar antes dela acabar deixava a mira presa até o próximo movimento.
  - `TryUseUltimate()`/`TryUseSecondaryAbility()` releem a mira à força antes de congelar — o Input System processa o clique antes do `Update()` do frame, então sem isso a mira congelava com o valor de 1 frame atrás.
  - `isAttacking` agora trava `TryUseSecondaryAbility()` contra sobreposição de ação — sem isso, apertar Shift em cima do fim de outra ação (Attack, Ultimate) ainda iniciava a Habilidade Secundária, capturando a mira congelada da ação anterior (causa raiz do "micro-teleport" do Mage).
  - **`DiagonalAimX`/`DiagonalAimY`** (`DirectionUtility.SnapTo4Diagonals`) — novo par de parâmetros pra Blend Tree que só tem pose desenhada pras 4 diagonais (sem pose cardeal real). `AimX`/`AimY` pode resolver num cardeal puro; num Blend Tree só-diagonal isso cai numa zona onde as 2 diagonais vizinhas ficam equidistantes, e o cálculo de peso do Unity vira instável ali — parecia um "flip" aleatório perto dos eixos N/E/S/W. Corrigido e religado no Barbarian (todos os 7 estados), Ranger (5 dos 6 — `Attack` fica em `AimX`/`AimY` porque tem pose real de 8 direções) e Mage (Idle/Walk/Damage/SummonPet). **Virou regra de arquitetura, documentada no GDD.**

## Decisões técnicas

- **Efeito Nocivo agora é bidirecional** — até a Sprint 19 o GDD dizia "monstro aplica no herói", sem contemplar o inverso; o Fire do Mage é a primeira exceção real, então a seção do GDD foi reescrita pra refletir isso.
- **Prioridade visual por categoria (Prisão > DoT) fica pra depois** — o `StatusEffectController` hoje só mostra o primeiro Efeito aplicado (ordem de chegada, não prioridade), registrado como dívida técnica no GDD porque Fire/Bleeding são os 2 da mesma categoria (DoT) e não têm conflito ainda — só vai importar quando algum Efeito de Prisão (Ice/Sleep/Stun) ganhar mecânica de verdade.
- **Lockout de energia continua curto de propósito** — cheguei a testar 35s (cobrindo a duração inteira da área persistente) antes do usuário esclarecer que só o impacto inicial devia ser bloqueado; a área persistente pode (e deve) continuar dando energia normalmente.

## Arquivos/classes principais

- `Assets/Scripts/Core/IDamageable.cs`, `StatusEffectController.cs` — sistema novo.
- `Assets/Scripts/Core/DirectionUtility.cs` — `SnapTo4Diagonals()`.
- `Assets/Scripts/Player/Heroes/HeroController.cs` — `DiagonalAimDirection`, polling de mira no `Update()`, releitura forçada em `TryUseUltimate()`/`TryUseSecondaryAbility()`, trava de `isAttacking` na Habilidade Secundária, `statusEffectController` + `Tick()`.
- `Assets/Scripts/Enemies/EnemyController.cs` — implementa `IDamageable`, `statusEffectController` + `Tick()` (substituindo o sistema antigo, só-Fire, que vivia aqui).
- `Assets/Scripts/Player/MageFireball.cs`, `RangerKnife.cs`, `Ranger.cs` — aplicação de Fire/Bleeding + consulta de imunidade.
- `Assets/Animation/Heros/{Barbarian,Ranger,Mage}/*.controller` — religação de `AimX`/`AimY` pra `DiagonalAimX`/`DiagonalAimY` nos Blend Trees só-diagonal.

## Testes executados

Checklist manual em Play Mode (confirmado pelo usuário, em várias rodadas): teleporte do Mage sem mais "micro-teleport" nem pose divergente entre `teleport_start`/`teleport_end`; Idle/Walk/Attack do Mage, Barbarian e Ranger sem mais espelhamento perto dos eixos cardeais; Fire aplicado pelo rastro da Ultimate do Mage; Bleeding aplicado pela Ultimate do Ranger (voo e chão); farm de energia da Ultimate bloqueado só no impacto inicial, liberado na área persistente. Sem testes automatizados novos.

## Dívida técnica

- Prioridade visual por categoria de Efeito Nocivo (Prisão > DoT) não implementada — só o primeiro aplicado é mostrado.
- Só Fire/Bleeding têm mecânica real; Fear/Heal/Ice/Nature/Petrification/Poison/Shock/Sickness/Sleep/Stun existem só como nome no enum.
- Ranger's `Attack` continua em `AimX`/`AimY` (correto, tem pose real de 8 direções) — qualquer estado futuro precisa checar se tem pose cardeal real antes de decidir qual par de parâmetros usar (regra documentada no GDD).

## Próximos passos

Base de Efeitos Nocivos pronta pro resto do roster (Poison do Blood Mage, Freeze de monstros do Andar 3, etc.) sem precisar de arquitetura nova. Próxima peça da Deadline: Sprint 20 (Floor 1A–2A + Bestiary Batch 1+2 completo).
