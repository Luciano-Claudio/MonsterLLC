# Loja de melhorias permanentes, Gold Bag e Stat Item — design

Data: 2026-11-26. Pedido do usuário (brainstorming de 2026-11-26). Vem depois do gold permanente
(`PermanentGold`, commit eb990cf2) e antes das animações dos itens.

## Por que

- O jogador gasta o gold permanente (moeda de 10% por monstro) em melhorias **por herói**. Isso faz
  ele querer evoluir todos os heróis, e prepara uma futura conquista de "100% de um herói".
- O botão de **vender tudo** devolve o gold. Assim, quem enjoar de um herói passa o gold para outro.

## Objetivos

1. **Loja por herói** no menu: Heros_Painel → card do herói → Select_Confirm (Play / Upgrade) →
   Stats_Painel com as melhorias daquele herói.
2. **Comprar, vender tudo e desligar:** comprar 1 nível por vez, vender tudo devolve 100%, e uma
   melhoria completa pode ser desligada pelo Toggle "Active" sem vender.
3. **Efeito na run:** as melhorias compradas valem na run daquele herói.
4. **Dois loots novos de boss:**
   - **Gold Bag:** 25 gold. Cai sempre de boss e de mini boss.
   - **Stat Item:** cai só de boss, com 10% de chance (até 50% com Luck). Coletado, abre a tela de
     cartas com 3 itens, 3 upgrades ou 3 atributos.

## Fora do escopo

- Melhorias ligadas a especialização (dano de DOT, área do terremoto, pets).
- Melhorias que valem para todos os heróis.
- Os outros 7 heróis: a loja é feita só para Barbarian, Ranger e Mage, como a alpha.
- Conquistas: só ficam possíveis no futuro.

## Anti-objetivos

- Comprar dentro da run (decisão de 2026-11-24: nenhuma compra na run).
- Perder gold: vender devolve tudo, e o arquivo de gold sobrevive à morte.

## Fluxo no menu (o usuário já montou a interface no Main_Menu)

1. Heros_Painel: clicar num card de herói abre o **Select_Confirm** (`Icon_Hero`, `Hero_Name`).
   - **Play_Game:** segue para o Map_Painel, como hoje.
   - **Upgrade:** desliga o Select_Confirm e abre o **Stats_Painel** daquele herói.
2. Stats_Painel:
   - `Hero_Image`: o 1º quadro do idle do herói, tirado do SpriteRenderer do prefab dele.
   - Texto do topo: **"Permanent Upgrades"** (era "Selecao de atributos").
   - Scroll View: mostra só o grupo do herói escolhido (`Content/Barbarian`, `Ranger` ou `Mage`).
   - **Selected:** o botão clicado fica selecionado até o jogador clicar em outro. Ao abrir o painel,
     o **Damage** já vem selecionado, porque o Background_Description sempre mostra uma melhoria.
   - **Background_Description:** mostra o ícone e o nome do botão selecionado, a descrição, o preço
     do próximo nível (`Value`) e o botão **Buy**.
   - **Completa:** quando todos os pips estão comprados, `Value` e `Buy` somem e aparece o Toggle
     **Active** (ligado = efeito vale; desligado = efeito não vale, sem vender).
   - **Pips:** cada botão tem exatamente o número de pips da melhoria. Ao comprar 1 nível, o pip
     desligado mais à esquerda acende, com o mesmo sprite "ligado" do Toggle do Background_Description.
   - **Sell_Everything:** texto **"Refund All Upgrades"** (era "Vender todos os aprimoramentos").
     Vende tudo daquele herói e devolve 100% do gold gasto.
   - **Voltar:** `Back_To_Hero_Painel` ou **Esc** voltam ao Heros_Painel.
   - **Sem gold:** o Buy fica desabilitado (não clicável) quando o gold não cobre o preço.

## As melhorias

Os pips são o nível máximo. Preços por nível (do usuário): o preço do nível N é o N-ésimo valor da
sequência do grupo.

