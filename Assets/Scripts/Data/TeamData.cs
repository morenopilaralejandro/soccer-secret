using UnityEngine;

[CreateAssetMenu(fileName = "TeamData", menuName = "ScriptableObject/TeamData")]
public class TeamData : ScriptableObject
{
    public string teamId;
    public string teamNameEn;
    public string teamNameJa;
    public int lv;
    public string formation;
    public string[] playerIds = new string[4];
}
