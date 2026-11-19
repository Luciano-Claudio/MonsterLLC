# D9.1 — Atacar andando: plano de implementação

> **Para agentes:** SUB-SKILL OBRIGATÓRIA: superpowers:executing-plans (método escolhido pelo usuário: Native, herói por herói). Os passos usam checkbox (`- [ ]`).

**Objetivo:** o herói anda durante o ataque primário tocando a animação `walk_attack` própria. A troca entre ataque parado e ataque andando continua no mesmo frame. Shift e Ultimate continuam travando o movimento.

**Arquitetura:**
- Cada clipe de ataque ganha um gêmeo `walk_<nome>`. Uma ferramenta de Editor deriva o gêmeo do clipe original: mesmos tempos, mesmos eventos, cada sprite trocado pelo de mesmo índice na sheet `Walk_Attack`.
- Cada estado de ataque ganha um gêmeo `<Estado>_Walk` no Animator, criado pela API.
- Em runtime, o `HeroController` troca entre os dois com `Animator.Play(hash, 0, normalizedTime)` quando o jogador começa ou para de andar.

**Tech stack:** Unity 6000.3.8f1, C#, `UnityEditor.Animations`, `ISpriteEditorDataProvider`, NUnit EditMode (`Assets/Tests/EditMode`), Unity CLI `script eval`.

**Spec:** `docs/new/Mudanca_Estrutural_Deadline_9_XP_Cartas_Horda.md`, Seção 1.1. Roadmap: `docs/superpowers/plans/2026-11-18-deadline-9-roadmap.md`, D9.1.

**Ritmo (decisão do usuário, 2026-11-19):** um herói por vez, discutido antes, sem pressa. A Parte A monta a infraestrutura genérica e já liga o Barbarian. Cada herói da Parte B só é detalhado em tasks quando chegar a vez dele, a partir das notas deixadas aqui.

## Restrições globais

- Comentários de código e textos em pt-BR, no estilo dos arquivos vizinhos.
- Animator Controllers e clipes são editados só pela API do Editor (`AnimatorController`, `AnimationUtility`), nunca à mão no YAML. Fatiamento de sprite só por `ISpriteEditorDataProvider`.
- Nunca gerar fileID de cena por max+1.
- Rodar código no Editor: `/c/Users/Luciano/AppData/Local/Unity/bin/unity.exe command script eval --caller plugin --skill unity-cli --project-path "C:/Users/Luciano/Documents/MonsterLLC/MonsterLLC" --format json '<C#>'`. Pra compilar: `unity recompile`.
- Testes: lógica pura vai em NUnit EditMode. O que depende de asset ou de cena vai em método estático ou `[ContextMenu]` que retorna `PASS`/`FAIL`, rodado por `script eval` (padrão do `RunTelemetryTest.cs`).
- Commit no fim de cada task, só com os arquivos da task. As mudanças sem commit de outras frentes (docs, `Main_Menu.unity`, `ProjectSettings.asset`) não entram. Toda mensagem termina com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Nunca dar push sem perguntar.
- Números ajustáveis ficam serializados e marcados com `// 🔢 ajustável`.

## Fatos levantados (2026-11-19)

**Sheets novas:** são 15 arquivos sem commit. Cada uma tem o mesmo tamanho e o mesmo número de sprites da original, e o `.meta` foi copiado da original (mesmos nomes de sprite, GUID único). **Exceção, a Ranger:** as duas `Walk_Attack` foram fatiadas no modo automático (52 e 44 sprites justos, ~10px). A original é uma grade 32x32 de 44 sprites.