| Grupo de preço | Sequência |
|---|---|
| A | 50 · 100 · 300 · 500 · 1000 |
| B | 100 · 200 · 500 · 1000 · 2000 |
| Maldição | 500 · 1000 · 2000 · 5000 · 10000 |
| Ressurreição | 10000 |

| Melhoria (botão) | Nome em jogo | Pips | Por nível | Preço |
|---|---|---|---|---|
| Damage_Button | Damage | 5 | +10% de dano (até +50%) | A |
| Life_Button | Max Health | 3 | +10% de vida máxima (até +30%) | A |
| Defense_Button | Armor | 5 | +2% de redução de dano (até 10%, respeita o teto de 75%) | A |
| Life_Regen_Button | Regeneration | 3 | +0,1% da vida máxima por segundo | A |
| Move_Speed_Button | Move Speed | 3 | +5% de velocidade de movimento | A |
| Size_Button (Barbarian) | Size | 3 | +5% de tamanho | A |
| Multishoot_Button (Ranger, nome do usuário na cena) | Multishot | 5 | +1 flecha no primário | A |
| Area_Button (Mage) | Flame Area | 3 | +10% de área do primário | A |
| Cooldown_Button | Cooldown | 3 | −5% de recarga do Primário e do Shift (até −15%) | B |
| Ultimate_Generation_Button | Ultimate Charge | 3 | +10% de energia da Ultimate ganha | B |
| Ultimate_Energy_Button | Ultimate Cost | 3 | −5% da energia necessária para a Ultimate | B |
| Magnet_Button | Magnet | 3 | +20% de raio do ímã | B |
| Xp_Button | Growth | 5 | +5% de XP | B |
| Gold_Button | Greed | 5 | +10% de gold | B |
| Luck_Button | Luck | 4 | Stat Item +10% (10% → 50%); Reroll_Dice +25% da chance base (dobra no 4); +10% de chance de 1 baú extra e de 1 vendedor extra na run | B |
| Reroll_Button | Reroll | 5 | +1 reroll no começo da run (1 → 6) | B |
| Curse_Button | Curse | 5 | ver abaixo | Maldição |
| Revival_Button | Revival | 1 | 1 morte evitada por run | Ressurreição |

**Maldição (Curse), por nível:**
- hordas +20% (até +100%);
- XP e gold +20% (até +100%);
- vida e velocidade do herói +20% (até +100%);
- vida, velocidade e dano dos inimigos +10% (até +50%).

O limite de 300 monstros comuns vivos (D9.3) continua valendo, então com a Maldição alta, à noite o
limite segura a horda.

**Ressurreição:** no golpe fatal, o herói não morre. Volta com **50% da vida** e fica **2 s
invulnerável**, 1 vez por run. O **Last Ember age primeiro**; a Ressurreição só age se ele estiver
em recarga (ou se o jogador não tiver a carta). O herói nunca entra no estado de morto, então a
proteção da animação de morte (herói morto nunca volta) continua valendo.

**XP e gold quebrados:** os bônus de XP e de gold (Growth, Greed e Maldição) acumulam a parte
fracionária entre coletas. Exemplo: com +10%, a cada 10 moedas de 1 gold sai 1 gold extra.

## Gold Bag e Stat Item

- **Prefabs** animados em loop, iguais ao XpGem e ao GoldCoin: `Gold_Bag.png` com 11 quadros e
  `Stat_Item.png` com 12. Coletados pelo mesmo sistema (`XpGemPool`): mesma coleta por distância,
  mesmo ímã. Collector's Instinct e Golden Momentum valem para os dois.
- **Gold Bag:** 25 gold (com os bônus de gold). Cai sempre de boss e de mini boss, junto das gemas.
- **Stat Item:** só de boss. Chance de 10% + 10% por nível de Luck (até 50%). Se cair, cai junto com
  a Gold Bag e as gemas.
