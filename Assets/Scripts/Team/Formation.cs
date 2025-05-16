using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Formation : MonoBehaviour
{
    public string FormationId => formationId;
    public string FormationNameEn => formationNameEn;
    public string FormationNameJa => formationNameJa;
    public List<Vector3> Coords => coords;
    public int KickOff => kickOff;

    [SerializeField] private string formationId;
    [SerializeField] private string formationNameEn;
    [SerializeField] private string formationNameJa;
    [SerializeField] private List<Vector3> coords;
    [SerializeField] private int kickOff;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(FormationData formationData)
    {
        formationId = formationData.formationId;
        formationNameEn = formationData.formationNameEn;
        formationNameJa = formationData.formationNameJa;
    
        for (int i = 0; i < formationData.coordIds.Length; i++) 
        {
            CoordData coordData  = TeamManager.Instance.GetCoordDataById(formationData.coordIds[i]);
            coords.Add(new Vector3 (coordData.x, coordData.y, coordData.z)); 
        }

        kickOff = formationData.kickOff;
    }
}
