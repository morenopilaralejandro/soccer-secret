using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameFrozen { get; private set; } = false;
    public GameObject OffPlayer { get; set; }
    public GameObject DefPlayer { get; set; }
    public bool OffPlayerIsAlly { get; set; }
    public int DuelType { get; set; }
    public int Command { get; set; }
    public string SecretId { get; set; }

    public List<GameObject> allyPlayers;
    public List<GameObject> oppPlayers;    
    public Transform[] players; // Array of player transforms
    public Transform ball; // Transform of the ball
    public Transform[] initialPlayerPositions; // Array to store initial player positions
    public Vector3 initialBallPosition; // Vector3 to store initial ball position

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        // Store initial positions
        StoreInitialPositions();

        // Reset positions at the start of the game
        ResetPositions();
    }

    void StoreInitialPositions()
    {
        initialPlayerPositions = new Transform[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            initialPlayerPositions[i] = players[i];
        }
        initialBallPosition = ball.position;
    }

    public void ResetPositions()
    {
        // Reset player positions
        for (int i = 0; i < players.Length; i++)
        {
            players[i].position = initialPlayerPositions[i].position;
        }

        // Reset ball position
        ball.position = initialBallPosition;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero; // Stop the ball
    }

    public void FreezeGame()
    {
        IsGameFrozen = true;
        // Show your UI here, e.g.:
        // UIManager.Instance.ShowFreezePanel();
    }

    public void UnfreezeGame()
    {
        IsGameFrozen = false;
        // Hide your UI here, e.g.:
        // UIManager.Instance.HideFreezePanel();
    }

    public void HandleDuel(GameObject offPlayer, GameObject defPlayer, int duelType)
    {
        /* 0 field 1 shoot*/
        GameManager.Instance.FreezeGame();

        OffPlayer = offPlayer;
        DefPlayer = defPlayer;
        DuelType = duelType;
        OffPlayerIsAlly = OffPlayer.GetComponent<Player>().isAlly;
        
        switch (duelType)
        {
            case 0:
                if (OffPlayerIsAlly) {
                    //ui dribble

                } else {
                    //uiblock
                }
                break;
            default:
                Debug.LogWarning("Unknown duelType: " + duelType);
                break;
        }

        UIManager.Instance.ShowPanelBottom();
    }

    public void ExecuteDuel(int command, string secretId)
    {
        Command = command;
        SecretId = secretId;

        float OffDamage = DamageCalc(OffPlayer, DuelType, 0, Command, SecretId);
    }

    public float DamageCalc(GameObject playerObject, int duelType, int action, int command, string secretId)
    {
        /*
            duelType: 
                0 field
                1 goal
            action:
                0 off
                1 def
            command:
                0 secret
                1 strength
                2 technique
        */
        string formulaId = "" + duelType + action + command;


        Player player = playerObject.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("No Player script found on this GameObject!");
            return 0f;
        }

        float damage = 0f;
        if (secretId == null) {
            switch (formulaId)
            {
                case "001":
                    /*dribble strength*/
                    damage = player.control + player.body * 0.05f + player.stamina * 0.02f + player.courage;
                    break;
                case "002":
                    /*dribble technique*/
                    damage = player.control + player.body * 0.05f + player.speed * 0.02f + player.courage;
                    break;
                case "011":
                    /*block strength*/
                    damage = player.body + player.guard * 0.05f + player.stamina * 0.02f + player.courage;
                    break;
                case "012":
                    /*block technique*/
                    damage = player.body + player.guard * 0.05f + player.control * 0.02f + player.courage;
                    break;

                default:
                    Debug.LogWarning("Unknown formulaId: " + formulaId);
                    break;
            }
        } else
        {

        }
        return damage;
    }

    // Call this method when a goal is scored
    public void OnGoalScored()
    {
        // Reset positions after a goal
        ResetPositions();

        // Additional logic for handling a goal (e.g., updating score) can be added here
    }
}
