public class AttackCooldown
{
    private readonly float cooldownDuration;
    private float timeRemaining;

    public AttackCooldown(float cooldownDuration)
    {
        this.cooldownDuration = cooldownDuration;
        timeRemaining = 0f;
    }

    public bool IsReady => timeRemaining <= 0f;

    public void Tick(float deltaTime)
    {
        if (timeRemaining > 0f) timeRemaining -= deltaTime;
    }

    // Consome o cooldown se estiver pronto. Sem efeito colateral se não estiver
    // (não zera/reinicia nada) — quem chama decide o que fazer com o "não, ainda não").
    public bool TryConsume()
    {
        if (!IsReady) return false;
        timeRemaining = cooldownDuration;
        return true;
    }

    // Arma o timer sem checar IsReady — separado de TryConsume() pros cooldowns de "tempo de
    // uso" (Habilidade Secundária): a checagem de "pronto pra usar" acontece no clique, mas o
    // timer só deve começar a contar quando a habilidade termina de verdade, não no clique.
    public void Start()
    {
        timeRemaining = cooldownDuration;
    }
}
