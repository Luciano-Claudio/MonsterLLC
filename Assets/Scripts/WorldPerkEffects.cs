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
    public static float RegenPercentPerRankPerSecond = 0.5f;

    public static long BagStackSize { get; private set; } = 16;
    public static int BagSlotBonus { get; private set; }
    public static bool AutoSellOnPickup { get; private set; }
    public static int ExtraChestCount { get; private set; }
    public static float DoubleRewardBonusPercent { get; private set; }
    public static int FreeTalentRerollCount { get; private set; } // inert até Talentos existirem (R10)

    public static void Recompute()
    {
        int instintoCacador = PerkState.GetRank(WorldPerkIds.InstintoCacador);
        int butimGuerra = PerkState.GetRank(WorldPerkIds.ButimGuerra);
        float goldBonusPercent = instintoCacador * 8f + butimGuerra * 15f;
        KillGoldReward.GoldBonusMultiplier = 1f + goldBonusPercent / 100f;

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
        float sellBonusPercent = negocianteNato * 10f + mercadorAstuto * 15f + redeContatos * 20f + tesourariaReal * 30f;
        Vendor.SellBonusMultiplier = 1f + sellBonusPercent / 100f;

        ExtraChestCount = PerkState.GetRank(WorldPerkIds.BauExtra);
        DoubleRewardBonusPercent = PerkState.GetRank(WorldPerkIds.BauDuplo) * 15f;

        BagSlotBonus = PerkState.GetRank(WorldPerkIds.MaisEspacoBag) * 5;
        int stackMaiorRank = Mathf.Clamp(PerkState.GetRank(WorldPerkIds.StackMaior), 0, StackMaiorCurve.Length - 1);
        BagStackSize = StackMaiorCurve[stackMaiorRank];
        BagController.Instance?.ApplyWorldPerkBonuses();

        FreeTalentRerollCount = PerkState.GetRank(WorldPerkIds.RerollTalento);

        int vigor = PerkState.GetRank(WorldPerkIds.Vigor);
        int fortitude = PerkState.GetRank(WorldPerkIds.Fortitude);
        int pulsoVital = PerkState.GetRank(WorldPerkIds.PulsoVital);
        int vitalidadeSuprema = PerkState.GetRank(WorldPerkIds.VitalidadeSuprema);
        float maxHealthBonusPercent = vigor * 5f + fortitude * 8f + pulsoVital * 10f + vitalidadeSuprema * 15f;
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
        float moveSpeedBonusPercent = passoLeve * 4f + ventoCostas * 6f + pesLigeiros * 8f;
        HeroController.MoveSpeedMultiplier = 1f + moveSpeedBonusPercent / 100f;

        HeroController.SegundaChanceUnlocked = PerkState.GetRank(WorldPerkIds.SegundaChance) > 0;
    }
}
