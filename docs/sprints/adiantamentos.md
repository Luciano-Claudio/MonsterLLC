# Adiantamentos

Registro de trabalho feito **fora do escopo da sprint corrente**, adiantado de propósito por já ter contexto/tempo disponível. Não é dívida técnica (nada aqui está quebrado ou incompleto por atalho) — é infraestrutura ou conteúdo que só vai "entrar em uso" numa sprint futura, documentado aqui em detalhe pra não se perder até lá. Cada entrada vira uma nota rápida na sprint/batch de destino, apontando de volta pra cá.

Quando um adiantamento é finalmente consumido pela sprint de destino, o próprio relatório daquela sprint deve linkar de volta pra entrada correspondente aqui (não duplicar o conteúdo).

---

## 2026-09-16 — Base Animator + ferramenta para monstros de "emboscada" (destino: Batch 4, Sprint 25, Floor 5)

**Por que adiantado:** enquanto adicionava manualmente as sprites de ataque de todos os monstros (trabalho contínuo, fora de sprint), o usuário notou que a maioria dos monstros do Andar 5 (undeads — principalmente Skeletons e o Gargoyle) não se encaixa no padrão de `idle` direcional usado por todo o resto do Bestiário: eles não vagam sozinhos, ficam numa única pose parada (sem arte NE/NW/SE/SW) até detectar o jogador. Isso exigia mudança na `MonsterAnimationGeneratorWindow` e em 2 novos Animators base — trabalho de infraestrutura que só vai ser efetivamente **usado** no Batch 4 (Floor 5–6, Sprint 25, GDD/Plano de Produção), mas que fazia sentido resolver agora, com o contexto do pivô de combate da Sprint 16 ainda fresco.

### O que é a variante de idle "emboscada"

Documentado em detalhe no [GDD Seção 22](../gdd/index.md) e no [Bestiário](../gdd/bestiary.md) (regra padrão de combate, topo do documento). Resumo:
- `idle` deixa de ser um Blend Tree direcional — vira **1 clipe único, não-direcional** (o monstro fica parado, "parte do cenário", igual o Gargoyle já era antes desse trabalho).
- Ao detectar o jogador, toca **`activate`** (clipe não-direcional, 1x) antes de entrar em `walk`/`idle_combat` normalmente.
- **Única exceção do jogo em que a detecção pode reverter:** se o jogador sair do raio de observação, o monstro volta pro `idle` estático **instantaneamente, sem animação de transição** (o jogador não estaria nem olhando pra ele nesse instante). Em todo o resto do Bestiário, uma vez detectado o jogador é perseguido pra sempre.
- Fora disso, ataque/dano/morte seguem 100% o padrão Melee/Ranged comum.

**Monstros afetados (Andar 5):** Gargoyle, Skeleton, Headless Skeleton, Skeletal Horse, Skeleton Mage, Skeleton Minotaur, Skeleton Rider, Skeleton Warrior. Fichas já atualizadas no Bestiário.

### Mudança adicional (mesma sessão): ataque do Skeleton Mage / Zombie Mage

Remudança dentro do próprio período de correção da Sprint 16: esses dois monstros tinham voltado a ter dano só por contato direto do projétil; voltou a ser uma **área de conjuração no chão** (mais fiel ao design original). Fluxo: `cast` (Animation Event) → nasce um prefab de área na posição atual do jogador naquele instante → o prefab tem sua própria animação de aviso, com seu próprio Animation Event, que só causa dano se o jogador ainda estiver dentro do trigger naquele frame exato (dá tempo de fugir).

### Arquivos alterados/criados

