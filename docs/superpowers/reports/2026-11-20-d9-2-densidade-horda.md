# D9.2: relatório do teste de densidade da horda

**Data:** 2026-11-20. **Medição:** no Editor, com o harness `HordeDensityTest`. Os dados brutos estão em `DensityReports/density_20261120_060241.md`.

O Editor tem custo próprio, então os números servem pra comparar as etapas e achar o gargalo, não como FPS final. O orçamento de 60 FPS é de 16,6 ms por frame.

## Resultado

| Etapa | Vivos | FPS | ms médio | ms p99 | Lixo de memória (GC) por frame |
|---|---|---|---|---|---|
| Parado | 100 | 87 | 11,5 | 20,7 | 88 KB |
| Parado | 200 | 43 | 23,1 | 31,5 | 157 KB |
| Parado | 300 | 23 | 43,6 | 63,8 | 265 KB |
| Andando | 300* | 16–17 | 57–62 | 77–88 | 290–330 KB |
| 300* monstros + 200 efeitos | 300 | 12 | 80,6 | 103,7 | 285 KB |
| 300* monstros + 500 efeitos | 300 | **1** | 708,6 | 1409 | 531 KB |

\* Defeito do harness: ao baixar a meta de população, os monstros que já existiam não somem. Por isso todas as etapas depois da de 300 parado rodaram com 300 monstros, e não 100 ou 200. Pra conclusão isso não muda nada, porque essas etapas mostram 300 andando.

**Hoje o jogo aguenta cerca de 100 monstros vivos.** Acima disso, o FPS cai mais rápido do que o número de monstros cresce: com o triplo de monstros, o custo dos scripts sobe 5,6 vezes. Andando, o custo quase dobra.

## O que pesa, em ordem

| # | Sistema | 100 parado | 300 parado | 300 andando | Observação |
|---|---|---|---|---|---|
| 1 | **Scripts dos monstros** (`Update`) | 4,2 ms | 23,3 ms | ~42 ms | Gargalo nº 1 |
| 2 | **Lixo de memória (GC)** | 88 KB/frame | 265 KB/frame | ~300 KB/frame | Picos de travada (pior frame: 130 ms) |
| 3 | **Física 2D** | 0,4 ms | 4,2 ms | 4,5 ms | Monstros empilhados (2 colliders cada) |
| 4 | **Física com efeitos** | — | +14 ms (200) | **+580 ms (500)** | Colisão de trigger contra trigger explode |
| 5 | AIPath (A*) | 0,3 ms | 2,5 ms | 3,4 ms | RVO ligado nos 300 o tempo todo |
| 6 | Animator | 1,1 ms | 2,6 ms | 2,3 ms | Moderado |
| 7 | Criar/destruir objetos | 0,05 ms | 0,4 ms | ~0 | Irrelevante agora |

## Causas encontradas no código

1. **`EnemyController.SpeciesKey`** (coesão de bando): a cada chamada lê `go.name`, que cria uma string nova, e ainda faz um `Substring`. Roda 2 vezes por vizinho, por monstro, todo frame, e é a principal fonte do GC.
2. **`ApplyPackCohesion`**: faz um `OverlapCircle` de raio 5 por monstro, todo frame. Na horda densa, o círculo pega o máximo de vizinhos (o buffer é de 16) sempre.
3. **`MeleeEnemyController`**: faz `player.GetComponent<...>()` repetido por frame.
4. **Simulation Rings**: todo monstro em combate conta como "perto", então os 300 ficam com RVO ligado e decidem a IA todo frame, mesmo os que estão longe.
5. **Física dos efeitos**: os efeitos de teste estavam na layer `Default`, que colide com tudo, inclusive com os outros efeitos. Com 500 triggers sobrepostos, os pares de contato explodem. Isso é um aviso pros itens e projéteis da Deadline 9.

## Problema de jogo encontrado

**Monstros empilhados prendem o herói.** Com a horda densa, os colliders dos monstros grudam no herói e ele não consegue andar sem atacar. O teste precisou desligar a colisão entre o herói e os monstros nas etapas andando.

## Recomendação pro D9.3, por custo e benefício

1. **Zerar o GC dos monstros:**
   - Guardar a espécie como número no `Awake`.
   - Guardar as referências do herói no `MeleeEnemyController`.
   - Tirar os lambdas restantes do caminho quente.

   É barato e elimina os picos de travada.
2. **Limitar a coesão de bando e a checagem de multidão:** rodar a cada 0,2–0,3 s, e não todo frame. Ou substituir por uma separação simples. Ataca direto o gargalo nº 1.
3. **Simulation Rings por distância, mesmo em combate:** monstro longe do herói decide a IA com menos frequência e fica sem RVO.
4. **Matriz de colisão dos projéteis e efeitos:**
   - Criar uma layer própria que colide só com `Enemy`.
   - Pra área de dano curta, preferir consulta (`Overlap`) a trigger persistente.
   - Deixar o monstro com só 1 collider de física.
5. **Animator desligado fora da tela** (culling).
6. **Caminho único compartilhado (flow field) no lugar do A* por monstro:** só se, depois dos itens 1 a 3, o AIPath continuar relevante.
7. **Pooling de monstros:** fora do D9.3, porque criar e destruir hoje não pesa.

O **herói preso pelos monstros** é um problema de jogo, não de performance. Ele vai pro D9.4 (cerco/spawn) ou vira uma task própria. As opções são o herói atravessar monstros, os monstros manterem distância do herói, ou um collider de monstro menor.

## Próxima medição

Depois do D9.3, rodar o mesmo harness, corrigindo o defeito da população pra ela também descer entre etapas. O objetivo é **300 monstros andando a 60 FPS no Editor**. Depois disso, o harness é apagado.
