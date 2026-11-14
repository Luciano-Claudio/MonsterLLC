using UnityEngine;

public class ShopHandler : MonoBehaviour
{
    // Vive no GameObject "Systems" (DontDestroyOnLoad, carregado a partir da cena Main_Menu) —
    // sem isto, nada em OUTRA cena (ex.: os botões Start_Day em _TestScene) consegue referenciar
    // esta instância, já que ela nem existe no arquivo de cena deles em tempo de edição. Mesmo
    // padrão de DayTimer.Instance/GameStateManager.Instance.
    public static ShopHandler Instance { get; private set; }

    private void Awake() => Instance = this;

    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.Shop) return;
        Debug.Log("[ShopHandler] Loja aberta. Use \"Buy Next Weapon Tier\" e/ou \"Start Next Day\".");
    }

    // Stub — nao e o sistema real de Weapon Tier (15 tiers, precos, multiplicadores
    // de dano/vida). So o suficiente pra provar que uma compra na Loja sobrevive ao
    // Save/Load. Vira o sistema de verdade na Deadline 9 (Sprint 33).
    private static readonly string[] tierOrder = { "Basic", "Copper", "Iron" };

    [ContextMenu("Buy Next Weapon Tier")]
    public void BuyNextTier()
    {
        int currentIndex = System.Array.IndexOf(tierOrder, MainMenuUI.CurrentRun.weaponTier);
        if (currentIndex == -1 || currentIndex >= tierOrder.Length - 1)
        {
            Debug.Log("[ShopHandler] Nenhum tier seguinte disponível (stub só vai até Iron nesta sprint).");
            return;
        }

        int price = (currentIndex + 1) * 50; // placeholder — preços reais são Deadline 9
        if (MainMenuUI.CurrentRun.gold < price)
        {
            Debug.Log($"[ShopHandler] Gold insuficiente ({MainMenuUI.CurrentRun.gold}/{price}) para {tierOrder[currentIndex + 1]}.");
            return;
        }

        MainMenuUI.CurrentRun.gold -= price;
        MainMenuUI.CurrentRun.weaponTier = tierOrder[currentIndex + 1];
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        Debug.Log($"[ShopHandler] Comprou {MainMenuUI.CurrentRun.weaponTier} por {price} gold.");
    }

    // Stub — nao e o sistema real de Tier Suppression (nome tematico, preco balanceado, UI de
    // loja). So o suficiente pra provar que a supressao sobrevive ao Save/Load e afeta o
    // spawn de monstro/boss de verdade (ver RunState.suppressedTierCount,
    // WorldTierSelector.ComputeTierPercentsNonAlloc). Doc de design, Secao 12: SEMPRE
    // sequencial a partir do Tier1 (nunca escolha livre) — por isso so existe "comprar o
    // PROXIMO", sem parametro de qual Tier.
    [ContextMenu("Buy Next Tier Suppression")]
    public void BuyNextTierSuppression()
    {
        if (MainMenuUI.CurrentRun.suppressedTierCount >= WorldTierSelector.MonsterTierCount)
        {
            Debug.Log("[ShopHandler] Todos os Tiers já suprimidos.");
            return;
        }

        int nextTierNumber = MainMenuUI.CurrentRun.suppressedTierCount + 1; // 1-based, só pro log
        int price = nextTierNumber * 100; // placeholder — preço real de balanceamento fica pra quando a mecânica virar de verdade
        if (MainMenuUI.CurrentRun.gold < price)
        {
            Debug.Log($"[ShopHandler] Gold insuficiente ({MainMenuUI.CurrentRun.gold}/{price}) para suprimir Tier {nextTierNumber}.");
            return;
        }

        MainMenuUI.CurrentRun.gold -= price;
        MainMenuUI.CurrentRun.suppressedTierCount++;
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        // Decisão do usuário: NÃO limpa a cena na hora da compra — monstros/bosses do Tier
        // recém-suprimido continuam vivos até morrerem/despawnarem naturalmente por distância;
        // só o REPLACEMENT a partir daqui já respeita a supressão. GameEvents.ClearAllEnemies()
        // fica só pro botão de debug "Regenerar Mundo" (ver ChunkTerrainGenerator).
        Debug.Log($"[ShopHandler] Tier {nextTierNumber} suprimido por {price} gold.");
    }

    // Cheat de debug — só pra testar compras (Weapon Tier, Tier Suppression) sem precisar
    // vender loot de verdade no Vendor primeiro. Nunca chamado pelo fluxo real do jogo.
    [ContextMenu("Debug: Give 1000 Gold")]
    public void DebugGiveGold()
    {
        MainMenuUI.CurrentRun.gold += 1000;
        GameEvents.GoldChanged(MainMenuUI.CurrentRun.gold);
        Debug.Log($"[ShopHandler] (Debug) +1000 gold — total agora: {MainMenuUI.CurrentRun.gold}.");
    }

    // Botão único da Loja pra voltar ao jogo (GDD Deadline 8.1, Seção 3) — o que acontece
    // depende de COMO o jogador chegou aqui (ver DayResolver.PlayerSurvivedLastDay):
    // sobreviveu o dia -> avança o número do dia e regenera o mundo (mapa novo, bioma/Tier do
    // dia novo); morreu -> repete o MESMO dia, mesmo mapa, nunca regenera terreno. Nos dois
    // casos o relógio reinicia do zero — só quem muda é se o dia avança ou não.
    [ContextMenu("Proceed From Shop")]
    public void ProceedFromShop()
    {
        if (DayResolver.PlayerSurvivedLastDay)
        {
            MainMenuUI.CurrentRun.day++;

            if (ChunkTerrainGenerator.Instance != null) ChunkTerrainGenerator.Instance.SyncPopulationDayOverrides();

            // Bug real reportado pelo usuário: ao remover o RegenerateWorld() síncrono daqui
            // (ver comentário abaixo), a limpeza de monstros/loot que vivia escondida DENTRO
            // dele também foi removida por engano — DayResolver.ResolveEndOfDay() (caminho de
            // sobreviver) nunca limpa isso por design (só a morte limpa, ver
            // DayResolver.ResolvePlayerDeath()); essa limpeza sempre dependeu de rodar aqui, no
            // "Start Day". Mesmo trio de chamadas de ResolvePlayerDeath, pra manter os 2
            // caminhos simétricos.
            GameEvents.ClearAllEnemies();
            EnemyController.DestroyAllSpawned();
            LootDrop.DestroyAllOnGround();
            HeroController.DestroyAllSpawnedEffects(); // idem pro lado do herói (ground crack, projétil etc. — bug real reportado pelo usuário)

            // O mapa do dia novo já vinha sendo pré-pintado em background desde que a Loja
            // abriu (dia de preview, GDD Deadline 8.1 Seção 5 — ver
            // ChunkTerrainGenerator.BeginBackgroundRegeneration, chamado por
            // ShopUIController). Aqui só "comita" esse preview pro dia real — HandleHeroSpawned
            // (disparado por RespawnHeroFresh abaixo) termina na hora qualquer coisa que o
            // background não tenha concluído ainda (mesmo chunk central instantâneo de sempre).
            if (ChunkTerrainGenerator.Instance != null) ChunkTerrainGenerator.Instance.CommitPreviewDay();

            Debug.Log($"[ShopHandler] Dia sobrevivido — avançando pro Dia {MainMenuUI.CurrentRun.day}.");
        }
        else
        {
            Debug.Log($"[ShopHandler] Repetindo o Dia {MainMenuUI.CurrentRun.day} — mesmo mapa, sem regenerar.");
        }

        // 3ª chamada da mesma receita de DayResolver.ResolveEndOfDay()/ResolvePlayerDeath() +
        // ShopUIController.HandleStateChanged() (ambas já chamam isto, ver comentários lá) —
        // faltava fechar a brecha ENTRE a Loja abrir e o Gameplay voltar de verdade: qualquer
        // texto flutuante que nasça enquanto o jogador navega a Loja (ex.: tick de efeito
        // atrasado, Coroutine que não respeita GameplayGate) fica congelado até aqui e só
        // reaparece animando quando o dia novo começa — bug relatado pelo usuário.
        FloatingCombatText.DestroyAll();

        DayTimer.Instance.ResetForNewDay();
        GameplayBootstrap.Instance?.RespawnHeroFresh(); // sempre herói 100% novo — nunca só reposiciona o mesmo (achado real: valores de Instantiate/Start ficavam presos)
        GameStateManager.Instance.SetState(GameState.Gameplay);
    }
}
