# Sprint 01 — Fundação Técnica

## Objetivo

Ter o projeto Unity inicializado, o repositório Git configurado corretamente (LFS, `.gitignore`, build settings), e a documentação pública (GitHub Pages) no ar com estrutura navegável.

## Sistemas adicionados

Nenhum sistema de gameplay — esta sprint é 100% fundação técnica e infraestrutura.

## Decisões técnicas

- **GitHub Pages com Jekyll sem tema** (`docs/_config.yml` com `theme: null`): o build padrão do GitHub Pages ("Deploy from a branch") quebrava com `Error: No such file or directory @ dir_chdir0` ao tentar renderizar o SCSS do tema `jekyll-theme-primer` — bug conhecido da plataforma quando o `Source` é uma subpasta (`/docs`). Desativar o tema evita essa etapa de renderização por completo, sem precisar migrar para o builder via GitHub Actions.
- **`_TestScene.unity` nasceu de um rename da `SampleScene` original, não de uma cena nova do zero** — durante a validação, a cena vazia criada à mão foi substituída por uma renomeação feita na própria Unity (preserva o GUID original e garante um arquivo `.unity` 100% gerado/validado pelo Editor, em vez de um YAML montado manualmente).
- **Git LFS com escopo restrito**: `.gitattributes` cobre apenas formatos-fonte pesados reais (`.psd`, `.aseprite`, `.wav`, `.mp4`, `.mov`, `.fbx`, `.exr`) — deliberadamente sem `.png`/`.jpg` em bloco, para não tratar todo asset visual como LFS por padrão.

## Arquivos/classes principais

- `docs/index.md`, `docs/gdd/index.md` — Home e GDD publicados no site.
- `docs/guides/project-setup.md`, `docs/guides/git-workflow.md` — guias de onboarding e de fluxo Git.
- `README.md` — porta de entrada do repositório.
- `Assets/Scenes/_TestScene.unity` — cena de teste registrada no Build Settings (índice 0).
- `.gitattributes` — configuração de Git LFS.
- `ProjectSettings/EditorBuildSettings.asset` — Build Settings apontando para `_TestScene`.

## Eventos adicionados

Nenhum (sem gameplay nesta sprint).

## Testes executados

Nenhum teste automatizado (Test Framework só entra na Sprint 3). Validação manual: `_TestScene` abre sem erro no Editor, aparece corretamente no Build Settings; `git lfs ls-files` confirma `_lfs-test.psd` rastreado pelo LFS.

## Bugs conhecidos

Nenhum em aberto no código — o bug do GitHub Pages (`dir_chdir0`) foi contornado via `_config.yml`.

## Dívida técnica

- **S1-T05** (`.gitignore` "definitivo" como task formal) nunca rodou como branch/commit dedicado — o `.gitignore` do bootstrap inicial já cobria o essencial e foi considerado suficiente na prática.
- **S1-T07** (proteção de branch `main` no GitHub) não foi executado nesta sessão — não confirmado se está ativo.
- **S1-T12** (Clean Clone Validation) e **S1-T13** (Build Smoke Test) não foram executados.
- Branches já mescladas via PR (`chore/project-settings`, `docs/github-pages-skeleton`) continuam existindo local e remotamente — nunca foram deletadas.
- A infraestrutura de Sprint Reports (`docs/sprints/`, este arquivo) só foi criada retroativamente, depois da Sprint 3 já estar pronta — não durante a própria Sprint 1 como o plano original previa (S1-T08/S1-T14).

## Próximos passos

Com o projeto, o Git e a documentação de pé, a Sprint 2 pôde focar em gameplay real (Input System) sem se preocupar com fundação.
