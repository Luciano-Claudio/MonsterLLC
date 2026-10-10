# Game Design Document — Projeto Torre (nome provisório)
### Versão 1.01238 — Combate Comum Híbrido (attack real + trigger, sem dano de contato)

> **Legenda de status**
> - ✅ **Decisão confirmada**
> - 🟡 **Direção de design / pendência** — precisa ser fechada antes da implementação daquele ponto específico
> - 🔢 **Pendência de balanceamento**
> - 🔭 **Visão Expandida**

> **Nota de versão:** esta revisão **reabre de novo** a regra de combate comum fechada na v1.01237, depois de testar o modelo puro de contato/auto-disparo na prática (Sprint 16, correção) e o resultado não ter agradado — faltava a identidade visual do ataque, que o projeto já tinha a arte pronta pra usar. Decisão final (meio-termo entre o modelo completo original e a simplificação total): **(1)** todo monstro comum (Melee/Ranged) volta a ter uma animação `attack`/conjuração real, disparada no seu próprio cooldown, com um **Animation Event** decidindo o instante exato do golpe/disparo — pro Melee, isso é um **trigger direcional** fixo na frente do monstro (4 posições, uma por diagonal), sem posição travada nem cálculo de esquiva por reposicionamento como no modelo pré-Sprint-16; **(2)** o **dano de contato passivo foi removido por completo** — chegou a existir em paralelo ao golpe real (Sprint 16, correção) e ficou confuso testando; agora só existe o dano da animação; **(3)** **Slimes** (comuns e Mother Slime Green/Blue) são a única exceção permanente — ficam só no contato, sem `attack`, pra sempre; **(4)** Ranged ganhou também uma regra de fuga (se o jogador chegar perto demais, ele se afasta) e um segundo par de parâmetros de Animator, `AimX`/`AimY`, pra mirar no jogador de verdade mesmo fugindo (`MoveX`/`MoveY` continuam sendo só a direção de movimento); **(5)** Attack Budget continua removido — não voltou junto com o `attack`. Bosses entram nessa mesma regra híbrida (ficha a ficha no Bestiário), com a lista de exceções nomeadas inalterada. Nenhuma outra regra estrutural foi alterada.

---

## 1. Visão Geral

Roguelite de ação, exploração e economia. O jogador explora um único mundo contínuo, finito e gerado proceduralmente, afastando-se cada vez mais de uma Zona Segura central pra enfrentar ameaças (Threat Tiers) cada vez maiores, cumprindo demandas diárias crescentes de Monster Essence ao longo de 15 dias por run, com pós-game opcional até o Dia 30. Combate ativo (mira e ataque pelo mouse), sem XP e sem level-up tradicional — o poder vem de progressão de arma persistente na run, economia, logística e automação via employees.

**Mudança estrutural (documento de trabalho) — versão anterior usava uma torre de 10 andares fixos (50 Floor Variants artesanais) em vez do mundo único abaixo; ver `docs/new/` para os documentos de origem desta revisão.**

## 2. Pitch

*Você entra fraco, com uma bag minúscula e uma arma qualquer. O mundo ao redor está infestado — e o reino quer sua cota diária de Essência. Cada passo pra longe do centro é uma aposta: você consegue matar mais rápido do que consegue carregar, antes que a ameaça fique maior do que você consegue aguentar?*

## 3. Gêneros e Referências

- **Gênero primário:** roguelite de ação com forte camada de gestão de tempo e economia.
- **Referência de loop econômico:** Coal LLC (demandas diárias, loja entre turnos, employees promovíveis).
- **Referência de combate:** ação com mira pelo mouse, ataques desviáveis, hordas — estética de Vampire Survivors, mas com ataque ativo, não automático.

## 4. Pilares de Design ✅

1. O jogador deve ficar absurdamente forte.
2. Toda vez que fica forte, deve existir algo acima dele.
3. Progressão econômica e habilidade devem coexistir.
4. O jogo não termina quando o jogador "vence".
5. Logística é tão importante quanto dano.
6. Runs recomeçam de verdade.

Fantasia central: *"Eu comecei contando Monster Essence de 1 em 1 e agora estou produzindo trilhões enquanto uma multidão de funcionários faz o trabalho por mim."*

---

## 5. Escopo — MVP vs. Visão Expandida

### MVP ✅
Mundo único, finito e gerado proceduralmente, com Zona Segura central e evolução visual/ambiental por dia (Seção 25) · 15 dias + pós-game até Dia 30 · 10 heróis (Seção 17) · sistema de demanda/venda/morte/save · inventário de loot · employees (ajudante + coletor) com árvore de promoção · 3 linhas de quest · baús com mimic + cartas · 2 traps (Falling Rock, Floor Spikes) · loja com 3 abas · Modo Padrão e Modo Free · agregação visual de drops e suporte a números grandes · Menu Principal com New Game / Continue Game / Settings / Exit · Map Selection (MVP: só o mundo único).

### Visão Expandida 🔭
Heróis futuros (Demonologist, Necromancer, The Gambler, Plague Doctor — Seção 18) · modos futuros além de Padrão/Free · novos mapas além do mundo principal (Cripta, Oceano) · novas traps.

**Nota histórica — a ideia de mapa único nasceu aqui, nesta mesma seção 🔭**
Antes desta revisão estrutural (ver `docs/new/` e nota no topo da Seção 1), esta seção registrava um "conceito de mapa contínuo" debatido e **descartado para a Torre** na época — a Torre era andares fixos com escada, e o motivo documentado era que o terreno contínuo quebraria o Floor Sleep e exigiria redesenhar manualmente até 6 combinações de corte como mapas completos, sem ganho aparente sobre o modelo antigo. A ideia nunca foi abandonada de verdade — o designer continuou desenvolvendo-a fora do roadmap, e a geração procedural por dados (Seção 25) resolve exatamente o problema que a tinha descartado antes (não é mais preciso desenhar manualmente cada combinação). Registrado aqui só para o histórico não parecer uma contradição não-intencional pra quem ler o GDD depois — a decisão atual (Seção 24/25) substitui esta nota por completo.

---

## 6. Menu Principal

Sistema formal com 4 opções: ✅

- **New Game** — inicia o fluxo completo de nova run (Seção 8).
- **Continue Game** — carrega diretamente o save existente, sem repetir nenhuma escolha (Seção 8, Seção 43).
- **Settings** — abre opções; conteúdo específico fica no documento de UI/UX (Seção 51).
- **Exit** — fecha o jogo.

---

## 7. Map Selection

Etapa formal do fluxo de **New Game**, que **não** aparece em Continue Game (a run salva já carrega o mapa escolhido anteriormente). ✅

- **MVP:** apenas 1 mapa jogável — **o mundo único** (Seção 24/25).
- A arquitetura já suporta a etapa mesmo com um único mapa, para permitir expansão futura sem retrabalho estrutural.
- Mapas futuros (Cripta, Oceano) usariam a mesma estrutura geral de run, economia, heróis e progressão — sem regra própria definida agora. 🔭

---

## 8. Estrutura da Partida — Máquina de Estados

```text
MENU PRINCIPAL
│
├── New Game
│      ↓
│   Escolha do modo (Padrão / Free)
│      ↓
│   Escolha do herói (entre os desbloqueados)
│      ↓
│   Map Selection (MVP: só o mundo único)
│      ↓
│   Cria nova run — gera o mundo a partir do WorldSeed (Seção 25)
│      ↓
│   DIA 1 — Gameplay
│
├── Continue Game
│      ↓
│   Carrega o save existente — o checkpoint da última Loja alcançada (Seção 43)
│      ↓
│   Abre diretamente essa Loja
│      ↓
│   Start Day N
│      ↓
│   Gameplay
│
├── Settings
│
└── Exit
```

### Regra importante — Continue Game ✅
Ao escolher Continue Game, o jogador **não** passa novamente por escolha de modo, herói, mapa, ou pela geração do mundo — o `WorldSeed` já pertence à run salva e é carregado diretamente (o mundo é regenerado deterministicamente a partir dele, mais os deltas salvos — Seção 25). O fluxo vai direto para a Loja do checkpoint salvo.

### Fluxo completo do Modo Padrão, dia a dia ✅

```text
DIA N — Gameplay
   ↓
Tempo chega a zero  OU  jogador usa a porta
   ↓
Validar demanda
   ↓
   ├─ Demanda NÃO cumprida → GAME OVER → run encerrada → Menu Principal
   │  (o save NÃO é apagado — continua sendo a Loja que precedia o Dia N; ver Seção 43)
   │
   └─ Demanda cumprida → Tela de Resultados → Loja → SAVE AUTOMÁTICO → Start Day N+1
```

**No Modo Free**, o mesmo fluxo ocorre sem a etapa de validação de demanda: `Tempo zera OU porta → Resultados → Loja → SAVE → Start Day N+1` (Seção 42).

Repete até o Dia 15.

### Dia 15 (Modo Padrão) — dois caminhos com consequências de save diferentes ✅

```text
DIA 15 — Demanda cumprida → Tela de Vitória Oficial
   │
   ├─ Menu Principal → run termina como vitória
   │      (NÃO passa por Resultados/Loja/novo autosave — o save
   │       permanece sendo a Loja que precedia o Dia 15; ver Seção 43)
   │
   └─ Continuar → Tela de Resultados do Dia 15 → Loja → AUTOSAVE →
                   (novo checkpoint = Loja que precede o Dia 16) → Start Day 16 → pós-game
```

**No Modo Free**, o Dia 15 é atingido pela conclusão normal do dia (sem demanda), mas a Tela de Vitória e as duas escolhas (Menu/Continuar) funcionam exatamente igual (Seção 42/44).

### Dias 16–30 (pós-game) ✅
Mesmo core loop, mesmo ciclo diário, mesma regra de save a cada Loja. No Padrão as demandas continuam crescendo; no Free não existem demandas (Seção 42).

```text
DIA 30 (Modo Padrão) — Demanda cumprida → Tela de Encerramento Definitivo
   ↓
Conquista Steam concedida
   ↓
Run termina obrigatoriamente
   ↓
Menu Principal
   (o save continua sendo a última Loja alcançada antes do Dia 30;
   não há autosave adicional após a tela de encerramento — Seção 43)
```

**No Modo Free:** `Dia 30 concluído normalmente → Tela de Encerramento Definitivo → Run termina → Menu Principal`, com o mesmo tratamento de save — mas **sem** a conquista Steam, que é Account Progression exclusiva do Padrão (Seção 44).

### Modo Standard vs. Modo Free — mesma máquina de estados, uma diferença ✅
Free usa exatamente a mesma estrutura de dia acima (gameplay → tempo zera/porta → Resultados → Loja → Save → Start Day N+1), incluindo Dia 15 e Dia 30. A única diferença está na validação de fim de dia: no Standard, o fim de dia verifica a demanda (não cumprida → Game Over); **no Free não existe demanda, então não existe essa verificação nem Game Over por ela** — o dia simplesmente encerra e segue direto para Resultados. Regras de progressão do Free (o que ele não concede) estão na Seção 42.

### O que salva, o que reseta (ver também Seção 15 e Seção 43) ✅
- Save é um **checkpoint da última Loja alcançada** — nunca um registro "por dia" que se apaga; ver semântica completa na Seção 43.
- **Run-Persistent:** gold, tier de arma por herói, bonuses comprados, employees possuídos, progresso de quests, cartas de pergaminho ativas, dia atual, modo, herói e mapa da run, e o `WorldSeed` usado para gerar o mundo único dessa run (Seção 25).
- **Daily:** tempo restante, demanda/progresso de venda (quando aplicável — Seção 42), inventário do jogador, monstros/bosses no mundo, loot no chão, Boss Timer global (Seção 22), Energia da Ultimate (Seção 11).
- **Reseta ao iniciar uma nova run** (inclusive trocar de modo): tudo Run-Persistent e Daily, exceto Permanent Account State (heróis, achievements, estatísticas de conta — Seção 15).
- **Existe um único slot de save de run, compartilhado entre os modos** — não há save separado por modo. Um novo autosave sempre sobrescreve o anterior, independentemente de qual modo pertencia o save anterior ou o novo (Seção 43).

---

## 9. Sistema Central de Pausa

Regra central única, referenciada por todos os subsistemas dependentes de tempo. ✅

### O que aciona a pausa
Abrir o Inventário (TAB), abrir o Controle Remoto (Q), a seleção de cartas de pergaminho, e futuramente o Pause/Menu geral (Seção 46).

### O que a pausa interrompe
> **Regra: PAUSA = NENHUM TEMPO DE GAMEPLAY AVANÇA.**

Relógio do dia, monstros (movimento e IA), ataques, projéteis, employees, pets/summons dependentes de simulação, cooldown de ataque, cooldown do Controle Remoto, cooldown/ausência de Employees, duração de buffs temporários, duração da Ultimate, duração de transformações, Boss Timer global (Seção 22), qualquer outro timer dependente da gameplay.

**Energia da Ultimate:** pausa não reduz nem zera a Energia — ela só avança por kills, nunca por tempo (Seção 11).

### Relação com o Sistema de Habilidades (Seção 12) ✅
Pausar entre o início da animação e o instante do hit congela o progresso sem cancelá-lo. Ao despausar, a habilidade continua exatamente daquele ponto. Isso é diferente da morte (Seção 11), que **cancela** em vez de congelar.

---

## 10. Core Loop

Loop compartilhado pelos dois modos (Seção 42):

```text
COMEÇA O DIA → entra na torre → combate ativo (LMB/RMB) →
coleta loot (chão → bag, TAB para ver) →
decide: volta e vende OU continua explorando com mais risco →
tempo esgota OU sai pela porta → resolução de fim do dia
```

**Modo Padrão:**
```text
resolução de fim do dia → validar demanda →
não cumprida → GAME OVER
cumprida → Resultados → Loja → Save → Start Day N+1
```

**Modo Free:**
```text
resolução de fim do dia → sem validação de demanda →
Resultados → Loja → Save → Start Day N+1
```

---

## 11. Sistema do Jogador

### Atributos base ✅
Vida, Vida Máxima, Dano (via fórmula abaixo), Chance de Crítico, Velocidade de Movimento, Velocidade de Ataque, Energia / Energia Total da Ultimate, Velocidade da Passiva.

**Não existe atributo de Armadura.** Defesa base é exclusivamente Vida/Vida Máxima; mecânicas defensivas adicionais (shield, cura, lifesteal, transformação) são recursos próprios de heróis específicos, nunca um sistema universal. ✅

### Fórmulas de dano e vida ✅
- **Poder Ofensivo Total** = Dano Base do Herói × Multiplicador de Dano da Arma (tier atual — Seção 19).
- **Dano final de uma fonte** = Poder Ofensivo Total × Participação da Fonte × Coeficiente da Habilidade × (1 + bônus de dano da run e da fonte).
- **Vida Máxima** = Vida Base do Herói × Multiplicador de Vida da Arma (tier atual — Seção 19) × (1 + soma dos bônus de Vida da run).
- Crítico dobra o dano do hit final quando ocorre.
- Pets/summons/passivas ofensivas automáticas consomem uma fatia do Poder Ofensivo Total (referências de proporção: ~40% para pet único / ~60% kit ativo; ~50% pool de summons do Necromancer com diminishing returns; ~25% para outras passivas automáticas — ajustáveis em balanceamento).
- Shields e curas passivas escalam sobre a **Vida Máxima final**; lifesteal usa a Vida Máxima apenas como teto de cura por hit (Seção 33 detalha a separação entre os três mecanismos).

### Energia da Ultimate — persistência diária (Daily) ✅
- A Ultimate carrega por **kills**, nunca por tempo — monstros mais fortes concedem mais carga por kill. O total necessário varia por herói.
- **A Energia é estado Daily do herói.** Permanece acumulada ao longo do dia inteiro, independentemente de para onde o jogador se mova no mundo único, ou de abrir menus:
  - Mover-se para qualquer distância do centro, voltar ao centro, ou usar o Controle Remoto (Seção 26): **não reseta.**
  - Abrir inventário/Controle Remoto (pausa): **não reseta nem reduz.**
- **A Energia zera em exatamente 3 situações, e apenas nelas:**
  1. O jogador **usa** a Ultimate (RMB com Energia cheia → Energia volta a 0).
  2. O herói **morre**.
  3. **O dia termina.**
- Fora dessas três situações, a Energia nunca reseta.

### Quais kills carregam a Ultimate — Hero-Owned Combat Source ✅
Nem toda kill próxima ao herói concede Energia. A regra: **fontes de combate pertencentes ao herói ("Hero-Owned") carregam a Ultimate; kills de Employees não.**

**Contam** (Hero-Owned Combat Sources):
1. Kills causadas diretamente pelo herói — ataque primário, Ultimate, área persistente, DoT, hitboxes, qualquer habilidade do próprio kit.
2. Kills causadas por **pets do kit** (Phoenix, Blood Elemental, equivalentes futuros).
3. Kills causadas por **summons pertencentes ao kit** (summons do Necromancer, criaturas temporárias de habilidades, outras entidades ofensivas do próprio herói).

Isso inclui explicitamente kills por DoT/área persistente mesmo que o herói já tenha se afastado fisicamente do local — ex.: um monstro que morre no rastro de fogo do Mage ou nas facas persistentes do Ranger depois que o herói já se afastou ainda conta, pois a fonte de dano pertence ao kit do herói. **Mundo único, sem Combat Scope por Floor** (revisão estrutural — ver `docs/new/` e Seção 24/25): não existe mais um domínio de busca/aplicação separado por andar; a única coisa que pode impedir uma kill de contar é o próprio monstro ter sido removido por **Distance Despawn** (Seção 23) antes de morrer — e `Distance Despawn ≠ Enemy Death` por definição (Seção 23), então um monstro reciclado por distância nunca gera kill nem Energia pra ninguém, não precisa de regra extra aqui.

**Não contam:**
- Kills causadas por **Ajudantes/Employees**, independentemente do tier, quantidade, dano, Strong/Fast, ou de quem iniciou o combate. Exemplo: monstro com 100 HP, jogador causa 90, Ajudante causa os 10 finais → a kill pertence ao Ajudante → **não concede Energia da Ultimate**.

Esta regra define apenas **quais kills contam** — não altera quanto de Energia cada monstro concede, o threshold de cada herói, ou a velocidade de carregamento (tudo isso continua 🔢 balanceamento, conforme já estabelecido: monstros mais fortes concedem mais Energia por kill).

### Movimentação e mira ✅
- **Movimento:** WASD. **Mira:** posição do mouse, resolvida em 8 direções (N, S, L, O, NE, NO, SE, SO). **Ataque primário:** LMB. **Ultimate:** RMB.

### Morte — ordem de eventos ✅
A penalidade de tempo não é uma animação com duração própria — é uma **subtração imediata**. Sequência estrutural:

```text
Player morre
   ↓
Cancela estados temporários ativos
   ↓
Destrói loot carregado
   ↓
Zera Energia da Ultimate
   ↓
TimeRemaining -= 30 segundos
   ↓
TimeRemaining <= 0 ?
   │
   ├─ NÃO → fade de tela → respawn no centro (0,0,0) com vida cheia → dia continua
   │
   └─ SIM → resolve encerramento do dia
              ↓
           Mode?
              ├─ Padrão → validar demanda
              │      ├─ Cumprida → Tela de Resultados
              │      └─ Não cumprida → Game Over (o save não é apagado — Seção 43)
              │
              └─ Free → sem validação de demanda → Tela de Resultados
```

- **Cancela imediatamente qualquer estado temporário ativo** (diferente de pausa, que apenas congela — Seção 9): Ultimate em andamento é interrompida, transformações (ex.: forma de urso do Druid — Seção 17.4) terminam **sem conversão proporcional de HP**, áreas/efeitos temporários no chão desaparecem, uma interação em curso é cancelada, summons **temporários** são destruídos.
- **Pets permanentes de kit** retornam junto com o personagem na transição. 🟡 Se a animação de summon/bloqueio inicial se repete nesse retorno ainda não está fechado.
- **Sem multiplicador de penalidade por distância do centro.**
- **Necromancer é exceção explícita a esta seção inteira quando implementado** — ver Seção 18 e Seção 33.

---

## 12. Sistema de Habilidades — Timing

Regra estrutural central: **o dano de uma habilidade não está automaticamente preso ao fim da animação.**

```text
Clique → Cast Start → Instante do primeiro Hit → Hits adicionais (se houver) →
Fim da animação → Recovery → Cooldown
```

Suporta ataques simples, multi-hit, em área, projéteis (Seção 13), ataques em duas fases, habilidades contínuas/persistentes, transformações, ultimates. **Relação com a pausa:** ver Seção 9. **A mesma filosofia estrutural se aplica ao timing de ataque dos monstros (Seção 22).**

---

## 13. Projéteis — Taxonomia

Regras gerais ✅: direção fixada no instante do disparo, não controlável depois de solto, tempo de vida próprio, quantidade máxima de alvos que pode atingir.

| Categoria | Comportamento | Exemplos |
|---|---|---|
| **Straight Projectile** | Reto, com ou sem perfuração | Flecha do Ranger, martelo do Paladin, projétil do Blood Mage |
| **Multi-Straight (leque)** | Múltiplos retos em ângulos distribuídos | Ranger com mais de 1 flecha |
| **Hitscan** | Dano instantâneo sem projétil visível | Gunslinger |
| **Homing** | Segue o alvo mais próximo | Ataque primário do Cleric |
| **Ground Target / Impact Area** | Viaja até colidir ou alcançar distância máxima, explode em área | Ultimate do Mage, ultimate do Blood Mage, Rat People, Goblin Raider |
| **Persistent Area** | Fica no lugar, dano contínuo | Facas da ultimate do Ranger, rastro de fogo do Mage |
| **Orbiting Hitbox** | Gira ao redor do herói | Espadas da ultimate do Paladin, ossos do Necromancer |
| **Dash Damage** | Deslocamento curto, dano ao longo de todo o trajeto (não só no ponto de chegada) | Ataque primário do Assassin |
| **Summoned Target Hit** | Entidade sumonada mirando o alvo mais próximo | Vinhas do Druid, osso-boomerang do Necromancer |
| **Rotating Line / Sweep** | Linha de mira que gira progressivamente, disparando em cada direção do giro | Ultimate do Gunslinger |
| **Rectangular Beam** | Retângulo de dano fixo na direção da mira | Ataque primário do Demonologist |
| **Self Area Pulse** | Trigger de área única no próprio herói, ativado por Animation Event (não é contínuo/orbital) | Ataque primário do Rogue, ataque primário do Mage (8 triggers direcionais, só o da direção da mira ativa), primário do Barbarian |

**Correção de categoria (pós-detalhamento de heróis):** o ataque primário do Rogue **não** é mais um Orbiting Hitbox — vira **Self Area Pulse** (um trigger circular único nos pés dele, ativado 1x por Animation Event, não giro contínuo). O antigo "adagas orbitando" foi descartado.

Ataques em área centrados no próprio herói (Barbarian, Mage, Rogue, Plague Doctor) e a ultimate global do Cleric (Seção 17.6) não usam projétil — são hitbox de área ou efeito de campo, não uma entidade que viaja.

### Modificadores de pós-vida do projétil — o que acontece ao fim da trajetória (Sprint 16) ✅
Além da categoria de comportamento em voo (tabela acima), todo projétil — de herói ou de monstro comum — pode ter um destes desfechos ao colidir ou alcançar sua distância máxima. Não são categorias novas, são **modificadores combináveis** com qualquer categoria da tabela:

- **Gruda (Sticky):** fica preso no alvo (ex.: flechas grudam no Player) ou no chão onde caiu, com tempo de vida próprio até desaparecer. Vários projéteis grudados ao mesmo tempo é intencional — reforça visualmente "quanto dano venho tomando".
- **Explode (Impact Area):** ao colidir ou alcançar o alcance máximo, dispara uma nova animação de explosão (sem precisar de Animation Event — a colisão em si já inicia essa animação nova no Animator) e causa dano em área no ponto do impacto. Já coberto pela categoria Ground Target/Impact Area da tabela.
- **Deixa condição no chão:** ao colidir ou alcançar o alcance máximo, cria um trigger persistente no chão (fogo, espinhos, etc.) que causa dano a quem passar por cima, durando um tempo antes de desaparecer. Comportamento de área independente do projétil que o criou — mesma família da Persistent Area.
- **O projétil é o próprio monstro:** ao colidir ou alcançar o alcance máximo, uma unidade real nasce ali (ex.: Bicephalous — o "slug" lançado vira um Slug de verdade). O projétil não é só efeito visual, é a fonte de uma nova entidade viva.
- **Teleguiado (Homing):** persegue o alvo até acertar — já existe como categoria própria na tabela (Homing), mas também pode se combinar com qualquer um dos desfechos acima (ex.: um projétil teleguiado que também gruda, ou que também explode).

