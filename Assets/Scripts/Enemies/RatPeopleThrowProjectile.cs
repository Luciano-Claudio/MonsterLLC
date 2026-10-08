using UnityEngine;

// Arremesso do Rat People Royalty (Bestiário, exceção pontual) — voa como qualquer projétil
// reto (nasce apontando pro Norte, rotaciona de verdade na direção real), causa dano se
// tocar o player no meio do caminho, e ao alcançar o limite OU acertar o player, toca a
// animação de impacto (um Rat People caído se levantando do chão) — no FIM dessa animação,
// nasce um Rat People vivo na posição exata de onde o projétil parou. Os dois desfechos
// (dano no impacto + nascimento no final) são independentes, decisão do usuário: acertar o
// player não cancela o nascimento, e o nascimento acontece tanto por limite de alcance
// quanto por hit — sempre na posição onde o projétil realmente parou, nunca num ponto
// calculado à parte.
public class RatPeopleThrowProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f; // 🔢 ajustável
    [SerializeField] private float maxDistance = 8f; // 🔢 ajustável
    [SerializeField] private GameObject ratPeoplePrefab; // precisa ter EnemyController — Rat_People.prefab comum
    [SerializeField] private Animator animator; // opcional — toca o clipe de "caído se levantando" no impacto

    private Vector2 direction;
    private float damage;
    private FloorDefinition ownerFloor;
    private float distanceTraveled;
    private bool impacted;

    public void Launch(Vector2 dir, float dmg, FloorDefinition floor)
    {
        direction = dir.normalized;
        damage = dmg;
        ownerFloor = floor;

        // Sprite de referência nasce apontando pra "cima" (N, +Y) — mesma técnica de todo
        // projétil do projeto (EnemyProjectile/RangerArrow/etc.).
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!GameplayGate.IsActive) return;
        if (!FloorActivationCheck.IsActive(ownerFloor, FloorManager.Instance.CurrentFloor)) return;
        if (impacted) return;

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;
        if (distanceTraveled >= maxDistance) Impact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        if (!other.CompareTag("Player")) return;

        var hero = other.GetComponent<HeroController>();
        if (hero != null) hero.TakeDamage(damage);
        Impact();
    }

    private void Impact()
    {
        if (impacted) return;
        impacted = true;

        // A rotação em ângulo livre só faz sentido durante o voo — a animação de impacto
        // (caído/levantando) tem orientação fixa própria (mesmo critério do RogueBomb/
        // MageFireball/ClericProjectile).
        transform.rotation = Quaternion.identity;

        if (animator != null) animator.SetTrigger("ImpactTrigger");
        else SpawnRatPeopleAndDestroy(); // sem Animator/clipe dedicado, nasce na hora (mesma rede de segurança de sempre)
    }

    // Animation Event, no último frame do clipe de impacto.
    public void AnimationImpactEndEvent()
    {
        SpawnRatPeopleAndDestroy();
    }

    private void SpawnRatPeopleAndDestroy()
    {
        if (ratPeoplePrefab != null)
        {
            var obj = Instantiate(ratPeoplePrefab, transform.position, Quaternion.identity);
            var enemy = obj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.ownerFloor = ownerFloor;
                enemy.dropsLoot = false; // Bestiário: "summons criados por este boss não geram loot"
                enemy.ForceImmediateAggro(); // nasceu na cara do player, já sabe que ele existe
            }
        }

        Destroy(gameObject);
    }
}
