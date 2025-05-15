using UnityEngine;

[CreateAssetMenu(fileName = "FormationData", menuName = "ScriptableObject/FormationData")]
public class FormationData : ScriptableObject
{
    public string formationId;
    public string formationNameEn;
    public string formationNameJa;
    public string[] coordIds = new string[4];
    public int kickOff;
}
