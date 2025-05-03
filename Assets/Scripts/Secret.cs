using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Secret : MonoBehaviour
{
    public string SecretId => secretId;
    public string SecretNameEn => secretNameEn;
    public string SecretNameJa => secretNameJa;
    public Element Element => element;
    public int Power => power;
    public int Cost => cost;

    [SerializeField] private string secretId;
    [SerializeField] private string secretNameEn;
    [SerializeField] private string secretNameJa;
    [SerializeField] private Element element;
    [SerializeField] private int power;
    [SerializeField] private int cost;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(SecretData secretData)
    {
        secretId = secretData.secretId;
        secretNameEn = secretData.secretNameEn;
        secretNameJa = secretData.secretNameJa;

        string auxString = secretData.element;
        Element auxElement;
        bool isValid = Enum.TryParse(auxString, true, out auxElement); // case-insensitive parse
        if (isValid)
        {
            element = auxElement;
        } else {
            element = Element.Fire;
        }

        power = secretData.power;
        cost = secretData.cost;
    }
}
