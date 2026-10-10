# **Mudança Estrutural do Projeto** 

_Versão atualizada — mapa único, Effective Spawn Day, reciclagem por distância e nova progressão de bosses_ 

Quero realizar uma mudança estrutural grande no projeto antes de iniciar a próxima sprint. 

Você deverá analisar integralmente o GDD atual e o plano de produção existente antes de propor qualquer alteração. O objetivo não é simplesmente adicionar uma nova mecânica: precisamos substituir o conceito atual de torre com 10 andares por um único grande mapa contínuo, preservando tudo que ainda fizer sentido no jogo e reestruturando corretamente todos os sistemas que dependiam dos andares. 

**IMPORTANTE:** não comece a programar ainda. Primeiro faça uma revisão arquitetural completa, identifique todas as consequências da mudança, proponha as novas regras, corrija o GDD e depois revise as Deadlines/Sprints afetadas. 

## **1. MUDANÇA PRINCIPAL — UM ÚNICO MAPA** 

O jogo NÃO terá mais 10 andares separados. 

Existirá um único grande mapa contínuo, explorável durante cada dia. 

Portanto, devem deixar de existir como conceito de gameplay: 

- Floor 1, Floor 2... Floor 10; 

- subir/descer escadas entre Floors; 

- Active Floor Position; 

- Current Floor; 

- Floor Sleep baseado na troca de andar; 

- Stair Routing; 

- Remove Tower Layer no formato atual. 

Porém, os grupos de monstros atualmente associados aos antigos Floors 1–10 continuarão existindo como 10 níveis de ameaça/progressão. 

Precisamos escolher uma nova nomenclatura consistente. 

Sugestão inicial: 

Enemy Tier 1–10 

ou: 

Threat Tier 1–10. 

Se existir um nome mais apropriado ao universo do jogo, proponha, mas internamente o sistema deverá continuar possuindo níveis numerados de 1 a 10. 

Exemplo: 

- monstros que eram do antigo Floor 1 → Tier 1; 

- antigo Floor 2 → Tier 2; 

- ... 

- antigo Floor 10 → Tier 10. 

O Bestiary, Loot, HP, Damage e demais dados desses monstros continuam existindo. 

## **2. CENTRO DO MAPA** 

O mapa deverá possuir uma região central que funcione como referência espacial para Safe Zone, progressão por distância, spawn e retorno do jogador. 

A dificuldade dos próximos spawns será determinada principalmente por: 

Mudança Estrutural do Projeto — documento de trabalho editável 

<mark>ActualDay + CurrentPhase + DistanceDayOfset -> Ef</mark> f f <mark>ectiveSpawnDay</mark> 

Próximo ao centro, o EffectiveSpawnDay será igual ou muito próximo do ActualDay. Conforme o jogador se afasta, faixas radiais adicionais fazem os próximos spawns e respawns utilizarem tabelas de dias futuros. 

Isso substitui a antiga lógica probabilística de somar +1 ou +2 Tiers diretamente. A distância passa a escolher qual dia da tabela representa a ameaça local. 

A decisão de exploração continua sendo: 

<mark>"Será que consigo me afastar mais do centro?"</mark> 

A exploração deve representar risco x recompensa. Quanto mais longe: 

- os próximos spawns podem usar tabelas de dias mais avançados; 

- a predominância de Tiers mais altos aumenta de forma natural pela própria tabela do dia efetivo; 

- melhores drops podem ser encontrados mais cedo; 

- o risco aumenta; 

- a composição de monstros passa a acompanhar a região atual do jogador. 

### **2.1. SAFE ZONE CENTRAL — ÁREA ONDE MONSTROS NÃO PODEM SPAWNAR** 

Ao redor do centro do mapa deverá existir uma Safe Zone circular, definida por um raio configurável. 

Essa região NÃO é uma área inacessível para monstros. 

Ela é apenas uma área proibida para nascimento/spawn. 

Regra principal: 

Enemy Spawn Position  Safe Zone∉ 

Ou seja: 

nenhum monstro comum poderá nascer dentro desse raio. 

Nenhum boss poderá nascer dentro desse raio. 

Qualquer outro sistema futuro que gere inimigos diretamente no mapa também deverá respeitar essa restrição, salvo alguma mecânica excepcional explicitamente criada para ignorá-la. 

Entretanto, a Safe Zone não deve bloquear movimentação nem pathfinding. 

Se um monstro que nasceu fora da Safe Zone detectar o jogador e adquirir o jogador como Target: 

ele poderá normalmente: 

- entrar na Safe Zone; 

- perseguir o jogador dentro dela; 

- atravessar a região; 

- atacar o jogador dentro dela; 

- continuar utilizando A* normalmente. 

Portanto, a Safe Zone NÃO deve ser implementada tornando os nós do A* não caminháveis. 

Isso seria incorreto porque impediria monstros de perseguirem o jogador para dentro da região. 

O A* deverá continuar considerando a Safe Zone como terreno caminhável normal. 

A restrição pertence exclusivamente ao Spawn Position Validation. 

Conceitualmente: 

Walkable ≠ Valid Spawn Position 

Uma posição pode ser perfeitamente caminhável pelo A* e ainda assim ser proibida para spawn. 

#### **2.1.1. RELAÇÃO COM O SISTEMA ATUAL DE A*** 

Atualmente o sistema procura nós caminháveis do GridGraph para encontrar posições válidas de spawn. 

Essa arquitetura poderá continuar sendo utilizada. 

Mudança Estrutural do Projeto — documento de trabalho editável 

Porém, após identificar nós caminháveis, o sistema deverá filtrar posições que estejam dentro da Safe Zone. Conceitualmente: 

Candidate Nodes = Walkable A* Nodes 

depois: 

Valid Spawn Nodes = Candidate Nodes - Safe Zone Nodes 

Isso NÃO significa remover esses Nodes do Graph. 

Significa apenas não escolhê-los durante o spawn. 

Uma abordagem possível: 

distance(candidatePosition, SafeZoneCenter) >= 

SafeZoneRadius 

somente então a posição poderá ser considerada candidata ao spawn. 

Avalie a implementação mais eficiente. 

Não quero que cada tentativa de spawn fique realizando buscas extremamente caras pelo mapa inteiro. 

Considere: 

- cache de nós válidos; 

- cache separado de Spawnable Nodes; 

- invalidação somente quando necessário; 

- Spatial Data; 

- Spawn Regions; 

- filtros pré-calculados. 

Como o centro e o raio provavelmente serão estáticos durante uma run, o sistema deve aproveitar essa característica. 

#### **2.1.2. SAFE ZONE NÃO SIGNIFICA INVULNERABILIDADE** 

Essa distinção é obrigatória. 

A Safe Zone significa: 

“nenhum inimigo nasce aqui.” 

Ela NÃO significa: 

“nenhum inimigo pode entrar aqui.” 

Se o jogador correr para o centro enquanto está sendo perseguido: 

os monstros continuam perseguindo. 

A Safe Zone não limpa: 

- Target; 

- Aggro; 

- ataques; 

- projéteis; 

- bosses; 

- inimigos vivos. 

Ela também não mata, teleporta ou remove monstros. 

Portanto, o centro é uma região de menor pressão de spawn, mas não necessariamente uma região completamente segura durante uma perseguição. 

#### **2.1.3. SPAWN NA BORDA DA SAFE ZONE** 

Evite também que monstros sejam gerados exatamente na borda do raio e apareçam visualmente quase dentro da área segura. 

Mudança Estrutural do Projeto — documento de trabalho editável 

Considere possuir: 

SafeZoneRadius 

e opcionalmente: 

