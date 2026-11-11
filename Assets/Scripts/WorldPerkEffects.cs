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

    // Perk Central "Despertar do Poder" (não é "Mundo" nem nenhuma das 4 áreas, mas recomputado
    // aqui mesmo assim — único agregador rank->efeito já existente, mesmo critério do resto
    // desta classe, em vez de criar um 2º sistema paralelo só pra 1 perk). Rank único (0 ou 1).
    private const string CentralPerkId = "central_despertar_do_poder";

    public static void Recompute()
    {
        // Balanceamento (2026-11-10, pedido do usuário): Dia 1 morrendo ~10x sem conseguir
        // progredir — dano/vida/gold-por-abate/venda/regen agora valem 5x o original (nunca
        // movespeed/tamanho/área/alcance/cooldown/aceleração/duração, esses ficam como
        // estavam). Central NÃO entra nessa escala de 5x.
        //
        // Balanceamento (2026-11-11, pedido do usuário): Central virou 5 ranks ("parcelas") de
        // +20% cada, em vez de 1 rank fixo em +50% — rank 5 = +100% (dobro), não mais +50%.
        int centralRank = PerkState.GetRank(CentralPerkId);
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

        int atracao = PerkState.GetRank(WorldPerkIds.Atracao);
        int campoAtracao = PerkState.GetRank(WorldPerkIds.CampoAtracao);
        int horizonteColeta = PerkState.GetRank(WorldPerkIds.HorizonteColeta);
        float pickupBonusPercent = atracao * 10f + campoAtracao * 20f + horizonteColeta * 30f;
        HeroController.PickupRadiusMultiplier = 1f + pickupBonusPercent / 100f;

        AutoSellOnPickup = PerkState.GetRank(WorldPerkIds.ImaVivo) > 0;

        int negocianteNato = PerkState.GetRank(WorldPerkIds.NegocianteNato);
        int mercadorAstuto = PerkState.GetRank(WorldPerkIds.MercadorAstuto);
        int redeContatos = PerkState.GetRank(WorldPerkIds.RedeContatos);
        int tesourariaReal = PerkState.GetRank(WorldPerkIds.TesourariaReal);
        float sellBonusPercent = negocianteNato * 50f + mercadorAstuto * 75f + redeContatos * 100f + tesourariaReal * 150f; // era 10/15/20/30 (5x)
        Vendor.SellBonusMultiplier = 1f + sellBonusPercent / 100f;

        ChestSpawnChanceBonusPercent = PerkState.GetRank(WorldPerkIds.BauExtra) * 5f + PerkState.GetRank(WorldPerkIds.ChamadoDoBau) * 10f;

        BagSlotBonus = PerkState.GetRank(WorldPerkIds.MaisEspacoBag) * 5;
        int stackMaiorRank = Mathf.Clamp(PerkState.GetRank(WorldPerkIds.StackMaior), 0, StackMaiorCurve.Length - 1);
        BagStackSize = StackMaiorCurve[stackMaiorRank];
        BagController.Instance?.ApplyWorldPerkBonuses();

        FreeTalentRerollCount = PerkState.GetRank(WorldPerkIds.RerollTalento);

        // Balanceamento (2026-11-11, pedido do usuário): Fortitude e Pulso Vital ganharam 2
        // ranks novos (3->5), mantendo o mesmo %/rank de sempre (rank*X já escala sozinho).
        // Vitalidade Suprema também virou 2->5 ranks, mas com uma tabela ESCALANDO (não
        // rank*X) — ela é quem carrega o "teto" novo de vida, pra bater os ~20 trilhões de
        // vida máxima calibrados contra os monstros Tier9/10 (ver chat 2026-11-11). GDD/
        // monstros desse tier têm vida/dano na faixa de 10^11-10^15 — nenhum %/rank linear
        // sensato chega lá, só uma tabela com um salto gigante no rank final.
        int vigor = PerkState.GetRank(WorldPerkIds.Vigor);
        int fortitude = PerkState.GetRank(WorldPerkIds.Fortitude);
        int pulsoVital = PerkState.GetRank(WorldPerkIds.PulsoVital);
        int vitalidadeSupremaRank = PerkState.GetRank(WorldPerkIds.VitalidadeSuprema);
        float[] vitalidadeSupremaPercent = { 0f, 100f, 2000f, 100000f, 20000000f, 24000000000000f };
        float vitalidadeSupremaBonus = vitalidadeSupremaPercent[Mathf.Clamp(vitalidadeSupremaRank, 0, 5)];
        float maxHealthBonusPercent = vigor * 25f + fortitude * 40f + pulsoVital * 50f + vitalidadeSupremaBonus;
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
