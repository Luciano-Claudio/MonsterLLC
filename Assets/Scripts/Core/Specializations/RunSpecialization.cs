// Etapa 4 — a especialização escolhida NESTA run (vale até o fim, sem troca). Estático porque só
// existe 1 herói jogável; zerado no começo de cada run (SpecializationScreenUI, OnDayStart) — com o
// domain reload desligado, o valor de uma run/Play anterior vazaria sem isso.
public static class RunSpecialization
{
    public static SpecializationDefinition Current { get; private set; }
    public static bool IsChosen => Current != null;
    public static SpecTag Tags => Current != null ? Current.tags : SpecTag.None;

    public static event System.Action<SpecializationDefinition> Chosen;

    public static bool Is(string specId) => Current != null && Current.id == specId;

    public static void Choose(SpecializationDefinition spec)
    {
        Current = spec;
        Chosen?.Invoke(spec);
    }

    public static void Reset() => Current = null;
}
