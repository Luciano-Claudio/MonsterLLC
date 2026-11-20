using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Bug real (2026-11-19): o Mage morreu atacando andando e a animação de morte nunca terminou
// (AnimationDieEndEvent não chegou, respawn forçado pelo timeout). Causa: um AttackTrigger ainda
// armado no frame da morte — o Animator consome o DieTrigger primeiro (die vem antes de Attack
// na lista do AnyState do Mage/Ranger) e, no frame seguinte, o AttackTrigger que sobrou leva
// AnyState -> Attack_Start, tirando o herói do die. Na morte, todo trigger pendente é limpo
// antes do DieTrigger (HeroController.OnDeath -> AnimatorTriggerUtility.ResetAllTriggers).
public class DeathTriggerTests
{
    private static GameObject SpawnHero(string hero)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Heros/{hero}/{hero}.prefab");
        var go = Object.Instantiate(prefab);
        go.hideFlags = HideFlags.HideAndDontSave;
        foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        return go;
    }

    [TestCase("Mage", "Attack_Continuous_Walk", "die")]
    [TestCase("Ranger", "Attack_Walk", "die")]
    [TestCase("Barbarian", "Attack_Walk", "Die")]
    public void PendingAttackTrigger_DoesNotPullHeroOutOfDie(string hero, string fromState, string dieState)
    {
        var go = SpawnHero(hero);
        try
        {
            var animator = go.GetComponent<Animator>();
            animator.Rebind();
            animator.Update(0f);
            animator.Play(fromState, 0, 0.3f);
            animator.Update(0.016f);

            animator.SetTrigger("AttackTrigger"); // armado no mesmo frame da morte
            AnimatorTriggerUtility.ResetAllTriggers(animator);
            animator.SetTrigger("DieTrigger");

            for (int i = 0; i < 4; i++) animator.Update(0.016f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(dieState),
                $"{hero} saiu do {dieState} por causa de um trigger pendente");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
