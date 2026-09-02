using UnityEngine;
using EasyTransition;

public abstract class HeroController : MonoBehaviour
{
    public HeroStats stats = new HeroStats();
    public TransitionSettings respawnTransition;
    protected PlayerControls controls;
    private Vector2 moveInput;

    // GDD Seção 11: "Mira: posição do mouse, resolvida em 8 direções (N, S, L, O, NE, NO, SE, SO)."
    protected Vector2 AimDirection { get; private set; } = Vector2.down;

    protected virtual void Awake()
    {
        controls = new PlayerControls();
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Gameplay.Look.performed += ctx => UpdateAimDirection(ctx.ReadValue<Vector2>());
        controls.Gameplay.Attack.performed += ctx => { if (!IsPaused()) PrimaryAttack(); };
        controls.Gameplay.Ultimate.performed += ctx => { if (!IsPaused()) TryUseUltimate(); };
    }

    protected virtual void Start()
    {
        GameEvents.HealthChanged(stats.health, stats.maxHealth);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    protected virtual void OnEnable()
    {
        controls.Enable();
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    protected virtual void OnDisable()
    {
        controls.Disable();
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    protected virtual void Update()
    {
        if (IsPaused()) return;
        transform.Translate(moveInput * stats.moveSpeed * Time.deltaTime);
    }

    protected bool IsPaused() => TimeManager.Instance != null && TimeManager.Instance.IsPaused;

    private void UpdateAimDirection(Vector2 screenPosition)
    {
        if (Camera.main == null) return;

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -Camera.main.transform.position.z));
        Vector2 toMouse = (Vector2)worldPoint - (Vector2)transform.position;
        if (toMouse.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        AimDirection = new Vector2(Mathf.Cos(snappedAngle * Mathf.Deg2Rad), Mathf.Sin(snappedAngle * Mathf.Deg2Rad));
    }

    private void HandleEnemyKilled(int energyValue)
    {
        stats.energy = EnergySystem.AddEnergy(stats.energy, stats.maxEnergy, energyValue);
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    private void TryUseUltimate()
    {
        if (!EnergySystem.IsReady(stats.energy, stats.maxEnergy)) return;
        UseUltimate();
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);
    }

    public void TakeDamage(float amount)
    {
        stats.health = HealthSystem.ApplyDamage(stats.health, amount);
        GameEvents.HealthChanged(stats.health, stats.maxHealth);

        if (HealthSystem.IsDead(stats.health)) OnDeath();
    }

    // GDD Seção 11 — "Morte: ordem de eventos". Fase 1 (Sprint 8): loot e Day Timer
    // ainda não existem no projeto, ficam como placeholder até as Deadlines que os criam.
    private void OnDeath()
    {
        Debug.Log("[HeroController] Morreu.");

        // 1. Cancela estados temporários — nenhum existe ainda (hook pra ultimates com duração/transformações)
        // 2. Destrói loot carregado — sem inventário ainda (hook pra Deadline 3)
        // 3. Zera Energia da Ultimate
        stats.energy = 0f;
        GameEvents.EnergyChanged(stats.energy, stats.maxEnergy);

        // 4. Penalidade de -30s no timer do dia — sem Day Timer ainda, só o log por enquanto
        Debug.Log("[HeroController] Penalidade de -30s no timer do dia (placeholder — Day Timer ainda não existe).");

        // 5. Respawn no térreo com HP cheio
        Respawn();
    }

    private void Respawn()
    {
        // Teleporte acontece dentro do callback, no momento em que a transição
        // já cobriu a tela por completo — o jogador nunca vê o salto de posição.
        TransitionHelper.PlayTransition(respawnTransition, () =>
        {
            var ground = FloorRegistry.Instance.Floors.Find(f => f.originalFloorIdentity == 0);
            if (ground != null)
            {
                transform.position = ground.transform.position;
                FloorManager.Instance.SetCurrentFloor(ground);
            }

            stats.health = stats.maxHealth;
            GameEvents.HealthChanged(stats.health, stats.maxHealth);
            Debug.Log("[HeroController] Respawn no térreo com HP cheio.");
        });
    }

    protected abstract void PrimaryAttack();
    protected abstract void UseUltimate();
}
