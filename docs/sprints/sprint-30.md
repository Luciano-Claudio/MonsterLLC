# Sprint 30 — Blood Mage Completo + Checklist dos 10 Heróis

## Objetivo

Blood Mage Gameplay Complete (Primário + Ultimate + Shift + Passiva) — fecha os 10 heróis do MVP (Deadline 7).

## Sistemas adicionados

- **Primário — leque de projéteis retos com reserva de dano** (`BloodMageProjectile`) — mesmo leque angular do martelo do Paladin (1–5 projéteis via upgrade, `projectileCount`), cada um recebe reserva CHEIA (sem dividir). A reserva (`damageReserveMultiplier`, padrão 1) define quantos "hits cheios" o projétil aguenta antes de ir pro Impact — padrão é 1 hit exato (vida útil = o próprio dano), upgradable pra perfuração real depois.
- **Ultimate — anel de dano CIRCULAR em 3 estágios** (`BloodMageShockwave`) — diâmetro 2→4→8, dano na BORDA (cada estágio exclui quem já estava dentro do círculo anterior — zona segura dinâmica), 5× o dano por estágio. Escala com `shockwaveSizeMultiplier` (upgrade futuro): o raio real E o offset do centro acompanham `transform.localScale.x` sozinhos (fonte única de verdade, sem pré-multiplicar em 2 lugares). Campos de teste (`stage1/2/3Size`, `ringOffsetY`) editáveis direto no prefab, com gizmo (`OnDrawGizmos`, sempre visível) pra calibrar sem precisar rodar o Blood Mage inteiro.
- **Shift — extração de sangue multi-alvo + status Drain** (`BloodOrb`) — drena até `extractTargetCount` monstros vivos mais próximos numa área (mesmo critério da vinha do Druid: `OverlapCircleAll` + descarta mortos + ordena por distância). Cada alvo recebe o status visual **"Drain"** (novo, `StatusEffectType.Drain` + `FlashStatus`) antes do dano — 1x só, sem loop, prioridade acima de qualquer outro Efeito ativo (Fire/Bleeding/etc.) na tela, mas nunca escapa `WordOfPain` (incapacitação sempre por cima). O dano real nunca é afetado pelo status, só a animação. Uma orb de sangue nasce no alvo e viaja (homing, segue a posição ATUAL do Blood Mage) até ele — ao chegar, cura **exatamente o dano real causado** (`extractHealMultiplier`, padrão 1 = cura idêntica ao dano) × orb, não um valor fixo desconectado.
- **Passiva — pet Elemental de Sangue** (`BloodElemental`) — cópia da Phoenix do Mage (`PetController`, mesma API `Initialize`), summon-lock a partir da própria animação de summon do Blood Mage. Diferente da Phoenix (que funde parado+andando num "Fly" só), o Elemental tem Idle e Move como Blend Trees separados — exigiu generalizar o gate de movimento do `PetController` (ver Decisões técnicas).
- **Status "Drain" no sistema genérico** (`StatusEffectController`) — primeiro uso de `FlashStatus` fora do Heal do Cleric; guarda nova impede que ESSE tipo de flash (1x só, override visual) esconda `WordOfPain` (incapacitação).

## Decisões técnicas