### Perfuração por reserva de dano — exclusivo de projétil de herói (Sprint 17+) ✅
Diferente do projétil de monstro comum (que aplica seu dano cheio de uma vez e é destruído no primeiro contato — Seção 22), todo projétil físico de herói carrega uma **reserva de dano igual ao dano total do golpe**, não um "hit único":
- Ao colidir com um monstro, aplica dano até **o menor entre** a reserva restante e a vida atual do monstro.
- Se sobrar reserva depois de matar aquele monstro, o projétil **continua a mesma trajetória** e pode acertar outro monstro na frente, repetindo a regra até a reserva zerar.
- Independente de reserva sobrando, todo projétil ainda desaparece ao alcançar sua **distância máxima própria** (Seção 13, regra geral) — reserva não zerada não estende o alcance.
- Ex.: projétil com 100 de reserva acerta um monstro com 50 de vida (mata, sobra 50 de reserva) e continua até acertar um segundo monstro com 100 de vida (aplica os 50 restantes, esse segundo monstro fica com 50 de vida) — o projétil só some aí (reserva zerada) ou ao bater no limite de distância antes disso.
- **Variante com sprite ligada à reserva restante (Ranger — Seção 17.2):** quando o projétil é visualmente um "cluster" de N unidades (ex.: N flechas agrupadas), o sprite exibido reflete `ceil(reserva restante / dano de 1 unidade)` — perder reserva suficiente pra "gastar" uma unidade inteira troca a sprite pra representar N-1, continuando a mesma trajetória sem recriar o objeto.

### Movimentação Ortogonal/Diagonal — 8 direções, sprites próprias (Sprint 17+) ✅
Todo projétil de herói e de monstro tem 8 sprites de trajetória (N/NE/E/SE/S/SW/W/NW) escolhidas no instante do disparo — precisa de um Animator próprio no projétil (blend tree ou 8 estados diretos) pra tocar a sprite certa durante o voo, igual ao herói/monstro que o disparou. Alguns projéteis (tipicamente os que não são flecha/faca — ex.: bola de fogo) também têm uma **animação de impacto/desaparecimento** ao invés de simplesmente sumir quando a reserva de dano zera ou a distância máxima é alcançada.

**Observação registrada pro futuro, não é trabalho agora:** "chão com condição negativa" não é exclusividade de projétil — o Spectre (Bestiário, Threat Tier 5) cria uma superfície de gelo direto no golpe de contato (sem projétil nenhum), e o mesmo padrão volta a aparecer no Dragon/Undead Dragon/Dragon Hatchling (fogo) e na Ultimate do Mage (Seção 17, também sem projétil). Ainda não existe um sistema genérico único cobrindo os três casos (projétil, golpe direto, área de herói) — cada um nasce isolado quando o conteúdo correspondente for implementado; vale considerar unificar quando houver 2-3 exemplos reais construídos pra comparar. **A parte de "efeito visual de status na frente do sprite" já foi desenhada** (Seção 33, "Efeitos Nocivos") — o que fica pendente aqui é só se os três *gatilhos* (projétil, golpe direto, área de herói) acabam compartilhando o mesmo código de aplicação de Efeito ou continuam isolados por conteúdo.

---

## 14. Attack Budget — **Removido (Sprint 16)** ✅

Este sistema existia pra evitar que muitos monstros entrassem em "estado de ataque" ao mesmo tempo — fazia sentido no modelo de combate com Telegraph/Hitbox/Recovery (Seção 22, versão anterior). Esse modelo foi testado na prática na Sprint 16 e substituído, por padrão, por dano de contato (Melee) e auto-disparo em alcance (Ranged), cada um limitado só pelo próprio cooldown do monstro — não existe mais um "estado de ataque" discreto pra limitar entre vários monstros ao mesmo tempo.

**O que substitui isso:** nada substitui o Attack Budget em si — não existe mais um "estado de ataque" discreto pra limitar. O Population System (Seção 23) continua sendo o único controle de **quantos monstros existem** ao redor do jogador. Mas surgiu, separadamente, um limitador de um eixo diferente — **quantos Melee ficam em contato simultâneo com o jogador** (não é sobre atacar, é sobre lotação física perto dele): o `MeleeAttackSlotManager` (Seção 22, "Lotação perto do jogador — flanco"), teto configurável único (mundo único, sem mais pool por Floor — padrão 12), quem não cabe fica flanqueando num anel em vez de amontoar. Não é o Attack Budget ressuscitado — não limita ataques nem existe por AttackType, só por Melee, e resolve um problema de legibilidade visual de horda, não de timing de combate. Bosses e as exceções documentadas no Bestiário (Seção 51) que ainda usam Animation Event real (Goblin Sapper, Orc Shaman, Serpent) não participam do flanco nem precisam de budget — cada um só tem sua própria instância de ação especial (bomba, totem, exposição) rodando por vez.

---

## 15. Terminologia de Persistência

Três categorias de dado, usadas de forma consistente em todo o documento: ✅

- **Permanent Account State** — nunca reseta, independente de run. Divide-se em duas categorias distintas (Seção 49):
  - **Account Progression** — heróis desbloqueados, achievements, e demais recompensas/critérios permanentes de unlock.
  - **Lifetime Statistics** — dados puramente informativos de perfil (total histórico de kills, gold vendido, dias jogados, bosses mortos, etc.). Uma estatística registrada **não** é, por si só, progressão.
- **Run-Persistent** — persiste entre os dias de uma run, reseta ao iniciar nova run: tier de arma (Seção 19), **buffs persistentes da run** obtidos por cartas de pergaminho (Seção 31), bonuses comprados (Seção 41) incluindo upgrades de Pickup Radius (Seção 37), employees (Seção 34), progresso de quests (Seção 29), gold, modo/herói/mapa da run, o `WorldSeed` do mundo único gerado pra essa run (Seção 25), Tiers suprimidos pela Supressão de Ameaça (Seção 27).
- **Daily** — reseta todo dia: tempo restante, vendas realizadas no dia, progresso da demanda quando aplicável ao Modo Padrão (Seção 42), inventário do jogador, monstros/bosses no mundo, loot no chão, Boss Timer global (Seção 22), Energia da Ultimate (Seção 11).

**Não existe mais lista de reset de escadas** — revisão estrutural (mundo único, Seção 24): escadas eram geografia fixa de cada Floor Variant e deixaram de existir junto com o Floor System.

O termo "permanente" isolado é evitado — cada sistema Run-Persistent é descrito como tal. Cartas de baú são descritas como **"buffs persistentes da run"**, nunca "buffs temporários".

---

## 16. Regras Comuns a Todos os Heróis

- Todos possuem os atributos da Seção 11.
- Ataque primário no LMB, Ultimate no RMB carregada por Energia via kills (Seção 11).
- **Não existe progressão por XP ou level-up durante a exploração.** Buffs persistentes da run (Seção 15) são obtidos exclusivamente através de baús/pergaminhos encontrados durante os dias (Seção 30–31), enquanto upgrades econômicos e a progressão de arma são adquiridos na loja entre dias (Seção 19, Seção 41).
- Desbloqueio é Account Progression (Seção 15).
- O jogo precisa rastrear tudo que o jogador faz numa run para viabilizar qualquer critério de desbloqueio (Seção 49).

### Direção do herói — sempre a mira, nunca o movimento (Sprint 17+) ✅
Diferente dos monstros comuns (Seção 22, `MoveX`/`MoveY` separado de `AimX`/`AimY`), o herói **não tem um par de parâmetros de movimento independente da mira**. A direção que o Animator usa pra `walk`/`attack`/`ultimate` é sempre a direção do mouse (já resolvida em 8 direções por `DirectionUtility.SnapTo8Directions`, existente desde a Seção 11) — **mesmo andando pra um lado, o herói olha e ataca pra onde o mouse aponta.** Ex.: jogador segurando movimento pro SW com o mouse mirando NE: o herói anda fisicamente pro SW, mas toca a animação de `walk` do NE e ataca/atira pro NE.

### Animações padrão de todo herói ✅
`walk`, `idle`, `damage`, `die`, `attack`, `ultimate` — todo herói do MVP tem essas 6 no mínimo (alguns ganham estados extras próprios: pet/summon do Mage e Blood Mage, transformação do Druid e do Assassin, dome do Paladin, etc., cada um na própria ficha). Mesma arquitetura de Animation Event já madura no Bestiário (Seção 22): o instante do dano/efeito real é decidido por um Animation Event dentro do clipe, nunca por um timer solto no código.

### Regra de arquitetura — Blend Tree de 4 pontos (só diagonais) nunca usa AimX/AimY (Sprint 19) ✅
**Todo Blend Tree 2D Freeform Directional que só tem pose desenhada pras 4 diagonais (NE/NW/SE/SW), sem pose real pra nenhum cardeal (N/E/S/W), deve usar os parâmetros `DiagonalAimX`/`DiagonalAimY` — nunca `AimX`/`AimY` diretamente.** Motivo (bug real encontrado em produção, Sprint 19): `AimX`/`AimY` vêm de `DirectionUtility.SnapTo8Directions`, que pode resolver num cardeal puro; um Blend Tree com só 4 amostras (as diagonais) não tem nenhum ponto ali, e o Unity tem que misturar as 2 diagonais vizinhas, que ficam exatamente equidistantes — o cálculo de peso nessa "zona morta" de 45° em volta de cada eixo cardeal é instável (ruído mínimo decide pra qual lado pende), parecendo um "flip" aleatório perto do eixo. `DirectionUtility.SnapTo4Diagonals()` resolve isso: cada metade do plano (dividida só pelo sinal de X e de Y) sempre cai em exatamente 1 diagonal, sem zona ambígua. `HeroController` já expõe isso pronto (`DiagonalAimDirection`, alimentando `DiagonalAimX`/`DiagonalAimY` no Animator, calculado junto com `AimDirection` toda vez que a mira atualiza) — só falta o Blend Tree em si apontar pros parâmetros certos.

- **Use `AimX`/`AimY`** só quando o Blend Tree realmente tem as 8 poses desenhadas (N/NE/E/SE/S/SW/W/NW) — ex.: `Attack` do Mage/Ranger, `Ultimate` do Mage, `teleport_start`/`teleport_end` do Mage (esses usam `TeleportAimX`/`TeleportAimY`, uma cópia travada de `AimDirection` no instante do cast — ver ficha do Mage, Seção 17.3).
- **Use `DiagonalAimX`/`DiagonalAimY`** em todo o resto — `idle`/`walk`/`damage` de todo herói (sem exceção conhecida até agora), e qualquer estado futuro que só tenha arte diagonal (ex.: hoje isso é **todo** estado do Barbarian e do Ranger, exceto o `Attack` do Ranger, que já tem as 8 poses reais).
- Corrigido retroativamente no Barbarian, Ranger e Mage (Sprint 19) depois do bug aparecer em produção nos 3.

### Movimento só é permitido durante o estado `walk` do Animator 🔲 sujeito a validação em playtest
Regra de partida: o herói só se move enquanto o Animator estiver genuinamente no estado `walk` — qualquer outra animação (`attack`, `ultimate`, `summon`, etc.) bloqueia o movimento até ela terminar. Mesmo mecanismo já usado no Bestiário (`AnimatorStateCheck.IsInState`, Seção 22) pra nunca destravar o `transform.Translate` antes do Animator confirmar a troca de estado. **Ainda não confirmado que esse é o feel certo** — pode se provar frustrante em teste (ex.: travar o Mage inteiro durante a invocação do pet no início do dia) e precisar de exceções por herói.

### Knockback ✅
Vários ataques de herói (Barbarian primário/ultimate, flechas/facas do Ranger, orbes do Paladin, dash do Assassin, etc. — cada um documentado na própria ficha) empurram o monstro atingido pra trás no instante do Animation Event de dano. 🔢 força/distância do knockback e se ele varia por herói ainda não têm valor definido — placeholder ajustável em teste. **Bosses são imunes a knockback** (decisão do usuário, Sprint 18→19) — evita que um empurrão forte tire o boss da própria arena/posição de telegraph.

### Habilidade Secundária (Shift) — todo herói ganha 1, Sprint 18→19 ✅
Além de primário (LMB) e ultimate (RMB), todo herói do MVP ganha uma **terceira habilidade, ativada por Shift** — cooldown próprio, mais alto que o do primário, e (assim como o primário/ultimate) afetado por upgrades futuros ainda não implementados (Tier de Arma/Cards, Seção 19/31). Cada ficha de herói (Seção 17) documenta a habilidade específica dele.

**Regra de cancelamento:** só habilidades que **transformam o herói ou o impedem de agir** (imobilizam, trocam o kit temporariamente, etc.) podem ser canceladas — clicar Shift de novo enquanto ativa a interrompe antes do tempo normal. Habilidades que são só um buff/efeito instantâneo (ex.: o dano dobrado do Barbarian) **não são canceláveis** — rodam até o fim sozinhas. Toda habilidade cancelável tem uma **animação de saída própria** (ela já teria uma "end" natural de qualquer forma, ex.: o Ranger saindo da camuflagem) — cancelar simplesmente adianta a transição pra essa mesma animação de saída, não cria um clipe extra só pro cancelamento.

**Bloqueio de outras ações durante a habilidade — varia por herói, não é uma regra única:**
- **Bloqueia tudo** (ataque, ultimate, movimento) — ex.: Ranger (camuflagem imóvel).
- **Bloqueia só o ataque, movimento continua** — ex.: Druid (nova transformação em coruja, se move mas não ataca).
- **Não bloqueia nada** — a maioria das outras: é só um efeito de dano/cura/mobilidade que roda em paralelo ao jogo normal (ex.: o buff do Barbarian), e assim que termina o jogador já pode fazer o que quiser, sem transição especial.

Cada ficha (Seção 17) especifica qual desses 3 comportamentos vale pra aquele herói.

### Prioridade visual entre Efeitos Nocivos simultâneos ✅ (correção — não envolve passiva)
**Correção de uma confusão registrada aqui ontem:** a passiva de herói (brilho do Barbarian, bolha do Paladin, aura do Cleric) **nunca** disputa esse sistema — ela sempre tem seu próprio slot visual dedicado, documentado na ficha de cada herói (Seção 17) e na Seção 33. O que de fato precisa de prioridade é só entre **Efeitos Nocivos** (status que monstro aplica no herói) quando mais de um está ativo ao mesmo tempo — regra completa, com categorias e exemplo, na Seção 33 ("Efeitos Nocivos"). Continua 🟡 só a lista de categorias em si (hoje só Prisão e DoT existem — cresce conforme novos monstros forem desenhados), não a estrutura do sistema, que já está fechada.

---

## 17. Heróis do MVP (10)

### 17.1 Barbarian — inicial ✅
- **Dano Base / Vida Base:** 2,0 / 42.
- **Ataque primário:** golpe de espada no chão — animação real do próprio Barbarian, sem hitbox contínua. Um **Animation Event** no frame exato em que a espada toca o chão ativa **1 de 4 triggers fixos** (NE/NW/SE/SW, mesmo padrão do golpe direcional do Bestiário — Seção 22, `GetHitboxForFacing`), escolhido pela direção da mira no momento do golpe — o GameObject não vira, só a animação muda. Deixa uma **rachadura no chão** (decal) por um tempo até sumir. Aplica **knockback** (Seção 16) em quem for atingido.
- **Ultimate:** salto no ar seguido de queda na mesma posição; no frame da queda (Animation Event), libera **8 projéteis retos** nas 8 direções fixas (N/NE/E/SE/S/SW/W/NW — Straight Projectile, Seção 13), cada um com dano igual a **2× o dano atual do Barbarian** (`stats.damage`, já incluindo Tier de Arma — 🔢 multiplicador ajustável em teste). Aplica knockback em quem for atingido.
- **Passiva — mais dano com vida perdida (revisão Sprint 18→19: sem efeito visual):** a cada **10% de Vida Máxima perdida**, ganha **+25% de dano**, num total de **até +200% (3× o dano total) ao perder 80% ou mais da Vida Máxima** (ou seja, com 20% de vida ou menos restante). Escala em degraus de 10% (perdeu 10% → +25%; perdeu 20% → +50%; ...; perdeu 80%+ → +200%, teto). **A mecânica continua idêntica — só o brilho visual contínuo foi removido** (decisão do usuário; sem substituto visual por enquanto).
- **Habilidade Secundária (Shift) — buff de dano e velocidade:** 1 animação só (sem direções, sem fases start/during/end — igual `die`). Enquanto ativa, o Barbarian ganha **mais dano e mais velocidade de movimento** por alguns segundos (🔢 duração e multiplicadores ajustáveis em teste). **Não bloqueia nada** — ataque, ultimate e movimento continuam funcionando normalmente durante o buff. **Não cancelável.**
- **Targeting:** nenhum no primário — a direção do golpe é sempre a mira (Seção 16), não a movimentação.
- **Cartas específicas:** nenhuma.
- **Desbloqueio:** disponível desde o início da conta.
- **Particularidade/filosofia:** é o herói de referência para "posicionamento importa mais que dano puro" (Seção 21) — seu desempenho depende diretamente de o jogador agrupar inimigos antes de atacar, já que tanto o primário quanto a ultimate são ataques em área que recompensam múltiplos alvos agrupados.

### 17.2 Ranger ✅
- **Dano Base / Vida Base:** 1,6 / 30.
- **Ataque primário:** flecha reta na **direção exata da mira** (Straight Projectile, Seção 13) no instante do disparo — **revisão da Sprint 17, substitui as 8 direções fixas originais**: só a pose do personagem e a sprite da flecha ficam presas nas 8 poses possíveis (limitação de arte), a trajetória de voo em si mira exato, sem travar em ângulos de 45°. Rotaciona o sprite de verdade em vez de trocar entre variações — 1 sprite só, gira pra apontar pra direção real. Não continua seguindo o mouse depois de solta. Aplica knockback em quem for atingido.
- **Quantidade de flechas — presa ao Tier de Arma, não a carta:** 1 flecha na Arma Básica; ganha **+1 flecha a cada tier de arma** (Seção 19, 15 tiers no total — 14 upgrades sobre a Arma Básica), chegando a **15 flechas no tier máximo (Arcane)** — teto revisado em relação à versão anterior deste documento (era 7). Decisão explícita do usuário: "gostei d+ de ter muitas flechas na tela".
- **Formação — cunha em "V" (revisão da Sprint 17, substitui o leque angular original):** em vez de ângulos diferentes, todas as flechas viajam **paralelas na mesma direção** (a mira), só nascem deslocadas — flechas de dentro nascem mais à frente, as de fora mais atrás e mais afastadas lateralmente (mesma lógica de bando de pássaros voando; par com maior índice sempre na posição-base, mais próximo nasce mais à frente). Generaliza pra qualquer quantidade: número par sempre tem 2 flechas na ponta, número ímpar tem 1. 1 sprite de flecha só, sem variações por quantidade (substitui o design antigo de "sprite regressiva por cluster").
- **Dano por flecha ✅:** o dano total do golpe (`stats.damage × 2`, multiplicador 🔢 ajustável em teste) é **dividido igualmente entre as flechas** — cada flecha carrega essa fração como sua própria reserva de perfuração (Seção 13, mesmo mecanismo do `HeroProjectile` do Barbarian: atravessa monstro, gasta o mínimo entre reserva e vida do alvo, continua se sobrar). Ex.: dano base seria 4 com 1 flecha; com 2 flechas, dobra pra 8 e cada flecha sai com reserva de 4.
- **Ultimate ✅ (Sprint 18, Efeito Nocivo Bleeding adicionado na Sprint 19):** giro do Ranger lançando **8 facas nas 8 direções fixas**, uma por Animation Event — Blend Tree 2D de 4 diagonais (mesmo esquema do Attack/Idle/Walk/Damage), e cada um dos 4 clipes carrega **os 8 eventos** (um método próprio por direção, `AnimationThrowKnife_N/NE/E/.../NW`, não 1 método parametrizado — decisão explícita do usuário). Como 2 clipes se misturam pra quase qualquer ângulo, cada faca tem uma trava individual (só a primeira chamada por direção realmente lança) — mesma correção de fundo já aplicada no Attack/EnemyController. Cada faca é um projétil com reserva de dano (Seção 13) valendo **2× o dano do Ranger** em voo, sprite própria por direção (Animator com 8 estados soltos, sem Blend Tree — a direção nunca muda depois do lançamento). **Exceção à regra geral de projétil:** ao invés de simplesmente sumir, uma faca que esgota a reserva **ou** alcança a distância máxima **fica no chão** como Persistent Area (9º estado no mesmo Animator, sem variação de direção), valendo o dano normal do Ranger (1×) em **tick** (intervalo 🔢 ajustável, GDD não especifica cadência) a quem estiver na área — incluindo quem já estava em cima no instante exato do pouso — por **30s**. Aplica knockback tanto em voo quanto no chão. **Efeito Nocivo Bleeding (Sprint 19):** tanto o hit em voo (projétil acerta um monstro) quanto o tick no chão (monstro em cima das facas pousadas) aplicam Bleeding — **metade do dano do Ranger por segundo, durante 5s** (🔢 passível de nerf/buff) — via `StatusEffectController` (Seção "Efeitos Nocivos"), continuando mesmo depois do monstro sair da área.
- **Passiva:** a progressão de flechas por Tier de Arma **é** a passiva do Ranger — não é um efeito periódico à parte.
- **Habilidade Secundária (Shift) — camuflagem, regra completa (Sprint 18→19):** 3 animações, cada uma **1 estado só, sem variação de direção** (igual `die`) — `start` (se escondendo), `during` (escondido) e `end` (surgindo de novo). **Duração máxima de 5s na fase `during`** (🔢 passível de nerf/buff) — passado esse tempo, sai da camuflagem sozinho, do mesmo jeito que cancelar manualmente. **Bloqueia tudo** — ataque, ultimate e movimento ficam desabilitados até a habilidade terminar ou ser cancelada (mesmo nível de incapacitação do Trapped). **Monstros o ignoram** durante a fase `during` — ficam sem alvo, como se ele não existisse (mesmo princípio do stealth do Assassin, Seção 17.9). **Cura o Ranger** enquanto durar — **10% da Vida Máxima por segundo** (🔢 ajustável), tick a cada 1s; como a fase `during` tem teto de 5s, o cap de cura total é **50% da Vida Máxima** se ficar camuflado o tempo inteiro (emerge do próprio tick × duração, não é um teto separado). **Cancelável** — Shift de novo interrompe a qualquer momento antes do teto de 5s, adiantando direto pra animação `end`.
- **Targeting:** direcional pela mira, sem travamento de alvo.
- **Cartas específicas:** nenhuma — **mudança em relação à versão anterior deste documento**, a quantidade de flechas não vem mais de carta de baú, só de Tier de Arma.
- **Desbloqueio:** vencer 1 partida com o Barbarian.