SpawnSafetyPadding 

Assim: 

MinimumSpawnDistanceFromCenter = SafeZoneRadius + SpawnSafetyPadding 

O Padding deverá ser configurável. 

Avalie se ele é necessário após os primeiros testes. 

#### **2.1.4. SAFE ZONE, DISTÂNCIA E EFFECTIVE SPAWN DAY** 

A distância usada para progressão de spawn deverá começar a ser contada a partir da borda efetiva da Safe Zone, considerando o SpawnSafetyPadding quando existir. 

DistanceBeyondSafeZone = max(0, DistanceFromCenter - SafeZoneRadius - SpawnSafetyPadding) DistanceDayOffset = floor(DistanceBeyondSafeZone / DistancePerSpawnDay) EffectiveSpawnDay = clamp(ActualDay + DistanceDayOffset, 1, 30) 

DistancePerSpawnDay representa quantos blocos, tiles ou unidades de mundo o jogador precisa avançar para que os próximos spawns passem a usar a tabela do dia seguinte. O valor deve ser configurável e Data-Driven. 

A Safe Zone continua sem spawns. A primeira faixa válida após ela utiliza o ActualDay; cada faixa adicional acrescenta +1 ao EffectiveSpawnDay. 

O cálculo utiliza a posição atual do jogador, não a posição aleatória escolhida para o inimigo. 

#### **2.1.5. SAFE ZONE E SPAWN DE BOSS** 

Bosses também não deverão spawnar dentro da Safe Zone. 

Caso o Boss seja criado fora da Safe Zone e imediatamente adquira Aggro no jogador: 

ele poderá entrar normalmente na região. 

Portanto: 

Boss Spawn = respeita Safe Zone 

mas: 

Boss Navigation = ignora Safe Zone como restrição. 

Não criar uma segunda regra de pathfinding para bosses. 

## **3. SISTEMA DE DIA E NOITE** 

Cada gameplay diária será dividida exatamente em: 

50% Dia / 50% Noite. 

Com tempo inicial: 

100 segundos 

teremos: 

- 50 segundos Dia 

- 50 segundos Noite 

Se comprar um bônus de +100 segundos: 

200 segundos 

então: 

Mudança Estrutural do Projeto — documento de trabalho editável 

100 segundos Dia 

100 segundos Noite Se chegar a: 300 segundos será: 

150 segundos Dia 150 segundos Noite Portanto: 

DayPhaseDuration = TotalDayDuration × 0.5 

e: 

NightPhaseDuration = TotalDayDuration × 0.5. 

Nenhum bônus de tempo poderá quebrar essa proporção. 

A transição precisa ser visualmente perceptível. 

Não estou definindo neste momento exatamente como será iluminação, skybox, shaders ou pós-processamento. Proponha uma arquitetura compatível com Unity 2D/URP sem transformar isso em um sistema excessivamente complexo. 

## **4. NOITE = PROGRESSÃO DE DIFICULDADE** 

A noite não deve simplesmente aumentar HP ou Damage artificialmente. 

Em vez disso, ela altera a composição dos monstros spawnados. 

Durante a noite: 

- Tiers superiores passam a possuir mais peso; 

- alguns Tiers podem aparecer pela primeira vez; 

- monstros fracos continuam podendo existir; 

- dificuldade aumenta através da composição das hordas. 

Não criar buffs secretos como: 

"à noite todos os monstros recebem +50% HP". 

A diferença principal deverá vir da tabela de spawn. 

## **5. TABELA BASE COMPLETA — DIAS 1 A 30** 

A tabela abaixo é a fonte de verdade da composição de Tiers. Ela representa a distribuição base de cada Day/Noite e também é reutilizada pelo sistema de distância através do EffectiveSpawnDay. 

Próximo ao centro, fora da Safe Zone, normalmente será utilizada a linha correspondente ao ActualDay. Em regiões distantes, o jogo poderá utilizar uma linha de dia futuro calculada pelo DistanceDayOffset. 

Cada linha deve totalizar exatamente 100%. Os valores são uma base inicial de balanceamento e devem permanecer Data-Driven e facilmente editáveis em playtests. 

Os Tiers antigos não desaparecem naturalmente. Conforme os dias avançam, a concentração muda progressivamente para Tiers mais altos. 

O Tier 10 não possui bloqueio especial baseado no ActualDay. Ele aparece com 0% nas linhas base dos Dias 1–19 e passa a ter peso próprio a partir da linha do Dia 20. Se a distância fizer o EffectiveSpawnDay alcançar 20 ou mais antes do ActualDay 20, T10 passa a ser elegível normalmente pela própria tabela. 

|**Dia**|**Período**|**T1**|**T2**|**T3**|**T4**|**T5**|**T6**|**T7**|**T8**|**T9**|**T10**|
|---|---|---|---|---|---|---|---|---|---|---|---|
|1|Dia|100%|0%|0%|0%|0%|0%|0%|0%|0%|0%|
|1|Noite|70%|30%|0%|0%|0%|0%|0%|0%|0%|0%|



Mudança Estrutural do Projeto — documento de trabalho editável 

