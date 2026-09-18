# Sprint 17 — Ranger (Primário) → Detalhamento dos 10 Heróis + Barbarian Completo

✅ **FECHADA.** Começou como a sprint mais simples do plano (só o leque de flechas do Ranger) e virou dois dias de trabalho: Dia 1 pivotou pro detalhamento dos 10 heróis do MVP no GDD e pro Barbarian completo (primeiro herói 100% animado do jogo, com o Ranger ficando de placeholder até "a vez dele chegar de verdade" — combinado explicitamente com o usuário). Dia 2 fechou o escopo original de verdade: sistema de Floating Combat Text, correção de um bug real de dano duplicado (afetava Barbarian e todos os monstros, não só o Ranger), e o Ranger primário completo (formação em cunha/V, mira livre, perfuração). Ver "Dia 2" mais abaixo pra essa segunda metade.

## Como o dia começou vs. como ele terminou

Começou como a sprint mais simples do plano: **Ranger — Primário**, só o leque de flechas (Deadline 5, dependia só da Sprint 16 já fechada). Terminou como o dia em que os **10 heróis do MVP foram detalhados por completo no GDD** e o **Barbarian ficou 100% implementado com animação real** — o primeiro herói do jogo a ter isso. O Ranger em si ficou só no esqueleto que já tinha (`FanSpread`/`RangerArrow`/`Ranger.cs` básico), sem retrabalho ainda — combinado explicitamente com o usuário: fica de placeholder até chegar a vez real dele.

---

## Fase 1 — Ranger (escopo original da sprint)

Implementado antes do pivô: `Assets/Scripts/Core/Combat/FanSpread.cs` (lógica pura do leque, com `FanSpreadTests.cs`), `Assets/Scripts/Player/RangerArrow.cs` (projétil simples, ainda sem a mecânica de perfuração que foi definida depois) e `Assets/Scripts/Player/Heroes/Ranger.cs`. Duas correções feitas em cima do task breakdown recebido de outra sessão de IA: sem `namespace Core.Combat` (o projeto inteiro é namespace global, só organiza por pasta) e o `Ranger.Update()` original tinha um bug de pausa (cooldown seguia contando com o jogo pausado) — corrigido antes até de virar redundante na Fase 9.

**Ficou pendente:** `RangerArrow` não usa a mecânica de perfuração/reserva de dano definida na Fase 2, e a contagem de flechas (`arrowCount`) ainda é um campo solto incrementado por `ContextMenu`, não ligada a Tier de Arma. Fica pra quando a Sprint der a volta pro Ranger de verdade.

---

## Fase 2 — O pivô: os 10 heróis detalhados de uma vez

O usuário trouxe uma descrição completa e detalhada de como cada um dos 10 heróis do MVP deveria funcionar — muito além do que o GDD Seção 17 já tinha. Isso virou uma reescrita grande das Seções 13, 16 e 17 do GDD (`docs/gdd/index.md`). Pontos centrais:

- **Regra nova de projétil de herói (Seção 13): perfuração por reserva de dano.** Diferente do projétil de monstro (dano cheio, destrói no primeiro contato), todo projétil de herói carrega uma reserva de dano — aplica o mínimo entre a reserva e a vida do alvo, sobra continua a mesma trajetória até esgotar ou bater na distância máxima. Variante do Ranger: sprite do "cluster" de flechas desce conforme a reserva esvazia.
- **8 direções fixas com sprite própria** pra projétil de herói e de monstro — precisa de Animator próprio no projétil (resolvido depois, Fase 6, sem Blend Tree: 8 estados soltos, `Animator.Play()` direto pelo nome).
- **Regras comuns novas (Seção 16):** herói não tem `MoveX`/`MoveY` como o monstro — só `AimX`/`AimY`, sempre a mira do mouse, nunca a direção de movimento (mesmo andando pra um lado, olha/ataca pra onde o mouse aponta). Animações padrão: `walk`/`idle`/`damage`/`die`/`attack`/`ultimate`. **Movimento só permitido durante o estado `walk`** — regra que gerou dois bugs sérios nas Fases 10 e 11. Knockback como conceito novo. Prioridade entre efeitos visuais simultâneos registrada como 🟡 pendente (ainda sem hierarquia definida).
- **Fichas dos 10 heróis reescritas por completo** (Seção 17.1–17.10): golpe de espada + 4 hitboxes fixas + rachadura no chão do Barbarian; flechas do Ranger presas a Tier de Arma (não mais carta) com 7 variações de sprite; ataque em 8 triggers do Mage; vinhas do Druid também por Tier de Arma; Rogue perdeu o "Orbiting Hitbox" e virou "Self Area Pulse" (categoria nova na Seção 13); Cleric só ataca se houver monstro no raio; dome do Paladin com 2 camadas (frente/atrás); Gunslinger com rajada de tiros por Tier de Arma e imprecisão crescente; Assassin no stealth cria uma dependência nova pro sistema de emboscada do Bestiário (Sprint 27, ainda não implementado — só anotado); Blood Mage com lifesteal via orb.
- **3 pendências (❓) resolvidas via pergunta direta ao usuário:** Druid vira **Alce** (não urso, correção de nomenclatura); ultimate do Mage viaja no ângulo livre do mouse (única exceção às 8 direções fixas); lifesteal do Blood Mage é **1 cura só**, entregue com atraso pela orb (não cura dupla).
- Corrigidas 2 referências antigas no GDD que ficaram incoerentes com a mudança (Ranger não tem mais "carta de flecha" — texto da Seção 31 sobre "cartas com teto" e a lista de exceções da Seção 13/17 atualizadas).

