using System;
using UnityEngine;
using UnityEngine.UI;

// Coordena a HUD de Skills inteira: troca os 3 Hability_Icon pros do herói certo e liga os 3
// botões (Right_Button_Mouse de Primary_Attack/Ultimate, Shift_Button) no HeroController na
// hora que o herói spawna — não dá pra arrastar isso no Inspector porque o HeroController só
// existe em runtime (instanciado por MainMenuUI.SpawnHero()).
public class SkillsHUDUI : MonoBehaviour
{
    [Serializable]
    private class HeroIconSet
    {
        public string heroName; // bate com RunState.hero / MainMenuUI.heroToSpawn (ex.: "Druid")
        public Sprite primary;
        public Sprite shift;
        public Sprite ultimate;
    }

    [Tooltip("1 entrada por herói — arraste os 3 ícones de Assets/Sprites/UI/UI_On_Game/Heros_Skills/<Herói>/ aqui.")]
    [SerializeField] private HeroIconSet[] heroIcons;

    [Header("Hability_Icon de cada skill")]
    [SerializeField] private Image primaryIcon;
    [SerializeField] private Image shiftIcon;
    [SerializeField] private Image ultimateIcon;

    [Header("Botões (ligados ao herói na hora que ele spawna)")]
    [SerializeField] private Button primaryButton;
    [SerializeField] private Button shiftButton;
    [SerializeField] private Button ultimateButton;

    private void OnEnable() => GameEvents.OnHeroSpawned += HandleHeroSpawned;
    private void OnDisable() => GameEvents.OnHeroSpawned -= HandleHeroSpawned;

    private void HandleHeroSpawned(Transform heroTransform)
    {
        ApplyIcons();

        var hero = heroTransform.GetComponent<HeroController>();
        if (hero == null) return;

        primaryButton.onClick.RemoveAllListeners();
        primaryButton.onClick.AddListener(() => hero.TryPrimaryAttack());

        shiftButton.onClick.RemoveAllListeners();
        shiftButton.onClick.AddListener(hero.TryUseSecondaryAbility);

        ultimateButton.onClick.RemoveAllListeners();
        ultimateButton.onClick.AddListener(hero.TryUseUltimate);
    }

    private void ApplyIcons()
    {
        string heroName = MainMenuUI.CurrentRun != null ? MainMenuUI.CurrentRun.hero : null;
        if (string.IsNullOrEmpty(heroName)) return;

        foreach (var set in heroIcons)
        {
            if (set.heroName != heroName) continue;

            if (primaryIcon != null) primaryIcon.sprite = set.primary;
            if (shiftIcon != null) shiftIcon.sprite = set.shift;
            if (ultimateIcon != null) ultimateIcon.sprite = set.ultimate;
            return;
        }
    }
}
