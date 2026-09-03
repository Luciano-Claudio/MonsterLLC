# Sprint 12 — Save Real + Continue Game + Ciclo de Dia Completo

## Objetivo

Fecha o Primeiro Vertical Slice: `New Game → Dia 1 → combate → venda → demanda → Results → Loja → compra → Save real no checkpoint → Start Day 2`, sem intervenção manual em nenhum ponto do meio. Última sprint da Deadline 3 — ver fechamento da Deadline no fim deste relatório.

## Sistemas adicionados

- **`RunState.weaponTier`** (stub) — não é o sistema real de Weapon Tier (15 tiers, Deadline 9/Sprint 33), só o suficiente pra provar que uma compra na Loja sobrevive ao Save/Load.
- **Save real** — `SaveManager.Save()` passa a ser chamado de verdade pela primeira vez no projeto.
- **`ShopHandler.BuyNextTier()`** (stub Basic/Copper/Iron, preços placeholder) — prova o loop de compra sem implementar a economia real.
- **`MainMenuUI.ContinueGame()` corrigido** — abre a Loja do checkpoint (`GameState.Shop`), não mais o Gameplay direto.

## Decisões técnicas

- **`SaveManager.Save()` foi para o `ResultsHandler`, não o `ShopHandler` como o texto original propunha.** A justificativa original ("só existe um caminho pra chegar em `GameState.Shop`, então a regra de não sobrescrever sai de graça") deixou de ser verdadeira dentro da própria sprint: o passo que corrige `ContinueGame()` cria um segundo caminho pro estado `Shop` (o checkpoint carregado). Um `Save()` incondicional em "sempre que entrar em Shop" dispararia de novo ao dar Continue Game, contrariando a GDD Seção 43 ("autosave ocorre **somente** ao entrar na Loja, **imediatamente após o encerramento normal de um dia** — nunca durante o dia"). Amarrando o `Save()` ao `ResultsHandler` (que só reage a `GameState.Results`, que só é alcançado via `DayResolver` quando a demanda foi cumprida), a regra volta a ser garantida pela própria estrutura do código, não por coincidência de haver um único caminho.
- **Expectativa do teste manual da Parte B corrigida.** Como o save só acontece uma vez (na transição Results→Shop, antes de qualquer compra ou de "Start Next Day"), o checkpoint da Parte A ficou em **Dia 1, weaponTier Basic** — não "Dia 2, weaponTier Copper" como o texto original esperava. Compra e avanço de dia só existem em memória até o *próximo* dia fechar com sucesso; não foi adicionado (nem pedido) nenhum save extra ao comprar ou ao avançar o dia — confirmado na prática pelo teste manual do usuário, que bateu exatamente com o valor corrigido.
- **Nenhuma mudança de Scene nesta sprint** — todos os scripts tocados (`ResultsHandler`, `ShopHandler`, `MainMenuUI`) já estavam anexados desde sprints anteriores; `RunState`/`RunCreation`/`SaveManager` são lógica pura, sem componente.

## Arquivos/classes principais

- `Assets/Scripts/Core/Save/RunState.cs`, `RunCreation.cs` — campo `weaponTier`.
- `Assets/Scripts/World/ResultsHandler.cs` — `SaveManager.Save()` real.
- `Assets/Scripts/World/ShopHandler.cs` — `BuyNextTier()`.
- `Assets/Scripts/UI/MainMenuUI.cs` — `ContinueGame()` abre `GameState.Shop`.

## Eventos adicionados

Nenhum evento novo — `GameEvents.GoldChanged` (Sprint 10) já cobre a compra de tier.

## Testes executados