| Herói | Sheet original → sheet walk | Estados com gêmeo `_Walk` | Clipes (fps, frames, eventos) |
|---|---|---|---|
| Barbarian | `Minifantasy_TrueHeroesBarbarianAttack` → `Barbarian/Attack/Walk_Attack` | `Attack` (4 diag) | `attack_*` 12fps, 6f, `AnimationAttackHitEvent@2`, `AnimationAttackEndEvent@5` |
| Ranger | `Ranger_Shot_Diagonal/Orthogonal` → `Walk_Attack_Diagonal/Orthogonal` | `Attack` (8) | 12fps, 11f, `AnimationShootEvent@7`, `AnimationAttackEndEvent@10` |
| Mage | `Wizard_Fire_Torrent` → `Mage/Attack/Walk_Attack` | `Attack_Start`, `Attack_Continuous`, `Attack_End` (8). O `Attack` sai (ver Parte B) | 16fps: start 5f, continuous 12f, end 5f (`AnimationAttackEndEvent@4`) |
| Rogue | `...RogueShurikens` e `...Shurikens2-export` → `Rogue/Attack/Walk_Attack` | `AttackStart`, `AttackEnd` (4 diag) | 16fps: start 8f (`AnimationAttackStartEndEvent@7`), end 5f (`AnimationAttackEndEvent@4`) |
| Gunslinger | `Shot_Diagonal/Orthogonal` → `Walk_Attack_Diagonal/Orthogonal` | `Attack_Start`, `Attack_End` (8). `Attack_Shoot` sem gêmeo, mas permite andar | 16fps: start 3f, end 2f (`AnimationAttackEndEvent@1`) |
| Druid | `...DruidRootSummoning` → `Druid/Attack/Walk_Attack`; `...ForestBeastAttack` → `Druid/Ultimate/Walk_Attack` | `Attack` em `Druid.controller` e em `Druid_Elk.controller` (4 diag) | humano 16fps 7f (`AnimationVineSummonEvent@3`, `AnimationVineEndEvent@6`); Alce 12fps 5f (`AnimationElkClawHitEvent@2`, `AnimationElkClawEndEvent@4`) |
| Cleric | `ClericDivineFireDiagonal/Orthogonal` → `Walk_Attack_*` | `Attack` (8) | 16fps 12f, `AnimationProjectileLaunchEvent@9`, `AnimationProjectileEndEvent@11` |
| Paladin | `PaladinHolyHammerDiagonal/Orthogonal` → `Walk_Attack_*` | `Attack` (8) | 16fps 12f, `AnimationHammerThrowEvent@5`, `AnimationAttackEndEvent@11` |
| Blood Mage | `Blood_Shards_Diagonal/Orthogonal` → `Walk_Attack_*` | `Attack` (8) | 16fps 10f, `AnimationProjectileLaunchEvent@7`, `AnimationAttackEndEvent@9` |
| Assassin | — | nenhum (isca, ver Parte B) | — |

**Fatos do código:**
- Todo estado de ataque tem saídas `Walk`/`Idle`/`Damage` com exit time, e a entrada é por AnyState com trigger.
- A mira trava enquanto `isAttacking` (`HeroController.UpdateAimDirection`).
- Só o `HeroController.cs:695` pergunta pelo nome de um estado de ataque.

## Foco da revisão (Parte A)

1. **Troca com o clipe já terminado.** Com `normalizedTime >= 1` num clipe sem loop, não troca, senão o golpe reinicia ou o `AttackEndEvent` dispara 2 vezes. Teste na Task A4.
2. **Ação que não é o primário dentro de um estado andável** (ex.: Ultimate do Gunslinger no `Attack_Start`). Não anda nem troca. Teste na Task A4 (`actionAllowsWalk=false`).
3. **Gêmeo que não existe no controller atual** (troca de controller do Druid, herói ainda sem gêmeo). Checa `animator.HasState` antes do `Play`. Teste na Task A4.
4. **Dano ou Prisão durante o ataque andando.** O gêmeo copia as saídas (`Damage` com exit time), checado na Task A3. Com `isTrapped`, não anda nem troca, testado na Task A4.
5. **Trocar de estado a cada frame** (o jogador batendo na tecla). O golpe sai 1 vez só. Teste na Task A5.

---

# Parte A — Infraestrutura + Barbarian

### Task A1: Validação das sheets (ferramenta) + Barbarian

**Arquivos:**
- Criar: `Assets/Editor/HeroAnimationTools/AttackWalkAssetTools.cs` (classe estática `AttackWalkAssetTools`)
- Commit também: `Assets/Sprites/Heros/Barbarian/Attack/Walk_Attack.png` + `.meta`. As sheets dos outros heróis entram nas tasks deles.

