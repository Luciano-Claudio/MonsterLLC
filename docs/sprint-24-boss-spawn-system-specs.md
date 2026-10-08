# Boss Spawn System — Especificação de Design (Decisão do Usuário)

> **Para quem for implementar (GDD + código):** este documento registra regras de design definidas pelo Luciano (dono do projeto) pro sistema de spawn de bosses, ainda dentro da Sprint 24 (Boss Framework + Boss Timer + Bosses do Floor 1). Tudo abaixo marcado como "decisão do usuário" foi dito explicitamente por ele — não é suposição. As "notas técnicas" são contexto do código já existente no projeto, pra evitar reinventar um padrão que já existe. As "perguntas abertas" no final NÃO foram decididas — precisam de confirmação antes de fechar a implementação.

## 1. Regras de spawn (decisão do usuário)

1. **Intervalo fixo de 10 segundos.** A cada 10s, nasce 1 boss no Floor.
2. **Rotação com preferência por bosses diferentes:**
   - Com N bosses disponíveis no Floor, a 1ª vez que o ciclo roda, a ORDEM é sorteada aleatoriamente entre os N (sem repetição dentro do próprio ciclo) — ex.: com 3 bosses, nasce 1 aleatório aos 10s, outro aleatório (excluindo o já escolhido) aos 20s, o último aos 30s.
   - **A aleatoriedade só acontece uma vez.** Depois que os N bosses já nasceram 1x cada (ciclo completo), o ciclo reseta e repete **a mesma ordem já sorteada da primeira vez** — não sorteia de novo a cada ciclo.
   - **Caso especial com 1 boss só:** nasce o mesmo boss repetidamente a cada 10s. Isso não exige nenhum código especial — é só o caso degenerado de N=1 na mesma lógica de rotação (uma lista de 1 elemento "embaralhada" sempre resulta nela mesma).
3. **O timer de 10s é por Floor e persiste entre trocas de andar.** Se o jogador passar 5s no Floor, subir/trocar de andar, e depois voltar, o próximo boss nasce em mais 5s (os 5s already elapsed não são perdidos/resetados — soma até completar os 10s).
4. **Local de nascimento:** igual aos monstros comuns — em qualquer ponto caminhável do Floor (não um ponto fixo de arena).
5. **Agressão imediata:** desde o instante em que nasce, o boss já tem o player como alvo e vai em direção a ele, mesmo que tenha nascido longe. Não entra em patrulha/idle esperando detectar o player por `observationRadius`.

## 2. Notas técnicas — como isso se encaixa no código já existente

Baseado no `FloorPopulationManager.cs` e `EnemyController.cs` atuais (já lidos), pra quem for implementar não reinventar o que já existe:

- **Item 3 (persistência do timer entre Floors) já "funciona de graça" se for implementado do jeito certo.** O projeto é single-Scene com Floor Sleep — `FloorPopulationManager` nunca é destruído/recriado ao trocar de Floor, só tem seu `Update()` gateado por `floorActive` (`FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)`). Se o timer do boss for um campo de instância comum (`float bossSpawnTimer`) num componente que segue o mesmo padrão — só incrementa quando o Floor está ativo, com um `if (!floorActive) return;` antes do incremento — ele automaticamente "congela" quando o jogador sai do Floor e retoma de onde parou quando ele volta. **Não precisa de nenhum save/load explícito nem estrutura de persistência** — é literalmente o mesmo gate que `FloorPopulationManager.Update()` já usa pra `aliveEnemies`/`respawnTimer`.
- **Item 4 (local de nascimento) reaproveita `TryGetRandomGraphPoint()`**, já existente em `FloorPopulationManager` — mesma lógica usada pra sortear ponto caminhável no GridGraph do Floor pros monstros comuns.
- **Item 5 (agressão imediata) precisa de um ponto de extensão novo em `EnemyController`.** Hoje, `isInCombat` e `combatLocked` são `private`, e o fluxo normal (`UpdatePatrol()`) só liga `isInCombat = true` quando `distanceToPlayer <= stats.observationRadius`. Pra um boss pular isso, alguém (o spawner do boss) precisa poder forçar esse estado assim que o `GameObject` nasce — ex.: um método público novo (`ForceImmediateAggro()` ou nome equivalente) que liga `isInCombat = true; combatLocked = true;` direto, chamado no mesmo momento em que o spawner já seta `enemyController.ownerFloor = ownerFloor` (mesmo ponto onde `FloorPopulationManager.SpawnOne()` faz isso pros monstros comuns).
- **O flag `isBoss` já existe** em `EnemyController` (hoje só usado pra ignorar knockback). Faz sentido o sistema de spawn de boss também ligar esse flag automaticamente ao invés de depender de cada prefab de boss já vir configurado certo no Inspector — mas isso é decisão de quem for implementar, não architecture obrigatória.

## 3. Perguntas que ainda NÃO foram decididas (não assumi nada sozinho)

1. **Bosses empilham?** Se o boss nascido aos 10s ainda estiver vivo quando o timer completar de novo aos 20s, nasce um segundo boss em cima do primeiro (ficando os dois vivos ao mesmo tempo), ou o spawn do próximo boss espera o anterior morrer?
2. **O timer reseta quando um boss morre?** Ex.: boss nasce aos 10s e é derrotado aos 13s — o próximo nasce aos 20s (timer nunca para) ou aos 23s (reinicia a contagem de 10s a partir da morte)?
3. **Todo Floor tem esse sistema, ou só Floors com boss configurado?** Presumo que o sistema seja opt-in por Floor (um array de `bossPrefabs` vazio = Floor sem boss, mesmo padrão seguro de "lista vazia não faz nada" já usado em `monsterPrefabs`), mas vale confirmar antes de fechar a implementação.

---

## 4. Resolução (decisão do usuário, confirmada antes do breakdown da Sprint 24)

1. **Bosses empilham: SIM.** O timer nunca espera — a cada 10s completados, nasce 1 boss, mesmo que o(s) anterior(es) ainda estejam vivos. Sem fila, sem limite de bosses simultâneos no Floor.
2. **O timer NÃO reseta com a morte de um boss.** É um relógio fixo e contínuo (os mesmos 10s acumulados por Floor, Seção 1 item 3) — a morte de um boss não acelera nem atrasa o próximo spawn.
3. **Todo Floor tem o sistema — não é opt-in.** Decisão de design: todo Floor eventualmente tem boss. Na prática, enquanto um Floor não tiver elenco de boss produzido (Floor 3+ antes da Deadline 8), o spawner ainda precisa tolerar um array `bossPrefabs` vazio sem quebrar — isso é um estado de produção incompleta, não uma feature de "Floor sem boss por design" (ver nota técnica do item 3 na Seção 2, acima).

GDD (Seção 22, "Boss Timer — regra final") e Plano de Produção (Deadline 6) já atualizados com essas decisões. Próximo passo: breakdown completo da Sprint 24.

---
*Documento gerado a pedido do usuário (Luciano) para ser repassado à IA responsável pela implementação (código + GDD). Após a atualização do GDD, trazer de volta para revisão e construção do breakdown completo da Sprint 24.*