- **Redesenho do Shift em 2 rodadas, ambas a pedido do usuário.** 1ª: trocou de "1 alvo fixo" pra multi-alvo (Druid) + viagem da orb não-bloqueante (Blood Mage se move livremente enquanto ela viaja). 2ª: inseriu o status "Drain" ANTES do dano/orb — mapeado pra `FlashStatus` (não `ApplyStatusEffect`) porque é visual-only, sem duração/dano próprio, exatamente o padrão que o Heal do Cleric já usava.
- **`PetController` generalizado de "só move durante Fly" pra "nunca move durante Summon"** — a Phoenix cobre parado+andando num Blend Tree só ("Fly"), mas o Elemental de Sangue tem arte separada pra Idle e Move. Como o script é compartilhado pelos 2 pets, trocar a checagem pra negativa (bloqueia só durante Summon, libera em qualquer outro estado) preserva o comportamento da Phoenix 100% e libera o Elemental a funcionar com 2 estados reais em vez de 1.
- **Die do pet virou 1 clipe único, não 4 direções** — erro meu: criei `die_ne/nw/se/sw.anim` seguindo o padrão do resto do Elemental (Idle/Move/Attack são direcionais), mas a arte real (`Banish-Die.png`) é 1 animação só, igual a TODOS os outros "die" do projeto (inclusive o do próprio corpo do Blood Mage). Corrigido a pedido do usuário.
- **Anel da Ultimate é círculo, não caixa** — a implementação original seguiu a notação do GDD ("2×2 → 4×4 → 8×8") literalmente como `OverlapBoxAll`; o usuário queria círculo de verdade. Trocado pra `OverlapCircleAll`/`DrawWireSphere`, mantendo os campos `stageNSize` como DIÂMETRO (não raio) pra não quebrar os números já calibrados.
- **`shockwaveSizeMultiplier` e `ringOffsetY` escalam via `transform.localScale.x` direto no `BloodMageShockwave`, não pré-multiplicados no `BloodMage.cs`** — tentativa inicial calculava o tamanho final no controlador E escalava o GameObject (2 caminhos aplicando o mesmo multiplicador), e o usuário notou que mudar o `transform.scale` na mão não refletia no gizmo nem no dano. Corrigido pra 1 fonte única: o Shockwave lê a própria escala sozinho.
- **`damageReserveMultiplier` baixado de 3 pra 1** — bug reportado como "projétil não vai pro Impact ao acertar monstro" era, na real, o mecanismo de perfuração funcionando como projetado: reserva = `dano × 3` precisa de 3 hits cheios pra esgotar, e a maioria dos testes só acertava 1 monstro no caminho. Confirmado com `Debug.Log` temporário em `OnTriggerEnter2D` (dano realmente acertando, health caindo certinho) antes de concluir que não era bug de detecção. O usuário queria cada projétil valendo exatamente 1 hit — resolvido só com o número, sem mudar a arquitetura de reserva (upgrade de perfuração real continua possível subindo o multiplicador).
- **Cura do Shift corrigida de valor fixo pra "= dano real causado"** — `extractHealAmount = 15` fixo (independente do dano) foi reportado como "extremamente absurdo" (2 de dano curava 14 de vida). `BloodOrb.Launch()` ganhou um parâmetro de cura (mesmo padrão do callback com payload do `MageTeleportProjectile`), calculado como `Mathf.Min(damage, target.stats.health) × extractHealMultiplier` (dano REAL, nunca passa da vida que o alvo tinha).
- **`statusAnimator` do `StatusEffectController` nunca foi wireado no prefab** (mesmo bug já visto no Gunslinger nesta Deadline) — Fire não aparecia na tela do Blood Mage porque o campo ficou em `{fileID: 0}` desde a criação do prefab. Corrigido.

## Arquivos/classes principais

- `Assets/Scripts/Player/Heroes/BloodMage.cs` (novo) — controlador; primário (leque+reserva), ultimate (shockwave+size multiplier), shift (extração multi-alvo+Drain+orb+cura), pet (cópia do Mage).
- `Assets/Scripts/Player/BloodMageProjectile.cs`, `BloodMageShockwave.cs`, `BloodOrb.cs` (novos) — os 3 filhos dedicados, sem balanceamento próprio (tudo via parâmetro, convenção desde a Sprint 27b).
- `Assets/Scripts/Core/StatusEffectController.cs` — `StatusEffectType.Drain` (novo valor do enum); guarda em `FlashStatus` contra `WordOfPain`.
- `Assets/Scripts/Enemies/PetController.cs` — gate de movimento generalizado (`!IsInState("Summon")` em vez de `IsInState("Fly")`); `SetMoving(bool)`/parâmetro `IsMoving` novo (ignorado pela Phoenix, usado pelo Elemental).
- `Assets/Animation/Status/Status.controller` + `Drain.anim` — estado "Drain" novo no Animator de status compartilhado.
- `Assets/Prefabs/Heros/BloodMage/BloodMage.prefab` (novo) + `BloodMageProjectile.prefab`, `BloodOrb.prefab`, `BloodMageShockwave.prefab`, `BloodElemental.prefab` (os 4 filhos).
- `Assets/Animation/Heros/Blood_Mage/` — `BloodMage.controller` (corpo: Idle/Walk/Damage/Die/Attack/Ultimate/ExtractBlood/ConsumeBlood/SummonPet/Trapped) + controllers dedicados de `Projectile/`, `Shift/Orb/`, `Ultimate/Shockwave/`, `Passiva/BloodElemental/`.

