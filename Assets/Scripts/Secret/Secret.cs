using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class Secret : MonoBehaviour
{
    public string SecretId => secretId;
    public string SecretName => secretName;
    public Category Category => category;
    public Element Element => element;
    public int Power => power;
    public int Cost => cost;

    [SerializeField] private string secretId;
    [SerializeField] private string secretName;
    [SerializeField] private Category category;
    [SerializeField] private Element element;
    [SerializeField] private int power;
    [SerializeField] private int cost;
    [SerializeField] private string tableCollectionName = "SecretNames";

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;   
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale obj)
    {
        // Update the text whenever the language changes
        SetName();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(SecretData secretData)
    {
        secretId = secretData.secretId;

        string auxString = secretData.category;
        Category auxCategory;
        bool isValid = Enum.TryParse(auxString, true, out auxCategory); // case-insensitive parse
        if (isValid)
        {
            category = auxCategory;
        } else {
            category = Category.Shoot;
        }

        auxString = secretData.element;
        Element auxElement;
        isValid = Enum.TryParse(auxString, true, out auxElement); // case-insensitive parse
        if (isValid)
        {
            element = auxElement;
        } else {
            element = Element.Fire;
        }

        power = secretData.power;
        cost = secretData.cost;

        SetName();
    }

    private async void SetName()
    {
        var handle = LocalizationSettings.StringDatabase.GetTableAsync(tableCollectionName);
        await handle.Task;

        var table = handle.Result;
        secretName = secretId;
        if (table != null)
        {
            var entry = table.GetEntry(secretId);
            if (entry != null)
                secretName = entry.GetLocalizedString();
        }
    }
}