**Decisão de sequência:** ajustar todo o GDD primeiro, começar a programar depois pelo **Barbarian** (não o Ranger, que é o dono real desta sprint) — combinado explicitamente com o usuário, com o Ranger revisitado quando a vez dele chegar de verdade.

---

## Fase 3 — Infra nova compartilhada em `HeroController.cs`

Nenhum herói tinha Animator wireado até hoje (nem o Barbarian, desde a Sprint 7). Adicionado à base, valendo pra todo herói futuro:
- `Animator` + `AimX`/`AimY` (sempre a mira, nunca movimento) + `IsMoving`.
- Movimento só acontece via `AnimatorStateCheck.IsInState(animator, "Walk")` — sem Animator ainda (Ranger hoje), libera sempre.
- `isAttacking` — herói comprometido com uma ação (ataque ou ultimate) não tem a mira atualizada e recebe dano só como flash (mesma regra "dano não interrompe ação" do Bestiário, Seção 22/12, que já existia pros monstros desde a Sprint 16 e foi só espelhada aqui).
- `DamageTrigger`/`DieTrigger` automáticos em `TakeDamage()`/`OnDeath()`.
- **Morte adiada pro fim do clipe** (`AnimationDieEndEvent`, mesmo padrão do `EnemyController`): `OnDeath()` só faz o que é dado/estado (bag, energia, penalidade de -30s); o respawn de verdade (transição + teleporte) só acontece quando o Animation Event do fim do `die` chama `AnimationDieEndEvent()`, com timeout de segurança (`maxDieDuration`) caso o evento não esteja configurado.
- Corrigido de quebra: o clique de ataque/ultimate é callback do Input System, não passa pelo `Update()` onde `isDead` já bloqueava tudo — um herói morto ainda conseguia atacar durante o próprio `die`. Adicionado `!isDead` nos dois callbacks.

**`EnemyController.cs`** ganhou suporte a **knockback** (`ApplyKnockback`/`UpdateKnockback`) — não existia nada parecido pro lado dos monstros até hoje, e o Barbarian precisava disso pro golpe e pra ultimate.

**`HeroProjectile.cs`** (novo) — o projétil reto genérico de herói: reserva de dano com perfuração (Fase 2), knockback, 8 sprites via `DirectionUtility.GetDirectionName()` (helper novo, sem Blend Tree — a direção não muda depois do disparo, só `Animator.Play()` uma vez no lançamento).

---

## Fase 4 — Barbarian completo

`Barbarian.cs` reescrito do zero: golpe com 4 hitboxes fixas (mesmo padrão do `MeleeEnemyController.GetHitboxForFacing`, Sprint 16) + knockback (rachadura no chão descartada — o usuário decidiu juntar isso na própria animação de ataque, sem precisar de prefab separado); ultimate com salto/queda soltando 8 `HeroProjectile` a `stats.damage × multiplicador × passiva`; passiva de dano por vida perdida (**+25% de dano a cada 10% de Vida Máxima perdida, teto de +200% aos 80%+ perdidos** — matemática confirmada com o usuário via pergunta direta, a primeira redação do GDD tinha 2 números que se contradiziam) com um GameObject de brilho ligando/desligando sozinho.

