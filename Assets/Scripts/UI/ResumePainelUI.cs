using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Resumo do Dia — GDD Deadline 8.1 (mensagem detalhada do usuário). Quando o painel fica
// visível (OnEnable — reabre toda vez que a Loja volta pra aba Resume), anima os números
// crescendo gradativamente na ordem: Survival Time sozinho -> Combat Summary + Loot Sold juntos
// -> Totals um de cada vez (Kill Gold, depois Loot Gold, depois Total). Nome/cor do ícone/
// unidade de um item revelado são INSTANTÂNEOS (sem animação) — só quantidade/subtotal crescem.
public class ResumePainelUI : MonoBehaviour
{
    [Tooltip("Mesmo asset que o Vendor usa (Assets/Settings/LootPricingSettings.asset) — pra mostrar o preço atual (Unit) de cada item revelado.")]
    [SerializeField] private LootPricingSettings pricingSettings;
    [SerializeField] private float growDuration = 0.8f; // 🔢 ajustável — tempo de cada fase de crescimento

    private static readonly Color SurvivedColor = new Color32(0xE9, 0xCF, 0x32, 0xFF); // cor clara, pedida pelo usuário
    private static readonly Color DiedColor = new Color32(0x8B, 0x1A, 0x1A, 0xFF); // vermelho escuro, pedido pelo usuário

    // Nome "de verdade" (com espaço, usado em LootPricingSettings/Bag/DayStatsTracker) — o
    // GameObject de cada linha troca o espaço por underscore (ex. "Monster Essence" ->
    // "Monster_Essence"), mesmo critério já usado no resto da cena. "Dark Crystal" ainda não
    // tem linha na cena (pendência antiga, 14/15) — LootRow() retorna null pra ele e o resto do
    // código já ignora com segurança, sem quebrar os outros 14.
    private static readonly string[] LootItemNames =
    {
        "Monster Essence", "Monster Fragment", "Spirit Dust", "Arcane Shard", "Dark Crystal",
        "Soul Fragment", "Corrupted Core", "Elemental Shard", "Ancient Fragment", "Infernal Ash",
        "Chaos Crystal", "Nightmare Residue", "Void Shard", "Celestial Fragment", "Divine Core",
    };

    private Coroutine runningRoutine;

    private void OnEnable()
    {
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        runningRoutine = StartCoroutine(AnimateResume());
    }

    private void OnDisable()
    {
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        runningRoutine = null;
    }

    private IEnumerator AnimateResume()
    {
        SetDayAndSurvivalStatusInstant();
        RevealKnownItemsInstant();

        yield return StartCoroutine(AnimateSurvivalTime());

        // Combat Summary + Loot Sold crescem JUNTOS (pedido do usuário) — dispara tudo, espera
        // o mais lento terminar.
        var combatAndLoot = new List<Coroutine>();
        AddCombatGrowCoroutines(combatAndLoot);
        AddLootGrowCoroutines(combatAndLoot);
        foreach (var c in combatAndLoot) yield return c;

        // Totals cresce 1 de cada vez, nesta ordem exata (pedido do usuário).
        long killGold = DayStatsTracker.GoldFromMonsters + DayStatsTracker.GoldFromBosses;
        long lootGold = SumAllLootSubtotals();
        yield return StartCoroutine(GrowNumber(FindTotalValue("Totals/Kill_Gold"), killGold));
        yield return StartCoroutine(GrowNumber(FindTotalValue("Totals/Loot_Gold"), lootGold));
        yield return StartCoroutine(GrowNumber(FindTotalValue("Totals/Total"), killGold + lootGold));
    }

    // Voltou a ser 1 TMP_Text único (Day_X) — texto inteiro "Day x - Survived/Died" e cor
    // inteira junto (pedido do usuário: reverteu a separação).
    private void SetDayAndSurvivalStatusInstant()
    {
        var dayLabel = transform.Find("Day_Sumary/Day_X")?.GetComponent<TMP_Text>();
        if (dayLabel == null) return;

        int day = MainMenuUI.CurrentRun?.day ?? 1;
        bool survived = DayResolver.PlayerSurvivedLastDay;
        dayLabel.text = $"Day {day} - {(survived ? "Survived" : "Died")}";
        dayLabel.color = survived ? SurvivedColor : DiedColor;
    }

