# Catálogo futuro: 30 itens, 30 atributos e upgrades

Data: 2026-11-24. Tabela do usuário, para as etapas seguintes à 4 (especializações e tags, ver
`2026-11-24-etapa-4-especializacoes-tags-design.md`). Ainda não foi implementada.

A coluna **Tag** é uma proposta do Claude, a confirmar com o usuário. "—" quer dizer que a carta aparece
para todos. A carta exige uma das tags da lista.

## Upgrades

São as cartas mais comuns: 60% de chance por carta no level up, com no máximo 2 itens por oferta.

- **Gerais:**
  - "aumento do dano": aumenta o dano das 3 habilidades ao mesmo tempo;
  - "aumento da vida".
- **Específicos de uma habilidade:**
  - "diminuição do cooldown do Shift";
  - "velocidade na animação da Ultimate";
  - "aumento dos projéteis do Primary Attack".
- **De pets e passivas:** cura, escudos e pets.
- **De especialização:**
  - Para o Barbarian Terremoto (tag Terremoto), baseados nos perks antigos que ainda estão no código:
    - Fenda Permanente (#29): o Ground Crack é infinito enquanto o Barbarian estiver perto;
    - Fenda Enfurecida (#27);
    - Fenda Selvagem (#28);
    - Ground Crack criado pela Ultimate.
  - Upgrades de nível máximo: a ideia ainda está aberta. A carta evolui, vira outra ou ganha um efeito extra?

## Itens

### Ofensivos

| # | Item | Efeito | Tag |
|---|---|---|---|
| 1 | Sovereign's Bane | Ataques primários causam dano adicional baseado em % da vida máxima do inimigo. Especialmente útil contra bosses. | — |
| 2 | Echo Fang | A cada X ataques primários, repete parte do dano do último ataque sem uma nova animação. | — |
| 3 | Executioner's Mark | Dano adicional contra inimigos com menos de 30% da vida. | — |
| 4 | Crimson Pact | Recupera vida com base em % do dano direto causado. | — |
| 5 | Razor Crown | Aumenta o multiplicador de dano crítico. | — |
| 6 | Primal Bond | Aumenta o dano, a velocidade de ataque e o tamanho dos pets. | Pet |
| 7 | Shattered Fortune | Críticos diretos têm chance de causar um segundo impacto no mesmo inimigo, igual a % do dano crítico original. | — |
| 8 | Wellspring Chalice | Usar o Survival recupera % da vida máxima, com cooldown próprio. | — |

### DOT e efeitos de status

| # | Item | Efeito | Tag |
|---|---|---|---|
| 9 | Blight Crucible | Bleeding, Fire e Poison acumulam até 3 aplicações de cada tipo no mesmo inimigo. Cada aplicação tem a sua própria duração, sem somar. | DOT |
| 10 | Ashen Hourglass | Aumenta a duração dos DOTs aplicados. | DOT |
| 11 | Plague Herald | Inimigo que morre com DOT pode espalhar o mesmo efeito para inimigos próximos. | DOT |
| 12 | Withering Eye | Inimigos com DOT recebem dano direto adicional das habilidades. | DOT |
| 13 | Cursed Diadem | Os ticks de DOT têm chance própria de crítico. | DOT |
| 14 | Affliction Heart | A cada N ticks de DOT, uma pequena explosão atinge os inimigos próximos ao alvo. | DOT |

### Defensivos e de resistência

| # | Item | Efeito | Tag |
|---|---|---|---|
| 15 | Thornwake Armor | Ao receber dano direto, lança espinhos em várias direções. A quantidade cresce com o rank e o dano é proporcional à vida máxima do herói. Não aplica DOT. | Tank |
| 16 | Colossus Heart | Aumenta o tamanho do personagem e o alcance e a área dos ataques. | Tamanho |
| 17 | Ironroot Core | Aumenta a vida máxima. | — |
| 18 | Everliving Heart | Regenera % da vida máxima por segundo. | — |
| 19 | Mender's Seal | Aumenta a efetividade das curas e dos escudos recebidos. | Cura, Escudo |
| 20 | Last Ember | Uma vez por dia, impede uma morte e dá uma breve proteção. | — |
| 21 | Gravebound Plate | Quanto mais inimigos próximos, maior a resistência, até um limite. | Tank |

### Projéteis

| # | Item | Efeito | Tag |
|---|---|---|---|
| 22 | Predator's Eye | Projéteis compatíveis perseguem inimigos. Se matarem o alvo e ainda tiverem dano disponível, perseguem outro, até esgotar o dano ou atingir seus limites. | Projétil |
| 23 | Shattered Prism | Certos impactos de projéteis geram projéteis menores em direções adicionais. | Projétil, Hitscan |
| 24 | Gravity Core | Aumenta o tamanho e a área de colisão dos projéteis. | Projétil |
| 25 | Windpiercer Charm | Aumenta a velocidade e a distância máxima dos projéteis. | Projétil |

### Mobilidade, energia e utilidade

| # | Item | Efeito | Tag |
|---|---|---|---|
| 26 | Stormstep Boots | Aumenta a velocidade de movimento. | — |
| 27 | Reaper's Feast | Ao matar monstros, recupera uma pequena % da vida máxima. | — |
| 28 | Soul Furnace | Aumenta a energia ganha nos abates, para usar a Ultimate mais vezes. | — |
| 29 | Gilded Hand | Aumenta o raio de coleta de Gold e o Gold recebido dos monstros. | — |
| 30 | Deadeye Lens | Aumenta a chance de crítico de todos os ataques diretos: Primary, Survival e Ultimate. | — |

## Atributos

### Evolução e crescimento

| # | Atributo | Efeito | Tag |
|---|---|---|---|
| 1 | Titan's Tribute | A cada 25 monstros mortos, +1% de vida máxima e +1% de tamanho, até +50% de cada. A porcentagem é calculada sobre a vida máxima **atual**, então acompanha a vida conquistada depois (50% de 50 de vida = 25; 50% de 500 = 250). | Tamanho |
| 2 | Trophy Hunter | Cada boss derrotado dá +5% de dano contra bosses até o fim da run, até um limite configurável. | — |
| 3 | Endless Hunger | A cada 30 monstros mortos, +1% de dano permanente na run, até +25%. | — |
| 4 | Golden Momentum | Coletar itens aumenta temporariamente a velocidade de movimento. Coletas seguidas renovam o efeito. | — |
| 5 | Survivor's Legacy | Cada boss derrotado aumenta a geração de energia da Ultimate na run, até um limite. | — |
| 6 | Dangerous Bargain | +30% de experiência, mas +15% de dano recebido de todas as fontes. | — |

### Combate ofensivo

| # | Atributo | Efeito | Tag |
|---|---|---|---|
| 7 | Relentless Momentum | Depois de se mover por alguns segundos, o próximo Primary Attack ganha bônus de dano e área. | — |
| 8 | Predator's Focus | Ataques primários seguidos no mesmo inimigo aumentam aos poucos o dano nele. Ideal contra bosses. | — |
| 9 | Critical Fever | Críticos dão velocidade de movimento e dano, acumulando até um limite. | — |
| 10 | Crowd Breaker | Acertar 3 ou mais monstros com um ataque primário aumenta o dano do próximo. | — |
| 11 | Blood Frenzy | Abater um inimigo com menos de 40% da vida dá bônus temporário de dano e de velocidade de movimento. | — |
| 12 | Death's Echo | Inimigos mortos por DOT explodem com pouco dano, podendo espalhar o próprio DOT. | DOT |
| 13 | Overkill | Quando um ataque direto passa do dano necessário para matar, parte do excesso vai para um inimigo próximo, sem cadeia. | — |

### Sobrevivência

| # | Atributo | Efeito | Tag |
|---|---|---|---|
| 14 | Final Stand | Com menos de 30% da vida, ganha resistência e dano enquanto continuar assim. | — |
| 15 | Scars of Battle | Receber dano direto aumenta a resistência por um tempo, acumulando até um limite. | Tank |
| 16 | Unbroken | Derrotar 15 inimigos seguidos sem receber dano dá um escudo temporário. | — |
| 17 | Adrenaline Rush | Com menos de 25% da vida, ganha velocidade de movimento por um tempo. | — |
| 18 | Shield Battery | Quando um escudo quebra, parte do dano que ele absorveu vira energia da Ultimate. | Escudo |
| 19 | Warrior's Feast | Derrotar um boss recupera % da vida máxima e dá um escudo temporário. | — |
| 20 | Moving Fortress | Depois de alguns segundos se movendo sem parar, ganha redução de dano enquanto se move. | — |

### Interações com habilidades

| # | Atributo | Efeito | Tag |
|---|---|---|---|
| 21 | Ricochet Fate | Projéteis do Primary, do Survival ou da Ultimate podem ricochetear para outro inimigo depois de acertar, até um limite de ricochetes. | Projétil, Hitscan |
| 22 | Affliction Harvest | A cada N ticks de DOT aplicados, ganha energia da Ultimate. | DOT |
| 23 | Elemental Supremacy | Ataques primários causam mais dano em inimigos com DOT. | DOT |
| 24 | Relentless Casting | Acertos diretos do Primary Attack reduzem um pouco o cooldown restante do Survival, com limite por segundo. | — |
| 25 | Spirit Link | Usar o Survival dá um pouco de energia da Ultimate. | — |
| 26 | Final Judgment | A Ultimate causa dano adicional contra bosses e mini bosses. | — |

### Economia e oportunidades

| # | Atributo | Efeito | Tag |
|---|---|---|---|
| 27 | Collector's Instinct | Todo item que cair dos monstros é coletado automaticamente, a qualquer distância. | — |
| 28 | Merchant's Whisper | Aumenta a chance de o mercador aparecer e dá uma atualização a mais do estoque por visita. | — |
| 29 | Greedbound Soul | A cada 25 monstros mortos, uma recompensa extra de Gold permanente. | — |
| 30 | Treasure Sense | Aumenta a chance de aparição de baús e dá uma atualização a mais do estoque por visita. | — |

## Pontos levantados na conversa (a resolver antes de implementar)

1. **Cartas repetidas:**
   - Plague Herald (item 11) é quase igual a Death's Echo (atributo 12).
   - Withering Eye (item 12) é quase igual a Elemental Supremacy (atributo 23).
   - Everliving Heart já existe como Troll Heart.
   - Stormstep Boots já existe como Swift Boots.
   - Ironroot Core, o Stat Vitality e o upgrade geral "aumento da vida" se sobrepõem.
   - Cada par precisa ser fundido ou diferenciado.
2. **Coisas que a run de 25 min não tem mais:**
   - Last Ember diz "uma vez por dia", mas não há mais dias. Pode virar uma vez por run ou a cada X minutos.
   - Golden Momentum e Collector's Instinct falam de itens que caem dos monstros, mas o loot está desligado. Hoje só cai XP.
   - Gilded Hand e Greedbound Soul dão Gold, mas o Gold não compra nada na run.
   - Treasure Sense fala em "atualização de estoque", texto que parece copiado do Merchant's Whisper.
3. **Sistemas novos que as cartas exigem:**
   - multiplicador de crítico;
   - pilhas de DOT com duração individual;
   - projéteis que perseguem, se dividem ou ricocheteiam;
   - escala de tamanho do herói;
   - escudo genérico;
   - contadores por boss e por abate.

   Cerca de 40 das 60 cartas dependem de algum deles. A proposta é implementar em lotes: primeiro as
   cartas que só mexem em números, depois um sistema novo por vez.
4. **O que já existe e pode ser aproveitado:**
   - chance de crítico (`PrimaryCritChance`, `UltimateCritChance`);
   - status Bleeding, Fire e Poison (hoje sem pilhas: aplicar de novo só renova a duração);
   - energia da Ultimate;
   - Gold por abate.
