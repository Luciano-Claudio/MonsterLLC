using System.Collections.Generic;
using UnityEngine;

// Ultimate do Blood Mage — anel de dano CIRCULAR em 3 estágios (diâmetro 2 → 4 → 8). O dano
// viaja com a BORDA: cada estágio só acerta quem está dentro do círculo ATUAL e FORA do
// círculo do estágio anterior (zona segura dinâmica, GDD Seção 17.10). Controlador
// (BloodMage.cs) decide todo valor de balanceamento e repassa em Activate() — este componente
// só guarda o timing da própria animação de crescimento. stageNSize = DIÂMETRO (não raio).
public class BloodMageShockwave : MonoBehaviour
{
    [SerializeField] private Animator animator; // clipe de crescimento com 3 Animation Events (1 por estágio)

    // Editáveis aqui no próprio prefab pra testar sem precisar rodar o Blood Mage inteiro —
    // Activate() SOBRESCREVE os 3 tamanhos com os valores reais (já escalados pelo
    // shockwaveSizeMultiplier) assim que a Ultimate de verdade dispara, então esses valores
    // default só valem em Editor/antes do primeiro Activate(). Ache os números certos aqui e
    // depois copie pra shockwaveStage1/2/3Size no BloodMage.
    [Header("Debug — tamanhos de teste (sobrescritos por Activate() em Play real)")]
    [SerializeField] private float stage1Size = 2f;
    [SerializeField] private float stage2Size = 4f;
    [SerializeField] private float stage3Size = 8f;
    [SerializeField] private float ringOffsetY = 0.7f; // sobe o centro do anel/dano — mesmo padrão do PaladinHammer/Vine/RogueBomb/EnemyController
    [SerializeField] private bool showRingGizmos = true;

    // Offset também escala com transform.localScale.x — confirmado em teste: 0.7 fica perfeito
    // em scale 1, 1.4 em scale 2 (proporcional, não fixo).
    private Vector3 RingCenter => transform.position + Vector3.up * ringOffsetY * transform.localScale.x;

    private float damagePerStage;
    private LayerMask enemyLayerMask;
    private readonly List<EnemyController> stageTargets = new();

    public void Activate(float stageDamage, float size1, float size2, float size3, LayerMask layerMask)
    {
        damagePerStage = stageDamage;
        stage1Size = size1;
        stage2Size = size2;
        stage3Size = size3;
        enemyLayerMask = layerMask;

        if (animator != null) animator.SetTrigger("GrowTrigger"); // os 3 Animation Events abaixo tocam dentro deste clipe
    }

    // Animation Event, no frame exato em que o anel atinge o diâmetro do estágio 1 — nada a
    // excluir ainda (é o primeiro estágio, não existe zona segura anterior).
    public void AnimationStage1Event()
    {
        ApplyRingDamage(stage1Size, 0f);
    }

    // Animation Event, no frame exato em que o anel atinge o diâmetro do estágio 2 — exclui
    // quem já está dentro do círculo do estágio 1 (já seguro).
    public void AnimationStage2Event()
    {
        ApplyRingDamage(stage2Size, stage1Size);
    }

    // Animation Event, no frame exato em que o anel atinge o diâmetro do estágio 3 — exclui
    // quem já está dentro do círculo do estágio 2.
    public void AnimationStage3Event()
    {
        ApplyRingDamage(stage3Size, stage2Size);
    }

    private void ApplyRingDamage(float outerSize, float innerSize)
    {
        // transform.localScale é a fonte única de "quão grande é essa instância" — BloodMage.cs
        // só escala o GameObject (shockwaveSizeMultiplier), nunca pré-multiplica stageNSize, pra
        // não ter 2 caminhos aplicando o mesmo multiplicador (visual E dano desalinhando).
        float outerRadius = outerSize * 0.5f * transform.localScale.x;
        float innerRadius = innerSize * 0.5f * transform.localScale.x;
        var hits = Physics2D.OverlapCircleAll(RingCenter, outerRadius, enemyLayerMask);

        stageTargets.Clear();
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null || stageTargets.Contains(enemy)) continue;

            // Zona segura — já estava dentro do círculo do estágio anterior.
            if (innerRadius > 0f && Vector2.Distance(enemy.transform.position, RingCenter) < innerRadius) continue;

            stageTargets.Add(enemy);
        }

        foreach (var enemy in stageTargets) enemy.TakeDamage(damagePerStage);
    }

    // Animation Event, no fim do clipe inteiro (depois do estágio 3 terminar de mostrar).
    public void AnimationShockwaveEndEvent()
    {
        Destroy(gameObject);
    }

    // Debug — os 3 estágios de uma vez (mesmo critério do showExplosionGizmo do PaladinHammer,
    // mas aqui é OnDrawGizmos, não OnDrawGizmosSelected: este objeto nasce e morre sozinho em
    // Play mode, o usuário nunca consegue clicar pra selecioná-lo a tempo de ver o gizmo).
    // stage1/2/3Size só têm valor depois de Activate() — sem Play, fica tudo em 0 (invisível).
    private void OnDrawGizmos()
    {
        if (!showRingGizmos) return;
        float scale = transform.localScale.x;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(RingCenter, stage1Size * 0.5f * scale);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(RingCenter, stage2Size * 0.5f * scale);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(RingCenter, stage3Size * 0.5f * scale);
    }
}