|**Dia**<br>2|**Período**<br>Dia|**T1**<br>70%|**T2**<br>30%|**T3**<br>0%|**T4**<br>0%|**T5**<br>0%|**T6**<br>0%|**T7**<br>0%|**T8**<br>0%|**T9**<br>0%|**T10**<br>0%|
|---|---|---|---|---|---|---|---|---|---|---|---|
|2|Noite|50%|50%|0%|0%|0%|0%|0%|0%|0%|0%|
|3|Dia|35%|50%|15%|0%|0%|0%|0%|0%|0%|0%|
|3|Noite|10%|60%|30%|0%|0%|0%|0%|0%|0%|0%|
|4|Dia|10%|25%|50%|15%|0%|0%|0%|0%|0%|0%|
|4|Noite|10%|15%|50%|25%|0%|0%|0%|0%|0%|0%|
|5|Dia|10%|25%|30%|35%|0%|0%|0%|0%|0%|0%|
|5|Noite|5%|15%|20%|45%|15%|0%|0%|0%|0%|0%|
|6|Dia|5%|20%|25%|35%|15%|0%|0%|0%|0%|0%|
|6|Noite|5%|15%|20%|35%|25%|0%|0%|0%|0%|0%|
|7|Dia|5%|15%|20%|30%|30%|0%|0%|0%|0%|0%|
|7|Noite|5%|10%|15%|30%|40%|0%|0%|0%|0%|0%|
|8|Dia|5%|10%|15%|30%|40%|0%|0%|0%|0%|0%|
|8|Noite|5%|10%|15%|25%|35%|10%|0%|0%|0%|0%|
|9|Dia|5%|10%|15%|25%|35%|10%|0%|0%|0%|0%|
|9|Noite|5%|5%|10%|20%|35%|25%|0%|0%|0%|0%|
|10|Dia|5%|5%|10%|20%|35%|25%|0%|0%|0%|0%|
|10|Noite|5%|5%|10%|15%|30%|35%|0%|0%|0%|0%|
|11|Dia|5%|5%|10%|15%|30%|35%|0%|0%|0%|0%|
|11|Noite|5%|5%|5%|10%|25%|40%|10%|0%|0%|0%|
|12|Dia|5%|5%|5%|10%|25%|40%|10%|0%|0%|0%|
|12|Noite|5%|5%|5%|10%|20%|35%|20%|0%|0%|0%|
|13|Dia|5%|5%|5%|10%|20%|35%|20%|0%|0%|0%|
|13|Noite|5%|5%|5%|10%|15%|30%|30%|0%|0%|0%|
|14|Dia|5%|5%|5%|10%|15%|30%|30%|0%|0%|0%|
|14|Noite|5%|5%|5%|5%|15%|25%|30%|10%|0%|0%|
|15|Dia|5%|5%|5%|5%|15%|25%|30%|10%|0%|0%|
|15|Noite|5%|5%|5%|5%|10%|20%|30%|20%|0%|0%|
|16|Dia|5%|5%|5%|5%|10%|20%|30%|20%|0%|0%|
|16|Noite|5%|5%|5%|5%|10%|15%|25%|30%|0%|0%|
|17|Dia|5%|5%|5%|5%|10%|15%|25%|30%|0%|0%|
|17|Noite|5%|5%|5%|5%|5%|15%|20%|30%|10%|0%|
|18|Dia|5%|5%|5%|5%|5%|15%|20%|30%|10%|0%|
|18|Noite|5%|5%|5%|5%|5%|10%|20%|25%|20%|0%|
|19|Dia|5%|5%|5%|5%|5%|10%|20%|25%|20%|0%|
|19|Noite|5%|5%|5%|5%|5%|10%|15%|25%|25%|0%|
|20|Dia|5%|5%|5%|5%|5%|10%|15%|20%|25%|5%|
|20|Noite|5%|5%|5%|5%|5%|5%|10%|20%|25%|15%|
|21|Dia|5%|5%|5%|5%|5%|5%|10%|20%|25%|15%|
|21|Noite|5%|5%|5%|5%|5%|5%|10%|15%|25%|20%|
|22|Dia|5%|5%|5%|5%|5%|5%|10%|15%|25%|20%|
|22|Noite|5%|5%|5%|5%|5%|5%|10%|15%|20%|25%|
|23|Dia|5%|5%|5%|5%|5%|5%|10%|15%|20%|25%|
|23|Noite|5%|5%|5%|5%|5%|5%|5%|15%|20%|30%|
|24|Dia|5%|5%|5%|5%|5%|5%|5%|15%|20%|30%|
|24|Noite|5%|5%|5%|5%|5%|5%|5%|10%|20%|35%|
|25|Dia|5%|5%|5%|5%|5%|5%|5%|10%|20%|35%|
|25|Noite|5%|5%|5%|5%|5%|5%|5%|10%|15%|40%|
|26|Dia|5%|5%|5%|5%|5%|5%|5%|10%|15%|40%|
|26|Noite|5%|5%|5%|5%|5%|5%|5%|10%|10%|45%|
|27|Dia|5%|5%|5%|5%|5%|5%|5%|10%|10%|45%|
|27|Noite|4%|4%|4%|4%|4%|5%|5%|10%|10%|50%|
|28|Dia|4%|4%|4%|4%|4%|5%|5%|10%|10%|50%|
|28|Noite|4%|4%|4%|4%|4%|5%|5%|5%|10%|55%|
|29|Dia|4%|4%|4%|4%|4%|5%|5%|5%|10%|55%|
|29<br>|Noite<br>|3%<br>|3%<br>|3%<br>|3%<br>|3%<br>|5%<br>|5%<br>|5%<br>|10%<br>|60%<br>|
|30<br>30|Dia<br>Noite|3%<br>2%|3%<br>2%|3%<br>2%|3%<br>2%|3%<br>2%|5%<br>5%|5%<br>5%|5%<br>5%|10%<br>10%|60%<br>65%|



_Todos os 60 estados fecham em 100%._ 

### **5.1. LÓGICA DE PROGRESSÃO DA TABELA** 

Essa tabela segue uma progressão deliberada. 

Não queremos simplesmente: 

##### Dia X = Tier X. 

A ideia é que cada Tier tenha uma janela de introdução, crescimento, domínio e permanência residual. 

Exemplo: 

T2 

Surge: 

Dia 1 à noite. 

Mudança Estrutural do Projeto — documento de trabalho editável 

Torna-se importante: 

Dias 2–3. 

Depois perde relevância progressivamente, mas continua aparecendo. T5 Surge: Dia 5 à noite. Cresce: Dias 6–8. Passa a dominar parte importante da composição antes da entrada de T6. T6 Surge: Dia 8 à noite. Torna-se dominante: aproximadamente entre os Dias 10–13. T7 Surge: Dia 11 à noite. Cresce fortemente entre: Dias 12–16. T8 Surge: Dia 14 à noite. Ganha importância principalmente entre: Dias 15–19. T9 Surge: Dia 17 à noite. Torna-se uma ameaça central antes da entrada do último Tier. T10 Não existe na pool até: Dia 19. Sua primeira aparição normal acontece: Dia 20 de dia = 5% e: Dia 20 à noite = 15%. Depois sua presença cresce gradualmente: Dia 25 à noite = 40% Dia 27 à noite = 50% Dia 29 à noite = 60% Dia 30 à noite = 65%. 

Mudança Estrutural do Projeto — documento de trabalho editável 

Mesmo no Dia 30, os Tiers anteriores continuam tendo alguma chance de aparecer. 

Isso é intencional. 

## **6. REGRAS IMPORTANTES DESTA TABELA** 

Essa tabela é a Base Spawn Distribution e não representa, sozinha, o spawn final. A distância não altera diretamente os pesos com um bônus de Tier; ela escolhe qual linha da própria tabela será usada. 

ActualDay | CurrentPhase | DistanceDayOffset | EffectiveSpawnDay | Base Day/Night Table do EffectiveSpawnDay | Suppressed Tiers | Normalization | Final Tier Roll 

Portanto, encontrar Tiers avançados cedo continua possível, mas acontece porque o jogador alcançou uma região cujo EffectiveSpawnDay é maior que o ActualDay. 

Não existe exceção absoluta para o Tier 10. Sua disponibilidade depende somente da linha do EffectiveSpawnDay consultada: enquanto essa linha tiver T10 = 0%, ele não pode sair; quando a linha tiver peso de T10, ele pode ser sorteado. 

### **6.1. A TABELA NÃO É HARDCODED** 

Os valores não deverão existir espalhados pelo código. Cada dia deverá possuir dados editáveis, por exemplo: 

DaySpawnDefinition DayNumber DayWeights[10] NightWeights[10] 

O sistema espacial deverá apenas decidir qual DaySpawnDefinition consultar. Alterar uma porcentagem de balanceamento não pode exigir alteração de código. 

### **6.2. VALIDAÇÃO AUTOMÁTICA** 

Para cada combinação Day + Period, a soma deve ser 100%. Caso a configuração esteja inválida, o Editor deve emitir um Warning claro; idealmente, uma ferramenta de validação deve revisar automaticamente os 30 dias. 

### **6.3. RELAÇÃO COM A SAFE ZONE** 

A Safe Zone não altera os pesos da tabela. Ela apenas torna uma posição inválida para nascimento. Tier Distribution e Spawn Position Validation continuam sendo responsabilidades separadas. 

## **7. TIER 10 — INTRODUÇÃO PELA TABELA, SEM HARD LOCK** 

O Tier 10 NÃO possui uma regra explícita do tipo “proibido antes do ActualDay 20”. A única razão para ele não aparecer nas linhas base dos Dias 1–19 é o próprio balanceamento da tabela. 

<mark>T10Weight = DaySpawnDef</mark> i <mark>nition[Ef</mark> f <mark>ectiveSpawnDay].CurrentPhaseWeights[T10]</mark> 

Mudança Estrutural do Projeto — documento de trabalho editável 