**Interfaces:**
- Produz: `public static readonly List<(string original, string walk)> SheetPairs`. Começa só com o par do Barbarian, e cada herói da Parte B acrescenta os seus.
- Produz: `public static string ValidateSheets(string heroFilter = null)`. Pra cada par, compara: número de sprites; retângulo e pivô por índice; `spritePixelsToUnits`, `filterMode`, `textureCompression` e `spriteMode`. Retorna `PASS (N pares)` ou uma linha `FAIL: <par> <campo> <original> != <walk>` por divergência.
- Produz: `public static Sprite SpriteAtIndex(Texture2D sheet, int index)`. O índice vem do sufixo `_N` do nome do sprite.
- Produz: `public static string CopySlicing(string sourceTexturePath, string targetTexturePath)`. Copia retângulos, pivôs, bordas e alinhamento via `ISpriteEditorDataProvider`, renomeia os sprites do alvo pra `<arquivoAlvo>_<índice>` e reimporta. A Ranger usa; no Barbarian não roda.

- [ ] **Passo 1:** escrever `ValidateSheets` e `SpriteAtIndex`. Rodar `ValidateSheets("Barbarian")`. Esperado: `PASS (1 par)`.
- [ ] **Passo 2:** escrever `CopySlicing`. Não roda agora. Teste rápido: copiar o fatiamento do original do Barbarian pra uma cópia temporária da walk em `Assets/_Temp/`, validar e apagar a cópia.
- [ ] **Passo 3:** commit `chore: ferramenta de validacao das sheets walk_attack (Barbarian)`.

### Task A2: Derivar os clipes `walk_*` (ferramenta) + Barbarian

**Arquivos:**
- Alterar: `AttackWalkAssetTools.cs`
- Gera: `Assets/Animation/Heros/Barbarian/walk_attack_{ne,nw,se,sw}.anim`

**Interfaces:**
- Produz: `public static AnimationClip DeriveWalkClip(AnimationClip source)`. Cria ou sobrescreve `walk_<source.name>.anim` na mesma pasta.
  - Copia `frameRate`, `AnimationClipSettings` (loop incluso), as curvas float sem mudança e todos os `AnimationEvent` (tempo, função, parâmetros).
  - Na curva `m_Sprite`, troca cada sprite por `SpriteAtIndex(walkSheet, índice)`, usando a sheet walk pareada com a textura daquele sprite.
  - Se faltar o par ou o índice, lança exceção com o nome do clipe.
- Produz: `public static string DeriveForHero(string heroFolder, string[] clipNames)`, que retorna `Derivados: N`.
- Produz: `public static string ValidateDerivedClips(string heroFolder, string[] clipNames)`. Por par, confere: `frameRate`, `length` e `isLooping`; número e tempos das keys de sprite; eventos iguais (função + frame); mesmo índice de sprite em cada key, vindo da sheet walk. Retorna `PASS (N clipes)` ou uma linha `FAIL` por divergência.

- [ ] **Passo 1:** escrever `ValidateDerivedClips`. Rodar pra `Barbarian` com `attack_ne/nw/se/sw`. Esperado: `FAIL`, 4 faltando.
- [ ] **Passo 2:** escrever `DeriveWalkClip` e `DeriveForHero`, e rodar. Esperado: `Derivados: 4`.
- [ ] **Passo 3:** rodar `ValidateDerivedClips` de novo. Esperado: `PASS (4 clipes)`.
- [ ] **Passo 4:** commit `feat: deriva clipes walk_attack do Barbarian`.

### Task A3: Estado gêmeo `_Walk` no Animator (ferramenta) + Barbarian

**Arquivos:**
- Alterar: `AttackWalkAssetTools.cs`
- Alterar, só pela API: `Assets/Animation/Heros/Barbarian/Barbarian.controller`

