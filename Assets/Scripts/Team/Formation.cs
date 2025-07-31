using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class Formation
{
    public string FormationId => formationId;
    public string FormationName => formationName;
    public List<Coord> Coords => coords;
    public int KickOff => kickOff;

    [SerializeField] private string formationId;
    [SerializeField] private string formationName;
    [SerializeField] private List<Coord> coords = new List<Coord>(4);
    [SerializeField] private int kickOff;
    [SerializeField] private string tableCollectionName = "FormationNames";



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

    public void Initialize(FormationData formationData)
    {
        formationId = formationData.formationId;
    
        for (int i = 0; i < formationData.coordIds.Length; i++) 
        {
            CoordData coordData  = TeamManager.Instance.GetCoordDataById(formationData.coordIds[i]);

            String auxString = coordData.position;
            Position auxPosition;
            bool isValid = Enum.TryParse(auxString, true, out auxPosition); // case-insensitive parse
            if (!isValid)
                auxPosition = Position.Fw;

            coords.Add(new Coord (coordData.coordId, auxPosition, new Vector3 (coordData.x, coordData.y, coordData.z))); 
        }

        kickOff = formationData.kickOff;

        SetName();
    }

    private async void SetName()
    {
        var handle = LocalizationSettings.StringDatabase.GetTableAsync(tableCollectionName);
        await handle.Task;

        var table = handle.Result;
        formationName = formationId;
        if (table != null)
        {
            var entry = table.GetEntry(formationId);
            if (entry != null)
                formationName = entry.GetLocalizedString();
        }
    }
}
