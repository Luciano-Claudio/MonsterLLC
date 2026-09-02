using UnityEngine;

public class TestTimer : MonoBehaviour
{
    public float timeRemaining = 100f;

    private void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.IsPaused) return;

        timeRemaining -= Time.deltaTime;
        if (Mathf.FloorToInt(timeRemaining * 10) % 10 == 0)
        {
            Debug.Log($"Time remaining: {timeRemaining:F1}");
        }
    }
}
