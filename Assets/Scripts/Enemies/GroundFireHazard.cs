using UnityEngine;

// Fogo persistente no chão (Dragon/Undead Dragon, Bestiário Andar 7 — baforada contínua):
// diferente do GroundTargetHazard (telegraph de 1 hit só, num frame fixo), esse fica em pé
// (loop) por um tempo fixo (duration) e aplica um Efeito Nocivo (dot) no player toda vez
// que ele ENTRA na área — sem Animation Event de hit, porque o player pode atravessar o
// fogo em qualquer instante enquanto ele ainda está de pé, não num frame fixo só.
public class GroundFireHazard : MonoBehaviour
{
    [Header("Dot ao tocar o fogo")]
    [SerializeField] private StatusEffectType statusEffectType;
    [SerializeField] private float statusEffectDuration = 3f; // 🔢 ajustável
    [SerializeField] private float statusEffectDamagePerSecond = 2f; // 🔢 ajustável

    [Header("Duração do próprio fogo (loop, até o tempo se esgotar)")]
    [SerializeField] private float duration = 5f; // 🔢 ajustável

    private float elapsed;

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        elapsed += Time.deltaTime;
        if (elapsed >= duration) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var hero = other.GetComponent<HeroController>();
        if (hero == null || hero.IsCurrentlyDamageImmune) return;

        var statusController = hero.GetComponent<StatusEffectController>();
        if (statusController != null)
            statusController.ApplyStatusEffect(statusEffectType, statusEffectDuration, statusEffectDamagePerSecond);
    }
}
