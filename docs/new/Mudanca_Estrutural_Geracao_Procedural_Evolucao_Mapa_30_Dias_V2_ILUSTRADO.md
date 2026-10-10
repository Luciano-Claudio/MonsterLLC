# **Mudança Estrutural do Projeto** 

_Complemento arquitetural — geração procedural, autotiles e evolução do mapa único durante os 30 dias_ 

Quero formalizar as regras do próprio mapa do jogo, complementando o documento “Mudança Estrutural do Projeto — mapa único, Effective Spawn Day, reciclagem por distância e progressão de bosses”. O novo objetivo é eliminar a necessidade de criar manualmente dezenas de mapas ou variantes completas: devemos gerar um mundo único, grande, circular, finito, coerente e visualmente mutável, usando as artes e os prefabs já existentes. 

IMPORTANTE: NÃO COMECE A PROGRAMAR. Primeiro analise o GDD Mestre, o documento anterior, as cenas, os Tilemaps, o pipeline URP/2D, o A* e o plano de produção. Faça análise de impacto, defina a arquitetura, documente as decisões e ajuste as sprints afetadas. Este arquivo é uma especificação de intenção e critérios; parâmetros ilustrativos não são decisões de balanceamento definitivas. 

## **1. OBJETIVO PRINCIPAL — UM ÚNICO MUNDO QUE EVOLUI** 

O jogo terá um mapa único, grande, finito e aproximadamente circular. Ele NÃO será uma sequência de 30 mapas, NÃO terá variantes completas obrigatórias por dia, e NÃO será reconstruído manualmente para cada mudança de cenário. 

- O jogador começa em região central com cenário natural, mas pode explorar em qualquer direção. 

- A distância em relação ao centro antecipa ambientes mais avançados e ameaças de dias futuros. 

- A passagem dos dias altera o cenário da MESMA região física, inclusive perto do centro. 

- Do Dia 1 ao Dia 2 mudanças podem ser discretas; do Dia 1 ao Dia 10 devem ser perceptíveis e profundas. 

- A transformação poderá afetar paleta, terreno, vegetação, água/lava, efeitos, ruínas e prefabs, respeitando regras de navegabilidade. 

- Tudo deve ser orientado a dados e reutilizar as artes existentes. 

Princípio visual: “Eu reconheço que estive aqui, mas o mundo não é mais o mesmo.” 

## **2. COMPATIBILIDADE COM O DOCUMENTO ESTRUTURAL ANTERIOR** 

Este documento acrescenta o sistema de geração/evolução visual. Não substitui as definições já aprovadas de ActualDay, CurrentPhase, Safe Zone, EffectiveSpawnDay, população, A*, Tier Suppression ou bosses. 

|**Conceito anterior**|**Regra preservada**|**Extensão para o mapa**|
|---|---|---|
|Mapa único|Mundo contínuo e finito|Construção procedural por<br>regiões/chunks|
|Safe Zone|Proíbe nascer inimigos; não bloqueia<br>perseguição|Proteção de conectividade e<br>legibilidade do centro|
|EffectiveSpawnDay|ActualDay + faixas radiais após borda<br>efetiva da Safe Zone|Fonte de referência de ameaça; NÃO<br>deve ser igualado automaticamente ao<br>bioma|
|CurrentPhase|Dia/Noite real, 50/50 da duração|Iluminação e atmosfera temporárias<br>separadas da transformação diária|
|Population/A*|Spawns em pontos caminháveis<br>validados|Colisores e grafo devem acompanhar<br>terrenos alterados|



Não usar a fórmula simplificada distanceFromCenter / 100 como regra definitiva: o documento anterior determina que a distância útil para spawns começa após SafeZoneRadius + SpawnSafetyPadding. DistancePerSpawnDay é configurável e o limite espacial decorre do mapa finito. 

Evolução Procedural do Mundo — documento de trabalho editável  |  1 

## **3. TRÊS DIMENSÕES DISTINTAS DE PROGRESSÃO** 

É obrigatório separar: (A) tempo real da run; (B) deslocamento radial; (C) identidade/evolução ambiental. Eles se influenciam, mas não são equivalentes. 

ActualDay + CurrentPhase + DistanceDayOffset -> EffectiveSpawnDay -> Spawn Table (mecânica existente) 

ActualDay + SpatialEnvironmentOffset + perfil geográfico -> EnvironmentState (novo sistema visual) 

SpatialEnvironmentOffset pode usar a mesma geometria radial do avanço de spawn, mas sua curva, suas transições e sua intensidade devem ser independentes e editáveis. EnvironmentState é CONTÍNUO ou discretizado em estágios visuais específicos, sem impor Dia = Tier ou “uma zona fixa de Tier”. 

A geometria radial, com variação de ruído, cria influência crescente e bordas orgânicas; nenhuma direção cardeal deve ser associada rigidamente a um tier. 

## **4. MAPA FINITO, CIRCULAR E CENTRO FIXO** 

- O mundo terá limites definidos e configuráveis: WorldCenter, WorldRadius/WorldMask e CellSize. 

- Usar distância euclidiana em coordenadas consistentes (células ou unidades), inclusive diagonais. 

- Máscara circular pode ser irregularizada visualmente sem eliminar o limite navegável. 

- O mapa deverá permanecer grande, mas finito; a quantidade de faixas radiais úteis depende do raio caminhável. 

- Não definir ainda raio exato, chunks totais ou quantidade exata de faixas: medir em protótipos. 

- Gerar limites visualmente justificáveis (montanhas, abismos, oceano, ruínas, etc.) e evitar bordas artificiais abruptas. 

## **5. GEOGRAFIA DE REFERÊNCIA E ESTADO TRANSFORMÁVEL** 

Cada posição do mundo possui dados geográficos persistentes (forma do lago, caminhos gerais, topografia abstrata, ocupação de estruturas) e uma materialização ambiental que muda de acordo com o dia e a posição. 

“Persistente” NÃO significa que um lago precisa continuar sendo água. Significa que é possível reconhecer o local e transformar o material/estado de uma região sem perder a consistência espacial. 