- **Automatizado (EditMode):** `SaveManagerTests.SaveAndLoad_RoundTripsCorrectly` — passou, junto com os 27 já existentes (28 no total). Este teste escreve/lê um `save.json` real em disco (não é isolado como os demais testes EditMode do projeto) — decisão deliberada, é exatamente o mecanismo que precisa estar certo. Rodado **antes** da sequência manual, pra não sobrescrever o save real com os dados sintéticos do teste no meio do fluxo.
- **Manual (Play Mode), Parte A — loop principal:** New Game → Dia 1 → matou/coletou/vendeu 40x Monster Essence (a 2 gold cada, 80 gold total) → `DayResolver` cumpriu a demanda → Results → **save automático confirmado no log e no disco** (`Dia 1, Gold 80, weaponTier Basic`) → Shop → `Buy Next Weapon Tier` (Copper, -50 gold) → `Start Next Day` (Dia 2, em memória).
- **Manual, Parte B — "New Game não sobrescreve":** saiu do Play Mode, New Game de novo (run nova), deixou o tempo zerar sem bater a demanda (Game Over → Menu, save não tocado) → `Continue Game` carregou corretamente **Dia 1, Gold 80, weaponTier Basic** (o checkpoint da Parte A) e não a run que acabou de falhar, confirmando a regra da GDD Seção 43 na prática — e abriu a Loja (`GameState.Shop`), não o Gameplay direto.
- **Manual, Parte C — "Continue sem save":** `save.json` apagado manualmente → `Continue Game` logou "indisponível — nenhum save encontrado", sem quebrar e sem criar arquivo novo.

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **Herdada (Sprint 11), agora atribuída: nada ainda pausa/congela o mundo quando o estado sai de `GameState.Gameplay`.** Ficou 2 sprints sem sprint formal atribuída no plano de produção; o usuário decidiu resolver dentro da **Sprint 13** (Enemy Framework Genérico) — faz sentido técnico além de calendário: a Sprint 13 já reescreve o `Update()` do inimigo (generalizando `MeleeEnemyPrototype` pra base Melee/Ranged) e provavelmente toca o `HeroController` também, então é o momento mais barato de trocar o guard isolado de `TimeManager.IsPaused` por um guard combinado (`IsPaused` **e** `GameStateManager.CurrentState == GameState.Gameplay`) em vez de continuar remendando isso disperso pelo código.
- **Ligada à dívida acima — reavaliar o `Respawn()` incondicional de `HeroController.OnDeath()` (Sprint 11) quando a Sprint 13 implementar o gating.** A decisão de sempre respawnar (mesmo quando a penalidade zera o dia) foi um patch em cima da lacuna de gating, não a regra final do GDD Seção 11 (que literalmente diz "sem respawn" nesse branch). Com o gating existindo, o jogador não vai nem ver o personagem nesse estado — nesse ponto vale reconsiderar se `Respawn()` incondicional continua necessário ou se dá pra voltar à ramificação literal da GDD. Não decidido ainda, só marcado para não esquecer (pedido explícito do usuário).
- Herdada (Sprint 9/11): prioridade entre interagíveis simultâneos (Sprint 35).
- Compra na Loja (`BuyNextTier`) e "Start Next Day" não geram save adicional — só o snapshot do momento exato Results→Shop é persistido, por design (GDD Seção 43, "nunca durante o dia"). Se o jogador fechar o jogo *depois* de comprar mas *antes* de fechar o próximo dia com sucesso, a compra é perdida. Isso é uma leitura literal da GDD, não um bug — mas vale confirmar com o usuário se esse é o comportamento pretendido antes de a Loja virar sistema real (Deadline 9), já que hoje não há nenhuma tela de Loja de verdade para comunicar esse risco ao jogador.
- Weapon Tier é stub (Basic/Copper/Iron, preços fixos) — sistema real de 15 tiers com multiplicadores de dano/vida é Deadline 9, Sprint 33.

## Próximos passos — fecha a Deadline 3

Com Save real, Continue Game correto e o ciclo de dia validado de ponta a ponta (sucesso e falha), a **Deadline 3 (Primeiro Vertical Slice, Sprints 9–12) está encerrada**: o jogo agora sustenta uma run completa — Menu, criação de run, combate, loot, venda, demanda, resultado, loja mínima e checkpoint real — sem nenhum passo manual escondido no meio. A Deadline 4 (Enemy Framework + Floor Sleep v1, Sprints 13–16) começa a expandir o combate para uma base genérica de inimigos (Melee/Ranged configuráveis) e a simulação seletiva por Floor, deixando o vertical slice atual como fundação em vez de protótipo descartável. A Sprint 13 também carrega a dívida de gating de `GameState.Gameplay` (ver "Dívida técnica" acima) — atribuída ali por já mexer no `Update()` dos inimigos e do herói.
