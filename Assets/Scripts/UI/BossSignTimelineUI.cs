using System.Collections.Generic;
using UnityEngine;

// Boss_Signs — instancia o prefab Boss_Sign.prefab direto (não depende de nenhum objeto
// específico sobrevivendo na cena) uma vez por boss previsto no dia, usando
// WorldBossSpawnManager.BossSpawnInterval (fonte única, nunca duplica o valor aqui — mudar o
// intervalo no BossSpawnSettings já atualiza a UI sozinho) e DayTimer.CurrentDayDuration.
// Reconstrói a timeline inteira a cada GameEvents.OnDayStart.
public class BossSignTimelineUI : MonoBehaviour
{
    [SerializeField] private GameObject signPrefab;
    [SerializeField] private DayTimeBarUI timeBar;

    private const float SignY = 42f;

    private readonly List<GameObject> spawnedSigns = new();

    // OnEnable (não só Start/OnDayStart) — o HUD (e esse objeto dentro dele) só fica ativo
    // quando o MainMenuUI entra em GameState.Gameplay, e isso acontece DEPOIS de
    // DayTimer.ResetForNewDay() já ter disparado GameEvents.OnDayStart (MainMenuUI.NewGame()).
    // Se dependesse só do evento, ele já teria passado antes do Boss_Signs se inscrever —
    // reconstrói direto com o estado atual do DayTimer/WorldBossSpawnManager assim que habilita.
    private void OnEnable()
    {
        GameEvents.OnDayStart += RebuildTimeline;
        RebuildTimeline();
    }

    private void OnDisable() => GameEvents.OnDayStart -= RebuildTimeline;

    // Cobre o outro caso (objeto já ativo desde o load da cena, ex.: _TestScene direto sem
    // passar pelo MainMenuUI) — Start() garante que todo Awake() da cena já rodou, caso o
    // OnEnable() acima tenha disparado cedo demais (WorldBossSpawnManager.Instance/DayTimer.
    // Instance ainda nulos). Rodar RebuildTimeline() 2x nesse caso é inofensivo (idempotente).
    private void Start() => RebuildTimeline();

    private void RebuildTimeline()
    {
        foreach (var sign in spawnedSigns)
        {
            if (sign != null) Destroy(sign);
        }
        spawnedSigns.Clear();

        if (signPrefab == null || timeBar == null) return;
        if (DayTimer.Instance == null || WorldBossSpawnManager.Instance == null) return;

        float dayDuration = DayTimer.Instance.CurrentDayDuration;
        float interval = WorldBossSpawnManager.Instance.BossSpawnInterval;
        float noSpawnWindow = WorldBossSpawnManager.Instance.NoSpawnWindowBeforeDayEnd;
        if (dayDuration <= 0f || interval <= 0f) return;

        for (float elapsed = interval; elapsed <= dayDuration; elapsed += interval)
        {
            float remaining = dayDuration - elapsed;
            // remaining só diminui a cada iteração — uma vez dentro da janela final sem spawn,
            // todo boss seguinte também cairia nela, então pode parar de desenhar sign de vez.
            if (remaining <= noSpawnWindow) break;

            float x = timeBar.ComputeXForRemaining(remaining, dayDuration);

            var signObj = Instantiate(signPrefab, transform);
            signObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, SignY);
            signObj.GetComponent<BossSignUI>().Initialize(remaining);

            spawnedSigns.Add(signObj);
        }
    }
}
