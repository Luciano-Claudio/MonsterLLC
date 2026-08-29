# Sprint 1 — Task Breakdown Executável

> Escopo, objetivo e Definition of Done da Sprint permanecem exatamente como aprovados. Este documento substitui apenas a seção de execução (blocos A–D) por tasks individuais rastreáveis.

## Correções incorporadas nesta versão
- Layers/Sorting Layers/Tags de gameplay **removidos** desta Sprint — pertencem à Sprint 2.
- Estrutura de `Assets/` reduzida ao indispensável para o bootstrap atual (não a árvore de 11 pastas da versão anterior).
- Versão exata do Unity Editor registrada: **Unity 6000.0.35f1 LTS**.
- Git LFS restrito a arquivos-fonte/pesados reais (`.psd`, `.aseprite`, `.wav`, `.mp4`, `.fbx` etc.) — **sem** `*.png`/`*.jpg` em bloco.
- Estratégia de branch solo-dev: `main` + `chore/*`, `docs/*`, `fix/*` (e `feature/*` a partir da Sprint 2). **Sem `develop`.**
- Nenhum trabalho de configuração/documentação feito diretamente em `main` (exceto o seed commit inicial, que por definição não tem branch anterior de onde nascer, e o merge/tag final de fechamento).
- Tasks explícitas adicionadas: Project Setup Guide, Git Workflow doc, Clean Clone Validation, Build Smoke Test, Sprint 1 Closure.
- GitHub Pages permanece simples (Markdown estático via `/docs`) — DocFX/GitHub Actions são escopo da Sprint 4, não antecipados aqui.

---

## S1-T01 — Criar o Projeto Unity (bootstrap local)

**Importância:** Crítica
**Estimativa:** S
**Dependências:** Nenhuma

**Objetivo:** Ter um projeto Unity local funcional, na versão e template corretos, antes de qualquer versionamento.

**Passo a passo:**
1. Instalar (ou confirmar instalada) a versão **Unity 6000.0.35f1 LTS** via Unity Hub.
2. Criar novo projeto: template **2D (URP)**, nome `ProjetoTorre`, localização de pasta definida localmente.
3. Abrir o projeto uma vez para confirmar que ele inicializa sem erro no Console.
4. Fechar o Unity antes de prosseguir para o Git (evitar arquivos de lock abertos).

**Arquivos/pastas envolvidos:** raiz do projeto (`Assets/`, `Packages/`, `ProjectSettings/`, `Packages/manifest.json` gerados automaticamente pelo template).

**Resultado esperado:** Projeto Unity abre localmente sem erro, ainda sem Git.

**Critérios de aceitação:**
- [ ] Unity Hub mostra a versão 6000.0.35f1 LTS associada ao projeto.
- [ ] Projeto abre sem erro no Console.

**Como validar/testar:** Abrir o projeto pelo Unity Hub, checar o Console (Window → General → Console) — deve estar vazio de erros.

**Documentação a atualizar:** nenhuma ainda.

**Branch exata:** — (pré-Git, trabalho local apenas)

**Commit(s):** — (nenhum commit nesta task)

**Done quando:** projeto abre localmente sem erro, pronto para inicializar o Git.

---

## S1-T02 — Inicializar Git + Seed Commit + Push para o Remoto

**Importância:** Crítica
**Estimativa:** S
**Dependências:** S1-T01

**Objetivo:** Colocar o projeto sob controle de versão e publicá-lo no GitHub como ponto de partida único e reprodutível.

**Passo a passo:**
1. Na raiz do projeto, rodar `git init`.
2. Criar `.gitignore` mínimo temporário (será substituído/expandido pela S1-T05; nesta task, incluir ao menos `/Library/`, `/Temp/`, `/Obj/`, `/Logs/`, `/UserSettings/` para não versionar lixo já na primeira foto).
3. `git add .`
4. `git commit -m "chore: initialize Unity project (Unity 6000.0.35f1 LTS, URP 2D template)"`
5. Criar o repositório remoto no GitHub (nome `projeto-torre` ou equivalente final), visibilidade **privada**.
6. `git branch -M main`
7. `git remote add origin <url-do-repositorio>`
8. `git push -u origin main`

**Arquivos/pastas envolvidos:** toda a raiz do projeto (primeira foto), `.gitignore` (versão mínima).

**Resultado esperado:** Repositório remoto no GitHub com 1 commit em `main`, refletindo o projeto Unity recém-criado.

**Critérios de aceitação:**
- [ ] `git log` mostra exatamente 1 commit em `main`.
- [ ] Repositório remoto acessível no GitHub com o mesmo conteúdo.
- [ ] `/Library/` e `/Temp/` **não** aparecem no commit.

**Como validar/testar:** `git status` limpo após o push; conferir no GitHub web que `Library/` não está listado na árvore de arquivos.

**Documentação a atualizar:** nenhuma ainda.

