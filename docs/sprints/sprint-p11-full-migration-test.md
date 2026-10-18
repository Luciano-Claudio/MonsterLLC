# Sprint P11 — Full Migration Test

## Objetivo

Fechar a Pré-Deadline Mundo Único (P1-P11) com um teste de ponta a ponta: todos os sistemas das sprints anteriores funcionando juntos (terreno, Safe Zone, EffectiveSpawnDay, população, Boss, Dia/Noite, Simulation Rings, Game Feel, Floor/Save), sem erro de console, numa run real explorando a região central.

## Sistemas adicionados

Nenhum sistema novo — sprint de validação pura, sem código além do que as sprints P1-P10 já entregaram.

## Decisões técnicas

Checklist estruturado por sistema (não um roteiro de playtest livre), cobrindo especificamente os achados/correções de cada sprint anterior — ex.: testar explicitamente um monstro com cooldown de ataque (Spider Queen/Rat People Royalty) voltando a atacar em intervalo normal depois de ficar fora do anel próximo (Simulation Rings), não só "o jogo não crasha".

## Arquivos/classes principais

Nenhum arquivo de código — só `docs/new/Plano_Spawn_Monstros_Mundo_Aberto.md`, atualizado com a confirmação.

## Eventos adicionados

Nenhum.

## Testes executados

Checklist manual completo, confirmado pelo usuário sem nenhum problema reportado:

- **Terreno (P1-P3)**: sem costura visível entre chunks (inclusive o chunk central pintado síncrono); bioma evolui com `ActualDay`.
- **Safe Zone + distância (P4)**: nenhum monstro nasce dentro da Safe Zone; Tiers mais altos aparecem longe da origem mesmo no Dia 1.
- **População (P5-P6)**: despawn/replacement por distância sem drop/kill; população oscila entre min/target/max sem travar em 0 nem explodir.
- **Boss (P7)**: timer no intervalo certo; Tier batendo com a predominância; identidade não repetindo no mesmo dia; reset por-Tier funcionando; reveal (pausa → foco → volta → despausa); despawn por distância repondo sem resetar o timer principal.
- **Simulation Rings (P8)**: monstro longe continua reagindo normal ao se aproximar; cooldown de ataque não atrasado anormalmente.
- **Câmera/Game Feel (P9)**: shake+zoom no hit de monstro (nunca em DoT/Pet); shake mais forte (sem zoom) ao tomar dano; % de spawn fora de tela funcionando.
- **Floor removido + Save (P10)**: sem "Missing Script" no Console; 2 "Novo Jogo" seguidos geram mundos diferentes; salvar/recarregar mantém o mesmo mundo.
- **Geral**: Console limpo de erro/warning numa sessão de alguns minutos explorando.

## Bugs conhecidos

Nenhum reportado no checklist.

## Dívida técnica

Nenhuma nova — esta sprint só CONFIRMA o que já existia. A dívida técnica real continua sendo a soma do que cada sprint P1-P10 já registrou nos próprios docs (preços placeholder, Animator Culling Mode não implementado, etc.).

## Próximos passos

Pré-Deadline Mundo Único (P1-P11) fecha por completo aqui. Próxima fase é produção de conteúdo pura (Deadline 8+, sem sistema novo de engenharia): Bestiário dos Tiers 3-10, Loot (15 LootDefinitions — hoje só existe 1 item, "Monster Essence"), BiomeProfile por Tier. Employees/Magnet/Remote Controller/Floor Variants (Seções 19-22 do doc de design maior) não existem no código ainda e estão fora do escopo próximo — Employees está marcado explicitamente pra Deadline 10 no plano de produção oficial.
