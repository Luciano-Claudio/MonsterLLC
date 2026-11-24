using UnityEngine;

// Agregador único dos 30 perks de "Mundo" (GDD Deadline 8.1, Seção 8) — recomputa TODOS os
// efeitos de uma vez (Recompute()) sempre que um rank muda (PerkState.TryBuy) ou que uma run
// nova começa/carrega (MainMenuUI — senão os static fields dos sistemas abaixo ficariam com o
// valor da run ANTERIOR na mesma sessão de Play). Nunca lê PerkState direto de dentro de um
// sistema de gameplay (KillGoldReward/HeroController/Bag/Vendor/EnemyController/
// WorldChestSpawnManager) — cada um só lê um multiplicador/flag já pronto, escrito aqui 1 vez.
//
// Fora de Assets/Scripts/Core/ de propósito — mesmo motivo de PerkState.cs: referencia
// HeroController/Vendor/EnemyController/BagController (assembly padrão), e Core não pode
// referenciar o assembly padrão de volta.
//
// Pendência explícita (sem número no GDD, Seção 8): Recuperação/Metabolismo Acelerado/Fôlego
// Eterno dizem só "regen de HP +%/rank", sem valor. RegenPercentPerRankPerSecond abaixo é um
// placeholder meu (0.5%) até o usuário confirmar o número real.
public static class WorldPerkEffects
{
    // Curva de Stack Maior (GDD Seção 8, item 15) — rank 0 = base do Bag (16), ranks 1-6 vêm
    // daqui. Não é "+%/rank", é uma tabela fixa — por isso array, não fórmula.
    private static readonly long[] StackMaiorCurve = { 16, 32, 128, 1024, 8192, 131072, 1048576 };

    // 🔢 placeholder — GDD não especifica o número de regen/rank (ver comentário acima da classe).
    // Balanceamento (2026-11-10): 5x o valor original — era 0.5f.
    public static float RegenPercentPerRankPerSecond = 2.5f;

    public static long BagStackSize { get; private set; } = 16;
    public static int BagSlotBonus { get; private set; }
    public static bool AutoSellOnPickup { get; private set; }
    // Baú Extra + Chamado do Baú (ex-"Baú Duplo", GDD Seção 8, itens 12/13) — redesenhados
    // (2026-11-09, pedido do usuário): o original ("+1 baú/rank" e "+15% chance de 2
    // recompensas/rank") ficava OP demais no dia em que o baú já nasce; virou os 2 somando
    // direto na % de NASCER baú por dia (ChestSpawnSettings.chestSpawnChancePercent), que é a
    // parte realmente rara/importante. Baú Extra +5%/rank (barato), Chamado do Baú +10%/rank
    // (caro) — mesma convenção de "perk mais caro dá bônus maior" já usada no resto desta
    // classe (ex.: ButimGuerra vs InstintoCacador).
    public static float ChestSpawnChanceBonusPercent { get; private set; }
    public static int FreeTalentRerollCount { get; private set; } // inert até Talentos existirem (R10)

    // Perk Central (não é "Mundo" nem nenhuma das 4 áreas, mas recomputado aqui mesmo assim —
    // único agregador rank->efeito já existente, mesmo critério do resto desta classe, em vez
    // de criar um 2º sistema paralelo só pra 1 perk). ID é POR HERÓI (HeroController.
    // CentralPerkId, cada um sobrescreve com o próprio) — nunca mais hardcoded pro Barbarian.