**Branch exata:** `main` (commit inicial — não há branch anterior da qual nascer).

**Commit(s):**
```
chore: initialize Unity project (Unity 6000.0.35f1 LTS, URP 2D template)
```

**Done quando:** o push para `origin main` é confirmado e o repositório é visível no GitHub.

---

## S1-T03 — Configurar Player/Render Pipeline/Quality/Time/Physics/Audio Settings

**Importância:** Alta
**Estimativa:** M
**Dependências:** S1-T02

**Objetivo:** Deixar as configurações centrais do projeto explícitas e intencionais, em vez dos defaults genéricos do template.

**Passo a passo:**
1. `git checkout -b chore/project-settings`
2. Player Settings: definir Company Name e Product Name; ícone placeholder (pode ser o ícone default do Unity por enquanto, só documentado como placeholder); Fullscreen Mode = Windowed (ambiente de desenvolvimento); Default Screen Width/Height = 1920×1080 como referência.
3. Confirmar o URP Asset ativo usa 2D Renderer; Post-Processing desabilitado por padrão; HDR desligado.
4. Quality Settings: reduzir para 1 nível único de qualidade; VSync ligado.
5. Time Settings: Fixed Timestep = 0.02.
6. Physics 2D Settings: Gravity Y = 0.
7. Audio Settings: manter defaults do Unity (nenhuma mudança necessária nesta sprint).
8. Salvar o projeto, reabrir e confirmar que as configurações persistiram.
9. `git add ProjectSettings/`
10. `git commit -m "chore: configure player, render pipeline, quality, time, physics and audio settings"`
11. `git push -u origin chore/project-settings`
12. Abrir PR (ou, se preferir merge local direto por ser solo dev, `git checkout main && git merge --no-ff chore/project-settings`) após validação visual das configurações.
13. `git push origin main`
14. Apagar a branch local e remota após merge (`git branch -d chore/project-settings`, `git push origin --delete chore/project-settings`).

**Arquivos/pastas envolvidos:** `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/QualitySettings.asset`, `ProjectSettings/TimeManager.asset`, `ProjectSettings/DynamicsManager.asset` (Physics2D), `ProjectSettings/AudioManager.asset`, o URP Asset em `Assets/Settings/` (caminho exato depende de onde o template 2D URP o colocou).

**Resultado esperado:** projeto configurado de forma intencional e documentável, sem nenhuma mudança de gameplay ainda.

**Critérios de aceitação:**
- [ ] Gravity Y = 0 confirmado em Physics 2D.
- [ ] Fixed Timestep = 0.02 confirmado.
- [ ] Quality Settings reduzido a 1 tier.
- [ ] URP 2D Renderer ativo, Post-Processing off, HDR off.

**Como validar/testar:** reabrir o projeto após o commit e conferir cada valor manualmente nos respectivos painéis de Project Settings.

**Documentação a atualizar:** nenhuma nesta task (será referenciada pelo Project Setup Guide na S1-T10).

**Branch exata:** `chore/project-settings` (mesclada em `main` ao final da task).

**Commit(s):**
```
chore: configure player, render pipeline, quality, time, physics and audio settings
```

**Done quando:** branch mesclada em `main`, push feito, branch deletada local e remotamente.

---

## S1-T04 — Configurar Build Settings + Test Scene

**Importância:** Alta
**Estimativa:** S
**Dependências:** S1-T03

**Objetivo:** Ter uma Scene mínima válida e o alvo de build definido, pré-requisito para qualquer teste de build futuro.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b chore/build-settings`
3. Criar `Assets/Scenes/_TestScene.unity` (Scene vazia, sem lógica).
4. File → Build Settings → Platform: **PC, Mac & Linux Standalone**, Target Platform: **Windows**, Architecture: x86_64.
5. Adicionar `_TestScene` à lista de Scenes In Build (índice 0).
6. Salvar.
7. `git add Assets/Scenes/ ProjectSettings/EditorBuildSettings.asset`
8. `git commit -m "chore: configure build settings and add test scene"`
9. `git push -u origin chore/build-settings`
10. Merge em `main` após validação (mesmo fluxo da S1-T03), push, deletar branch.

**Arquivos/pastas envolvidos:** `Assets/Scenes/_TestScene.unity`, `ProjectSettings/EditorBuildSettings.asset`.

**Resultado esperado:** Scene de teste existe e está registrada no Build Settings, alvo Windows configurado.

**Critérios de aceitação:**
- [ ] `_TestScene.unity` existe em `Assets/Scenes/`.
- [ ] Build Settings mostra `_TestScene` no índice 0.
- [ ] Target Platform = Windows x86_64.

**Como validar/testar:** abrir Build Settings e conferir visualmente a lista de Scenes e a plataforma selecionada.

**Documentação a atualizar:** nenhuma nesta task.

**Branch exata:** `chore/build-settings` (mesclada em `main`).

**Commit(s):**
```
chore: configure build settings and add test scene
```

**Done quando:** branch mesclada, `_TestScene` presente e registrada, branch deletada.

---

## S1-T05 — Configurar `.gitignore` Definitivo

**Importância:** Crítica
**Estimativa:** S
**Dependências:** S1-T02

**Objetivo:** Substituir o `.gitignore` mínimo temporário da S1-T02 por uma versão completa e correta para Unity.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b chore/gitignore`
3. Substituir `.gitignore` por uma versão completa cobrindo: `/Library/`, `/Temp/`, `/Obj/`, `/Build/`, `/Builds/`, `/Logs/`, `/UserSettings/`, `/MemoryCaptures/`, `/.vs/`, `*.csproj`, `*.sln`, `*.tmp`, `.DS_Store`, `Thumbs.db`, `/Assets/AddressableAssetsData/**/*.bin*` (placeholder, só se Addressables vier a ser usado futuramente).
4. Confirmar que arquivos já ignorados não estão rastreados: `git status` não deve listar nada de `Library/`.
5. `git add .gitignore`
6. `git commit -m "chore: add complete .gitignore for Unity"`
7. `git push -u origin chore/gitignore`
8. Merge em `main`, push, deletar branch.

