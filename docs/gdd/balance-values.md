# Valores de Calibragem — Habilidades de Herói e Monstro

> **Documento Especializado**, referenciado pelo GDD Mestre. Índice de todo valor `🔢` (placeholder de balanceamento, ainda não calibrado em playtest) que hoje vive espalhado em campos `[SerializeField]` no código — pensado pra achar rápido na hora de ajustar, sem precisar vasculhar script por script. **Não é o dado real** — o dado real continua sendo o campo no Inspector/prefab; isso aqui é só o índice de onde cada um mora e o valor com que nasceu. Quando os valores de herói/monstro virarem XML/JSON (decisão já tomada, ainda não implementada), este documento vira o guia de migração — cada linha já diz o nome do campo que precisa virar uma entrada nesse arquivo.

**Como manter:** toda vez que um campo novo nascer marcado `🔢` num script de herói/monstro, adiciona uma linha aqui. Não precisa esperar "fechar o sistema" — o ponto é nunca perder o rastro de qual número mora onde.

---

## Heróis — `HeroController.cs` (compartilhado por todo herói)

| Campo | Valor atual | O que controla |
|---|---|---|
| `damageSpeedStepPerHit` | `0.5` | Quanto a animação de `Damage` acelera a cada hit recebido enquanto já está reagindo a um anterior (Seção 16). |
| `maxDamageSpeedMultiplier` | `3` | Teto de aceleração do `Damage` — acima disso, pula a animação e devolve o controle na hora. |
| `damageReactionDuration` | `0.5s` | Duração de referência (1x) do clipe de `Damage` — usada pro timer que decide quando a reação "acabou sozinha". Precisa bater com a duração real do clipe de cada herói; hoje calibrado pro Barbarian. |
| `ultimateEnergyLockoutDuration` | `2s` | Janela pós-ultimate sem ganho de Energia — evita ultimate se autoalimentar matando monstros com o próprio dano dela. |
| `maxDieDuration` | `3s` | Timeout de segurança se `AnimationDieEndEvent` nunca disparar (clipe sem o evento configurado). |

## Barbarian — `Barbarian.cs` (GDD Seção 17.1)

| Campo | Valor atual | O que controla |
|---|---|---|
| `knockbackForce` | `4` | Força do knockback aplicado pelo golpe primário. |
| `ultimateDamageMultiplier` | `2` | Multiplicador sobre `stats.damage` (já com a passiva aplicada) pra cada um dos 8 projéteis da ultimate — GDD: "2x o dano atual da arma". |
| `maxActionDuration` | `3s` | Timeout de segurança se o Animation Event de fim (`Attack` ou `Ultimate`) nunca disparar. |
| `groundCrackDuration` | `3s` | 🟡 Provavelmente vestigial — a rachadura no chão foi unificada com a própria animação de ataque (decisão do usuário, Sprint 17); confirmar se o campo/prefab ainda tem uso antes de mexer. |
| *(hardcoded, não é campo)* `GetPassiveDamageMultiplier()` | +25% de dano a cada 10% de Vida Máxima perdida, teto +200% aos 80%+ perdidos | Passiva de dano por vida perdida (GDD Seção 17.1). Constantes fixas dentro do método (`0.25f` por degrau, 8 degraus de teto) — ainda não expostas como `[SerializeField]`. |

## `HeroProjectile.cs` (compartilhado — Barbarian hoje, Ranger/Paladin/Blood Mage quando chegar a vez)

| Campo | Valor atual | O que controla |
|---|---|---|
| `speed` | `10` | Velocidade de voo do projétil. |
| `maxDistance` | `8` | Alcance máximo antes de desaparecer, mesmo com reserva de dano sobrando. |
| `knockbackForce` | `4` | Força do knockback aplicado a quem o projétil acerta. |

## Monstros — `EnemyController.cs` (compartilhado por todo Melee/Ranged comum)

| Campo | Valor atual | O que controla |
|---|---|---|
| `attackSpeedStepPerHit` | `0.5` | Quanto a animação de `Attack` do monstro acelera a cada hit recebido durante o próprio golpe (Bestiário, regra revisada na Sprint 17 — antes só um flash). |
| `maxAttackSpeedMultiplier` | `3` | Teto de aceleração do `Attack` — acima disso, o golpe resolve na hora, sem esperar a animação. |
| `maxAttackAnimationDuration` | `5s` | Timeout de segurança se `AnimationAttackEndEvent` nunca disparar. |
| `maxDieDuration` | `3s` | Mesmo timeout de segurança do herói, lado do monstro. |
| `KnockbackDecay` (const, não editável no Inspector) | `8` | Velocidade com que o knockback recebido de um herói perde força — hoje só ajustável direto no código. |

## GDD — valores citados em prosa que ainda não têm campo/código associado

Estes são só o número já escrito no GDD, sem implementação ainda (aparecem como `🔢` na Seção correspondente) — trazidos pra cá só pra não duplicar o valor errado quando a implementação chegar:

| Onde no GDD | Valor de referência | Contexto |
|---|---|---|
| Seção 33 (Lifesteal, Blood Mage) | 10% do dano como cura, teto de 3% da Vida Máxima por hit | Ainda sem campo no código — Blood Mage não foi implementado. |
| Seção 16 (Knockback) | Sem valor ainda | "Se varia por herói" continua em aberto — hoje cada herói/projétil tem seu próprio `knockbackForce` solto (ver tabelas acima), sem uma regra geral de escala. |
| Seção 32 (Attack Speed multi-fonte) | Sem curva definida | Cada família de fonte (primário, passiva periódica, summon, orbital) precisa do próprio coeficiente — nenhum ainda calibrado. |

---

**Nada aqui foi testado em profundidade** — são os valores com que cada sistema nasceu na Sprint 17, ajustáveis a qualquer momento direto no Inspector sem precisar mexer em código.