Se o jogador estiver longe o suficiente para que o EffectiveSpawnDay alcance uma linha em que T10 tenha peso maior que 0%, monstros Tier 10 poderão aparecer mesmo que o ActualDay ainda seja menor que 20. Exemplo: ActualDay = 15 e DistanceDayOffset = 5 resultam em EffectiveSpawnDay = 20; nesse caso, a linha Day 20 / período atual é usada integralmente, inclusive o peso de T10. 

## **8. DISTÂNCIA DO CENTRO — EFFECTIVE SPAWN DAY** 

A distância do centro não modificará mais diretamente o Tier através de chances de +1 ou +2. Em vez disso, ela determinará qual dia da tabela de spawn será utilizado pelos próximos spawns e respawns. 

### **8.1. ACTUAL DAY x EFFECTIVE SPAWN DAY** 

ActualDay = dia real da run EffectiveSpawnDay = dia da tabela utilizado na posição atual do jogador 

Exemplo: o jogador pode estar em ActualDay = 1 e EffectiveSpawnDay = 4 por ter se afastado suficientemente do centro. Isso não avança Demanda, Loja, vitória, Save, Dia 15, Dia 30 ou qualquer outra progressão diária. 

### **8.2. DISTANCE DAY OFFSET** 

A partir da borda efetiva da Safe Zone, o mapa é dividido em faixas radiais. Cada faixa adicional equivale a +1 dia na tabela de spawn. 

DistancePerSpawnDay = X blocos DistanceDayOffset = floor(DistanceBeyondSafeZone / DistancePerSpawnDay) EffectiveSpawnDay = clamp(ActualDay + DistanceDayOffset, 1, 30) 

DistancePerSpawnDay deve ser configurável. O jogo pode medir essa distância em tiles, células do Grid ou unidades de mundo, desde que a conversão seja consistente. 

### **8.2.1. MAPA FINITO E LIMITE NATURAL DO DISTANCE DAY OFFSET** 

O mapa será grande, porém finito. Portanto, o DistanceDayOffset máximo não será infinito: ele será limitado naturalmente pela maior distância caminhável entre a Safe Zone e os extremos do mapa. A expectativa inicial é que o mapa permita algo em torno de cinco a seis estados de dia efetivo ao longo da exploração, mas esse número ainda é uma hipótese de level design e só deverá ser fechado depois que o mapa real existir. 

A intenção de balanceamento é que essa limitação física torne T10 naturalmente inalcançável muito cedo — aproximadamente antes do ActualDay 15 — sem qualquer if especial proibindo o Tier. Se o mapa final permitir alcançar uma linha com T10 cedo demais, o ajuste correto será mudar DistancePerSpawnDay, o tamanho/forma navegável do mapa ou o número de faixas úteis, e não criar um Hard Lock artificial para T10. 

Em outras palavras: a geometria do mapa limita quantos dias futuros podem ser antecipados. O código apenas calcula o EffectiveSpawnDay alcançável naquela posição. 

### **8.3. A POSIÇÃO DO JOGADOR DEFINE O DIA EFETIVO** 

O EffectiveSpawnDay é calculado pela posição atual do jogador. A posição aleatória escolhida para o spawn não pode alterar a dificuldade do inimigo. 

### **8.4. DIA/NOITE CONTINUA SENDO O PERÍODO REAL** 

Distância muda o dia consultado, mas não muda o horário. Se ActualDay = 3, CurrentPhase = Night e DistanceDayOffset = 4, então o jogo utiliza Day 7 / Night. 

### **8.5. MONSTROS JÁ EXISTENTES NÃO SÃO TRANSFORMADOS** 

Mudar de faixa não substitui instantaneamente todos os inimigos vivos. A nova composição vale para novos spawns, respawns e replacements de entidades removidas por distância. A transição deve acontecer de forma gradual e orgânica. 

Mudança Estrutural do Projeto — documento de trabalho editável 

## **9. DISTÂNCIA NÃO SOMA MAIS TIERS DIRETAMENTE** 

Remover completamente a regra antiga de 75% normal / 20% +1 Tier / 5% +2 Tiers e equivalentes. O único papel da distância na composição é selecionar o EffectiveSpawnDay. 

Distance From Center | DistanceDayOffset | EffectiveSpawnDay | Day/Noite Table | Tier Distribution 

Isso preserva a possibilidade de antecipar ameaças sem criar uma segunda curva de balanceamento paralela à tabela dos 30 dias. 

## **10. NÃO AUMENTAR POPULAÇÃO AUTOMATICAMENTE COM DISTÂNCIA** 

Na primeira implementação, distância altera a composição dos inimigos através do EffectiveSpawnDay, mas não aumenta automaticamente Minimum, Target, Maximum ou o Spawn Rate. 

Population = mesma lógica Spawn Rate = mesma lógica Tier Distribution = tabela do EffectiveSpawnDay 

A quantidade poderá ser revisada depois dos playtests, mas deve permanecer uma variável separada da progressão por distância. 

## **11. POPULATION, DESPAWN E REPOSIÇÃO POR DISTÂNCIA** 

A regra principal continua: o mapa nunca deve parecer permanentemente vazio. Minimum Population, Target Population e Maximum Population continuam existindo. 

### **11.1. ENEMY DESPAWN DISTANCE** 

Monstros que ficarem muito distantes do jogador poderão ser removidos da simulação. 

<mark>if Distance(Enemy, Player) > EnemyDespawnDistance -> Distance Despawn</mark> 

EnemyDespawnDistance deve ser configurável e suficientemente maior que a área visível para evitar desaparecimentos perceptíveis durante combate normal. 

### **11.2. DESPAWN NÃO É MORTE** 

Um inimigo removido por distância não morreu. Ele não gera loot, gold, kill, energia de Ultimate, efeitos de morte, progresso de quest ou achievement. 

<mark>Enemy Death != Distance Despawn</mark> 

### **11.3. CADA DESPAWN GERA UMA REPOSIÇÃO** 

Quando um monstro é removido por distância, a população deve manter sua vaga. Um novo inimigo será criado em uma região relevante ao redor do jogador. 

<mark>1 Enemy Distance Despawn -> 1 Replacement Spawn</mark> 

O replacement não precisa ser do mesmo Tier ou da mesma EnemyDefinition. Ele é sorteado novamente usando CurrentPhase, EffectiveSpawnDay e Tier Suppression atuais. O ActualDay participa apenas do cálculo que origina o EffectiveSpawnDay; não existe bloqueio adicional de T10 baseado no dia real. 

Mudança Estrutural do Projeto — documento de trabalho editável 

### **11.4. REPLACEMENT USA A ÁREA ATUAL** 

Se monstros T1 ficaram para trás, mas o jogador avançou até uma região onde EffectiveSpawnDay = 5, os replacements serão sorteados pela composição do Day 5 / período atual. É assim que a população acompanha a exploração. 

### **11.5. SPAWN PRÓXIMO AO JOGADOR, MAS NÃO COLADO** 

Replacement Spawn deve ocorrer dentro de um anel configurável ao redor do jogador: 

<mark>ReplacementSpawnMinDistance <= Distance(Player, SpawnPosition) <= ReplacementSpawnMaxDistance</mark> 

A posição deve estar fora da Safe Zone, preferencialmente fora da câmera, em nó caminhável, sem paredes, obstáculos ou interações importantes. 

### **11.6. JOGADOR DENTRO DA SAFE ZONE** 

Se o jogador estiver dentro da Safe Zone, replacements continuam proibidos dentro dela. O sistema procura uma posição válida no anel ao redor do jogador que também esteja fora da Safe Zone. Se não existir posição válida, o replacement fica pendente; não usar loops infinitos. 

### **11.7. NOVO PIPELINE DE POPULAÇÃO** 

