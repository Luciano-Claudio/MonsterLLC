# D9.5 (enxuto) — morte encerra a run + transições (design curto)

**Data:** 2026-11-21. **Contexto:** o rework e a sequência dos bosses ficam por último na Deadline 9. O D9.5 entra agora só com a estrutura da run. O painel de resumo (`ResumePainelUI`) fica como está; o usuário vai reformular depois.

## Decisões do usuário

1. **A morte encerra a run.** Sem tela nenhuma: depois da animação de morte, toca uma transição e volta pro menu. Sai o retry do mesmo dia.
2. **As transições do Easy Transitions voltam:**
   - **Morte → menu:** **CircleWipe** completo, que fecha e abre já no menu.
   - **Menu → loading:** a **primeira metade do Fade**: o menu escurece até o preto, e aí aparece a tela de loading.
   - **Loading → jogo:** a **segunda metade do Fade**: a tela fica preta e o jogo clareia a partir dela.
3. O dia sobrevivido continua indo pra Resultados e depois pra Loja até o D9.6. Gold permanente (D9.9) e vitória no Divine God (bosses) ficam pra depois.

## Abordagens

| Abordagem | Decisão |
|---|---|
| **Morte com o `TransitionManager` da `_TestScene`** (`Transition("Main_Menu", CircleWipe)`, troca de cena nativa do pacote) **+ um `ScreenFader` próprio, que persiste entre cenas e toca as metades do Fade** (`FadeIN`/`FadeOUT` do pacote) separadas. | **Escolhida.** Cada transição faz exatamente a metade pedida, e a cena do menu não precisa mudar. |
| Um `TransitionManager` persistente pra tudo. | Descartada: o pacote sempre toca as duas metades juntas (cobre e, na troca de cena, revela). |
| Um `TransitionManager` em cada cena. | Descartada: a transição menu → loading atravessa a troca de cena, e a peça que a disparou é destruída no meio. |

## Design

- **Configuração:** `TransitionsConfig` (ScriptableObject) em `Assets/Resources/TransitionsConfig.asset`, com `fade` (= `Fade.asset`) e `death` (= `CircleWipe.asset`). Carregado por `Resources.Load`, então não precisa editar cena.
- **`ScreenFader`** (`Assets/Scripts/Transitions/ScreenFader.cs`):
  - é criado sob demanda e persiste entre cenas (`DontDestroyOnLoad`), com um Canvas overlay acima de tudo;
  - `FadeToBlack(TransitionSettings, Action onBlack)`: toca o prefab IN; ao fim, segura o preto e chama `onBlack`;
  - `SnapToBlack(TransitionSettings)`: preto na hora (o IN no último frame);
  - `FadeFromBlack(TransitionSettings, Action onClear)`: troca pelo prefab OUT; ao fim, remove e chama `onClear`;
  - `ClearInstant()`;
  - a duração vem do `TransitionSettings` (`transitionTime / transitionSpeed`), e o tempo conta em tempo real (`unscaled`).
- **Menu → loading** (`MainMenuUI.LoadGameplayScene`): `FadeToBlack(fade)`; no preto, `LoadingScreenUI.LoadScene(...)` e `ClearInstant()`. A tela de loading aparece direto.
- **Loading → jogo** (`GameplayBootstrap.HideLoadingScreenWhenWorldReady`): com o mundo pronto, `SnapToBlack`, depois `LoadingScreenUI.Hide()` e `FadeFromBlack(fade)`. **`SetState(Gameplay)` só no fim do clarear**, então o cronômetro e o spawn não andam durante o fade.
- **Continue (loading → Loja):** mesmo clarear a partir do preto, sem mudar o estado. *(Decisão minha.)*
- **Morte** (`HeroController.AnimationDieEndEvent` → `DayResolver.EndRunAfterDeath()`, no lugar de `OpenResultsAfterDeath`):
  - `SaveManager.Delete()`, porque a run acabou e o "Continue" não pode trazê-la de volta;
  - `PlayerSurvivedLastDay` volta a `true`, o estado certo pra uma run nova;
  - `TransitionManager.Instance().Transition("Main_Menu", death, 0)`;
  - sem `TransitionManager` na cena, vai pro menu sem transição.
- **Teste:** EditMode pro `SaveManager.Delete`. O resto é checado no jogo: as transições são visuais.

## Fora do escopo

Reformular o painel de resumo, tirar a Loja (D9.6), gold permanente (D9.9), vitória e bosses.

## Decisões minhas (o usuário confere)

- A tela de loading aparece direto quando o menu fica preto, sem fade próprio.
- No Continue, a Loja também aparece clareando a partir do preto.
- A configuração dos efeitos fica num asset em `Resources`, pra não mexer em cena.
- O caminho de retry da Loja (`ShopHandler`, com `PlayerSurvivedLastDay` falso) fica sem uso; a limpeza dele fica pro D9.6, que reescreve a Loja.
