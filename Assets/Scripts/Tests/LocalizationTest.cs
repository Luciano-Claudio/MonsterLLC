using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationTest : MonoBehaviour
{
    public LocalizedString testString; // no Inspector: Table = "UI Text", Entry = "test.hello"

    private void Start()
    {
        testString.StringChanged += value => Debug.Log($"[Localization] {value}");
    }

    [ContextMenu("Switch To English")]
    public void SwitchToEnglish() => SetLocale("en");

    [ContextMenu("Switch To Portuguese")]
    public void SwitchToPortuguese() => SetLocale("pt-BR");

    private void SetLocale(string code)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
    }
}
