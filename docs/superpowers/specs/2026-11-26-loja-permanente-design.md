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
| Multishot_Button (Ranger; hoje `+Arrow_Button`, renomeado na implementação) | Multishot | 5 | +1 flecha no primário | A |
| Area_Button (Mage) | Flame Area | 3 | +10% de área do primário | A |
| Cooldown_Button | Cooldown | 3 | −5% de recarga do Primário e do Shift (até −15%) | B |
| Ultimate_Generation_Button | Ultimate Charge | 3 | +10% de energia da Ultimate ganha | B |
| Ultimate_Energy_Button | Ultimate Cost | 3 | −5% da energia necessária para a Ultimate | B |
| Magnet_Button | Magnet | 3 | +20% de raio do ímã | B |
| Xp_Button | Growth | 5 | +5% de XP | B |
| Gold_Button | Greed | 5 | +10% de gold | B |
| Luck_Button | Luck | 4 | +10% de chance do Stat Item (10% → 50%) | B |
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

## Perguntas para o usuário (2026-11-27, antes do plano)

1. **Nomes nos botões:** o texto `Name_Permanent_Stat` de cada botão passa a mostrar o nome em jogo
   da tabela (ex.: "Armor" no Defense_Button, "Growth" no Xp_Button, "Greed" no Gold_Button)? Ou os
   botões ficam com os textos que você escreveu?
2. **Maldição + Move Speed:** com tudo no máximo, o herói fica com +115% de velocidade (+100% da
   Maldição e +15% do Move Speed). Está certo assim?
3. **Vender tudo sem confirmação:** como devolve 100%, um clique sem querer não perde nada, só
   desfaz as compras. Vende direto, ou abre um "Tem certeza?" antes (como o Panel_Confirm das
   cartas)?
4. **Esc no Select_Confirm:** com o Select_Confirm aberto, o Esc fecha ele e volta à lista de heróis?
5. **Sprite do pip apagado:** o pip comprado usa o sprite "ligado" do Toggle Active. O Toggle tem
   só `Background` e `Label`, sem `Checkmark`. Qual sprite é o pip apagado: o que já está nos pips
   hoje?
6. **Stat Item com 1 reroll:** a tela de cartas do Stat Item tem 1 reroll, como o level up?
7. **Stat Item e Gold Bag fora do ímã:** a Gold Bag (25 gold) e o Stat Item ficam no chão até o
   herói pegar (como as gemas), ou vão sozinhos até o herói ao cair, para ninguém perder o item do
   boss?
8. **Preço no Value:** o texto mostra só o número (ex.: "300"), com a imagem de moeda que já está
   ao lado?