---

## Fase 5 — Animator do Barbarian: construção, revisão e 3 rodadas de bugs

Especificação completa passada em tabela (padrão já usado desde a Sprint 16 pro Bestiário): parâmetros, 6 estados (`Idle`/`Walk`/`Attack`/`Ultimate`/`Damage` como Blend Tree 2D por `AimX`/`AimY`, `Die` como clipe único terminal), transições. Duas correções em cima da minha primeira proposta, ambas a pedido do usuário:
1. `Ultimate` e `Damage` também são Blend Tree direcional (eu tinha assumido errado que seriam clipe único).
2. `Idle/Walk → Damage` instantâneo, mas **`Attack/Ultimate → Damage` precisa de Exit Time** — sem isso, um dano recebido durante a ultimate interromperia a animação antes do `AnimationUltimateLandEvent` disparar, perdendo os 8 projéteis. (Na prática o código já impedia o Trigger de disparar nesse caso via `isAttacking`, mas a proteção redundante no grafo ficou, por segurança.)

Depois de eu revisar o `Barbarian.controller` já construído linha a linha (é YAML puro, dá pra auditar sem abrir a Unity), achei e corrigi:
- Loop ligado (`m_LoopTime: 1`) nos 12 clipes que deveriam tocar 1x só (`attack_*`, `ult_*`, `dmg_*`) — só `Idle`/`Walk` deveriam ter loop.
- `Barbarian_Ultimate_Projectile.prefab`: `CircleCollider2D` sem `Is Trigger` (corrigido pelo usuário).
- As 4 hitboxes de ataque também sem `Is Trigger` — sólidas e sempre ativas, colidindo fisicamente com monstros o tempo todo, não só durante o golpe (corrigido pelo usuário).
- Guids/thresholds dos 20 clipes direcionais (5 estados × 4 diagonais) conferidos 1 a 1 — todos corretos.

**Prefab do projétil da ultimate:** esclarecido que não precisa de Blend Tree — 8 estados soltos (`N`/`NE`/`E`/`SE`/`S`/`SW`/`W`/`NW`), sem parâmetro nenhum, `Animator.Play()` direto por nome (código já faz isso). Confirmado construído certo.

---

## Fase 6 — `FloorPopulationManager` (facilitador de teste)

A pedido do usuário, pra testar o Barbarian contra vários tipos de monstro: `meleePrefab`/`rangedPrefab` (2 campos fixos) viraram `monsterPrefabs` (array, mesmo padrão do `spawnPoints` que já existia ali). `SpawnOne()` sorteia 1 prefab qualquer da lista. **Atenção:** os 2 `FloorPopulationManager` da `_TestScene` (Floor_1 e Floor_2) perderam a referência antiga ao renomear o campo — precisam ser preenchidos de novo manualmente.

Aproveitado pra explicar `PopulationConfig` (minimum/target/maximum) — `minimum` existe mas **não é usado em lugar nenhum do código hoje**, mesma pendência que o próprio GDD Seção 23 já registra (🟡 curva de reposição).

---

## Fase 7 — Bug real: Barbarian virou metralhadora

Descoberto em teste: sem cooldown nenhum no ataque primário, segurar o clique matava a paridade de combate — nenhum Melee sobrevivia tempo suficiente pra chegar perto, principalmente com knockback aplicado a cada hit. Causa: `attackSpeed` só era usado pelo `Ranger` (cooldown próprio dele); o Barbarian não tinha nada, só o `isAttacking` que solta assim que a animação (curta) termina.

**Correção — cooldown universal na base, pra todo herói:** `HeroController` ganhou seu próprio `AttackCooldown` (`1 / attackSpeed`) e trocou o clique único por "segurar o botão continua atacando sozinho" (`attackHeld`, bool guardado em `performed`/`canceled`, mesmo padrão do `moveInput`), consumido a cada frame respeitando o cooldown. `Ranger.cs` simplificado — o cooldown/Update() dele viraram redundantes e foram removidos.

---

## Fase 8 — Regra nova: aceleração de animação sob dano repetido (em vez de silêncio)

