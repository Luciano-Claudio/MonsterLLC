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

    // Bug real (2026-11-26): o Ranger morreu por tick de Bleed segurando o ataque — o resto do Update
    // (e os Update dos heróis, ex.: fim da camuflagem/coruja/pulso, pet, escudo automático) disparava
    // triggers DEPOIS do DieTrigger. Herói morto não aceita nenhum trigger além do DieTrigger.
    [TestCase("Assassin/Assassin")]
    [TestCase("Barbarian/Barbarian")]
    [TestCase("Blood_Mage/BloodMage")]
    [TestCase("Cleric/Cleric")]
    [TestCase("Druid/Druid")]
    [TestCase("Gunslinger/Gunslinger")]
    [TestCase("Mage/Mage")]
    [TestCase("Paladin/Paladin")]
    [TestCase("Ranger/Ranger")]
    [TestCase("Rogue/Rogue")]
    public void DeadHero_IgnoresEveryTriggerButDie(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Heros/{path}.prefab");
        var go = Object.Instantiate(prefab);
        go.hideFlags = HideFlags.HideAndDontSave;
        foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        try
        {
            // O asmdef dos testes não referencia o assembly dos heróis — HeroController por reflexão.
            System.Type heroType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                if ((heroType = assembly.GetType("HeroController")) != null) break;
            Assert.IsNotNull(heroType, "HeroController não encontrado");
            var hero = go.GetComponent(heroType);
            var animator = go.GetComponent<Animator>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            heroType.GetField("animator", flags).SetValue(hero, animator);
            heroType.GetField("isDead", flags).SetValue(hero, true);
            var trigger = heroType.GetMethod("AnimatorTrigger", flags);

            animator.Rebind();
            animator.Update(0f);
            trigger.Invoke(hero, new object[] { "DieTrigger" });
            animator.Update(0.016f);
            Assert.IsTrue(InDie(animator), $"{path}: o DieTrigger não levou pro die");

            foreach (var parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name != "DieTrigger")
                    trigger.Invoke(hero, new object[] { parameter.name });
            for (int i = 0; i < 4; i++) animator.Update(0.016f);
            Assert.IsTrue(InDie(animator), $"{path}: um trigger depois da morte tirou o herói do die");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static bool InDie(Animator animator)
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        return state.IsName("die") || state.IsName("Die");
    }
}