|**Dado**|**Exemplo**|**Persistência**|
|---|---|---|
|Identidade geográfica|Contorno de lago e corredor de<br>passagem|Preferencialmente estável por seed|
|Material de superfície|Água -> água sombria -> lava|Variável por estado ambiental|
|Objeto ambiental|Árvore viva -> seca -> queimada|Pode mudar ou desaparecer por regra|
|Estrutura/landmark|Ruína com entradas e fundação|Módulo persistente ou variante<br>aprovada|
|Edição do jogador|Árvore derrubada, baú aberto|Delta salvo e respeitado no próximo<br>estado|



## **6. PROGRESSÃO VISUAL E PERFIS DE AMBIENTE** 

Os cenários apresentados foram agrupados de forma inicial, sem impor um tileset único por tier. O bestiário informa temática e ameaça; a tabela de spawns controla monstros, não a porcentagem de biomas. 

|**Referência**|**Identidade visual**|**Exemplos de conteúdo**|
|---|---|---|
|T1–T3 / Natural|Campos, florestas, água, ruínas,<br>deserto e áreas destruídas|Gramas variadas, árvores, trilhas,<br>rochas, rios, aldeias em ruínas|
|T4–T5 / Sombrio-místico|Floresta morta e bioluminescente|Terra escura, árvores secas,<br>cogumelos/cristais, flora azul-roxa|



Evolução Procedural do Mundo — documento de trabalho editável  |  2 

|T6 / Corrompido|Solo escuro, flora hostil e decadência|Plantas vermelhas, ossadas, raízes<br>retorcidas|
|---|---|---|
|T7–T8 / Infernal|Lava, pedra negra, ruínas rituais|Rios de lava, colunas, templos, altares,<br>árvores mortas|
|T9 / Abissal|Vazio escuro e arquitetura abandonada|Ruínas negras, elementos arcanos, flora<br>residual estranha|
|T10 / Provisório|Mesmo repertório do T9 por enquanto|Suporte futuro a perfil divino<br>independente|



Esses rótulos são famílias de assets e tendências visuais. Uma região pode mesclar famílias e subambientes, em vez de mudar todo o chão ao atingir uma fronteira de dia. 

## **7. BIOMAS, SUB-BIOMAS E TRANSIÇÕES** 

- BiomeProfile define famílias de terreno, overlays e objetos aceitos; SubBiomeProfile cria variações internas (clareira, floresta densa, ruína, lago, campo rochoso). 

- Cada região deve escolher composição por campos determinísticos de ruído e pesos, formando manchas com continuidade espacial. 

- Faixas radiais NÃO podem resultar em anéis perfeitos perceptíveis; aplicar deslocamento por ruído com amplitude e largura configuráveis. 

- Transições de visual são graduais e diferentes do degrau discreto usado na tabela de EffectiveSpawnDay. 

- Interseções incompatíveis de terrenos precisam de prioridade, sprite de transição adequado ou faixa intermediária; nunca produzir bordas inexistentes. 

## **8. DISTRIBUIÇÃO DE TERRENO POR PORCENTAGENS** 

O usuário poderá definir metas como “70% grama predominante e 30% outros terrenos”, com composição adicional de água, terra e pedras. As porcentagens deverão esclarecer o denominador: terreno total da região, área caminhável, ou subconjunto compatível. 

Não sortear cada célula de modo independente. Usar máscaras coerentes com manchas, ruído de múltiplas frequências, limiares e pós-processamento de regiões. 

- Porcentagens são metas de geração, não garantia matemática local em cada chunk. 

- Medir cobertura em janelas maiores e oferecer opção futura de calibração para aproximar percentuais agregados. 

- Separar % do terreno-base da % de overlays internos (ex.: grama alta dentro da grama). 

- Não permitir duplicação incoerente de porcentagens entre classes mutuamente exclusivas e overlays. 

- Oferecer debug de cobertura real gerada versus meta por seed/dia/bioma. 

## **9. AUTOTILES E VARIAÇÕES FECHADAS DE GRAMA** 

Gramados altos, manchas de terra, lama, vegetação densa e superfícies com bordas pixel-art NÃO podem aparecer como quadrados isolados. A unidade procedural será uma região conectada; cada célula recebe sprite conforme vizinhança. 

1. Gerar máscara de ocupação do overlay ou do terreno conectado. 

2. Identificar vizinhos necessários (4/8 vizinhos, conforme organização das artes). 

3. Resolver bordas, cantos externos, cantos internos, isolados e peças centrais. 

4. Selecionar variantes de sprites internos para diminuir repetição sem alterar conectividade. 

5. Resolver transições com outros terrenos, ou aplicar regras explícitas de incompatibilidade. 

6. Recalcular vizinhanças após alterações de dia, inclusive células na borda de chunks. 

Evolução Procedural do Mundo — documento de trabalho editável  |  3 

Avaliar Rule Tile, Rule Override Tile e/ou bitmask customizado conforme os sprites reais. Não assumir que um único Rule Tile resolve qualquer tileset de 16/47 peças ou sobreposições de múltiplos tipos. 

## **10. MODELO DE TILEMAPS E ORDEM VISUAL** 

Usar poucos Tilemaps por responsabilidade de renderização/colisão, não um Tilemap por cada curva ou segmento de parede. Exemplo conceitual: BaseGround, GroundTransitions, GroundOverlays, ObstacleTiles, CollisionTiles e opcionalmente Decorations. 

No contexto atual de URP 2D e sorting por pivot/Y, distinguir claramente piso de objetos altos: árvores e estruturas com topo acima da base podem exigir múltiplos sprites/sorting groups ou prefabs com pivots adequados. 

- Não misturar indiscriminadamente estruturas verticais em um Tilemap com sorting fixo que recorta o jogador. 

- Separar footprints de colisão, parte visual e ponto de sorting. 

- Validar TilemapRenderer individual/chunk mode, SortingGroup e transparências com o renderer/URP efetivamente ativo. 

- Priorizar consistência e custo de draw calls antes de proliferar dezenas de Tilemaps. 

## **11. ÁGUA, LAVA, PAREDES E COLISORES** 

Água e lava devem possuir contornos procedurais completos, sprites apropriados de margem e regras de jogabilidade separadas da aparência. Não assumir que toda água bloqueia o jogador ou que toda lava tem exatamente o mesmo tipo de colisão. 

