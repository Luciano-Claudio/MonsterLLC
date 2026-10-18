// Qualquer entidade que aceita dano por TakeDamage(float) — hoje só HeroController e
// EnemyController, os 2 "alvos de dano" do jogo. Existe só pra permitir componentes
// genéricos (ex.: StatusEffectController) aplicarem dano sem precisar saber se estão
// grudados num herói ou num monstro.
//
// `isDirectPlayerHit` (default true, Game Feel) — declarado na INTERFACE, não só em
// EnemyController, porque quem mais precisa chamar com `false` (StatusEffectController, pro
// tick de DoT) mora no assembly Core, que não referencia Assembly-CSharp (onde
// EnemyController/HeroController vivem) — não dá pra nem mencionar o tipo concreto lá, só
// chamar através de IDamageable mesmo. false = dano repetido/passivo (DoT) — nunca aciona o
// shake/zoom de hit direto (HitFeelController). HeroController ignora esse parâmetro (só
// EnemyController usa de verdade), mas precisa declará-lo pra continuar implementando a interface.
public interface IDamageable
{
    void TakeDamage(float amount, bool isDirectPlayerHit = true);
}