    // "Survival Time:" (Text (TMP)) e "0s / 200s" (Time) são 2 TMP_Text separados agora — só o
    // "Time" muda de cor/anima (pedido do usuário). O lado direito (duração total do dia) nunca
    // anima, só o esquerdo (tempo sobrevivido) cresce de 0 até o valor real.
    private IEnumerator AnimateSurvivalTime()
    {
        var label = transform.Find("Survival_Time/Time")?.GetComponent<TMP_Text>();
        if (label == null) yield break;

        float elapsedReal = DayTimer.Instance != null
            ? DayTimer.Instance.CurrentDayDuration - DayTimer.Instance.timeRemaining
            : 0f;
        int finalElapsedSeconds = Mathf.RoundToInt(Mathf.Max(0f, elapsedReal));
        int totalSeconds = DayTimer.Instance != null ? Mathf.RoundToInt(DayTimer.Instance.CurrentDayDuration) : 0;

        label.color = SurvivedColor;
        var rect = label.rectTransform;
        Quaternion baseRotation = rect.localRotation;
        Vector3 baseScale = rect.localScale;

        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / growDuration);
            int current = Mathf.RoundToInt(Mathf.Lerp(0, finalElapsedSeconds, p));
            label.text = $"{current}s / {totalSeconds}s";
            ApplyGrowJuice(rect, baseRotation, baseScale, p);
            yield return null;
        }
        label.text = $"{finalElapsedSeconds}s / {totalSeconds}s";
        rect.localRotation = baseRotation;
        rect.localScale = baseScale;
    }

    private void AddCombatGrowCoroutines(List<Coroutine> list)
    {
        list.Add(StartCoroutine(GrowNumber(FindValue("Combat_Sumary/List/Creatures_Slain"), DayStatsTracker.CreaturesSlain)));
        list.Add(StartCoroutine(GrowNumber(FindValue("Combat_Sumary/List/Bosses_Defeated"), DayStatsTracker.BossesDefeated)));
        list.Add(StartCoroutine(GrowNumber(FindValue("Combat_Sumary/List/Gold_From_Monsters"), DayStatsTracker.GoldFromMonsters)));
        list.Add(StartCoroutine(GrowNumber(FindValue("Combat_Sumary/List/Gold_From_Bosses"), DayStatsTracker.GoldFromBosses)));
    }

    // Nome/ícone/unidade são setados na hora (RevealKnownItemsInstant, fora desta função) —
    // aqui só dispara o crescimento de Quantity/Subtotal, e só pra item já conhecido (item
    // nunca vendido continua "???", sem nada pra crescer).
    private void AddLootGrowCoroutines(List<Coroutine> list)
    {
        foreach (var itemName in LootItemNames)
        {
            var row = LootRow(itemName);
            if (row == null || !DayStatsTracker.IsItemKnown(itemName)) continue;

            var qtyLabel = row.Find("Quantity")?.GetComponent<TMP_Text>();
            var subtotalLabel = row.Find("Subtotal")?.GetComponent<TMP_Text>();
            list.Add(StartCoroutine(GrowNumber(qtyLabel, DayStatsTracker.GetSoldQuantity(itemName))));
            list.Add(StartCoroutine(GrowNumber(subtotalLabel, DayStatsTracker.GetSoldSubtotal(itemName))));
        }
    }

    // Nome "???"/ícone cor 141414 (default da cena) ficam intocados pra item nunca vendido.
    // Item conhecido (vendido hoje OU em qualquer dia anterior da run — RunState.
    // knownLootItems, pra sempre) revela nome/ícone/preço na hora, sem animação nenhuma
    // (pedido explícito do usuário).
    private void RevealKnownItemsInstant()
    {
        foreach (var itemName in LootItemNames)
        {
            var row = LootRow(itemName);
            if (row == null || !DayStatsTracker.IsItemKnown(itemName)) continue;

            var nameLabel = row.Find("Name_Txt")?.GetComponent<TMP_Text>();
            if (nameLabel != null) nameLabel.text = itemName;

            var icon = row.Find("Image")?.GetComponent<Image>();
            if (icon != null) icon.color = Color.white; // só remove o tom escuro — sprite placeholder fica

            var unitLabel = row.Find("Unit")?.GetComponent<TMP_Text>();
            if (unitLabel != null && pricingSettings != null && pricingSettings.TryGetPrice(itemName, out long sellValue))
                unitLabel.text = LargeNumberFormatter.Format(sellValue);
        }
    }

    private long SumAllLootSubtotals()
    {
        long sum = 0;
        foreach (var itemName in LootItemNames) sum += DayStatsTracker.GetSoldSubtotal(itemName);
        return sum;
    }

    // Troca espaço por underscore pra bater com o nome do GameObject na cena (ex.: "Monster
    // Essence" -> "Monster_Essence") — null se a linha ainda não existir (Dark Crystal, 14/15).
    private Transform LootRow(string itemName) => transform.Find($"Loot_Sold/List/{itemName.Replace(" ", "_")}");

    private TMP_Text FindValue(string rowPath) => transform.Find(rowPath + "/Value")?.GetComponent<TMP_Text>();
    private TMP_Text FindTotalValue(string rowPath) => transform.Find(rowPath + "/Total_Value")?.GetComponent<TMP_Text>();

    // Cresce de 0 até finalValue em growDuration segundos, formatado com LargeNumberFormatter
    // (1k/1M/1B/1T...) a cada frame — double de propósito (não float), pra não perder precisão
    // em valores grandes (a tabela de gold por dia já chega na casa dos milhões). Junto, toca a
    // "mini animação" pedida (sem Animator): balança pra esquerda/direita (amplitude decaindo,
    // 3 ciclos) + um pulso de escala (cresce e volta), tudo via RectTransform direto.
    private IEnumerator GrowNumber(TMP_Text label, long finalValue)
    {
        if (label == null) yield break;

        var rect = label.rectTransform;
        Quaternion baseRotation = rect.localRotation;
        Vector3 baseScale = rect.localScale;

        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / growDuration);
            double current = finalValue * (double)p;
            label.text = LargeNumberFormatter.Format(current);
            ApplyGrowJuice(rect, baseRotation, baseScale, p);
            yield return null;
        }
        label.text = LargeNumberFormatter.Format(finalValue);
        rect.localRotation = baseRotation;
        rect.localScale = baseScale;
    }

    private static void ApplyGrowJuice(RectTransform rect, Quaternion baseRotation, Vector3 baseScale, float p)
    {
        float wobbleDegrees = Mathf.Sin(p * Mathf.PI * 3f) * Mathf.Lerp(10f, 0f, p);
        float scalePulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.2f;
        rect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wobbleDegrees);
        rect.localScale = baseScale * scalePulse;
    }
}