- Gerar máscaras contínuas para corpos d’água/lava; impedir poças pontuais indesejadas e lacunas de borda. 

- Configurar por tipo: bloqueio físico, dano, desaceleração, atravessável ou acesso condicionado. 

- Atualizar TilemapCollider2D/CompositeCollider2D quando a máscara efetivamente mudar. 

- Sincronizar walkability do A* ou do sistema de navegação com a superfície atual. 

- Não transformar uma posição ocupada em lava ou obstáculo sem validação de segurança. 

## **12. GENERAÇÃO POR CHUNKS E COSTURAS** 

A geração será dividida em chunks determinísticos, carregados conforme a posição do jogador, em vez de manter todos os tiles/prefabs ativos. 

ChunkKey = (chunkX, chunkY) ; Geometry = F(WorldSeed, WorldCell) ; Visual = G(Geometry, ActualDay, SpatialProfile) 

- ChunkSize inicial para testes: 32×32 ou 64×64 células (proposta, não decisão final). 

- Vizinhos devem ser consultados para ruído, rios, autotile, estruturas e colisão, mesmo quando estão descarregados. 

- Usar margem de avaliação (“halo”/ghost cells), gerando dados vizinhos antes de resolver bordas. 

- Chaves e seeds derivadas de coordenadas globais, sem depender da ordem de carregamento. 

- Descarregar visual e física de chunks fora do raio, preservando estado necessário e deltas. 

- Evitar geração síncrona pesada durante movimento; definir orçamento por frame e pré-carregamento. 

## **13. DETALHES E PREFABS DE ENVIRONMENT** 

Cada elemento cadastrado terá regras de solo aceito, família ambiental, estado diário, densidade, chance, tamanho, footprint, espaçamento e colisão. O spawn de enviroments não é o spawn de monstros. 

Evolução Procedural do Mundo — documento de trabalho editável  |  4 

|**Categoria**|**Exemplos**|**Estratégia**|
|---|---|---|
|Micro deco|Flores, folhas, ossos, pequenas pedras|Máscaras de densidade;<br>preferencialmente sem GameObject|
|Props médios|Árvores, rochas, cogumelos, cristais|Prefabs com footprint, sorting e<br>espaçamento|
|Macro estruturas|Ruínas, aldeias, templos, cemitérios|Prefabs modulares e reserva antecipada<br>da área|



Colônias de árvores/flora devem usar ruído de densidade e agrupamentos; não fazer apenas Random.Range 

por tile. Objetos não devem ocupar entradas, corredores críticos, margens inválidas ou footprints já reservados. 

## **14. MACROESTRUTURAS SEM MAPEAR ÁREAS INTEIRAS** 

Construções complexas são montadas com módulos artísticos previamente preparados (prefabs/tiles de composição), não colocando parede por parede por sorteio irrestrito. 

- Cada módulo declara footprint, pontos de conexão, entrada, categoria e estado/variante. 

- Posicionar primeiro grandes estruturas e seus corredores; preencher terreno e props ao redor depois. 

- Permitir variantes para ruínas naturais, amaldiçoadas, infernais e abissais quando existir arte correspondente. 

- Validar colisão, entradas transitáveis, distâncias mínimas e espaço de combate. 

- Não gerar estruturas de grande porte em área já ocupada pelo jogador ou por ponto permanente essencial. 

## **15. EVOLUÇÃO DO MESMO LUGAR ENTRE DIAS** 

Ao começar um novo ActualDay, a região física pode receber substituições de terreno e ambiente. Exemplo permitido: lago anteriormente azul torna-se lava; grama clara vira solo escuro; árvores vivas são substituídas por troncos secos. 

Não exigir evolução monotônica de TODO tile. Cada tipo pode ter estágios e probabilidades próprias; parte da região pode conservar elementos antigos, criando memória visual e transição orgânica. 

EnvironmentState = EvaluateEnvironment(WorldSeed, GlobalPosition, ActualDay, EvolutionProfile) 

O gerador deve calcular a aparência final a partir dos dados e do dia corrente, e NÃO aplicar 30 operações cumulativas obrigatórias. Assim o Dia 10 fica igual mesmo se os chunks dos Dias 2–9 nunca estiveram carregados. 

## **16. COMPORTAMENTO DA TROCA DE DIA** 

A atualização visual significativa ocorrerá quando ActualDay avançar. CurrentPhase (dia/noite dentro do mesmo dia) afeta iluminação e atmosfera, sem forçar regeneração total do terreno a cada 50% do cronômetro. 

7. Persistir estado da run e deltas relevantes do jogador. 

8. Incrementar ActualDay pelo fluxo oficial existente. 

9. Calcular estados novos dos chunks ativos e marcar os demais como desatualizados. 

10. Atualizar em lotes com prioridade para área central e próxima do jogador. 

11. Reconstruir colliders e navegação apenas onde a geometria/material físico mudou. 

12. Validar posição do jogador e entidades, resolver sobreposição sem teleporte injustificado. 

13. Concluir atualização antes de liberar controle pleno, quando necessário. 

Evolução Procedural do Mundo — documento de trabalho editável  |  5 

Como o documento estrutural anterior ainda não fixa se o novo dia sempre começa no centro, a implementação deve suportar ambos os cenários até que a regra de retorno seja confirmada. Não presumir retorno obrigatório. 

## **17. SAFE ZONE, CENTRO E EVOLUÇÃO AMBIENTAL** 

A Safe Zone permanece uma zona de PROIBIÇÃO DE SPAWN, não de invulnerabilidade. Sua existência não implica congelar visualmente o centro em grama verde para sempre. 

- A área pode evoluir visualmente com os dias, inclusive no solo e nos objetos. 

- Proteger zonas fundamentais de retorno/loja/interação da destruição física ou garantir variantes compatíveis. 

- A posição de spawn do jogador precisa continuar segura e acessível após cada transformação. 

- Não tornar automaticamente a Safe Zone bloqueada para A* nem permitir spawn de inimigos nela. 

- Diferenciar restrição de spawn, área de proteção de estruturas e área de navegação. 