- `Assets/Scripts/Enemies/EnemyController.cs` — novo campo `staysDormantUntilDetected`; `UpdatePatrol()` ganhou um branch que pula a patrulha aleatória pra esses monstros; `UpdateCombat()` ganhou o branch de de-aggro (`isInCombat = false` quando o jogador sai do raio, com snap instantâneo pro `idle`).
- `Assets/Editor/MonsterAnimationTools/MonsterAnimationGeneratorWindow.cs` — toggle "Emboscada" na UI; campo `activateSpriteSheet` (substitui o `idleSpriteSheet` quando ativado); gera `activate` (linha única, como `die`) + `idle` estático (1 frame, tirado do 1º frame do `activate`); `idle_combat`, nesse caso, passa a ser derivado do 1º frame de cada direção do `walk` (não existe mais sheet de Idle direcional); 2 novos campos de base controller (`baseMeleeAmbushController`/`baseRangedAmbushController`); `CreateOrUpdateOverrideController` já escolhe entre os 4 base controllers conforme `isAmbushMonster` + `MonsterType` — **testado de ponta a ponta**, rodou no Skeleton (Floor 5) e os clipes gerados (`idle.anim`/`activate.anim`) já entraram certos no Override Controller.
- `Assets/Animation/Base/Base_Melee_Ambush.controller` / `Base_Ranged_Ambush.controller` — novos, duplicados de `Base_Melee`/`Base_Ranged` e ajustados manualmente pelo usuário na Unity, com revisão + 4 correções feitas por mim direto no YAML (ver "Erros encontrados e corrigidos" abaixo).
- `Assets/Scripts/Enemies/GroundCasterEnemyController.cs` (novo) — herda de `RangedEnemyController`, só sobrescreve `ExecuteAttackHit()` pra instanciar o hazard em vez do projétil.
- `Assets/Scripts/Enemies/GroundTargetHazard.cs` (novo) — área de dano telegrafada, com `AnimationHazardHitEvent()`/`AnimationHazardEndEvent()` (mesmo padrão de Animation Event + timeout de segurança usado no resto do `EnemyController`).
- `docs/gdd/bestiary.md` — 8 fichas do Andar 5 atualizadas (Gargoyle, Skeleton, Headless Skeleton, Skeletal Horse, Skeleton Mage, Skeleton Minotaur, Skeleton Rider, Skeleton Warrior) + regra padrão no topo do documento + exceção do Skeleton Mage/Zombie Mage.
- `docs/gdd/index.md` — GDD Seção 22 ganhou a subseção "Variante de idle 'emboscada'" e a exceção de ataque do Skeleton Mage/Zombie Mage.

### Erros encontrados e corrigidos (na revisão dos 2 Animators)

Revisão feita lendo o YAML dos `.controller` diretamente (são texto serializado, dá pra auditar sem abrir a Unity):
1. **`Base_Melee_Ambush`** — `Activate → Walk` e `Activate → IdleCombat` com Exit Time 0.95 (deveria ser 1.0, já que `activate` é uma animação real que precisa tocar inteira, não uma pose estática). Corrigido.
2. **`Base_Ranged_Ambush`** — mesmo ajuste de Exit Time nas mesmas 2 transições. Corrigido.
3. **`Base_Ranged_Ambush`** — **bug real, não só estético:** faltava a transição `idleCombat → Idle` inteira (só existia `idleCombat → Walk`). Sem ela, um Ranged de emboscada que desiste do jogador **parado** (sem chegar a andar) travaria pra sempre em `idleCombat`. Criada copiando a mesma transição já validada no Melee (`InCombat == false`, sem Exit Time, duração 0).
4. Confirmado (2 rodadas de checagem): `Idle`/`Activate` de ambos os controllers têm Motion atribuído (`idle.anim`/`activate.anim` do Skeleton, usados como placeholder — mesmo padrão que o Rat já servia de placeholder pro `Base_Melee` original).

### Pendências (ficam para quando o Batch 4 chegar)

- **Nenhum teste em Play Mode ainda** — nem da ativação/de-aggro, nem do ataque em área.
- **Prefab do `GroundTargetHazard` não existe** — falta criar (Animator + `Collider2D` trigger + os 2 Animation Events) e trocar o componente `RangedEnemyController` por `GroundCasterEnemyController` nos prefabs de Skeleton Mage/Zombie Mage.
- Gerar os clipes dos outros 7 monstros de emboscada restantes (só o Skeleton foi gerado até aqui).

### Quando isso for retomado (Sprint 25)

Ler esta entrada inteira antes de começar o Batch 4 — ela cobre o "porquê" de toda a arquitetura de emboscada, que não vai estar óbvio só olhando pro código essa altura.
