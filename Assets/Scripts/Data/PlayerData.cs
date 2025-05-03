using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObject/PlayerData")]
public class PlayerData : ScriptableObject
{
    public string playerId;
    public string playerNameEn;
    public string playerNameJa;
    public string gender;
    public string element;
    public string position;
    public int hp;
    public int sp;
    public int kick;
    public int body;
    public int control;
    public int guard;
    public int speed;
    public int stamina;
    public int courage;	
    public int freedom;
}
