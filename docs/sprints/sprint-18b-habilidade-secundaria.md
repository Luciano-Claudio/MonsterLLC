# Sprint 18b — Habilidade Secundária (Shift) — Barbarian + Ranger

## Objetivo

Antes de abrir a Sprint 19 (Mage), dar a todo herói uma terceira habilidade (Shift), com cooldown próprio, e implementar de fato as duas primeiras (Barbarian e Ranger) — as outras 8 ficam registradas só no GDD, pra chegar já especificadas quando a sprint de cada herói abrir. Junto: bosses ficam imunes a knockback, e ESC passa a pausar o jogo (sem menu ainda, só o input).

## Sistemas adicionados

- Arquitetura genérica de Shift na base `HeroController`: cooldown próprio (`secondaryAbilityCooldownDuration`/`secondaryAbilityCooldown`), `protected abstract void UseSecondaryAbility()`, `protected virtual void CancelSecondaryAbility()` (no-op por padrão — herói sem cancelamento simplesmente não sobrescreve), dispatch de input único (Shift alterna entre usar e cancelar dependendo de `isUsingSecondaryAbility`).
- `HeroController.IsPlayerUntargetable` (flag estática) — mecanismo de "sumir do radar dos monstros", pensado pra ser reaproveitado depois pela transformação de coruja do Druid e pela ultimate de stealth do Assassin.
- Barbarian: buff temporário de dano (2×) e velocidade (1.5×) por 5s, 1 animação sem direção, não bloqueia nada, não cancelável.
- Ranger: camuflagem em 3 fases (`shift_start`/`during`/`end`, cada uma 1 estado sem direção), bloqueia tudo, cancelável (Shift de novo avança pra `end` antes do teto), cura 10% da Vida Máxima por segundo com teto de 5s (50% de cura total se ficar o tempo inteiro camuflado), monstros perdem o alvo de verdade (não congelam).
- `EnemyController.isBoss` — bosses agora ignoram `ApplyKnockback()` completamente.
- Input novo (`SecondaryAbility` = Shift, `Pause` = ESC) e `TimeManager.Instance.TogglePause()` ligado ao ESC em `SystemsBootstrap`.

## Decisões técnicas

- **Bloqueio "depende da habilidade", não uma regra única**: Ranger bloqueia tudo (como `Trapped`), Druid (GDD only) bloqueia só o ataque (movimento continua, forma de coruja), a maioria dos outros não bloqueia nada (buffs/efeitos instantâneos). Implementado reaproveitando `isAttacking` quando a habilidade deve bloquear, e nunca setando quando não deve — sem flag nova pra "bloqueia parcialmente", cada herói decide caso a caso no próprio `UseSecondaryAbility()`.
- **Habilidades canceláveis têm "animação de saída"**, reaproveitando a fase final natural da própria habilidade (`shift_end` do Ranger) em vez de um clipe dedicado só pra cancelamento.
- **Perder o alvo ≠ congelar** (bug real encontrado em teste, corrigido nesta sprint): a primeira versão de `IsPlayerUntargetable` fazia um `return` antecipado no `Update()` do `EnemyController`, travando o monstro preso na animação que estava tocando no instante da camuflagem. Corrigido forçando `isInCombat = false` sem cortar o resto do `Update()` (deixa animação/patrulha seguirem normalmente) e travando a redetecção por proximidade em `UpdatePatrol()` com `&& !HeroController.IsPlayerUntargetable` — sem isso, a flag seria desfeita no frame seguinte, já que o jogador continua fisicamente dentro do raio de observação, só "invisível".
- **Cura da camuflagem é percentual, não valor fixo**: 10% da Vida Máxima por tick (1 tick/s), teto de 5s de duração — o cap de 50% emerge do próprio tick rate × duração, sem precisar de uma variável de teto separada.
- **Teto de tempo + cancelamento não são mutuamente exclusivos**: a fase `during` da camuflagem tem um teto real (`secondaryAbilityMaxDuration`, 5s) mas pode terminar antes via Shift de novo — os dois caminhos convergem no mesmo método (`EndCamouflage()`), pra não duplicar a lógica de encerramento.
- **Barbarian: Exit Time = 1 em todas as transições de saída do Shift** (Walk/Idle/Damage), mesmo a habilidade não bloqueando nada mecanicamente — decisão explícita do usuário pra a animação sempre completar visualmente antes de sair, mesmo sendo tecnicamente instantânea.
- Passivas só mudaram onde o usuário citou explicitamente (Barbarian perdeu o glow visual da passiva, mantendo a mecânica de escala de dano) — todo o resto dos 10 heróis manteve a passiva como já estava.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/HeroController.cs` — arquitetura genérica do Shift, `IsPlayerUntargetable`, input de Shift/ESC, reset no `OnDeath()`.
- `Assets/Scripts/Player/Heroes/Barbarian.cs` — remoção do glow da passiva, buff de dano/velocidade via Shift.
- `Assets/Scripts/Player/Heroes/Ranger.cs` — camuflagem completa (3 fases, cura percentual, teto+cancelamento, `EndCamouflage()` compartilhado).
- `Assets/Scripts/Enemies/EnemyController.cs` — `isBoss` (imunidade a knockback), tratamento de `IsPlayerUntargetable` no `Update()`/`UpdatePatrol()` sem congelar animação.
- `Assets/PlayerControls.inputactions`/`PlayerControls.cs` — ações `SecondaryAbility` (Shift) e `Pause` (ESC).
- `Assets/Scripts/SystemsBootstrap.cs` — `Pause` ligado a `TimeManager.Instance.TogglePause()`.
- `Assets/Animation/Heros/Barbarian/Barbarian.controller` — estado `Shift` (Blend Tree de 4 diagonais), Exit Time = 1 em todas as saídas.
- `Assets/Animation/Heros/Ranger/Ranger.controller` — estados `shift_start`/`shift_during`/`shift_end`, triggers de início/fim.

## Testes executados

Checklist manual em Play Mode (confirmado pelo usuário): Barbarian — Shift aplica buff de dano e velocidade por 5s, não bloqueia ataque/ultimate/movimento, reverte sozinho no fim. Ranger — Shift esconde o herói (3 fases), monstros perdem o alvo e voltam a patrulhar/ficar idle normalmente (inclusive monstros de emboscada voltando ao estado dormente), cura por tick durante a fase `during`, teto de 5s encerra sozinho, Shift de novo cancela antes do teto e adianta pra `end`. Bosses (`isBoss = true`) não sofrem knockback. ESC pausa o jogo. Sem testes automatizados novos.

## Bugs conhecidos

Nenhum pendente — o bug de monstros congelando durante a camuflagem foi encontrado e corrigido dentro desta mesma sprint.

## Dívida técnica

- Valores 🔢 de balanceamento (multiplicadores/durações/cooldowns do Shift de Barbarian e Ranger) são chutes de primeiro teste, ajustáveis em playtest.
- As 8 habilidades secundárias restantes (Mage, Druid, Rogue, Cleric, Paladin, Gunslinger, Assassin, Blood Mage) existem só no GDD — sem código, sem Animator — até a sprint de cada herói abrir.
- Menu de pausa clássico (sair/configurações) não existe ainda — só o input do ESC chamando `TogglePause()` foi ligado.

## Próximos passos

Barbarian e Ranger saem desta sprint com o kit completo (primário + ultimate + secundária). Próxima peça da Deadline: Sprint 19 (Mage — Primário + Pet Phoenix).
