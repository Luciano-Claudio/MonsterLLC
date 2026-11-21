using UnityEngine;

// D9.4 (spec D9, Seção 2.5 — "o jogador nunca pode morrer sem entender do quê"; formato pedido
// pelo usuário em 2026-11-21): aviso de explosão em 2 círculos no chão.
// - Externo: a área COMPLETA da explosão, vermelho bem transparente, desde o começo.
// - Interno: um pouco mais forte, cresce do centro (SetProgress 0 -> 1) e preenche a área toda no
//   instante da explosão.
// Mesma sprite neutra (círculo branco de 2 unidades de diâmetro: escala = raio) da área do
// Burning Skull, cor aplicada em runtime. Usado pela bomba do Goblin Sapper.
public class ExplosionWarningArea : MonoBehaviour
{
    private Transform inner;

    public static ExplosionWarningArea Create(Transform parent, Sprite sprite, int sortingLayerId, int sortingOrder, Color outerColor, Color innerColor)
    {
        var go = new GameObject("ExplosionWarningArea");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.zero;
        AddCircle(go, sprite, sortingLayerId, sortingOrder, outerColor);

        var innerGo = new GameObject("Fill");
        innerGo.transform.SetParent(go.transform, false);
        innerGo.transform.localScale = Vector3.zero;
        AddCircle(innerGo, sprite, sortingLayerId, sortingOrder + 1, innerColor);

        var area = go.AddComponent<ExplosionWarningArea>();
        area.inner = innerGo.transform;
        return area;
    }

    private static void AddCircle(GameObject go, Sprite sprite, int sortingLayerId, int sortingOrder, Color color)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = sortingLayerId;
        sr.sortingOrder = sortingOrder;
        sr.color = color;
    }

    // Raio real da explosão (o círculo externo).
    public void Show(float radius)
    {
        gameObject.SetActive(true);
        transform.localScale = new Vector3(radius, radius, 1f);
    }

    // 0 = círculo interno acabou de nascer; 1 = preencheu a área (explode agora).
    public void SetProgress(float t01)
    {
        float t = Mathf.Clamp01(t01);
        inner.localScale = new Vector3(t, t, 1f);
    }

    public void Hide() => gameObject.SetActive(false);
}
