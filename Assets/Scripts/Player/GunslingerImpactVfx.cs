using UnityEngine;

// VFX puramente cosmético do tiro do Gunslinger (primário e Ultimate, GDD Seção 17.8) —
// sem dano, sem collider: o dano já foi aplicado direto pelo raycast em
// Gunslinger.FireHitscanShot() antes de instanciar isto. Só existe 1 animação (toca sozinha,
// sem Trigger nenhum — é o Default State do próprio Animator Controller) e, quando ela termina,
// o objeto se destrói (AnimationImpactEndEvent). maxLifetime é só a rede de segurança padrão do
// projeto — Animation Event nunca disparar não pode deixar o objeto pra sempre na cena.
public class GunslingerImpactVfx : MonoBehaviour
{
    [SerializeField] private float maxLifetime = 1f; // 🔢 rede de segurança

    private void Awake()
    {
        Destroy(gameObject, maxLifetime);
    }

    // Animation Event, no último frame do clipe de impacto.
    public void AnimationImpactEndEvent()
    {
        Destroy(gameObject);
    }
}