## **18. PASSABILIDADE, CONECTIVIDADE E VALIDAÇÃO ESPACIAL** 

Uma mudança água -> lava, grama -> rocha ou árvore -> obstáculo pode criar bloqueios. A navegação do mundo deve ser verificada antes e depois de evoluções. 

- Manter rede mínima de caminhos conectando o centro às principais regiões navegáveis. 

- Reservar corredores e garantir saídas locais mesmo com terrain variation. 

- Testar conectividade por flood fill ou análise de componentes no grafo de navegação, em resolução adequada. 

- Realocar props ou revisar máscaras quando fecharem um gargalo obrigatório. 

- Separar Walkable de Spawnable conforme o documento anterior. 

- Mapear alterações de colisão para atualização incremental de A*/GridGraph, evitando scan completo por frame. 

## **19. SAVE, SEED E EDIÇÕES DO JOGADOR** 

Salvar WorldSeed, versão do gerador, ActualDay, configurações estáveis e principalmente DELTAS de alterações do jogador. O mapa base pode ser regenerado; ações persistentes não. 

|**Situação**|**Regra requerida**|
|---|---|
|Árvore destruída|Não reaparece simplesmente quando o chunk<br>descarrega/recarrega|
|Lago que vira lava|Mantém identidade/forma derivada da seed com material do<br>dia atual|
|Ruína saqueada|Estado de interação é preservado mesmo quando o visual<br>muda|
|Atualização de versão|Definir estratégia de migração ou invalidação explícita de<br>saves|
|Ordem de carga|Chunk A e B produzem mesmos resultados<br>independentemente da ordem|



Avaliar precedência entre delta do jogador e evolução ambiental (por exemplo: árvore cortada não revive ao trocar de perfil). Essas regras deverão ser explícitas por categoria de objeto. 

## **20. DETERMINISMO, SEEDS E REPRODUTIBILIDADE** 

Uma mesma seed, coordenada, versão de regras e ActualDay deve gerar exatamente o mesmo terreno visual e físico antes dos deltas salvos. 

Evolução Procedural do Mundo — documento de trabalho editável  |  6 

- Evitar dependência de Random global, Time.time e da ordem de Update ou de carregamento. 

- Derivar canais independentes de aleatoriedade para terreno, variantes, rios, props e estruturas. 

- As bordas de chunks devem coincidir no mesmo estado diário. 

- 

- Adicionar ferramenta “copiar seed + célula + dia + versão” para reproduzir bugs de geração. 

## **21. PERFORMANCE, URP E FÍSICA** 

O mundo é visualmente grande, mas apenas um entorno do jogador deve ser renderizado e simulado. A arquitetura deve respeitar hordas, Employees e demais sistemas descritos no GDD. 

- Pooled prefabs ambientais quando pertinente; evitar milhares de GameObjects para decorativos que podem ser tiles. 

- Atualização em batch das células alteradas em Tilemaps; minimizar chamadas SetTile individuais durante 

   - gameplay. 

- Composite Collider para regiões de piso sólido adequadas e rebuild apenas sob necessidade. 

- Separate rendering range / physics range / prefetch range para chunks. 

- Evitar dezenas de Tilemaps e sorting groups desnecessários por chunk; medir perfil de draw calls. 

- Validar compatibilidade com Pixel Perfect Camera, URP 2D Renderer e sorting Y existente. 

## **22. FERRAMENTA DE AUTORIA E PRÉ-VISUALIZAÇÃO NO EDITOR** 

Criar um EditorWindow ou ferramentas equivalentes para que o usuário configure artes e veja resultados sem construir mapas manualmente. 

- Selecionar WorldSeed, ActualDay, coordenada/região e raio de preview. 

- Comparar a MESMA área em dias diferentes (Dia 1, 3, 5, 10, 20, 30). 

- Cadastrar Tileset/RuleTile, overlays, prefabs, densidade, compatibilidades e estágios ambientais. 

- Exibir heatmaps de família visual, cobertura de terreno, obstáculos, lago/lava, walkability e centro/Safe Zone. 

- Botões Generate Preview, Regenerate, Validate, Clear Preview e Export Diagnostics. 

- Warnings para Rule Tiles incompletos, materiais sem transição, colisão ausente, interseções de footprints, configs inválidas. 

- Gerar preview em cena de teste ou modo isolado, sem modificar silenciosamente a cena principal. 

## **23. SCRIPTABLEOBJECTS E ARQUITETURA DATA-DRIVEN** 

|**Asset/Serviço proposto**|**Responsabilidade**|
|---|---|
|WorldGenerationSettingsSO|Seed padrão, limites, grid, chunk size, orçamento e regras<br>espaciais|
|TerrainPaletteSO|Terrenos base, variantes, bordas e compatibilidades|
|ConnectedOverlaySO|Grama alta, manchas conectadas e sprites/bitmask de<br>transição|
|BiomeProfileSO|Famílias ambientais, pesos, subzonas e props elegíveis|
|WorldEvolutionProfileSO|Curvas/stages por dia e distância; transformação de terreno e<br>objetos|
|EnvironmentPropDefinitionSO|Prefab, footprint, colisão, pivô, densidade e filtros|
|StructureDefinitionSO|Composição macro, entradas, footprint e regras de exclusão|
|ChunkGenerator / ChunkStreamer|Geração determinística e streaming de regiões|
|WorldEvolutionManager|Invalidação/aplicação das mudanças de ActualDay|
|WorldPersistenceManager|Seed/versão + deltas de gameplay|



Os nomes são propostas arquiteturais, não obrigatoriedade de criar uma classe por cada linha antes de validar o protótipo. Evitar abstração excessiva. 

Evolução Procedural do Mundo — documento de trabalho editável  |  7 

## **24. PIPELINE RECOMENDADO DE GERAÇÃO** 

14. Ler WorldSeed, posição global e estado ambiental solicitado. 

15. Calcular máscaras gerais: limite do mundo, reservas do centro, geografia estável, canais de passagem. 

16. Determinar mistura de famílias visuais e sub-biomas na região. 

17. Gerar superfícies base e manchas conectadas de terrenos. 

18. Reservar e posicionar landmarks/estruturas, respeitando entradas. 

