# Sprint P9 — Camera System: Game Feel + Spawn Fora da Visão da Câmera

## Objetivo

Dar um dono explícito ao Camera System (ausente do roadmap anterior) e fechar os dois itens oficiais do P9 que ainda faltavam (Pixel Perfect follow já estava resolvido de uma sprint anterior): spawn de monstro/boss priorizando posições fora da visão da câmera, e — pedido extra do usuário, "game feel and juice" usando mecânicas do próprio Cinemachine — shake/zoom em hit e uma sequência de reveal ao nascer um boss.

## Sistemas adicionados

- **`HitFeelController`** — `CinemachineImpulseSource` (shake) + punch-zoom direto no `Lens.OrthographicSize`, só no hit DIRETO do jogador contra monstro (nunca DoT/Pet/pulse tick). Shake mais agressivo (sem zoom) quando o herói leva dano.
- **`BossEncounterCameraController`** — ao nascer um boss: pausa, troca `Follow` pro boss (pan suave via damping TEMPORÁRIO — ver achado real abaixo), segura, volta pro jogador, despausa. SEM `CinemachineTargetGroup` no desenho final (ver Decisões técnicas).
- **`isDirectPlayerHit`** (`IDamageable`/`EnemyController.TakeDamage`, default `true`) — só fontes de dano DIRETO do jogador acionam o Game Feel; DoT (`StatusEffectController`), rastros "grounded"/pulse tick (`MageFireball`, `RangerKnife`, `RoguePulseArea`) e dano de Pet passam `false`.
- **`WorldSpawnUtility.TryGetSpawnPosition` ganhou `preferOutsideCameraView`** (default `false`, aditivo) — dentro do mesmo laço de tentativas, guarda o primeiro candidato válido como fallback e continua tentando achar um que TAMBÉM fique fora da visão atual da câmera (`Camera.main.WorldToViewportPoint`); nunca falha o spawn só por causa disso — a Safe Zone continua sendo a única verificação que de fato bloqueia.
- **`outsideCameraViewChance`** (`MonsterSpawnSettings`/`BossSpawnSettings`, padrão 0.6 = 60%) — rolado por spawn INDIVIDUAL, não por tipo de monstro/boss: uma % fixa nasce perto/à vista do jogador de propósito, pra dar um ar de perigo.
- **Fix real de bug pré-existente**: `PetController.SetMoving`/`Initialize` chamavam `Animator.SetBool`/`SetFloat` em parâmetros que o Animator do Phoenix não tem (`IsMoving`/`ActionSpeedMultiplier`, só o Elemental de Sangue tem) — a suposição antiga de que isso era silencioso estava errada, a Unity loga "Parameter 'X' does not exist" toda chamada. Cacheado 1x em `Awake` (`HasParameter`), sem checar todo frame.
- **`AstarPath.logPathResults`**: Normal → None — log "Path Completed" a cada path calculado, achado real em teste.

## Decisões técnicas

