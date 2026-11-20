# D9.4 — Horda: curva por dia, cerco, fluxo contínuo e anti-kite (design)

**Data:** 2026-11-20. **Status:** aguardando aprovação do usuário.
**Base:** roadmap `docs/superpowers/plans/2026-11-18-deadline-9-roadmap.md` (D9.4) e spec `docs/new/Mudanca_Estrutural_Deadline_9_XP_Cartas_Horda.md` (Seção 2). Este documento **substitui** a spec onde as duas discordam (ver "Mudanças na spec").

## 1. Intenção

Progressão no estilo Vampire Survivors. Cada dia começa com poucos monstros, espalhados, vindo na direção do herói. A quantidade sobe aos poucos até um teto alto. Os monstros chegam sempre de fora da tela e de todos os lados, num cerco sem buracos. A reposição acompanha a velocidade com que o jogador mata, sem rajadas visíveis.

Quem demora pra matar os bosses enfrenta cada vez mais monstros: a horda segue o **tempo real** do dia, que não pausa com boss vivo.

### Objetivos

- Curva de população por tempo real, com começo e teto próprios de cada dia.
- Nascimento sempre fora da tela, numa elipse ao redor do herói, distribuído por setores (cerco).
- Fluxo contínuo de reposição, no lugar do "1 a cada 3 s" e das rajadas da crise.
- Anti-kite sempre ligado, com limite.
- O herói empurra a horda em vez de ficar preso.
- O monstro que fica longe é reposicionado na frente, sem ser destruído.
- Avisos legíveis de explosão (Burning Skull e bomba do Goblin Sapper).

### Fora do escopo

- Sequência de bosses, pausa do cronômetro, enrage, fim do dia por boss: tudo isso é D9.5.
- Rebalanceamento de vida e dano dos monstros: D9.6.
- Pooling de monstros.

### Anti-objetivos (falhou, mesmo que funcione)

- Monstro aparecendo **dentro da tela** (nascendo ou reposicionado).
- Rajada visível: muitos monstros surgindo juntos no mesmo ponto.
- Um lado do cerco vazio, que vire rota de fuga fixa.
- Herói **preso sem saída** no meio da horda.
- Anti-kite que pareça trapaça: todo monstro nascendo na frente.
- Jogador forte esperando monstro nascer.
- FPS abaixo do medido no D9.3 com a mesma quantidade de monstros. O teto é 300; o D9.3 mediu 76 FPS no Editor com 300 andando.

## 2. Decisões do usuário (2026-11-20)

1. **A horda segue o tempo real do dia**, contado do início do dia. **Não pausa com boss vivo**; só pausa quando o jogo está pausado (`GameplayGate`). O objetivo é punir quem demora pra matar boss.
2. **O cronômetro manda só nos bosses (D9.5) e no Dia/Noite.** A noite continua trazendo tiers mais fortes, no mesmo ponto do cronômetro pra todo mundo.
3. **Cada dia tem começo e teto próprios.** Os dias mais avançados começam mais cheios; o dia 3, por exemplo, nunca recomeça com 10. O dia 1 provavelmente não chega a 300.
4. **Sem monstros ranged nos primeiros 30 s** de tempo real, **só no dia 1**. "Ranged" é só quem atira de longe. O Goblin Sapper (bomba) não conta.
4b. **O dia conta desde o começo.** O cronômetro e o relógio da horda começam quando o jogo fica jogável. Sai a regra da D8.1 de "o cronômetro só começa no primeiro aggro". Os monstros já nascem com o herói como alvo. **Nada conta durante o loading.**
5. **Duração esperada de um dia: ~3 minutos.** O dia termina 10–30 s depois que o último boss morre (D9.5). Dias com 3 bosses têm ~50 s entre eles; dias com 1 boss têm o boss em ~2:30. Nenhum dia chega a 5 minutos.
6. **O herói empurra a horda** (opção C). Nunca fica preso sem saída, mas sair de um cerco custa tempo.
7. **Anti-kite sempre ligado**, não só quando o jogador ataca. Tem limite.
8. **Caminho de construção 1:** evoluir o `WorldPopulationManager` e o `WorldSpawnUtility`, com as regras puras no Core e testes EditMode.

## 3. Abordagens consideradas

