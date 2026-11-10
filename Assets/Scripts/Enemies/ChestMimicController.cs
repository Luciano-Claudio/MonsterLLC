using UnityEngine;

// Chest Mimic (GDD Deadline 8.1, Seção 8) — nasce disfarçado de baú fechado e vira monstro ao
// ativar. Diferente da minha 1ª tentativa (ficou disfarce+Activate reinventados na mão aqui),
// o mecanismo de "disfarçado até detectar" já existe pronto em EnemyController
// (staysDormantUntilDetected — mesmo campo que Gargoyle/todo Skeleton usam), com o próprio
// Animator cuidando do visual (estado "Idle" dormente -> "Activate" -> Walk/idleCombat, tudo
// isso já tem até um bugfix de mira congelada durante o Activate). Então este arquivo não
// precisa reimplementar NADA disso — só:
// 1. marcar staysDormantUntilDetected=true no Inspector do prefab (não dá pra setar daqui,
//    o campo é private na base — é só um checkbox a marcar na hora de montar o prefab);
// 2. aplicar os stats do dia (ChestMimicStatTableSO) na hora que o objeto nasce;
// 3. ChestMimicInteractable (componente irmão) chama ForceImmediateAggro() direto no clique —
//    isso sozinho já dispara Idle->Activate no Animator (a transição é literalmente
//    "InCombat == true", e ForceImmediateAggro() liga esse bool).
// Herda 100% de SlimeEnemyController ("dano body apenas", sem animação de ataque própria,
// sempre persegue direto) — mesmo motivo de sempre.
public class ChestMimicController : SlimeEnemyController
{
    [Header("Stats por dia (Seção 8 — \"o mais forte do Tier do dia\", manual)")]
    [SerializeField] private ChestMimicStatTableSO statTable;

    private bool rewardGranted;

    // Achado real do usuário: o Mimic estava "ganhando target" igual todo outro monstro —
    // tanto por proximidade (observationRadius, igual Gargoyle/Skeleton normal) quanto por
    // contágio de Aggro Global (se QUALQUER outro monstro do mapa detectasse o player, o
    // Mimic virava monstro de verdade junto, mesmo o player nunca tendo ido perto do baú).
    // Isso contraria a própria identidade do Mimic — ele só deve virar monstro quando o
    // player clica pra abrir (ChestMimicInteractable.ForceImmediateAggro(), gatilho explícito,
    // continua funcionando normalmente).
    protected override bool CanAutoDetectPlayer => false;

    protected override void Awake()
    {
        base.Awake();

        if (statTable == null) return;
        var entry = statTable.GetDay(MainMenuUI.CurrentRun?.day ?? 1);
        if (entry == null) return;

        stats.health = entry.health;
        stats.maxHealth = entry.health;
        stats.attackDamage = entry.attackDamage;
    }

    // Decisão do usuário (2026-11-08): morrer dá a MESMA recompensa que abrir um Chest normal
    // (ChestRewardSystem — fonte única). Nunca dropa loot próprio (dropsLoot fica 0 no prefab).
    // Guard próprio antes do base, mesmo padrão de MotherSlimeController/OrcRiderController —
    // o dieHandled da base evita Destroy/loot 2x, mas cada override precisa da própria trava
    // pra não conceder a recompensa 2x (evento real + timeout de segurança).
    public override void AnimationDieEndEvent()
    {
        if (rewardGranted) return;
        rewardGranted = true;

        ChestRewardSystem.GrantReward();
        base.AnimationDieEndEvent();
    }
}
