using System.Collections.Generic;
using UnityEngine;

// Área de dano do primário redesenhado do Rogue (Self Area Pulse, GDD Seção 17.5) — filho FIXO
// do Rogue (nunca instanciado, sempre presente no prefab, mesma posição dele, sem offset),
// mesmo critério do "sempre presente no prefab" das espadas orbitais do Paladin
// (PaladinOrbitingBlade/Blades). Rogue.cs decide TUDO sobre balanceamento e repassa em
// Activate() — este componente só guarda o próprio Collider2D/Animator e a mecânica de tick.
//
// Ativa aqui (Activate()) quando o clipe "attack_start" do Rogue termina — fica tocando
// "Cycle" em loop (igual à Ultimate do Paladin) enquanto durar, aplicando dano por segundo a
// quem estiver dentro do próprio CircleCollider2D (trigger). Mesmo critério do rastro
// "Grounded" da bola de fogo do Mage: enemiesInRange (HashSet, mantido via Enter/Exit) + tick
// por AttackCooldown, não 1 hit só por entrada — quem fica parado dentro continua recebendo
// dano a cada intervalo.
public class RoguePulseArea : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D triggerCollider;

    private float damagePerTick;
    private AttackCooldown tick;
    private readonly HashSet<EnemyController> enemiesInRange = new();
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[16];

    public void Activate(float damagePerSecond, float tickInterval)
    {
        damagePerTick = damagePerSecond * tickInterval;
        tick = new AttackCooldown(tickInterval);
        enemiesInRange.Clear();

        gameObject.SetActive(true);
        if (triggerCollider != null) triggerCollider.enabled = true;
        if (animator != null) animator.Play("Cycle");

        // Pega de graça quem já está em cima no instante exato da ativação — ligar o collider
        // não reemite OnTriggerEnter2D pra quem já estava sobreposto (mesmo fix já aplicado no
        // AnimationExplosionEndEvent do MageFireball).
        if (triggerCollider != null)
        {
            int count = triggerCollider.Overlap(ContactFilter2D.noFilter, OverlapBuffer);
            for (int i = 0; i < count; i++)
            {
                if (!OverlapBuffer[i].CompareTag("Enemy")) continue;
                var enemy = OverlapBuffer[i].GetComponent<EnemyController>();
                if (enemy != null) enemiesInRange.Add(enemy);
            }
        }
    }

    // Chamado pelo Rogue quando pulseDuration esgota — sem animação de desligar de propósito
    // (pedido do usuário: só 1 clipe, "Cycle"), some na hora.
    public void Deactivate()
    {
        if (triggerCollider != null) triggerCollider.enabled = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;

        tick.Tick(Time.deltaTime);
        if (!tick.TryConsume()) return;

        enemiesInRange.RemoveWhere(e => e == null); // limpa quem morreu desde o último tick
        foreach (var enemy in enemiesInRange) enemy.TakeDamage(damagePerTick);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemiesInRange.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null) enemiesInRange.Remove(enemy);
    }
}