### 17.3 Mage ✅
- **Dano Base / Vida Base:** 2,4 / 24.
- **Ataque primário — revisão Sprint 18→19, usa a rotação real (mesma técnica da flecha do Ranger):** o Mage casta uma magia (animação própria); no Animation Event, um **GameObject filho** — um trigger retangular que fica constantemente rotacionando pra acompanhar a mira, sempre grudado no Mage — dispara sua própria animação de fogo saindo na direção exata da mira (não mais travado em 8 direções fixas nem em 8 triggers separados). O filho aciona o dano no instante certo da própria animação.
- **Ultimate:** nasce da posição do próprio Mage (sem pontos fixos de lançamento — revisão Sprint 19, já que a mira é livre e não precisa de 8 "portas de saída" fixas) uma bola de fogo que viaja **no ângulo contínuo e exato do mouse, não travado em 1 dos 8 vetores** — **única exceção do MVP** entre os projéteis retos, que por padrão saem sempre travados numa das 8 direções (Ranger, Barbarian, Paladin, Blood Mage). Continua respeitando a regra geral de "direção fixada no instante do disparo" (Seção 13) — só não passa pelo snap de 8 direções antes de fixar. Explode ao colidir com uma entidade ou ao alcançar uma distância curta (Ground Target/Impact Area), com dano de impacto = **4× o dano do Mage** (🔢 ajustável), e deixa um **rastro de fogo persistente** no chão causando **0,5× o dano do Mage por segundo** (🔢 ajustável) a quem passar por cima, durando **30s** (🔢 ajustável). Quem fica no rastro também recebe o Efeito Nocivo **Fire** (queimadura): **metade do dano do Mage por segundo, durando 3s** (🔢 ajustável), continuando mesmo depois de sair do fogo — primeiro Efeito Nocivo de dano-ao-longo-do-tempo do jogo, sistema genérico já pronto pra outros efeitos futuros (Poison, Bleed etc.).
- **Habilidade Secundária (Shift) — teleporte na direção da mira (Sprint 19):** teleporta o Mage na direção exata do mouse, com um **alcance máximo** (se o mouse estiver além do alcance, vai só até onde o alcance permite, na mesma direção). 2 estados no Animator, `teleport_start` e `teleport_end`, cada um Blend Tree 2D Freeform Directional de **8 pontos** (mesma técnica do Attack dos monstros Ranged). No último frame de `teleport_start`, o Mage some (sprite desligada) e nasce um projétil "carregador" (rotacionado de verdade, ângulo livre) que viaja pela sala pela distância combinada; **o Mage só reaparece de verdade (posição + `teleport_end`) quando esse projétil termina de viajar** — a transição `teleport_start → teleport_end` não é automática, é disparada pelo código nesse instante exato, já que a duração da viagem varia com a distância. **Não cancelável.**
- **Passiva:** pet **Phoenix** — ver "Pets de início de dia (Mage/Blood Mage)" logo abaixo da ficha do Blood Mage (Seção 17.10) para o comportamento completo, compartilhado entre os dois heróis.
- **Cartas específicas:** dano, velocidade e velocidade de ataque da Phoenix, todas em %.
- **Desbloqueio:** vender 1.000 Monster Essence em um único dia da run — resolve o 🔢 anterior desta ficha.

### 17.4 Druid ✅
- **Dano Base / Vida Base:** 1,8 / 36.
- **Ataque primário:** vinha nasce no pé do monstro mais próximo (Summoned Target Hit, Seção 13) — um Animation Event sumona um circle trigger pequeno ali; quando a animação da própria vinha chega no frame de dano (Animation Event dela), causa dano em quem estiver dentro (pode acertar mais de 1 monstro se estiverem muito próximos, mas é incomum). Cada vinha causa o mesmo dano — o dano normal do Druid, sem divisão entre elas (diferente do leque do Ranger).
- **Quantidade de vinhas — presa ao Tier de Arma:** começa com **1 vinha**; **cada um dos 15 Tiers de Arma (Seção 19) soma +1 vinha**, até o teto de **15 vinhas** no Tier 15 (Divine). O mesmo Animation Event do golpe sumona todas as vinhas ativas de uma vez. **Nunca repete o mesmo alvo** — a vinha sempre mira o monstro vivo mais próximo que ainda não recebeu uma vinha nesta ativação; se todos os mais próximos já tiverem uma, ela vai para o próximo mais próximo sem vinha.
- **Ultimate — transformação em Alce, regra completa:**
  1. Ao ativar, o Druid se transforma em um **Alce** (correção de nomenclatura — revisões anteriores deste documento chamavam o animal de "urso"; Alce é o nome definitivo a partir de agora). Substitui **todas** as animações do Druid (`walk`/`idle`/`damage`/`die` próprias do Alce): **Vida Máxima aumenta temporariamente** (multiplicador 🔢 pendente de balanceamento — o valor ×2 usado nos exemplos é apenas ilustrativo); o **ataque primário muda** para uma patada frontal — **4 triggers fixos direcionais (NE/NW/SE/SW)**, mesmo padrão do golpe do Barbarian/Bestiário, no lugar das vinhas; **mais dano melee** e **maior velocidade de movimento** na forma de Alce. Durante a própria animação de transformação (entrando e saindo), o Druid fica **imune a dano**.
  2. **No instante da transformação, o Druid é curado para 100% da Vida Máxima da forma de Alce** — funciona como uma cura completa.
  3. Enquanto transformado, a vida se comporta normalmente (dano recebido reduz a vida atual do Alce normalmente).
  4. **Termina após 30s (🔢 ajustável) ou a qualquer momento que o jogador clicar RMB de novo pra cancelar.** Cancelar manualmente **zera toda a Energia** da Ultimate (precisa "farmar" de novo do zero) — mesmo custo de ter deixado o tempo acabar.
  5. **Quando a transformação termina normalmente, ou é cancelada por qualquer motivo que NÃO seja morte, o percentual de vida é convertido proporcionalmente para a forma humana** — nunca um valor absoluto, nunca travado no máximo humano, nunca restaurado automaticamente para 100%:

     ```text
     HealthRatio = CurrentBearHealth / BearMaxHealth

     HumanCurrentHealth = HumanMaxHealth × HealthRatio
     ```

     Exemplo ilustrativo (valores de HP apenas para explicar a regra, não confirmados como balanceamento): forma humana com 20/100 → ativa a Ultimate → cura para 200/200 (Alce) → recebe dano, fica em 180/200 (90%) → Ultimate termina → retorna como 90/100 na forma humana.

  6. **Morte durante a transformação é a única exceção — NÃO exige a conversão proporcional acima.** Se o HP do Alce chega a 0, o Druid morre e segue diretamente o fluxo universal de morte (Seção 11): a transformação é cancelada, a Energia zera, o loot é perdido, os 30s de penalidade se aplicam, e ele reaparece no centro do mundo (revisão estrutural — antes "no térreo", Seção 11/24) **em forma humana normal, sem a ultimate ativa, com vida cheia** — igual a qualquer outro herói.
- **Habilidade Secundária (Shift) — transformação em coruja, separada da ultimate (Sprint 18→19):** transformação **diferente** da Ultimate (Alce) — vira uma coruja. 3 fases: `start` (1 estado só, sem direção, igual `die`) → `during` (`walk` com **4 direções**) → `end` (1 estado só, sem direção). Ganha **velocidade de movimento maior**, e **monstros o ignoram** durante a transformação (mesmo princípio do stealth do Assassin, Seção 17.9). **Não pode atacar** enquanto transformado (bloqueia só o ataque — movimento continua liberado, diferente do Ranger que bloqueia tudo). **Cancelável** — Shift de novo adianta pra animação `end`. **Correção (Sprint 21):** esta seção previa as 4 direções como cardeais (N/E/S/W); a arte entregue pra fase `during` é diagonal, igual ao resto do Bestiário/heróis — o código foi adaptado pra arte real em vez do contrário (decisão do usuário), reaproveitando o mesmo snap de 4 diagonais (`DirectionUtility.SnapTo4Diagonals`) que todo herói já usa pra Idle/Walk/Damage.
- **Passiva:** nenhuma — a progressão de vinhas por Tier de Arma é quem faz esse papel, mesma lógica das flechas do Ranger.
- **Cartas específicas:** nenhuma — mesma mudança do Ranger, a quantidade de vinhas não vem mais de carta de baú.
- **Desbloqueio:** vencer 1 partida com o Mage.
- **Particularidade de implementação:** a ultimate substitui temporariamente todo o kit de ataque primário — exige máscara de estado clara ("transformado" vs. "normal"). A conversão proporcional de HP só se aplica a fim natural ou cancelamento sem morte; morte segue a regra padrão, sem exceção adicional.

### 17.5 Rogue ✅
- **Dano Base / Vida Base:** 1,5 / 28.
- **Ataque primário:** **mudança de categoria** em relação à versão anterior deste documento — deixou de ser adagas orbitando continuamente e virou **Self Area Pulse** (Seção 13): 1 trigger circular grande nos pés do Rogue, ativado **1 vez por Animation Event** a cada uso (não é contínuo/automático, é uma animação de ataque de verdade com cooldown, como qualquer outro herói). Aplica **knockback** em quem for atingido (mesmo padrão do golpe do Barbarian).
- **Ultimate:** bomba nasce do próprio Rogue e viaja em **ângulo livre** na direção da mira (mesma técnica da bola de fogo do Mage — Seção 17.3 — rotação real, não travada nas 8 direções; **Correção:** não "lançada na posição do mouse", o projétil parte da posição do herói) — viaja até colidir com um monstro **ou** alcançar o alcance máximo, o que vier primeiro, então explode: dano em área circular centrada na explosão, valendo **4× o dano do Rogue** (🔢 ajustável). Sem fase de superfície persistente no chão (diferença em relação à bola de fogo do Mage). Possui **cooldown mais baixo** que os demais heróis, permitindo uso mais frequente.
- **Habilidade Secundária (Shift) — cambalhota (mini-dash), Sprint 18→19:** dash curto em **1 de 4 direções fixas** (**Correção Sprint 22:** NE/NW/SE/SW, não N/E/S/W como previsto originalmente — a arte real funciona melhor em diagonal; escolhida pela mira no instante do uso, mesmo critério de snap dos outros heróis, só que só 4 opções em vez de 8. A trajetória do dash em si segue a mira livremente, só a pose do Animator fica presa às 4 diagonais). **Imune a todo tipo de dano e sem colisão física com monstro** durante a cambalhota inteira (atravessa quem estiver no caminho, sem travar no corpo do monstro), e aplica **knockback** em quem estiver perto dele durante o movimento. **Não cancelável.**
- **Passiva — nova, substitui a antiga:** ganha **4× mais Energia de Ultimate por kill** (ex.: um monstro que dropa 2 de Energia rende 8 pro Rogue). **A antiga passiva ("maior velocidade de movimento base") foi transferida pro Assassin** (Seção 17.9) — os dois heróis não podem reivindicar o mesmo traço de "mais rápido do elenco".
- **Targeting:** nenhum no primário (área nos próprios pés); Ground Target na ultimate.
- **Cartas específicas:** nenhuma.
- **Desbloqueio:** sobreviver até o fim do Dia 30 (o "final supremo"), com qualquer herói, **numa única run** — resolve o 🟡 anterior desta ficha (não acumula entre runs).

### 17.6 Cleric ✅
- **Dano Base / Vida Base:** 1,7 / 32.
- **Ataque primário:** projétil que persegue o monstro mais próximo (Homing, Seção 13) — se acertar e ainda sobrar reserva de dano (Seção 13), **para de perseguir** e continua reto na mesma direção que estava até esgotar a reserva ou alcançar a distância máxima. **Só pode ser usado se houver ao menos 1 monstro dentro do raio de ataque do Cleric** — sem monstro no raio, o clique de ataque simplesmente não faz nada (única exceção do MVP a "todo herói sempre pode tentar atacar"). **Implementação (Sprint 18→19):** já que o projétil muda de direção em voo (perseguindo), usa rotação real do sprite continuamente, mesma técnica descoberta na flecha do Ranger, em vez de trocar entre variações fixas. Upgrades da loja vão aumentar a velocidade do projetil e aumentar o atk speed!
- **Ultimate:** oração — **Correção:** não é global (Floor inteiro) como esta seção dizia antes, é um raio centrado no Cleric ("raio de visão", mesma categoria de área que todo outro herói usa). Todo monstro dentro do raio ganha o Efeito Nocivo **WordOfPain** (Seção 33 — Efeitos Nocivos), que paralisa (sem IA/ataque/movimento, mas continua tomando dano normal) e aplica dano por segundo ao mesmo tempo, durante alguns segundos (🔢 ajustável). O dano por tick é sempre **metade do dano normal do Cleric** (ex.: Cleric com 100 de dano aplica 50 por tick) — **não** leva o 2× da passiva, essa regra própria já substitui. WordOfPain tem prioridade visual sobre qualquer outro Efeito ativo no mesmo monstro (ex.: Fire/Bleeding já presentes continuam causando dano, mas o ícone mostrado é sempre o WordOfPain enquanto durar).
- **Habilidade Secundária (Shift) — a cura vira ativa, Sprint 18→19:** animação de reza, **4 direções** (**Correção Sprint 23:** NE/NW/SE/SW, não N/E/S/W como previsto originalmente — mesmo caso da Cambalhota do Rogue, a arte real é diagonal). A cura (uma % da própria Vida Máxima, 🔢 ajustável) vem em **4 ondas** (1/4 cada), disparadas por um efeito visual dedicado próprio (não mais 1 evento único) — cada onda também faz o status **Heal** (Seção 33, Efeitos Nocivos) piscar por cima de qualquer Efeito já ativo no Cleric (ex.: Fire), que volta a aparecer sozinho assim que a piscada termina. Acionada pelo jogador via Shift, não mais automática a cada 10s. **Não cancelável.**
- **Passiva — nova, substitui a antiga (Sprint 18→19):** monstros sofrem **2× de dano** do Cleric (🔢 passível de nerf). **A cura periódica automática deixou de existir como passiva** — virou a Habilidade Secundária acima.
- **Cartas específicas:** aumento do valor da cura (agora ativa via Shift, não mais periódica), em %.
- **Desbloqueio:** vencer 1 partida com o Druid.

### 17.7 Paladin ✅
- **Dano Base / Vida Base:** 1,8 / 50 (maior Vida Base do MVP).
- **Ataque primário:** martelo arremessado, projétil reto na **direção exata da mira** (Straight Projectile, Seção 13), lançado por Animation Event; tem sua própria animação de impacto ao esgotar a reserva de dano ou alcançar o alcance máximo (regra geral da Seção 13). **Implementação (Sprint 18→19):** mira livre + rotação real do sprite, mesma técnica da flecha do Ranger — como é só 1 projétil, não precisa de 8 variações fixas.
- **Ultimate:** 2 espadas orbitando o Paladin (Orbiting Hitbox, Seção 13) por alguns segundos. Implementação: um **GameObject filho dedicado** (não é o Animator do próprio Paladin) com 3 animações — `BladesStart`, `BladesCycle`, `BladesEnd` — e **2 triggers de dano que giram junto com a arte** (a própria animação precisa mover os triggers, não só o sprite). Quem colidir sofre **2× o dano do Paladin** (🔢 ajustável).
- **Habilidade Secundária (Shift) — shield bash, Sprint 18→19:** golpe de escudo nas **4 direções fixas simultaneamente** (N/S/E/W — não escolhe 1 pela mira, atinge as 4 de uma vez, formato de cruz), com **4 triggers** (um por posição). Um único Animation Event na animação causa dano e knockback em quem estiver em qualquer um dos 4 triggers. **Imune a dano** durante a animação inteira. **Não cancelável.**
- **Passiva:** de tempos em tempos (🔢 a cada 30s, ajustável) ganha um **shield** com vida própria que absorve dano no lugar do Paladin. Implementação: **outro GameObject filho dedicado** (separado do da ultimate, pra não colidir com ele), com **2 camadas visuais** — `DomeStart`/`DomeCycle`/`DomeEnd` **na frente** do Paladin e `DomeBaseStart`/`DomeBaseCycle`/`DomeBaseEnd` **atrás** dele (order layer). Sem duração — fica **indefinidamente** até a vida do shield chegar a 0. Absorção: dano recebido é descontado da vida do shield primeiro; se o shield tiver vida suficiente pra cobrir o hit inteiro, o Paladin **não sofre dano nenhum** naquele hit, mesmo que o hit sozinho exceda a vida restante do shield (o shield absorve o hit inteiro que o estoura, e só então quebra e vai pra `DomeEnd`/`DomeBaseEnd`) — só a partir do hit seguinte, sem shield, o Paladin volta a sofrer dano normalmente. Ao quebrar, entra em cooldown até reaparecer.
- **Cartas específicas:** aumento de quanto o shield pode absorver de dano, em %.
- **Desbloqueio:** vencer 1 partida com o Cleric.

### 17.8 Gunslinger ✅
- **Dano Base / Vida Base:** 1,2 / 28 (menor Dano Base do MVP, compensado por múltiplos tiros).
- **Ataque primário:** tiro instantâneo — Hitscan (Seção 13), sem projétil físico. 3 clipes de animação: `Shot_Orthogonal`, `Shot_Diagonal` (dependendo da mira estar numa das 4 direções cardeais ou diagonais — mesma separação orto/diagonal já usada no Bestiário) e `Projectile_Impact` (VFX à parte, instanciado no ponto onde o tiro terminou — no monstro atingido ou no fim da linha — a cada tiro da rajada, não 1 só por rajada inteira). Cada `Shot_*` é 2 frames repetidos por bala da rajada: frame 1 = disparo real (tem o Animation Event que resolve o dano instantâneo numa linha reta a partir do Gunslinger, e instancia o `Projectile_Impact`), frame 2 = recuo da arma antes do próximo par.
- **Quantidade de tiros por rajada — presa ao Tier de Arma:** 1 tiro na Arma Básica; **Iron → 2, Silver → 3, Emerald → 4, Gold → 5, Diamond → 6 (teto)** — os outros 5 tiers usados pelo Ranger pra flechas (Seção 17.2) ficam livres pra escalar dano/outros atributos aqui. **Quanto mais tiros, mais impreciso:** cada bala da rajada mira a direção fixa da mira (1 das 8) com um desvio angular aleatório, sorteado dentro de um cone que se abre conforme a quantidade de balas aumenta — 🔢 fórmula exata (largura do cone por bala) fica como placeholder ajustável em teste, sem uma curva fechada ainda.
- **Ultimate:** giro do Gunslinger disparando nas 8 direções — **8 Animation Events distintos**, um por frame-chave do giro, cada um disparando um tiro Hitscan naquela direção específica (Rotating Line/Sweep, Seção 13) com seu próprio `Projectile_Impact`.
- **Habilidade Secundária (Shift) — chicote, Sprint 18→19:** **exatamente o mesmo mecanismo do shield bash do Paladin** (Seção 17.7) — 4 triggers fixos nas 4 direções (N/S/E/W), ativados simultaneamente por um Animation Event, dano e knockback em quem estiver em qualquer um deles. Só muda a animação/arte (chicote em vez de escudo). **Não cancelável.**
- **Passiva:** monstros dropam **2× mais loot** (🔢 ajustável) — dobra a quantidade de qualquer item que já tenha dropado, não altera a chance de drop em si.
- **Cartas específicas:** nenhuma — mudança em relação à versão anterior, a quantidade de tiros não vem mais de carta de baú.
- **Desbloqueio:** vencer 30 dias jogando com o Ranger.

### 17.9 Assassin ✅
- **Dano Base / Vida Base:** 2,5 / 28 (maior Dano Base do MVP).
- **Ataque primário:** dash curto em **1 de 8 direções fixas** (Dash Damage, Seção 13) — animação em 2 partes, `Deadly_Dash_Start`/`Deadly_Dash_End`. Um trigger circular nos pés do Assassin acompanha o dash e causa dano **a todo mundo que ele tocar ao longo do trajeto inteiro** (não só no ponto de chegada — correção em relação à versão anterior deste documento), aplicando knockback. O deslocamento tem distância fixa e curta, mesmo que a mira aponte muito mais longe.
- **Ultimate — stealth, regra completa:**
  - Assassin muda pra uma **sprite mais sombria**, com seu próprio conjunto completo de `walk`/`idle`/`damage`/`die`. Imune a dano durante as animações de entrar/sair da forma sombria (mesmo padrão do Druid).
  - **Monstros deixam de enxergá-lo**, ficando sem alvo, como se estivessem sozinhos. **Único cuidado real (Sprint 27, quando o Assassin for construído):** os monstros de emboscada (Skeleton/Gargoyle, Seção 22) não podem voltar pra pose dormente/desativada nessa condição — continuam andando/parados normalmente, sem re-dormir. Fora esse ponto específico, não há mais regra nova a definir aqui; a implementação em si (como cada tipo de monstro perde o alvo durante o stealth) fica pra quando o Assassin chegar.
  - O ataque primário ganha uma variante sombria, **Thousand_Blades**, substituindo o Deadly_Dash enquanto a ultimate estiver ativa: `ITS_Thousand_Blades_Start` → `ITS_Thousand_Blades_Effect` (solta o dano no fim do dash, antes do End) → `ITS_Thousand_Blades_End`, nas mesmas 8 direções, valendo **2× o dano** do Deadly_Dash normal (🔢 ajustável).
  - **Dash fica sem cooldown** durante a ultimate, permitindo encadear vários seguidos.
- **Habilidade Secundária (Shift) — teleporte, Sprint 18→19:** teleporta na direção da mira, com **alcance máximo** (mesmo princípio do teleporte do Mage, Seção 17.3, mas mais simples — **sem** a fase de projétil visual). Só 2 animações, `disappear` (Animation Event dispara o teleporte) e `appear`, cada uma com **4 direções** (NE/NW/SE/SW, não as 8 completas). **Não cancelável.**
- **Passiva:** **maior velocidade de movimento base** do elenco — característica intrínseca do kit, não um efeito periódico (traço reatribuído do Rogue nesta revisão, ver Seção 17.5).
- **Cartas específicas:** nenhuma.
- **Desbloqueio:** 🟡 **Pendência nova (revisão estrutural, mundo único)** — o critério antigo ("alcançar o Andar 6") não existe mais, já que não há mais andares. Precisa de um critério equivalente definido no mundo único — candidato natural: alcançar a distância do centro onde `EffectiveSpawnDay` chega a 6 (Seção 23) pela primeira vez numa run, mas isso ainda não foi decidido pelo designer — ver Seção 53.

### 17.10 Blood Mage ✅
- **Dano Base / Vida Base:** 2,1 / 34.
- **Ataque primário:** projétil reto em 1 de 8 direções fixas (Straight Projectile, Seção 13) com reserva de dano (perfuração, Seção 13); ao esgotar a reserva ou alcançar a distância máxima, toca sua própria animação de impacto.
- **Ultimate — onda de choque em anel, regra completa:** o Blood Mage pula e, no frame da queda (Animation Event), nasce uma onda em forma de anel que cresce em 3 etapas de tamanho — **2×2 → 4×4 → 8×8**. **O dano viaja com a borda da onda, não preenche a área toda**: quando o anel 4×4 aparece, o quadrado 2×2 original já está seguro — é uma zona segura dinâmica, o jogador precisa dar um passo pra dentro ou pra fora no tempo certo pra desviar. Dano no instante de cada etapa = **5× o dano do Blood Mage** (🔢 ajustável).
- **Habilidade Secundária (Shift) — extração de sangue, substitui o lifesteal do primário (Sprint 18→19):** animação de reza (`extract_blood`, mesmo espírito da reza do Cleric), **4 direções** (NE/NW/SE/SW). No fim da animação, o **monstro vivo mais próximo** sofre dano e solta uma **orb de sangue** que viaja até o Blood Mage, curando-o ao chegar (mesmo visual de orb que já existia, só que agora acionado pelo Shift, não em todo hit do primário). **Não cancelável.**
- **Passiva — revisão Sprint 18→19, perde a metade de lifesteal:** só o pet **Elemental de Sangue** continua — ver "Pets de início de dia (Mage/Blood Mage)" abaixo, comportamento idêntico ao da Phoenix do Mage. **O lifesteal automático em todo ataque deixou de existir** — virou a Habilidade Secundária acima, que só afeta 1 monstro por uso, não todo hit do primário.
- **Cartas específicas:** dano, velocidade e velocidade de ataque do pet Elemental de Sangue, em %.
- **Desbloqueio:** matar X unidades de um monstro específico em um único dia. 🔢 monstro e valor de X pendentes de balanceamento — a estrutura do critério (monstro específico + quantidade em um único dia) já está definida, só os valores exatos ficam em aberto.

