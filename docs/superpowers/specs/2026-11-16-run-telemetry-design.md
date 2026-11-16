# Telemetria de Run (Análise de Balanceamento/Diversão) — Design

## Intenção e porquê

O usuário quer jogar o jogo (foco total no Barbarian, até ele ficar 100% divertido/balanceado)
e depois abrir uma discussão orientada a dados: onde o jogo está monótono, onde está dinâmico,
onde o jogador ganha gold de menos/de mais, se os preços astronômicos dos perks fazem sentido
pro ritmo real de progressão, se tem buracos de monstro (mapa vazio) ou excesso, se o jogador
fica andando sem lutar, etc. Hoje não existe nenhum registro persistente desses dados — o que
existe (`DayStatsTracker`) já reseta todo dia e só guarda totais agregados, sem granularidade de
tempo nem contexto de "quando"/"por quê".

Este script resolve isso: grava, em disco, um JSON bruto e completo de tudo que aconteceu numa
sessão de jogo, pra eu (Claude) analisar junto com o usuário depois, sem precisar reproduzir o
play novamente pra responder uma pergunta nova.

## Goals

- Registrar, por tentativa de dia (dia pode ser retentado várias vezes após morte): monstros
  mortos, hits pra matar cada um (monstro comum e boss, com nome/tipo), gold ganho, e uma série
  temporal a 1Hz com contagem de monstros vivos, posição do herói, HP%, gold, e "há quanto tempo
  sem acertar um hit"/"há quanto tempo sem levar dano".
- Registrar, por visita à Loja: o que foi vendido (item/quantidade/subtotal) e cada perk comprado
  (e desfeito via "Redefine"), com o gold antes/depois e o número do dia que vai começar.
- Produzir 1 arquivo JSON por sessão de jogo (run), completo, que sirva de fonte única pra
  qualquer pergunta de balanceamento que a gente ainda não pensou em fazer hoje.
- Ser trivialmente desligável (1 checkbox no Inspector, sem precisar comentar/remover código).
- Zero impacto no gameplay real (não altera economia, drops, dano — é só leitura/observação).

## Non-goals

- O script NÃO responde "fez sentido comprar esse perk agora?" — ele só registra o contexto
  (gold antes/depois, dia, o que já tinha sido comprado antes). A análise/discussão é feita por
  nós depois, lendo o JSON.
- Não gera relatório em Markdown/texto automático — só o JSON bruto (decisão do usuário: eu
  (Claude) gero o resumo em texto quando formos discutir, assim não preciso reescrever o script
  cada vez que a forma da discussão mudar).
- Não é um sistema de analytics em produção (sem envio pra servidor, sem agregação entre
  sessões/jogadores) — é uma ferramenta de desenvolvimento local, só pra esta fase de
  balanceamento do Barbarian.
- Não tenta persistir/retomar entre lançamentos do jogo — cada vez que a cena de boot carrega,
  nasce 1 arquivo novo (1 sessão = 1 arquivo).

## Anti-goals (seria falha mesmo "funcionando")

- Rodar mesmo desligado (custo zero quando o checkbox estiver desmarcado — nem assina eventos).
- Travar/crashar o jogo por falha de escrita em disco (precisa de try/catch, nunca propagar
  excepção de I/O pro jogo real).
- Perder dados de uma sessão longa por crash do Editor/jogo no meio — por isso o arquivo é
  regravado (overwrite completo, não só apêndice) a cada fronteira natural (fim de dia, fim de
  visita à loja), não só no fim da sessão.

## Constraints encontradas no código

- `GameEvents.cs` (asmdef `Core`) não pode referenciar tipos fora de `Core` (mesma razão pela
  qual `DayStatsTracker`/`PerkState` vivem fora de `Core`, e por isso `OnEnemyKilled` só carrega
  primitivos). O novo evento de "kill detalhado" segue a mesma regra — só primitivos/tipos do
  próprio Unity.
- `EnemyController.Die()` já é o ÚNICO ponto de morte de todo monstro/boss (herdado por todos) —
  é o ponto certo pra reportar hits-até-morrer, sem duplicar lógica por tipo de monstro.
- `PerkState.TryBuy()`/`UndoLastPurchase()` já são os ÚNICOS pontos de compra/desfazer — mesmo
  raciocínio.
