using UnityEngine;

// Efeito visual da Reza (Shift) do Cleric — filho dedicado com Animator próprio (1 clipe só,
// "HealStatusCleric", sem loop, tocado do zero a cada ativação). A cura total vem em 4 ondas
// (Animation Events no meio do próprio clipe, não no corpo do Cleric) — cada onda cura 1/4
// e pisca o status "Heal" (StatusEffectController.FlashStatus) por cima de qualquer Efeito
// Nocivo ativo (ex.: Fire), que volta a aparecer sozinho assim que a piscada termina.
public class ClericShiftEffect : MonoBehaviour
{
    private Animator effectAnimator;
    private Cleric owner;

    private void Awake()
    {
        effectAnimator = GetComponent<Animator>();
        owner = GetComponentInParent<Cleric>();
    }

    // Chamado pelo Cleric em UseSecondaryAbility() — Play() direto pelo nome força reiniciar
    // do frame 0 mesmo se já estiver tocando (clipe único, sem Trigger: o controller não tem
    // nenhum parâmetro).
    public void Play()
    {
        if (effectAnimator != null) effectAnimator.Play("HealStatusCleric", 0, 0f);
    }

    // Animation Event, 1x por onda de cura (4x por clipe, ver comentário da classe).
    public void AnimationHealWaveEvent()
    {
        if (owner != null) owner.ApplyHealWave();
    }
}
