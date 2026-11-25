// Etapa 4 (docs/superpowers/specs/2026-11-24-etapa-4-especializacoes-tags-design.md) — estilos de jogo
// de uma especialização. Uma carta pode EXIGIR tags (CardDefinition.requiredTags): basta a especialização
// ter UMA delas. Valores fixos — ficam salvos em assets como int, nunca reordenar.
[System.Flags]
public enum SpecTag
{
    None = 0,
    Dot = 1,
    Projectile = 2,
    Hitscan = 4,
    Pet = 8,
    Tank = 16,
    Shield = 32,
    Heal = 64,
    Size = 128,
    Earthquake = 256,
}
