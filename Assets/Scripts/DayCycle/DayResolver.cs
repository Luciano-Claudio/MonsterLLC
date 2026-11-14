using UnityEngine;

// GDD Deadline 8.1, Seção 3 — substitui o antigo par "Demanda cumprida -> Results / Demanda NÃO
// cumprida -> GameOver". Não existe mais Game Over: só 2 formas do dia acabar, sobreviver (timer
// chega a 0) ou morrer, e as duas vão pra Results -> Shop. A diferença entre elas é só o que o
// Shop faz quando o jogador sai de lá (avança o dia e regenera o mundo, ou repete o mesmo dia) —
// ver PlayerSurvivedLastDay, lido por ShopHandler.
public static class DayResolver
{
    // Lido por ResultsHandler (loga/decide Save), ShopHandler (decide avançar dia + regenerar
    // mundo, ou só repetir o dia atual) e WorldPopulationManager.SpawnOne() (Seção 4 — aggro
    // instantâneo só em retry). Default true (não false) de propósito: no Dia 1, ANTES de
    // qualquer morte/sobrevivência acontecer pela 1ª vez na run, isto nunca foi setado ainda —
    // sem o default true, o valor-padrão de bool (false) seria lido como "morreu", fazendo a
    // primeira população do jogo inteiro nascer com aggro instantâneo por engano. Static pelo
    // mesmo motivo de outros flags de 1-run-só do projeto (ex.: HeroController.
    // IsPlayerUntargetable) — nunca mais de 1 jogador por vez.
    public static bool PlayerSurvivedLastDay { get; private set; } = true;

    // Chamado por DayTimer.EndDay() — único jeito de sobreviver um dia, o timer chegar a 0
    // contando pra frente (não precisa fazer nada com o mundo: ele já está do jeito que o
    // jogador deixou, loot inclusive — só a morte perde o loot da Bag, GDD Seção 3).
    public static void ResolveEndOfDay()
    {
        PlayerSurvivedLastDay = true;
        FloatingCombatText.DestroyAll(); // sem isso, qualquer texto flutuante vivo na virada do dia congela pra sempre (GameplayGate desliga antes do próprio lifetime zerar)
        GameEvents.DayEnded();
        Debug.Log("[DayResolver] Dia sobrevivido — indo para Resultados.");
        GameStateManager.Instance.SetState(GameState.Results);
    }

    // Chamado por HeroController.OnDeath() — IMEDIATO, não espera a animação de morte terminar
    // (achado/pedido do usuário: monstro/projétil continuando em cena durante o "Die" ficava
    // estranho). Reseta o dia ATUAL do zero (nunca regenera terreno, nunca avança o número do
    // dia): monstros, projétil/summon de monstro e loot no chão somem e renascem. Perde o loot
    // da Bag ainda não vendido (já feito em OnDeath(), antes de chamar isto) — mantém o gold já
    // ganho, que nunca passou pela Bag. NÃO mexe em GameState ainda — isso é
    // OpenResultsAfterDeath(), separado de propósito (ver lá).
    public static void ResolvePlayerDeath()
    {
        PlayerSurvivedLastDay = false;
        GameEvents.ClearAllEnemies();
        EnemyController.DestroyAllSpawned(); // ClearAllEnemies só limpa a lista rastreada — isto pega até projétil/summon que escapou dela
        LootDrop.DestroyAllOnGround();
        FloatingCombatText.DestroyAll();
        HeroController.DestroyAllSpawnedEffects(); // idem pro lado do herói (ground crack, projétil etc. — bug real reportado pelo usuário)
        GameEvents.DayEnded();
        Debug.Log("[DayResolver] Jogador morreu — mundo resetado, aguardando fim da animação de morte...");
    }

    // Chamado por HeroController.AnimationDieEndEvent() — só DEPOIS que a animação de morte
    // termina de tocar de verdade (evento real ou timeout de segurança). Separado de
    // ResolvePlayerDeath() de propósito: o reset do mundo é instantâneo (o jogador não deveria
    // ver monstro algum enquanto a animação de morte do próprio herói ainda está rolando), mas
    // abrir a Loja/trocar de tela só deve acontecer depois que o jogador viu a morte completa.
    public static void OpenResultsAfterDeath()
    {
        Debug.Log("[DayResolver] Animação de morte terminou — repetindo o dia atual.");
        GameStateManager.Instance.SetState(GameState.Results);
    }
}