O usuário achou a regra antiga do Bestiário ("dano durante o ataque = só um flash, sem mais nada") ruim visualmente — "parece que o ataque não pegou". Regra nova, pro lado dos **monstros** primeiro: cada hit recebido durante o próprio `attack` acelera a animação (`AttackSpeedMultiplier`, Float parametrizando a Speed do estado `Attack` no Animator — feature nativa da Unity, não existia em nenhum controller do projeto até hoje), até um teto (`maxAttackSpeedMultiplier`, padrão 3x) onde o golpe resolve instantaneamente, sem esperar a animação. Implementado em `EnemyController.cs` (`AccelerateAttack`, com `attackHitFired` pra não golpear 2x se o Hit Event original já tinha disparado).

**Dois bugs de Animator, dois achados em sequência, ambos resolvidos:**
1. Parâmetro `AttackSpeedMultiplier` nasce com default `0` na Unity (não 1) — o primeiro ataque de cada monstro novo travava a 0x de velocidade pra sempre. Corrigido no código (seta explicitamente 1 ao iniciar o ataque) e nos 4 controllers (default do parâmetro pra 1).
2. Mesmo corrigido, continuava bugando a maior parte das vezes. Causa real, achada auditando os 4 controllers (`Base_Melee`, `Base_Ranged`, `Base_Melee_Ambush`, `Base_Ranged_Ambush`) linha a linha: o campo "Speed Parameter" do estado `Attack` estava ligado em **`MoveX`** por engano, não em `AttackSpeedMultiplier`. Como `MoveX` fica congelado (o `Move()` não roda durante o `Attacking`) no valor de antes do golpe começar, um valor perto de zero ou negativo travava/invertia a animação. Corrigido via edição direta do YAML nos 4 controllers.

Confirmado pelo usuário: sem bugs depois da correção #2.

---

## Fase 9 — Mesma regra pro lado do herói (Damage) — e um bug mais sério ainda

O usuário identificou uma consequência pior do lado do herói: como **movimento só é permitido durante `Walk`** (Fase 3), ficar preso repetindo `Damage` por tomar dano de várias fontes deixa o jogador **literalmente incapaz de andar**, o que causa mais dano ainda — o "eu nem consegui andar e morri" que qualquer jogo ruim tem. Implementada a mesma aceleração (`DamageSpeedMultiplier`), agora na base (`HeroController`), com o teto forçando `animator.Play("Walk"/"Idle", 0, 0f)` direto — devolve o controle na hora, sem esperar o Animator.

**Bug real, mais sutil que o dos monstros:** mesmo com o Animator certo (`DamageSpeedMultiplier`, default 1, sem repetir o erro do `MoveX`), o jogador continuava preso, **sem conseguir se mover**. Causa: uma corrida de frame. `SetTrigger()` arma o Trigger na hora, mas o Animator só processa a transição de verdade na própria passada de update dele, **depois** do `Update()` dos scripts. Quando o dano chega via física (colisão, que roda antes do `Update()` do mesmo frame), a checagem `!IsInState(animator, "Damage")` ainda via o estado antigo e resetava `isReactingToDamage` cedo demais, no mesmo frame — fazendo o próximo hit chamar `StartDamageReaction()` de novo (reiniciando o Trigger) em vez de `AccelerateDamageReaction()` (nunca acumulava, nunca batia o teto). Como `Damage` não escuta um `DamageTrigger` novo enquanto já está nele, esse Trigger ficava armado e disparava sozinho assim que o Exit Time normal terminasse — criando um loop `Damage → Walk/Idle → Damage` instantâneo, sem nenhum tempo real gasto em `Walk`. **Corrigido trocando a checagem de estado do Animator por um timer puro** (`damageReactionElapsed`/`damageReactionDuration`, escalado pelo próprio multiplicador de velocidade), desacoplado de qualquer introspecção do Animator.

---

## Fase 10 — Lockout de Energia pós-ultimate

Regra nova: usar a ultimate bloqueia o ganho de Energia por **2s** (`ultimateEnergyLockoutDuration`, 🔢 ajustável em teste) — sem isso, uma ultimate boa (que geralmente limpa a área ou salva o jogador) já pagaria sozinha a energia da próxima, virando ultimate infinita. Cooldown puro sobre o ganho de Energia (`HandleEnemyKilled` ignora enquanto a janela não zerar), sem distinguir quem matou o quê — resolvido exatamente como o usuário sugeriu.

---

## Arquivos alterados/criados hoje

