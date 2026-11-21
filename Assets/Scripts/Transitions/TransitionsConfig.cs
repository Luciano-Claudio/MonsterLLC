using EasyTransition;
using UnityEngine;

// D9.5 (docs/superpowers/specs/2026-11-21-d9-5-estrutura-run-design.md) — quais efeitos do Easy
// Transitions o jogo usa em cada momento. Fica em Resources (Resources/TransitionsConfig.asset)
// pra ser carregado de qualquer cena sem editar cena nenhuma.
[CreateAssetMenu(fileName = "TransitionsConfig", menuName = "MonsterLLC/Transitions Config")]
public class TransitionsConfig : ScriptableObject
{
    [Tooltip("Menu -> loading (só a metade que escurece) e loading -> jogo (só a metade que clareia). Decisão do usuário: Fade.")]
    public TransitionSettings fade;

    [Tooltip("Morte -> tela de resumo total (fecha e abre na mesma cena). Decisão do usuário: CircleWipe.")]
    public TransitionSettings death;

    private static TransitionsConfig cached;

    public static TransitionsConfig Load()
    {
        if (cached == null) cached = Resources.Load<TransitionsConfig>("TransitionsConfig");
        return cached;
    }
}
