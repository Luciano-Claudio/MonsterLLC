# Sprint 04 — Localization + Save/RunState + Large Number + Docs Pipeline Maduro

## Objetivo

Fechar os 3 últimos esqueletos fundacionais da Deadline 1 (Localization, Save/RunState, Large Number) e avaliar a maturidade do pipeline de documentação.

## Sistemas adicionados

- **Localization** — pacote configurado com 2 locales (`en`, `pt-BR`), uma String Table Collection (`UI Text`) com uma entrada de teste (`test.hello`), e `LocalizationTest.cs` trocando o locale ativo via `ContextMenu`.
- **Save/RunState skeleton** — `RunState` (dado puro serializável) + `SaveManager` (grava/lê `RunState` como JSON em `Application.persistentDataPath`), validados via `SaveTest.cs`.
- **Large Number Abstraction** — `LargeNumberFormatter`, notação k/m/b/t sobre `double`, coberto por 3 testes automatizados.

## Decisões técnicas

- **`double` como tipo de valor econômico**, não um BigNumber customizado. O GDD (Seção 48) deixa o tipo de dado como decisão técnica, exigindo só a notação k/m/b/t sem teto fixo — `double` cobre confortavelmente até trilhões (~15-17 dígitos significativos), e por estar isolado em `LargeNumberFormatter`/`Core.asmdef`, uma troca futura (caso precisão vire problema real em valores extremos) fica contida numa única classe, não espalhada pelo projeto.
- **Pipeline de docs via DocFX + GitHub Actions avaliado e descartado.** O plano original desta sprint incluía migrar a publicação de `docs/` para DocFX (extraindo também documentação de API a partir de `Assets/Scripts`), rodando via GitHub Actions. Testado localmente antes do commit (seguindo a diretriz da Sprint 3 de validar antes de confiar em config gerado): o passo de extração de API do DocFX compila os `.cs` via Roslyn **sem nenhuma referência à `UnityEngine.dll`**, e todo script que usa `MonoBehaviour`/`Debug`/etc falha (`CS0246`) — o processo inteiro sai com erro (exit != 0). Isso é crítico porque, no workflow proposto, uma falha nesse passo interrompe o pipeline **antes** dos passos de deploy — ou seja, o primeiro push quebraria a publicação do site inteiro, não só a seção de API ausente. A causa não é corrigível só localmente: o runner do GitHub Actions (`ubuntu-latest`) não tem Unity instalada, então não existe `UnityEngine.dll` nenhuma para referenciar lá — resolver isso de verdade exigiria rodar a própria Unity dentro do CI (licença, minutos de build) só para gerar documentação de API que, num projeto solo, ninguém navega fora do próprio Editor. **Decisão: manter o pipeline atual** (GitHub Pages "Deploy from a branch", Jekyll sem tema, publicando `docs/` automaticamente a cada push em `main` desde a Sprint 1) — já é maduro o suficiente para o projeto. `docfx_project/` e o workflow associado não entraram no repositório.
- **`RunState`/`SaveManager`/`LargeNumberFormatter` moram em `Assets/Scripts/Core/`**, caindo automaticamente no `Core.asmdef` da Sprint 3 — nenhum ajuste de assembly reference foi necessário desta vez (diferente da Sprint 3), confirmando que o padrão "scripts puros em `Core/`" já é estável.

## Arquivos/classes principais

- `Assets/Scripts/LocalizationTest.cs` — troca de locale de teste.
- `Assets/Scripts/Core/RunState.cs`, `SaveManager.cs` — dado + serialização de save.
- `Assets/Scripts/SaveTest.cs` — validação manual de save/load.
- `Assets/Scripts/Core/LargeNumberFormatter.cs` — formatação k/m/b/t.
- `Assets/Scripts/LargeNumberTest.cs` — validação manual de formatação.
- `Assets/Localization/`, `Assets/Localization Settings.asset`, `Assets/AddressableAssetsData/` — assets gerados pelo pacote de Localization (Locales, String Table Collection, config do Addressables por trás).

## Eventos adicionados

Nenhum (`GameEvents` não ganhou eventos novos nesta sprint).

## Testes executados

- **Automatizado (EditMode):** `LargeNumberFormatterTests` — 3 testes (`Format_BelowThousand_ReturnsPlainNumber`, `Format_Million_ReturnsMSuffix`, `Format_Trillion_ReturnsTSuffix`). **Todos passaram**, junto com o teste da Sprint 3 (`ProgressTrackerTests`), sem nenhum ajuste manual de `asmdef`.
- **Manual (Play Mode):** troca de locale loga `[Localization] Hello, Tower` / `[Localization] Olá, Torre`; `SaveTest` grava e recupera `day=5, gold=1200, hero=Barbarian`; `LargeNumberTest` loga os 5 valores esperados (`850`, `1.5k`, `2.3m`, `4b`, `1t`).

## Bugs conhecidos

Nenhum em aberto.

## Dívida técnica

- **API Documentation automática (DocFX) não existe.** Avaliada e descartada nesta sprint (ver "Decisões técnicas") — não deve ser retentada sem antes resolver como gerar referências reais da Unity num ambiente de CI sem o Editor instalado.
- `RunState` ainda é um subconjunto mínimo do que o GDD (Seção 43) define para o save real — falta weapon tier, bonuses, employees, quests, cards, Floor Variants sorteados, Remove Tower Layers, e a distinção `LastCompletedDay`/`NextDay`. Esperado nesta fase (skeleton); precisa crescer junto com cada sistema correspondente.
- `ProgressTracker` (Sprint 3) continua sem os três escopos formais que o GDD (Seção 49) define (Daily/Run/Lifetime) nem o gate de avaliação por Mode (Padrão vs. Free) — não tocado nesta sprint, segue como debt pré-existente.

## Próximos passos

Com Localization, Save/RunState e Large Number no lugar, a Deadline 1 (Fundação Técnica Completa) está encerrada. A Sprint 5 inicia a Deadline 2 com o Floor System Skeleton.
