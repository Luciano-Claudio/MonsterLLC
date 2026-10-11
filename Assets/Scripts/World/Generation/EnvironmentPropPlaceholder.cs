using UnityEngine;

// Marcador visual (Gizmo) pra prefabs de EnvironmentPropSO que ainda não têm arte final
// (árvore/rocha reais entram depois — ver Sprint P2, docs/sprints/). Sem Collider nem
// SpriteRenderer assumidos: é só pra conseguir ver no Scene view onde o algoritmo de
// posicionamento colocou cada instância antes da arte chegar.
public class EnvironmentPropPlaceholder : MonoBehaviour
{
    public float radius = 0.4f;
    public Color color = new Color(0.2f, 0.8f, 0.3f);

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
