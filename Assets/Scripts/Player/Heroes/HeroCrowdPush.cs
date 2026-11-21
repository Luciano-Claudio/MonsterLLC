using System.Collections.Generic;
using UnityEngine;

// D9.4 (decisão do usuário, opção C — docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md,
// 4.8): o herói empurra a horda. O corpo dele não colide mais com o corpo dos monstros comuns
// (EnemyController.IgnoreHeroBodyCollision); no lugar, andando contra monstros, ele avança mais
// devagar e os monstros da frente são empurrados pros lados. Parado, nada muda: o cerco
// continua cercando. Boss nunca é empurrado (imune a knockback) e continua bloqueando.
// Adicionado pelo HeroController no Awake (mesmo padrão do HeroPickupRadius).
public class HeroCrowdPush : MonoBehaviour
{
    [SerializeField] private float pushSpeedMultiplier = 0.5f; // 🔢 velocidade do herói enquanto empurra
    [SerializeField] private float probeRadius = 0.6f;         // 🔢 raio da sonda à frente
    [SerializeField] private float probeForwardOffset = 0.5f;  // 🔢 distância da sonda à frente do herói
    [SerializeField] private float lateralForce = 6f;          // 🔢 força do empurrão lateral (knockback)

    private readonly Collider2D[] probeBuffer = new Collider2D[16];
    private readonly List<EnemyController> pushedBuffer = new List<EnemyController>(16);

    // Chamado pelo HeroController no frame em que decide a velocidade: empurra quem está à frente
    // na direção do movimento e devolve o multiplicador de velocidade (1 = ninguém na frente).
    public float EvaluateAndPush(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude < 0.0001f) return 1f;

        Vector2 dir = moveInput.normalized;
        Vector2 heroPos = transform.position;
        int hits = Physics2D.OverlapCircle(heroPos + dir * probeForwardOffset, probeRadius, EnemyController.EnemyBodiesFilter, probeBuffer);
        EnemyController.CollectDistinct(probeBuffer, hits, pushedBuffer);

        bool pushedAny = false;
        for (int i = 0; i < pushedBuffer.Count; i++)
        {
            var enemy = pushedBuffer[i];
            if (enemy.IsDead || enemy.IsBoss) continue;
            enemy.ApplyCrowdPush(CrowdPushRules.LateralPush(dir, (Vector2)enemy.transform.position - heroPos), lateralForce);
            pushedAny = true;
        }
        return pushedAny ? pushSpeedMultiplier : 1f;
    }
}