### Pets de início de dia (Mage/Blood Mage) — comportamento compartilhado ✅
Phoenix (Mage) e Elemental de Sangue (Blood Mage) usam exatamente o mesmo comportamento — só a arte muda por herói:
- **Sumão:** o herói toca uma animação de conjuração (duração = a do próprio clipe, não um tempo fixo) com um Animation Event no meio que sumona o pet; o pet nasce já com sua própria animação de surgimento (`summon`), depois passa a tocar `fly` (cobre parado e se movendo ao mesmo tempo — não existe pose de "parado" separada, sempre mostra a última direção conhecida).
- **Não é alvo válido** — monstros nunca o atacam, e ele não tem Vida própria (não pode morrer/ser destruído por dano).
- **Raio de perseguição/ataque tem centro no herói, não no pet** — o pet persegue e ataca qualquer monstro dentro desse raio ao redor do herói; fora dele, o pet ignora monstros e tenta ficar a uma distância curta do herói (🔢 ajustável), nunca sai da tela/visão do herói.
- **Trava no alvo até ele morrer** — uma vez escolhido, o pet não reavalia por distância a cada frame (senão troca de alvo toda vez que o jogador se move e um monstro diferente vira "o mais próximo"); só solta o alvo quando ele é destruído de verdade, aí sim escolhe o novo mais próximo.
- **Ataque do pet:** igual a um Melee comum do Bestiário — 4 triggers fixos direcionais, Animation Event decide o dano de quem estiver dentro no instante certo. Persegue até ficar em alcance de contato, então ataca.
- **Teleporta junto quando o herói teleporta, não anda** — revisão estrutural (mundo único, sem mais troca de andar): o gatilho antigo ("mudar de Floor") deixa de existir, mas a regra sobrevive igual nos 2 casos que ainda causam teleporte descontínuo do próprio herói — **respawn por morte** (Seção 11, volta ao centro) e **uso do Controle Remoto** (Seção 26, também volta ao centro) — o pet salta direto pro ponto de spawn ao lado do herói nesses 2 momentos (e descarta o alvo atual, já que um monstro muito distante não faz mais sentido como perseguição), em vez de "andar" visualmente a distância toda. Durante exploração normal (sem teleporte do herói), o pet nunca precisa desse salto — já persegue/segue normalmente.
- **Animações do pet:** `summon`, `fly`, `attack`, `die` (tocada quando o herói morre — GDD: "o pet retorna junto na transição" — o pet só é destruído de verdade no fim dessa animação, não instantaneamente).
- **Bloqueio de movimento do herói durante o summon:** ver a regra geral de "só anda durante `walk`" (Seção 16) — aqui é só uma aplicação concreta dela, ainda sujeita à mesma validação em playtest.

---

## 18. Heróis Futuros — Visão Expandida 🔭

**Não fazem parte dos 10 do MVP.**

- **Demonologist** — ataque primário: raio frontal retangular na direção da mira (Rectangular Beam). Ultimate: pentagrama que sumona uma criatura com dano alto. Passiva: pet não-alvejado, mesma família de Mage/Blood Mage. Cartas previstas: dano/velocidade/atk speed do pet; quantidade de pentagramas. Condição de desbloqueio: não definida.
- **Necromancer** — ataque primário: osso-boomerang (vai e volta, explode ao atingir o limite de alvos). Ultimate: 3 ossos orbitando o personagem (Orbiting Hitbox). Passiva dupla: *(1)* sumona esqueletos periodicamente, alvejáveis por monstros (diferente dos pets de Mage/Blood Mage/Demonologist), tempo de vida próprio ampliável, podem stackar múltiplas instâncias; *(2)* **2 vidas fixas por dia**. Ao perder a primeira vida: os summons ativos **são destruídos** (regra padrão, sem exceção), ele vira uma alma sem ataque/interação, imune a monstros, só podendo se mover; após alguns segundos, retorna com vida cheia (ajustável para ~50% em balanceamento futuro). **A exceção do Necromancer está apenas no comportamento de morte do herói em si** (não retorna imediatamente ao centro do mundo — Seção 11/24 —, entra em estado de alma no lugar onde morreu), **não na destruição dos summons**. Cartas previstas: dano/velocidade/atk speed/vida dos summons. Condição de desbloqueio: não definida.
- **The Gambler** — ataque primário: projétil de carta na direção da mira. Ultimate: 6 cartas aparecem ao redor do personagem e caem, explodindo em 6 círculos de dano. Passiva dupla, ativada periodicamente conforme a vida atual: carta de coração cura quando vida <100%; carta de diamante cria shield quando vida >100%. 🟡 **Pendência de design:** o material original não explica como o personagem chegaria acima de 100% de vida — não inventar overheal ou buff de HP temporário até revisão. Cartas previstas: cura da passiva e capacidade de bloqueio do shield, em %. Condição de desbloqueio: não definida.
- **Plague Doctor** — ataque primário: onda de ratos avançando (trigger retangular que caminha para frente e desaparece, dano em quem tocar). Ultimate: dano em área circular centrada no personagem, girando o cajado, sumonando fogos-fátuos ao redor. Passiva: 2 orbs orbitando infinitamente ao redor do personagem, dano ao colidir com monstros, mesmo bloqueio inicial de movimento dos outros summons de início de dia. Cartas específicas: nenhuma. Condição de desbloqueio: não definida.

---

## 19. Upgrades de Arma — Progressão Run-Persistent

### Regras estruturais ✅
- **Não são itens físicos.** Não ocupam inventário, não são vendáveis, não podem ser trocadas por tier inferior.
- 15 tiers compráveis na aba Upgrades: Copper, Iron, Steel, Silver, Sapphire, Emerald, Amethyst, Gold, Ruby, Diamond, Arcane, Infernal, Nightmare, Void, Divine. Arma Básica (tier 0) é gratuita, não conta como um dos 15.
- **Compra sequencial obrigatória:** exige possuir o tier imediatamente anterior; o botão do próximo fica bloqueado até isso.
- **Sistema percentual, nunca absoluto** (fórmulas na Seção 11): a arma multiplica o Dano Base e a Vida Base do herói.
- Prefixo do tier é universal; representação visual muda por herói.
- **Sem venda, sem downgrade.**
- **Reseta por completo em nova run** — volta ao tier 0. Progressão **Run-Persistent** (Seção 15), nunca chamada de "permanente".

### Filosofia de progressão ✅
Regra prática: uma arma ideal domina a ameaça do Dia/Tier anterior, é adequada para o Dia/Tier atual, e ainda sofre no Dia/Tier seguinte — essa relação de 3 níveis orienta o ritmo de progressão, não apenas os multiplicadores em si. **Revisão estrutural (mundo único):** o eixo de dificuldade deixou de ser "qual andar da Torre" e passou a ser o `EffectiveSpawnDay` (Seção 23) — a mesma régua de 3 níveis vale tanto avançando por `ActualDay` (tempo) quanto se afastando do centro (distância), já que os dois alimentam o mesmo cálculo. Tabela completa dos 15 tiers fica no documento de balanceamento (Seção 51).

---

## 20. Economia dos Primeiros Dias — Filosofia de Ritmo

**Revisão estrutural (mundo único):** os exemplos abaixo usavam "andar"/"subir" como eixo de risco, da época da Torre. O eixo equivalente no mundo único é a **distância do centro** (zona segura → anéis de spawn progressivamente mais difíceis, Seção 23/24) — "subir" virou "se afastar", e "voltar ao térreo" virou "voltar pra perto do centro"/usar o Controle Remoto (Seção 26). A lógica de ritmo em si (cada upgrade de arma reabre a decisão de arriscar mais um pouco) não muda.

### Padrão de sensação esperado ✅
- **Dia 1:** Arma Básica, bag minúscula (5 slots/stack 16) já cria decisões de risco mesmo perto do centro, na área mais fácil. Renda esperada ao fim do dia: suficiente para o primeiro upgrade de arma (Copper).
- **Dia 2:** com o primeiro upgrade, a área inicial fica sensivelmente mais fácil — mas se afastar mais do centro devolve a fragilidade, introduzindo ameaças novas (ranged, explosivos). Escolha entre caminho seguro (perto) e caminho arriscado (longe).
- **Dia 3 em diante:** o gargalo passa a ser "carregar tudo que consigo matar" — a bag pequena cria desejo genuíno pelos upgrades de slots, stack, employees e filtros.

### Ciclo emocional esperado ✅
Pressão ("preciso cumprir a demanda") → Eficiência ("se eu agrupar esses inimigos consigo matar vários") → Limitação ("minha bag está cheia") → Decisão ("volto agora ou arrisco mais e me afasto?") → Alívio ("cumpri a demanda") → Recompensa ("tenho dinheiro para um upgrade") → Power Fantasy ("essa área ficou fácil") → Curiosidade ("será que consigo ir mais longe?") → Choque ("esses monstros não morrem") → repete. **Este ciclo emocional descreve principalmente o Modo Padrão, que é a experiência principal de progressão da campanha; o Free preserva combate/economia/logística sem a camada de pressão da quota** (Seção 42). Esse ciclo é o critério para validar calibração de qualquer novo sistema, arma ou distância — não os preços em si. Valores de referência ficam no documento de balanceamento (Seção 51).

---

## 21. Habilidade × Eficiência

Dois jogadores com exatamente os mesmos upgrades podem terminar um dia com resultados bem diferentes — isso é desejado, não falha de balanceamento. ✅ Diferenciais de jogador habilidoso: agrupar inimigos para aproveitar ataques em área (ex.: Barbarian), alinhar arcos de ataque, desviar de telegraphs, escolher rotas de coleta eficientes, usar o Controle Remoto pra voltar ao centro só quando necessário (revisão estrutural — antes "retornar ao térreo", Seção 26).

---

## 22. Monstros e Bosses — Arquitetura de Comportamento

### Categorias ✅
Melee, Ranged, Boss. Variações Suporte/híbrido (ex.: Orc Shaman com totens) tratadas como variação dentro de Ranged/Support, sem virar categoria própria no MVP.

### IA comum (Melee/Ranged) — regra híbrida final (Sprint 16, correção) ✅
- Movimentação aleatória por padrão, evitando obstáculos.
- Ao entrar no raio de observação, o jogador é detectado e o monstro passa a perseguir diretamente.
- **Melee:** aproxima-se até o alcance de contato e ataca com uma animação `attack` real (arte própria), no seu próprio cooldown. Um **Animation Event** no frame do golpe ativa um **trigger direcional** fixo na frente do monstro (4 triggers, um por direção diagonal — o GameObject nunca vira, só a animação muda) — só causa dano se o jogador estiver dentro desse trigger naquele instante exato; fora disso, o golpe erra. **Sem dano de contato passivo** — chegamos a testar os dois em paralelo (contato + golpe real) e ficou confuso; removido de vez.
- **Ranged:** mantém distância dentro do alcance e foge se o jogador chegar perto demais. Ataca com uma animação de conjuração real, no seu próprio cooldown — o projétil só nasce quando o **Animation Event** da animação dispara, mirando a posição real do jogador naquele instante (sem telegraph). Também sem dano de contato passivo.
- **Direção de movimento ≠ direção de mira:** um Ranged fugindo alimenta `MoveX`/`MoveY` pra longe do jogador, mas `attack`/`idle_combat` precisam continuar "olhando" pro jogador — por isso existe um segundo par de parâmetros no Animator, `AimX`/`AimY`, recalculado a cada frame de combate em direção real ao jogador, independente de `Move()` estar fugindo, aproximando ou parado. `Walk` usa `MoveX`/`MoveY`; `Attack` e `IdleCombat` usam `AimX`/`AimY`.
- Cada monstro comum tem apenas **1 tipo de ataque** — diversidade vem da variedade de monstros, não de múltiplos ataques por indivíduo (exclusivo de Bosses e das exceções documentadas no Bestiário).
- **Animações padrão de todo monstro comum:** `idle`, `walk`, `idle_combat`, `attack` (com Animation Event), `damage`, `die`. **Única exceção permanente: Slimes** (comuns e Mother Slime Green/Blue) — ficam só no dano de contato, sem `attack`, para sempre.

### Movimentação — pathfinding real (A* Pathfinding Project + RVO Local Avoidance, Sprint 20) ✅
"Movimentação aleatória por padrão, evitando obstáculos" (acima) deixou de ser `transform.Translate` cru na direção do alvo — desde a Sprint 20, todo monstro comum usa o asset de terceiros **A* Pathfinding Project Pro** (Aron Granberg) pra navegação real em volta de parede/obstáculo estático, mais **RVO Local Avoidance** pra desviar de outro monstro em tempo real (o pathfinding em si não sabe que outro monstro existe — GridGraph só marca parede, não agente). **Revisão estrutural (mundo único — ver `docs/new/` e Seção 24):** o `GridGraph` por Floor (nomeado, restrito por `Seeker.graphMask`, um monstro de um Floor nunca encontrava nó de outro) deixa de existir como conceito — não há mais "Floors" para segmentar; a grade de navegação passa a cobrir o mundo único contínuo. Como manter isso performático num mundo grande (um `GridGraph` só, grades parciais atualizadas por distância, ou outra estratégia) é decisão técnica, não de design — ver Technical Architecture Document (Seção 51). A decisão de **pra onde** ir continua 100% código do jogo (`Move()` de cada `EnemyController`, perseguir/fugir/flanquear), só a execução de **como chegar lá** é da lib. `RVOController.priority` (quem cede espaço pra quem, numa negociação de desvio) é calculado por dano do monstro — quem causa mais dano tem prioridade maior, cede menos. Ver relatório completo em `docs/sprints/sprint-20.md` (decisões técnicas, bugs corrigidos, dívida técnica).

### Lotação perto do jogador — flanco (só Melee) ✅
Com hordas grandes, todo Melee tentando ficar dentro do `attackRadius` ao mesmo tempo lota o corpo a corpo e fica ilegível. Existe um teto (`MeleeAttackSlotManager`, padrão **12**, ajustável no Inspector) de quantos Melee podem estar em alcance de contato do jogador ao mesmo tempo. **Revisão estrutural (mundo único):** o teto deixa de ser "por Floor" e passa a ser **um único pool pra run inteira** — a justificativa antiga pro pool por Floor (um Melee com vaga reservada cujo Floor dormia via Floor Sleep nunca liberava a vaga, roubando capacidade do Floor onde o jogador estava agora) não existe mais, porque não há mais Floor Sleep nem Floors — a vaga libera pelas mesmas 2 regras de sempre (abaixo), então não há mais risco de vaga presa num Floor "fora de alcance". Um Melee que chega no `flankRadius` (Sprint 20: raio geral, configurado direto no `MeleeAttackSlotManager` junto do teto acima — deixou de ser um campo por monstro em `EnemyStats`) sem conseguir uma vaga fica **flanqueando**: alterna entre andar na borda desse anel ao redor do jogador e parar em `idle_combat`, tentando de novo a cada frame até uma vaga liberar. A vaga libera de duas formas: no instante em que o HP zera (`Die()`, não quando o clipe `die` termina de tocar — senão um Melee morrendo continuaria "ocupando" espaço de horda pela duração inteira da animação de morte), **ou quando o próprio dono da vaga cai fora do `flankRadius`** (o player se afasta correndo) — sem isso, quem pegasse vaga primeiro ficaria com ela pra sempre perseguindo o player pelo mundo inteiro, enquanto outros monstros de verdade perto agora não conseguiriam nenhuma; a vaga sempre reflete quem está perto **agora**, não quem chegou primeiro. Ranged não participa disso — já mantém distância própria, não lota o corpo a corpo.

### Idle de patrulha vs. `idle_combat` — duas animações "paradas" distintas ✅
Todo monstro (comum, exceção ou boss) tem **duas animações de parado**, nunca uma só, porque servem a dois momentos diferentes:
- **`idle`** — só toca **antes de detectar o jogador**, durante a movimentação aleatória de patrulha: o monstro alterna entre `walk` (andando aleatoriamente) e `idle` (parado num ponto). **Regra rígida, sem exceção: uma vez que `idle` começou, ela tem que tocar até o fim antes de qualquer transição — em hipótese nenhuma o monstro anda "durante" o clipe de `idle`.** Isso vale mesmo que o jogador seja detectado no meio do clipe: a detecção **não** interrompe o `idle` — o monstro só reage (troca pra `walk`/`idle_combat` de combate) depois que a animação de patrulha termina por completo, criando um pequeno atraso de reação intencional. Na prática, toda transição que sai de `Idle` no Animator usa Exit Time (perto de 1.0), nunca sai no meio do clipe, inclusive a transição pra combate.
- **`idle_combat`** — toca **depois que o jogador foi detectado**, sempre que o monstro está parado em combate: colado no jogador (Melee, entre um contato e outro do cooldown), segurando distância em alcance (Ranged, entre um disparo e outro), ou esperando o cooldown liberar o próximo ataque real (exceções/Bosses com animação `attack` de verdade). Sem essa animação, o monstro pareceria "andar parado no lugar" enquanto solta magia, flecha, ou fica grudado no jogador — o que nunca deve acontecer. É uma animação simples, sem Animation Event, sem duração fixa pra tocar até o fim (fica em loop enquanto o monstro estiver parado em combate) — geralmente um blend tree de só 4 direções (não precisa da mesma fidelidade direcional do `walk`).
- As duas coexistem com `walk`: fora de combate, o monstro está em `walk` ou `idle`; em combate, está em `walk` (perseguindo/reposicionando) ou `idle_combat` (parado). Nunca usa `idle` de patrulha depois de detectar o jogador, e nunca usa `idle_combat` antes de detectar — **exceto a variante "emboscada" abaixo**, que é a única do jogo em que se volta pro `idle` de patrulha depois de já ter detectado o jogador.

### Variante de idle "emboscada" — Gargoyle, Skeletons do Threat Tier 5 (Sprint 16) ✅
Alguns monstros (Gargoyle, Skeleton, Headless Skeleton, Skeletal Horse, Skeleton Mage, Skeleton Minotaur, Skeleton Rider e Skeleton Warrior — todos Threat Tier 5, ver Bestiário) não vagam sozinhos: ficam parados numa única pose até detectar o jogador, e não têm arte de `idle` direcional (NE/NW/SE/SW) — só 1 sprite estático, sem direção. Diferenças em relação ao `idle` de patrulha padrão:
- **`idle` não é mais um Blend Tree direcional** — é um único clipe de 1 frame (o próprio 1º frame do `activate`, sem arte própria). Usa um Animator base à parte (`Base_Melee_Ambush`/`Base_Ranged_Ambush`), incompatível com o `Base_Melee`/`Base_Ranged` comum (cujo `Idle` é Blend Tree).
- Ao detectar o jogador, toca **`activate`** — um clipe não-direcional próprio, 1 vez, antes de entrar em `walk`/`idle_combat` normalmente (via Exit Time, mesmo mecanismo do `Idle -> Walk`/`Idle -> IdleCombat` comum).
- **É a única exceção do jogo em que a detecção pode reverter.** Em todo o resto do Bestiário, uma vez detectado o jogador o monstro persegue para sempre (nunca existe transição de volta a `idle`/patrulha). Nas emboscadas, se o jogador sair do raio de observação, o monstro volta pro `idle` estático **instantaneamente, sem nenhuma animação de transição** — o jogador não estaria nem olhando pra ele nesse instante, então não existe (nem faz sentido existir) um clipe de "voltar a dormir". Na prática, as transições `Walk -> Idle` e `IdleCombat -> Idle` desses Animators não têm Exit Time nem duração — são um snap direto, condicionadas só a `InCombat == false`.
- Fora dessa diferença de ativação/desativação, ataque, dano e morte seguem 100% o padrão Melee/Ranged comum (inclusive o Skeleton Mage, que além disso tem uma exceção própria de ataque — ver abaixo).

### Atributos de monstro comum ✅
Vida, Vida Máxima, velocidade de ataque (= cooldown de contato/disparo), velocidade de movimento, raio de observação, raio de ataque. **Sem Armadura** (Seção 11).

### Reação a dano — regra restaurada (Sprint 16) ✅
Três coisas independentes, nunca uma só: **receber dano ≠ reagir visualmente ≠ interromper uma ação.** Comprometido com a animação `attack` de verdade — o padrão agora, não só as exceções — o dano nunca cancela ela: só um flash leve, sem trocar de estado no Animator (não existe transição `Attack -> Damage`); a animação de ataque sempre termina de tocar por completo. Fora do ataque (patrulha, perseguindo, ou parado em `idle_combat` entre um golpe/disparo e outro), dano toca a animação `damage` normalmente. *(O Orc Shaman continua sendo um caso à parte: quem tem arquitetura própria por Animation Event é o totem, um prefab separado — o Shaman em si segue essa mesma regra padrão.)*

### Morte — destruição só ao fim da animação ✅
`die` é a única animação que todo monstro do jogo mantém, comum ou boss/exceção. A morte segue o mesmo princípio das outras ações reais: **a animação é a fonte de verdade do timing, não um timer independente.** Ao morrer, o monstro dispara `DieTrigger` e só isso — ele continua existindo em cena, tocando o clipe `die` do início ao fim. A destruição de verdade do GameObject, junto com o drop de loot, só acontece por um **Animation Event no último frame do clipe** (mesmo padrão de `AnimationHitEvent`/`AnimationAttackEndEvent`), garantindo que a animação de morte sempre seja vista por completo antes do monstro sumir e o loot aparecer no lugar dele.

### Exceções — monstros com arquitetura própria, orientada por Animation Event ✅
Alguns monstros quebram a regra padrão acima porque têm uma mecânica genuinamente distinta (um objeto próprio no mundo, uma janela de vulnerabilidade, etc.). Para esses, o momento exato em que algo acontece (dano, cura, stun, explosão) é decidido por um **Animation Event dentro do próprio clipe** — a animação é a fonte de verdade do timing, não um timer independente. Documentados individualmente no Bestiário (Seção 51): Goblin Sapper (bomba), Orc Shaman (o totem que ele planta — prefab separado, o Shaman em si usa animações padrão), Burning Skull (explosão suicida), Serpent (janela de exposição), e o projétil do Bicephalous (vira o próprio monstro). Bosses (abaixo) também usam esse modelo, por terem mais de um ataque.

**Skeleton Mage / Zombie Mage (Threat Tier 5) — área de conjuração no chão, não projétil.** Remudança registrada aqui: chegaram a ter dano só por contato direto do projétil (revisão anterior desta mesma sprint); voltou a ser uma área no chão. Ao entrar em alcance, toca uma animação de conjuração (`cast`, no lugar do `attack`/projétil comum) com um **Animation Event** que instancia um prefab de área na posição atual do jogador naquele instante exato — não é mirado, e não segue o jogador depois de nascer. Esse prefab (`GroundTargetHazard`) tem sua própria animação de aviso e seu próprio **Animation Event**: só causa dano se o jogador ainda estiver dentro do trigger dele naquele frame — dando uma janela real pra fugir do círculo antes do estouro, diferente do golpe/disparo comum (que não tem telegraph). Implementado como `GroundCasterEnemyController` (herda de `RangedEnemyController`, reaproveita 100% do Move()/InAttackRange() — mantém distância normalmente —, só troca o que acontece no `ExecuteAttackHit()`).

### Riders / geração de unidades ao morrer ✅
Alguns monstros, ao morrer, geram outras unidades (ex.: Orc Rider gera 1 Warg + 1 Orc Blade). Unidades geradas podem dropar loot próprio. **Nem todo monstro com nome "Rider" usa isso** — o Skeleton Rider deixou de ter essa mecânica (Sprint 16) e hoje é um Melee comum.

### Decisão de arquitetura — resolvida na Sprint 16, revisada na correção ✅
Havia uma contingência aberta aqui (🟡, "simplificação de monstros comuns") cobrindo a possibilidade de o Bestiário completo (timing/telegraph + animação real por criatura) se provar inviável em escopo solo. **Isso foi testado na prática** (3 monstros reais — Rat, Goblin, Rat People — com Animator de verdade, Sprint 16) e a contingência **foi acionada**: o Bestiário passou, por padrão, a usar dano de contato/auto-disparo sem nenhuma animação de ataque. O Attack Budget (Seção 14) foi removido como consequência direta.

**Essa primeira versão simplificada não durou.** Depois de testar o modelo puro de contato/auto-disparo na prática (Sprint 16, correção), a decisão final foi um meio-termo: cada monstro comum mantém a animação `attack` real — reaproveitando a arte que já existia pronta — mas sem o custo do telegraph completo (posição travada, janela de esquiva por reposicionamento). O golpe conecta ou erra checando um trigger simples no instante do Animation Event, não um cálculo de "o player se afastou o suficiente". O Attack Budget continua removido — não voltou junto com o `attack`.

### Bosses ✅
- A maioria dos 30 bosses (definida ficha a ficha no Bestiário) segue a mesma regra híbrida dos monstros comuns: `attack` real com Animation Event (golpe/disparo) e sem dano de contato passivo — perdendo só a complexidade extra que tinham antes (telegraph, dano em área, múltiplos hits), não a animação de ataque em si.
- Um grupo pequeno e nomeado mantém arquitetura própria por Animation Event além do padrão, porque a mecânica não existe sem ela (ver Bestiário): Mother Slime Green/Blue (só contato, igual aos Slimes comuns, mais o spawn de filhotes em posição fixa), Rat People Royalty, Spider Queen (só a teia, sem animação), Dark Channeler, Lich, Dragon, Undead Dragon e Divine God — este último somando a arquitetura completa a uma camada extra de dano por contato (híbrido Melee/Ranged, única exceção que ainda tem contato).
- Bosses com mais de um ataque real (os da lista acima) usam cooldowns próprios por ataque — diferente de monstro comum, que tem só 1 tipo de ataque.
- Matar um boss de topo (ex.: Divine God) **não encerra a run** — é conquista, não condição de vitória.

