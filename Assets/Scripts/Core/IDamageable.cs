// Qualquer entidade que aceita dano por TakeDamage(float) — hoje só HeroController e
// EnemyController, os 2 "alvos de dano" do jogo. Existe só pra permitir componentes
// genéricos (ex.: StatusEffectController) aplicarem dano sem precisar saber se estão
// grudados num herói ou num monstro.
public interface IDamageable
{
    void TakeDamage(float amount);
}