| Abordagem | Prós | Contras | Decisão |
|---|---|---|---|
| **1. Evoluir o sistema atual** | Mantém o que funciona (tiers por dia e noite, safe zone, obstáculos, ilha do grafo, telemetria). Mexe só no que muda. Regras puras testáveis no Core. | Arquivo antigo com histórico de decisões; precisa limpar o que sai. | **Escolhida pelo usuário** |
| 2. Diretor da horda novo, do zero | Código limpo. | Reescreve a escolha de tier e a safe zone, que já estão certas. Risco de regressão. | Descartada |
| 3. Ondas roteirizadas (lista de monstros por minuto) | Controle total. | Muito trabalho de autoria pra 10 dias. Difícil de ajustar enquanto os números são incertos. | Descartada |

## 4. Design

### 4.1 Relógio da horda

- Um contador de **tempo real do dia** (`HordeElapsed`). Zera no início de cada dia e avança com `Time.deltaTime` só enquanto `GameplayGate.IsActive`.
- **Início do dia = jogo jogável, nunca durante o loading.** Hoje o `GameplayBootstrap.HideLoadingScreenWhenWorldReady` espera o mundo ficar pronto e a câmera assentar, e só então muda o estado pra `GameState.Gameplay` e esconde a tela de loading. O relógio da horda, o cronômetro (`DayTimer`) e o spawn só correm a partir daí. Vale também na volta pra um dia novo. Se a tela de loading tiver fade, o relógio começa quando ela termina de sumir (decisão do construtor, com essa regra).
- **Sai a regra da D8.1** "o cronômetro só começa no primeiro aggro" (`DayTimer.StartDayClock` chamado pelo setter de `HeroController.GlobalAggroActive`). O cronômetro passa a correr desde o início do dia, e o aggro global já fica ligado no início do dia.
- **Retry do mesmo dia** (existe até o D9.5): o relógio da horda volta a zero, como num dia novo. *(Decisão minha: o retry some no D9.5 de qualquer jeito.)*
- **Teste:** campo de debug pra começar o dia com o relógio da horda adiantado (ex.: 3:00), pra testar o fim da curva sem esperar. O dia continua com a duração atual (150 s) até o D9.5 trocar o fim do dia. *(Decisão minha.)*
- É independente do `DayTimer`. No D9.4 os dois andam juntos; a diferença aparece no D9.5, quando o cronômetro passa a pausar com boss vivo.
- Tudo da horda usa este relógio: curva, picos e liberação dos ranged.
- O Dia/Noite continua no cronômetro (`DayTimer.IsNight`) e segue decidindo os percentuais de tier.

### 4.2 Curva de população

A meta é a quantidade de **monstros comuns vivos ao redor do herói**, sem contar bosses.

- **Forma da curva:** uma só, normalizada (0→1 no tempo e 0→1 no valor), compartilhada por todos os dias. Fica num `AnimationCurve` editável. A forma inicial segue os números do usuário pro dia 1: 0:00 → 10, 0:30 → 15, 1:00 → 30, 2:00 → 60, 3:00 → 100, 4:00 → 150 (teto).
- **Tempo até o teto:** ~4 min de tempo real (🔢 `secondsToCeiling` = 240). Quem joga bem termina o dia perto dos 3 min, com ~2/3 do teto. Quem demora chega no teto.
- **Tabela por dia** (🔢, editável no Inspector, um par por dia de 1 a 10):

  | Dia | Começo | Teto |
  |---|---|---|
  | 1 | 10 | 150 |
  | 2 | 15 | 180 |
  | 3 | 25 | 210 |
  | 4 | 30 | 240 |
  | 5 | 40 | 270 |
  | 6 a 10 | 50 | 300 |

  `meta(t) = começo + (teto − começo) × forma(t / secondsToCeiling)`. Depois do teto, a meta fica no teto.
- **Picos:** a cada 60 s (🔢), por 15 s (🔢), a meta sobe +50% (🔢). A meta nunca passa de 300 (🔢 `hardCap`, limite de desempenho medido no D9.3).
- **Qual dia usar:** o dia da run (`ActualDay`), e não o dia efetivo por distância da origem. O dia efetivo continua decidindo só os tiers, como hoje.

### 4.3 Fluxo contínuo de reposição

- Se os vivos estão abaixo da meta, nascem monstros a uma taxa máxima (🔢 `spawnsPerSecond`, ~10/s), com um acumulador fracionário espalhando os nascimentos pelos frames. Nunca vários no mesmo frame no mesmo setor.
- Isso cobre o preenchimento inicial do dia, a subida da curva e a reposição depois das mortes. Com a taxa acima da velocidade de kill, o jogador forte nunca espera.
- **Sai:** `respawnInterval`, a crise (`HandleCrisis`, `maxCrisisSpawnsPerFrame`), `populationMinimum`/`Target`/`Maximum` e a rampa `PopulationCurve01`.
- **Todo monstro comum nasce com o herói como alvo** (aggro imediato), em qualquer dia. Isso substitui o caso especial de hoje, que só dava aggro imediato no retry (`DayResolver.PlayerSurvivedLastDay`). Monstros de emboscada (`staysDormantUntilDetected`) seguem a mesma regra: o aggro global já acorda eles hoje.

