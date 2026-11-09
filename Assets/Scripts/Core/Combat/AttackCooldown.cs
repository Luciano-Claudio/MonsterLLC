using UnityEngine;

public class AttackCooldown
{
    private readonly float cooldownDuration;
    private float timeRemaining;

    // Perks de cooldown (-%/rank, Esquerda #21-22/Direita #13-16/#26) — multiplica POR CIMA de
    // cooldownDuration, nunca substitui (mesmo critério de qualquer outro multiplicador
    // "static 1f default" do projeto). Default 1 = nenhuma mudança pra quem nunca tocar nisto.
    public float DurationMultiplier = 1f;

    public AttackCooldown(float cooldownDuration)
    {
        this.cooldownDuration = cooldownDuration;
        timeRemaining = 0f;
    }

    public bool IsReady => timeRemaining <= 0f;
    public float TimeRemaining => timeRemaining;
    public float Duration => cooldownDuration * DurationMultiplier;

    public void Tick(float deltaTime)
    {
        if (timeRemaining > 0f) timeRemaining -= deltaTime;
    }

    // Consome o cooldown se estiver pronto. Sem efeito colateral se não estiver
    // (não zera/reinicia nada) — quem chama decide o que fazer com o "não, ainda não").
    public bool TryConsume()
    {
        if (!IsReady) return false;
        timeRemaining = Duration;
        return true;
    }

    // Arma o timer sem checar IsReady — separado de TryConsume() pros cooldowns de "tempo de
    // uso" (Habilidade Secundária): a checagem de "pronto pra usar" acontece no clique, mas o
    // timer só deve começar a contar quando a habilidade termina de verdade, não no clique.
    public void Start()
    {
        timeRemaining = Duration;
    }

    // Direita #23 (Fôlego Recuperado — matar durante o Shift reduz o cooldown restante por %).
    public void ReduceTimeRemaining(float amount)
    {
        timeRemaining = Mathf.Max(0f, timeRemaining - amount);
    }

    // Direita #29 (Coração Selvagem — zera o cooldown do Shift sempre que o HP cruza um limiar).
    public void ResetTimeRemaining()
    {
        timeRemaining = 0f;
    }
}