- **`CinemachineTargetGroup` removido do desenho final** — tentado primeiro (câmera afasta pra enquadrar jogador+boss durante o combate), mas o usuário reportou que isso atrapalhava a visão/controle do jogador. Revertido pra: depois do reveal, a câmera simplesmente volta a seguir só o jogador, igual sempre foi.
- **Achado real de `PositionDamping=0`** — faz parte da correção do jitter do Pixel Perfect (sprint anterior). Sem ajuste, trocar `Follow` durante o reveal seria um corte seco, não um pan (confirmado: `CinemachineBrain.DefaultBlend` só blenda troca ENTRE vcams diferentes, nunca troca de `Follow` dentro do MESMO vcam). Resolvido subindo o damping SÓ durante a janela pausada do reveal (jogo pausado, alvos parados — risco de jitter muito menor que perseguir um alvo em movimento) e devolvendo a 0 antes de despausar.
- **Componente errado inicialmente (`Cinemachine Collision Impulse Source` em vez de `Cinemachine Impulse Source`)** — achado pelo usuário via screenshot: o Collision variant não chama `base.Reset()`, então nasce com `Impulse Type: Legacy`/sem forma de onda em vez de `Uniform`/`Bump`. Corrigido trocando pro componente simples — resolve de graça um bug futuro também: com o tipo errado, a força do impulso cairia com a distância até a posição FIXA do GameObject gerador (problema real num mundo aberto onde o jogador se afasta muito da origem).
- **Valores de intensidade reduzidos 3x em sessão** — primeira leva (0.5/0.6/1.5) "extremamente alto"; segunda leva (0.15/0.2/0.4) ainda alto; final (0.08/0.1/0.25) considerado sutil o bastante. Hero hit shake mantido mais agressivo que o de monstro de propósito, nunca com zoom.
- **`isDirectPlayerHit` entrou em `IDamageable`, não só em `EnemyController`** — `StatusEffectController` (quem precisa chamar com `false` pro DoT) mora no assembly Core, que não referencia Assembly-CSharp (onde `EnemyController`/`HeroController` vivem) — não dá pra nem mencionar o tipo concreto lá, só chamar através da interface.
- **`PaladinOrbitingBlades` deixado de fora do filtro `isDirectPlayerHit`** — caso ambíguo (ultimate ativa com hit repetido por volta, não um tick de intervalo fixo como os outros); mantido como hit direto (`true`) por ser mais parecido com arma ativa do que efeito passivo.

## Arquivos/classes principais

- `Assets/Scripts/Camera/HitFeelController.cs`, `BossEncounterCameraController.cs` — novos.
- `Assets/Scripts/Core/IDamageable.cs`, `GameEvents.cs` — `isDirectPlayerHit`/`OnBossSpawned`.
- `Assets/Scripts/Enemies/EnemyController.cs`, `PetController.cs` — `TakeDamage(float, bool)`, fix de Animator.
- `Assets/Scripts/Core/StatusEffectController.cs`, `Assets/Scripts/Player/MageFireball.cs`, `RangerKnife.cs`, `RoguePulseArea.cs` — `isDirectPlayerHit: false` nos ticks/DoT.
- `Assets/Scripts/Player/Heroes/HeroController.cs`, `Paladin.cs` — assinatura `TakeDamage` atualizada (interface).
- `Assets/Scripts/World/WorldSpawnUtility.cs`, `WorldPopulationManager.cs`, `WorldBossSpawnManager.cs` — `preferOutsideCameraView`/`outsideCameraViewChance`.
- `Assets/Scripts/Core/WorldGeneration/MonsterSpawnSettings.cs`, `BossSpawnSettings.cs` — campo novo.

## Eventos adicionados

- `GameEvents.OnBossSpawned` (`Action<Transform>`) — dispara logo depois do `Instantiate()` de um boss.
- `GameEvents.OnDamageTaken` ganhou 2 parâmetros: `isPlayer` (já existia) + `isDirectPlayerHit` (novo) — quebra de assinatura proposital, único consumidor antes disso era `FloatingCombatTextSpawner`.

## Testes executados

Manual (Play Mode), com bastante iteração guiada por screenshot do Inspector — shake/zoom em hit de monstro, shake em dano do herói, sequência de reveal de boss (pausa → foco → volta → despausa), confirmação de que DoT/Pet não acionam o Game Feel, % de spawn fora da câmera funcionando.

## Bugs conhecidos

Nenhum em aberto ao fim da sprint — todos os achados (componente de Impulse errado, `PositionDamping=0`, Animator sem parâmetro, log do A*) foram corrigidos na própria sprint.

## Dívida técnica

- **`PaladinOrbitingBlades` não revisado** — caso ambíguo deixado como está, usuário avisado que pode precisar do mesmo tratamento se incomodar em teste futuro.
- **Preço/número de "ar de perigo" (`outsideCameraViewChance`) é um valor inicial (60%), não balanceado em playtest real.**

## Próximos passos

Simulation Rings (P8 oficial) — usuário pediu implementação mesmo sem problema de FPS medido, "como preparação".
