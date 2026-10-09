# Sprint 27 — Paladin Completo (Primário + Ultimate + Shift + Passiva)

## Objetivo

Paladin Gameplay Complete: martelo arremessado (primário), espadas orbitando escaláveis por upgrade (ultimate), shield bash em 4 direções fixas (Shift) e shield periódico com vida própria (passiva).

## Sistemas adicionados

- **Martelo (primário)** — `PaladinHammer.cs`: projétil reto em mira livre (sem snap de 8 direções), rotação contínua do sprite. Ao acertar ou alcançar o alcance máximo, causa dano em área na explosão (sempre metade do dano recebido no lançamento, nunca o que "sobrou" de reserva). Upgrade futuro de contagem: `hammerCount` (1 a 5) abre o lançamento em leque — pares completos ficam simétricos (±15°, ±30°...), a contagem par (2, 4) resolve o martelo "solteiro" pra cima ou pra baixo conforme o quadrante da mira atual. Cada martelo recebe dano **cheio**, sem dividir reserva entre eles (mesmo critério dos 8 projéteis da Ultimate do Barbarian).
- **Espadas orbitando (Ultimate)** — primeiro "Orbiting Hitbox" real do projeto. `PaladinOrbitingBlades.cs` (controlador, no filho "Blades") + `PaladinOrbitingBlade.cs` (1 por espada, até 8 slots fixos já posicionados/rotacionados ao redor do círculo no prefab, nunca instanciados). A órbita é só a rotação do próprio Transform do "Blades" — como as 8 espadas são filhas dele, giram juntas automaticamente, sem recalcular posição por trigonometria a cada frame. Gira desde o Start até o fim do End de cada espada, não só durante o Cycle. `bladeCount` (upgrade futuro: 2, 4 ou 8, nunca ímpar) decide quantas acordam numa ativação.
- **Shield bash (Shift)** — 4 triggers cardeais fixos disparando juntos (não escolhidos pela mira, diferente do golpe do Barbarian); imune a dano durante a animação inteira.
- **Shield periódico (passiva)** — vida própria absorvendo dano antes do Paladin, recarrega a cada `shieldRecoverInterval` depois de quebrar. **Correção pós-implementação:** o Paladin não nasce com o shield ativo — espera o 1º intervalo completo (30s) antes de receber a passiva pela primeira vez, igual a qualquer recarga normal.
- **2 GameObjects novos do passivo** — `ShieldBase` (atrás do sprite do Paladin) e `ShieldDome` (na frente), cada um com Animator próprio (`DomeStart`/`DomeCycle`/`DomeEnd`/`None`) — `None` é o estado default dos dois, e `DomeEnd` transiciona pra ele automaticamente (Exit Time, sem Animation Event).
- **`HeroAnimationGeneratorWindow`** (novo, `Tools/Hero Animation Generator`) — ferramenta dedicada pra animação de herói (walk/die/dmg/idle/attack diagonal+ortogonal/shift diagonal+ortogonal/ultimate diagonal+ortogonal, todos opcionais), com diferenciação de Idle (`idle_start` sozinho, ou `idle_start`+`idle_end` concatenados numa animação só).
- **Animação de `Trapped` retroativa** — Cleric, Druid, Mage e Rogue (heróis antigos que não tinham) ganharam o BlendTree + estado + 3 transições de `Trapped`, mesma receita já usada em Barbarian/Ranger. Druid's Owl (forma do Shift) ficou de fora de propósito — já é imune/inalcançável enquanto ativo.

## Decisões técnicas

- **Sem arquivo real de referência pro martelo/Ranger** — modelado em cima do `ClericProjectile.cs` (Sprint 23), sem homing (vai sempre reto até esgotar a reserva ou o alcance).
- **Ultimate não bloqueia nada e não é cancelável** — Paladin continua andando/atacando normalmente com as espadas girando (como um pet); duração inteira controlada dentro do próprio `PaladinOrbitingBlades`, sem override de `CanUseUltimate()`/`IsUltimateActive()`/`CancelUltimate()`.
- **Animation Event no meio do clipe de Ultimate, não no clique do botão** — `AnimationUltimateBladesStartEvent()` separado de `UseUltimate()`, sincroniza o surgimento das espadas com o frame certo da pose do corpo (erguer o martelo).
- **`PaladinBladeHitbox` (classe-ponte original) foi eliminada** — a 1ª versão previa uma 2ª classe no mesmo arquivo (`PaladinOrbitingBlades.cs`) só pra repassar `OnTriggerEnter2D`, cujo fileID não dá pra escrever à mão com segurança fora da Unity. Resolvido criando `PaladinOrbitingBlade.cs` como classe própria (1 arquivo = 1 classe, fileID padrão) que já cuida da própria animação E do próprio hit — sem precisar de ponte nenhuma.
- **FloatingCombatText de dano bloqueado** — `GameEvents.OnDamageBlocked` (separado de `OnDamageTaken`), cor azul bebê configurável (`shieldBlockedTextColor`), pra mostrar que o hit aconteceu mesmo sem afetar a vida real do Paladin.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/Paladin.cs`, `PaladinHammer.cs`, `PaladinOrbitingBlades.cs`, `PaladinOrbitingBlade.cs` (novos).
- `Assets/Prefabs/Heros/Paladin/Paladin.prefab`, `PaladinHammer.prefab` (novos).
- `Assets/Animation/Heros/Paladin/Paladin.controller` + clipes de ataque/ultimate/shift (novos); `Assets/Animation/Heros/Paladin/Hammer/`, `Blades/`, `Shield/` (controllers + clipes dedicados).
- `Assets/Editor/HeroAnimationTools/HeroAnimationGeneratorWindow.cs` (novo).
- `trapped_*.anim` + wiring de Animator Controller em Cleric/Druid/Mage/Rogue.
- `Assets/Scripts/Core/GameEvents.cs`, `FloatingCombatText.cs`, `FloatingCombatTextSpawner.cs` — `OnDamageBlocked` novo.

## Eventos adicionados

- `GameEvents.OnDamageBlocked(Vector3, float, Color)` — dano absorvido pelo shield do Paladin (ou qualquer futuro mecanismo similar), separado do `OnDamageTaken` normal.

## Testes executados

Validação manual em Play Mode pelo usuário, iterativa ao longo da sprint (sprites sendo adicionadas nos clipes conforme cada prefab ficava pronto) — **confirmado funcionando** nas mensagens de fechamento: martelo, espadas, shield bash e shield periódico operando como esperado, incluindo a correção de timing do spawn inicial do shield.

## Bugs conhecidos

Nenhum pendente do escopo original do Paladin (ver Sprint 27b pra bugs encontrados nos heróis antigos durante o trabalho de infraestrutura que saiu desta sprint).

## Dívida técnica

- Collider físico (`CapsuleCollider2D`) e hitboxes do shield bash usam valores placeholder ajustados manualmente pelo usuário em Play Mode, não calibrados em teste de balance de verdade.
- `docs/gdd/balance-values.md` não foi atualizado com os campos `🔢` novos do Paladin (nem com os de nenhum herói desde o Barbarian, Sprint 17) — doc já estava desatualizado antes desta sprint; ver nota na Sprint 27b.

## Próximos passos

Paladin Gameplay Complete — Deadline 7 segue para a Sprint 28 (Gunslinger), agora com dependência em **Sprint 27b** (ver relatório), não mais direto na 27.