### Boss Timer — revisão estrutural (mundo único, ver `docs/new/`) ✅
**Substitui por completo a regra "Individual por Floor" desta seção (histórico abaixo).** Sem Floors, não existe mais "qual Floor acumulou quanto tempo" — o Boss Timer passa a ser **único e global pra run inteira**, e o que decide **qual Tier de boss nasce** passa a ser a posição atual do jogador no mundo, não mais um elenco fixo por andar.

- **Boss Timer é único, estado Daily, intervalo fixo que se repete indefinidamente** (🔢 referência: 20–30s, igual ao antigo 10s mas pendente de novo playtest — a cadência muda porque agora qualquer posição do mundo pode gerar boss, não só Floors com elenco produzido). Pausa **não avança** (Seção 9). Morte de boss **não reseta**. **Reseta apenas quando o dia termina.**
- **Boss Timer decide QUANDO; Tier Predominance decide QUAL.** No instante em que o timer completa, o jogo recalcula a mesma distribuição final de Tiers usada pelos monstros comuns (Seção 23 — `EffectiveSpawnDay`/`CurrentPhase`/Supressão de Ameaça, na posição atual do jogador) e usa o **Tier com maior peso** como prioridade de boss. A antiga tabela própria de bosses (BT1–BT10) é removida — não existem duas curvas de balanceamento paralelas.
- **Nenhum boss específico repete no mesmo `ActualDay`.** Dentro do Tier prioritário, os bosses daquele Tier funcionam como uma bag sem repetição durante o dia — uma vez usado, aquele `BossDefinition` fica indisponível até o próximo `ActualDay` (zera com ele). Se todos os bosses do Tier prioritário já apareceram hoje, o sistema cai pro próximo Tier por ordem de predominância atual, e assim por diante.
- **Mudar de Tier predominante (dia/noite, ou o jogador se mover pra uma região de `EffectiveSpawnDay` diferente) não troca bosses já vivos** — só afeta o próximo Boss Event.
- **Bosses empilham:** se o boss anterior ainda estiver vivo quando o timer completar de novo, nasce um segundo — os dois ficam vivos ao mesmo tempo. Não existe fila nem espera pelo boss anterior morrer.
- **Local de nascimento:** mesma regra de posição de monstro comum (Seção 23) — num anel ao redor do jogador, fora da Zona Segura, preferencialmente fora da câmera. Bosses nunca nascem na Zona Segura, mas podem entrar nela, perseguir e atacar normalmente depois de nascer (mesma regra de Spawn vs. Navegação da Seção 23/24).
- **Despawn por distância, com reposição imediata:** se o boss ficar longe demais do jogador (🔢 `BossDespawnDistance`, maior que o de monstro comum pra não desaparecer no meio de uma luta normal — Seção 23), ele é removido (não é morte — sem loot, sem contar como derrotado, sem liberar o `BossDefinition` do dia) e **sua vaga é reposta imediatamente**, recalculando o Tier pela posição atual do jogador — isso não reseta nem adianta o Boss Timer normal, é reposição de vaga, não um Boss Event novo. Boss **derrotado de verdade** não gera reposição automática — a próxima entrada depende só do Boss Timer normal. Proteção contra abuso (correr alguns passos pra forçar troca): `BossDespawnDistance` generoso + histerese/cooldown de reposição, mesmo critério de qualquer sistema de distância desta revisão.
- **Agressão imediata:** diferente de monstro comum (que só entra em combate ao detectar o jogador por `observationRadius`), todo boss nasce já com o jogador como alvo e vai direto até ele, mesmo nascendo longe — sem fase de patrulha/idle.
- **Agressão imediata também vale na volta da Camuflagem (decisão do usuário, preservada):** enquanto o jogador está camuflado (Ranger, Seção 17/`IsPlayerUntargetable`), o boss perde o alvo igual a um monstro comum — mas no instante exato em que a camuflagem termina, ele recupera o alvo automaticamente, sem precisar que o jogador volte a entrar no `observationRadius`. Monstro comum continua exigindo essa redetecção normalmente; só o boss tem esse atalho.

**Histórico (pré-revisão, preservado só como registro):** antes desta revisão, o Boss Timer era individual por Floor, acumulava só o tempo que o jogador passava em cada andar, e o Tier de boss vinha de uma tabela própria BT1–BT10 fixa por andar — esse modelo deixou de existir junto com o Floor System (Seção 24).

---

## 23. Sistema de População (Spawn/Respawn)

**Revisão estrutural completa (mundo único — ver `docs/new/`).** Regra principal, preservada: **o mundo ao redor do jogador nunca deve parecer vazio.** ✅ Tudo abaixo substitui o modelo antigo "três valores por andar" por um sistema orientado à posição do jogador no mundo único, à distância do centro e ao `ActualDay`/`CurrentPhase` (Seção 40).

### Population System — o único controle de quantos monstros existem ✅
Population System determina **quantos monstros existem** ao redor do jogador. Desde a remoção do Attack Budget (Seção 14, Sprint 16), não existe mais um sistema limitando quantos monstros comuns podem *atacar* simultaneamente — cada um respeita só o próprio cooldown individual. Existe, separadamente, um limitador de um eixo diferente: quantos **Melee** podem ficar em **contato simultâneo** com o jogador (Seção 22, "Lotação perto do jogador — flanco", `MeleeAttackSlotManager`, agora um pool único de mundo, não mais por Floor) — não é sobre atacar, é sobre lotação visual de horda; quem não cabe flanqueia em vez de amontoar. Ranged não é afetado por isso.

### Zona Segura (Safe Zone) — proibição de nascimento, não de invulnerabilidade ✅
Ao redor do centro do mapa (0,0,0 — Seção 24) existe uma região circular, raio configurável (`SafeZoneRadius`), onde **nenhum monstro comum e nenhum boss pode nascer**. Essa é a única exceção espacial absoluta do sistema: dentro da Zona Segura, chance de spawn = 0, independentemente de dia, noite, Tier ou boss.

- **Não bloqueia movimentação nem pathfinding.** Um monstro que nasceu fora da Zona Segura e detectou o jogador pode entrar nela, perseguir, atacar e continuar usando A* normalmente — a restrição é só sobre **onde um spawn pode nascer**, nunca sobre onde um monstro já ativo pode ir. Conceitualmente: `Walkable ≠ Valid Spawn Position`. A Zona Segura **não deve** tornar nós do A* não-caminháveis.
- **Não significa invulnerabilidade.** Ela não limpa alvo/aggro/projéteis/bosses, não mata nem remove monstro nenhum. Se o jogador correr pra dentro dela sendo perseguido, a perseguição continua.
- **`SpawnSafetyPadding` (🔢 opcional):** evita monstro nascendo visualmente colado na borda. `MinimumSpawnDistanceFromCenter = SafeZoneRadius + SpawnSafetyPadding`.
- **Vale igualmente para bosses** (Seção 22) — nascem fora dela, podem entrar livremente depois.
- Implementação eficiente é decisão técnica (cache de nós válidos/Spawnable separado do Walkable, invalidação só quando necessário — centro e raio são estáticos durante a run) — não fixada aqui.

### EffectiveSpawnDay — a distância escolhe qual dia da tabela é usado, não um bônus de Tier ✅
A dificuldade dos próximos spawns (e respawns/replacements) não soma mais Tiers diretamente por distância (antiga lógica probabilística de +1/+2 Tier, removida). Em vez disso, a distância escolhe **qual linha da tabela de 30 dias** (abaixo) é consultada:

```text
DistanceBeyondSafeZone = max(0, DistanceFromCenter - SafeZoneRadius - SpawnSafetyPadding)
DistanceDayOffset = floor(DistanceBeyondSafeZone / DistancePerSpawnDay)
EffectiveSpawnDay = clamp(ActualDay + DistanceDayOffset, 1, 30)
```

- `DistancePerSpawnDay` (🔢) é configurável — quantas unidades de mundo o jogador precisa se afastar pra que os próximos spawns passem a usar a tabela do dia seguinte. A distância é contada a partir da **borda efetiva** da Zona Segura (já descontando o Padding), usando a posição **atual do jogador**, nunca a posição sorteada pro monstro.
- `ActualDay` é o dia real da run (o mesmo "Dia N" de sempre — Seção 40); `EffectiveSpawnDay` é só o dia **consultado pela tabela de spawn** naquela posição — não avança Demanda, Loja, Save, Vitória do Dia 15, nem nenhuma outra progressão diária.
- **Mapa finito ⇒ teto natural, sem Hard Lock de Tier.** O `DistanceDayOffset` máximo emerge da maior distância caminhável entre a Zona Segura e os limites do mapa (Seção 25) — não existe um "if" proibindo Tier 10 cedo; ele simplesmente não tem peso nas linhas de dia baixo da tabela (abaixo). Se o tamanho real do mapa permitir alcançar uma linha alta demais cedo, o ajuste correto é mudar `DistancePerSpawnDay`/o tamanho do mapa, nunca criar uma trava especial de Tier.
- **Monstros já existentes não são transformados retroativamente** — mudar de faixa só afeta novos spawns, respawns e replacements a partir daquele momento.
- **Fim do mapa / distância além do que qualquer `EffectiveSpawnDay` (até 30) consultaria:** se o jogador se afastar além do ponto onde a tabela já está no Dia 30, não existe Dia 31 pra consultar — nesse caso, em vez de extrapolar a tabela, a % de **Threat Tier 10** continua subindo diretamente com a distância (🔢 curva própria, pendente de playtest), redistribuindo os Tiers restantes pela mesma normalização da Supressão de Ameaça (Seção 27). Decisão do designer.

### Tabela Base de Spawn — Dia × Período × Tier ✅
Tabela de 30 dias × 2 períodos (Dia/Noite — Seção 40) × 10 Threat Tiers, cada linha somando exatamente 100%, é a fonte de verdade da composição. Vive em um documento especializado (Seção 51 — `docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md` traz a proposta inicial completa de todas as 60 linhas; migra pra `docs/gdd/balance-values.md` ou doc próprio quando formalizado). Nunca hardcoded — cada combinação Dia+Período é um registro de dados editável (`DaySpawnDefinition`), validado automaticamente (soma = 100%). Não existe `Dia X = Tier X`: cada Tier tem janela de introdução, crescimento, domínio e permanência residual (ex.: Tier 10 tem 0% nas linhas 1–19, ganha peso a partir da linha do Dia 20, mas é alcançável antes do `ActualDay` 20 se o `EffectiveSpawnDay` da posição do jogador já chegar lá).

### Threat Tier contém vários monstros — sorteio em 2 etapas ✅
O sorteio nunca escolhe 1 monstro entre todos os monstros do jogo direto. Primeiro `Roll Tier` (pela tabela acima, já suprimida/normalizada — Seção 27), depois `Roll Monster Definition` dentro daquele Tier (pesos individuais por monstro dentro do Tier são upgrade futuro, não obrigatório fechar agora).

### Onde monstros nascem ✅
Num anel (`Spawn Annulus`) ao redor do jogador — nunca "no mapa inteiro" — fora da Zona Segura, preferencialmente fora da câmera, distância mínima/máxima do jogador configuráveis, nunca em obstáculo/estrutura/interação. Posições válidas vêm da geração procedural do mundo (Seção 25), não mais de uma Floor Variant artesanal.

### Reação a jogador matando rápido ✅
Aumenta a **frequência de reposição** (até o teto do Maximum), não Vida/Dano dinamicamente. Na primeira implementação, a distância do centro muda a **composição** (via `EffectiveSpawnDay`), mas não aumenta automaticamente Minimum/Target/Maximum nem a cadência de spawn — população e distância são variáveis separadas (revisável depois de playtest).

### Distance Despawn e Replacement — a população acompanha a exploração ✅
Monstro muito distante do jogador (🔢 `EnemyDespawnDistance`, maior que a área visível, pra não desaparecer perceptivelmente em combate normal) é removido da simulação — **isso não é morte**: sem loot, gold, kill, Energia da Ultimate, ou qualquer efeito de morte (`Enemy Death ≠ Distance Despawn`). Cada remoção por distância abre 1 vaga de população, que é reposta imediatamente dentro de um `Spawn Annulus` ao redor da posição **atual** do jogador, sorteada pela composição do `EffectiveSpawnDay`/`CurrentPhase` daquele momento — não precisa ser do mesmo Tier nem da mesma definição do monstro removido. Se o jogador estiver dentro da Zona Segura, a reposição procura posição válida fora dela no próprio anel; se não encontrar nenhuma, o replacement fica pendente (nunca em loop infinito). Pooling/histerese recomendados pra não gerar Instantiate/Destroy excessivo em sequência.

### Interações ✅
- **Boss:** timer independente da população comum, mas agora também usa a Tier Predominance calculada na posição do jogador (Seção 22).
- **Employees:** não interferem na população.

### Population State persiste — simulação completa não é obrigatória ✅
O **estado** da população (quantos monstros existem, crise/escalação se existir) precisa persistir enquanto o jogador está em outra região do mundo, mas isso não exige que toda a simulação (pathfinding, Animator, targeting, AI Update, ataques, colisões, movimentação) continue rodando em tempo real fora da vizinhança do jogador — pode ser suspensa, virtualizada, atualizada logicamente, ou reconstruída (decisão técnica, Seção 51; "Simulation Rings" — próximo ao jogador simula completo, longe simplifica, muito longe vira Distance Despawn/Replacement acima). A regra fixa de gameplay: **afastar-se e voltar não pode parecer um reset artificial/explorável da população.**

### Pipeline de Spawn — ordem formal ✅
```text
Player Position → ActualDay → CurrentPhase (Seção 40) → Distance From Center →
DistanceDayOffset → EffectiveSpawnDay → Base Spawn Weights (tabela acima) →
Supressão de Ameaça (Seção 27) → Normalize → Roll Tier → Roll Monster Definition →
Spawn Annulus ao redor do Player → Safe Zone Check → Câmera/Distância/A*/Obstáculo →
Spawn
```
A validação espacial (Zona Segura, câmera, A*, obstáculo) é sempre a etapa final, separada da escolha de ameaça (Tier) — as duas responsabilidades nunca se misturam.

🔢 Valores exatos (Minimum/Target/Maximum, `DistancePerSpawnDay`, `EnemyDespawnDistance`, raios do Spawn Annulus) pendentes de playtest.

---

## 24. Mundo Único — Estrutura Técnica e Zona Segura

**Revisão estrutural completa — substitui por inteiro o antigo "Floor System" (ver `docs/new/` pra origem completa desta mudança).** O jogo deixa de ter 10 andares separados. Existe **um único mundo contínuo, grande e finito**, explorável durante cada dia.

### Uma única Unity Scene de gameplay ✅
O mundo inteiro existe dentro de **uma única Scene de gameplay**. Não existe carregamento de outra Scene durante a exploração normal — o jogador anda livremente, sem transição/teleporte entre regiões (exceção: Controle Remoto, Seção 26, e respawn por morte, Seção 11 — os 2 únicos teleportes discretos que sobrevivem, ambos levando ao centro).

### Centro fixo, mundo finito e aproximadamente circular ✅
O mundo tem centro fixo em **(0, 0, 0)** e limites definidos/configuráveis (`WorldRadius`/`WorldMask`). É grande, mas finito — a quantidade de faixas radiais úteis pra progressão (Seção 23) depende do raio real caminhável, medido em protótipo, não fixado a priori. Limites visualmente justificáveis (montanhas, abismos, oceano, ruínas), nunca uma borda artificial abrupta.

### Zona Segura (Safe Zone) ✅
Ver regra completa na Seção 23 — é especificamente sobre proibição de **spawn**, não sobre o mapa em si. Fica registrado aqui só que ela é uma região fixa centrada em (0,0,0), parte da estrutura do mundo.

### Vendor — estrutura permanente e imutável ✅
Existe um prefab de Vendor, fixo e **imutável independente de qualquer evolução/regeneração do terreno** (Seção 25) — a loja/vendedor (Seção 39) sempre existe, sempre acessível, nunca é substituída por lava, obstáculo ou qualquer transformação ambiental. Vive dentro ou ao lado da Zona Segura.

### Identidade visual evolui por dia, não mais por Floor ✅
O ambiente ao redor do jogador muda com a passagem dos dias (`ActualDay`) e com a distância do centro, usando geração procedural orientada a dados (Seção 25) — substitui a antiga "Global Light própria por Floor". Dia 1 e Dia 2 podem ser parecidos; Dia 1 e Dia 10 devem parecer etapas nitidamente diferentes do mesmo lugar físico.

### Persistência do mundo ✅
Loot deixado no mundo **não desaparece** com a passagem do tempo dentro do mesmo dia — permanece no mesmo lugar até ser coletado, vendido pelo Coletor, ou destruído ao fim do dia junto com o resto do estado Daily (Seção 15). Edições do jogador sobre o terreno gerado (ex.: um baú já aberto) são salvas como delta sobre a geração base — ver Seção 25.

### Simulação seletiva por distância, não mais por Floor ✅
O que antes era "Floor Sleep" (Floor fora da região ativa suspende sistemas caros) passa a ser **Simulation Rings por distância do jogador**: perto simula completo, mais longe simplifica, muito longe vira Distance Despawn/Replacement (Seção 23). O estado (população, loot, Boss Timer) continua existindo mesmo fora do raio de simulação completa — só a simulação em si (pathfinding, Animator, AI Update) pode ser suspensa/virtualizada. Implementação exata é decisão técnica (Seção 51).

### Coletores — sem mais "fora do Floor atual", agora é só alcance ✅
Coletores (Seção 34/36) continuam podendo localizar/coletar loot espalhado pelo mundo sem exigir que todos os Employees sejam simulados individualmente em tempo real — a antiga formulação "cross-Floor" deixa de fazer sentido (não existe mais "Floor do jogador" vs. "outro Floor"), mas o resultado esperado é o mesmo: loot longe do jogador continua coletável pelo Coletor sem simulação completa.

### Combat Scope — removido, não tem mais o que restringir ✅
**O antigo "Combate opera no Floor atual" deixa de existir como regra.** Não existe mais "Floor do jogador" vs. "Floor de outro monstro" pra restringir — é tudo o mesmo mundo contínuo. "Inimigo mais próximo" (Homing do Cleric, vinhas do Druid), "todos os monstros em campo" (ultimate do Cleric) e qualquer projétil/área/pet/summon/Ajudante do kit de herói continuam usando os próprios raios de busca já definidos em cada habilidade (Seção 13/17/33/34) — sem precisar de uma restrição adicional por Floor, porque a separação espacial que o Combat Scope resolvia (times por Floor literalmente empilhados em posições diferentes da Scene) não existe mais nesse formato.

**Achado da auditoria de código desta revisão, registrado aqui por transparência:** mesmo antes desta mudança, o Combat Scope nunca foi implementado como um filtro real de identidade de Floor no caminho de dano (`TakeDamage` nunca checou Floor) — a separação sempre foi **incidental**, decorrente de Floors ficarem fisicamente distantes na mesma Scene mais o Floor Sleep desligando a IA de quem estava "fora". Ou seja: a regra comportamental sempre foi garantida por geometria + simulação seletiva, nunca por uma regra de negócio explícita — e é exatamente esse padrão (geometria + simulação seletiva por distância) que continua valendo no mundo único, só que agora de forma intencional e documentada, não mais incidental.

**Pendência nova, registrada em vez de decidida aqui (ver Seção 53):** efeitos de combate persistentes do próprio herói (rastro de fogo do Mage, facas do Ranger) que o jogador deixa atrás ao se afastar — eles seguem a mesma regra de Simulation Rings dos monstros (podem ser pausados/recolhidos por distância), ou continuam ativos indefinidamente até a duração acabar, independente da distância do jogador? Não decidir agora — comportamento análogo ao que já era pendente antes desta revisão (Combat System Document, Seção 51).

---

## 25. Geração Procedural do Mundo

**Substitui por inteiro o antigo "Floor Variants" (50 mapas artesanais, 5 por andar).** Motivo da mudança: 50 mapas manuais é inviável pra produção solo — a solução não é reduzir o escopo, é eliminar a necessidade de desenhar manualmente, gerando o mundo por dados a partir de assets (sprites/autotiles/prefabs) já existentes, de forma determinística e orientada a seed. Fonte completa: `docs/new/Mudanca_Estrutural_Geracao_Procedural_Evolucao_Mapa_30_Dias_V2_ILUSTRADO.md`.

### Princípio ✅
*"Eu reconheço que estive aqui, mas o mundo não é mais o mesmo."* O mundo é gerado por `WorldSeed` + posição global + `ActualDay` — determinístico (mesma seed, posição, dia e versão das regras sempre geram o mesmo resultado antes dos deltas salvos). Orientado a dados (ScriptableObjects cadastrando Tiles/RuleTiles/AnimatedTiles/prefabs já prontos no projeto), nunca geração de sprites do zero.

### Geografia estável vs. materialização ambiental ✅
Cada posição do mundo tem uma identidade geográfica de referência (contorno de lago, corredor de passagem — preferencialmente estável por seed) separada do **material/estado** que a representa visualmente num dado dia (ex.: um lago pode virar lava sem perder a própria identidade/forma). Rios e estradas são uma exceção explícita — podem mudar de **traçado**, não só de material, entre dias (decisão do designer).

### Evolução por dia, não por Floor ✅
Ao avançar `ActualDay`, a MESMA região física pode receber substituição de terreno/ambiente (grama viva → solo escuro, água → lava). Não é incremental/cumulativo — o visual de um dia é calculado a partir da seed + posição + dia atual, nunca de 30 operações acumuladas (o Dia 10 fica certo mesmo que os Dias 2–9 nunca tenham sido carregados ali). Dia 1 e Dia 2 podem ser parecidos; Dia 1 e Dia 10 devem parecer etapas nitidamente diferentes do mesmo lugar.

### Água e lava — sempre bloqueio físico (decisão do designer, simplificação confirmada) ✅
Diferente da proposta original (que previa água/lava com comportamento configurável por tipo — bloqueio, dano, atravessável), a decisão final é mais simples: **água e lava sempre bloqueiam a passagem, como paredes.** Não existe variante de água/lava atravessável ou que só cause dano sem bloquear.

### Estruturas permanentes e imunes à evolução ✅
O Vendor (Seção 24) é imutável independente de qualquer regeneração de terreno. Baús (Seção 30) são gerados **depois** do mundo gerado, nas posições válidas resultantes — não fazem parte da geração de terreno em si.

### Sem mineração nem destruição de ambiente ✅
Árvores, rochas e demais props ambientais são **decoração pura** — não existe coleta de recursos, destruição ou regeneração de ambiente no MVP. As únicas interações persistentes sobre o mundo gerado são baús já abertos e o estado do Vendor — nada além disso precisa de delta salvo por objeto ambiental.

### Chunks, biomas e detalhes técnicos — documento especializado ✅
Geração por chunks determinísticos (streaming conforme o jogador se move), autotiles/RuleTiles pra manchas conectadas (grama alta, água, etc.), perfis de bioma por família visual (T1–T10, sem zona fixa de 1 Tier por região — Seção 23 continua controlando a distribuição de monstros, independente do visual do terreno), e a arquitetura de ScriptableObjects/pipeline completo pertencem a um documento especializado próprio (Seção 51), não ao GDD Mestre — aqui ficam só as regras estruturais de comportamento acima.

### Seed ✅
Fixa por run, mas pode ser compartilhada/reutilizada manualmente entre runs — principalmente pra debug. Um sistema futuro de "comparar mundos com amigos" usando a mesma seed é só uma ideia registrada, fora de escopo agora.

### Save ✅
`WorldSeed` + versão do gerador + `ActualDay` + deltas de edição do jogador (baús abertos) são o suficiente pra reconstruir o mundo exatamente — não salva o mundo tile a tile.

---

## 26. Controle Remoto

**Revisão estrutural — sem Floors, não existe mais "travessia entre andares"** (escadas, buracos, Active Floor Position e toda a regra de destino relativo desta seção deixam de existir). O único conteúdo que sobrevive é o Controle Remoto, com função nova.

