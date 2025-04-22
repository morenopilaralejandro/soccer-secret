using UnityEngine;

public class Player : MonoBehaviour
{
    public string id;
    public string playerName;
    public string gender;
    public string type;
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

    public void Initialize(PlayerData playerData)
    {
        id = playerData.id;
        playerName = playerData.playerName;
        gender = playerData.gender;
        type = playerData.type;
        position = playerData.position;
        hp = playerData.hp;
        sp = playerData.sp;
        kick = playerData.kick;
        body = playerData.body;
        control = playerData.control;
        guard = playerData.guard;
        speed = playerData.speed;
        stamina = playerData.stamina;
        courage = playerData.courage;
        freedom = playerData.freedom;

        // Additional initialization logic can go here
    }
}
