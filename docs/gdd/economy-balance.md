# Economia & Balanceamento — Weapon Tiers e Materiais

> **Documento Especializado**, referenciado pelo GDD Mestre (Seção 19 — Upgrades de Arma — e Seção 39 — Economia e Materiais — que apontam pra cá). Fonte: documento de visão original do jogo, migrado para cá.

---

## 1. Fórmula estrutural (já congelada no GDD, Seção 11)

```text
Dano = Dano Base do Herói × Multiplicador da Arma × Participação da Fonte × Coeficiente × Bônus da Run
Vida Máxima = Vida Base × Multiplicador Percentual da Arma × Bônus da Run
```

Regras de implementação que valem para todo o sistema de arma:
- **Nunca armazenar a arma como "+X dano" absoluto.** O dado principal é o multiplicador percentual do tier.
- Dano Base pertence ao herói. Multiplicador da arma pertence ao equipamento. Coeficiente pertence à habilidade. As três camadas ficam sempre separadas.
- Bônus de Dano de cartas da run é aplicado **depois** do multiplicador da arma.
- Pets e summons recebem só a fração do orçamento ofensivo destinada a eles — nunca duplicar o Poder Ofensivo Total inteiro no herói e no pet ao mesmo tempo.
- Quantidade de projéteis/vinhas/balas/hits aumenta cobertura e/ou DPS por coeficientes próprios — não multiplica automaticamente o dano total pela contagem de objetos.
- Vida, shields e curas usam a Vida Máxima resultante do sistema percentual — nunca valores absolutos separados por tier.
- **Regra prática de ritmo:** uma arma ideal domina o andar anterior, é adequada pro andar-alvo, e ainda sofre no andar seguinte.

---

## 2. Os 15 Tiers (+ Arma Básica)

Todos os valores de "Dano de Referência" abaixo assumem um herói-modelo com **Dano Base 2, coeficiente 1,0, sem crítico, sem bônus de run** — servem só de régua comparável entre tiers, não são o dano real de nenhum herói específico.

| # | Tier | Mult. Dano | Bônus Dano equiv. | Dano Ref. (Base 2) | Bônus Vida | Preço | Requisito | Momento sugerido |
|---|---|---|---|---|---|---|---|---|
| 0 | Arma Básica | x1 | +0% | 2 | +0% | Grátis | Inicial | Dia 1 / Andar 1 |
| 1 | Copper | x2,5 | +150% | 5 | +50% | 500g | Arma Básica | Fim do Dia 1 / Andar 1 confortável |
| 2 | Iron | x5 | +400% | 10 | +200% | 1,5k | Copper | Andar 2 |
| 3 | Steel | x10 | +900% | 20 | +500% | 5k | Iron | Andar 2 confortável / entrada do 3 |
| 4 | Silver | x20 | +1.900% | 40 | +1.000% | 20k | Steel | Andar 3 |
| 5 | Sapphire | x50 | +4.900% | 100 | +2.500% | 80k | Silver | Entrada/progressão do Andar 4 |
| 6 | Emerald | x125 | +12.400% | 250 | +5.000% | 350k | Sapphire | Andar 4 confortável / preparação do 5 |
| 7 | Amethyst | x500 | +49.900% | 1.000 | +50.000% | 2m | Emerald | Andar 5 |
| 8 | Gold | x10k | +999,9k% | 20.000 | +1m% | 10m | Amethyst | Andar 6 |
| 9 | Ruby | x500k | +50m% | 1m | +80m% | 100m | Gold | Andar 7 |
| 10 | Diamond | x150m | +15b% | 300m | +15b% | 5b | Ruby | Andar 8 |
| 11 | Arcane | x2,5b | +250b% | 5b | +3t% | 500b | Diamond | Fim do Andar 8 / preparação do 9 |
| 12 | Infernal | x40b | +4t% | 80b | +12t% | 5t | Arcane | Andar 9 — ainda exige combate |
| 13 | Nightmare | x200b | +20t% | 400b | +30t% | 50t | Infernal | Andar 9 avançado |
| 14 | Void | x1t | +100t% | 2t | +400t% | 500t | Nightmare | Entrada/progressão do Andar 10 |
| 15 | Divine | x6t | +600t% | 12t | +1.000t% | 5.000t | Void | Endgame / Divine God |

**Exemplos de nomenclatura por tier** (o prefixo é universal, a representação visual muda por herói): Copper Sword / Copper Staff / Copper Bow / Copper Daggers / Copper Hammer / Copper Pistols — e assim por diante pros outros 14 tiers, mesma lista de 6 variações (espada/cajado/arco/adagas/martelo/pistolas).

### Nota de balanceamento por tier (contexto, não regra)
- **Copper:** primeiro salto real — A1 cai pra 1-2 hits, A2 ainda exige 5-7.
- **Iron:** normaliza o A2 pra 3-4 hits.
- **Steel:** facilita A2; ao entrar no A3 (100-140 HP) volta pra 5-7 hits.
- **Silver:** deixa A3 confortável (3-4 hits).
- **Sapphire:** contra 500-700 HP do A4, ainda exige 5-7 hits.
- **Emerald:** reduz A4 pra 2-3 hits, prepara o salto do A5.
- **Amethyst:** primeiro tier realmente apropriado pro A5 — cartas/crítico/coeficientes passam a pesar muito.
- **Gold:** primeiro grande salto exponencial da progressão.
- **Ruby:** apropriado pra inimigos na casa dos milhões de HP.
- **Diamond:** entra na escala de centenas de milhões sem trivializar o A9.
- **Arcane:** muito forte no A8, mas ainda não resolve sozinho o A9.
- **Infernal:** na referência, ainda exige ~7-13 hits nos mobs do A9.
- **Nightmare:** domínio progressivo do A9, preparação pro A10.
- **Void:** primeira arma realmente apropriada pro A10; mobs divinos ainda exigem dezenas de hits equivalentes.
- **Divine:** mobs do A10 ficam em ~4-7 hits equivalentes; Divine God continua sendo uma luta longa.