### Prioridade entre interações simultâneas ✅
**Todo interagível (baú, vendedor, NPC, e qualquer outro que venha a existir) sempre tem prioridade sobre largar o Magnet.** A regra que precisava ser fechada era especificamente **Magnet vs. qualquer outro interagível**, e essa está resolvida: o Magnet nunca "rouba" o E de uma interação real — só é largado se nenhum outro interagível estiver no alcance no momento do E. Implementação: `InteractionManager` trata qualquer `Interactable` "de verdade" como prioritário sobre a ação de largar o Magnet.

### Controle Remoto — nova função: retorno ao centro ✅
- **Comprado na aba Bonuses** (Seção 41). **Não é item físico** — não ocupa a Bag, ao contrário de qualquer material de loot (Seção 37).
- Acessado através da tecla **Q** (Seção 46).
- Abrir a interface **pausa completamente o jogo**, seguindo a regra central de pausa (Seção 9).
- Possui **cooldown entre usos**. 🔢 valor exato pendente de balanceamento (referência anterior: 10s).
- **Função (revisão estrutural):** sem Floors pra listar, deixa de ser um seletor de andares — vira um teleporte de retorno: abre um popup, o jogador confirma, e é teleportado direto pro **centro do mundo (0,0,0 — Seção 24)**, de qualquer distância que esteja.

**Fluxo conceitual:**
```text
Q → jogo pausa → abre popup do Controle Remoto →
jogador escolhe voltar pro centro → confirma →
teleporta pro centro (0,0,0) → popup fecha → cooldown começa
```
Layout visual da interface não é definido aqui — pertence ao documento de UI/UX (Seção 51).

---

## 27. Supressão de Ameaça

**Substitui por inteiro o antigo "Remove Tower Layer".** Nome provisório, pode mudar depois sem impacto estrutural (confirmado pelo designer). Mesma regra sequencial de sempre, só troca "remover andar" por "suprimir o Tier mais fraco ainda ativo".

### Regra final confirmada ✅
- Comprado na aba Bonuses. **O jogador não escolhe qual Tier suprimir** — não existe seleção de alvo.
- **Cada compra suprime automaticamente o Threat Tier mais baixo ainda ativo.** Compra 1 suprime Tier 1; compra 2 suprime Tier 2; e assim por diante.
- **Máximo de 5 compras por run** → no limite, suprime sequencialmente os **Threat Tiers 1, 2, 3, 4 e 5**. **Threat Tiers 6–10 nunca podem ser suprimidos.**
- Um Tier suprimido deixa de poder ser sorteado como monstro comum **e** como boss (Seção 22) — se o Tier predominante numa posição estiver suprimido, o sistema de boss procura a maior predominância entre os Tiers ainda disponíveis.

### Redistribuição — normalização, nunca tabela especial ✅
A porcentagem do Tier suprimido **não desaparece** — é redistribuída proporcionalmente entre os Tiers restantes daquela linha (Seção 23), sempre por normalização matemática, nunca por uma tabela escrita à mão pra cada combinação possível de supressão.

**Exemplo:** linha com T1=5%, T2=15%, T3=20%, T4=35%, T5=25%. Suprime T2. Soma restante = 5+20+35+25 = 85. Nova distribuição: T1 = 5/85 ≈ 5,88%; T3 = 20/85 ≈ 23,53%; T4 = 35/85 ≈ 41,18%; T5 = 25/85 ≈ 29,41%. Soma = 100%. Essa normalização funciona pra qualquer combinação de Dia, Período (Seção 40) e Tiers suprimidos.

### NPCs ✅
NPCs nunca residem numa área cuja ameaça predominante seja um Tier suprimível (vivem efetivamente perto do centro/Vendor — Seção 24).

---

## 28. Traps

- Adicionam risco ao deslocamento, sem virar puzzle. ✅
- Precisam de telegraph antes do dano.
- **Não são monstros:** não dropam loot, não contam como kill, não carregam Ultimate, não contam para demanda.
- Ameaça ambiental independente — não são afetadas por cooldown de monstro nem por nada do combate comum.
- **Revisão estrutural (mundo único):** fazem parte do layout gerado proceduralmente do mundo único (Seção 25), assim como baús — sem mais escadas (deixaram de existir junto com o Floor System).

### MVP ✅
**Falling Rock** (sombra no chão → pedra cai, dano em área) e **Floor Spikes** (indicação de furos → espinhos surgem, dano em área). Novas traps ficam em Visão Expandida. 🔭

---

## 29. Quests

Todas Run-Persistent — recompensas se perdem ao iniciar nova run. ✅

### Magnet (3 etapas) — revisão estrutural, alcance por distância do centro ✅
**Substitui por inteiro o antigo "até qual Active Floor Position" (Seção 24 não existe mais).** O Magnet passa a ter um **raio máximo de distância do centro** (0,0,0 — Seção 24) dentro do qual ele acompanha/funciona — além desse raio, ele não acompanha o jogador. Os 3 Tiers usam exatamente as mesmas referências de distância já definidas pelo sistema de spawn (Seção 23), amarrando a progressão do Magnet à mesma régua de dificuldade do mundo, não a um número arbitrário:

| Etapa | Entrega | Recompensa — alcance máximo do centro |
|---|---|---|
| 1 | 5 Arcane Shard | Magnet Tier 1 — até a **metade** da área de dificuldade inicial (`SafeZoneRadius + SpawnSafetyPadding + 0,5 × DistancePerSpawnDay` — Seção 23) |
| 2 | 20 Dark Crystal | Magnet Tier 2 — até o **começo** da área onde já aparecem monstros do próximo dia (`SafeZoneRadius + SpawnSafetyPadding + 1 × DistancePerSpawnDay`) |
| 3 | 40 Soul Fragment | Magnet Tier 3 — até o **começo** da área onde aparecem monstros de 2 dias adiante (`SafeZoneRadius + SpawnSafetyPadding + 2 × DistancePerSpawnDay`) |

- Começa **todo dia** no centro (0,0,0 — mesmo ponto de respawn, Seção 11/24).
- **E** pega/larga o Magnet. **Qualquer outro interagível no alcance (baú, vendedor, NPC) sempre tem prioridade sobre largar o Magnet** (regra completa e resolvida na Seção 26) — só larga com E se não houver nenhum outro interagível no alcance no momento.
- **Dentro da área/limite permitido pelo tier atual, o Magnet coleta e vende loot automaticamente** — esta é uma regra estrutural central da recompensa, não implícita.
- **Dois conceitos distintos, não confundir:**
  - **Magnet Range** (os tiers acima) define **até qual distância do centro** o Magnet consegue acompanhar/funcionar — mesmo papel de antes, só a régua de medida mudou de "Floor" pra "distância".
  - **Pickup Radius** (Seção 37) define **quão perto fisicamente do jogador** o loot precisa estar para ser processado. **O Magnet utiliza o Pickup Radius atual do jogador** como sua área de captura ao redor do jogador — ele não possui um segundo sistema independente de raio horizontal próprio. Consequentemente, comprar **Increase Pickup Radius** (Bonuses — Seção 41) também aumenta naturalmente a área efetiva de atuação do Magnet.
- **Comportamento de venda:** sem Magnet, loot que entra no Pickup Radius vai para a Bag normalmente (Seção 37). **Com o Magnet ativo/acompanhando o jogador**, o loot que entra no Pickup Radius é processado pelo Magnet e **vendido automaticamente** — não precisa entrar na Bag primeiro. Fantasia resultante: com o Magnet, atravessar uma pilha de loot dentro do Magnet Range permitido vende tudo que entra no raio, sem gerenciar a Bag.
- **O Magnet só processa loot vendável** — o mesmo conjunto de materiais econômicos que o Pickup Radius já reconhece como "loot válido" (Seção 37). Ele **não** pode vender pergaminhos, outro Magnet, baús, quest objects não destinados à venda, ou qualquer elemento estrutural do mapa — esses continuam usando suas próprias regras de interação, sem interferência do Magnet.
- **No Modo Padrão, Monster Essence vendida pelo Magnet conta normalmente para a demanda do dia** (Seção 39), e o Gold correspondente é adicionado normalmente, do mesmo jeito que uma venda pelo NPC vendedor ou pelo Coletor Employee — não existe categoria especial de venda para o Magnet. **No Modo Free**, a mesma venda de Monster Essence pelo Magnet ocorre normalmente e o Gold é recebido normalmente, mas não existe quota para incrementar (Seção 42).
- **Vendas do Magnet aparecem normalmente na Tela de Resultados** (Seção 40), que resume todas as vendas do dia independentemente da origem (NPC, Magnet, Coletor) — a estrutura da Tela de Resultados não muda.
- **O Magnet não aumenta slots, stack ou capacidade da Bag** — ele desvia o fluxo do loot capturado para venda automática, evitando o gargalo da Bag enquanto estiver em funcionamento dentro de suas regras.
- 🟡 **Pendência:** o Magnet utiliza o Filtro de Bag do jogador (Seção 37) para decidir quais tipos de loot vender automaticamente, ignora esse filtro, ou terá uma regra própria de filtro? Não definido — não existe um "Magnet Filter" distinto do Filtro de Bag e do Filtro de Coletor já documentados, e não presumir qual dos dois filtros existentes (se algum) ele deveria seguir.

### Chest Pointer ✅
Entregar 100 Spirit Dust → seta visual que aponta para baús próximos, atualizando direção conforme o jogador se move. Não tem vida, não ataca, não coleta, não interage com monstros.

### Chaos Crystal ✅
Entregar 1 Chaos Crystal → **Royal Contract** (+100% valor de venda pelo resto da run). 🟡 Alternativa em avaliação: Epic Card Selection (3 cartas mais fortes que baú comum, escolhe 1) — decisão em playtest.

---

## 30. Baús

### Spawn ✅
**Revisão estrutural (mundo único — ver `docs/new/`):** gerados aleatoriamente dentro das posições válidas do mundo único, calculadas **depois** da geração procedural do terreno (Seção 25) — substitui por completo a antiga geração dentro de cada Floor Variant artesanal. Quantidade por região, distância mínima entre baús e eventual aumento de frequência conforme a distância do centro não estão definidos — configuráveis por playtest/balanceamento.

### Chest Mimic — ciclo de disfarce (Sprint 16) ✅
- Uma pequena % (🔢 configurável) de baús em qualquer ponto do mundo pode ser um **Mimic**.
- **Desativado:** enquanto disfarçado, é um baú comum igual a qualquer outro — interagível com E, sem perseguir nem atacar.
- **Ativação:** ao pressionar E pra abrir, toca a animação `activation` (em vez de liberar a recompensa direto). Só depois que ela termina por completo é que o Mimic se revela e passa a se comportar como um Melee comum — persegue o jogador e causa dano por contato normal, com cooldown próprio (GDD Seção 22, mesma regra de qualquer Melee comum).
- **Desativação:** se o jogador escapar do raio de observação dele, o Mimic toca `desactivation` e volta ao estado de baú disfarçado, interagível com E de novo — o ciclo pode se repetir várias vezes até o jogador efetivamente derrotá-lo.
- **Único monstro do jogo com Dano/Vida por porcentagem em vez de tabela fixa por Tier:** em vez de valores travados ficha a ficha (como todo o resto do Bestiário), o Chest Mimic calcula Dano/Vida como uma fórmula percentual sobre uma base, crescendo conforme o `EffectiveSpawnDay` (Seção 23) no instante/posição em que nasceu. 🔢 fórmula exata (base e % por nível) pendente de balanceamento — ver Bestiário (Seção 51) pra ficha completa.
- **Ao morrer, o Mimic libera a mesma recompensa que o baú normal teria fornecido e abre a mesma UI de 3 opções** (Seção 30) — o jogador **não perde a recompensa** apenas por ter encontrado um Mimic; ele só precisa vencer o combate primeiro para recebê-la.

### Interação — abertura com E, escolha com mouse ✅
O jogador se aproxima do baú e pressiona **E** para abri-lo. Isso vale tanto para baú normal quanto para Mimic:
```text
Player se aproxima do baú → pressiona E → baú é aberto
```
- **Baú normal:** ao abrir com E, a recompensa é liberada e a UI de 3 pergaminhos/cartas aparece diretamente — **não existe uma segunda interação de "pegar o pergaminho do chão"**; o pergaminho não é um objeto separado que exige outra tecla ou é coletado pelo Pickup Radius (Seção 37).
- **Mimic:** ao pressionar E, toca `activation` e, ao final dela, se revela e passa a perseguir/causar dano por contato (pode voltar a se disfarçar com `desactivation` se o jogador fugir — Seção 30); ao morrer, libera a mesma recompensa que o baú normal teria dado, abrindo a mesma UI de 3 opções.
- **Escolha da recompensa:** a UI de 3 pergaminhos/cartas **pausa o jogo** (Seção 9) e o jogador escolhe **1 das 3 opções clicando com o mouse** — não com E, Enter, ou qualquer tecla do teclado. Após a escolha, o buff é aplicado, a UI fecha, e o gameplay continua.

```text
Baú → E → recompensa liberada → UI de 3 opções (jogo pausado) →
clique do mouse em 1 opção → buff Run-Persistent aplicado → UI fecha → gameplay continua
```

Preservado sem alteração: 3 opções sem duplicar tipo no mesmo sorteio, escolha de exatamente 1, 1 reroll gratuito por dia, rerolls extras compráveis, buffs Run-Persistent (Seção 31).

---

## 31. Cartas (Pergaminho)

### Regras de sorteio ✅
3 cartas aleatórias por pergaminho, nunca repetindo tipo entre si no mesmo sorteio. Escolhe exatamente 1. Bônus aumentam por `ActualDay` (revisão estrutural — antes "por nível do andar", Seção 23). 🔢 curva exata. Buffs escolhidos são **Run-Persistent** (Seção 15). Buffs repetidos entre pergaminhos diferentes acumulam. **1 reroll gratuito por dia de run**, confirmado. Rerolls adicionais compráveis na aba Bonuses. 🔢 preço. Cartas com teto (ex.: cura periódica do Cleric, absorção do shield do Paladin) param de aparecer ao atingir o limite. **Correção (pós-detalhamento de heróis):** a quantidade de flechas do Ranger, de vinhas do Druid e de balas do Gunslinger **não vêm mais de carta nenhuma** — passaram a escalar com o Tier de Arma (Seção 19, Seção 17.2/17.4/17.8), a mesma progressão que já multiplica Dano/Vida Base de todo herói.

### Pools ✅
**Universal:** Velocidade de Ataque, Dano de Ataque, Velocidade de Movimento, Vida — em %. **Específica por herói:** listadas em cada ficha da Seção 17. **De Employee:** só entra no sorteio se houver employees possuídos naquele dia.

---

## 32. Attack Speed — Escalonamento Multi-Fonte

Attack Speed não é uma única porcentagem aplicada uniformemente. Cada família de fonte (ataque primário, passivas periódicas, cooldown de summon, hitboxes orbitais) tem seu próprio coeficiente/curva de conversão, para impedir que uma passiva chegue a 100% de uptime só por acúmulo de Attack Speed pensado no ataque primário. 🔢 curvas e tetos por família ficam no documento de balanceamento.

---

## 33. Pets, Summons e Fontes Automáticas

**Pet ≠ Employee.** ✅

### Pets permanentes de kit (Mage, Blood Mage, Demonologist) ✅
Não alvejados por monstros. Sumonados no início de cada dia com animação (duração = a do próprio clipe de invocação do herói, Sprint 19 — não é um tempo fixo à parte) e bloqueio de movimento. Consomem uma fatia do orçamento ofensivo do herói (Seção 11). Ao morrer o herói, o pet retorna junto na transição (Seção 11) — a Phoenix/Elemental toca a própria animação de desaparecer, não some instantaneamente.

### Summons temporários alvejáveis (Necromancer) ✅
Alvejáveis por monstros, tempo de vida próprio, podem stackar. **São destruídos ao morrer o herói — regra padrão, sem exceção, mesmo para o Necromancer** (Seção 18).

### Passivas com camada visual própria — não são "Efeito" (Paladin — shield; Plague Doctor — orbs orbitais; Barbarian — brilho de baixa vida) ✅
Gira/aparece ao redor do herói periodicamente, objeto filho do GameObject principal. Shields têm vida própria distinta da vida do herói. **Importante (fonte de confusão registrada — Sprint 17):** passiva de herói nunca usa o sprite/prioridade do sistema de Efeitos Nocivos abaixo — tem sempre seu próprio slot visual dedicado, exclusivo daquele herói (a bolha do Paladin, a aura do Cleric, o brilho do Barbarian). "Efeito", a partir de agora neste documento, significa exclusivamente status nocivo que um **monstro aplica no herói** — nunca o contrário.

**Implementação (Sprint 17) — mesma estrutura técnica pras duas camadas (passiva e Efeito Nocivo), só a prioridade de renderização muda:** cada slot é um GameObject filho dedicado com `SpriteRenderer` + `Animator` próprio, tocando uma animação em loop, pixel art, semi-transparente, **na frente** da entidade (sprite base do herói/monstro) — igual ao `DomeStart/DomeCycle/DomeEnd` do Paladin, só generalizado pra qualquer passiva ou Efeito. Exemplos concretos: cura periódica do Cleric = cruzes douradas subindo; bolha do Paladin = bolha translúcida com a base atrás (`DomeBase`); brilho de baixa vida do Barbarian = auréola; Burn (Efeito Nocivo) = chamas subindo. Nunca é Particle System — mesmo padrão Animator+sprite do resto do jogo.

### Defesa — três mecanismos distintos, sem generalização ✅
- **Cura periódica** (Cleric): recupera vida ao longo do tempo; pode escalar com Vida Máxima.
- **Shields** (Paladin): absorvem dano com vida própria; escalam com Vida Máxima final.
- **Lifesteal** (Blood Mage): nasce do dano causado, não é cura independente — `Cura = percentual do dano causado`, com teto de cura por hit baseado em % da Vida Máxima (referência: 10% do dano como cura, teto de 3% da Vida Máxima por hit — valores no documento de balanceamento). Vida Máxima entra aqui só como **limite superior**, não como base do cálculo.

Essas três mecânicas não constituem sistema defensivo universal — são recursos próprios de heróis específicos (Seção 11).

### Efeitos Nocivos — status aplicado em herói ou monstro (Sprint 17+, dos dois lados desde a Sprint 19) ✅
Sistema à parte, sem relação com passiva de herói (ver acima). **Revisão Sprint 19:** até então só existia monstro→herói ("se surgir [herói→monstro], é uma variação futura, não presumir agora" — a revisão anterior deste documento). Isso deixou de ser verdade: o Fire da ultimate do Mage (Seção 17.3) é herói→monstro, então o sistema hoje funciona nos dois sentidos igualmente. Tecnicamente é 1 componente genérico só (`StatusEffectController`, Sprint 19), compartilhado entre `HeroController` e `EnemyController` (os 2 únicos `IDamageable` do jogo) — não são mais "código próprio" separado por lado, é a mesma classe nos dois. Enum completo de tipos já preparado (`Fire`, `Bleeding`, `Fear`, `Heal`, `Ice`, `Nature`, `Petrification`, `Poison`, `Shock`, `Sickness`, `Sleep`, `Stun`) — confirmados pro MVP: **Fire, Bleeding, Ice, Fear, Heal**; o resto é reserva de nome até decidir se entra no jogo. **Hoje só existe mecânica de dano-ao-longo-do-tempo** (serve Fire/Bleeding, e serviria Poison/Sickness se algum dia virarem DoT) — Heal (cura, oposto de dano), Ice/Sleep/Stun/Petrification (incapacitação, mesma família do Trapped) e Fear (muda comportamento) ainda não têm nenhuma mecânica própria implementada, só o nome reservado no enum.

- **Duração e renovação:** um Efeito tem uma duração fixa (ex.: Fire do Mage, 3s; Bleeding do Ranger, 5s). Enquanto a fonte que aplica o efeito continuar em contato com o herói (ex.: o jogador parado em cima de um totem), a duração se **renova continuamente** — nunca cai abaixo do valor cheio. Só quando a fonte é destruída ou termina por conta própria (o totem expira) é que a duração começa a contar de verdade até zerar.
- **Imunidade por tipo (Sprint 19):** monstro pode ser configurado imune a 1 ou mais `StatusEffectType` (`StatusEffectController.immunities`, lista no Inspector) — imunidade bloqueia só o **Efeito em si** (o DoT contínuo), nunca o dano de impacto direto de quem aplicou. **Exemplo de referência (futuro Fire Elemental):** imune a Fire — toma o dano da explosão da Ultimate do Mage normalmente (é impacto direto, não é o Efeito), mas não pega o DoT depois, e nem toma dano do rastro de fogo persistente no chão (a fonte do rastro, `MageFireball`, consulta a imunidade antes de aplicar o tick da área — não é só a `StatusEffectController` recusando o Efeito, é a fonte inteira ignorando esse alvo pro dano de área também, já que conceitualmente é o mesmo "fogo" acertando).
- **Múltiplos efeitos simultâneos (stack):** o herói pode estar sob vários Efeitos diferentes ao mesmo tempo (ex.: Fire de um totem + Freeze de outro totem do mesmo Shaman) — cada um continua aplicando sua própria lógica (dano por segundo, paralisia, etc.) em paralelo, independente dos outros.
- **1 sprite só na frente do herói por vez — prioridade por categoria:** mesmo com vários Efeitos ativos em paralelo, só **1** anima na frente do sprite do herói. Cada Efeito pertence a uma categoria, e existe uma ordem de prioridade entre categorias decidindo qual sprite aparece quando mais de uma está ativa. **Categorias confirmadas até agora (lista cresce conforme novos monstros forem desenhados — 🟡 não é lista fechada):**
  - **Prisão** (ex.: Freeze) — prioridade mais alta.
  - **DoT** (ex.: Burn) — prioridade mais baixa.
- 🟡 **Dívida técnica (Sprint 19):** o `StatusEffectController` ainda não implementa essa prioridade por categoria — hoje ele só mostra o **primeiro Efeito que foi aplicado** (ordem de aplicação, não prioridade de categoria). Sem problema por enquanto porque Fire/Bleeding (os únicos com mecânica real hoje) são os 2 da mesma categoria (DoT), sem conflito possível. Precisa ser implementado antes de qualquer Efeito de Prisão (Ice/Sleep/Stun/Petrification) ganhar mecânica de verdade, senão o exemplo do Shaman abaixo não funciona.
- **Exemplo de referência (Shaman, Threat Tier 3):** um totem de fogo aplica Fire (DoT, 3s) e outro totem aplica Freeze (Prisão, 1s). Se o herói pegar os dois ao mesmo tempo: o dano do Fire continua contando normalmente em segundo plano, mas a sprite exibida é a do Freeze (Prisão > DoT). Quando o Freeze (1s, menor duração) termina, a sprite volta pra Burn — que ainda está ativo, e agora é o único Efeito restante.
- **Efeito Nocivo já funciona nos dois sentidos** (herói→monstro e monstro→herói, ver revisão Sprint 19 no topo desta seção) — não existe ainda efeito de monstro sobre si mesmo/outro monstro documentado aqui (se surgir, é uma variação futura, não presumir agora).
- **Ordem de renderização entre as duas camadas (Sprint 17):** passiva sempre desenha **na frente** de Efeito Nocivo — o Sorting Layer/Order in Layer da camada de passiva (Seção acima) tem que ficar numericamente acima do que a camada de Efeito Nocivo vier a usar, sempre, sem exceção por herói.

---

## 34. Employees — Sistema Completo

### Estrutura geral ✅
Ajudante (combate) e Coletor. Nenhum é alvejado. **Revisão estrutural (mundo único):** o antigo gatilho "ao entrar em um andar diferente do térreo" deixa de existir — Employees possuídos acompanham o jogador continuamente desde o início do dia, sem precisar de um evento de spawn próprio ligado a troca de andar.

### Compra — fluxo completo ✅
Na aba Employees, lado esquerdo: o jogador seleciona o tipo/tier que deseja comprar. Ao clicar, abre um popup de compra contendo **scroll/slider de quantidade**, **input numérico manual**, e **confirmação da compra**.
- **Limite pelo dinheiro:** o scroll não pode passar da quantidade máxima que o jogador consegue pagar (ex.: preço unitário 100g, gold 850g → máximo comprável = 8, scroll vai até 8).
- **Input manual:** se o jogador digitar um valor acima do que pode pagar (ex.: 999 quando só pode comprar 8), o valor é **clampado para o máximo possível** (8) — nunca rejeita a compra inteira, nunca permite valor acima do possível.

### Promoção — fluxo completo ✅
Árvore: Intern → Junior → Mid-level → Senior → (Strong ou Fast). Cada promoção exige **dinheiro + 1 employee do tier imediatamente anterior**, que é **consumido** no processo (ex.: promover 10 Junior exige 10 Intern + o gold necessário; ao confirmar, -10 Intern, +10 Junior).