**Interfaces:**
- Produz: `public static string AddWalkTwin(AnimatorController controller, string standStateName)`. Idempotente: se o gêmeo já existe, recria.
  - Cria `<standStateName>_Walk` na layer 0 com o mesmo `speed`, speed parameter, `tag` e `writeDefaultValues`.
  - O motion é uma BlendTree nova, sub-asset do controller, com o mesmo `blendType`, parâmetros e posições. Cada filho troca o clipe por `walk_<clipe>`.
  - Copia cada transição de saída com o mesmo destino, condições, exit time e duração. A transição pra ele mesmo aponta pro próprio gêmeo.
  - Não cria transição de AnyState.
- Produz: `public static string ValidateTwin(AnimatorController controller, string standStateName)`. Confere que o gêmeo existe, que filhos e posições batem, que cada filho é `walk_` + o nome do original, o speed parameter e as transições com destinos mapeados.

- [ ] **Passo 1:** escrever `ValidateTwin` e rodar no Barbarian/`Attack`. Esperado: `FAIL` (não existe).
- [ ] **Passo 2:** escrever `AddWalkTwin`. Rodar e depois `AssetDatabase.SaveAssets()`.
- [ ] **Passo 3:** `ValidateTwin` de novo. Esperado: `PASS`.
- [ ] **Passo 4:** commit `feat: estado Attack_Walk no Animator do Barbarian`.

### Task A4: Troca em runtime e liberação do movimento

**Arquivos:**
- Criar: `Assets/Scripts/Player/Heroes/AttackWalkSwap.cs` (lógica pura, estática)
- Criar: `Assets/Tests/EditMode/AttackWalkSwapTests.cs`
- Alterar: `Assets/Scripts/Player/Heroes/HeroController.cs` (campos perto da linha 175, gate de movimento perto das linhas 687-713)
- Alterar: prefab do Barbarian (`walkableAttackStates = ["Attack"]`, `allowMoveDuringPrimaryAttack = true`)

**Interfaces:**
- Produz: `public enum AttackWalkAction { None, ToWalk, ToStand }`.
- Produz: `public static AttackWalkAction AttackWalkSwap.Decide(bool wantsToMove, bool trapped, bool actionAllowsWalk, bool inStandState, bool inWalkTwin, bool counterpartExists, float normalizedTime, bool looping)`.
  - Retorna `ToWalk` só com: andando, não preso, ação permite andar, no estado parado, gêmeo existe e (`looping` ou `normalizedTime < 1`).
  - Retorna `ToStand` no caso simétrico, quando está no gêmeo e o jogador parou.
  - Em qualquer outro caso, `None`.
- Produz: `public static float AttackWalkSwap.ContinueTime(float normalizedTime, bool looping)`. Retorna a parte fracionária se `looping`, e o valor sem mudança se não.
- Produz, no `HeroController`:
  - `[SerializeField] private string[] walkableAttackStates`. O gêmeo é o nome + `"_Walk"`, se existir no controller atual.
  - `protected virtual bool CanWalkDuringCurrentAction => true;`
  - `public bool IsAttackWalking { get; private set; }`

- [ ] **Passo 1:** escrever os testes:
  - `Decide_StartsWalking_InStandMidClip_ReturnsToWalk` com `0.4`, sem loop → `ToWalk`.
  - `Decide_StopsWalking_InWalkTwin_ReturnsToStand` → `ToStand`.
  - `Decide_ClipFinished_NonLooping_ReturnsNone` com `1.0` → `None`.
  - `Decide_Looping_PastOne_StillSwaps` com `3.25` e loop → `ToWalk`.
  - `Decide_ActionDisallowsWalk_ReturnsNone`.
  - `Decide_CounterpartMissing_ReturnsNone`.
  - `Decide_Trapped_ReturnsNone`.
  - `ContinueTime_Looping_ReturnsFraction`: `(3.25, true)` → `0.25`.
  - `ContinueTime_NonLooping_ReturnsSame`: `(0.4, false)` → `0.4`.