**Confirmação de filosofia de endgame:** Infernal não encerra o jogo; Nightmare domina melhor o A9; Void entra no A10; Divine torna o A10 vencível sem trivializar mobs divinos ou o Divine God.

---

## 3. Validação da curva nos 4 primeiros andares

Mesmo herói-modelo (Dano Base 2, coeficiente 1,0, sem crítico, sem bônus de run):

| Situação | Vida inimigo | Arma | Dano ref. | Hits esperados |
|---|---|---|---|---|
| Andar 1 - início | 5-10 | Arma Básica (x1) | 2 | 3-5 hits |
| Andar 1 após upgrade | 5-10 | Copper (x2,5) | 5 | 1-2 hits |
| Andar 2 ao explorar | 25-35 | Copper (x2,5) | 5 | 5-7 hits |
| Andar 2 equipado | 25-35 | Iron (x5) | 10 | 3-4 hits |
| Andar 3 ao explorar | 100-140 | Steel (x10) | 20 | 5-7 hits |
| Andar 3 equipado | 100-140 | Silver (x20) | 40 | 3-4 hits |
| Andar 4 ao explorar | 500-700 | Sapphire (x50) | 100 | 5-7 hits |
| Andar 4 equipado | 500-700 | Emerald (x125) | 250 | 2-3 hits |

## 4. Referência econômica dos primeiros dias

A mudança pra multiplicadores percentuais não altera esses preços já aprovados. Objetivo: evitar vários dias de espera por uma arma necessária pra avançar.

| Compra | Preço | Referência de renda | Momento | Função |
|---|---|---|---|---|
| Copper | 500g | Dia 1: ~500g-1k | Fim do Dia 1 | Primeiro salto imediato |
| Iron | 1,5k | Dia 2 explorando A2 | Fim do Dia 2 | Normaliza A2 |
| Steel | 5k | Dia 3 com 200s | Fim do Dia 3 | Facilita A2 / abre A3 |
| Silver | 20k | Dia 4 explorando A3 | Fim do Dia 4 | Normaliza A3 |
| Sapphire | 80k | Dia 5 com 300s | Fim do Dia 5 | Permite entrar de verdade no A4 |
| Emerald | 350k | Farm do A4 | Após estabelecer-se no A4 | Transforma A4 em farm confortável |

---

## 5. Tabela de valores de venda dos 15 materiais

Escala monetária: `1kg = 1.000g` · `1mg = 1.000.000g` · `1bg = 1.000.000.000g` · `1tg = 1.000.000.000.000g`.

| # | Item | Valor de venda |
|---|---|---|
| 1 | Monster Essence | 1g |
| 2 | Monster Fragment | 3g |
| 3 | Spirit Dust | 4g |
| 4 | Arcane Shard | 8g |
| 5 | Dark Crystal | 10g |
| 6 | Soul Fragment | 40g |
| 7 | Corrupted Core | 70g |
| 8 | Elemental Shard | 300g |
| 9 | Ancient Fragment | 500g |
| 10 | Infernal Ash | 4kg |
| 11 | Chaos Crystal | 7kg |
| 12 | Nightmare Residue | 750kg |
| 13 | Void Shard | 5mg |
| 14 | Celestial Fragment | 10bg |
| 15 | Divine Core | 1tg |

**Regra de progressão:** o desbloqueio de um novo material não remove os anteriores do drop pool. Quanto mais alto o andar, maiores ficam principalmente as *quantidades* dos materiais básicos, enquanto materiais novos começam com chances muito baixas.

**Objetivo de escala:** builds fortes devem conseguir gerar milhões, bilhões e depois trilhões de recursos sem exigir milhares de kills por dia. Multiplicadores futuros de loot (cartas, bonuses) podem aumentar essas quantidades sem alterar a tabela-base por monstro.

### Representação visual de drops no chão (já em GDD Seção 38, reforçado aqui com os exemplos originais)

| Quantidade | Representação |
|---|---|
| 1 a 9 unidades | Cada unidade exibida separadamente |
| 10 a 49 unidades | Stacks visuais de 10 |
| 50 a 99 unidades | Stacks visuais de 50 |
| 100+ unidades | Um único stack com sprite de "100+", quantidade real armazenada internamente |

Exemplos: drop de 7 → 7 itens separados. Drop de 26 → 2 stacks de 10 + 6 separados. Drop de 75 → 1 stack de 50 + 2 stacks de 10 + 5 separados. Drop de 5.000 → 1 stack 100+ contendo 5.000 internamente (coletar pode adicionar milhões de uma vez ao inventário).

---

## 6. Attack Budget — valor de referência NÃO confirmado

O documento original menciona, em tom de sugestão ("eu colocaria um sistema que considero extremamente importante..."), o exemplo de **máximo de 6 melees atacando simultaneamente** como ilustração do conceito de Attack Budget. **Isso não é uma decisão travada** — confirmado explicitamente pelo designer como pendente de playtest, condicionado à mesma decisão de viabilidade de combate documentada no Bestiário (`docs/gdd/bestiary.md`, nota de encerramento). Não usar "6" como valor final em nenhuma implementação sem reconfirmação.