## Eventos adicionados

Nenhum em `GameEvents` — toda a comunicação é local (Animation Events) ou callback direto (`BloodOrb` → `BloodMage.OnBloodOrbArrived`).

## Testes executados

Validação manual em Play Mode pelo usuário, iterativa, com 4 rodadas de bugfix real encontradas em teste:
1. Animação de SummonPet do corpo do Blood Mage "parecia idle" — causa raiz: clipes placeholder (`summonpet_*.anim`) ainda usando os sprites duplicados do Idle, nunca substituídos pela arte real que já existe (`Passiva/Summon.png`) — identificado, troca de sprite é passo do usuário (ver Dívida técnica).
2. Status Fire não aparecia na tela do Blood Mage — causa raiz: `statusAnimator` nunca wireado no prefab — corrigido.
3. Projétil nunca ia pro Impact ao acertar monstro, só no limite da trajetória — investigado com `Debug.Log` temporário em `OnTriggerEnter2D` (confirmou dano acertando corretamente); causa raiz não era bug de detecção, era `damageReserveMultiplier = 3` exigindo 3 hits cheios pra esgotar a reserva — corrigido baixando pra 1 (cada projétil = 1 hit).
4. Cura do Shift "extremamente absurda" (valor fixo de 15, independente do dano real) — corrigido pra cura = dano real causado × multiplicador.

Confirmado funcionando pelo usuário após as 4 correções: "Perfeito, agora tá funcionando 100%!".

## Bugs conhecidos

Nenhum pendente do que foi testado.

## Dívida técnica

- `summonpet_ne/nw/se/sw.anim` do corpo do Blood Mage seguem com sprites placeholder (idênticos ao Idle) — a arte real (`Assets/Sprites/Heros/Blood_Mage/Passiva/Summon.png`, já existente no projeto) ainda não foi trocada nos clipes.
- `stats` (HP/dano/velocidade/etc.) seguem com valores placeholder — sem calibração de balance real.
- `shockwaveStage1/2/3Size`, `ringOffsetY`, raio do `CircleCollider2D` do projétil (0.3) — calibrados por teste visual (gizmo) em Play Mode, não por balance real.
- `docs/gdd/balance-values.md` segue desatualizado (dívida já registrada desde a Sprint 27b).
- `damageReserveMultiplier = 1` (sem perfuração real por padrão) — arquitetura de reserva já suporta perfuração de múltiplos monstros se o número subir via upgrade futuro, só não é o comportamento padrão hoje.
- Checklist manual formal dos 10 heróis (ataque/ultimate/secundária/passiva/morte/pausa/Combat Scope, sem erro de console) citado no objetivo da Deadline 7 ainda não foi executado nesta sprint — fica como passo seguinte antes de avançar pra Deadline 8.

## Próximos passos

**10 heróis MVP Gameplay Complete — fecha a Deadline 7.** Antes de avançar pra Sprint 31 (Deadline 8, 15 LootDefinitions), falta rodar o checklist manual formal dos 10 heróis citado no objetivo desta sprint (não foi feito ainda nesta conversa).