### 4.4 Ranged só depois de 30 s (só no dia 1)

- O `EnemyDefinitionSO` ganha `isRanged` (bool). "Ranged" é **só quem atira de longe**, ou seja, o prefab tem `RangedEnemyController` ou uma subclasse dele. O valor inicial dos assets existentes é preenchido por esse critério. O Goblin Sapper e outros que não herdam dele ficam `false`.
- **Só no dia 1:** antes de `rangedUnlockSeconds` (🔢 30) de tempo real, a escolha de monstro ignora quem tem `isRanged`. Do dia 2 em diante não há bloqueio.
- Se um tier sorteado só tiver ranged durante o bloqueio, cai na regra de tier vazio que já existe (tier de fallback).

### 4.5 Nascer fora da tela, em elipse, por setores

- **Elipse:** semi-eixos = metade da largura e da altura da área visível da câmera, mais uma margem (🔢 `offscreenMargin`, ~2 unidades). Calculados a partir da câmera a cada spawn, porque a câmera pode mudar. O candidato fica na borda da elipse, com uma variação pequena pra fora (🔢 `ringJitter`).
- **Setores:** a elipse é dividida em **12** (🔢). Pra cada spawn, conta os monstros comuns vivos em cada setor, pelo ângulo de cada um em relação ao herói (vale qualquer distância). Escolhe o setor com menos monstros (empate: sorteio) e sorteia um ângulo dentro dele.
- **Validação do ponto:** a mesma de hoje (fora da safe zone, nó caminhável, mesma ilha do grafo do herói, sem obstáculo) **e** fora da tela.
  - Se o setor escolhido falhar nas tentativas, tenta o próximo menos cheio.
  - Se todas falharem, **não nasce** e tenta no frame seguinte.
  - **Sai o fallback visível** e sai o `outsideCameraViewChance`.

### 4.6 Anti-kite (sempre ligado)

- A direção de fuga é a média da velocidade do herói numa janela recente (🔢 `kiteWindowSeconds`, ~3 s). Só vale se o deslocamento médio passa de um mínimo (🔢), ou seja, se ele está andando de forma consistente pra um lado.
- Os setores dentro de um arco à frente dessa direção (🔢 `kiteArcDegrees`, ~90°) ganham **peso extra**. Na escolha de setor, o peso funciona como se o setor tivesse menos monstros (🔢 `kiteBias`).
- **Limite:** no máximo 🔢 `kiteMaxFrontShare` (~40%) dos spawns de uma janela vão pro arco da frente. Passou disso, a escolha volta a ser só por contagem. O objetivo é dificultar o kite, não proibir.
- Vale sempre, inclusive andando até baú ou Vendor (decisão do usuário).

### 4.7 Reposicionamento do monstro que ficou longe

- Monstro comum além de `enemyDespawnDistance` (🔢, hoje 30) é **teleportado** pra um ponto válido da elipse, escolhido pela mesma regra de setor e anti-kite. O anti-kite já faz ele reaparecer na frente de quem foge.
- Mantém vida, status e aggro. Cancela o ataque em andamento: já existe `CancelAttackForTeleport`, usado pelos bosses. Usa o teleporte do `AIPath`/`IAstarAI` pra não deixar estado velho no path, no RVO e no stuck-check.
- Se não acha ponto válido, tenta no frame seguinte. O monstro continua vivo e perseguindo.
- Bosses ficam fora (são do `WorldBossSpawnManager`).

### 4.8 O herói empurra a horda

**Fatos do código:** os monstros têm `Rigidbody2D` **cinemático** (os 78 prefabs). O herói é dinâmico (massa 1, linear damping 10). Corpo cinemático não é empurrado pela física, por isso o herói bate e para.

- O corpo do herói **deixa de colidir fisicamente** com o corpo dos monstros comuns.
  - Os triggers continuam funcionando: hitboxes de ataque dos monstros, dano de contato, contato do Burning Skull, coleta e projéteis.
  - Não usar `IgnoreLayerCollision(Player, Enemy)` global: ele desligaria também esses triggers. A Coruja do Druid e a Cambalhota do Rogue usam esse recurso de propósito, pra atravessar de vez.