19. Resolver overlays, margens, bitmasks e todas as vizinhanças, inclusive além do chunk. 

20. Distribuir microdecorações e prefabs por máscaras, exclusão e espaçamento. 

21. Construir colliders, marcadores de walkability e dados espaciais para A*. 

22. Aplicar deltas salvos, validar conectividade e resolver conflitos. 

23. Publicar a região para renderização e simulação sem travamentos perceptíveis. 

A ordem pode ser refinada após protótipo; elementos macro que alteram geografia devem ser reservados antes de “finalizar” água, passagens e colisões. 

## **25. REGRAS DE COMPATIBILIDADE COM MONSTROS E BESTIÁRIO** 

Os antigos “andares” do bestiário são Enemy Tiers. O mapa não será dividido em regiões fixas com um único Tier. O sistema de ambiente não sorteia diretamente os monstros. 

- Não alterar tabela Day/Night, EffectiveSpawnDay, Tier Suppression, população ou regras de bosses por causa do tileset. 

- Uma área visualmente infernal pode ter monstros de tiers anteriores, conforme a tabela efetiva. 

- A identidade ambiental pode antecipar a ameaça, mas sem prometer correspondência 1:1 entre terreno e monstro. 

- Não implementar regras especiais de boss nesta tarefa; apenas garantir regiões navegáveis e válidas para entidades já previstas. 

## **26. PARÂMETROS CONFIGURÁVEIS — NÃO FIXAR SEM TESTE** 

|**Parâmetro**|**Ponto de partida ilustrativo**|**Observação**|
|---|---|---|
|ChunkSize|32×32 ou 64×64|Benchmark antes de decidir|
|DistancePerSpawnDay|Conforme documento estrutural<br>anterior|Não hardcode 100 blocos|
|BiomeBlendWidth|Configurável|Largura de transição orgânica|
|NoiseScale / Octaves|Configurável|Controla manchas e densidade|
|TerrainCoverage|Ex.: 70% grama, demais terrenos 30%|Validar denominador/cobertura|
|PropMinSpacing|Por definição de prefab|Medido em células/unidades|
|VisualEvolutionCurve|Curvas/stages por dia|Sem mapa específico por dia|
|Render/Physics/PrefetchRadius|Configuráveis separadamente|Perfil de desempenho|



## **27. CASOS DE TESTE E CRITÉRIOS DE ACEITE** 

24. A mesma seed e o mesmo dia produzem exatamente a mesma área ao visitar o chunk por rotas diferentes. 

25. Uma mancha de grama alta cruza a fronteira de dois chunks sem linha de corte e com cantos corretos. 

26. Uma área de grama base apresenta variações conectadas naturais; não vira xadrez aleatório. 

27. Água, lava e terra fecham margens corretamente sem sprites ausentes ou buracos. 

28. No Dia 10, a mesma coordenada do Dia 1 apresenta evolução visual substancial nas configurações projetadas. 

29. Uma transformação água -> lava atualiza renderização, dano/colisão e A* de acordo com as regras. 

30. Jogador e interações essenciais não nascem nem ficam presos por atualização de dia. 

Evolução Procedural do Mundo — documento de trabalho editável  |  8 

31. Árvore cortada, baú aberto e objeto persistente não reiniciam após chunk reload. 

32. Safe Zone segue spawn-free mas continua caminhável e sujeita a perseguição. 

33. O mapa permanece finito, circular e sem “zonas de tier” rígidas. 

34. As curvas ambientais podem ser editadas no Inspector sem alterar código de gameplay. 

35. Geração e transição de chunks ficam dentro do orçamento de frame definido nos testes. 

## **28. PLANO DE IMPLEMENTAÇÃO — PROTÓTIPO ANTES DE ESCALA** 

|**Etapa**|**Escopo**|**Saída verificável**|
|---|---|---|
|P0 — Auditoria|GDD, Tilemaps, URP, A*, saves, assets<br>de terreno|Mapa de dependências e riscos|
|P1 — Terreno natural|Seed + base + grama alta autotile +<br>terra/água|Preview de 128×128 células|
|P2 — Chunks|Costuras + streaming + caching|Mesma seed sem linhas entre chunks|
|P3 — Props|Árvores, pedras, footprint, sorting Y|Composições naturais sem bloqueios|
|P4 — Evolução|Mesmo local em 2+ estados diários|Troca água/lava e atualização física|
|P5 — Biomas|Mistura progressiva radial e subzonas|Transição natural sem anéis duros|
|P6 — Estruturas|Ruínas/templos modulares e<br>corredores|Landmarks navegáveis|
|P7 — Save/QA|Deltas, regressões e benchmark|Run reproduzível e estável|



As etapas são dependências técnicas sugeridas, NÃO equivalem automaticamente a uma sprint inteira. Dimensionar tarefas conforme esforço real; adaptar as 14 Deadlines / 56 Sprints existentes, sem inventar trabalho para preencher semanas. 

## **29. REVISÃO DO GDD E DO ROADMAP** 

Após a auditoria, identificar com precisão quais seções existentes devem incorporar: World Generation, Biome & SubBiome Profiles, Terrain/Autotile Pipeline, Day Evolution, Chunk Streaming, Navigation Sync, Environment Prefabs, Macro Landmarks, World Seed & Save Deltas e Editor Preview. 

Remover o plano “10 Floors × 5 Variants = 50 Floor Variants” como obrigação de produção, se ainda estiver presente. Preservar assets reutilizáveis, mas migrar o conceito de variantes para componentes/estados de bioma e gerações por seed. 

Manter a filosofia do documento anterior: o sistema novo deve simplificar o trabalho manual, não substituir 50 mapas por centenas de ScriptableObjects ou regras impossíveis de manter. 

## **30. ETAPAS DE REVISÃO SOLICITADAS ANTES DE PROGRAMAR** 

36. Impact Analysis: classificar sistemas atuais em KEEP / ADAPT / DELETE / REPLACE, com referência concreta aos scripts, cenas e GDD. 

37. Inventory de Assets: listar tilesets, sprites de borda, overlays, prefabs, tamanhos, pivots e lacunas visuais reais. 

38. Architecture Decision Record: decidir Rule Tiles versus bitmask, chunk size, Tilemap layout, salvar deltas e custos de A*. 