- **Coletar o Stat Item** abre a tela de cartas (a mesma do level up, baú e vendedor, com 1 reroll):
  - 40%: 3 itens;
  - 40%: 3 upgrades;
  - 20%: 3 atributos.
  - As regras normais de cada tipo continuam valendo (6 espaços de item, máximo de 6 atributos,
    filtro por especialização). Se o tipo sorteado não tiver 3 cartas possíveis, completa com os
    outros tipos.

## Rerolls: recurso da run (mudança do usuário, 2026-11-27)

- O reroll deixa de ser "1 por tela". Vira um **estoque da run**: começa com **1** (+1 por nível da
  melhoria Reroll, até 6) e serve em **qualquer** tela de cartas (level up, baú, vendedor, Stat Item).
  O que não for usado continua no estoque até o fim da run.
- **Reroll_Dice** (loot novo, prefab animado como os outros, sprite `Reroll_Dice.png`): +1 reroll no
  estoque. Chance: **1%** em monstro comum, **5%** em mini boss e boss. Luck soma +25% da chance base
  por nível (Luck 4 = 2% e 10%). Coletado como as gemas.
- **Treasure Sense** (só no baú) e **Merchant's Whisper** (só no vendedor): o **1º reroll** daquela
  tela é grátis, não gasta o estoque. Continuam dando +1 aparição de baú/vendedor na run. As
  descrições das 2 cartas mudam para dizer isso.
- **Contador:** o `reroll_quantity` que o usuário criou no `Cards_Popup` mostra o estoque atual (e
  "grátis" não aparece no número: o botão de reroll fica clicável mesmo com estoque 0 quando o 1º
  reroll da tela é grátis).

## Save

O `profile.json` (hoje só com o gold) passa a guardar, por herói, o nível de cada melhoria e quais
estão desligadas no Toggle. Um save antigo, só com o gold, carrega com tudo no nível 0.

## Abordagens consideradas

1. **Definições em ScriptableObject + uma tela que lê a cena (escolhida):** um asset por melhoria
   (id, nome, descrição, ícone, pips, valor por nível, grupo de preço), numa lista por herói. A tela
   acha os botões da cena pelo nome (`Damage_Button` etc.), igual ao `MainMenuNavigationUI`, sem
   campos para arrastar. Números e textos ajustáveis no Inspector.
2. **Tudo no código, numa tabela:** mais rápido de escrever, mas para mudar um preço é preciso mexer
   no código. Descartada: o usuário ajusta valores no Unity.
3. **Gerar os botões por código:** descartada, porque o usuário já montou e estilizou os botões.

## Decisões minhas (o usuário pode corrigir)

- Nomes em jogo e descrições das melhorias (em inglês). Multishot é do usuário (mesmo nome da carta de upgrade, aceito por ele).
- Valores por nível que o usuário não fixou: Armor, Regeneration, Move Speed, Size, Flame Area,
  Cooldown, as 2 de Ultimate, Magnet, Growth e Greed.
- O ícone de cada melhoria é o que já está no botão da cena.
- Buy desabilitado sem gold suficiente.
- Se o tipo sorteado do Stat Item não tiver cartas suficientes, completa com os outros.

## Respondido pelo usuário

1. Cooldown afeta o **Primário e o Shift** (2026-11-26).

2. Os botões mostram os **nomes novos** da tabela (o código escreve o nome).
3. Velocidade +115% no máximo (Maldição + Move Speed) **está certa**.
4. Refund All Upgrades **vende direto**, sem confirmação.
5. Esc com o Select_Confirm aberto **fecha ele** e fica na lista de heróis; Esc no Stats_Painel volta
   à lista de heróis.
6. Pip apagado = o sprite que já está nos pips; comprado = o sprite "ligado" do Toggle Active.
7. Rerolls viram recurso da run (seção própria acima), com o Reroll_Dice e a melhoria Reroll.
8. Gold Bag e Stat Item ficam no chão como as gemas (ímã normal).
9. O Value mostra **só o número**.
10. Luck também aumenta um pouco a chance de baú e vendedor (valor é decisão minha, na tabela).