- [ ] **Passo 2:** rodar os testes EditMode. Esperado: falha de compilação.
- [ ] **Passo 3:** implementar `AttackWalkSwap.cs`. Esperado: 9 PASS.
- [ ] **Passo 4:** integrar no `HeroController.Update()`, antes do `canMove`.
  - Hashes pré-calculados no `Awake`, sem alocar nada no `Update`.
  - Ler `GetCurrentAnimatorStateInfo(0)` uma vez por frame e achar o par pelo `shortNameHash`.
  - Ignorar a troca durante `animator.IsInTransition(0)`.
  - Checar `animator.HasState(0, hashDestino)` e chamar `animator.Play(hashDestino, 0, ContinueTime(...))`.
  - `canMove = wantsToMove && (IsInState("Walk") || (allowMoveDuringPrimaryAttack && !isTrapped && CanWalkDuringCurrentAction && estadoAtualAndável))`.
  - Remover a checagem antiga de `"Attack"` e o comentário "TESTE 2026-11-18".
- [ ] **Passo 5:** configurar o prefab do Barbarian, por Inspector ou por `SerializedObject` via `script eval`.
- [ ] **Passo 6:** `unity recompile` sem erros.
- [ ] **Passo 7:** commit `feat: troca Attack <-> Attack_Walk no mesmo frame (Barbarian ligado)`.

### Task A5: Eventos à prova de disparo duplo (harness) + Barbarian

**Arquivos:**
- Criar: `Assets/Scripts/Tests/AttackWalkEventTest.cs` (padrão `RunTelemetryTest`, com `testEnemyPrefab` e a lista de prefabs de herói)
- Alterar, se precisar: `Barbarian.cs`, `AnimationAttackHitEvent` e `AnimationAttackEndEvent`. O hit já tem `attackHitFired`; conferir o End.

**Interfaces:**
- Produz: `public string AttackWalkEventTest.TestDoubleFire(string heroName)`.
  - Instancia o herói e chama `TryPrimaryAttack()`.
  - Chama 2 vezes seguidas o handler de impacto e 2 vezes o de fim.
  - Conta o efeito: `TakeDamage` no inimigo de teste, ou projéteis novos nos heróis com projétil.
  - Também confere que o cooldown começou 1 vez só.
  - Retorna `PASS` ou `FAIL <herói>: <detalhe>`.

- [ ] **Passo 1:** escrever o teste, colocar no `_TestScene` e rodar `TestDoubleFire("Barbarian")` em Play Mode.
- [ ] **Passo 2:** corrigir o que falhar. Esperado: `PASS`.
- [ ] **Passo 3:** commit `test: harness de disparo duplo dos animation events (Barbarian)`.

### Task A6: Teste de jogo do Barbarian + telemetria

**Arquivos:**
- Alterar: `Assets/Scripts/Telemetry/RunTelemetryRecorder.cs`. Adiciona `public bool isAttackWalking;` no `SnapshotRecord`, preenchido com `hero.IsAttackWalking` na amostragem de 1Hz (linha ~85).

- [ ] **Passo 1:** adicionar o campo e rodar os testes do `RunTelemetryTest`. Esperado: todos `PASS`.
- [ ] **Passo 2:** checklist do usuário no `_TestScene`:
  - (a) Começar parado e andar no meio do golpe: continua do mesmo frame.
  - (b) O inverso.
  - (c) O golpe sai 1 vez, no frame certo.
  - (d) Ultimate e Shift continuam travando.
  - (e) Tomar dano no meio funciona como hoje.
  - (f) A animação girando (ressalva do usuário) fica boa?
- [ ] **Passo 3:** o usuário ajusta `moveSpeedMultiplierDuringPrimaryAttack` (começa em 1).
- [ ] **Passo 4:** commit `feat: Barbarian ataca andando, telemetria isAttackWalking`.

---

# Parte B — Demais heróis (detalhar quando chegar a vez)

Cada herói segue o mesmo roteiro: sheets (A1), clipes (A2), gêmeos (A3), `walkableAttackStates` no prefab (A4), `TestDoubleFire` (A5) e checklist (A6). As notas abaixo são o que muda em cada um.

### Ranger
- Rodar `CopySlicing` de `Ranger_Shot_Diagonal` → `Walk_Attack_Diagonal` e de `Ranger_Shot_Orthogonal` → `Walk_Attack_Orthogonal` antes do `ValidateSheets`.
- Estado: `Attack` (8 direções). Discutir a velocidade por causa do kite.