**Arquivos/pastas envolvidos:** `.gitignore` (raiz).

**Resultado esperado:** `.gitignore` cobre todos os artefatos gerados pelo Unity/Editor que não devem ser versionados.

**Critérios de aceitação:**
- [ ] `git status` após um Play Mode local não mostra nada de `Library/`, `Temp/`, `Logs/`.
- [ ] `.gitignore` versionado em `main`.

**Como validar/testar:** rodar o Editor localmente, entrar em Play Mode uma vez, rodar `git status` — deve continuar limpo.

**Documentação a atualizar:** nenhuma nesta task (referenciado no Git Workflow, S1-T11).

**Branch exata:** `chore/gitignore` (mesclada em `main`).

**Commit(s):**
```
chore: add complete .gitignore for Unity
```

**Done quando:** branch mesclada, `git status` limpo após uso normal do Editor, branch deletada.

---

## S1-T06 — Configurar `.gitattributes` + Git LFS (escopo restrito)

**Importância:** Crítica
**Estimativa:** M
**Dependências:** S1-T05

**Objetivo:** Garantir que arquivos binários pesados/fonte usem Git LFS desde o início, sem tratar todo asset visual como LFS por padrão.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b chore/git-lfs`
3. Instalar Git LFS localmente: `git lfs install`.
4. Criar `.gitattributes` na raiz com:
   ```gitattributes
   * text=auto

   *.psd filter=lfs diff=lfs merge=lfs -text
   *.psb filter=lfs diff=lfs merge=lfs -text
   *.aseprite filter=lfs diff=lfs merge=lfs -text
   *.wav filter=lfs diff=lfs merge=lfs -text
   *.mp4 filter=lfs diff=lfs merge=lfs -text
   *.mov filter=lfs diff=lfs merge=lfs -text
   *.fbx filter=lfs diff=lfs merge=lfs -text
   *.exr filter=lfs diff=lfs merge=lfs -text
   ```
   **Sem** `*.png`/`*.jpg`/`*.mp3`/`*.ogg` em bloco — esses só entram em LFS individualmente se algum arquivo específico se mostrar pesado o suficiente para justificar, decisão tomada caso a caso quando os assets reais chegarem.
5. Adicionar um arquivo binário de teste real (ex.: `Assets/Art/_lfs-test.psd`, um PSD vazio de poucos KB só para validar o pipeline) para provar que o filtro funciona.
6. `git add .gitattributes Assets/Art/_lfs-test.psd`
7. `git commit -m "chore: configure .gitattributes and Git LFS for heavy source files"`
8. `git lfs ls-files` — confirmar que `_lfs-test.psd` aparece listado como rastreado por LFS.
9. `git push -u origin chore/git-lfs`
10. Merge em `main`, push, deletar branch.

**Arquivos/pastas envolvidos:** `.gitattributes` (raiz), `Assets/Art/_lfs-test.psd` (arquivo de validação, pode ser removido depois que o primeiro asset real de PSD chegar).

**Resultado esperado:** Git LFS funcional, escopado corretamente, comprovado com 1 arquivo real.

**Critérios de aceitação:**
- [ ] `.gitattributes` versionado com os padrões acima.
- [ ] `git lfs ls-files` lista `_lfs-test.psd` como rastreado por LFS.
- [ ] Nenhum `*.png`/`*.jpg` tratado como LFS por padrão.

**Como validar/testar:** `git lfs ls-files` no repositório após o push; conferir no GitHub que o arquivo aparece com o badge "Stored with Git LFS".

**Documentação a atualizar:** nenhuma nesta task (referenciado no Git Workflow, S1-T11).

**Branch exata:** `chore/git-lfs` (mesclada em `main`).

**Commit(s):**
```
chore: configure .gitattributes and Git LFS for heavy source files
```

**Done quando:** branch mesclada, LFS validado com arquivo real, branch deletada.

---

## S1-T07 — Configurar Proteção Básica da Branch `main`

**Importância:** Média
**Estimativa:** XS
**Dependências:** S1-T02

**Objetivo:** Evitar push acidental direto em `main` fora do fluxo de merge combinado.

**Passo a passo:**
1. No GitHub: Settings → Branches → Add branch protection rule.
2. Branch name pattern: `main`.
3. Ativar "Require a pull request before merging" **ou**, se optar por merge local (linha de comando) em vez de PRs, ativar ao menos "Restrict who can push to matching branches" limitando a você mesmo com aviso, conforme preferência de fluxo real de trabalho.
4. Salvar a regra.

**Arquivos/pastas envolvidos:** nenhum (configuração de repositório, não arquivo versionado).

**Resultado esperado:** `main` protegida contra push direto acidental de branches não revisadas.

**Critérios de aceitação:**
- [ ] Regra de proteção visível em Settings → Branches.
- [ ] Tentativa de push direto não revisado é bloqueada ou avisada.

**Como validar/testar:** tentar (em um teste controlado) um push direto a `main` a partir de uma branch não relacionada e confirmar o bloqueio/aviso.

**Documentação a atualizar:** Git Workflow (S1-T11) deve mencionar essa regra.

**Branch exata:** — (configuração via GitHub UI, sem commit)

**Commit(s):** — (nenhum)

**Done quando:** regra de proteção ativa e validada.

---

## S1-T08 — Criar Docs Skeleton + Publicar GitHub Pages

**Importância:** Alta
**Estimativa:** M
**Dependências:** S1-T02

**Objetivo:** Ter a documentação pública já com estrutura navegável, mesmo que ainda simples (sem DocFX/automação — isso é Sprint 4).

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b docs/github-pages-skeleton`
3. Criar `docs/index.md` (Home): nome do projeto, pitch curto (GDD Seção 2), link para `docs/gdd/`, link para `docs/sprints/`, estado atual ("Em desenvolvimento — Sprint 1").
4. Criar `docs/gdd/index.md`: conteúdo do GDD Mestre v1.01235 (fonte de verdade), com nota de topo indicando que é a versão congelada.
5. Criar `docs/sprints/index.md`: página-índice, inicialmente listando só "Sprint 1 (em andamento)".
6. Criar `docs/sprints/_template.md`: template reutilizável de Sprint Report (seções: Objetivo, Sistemas adicionados, Decisões técnicas, Arquivos/classes principais, Eventos adicionados, Testes executados, Bugs conhecidos, Dívida técnica, Próximos passos).
7. Criar `docs/changelog.md` com cabeçalho e seção vazia para "Sprint 01" (será preenchida na S1-T14).
8. `git add docs/`
9. `git commit -m "docs: add GitHub Pages skeleton (home, gdd, sprint reports, changelog)"`
10. `git push -u origin docs/github-pages-skeleton`
11. Merge em `main`, push.
12. No GitHub: Settings → Pages → Source: Deploy from a branch → Branch `main`, pasta `/docs`. Salvar.
13. Aguardar a publicação e acessar a URL gerada para confirmar Home/GDD/Sprints carregando.
14. Deletar a branch local e remota.