- **Novos:** `Assets/Scripts/Core/Combat/FanSpread.cs`, `Assets/Tests/EditMode/FanSpreadTests.cs`, `Assets/Scripts/Player/RangerArrow.cs`, `Assets/Scripts/Player/Heroes/Ranger.cs`, `Assets/Scripts/Player/HeroProjectile.cs`, `Assets/Animation/Heros/Barbarian/` (controller + clipes + pasta do projétil), `Assets/Prefabs/Heros/Barbarian/` (Barbarian + projétil da ultimate).
- **Reescritos por completo:** `Assets/Scripts/Player/Heroes/Barbarian.cs`.
- **Modificados:** `Assets/Scripts/Player/Heroes/HeroController.cs` (a maior mudança do dia — Animator, cooldown universal, aceleração de dano, lockout de energia, morte adiada), `Assets/Scripts/Enemies/EnemyController.cs` (knockback + aceleração de ataque), `Assets/Scripts/Core/DirectionUtility.cs` (`GetDirectionName`), `Assets/Scripts/World/FloorPopulationManager.cs` (`monsterPrefabs`), `Assets/Animation/Base/Base_Melee.controller`, `Base_Ranged.controller`, `Base_Melee_Ambush.controller`, `Base_Ranged_Ambush.controller` (parâmetro `AttackSpeedMultiplier` + correção do `MoveX`).
- **GDD:** `docs/gdd/index.md` (Seções 13, 16, 17 reescritas), `docs/gdd/bestiary.md` (referências corrigidas).

## Pendências abertas no fim do Dia 1 (status no fim da sprint, ver Dia 2 abaixo)

- ~~Fechar a sprint de verdade: **Ranger** ainda não foi atualizado pra bater com o GDD novo (8 direções fixas, perfuração, Tier de Arma em vez de carta).~~ **Resolvido no Dia 2.**
- `passiveGlowEffect` do Barbarian continua vazio (nenhum GameObject de brilho criado ainda) — **resolvido no Dia 2** (Passive Glow Effect finalizado).
- Reatribuir `monsterPrefabs` nos 2 `FloorPopulationManager` da `_TestScene` (perderam a referência ao renomear o campo) — não confirmado, segue como dívida técnica pra sprint seguinte.
- Prioridade entre efeitos visuais simultâneos (Seção 16) continua 🟡, sem hierarquia definida — resolvido parcialmente no Dia 2 (distinção Passiva vs Efeito Nocivo formalizada na Seção 33), lista de categorias de Efeito Nocivo em si continua 🟡 (só Prisão/DoT existem).
- Dependência nova da ultimate do Assassin com o sistema de emboscada (Seção 22) só está anotada, não implementada — fica pra quando o Assassin (Sprint 27) chegar.
- Valores `🔢` ainda são chutes de primeiro teste: `attackSpeedStepPerHit`/`maxAttackSpeedMultiplier` (monstro), `damageSpeedStepPerHit`/`maxDamageSpeedMultiplier`/`damageReactionDuration` (herói), `ultimateEnergyLockoutDuration` (2s).

---

## Dia 2 — Floating Combat Text, bug de dano duplicado, e o Ranger de verdade

### Floating Combat Text
Sistema de número de dano flutuante, pedido pelo usuário como "o único ponto antes de ir pro Ranger de verdade". Caminho até a versão final foi longo: (1) tentativa com `TextMeshPro` 3D solto — nunca ficou 100% claro por que a posição/direção não batia (câmera estática descartada como causa via teste do usuário), abandonado; (2) tentativa com Canvas World Space — `CanvasScaler` força o `Scale` do Canvas de volta pra `(1,1,1)` automaticamente em World Space (baseado em `Dynamic Pixels Per Unit`), fazendo o texto renderizar gigante mesmo com Scale manual pequeno; (3) **solução final:** Canvas de tela (Screen Space - Overlay, reaproveitando o Canvas de HUD que já existia) + `Camera.WorldToScreenPoint()` recalculado a cada frame — técnica padrão da indústria pra isso, evita todas as pegadinhas de Canvas novo. `Assets/Scripts/Core/FloatingCombatText.cs` e `FloatingCombatTextSpawner.cs` (novos), `GameEvents.OnDamageTaken` (novo evento), `GetFloatingTextSpawnPosition()`/`floatingTextHeightAdjust` em `EnemyController`/`HeroController`.