### Mage (rework do primário, decidido em 2026-11-19)

**Tasks do Mage (detalhadas em 2026-11-19, depois da conversa com o usuário):**

Decisões do usuário:
- Em cada linha de 20 sprites (no `attack` e no `walk_attack`), os frames 0–3 são o start, 4–15 o continuous e 16–19 o end.
- Mapeamento das linhas: 1ª linha = se/e/ne, 2ª = sw/w, 3ª = s, 4ª = n/nw. Fica como já é hoje.
- Tick de 0,25s, com dano = dano × intervalo.
- Perks: não mexer, porque saem no D9.6.

Tasks:
- **M1. Refazer os clipes de corpo** `attack_start_*`, `attack_continuous_*` e `attack_end_*` com a divisão 4/12/4, usando `AttackWalkAssetTools.ReframeFromRow(clip, rowLength=20, startInRow, count)`. Eventos que caem depois do último frame vão pro último frame; o `AnimationAttackEndEvent` do end fica no frame 3. Validador: `ValidateRowSlice`.
- **M2. Primário vira canalização sempre:**
  - Sai o estado `Attack` e os 8 clipes `attack_{dir}.anim`. Só o `Mage.controller` referencia esses clipes.
  - `AttackTrigger` passa a ir pra `Attack_Start`.
  - O fim ao soltar o botão continua no trigger atual.
  - O `PrimaryAttack` do Mage não tem mais o ramo do `Attack` normal.
  - O timer de segurança (`maxActionDuration`) não conta enquanto o jogador está canalizando. Hoje ele cortaria o fogo depois de 3s.
- **M3. Walk:**
  - Par de sheets `Wizard_Fire_Torrent` → `Mage/Attack/Walk_Attack`.
  - 24 clipes `walk_attack_{start,continuous,end}_*`.
  - Gêmeos `Attack_Start_Walk`, `Attack_Continuous_Walk` e `Attack_End_Walk`.
  - `walkableAttackStates` = `Attack_Start`, `Attack_Continuous`, `Attack_End`.
- **M4. Mira livre durante o primário:**
  - `protected virtual bool CanAimDuringPrimaryAttack => false;` no `HeroController`, e o Mage sobrescreve com `true`.
  - O `UpdateAimDirection` deixa de travar quando `isAttackResolving && CanAimDuringPrimaryAttack`.
  - A BlendTree troca de direção no mesmo `normalizedTime`, e o pivô do fogo gira junto.
- **M5. Fogo start/loop/end + tick por monstro** (espera a divisão do fogo, que o usuário vai passar):
  - O rastreador por monstro é lógica pura testada em EditMode.
  - O dano só existe no loop do fogo.
- **M6. Teleporte atravessa monstros:**
  - `TeleportUtility.ClampDistance` ignora colliders da layer `Enemy`.
  - Vale pro Mage e pro Assassin.
