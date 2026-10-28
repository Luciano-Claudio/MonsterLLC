using UnityEngine;

// Um Boss Sign individual (instanciado a partir de Boss_Sign.prefab por BossSignTimelineUI).
// Sabe o próprio instante de spawn (em termos de DayTimer.timeRemaining) e aciona
// Boss_Sign_Alert 1x quando faltam 5s pra esse boss específico nascer.
[RequireComponent(typeof(Animator))]
public class BossSignUI : MonoBehaviour
{
    private static readonly int AlertTrigger = Animator.StringToHash("AlertTrigger");

    private Animator animator;
    private float spawnRemaining;
    private bool alertFired;

    private void Awake() => animator = GetComponent<Animator>();

    public void Initialize(float spawnRemainingValue)
    {
        spawnRemaining = spawnRemainingValue;
        alertFired = false;
    }

    private void OnEnable() => GameEvents.OnTimeChanged += CheckAlert;
    private void OnDisable() => GameEvents.OnTimeChanged -= CheckAlert;

    private void CheckAlert(float timeRemaining)
    {
        if (alertFired) return;
        if (timeRemaining - spawnRemaining > 5f) return;

        alertFired = true;
        animator.SetTrigger(AlertTrigger);
    }
}