Player Position | ActualDay | CurrentPhase | Distance From Center | DistanceDayOffset | EffectiveSpawnDay | Get Spawn Table | Apply Tier Suppression | Normalize | Roll Tier | Roll Enemy Definition | Find Position Around Player | Safe Zone + A* + Obstacle Validation | Spawn 

## **12. REMOÇÃO DOS ANTIGOS “ANDARES”** 

O antigo upgrade: 

Remove Tower Layer 

não existe mais conceitualmente. 

Mudança Estrutural do Projeto — documento de trabalho editável 

Ele deverá se transformar em um sistema de remoção/supressão de Tiers fracos da pool de spawn. 

Precisamos criar um novo nome temático para esse Bonus. 

Exemplos conceituais: 

- Threat Suppression; 

- Enemy Suppression; 

- Extermination Order; 

- Purge Contract; 

- Suppress Threat Tier. 

Proponha alternativas adequadas ao jogo. 

A regra deverá continuar sequencial: 

Compra 1: remove Tier 1 Compra 2: remove Tier 2 Compra 3: remove Tier 3 Compra 4: remove Tier 4 Compra 5: remove Tier 5. 

Não poderá escolher livremente um Tier. 

## **13. REDISTRIBUIÇÃO DAS PORCENTAGENS** 

Quando um Tier é removido, sua porcentagem NÃO desaparece. Ela deverá ser redistribuída proporcionalmente entre os Tiers restantes. Não criar tabelas especiais manualmente. 

Utilizar normalização matemática. 

Exemplo: tabela: T1 5 T2 15 T3 20 T4 35 T5 25 jogador remove T2. Soma restante: 5 + 20 + 35 + 25 = 85. Nova tabela: T1 = 5 / 85 T3 = 20 / 85 T4 = 35 / 85 

Mudança Estrutural do Projeto — documento de trabalho editável 

T5 = 25 / 85 

multiplicados por 100. 

Resultado aproximado: 

- T1: 5,88% 

- T3: 23,53% 

- T4: 41,18% 

- T5: 29,41%. 

Total: 

100%. 

Isso deverá funcionar para qualquer combinação de: 

- Dia; 

- Noite; 

- Distância; 

- Tiers removidos. 

## **14. ORDEM CORRETA DO PIPELINE DE SPAWN** 

A arquitetura deverá ser previsível, Data-Driven e separar claramente seleção de ameaça, seleção de entidade e validação espacial. 

1. ActualDay 

- | 2. CurrentPhase (Day/Noite) | 3. Player Distance From Center | 4. DistanceDayOffset | 5. EffectiveSpawnDay | 6. Base Spawn Weights do EffectiveSpawnDay | 7. Suppressed Tiers | 8. Normalize Weights | 9. Roll Enemy Tier | 10. Roll Enemy Definition dentro do Tier | 11. Encontrar posição no Spawn Annulus ao redor do Player | 12. Safe Zone Check | 

13. Camera / Player Distance / A* / Obstacle Validation 

- | 14. Spawn 

A Safe Zone continua sendo validação espacial. O EffectiveSpawnDay continua sendo regra de seleção da tabela. Não misturar as duas responsabilidades. 

Mudança Estrutural do Projeto — documento de trabalho editável 

## **15. O SISTEMA PRECISA SER DATA-DRIVEN** 

Não quero if(day == 1), else if(day == 2) e equivalentes espalhados pelo código. A progressão, as faixas de distância e os raios de spawn/despawn devem ser configuráveis. 

### **15.1. DAY SPAWN DEFINITION** 

DaySpawnDefinition DayNumber DayWeights[10] NightWeights[10] 

### **15.2. ENEMY TIER DEFINITION** 

EnemyTierDefinition TierId EnemyDefinitions[] OptionalIndividualWeights 

### **15.3. DISTANCE PROGRESSION CONFIG** 

SafeZoneCenter SafeZoneRadius SpawnSafetyPadding DistancePerSpawnDay MaximumSpawnDistance EnemyDespawnDistance ReplacementSpawnMinDistance ReplacementSpawnMaxDistance 

### **15.4. BOSS PROGRESSION CONFIG** 

BossSpawnInterval BossDespawnDistance BossReplacementSpawnMinDistance BossReplacementSpawnMaxDistance BossReplacementCooldown / Hysteresis parameters 

Analise se ScriptableObjects, componentes de configuração ou uma combinação são a melhor abordagem. O objetivo é alterar balanceamento sem modificar código. 

## **16. UM TIER POSSUI VÁRIOS MONSTROS** 

O sorteio NÃO escolhe diretamente um monstro entre todos os monstros do jogo. 

Primeiro: 

Roll Tier 

depois: 

Roll Monster Inside Tier. 

Exemplo: 

Tier 1 pode conter: 

- Rat; 

- Wolf; 

- Bat; 

- Slime Green; 

Mudança Estrutural do Projeto — documento de trabalho editável 

- Slime Blue. 

Se Tier 1 for sorteado: 

depois escolhemos uma EnemyDefinition válida daquele Tier. 

Considere pesos individuais futuros dentro do próprio Tier. 

Exemplo: 

dentro de T1: 

- Rat: peso 30; 

- Wolf: 20; 

- Bat: 20; 

- Slime Green: 15; 

- Slime Blue: 15. 

Não é obrigatório fechar esses pesos agora, mas a arquitetura deve permitir isso. 

## **17. BOSSES — NOVA REGRA DEFINITIVA** 

O sistema antigo de bosses dependia do Floor atual e de uma rotação que podia repetir indefinidamente. No mapa único, bosses continuam surgindo por tempo, mas o Tier do boss deve refletir a ameaça predominante na posição atual do jogador. 

### **17.1. BOSS TIMER CONTINUA EXISTINDO** 

Um novo Boss Event ocorrerá aproximadamente a cada 20–30 segundos. O valor final é configurável e sujeito a playtest. 

Boss Timer -> decide QUANDO adicionar um boss Tier Predominance -> decide QUAL Boss Tier priorizar 

### **17.2. BOSS TIER SEGUE A PREDOMINÂNCIA ATUAL** 

Quando o timer completar, o jogo calcula a mesma distribuição final de Tiers usada pelos monstros comuns na posição atual. O Boss Tier prioritário é o Tier com maior peso, não um roll separado de porcentagens de bosses. 

CurrentPhase + EffectiveSpawnDay + Tier Suppression -> Final Enemy Weights DominantTier = Tier com maior peso Boss Priority = Boss Tier correspondente 

A antiga Tabela Base de Bosses BT1–BT10 deve ser removida. Não manter duas curvas de balanceamento paralelas. 

### **17.3. DISTÂNCIA ALTERA O BOSS TIER PELO EFFECTIVE SPAWN DAY** 

Se ActualDay = 1, mas o jogador alcança uma faixa onde EffectiveSpawnDay = 3, o próximo Boss Event usa a predominância da tabela Day 3 / período atual. Assim, bosses de Tiers superiores podem surgir cedo por exploração distante. 

Não existe bloqueio de BT10 baseado no ActualDay. Se a distância elevar o EffectiveSpawnDay até uma linha onde T10 já possui peso, esse Tier passa a participar normalmente da predominância e da ordem de fallback dos bosses. Assim, um BT10 pode surgir antes do ActualDay 20 se o jogador tiver alcançado uma região suficientemente distante. Na prática, a expectativa é que o tamanho finito do mapa impeça isso muito cedo. 

### **17.4. A NOITE PODE TROCAR A PREDOMINÂNCIA** 

