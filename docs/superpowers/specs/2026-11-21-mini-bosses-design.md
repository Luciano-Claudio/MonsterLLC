# Mini bosses (design curto)

**Data:** 2026-11-21. **Contexto:** os bosses vão ter um rework grande, que fica pro fim da Deadline 9. Até lá, 5 bosses saem do roster de bosses e viram **mini bosses**. Este trabalho vem antes do D9.5.

## Intenção

Cinco bosses viram monstros comuns, um pouco mais fortes que o resto, que aparecem sozinhos a cada 30–40 s, sem aviso:
- Werewolf (T2)
- Pale Champion (T3)
- Mummy King, Undead Knight e Headless Horseman (T5)

Eles não pausam o jogo, não mexem na câmera e não aparecem na linha do tempo de bosses. Sem eles, cada tier fica com 1 a 3 bosses: T1–T6 e T9 com 3, T7 com 2, T8 e T10 com 1.

## Decisões do usuário

1. Os 5 viram monstros comuns, **sem nenhuma mudança neles** (stats e prefab iguais). Como deixam de ser boss, perdem as proteções de boss: levam knockback, são empurrados pelo herói, perdem o alvo com invisibilidade e são reposicionados quando ficam longe.
2. **Spawn próprio:** um mini boss nasce a cada 30–40 s, sozinho, sem aviso.
3. **Sorteio sem repetir o último:** entre os elegíveis, o próximo nunca é o que acabou de nascer. Se só um for elegível, ele pode repetir, pra o spawn não travar.
4. **Elegibilidade pelo tier do dia:** só nascem os mini bosses cujo tier tem chance maior que zero na tabela de spawn do dia (dia ou noite, como os monstros comuns). O dia 1 (só T1) não tem mini boss.
5. **Nascem mesmo com outro vivo:** quem não mata os mini bosses vai acumulando.
6. **Organização:** os prefabs saem de `Assets/Prefabs/Bosses/FloorN` e vão pra uma pasta nova de mini bosses, separada por tier: `Assets/Prefabs/MiniBosses/tier_2`, `tier_3` e `tier_5`.

## Abordagens

| Abordagem | Decisão |
|---|---|
| **1. Seção de mini bosses dentro do `WorldPopulationManager`.** Reaproveita o anel fora da tela, os setores, o reposicionamento e o relógio da horda (D9.4). | **Escolhida** |
| 2. Componente novo `MiniBossSpawner`. Precisaria expor ou duplicar o anel e o reposicionamento do `WorldPopulationManager`. | Descartada |
| 3. Os 5 no roster normal, com peso alto. Não dá o "um a cada 30–40 s, sozinho". | Descartada |

## Design

- **Dados:**
  - `MonsterSpawnSettings` ganha `miniBossRoster` (`List<EnemyDefinitionSO>`), mais `miniBossIntervalMin` (🔢 30) e `miniBossIntervalMax` (🔢 40).
  - Cada mini boss ganha um `EnemyDefinitionSO` (mesmo prefab, mesmo tier, `isRanged` falso) em `Assets/Settings/MiniBossDefinitions/`.
  - Os 5 `BossDefinition_*` saem do `BossSpawnSettings.roster` e são apagados.
- **Prefabs:** movidos com `AssetDatabase.MoveAsset` (o GUID se mantém, nenhuma referência quebra) pra `Assets/Prefabs/MiniBosses/tier_2`, `tier_3` e `tier_5`.
- **Relógio:** usa o tempo real do dia da horda (`HordeElapsed`). O primeiro mini boss nasce após um intervalo sorteado entre o mínimo e o máximo, e cada intervalo seguinte é sorteado de novo. O relógio zera no início do dia.
- **Elegíveis:** os mini bosses do roster cujo tier tem chance maior que zero nos percentuais de tier do dia, calculados pelo mesmo `WorldTierSelector.ComputeTierPercentsNonAlloc` do spawn comum (dia efetivo, noite, supressão de tier). O último que nasceu sai do sorteio, a menos que seja o único elegível.
- **Spawn:** usa o mesmo anel fora da tela, por setores e com anti-kite, do spawn comum. O mini boss nasce com o herói como alvo e não é boss (`SetIsBoss` nunca é chamado). Sem ponto válido, tenta no frame seguinte, e o relógio não reinicia até nascer.
- **Contagem:** numa lista própria, fora da meta da horda. Entra na contagem de setores, no reposicionamento por distância, no `HandleClearAllEnemies` e na telemetria de vivos (`AliveCount` soma os dois).
- **Teste:** EditMode pra regra pura do sorteio, ou seja, elegíveis por tier, sem repetir o último e repetição permitida com um só elegível. O resto é checado no jogo.

## Fora do escopo

Mudar stats, animações ou loot dos mini bosses. Qualquer coisa do rework dos bosses ou do D9.5.

## Decisões minhas (o usuário confere)

- Os mini bosses nascem no anel fora da tela da horda, sem aviso.
- Os 30–40 s contam no tempo real do dia, igual à horda.
- Ficam fora da meta da horda, mas entram no reposicionamento e na contagem de setores.
- Loot e XP iguais aos de qualquer monstro comum (o que o prefab já tem).
- Nomes das pastas: `Assets/Prefabs/MiniBosses/tier_N` e `Assets/Settings/MiniBossDefinitions/`.
