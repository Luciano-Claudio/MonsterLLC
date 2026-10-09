using UnityEngine;

// 1 das até 8 espadas da Ultimate do Paladin (GDD Seção 17.7, ver PaladinOrbitingBlades) —
// cada espada tem seu próprio Animator (Start/Cycle/End) e seu próprio Collider2D de dano. A
// órbita em si (girar ao redor do Paladin) não é responsabilidade desta espada: ela só
// acompanha o pai (PaladinOrbitingBlades.Update() rotaciona o próprio Transform, e como as 8
// espadas são filhas dele, giram juntas automaticamente). Aqui só cuida da própria animação
// (surgir/ficar girando no lugar/desaparecer) e do próprio hit — por isso o Collider2D precisa
// estar neste mesmo GameObject (OnTriggerEnter2D só dispara no GameObject do próprio collider).
public class PaladinOrbitingBlade : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D hitbox; // precisa de Rigidbody2D Kinematic no mesmo GameObject (trigger sem Rigidbody2D não gera evento)

    private PaladinOrbitingBlades owner;

    public void Init(PaladinOrbitingBlades bladeOwner)
    {
        owner = bladeOwner;
    }

    public void PlayStart()
    {
        gameObject.SetActive(true);
        if (hitbox != null) hitbox.enabled = false; // só liga de verdade no AnimationStartEndEvent
        if (animator != null) animator.Play("Start");
        else AnimationStartEndEvent();
    }

    // Animation Event, no fim do clipe Start — só aqui o collider passa a causar dano de verdade.
    public void AnimationStartEndEvent()
    {
        if (hitbox != null) hitbox.enabled = true;
        if (animator != null) animator.Play("Cycle");
    }

    public void PlayEnd()
    {
        if (hitbox != null) hitbox.enabled = false;
        if (animator != null) animator.Play("End");
        else AnimationEndEndEvent();
    }

    // Animation Event, no fim do clipe End.
    public void AnimationEndEndEvent()
    {
        gameObject.SetActive(false);
        if (owner != null) owner.NotifyBladeEndFinished();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.NotifyHit(other);
    }
}