A mudança Day -> Night recalcula a distribuição. Se durante o dia T1 é predominante e à noite T2 passa a ser predominante, os próximos Boss Events priorizam BT2. Bosses já vivos não são trocados apenas porque anoiteceu. 

### **17.5. UM BOSS ESPECÍFICO SÓ PODE APARECER UMA VEZ POR ACTUALDAY** 

Nunca poderá existir o mesmo BossDefinition duas vezes no mesmo dia real da run. 

Mudança Estrutural do Projeto — documento de trabalho editável 

UsedBossIdsToday = conjunto de BossDefinitions já spawnadas no ActualDay Novo ActualDay -> UsedBossIdsToday.Clear() 

Morte, despawn por distância ou mudança de região não tornam aquele mesmo boss elegível novamente no mesmo dia. 

### **17.6. POOL SEM REPETIÇÃO, NÃO ROTAÇÃO INFINITA** 

Dentro de cada Boss Tier, os bosses funcionam como uma bag sem repetição durante o dia. Eles podem ser embaralhados para variar a ordem, mas um BossDefinition usado fica indisponível até o próximo ActualDay. 

### **17.7. QUANDO O TIER PREDOMINANTE ESGOTAR** 

Se todos os bosses disponíveis do Tier predominante já apareceram naquele dia, o sistema consulta o próximo Tier por ordem de predominância atual. 

Exemplo: T1 70%, T2 30% BT1 esgotado -> próximo Boss Event usa BT2 BT1 e BT2 esgotados -> consulta o próximo Tier elegível 

Empates entre Tiers devem ser resolvidos de forma determinística; como regra inicial, priorizar o Tier mais alto. 

Exemplo de regra desejada: se no dia atual já apareceram Mother Slime Green, Mother Slime Blue e Goblin King, nenhum deles pode reaparecer naquele mesmo ActualDay. O próximo Boss Event deve buscar o próximo BossDefinition elegível conforme a ordem de predominância atual. 

### **17.8. BOSS DESPAWN POR DISTÂNCIA** 

Bosses também podem ficar para trás, mas devem possuir BossDespawnDistance próprio, preferencialmente maior que EnemyDespawnDistance para evitar desaparecimentos durante lutas normais. 

<mark>if Distance(Boss, Player) > BossDespawnDistance -> Boss Distance Despawn</mark> 

Boss Distance Despawn não é morte: não gera loot, kill, recompensa, quest ou efeito de morte. 

### **17.9. BOSS PERDIDO DEVE SER SUBSTITUÍDO** 

Se um boss for removido por distância, ele deixa uma vaga de boss que precisa ser reposta imediatamente, mesmo que o Boss Timer ainda não tenha atingido o próximo intervalo. 

Boss Distance Despawn -> Boss Replacement Boss Replacement != novo Boss Timer Event 

O replacement não zera nem altera o Boss Timer. 

### **17.10. REPLACEMENT RECALCULA O TIER NA POSIÇÃO ATUAL** 

O boss de replacement não precisa pertencer ao mesmo Tier do boss perdido. O sistema recalcula a predominância usando a posição atual do jogador e escolhe um BossDefinition ainda não usado naquele dia. 

Boss perdido BT1 Player agora em região onde T2 é predominante Replacement -> boss BT2 elegível 

### **17.11. BOSS DERROTADO NÃO É REPOSTO IMEDIATAMENTE** 

Se o jogador matar o boss, não existe replacement automático. A próxima entrada depende do Boss Timer. Apenas Distance Despawn gera reposição imediata da vaga perdida. 

### **17.12. MÚLTIPLOS BOSSES CONTINUAM POSSÍVEIS** 

Cada Boss Timer completado adiciona uma nova vaga. Se bosses anteriores continuarem vivos, eles podem acumular. A restrição é de identidade: nenhum BossDefinition pode aparecer duas vezes no mesmo ActualDay. 

Mudança Estrutural do Projeto — documento de trabalho editável 

### **17.13. TIER SUPPRESSION TAMBÉM AFETA BOSSES** 

Se T1 foi suprimido, BT1 fica inelegível. O sistema procura a maior predominância entre Tiers ainda disponíveis. 

### **17.14. BOSS SPAWN POSITION** 

Boss Event ou Replacement | Resolve Boss Tier Priority | Select Unused BossDefinition | Find Spawn Position Around Player | Outside Safe Zone | Preferably Outside Camera | Minimum Player Distance | Walkable A* Node | Obstacle Validation | Spawn | Immediate Aggro 

Bosses nunca nascem dentro da Safe Zone, mas podem entrar nela, perseguir e atacar normalmente após o spawn. 

### **17.15. PROTEÇÃO CONTRA ABUSO DE DESPAWN** 

Como Distance Despawn consome um BossDefinition do dia e gera outro boss, o sistema deve impedir reroll voluntário caminhando poucos passos. Use BossDespawnDistance amplo, hysteresis e/ou BossReplacementCooldown. Um boss só deve ser considerado perdido quando estiver realmente muito distante. 

## **18. LOOT** 

Loot continua ligado ao monstro, não à região. 

Portanto: 

encontrar um Tier alto cedo através de exploração distante significa poder conseguir materiais melhores cedo. 

Isso é proposital. 

Entretanto, analise os riscos econômicos: 

- farm precoce de materiais extremamente valiosos; 

- possibilidade de acelerar demais armas; 

- combinação com críticos/builds fortes; 

- employees; 

- Magnet; 

- bônus econômicos. 

Não bloqueie automaticamente loot por dia. 

Primeiro avalie se o risco do combate já é suficiente para justificar a recompensa. 

Mudança Estrutural do Projeto — documento de trabalho editável 

## **19. EMPLOYEES** 

O sistema de Employees também precisa ser revisado. 

Atualmente existiam conceitos relacionados a: 

- Current Floor; 

- mudança de Floor; 

- cross-Floor Collector; 

- spawn do employee quando player troca de Floor. 

Isso deixa de fazer sentido. 

Agora teremos um mapa contínuo. 

Analise como: 

- Helpers; 

- Collectors; 

- Strong; 

- Fast; 

- virtualização; 

- milhões de Employees; 

devem funcionar nesse mapa. 

Especial atenção para Collections fora da tela. 

Não quero milhões de GameObjects reais. 

A fantasia visual de uma operação gigantesca continua sendo essencial. 

Analise também a relação dos Employees com a Safe Zone. 

Por padrão, a Safe Zone é uma restrição para Enemy Spawn, não para Employees. 

Portanto não assuma automaticamente que Employees não possam aparecer, atravessar ou trabalhar nela. 

## **20. MAGNET** 

O Magnet anteriormente tinha limitação por Active Floor Position. 

Isso deixa de existir. 

Será necessário redesenhar seus três Tiers. 

Considere possibilidades como: 

- distância máxima do centro; 

- distância máxima em relação ao jogador; 

- raio operacional; 

- capacidade; 

- eficiência; 

mas NÃO escolha arbitrariamente. 

Proponha alternativas e explique qual preserva melhor a progressão original do Magnet. 

## **21. REMOTE CONTROLLER** 

O antigo Remote Controller permitia viajar entre andares visitados. 

Isso também perde a função atual. 

Precisamos decidir se ele: 

1. será removido; 2. será substituído; 3. será transformado em Fast Travel; 4. permitirá teleporte para landmarks desbloqueados; 5. possuirá outra função. 

Mudança Estrutural do Projeto — documento de trabalho editável 

Analise e proponha. 

## **22. FLOOR VARIANTS** 

O planejamento atual possui: 

10 Floors × 5 Variants = 50 Floor Variants. 

Isso deixa de fazer sentido exatamente dessa maneira. 

