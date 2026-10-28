using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Primary_Attack e Shift usam a mesma lógica — um script reaproveitado nos dois, só trocando
// qual par de eventos (Active/Cooldown) ele escuta via o enum Source.
public class SkillCooldownUI : MonoBehaviour
{
    private enum Source { PrimaryAttack, SecondaryAbility }

    [SerializeField] private Source source;
    [SerializeField] private GameObject ussage;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private GameObject cooldownValue;
    [SerializeField] private TMP_Text cooldownValueLabel;

    private bool isActive;
    private float cooldownTimeRemaining;
    private float cooldownDuration;

    private void OnEnable()
    {
        if (source == Source.PrimaryAttack)
        {
            GameEvents.OnPrimaryAttackActiveChanged += HandleActiveChanged;
            GameEvents.OnPrimaryAttackCooldownChanged += HandleCooldownChanged;
        }
        else
        {
            GameEvents.OnSecondaryAbilityActiveChanged += HandleActiveChanged;
            GameEvents.OnSecondaryAbilityCooldownChanged += HandleCooldownChanged;
        }
    }

    private void OnDisable()
    {
        if (source == Source.PrimaryAttack)
        {
            GameEvents.OnPrimaryAttackActiveChanged -= HandleActiveChanged;
            GameEvents.OnPrimaryAttackCooldownChanged -= HandleCooldownChanged;
        }
        else
        {
            GameEvents.OnSecondaryAbilityActiveChanged -= HandleActiveChanged;
            GameEvents.OnSecondaryAbilityCooldownChanged -= HandleCooldownChanged;
        }
    }

    private void HandleActiveChanged(bool active)
    {
        isActive = active;
        Refresh();
    }

    private void HandleCooldownChanged(float timeRemaining, float duration)
    {
        cooldownTimeRemaining = timeRemaining;
        cooldownDuration = duration;
        Refresh();
    }

    // Ussage tem prioridade visual sobre o Cooldown — enquanto a habilidade está ativa (mesmo
    // que o cooldown técnico já tenha começado a contar por baixo, hoje — ver docs/gdd/index.md
    // Seção 53), mostra Ussage, não Cooldown.
    private void Refresh()
    {
        bool onCooldown = cooldownTimeRemaining > 0f && !isActive;

        if (ussage != null) ussage.SetActive(isActive);
        if (cooldownFill != null) cooldownFill.gameObject.SetActive(onCooldown);
        if (cooldownValue != null) cooldownValue.SetActive(onCooldown);

        if (!onCooldown) return;

        if (cooldownFill != null) cooldownFill.fillAmount = cooldownDuration > 0f ? Mathf.Clamp01(cooldownTimeRemaining / cooldownDuration) : 0f;
        if (cooldownValueLabel != null) cooldownValueLabel.text = Mathf.CeilToInt(cooldownTimeRemaining).ToString();
    }
}