- **Empurrão por código:** a cada frame, se o herói está se movendo e há corpos de monstro comum logo à frente (🔢 raio/arco curto):
  - a velocidade do herói é multiplicada por 🔢 `crowdPushSpeedMultiplier` (0,5);
  - esses monstros são deslocados pros lados, perpendicular ao caminho do herói, usando o knockback existente (`ApplyKnockback`) ou um deslocamento equivalente.
- Parado, nada muda: o cerco continua cercando e batendo. Sair custa tempo e vida, mas nunca é impossível.
- **Bosses:** imunes a knockback (regra existente). Eles continuam colidindo fisicamente com o herói como hoje; são poucos e não formam parede.
- A Coruja do Druid e a Cambalhota do Rogue continuam atravessando a horda de vez, como hoje.

### 4.9 Avisos de explosão

- **Burning Skull:** a área de explosão (`BurningSkullExplosionArea`, raio `FinalRadius`) já cresce no início do clipe Die, mas aparece tarde demais.
  - Passa a aparecer um **contorno do raio** no chão enquanto ele está perto do herói (🔢 distância), antes de explodir.
  - Na contagem final (clipe Die), o aviso **pisca**.
- **Bomba do Goblin Sapper:** do momento em que a bomba é plantada até explodir, mostra a área no chão com o raio real (`explosionRadius`), piscando mais rápido perto do fim.
- Reaproveitar o visual de área no chão que já existe nos ataques de monstro (`GroundTargetHazard` e a área do Burning Skull), sem arte nova.

### 4.10 Desempenho

- Contar monstros por setor: uma passada pelos vivos por spawn, sem alocar (buffers fixos). Com 10 spawns por segundo, o custo é desprezível.
- A média de velocidade do anti-kite usa um buffer circular fixo.
- O empurrão consulta só a layer `Enemy`, num raio curto, com buffer fixo (mesmo padrão do D9.3).
- Nada de lambda nem `new` por frame nos caminhos novos.

## 5. Regras puras no Core (com testes EditMode)

- **Curva:** meta por (dia, tempo real), incluindo picos e o `hardCap`.
- **Escolha de setor:**
  - setor menos cheio;
  - empate;
  - peso do anti-kite;
  - limite de participação da frente;
  - fallback pro próximo setor.
- **Elipse:** ponto da borda dado um ângulo e as dimensões da câmera; teste de "fora da tela".
- **Liberação dos ranged** por tempo.

## 6. Mudanças na spec (aplicar junto com este design)

- **Seção 8:** "tudo o que depende do tempo de dia (spawn gradual, picos da horda, liberação dos ranged…) continua controlado pelo cronômetro" passa a ser:
  - a horda (curva, picos, ranged) segue o **tempo real** do dia e não pausa com boss;
  - só o Dia/Noite segue o cronômetro.
- **Seção 2.2:** os pendentes resolvidos:
  - elipse, com margem ajustável;
  - 12 setores;
  - anti-kite sempre ligado;
  - a regra do "mínimo 3 vezes vira máximo" já não existe (virou rampa na D8.1) e sai de vez com a curva por dia.
- **Seção 2.3:** ranged (só os que atiram de longe) liberados com 30 s de tempo real, **só no dia 1**.
- **Seção 2.1 / regra da D8.1:** o dia conta desde o início (nunca durante o loading). Os monstros nascem com o herói como alvo, e sai o "cronômetro espera o primeiro aggro".

## 6b. Decisões minhas (o usuário confere na leitura)

- Duração do dia continua 150 s no D9.4. Um campo de debug adianta o relógio da horda pra testar o fim da curva.
- Retry do mesmo dia zera o relógio da horda.
- Dias acima de 10 usam a linha do dia 10. Os picos começam no minuto 1 (o primeiro em 1:00). O `hardCap` conta só monstros comuns.
- Valores 🔢 iniciais: tabela por dia, picos (+50%, 15 s, a cada 60 s), 4 min até o teto, ~10 spawns/s, 12 setores, margem de ~2 unidades, anti-kite (janela de 3 s, arco de 90°, no máximo 40% dos spawns na frente), empurrão a 50% da velocidade.

## 7. Deixado pro construtor

- Os valores 🔢 marcados como iniciais: o usuário calibra depois de jogar.
- O mecanismo exato pra separar a colisão física herói × corpo de monstro sem afetar os triggers. Opções: `Physics2D.IgnoreCollision` por par no spawn, ou uma layer só pro corpo físico do monstro. A regra é: os triggers do item 4.8 continuam funcionando.
- O formato do deslocamento lateral no empurrão (knockback ou deslocamento direto), desde que não brigue com o `AIPath`.
- O visual exato dos avisos de explosão, dentro do que já existe no projeto.
