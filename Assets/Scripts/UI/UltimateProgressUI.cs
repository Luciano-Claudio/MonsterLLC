using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ultimate_Progress_Bar/Ultimate_Progress_Percentage sobem de 0 a 100% junto com
// GameEvents.OnEnergyChanged (mesmo sinal que já existe no HeroController). Ultimate_Active
// liga quando a energia chega em 100% OU quando a Ultimate do herói está em uso agora
// (rework 2026-10-30 — Alce do Druid/forma sombria do Assassin, via GameEvents.
// OnUltimateActiveChanged/HeroController.IsUltimateActive): fica aceso o tempo TODO que o Alce
// estiver ativo, não só até o clique que consome a energia.
public class UltimateProgressUI : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text progressPercentage;
    [SerializeField] private GameObject ultimateActive;
    // Filho de Ultimate_Active — só aceso enquanto a Ultimate está REALMENTE em uso (Alce
    // transformado), nunca só por energia cheia (mesmo critério do Ussage do Primary_Attack/
    // Shift, que também tem prioridade visual sobre o resto do próprio pai).
    [SerializeField] private GameObject ussage;

    private bool energyFull;
    private bool ultimateInUse;

    private void OnEnable()
    {
        GameEvents.OnEnergyChanged += HandleEnergyChanged;
        GameEvents.OnUltimateActiveChanged += HandleUltimateActiveChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnEnergyChanged -= HandleEnergyChanged;
        GameEvents.OnUltimateActiveChanged -= HandleUltimateActiveChanged;
    }

    private void HandleEnergyChanged(float current, float max)
    {
        float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (progressBar != null) progressBar.fillAmount = fraction;
        if (progressPercentage != null) progressPercentage.text = $"{Mathf.RoundToInt(fraction * 100f)}%";
        energyFull = fraction >= 1f;
        Refresh();
    }

    private void HandleUltimateActiveChanged(bool isActive)
    {
        ultimateInUse = isActive;
        Refresh();
    }

    private void Refresh()
    {
        if (ultimateActive != null) ultimateActive.SetActive(energyFull || ultimateInUse);
        if (ussage != null) ussage.SetActive(ultimateInUse);
    }
}
