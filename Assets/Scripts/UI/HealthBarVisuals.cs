using UnityEngine;
using UnityEngine.UI;

// Lógica compartilhada entre PlayerHealthBarUI, MonsterHealthBarUI e DayTimeBarUI — todos usam
// a mesma estrutura de 3 imagens (Background, Bar, Head_Bar) com o mesmo problema: as sprites
// mantêm a proporção original do canvas (ex.: 64x16) com margem transparente ao redor do
// conteúdo opaco real (ex.: 40x16 centralizado), então tanto o fillAmount nativo quanto a
// posição do Head_Bar precisam ser remapeados pra dentro da faixa de conteúdo real, não o rect
// inteiro.
public static class HealthBarVisuals
{
    // Image.fillAmount nativo corta em cima do rect inteiro (incluindo a margem transparente) —
    // sem remapear, o preenchimento visível fica "parado" em 100% até passar da margem direita,
    // e "parado" em 0% antes de passar da margem esquerda.
    public static float NativeFillAmount(float fillAmount, float contentLeftFraction, float contentRightFraction)
        => Mathf.Lerp(contentLeftFraction, contentRightFraction, fillAmount);

    // Posição X (no espaço local do próprio bar) onde o preenchimento visível termina — usado
    // tanto pelo Head_Bar quanto pelos Boss Signs (que precisam da mesma posição, sem ter
    // nenhum Head_Bar próprio).
    public static float FillEdgeX(Image bar, float nativeFillAmount)
    {
        RectTransform rect = bar.rectTransform;
        return rect.rect.xMin + nativeFillAmount * rect.rect.width;
    }

    public static void Apply(Image bar, RectTransform headBar, float fillAmount, float contentLeftFraction, float contentRightFraction)
    {
        float nativeFillAmount = NativeFillAmount(fillAmount, contentLeftFraction, contentRightFraction);
        bar.fillAmount = nativeFillAmount;

        if (headBar == null) return;

        float visibleFillEdgeX = FillEdgeX(bar, nativeFillAmount);

        // O Head_Bar também tem margem (conteúdo colado na borda esquerda do seu próprio
        // canvas) — a borda ESQUERDA do seu RectTransform é que precisa cair exatamente na
        // borda visível do preenchimento, não o pivot abstrato.
        RectTransform barRect = bar.rectTransform;
        float headPivotTargetX = visibleFillEdgeX + headBar.pivot.x * headBar.rect.width;
        float anchorRefX = barRect.rect.xMin + headBar.anchorMin.x * barRect.rect.width;
        headBar.anchoredPosition = new Vector2(headPivotTargetX - anchorRefX, headBar.anchoredPosition.y);
    }
}