- `MainMenuUI.CurrentRun.day` já incrementa (`ShopHandler.ProceedFromShop()`) ANTES da cena de
  gameplay carregar e `GameEvents.OnDayStart` disparar — ler `CurrentRun.day` dentro do handler
  de `OnDayStart` já dá o número do dia NOVO, correto.
- `GameEvents.OnDayEnd` já dispara tanto na sobrevivência quanto na morte — `DayResolver.
  PlayerSurvivedLastDay` (já público) diferencia os dois casos sem precisar tocar em
  `DayResolver.cs`.
- Contagem de monstros/bosses vivos já existe (`WorldPopulationManager.aliveEnemies`,
  `WorldBossSpawnManager.aliveBosses`), só falta um getter público em cada (`AliveCount`).
- `SystemsBootstrap` já é o padrão do projeto pra inicializar managers "globais" (`DayStatsTracker.
  Init()`, `KillGoldReward.Init(goldTable)` etc.) a partir de 1 `[SerializeField]` no Inspector —
  vou seguir o mesmo padrão pro checkbox de ligar/desligar.

## Approach (só existe 1 jeito sensato)

Pura reação a eventos já existentes (`GameEvents`) + um amostrador periódico (1Hz) pra série
temporal (densidade de monstro, tempo ocioso, posição) — que eventos pontuais não conseguem
capturar por natureza. A alternativa ("recalcular tudo via polling pesado todo frame, sem
nenhum gancho novo") foi descartada: não dá pra saber "quantos hits pra matar O Centaur
específico" sem contador por instância em `EnemyController`, então pelo menos esse gancho
pontual é obrigatório; e polling todo frame pra contagem de monstro é desperdício quando o
`WorldPopulationManager` já mantém a lista.

## Decisões já tomadas (pelo usuário)

- Formato: só JSON (sem Markdown automático).
- Frequência da amostragem periódica: 1 vez por segundo.
- Amostra inclui posição (x,y) do herói, pra permitir perguntas espaciais ("fica muito perto do
  Vendor", "explora muito").

## Esquema do JSON (minha proposta, cobre tudo que foi pedido)

```json
{
  "runId": "2026-11-16_193042",
  "hero": "Barbarian",
  "startedAtUtc": "2026-11-16T19:30:42Z",
  "shopVisits": [
    {
      "beforeDayNumber": 4,
      "goldAtOpen": 12000,
      "goldAtClose": 500,
      "itemsSold": [{ "itemName": "Garra de Goblin", "quantity": 12, "subtotalGold": 3600 }],
      "perksPurchased": [{
        "secondsSinceShopOpen": 8.2, "perkId": "barbarian_esquerda_16", "displayName": "Terremoto",
        "rankAfterPurchase": 1, "cost": 25000, "goldBefore": 30000, "goldAfter": 5000
      }],
      "perksUndone": [{ "perkId": "barbarian_esquerda_16", "rankAfterUndo": 0, "refund": 25000 }]
    }
  ],
  "dayAttempts": [
    {
      "dayNumber": 3, "attemptIndex": 2, "outcome": "died",
      "startedAtUtc": "2026-11-16T19:40:00Z",
      "elapsedSeconds": 47.3, "reachedNight": false,
      "creaturesSlain": 9, "bossesDefeated": 0,
      "goldFromMonsters": 1800, "goldFromBosses": 0, "survivalGoldBonusGranted": 0,
      "deathAtSecond": 47.3,
      "kills": [{
        "secondOfDay": 12.1, "enemyName": "Centaur", "isBoss": false,
        "directHitsTaken": 4, "dotTicksTaken": 2, "maxHealth": 120, "energyReward": 20,
        "position": { "x": 14.2, "y": -3.1 }
      }],
      "hitsTakenByPlayer": [{ "secondOfDay": 45.0, "amount": 18.5 }],
      "hitsDealtByPlayer": [{ "secondOfDay": 11.8, "amount": 30, "isDirectHit": true }],
      "snapshots": [{
        "secondOfDay": 20, "aliveMonsters": 6, "aliveBosses": 0,
        "heroHpPercent": 82.4, "heroPosition": { "x": 10.0, "y": 2.0 },
        "gold": 2100, "secondsSinceLastHitDealt": 1.0, "secondsSinceLastHitReceived": 14.0,
        "isNight": false
      }]
    }
  ]
}
```

`attemptIndex` conta quantas vezes aquele NÚMERO de dia específico já foi tentado nesta run
(1ª tentativa, 2ª depois de morrer, etc.) — responde direto "esse dia matou o jogador quantas
vezes" e "quantas mortes no total" (contagem de `outcome == "died"` em todo `dayAttempts`).

`directHitsTaken` (golpe discreto, `isDirectPlayerHit == true`) e `dotTicksTaken` (tick repetido
de área/DoT, ex.: Ground Crack do Barbarian, `isDirectPlayerHit == false`) ficam separados — ver
"Revisão do builder-check" abaixo, item 2. Nunca somados num único "hitsTaken": misturar os dois
enviesaria justo a pergunta "esse monstro toma hit de menos/mais" pra habilidades em teste agora.

## Revisão do builder-check (resolvida antes da aprovação final)

Um agente revisou este documento à parte, cruzando com o código real, e trouxe 3 pontos que
mudariam a arquitetura se eu chutasse errado. Resolvidos assim:

1. **Amostrador de 1Hz não pode ser uma Coroutine crua com `WaitForSeconds`.** O projeto tem um
   padrão repetido (`EnemyController.cs`, `GroundTargetHazard.cs`, `GoblinSapperBomb.cs`,
   `VendorCart.cs`, `ShopHandler.cs` — todos com o mesmo comentário "Coroutine não respeita o
   GameplayGate") de que `GameplayGate` é só uma flag lógica, nunca pausa `Time.timeScale` — uma
   Coroutine continuaria tiquetaqueando durante ESC/Bag abertos, contaminando exatamente a
   métrica "tempo sem lutar"/posição ociosa com tempo de menu. **Resolvido**: o amostrador usa
   timer manual (`Update()` acumulando `Time.deltaTime`), só dispara a amostra quando
   `GameplayGate.IsActive == true`, mesmo padrão já usado no resto do projeto.
2. **"Hits pra matar" não pode misturar golpe discreto com tick de DoT.** `EnemyController.
   TakeDamage()` é chamado tanto por ataques diretos (`isDirectPlayerHit: true`) quanto por
   áreas de dano contínuo como o Ground Crack do próprio Barbarian (`isDirectPlayerHit: false`,
   ver `GroundCrack.cs`) — um monstro morto parcialmente por Ground Crack teria o contador
   inflado por ticks, não por golpes. **Resolvido**: `hitsTakenThisLife` em `EnemyController`
   vira 2 contadores (`directHitsThisLife`/`dotTicksThisLife`), ambos reportados separados no
   `KillRecord` (`directHitsTaken`/`dotTicksTaken`, schema acima) — nenhuma informação se perde.
3. **Voltar ao Menu Principal no meio de um dia (sem morrer/sobreviver) cria um 2º recorder E
   perde a tentativa em andamento.** `RunTelemetryRecorder` nasceria via `AddComponent` no
   `Awake()` de `SystemsBootstrap`, que vive em `DontDestroyOnLoad` — mas "Voltar ao Menu
   Principal" (`PauseMenuUI`) recarrega a cena `Main_Menu`, que tem seu próprio GameObject
   "Systems"/`SystemsBootstrap`, criando um 2º recorder assinando os mesmos eventos estáticos ao
   mesmo tempo que o 1º (duplicaria kills/amostras, colidiria escrevendo o mesmo arquivo); e como
   nem `OnDayEnd` nem `OnDayStart` disparam nesse caminho, a tentativa de dia em andamento nunca
   seria fechada/gravada. **Resolvido (confirmado com o usuário)**:
   - `RunTelemetryRecorder` ganha guarda de singleton de verdade (`static Instance`; se já
     existir um ao nascer, `Destroy()` a cópia nova) — o recorder original (e o `runId`/arquivo
     dele) sobrevive ao reload, nenhum 2º arquivo nasce.
   - Assina a troca de cena pra `Main_Menu` (`SceneManager.sceneLoaded`, ou
     `GameEvents.OnGameStateChanged` se o estado de Menu Principal for exposto por ali) e, se
     houver uma tentativa de dia aberta (ainda sem `OnDayEnd`), fecha ela na hora com
     `outcome: "abandoned"`, grava o JSON, e só então zera o estado pra próxima tentativa.

## O que fica a cargo de quem construir (minhas próprias calls, documentadas aqui)

- **Onde mora o script**: `Assets/Scripts/Telemetry/RunTelemetryRecorder.cs` (pasta nova,
  fora de `Core`, mesma razão de `DayStatsTracker`/`PerkState`). É um `MonoBehaviour` de verdade
  (não só uma classe estática) porque precisa de um `Update()` pra hospedar o timer manual de
  amostragem a 1Hz (ver "Revisão do builder-check", item 1 — não é Coroutine).
- **Como liga/desliga**: `SystemsBootstrap` ganha `[SerializeField] private bool
  telemetryEnabled = true;` (mesmo padrão do `goldTable`) e faz
  `gameObject.AddComponent<RunTelemetryRecorder>().Init(telemetryEnabled)` no `Awake()` — nasce
  junto com os outros managers no GameObject "Systems", sem precisar editar a cena na mão
  (evita risco de fileID — ver memória do projeto sobre edição de cena). Desmarcado = não
  assina NENHUM evento, não roda a Coroutine, não escreve arquivo: custo zero.
- **Onde o arquivo é salvo**: `Application.persistentDataPath/Telemetry/run_<timestamp>.json`
  (mesma convenção de `SaveManager`, pasta própria pra não misturar com o save real).
- **Quando grava em disco**: regrava o JSON inteiro (overwrite) a cada fronteira (fim de dia —
  `OnDayEnd` — e fim de visita à loja — `OnDayStart`), nunca a cada amostra (1x/seg seria
  desperdício de I/O) — protege contra perda de dados em crash no meio de uma sessão longa.
- **Novo evento em `GameEvents.cs`**: `OnEnemyKilledDetailed(string enemyName, bool isBoss, int
  directHits, int dotTicks, float maxHealth, int energyReward, Vector3 position)` — só
  primitivos/Vector3, respeita a regra do asmdef `Core`. Disparado em `EnemyController.Die()`
  ao lado do `OnEnemyKilled` já existente (não substitui, só complementa).
- **Guarda de singleton + sobrevivência ao "Voltar ao Menu Principal"**: ver "Revisão do
  builder-check", item 3 — `static Instance` em `RunTelemetryRecorder`, mais a assinatura de
  troca de cena pra fechar/gravar uma tentativa de dia aberta como `"abandoned"`.
- **Contadores de hits por instância**: campos privados `directHitsThisLife`/`dotTicksThisLife`
  em `EnemyController` (ver "Revisão do builder-check", item 2), incrementados em
  `TakeDamage()` conforme `isDirectPlayerHit`, lidos e resetados implicitamente (nova
  instância sempre começa em 0) — nenhuma mudança em monstros/bosses específicos (todos herdam).
- **Getters novos (read-only, sem mudar comportamento)**: `WorldPopulationManager.AliveCount`,
  `WorldBossSpawnManager.AliveCount`.
- **Hits dados/recebidos pelo jogador**: via `GameEvents.OnDamageTaken` já existente (sem
  modificar `HeroController`/`EnemyController` além do contador acima).
- **Nome do monstro**: `gameObject.name` sem o sufixo `(Clone)` (removido na hora de gravar, só
  no `RunTelemetryRecorder`, sem mexer em `EnemyController`).
- **HP% do herói na amostra**: se `HeroController` não expuser health/maxHealth publicamente o
  suficiente, adiciono um getter read-only (`HealthPercent01`) — mesmo padrão de pequena adição
  usado nesta sessão pra outros fixes.
- **Falhas de I/O**: `try/catch` ao redor de toda escrita, só `Debug.LogWarning`, nunca lança
  exceção pro resto do jogo.

## Fora de escopo explícito (podemos adicionar depois se fizer falta)

- Posição/estado de bosses individuais nas amostras (só a contagem) — dá pra adicionar depois.
- Qual monstro especificamente desferiu o hit que o jogador recebeu (hoje `OnDamageTaken` não
  carrega essa informação; adicionar exigiria mudar a assinatura usada por todo monstro do jogo).
