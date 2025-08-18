using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;

public class DropdownLanguage : MonoBehaviour
{
    public TMP_Dropdown dropdownLanguage;

    private int initialLanguageIndex;

    void Start() // Or call this when opening settings
    {
        InitializeDropdown();
    }

    public void InitializeDropdown()
    {
        dropdownLanguage.ClearOptions();
        List<string> options = new List<string>();
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            options.Add(locale.Identifier.CultureInfo.NativeName);
        }
        dropdownLanguage.AddOptions(options);

        // Use SettingsManager to retrieve the language index
        int defaultLanguageIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
        int savedLanguageIndex = SettingsManager.GetLanguageIndex();

        // Fallback: If saved index is out of bounds (e.g. localization changed), use default
        if (savedLanguageIndex < 0 || savedLanguageIndex >= LocalizationSettings.AvailableLocales.Locales.Count)
            savedLanguageIndex = defaultLanguageIndex;

        dropdownLanguage.value = savedLanguageIndex;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[savedLanguageIndex];
        initialLanguageIndex = savedLanguageIndex;
    }

    public void ConfirmLanguage()
    {
        int selected = dropdownLanguage.value;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[selected];
        initialLanguageIndex = selected;
        SettingsManager.SetLanguageIndex(selected);
    }

    public void CancelLanguage()
    {
        dropdownLanguage.value = initialLanguageIndex;
    }

    public void OnValueChanged() {
        AudioManager.Instance.PlaySfx("SfxMenuChange");
    }
}