Precisamos decidir o que acontecerá com essa quantidade enorme de conteúdo. 

Considere alternativas: 

- um mapa principal grande com variantes completas; 

- regiões modulares; 

- chunks; 

- biomas; 

- landmarks variáveis; 

- layouts parcialmente randomizados; 

- versões do mapa por run. 

Minha preferência atual é possuir um mapa grande muito bem construído, em vez de fabricar artificialmente 50 mapas somente porque o roadmap antigo previa isso. 

Não preserve as 50 Variants apenas por apego ao documento antigo. 

Analise qual abordagem realmente beneficia o novo jogo. 

## **23. CÂMERA** 

Aproveite essa revisão para finalmente dar um dono explícito ao Camera System, que estava ausente no roadmap anterior. 

Precisamos definir pelo menos: 

- câmera seguindo o jogador; 

- Pixel Perfect; 

- comportamento em mapa grande; 

- possíveis limites; 

- camera shake; 

- relação da câmera com spawn de monstros; 

- spawn preferencialmente fora da visão; 

- comportamento futuro durante bosses. 

Não precisa implementar polish completo agora. 

Quero principalmente a arquitetura base. 

A Safe Zone é independente da câmera. 

Portanto uma posição precisa passar pelas duas verificações quando aplicável: 

OutsideSafeZone 

e preferencialmente: 

OutsideCameraView. 

Estar fora da câmera não torna uma posição válida caso ela esteja dentro da Safe Zone. 

## **24. MAPA E SISTEMA ESPACIAL** 

Como agora existe um único mapa grande, precisamos formalizar um sistema espacial que atenda Safe Zone, progressão radial, spawn, despawn e replacement. 

Mudança Estrutural do Projeto — documento de trabalho editável 

O jogo deverá conseguir identificar e configurar: 

- posição do centro; 

- centro e raio da Safe Zone; 

- Spawn Safety Padding; 

- DistancePerSpawnDay; 

- DistanceBeyondSafeZone; 

- DistanceDayOffset; 

- EffectiveSpawnDay; 

- posições caminháveis; 

- posições caminháveis porém proibidas para spawn; 

- EnemyDespawnDistance e BossDespawnDistance; 

- ReplacementSpawnMinDistance e ReplacementSpawnMaxDistance; 

- obstáculos; 

- regiões válidas de spawn; 

- posições de baú e traps; 

- landmarks; 

- regiões proibidas. 

Considere Tilemap, Bounds, Collider, Grid, A* GridGraph, Spatial Regions, Spawn Volumes ou combinação desses sistemas. A solução precisa funcionar em mapa grande sem se tornar exageradamente complexa. 

Walkable Area pode incluir a Safe Zone Spawnable Area deve excluir a Safe Zone Difficulty Band é derivada da distância do Player ao centro Replacement Area é um anel ao redor do Player 

## **25. DAY/NIGHT VISUAL** 

Quero que Dia e Noite sejam reconhecíveis imediatamente. 

Porém o jogo continua sendo pixel art top-down. 

Considere: 

- Global Light 2D; 

- luzes locais; 

- mudança gradual de iluminação; 

- tint; 

- fog/ambience; 

- partículas; 

sem destruir a legibilidade. 

A mudança precisa continuar compatível com hordas grandes. 

## **26. HUD** 

A HUD deverá continuar mostrando claramente: 

- tempo restante; 

- demanda; 

- inventário/bag; 

- vida; 

- energia; 

e agora provavelmente: 

- indicação visual de Dia/Noite. 

Não quero necessariamente mostrar explicitamente: 

Mudança Estrutural do Projeto — documento de trabalho editável 

"Você está na Zona Tier 7". 

A dificuldade por distância deve preferencialmente ser percebida através do mundo e dos inimigos. 

Também não é obrigatório colocar: 

"SAFE ZONE" 

escrito na HUD. 

A existência da região segura de spawn pode ser comunicada visualmente pelo próprio mapa, iluminação, arquitetura ou outro elemento ambiental. 

## **27. PROGRESSÃO E FILOSOFIA** 

NÃO altere a filosofia central do jogo. 

Ela continua sendo: 

ficar forte 

↓ 

dominar a ameaça atual ↓ buscar uma região mais perigosa ↓ sofrer novamente ↓ receber loot melhor ↓ ficar mais forte ↓ dominar ↓ repetir. 

Também permanecem fundamentais: 

- demanda diária; 

- logística; 

- inventário limitado; 

- retorno para venda; 

- employees; 

- automação; 

- builds absurdas; 

- números chegando a milhões/bilhões/trilhões; 

- vitória no Dia 15; 

- pós-game até Dia 30; 

- sensação de ficar extremamente roubado. 

A Safe Zone deve ajudar a organizar o fluxo do mapa e impedir spawns injustos no centro, mas não pode transformar o centro em um botão de invulnerabilidade. 

Mudança Estrutural do Projeto — documento de trabalho editável 

## **28. NÃO TRANSFORMAR O MAPA EM ZONAS FIXAS DE TIER** 

O mapa não deve possuir regiões como "norte = Tier 8" ou "sul = Tier 3". A nova estrutura possui faixas radiais de DistanceDayOffset, mas cada faixa apenas escolhe um EffectiveSpawnDay; dentro dela, os Tiers continuam sendo sorteados pela distribuição daquele dia e período. 

Portanto, duas pessoas na mesma faixa ainda podem encontrar composições diferentes. A região não define um Tier fixo; ela define qual tabela de progressão será usada. 

A Safe Zone continua sendo a única exceção espacial absoluta: dentro dela, Enemy Spawn Chance = 0 independentemente de dia, noite, Tier ou boss. 

## **29. SISTEMA DE SAVE** 

Revise o RunState considerando a nova arquitetura. Ele deverá guardar pelo menos: 

- ActualDay; 

- duração total do dia; 

- upgrades; 

- Tiers suprimidos; 

- Employees; 

- armas; 

- bônus; 

- quests; 

- cards; 

- Progress Tracking; 

- demais estados persistentes da run. 

EffectiveSpawnDay não deve ser salvo como progressão permanente; ele é derivado da posição atual do jogador e da configuração de distância. Se o jogo salvar posição durante o dia, ele poderá ser recalculado no load. 

UsedBossIdsToday é estado diário. Se existir save/checkpoint no meio do dia, deve ser persistido para impedir repetição de bosses após reload. Se o save continuar exclusivamente entre dias, o conjunto pode simplesmente reiniciar com o novo ActualDay. 

Estados antigos exclusivamente relacionados a CurrentFloor, ActiveFloorPosition, RemoveTowerLayer e Stair Routing devem ser removidos ou migrados quando não tiverem mais responsabilidade real. 

## **30. PERFORMANCE — SIMULATION RINGS E RECYCLING POR DISTÂNCIA** 

A remoção do Floor Sleep cria um novo desafio: um mapa grande dentro da mesma Scene. A solução deve combinar activation distance, virtualização e recycling por distância. 

Próximo ao Player -> simulação completa | Longe -> simulação simplificada / AI e Animator reduzidos quando apropriado | Além de EnemyDespawnDistance -> remover entidade e criar replacement relevante | 

Além de BossDespawnDistance -> remover boss e repor sua vaga com a predominância atual 

Reavaliar: AI activation distance, Animator activation, rendering, enemy pooling, projectile pooling, loot aggregation, Employees virtualization, objetos fora da câmera, cache de nós válidos, Safe Zone, Spawn Annulus e custo dos replacements. 

