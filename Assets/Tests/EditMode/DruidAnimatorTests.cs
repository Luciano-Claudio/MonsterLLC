using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// D9.1 — rework do Druid (2 heróis em 1): confere as transições de Animator de cada variante.
// Floresta (controller humano): UltimateTrigger -> LeavesSpellCast.
// Feral (controller do urso): SecondaryAbilityTrigger -> Roar, UltimateTrigger -> Headbutt.
public class DruidAnimatorTests
{
    private const string HumanController = "Assets/Animation/Heros/Druid/Druid.controller";
    private const string BearController = "Assets/Animation/Heros/Druid/Ultimate/Druid_Elk.controller";

    private static (GameObject go, Animator animator) Spawn(string controllerPath, string startState)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Heros/Druid/Druid.prefab");
        var go = Object.Instantiate(prefab);
        go.hideFlags = HideFlags.HideAndDontSave;
        foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        var animator = go.GetComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        animator.Rebind();
        animator.Update(0f);
        animator.Play(startState, 0, 0f);
        animator.Update(0.016f);
        return (go, animator);
    }

    private static void AssertTriggerGoesTo(string controller, string trigger, string expectedState)
    {
        var (go, animator) = Spawn(controller, "Idle");
        try
        {
            animator.SetTrigger(trigger);
            animator.Update(0.016f);
            animator.Update(0.016f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(expectedState), $"{trigger} não levou a {expectedState}");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Forest_UltimateTrigger_PlaysLeavesSpellCast() =>
        AssertTriggerGoesTo(HumanController, "UltimateTrigger", "LeavesSpellCast");

    [Test]
    public void Feral_ShiftTrigger_PlaysRoar() =>
        AssertTriggerGoesTo(BearController, "SecondaryAbilityTrigger", "Roar");

    [Test]
    public void Feral_UltimateTrigger_PlaysHeadbutt() =>
        AssertTriggerGoesTo(BearController, "UltimateTrigger", "Headbutt");
}