**Promoção em lote — duplo limite:** o scroll/input de promoção é limitado por **duas condições simultâneas**: *(1)* gold disponível; *(2)* quantidade disponível do employee do tier anterior. Exemplo: gold permitiria promover 50, mas existem só 12 Intern → máximo = 12. Ou: existem 100 Intern, mas gold só permite 7 promoções → máximo = 7. Um valor digitado acima do máximo possível é **clampado** para o maior valor permitido pelas duas condições.

### Venda — fluxo completo ✅
No lado direito da aba Employees: employees possuídos são exibidos por imagem, tier/tipo e quantidade possuída. Ao clicar em um employee possuído, abre um painel de venda com **scroll/slider de quantidade**, **input numérico manual**, **botão de confirmar** e **botão X/fechar**.
- Máximo vendável = quantidade possuída; valor digitado acima disso é clampado.
- **Fechar no X não realiza venda.** Só **Confirmar** vende a quantidade selecionada.

### Ajudante (combate) ✅
Dano, atk speed, velocidade de movimento, delay. Intern→Senior melhora tudo gradualmente. **Fast:** dano/atk speed = Senior; velocidade muito maior; delay quase inexistente. **Strong:** velocidade/delay = Senior; dano/atk speed muito maiores.

### Coletor ✅
Vai até itens com loot disponível em qualquer parte do mundo (Seção 24/36), coleta instantaneamente ao entrar no raio. Ao atingir capacidade/tempo definido, some por um tempo, vende automaticamente, depois retorna. Intern→Senior melhora capacidade/velocidade e reduz tempo de ausência. **Fast:** capacidade ≈ Senior; velocidade muito maior; ausência extremamente reduzida — ciclos rápidos. **Strong:** velocidade ≈ Senior; capacidade muito maior; ausência também melhora em relação ao Senior — grandes quantidades por ciclo.

### Filtro ✅
Comprado em Bonuses, impede o Coletor de pegar tipos de item específicos.

---

## 35. Virtualização de Employees

Contagem lógica (quanto o jogador possui) é diferente de quantidade fisicamente simulada em cena. O jogo pode representar posse de milhões simulando um número muito menor de unidades mais fortes/eficientes, mantendo a leitura visual de "está muito cheio" sem travar performance. Mesma filosofia vale para loot no chão (Seção 38) e para monstros/população fora do raio de simulação completa, nos Simulation Rings mais distantes (revisão estrutural — antes "Floors fora da região ativa", Seção 24). Parâmetros exatos são decisão técnica/de balanceamento.

---

## 36. Coleta Distribuída pelo Mundo

**Revisão estrutural — substitui "entre Floors" por "pelo mundo único".** Resultado esperado, sem determinar implementação: **Coletores podem localizar/coletar loot em qualquer parte do mundo, mesmo longe do jogador, sem exigir que todos os Employees sejam simulados individualmente em tempo real** (mesma filosofia de Virtualização — Seção 35 — e Simulation Rings — Seção 24). A forma exata pertence ao Technical Architecture Document (Seção 51).

---

## 37. Inventário (Bag)

### Regras confirmadas ✅
Aberto com **TAB** (pausa — Seção 9). Contém exclusivamente loot — nenhum item utilizável, arma física ou consumível. Início: **5 slots**, stack **16**.

### Progressão de slots — matemática corrigida ✅
```text
5 (inicial)
↓ compra 1
10
↓ compra 2
15
↓ compra 3
20 (máximo)
```
**3 compras** levam de 5 a 20 slots (+5 por compra) — não 4. 🔢 preços de cada compra.

Progressão de stack: 16→32→128→1.024→8.192→131.072→1.048.576 (valores totais). 🔢 preços.

**Descartar:** clique direito. **Reorganizar:** arrastar com clique esquerdo. Compras na loja nunca competem por espaço no inventário.

### Interações de drag/drop ainda não definidas 🟡
Os comportamentos exatos de merge/swap ao arrastar itens não foram fechados pelo designer — não devem ser presumidos. Ver a lista completa na Seção 53 (Pendências Abertas): comportamento ao arrastar uma stack sobre outra stack do mesmo item; merge parcial quando o destino não comporta a stack inteira; comportamento ao arrastar para um slot ocupado por item diferente; quantidade efetivamente removida por um clique direito (stack inteira, 1 unidade, ou seleção de quantidade).

### Coleta parcial ✅
Pilha maior que o espaço disponível: coleta o máximo que couber, resto fica no chão como entidade separada. Nunca tudo-ou-nada.

### Pickup Radius — coleta automática por proximidade ✅
O jogador não precisa encostar exatamente no sprite do loot. Ele possui uma **área/raio de coleta (Pickup Radius)**: quando um **loot válido** entra nessa área, o sistema tenta coletá-lo automaticamente — não exige apertar E, clicar no item, ou encostar pixel a pixel.

```text
Loot entra no Pickup Radius → Player tenta coletar
```

- **O que é "loot válido":** o Pickup Radius atua **somente sobre materiais coletáveis/vendáveis destinados à Bag ou à venda** — os 15 materiais econômicos (Monster Essence, Monster Fragment, Spirit Dust, Arcane Shard, Dark Crystal, e demais da lista — Seção 39). **Interactables especiais não são loot do Pickup Radius** e continuam usando sua própria regra de interação existente, nunca sendo coletados automaticamente só por entrar no raio: baús, o próprio Magnet, NPCs, interações de quest, o Controle Remoto, e qualquer outro interactable especial futuro. **Pergaminhos/recompensas de baú não são materiais econômicos válidos para o Pickup Radius e não são aspirados automaticamente** — a interação de abertura ocorre no baú através de E (Seção 30), e a escolha entre as três opções de recompensa é feita com o mouse na UI, não pelo Pickup Radius. O Pickup Radius não substitui a tecla **E** como sistema de interação contextual — ele é exclusivamente um sistema automático de coleta de loot econômico.
- **Se houver espaço na Bag:** o loot é coletado automaticamente, seguindo as regras já existentes de slots, stacks e filtro de bag.
- **Coleta parcial continua valendo:** se a pilha exceder o espaço disponível, entra o máximo que couber e o restante permanece no chão como entidade separada (mesma regra da subseção acima) — o Pickup Radius não ignora os limites da Bag.
- **Filtro de bag continua valendo:** um tipo de loot bloqueado pelo filtro não é coletado automaticamente mesmo entrando no raio.
- **Valor base:** o jogador possui um Pickup Radius base. 🔢 valor numérico pendente de balanceamento — não fixado em unidades, tiles, metros ou pixels aqui.
- **Upgrade "Increase Pickup Radius"** (aba Bonuses — Seção 41): aumenta o raio do jogador. **Run-Persistent** — persiste entre dias da run, reseta para o raio base em nova run (mesma categoria de qualquer bonus comprado, Seção 15). 🔢 quantidade de compras, curva de aumento e preços pendentes — não presumir tiers, percentuais ou incrementos específicos.
- **Aggregação de drops não muda:** pilhas seguem a mesma lógica de representação visual (1–9 individual, 10–49/50–99 stacks, 100+ quantidade interna — Seção 38); ao entrar no Pickup Radius, a lógica de coleta/venda se aplica sobre a quantidade interna real, não exige que cada unidade visual entre individualmente na área.
- **Feedback visual do raio é decisão de UI/UX** — pode ser invisível normalmente, mostrado ao comprar o upgrade, mostrado em debug, ou outra solução; não definido aqui.
- **Não é o raio do Coletor Employee.** O Pickup Radius é um sistema do jogador; o Coletor continua com sua própria lógica, capacidade, movimento e ciclo (Seção 34), incluindo a Coleta Distribuída pelo Mundo (Seção 36, antigo "Cross-Floor") — os dois sistemas não se conectam.

### Filtro de bag ✅
Toggles por tipo de item.

### Fim de dia ✅
Todo loot ainda no inventário é destruído (Daily — Seção 15).

---

## 38. Drops no Chão — Agregação Visual e Regras de Loot

### Agregação visual — feedback de poder, não só otimização ✅
1–9: individual. 10–49: stacks de 10. 50–99: stacks de 50. 100+: sprite(s) com quantidade real interna, podendo se distribuir em múltiplas pilhas visuais para reforçar a sensação de fartura. Coleta parcial de pilhas grandes segue a mesma lógica da Seção 37.

### Rolagens independentes — regra restaurada ✅
**Cada tipo de loot realiza sua própria rolagem de chance de drop, independentemente das demais.** Um único abate pode dropar múltiplos materiais diferentes simultaneamente se as rolagens correspondentes forem bem-sucedidas — por exemplo, um mesmo monstro pode dropar Monster Essence **+** Monster Fragment **+** Spirit Dust **+** Arcane Shard no mesmo abate. **Não se trata de "escolher apenas um item da tabela"** — é um conjunto de rolagens independentes, uma por material possível daquele monstro.

### Materiais de Tiers anteriores continuam no pool ✅
**Revisão estrutural (mundo único — antes "Floors", Seção 24):** desbloquear os materiais associados a um Threat Tier superior **não remove** os materiais dos Tiers anteriores da tabela de drop. Monstros de Tier superior continuam podendo dropar materiais "antigos" normalmente, conforme a tabela de drop de cada monstro (documento de balanceamento — Seção 51).

---

## 39. Economia e Materiais

### Materiais (15 tipos) ✅
Monster Essence, Monster Fragment, Spirit Dust, Arcane Shard, Dark Crystal, Soul Fragment, Corrupted Core, Elemental Shard, Ancient Fragment, Infernal Ash, Chaos Crystal, Nightmare Residue, Void Shard, Celestial Fragment, Divine Core — cada um com valor de venda próprio. Notação de UI: k/m/b/t.

- Cada item faz sua própria rolagem de chance (regra completa na Seção 38).
- Valores são valor-base por abate, antes de bônus de run/employees.

### Demanda diária — Modo Padrão ✅
Só Monster Essence conta. `Demanda do Dia = 40 × 2^(Dia - 1)`. Contabilizada pela venda acumulada no dia. Esta subseção não se aplica ao Modo Free; nesse modo, Monster Essence continua sendo loot econômico vendável normalmente, apenas sem função de quota obrigatória (Seção 42).

### Venda ✅
NPC vendedor fixo, na/perto da Zona Segura (revisão estrutural — antes "no térreo", Seção 24), durante o gameplay do dia (relógio correndo).

---

## 40. Ciclo de Dia

### Duração ✅
Inicial: 100s. Upgrade "Add Time": +100s por compra, até 2 compras (teto 300s). 🔢 preços.

### CurrentPhase — Dia/Noite dentro do próprio Ciclo de Dia (mecânica nova, não preservação) ✅
**Proposta nova desta revisão estrutural, não uma formalização de algo já existente** — antes desta mudança, o GDD não tinha nenhum conceito de fase diurna/noturna dentro da contagem do dia (a única menção prévia a "dia/noite" era uma ideia de iluminação por bioma, em Visão Expandida, nunca implementada e sem relação com a tabela de spawn).

- Cada Ciclo de Dia se divide em exatamente **50% `CurrentPhase = Day` / 50% `CurrentPhase = Night`**, qualquer que seja a duração total (inicial ou com Add Time): 100s → 50/50; 200s → 100/100; 300s → 150/150. **Nenhum bônus de tempo pode quebrar essa proporção.**
- A transição precisa ser visualmente perceptível (iluminação/atmosfera — Seção 25 cobre a materialização ambiental; a implementação exata de luz/shader é decisão técnica, Seção 51).
- **Não é buff de HP/Dano.** A noite nunca dá bônus artificial de atributo a monstro nenhum — o efeito inteiro é indireto, através da composição da Tabela Base de Spawn (Seção 23): cada combinação `Dia N` + `CurrentPhase` tem a própria linha de pesos por Tier, e a noite tipicamente favorece Tiers mais altos que o dia do mesmo número. `CurrentPhase` é um dos 3 insumos de `EffectiveSpawnDay` (junto de `ActualDay` e `DistanceDayOffset` — Seção 23).
- **Pausa não avança `CurrentPhase`** (mesmo critério de qualquer timer — Seção 9). Trocar de `CurrentPhase` no meio de um Boss Event não troca bosses já vivos (Seção 22).

### Encerramento — compartilhado, com validação de demanda apenas no Padrão ✅
Tempo zera **ou** jogador usa a porta — igual nos dois modos.

**Modo Padrão:** valida a demanda. **Não cumprida → GAME OVER** (texto de tom bem-humorado; run encerrada, mas **o save não é apagado** — Seção 43). **Cumprida →** segue para a Tela de Resultados.

**Modo Free:** não valida demanda (Seção 42) — segue diretamente para a Tela de Resultados.

### Tela de Resultados — igual nos dois modos ✅
- Revela **apenas os itens vendidos naquele dia** — nenhum item não obtido aparece.
- Para cada item: **imagem, nome, quantidade vendida, valor unitário, e total daquele item.**
- Os itens aparecem **um por um**, em sequência animada.
- Pressionar **Enter** revela todos imediatamente, pulando a animação.
- Ao final: `TOTAL DO DIA: Xg`.
- Botão **Continuar** → Loja.
- **A Tela de Resultados não vende nada** — ela apenas resume vendas que já aconteceram durante o dia (pelo vendedor fixo, Seção 39, ou pelo Coletor/Magnet). Loot que ainda estiver no inventário do jogador ao final do dia é **destruído**, não vendido (Seção 37).

### Loja entre dias ✅
3 abas: Upgrades / Bonuses / Employees. Botão **Start Day N**. **Save automático ao entrar na loja** (Seção 43). Idêntica nos dois modos.

---

## 41. Bonuses (aba da loja)

Controle Remoto (Seção 26), Filtro de bag (Seção 37), Filtro de Employee coletor (Seção 34), **Add Time** (+100s por compra, até 2 compras), **Increase Inventory Slots** (+5 por compra, **3 compras** até o máximo de 20 — Seção 37), **Increase Inventory Stack Size** (16→32→128→1.024→8.192→131.072→1.048.576), **Increase Pickup Radius** (aumenta o Pickup Radius do jogador — Seção 37; também beneficia a área efetiva do Magnet, Seção 29; quantidade de compras, curva e preços 🔢), **Supressão de Ameaça** (Seção 27, antigo "Remove Tower Layer"), **Reroll de pergaminho** (Seção 31). 🔢 todos os preços.

---

## 42. Modos de Jogo

### Modo Padrão ✅
Demanda diária obrigatória de Monster Essence (Seção 8, Seção 39). Único modo que desbloqueia heróis e conta Account Progression (Seção 15). No fim do dia, valida a demanda: não cumprida → Game Over; cumprida → Resultados → Loja → Save.

### Modo Free — regra final ✅
**O Modo Free é o mesmo jogo do Modo Padrão, apenas sem a demanda obrigatória de Monster Essence.** Não é uma máquina de estados própria, não é sandbox infinito, e nenhum dos sistemas abaixo é removido:

- **Dias, timer, Loja, Resultados, Save, o mundo único, progressão de arma, Bonuses, Employees, Quests, baús, cards e bosses funcionam normalmente**, exatamente como no Modo Padrão.
- **Timer:** o mesmo do Padrão — começa em 100s, "Add Time" funciona normalmente, pode chegar a 300s pelos upgrades já definidos (Seção 40). Não existe timer infinito.
- **Porta de saída:** funciona normalmente — usá-la encerra o dia voluntariamente.
- **Fim de dia:** tempo acaba ou jogador usa a porta → **não existe validação de demanda** (porque não há demanda no Free) → segue direto para a Tela de Resultados → Loja → Save → Start Day N+1. **O Free não tem Game Over por falha de demanda**, já que não existe demanda a falhar.
- **Morte continua existindo normalmente:** -30s de penalidade, perda de loot, respawn no centro do mundo (revisão estrutural — antes "no térreo", Seção 11/24) — a única coisa removida é a possibilidade de falhar o dia por não vender Essência suficiente.
- **Resultados:** exatamente a mesma tela do Padrão (Seção 40) — item por item vendido, imagem, nome, quantidade, valor unitário, total, `TOTAL DO DIA: Xg`, Enter revela tudo, botão Continuar → Loja. Monster Essence vendida no Free continua aparecendo normalmente como item vendido, mesmo sem função de quota.
- **Loja:** as mesmas 3 abas (Upgrades, Bonuses, Employees), mesmos sistemas, mesmo botão "Start Day N".
- **Dia 15:** o marco é atingido pela **conclusão normal do dia** (tempo esgotado ou porta usada, sem demanda a cumprir) em vez de pela demanda cumprida do Padrão — mas apresenta a mesma Tela de Vitória e as mesmas escolhas Menu/Continuar, seguindo a mesma estrutura de fluxo e de save da Seção 43.
- **Dia 30:** mesma lógica — concluído normalmente (sem demanda), apresenta a mesma tela de encerramento definitivo do Padrão (Seção 44).
- **Progressão dentro da run funciona normalmente:** upgrades de arma evoluem, Employees funcionam, cards são sorteadas e aplicadas, Quests progridem — tudo isso opera normalmente **dentro daquela run Free**, do mesmo jeito que no Padrão.

**O que o Free não concede** (Account Progression — Seção 15), preservado sem alteração:
- **Não desbloqueia heróis.**
- **Não libera recompensas/achievements de progressão** — inclusive a conquista Steam do Dia 30 (Seção 44) não é concedida numa run Free.
- **Não contabiliza progressão de campanha** — não pode ser usado para cumprir critérios de unlock (Seção 49).

**Lifetime Statistics continuam sendo registradas normalmente no Free** (Seção 15/49) — ex.: kills, gold vendido, dias jogados podem alimentar contadores informativos do perfil mesmo numa run Free. Isso é puramente informativo: **registrar a estatística nunca aciona avaliação de progressão/unlock**. Isso evita que o perfil do jogador fique artificialmente incompleto sem abrir uma forma de "farmar" desbloqueios pelo Free.

Ou seja: os sistemas Run-Persistent (arma, Employees, cards, quests) funcionam normalmente dentro de uma run Free; apenas o que seria Account Progression nunca é gerado por ela.

### Modos futuros 🔭
"Only Monster Essence" — ideia mencionada, sem regras fechadas.

---

## 43. Save — Semântica Completa e Definitiva

O save deste jogo é simples: **terminou o dia → entrou na Loja → salva. Se já existia um save, o novo sobrescreve o anterior — sem exceções entre modos.**

### Regra Geral — 1 slot ✅
Existe **um único slot de save de run**, compartilhado entre o Modo Padrão e o Modo Free. Não existe save separado por modo, slot protegido, prioridade entre saves, ou backup — apenas o último checkpoint salvo, seja qual for o modo ao qual ele pertence.

### O save é um checkpoint, não um registro de resultado de run ✅
> **O save representa o checkpoint da última Loja alcançada — nunca é apagado pelo fim de uma run**, seja esse fim um Game Over, uma vitória no Dia 15, ou o encerramento definitivo do Dia 30. "A run termina" significa que a sessão/tentativa atual se encerra e o jogador retorna ao Menu Principal — **isso nunca significa deletar o checkpoint da última Loja.** Esses dois conceitos (run terminar / save ser apagado) são estruturalmente diferentes.

### Autosave ✅
Save ocorre **somente** ao entrar na fase de Loja, imediatamente após o encerramento normal de um dia — nunca durante o dia. Isso vale igualmente para os dois modos: no Padrão, "encerramento normal" pressupõe ter cumprido a demanda; no Free, não há demanda, então qualquer fim de dia normal (tempo zerado ou porta usada) leva a esse mesmo fluxo.
```text
Fim do dia (Padrão: demanda cumprida / Free: sempre) → Resultados → Loja → SAVE AUTOMÁTICO
```

### Overwrite — sempre sobrescreve, sem exceção entre modos ✅
Todo novo autosave **sobrescreve** o save existente, não importa a que modo pertencia o save anterior ou o novo. Exemplos:
```text
Save atual = Standard, Dia 10
↓ Player inicia uma run Free, conclui o Dia 1, chega à Loja → SAVE
↓
O save do Free sobrescreve o save Standard anterior.
```
```text
Save atual = Free, Dia 8
↓ Player inicia uma run Standard, conclui o Dia 1 com demanda cumprida, chega à Loja → SAVE
↓
O save Standard sobrescreve o save Free anterior.
```
Isso é intencional — o save não é uma mecânica estratégica do jogo, é apenas um checkpoint automático entre dias. Não existe confirmação obrigatória de overwrite no nível de sistema (uma eventual confirmação de UX ao clicar New Game é decisão de interface, não altera esta regra — Seção 52).

### Game Over não gera novo save ✅
```text
Jogador terminou o Dia 8 → Resultados → Loja → SAVE → Start Day 9
   ↓
Falha na demanda do Dia 9 (Padrão) → GAME OVER → Menu Principal
```
Não houve entrada em uma nova Loja, então **não houve novo autosave** — o save simplesmente **não foi sobrescrito**, continuando a ser a Loja que precede o Dia 9. Continue Game volta a essa Loja, com "Start Day 9" disponível. Isso não é permadeath. O Free não possui esse cenário — sem demanda, não há falha de dia por essa causa (Seção 42); morte continua existindo normalmente (Seção 11), mas não gera Game Over por conta própria.

### Dia 15 — Menu vs. Continuar (Padrão e Free, mesma regra) ✅
```text
Dia 14 concluído → Resultados → Loja → SAVE → Start Day 15
   ↓
Demanda cumprida (Padrão) / dia concluído (Free) → Tela de Vitória
   ↓
Escolhe Menu Principal
```
Essa escolha **não** passa por Resultados → Loja → novo save, então o checkpoint continua sendo **a Loja que precede o Dia 15** — o jogador pode jogar o Dia 15 de novo via Continue Game. Se escolher **Continuar**:
```text
Tela de Vitória → Continuar → Resultados do Dia 15 → Loja → AUTOSAVE (novo checkpoint: Loja pré-Dia 16)
```

### Dia 30 ✅
Ao concluir o Dia 30, a run encerra obrigatoriamente (Seção 44), mas **o save não é apagado** — continua sendo a última Loja em que um autosave ocorreu. Não há autosave adicional após a tela de encerramento definitivo.

### New Game não sobrescreve antes do primeiro autosave da nova run ✅
```text
Save existente: Loja que precede o Dia 12
   ↓
New Game (qualquer modo) → nova run começa no Dia 1
   ↓
Game Over (Padrão) ou fechamento antes da primeira Loja
```
O save antigo continua intacto — Continue Game ainda carrega essa run anterior. Só quando a nova run **conclui seu primeiro dia e chega à Loja com autosave** é que o save anterior é sobrescrito, seguindo exatamente a regra de Overwrite acima.

### Semântica do dia salvo ✅
O save precisa distinguir inequivocamente **qual dia acabou** e **qual é o próximo dia a iniciar** (conceitualmente `LastCompletedDay`/`NextDay`, ou estrutura equivalente — nomes não obrigatórios), evitando risco de repetir, pular, ou mostrar o número errado no botão da Loja.

### Continue Game — não pergunta o modo ✅
```text
Existe save? → SIM → carrega o RunState salvo (que já contém o Mode) →
abre a Loja do checkpoint → Start Day N
```
O `RunState` salvo já contém o **Mode** da run (além de herói, mapa, dia, gold, weapon tier, bonuses, employees, quests, cards, `WorldSeed` do mundo único gerado, Tiers suprimidos pela Supressão de Ameaça — revisão estrutural, antes "Floor Variants sorteados, Remove Tower Layers aplicados"). Continue Game **lê o Mode salvo** e carrega a experiência correspondente automaticamente — nunca pergunta modo, herói ou mapa de novo, seja o save de uma run Padrão ou Free. Esses valores são **carregados**, nunca escolhidos de novo.

---

## 44. Vitória e Pós-Game

