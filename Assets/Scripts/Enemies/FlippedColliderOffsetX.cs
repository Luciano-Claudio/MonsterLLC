using UnityEngine;

// Corrige o offset.x do collider físico (não-trigger) quando a arte reaproveita sprite
// espelhado (FlipX) pra cobrir a direção oposta — sem isso, o collider fica "do lado errado"
// em relação ao corpo visível sempre que o Animator troca pra um estado espelhado, já que o
// FlipX é uma propriedade do SpriteRenderer/clipe de animação, nunca do Collider2D.
//
// Todo offset.x já configurado nos prefabs foi tunado pra direção E (flipX = false) — quando
// o sprite espelha pra W, o offset.x correto é sempre esse mesmo valor invertido (× -1), então
// não precisa de 2 campos configurados à mão por monstro: só captura o que já está no collider
// no Awake (o valor "E" já tunado) e espelha automaticamente conforme o flipX mudar.
[RequireComponent(typeof(SpriteRenderer))]
public class FlippedColliderOffsetX : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D physicalCollider; // o físico (não-trigger), não os de dano/ataque

    private SpriteRenderer spriteRenderer;
    private float offsetXFacingEast; // capturado do próprio collider no Awake — já vem certo do prefab
    private bool lastFlipX;
    private bool initialized;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (physicalCollider == null) physicalCollider = GetComponent<CapsuleCollider2D>();
        if (physicalCollider != null) offsetXFacingEast = physicalCollider.offset.x;
    }

    // LateUpdate, não Update — precisa rodar depois que o Animator (que decide o flipX do
    // frame) já aplicou a pose desse frame, senão fica sempre 1 frame atrasado.
    private void LateUpdate()
    {
        if (physicalCollider == null || spriteRenderer == null) return;
        if (initialized && spriteRenderer.flipX == lastFlipX) return;

        lastFlipX = spriteRenderer.flipX;
        initialized = true;

        var offset = physicalCollider.offset;
        offset.x = lastFlipX ? -offsetXFacingEast : offsetXFacingEast;
        physicalCollider.offset = offset;
    }
}