39. Generation Prototype Specification: formalizar P1/P2 com testes determinísticos e critérios de visual. 

40. Evolution Contract: definir quais materiais/objetos podem trocar, persistir, desaparecer ou manter footprint. 

41. World Progression Integration: ligar ActualDay e distância ao visual sem acoplar a tabela de spawns. 

42. GDD Update: inserir seções novas e eliminar obrigações de Floor Variants. 

43. Roadmap Update: atribuir tarefas realistas e dependências às sprints certas. 

Evolução Procedural do Mundo — documento de trabalho editável  |  9 

44. Migration Plan: indicar como código existente e cenas serão preservados/adaptados sem reescrita generalizada. 

45. Test Plan: testes de costura, conectividade, física, sorting, persistência e desempenho antes da expansão para T1–T10. 

## **31. PONTOS EM ABERTO — NÃO INVENTAR DECISÕES** 

- O jogador sempre retorna ao centro no início de um novo ActualDay, ou pode começar onde terminou? Arquitetura deve tolerar ambas até decisão. 

- Quais elementos do centro/loja são permanentes e imunes a alterações estruturais? 

- Quais sprites reais possuem cantos internos/externos suficientes para autotile completo? 

- Água/lava são bloqueio, superfície de dano, ou dependem do estado do personagem? 

- Rios, estradas e passagens podem mudar de traçado ou apenas de material? 

- Seed fixa por run ou compartilhada entre runs? Não assumir alteração randômica obrigatória a cada partida. 

- Existem recursos mineráveis/árvores destruíveis cuja regeneração faz parte de gameplay? 

- Qual é o limite real do mundo e o budget alvo de memória/frame? 

## **32. PRINCÍPIO FINAL E RESULTADO ESPERADO** 

Quero abrir a Unity, cadastrar os sprites de chão, seus autotiles e transições, configurar onde árvores/rochas/ruínas podem aparecer e informar como os ambientes evoluem ao longo dos dias. A ferramenta gera um mundo grande, consistente, com margens corretas e objetos posicionados por regras, e o mesmo mundo se transforma quando ActualDay avança. 

Dia 1 e Dia 2 podem ser semelhantes; Dia 1 e Dia 10 devem parecer etapas nitidamente diferentes da evolução do mesmo lugar. À medida que se distancia do centro, o jogador antecipa visualmente regiões mais ameaçadoras, sem zonas fixas de um único Tier. Tudo ocorre sem a criação manual de 30 mapas, respeitando o mundo finito, a Safe Zone, o EffectiveSpawnDay, o A*, o sorting da URP e o save. 

NÃO comece a implementação até concluir a revisão arquitetural, obter o inventário de assets e ajustar o GDD e o roadmap. Questione incompatibilidades das artes e custos técnicos reais em vez de prometer autotile universal ou geração perfeita sem validação. 

Evolução Procedural do Mundo — documento de trabalho editável  |  10 

# **ANEXO TÉCNICO ILUSTRADO — COMO O GERADOR PROCEDURAL DEVE FUNCIONAR** 

Complemento operacional à versão anterior. Este anexo detalha o fluxo de autoria dos assets na Unity, a geração efetiva dos Tilemaps e o comportamento de cada família de Tile, sem alterar as regras já definidas para Safe Zone, EffectiveSpawnDay, monstros ou bosses. 

## **33. CONTRATO DE AUTORIA: A UNITY RECEBE OS TILES JÁ CONFIGURADOS** 

O sistema procedural NÃO deve obrigar o usuário a importar sprites brutos e reconstruir manualmente regras em código. O fluxo principal deve permitir arrastar assets Unity já existentes: Tile, RuleTile, AnimatedTile e prefabs. Cada item é registrado em um catálogo configurável (ScriptableObjects), identificado pelo tipo e pelas regras de colocação. O editor não deve converter silenciosamente um AnimatedTile em Tile estático nem desmontar o RuleTile em sprites independentes. 



<!-- Start of picture text -->
DO ASSET DO PROJETO AO MAPA GERADO<br>TILE SIMPLES RULE TILE ANIMATED TILE PREFAB<br>Base / decoracao Bordas por vizinhos Frames + velocidade Objeto + collider<br>CATALOGO DE ASSETS (ScriptableObjects)<br>Tipo, regras, biomas, pesos e evolucées<br><!-- End of picture text -->

_Figura 1 — Do asset já configurado até as camadas do mapa._ 

|Asset recebido|O que o sistema usa|Exemplo|
|---|---|---|
|Tile simples|Referência TileBase, peso,<br>superfície permitida|Grama base ou pedrinha|
|RuleTile / autotile|Referência TileBase + máscara<br>espacial de mesma família|Grama alta com bordas|
|AnimatedTile|Referência TileBase + área em<br>que pode ocorrer|Água ou lava animada|
|Prefab|Referência GameObject + regras<br>espaciais / footprint|Árvore, ruína, rocha|



Compatibilidade: TileBase deve ser a interface de colocação na camada de tiles; metadados do gerador ficam em definitions próprias. Nem todo AnimatedTile é um autotile, e nem todo RuleTile tem animação. Caso seja exigida uma combinação (regra de borda + animação), ela deve ser suportada por uma implementação explicitamente compatível, uma composição de camadas ou um Tile customizado — nunca assumida automaticamente. 

Evolução Procedural do Mundo — documento de trabalho editável  |  11 

## **34. TIPOS DE ASSET E COMPORTAMENTO EXATO DO GERADOR** 

### **34.1. Tile simples** 

Recebe um Tile já pronto. O sistema o coloca na célula escolhida quando a categoria correspondente for selecionada pela máscara de terreno. Permitir múltiplas variantes com pesos internos para reduzir repetição visual, mas somente após fixar a categoria lógica daquela célula. Tile simples de decoração não deve sobrescrever o tile base; usar camada ou posição de decoração apropriada. 

### **34.2. RuleTile / autotile** 

Recebe um RuleTile previamente criado e configurado com sprites de interior, lados e cantos. O gerador NÃO sorteia as bordas: primeiro determina uma máscara lógica contínua de grama alta; depois coloca o RuleTile nas células da máscara. O RuleTile avalia os vizinhos para escolher sprites compatíveis. As regras de vizinhança devem trabalhar com identidade de terreno, e não apenas com igualdade acidental de sprite. 



