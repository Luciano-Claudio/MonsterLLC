# Git Workflow Guide

Referência única do fluxo de Git do projeto — estratégia de branch, convenção de commits e processo de fechamento de sprint. Documento vivo: se a prática real divergir do que está escrito aqui, corrija o texto, não a memória.

## Estratégia de branch

`main` é a única branch estável e de longa duração. **Não existe `develop`.** Todo trabalho nasce de `main` atualizada e volta para `main` via merge:

- **`chore/*`** — configuração de projeto, tooling, infraestrutura (ex.: `chore/build-settings`, `chore/git-lfs`).
- **`docs/*`** — documentação (ex.: `docs/readme`, `docs/git-workflow`).
- **`fix/*`** — correção de bug.
- **`feature/*`** — a partir da Sprint 2, para sistemas de gameplay novos.

Fluxo de uma branch, do início ao fim:

```
git checkout main && git pull
git checkout -b <tipo>/<nome-curto>
# ... trabalho + commits ...
git push -u origin <tipo>/<nome-curto>
git checkout main && git pull
git merge --no-ff <tipo>/<nome-curto> -m "Merge branch '<tipo>/<nome-curto>'"
git push origin main
git branch -d <tipo>/<nome-curto>
git push origin --delete <tipo>/<nome-curto>
```

`--no-ff` é deliberado: mantém no histórico de `main` o ponto exato em que cada task entrou, mesmo quando a branch teria fast-forward.

## Regra central: nunca trabalhar direto em `main`

`main` só recebe commit direto em exatamente dois casos:

1. O **seed commit** inicial do repositório (por definição, não existe branch anterior de onde nascer).
2. **Merges e tags de fechamento** — o próprio ato de mesclar uma branch pronta em `main` (e a tag de fechamento de sprint, abaixo).

Todo o resto — qualquer arquivo de código, configuração ou documentação — nasce em uma branch dedicada, mesmo que seja uma mudança de uma linha.

## Convenção de commits

Prefixo obrigatório no início da mensagem:

| Prefixo | Uso |
|---|---|
| `feat:` | novo sistema/funcionalidade de gameplay |
| `fix:` | correção de bug |
| `docs:` | documentação (README, GDD, guides, sprint reports) |
| `refactor:` | mudança de estrutura sem alterar comportamento |
| `test:` | testes automatizados |
| `chore:` | configuração de projeto, build, tooling, dependências |

Mensagem no imperativo, curta, descrevendo o *porquê*/*o quê* — não uma lista de arquivos tocados (isso já está no diff). Exemplos reais desta sprint: `chore: configure build settings and add test scene`, `docs: add Project Setup Guide`.

## Proteção de `main`

`main` tem um **Ruleset** configurado em **GitHub → Settings → Rules → Rulesets** (S1-T07, resolvido em 2026-09-02 — não em **Settings → Branches**, o "Branch protection rules" clássico, que é um sistema separado), bloqueando `deletion` e `non_fast_forward` (force-push). Não exige Pull Request — o merge continua sendo local (`--no-ff`) + push, como descrito acima; a regra só é rede de segurança contra force-push ou deleção acidental de `main`, não contra push direto em si.

## Fechamento de sprint

Ao final de cada sprint, antes de considerá-la encerrada:

1. Confirmar que **todas as branches pendentes da sprint foram mescladas em `main`** (nenhuma `chore/*`, `docs/*`, `fix/*` ou `feature/*` da sprint deixada para trás).
2. Confirmar que as branches mescladas foram deletadas (local e remota).
3. Criar a tag de fechamento a partir de `main` atualizada:

   ```
   git checkout main && git pull
   git tag -a sprint-01 -m "Sprint 1 — <resumo curto>"
   git push origin sprint-01
   ```

4. Numeração sequencial: `sprint-01`, `sprint-02`, etc., sempre com dois dígitos.
