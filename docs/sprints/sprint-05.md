# Sprint 05 — Floor System Skeleton

## Objetivo

Ground + 2 Floors placeholder dentro da mesma Scene (GDD Seção 24 — sem carregar Scene nova ao trocar de andar), com o conceito de "Current Floor" sendo identificado corretamente. Sem travessia real ainda — teleporte manual só para provar a detecção.

## Sistemas adicionados

- **`FloorDefinition`** — dado por Floor (nome, Original Floor Identity, Active Floor Position), seguindo a terminologia exata do GDD Seção 24.
- **`FloorManager`** — singleton com o `CurrentFloor` ativo, atualizado via `SetCurrentFloor` (idempotente — não reprocessa se já for o Floor atual).
- **`FloorTrigger`** — detecta o Player entrando na região de um Floor via `Collider2D` (Is Trigger) e notifica o `FloorManager`.
- 3 regiões físicas na `_TestScene`: `Floor_Ground` (0,0), `Floor_1` (0,50), `Floor_2` (0,100) — Original Floor Identity e Active Floor Position começam idênticos, só divergem quando Remove Tower Layer existir (Deadline 12).

## Decisões técnicas

- **Trigger 2D exige `Rigidbody2D` em pelo menos um dos dois lados da colisão** — na primeira tentativa, nenhum log aparecia mesmo com os `Collider2D` e `Is Trigger` corretos, porque nem Player nem os Floors tinham `Rigidbody2D`. Unity não gera eventos de trigger 2D sem isso, independente de outras configurações estarem certas. Corrigido adicionando `Rigidbody2D` (Body Type = **Kinematic**, já que o Player se move via `transform.Translate` no `PlayerInputTest`, não por força física — Dynamic aplicaria gravidade e derrubaria o Player).
- **Scripts de teste (`TestTimer`, `SaveTest`, `LargeNumberTest`, `LocalizationTest`, `FloorTeleportTest`) foram consolidados pelo usuário num GameObject `Tests` separado**, dentro de `//SYSTEMS`, distinto do `Systems` "real" (que mantém só `TimeManager`/`GameStateManager`/`SystemsBootstrap`). Como `TestTimer` só lê `TimeManager.Instance` (não precisa estar no mesmo GameObject), essa reorganização não quebra nada — é só uma separação mais clara entre sistemas de produção e botões de teste manual.
- **Nenhum teste automatizado nesta sprint** — o comportamento validado (colisão de trigger, posição física na Scene) é fundamentalmente runtime; um teste EditMode "só para ter teste" não validaria nada de real. Volta na Sprint 6 com o cálculo de roteamento (lógica pura, testável).

## Arquivos/classes principais

- `Assets/Scripts/Core/FloorDefinition.cs`, `FloorManager.cs`, `FloorTrigger.cs` — núcleo do Floor System (em `Core.asmdef`).
- `Assets/Scripts/FloorTeleportTest.cs` — teleporte manual de validação.

## Eventos adicionados

Nenhum (`GameEvents` não ganhou eventos novos — a detecção de Floor ainda não dispara nada centralizado).

## Testes executados

Nenhum automatizado (ver "Decisões técnicas"). Manual (Play Mode): teleportar para Ground/Floor 1/Floor 2 loga corretamente `[FloorManager] Current Floor = ... (Original X, Active Position X)` para os 3, sem repetir o log ao reentrar no mesmo Floor duas vezes seguidas.

## Bugs conhecidos

Nenhum em aberto — o problema do `Rigidbody2D` ausente foi identificado e corrigido durante a própria sprint.

## Dívida técnica

Nenhuma nova nesta sprint. (A dívida em aberto do Stair Routing simplificado é da Sprint 6, ainda não existe.)

## Próximos passos

Com a detecção de Floor funcionando, a Sprint 6 substitui o teleporte manual por escadas reais, roteando por Active Floor Position relativa (não índice fixo) — pré-requisito documentado para Remove Tower Layer (Deadline 12) funcionar sem retrabalho, com validação em escala real exigida até a **Deadline 8 (Sprint 32)**.
