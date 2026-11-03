using UnityEngine;

// Unity só entrega OnTrigger Enter/Exit pro script que está no MESMO GameObject do collider que
// colidiu — nunca sobe pro pai sozinho. O GroundCrack tem 4 colliders, 1 por filho (Crack_NE/
// NW/SE/SW); este componente é adicionado via código (GroundCrack.AttachRelay(), não precisa
// anexar manualmente no prefab) em cada um desses filhos, só pra repassar o evento pro pai.
public class GroundCrackHitRelay : MonoBehaviour
{
    public GroundCrack Owner;

    private void OnTriggerEnter2D(Collider2D other) => Owner?.HandleTriggerEnter(other);
    private void OnTriggerExit2D(Collider2D other) => Owner?.HandleTriggerExit(other);
}