<!-- Start of picture text -->
MASCARA DE REGIAO -> AUTOTILE CONECTADO<br>1. Algoritmo escolhe as células 2. Rule Tile escolhe as bordas<br>EERE 6B<br>| | [||| oo Td LT TTY<br>|||| | | | | |_|[||| | | |  ai ||| LT[ {|  [||||<br>PTT yy | | i |<br>SERRE ae BRE ae<br>As bordas sao resolvidas a partir da mascara ldgica, e ndo sorteadas isoladamente.<br><!-- End of picture text -->

_Figura 2 — O gerador forma a mancha; o autotile decide o acabamento visual._ 



<!-- Start of picture text -->
AUTOTILE: VIZINHOS E ESCOLHA DA PECA<br>X = célula atual<br>Amostra os 8 vizinhos (3 x 3).<br>Calcula mascara / padrao de borda.<br>Usa variantes apenas no interior.<br>compativeis com o modelo do tileset.<br><!-- End of picture text -->

Evolução Procedural do Mundo — documento de trabalho editável  |  12 

_Figura 3 — Leitura de vizinhos para bordas, interior e cantos._ 

Importante: os modelos de 4 vizinhos, 8 vizinhos e 47 peças não são intercambiáveis sem conferir a organização dos sprites. O projeto deve registrar qual padrão cada tileset aceita. Para encontros entre várias categorias (grama, terra, areia), criar regras de prioridade e transição explícitas, com fallback visual controlado. 

### **34.3. AnimatedTile** 

Recebe o AnimatedTile já configurado com os frames e o intervalo/velocidade fornecidos pelo asset. O gerador escolhe a posição, coloca o tile animado na camada adequada e deixa a Unity atualizar seus frames. A animação NÃO altera a geografia, a máscara lógica nem o collider a cada frame. Água animada com margens requer separar a superfície interna animada das bordas estáticas ou criar um sistema híbrido explicitamente definido. 



<!-- Start of picture text -->
ANIMATED TILE: POSICAO FIXA, SPRITES EM CICLO<br>FRAME 1 FRAME 2 FRAME 3 FRAME 4<br>t=0,00s t=0,25s t=0,50s t=0,75s<br>© mapa mantém a mesma célula e a mesma méscara; somente o frame visual muda<br><!-- End of picture text -->

_Figura 4 — Quatro frames do mesmo tile; a célula e a colisão permanecem estáveis._ 

### **34.4. Prefabs** 

Prefabs não são tiles de terreno. O gerador consulta terreno, bioma, dia, distância mínima, footprint, collider e pontos de reserva, então posiciona o GameObject com pivot coerente com o Y sorting do projeto. Objetos multi-tile devem reservar sua área antes da decoração miúda. Quando possível, preferir pooling/streaming e instanciar apenas prefabs próximos. 

## **35. EXEMPLO REAL: GRAMA 70% COM VARIAÇÕES INTERNAS** 

Exemplo didático, não balanceamento definitivo: um perfil ambiental tem 70% de GRAMA como categoria base e 30% de OUTROS TERRENOS. Dentro dos 70% de grama, uma máscara secundária pode escolher 12% daquela área para grama alta (equivale a 8,4% da área total), sem reduzir indevidamente o 70% da família de grama. O restante da grama recebe tiles simples alternativos ou decoração independente. 

Etapas: (1) formar manchas de terreno de grande escala; (2) calibrar distribuição geral aproximada; (3) gerar máscaras secundárias apenas dentro de terrenos compatíveis; (4) limpar ilhas muito pequenas e buracos inadequados; (5) resolver RuleTiles e transições; (6) distribuir decoração; (7) validar caminho e collider. Percentuais de área são objetivos globais aproximados, não probabilidades independentes por célula. 

## **36. COSTURA ENTRE CHUNKS — O AUTOTILE NÃO PODE QUEBRAR** 

Cada chunk deve consultar coordenadas globais e uma borda extra de amostragem (halo) no mínimo suficiente para as regras de vizinhança. Antes de aplicar os tiles, as máscaras lógicas precisam ser 

Evolução Procedural do Mundo — documento de trabalho editável  |  13 

determinísticas e independentes da ordem em que os chunks foram carregados. Quando um chunk adjacente entra ou evolui, atualizar apenas a faixa de borda relevante. Evitar geração por Random sequencial que dependa de qual chunk foi solicitado primeiro. 



<!-- Start of picture text -->
CHUNKS: AUTOTILE SEM COSTURA NAS DIVISAS<br>Cada chunk consulta uma margem (halo) externa antes de resolver as bordas.<br>1 | 1|]| SS<br>EEREERSEEEER 6B<br>an || || I<br>/ — A<br>|_| = I<br>OC<br>| | | || |] | oo | [|<br>EEREERSREEER 6<br>ERRO: bordas locais ao chunk CERTO: amostrar célula vizinha<br><!-- End of picture text -->

_Figura 5 — O limite em vermelho não é uma fronteira de terreno._ 

## **37. MUDANÇA DE DIA: REGERAR REPRESENTAÇÃO, NÃO APAGAR O MUNDO** 

A seed e as coordenadas definem geografia de referência, enquanto ActualDay e um perfil de evolução visual determinam aparência e classes ambientais atuais. No início de um novo dia, o sistema marca chunks residentes como desatualizados, planeja a substituição de terrenos, recalcula bordas e colisores necessários, e preserva deltas de save. Chunks distantes só precisam ser materializados já no estado do dia corrente quando visitados. 



<!-- Start of picture text -->
MESMA GEOGRAFIA, ESTADO AMBIENTAL DIFERENTE<br>DIA1 DIA 10<br>Grama + lago + vegetacao viva Solo escuro + lava + vegetacao morta<br><!-- End of picture text -->

_Figura 6 — Lago e árvores ocupam regiões reconhecíveis, mas suas aparências mudam._ 

Mudanças de água para lava devem atualizar categoria lógica, renderização, colisão, risco/hazard e custo de navegação de forma coordenada. Não basta substituir o sprite. Antes de liberar o controle do jogador, garantir ponto de retorno transitável e conectividade até áreas relevantes. Transformações não devem posicionar lava ou obstáculos sob o jogador sem uma regra explícita de segurança. 

Evolução Procedural do Mundo — documento de trabalho editável  |  14 

## **38. CONFIGURAÇÃO EXATA NO INSPECTOR E NO EDITOR CUSTOMIZADO** 



<!-- Start of picture text -->
EXEMPLO DO FLUXO DE CONFIGURACAO NO INSPECTOR<br>TerrainDefinitionSO: Grama Alta O gerador executa:<br>Tipo: AUTOTILE _<br>RuleTile: GrassTall_ Rule Vora mgepocia<br>Biomas: Natural / Transicao 2. Calcula mascara de grama alta<br>Terrenos permitidos: Grama Base 3. Seleciona Rule Tile configurado<br>Cobertura alvo: 12% (exemplo) 4. Aplica no Tilemap overlay<br>Forma: manchas conectadas 5, Atualiza bordas dos chunks<br>Collider: Nao .<br>. .<br>Evolucao: grama escura / corupcao 6. No novo dia, avalia evolucao<br><!-- End of picture text -->

_Figura 7 — O usuário cadastra o RuleTile pronto, sem redesenhar mapas._ 

|Definition (sugestão)|Campos mínimos|
|---|---|
|TerrainDefinitionSO|id, TileBase, tipo, layer, palette, biome tags, weight,<br>collision/hazard|
|AutotileRegionSO|id, RuleTile ref, allowed base, coverage, noise scale,<br>min patch size, neighbor policy|
|AnimatedSurfaceSO|id, AnimatedTile ref, allowed region, edge<br>companion, collider policy|
|EnvironmentPropSO|prefab, footprint, terrain tags, density, min distance,<br>sorting pivot|
|BiomeProfileSO|terrain weights, overlays, prop rules, structure rules,<br>evolution curve|
|WorldEvolutionSO|day keys, cross-biome mappings, constraints, state<br>version|
|WorldGeneratorSettingsSO|seed, chunk size, finite world mask, preview day,<br>preview position|



A janela de Editor deve oferecer Drag & Drop de TileBase ou prefab, detectar o tipo efetivo do asset, mostrar os campos correspondentes, impedir combinações inválidas e fornecer pré-visualização de uma seed fixa em dias diferentes. Deve haver botão de “Validar catálogo”: assets ausentes, RuleTile sem variações, referências duplicadas, AnimatedTile sem frames, incompatibilidades entre terrain tags e overlays, camadas sem renderer/collider e transições impossíveis. 

## **39. EXEMPLO DE EXECUÇÃO PARA UMA CÉLULA E UMA REGIÃO** 

Célula global (x=124, y=67), Dia 1: o gerador consulta seed e coordenadas e classifica a área como floresta natural. A categoria base é grama; máscara de grama alta inclui a célula. O Tilemap base recebe GramaBase; o OverlayTilemap recebe GrassTall_Rule, que seleciona a borda correta pela vizinhança. Uma árvore pode ser posicionada mais tarde caso não viole distâncias/footprint. Dia 10: o mesmo ponto pode pertencer a solo escuro; o sistema remove a grama alta incompatível, coloca o solo adequado e recalcula as bordas locais. A geografia de referência e as alterações persistentes do jogador continuam registradas. 

Evolução Procedural do Mundo — documento de trabalho editável  |  15 

## **40. PIPELINE TÉCNICO E CONTRATOS ENTRE MÓDULOS** 

1. Obter seed, limites do mundo, coordenadas globais e ActualDay. 2. Resolver geografia estável (elevações, corpos d’água de referência, caminhos e reservas de landmarks). 3. Consultar campo ambiental contínuo para o dia e distância. 4. Resolver máscaras de terreno e coberturas secundárias. 5. Adaptar tipos de terreno e hazards à evolução. 6. Garantir passagens obrigatórias. 7. Preencher Tilemaps de base e overlays com TileBase apropriado. 8. Atualizar RuleTiles e faixas de chunk. 9. Recriar colisores e dados espaciais alterados em batches. 10. Posicionar prefabs por regras e aplicar deltas de save. 11. Enviar regiões navegáveis atualizadas ao A* em lotes, sem tornar a Safe Zone proibida para pathfinding. 

Este pipeline NÃO altera a fórmula de EffectiveSpawnDay já definida no documento estrutural: aquela permanece sob responsabilidade do spawn. O campo ambiental pode consultar progressão visual e distância, mas nenhuma substituição de terreno modifica diretamente a tabela de tiers dos monstros. 

## **41. CHECKLIST DE ACEITE DO PROTÓTIPO** 

- Importar diretamente um Tile simples, um RuleTile e um AnimatedTile prontos no catálogo. 

- Gerar 128 x 128 células com seed estável, múltiplos chunks e duas famílias de terreno. 

- Criar manchas fechadas de grama alta sem bordas soltas nem quadrados isolados. 

- Manter a mesma transição de terreno ao carregar chunks em ordens diferentes. 

- Mostrar água animada com margem correta e collider/hazard independente dos frames. 

- Trocar Dia 1 para Dia 10 e comparar mesma coordenada antes e depois. 

- Atualizar água -> lava como lógica de terreno, não apenas aparência. 

- Garantir caminhos transitáveis e validação de retorno seguro. 

- Manter árvores removidas removidas após unload/reload e evolução do dia. 

- Pré-visualizar no Editor e gerar warnings claros de assets incompletos. 

## **42. ENTREGA ESPERADA ANTES DA IMPLEMENTAÇÃO** 

Antes de escrever o gerador completo, apresentar diagrama de dependências, classes/interfaces, exemplo de configuração de cada TileBase, regras de prioridade de overlays, matriz de compatibilidade entre famílias de terrenos e estratégia de sincronização com A* / URP. Implementar primeiro um vertical slice pequeno com grama base, grama alta RuleTile, água AnimatedTile, colisores e árvores; só então ampliar para famílias T1-T10 e 30 dias. 

Evolução Procedural do Mundo — documento de trabalho editável  |  16 