Distance Despawn não pode causar Instantiate/Destroy excessivo em sequência. Considere pooling e hysteresis. O objetivo é fazer a população acompanhar a exploração sem manter centenas de entidades abandonadas simulando do outro lado do mapa. 

Mudança Estrutural do Projeto — documento de trabalho editável 

## **31. RESULTADO QUE EU QUERO DESTA REVISÃO** 

Faça a análise em etapas. 

### **ETAPA 1 — Impact Analysis** 

Liste todos os sistemas do GDD e roadmap afetados pela remoção dos Floors e pelas novas regras de EffectiveSpawnDay, Safe Zone, Distance Despawn/Replacement e bosses sem repetição. Classifique cada um como: 

- permanece igual; 

- pequena adaptação; 

- grande refatoração; 

- removido; 

- substituído. 

### **ETAPA 2 — Nova arquitetura** 

Defina e mostre como se relacionam: 

- Single World Map; 

- Map Center; 

- Safe Zone; 

- ActualDay; 

- CurrentPhase; 

- DistancePerSpawnDay; 

- DistanceDayOffset; 

- EffectiveSpawnDay; 

- Spawn Table; 

- Enemy Tiers; 

- Tier Suppression; 

- Population; 

- Distance Despawn; 

- Replacement Spawn Annulus; 

- Boss Timer; 

- Boss Tier Predominance; 

- UsedBossIdsToday; 

- Simulation/Performance; 

- Employees; 

- Magnet; 

- Remote Controller; 

- Save. 

### **ETAPA 3 — Validação da tabela Dias 1–30** 

Valide a tabela já definida neste documento. Cada linha deve totalizar 100%. T10 permanece com 0% nas linhas base dos Dias 1–19 e ganha peso a partir da linha do Dia 20. Porém, não existe trava por ActualDay: se o EffectiveSpawnDay consultar Dia 20 ou superior, T10 fica elegível normalmente. 

### **ETAPA 4 — Effective Spawn Day / Distance Model** 

Defina matematicamente DistancePerSpawnDay e mostre exemplos concretos de ActualDay x DistanceDayOffset x EffectiveSpawnDay para dias iniciais, médios e finais. Considere a borda efetiva da Safe Zone como origem da progressão radial e o fato de o mapa ser finito. O máximo DistanceDayOffset deve emergir da geometria real do mapa; a expectativa inicial é algo em torno de cinco a seis estados de variação, com T10 ficando naturalmente inalcançável muito cedo sem qualquer Hard Lock. 

Mudança Estrutural do Projeto — documento de trabalho editável 

### **ETAPA 5 — Tier Suppression** 

Formalize a normalização após remover Tiers e mostre casos com nenhum Tier removido, T1 removido, T1–T3 removidos e T1–T5 removidos. 

### **ETAPA 6 — Safe Zone Validation** 

A* Walkable Node | Safe Zone Check | Spawn Annulus Check | Camera Check | Player Distance Check | Obstacle / Interaction Check | Valid Spawn Position 

Explique centro, raio, Padding, cache de posições, prevenção de tentativas infinitas e reutilização da validação por monstros e bosses. 

### **ETAPA 7 — Population Recycling** 

Formalize EnemyDespawnDistance, ReplacementSpawnMinDistance/MaxDistance, pending replacements e a distinção entre morte e Distance Despawn. Garanta que 1 despawn corresponda a 1 vaga de população reposta. 

### **ETAPA 8 — Boss System** 

Formalize BossSpawnInterval, Tier predominante, fallback por ordem de predominância, UsedBossIdsToday, reset por ActualDay, BossDespawnDistance, replacement imediato sem reset de timer e proteção contra reroll por distância. 

### **ETAPA 9 — Revisão do GDD** 

Liste exatamente quais seções precisam ser removidas, reescritas ou adicionadas. Não reescreva coisas sem relação com essa mudança. 

### **ETAPA 10 — Revisão das 14 Deadlines / 56 Sprints** 

Preserve o máximo possível da ordem saudável Fundação -> Gameplay -> Vertical Slice -> Enemy Framework -> Heróis -> Conteúdo -> Economia -> Employees -> Meta -> Balance -> Release, mas retire/replaneje tarefas de Floor System, Stair Routing, Floor Sleep, Floor Variants, Remove Tower Layer, Cross-Floor e Remote Controller antigo. 

Adicione explicitamente donos para World Map Framework, Camera Framework, Day/Night, EffectiveSpawnDay/Distance System, Spawn/Respawn, Spatial Data, Safe Zone, Spawn Position Validation, Distance Despawn/Replacement, Simulation/Activation, Tier Suppression e novo Boss System. 

### **ETAPA 11 — Migration Plan** 

Como já existe código implementado, produza KEEP / ADAPT / DELETE / REPLACE para FloorManager, FloorIdentity, Active Floor Position, FloorActivationCheck, Floor Sleep, Stair Routing, Combat Scope, MeleeAttackSlotManager por Floor, Population, Enemy Framework, Monster Animation Generator, BossSpawnManager, FloorPopulationManager, FloorSpawnUtility, A* GridGraph, Save/RunState e GameEvents. 

Avalie principalmente se FloorSpawnUtility pode evoluir para uma utility global de spawn, distinguindo Walkable Node de Valid Enemy Spawn Node e suportando Safe Zone, Spawn Annulus e caches. 

Mudança Estrutural do Projeto — documento de trabalho editável 

## **32. PRINCÍPIO FINAL** 

Essa mudança deve simplificar e melhorar o jogo, não apenas trocar uma arquitetura complexa por outra igualmente complicada. 

O resultado desejado é um grande mapa contínuo em que tempo e exploração determinam organicamente o nível das ameaças. 

No centro existe uma Safe Zone de spawn: nenhum monstro ou boss nasce dentro dela, mas inimigos podem entrar, perseguir e atacar normalmente. 

Fora dela, a distância não aplica bônus arbitrários de Tier. Ela avança o EffectiveSpawnDay em faixas configuráveis. O jogador que se afasta passa a receber novos spawns e replacements como se estivesse consultando dias futuros da tabela, enquanto ActualDay continua controlando a progressão real da run. Como o mapa é finito, existe um teto espacial para esse avanço. 

Tier 10 não possui proibição especial por ActualDay. Ele simplesmente tem 0% nas linhas base anteriores ao Dia 20. Se o jogador alcançar fisicamente uma região cujo EffectiveSpawnDay consulte Dia 20 ou superior, T10 pode aparecer. A expectativa de que isso só seja possível por volta do ActualDay 15 ou mais tarde deve vir do tamanho real do mapa e de DistancePerSpawnDay, não de uma regra hardcoded. 

Monstros muito distantes são reciclados sem recompensa e substituídos dentro de um anel válido ao redor do jogador, fazendo a população acompanhar a exploração. Bosses muito distantes também podem ser substituídos, mas a vaga é reposta com um Boss Tier coerente com a predominância atual. 

Bosses continuam surgindo por tempo, mas nenhum BossDefinition pode aparecer duas vezes no mesmo ActualDay. O Tier predominante tem prioridade; quando sua pool diária se esgota, o sistema avança para o próximo Tier por ordem de predominância. 

A fantasia final continua sendo: 

"Eu comecei apanhando dos inimigos mais fracos e terminei me afastando cada vez mais do centro para forçar o mundo a me entregar ameaças e recompensas cada vez mais absurdas." 

Faça a revisão como uma decisão real de arquitetura de produção. Questione inconsistências, identifique consequências não percebidas e evite soluções frágeis ou hardcoded. Não altere sistemas fora do escopo sem justificativa técnica clara. 

Mudança Estrutural do Projeto — documento de trabalho editável 