    public static void Recompute()
    {
        // Balanceamento (2026-11-10, pedido do usuário): Dia 1 morrendo ~10x sem conseguir
        // progredir — dano/vida/gold-por-abate/venda/regen agora valem 5x o original (nunca
        // movespeed/tamanho/área/alcance/cooldown/aceleração/duração, esses ficam como
        // estavam). Central NÃO entra nessa escala de 5x.
        //
        // Balanceamento (2026-11-11, pedido do usuário): Central virou 5 ranks ("parcelas") de
        // +20% cada, em vez de 1 rank fixo em +50% — rank 5 = +100% (dobro), não mais +50%.
        string centralPerkId = HeroController.Instance != null ? HeroController.Instance.CentralPerkId : null;
        int centralRank = centralPerkId != null ? PerkState.GetRank(centralPerkId) : 0;
        float centralMultiplier = 1f + centralRank * 0.2f;
        HeroController.CentralDamageMultiplier = centralMultiplier;
        HeroController.CentralHealthMultiplier = centralMultiplier;

        // Balanceamento (2026-11-11, pedido do usuário): gold por kill é long (inteiro) — um
        // bônus de +40%/75% em cima de uma base pequena (Dia 1: killGold=1) truncava pra ZERO
        // de efeito ((long)(1 * 1.4f) = 1, sem mudança nenhuma). Virou multiplicativo entre os
        // 2 perks em vez de somar % num pool só: cada perk tem seu PRÓPRIO multiplicador local
        // (1 + rank, ou seja, rank1=x2, rank2=x3, rank3=x4...), e os 2 multiplicadores se
        // multiplicam entre si (não somam) — rank1 de Hunter's Instinct (x2) + rank1 de War
        // Spoils (x2) = x4 total, não x3. Isso também resolve o truncamento: base=1, rank1 de
        // qualquer um dos 2 já vira 2 (dobro), nunca fica invisível.
        int instintoCacador = PerkState.GetRank(WorldPerkIds.InstintoCacador);
        int butimGuerra = PerkState.GetRank(WorldPerkIds.ButimGuerra);
        float hunterMultiplier = 1f + instintoCacador;
        float warSpoilsMultiplier = 1f + butimGuerra;
        KillGoldReward.GoldBonusMultiplier = hunterMultiplier * warSpoilsMultiplier;

        // Faro de Saque (GDD: "chance/quantidade de drop de loot +8%/rank") — os 2 efeitos
        // juntos, mesma magnitude. Quantidade reaproveita o multiplicador que já existe
        // (HeroController.LootMultiplier, mesmo usado pela passiva do Gunslinger).
        int faroSaque = PerkState.GetRank(WorldPerkIds.FaroSaque);
        float lootBonusPercent = faroSaque * 8f;
        HeroController.LootMultiplier = 1f + lootBonusPercent / 100f;
        EnemyController.LootDropChanceBonusPercent = lootBonusPercent;

        // Balanceamento (2026-11-14, pedido do usuário): dobro do %/rank original (era
        // 10/20/30) — era 10f/20f/30f.
        int atracao = PerkState.GetRank(WorldPerkIds.Atracao);
        int campoAtracao = PerkState.GetRank(WorldPerkIds.CampoAtracao);
        int horizonteColeta = PerkState.GetRank(WorldPerkIds.HorizonteColeta);
        float pickupBonusPercent = atracao * 20f + campoAtracao * 40f + horizonteColeta * 60f;
        HeroController.PickupRadiusMultiplier = 1f + pickupBonusPercent / 100f;

        AutoSellOnPickup = PerkState.GetRank(WorldPerkIds.ImaVivo) > 0;

        // Etapa 3: bônus de venda (Negociante Nato etc.) saiu junto com a venda do Vendor.

        ChestSpawnChanceBonusPercent = PerkState.GetRank(WorldPerkIds.BauExtra) * 5f + PerkState.GetRank(WorldPerkIds.ChamadoDoBau) * 10f;

        BagSlotBonus = PerkState.GetRank(WorldPerkIds.MaisEspacoBag) * 5;
        int stackMaiorRank = Mathf.Clamp(PerkState.GetRank(WorldPerkIds.StackMaior), 0, StackMaiorCurve.Length - 1);
        BagStackSize = StackMaiorCurve[stackMaiorRank];
        BagController.Instance?.ApplyWorldPerkBonuses();

        FreeTalentRerollCount = PerkState.GetRank(WorldPerkIds.RerollTalento);

        // Rebalance (2026-11-14, discussão completa no GDD Seção 8-bis "Metodologia de
        // Rebalance de Dano/Vida") — os 4 perks de Vida (20 ranks: 5+5+5+5) agora são TODOS
        // tabela por rank (não mais rank*X linear), calibrados contra a meta "1 hit do monstro
        // do dia tira só 8% do HP máximo = de boa, 1 hit do monstro do dia SEGUINTE tira só 15%
        // = pode avançar", cruzada com a curva real de dano do bestiário (Tier1-10). Mesma
        // decisão de design do lado de Dano: ranks iniciais baratos/fracos, poder concentrado
        // nos ranks finais de cada perk, nunca mais um degrau de milhões de vezes só no
        // capstone.
        int vigorRank = PerkState.GetRank(WorldPerkIds.Vigor);
        float[] vigorPercent = { 0f, 10.8f, 22.7f, 35.9f, 50.5f, 66.7f };
        int fortitudeRank = PerkState.GetRank(WorldPerkIds.Fortitude);
        float[] fortitudePercent = { 0f, 54.8f, 128f, 224f, 353f, 524f };
        int pulsoVitalRank = PerkState.GetRank(WorldPerkIds.PulsoVital);
        float[] pulsoVitalPercent = { 0f, 1470f, 6090f, 20500f, 65800f, 208000f };
        int vitalidadeSupremaRank = PerkState.GetRank(WorldPerkIds.VitalidadeSuprema);
        float[] vitalidadeSupremaPercent = { 0f, 14600000f, 2940000000f, 1780000000000f, 149000000000000f, 224000000000000f };
        float maxHealthBonusPercent = vigorPercent[Mathf.Clamp(vigorRank, 0, 5)]
            + fortitudePercent[Mathf.Clamp(fortitudeRank, 0, 5)]
            + pulsoVitalPercent[Mathf.Clamp(pulsoVitalRank, 0, 5)]
            + vitalidadeSupremaPercent[Mathf.Clamp(vitalidadeSupremaRank, 0, 5)];
        HeroController.MaxHealthMultiplier = 1f + maxHealthBonusPercent / 100f;

        int peleGrossa = PerkState.GetRank(WorldPerkIds.PeleGrossa);
        int couraca = PerkState.GetRank(WorldPerkIds.Couraca);
        int cascoFerro = PerkState.GetRank(WorldPerkIds.CascoFerro);
        HeroController.DamageReductionPercent = peleGrossa * 3f + couraca * 5f + cascoFerro * 7f;

        int recuperacao = PerkState.GetRank(WorldPerkIds.Recuperacao);
        int metabolismoAcelerado = PerkState.GetRank(WorldPerkIds.MetabolismoAcelerado);
        int folegoEterno = PerkState.GetRank(WorldPerkIds.FolegoEterno);
        int totalRegenRanks = recuperacao + metabolismoAcelerado + folegoEterno;
        HeroController.HpRegenPercentPerSecond = totalRegenRanks * RegenPercentPerRankPerSecond;

        int passoLeve = PerkState.GetRank(WorldPerkIds.PassoLeve);
        int ventoCostas = PerkState.GetRank(WorldPerkIds.VentoCostas);
        int pesLigeiros = PerkState.GetRank(WorldPerkIds.PesLigeiros);
        // Velocidade de movimento SAI da escala de buff (2026-11-10, pedido do usuário: "tá
        // roubado o movespeed") — volta pro valor original (4/6/8), nunca mais escala junto
        // com dano/vida/gold.
        float moveSpeedBonusPercent = passoLeve * 4f + ventoCostas * 6f + pesLigeiros * 8f;
        HeroController.MoveSpeedMultiplier = 1f + moveSpeedBonusPercent / 100f;

        HeroController.SegundaChanceUnlocked = PerkState.GetRank(WorldPerkIds.SegundaChance) > 0;
    }
}
