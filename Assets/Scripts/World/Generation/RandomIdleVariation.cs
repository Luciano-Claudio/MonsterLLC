using UnityEngine;

// Prop decorativo com animação própria (ex.: Hickory_Tree_1..4 — balanço com o vento; Bubble do
// Tier6-8_Fire — Small_Bubble/Big_Bubble saindo da lava) que sai do Idle em intervalos
// aleatórios, pra não parecer engessado (todas as instâncias fazendo a mesma coisa junto).
// Chama Animator.Play() DIRETO por nome de estado — os Controllers desses props não têm NENHUMA
// transição configurada (m_Transitions vazio em todos os estados), então não existe Trigger/bool
// pra disparar, só o Play() mesmo (mesmo padrão já usado em HeroController.Respawn/
// AccelerateDamageReaction).
//
// Cada instância sorteia o próprio intervalo (Random.Range chamado de forma independente, aqui
// mesmo) — não precisa de nenhuma sincronização pra desalinhar: instâncias diferentes nunca
// compartilham estado, então cada árvore/bolha já balança/estoura num tempo próprio sozinha.
public class RandomIdleVariation : MonoBehaviour
{
    [Tooltip("Nome do estado de repouso no Animator (ex.: \"Idle_Tree_1\", \"Bubble_Idle\").")]
    [SerializeField] private string idleStateName = "Idle";

    [Tooltip("Estados sorteados quando a variação dispara. 1 entrada = sempre a mesma (árvore: só o balanço dela); 2+ = sorteia entre elas a cada vez (bolha: Small_Bubble ou Big_Bubble).")]
    [SerializeField] private string[] variationStateNames;

    [Tooltip("Espera mínima/máxima (segundos) entre uma variação e a próxima — sorteada de novo a cada ciclo.")]
    [SerializeField] private float minInterval = 4f; // 🔢 ajustável — árvore: balanço raro; bolha: pode ser mais frequente
    [SerializeField] private float maxInterval = 10f; // 🔢 ajustável

    private Animator animator;
    private float waitRemaining;
    private bool isPlayingVariation;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ScheduleNextVariation();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (animator == null || variationStateNames == null || variationStateNames.Length == 0) return;

        if (isPlayingVariation)
        {
            // Todos os clipes de variação estão marcados Loop na importação (necessário pro
            // Unity não travar parado no último frame), mas normalizedTime ainda cruza 1.0
            // exatamente quando o 1º ciclo termina — detecta isso em vez de um tempo fixo
            // chutado, pra nunca cortar a animação antes da hora nem segurar além da duração
            // real dela (se o clipe for reeditado depois, continua certo sem precisar reajustar
            // nada aqui).
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                isPlayingVariation = false;
                animator.Play(idleStateName);
                ScheduleNextVariation();
            }
            return;
        }

        waitRemaining -= Time.deltaTime;
        if (waitRemaining <= 0f) PlayRandomVariation();
    }

    private void PlayRandomVariation()
    {
        string chosen = variationStateNames[Random.Range(0, variationStateNames.Length)];
        animator.Play(chosen);
        isPlayingVariation = true;
    }

    private void ScheduleNextVariation()
    {
        waitRemaining = Random.Range(minInterval, maxInterval);
    }
}
