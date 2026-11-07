using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

// Liga o Language_Dropdown (Settings/Display, Main_Menu e _TestScene) ao idioma real do jogo —
// GDD Seção 1 (Sistema de Tradução). Populado em runtime a partir das Locales cadastradas no
// LocalizationSettings (hoje: English, Português (Brasil)), não hardcoded, pra não precisar mexer
// aqui se um 3º idioma for adicionado depois.
public class LanguageSelector : MonoBehaviour
{
    private const string PlayerPrefsKey = "language_locale_code";

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        // Idioma escolhido numa sessão anterior continua valendo — sem isso o jogo sempre voltaria
        // pro idioma padrão do LocalizationSettings a cada novo Play/build.
        string savedCode = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(savedCode))
        {
            var savedLocale = LocalizationSettings.AvailableLocales.GetLocale(savedCode);
            if (savedLocale != null) LocalizationSettings.SelectedLocale = savedLocale;
        }

        PopulateOptions();
        dropdown.onValueChanged.AddListener(HandleValueChanged);
    }

    private void PopulateOptions()
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var options = new System.Collections.Generic.List<string>(locales.Count);
        int selectedIndex = 0;

        for (int i = 0; i < locales.Count; i++)
        {
            options.Add(locales[i].Identifier.CultureInfo.NativeName);
            if (locales[i] == LocalizationSettings.SelectedLocale) selectedIndex = i;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(selectedIndex); // não dispara HandleValueChanged de novo
    }

    private void HandleValueChanged(int index)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales[index];
        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString(PlayerPrefsKey, locale.Identifier.Code);
    }
}
