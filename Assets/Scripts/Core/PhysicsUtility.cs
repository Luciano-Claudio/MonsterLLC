using UnityEngine;

// Achado real (2026-10-25, bug crítico reportado pelo usuário): TODO projétil do jogo (herói
// e monstro) move via transform.position += direto no Update(), sem Rigidbody2D nenhum —
// Goblin Raider, Rat People, Spider Queen e qualquer outro confirmados com o mesmo sintoma
// ("as vezes acerta e não dá dano, as vezes só desaparece sem acertar"). Um Collider2D sem
// Rigidbody2D é tratado como "estático" pela física 2D da Unity — a sincronização da posição
// dele no mundo de física não acontece com a mesma confiabilidade/ordem de um corpo "ativo",
// então o overlap com outro collider (o trigger do próprio projétil, nesse caso) pode nunca
// ser avaliado no frame exato em que ele atravessa o alvo (tunelamento), perdendo o
// OnTriggerEnter2D por completo. Um Rigidbody2D Kinematic resolve isso sem mudar NADA de como
// o objeto já é movido (continua sendo transform.position += na mão) — só precisa existir.
public static class PhysicsUtility
{
    public static void EnsureKinematicRigidbody2D(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // a posição muda via script todo frame, nunca deve "dormir"
    }
}
