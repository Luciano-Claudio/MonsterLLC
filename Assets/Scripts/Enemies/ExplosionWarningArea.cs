using UnityEngine;

// D9.4 (spec D9, Seção 2.5 — "o jogador nunca pode morrer sem entender do quê"): área no chão
// mostrando o raio REAL de uma explosão, piscando cada vez mais rápido conforme ela chega perto.
// Usado pela bomba do Goblin Sapper. Mesma sprite neutra (círculo branco, 2 unidades de diâmetro
// = escala igual ao raio) da área de explosão do Burning Skull, cor aplicada em runtime.
public class ExplosionWarningArea : MonoBehaviour
{
    [SerializeField] private float minBlinkHz = 2f; // 🔢 piscada no começo
    [SerializeField] private float maxBlinkHz = 8f; // 🔢 piscada perto da explosão
    [SerializeField] private float minAlphaFactor = 0.35f; // 🔢 quanto a cor apaga no "vale" da piscada

    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float urgency;
    private float phase;

    public static ExplosionWarningArea Create(Transform parent, Sprite sprite, int sortingLayerId, int sortingOrder, Color color)
    {
        var go = new GameObject("ExplosionWarningArea");
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = sortingLayerId;
        sr.sortingOrder = sortingOrder;
        var area = go.AddComponent<ExplosionWarningArea>();
        area.spriteRenderer = sr;
        area.baseColor = color;
        sr.color = color;
        go.transform.localScale = Vector3.zero;
        return area;
    }

    public void Show(float radius) => transform.localScale = new Vector3(radius, radius, 1f);

    public void SetUrgency(float t01) => urgency = Mathf.Clamp01(t01);

    private void Update()
    {
        if (spriteRenderer == null || !GameplayGate.IsActive) return; // pausado: congela a piscada junto com o resto
        phase += Time.deltaTime * Mathf.Lerp(minBlinkHz, maxBlinkHz, urgency) * Mathf.PI * 2f;
        float wave = 0.5f + 0.5f * Mathf.Sin(phase);
        var c = baseColor;
        c.a = baseColor.a * Mathf.Lerp(minAlphaFactor, 1f, wave);
        spriteRenderer.color = c;
    }
}
