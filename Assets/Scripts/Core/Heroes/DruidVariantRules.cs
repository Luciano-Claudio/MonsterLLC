public enum DruidFormAction { None, TransformIn, TransformOut }

// Druid (D9.1, 2 heróis em 1): a forma acompanha o bool isFeral comparando, todo frame, a forma
// DESEJADA com a ATUAL e trocando assim que o Druid estiver livre (nunca corta ação, nunca no
// meio de uma transformação). Estado, não evento: antes a troca só era tentada no frame em que
// o bool mudava, e desligar isFeral com o urso ocupado deixava o Druid urso pra sempre
// (achado da revisão final do D9.1).
public static class DruidVariantRules
{
    public static DruidFormAction Next(bool wantFeral, bool isBear, bool transforming, bool busy)
    {
        if (transforming || busy) return DruidFormAction.None;
        if (wantFeral && !isBear) return DruidFormAction.TransformIn;
        if (!wantFeral && isBear) return DruidFormAction.TransformOut;
        return DruidFormAction.None;
    }
}