**Arquivos/pastas envolvidos:** `docs/index.md`, `docs/gdd/index.md`, `docs/sprints/index.md`, `docs/sprints/_template.md`, `docs/changelog.md`.

**Resultado esperado:** site publicado e navegável com as 3 seções mínimas.

**Critérios de aceitação:**
- [ ] URL do GitHub Pages carrega a Home.
- [ ] GDD acessível a partir da Home.
- [ ] Sprint Reports (índice + template) acessível.

**Como validar/testar:** acessar a URL pública em uma aba anônima do navegador e navegar pelos 3 links.

**Documentação a atualizar:** é a própria task de documentação.

**Branch exata:** `docs/github-pages-skeleton` (mesclada em `main`).

**Commit(s):**
```
docs: add GitHub Pages skeleton (home, gdd, sprint reports, changelog)
```

**Done quando:** site publicado, acessível publicamente, branch mesclada e deletada.

---

## S1-T09 — Escrever README.md

**Importância:** Alta
**Estimativa:** S
**Dependências:** S1-T08

**Objetivo:** Ter um ponto de entrada curto no próprio repositório, direcionando para a documentação completa.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b docs/readme`
3. Escrever `README.md` na raiz com: nome do projeto, pitch de 1–2 frases, versão exata do Unity (6000.0.35f1 LTS), link para o site de documentação (GitHub Pages), instruções rápidas de clone+abertura (resumidas — detalhe completo fica no Project Setup Guide), link para o Project Setup Guide e para o Git Workflow (mesmo que esses dois arquivos ainda não existam no momento de escrever este README — serão criados nas próximas 2 tasks; ajustar os links quando eles existirem, antes do merge final desta branch).
4. `git add README.md`
5. `git commit -m "docs: add README with project overview and quickstart"`
6. `git push -u origin docs/readme`
7. Merge em `main`, push, deletar branch.

**Arquivos/pastas envolvidos:** `README.md` (raiz).

**Resultado esperado:** README funcional como porta de entrada do repositório.

**Critérios de aceitação:**
- [ ] README contém versão exata do Unity.
- [ ] README linka corretamente para o site de documentação.
- [ ] README linka para Project Setup Guide e Git Workflow (links válidos após S1-T10/S1-T11 existirem).

**Como validar/testar:** abrir o README renderizado no GitHub e clicar em cada link.

**Documentação a atualizar:** é a própria task.

**Branch exata:** `docs/readme` (mesclada em `main`).

**Commit(s):**
```
docs: add README with project overview and quickstart
```

**Done quando:** branch mesclada, README visível na página principal do repositório no GitHub.

---

## S1-T10 — Escrever Project Setup Guide

**Importância:** Alta
**Estimativa:** M
**Dependências:** S1-T04, S1-T06

**Objetivo:** Documentar o passo a passo real para qualquer pessoa (inclusive o próprio Luciano, meses depois) configurar o ambiente do zero.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b docs/project-setup-guide`
3. Criar `docs/guides/project-setup.md` cobrindo: versão exata do Unity e como instalá-la via Hub; template usado (2D URP); passo a passo de clone; Git LFS (`git lfs install` antes do clone, se for a primeira vez na máquina); como abrir o projeto pela primeira vez; onde estão as Project Settings relevantes (referenciando S1-T03/S1-T04); como rodar a `_TestScene`; como gerar um build local (File → Build Settings → Build).
4. Referenciar este guia a partir de `docs/index.md` (Home).
5. `git add docs/guides/project-setup.md docs/index.md`
6. `git commit -m "docs: add Project Setup Guide"`
7. `git push -u origin docs/project-setup-guide`
8. Merge em `main`, push, deletar branch.