### Bug de dano duplicado (descoberto graças ao Floating Combat Text)
Usuário notou visualmente (2 números por hit) que monstros aplicavam dano em dobro no herói. Causa raiz: a `Attack` state (monstros e Barbarian) é uma Blend Tree 2D Freeform Directional com só 4 pontos diagonais — pra quase qualquer ângulo de mira, 2 clipes tocam misturados ao mesmo tempo, e o Animator dispara o Animation Event de **todo** clipe com peso > 0, não só do dominante. Cada clipe carregava seu próprio `AnimationHitEvent`/`AnimationAttackHitEvent`, disparando 2x por golpe. Corrigido com trava de idempotência (`attackHitFired` já existia mas só era consultado em `AccelerateAttack()`, nunca no evento em si) em `EnemyController.AnimationHitEvent()` **e** em `Barbarian.cs` (`AnimationAttackHitEvent`/`AnimationUltimateLandEvent` — o Barbarian tinha o mesmo bug, nunca reportado por afetar monstros com HP alto, menos perceptível). Diagnosticado via log de stack trace temporário em `HeroController.TakeDamage()`, mesma técnica que resolveu o bug de posição do Floating Combat Text.

### Regra nova: aggro instantâneo ao tomar dano
`EnemyController.TakeDamage()` agora força `isInCombat = true` na hora, independente de distância — evita o jogador "pokar" um monstro parado de longe (leque do Ranger) sem ele nunca vir de verdade. Trava (`combatLocked`) garante que monstros de emboscada (`staysDormantUntilDetected`) não voltem a dormir depois de serem atingidos uma vez.

### Ranger — primário completo
Reconstruído do zero em cima do esqueleto do Dia 1: Animator real (Idle/Walk/Damage com Blend Tree de 4 diagonais, Attack com Blend Tree de **8** pontos, Die sem Blend Tree), `ArrowFormation.cs` (formação em cunha/V — todas as flechas paralelas na mesma direção, só nascem deslocadas, gera o teto ponta-de-1-se-ímpar/ponta-de-2-se-par pra qualquer quantidade futura), depois revisado pra mira livre (`RawAimDirection`, novo em `HeroController` — direção crua do mouse sem o snap de 8 direções, só a pose visual continua presa nas 8 poses possíveis) com rotação real do sprite (`Quaternion.Euler` baseado no ângulo, substitui completamente o Blend Tree/8-sprites da flecha em si — 1 sprite só, gira de verdade; mesmo padrão aplicado ao `EnemyProjectile.cs` genérico de monstro). Perfuração/reserva de dano implementada na `RangerArrow` (idêntico ao `HeroProjectile`). Teto de flechas revisado pra **15** (não mais 7), +1 por tier de arma — decisão de gosto do usuário ("gostei d+ de ter muitas flechas na tela").

### Arquivos novos/alterados no Dia 2
`Assets/Scripts/Core/FloatingCombatText.cs`, `FloatingCombatTextSpawner.cs`, `Assets/Scripts/Core/Combat/ArrowFormation.cs` (novos); `HeroController.cs` (`RawAimDirection`, `floatingTextHeightAdjust`), `EnemyController.cs` (aggro instantâneo, `AnimationHitEvent` idempotente, `floatingTextHeightAdjust`), `Barbarian.cs` (trava idempotente no Attack/Ultimate), `Ranger.cs`/`RangerArrow.cs` (reescritos), `EnemyProjectile.cs`/`RangedEnemyController.cs` (rotação real, API `Launch()`), `DirectionUtility.cs` (`GetDirectionIndex`), `docs/gdd/index.md` (Seção 17.2 revisada, Seção 33 Efeitos Nocivos vs Passiva, `balance-values.md` novo).

### Dívida técnica real no fim da sprint
- Reatribuir `monsterPrefabs` nos 2 `FloorPopulationManager` da `_TestScene` — nunca confirmado.
- Lista de categorias de Efeito Nocivo (só Prisão/DoT existem) — cresce conforme novos monstros.
- Valores `🔢` de balanceamento em geral, incluindo os novos do Ranger (`attackDamageMultiplier`, `arrowLateralStep`/`arrowForwardStep`, `knockbackForce` da flecha).

## Testes executados

Só manual, em Play Mode, ao longo do dia — nenhum teste automatizado novo além do `FanSpreadTests` da Fase 1. Confirmado pelo usuário: golpe/ultimate/passiva do Barbarian funcionando, sem monstro travando em `Attack`, sem herói travando em `Damage`.
