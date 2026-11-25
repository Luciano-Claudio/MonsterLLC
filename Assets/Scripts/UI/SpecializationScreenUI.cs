using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Etapa 4 (docs/superpowers/specs/2026-11-24-etapa-4-especializacoes-tags-design.md) — tela
// "Specialization_Choose": a PRIMEIRA escolha da run. Abre quando o jogo entra em Gameplay (ou já no
// Start, no Play direto da cena), com o jogo pausado, e só fecha depois da escolha (clique ou 1/2).
// Herói sem 2 especializações no catálogo: a tela não abre e as tags ficam None.
// Fica no Canvas (não no próprio Specialization_Choose, que começa desligado e não teria Awake).
public class SpecializationScreenUI : MonoBehaviour
{
    [Serializable]
    public struct SpecButtonView
    {
        public Button button;
        public Image icon;
        public TMP_Text name;
        public TMP_Text description;
    }

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private SpecButtonView[] buttons = new SpecButtonView[2];

    private readonly List<SpecializationDefinition> options = new();
    private readonly UnityEngine.Events.UnityAction[] clickHandlers = new UnityEngine.Events.UnityAction[2];
    private bool pausedByMe;

    public bool IsVisible => root != null && root.activeInHierarchy;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        for (int i = 0; i < clickHandlers.Length; i++)
        {
            int index = i;
            clickHandlers[i] = () => PressSlot(index);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnDayStart += HandleRunStart;
        GameEvents.OnGameStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnDayStart -= HandleRunStart;
        GameEvents.OnGameStateChanged -= HandleStateChanged;
        Close();
    }

    private void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Gameplay) TryOpen();
    }

    // Começo de run: a escolha da run anterior não vale mais (domain reload desligado = estático vaza).
    private void HandleRunStart() => RunSpecialization.Reset();

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Gameplay) TryOpen();
    }

    public void TryOpen()
    {
        if (IsVisible || RunSpecialization.IsChosen || root == null) return;
        var hero = HeroController.Instance;
        var catalog = SpecializationCatalog.Load();
        if (hero == null || catalog == null) return;
        catalog.ForHero(hero.GetType().Name, options);
        if (options.Count < 2) return;

        if (heroName != null) heroName.text = options[0].heroId;
        for (int i = 0; i < buttons.Length && i < options.Count; i++) Bind(i, options[i]);

        root.SetActive(true);
        if (TimeManager.Instance != null) { TimeManager.Instance.PauseBy(this); pausedByMe = true; }
    }

    private void Bind(int i, SpecializationDefinition spec)
    {
        var view = buttons[i];
        if (view.name != null) view.name.text = spec.displayName;
        if (view.description != null) view.description.text = spec.description;
        if (view.icon != null && spec.icon != null) view.icon.sprite = spec.icon; // sem ícone: fica o do layout
        if (view.button != null)
        {
            view.button.onClick.RemoveListener(clickHandlers[i]);
            view.button.onClick.AddListener(clickHandlers[i]);
        }
    }

    private void Update()
    {
        if (!IsVisible) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PressKey(0);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PressKey(1);
    }

    // Teclas 1/2: ignoradas com o menu de pausa aberto (o clique já fica bloqueado pelo menu por cima).
    public bool PressKey(int index)
    {
        if (PauseMenuUI.IsOpen) return false;
        PressSlot(index);
        return true;
    }

    public void PressSlot(int index)
    {
        if (!IsVisible || index < 0 || index >= options.Count) return;
        var spec = options[index];
        Close();
        RunSpecialization.Choose(spec);
    }

    private void Close()
    {
        if (root != null) root.SetActive(false);
        if (pausedByMe && TimeManager.Instance != null) TimeManager.Instance.ResumeBy(this);
        pausedByMe = false;
    }
}
