using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ultimate_Progress_Bar/Ultimate_Progress_Percentage sobem de 0 a 100% junto com
// GameEvents.OnEnergyChanged (mesmo sinal que já existe no HeroController). Ao chegar em 100%,
// ativa Ultimate_Active — usar a Ultimate zera a energia (HeroController.TryUseUltimate), que já
// dispara OnEnergyChanged(0, max) e volta tudo sozinho sem precisar de lógica extra aqui.
public class UltimateProgressUI : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text progressPercentage;
    [SerializeField] private GameObject ultimateActive;

    private void OnEnable() => GameEvents.OnEnergyChanged += UpdateProgress;
    private void OnDisable() => GameEvents.OnEnergyChanged -= UpdateProgress;

    private void UpdateProgress(float current, float max)
    {
        float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (progressBar != null) progressBar.fillAmount = fraction;
        if (progressPercentage != null) progressPercentage.text = $"{Mathf.RoundToInt(fraction * 100f)}%";
        if (ultimateActive != null) ultimateActive.SetActive(fraction >= 1f);
    }
}