- **Dia 15:** marco atingido por **completar a demanda** (Modo Padrão) ou por **concluir normalmente o dia** (Modo Free, sem demanda — Seção 42). Ambos apresentam a mesma Tela de Vitória → jogador escolhe Menu Principal (save permanece na Loja pré-Dia 15, Seção 43) ou Continuar (novo save na Loja pré-Dia 16, Seção 43).
- **Dias 16–30 (pós-game):** mesmo core loop. **No Padrão, as demandas continuam crescendo; no Free não existem demandas** (Seção 42) — o restante do loop (Loja, Employees, upgrades, exploração) continua igual nos dois modos. Matar um boss de topo (ex.: Divine God) não encerra a run — é conquista, não condição de vitória.
- **Dia 30:** limite real da run em ambos os modos. Atingido por **completar a demanda** (Padrão) ou **concluir normalmente o dia** (Free). Em ambos os casos, a mesma tela de encerramento definitivo é exibida e a run termina, com o save permanecendo no último checkpoint (Seção 43). **A conquista Steam de conclusão do Dia 30 é Account Progression (Seção 15) e só é concedida no Modo Padrão** — o Free mostra o mesmo encerramento, mas não concede o achievement nem qualquer outra recompensa de progressão.

---

## 45. UI / HUD

### Sempre visível nos dois modos ✅
Tempo restante, bag (slots/stacks).

### Modo Padrão apenas ✅
Demanda do dia (`Monster Essence: X/Y` → `DEMANDA CUMPRIDA`, Seção 39). **O Modo Free não exibe esse indicador** — sem demanda, não há progresso de quota para mostrar; Monster Essence continua existindo normalmente como loot vendável.

### Necessário por decorrência dos sistemas definidos, sem layout fechado 🟡
Vida/Vida Máxima, Energia/carga da Ultimate, cooldown do ataque primário, cooldown do Controle Remoto, indicador de Employees ativos, indicador de pets/summons ativos, buffs de carta ativos, indicador de Continue Game disponível/indisponível (Seção 52), **indicador de distância do centro/Zona Segura** (revisão estrutural — antes "Floor atual/Active Floor Position", Seção 24), Mapa selecionado na criação da run.

### Outras telas ✅
Tela de Resultados (Seção 40), tela de Loja, tela de Vitória (Dia 15), tela de Encerramento Definitivo (Dia 30), tela de Game Over — todas com texto próprio.

---

## 46. Controles

| Entrada | Função |
|---|---|
| WASD | Movimentação |
| Mouse | Mira / direção |
| LMB | Ataque primário |
| RMB | Ultimate |
| Shift | Habilidade Secundária (Seção 16, todo herói — Sprint 18→19) |
| E | Interação contextual (ex.: abrir baús — Seção 30) |
| TAB | Inventário (pausa) |
| Q | Controle Remoto (pausa) |
| ESC | Pausa (Sprint 18→19) — mesmo `TogglePause` do Q; o menu clássico (sair/configurações) em si ainda não existe, é peça de UI própria sem escopo definido |
| Enter | Pula animação da tela de resultados |

A escolha entre as 3 opções de recompensa de baú/pergaminho (Seção 30–31) é feita com **clique do mouse** na UI pausada, não por nenhuma tecla desta tabela.

---

## 47. Feedback Visual e Sonoro — Necessidades Funcionais

Dano causado, crítico, Ultimate pronta, Ultimate sem energia suficiente, dano recebido, morte, coleta, venda, **demanda cumprida (Modo Padrão apenas — Seção 42)**, aparição de boss, Mimic revelado, baú abrindo, upgrade de arma comprado, promoção de employee, inventário cheio, item descartado, dinheiro aumentando, grandes quantidades, Controle em cooldown, interação disponível, telegraph de trap, evolução visual/ambiental por dia e distância (revisão estrutural — antes "Floor atual/troca de Global Light", Seção 24/25), Continue Game indisponível quando não há save (Seção 52), mapa selecionado na criação da run. 🟡 Estética exata não definida — só a necessidade funcional.

**Transições de tela (decisão técnica, Sprint 8):** implementadas via o asset Easy Transitions (`Assets/EasyTransitions/`). O estilo padrão adotado é **Fade** (`Assets/EasyTransitions/Transitions/Fade/Fade.asset`) — usar Fade por padrão em qualquer transição nova, salvo decisão explícita em contrário. Primeiro uso: respawn do herói após a morte (teleporte ocorre escondido, no `onTransitionCutPointReached`, nunca visível ao jogador).

---

## 48. Números Grandes

Suporte a milhões/bilhões/trilhões e além, sem teto fixo. Notação k/m/b/t. Tipo de dado é decisão técnica. Agregação de drops (Seção 38) e virtualização de employees (Seção 35) preservam lógica interna exata.

---

## 49. Progress Tracking / Achievement Hooks

Quatro escopos formais: três de contagem (Daily/Run/Lifetime, por necessidade de gameplay/estatística) mais a distinção sobre o que cada um pode acionar. ✅

### Daily Counters
Resetam no começo de outro dia. Exemplos: monstros mortos hoje, monstro específico morto hoje, Essência vendida hoje, gold ganho hoje, baús abertos hoje. Necessários para desbloqueios como Mage e Blood Mage (Seção 17) — no Modo Padrão.

### Run Counters
Resetam em nova run. Exemplos: dias concluídos nesta run, maior distância do centro alcançada nesta run (revisão estrutural — antes "maior Floor desta run"), bosses mortos nesta run, total de monstros nesta run, quantidade máxima de Employees nesta run.

### Lifetime Statistics (Permanent Account State — Seção 15)
Nunca resetam. Exemplos: total histórico de kills, total de runs vencidas, runs vencidas por herói, maior distância do centro alcançada historicamente (revisão estrutural — antes "maior Floor histórico"), bosses específicos mortos historicamente, gold vendido, dias jogados. Pode existir naturalmente um contador de dias jogados/completados para fins estatísticos, mas isso **não** deve ser presumido como o contador usado para o desbloqueio do Rogue — o escopo exato desse critério (run única vs. acumulado) ainda está pendente (Seção 17.5, Seção 53). **Lifetime Statistics são puramente informativas e existem independentemente do modo** — inclusive Daily/Run Counters gerados numa run Free podem alimentar Lifetime Statistics normalmente (Seção 42).

### Account Progression × Mode — regra de avaliação ✅
Daily e Run Counters podem existir e ser registrados normalmente em **qualquer** run, de qualquer modo, por necessidade de gameplay/estatística. Porém a **avaliação de critérios de desbloqueio/achievement** (o que transforma um contador em uma recompensa de Account Progression — Seção 15) depende do modo:
```text
Mode = Padrão → Progression/Unlock Evaluator pode usar os counters normalmente
Mode = Free   → counters continuam sendo registrados → Progression/Unlock Evaluator não concede nada
```
Ou seja: **Statistics Recording** permanece ativo em qualquer modo; **Progression Evaluation** é desativado no Free. Isso vale para todos os critérios de desbloqueio de herói (Mage, Blood Mage, Rogue, Assassin, e qualquer herói futuro) e para achievements de progressão — nenhum deles pode ser concedido por eventos ocorridos numa run Free.

Esse sistema precisa existir desde o início da arquitetura — critérios de desbloqueio dependem diretamente dele.

---

## 50. Arquitetura Técnica de Alto Nível

Responsabilidades conceituais (nomes ilustrativos):

- **Hero Definition** — Dano Base, Vida Base, referências de habilidade, condição de desbloqueio.
- **Ability Definition** — timing (Seção 12), tipo de hitbox/projétil, coeficiente de dano.
- **Projectile Definition** — categoria (Seção 13).
- **Enemy Definition** — categoria, atributos, timing de ataque (Seção 22), drops, Threat Tier(s). ✅ **Decisão de formato (ainda não implementada):** com 99 criaturas + 30 bosses, configurar cada uma manualmente no Inspector de um prefab não escala — o formato final vai ser um arquivo de dados externo (JSON ou XML, a decidir qual na hora de implementar), um por criatura ou uma tabela única, cobrindo vida, dano, velocidade de ataque, energia dropada e a tabela de loot (itens/faixas/chance). Pensado especificamente pra permitir gerar esses valores em lote (com ajuda de IA) em vez de digitar campo por campo. Construir isso é trabalho da fase de produção de conteúdo do Bestiary (Deadline 5 em diante, Sprint 20+ no plano de produção), não antes — implementar cedo demais, sem nenhum dado real de referência ainda, arriscaria desenhar o formato errado.
- **Boss Definition** — múltiplos ataques, elegível por Threat Tier (Seção 22), sem mais Boss Timer por Floor.
- **DaySpawnDefinition** — pesos de Tier por `ActualDay` × `CurrentPhase` (Seção 23/40), substitui Floor Definition/Floor Variant Definition.
- **WorldGenerationSettingsSO/BiomeProfileSO/WorldEvolutionProfileSO** (e afins) — geração procedural do mundo único por seed (Seção 25); arquitetura detalhada pertence ao documento especializado (Seção 51), não ao GDD Mestre.
- **Loot Definition** — 15 materiais, valor de venda, regras de drop independentes (Seção 38).
- **Employee Definition** — tier, atributos, custo de promoção.
- **Card Definition** — pool, efeito, teto quando aplicável.
- **Quest Definition** — etapas, requisitos, recompensa.
- **Run State** — modo, herói, mapa, dia do checkpoint, gold, weapon tier, bonuses, employees, quests, cards, `WorldSeed`, Threat Tiers suprimidos (Seção 27), deltas de edição do mundo (Seção 25).
- **Day State** — tudo Daily (Seção 15), incluindo Boss Timer global (não mais por Floor), `UsedBossIdsToday` (Seção 22) e Energia da Ultimate.
- **Permanent Account State** — dividido em *(1)* Account Progression: heróis desbloqueados, achievements, recompensas/critérios permanentes de unlock; *(2)* Lifetime Statistics: contadores informativos de perfil (Seção 15/49).
- **Save Manager** — mantém um único slot de save de run; realiza autosave ao entrar na Loja após o encerramento normal de um dia; cada novo save sobrescreve o anterior, independentemente do modo; Continue carrega o último RunState salvo, que já contém o Mode (Seção 43).
- **Pause Manager** — sistema central de pausa (Seção 9).
- **Population Manager** — Minimum/Target/Maximum ao redor do jogador, Distance Despawn/Replacement (Seção 23) — não mais por Floor.
- **Loot Aggregation** — agregação visual, rolagens independentes (Seção 38).
- **Employee Virtualization** — contagem lógica vs. simulada (Seção 35).
- **Simulation Rings Manager** — suspensão/virtualização por distância do jogador (Seção 24), substitui Floor Sleep/Activation Manager.
- **Distributed Loot Access** — Coletores acessando loot em qualquer ponto do mundo (Seção 36), substitui Cross-Floor Loot Access.
- **Safe Zone / Spawn Validator** — filtra posições candidatas de spawn (comum e boss) excluindo a Zona Segura, cache de nós válidos (Seção 23), substitui Stair/Hole Routing.
- **Progress Tracker** — Daily/Run Counters e Lifetime Statistics (Seção 49); precisa saber qual Mode gerou o evento, para que o Progression/Unlock Evaluator só conceda Account Progression quando `Mode = Padrão` (Seção 42).
- **Large Number Abstraction** — suporte a valores grandes (Seção 48).
- **Shop Manager** — 3 abas, popups de compra/promoção/venda.
- **Chest System** — spawn em posições válidas geradas pelo mundo procedural (Seção 25), chance de Mimic, interação com E e abertura da UI de seleção de 3 cartas/recompensas; o Mimic adia essa UI até ser derrotado. Não exige um objeto de pergaminho físico coletável como requisito técnico (Seção 30).
- **Remote Controller System** — abertura por Q, pausa (Seção 9), teleporte de retorno ao centro (0,0,0), cooldown (Seção 26) — não mais lista de Floors/toggle de alcance A/B.
- **Pickup System** — detecta loot válido (materiais econômicos coletáveis/vendáveis — Seção 37) dentro do Pickup Radius do jogador. Na coleta normal, respeita o Bag Filter, a capacidade da Bag e as regras de coleta parcial. Quando o Magnet está ativo, encaminha o loot para o fluxo de venda automática do Magnet, cuja regra de filtro ainda permanece pendente (Seção 29/53). Nome técnico não obrigatório.

---

## 51. Documentos Especializados

| Documento | Conteúdo |
|---|---|
| **GDD Mestre** (este documento) | Fonte de verdade estrutural |
| **Hero Design Document** | Kits completos, frames de animação, coeficientes finais |
| **Combat System Document** | Timing de ataque (todo Melee/Ranged, Bosses e exceções), projéteis |
| **World Generation Document** *(substitui o antigo "Tower/Floor Document")* | Geração procedural do mundo único por seed — chunks, autotiles, biomas, evolução por dia, ScriptableObjects (Seção 25). Fonte inicial: `docs/new/Mudanca_Estrutural_Geracao_Procedural_Evolucao_Mapa_30_Dias_V2_ILUSTRADO.md` e `docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md` |
| **Employee System Document** | IA detalhada, virtualização técnica |
| **Bestiary** ✅ *(existe — `docs/gdd/bestiary.md`)* | Fichas completas de monstros e bosses, drop rates exatos — 99 criaturas, 10 Threat Tiers (antigos "Andares" — revisão estrutural só renomeia o agrupamento, não as fichas). Atualizado na Sprint 16 (correção) pro modelo híbrido de combate (Seção 22 — `attack` real com Animation Event, sem contato passivo), com exceções documentadas por ficha |
| **Economy & Balance Document** ✅ *(parcial — `docs/gdd/economy-balance.md`)* | Tabela dos 15 tiers e valores dos 15 materiais migrados nesta revisão; curva de demanda, multiplicador de vida da forma de urso e demais valores 🔢 continuam pendentes |
| **Valores de Calibragem — Habilidades** ✅ *(existe — `docs/gdd/balance-values.md`, Sprint 17)* | Índice de todo campo `🔢` de habilidade de herói/monstro (cooldowns, aceleração de animação, knockback, etc.) e onde ele mora no código — nasce junto com o Hero Design Document, serve de guia de migração pro XML/JSON futuro |
| **Chest & Card Document** | Pools de carta, curva de bônus, chance de Mimic |
| **Quest Document** | Progresso das 3 linhas |
| **UI/UX Document** | Layout final de HUD, loja, telas, Settings |
| **Technical Architecture Document** | Implementação de Simulation Rings/Distance Despawn, Safe Zone Validator, Distributed Loot Access, LastCompletedDay/NextDay |
| **Production Roadmap** | Sprints, milestones, estratégia de produção do mundo procedural (vertical slice primeiro — Seção 25) |

---

## 52. Casos-Limite

### Menu / Save
| Caso | Resultado esperado |
|---|---|
| Continue Game sem save existente | Botão fica desabilitado — não é pendência de sistema complexo, apenas UI simples |
| New Game com save de run já existente | Não exige confirmação/modal obrigatória — inicia a nova run diretamente. O save antigo continua existindo até a nova run alcançar seu primeiro autosave (linha abaixo); uma confirmação de UX é decisão de interface futura, não requisito do GDD |
| Nova run iniciada e encerrada/fechada antes do primeiro autosave da Loja — por exemplo, o jogador fecha o jogo ou recebe Game Over no Modo Padrão | O save anterior continua intacto porque nenhum novo autosave ocorreu (Seção 43) |
| Continue Game após o cenário acima | Carrega normalmente a run antiga, pois ela nunca foi substituída |
| Game Over de uma run já salva (Modo Padrão — o Free não tem Game Over por demanda, Seção 42) | O checkpoint da última Loja permanece; Continue Game permite tentar o mesmo dia novamente (Seção 43) |
| Vitória no Dia 15 → Menu Principal | Save continua sendo a Loja que precede o Dia 15 — o Dia 15 pode ser jogado de novo via Continue Game (Seção 43) |
| Vitória no Dia 15 → Continuar | Resultados → Loja → novo autosave → checkpoint passa a ser a Loja que precede o Dia 16 |
| Dia 30 concluído | Save continua sendo a última Loja salva antes do Dia 30; sem autosave adicional após o encerramento |
| Continue Game | Nunca pede modo/herói/mapa/`WorldSeed` novamente (revisão estrutural — antes "Floor Variants") — lê o Mode salvo no RunState |
| Save corrompido | Pendência técnica |
| Fechar o jogo na Loja antes de "Start Day N" | Já salvo |
| Fechar o jogo durante o dia | Progresso desde a última Loja é perdido — ao reabrir, volta ao checkpoint |

### Player
| Caso | Resultado esperado |
|---|---|
| Morrer durante a Ultimate | Interrompida imediatamente; Energia zera (Seção 11) |
| Morrer durante transformação (Druid) | **Sem** conversão proporcional — fluxo padrão de morte, retorna em forma humana com vida cheia (Seção 17.4) |
| Morrer com pet permanente ativo | Pet retorna junto; reanimação da animação ainda 🟡 |
| Morrer com summon temporário ativo (Necromancer) | Destruídos — regra padrão, sem exceção |
| Morrer durante interação | Interação cancelada |
| Tempo zera exatamente durante a resolução da penalidade de morte | Segue a ordem estrita da Seção 11: subtrai os 30s, só então verifica se o tempo acabou |
| Jogo pausado durante habilidade em andamento | Congela, não cancela; retoma ao despausar |

### Mundo Único / Safe Zone / Boss Timer / Supressão de Ameaça
*(substitui a antiga tabela "Floors / Escadas / Boss Timer / Floor Variants" — revisão estrutural)*
| Caso | Resultado esperado |
|---|---|
| Suprimir Threat Tier 6–10 | Bloqueado — nunca suprimíveis (Seção 27) |
| Tentar uma 6ª compra de Supressão de Ameaça na mesma run | Bloqueado — máximo 5 |
| Tentar escolher manualmente qual Tier suprimir | Não existe essa opção — sempre suprime o Tier mais baixo ainda ativo |
| Jogador tenta nascer monstro/boss dentro da Zona Segura | Bloqueado na validação espacial — nunca nasce ali, mas pode entrar/perseguir normalmente se já existia fora (Seção 23) |
| Jogador se afasta além do que qualquer `EffectiveSpawnDay` (até 30) alcançaria | Sem Dia 31 pra consultar — a % de Threat Tier 10 continua subindo direto com a distância (Seção 23) |
| Controle Remoto em cooldown | Ação bloqueada |
| Boss Timer ao se mover pelo mundo / morrer / voltar ao centro | Continua acumulado, não reseta (Seção 22) |
| Boss anterior ainda vivo quando o timer completa de novo | Nasce um segundo boss em cima dele — empilham, sem fila nem espera (Seção 22) |
| Boss removido por Distance Despawn | Repõe a vaga imediatamente, recalculando o Tier pela posição atual do jogador — não é morte, não reseta o Boss Timer (Seção 22) |
| Boss derrotado de verdade | Sem reposição automática — a próxima entrada depende só do Boss Timer normal (Seção 22) |
| Mesmo `BossDefinition` já usado hoje | Não pode nascer de novo até o próximo `ActualDay` (`UsedBossIdsToday`, Seção 22) |
| Monstro removido por Distance Despawn | Não é morte — sem loot/kill/Energia; abre 1 vaga reposta no Spawn Annulus ao redor do jogador atual (Seção 23) |

### Inventário / Loot / Employees
| Caso | Resultado esperado |
|---|---|
| Pilha maior que o espaço disponível | Coleta parcial |
| Fim de dia com loot não vendido | Destruído |
| Um abate droppa múltiplos materiais diferentes | Esperado — cada material rola independentemente (Seção 38) |
| Ajudante sem monstro no alcance | 🟡 não definido |
| Alvo do ajudante morre antes do ataque concluir | 🟡 não definido |
| Coletor buscando loot longe do jogador/fora da tela | Funciona normalmente (Seção 36), sem exigir simulação individual completa |
| Baú é um Chest Mimic | Ataca ao ser aberto; ao morrer, libera a mesma recompensa que o baú normal daria e abre a mesma UI de 3 opções (Seção 30) |

### Gambler (Visão Expandida)
| Caso | Resultado esperado |
|---|---|
| Vida acima de 100% para ativar a Carta de Diamante | 🟡 Pendência de design — não inventar overheal |

---

## 53. Pendências Abertas

🟡 **Pendências localizadas de design/implementação — não reabrem sistemas já definidos e não impedem o início do desenvolvimento:**
- Reanimação da animação de summon de pets permanentes ao retornar de uma morte.
- Ajudante sem alvo no alcance / alvo morre antes do ataque concluir.
- Tecla de pause/menu geral.
- Condição exata que ativaria a Carta de Diamante do Gambler acima de 100% de vida.
- Condição de desbloqueio de Demonologist, Necromancer, The Gambler, Plague Doctor.
- **Rogue (Seção 17.5):** definir se "completar 30 dias" exige uma única run ou pode ser acumulado entre runs.
- **Assassin (Seção 17.9) — pendência nova desta revisão estrutural:** o critério antigo ("alcançar o Andar 6") não existe mais; precisa de um critério equivalente no mundo único (candidato: distância do centro onde `EffectiveSpawnDay` chega a 6 — Seção 23).
- **Inventário (Seção 37):** drag entre stacks iguais — comportamento de merge ainda não definido.
- **Inventário (Seção 37):** merge parcial quando o destino não comporta a stack inteira ainda não definido.
- **Inventário (Seção 37):** drag para slot ocupado por item diferente — swap/bloqueio ainda não definido.
- **Inventário (Seção 37):** clique direito — quantidade efetivamente descartada ainda não definida.
- **Magnet (Seção 29):** utiliza o Filtro de Bag do jogador para decidir quais tipos de loot vender automaticamente, ignora esse filtro, ou terá uma regra própria de filtro?
- **Efeitos persistentes do herói e Simulation Rings (Seção 24) — pendência nova desta revisão:** rastro de fogo do Mage, facas do Ranger e equivalentes, deixados atrás ao se afastar — seguem a mesma regra de simulação seletiva por distância dos monstros (pausam/são recolhidos), ou continuam ativos indefinidamente até a duração acabar, independente da distância? Substitui a antiga pendência "Combat / Floor Transition".

🔢 **Balanceamento:**
Preços de Bonuses (incluindo as 3 compras de slots corrigidas e o novo Increase Pickup Radius), preço dos rerolls extras, cooldown do Controle Remoto, Population (Minimum/Target/Maximum e frequência), curva de bônus de carta por Threat Tier, desbloqueio numérico do Mage e do Blood Mage, intervalo do Boss Timer (referência anterior 10s, agora global — Seção 22), curvas de Attack Speed por família de fonte, % de chance de Mimic, proporções de orçamento ofensivo de pets/summons, multiplicador de Vida Máxima da forma de urso do Druid, valor base do Pickup Radius e curva/preços de seus upgrades (Seção 37). **Novos desta revisão (Seção 23/25):** `SafeZoneRadius`/`SpawnSafetyPadding`, `DistancePerSpawnDay`, `EnemyDespawnDistance`/`BossDespawnDistance`, raios do Spawn Annulus, curva de crescimento de Threat Tier 10 além do que a tabela de 30 dias alcançaria por distância, e todos os parâmetros de geração procedural do mundo (`ChunkSize`, `BiomeBlendWidth`, `NoiseScale`, etc. — pertencem ao World Generation Document, Seção 51, não a este índice).

---

### GDD Mestre — Structural Freeze ✅

**A máquina de estados dos modos, Save, Core Loop, Mundo Único (Seção 24/25), Sistema de População/Spawn (Seção 23), Boss Timer (Seção 22), progressão de run, Inventory, Employees, Quests, Chest/Card flow e principais regras de herói estão estruturalmente fechados.** As pendências restantes listadas acima são localizadas e devem ser resolvidas nos documentos especializados (Seção 51) ou em playtest, sem exigir nova auditoria geral deste GDD.

Este documento só deve receber nova versão se: *(1)* o designer mudar uma regra estrutural; *(2)* uma pendência estrutural existente for decidida e precisar refletir no GDD Mestre; *(3)* surgir durante a implementação uma contradição real de gameplay. Não versionar novamente por wording, detalhe técnico, balanceamento, UI fina ou implementação — essas informações vão aos documentos especializados (Hero Design, Combat System, World Generation, Employee System, Bestiary, Economy & Balance, Chest & Card, Quest, UI/UX, Technical Architecture, Production Roadmap).

**Nota de versão — revisão estrutural (mundo único, ver `docs/new/`):** esta é exatamente a situação prevista no gatilho *(1)* acima — o designer mudou uma regra estrutural central (Floor System → Mundo Único, Seção 24/25; Boss Timer e População, Seção 22/23; Remove Tower Layer → Supressão de Ameaça, Seção 27; `CurrentPhase`/Dia-Noite como mecânica nova, Seção 40). Todas as seções tocadas mantiveram o próprio número (só o conteúdo mudou), então toda referência cruzada ("Seção 24", "Seção 27" etc.) no resto do documento continua válida sem precisar de correção individual.
