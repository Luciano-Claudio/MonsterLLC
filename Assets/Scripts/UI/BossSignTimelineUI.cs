using System.Collections.Generic;
using UnityEngine;

// Boss_Signs — instancia o prefab Boss_Sign.prefab uma vez por boss da run (etapas 1+2: a ordem
// do RunSettings, via DayTimer.Schedule — fonte única), no segundo em que cada um nasce.
// Reconstrói a timeline inteira a cada GameEvents.OnDayStart (= início da run).
public class BossSignTimelineUI : MonoBehaviour
{
    [SerializeField] private GameObject signPrefab;
    [SerializeField] private DayTimeBarUI timeBar;

    private const float SignY = 42f;

    private readonly List<GameObject> spawnedSigns = new();
    private readonly List<float> signSpawnSeconds = new(); // segundo de run em que o boss de cada sinal nasce
    private int visibleIndex = -2;

    // OnEnable (não só Start/OnDayStart) — o HUD (e esse objeto dentro dele) só fica ativo
    // quando o MainMenuUI entra em GameState.Gameplay, e isso acontece DEPOIS de
    // DayTimer.ResetForNewDay() já ter disparado GameEvents.OnDayStart (MainMenuUI.NewGame()).
    // Se dependesse só do evento, ele já teria passado antes do Boss_Signs se inscrever —
    // reconstrói direto com o estado atual do DayTimer/WorldBossSpawnManager assim que habilita.
    private void OnEnable()
    {
        GameEvents.OnDayStart += RebuildTimeline;
        GameEvents.OnTimeChanged += ShowOnlyNext;
        RebuildTimeline();
    }

    private void OnDisable()
    {
        GameEvents.OnDayStart -= RebuildTimeline;
        GameEvents.OnTimeChanged -= ShowOnlyNext;
    }

    // Só o sinal do PRÓXIMO boss a nascer fica visível (25 sinais juntos poluíam a barra).
    // Troca de sinal só quando o próximo boss muda — nunca SetActive por frame.
    private void ShowOnlyNext(float timeRemaining)
    {
        if (DayTimer.Instance == null) return;
        float elapsed = DayTimer.Instance.RunElapsed;
        int next = -1;
        for (int i = 0; i < signSpawnSeconds.Count; i++)
            if (signSpawnSeconds[i] > elapsed) { next = i; break; }
        if (next == visibleIndex) return;
        visibleIndex = next;
        for (int i = 0; i < spawnedSigns.Count; i++)
            if (spawnedSigns[i] != null) spawnedSigns[i].SetActive(i == next);
    }

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
        signSpawnSeconds.Clear();
        visibleIndex = -2;

        if (signPrefab == null || timeBar == null) return;
        if (DayTimer.Instance == null || DayTimer.Instance.Schedule == null) return;

        // Etapas 1+2 — 1 sinal por espaço da ordem dos bosses (RunSettings), no segundo em que ele nasce.
        var schedule = DayTimer.Instance.Schedule;
        float runDuration = DayTimer.Instance.CurrentDayDuration;
        if (runDuration <= 0f) return;
        for (int slot = 0; slot < schedule.SlotCount; slot++)
        {
            if (schedule.SlotTier(slot) <= 0) continue; // espaço vazio: sem boss nesse minuto
            float remaining = Mathf.Max(0f, runDuration - schedule.BossSpawnSecond(slot));
            float x = timeBar.ComputeXForRemaining(remaining, runDuration);
            var signObj = Instantiate(signPrefab, transform);
            signObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, SignY);
            signObj.GetComponent<BossSignUI>().Initialize(remaining);
            spawnedSigns.Add(signObj);
            signSpawnSeconds.Add(schedule.BossSpawnSecond(slot));
        }
        ShowOnlyNext(DayTimer.Instance.timeRemaining);
    }
}