**Arquivos/pastas envolvidos:** `docs/guides/project-setup.md`, `docs/index.md` (link adicionado).

**Resultado esperado:** guia completo o bastante para onboarding sem depender de memória.

**Critérios de aceitação:**
- [ ] Guia cobre versão do Unity, clone, LFS, abertura, build.
- [ ] Linkado a partir da Home.

**Como validar/testar:** seguir o próprio guia do zero, literalmente, durante a S1-T12 (Clean Clone Validation) — se algum passo estiver incompleto, ele aparece ali.

**Documentação a atualizar:** é a própria task; também atualiza `docs/index.md`.

**Branch exata:** `docs/project-setup-guide` (mesclada em `main`).

**Commit(s):**
```
docs: add Project Setup Guide
```

**Done quando:** branch mesclada, guia acessível pela Home publicada.

---

## S1-T11 — Escrever Git Workflow Guide

**Importância:** Alta
**Estimativa:** M
**Dependências:** S1-T05, S1-T06, S1-T07

**Objetivo:** Documentar formalmente a estratégia de branch e a convenção de commits para não depender de memória nas próximas 55 sprints.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b docs/git-workflow`
3. Criar `docs/guides/git-workflow.md` cobrindo: estratégia de branch (`main` estável + `chore/*`, `docs/*`, `fix/*`, `feature/*` a partir da Sprint 2 — sem `develop`); regra de nunca trabalhar direto em `main` (exceto o seed commit e merges/tags de fechamento); convenção de commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`); regra de proteção de `main` (referenciando S1-T07); processo de fechamento de sprint (merge de todas as branches pendentes + tag `sprint-XX`).
4. Referenciar este guia a partir de `docs/index.md`.
5. `git add docs/guides/git-workflow.md docs/index.md`
6. `git commit -m "docs: add Git Workflow guide"`
7. `git push -u origin docs/git-workflow`
8. Merge em `main`, push, deletar branch.

**Arquivos/pastas envolvidos:** `docs/guides/git-workflow.md`, `docs/index.md` (link adicionado).

**Resultado esperado:** referência única e definitiva do fluxo de Git do projeto.

**Critérios de aceitação:**
- [ ] Estratégia de branch documentada corretamente (sem `develop`).
- [ ] Convenção de commits documentada.
- [ ] Processo de fechamento de sprint (merge + tag) documentado.

**Como validar/testar:** revisão de leitura — conferir que o texto reflete exatamente o que está sendo praticado nesta própria Sprint 1 (dogfooding).

**Documentação a atualizar:** é a própria task; também atualiza `docs/index.md`.

**Branch exata:** `docs/git-workflow` (mesclada em `main`).

**Commit(s):**
```
docs: add Git Workflow guide
```

**Done quando:** branch mesclada, guia acessível pela Home publicada.

---

## S1-T12 — Clean Clone Validation

**Importância:** Crítica
**Estimativa:** S
**Dependências:** S1-T03, S1-T04, S1-T05, S1-T06, S1-T10

**Objetivo:** Provar, na prática, que o Project Setup Guide funciona e que o repositório está em estado clonável e abrível do zero.

**Passo a passo:**
1. Em uma pasta separada (simulando "outra máquina"), seguir **literalmente** o `docs/guides/project-setup.md`: `git lfs install` → `git clone <url>` → abrir pelo Unity Hub.
2. Confirmar que os packages resolvem sem erro.
3. Confirmar que o Console não mostra nenhum erro ao abrir `_TestScene`.
4. Entrar em Play Mode uma vez — deve rodar sem exceptions.
5. Se qualquer passo falhar ou estiver incompleto no guia: corrigir o guia e/ou a configuração do projeto na hora, numa branch `fix/clean-clone-<descrição-curta>`, commitar, mesclar em `main`, e **repetir a validação do zero** (nova pasta, novo clone) até passar limpo.
6. Registrar o resultado (passou de primeira, ou quais correções foram necessárias) para incluir no Sprint Report (S1-T14).

**Arquivos/pastas envolvidos:** nenhum por padrão; se houver correção, os arquivos afetados dependem do problema encontrado (tipicamente `.gitignore`, `.gitattributes`, ou o próprio guia).

**Resultado esperado:** clone limpo → abre → Play funciona, sem intervenção manual não documentada.

**Critérios de aceitação:**
- [ ] Clone limpo em pasta nova, sem erro de LFS.
- [ ] Unity Hub resolve packages sem erro.
- [ ] Console sem erro ao abrir `_TestScene`.
- [ ] Play Mode funciona sem exception.

**Como validar/testar:** a própria execução desta task **é** o teste.

**Documentação a atualizar:** `docs/guides/project-setup.md` (se algo precisar de correção); resultado registrado no Sprint Report.

**Branch exata:** `fix/clean-clone-<descrição>` **somente se** um problema for encontrado; caso contrário, nenhuma branch é necessária (task de validação pura).

**Commit(s):** condicional —
```
fix: <descrição específica do problema encontrado na clonagem limpa>
```
Se nenhum problema for encontrado, nenhum commit é gerado nesta task.

**Done quando:** um clone 100% limpo (sem nenhuma correção pendente) abre e roda em Play Mode sem erro.

---

## S1-T13 — Build Smoke Test (Windows)

**Importância:** Crítica
**Estimativa:** S
**Dependências:** S1-T12

**Objetivo:** Confirmar que o projeto não só roda no Editor, mas gera um executável funcional fora dele.

**Passo a passo:**
1. A partir do clone limpo validado na S1-T12 (ou de `main` atualizado), abrir File → Build Settings.
2. Build → escolher pasta de saída (ex.: `Builds/Windows/` — já coberta pelo `.gitignore`, não será versionada).
3. Aguardar o build completar sem erro no Console.
4. Abrir o executável gerado manualmente.
5. Confirmar que ele abre sem crash e fecha normalmente.
6. Se houver erro de build: corrigir numa branch `fix/build-<descrição>`, commitar, mesclar em `main`, repetir o build do zero.
7. Registrar o resultado para o Sprint Report (S1-T14).

**Arquivos/pastas envolvidos:** `Builds/Windows/` (gerado, não versionado); possíveis correções em `ProjectSettings/` se o problema for de configuração de build.

**Resultado esperado:** executável Windows funcional gerado a partir de `main`.

**Critérios de aceitação:**
- [ ] Build completa sem erro no Console.
- [ ] Executável abre sem crash.
- [ ] Executável fecha normalmente.

**Como validar/testar:** a própria execução desta task **é** o teste.

**Documentação a atualizar:** resultado registrado no Sprint Report.

**Branch exata:** `fix/build-<descrição>` **somente se** um problema for encontrado; caso contrário, nenhuma branch necessária.

**Commit(s):** condicional —
```
fix: <descrição específica do problema de build encontrado>
```

**Done quando:** build gerado a partir de `main`, sem correções pendentes, executável validado.

---

## S1-T14 — Escrever Sprint 1 Report + Changelog Final

**Importância:** Alta
**Estimativa:** S
**Dependências:** S1-T09, S1-T10, S1-T11, S1-T12, S1-T13

**Objetivo:** Fechar o registro histórico da sprint conforme o template já criado.

**Passo a passo:**
1. `git checkout main && git pull`
2. `git checkout -b docs/sprint-01-report`
3. Preencher `docs/sprints/sprint-01.md` usando `docs/sprints/_template.md`: Objetivo da sprint, sistemas/infraestrutura adicionados (projeto Unity, Git, Docs), decisões técnicas (Unity 6000.0.35f1 LTS, URP 2D, branch strategy, LFS escopado), resultado da Clean Clone Validation (S1-T12) e do Build Smoke Test (S1-T13), bugs conhecidos (se houver), dívida técnica (se houver), próximos passos (Sprint 2 — Input System + Hierarchy Organization).
4. Atualizar `docs/sprints/index.md` para linkar `sprint-01.md`.
5. Preencher a entrada "Sprint 01" em `docs/changelog.md` com a lista real do que foi adicionado.
6. `git add docs/sprints/sprint-01.md docs/sprints/index.md docs/changelog.md`
7. `git commit -m "docs: add Sprint 1 report and update changelog"`
8. `git push -u origin docs/sprint-01-report`
9. Merge em `main`, push, deletar branch.

**Arquivos/pastas envolvidos:** `docs/sprints/sprint-01.md`, `docs/sprints/index.md`, `docs/changelog.md`.

**Resultado esperado:** histórico da sprint publicado e correto.

**Critérios de aceitação:**
- [ ] Sprint Report preenchido em todas as seções do template.
- [ ] Changelog com a entrada real da Sprint 01.
- [ ] Ambos acessíveis via GitHub Pages.

**Como validar/testar:** acessar a URL publicada e conferir que o relatório reflete fielmente o que foi feito (sem inflar nem omitir problemas encontrados nas validações).

**Documentação a atualizar:** é a própria task.

**Branch exata:** `docs/sprint-01-report` (mesclada em `main`).

**Commit(s):**
```
docs: add Sprint 1 report and update changelog
```

**Done quando:** branch mesclada, relatório e changelog publicados e acessíveis.

---

## S1-T15 — Sprint 1 Closure (merge final, tag `sprint-01`)

**Importância:** Crítica
**Estimativa:** XS
**Dependências:** Todas as tasks anteriores (S1-T01 a S1-T14)

**Objetivo:** Fechar formalmente a sprint com `main` estável, working tree limpa, e um marco de Git rastreável.

**Passo a passo:**
1. Confirmar que **todas** as branches de tasks anteriores já foram mescladas em `main` e deletadas (local e remotamente) — nenhuma branch órfã pendente.
2. `git checkout main && git pull`
3. `git status` — confirmar working tree limpa (nada para commitar).
4. Rodar mais uma vez a checagem rápida: projeto abre, Console sem erro, Play funciona (repetição leve da S1-T12, só para confirmar que o estado final de `main` pós-todos-os-merges continua saudável).
5. `git tag -a sprint-01 -m "Sprint 1 — Setup do Projeto + Git + Docs Skeleton"`
6. `git push origin sprint-01`
7. Confirmar no GitHub (aba Tags/Releases) que `sprint-01` aponta para o commit final estável de `main`.

**Arquivos/pastas envolvidos:** nenhum arquivo novo — apenas a tag Git.

**Resultado esperado:** `main` estável, working tree limpa, tag `sprint-01` publicada.

**Critérios de aceitação:**
- [ ] Nenhuma branch de task pendente sem merge.
- [ ] `git status` limpo.
- [ ] Tag `sprint-01` visível no GitHub, apontando para o commit correto.

**Como validar/testar:** `git branch -a` mostra só `main` (mais o que já for padrão do GitHub); `git tag` lista `sprint-01`; a aba de Tags no GitHub confirma.

**Documentação a atualizar:** nenhuma nova — este é o fechamento, não geração de conteúdo.

**Branch exata:** `main` (tag criada diretamente sobre o commit final de `main`).

**Commit(s):** nenhum commit novo — apenas a tag anotada `sprint-01`.

**Done quando:** tag `sprint-01` publicada no remoto, `main` estável, nenhuma branch pendente.

---

# Ordem de Execução

```text
S1-T01 → S1-T02 → S1-T03 → S1-T04 → S1-T05 → S1-T06 → S1-T07 →
S1-T08 → S1-T09 → S1-T10 → S1-T11 → S1-T12 → S1-T13 → S1-T14 → S1-T15
```

T07 pode, na prática, ser executada a qualquer momento entre T02 e T15 (é configuração de repositório, não bloqueia nenhuma outra task) — mas manter essa posição evita esquecê-la. T09/T10/T11 podem ser feitas em qualquer ordem entre si, desde que todas terminem antes de T12 (a validação de clone precisa do Project Setup Guide já mesclado).

# Caminho Crítico

```text
S1-T01 → S1-T02 → S1-T03 → S1-T04 → S1-T06 → S1-T10 → S1-T12 → S1-T13 → S1-T14 → S1-T15
```

Este é o caminho que, se atrasar, atrasa a sprint inteira: criar o projeto → versioná-lo → configurá-lo → ter uma Scene de build → ter LFS funcionando → ter o guia que a validação vai seguir → provar que o clone funciona → provar que o build funciona → registrar → fechar. T05, T07, T09, T11 correm em paralelo a esse caminho sem bloqueá-lo diretamente (embora T05 seja pré-requisito técnico de T06).

# Branch Plan

| Branch | Tasks | Finalidade |
|---|---|---|
| `main` | S1-T01 (local), S1-T02 (seed), S1-T15 (fechamento) | Estado estável do projeto; recebe apenas merges validados |
| `chore/project-settings` | S1-T03 | Configurações centrais do Unity |
| `chore/build-settings` | S1-T04 | Build target + test scene |
| `chore/gitignore` | S1-T05 | `.gitignore` completo |
| `chore/git-lfs` | S1-T06 | `.gitattributes` + Git LFS |
| — (GitHub UI) | S1-T07 | Proteção de branch (sem commit) |
| `docs/github-pages-skeleton` | S1-T08 | Home, GDD, Sprint Reports, Changelog + Pages ativado |
| `docs/readme` | S1-T09 | README.md |
| `docs/project-setup-guide` | S1-T10 | Guia de setup |
| `docs/git-workflow` | S1-T11 | Guia de fluxo Git |
| `fix/clean-clone-<descrição>` | S1-T12 (condicional) | Correções encontradas na validação de clone limpo |
| `fix/build-<descrição>` | S1-T13 (condicional) | Correções encontradas no smoke test de build |
| `docs/sprint-01-report` | S1-T14 | Sprint Report + Changelog final |

# Commit Plan

| Ordem | Task | Branch | Commit exato |
|---|---|---|---|
| 1 | S1-T02 | `main` | `chore: initialize Unity project (Unity 6000.0.35f1 LTS, URP 2D template)` |
| 2 | S1-T03 | `chore/project-settings` | `chore: configure player, render pipeline, quality, time, physics and audio settings` |
| 3 | S1-T04 | `chore/build-settings` | `chore: configure build settings and add test scene` |
| 4 | S1-T05 | `chore/gitignore` | `chore: add complete .gitignore for Unity` |
| 5 | S1-T06 | `chore/git-lfs` | `chore: configure .gitattributes and Git LFS for heavy source files` |
| 6 | S1-T08 | `docs/github-pages-skeleton` | `docs: add GitHub Pages skeleton (home, gdd, sprint reports, changelog)` |
| 7 | S1-T09 | `docs/readme` | `docs: add README with project overview and quickstart` |
| 8 | S1-T10 | `docs/project-setup-guide` | `docs: add Project Setup Guide` |
| 9 | S1-T11 | `docs/git-workflow` | `docs: add Git Workflow guide` |
| 10 | S1-T12 | `fix/clean-clone-<descrição>` (condicional) | `fix: <descrição específica do problema encontrado na clonagem limpa>` |
| 11 | S1-T13 | `fix/build-<descrição>` (condicional) | `fix: <descrição específica do problema de build encontrado>` |
| 12 | S1-T14 | `docs/sprint-01-report` | `docs: add Sprint 1 report and update changelog` |
| 13 | S1-T15 | `main` | (sem commit novo — tag anotada `sprint-01`) |

Cada linha 2–9 e 12 também gera, no merge para `main`, um commit de merge (mensagem padrão do Git/GitHub, ex. `Merge branch 'chore/project-settings' into main`) — não listado individualmente acima por ser gerado automaticamente, não redigido à mão.

---

# Sprint 1 — Master Completion Checklist

- [ ] Versão exata do Unity documentada (6000.0.35f1 LTS) — README + Project Setup Guide.
- [ ] Projeto abre sem erro (local e em clone limpo).
- [ ] Console sem erro crítico ao abrir `_TestScene`.
- [ ] Git remoto (GitHub) funcionando, repositório acessível.
- [ ] `.gitignore` validado (nenhum artefato de `Library/`/`Temp/` versionado).
- [ ] `.gitattributes` validado (padrões corretos, sem `*.png`/`*.jpg` em bloco).
- [ ] Git LFS validado com arquivo real (`git lfs ls-files` confirma).
- [ ] GitHub Pages publicado e acessível publicamente.
- [ ] GDD acessível a partir da documentação publicada.
- [ ] README completo e correto.
- [ ] Project Setup Guide completo e correto.
- [ ] Git Workflow documentado e correto.
- [ ] Sprint Report da Sprint 1 publicado.
- [ ] Clean Clone Validation executada com sucesso (S1-T12).
- [ ] Play Mode funciona sem exception.
- [ ] Build Windows gerado com sucesso (S1-T13).
- [ ] Executável abre sem crash.
- [ ] Working tree de `main` limpa (`git status` sem pendências).
- [ ] Todas as branches de task concluídas foram mescladas e deletadas (nenhuma órfã).
- [ ] `main` estável (reflete o estado final validado).
- [ ] Tag `sprint-01` criada e enviada ao remoto, apontando para o commit final de `main`.

---

SPRINT 1 — TASK LIST APROVÁVEL PARA EXECUÇÃO