- **Primário vira canalização:** `Attack_Start` → `Attack_Continuous` (loop enquanto segura) → `Attack_End`. Cada um tem o gêmeo `_Walk`, todos em 8 direções.
- **Sai:** o estado `Attack` e os 8 clipes `attack_{n,ne,e,se,s,sw,w,nw}.anim`. Hoje só o `Mage.controller` referencia esses clipes (conferido por GUID).
- **Gatilhos:** `AttackTrigger` → `Attack_Start`, e o fim ao soltar o botão (hoje `EternalFlameEndTrigger`).
- **Perk:** a "Chama Eterna" (#29) deixa de existir como perk, porque vira o padrão. Os perks do primário do Mage que são "por golpe" (Rajada Dupla #22, Chama Vingativa #27) ficam pra decidir na hora. Os perks saem no D9.6 de qualquer jeito.
- **Mira livre durante o ataque:**
  - Gancho `protected virtual bool CanAimDuringPrimaryAttack => false;` no `HeroController`, com o Mage em `true`.
  - Trocar a mira muda `AimX/AimY`, e a BlendTree troca de direção no mesmo `normalizedTime`. Todas as direções de cada estado têm o mesmo número de frames.
  - O pivô do fogo (`attackHitboxPivot`) já gira com `RawAimDirection` a cada frame. Hoje ele só fica parado porque a mira congela.
- **Fogo em 3 partes:**
  - O clipe `fire.anim` atual tem 16 frames e não faz loop. Proposta de corte, pra confirmar olhando a sheet `Orthogonal_Fire_Torrent_Effect`: start 0–3 (crescendo), loop 4–11 (cheio), end 12–15 (diminuindo).
  - Cada parte sincroniza com `Attack_Start`/`_Continuous`/`_End` do corpo.
- **Dano por tick, por monstro** (regra compartilhada com o Rogue):
  - Um rastreador por monstro guarda o último hit. Monstro que entra no fogo pela 1ª vez leva dano na hora, e depois leva de novo a cada `tickInterval` enquanto continuar dentro.
  - Sair e entrar de novo não zera o timer, então não dá pra abusar. O registro sobrevive entre ataques seguidos e é limpo quando o tempo do monstro já passou.
  - O intervalo tem que ser baixo, pra ninguém sentir que "o monstro entrou e não levou dano". O valor 🔢 vai ser discutido.
  - O dano por tick segue a fórmula do Rogue (`dano por segundo × intervalo`). O balanceamento fino fica pro D9.6.
- **Bug do teleporte (anotado em 2026-11-19):**
  - `TeleportUtility.ClampDistance` (`Assets/Scripts/Player/TeleportUtility.cs`) para em qualquer collider sólido, inclusive no corpo dos monstros (`CapsuleCollider2D` sólido na layer `Enemy`, índice 9).
  - Correção: ignorar colliders da layer `Enemy` e continuar parando em árvores, água e props.
  - Vale também pro Assassin, que usa a mesma função (decisão do usuário).
  - Teste de cena: monstro no caminho → distância cheia. Collider de prop no caminho → distância encurtada.

### Rogue
- Estados: `AttackStart` e `AttackEnd`. A `Walk_Attack` serve pras 2 sheets de origem; conferir os índices visualmente.
- **Tick por monstro:** o `RoguePulseArea` hoje usa um timer único da área inteira (`AttackCooldown tick`): quem entra entre ticks espera o próximo. Passa a usar o mesmo rastreador por monstro do Mage, com hit imediato na 1ª entrada.
- Nota: o fogo no chão do `MageFireball` usa o mesmo padrão de timer por área. Fica fora do escopo, a não ser que o usuário peça.

### Gunslinger
- Estados andáveis: `Attack_Start`, `Attack_Shoot` (sem gêmeo, mesmo clipe) e `Attack_End`.
- `CanWalkDuringCurrentAction => activeAction == ActiveAction.Primary`, porque a Ultimate e o Shift usam o mesmo `Attack_Start`.

### Druid
- Gêmeo do `Attack` nos 2 controllers (`Druid.controller` e `Druid/Ultimate/Druid_Elk.controller`). A troca de controller é coberta pelo `HasState`.

### Cleric, Paladin, Blood Mage
- Estado `Attack` (8 direções), 2 sheets cada (diagonal e ortogonal). Sem particularidade conhecida.

### Assassin
- Não ganha gêmeo e continua travado durante o ataque.
- **Isca:**
  - Registro estático `HeroDecoys` (`Register`, `Unregister`, `Version`, `PickNearest`, `Clear`).
  - O `AssassinDashProjectile` registra no `Launch` e remove no retorno e no `OnDestroy`.
  - O `EnemyController` ganha `ChaseTargetPosition`: a isca mais perto, escolhida quando `Version` muda, ou senão o player. Vale pra boss também.
  - Só as leituras de **movimento** migram. Ataque, mira e teleporte de boss continuam no player.
- A correção do teleporte (seção do Mage) também vale aqui.

---

## Fora do escopo do D9.1

- Mudar os ataques, exceto o rework do Mage e o tick por monstro do Rogue, que foram decididos pelo usuário.
- Velocidade de animação ou cooldown novos.
- Ajuste da horda.
