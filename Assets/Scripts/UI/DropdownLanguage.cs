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

        initialLanguageIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
        dropdownLanguage.value = initialLanguageIndex;
    }

    public void ConfirmLanguage()
    {
        int selected = dropdownLanguage.value;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[selected];
        initialLanguageIndex = selected;
    }

    public void CancelLanguage()
    {
        dropdownLanguage.value = initialLanguageIndex;
    }
}
