using UnityEngine;

public class LargeNumberTest : MonoBehaviour
{
    [ContextMenu("Test Formatting")]
    public void TestFormat()
    {
        Debug.Log(LargeNumberFormatter.Format(850));         // 850
        Debug.Log(LargeNumberFormatter.Format(1500));        // 1.5k
        Debug.Log(LargeNumberFormatter.Format(2_300_000));   // 2.3m
        Debug.Log(LargeNumberFormatter.Format(4_000_000_000)); // 4b
        Debug.Log(LargeNumberFormatter.Format(1_000_000_000_000)); // 1t
    }
}
